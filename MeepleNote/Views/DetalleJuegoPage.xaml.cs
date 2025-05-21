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
            _dbService = new SQLiteService();
            _explorarService = new ExplorarService();
            _juego = juego;
            _partidas = new ObservableCollection<PartidaViewModel>();
            PartidasCollection.ItemsSource = _partidas;

            CargarDatosIniciales();
            CargarDetallesCompletos();
            CargarPartidasRecientes();
        }

        private void CargarDatosIniciales() {
            ImagenPortada.Source = _juego.FotoPortada;
            Titulo.Text = _juego.Titulo;
            Puntuacion.Text = _juego.PuntuacionFormateada;
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

        private async void OnRegistrarPartidaClicked(object sender, EventArgs e) {
            await Navigation.PushAsync(new RegistrarPartidaPage(_juego));
        }

        private async void OnPartidaSelected(object sender, SelectionChangedEventArgs e) {
            if (e.CurrentSelection.FirstOrDefault() is PartidaViewModel partida) {
                var partidaCompleta = await _dbService.GetPartidaByIdAsync(partida.IdPartida);
                await Navigation.PushAsync(new DetallePartidaPage(partidaCompleta));
                PartidasCollection.SelectedItem = null;
            }
        }
    }
}