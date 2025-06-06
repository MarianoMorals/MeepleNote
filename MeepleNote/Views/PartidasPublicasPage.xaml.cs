using MeepleNote.Models;
using MeepleNote.Services;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace MeepleNote.Views {
    /// <summary>
    /// Página que muestra partidas públicas disponibles y permite marcarlas como completadas.
    /// </summary>
    public partial class PartidasPublicasPage : ContentPage, INotifyPropertyChanged {
        private readonly SQLiteService _dbService;
        private FirebaseDatabaseService _firebaseService;
        private readonly ExplorarService _explorarService;

        private ObservableCollection<PartidaPublicaViewModel> _partidas;
        private bool _isLoading = false;

        // Indica si la vista está siendo actualizada (usado para animaciones de "pull-to-refresh")
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

        /// <summary>
        /// Comando para refrescar la lista de partidas públicas.
        /// </summary>
        public ICommand RefreshCommand => new Command(async () => {
            if (_isLoading) return;
            await CargarPartidasPublicas();
        });

        /// <summary>
        /// Comando para marcar una partida pública como completada (solo si el usuario es el organizador).
        /// </summary>
        public ICommand CompletarCommand => new Command<PartidaPublicaViewModel>(async (partida) => {

            if (!NetworkUtils.TieneConexionInternet()) {
                await DisplayAlert(
                    "Sin conexión",
                    "Necesitas conexión a Internet para hacer una marcar la partida como completada.",
                    "OK");
                return;
            }

            // Confirmar acción del usuario
            bool confirmar = await DisplayAlert("Confirmar",
                "¿Marcar esta partida como completada?", "Sí", "No");

            if (confirmar) {
                try {
                    // Paso 1: Actualizar en Firebase
                    if (_firebaseService != null) {
                        var partidaCompleta = (await _dbService.GetPartidasPublicasAsync())
                            .FirstOrDefault(p => p.IdFirebase.Equals(partida.Id));

                        if (partidaCompleta != null) {
                            partidaCompleta.Completada = true;
                            await _firebaseService.ActualizarPartidaPublicaEnFirebase(partidaCompleta);

                            // Paso 2: Sincronizar datos locales con Firebase
                            await _dbService.ReplacePartidasPublicasAsync(
                                await _firebaseService.DescargarPartidasPublicas());
                        }
                    }

                    // Paso 3: Recargar la lista actualizada
                    await CargarPartidasPublicas();
                    await DisplayAlert("Éxito", "Partida marcada como completada", "OK");
                }
                catch (Exception ex) {
                    await DisplayAlert("Error", $"No se pudo completar la partida: {ex.Message}", "OK");
                }
            }
        });

        /// <summary>
        /// Constructor principal de la página.
        /// Inicializa servicios, recupera el token de Firebase y establece el contexto de datos.
        /// </summary>
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

        /// <summary>
        /// Evento que se ejecuta cuando la página aparece en pantalla.
        /// Carga las partidas públicas si no se está cargando ya.
        /// </summary>
        protected override async void OnAppearing() {
            base.OnAppearing();
            if (!_isLoading)
                await CargarPartidasPublicas();
        }

        /// <summary>
        /// Carga las partidas públicas desde Firebase (si hay conexión) o desde la base de datos local.
        /// También sincroniza los juegos no registrados descargándolos con el servicio de exploración.
        /// </summary>
        private async Task CargarPartidasPublicas() {
            if (_isLoading) return;

            _isLoading = true;
            IsRefreshing = true;

            try {
                _partidas.Clear();
                List<PartidaPublica> partidasPublicas = new();

                // 1. Cargar desde Firebase
                try {
                    if (_firebaseService != null) {
                        partidasPublicas = await _firebaseService.DescargarPartidasPublicas();
                        await _dbService.ReplacePartidasPublicasAsync(partidasPublicas);
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error Firebase: {ex.Message}");
                    // Continuar con base local si falla Firebase
                }

                // 2. Cargar desde SQLite (local)
                var partidasLocales = await _dbService.GetPartidasPublicasAsync();

                // Filtrar partidas no completadas y sin duplicados
                var partidasUnicas = partidasLocales
                    .Where(p => !p.Completada)
                    .DistinctBy(p => p.IdFirebase)
                    .ToList();

                // 3. Construir la colección de partidas a mostrar
                foreach (var partida in partidasUnicas) {
                    var juego = await _dbService.GetJuegoByIdAsync(partida.IdJuego);

                    // Si no está en local, obtenerlo desde la API y guardarlo
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
                // Refrescar la vista de lista y liberar el bloqueo de carga
                Device.BeginInvokeOnMainThread(() => {
                    PartidasPublicasList.ItemsSource = null;
                    PartidasPublicasList.ItemsSource = _partidas;
                    IsRefreshing = false;
                    _isLoading = false;
                });
            }
        }
    }
}
