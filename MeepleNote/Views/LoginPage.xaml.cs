using Firebase.Auth;
using MeepleNote.Services;
using Microsoft.Maui.Storage; // Para Preferences
namespace MeepleNote.Views;

public partial class LoginPage : ContentPage {
    
    // Se asume que _authService est� bien definido en alg�n lugar
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
                throw new Exception("Token inv�lido");

            // Guardamos preferencias para recordar la sesi�n
            Preferences.Set("SesionIniciada", RecordarSesionCheck.IsChecked);
            Preferences.Set("UsuarioId", usuarioId);

            // Sincronizaci�n de datos
            var firebaseDb = new FirebaseDatabaseService(token);
            var sqliteDb = new SQLiteService();

            var fechaFirebase = await firebaseDb.ObtenerFechaUltimaSync(usuarioId);
            var fechaLocal = sqliteDb.ObtenerFechaUltimaSync();

            if (fechaFirebase > fechaLocal) {
                var datosFirebase = await firebaseDb.DescargarTodo(usuarioId);
                await sqliteDb.GuardarTodoDesdeFirebase(datosFirebase);
            }

            // Redirigir a la p�gina de Colecci�n
            await Shell.Current.GoToAsync($"//{nameof(ColeccionPage)}");
        }
        catch (Exception ex) {
            await DisplayAlert("Error", "Usuario o contrase�a incorrectos", "OK");
        }
    }

    private async void OnCerrarSesionClicked(object sender, EventArgs e) {
        // Eliminar preferencias al cerrar sesi�n
        Preferences.Remove("SesionIniciada");
        Preferences.Remove("UsuarioId");

        // Redirigir a la p�gina de login
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private async void OnIrARegistro(object sender, EventArgs e) {
        // Redirigir a la p�gina de registro
        await Shell.Current.GoToAsync("//RegisterPage");
    }
}
