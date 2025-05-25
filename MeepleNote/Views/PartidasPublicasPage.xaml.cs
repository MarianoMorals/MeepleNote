using MeepleNote.Models;
using MeepleNote.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MeepleNote.Views {
    public partial class PartidasPublicasPage : ContentPage {
        private readonly SQLiteService _dbService;
        private ObservableCollection<PartidaPublicaViewModel> _partidas;
        private bool _isLoading = false;

        public bool IsRefreshing { get; set; }

        public ICommand RefreshCommand => new Command(async () => {
            if (_isLoading) return;
            await CargarPartidasPublicas();
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
            if (!_isLoading)
                await CargarPartidasPublicas();
        }

        private async Task CargarPartidasPublicas() {
            if (_isLoading) return;

            _isLoading = true;
            IsRefreshing = true;

            try {
                _partidas.Clear();

                // 1. Obtener datos de SQLite
                var partidas = await _dbService.GetPartidasPublicasAsync();

                // 2. Filtrar duplicados usando DistinctBy (necesita LINQ)
                var partidasUnicas = partidas
                    .Where(p => !p.Completada)
                    .GroupBy(p => p.Id)
                    .Select(g => g.First())
                    .ToList();

                var idUsuarioActual = Preferences.Get("IdUsuario", 0);

                // 3. Cargar datos en un solo paso
                foreach (var partida in partidasUnicas) {
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
            }
            finally {
                PartidasPublicasList.ItemsSource = null;
                PartidasPublicasList.ItemsSource = _partidas;
                IsRefreshing = false;
                _isLoading = false;
            }
        }
    }
}