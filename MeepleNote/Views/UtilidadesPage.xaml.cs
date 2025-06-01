using MeepleNote.Views;

namespace MeepleNote.Views {
    public partial class UtilidadesPage : ContentPage {
        public UtilidadesPage() {
            InitializeComponent();
        }

        private async void OnDadosClicked(object sender, EventArgs e) {
            await Navigation.PushAsync(new DadosPage());
        }


        private async void OnContadorVidaClicked(object sender, EventArgs e) {
            await Navigation.PushAsync(new ContadorVidaPage());
        }
    }
}