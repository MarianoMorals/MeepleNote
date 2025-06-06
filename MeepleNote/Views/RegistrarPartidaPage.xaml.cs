using MeepleNote.Models;
using MeepleNote.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MeepleNote.Views {

    /// <summary>
    /// Página para registrar una nueva partida de un juego, permitiendo introducir jugadores y seleccionar un ganador.
    /// </summary>
    public partial class RegistrarPartidaPage : ContentPage, INotifyPropertyChanged {
        private readonly SQLiteService _dbService;
        private readonly ExplorarService _explorarService;
        private Juego _juego;
        private ObservableCollection<JugadorTemp> _jugadores;
        private ObservableCollection<JugadorTemp> _jugadoresParaGanador;

        /// <summary>
        /// Fecha actual, usada como valor predeterminado.
        /// </summary>
        public DateTime TodayDate => DateTime.Today;

        /// <summary>
        /// Título del juego mostrado en la interfaz.
        /// </summary>
        public string TituloJuego => _juego?.Titulo ?? "Juego no registrado";

        /// <summary>
        /// Fecha seleccionada para la partida.
        /// </summary>
        public DateTime Fecha { get; set; } = DateTime.Now;

        /// <summary>
        /// Juego asociado a la partida.
        /// </summary>
        public Juego Juego => _juego;

        /// <summary>
        /// Lista de jugadores mostrados en el Picker de selección de ganador.
        /// </summary>
        public ObservableCollection<JugadorTemp> JugadoresParaGanador {
            get => _jugadoresParaGanador;
            set {
                _jugadoresParaGanador = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Constructor principal que recibe el objeto Juego directamente.
        /// </summary>
        public RegistrarPartidaPage(Juego juego) {
            InitializeComponent();
            _dbService = new SQLiteService();
            _explorarService = new ExplorarService();
            _juego = juego ?? new Juego { Titulo = "Juego no registrado" };
            _jugadores = new ObservableCollection<JugadorTemp>();
            _jugadoresParaGanador = new ObservableCollection<JugadorTemp>();

            BindingContext = this;
            JugadoresCollection.ItemsSource = _jugadores;

        }

        /// <summary>
        /// Constructor alternativo cuando se recibe solo el ID y el nombre del juego.
        /// </summary>
        public RegistrarPartidaPage(int idJuego, string nombreJuego) : this(new Juego {
            IdJuego = idJuego,
            Titulo = nombreJuego
        }) {
            // Cargar imagen del juego si existe en BGG
            _ = CargarImagenJuego();
        }

        /// <summary>
        /// Intenta obtener la imagen del juego desde la BGG si aún no está disponible localmente.
        /// </summary>
        private async Task CargarImagenJuego() {
            if (_juego.IdJuego > 0 && string.IsNullOrEmpty(_juego.FotoPortada)) {
                var juegoCompleto = await _explorarService.ObtenerDetallesJuegoAsync(_juego.IdJuego);
                if (juegoCompleto != null) {
                    _juego.FotoPortada = juegoCompleto.FotoPortada;
                    OnPropertyChanged(nameof(Juego));
                }
            }
        }

        /// <summary>
        /// Se ejecuta cuando el Entry de un jugador se completa.
        /// </summary>
        private void OnAgregarJugadorClicked(object sender, EventArgs e) {
            _jugadores.Add(new JugadorTemp());
            JugadoresCollection.ItemsSource = _jugadores;
            ActualizarListaGanadores();
        }

        /// <summary>
        /// Se ejecuta cuando el Entry de un jugador se completa.
        /// </summary>
        private void OnJugadorEntryCompleted(object sender, EventArgs e) {
            ActualizarListaGanadores();
        }

        /// <summary>
        /// Elimina un jugador de la lista.
        /// </summary>
        private void OnEliminarJugadorClicked(object sender, EventArgs e) {
            if (sender is Button button && button.BindingContext is JugadorTemp jugador) {
                _jugadores.Remove(jugador);
                ActualizarListaGanadores();
            }
        }

        /// <summary>
        /// Actualiza el listado de jugadores disponibles para seleccionar como ganador.
        /// </summary>
        private void ActualizarListaGanadores() {
            JugadoresParaGanador.Clear();
            foreach (var jugador in _jugadores.Where(j => !string.IsNullOrWhiteSpace(j.Nombre))) {
                JugadoresParaGanador.Add(jugador);
            }

            // Seleccionar el primer jugador por defecto si hay alguno
            if (JugadoresParaGanador.Count > 0 && GanadorPicker.SelectedItem == null) {
                GanadorPicker.SelectedItem = JugadoresParaGanador.First();
            }
        }

        /// <summary>
        /// Guarda la partida en la base de datos local.
        /// </summary>
        private async void OnGuardarPartidaClicked(object sender, EventArgs e) {
            if (GanadorPicker.SelectedItem == null) {
                await DisplayAlert("Error", "Debes seleccionar un ganador", "OK");
                return;
            }

            if (_jugadores.Count < 1) {
                await DisplayAlert("Error", "Debes añadir al menos un jugador", "OK");
                return;
            }

            // Si el juego no existe en la colección, guardar info básica
            var idUsuario = Preferences.Get("UsuarioId", "0"); //FirebaseId
            if (_juego.IdJuego > 0 && !await _dbService.JuegoExisteAsync(_juego.IdJuego, idUsuario)) {
                _juego.EnColeccion = false;//Lo añadimos pero no a la coleccion.
                await _dbService.SaveJuegoAsync(_juego);
            }

            var partida = new Partida {
                IdPartida = await _dbService.GetNuevoIdPartidaAsync(),  // Auto-incremento manual
                IdJuego = _juego.IdJuego,
                IdUsuario = Preferences.Get("UsuarioId", "1"), //FirebaseId
                Fecha = FechaPicker.Date,
                Ganador = (GanadorPicker.SelectedItem as JugadorTemp)?.Nombre
            };

            try {
                // Guardar partida
                int idGenerado = await _dbService.SavePartidaAsync(partida);

                // Guardar jugadores
                foreach (var jugador in _jugadores) {
                    await _dbService.SaveJugadorPartidaAsync(new JugadorPartida {
                        IdUsuario = Preferences.Get("UsuarioId", "1"), //FirebaseId
                        IdPartida = idGenerado,
                        NombreJugador = jugador.Nombre
                    });
                }

                await DisplayAlert("Éxito", "Partida registrada correctamente", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex) {
                await DisplayAlert("Error", $"No se pudo guardar la partida: {ex.Message}", "OK");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Clase temporal para representar un jugador antes de guardar la partida.
    /// </summary>
    public class JugadorTemp : INotifyPropertyChanged {

        private string _nombre;

        /// <summary>
        /// Nombre del jugador.
        /// </summary>
        public string Nombre {
            get => _nombre;
            set {
                _nombre = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}