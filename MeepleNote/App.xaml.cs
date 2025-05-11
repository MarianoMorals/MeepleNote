using Firebase.Auth;
using MeepleNote.Services;
using MeepleNote.Views;
using Microsoft.Maui.Controls;

namespace MeepleNote {
    public partial class App : Application {
        private const string ApiKey = "AIzaSyCmcqsaPemAyArjJBBiV7nFm2TeXLFp9cI";


        public App() {
            InitializeComponent();

            // Configurar la página principal inicial
            MainPage = new AppShell();

            // Manejar la navegación inicial
            HandleInitialNavigation();
        }

        private async void HandleInitialNavigation() {
            bool sesionActiva = Preferences.Get("SesionIniciada", false);

            if (sesionActiva) {
                // Navegar a la página principal
                await Shell.Current.GoToAsync($"//{nameof(ExplorarPage)}");
            }
            else {
                // Navegar a la página de login
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }

        protected override async void OnSleep() {
            await RealizarSincronizacionAntesDeCerrar();
        }

        private async Task RealizarSincronizacionAntesDeCerrar() {
            var usuarioId = Preferences.Get("UsuarioId", null);
            if (usuarioId == null) return;

            var sqliteDb = new SQLiteService();
            var token = await new FirebaseAuthProvider(new FirebaseConfig(ApiKey))
                .SignInWithEmailAndPasswordAsync("TU_EMAIL", "TU_PASSWORD")
                .ContinueWith(t => t.Result.FirebaseToken);

            var firebaseDb = new FirebaseDatabaseService(token);
            var datosLocales = await sqliteDb.ObtenerTodo();

            var fechaActual = DateTime.UtcNow;
            await firebaseDb.SubirDatosUsuario(usuarioId, datosLocales.Perfil, datosLocales.Coleccion,
                datosLocales.Juegos, datosLocales.Partidas, datosLocales.JugadoresPartida, fechaActual);

            sqliteDb.GuardarFechaUltimaSync(fechaActual);
        }

    }
}