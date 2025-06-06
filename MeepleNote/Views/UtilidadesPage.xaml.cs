using MeepleNote.Views;
using System.Diagnostics;

namespace MeepleNote.Views {

    /// <summary>
    /// Página de utilidades que permite acceder a herramientas adicionales como los dados y el contador de vida.
    /// </summary>
    public partial class UtilidadesPage : ContentPage {

        // Bandera para evitar múltiples navegaciones simultáneas
        private bool _isNavigating = false;

        /// <summary>
        /// Constructor de la página UtilidadesPage.
        /// Inicializa los componentes visuales definidos en XAML.
        /// </summary>
        public UtilidadesPage() {
            InitializeComponent();
        }

        /// <summary>
        /// Evento que se ejecuta al pulsar el botón para abrir la utilidad de dados.
        /// Previene navegación duplicada y controla errores en tiempo de ejecución.
        /// </summary>
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

        /// <summary>
        /// Evento que se ejecuta al pulsar el botón para abrir la utilidad del contador de vida.
        /// Similar a OnDadosClicked, controla la navegación y manejo de errores.
        /// </summary>
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