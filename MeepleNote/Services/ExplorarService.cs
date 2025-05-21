using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MeepleNote.Models;

// Clase que gestiona la búsqueda y obtención de detalles de juegos desde la API de BoardGameGeek.
public class ExplorarService {
    private readonly HttpClient _httpClient;  // Cliente HTTP usado para hacer peticiones a la API.

    // Constructor: configura la URL base y el tiempo de espera del cliente HTTP.
    public ExplorarService() {
        _httpClient = new HttpClient {
            BaseAddress = new Uri("https://boardgamegeek.com/xmlapi2/") // URL base de la API.
        };
        _httpClient.Timeout = TimeSpan.FromSeconds(30); // Tiempo máximo de espera para respuestas.
    }

    // Busca juegos por nombre. Retorna una lista de objetos Juego.
    public async Task<List<Juego>> BuscarJuegosAsync(string query, int start = 0, int limit = 10) {
        try {
            // Construye la URL con los parámetros de búsqueda.
            string url = $"search?query={Uri.EscapeDataString(query)}&type=boardgame&start={start}&max={limit}";

            // Realiza una solicitud GET a la API.
            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);

            // Si la respuesta es 202 (Accepted), espera 1 segundo y vuelve a intentar.
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted) {
                await Task.Delay(1000).ConfigureAwait(false);
                response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            }

            // Si la respuesta no es exitosa, retorna una lista vacía.
            if (!response.IsSuccessStatusCode)
                return new List<Juego>();

            // Lee el contenido XML de la respuesta.
            var xml = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var doc = XDocument.Parse(xml); // Parsea el XML.

            // Extrae todos los IDs de los juegos devueltos.
            var ids = doc.Descendants("item")
                        .Select(item => int.Parse(item.Attribute("id")?.Value ?? "0"))
                        .ToList();

            // Para cada ID, obtiene detalles completos del juego en paralelo.
            var detallesTasks = ids.Select(id => ObtenerDetallesJuegoAsync(id));
            var resultados = await Task.WhenAll(detallesTasks); // Espera que terminen todas las tareas.

            // Retorna solo los resultados no nulos.
            return resultados.Where(j => j != null).ToList();
        }
        catch (Exception ex) {
            Debug.WriteLine($"Error en BuscarJuegosAsync: {ex.Message}"); // Muestra error en consola.
            return new List<Juego>();
        }
    }

    // Obtiene los detalles completos de un juego por su ID desde la API.
    public async Task<Juego?> ObtenerDetallesJuegoAsync(int idJuego) {
        try {
            // Construye la URL con el ID del juego.
            string url = $"thing?id={idJuego}&stats=1";
            var response = await _httpClient.GetAsync(url); // Solicita los detalles del juego.

            // Si aún está procesando, espera 2 segundos y vuelve a intentar.
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted) {
                await Task.Delay(2000);
                response = await _httpClient.GetAsync(url);
            }

            // Si la respuesta no es exitosa, retorna null.
            if (!response.IsSuccessStatusCode)
                return null;

            // Lee el contenido XML y lo parsea.
            var xml = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(xml);
            var item = doc.Descendants("item").FirstOrDefault(); // Toma el primer item (juego).

            if (item == null)
                return null;

            // Obtiene y decodifica la descripción (puede tener caracteres HTML).
            var description = item.Element("description")?.Value ?? "Descripción no disponible";
            description = System.Net.WebUtility.HtmlDecode(description);

            // Retorna un objeto Juego con los datos obtenidos del XML.
            return new Juego {
                IdJuego = idJuego,
                Titulo = item.Element("name")?.Attribute("value")?.Value ?? "Sin título",
                FotoPortada = item.Element("image")?.Value ?? "",
                Puntuacion = double.TryParse(
                    item.Element("statistics")?
                        .Element("ratings")?
                        .Element("average")?
                        .Attribute("value")?.Value,
                    out var rating) ? Math.Clamp(rating/100000, 1, 10) : 0, // Convierte a número y limita entre 1-10.

                MinJugadores = int.TryParse(
                    item.Element("minplayers")?.Attribute("value")?.Value,
                    out var minPlayers) ? minPlayers : 0,

                MaxJugadores = int.TryParse(
                    item.Element("maxplayers")?.Attribute("value")?.Value,
                    out var maxPlayers) ? maxPlayers : 0,

                DuracionEstimada = item.Element("playingtime")?.Attribute("value")?.Value ?? "N/D",

                Edad = int.TryParse(
                    item.Element("minage")?.Attribute("value")?.Value,
                    out var edad) ? edad : 0,

                Autor = item.Elements("link")
                    .FirstOrDefault(l => l.Attribute("type")?.Value == "boardgamedesigner")?
                    .Attribute("value")?.Value ?? "Desconocido",

                Artista = item.Elements("link")
                    .FirstOrDefault(l => l.Attribute("type")?.Value == "boardgameartist")?
                    .Attribute("value")?.Value ?? "Desconocido",

                Descripcion = description,
                
                EnColeccion = true
            };
        }
        catch (Exception ex) {
            Console.WriteLine($"Error en ObtenerDetallesJuegoAsync: {ex.Message}"); // Muestra error.
            return null;
        }
    }

    // Método auxiliar que limpia una descripción en HTML.
    private string LimpiarDescripcion(string html) {
        // Elimina etiquetas HTML.
        string descripcion = Regex.Replace(html, "<[^>]*>", string.Empty);

        // Decodifica caracteres especiales (&amp;, &lt;, etc.).
        descripcion = System.Net.WebUtility.HtmlDecode(descripcion);

        // Limita la longitud a 1000 caracteres.
        return descripcion.Length > 1000
            ? descripcion.Substring(0, 1000) + "..."
            : descripcion;
    }

    // Realiza una búsqueda básica para autocompletado o sugerencias (sin detalles).
    public async Task<List<Juego>> BuscarSugerenciasAsync(string query) {
        try {
            // Construye la URL para buscar sugerencias.
            string url = $"search?query={Uri.EscapeDataString(query)}&type=boardgame";
            var response = await _httpClient.GetAsync(url);

            // Si la respuesta falla, retorna una lista vacía.
            if (!response.IsSuccessStatusCode)
                return new List<Juego>();

            // Lee el XML y lo parsea.
            var xml = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(xml);

            // Devuelve hasta 10 juegos con su ID y título solamente.
            return doc.Descendants("item")
                .Take(10)
                .Select(item => new Juego {
                    IdJuego = int.Parse(item.Attribute("id")?.Value ?? "0"),
                    Titulo = item.Element("name")?.Attribute("value")?.Value ?? "Sin título"
                })
                .ToList();
        }
        catch (Exception ex) {
            Console.WriteLine($"Error en BuscarSugerenciasAsync: {ex.Message}");
            return new List<Juego>();
        }
    }
}
