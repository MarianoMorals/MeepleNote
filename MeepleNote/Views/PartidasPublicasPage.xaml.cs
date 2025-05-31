using MeepleNote.Models;
using MeepleNote.Services;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace MeepleNote.Views {
    public partial class PartidasPublicasPage : ContentPage, INotifyPropertyChanged {
        private readonly SQLiteService _dbService;
        private FirebaseDatabaseService _firebaseService;
        private readonly ExplorarService _explorarService;

        private ObservableCollection<PartidaPublicaViewModel> _partidas;
        private bool _isLoading = false;

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

        public ICommand RefreshCommand => new Command(async () => {
            if (_isLoading) return;
            await CargarPartidasPublicas();
        });

        public ICommand CompletarCommand => new Command<PartidaPublicaViewModel>(async (partida) => {
            bool confirmar = await DisplayAlert("Confirmar",
                "¿Marcar esta partida como completada?", "Sí", "No");

            if (confirmar) {
                try {
                    // 1. Primero actualizar en Firebase
                    if (_firebaseService != null) {
                        // Obtener la partida completa
                        var partidaCompleta = (await _dbService.GetPartidasPublicasAsync())
                            .FirstOrDefault(p => p.IdFirebase.Equals( partida.Id));

                        if (partidaCompleta != null) {
                            partidaCompleta.Completada = true;
                            await _firebaseService.ActualizarPartidaPublicaEnFirebase(partidaCompleta);

                            // 2. Luego actualizar localmente desde Firebase para garantizar consistencia
                            await _dbService.ReplacePartidasPublicasAsync(
                                await _firebaseService.DescargarPartidasPublicas());
                        }
                    }

                    // 3. Recargar la lista (que ahora vendrá de Firebase)
                    await CargarPartidasPublicas();

                    await DisplayAlert("Éxito", "Partida marcada como completada", "OK");
                }
                catch (Exception ex) {
                    await DisplayAlert("Error", $"No se pudo completar la partida: {ex.Message}", "OK");
                }
            }
        });

        public PartidasPublicasPage() {
            InitializeComponent();

            string authToken = Preferences.Get("FirebaseToken", null);
            if (!string.IsNullOrEmpty(authToken)) {
                _firebaseService = new FirebaseDatabaseService(authToken);
            }
            _explorarService = new ExplorarService();

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

                // 1. Intentar cargar desde Firebase primero
                List<PartidaPublica> partidasPublicas = new();

                try {
                    if (_firebaseService != null) {
                        partidasPublicas = await _firebaseService.DescargarPartidasPublicas();
                        await _dbService.ReplacePartidasPublicasAsync(partidasPublicas);
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error Firebase: {ex.Message}");
                    // Continuar con SQLite si falla Firebase
                }

                // 2. Obtener datos locales como respaldo
                var partidasLocales = await _dbService.GetPartidasPublicasAsync();
                var partidasUnicas = partidasLocales
                    .Where(p => !p.Completada)
                    .DistinctBy(p => p.IdFirebase)
                    .ToList();

                // 3. Cargar datos en la UI
                foreach (var partida in partidasUnicas) {
                    var juego = await _dbService.GetJuegoByIdAsync(partida.IdJuego);

                    if (juego == null) {
                        juego = await _explorarService.ObtenerDetallesJuegoAsync(partida.IdJuego);
                        if (juego != null) {
                            juego.EnColeccion = false;
                            await _dbService.SaveJuegoAsync(juego);
                        }
                    }

                    _partidas.Add(new PartidaPublicaViewModel {
                        Id = partida.IdFirebase,
                        TituloJuego = juego?.Titulo ?? "Juego desconocido",
                        FotoPortada = juego?.FotoPortada ?? "icon_juego_desconocido.png",
                        Ciudad = partida.Ciudad,
                        Fecha = partida.Fecha,
                        JugadoresRequeridos = partida.JugadoresRequeridos,
                        Organizador = partida.NombreOrganizador,
                        EmailContacto = partida.EmailContacto,
                        EsOrganizador = partida.IdUsuarioOrganizador == Preferences.Get("UsuarioId", "1")
                    });
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Error general: {ex.Message}");
                await DisplayAlert("Error", "No se pudieron cargar las partidas", "OK");
            }
            finally {
                // Garantizar que siempre se desactive el refresco
                Device.BeginInvokeOnMainThread(() =>
                {
                    PartidasPublicasList.ItemsSource = null;
                    PartidasPublicasList.ItemsSource = _partidas;
                    IsRefreshing = false;
                    _isLoading = false;
                });
            }
        }
    }
}