using MeepleNote.Models;
using MeepleNote.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MeepleNote.Views {
    public partial class RegistrarPartidaPage : ContentPage, INotifyPropertyChanged {
        private readonly SQLiteService _dbService;
        private readonly ExplorarService _explorarService;
        private Juego _juego;
        private ObservableCollection<JugadorTemp> _jugadores;
        private ObservableCollection<JugadorTemp> _jugadoresParaGanador;

        public DateTime TodayDate => DateTime.Today;
        public string TituloJuego => _juego?.Titulo ?? "Juego no registrado";
        public DateTime Fecha { get; set; } = DateTime.Now;
        public Juego Juego => _juego;

        public ObservableCollection<JugadorTemp> JugadoresParaGanador {
            get => _jugadoresParaGanador;
            set {
                _jugadoresParaGanador = value;
                OnPropertyChanged();
            }
        }

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

        public RegistrarPartidaPage(int idJuego, string nombreJuego) : this(new Juego {
            IdJuego = idJuego,
            Titulo = nombreJuego
        }) {
            // Cargar imagen del juego si existe en BGG
            _ = CargarImagenJuego();
        }

        private async Task CargarImagenJuego() {
            if (_juego.IdJuego > 0 && string.IsNullOrEmpty(_juego.FotoPortada)) {
                var juegoCompleto = await _explorarService.ObtenerDetallesJuegoAsync(_juego.IdJuego);
                if (juegoCompleto != null) {
                    _juego.FotoPortada = juegoCompleto.FotoPortada;
                    OnPropertyChanged(nameof(Juego));
                }
            }
        }

        private void OnAgregarJugadorClicked(object sender, EventArgs e) {
            _jugadores.Add(new JugadorTemp());
            JugadoresCollection.ItemsSource = _jugadores;
            ActualizarListaGanadores(); // ? Añade esta línea
        }


        private void OnJugadorEntryCompleted(object sender, EventArgs e) {
            ActualizarListaGanadores();
        }

        private void OnEliminarJugadorClicked(object sender, EventArgs e) {
            if (sender is Button button && button.BindingContext is JugadorTemp jugador) {
                _jugadores.Remove(jugador);
                ActualizarListaGanadores();
            }
        }

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
            if (_juego.IdJuego > 0 && !await _dbService.JuegoExisteAsync(_juego.IdJuego)) {
                _juego.EnColeccion = false;//Lo añadimos pero no a la coleccion.
                await _dbService.SaveJuegoAsync(_juego);
            }

            var partida = new Partida {
                IdJuego = _juego.IdJuego,
                IdUsuario = Preferences.Get("IdUsuario", 1),
                Fecha = FechaPicker.Date,
                Ganador = (GanadorPicker.SelectedItem as JugadorTemp)?.Nombre
            };

            try {
                // Guardar partida
                int idGenerado = await _dbService.SavePartidaAsync(partida);

                // Guardar jugadores
                foreach (var jugador in _jugadores) {
                    await _dbService.SaveJugadorPartidaAsync(new JugadorPartida {
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

    public class JugadorTemp : INotifyPropertyChanged {
        private string _nombre;
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