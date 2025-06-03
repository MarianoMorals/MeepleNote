using MeepleNote.Models;
using MeepleNote.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MeepleNote.Views {
    public partial class DetalleJuegoPage : ContentPage {
        private readonly SQLiteService _dbService;
        private readonly ExplorarService _explorarService;
        private readonly Juego _juego;
        private bool _juegoEnColeccion;
        private bool _isNavigating = false;

        public ObservableCollection<PartidaViewModel> Partidas { get; } = new ObservableCollection<PartidaViewModel>();

        public DetalleJuegoPage(Juego juego) {
            InitializeComponent();
            BindingContext = this;

            if (juego == null) {
                DisplayAlert("Error", "Juego no válido", "OK");
                return;
            }

            _dbService = new SQLiteService();
            _explorarService = new ExplorarService();
            _juego = juego ?? throw new ArgumentNullException(nameof(juego));

            try {
                ComprobarJuegoEnColeccion();
                CargarDatosIniciales();
                CargarDetallesCompletos();
                //CargarPartidasRecientes();
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error inicializando página: {ex.Message}");
            }
        }

        protected override async void OnAppearing() {
            base.OnAppearing();
            await CargarPartidasRecientes();
        }

        private async void ComprobarJuegoEnColeccion() {
            _juegoEnColeccion = await _dbService.JuegoExisteEnColeccionAsync(_juego.Id);
        }

        private void CargarDatosIniciales() {
            ImagenPortada.Source = _juego.FotoPortada;
            Titulo.Text = _juego.Titulo;
            Puntuacion.Text = _juego.PuntuacionFormateada;

            if (_juego.PuntuacionPersonal >= 1 && _juego.PuntuacionPersonal <= 5) {
                PuntuacionPicker.SelectedIndex = (int)_juego.PuntuacionPersonal - 1;
            }

            Duracion.Text = $"⏱ {_juego.DuracionEstimada} min";
            Edad.Text = $"🧒 +{_juego.Edad} años";
            Autor.Text = $"🎨 {_juego.Autor}";
            Artista.Text = $"✏️ {_juego.Artista}";
            Descripcion.Text = "Cargando descripción...";
        }

        private async void CargarDetallesCompletos() {
            var juegoCompleto = await _explorarService.ObtenerDetallesJuegoAsync(_juego.IdJuego);
            if (juegoCompleto != null) {
                Device.BeginInvokeOnMainThread(() => {
                    Puntuacion.Text = juegoCompleto.PuntuacionFormateada;
                    Jugadores.Text = $"👥 {juegoCompleto.RangoJugadores} jugadores";
                    Descripcion.Text = juegoCompleto.Descripcion;

                    _juego.Descripcion = juegoCompleto.Descripcion;
                    _juego.MinJugadores = juegoCompleto.MinJugadores;
                    _juego.MaxJugadores = juegoCompleto.MaxJugadores;
                    _juego.IdUsuario = Preferences.Get("UsuarioId", "0");
                });
            }
        }

        private async Task CargarPartidasRecientes() {
            try {
                Partidas.Clear();
                var partidas = await _dbService.GetPartidasByJuegoAsync(_juego.IdJuego);

                if (partidas != null && partidas.Any()) {
                    foreach (var partida in partidas.OrderByDescending(p => p.Fecha).Take(5)) {
                        Partidas.Add(new PartidaViewModel {
                            IdPartida = partida.IdPartida,
                            TituloJuego = _juego.Titulo,
                            FotoPortada = _juego.FotoPortada,
                            Fecha = partida.Fecha,
                            Ganador = partida.Ganador
                        });
                    }
                }
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error cargando partidas: {ex.Message}");
            }
        }

        private async void OnPuntuacionChanged(object sender, EventArgs e) {
            if (PuntuacionPicker.SelectedIndex >= 0) {
                bool estabaEnColeccion = _juegoEnColeccion;
                _juego.PuntuacionPersonal = PuntuacionPicker.SelectedIndex + 1;
                var idUsuario = Preferences.Get("UsuarioId", "0");
                _juego.IdUsuario = idUsuario;

                if (estabaEnColeccion) {
                    await _dbService.ActualizarPuntuacionJuegoAsync(_juego.IdJuego, _juego.IdUsuario, _juego.PuntuacionPersonal);
                }
                else {
                    await _dbService.SaveJuegoAsync(_juego);
                }
            }
        }

        private async void OnRegistrarPartidaClicked(object sender, EventArgs e) {
            if (_isNavigating) return;

            try {
                _isNavigating = true;
                if (sender is Button button) button.IsEnabled = false;

                await Navigation.PushAsync(new RegistrarPartidaPage(_juego));
                await Task.Delay(500); // Pequeña espera antes de recargar
                await CargarPartidasRecientes();
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error al navegar: {ex.Message}");
                await DisplayAlert("Error", "No se pudo abrir el registro de partida.", "OK");
            }
            finally {
                _isNavigating = false;
                if (sender is Button button) button.IsEnabled = true;
            }
        }

        public Command<PartidaViewModel> PartidaTapCommand => new Command<PartidaViewModel>(async (partida) => {
            if (_isNavigating || partida == null) return;

            try {
                _isNavigating = true;
                var partidaCompleta = await _dbService.GetPartidaByIdAsync(partida.IdPartida);

                if (partidaCompleta != null) {
                    await Navigation.PushAsync(new DetallePartidaPage(partidaCompleta));
                }
                else {
                    await DisplayAlert("Error", "No se encontró la partida.", "OK");
                }
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error al navegar: {ex.Message}");
                await DisplayAlert("Error", "No se pudo abrir la partida.", "OK");
            }
            finally {
                _isNavigating = false;
            }
        });

        private async void OnCrearPartidaPublicaClicked(object sender, EventArgs e) {
            if (_isNavigating) return;

            if (!NetworkUtils.TieneConexionInternet()) {
                await DisplayAlert("Sin conexión", "Necesitas conexión a Internet para publicar una partida.", "OK");
                return;
            }

            try {
                _isNavigating = true;
                if (sender is Button button) button.IsEnabled = false;

                await Navigation.PushAsync(new NuevaPartidaPublicaPage(_juego));
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error al navegar: {ex.Message}");
                await DisplayAlert("Error", "No se pudo crear la partida pública.", "OK");
            }
            finally {
                _isNavigating = false;
                if (sender is Button button) button.IsEnabled = true;
            }
        }
    }
}