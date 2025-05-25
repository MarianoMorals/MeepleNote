using MeepleNote.Models;
using MeepleNote.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MeepleNote.Views {
    public partial class PartidasPublicasPage : ContentPage {
        private readonly SQLiteService _dbService;
        private ObservableCollection<PartidaPublicaViewModel> _partidas;

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
            await CargarPartidasPublicas();
        });
        public ICommand ContactarCommand => new Command<PartidaPublicaViewModel>(async (partida) => {
            await Launcher.Default.OpenAsync($"mailto:{partida.EmailContacto}?subject=Partida%20de%20{partida.TituloJuego}");
        });

        public ICommand CompletarCommand => new Command<PartidaPublicaViewModel>(async (partida) => {
            bool confirmar = await DisplayAlert("Confirmar",
                "¿Marcar esta partida como completada?", "Sí", "No");

            if (confirmar) {
                await _dbService.MarcarPartidaPublicaCompletadaAsync(partida.Id);
                await CargarPartidasPublicas();
            }
        });

        public PartidasPublicasPage() {
            InitializeComponent();
            _dbService = new SQLiteService();
            _partidas = new ObservableCollection<PartidaPublicaViewModel>();
            BindingContext = this;
        }

        protected override async void OnAppearing() {
            base.OnAppearing();
            await CargarPartidasPublicas();
        }

        private async Task CargarPartidasPublicas() {
            IsRefreshing = true;
            _partidas.Clear();

            var partidas = await _dbService.GetPartidasPublicasAsync();
            var idUsuarioActual = Preferences.Get("IdUsuario", 0);

            foreach (var partida in partidas) {
                var juego = await _dbService.GetJuegoByIdAsync(partida.IdJuego);

                _partidas.Add(new PartidaPublicaViewModel {
                    Id = partida.Id,
                    TituloJuego = juego?.Titulo ?? "Juego desconocido",
                    FotoPortada = juego?.FotoPortada ?? "icon_juego_desconocido.png",
                    Ciudad = partida.Ciudad,
                    Fecha = partida.Fecha,
                    JugadoresRequeridos = partida.JugadoresRequeridos,
                    Organizador = partida.NombreOrganizador,
                    EmailContacto = partida.EmailContacto,
                    EsOrganizador = partida.IdUsuarioOrganizador == idUsuarioActual
                });
            }

            PartidasPublicasList.ItemsSource = _partidas;
            IsRefreshing = false;
        }
    }
}