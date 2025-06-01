using MeepleNote.Models;
using MeepleNote.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace MeepleNote.Views {
    public partial class PartidasPage : ContentPage, INotifyPropertyChanged {
        private readonly SQLiteService _dbService;
        private ObservableCollection<PartidaViewModel> _partidas = new();

        private bool _isRefreshing;
        public bool IsRefreshing {
            get => _isRefreshing;
            set {
                if (_isRefreshing != value) {
                    _isRefreshing = value;
                    OnPropertyChanged(nameof(IsRefreshing));
                }
            }
        }
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
        public ICommand RefreshCommand => _refreshCommand;

        public PartidasPage() {
            InitializeComponent();
            _dbService = new SQLiteService();
            _refreshCommand = new Command(async () => await CargarPartidas());

            BindingContext = this;
        }

        protected override async void OnAppearing() {
            base.OnAppearing();
            await CargarPartidas();
        }

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


        public Command<PartidaViewModel> PartidaTapCommand => new(async (partida) => {
            if (partida == null) return;

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
        });

        private async void OnVerPartidasPublicasClicked(object sender, EventArgs e) {
            try {

                if (!NetworkUtils.TieneConexionInternet()) {
                    await DisplayAlert(
                        "Sin conexión",
                        "Necesitas conexión a Internet para hacer una busqueda.",
                        "OK");
                    return;
                }


                await Navigation.PushAsync(new PartidasPublicasPage());
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error al navegar a partidas públicas: {ex.Message}");
                await DisplayAlert("Error", "No se pudo abrir la vista de partidas públicas.", "OK");
            }
        }
    }
}
