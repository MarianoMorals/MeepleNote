using System.Collections.ObjectModel;
using System.Diagnostics;
using MeepleNote.Models;
using MeepleNote.Services;
using System.Windows.Input;

namespace MeepleNote.Views {
    public partial class ExplorarPage : ContentPage {
        private readonly ExplorarService _explorarService;
        private readonly SQLiteService _dbService;

        private int _currentPage = 0;
        private const int PageSize = 10;
        private bool _isLoading = false;
        private bool _hasMoreItems = true;
        private string _currentQuery = string.Empty;
        private ObservableCollection<Juego> _juegos = new ObservableCollection<Juego>();

        private bool _isRefreshing = false;
        public bool IsRefreshing {
            get => _isRefreshing;
            set {
                if (_isRefreshing != value) {
                    _isRefreshing = value;
                    OnPropertyChanged(nameof(IsRefreshing));
                }
            }
        }

        public ICommand RefreshCommand { get; }

        public ExplorarPage() {
            InitializeComponent();
            _explorarService = new ExplorarService();
            _dbService = new SQLiteService();

            RefreshCommand = new Command(async () => await RefreshDataAsync());
            BindingContext = this;

            ResultadosList.ItemsSource = _juegos;
            ResultadosList.RemainingItemsThreshold = 1; // Cargar más cuando quede 1 item por ver
            ResultadosList.RemainingItemsThresholdReached += ResultadosList_RemainingItemsThresholdReached;
        }

        private async Task RefreshDataAsync() {
            if (string.IsNullOrWhiteSpace(_currentQuery)) {
                IsRefreshing = false;
                return;
            }

            try {
                _currentPage = 0;
                _hasMoreItems = true;
                _juegos.Clear();
                SinResultadosLabel.IsVisible = false;

                await LoadMoreItems();
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error en RefreshDataAsync: {ex.Message}");
                await DisplayAlert("Error", "No se pudo actualizar la lista", "OK");
            }
            finally {
                IsRefreshing = false;
            }
        }

        private async void OnBuscarClicked(object sender, EventArgs e) {
            _currentQuery = BuscarEntry.Text?.Trim() ?? string.Empty;

            BuscarEntry.Text = string.Empty;

            // Ocultar sugerencias al hacer clic en buscar
            SugerenciasList.IsVisible = false;

            SinResultadosLabel.IsVisible = false;

            if (!string.IsNullOrWhiteSpace(_currentQuery)) {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                await RefreshDataAsync();

                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private async Task LoadMoreItems() {
            if (_isLoading || !_hasMoreItems || string.IsNullOrWhiteSpace(_currentQuery))
                return;

            _isLoading = true;
            Device.BeginInvokeOnMainThread(() => LoadingMoreIndicator.IsVisible = true);

            try {
                var nuevosJuegos = await _explorarService.BuscarJuegosAsync(
                    _currentQuery,
                    _currentPage * PageSize,  // Calcula el offset correctamente
                    PageSize);

                if (nuevosJuegos.Any()) {
                    Device.BeginInvokeOnMainThread(() => {
                        foreach (var juego in nuevosJuegos) {
                            // Verifica que el juego no esté ya en la lista
                            if (!_juegos.Any(j => j.IdJuego == juego.IdJuego)) {
                                _juegos.Add(juego);
                            }
                        }
                    });
                    _currentPage++; // Solo incrementa la página si obtuvimos resultados
                }
                else {
                    _hasMoreItems = false;
                    if (_currentPage == 0) { // Solo mostrar "sin resultados" en la primera carga
                        Device.BeginInvokeOnMainThread(() => {
                            SinResultadosLabel.IsVisible = true;
                        });
                    }
                }
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error en LoadMoreItems: {ex.Message}");
                _hasMoreItems = false;
            }
            finally {
                _isLoading = false;
                Device.BeginInvokeOnMainThread(() => {
                    LoadingMoreIndicator.IsVisible = false;
                });
            }
        }

        private async void ResultadosList_RemainingItemsThresholdReached(object sender, EventArgs e) {
            await LoadMoreItems();
        }

        private async void OnBuscarTextChanged(object sender, TextChangedEventArgs e) {
            string texto = e.NewTextValue?.Trim() ?? string.Empty;

            // Si el texto coincide con la búsqueda actual, no hacer nada
            if (texto == _currentQuery) {
                return;
            }

            // Ocultar sugerencias si el texto está vacío
            if (string.IsNullOrWhiteSpace(texto)) {
                SugerenciasList.IsVisible = false;
                return;
            }

            if (texto.Length >= 2) {
                var juegos = await _explorarService.BuscarSugerenciasAsync(texto);
                Device.BeginInvokeOnMainThread(() => {
                    SugerenciasList.ItemsSource = juegos;
                    SugerenciasList.IsVisible = juegos.Any();
                });
            }
            else {
                SugerenciasList.IsVisible = false;
            }
        }

        private async void SugerenciasList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.CurrentSelection.FirstOrDefault() is Juego juego) {
                // Ocultar sugerencias inmediatamente
                SugerenciasList.IsVisible = false;

                // Establecer el texto de búsqueda
                BuscarEntry.Text = string.Empty;
                _currentQuery = juego.Titulo;

                // Mostrar indicador de carga
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                // Limpiar resultados anteriores
                _juegos.Clear();
                _currentPage = 0;
                _hasMoreItems = true;


                // Realizar la búsqueda
                await LoadMoreItems();

                // Ocultar indicador de carga
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }
    

        private async void OnAgregarClicked(object sender, EventArgs e) {
            if (sender is Button button && button.CommandParameter is Juego juego) {
                try {
                    var idUsuario = Preferences.Get("UsuarioId", "0"); // Lo vinculamos al id de firebase que es unico a nivel global y no solo local.
                    bool yaExiste = await _dbService.JuegoExisteAsync(juego.IdJuego, idUsuario);

                    if (yaExiste) {
                        bool enColeccion = await _dbService.JuegoEnColeccionAsync(juego.IdJuego, idUsuario);

                        if (enColeccion) {
                            await DisplayAlert("Atención", "Este juego ya está en tu colección.", "OK");
                            return;
                        }
                        else {
                            await _dbService.AnnadirJuegoExistenteAColeccion(juego.IdJuego, idUsuario);
                            await DisplayAlert("Éxito", $"{juego.Titulo} añadido a tu colección", "OK");
                            return;
                        }
                    }

                    var juegoCompleto = await _explorarService.ObtenerDetallesJuegoAsync(juego.IdJuego);
                    if (juegoCompleto != null) {
                        juegoCompleto.IdUsuario = idUsuario; // Asignar el ID de usuario
                        await _dbService.SaveJuegoAsync(juegoCompleto);
                        await DisplayAlert("Éxito", $"{juegoCompleto.Titulo} añadido a tu colección", "OK");
                    }
                }
                catch (Exception ex) {
                    Debug.WriteLine($"Error en OnAgregarClicked: {ex.Message}");
                    await DisplayAlert("Error", "No se pudo añadir el juego", "OK");
                }
            }
        }

        private async void OnVerDetallesClicked(object sender, EventArgs e) {
            if (sender is Button button && button.CommandParameter is Juego juego) {
                try {
                    var idUsuario = Preferences.Get("UsuarioId", "0");

                    if (await _dbService.JuegoExisteAsync(juego.IdJuego, idUsuario)) {
                        Juego juegoExistente = await _dbService.GetJuegoByIdAsync(juego.IdJuego);

                        await Navigation.PushAsync(new DetalleJuegoPage(juegoExistente));

                    }
                    else {
                        var detalles = await _explorarService.ObtenerDetallesJuegoAsync(juego.IdJuego);
                        if (detalles != null) {
                            await Navigation.PushAsync(new DetalleJuegoPage(detalles));
                        }
                    }

                    
                }
                catch (Exception ex) {
                    Debug.WriteLine($"Error en OnVerDetallesClicked: {ex.Message}");
                    await DisplayAlert("Error", "No se pudo cargar los detalles", "OK");
                }
            }
        }

        protected override void OnDisappearing() {
            base.OnDisappearing();
            ResultadosList.RemainingItemsThresholdReached -= ResultadosList_RemainingItemsThresholdReached;
        }
    }
}