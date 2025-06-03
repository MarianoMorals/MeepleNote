using MeepleNote.Views;
using System.Diagnostics;

namespace MeepleNote.Views {
    public partial class UtilidadesPage : ContentPage {

        private bool _isNavigating = false;

        public UtilidadesPage() {
            InitializeComponent();
        }

        private async void OnDadosClicked(object sender, EventArgs e) {
            if (_isNavigating)
                return;

            if (sender is Button button) {
                try {
                    _isNavigating = true;
                    button.IsEnabled = false;

                    await Navigation.PushAsync(new DadosPage());
                }
                catch (Exception ex) {
                    Debug.WriteLine($"Error al navegar a DadosPage: {ex.Message}");
                    await DisplayAlert("Error", "No se pudo abrir la utilidad de dados.", "OK");
                }
                finally {
                    _isNavigating = false;
                    button.IsEnabled = true;
                }
            }
        }
        private async void OnContadorVidaClicked(object sender, EventArgs e) {
            if (_isNavigating)
                return;

            if (sender is Button button) {
                try {
                    _isNavigating = true;
                    button.IsEnabled = false;

                    await Navigation.PushAsync(new ContadorVidaPage());
                }
                catch (Exception ex) {
                    Debug.WriteLine($"Error al navegar a ContadorVidaPage: {ex.Message}");
                    await DisplayAlert("Error", "No se pudo abrir el contador de vida.", "OK");
                }
                finally {
                    _isNavigating = false;
                    button.IsEnabled = true;
                }
            }
        }

    }
}