using MeepleNote.Models;
using MeepleNote.Services;

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
            var juegos = await dbService.GetJuegosAsync();
            ColeccionList.ItemsSource = juegos;
        }

        private async void OnVerDetallesClicked(object sender, EventArgs e) {
            if (sender is Button button && button.CommandParameter is Juego juego) {
                await Navigation.PushAsync(new DetalleJuegoPage(juego));
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
                    await dbService.DeleteJuegoAsync(juego);
                }

                await CargarColeccion();
            }
        }


    }
}
