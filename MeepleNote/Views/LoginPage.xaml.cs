using Firebase.Auth;
using MeepleNote.Services;
using Microsoft.Maui.Storage; // Para Preferences
namespace MeepleNote.Views;

public partial class LoginPage : ContentPage {
    
    // Se asume que _authService está bien definido en algún lugar
    private readonly FirebaseAuthService _authService = new FirebaseAuthService();

    public LoginPage() {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e) {
        var email = UsernameEntry.Text;
        var password = PasswordEntry.Text;

        try {
            // Login en Firebase
            var auth = await _authService.LoginAsync(email, password);
            var token = auth.FirebaseToken;
            var usuarioId = auth.User.LocalId;

            if (string.IsNullOrEmpty(token))
                throw new Exception("Token inválido");

            // Guardamos preferencias para recordar la sesión
            Preferences.Set("SesionIniciada", RecordarSesionCheck.IsChecked);
            Preferences.Set("UsuarioId", usuarioId);

            // Sincronización de datos
            var firebaseDb = new FirebaseDatabaseService(token);
            var sqliteDb = new SQLiteService();

            var fechaFirebase = await firebaseDb.ObtenerFechaUltimaSync(usuarioId);
            var fechaLocal = sqliteDb.ObtenerFechaUltimaSync();

            if (fechaFirebase > fechaLocal) {
                var datosFirebase = await firebaseDb.DescargarTodo(usuarioId);
                await sqliteDb.GuardarTodoDesdeFirebase(datosFirebase);
            }

            // Redirigir a la página de Colección
            await Shell.Current.GoToAsync($"//{nameof(ColeccionPage)}");
        }
        catch (Exception ex) {
            await DisplayAlert("Error", "Usuario o contraseña incorrectos", "OK");
        }
    }

    private async void OnCerrarSesionClicked(object sender, EventArgs e) {
        // Eliminar preferencias al cerrar sesión
        Preferences.Remove("SesionIniciada");
        Preferences.Remove("UsuarioId");

        // Redirigir a la página de login
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private async void OnIrARegistro(object sender, EventArgs e) {
        // Redirigir a la página de registro
        await Shell.Current.GoToAsync("//RegisterPage");
    }
}
