namespace MeepleNote.Views {
    public partial class PrincipalPage : ContentPage {
        public PrincipalPage() {
            InitializeComponent();
            //ContentView_Principal.Content = new ColeccionPage();
        }

        private async void OnPerfilClicked(object sender, EventArgs e) {
            //ContentView_Principal.Content = new PerfilPage();
        }

        private async void OnColeccionClicked(object sender, EventArgs e) {
            //ContentView_Principal.Content = new ColeccionPage();
        }

        private async void OnPartidasClicked(object sender, EventArgs e) {
            //ContentView_Principal.Content = new PartidasPage();
        }

        private async void OnExplorarClicked(object sender, EventArgs e) {
            //ContentView_Principal.Content = new ExplorarPage();
        }

        private async void OnUtilidadesClicked(object sender, EventArgs e) {
            await DisplayAlert("Info", "Funcionalidad en desarrollo", "OK");
        }
        private async void Btn_Ayuda_Clicked(object sender, EventArgs e) {
            await DisplayAlert("Info", "Funcionalidad en desarrollo", "OK");
        }
        private async void Btn_Configuracion_Clicked(object sender, EventArgs e) {
            await DisplayAlert("Info", "Funcionalidad en desarrollo", "OK");
        }

    }
}
