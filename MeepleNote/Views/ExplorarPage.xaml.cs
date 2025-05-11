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
            ResultadosList.RemainingItemsThreshold = 5; // Cargar más cuando queden 5 items por ver
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
            SinResultadosLabel.IsVisible = false;
            SugerenciasList.IsVisible = false;

            if (!string.IsNullOrWhiteSpace(_currentQuery)) {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                await RefreshDataAsync();

                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private async Task LoadMoreItems() {
            if (_isLoading || !_hasMoreItems) return;

            _isLoading = true;
            Device.BeginInvokeOnMainThread(() => LoadingMoreIndicator.IsVisible = true);

            try {
                var nuevosJuegos = await _explorarService.BuscarJuegosAsync(
                    _currentQuery,
                    _currentPage * PageSize,
                    PageSize);

                if (nuevosJuegos.Any()) {
                    Device.BeginInvokeOnMainThread(() => {
                        foreach (var juego in nuevosJuegos) {
                            _juegos.Add(juego);
                        }
                    });

                    _currentPage++;
                }
                else {
                    _hasMoreItems = false;
                    if (_currentPage == 0) {
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
        }

        private async void SugerenciasList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.CurrentSelection.FirstOrDefault() is Juego juego) {
                Device.BeginInvokeOnMainThread(() => {
                    BuscarEntry.Text = juego.Titulo;
                    SugerenciasList.IsVisible = false;
                });

                _currentQuery = juego.Titulo;

                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                await RefreshDataAsync();

                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private async void OnAgregarClicked(object sender, EventArgs e) {
            if (sender is Button button && button.CommandParameter is Juego juego) {
                try {
                    bool yaExiste = await _dbService.JuegoExisteAsync(juego.IdJuego);
                    if (yaExiste) {
                        await DisplayAlert("Atención", "Este juego ya está en tu colección.", "OK");
                        return;
                    }

                    var juegoCompleto = await _explorarService.ObtenerDetallesJuegoAsync(juego.IdJuego);
                    if (juegoCompleto != null) {
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
                    var detalles = await _explorarService.ObtenerDetallesJuegoAsync(juego.IdJuego);
                    if (detalles != null) {
                        await Navigation.PushAsync(new DetalleJuegoPage(detalles));
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