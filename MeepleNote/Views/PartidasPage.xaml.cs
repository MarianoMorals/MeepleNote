using MeepleNote.Models;
using MeepleNote.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace MeepleNote.Views {
    public partial class PartidasPage : ContentPage {
        private readonly SQLiteService _dbService;
        private readonly ObservableCollection<PartidaViewModel> _partidas;
        private bool _isInitialLoad = true;

        private bool _isRefreshing;
        public bool IsRefreshing {
            get => _isRefreshing;
            set {
                if (_isRefreshing != value) {
                    _isRefreshing = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand RefreshCommand => new Command(async () => {
            if (IsRefreshing) return;
            await CargarPartidas();
        });

        public PartidasPage() {
            InitializeComponent();
            _dbService = new SQLiteService();
            _partidas = new ObservableCollection<PartidaViewModel>();
            BindingContext = this;
            PartidasCollection.ItemsSource = _partidas;
        }

        protected override async void OnAppearing() {
            base.OnAppearing();

            /*if (_isInitialLoad) {
                _isInitialLoad = false;
                await CargarPartidas();
            }*/
            await CargarPartidas();
        }

        private async Task CargarPartidas() {
            IsRefreshing = true;
            _partidas.Clear();

            var idUsuario = Preferences.Get("UsuarioId", "0");
            var partidas = (await _dbService.GetPartidasAsync(idUsuario))
                .DistinctBy(p => p.IdPartida)
                .OrderByDescending(p => p.Fecha)
                .ToList();

            foreach (var partida in partidas) {
                var juego = await _dbService.GetJuegoByIdAsync(partida.IdJuego);

                _partidas.Add(new PartidaViewModel {
                    IdPartida = partida.IdPartida,
                    IdJuego = partida.IdJuego,
                    TituloJuego = juego?.Titulo ?? "Juego no registrado",
                    FotoPortada = juego?.FotoPortada,
                    Fecha = partida.Fecha,
                    Ganador = partida.Ganador
                });
            }

            IsRefreshing = false;
        }

        public ICommand PartidaTapCommand => new Command<PartidaViewModel>(async (partida) => {
            if (partida == null) return;

            try {
                Debug.WriteLine($"Partida seleccionada ID: {partida.IdPartida}");
                var partidaCompleta = await _dbService.GetPartidaByIdAsync(partida.IdPartida);

                if (partidaCompleta != null) {
                    await Navigation.PushAsync(new DetallePartidaPage(partidaCompleta));
                }
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error al navegar a partida: {ex.Message}");
            }
        });

        private async void OnVerPartidasPublicasClicked(object sender, EventArgs e) {
            await Navigation.PushAsync(new PartidasPublicasPage());
        }
    }
}