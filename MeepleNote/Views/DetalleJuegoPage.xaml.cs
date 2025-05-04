using MeepleNote.Models;

namespace MeepleNote.Views {
    public partial class DetalleJuegoPage : ContentPage {
        public DetalleJuegoPage(Juego juego) {
            InitializeComponent();

            ImagenPortada.Source = juego.FotoPortada;
            Titulo.Text = juego.Titulo;
            Puntuacion.Text = $"Puntuaci�n: {juego.Puntuacion:F1}";
            Jugadores.Text = $"Jugadores: {juego.NumeroJugadores}+";
            Duracion.Text = $"Duraci�n: {juego.DuracionEstimada} min";
            Edad.Text = $"Edad m�nima: {juego.Edad}+";
            Autor.Text = $"Autor: {juego.Autor}";
            Artista.Text = $"Artista: {juego.Artista}";
        }
    }
}
