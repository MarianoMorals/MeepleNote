using System.Xml.Linq;
using MeepleNote.Models;

public class ExplorarService {
    private readonly HttpClient _httpClient;

    public ExplorarService() {
        _httpClient = new HttpClient();
    }

    public async Task<List<Juego>> BuscarJuegosAsync(string query) {
        string urlBusqueda = $"https://www.boardgamegeek.com/xmlapi2/search?query={Uri.EscapeDataString(query)}&type=boardgame";
        var response = await _httpClient.GetAsync(urlBusqueda);

        if (!response.IsSuccessStatusCode)
            return new List<Juego>();

        var xml = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(xml);

        var items = doc.Descendants("item").ToList();
        if (!items.Any())
            return new List<Juego>();

        var juegos = new List<Juego>();

        // Limita la cantidad para evitar sobrecargar el servidor
        //foreach (var item in items.Take(10)) {
        foreach (var item in items) {
                int id = int.Parse(item.Attribute("id")?.Value ?? "0");

            var detalles = await ObtenerDetallesJuegoAsync(id);
            if (detalles != null) {
                juegos.Add(detalles);
            }
        }

        return juegos;
    }


    public async Task<Juego?> ObtenerDetallesJuegoAsync(int idJuego) {
        string url = $"https://www.boardgamegeek.com/xmlapi2/thing?id={idJuego}&stats=1";
        var response = await _httpClient.GetAsync(url);

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
            NumeroJugadores = int.TryParse(item.Element("minplayers")?.Attribute("value")?.Value, out var jugadores) ? jugadores : 0,
            DuracionEstimada = item.Element("playingtime")?.Attribute("value")?.Value ?? "N/D",
            Edad = int.TryParse(item.Element("minage")?.Attribute("value")?.Value, out var edad) ? edad : 0,
            Autor = item.Elements("link").FirstOrDefault(l => l.Attribute("type")?.Value == "boardgamedesigner")?.Attribute("value")?.Value ?? "Desconocido",
            Artista = item.Elements("link").FirstOrDefault(l => l.Attribute("type")?.Value == "boardgameartist")?.Attribute("value")?.Value ?? "Desconocido"
        };
    }

    public async Task<List<Juego>> BuscarSugerenciasAsync(string query) {
        string url = $"https://www.boardgamegeek.com/xmlapi2/search?query={Uri.EscapeDataString(query)}&type=boardgame";
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


}
