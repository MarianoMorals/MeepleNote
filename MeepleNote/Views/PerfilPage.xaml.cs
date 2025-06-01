using Firebase.Auth;
using MeepleNote.Models;
using MeepleNote.Services;
using Microsoft.Maui.Storage;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MeepleNote.Views {
    public partial class PerfilPage : ContentPage {
        private SincronizacionService _sincronizacionService;
        private readonly SQLiteService _sqliteDb;
        private readonly FirebaseAuthService _firebaseAuthService = new FirebaseAuthService();
        private Usuario _usuarioActual;
        private bool _datosModificados = false;

        public PerfilPage() {
            InitializeComponent();
            _sqliteDb = new SQLiteService();

            // Inicializar el binding para TodayDate
            BindingContext = new {
                TodayDate = DateTime.Today
            };

        }

        protected override async void OnAppearing() {
            base.OnAppearing();
            await CargarPerfilUsuario();

        }

        private async Task CargarPerfilUsuario() {
            var firebaseUsuarioId = Preferences.Get("UsuarioId", null);
            if (!string.IsNullOrEmpty(firebaseUsuarioId)) {
                _usuarioActual = await _sqliteDb.GetUsuarioByFirebaseIdAsync(firebaseUsuarioId);
                if (_usuarioActual != null) {
                    // Actualizar los controles directamente para evitar binding
                    NombreEntry.Text = _usuarioActual.Nombre;
                    FechaNacimientoPicker.Date = _usuarioActual.FechaNacimiento;
                }
            }
        }

        
        private async void OnGuardarClicked(object sender, EventArgs e) {
            if (_usuarioActual == null)
                return;

            try {
                // Actualizar el objeto usuario
                _usuarioActual.Nombre = NombreEntry.Text;
                _usuarioActual.FechaNacimiento = FechaNacimientoPicker.Date;

                // Guardar en SQLite
                await _sqliteDb.SaveUsuarioAsync(_usuarioActual);

                // Sincronizar con Firebase
                await InicializarSincronizacionService();
                if (_sincronizacionService != null) {
                    await _sincronizacionService.SincronizarConFirebase();
                }

                await DisplayAlert("Éxito", "Los cambios se guardaron correctamente", "OK");
            }
            catch (Exception ex) {
                await DisplayAlert("Error", $"No se pudieron guardar los cambios: {ex.Message}", "OK");
            }
        }

        private async Task InicializarSincronizacionService() {
            string authToken = Preferences.Get("FirebaseToken", null);
            if (!string.IsNullOrEmpty(authToken)) {
                var firebaseDb = new FirebaseDatabaseService(authToken);
                _sincronizacionService = new SincronizacionService(_sqliteDb, firebaseDb);
            }
        }

        private async void OnRestablecerContraseñaClicked(object sender, EventArgs e) {
            try {

                if (!NetworkUtils.TieneConexionInternet()) {
                    await DisplayAlert(
                        "Sin conexión",
                        "Necesitas conexión a Internet para restablecer contraseña.",
                        "OK");
                    return;
                }

                if (_usuarioActual == null || string.IsNullOrEmpty(_usuarioActual.Email)) {
                    await DisplayAlert("Error", "No se pudo obtener el correo electrónico del usuario", "OK");
                    return;
                }

                await _firebaseAuthService.SendPasswordResetEmailAsync(_usuarioActual.Email);
                await DisplayAlert("Correo Enviado", "Se ha enviado un correo electrónico para restablecer tu contraseña", "OK");
            }
            catch (FirebaseAuthException ex) {
                await DisplayAlert("Error", $"Error al enviar el correo: {ex.Message}", "OK");
            }
            catch (Exception ex) {
                await DisplayAlert("Error", $"Error inesperado: {ex.Message}", "OK");
            }
        }

        private async void OnCerrarSesionClicked(object sender, EventArgs e) {
            bool confirmar = await DisplayAlert("Cerrar sesión",
                "¿Estás seguro de que quieres cerrar la sesión?",
                "Sí", "No");

            if (!confirmar) return;

            try {
                if (!NetworkUtils.TieneConexionInternet()) {
                    bool continuar = await DisplayAlert(
                        "Sin conexión",
                        "No hay conexión a Internet. Si cierras sesión ahora, los cambios no se sincronizarán. ¿Deseas cerrar sesión igualmente?",
                        "Sí, cerrar",
                        "Cancelar");

                    if (!continuar) return;
                }
                else {
                    // Sincronizar solo si hay conexión
                    await SincronizarYLimpiar();
                }

                await CerrarSesion();
            }
            catch (Exception ex) {
                await DisplayAlert("Error", $"Error al cerrar sesión: {ex.Message}", "OK");
            }
        }
        private async Task SincronizarYLimpiar() {
            try {
                var token = Preferences.Get("FirebaseToken", null);
                if (!string.IsNullOrEmpty(token)) {
                    var firebaseDb = new FirebaseDatabaseService(token);
                    var sqliteDb = new SQLiteService();
                    var sincService = new SincronizacionService(sqliteDb, firebaseDb);
                    await sincService.SincronizarConFirebase();
                }
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error al sincronizar: {ex.Message}");
            }
        }
        private async Task CerrarSesion() {
            Preferences.Clear();
            await Shell.Current.GoToAsync($"//LoginPage");
        }

        protected override bool OnBackButtonPressed() {
            Dispatcher.Dispatch(async () => {
                await VolverAInicio();
            });
            return true; // Indica que hemos manejado el evento
        }

        private async void OnBackClicked(object sender, EventArgs e) {
            await VolverAInicio();
        }

        private async Task VolverAInicio() {
            if (Navigation.NavigationStack.Count > 1) {
                await Navigation.PopAsync();
            }
            else {
                await Shell.Current.GoToAsync("//PrincipalPage");
            }
        }
    }
}