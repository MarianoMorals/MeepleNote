using MeepleNote.Models;
using MeepleNote.Services;

namespace MeepleNote.Views;

public partial class PrincipalPage : ContentPage {
    private readonly SQLiteService _dbService = new SQLiteService();

    public PrincipalPage() {
        InitializeComponent();
        LoadData();
    }

    protected override void OnAppearing() {
        base.OnAppearing();
        LoadData();
    }

    private async void LoadData() {
        var userId = Preferences.Get("UsuarioId", "0");

        // 1. Cargar nombre de usuario
        var user = await _dbService.GetUsuarioByFirebaseIdAsync(userId);
        UserNameLabel.Text = $"¡Hola, {user?.Nombre ?? "Jugador"}!";

        // 2. Juego más jugado
        var mostPlayed = await GetMostPlayedGame(userId);
        if (mostPlayed != null) {
            MostPlayedGameImage.IsVisible = true;
            MostPlayedGameLabel.IsVisible = true;
            PlayCountLabel.IsVisible = true;
            MostPlayedGameImage.Source = mostPlayed.FotoPortada ?? "missing_image.png";
            MostPlayedGameLabel.Text = mostPlayed.Titulo;
            PlayCountLabel.Text = $"{mostPlayed.PartidasCount} partidas";
            NoMostPlayedLabel.IsVisible = false;
        }
        else {
            MostPlayedGameImage.IsVisible = false;
            MostPlayedGameLabel.IsVisible = false;
            PlayCountLabel.IsVisible = false;
            NoMostPlayedLabel.IsVisible = true;
        }

        // 3. Últimas partidas
        var recentGames = await _dbService.GetPartidasAsync(userId);
        var viewModels = new List<PartidaViewModel>();

        foreach (var partida in recentGames.OrderByDescending(p => p.Fecha).Take(3)) {
            var juego = await _dbService.GetJuegoByIdAsync(partida.IdJuego);
            viewModels.Add(new PartidaViewModel {
                IdPartida = partida.IdPartida,
                IdJuego = partida.IdJuego,
                TituloJuego = juego?.Titulo ?? "Juego no encontrado",
                FotoPortada = juego?.FotoPortada ?? "missing_image.png",
                Fecha = partida.Fecha,
                Ganador = partida.Ganador
            });
        }

        RecentGamesCollection.ItemsSource = viewModels;

        // 4. Estadísticas
        TotalGamesLabel.Text = (await _dbService.GetJuegosAsyncEnColeccion(userId)).Count.ToString();
        TotalMatchesLabel.Text = recentGames.Count.ToString();
    }

    private async Task<JuegoConPartidas> GetMostPlayedGame(string userId) {
        var partidas = await _dbService.GetPartidasAsync(userId);

        // Si no hay partidas, retornar null
        if (!partidas.Any()) {
            return null;
        }

        var juegos = await _dbService.GetJuegosAsync(userId);
        var juegoMasJugado = partidas
            .Where(p => p.IdUsuario == userId)
            .GroupBy(p => p.IdJuego)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        if (juegoMasJugado != null) {
            var juego = juegos.FirstOrDefault(j => j.IdJuego == juegoMasJugado.Key);
            if (juego != null) {
                return new JuegoConPartidas {
                    IdJuego = juego.IdJuego,
                    Titulo = juego.Titulo,
                    FotoPortada = juego.FotoPortada,
                    PartidasCount = juegoMasJugado.Count()
                };
            }
        }
        return null;
    }

    private async void OnPerfilClicked(object sender, EventArgs e) {
        await Shell.Current.GoToAsync("//PerfilPage");
    }

    private async void OnViewCollectionClicked(object sender, EventArgs e) {
        await Shell.Current.GoToAsync("//ColeccionPage");
    }
}

// Clase auxiliar para el juego más jugado
public class JuegoConPartidas {
    public int IdJuego { get; set; }
    public string Titulo { get; set; }
    public string FotoPortada { get; set; }
    public int PartidasCount { get; set; }
}