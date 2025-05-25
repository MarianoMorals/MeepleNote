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
            MostPlayedGameImage.Source = mostPlayed.FotoPortada ?? "placeholder.png";
            MostPlayedGameLabel.Text = mostPlayed.Titulo;
            PlayCountLabel.Text = $"{mostPlayed.PartidasCount} partidas";
        }

        // 3. Últimas partidas - Versión optimizada
        var recentGames = await _dbService.GetPartidasAsync(userId);
        var viewModels = new List<PartidaViewModel>();

        foreach (var partida in recentGames.OrderByDescending(p => p.Fecha).Take(3)) {
            var juego = await _dbService.GetJuegoByIdAsync(partida.IdJuego);
            viewModels.Add(new PartidaViewModel {
                IdPartida = partida.IdPartida,
                IdJuego = partida.IdJuego,
                TituloJuego = juego?.Titulo ?? "Juego no encontrado",
                FotoPortada = juego?.FotoPortada ?? "placeholder.png",
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
        var juegos = await _dbService.GetJuegosAsyncEnColeccion(userId);

        var juegoMasJugado = partidas
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