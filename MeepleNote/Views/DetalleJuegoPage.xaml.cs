using MeepleNote.Models;
using MeepleNote.Services;
using System.Collections.ObjectModel;

namespace MeepleNote.Views {
    public partial class DetalleJuegoPage : ContentPage {
        private readonly SQLiteService _dbService;
        private readonly ExplorarService _explorarService;
        private readonly Juego _juego;
        private ObservableCollection<PartidaViewModel> _partidas;

        public DetalleJuegoPage(Juego juego) {
            InitializeComponent();

            if (juego == null) {
                DisplayAlert("Error", "Juego no válido", "OK");
                return;
            }

            _dbService = new SQLiteService();
            _explorarService = new ExplorarService();
            _juego = juego ?? throw new ArgumentNullException(nameof(juego));
            _partidas = new ObservableCollection<PartidaViewModel>();

            try {
                PartidasCollection.ItemsSource = _partidas;
                CargarDatosIniciales();
                CargarDetallesCompletos();
                CargarPartidasRecientes();
            }
            catch (Exception ex) {
                Console.WriteLine($"Error inicializando página: {ex.Message}");
            }
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
                Device.BeginInvokeOnMainThread(() =>
                {
                    // Actualizar con los datos completos de BGG
                    Puntuacion.Text = juegoCompleto.PuntuacionFormateada;
                    Jugadores.Text = $"👥 {juegoCompleto.RangoJugadores} jugadores";
                    Descripcion.Text = juegoCompleto.Descripcion;

                    // Actualizar el objeto juego
                    _juego.Descripcion = juegoCompleto.Descripcion;
                    _juego.MinJugadores = juegoCompleto.MinJugadores;
                    _juego.MaxJugadores = juegoCompleto.MaxJugadores;
                });
            }
        }

        private async void CargarPartidasRecientes() {
            var partidas = await _dbService.GetPartidasByJuegoAsync(_juego.IdJuego);
            _partidas.Clear();

            foreach (var partida in partidas.OrderByDescending(p => p.Fecha).Take(5)) {
                _partidas.Add(new PartidaViewModel {
                    IdPartida = partida.IdPartida,
                    TituloJuego = _juego.Titulo,
                    FotoPortada = _juego.FotoPortada,
                    Fecha = partida.Fecha,
                    Ganador = partida.Ganador
                });
            }
        }

        private void CargarDatosJuego() {
            ImagenPortada.Source = _juego.FotoPortada;
            Titulo.Text = _juego.Titulo;
            Puntuacion.Text = _juego.Puntuacion.ToString("0.00");
            Duracion.Text = $"⏱ {_juego.DuracionEstimada} min";
            Edad.Text = $"🧒 {_juego.Edad}+ años";
            Autor.Text = $"🎨 {_juego.Autor}";
            Artista.Text = $"✏️ {_juego.Artista}";
            Descripcion.Text = "Descripción del juego..."; // Aquí deberías obtener la descripción real
        }

        /*private async void CargarPartidasRecientes() {
            var partidas = await _dbService.GetPartidasByJuegoAsync(_juego.IdJuego);
            _partidas.Clear();

            foreach (var partida in partidas.OrderByDescending(p => p.Fecha).Take(5)) {
                _partidas.Add(new PartidaViewModel {
                    IdPartida = partida.IdPartida,
                    TituloJuego = _juego.Titulo,
                    FotoPortada = _juego.FotoPortada,
                    Fecha = partida.Fecha,
                    Ganador = partida.Ganador
                });
            }
        }*/
        private async void OnPuntuacionChanged(object sender, EventArgs e) {
            if (PuntuacionPicker.SelectedIndex >= 0) {
                _juego.PuntuacionPersonal = PuntuacionPicker.SelectedIndex + 1;
                await _dbService.SaveJuegoAsync(_juego);
            }
        }
        private async void OnRegistrarPartidaClicked(object sender, EventArgs e) {
            await Navigation.PushAsync(new RegistrarPartidaPage(_juego));

            // Esperar a que se cierre la página de registro
            this.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), () =>
            {
                CargarPartidasRecientes();
            });
        }

        private async void OnPartidaSelected(object sender, SelectionChangedEventArgs e) {
            if (e.CurrentSelection.FirstOrDefault() is PartidaViewModel partida) {
                var partidaCompleta = await _dbService.GetPartidaByIdAsync(partida.IdPartida);
                await Navigation.PushAsync(new DetallePartidaPage(partidaCompleta));
                PartidasCollection.SelectedItem = null;
            }
        }

        private async void OnCrearPartidaPublicaClicked(object sender, EventArgs e) {
            await Navigation.PushAsync(new NuevaPartidaPublicaPage(_juego));
        }
    }
}