using MeepleNote.Models;

namespace MeepleNote.Views {
    public partial class DetalleJuegoPage : ContentPage {
        public DetalleJuegoPage(Juego juego) {
            InitializeComponent();

            ImagenPortada.Source = juego.FotoPortada;
            Titulo.Text = juego.Titulo;
            Puntuacion.Text = $"Puntuación: {juego.Puntuacion:F1}";
            Jugadores.Text = $"Jugadores: {juego.NumeroJugadores}+";
            Duracion.Text = $"Duración: {juego.DuracionEstimada} min";
            Edad.Text = $"Edad mínima: {juego.Edad}+";
            Autor.Text = $"Autor: {juego.Autor}";
            Artista.Text = $"Artista: {juego.Artista}";
        }
    }
}
