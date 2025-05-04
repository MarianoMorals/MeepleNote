using MeepleNote.Models;
using MeepleNote.Services;

namespace MeepleNote.Views {
    public partial class ExplorarPage : ContentPage {
        private ExplorarService explorarService;
        private SQLiteService dbService;

        public ExplorarPage() {
            InitializeComponent();
            explorarService = new ExplorarService();
            dbService = new SQLiteService();

            SugerenciasList.SelectionChanged += SugerenciasList_SelectionChanged;

        }


        private async void OnBuscarClicked(object sender, EventArgs e) {
            string query = BuscarEntry.Text;
            SinResultadosLabel.IsVisible = false;
            ResultadosList.ItemsSource = null;

            if (!string.IsNullOrWhiteSpace(query)) {
                var resultados = await explorarService.BuscarJuegosAsync(query);
                if (resultados.Any()) {
                    ResultadosList.ItemsSource = resultados;
                }
                else {
                    SinResultadosLabel.IsVisible = true;
                }
            }

            SugerenciasList.IsVisible = false;
        }

        private async void OnAgregarClicked(object sender, EventArgs e) {
            if (sender is Button button && button.CommandParameter is Juego juegoBase) {
                // Verifica si ya est� en colecci�n
                bool yaExiste = await dbService.JuegoExisteAsync(juegoBase.IdJuego);
                if (yaExiste) {
                    await DisplayAlert("Ya en colecci�n", "Este juego ya est� en tu colecci�n.", "OK");
                    return;
                }

                var juegoCompleto = await explorarService.ObtenerDetallesJuegoAsync(juegoBase.IdJuego);

                if (juegoCompleto != null) {
                    await dbService.SaveJuegoAsync(juegoCompleto);
                    await DisplayAlert("A�adido", $"{juegoCompleto.Titulo} ha sido a�adido a tu colecci�n.", "OK");
                }
                else {
                    await DisplayAlert("Error", "No se pudo obtener la informaci�n completa del juego.", "OK");
                }
            }
        }

        private async void OnVerDetallesClicked(object sender, EventArgs e) {
            if (sender is Button button && button.CommandParameter is Juego juego) {
                var detalles = await explorarService.ObtenerDetallesJuegoAsync(juego.IdJuego);
                if (detalles != null) {
                    await Navigation.PushAsync(new DetalleJuegoPage(detalles));
                }
            }
        }

        private async void OnBuscarTextChanged(object sender, TextChangedEventArgs e) {
            string texto = e.NewTextValue;

            if (string.IsNullOrWhiteSpace(texto) || texto.Length < 3) {
                SugerenciasList.IsVisible = false;
                return;
            }

            // Obtener sugerencias b�sicas (sin detalles a�n)
            var juegos = await explorarService.BuscarSugerenciasAsync(texto); // Implementamos abajo
            if (juegos.Any()) {
                SugerenciasList.ItemsSource = juegos;
                SugerenciasList.IsVisible = true;
            }
            else {
                SugerenciasList.IsVisible = false;
            }
        }

        private async void SugerenciasList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.CurrentSelection.FirstOrDefault() is Juego juego) {
                BuscarEntry.Text = juego.Titulo;
                SugerenciasList.IsVisible = false;

                var resultado = await explorarService.BuscarJuegosAsync(juego.Titulo);
                ResultadosList.ItemsSource = resultado;
            }
        }



    }
}
