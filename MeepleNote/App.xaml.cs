using MeepleNote.Views;

namespace MeepleNote {
    public partial class App : Application {
        public App() {
            InitializeComponent();
            MainPage = new AppShell();

            bool sesionActiva = Preferences.Get("SesionIniciada", false);

            if (sesionActiva)
                Shell.Current.GoToAsync("//PrincipalPage");
            else
                Shell.Current.GoToAsync("//LoginPage");
        }

    }
}
