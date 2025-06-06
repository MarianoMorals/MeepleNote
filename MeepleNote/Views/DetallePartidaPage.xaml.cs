using MeepleNote.Models;
using MeepleNote.Services;
using System.Collections.ObjectModel;

namespace MeepleNote.Views {
    /// <summary>
    /// Página que muestra los detalles de una partida registrada.
    /// Permite visualizar información del juego, fecha, ganador y participantes.
    /// También permite eliminar la partida si el usuario lo desea.
    /// </summary>
    public partial class DetallePartidaPage : ContentPage {
        // Título del juego correspondiente a la partida
        public string TituloJuego { get; set; }

        // Fecha en que se jugó la partida
        public DateTime Fecha { get; set; }

        // Nombre del ganador de la partida
        public string Ganador { get; set; }

        // Lista observable de jugadores que participaron en la partida
        public ObservableCollection<JugadorPartida> Jugadores { get; } = new();

        // Referencia interna a la partida actual
        private Partida _partida;

        /// <summary>
        /// Constructor: Inicializa componentes y carga los datos de la partida.
        /// </summary>
        /// <param name="partida">Partida seleccionada para mostrar sus detalles.</param>
        public DetallePartidaPage(Partida partida) {
            InitializeComponent();
            BindingContext = this;
            CargarDatosPartida(partida); // Carga detalles
        }

        /// <summary>
        /// Carga los datos de la partida desde la base de datos y actualiza la interfaz.
        /// </summary>
        /// <param name="partida">Partida de la que se deben obtener los detalles.</param>
        private async void CargarDatosPartida(Partida partida) {
            _partida = partida; // Guarda la partida actual

            var dbService = new SQLiteService();

            // Obtiene el título del juego
            var juego = await dbService.GetJuegoByIdAsync(partida.IdJuego);
            TituloJuego = juego?.Titulo ?? "Juego desconocido";

            // Asigna valores básicos
            Fecha = partida.Fecha;
            Ganador = partida.Ganador;

            // Carga los jugadores de la partida
            var jugadores = await dbService.GetJugadoresByPartidaAsync(partida.IdPartida, partida.IdUsuario);
            foreach (var jugador in jugadores) {
                Jugadores.Add(jugador);
            }

            // Notifica los cambios a la interfaz
            OnPropertyChanged(nameof(TituloJuego));
            OnPropertyChanged(nameof(Fecha));
            OnPropertyChanged(nameof(Ganador));
            OnPropertyChanged(nameof(Jugadores));
        }

        /// <summary>
        /// Evento al hacer clic en el botón "Eliminar Partida".
        /// Solicita confirmación y elimina la partida si se acepta.
        /// </summary>
        private async void OnEliminarPartidaClicked(object sender, EventArgs e) {
            // Solicita confirmación
            var confirmacion = await DisplayAlert("Confirmar", "¿Seguro que quieres eliminar esta partida?", "Sí", "Cancelar");

            if (confirmacion && _partida != null) {
                var dbService = new SQLiteService();

                // Elimina la partida de la base de datos
                await dbService.EliminarPartidaAsync(_partida.IdPartida, _partida.IdUsuario);

                await DisplayAlert("Éxito", "La partida ha sido eliminada.", "OK");

                // Regresa a la página anterior
                await Navigation.PopAsync();
            }
        }
    }
}
