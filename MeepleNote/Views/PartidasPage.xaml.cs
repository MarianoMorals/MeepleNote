using MeepleNote.Models;
using MeepleNote.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace MeepleNote.Views {
    /// <summary>
    /// Página que muestra la lista de partidas jugadas por el usuario.
    /// Permite refrescar la lista, navegar a detalles de una partida y acceder a partidas públicas.
    /// </summary>
    public partial class PartidasPage : ContentPage, INotifyPropertyChanged {
        private readonly SQLiteService _dbService;
        private ObservableCollection<PartidaViewModel> _partidas = new();

        private bool _isRefreshing;

        /// <summary>
        /// Indica si la lista está en proceso de actualización para mostrar el indicador de carga.
        /// </summary>
        public bool IsRefreshing {
            get => _isRefreshing;
            set {
                if (_isRefreshing != value) {
                    _isRefreshing = value;
                    OnPropertyChanged(nameof(IsRefreshing));
                }
            }
        }

        private bool _isNavigating = false;

        /// <summary>
        /// Colección observable con las partidas mostradas en la lista.
        /// </summary>
        public ObservableCollection<PartidaViewModel> Partidas {
            get => _partidas;
            set {
                if (_partidas != value) {
                    _partidas = value;
                    OnPropertyChanged();
                }
            }
        }

        private ICommand _refreshCommand;

        /// <summary>
        /// Comando para refrescar la lista de partidas.
        /// </summary>
        public ICommand RefreshCommand => _refreshCommand;

        /// <summary>
        /// Constructor que inicializa la página y servicios, y configura el comando de refresco.
        /// </summary>
        public PartidasPage() {
            InitializeComponent();
            _dbService = new SQLiteService();
            _refreshCommand = new Command(async () => await CargarPartidas());

            BindingContext = this;
        }

        /// <summary>
        /// Método que se ejecuta al aparecer la página.
        /// Carga la lista de partidas.
        /// </summary>
        protected override async void OnAppearing() {
            base.OnAppearing();
            await CargarPartidas();
        }

        /// <summary>
        /// Carga las partidas desde la base de datos local, ordenadas por fecha descendente.
        /// Convierte los datos a PartidaViewModel para su visualización.
        /// </summary>
        /// <returns>Tarea asincrónica.</returns>
        private async Task CargarPartidas() {
            IsRefreshing = true;

            try {
                var idUsuario = Preferences.Get("UsuarioId", "0");
                var partidasLocales = (await _dbService.GetPartidasAsync(idUsuario))
                    .OrderByDescending(p => p.Fecha)
                    .ToList();

                var nuevasPartidas = new ObservableCollection<PartidaViewModel>();
                foreach (var partida in partidasLocales) {
                    var juego = await _dbService.GetJuegoByIdAsync(partida.IdJuego);
                    nuevasPartidas.Add(new PartidaViewModel {
                        IdPartida = partida.IdPartida,
                        IdJuego = partida.IdJuego,
                        TituloJuego = juego?.Titulo ?? "Juego no registrado",
                        FotoPortada = juego?.FotoPortada ?? "missing_image.png",
                        Fecha = partida.Fecha,
                        Ganador = partida.Ganador
                    });
                }

                Partidas = nuevasPartidas;
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error al cargar partidas: {ex.Message}");
                await DisplayAlert("Error", "No se pudieron cargar las partidas.", "OK");
            }
            finally {
                Device.BeginInvokeOnMainThread(() => {
                    IsRefreshing = false;
                });
            }
        }

        /// <summary>
        /// Comando que se ejecuta al seleccionar una partida en la lista.
        /// Navega a la página de detalles de la partida seleccionada.
        /// </summary>
        public Command<PartidaViewModel> PartidaTapCommand => new(async (partida) => {
            if (_isNavigating || partida == null)
                return;

            _isNavigating = true;

            try {
                Debug.WriteLine($"Partida seleccionada ID: {partida.IdPartida}");
                var partidaCompleta = await _dbService.GetPartidaByIdAsync(partida.IdPartida);

                if (partidaCompleta != null) {
                    await Navigation.PushAsync(new DetallePartidaPage(partidaCompleta));
                }
                else {
                    await DisplayAlert("Error", "No se encontró la partida seleccionada.", "OK");
                }
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error al navegar a partida: {ex.Message}");
                await DisplayAlert("Error", "No se pudo abrir la partida.", "OK");
            }
            finally {
                _isNavigating = false;
            }
        });

        /// <summary>
        /// Evento que se ejecuta al pulsar el botón para ver partidas públicas.
        /// Comprueba la conexión a Internet y navega a la página de partidas públicas.
        /// </summary>
        /// <param name="sender">Botón que dispara el evento.</param>
        /// <param name="e">Argumentos del evento.</param>
        private async void OnVerPartidasPublicasClicked(object sender, EventArgs e) {
            if (_isNavigating)
                return;

            try {
                _isNavigating = true;

                if (sender is Button btn)
                    btn.IsEnabled = false;

                if (!NetworkUtils.TieneConexionInternet()) {
                    await DisplayAlert(
                        "Sin conexión",
                        "Necesitas conexión a Internet para hacer una búsqueda.",
                        "OK");
                    return;
                }

                await Navigation.PushAsync(new PartidasPublicasPage());
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error al navegar a partidas públicas: {ex.Message}");
                await DisplayAlert("Error", "No se pudo abrir la vista de partidas públicas.", "OK");
            }
            finally {
                _isNavigating = false;

                if (sender is Button btn)
                    btn.IsEnabled = true;
            }
        }
    }
}
