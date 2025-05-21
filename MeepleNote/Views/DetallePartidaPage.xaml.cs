using MeepleNote.Models;
using MeepleNote.Services;
using System.Collections.ObjectModel;

namespace MeepleNote.Views {
    public partial class DetallePartidaPage : ContentPage {
        public string TituloJuego { get; set; }
        public DateTime Fecha { get; set; }
        public string Ganador { get; set; }
        public ObservableCollection<JugadorPartida> Jugadores { get; } = new();
        private Partida _partida;

        public DetallePartidaPage(Partida partida) {
            InitializeComponent();
            BindingContext = this;

            CargarDatosPartida(partida);
        }

        private async void CargarDatosPartida(Partida partida) {
            _partida = partida; // Guardamos la referencia

            var dbService = new SQLiteService();
            var juego = await dbService.GetJuegoByIdAsync(partida.IdJuego);

            TituloJuego = juego?.Titulo ?? "Juego desconocido";
            Fecha = partida.Fecha;
            Ganador = partida.Ganador;

            var jugadores = await dbService.GetJugadoresByPartidaAsync(partida.IdPartida);
            foreach (var jugador in jugadores) {
                Jugadores.Add(jugador);
            }

            OnPropertyChanged(nameof(TituloJuego));
            OnPropertyChanged(nameof(Fecha));
            OnPropertyChanged(nameof(Ganador));
        }
        private async void OnEliminarPartidaClicked(object sender, EventArgs e) {
            var confirmacion = await DisplayAlert("Confirmar", "¿Seguro que quieres eliminar esta partida?", "Sí", "Cancelar");

            if (confirmacion && _partida != null) {
                var dbService = new SQLiteService();
                await dbService.EliminarPartidaAsync(_partida.IdPartida);

                await DisplayAlert("Éxito", "La partida ha sido eliminada.", "OK");

                // Volver a la página anterior
                await Navigation.PopAsync();
            }
        }

    }
}