using Firebase.Auth;
using MeepleNote.Services;
using MeepleNote.Views;
using Microsoft.Maui.Controls;

namespace MeepleNote {
    public partial class App : Application {
        private const string ApiKey = "AIzaSyCmcqsaPemAyArjJBBiV7nFm2TeXLFp9cI";

        public App() {
            InitializeComponent();

            Application.Current.UserAppTheme = AppTheme.Light;

            MainPage = new AppShell();
            HandleInitialNavigation();
        }

        private async void HandleInitialNavigation() {
            // Limpiar datos residuales al iniciar la app
            var sqliteDb = new SQLiteService();
            //await sqliteDb.LimpiarDatosUsuario();

            bool sesionActiva = Preferences.Get("SesionIniciada", false);

            if (sesionActiva) {
                // Cargar datos desde Firebase al iniciar
                await CargarDatosDesdeFirebase();
                await Shell.Current.GoToAsync($"//{nameof(ExplorarPage)}");
            }
            else {
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }

        private async Task CargarDatosDesdeFirebase() {
            try {
                var usuarioId = Preferences.Get("UsuarioId", null);
                var token = Preferences.Get("FirebaseToken", null);

                if (string.IsNullOrEmpty(usuarioId)) return;

                var sqliteDb = new SQLiteService();
                var firebaseDb = new FirebaseDatabaseService(token);
                var sincService = new SincronizacionService(sqliteDb, firebaseDb);

                await sincService.SincronizarDesdeFirebaseSiNecesario();
            }
            catch (Exception ex) {
                Console.WriteLine($"Error al cargar datos desde Firebase: {ex.Message}");
                // Podrías mostrar un mensaje al usuario si lo deseas
            }
        }

        protected override async void OnSleep() {
            await RealizarSincronizacionAntesDeCerrar();
        }

        private async Task RealizarSincronizacionAntesDeCerrar() {
            try {
                var usuarioId = Preferences.Get("UsuarioId", null);
                var token = Preferences.Get("FirebaseToken", null);

                if (string.IsNullOrEmpty(usuarioId)) return;

                var sqliteDb = new SQLiteService();
                var firebaseDb = new FirebaseDatabaseService(token);
                var sincService = new SincronizacionService(sqliteDb, firebaseDb);

                // Sincronizar todos los datos locales con Firebase
                await sincService.SincronizarConFirebase();

                Console.WriteLine("Datos sincronizados correctamente con Firebase");
            }
            catch (Exception ex) {
                Console.WriteLine($"Error en sincronización: {ex.Message}");
                // Aquí podrías implementar un sistema de reintentos o notificación de error
            }
        }

        protected override async void OnResume() {
            base.OnResume();

            // Verificar si hay cambios en Firebase al reanudar la app
            if (Preferences.Get("SesionIniciada", false)) {
                await CargarDatosDesdeFirebase();
            }
        }
    }
}