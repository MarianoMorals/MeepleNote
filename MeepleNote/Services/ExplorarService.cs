using System.Diagnostics;
using System.Xml.Linq;
using MeepleNote.Models;

public class ExplorarService {
    private readonly HttpClient _httpClient;

    public ExplorarService() {
        _httpClient = new HttpClient {
            BaseAddress = new Uri("https://boardgamegeek.com/xmlapi2/")
        };
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<List<Juego>> BuscarJuegosAsync(string query, int start = 0, int limit = 10) {
        try {
            string url = $"search?query={Uri.EscapeDataString(query)}&type=boardgame&start={start}&max={limit}";
            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.Accepted) {
                await Task.Delay(1000).ConfigureAwait(false);
                response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            }

            if (!response.IsSuccessStatusCode)
                return new List<Juego>();

            var xml = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var doc = XDocument.Parse(xml);

            // Primero obtenemos todos los IDs
            var ids = doc.Descendants("item")
                        .Select(item => int.Parse(item.Attribute("id")?.Value ?? "0"))
                        .ToList();

            // Luego procesamos en paralelo los detalles
            var detallesTasks = ids.Select(id => ObtenerDetallesJuegoAsync(id));
            var resultados = await Task.WhenAll(detallesTasks);

            return resultados.Where(j => j != null).ToList();
        }
        catch (Exception ex) {
            Debug.WriteLine($"Error en BuscarJuegosAsync: {ex.Message}");
            return new List<Juego>();
        }
    }

    public async Task<Juego?> ObtenerDetallesJuegoAsync(int idJuego) {
        try {
            string url = $"thing?id={idJuego}&stats=1";
            var response = await _httpClient.GetAsync(url);

            // Retry si la API está procesando
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted) {
                await Task.Delay(2000);
                response = await _httpClient.GetAsync(url);
            }

            if (!response.IsSuccessStatusCode)
                return null;

            var xml = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(xml);
            var item = doc.Descendants("item").FirstOrDefault();

            if (item == null)
                return null;

            return new Juego {
                IdJuego = idJuego,
                Titulo = item.Element("name")?.Attribute("value")?.Value ?? "Sin título",
                FotoPortada = item.Element("image")?.Value ?? "",
                Puntuacion = double.TryParse(
                    item.Element("statistics")?
                        .Element("ratings")?
                        .Element("average")?
                        .Attribute("value")?.Value,
                    out var rating) ? rating : 0,
                NumeroJugadores = int.TryParse(
                    item.Element("minplayers")?.Attribute("value")?.Value,
                    out var jugadores) ? jugadores : 0,
                DuracionEstimada = item.Element("playingtime")?.Attribute("value")?.Value ?? "N/D",
                Edad = int.TryParse(
                    item.Element("minage")?.Attribute("value")?.Value,
                    out var edad) ? edad : 0,
                Autor = item.Elements("link")
                    .FirstOrDefault(l => l.Attribute("type")?.Value == "boardgamedesigner")?
                    .Attribute("value")?.Value ?? "Desconocido",
                Artista = item.Elements("link")
                    .FirstOrDefault(l => l.Attribute("type")?.Value == "boardgameartist")?
                    .Attribute("value")?.Value ?? "Desconocido"
            };
        }
        catch (Exception ex) {
            Console.WriteLine($"Error en ObtenerDetallesJuegoAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Juego>> BuscarSugerenciasAsync(string query) {
        try {
            string url = $"search?query={Uri.EscapeDataString(query)}&type=boardgame";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<Juego>();

            var xml = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(xml);

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