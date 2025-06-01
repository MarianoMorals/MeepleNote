using MeepleNote.Models;
using MeepleNote.Services;
using System.Diagnostics;

namespace MeepleNote.Views {
    public partial class ColeccionPage : ContentPage {
        private SQLiteService dbService;

        public ColeccionPage() {
            InitializeComponent();
            dbService = new SQLiteService();
        }

        protected override async void OnAppearing() {
            base.OnAppearing();
            await CargarColeccion();
        }

        private async Task CargarColeccion() {
            var idUsuario = Preferences.Get("UsuarioId", "0");
            var juegos = await dbService.GetJuegosAsyncEnColeccion(idUsuario.ToString());

            ColeccionList.ItemsSource = juegos;
            EliminarButton.IsVisible = juegos?.Any() == true;
            EmptyLabel.IsVisible = !juegos.Any();
        }

        private async void OnVerDetallesClicked(object sender, EventArgs e) {
            try {
                if (sender is Button button && button.BindingContext is Juego juego) {
                    if (juego == null) {
                        await DisplayAlert("Error", "Juego no disponible", "OK");
                        return;
                    }

                    var detailPage = new DetalleJuegoPage(juego);
                    await Navigation.PushAsync(detailPage);
                }
            }
            catch (Exception ex) {
                Debug.WriteLine($"CRASH DETAIL: {ex.ToString()}");
                await DisplayAlert("Error", $"Error técnico: {ex.Message}", "OK");
            }
        }

        private async void OnEliminarSeleccionadosClicked(object sender, EventArgs e) {
            var juegos = (List<Juego>)ColeccionList.ItemsSource;
            var seleccionados = juegos.Where(j => j.IsSelected).ToList();

            if (!seleccionados.Any()) {
                await DisplayAlert("Aviso", "No has seleccionado ningún juego.", "OK");
                return;
            }

            bool confirmar = await DisplayAlert("Eliminar", $"¿Eliminar {seleccionados.Count} juego(s) de tu colección?", "Sí", "No");
            if (confirmar) {
                foreach (var juego in seleccionados) {
                    //await dbService.DeleteJuegoAsync(juego);
                    var idUsuario = Preferences.Get("UsuarioId", "0");

                    await dbService.QuitarJuegoExistenteDeColeccion(juego.IdJuego, idUsuario);
                }

                await CargarColeccion();
            }
        }


    }
}
