using Firebase.Auth;
using MeepleNote.Models;
using MeepleNote.Services;
using Microsoft.Maui.Storage; // Para Preferences
using System;
using System.Threading.Tasks;

namespace MeepleNote.Views;

public partial class LoginPage : ContentPage {
    private readonly FirebaseAuthService _authService = new FirebaseAuthService();

    public LoginPage() {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e) {
        var email = UsernameEntry.Text;
        var password = PasswordEntry.Text;

        try {
            // Iniciar sesi�n en Firebase Authentication
            var auth = await _authService.LoginAsync(email, password);
            var token = auth.FirebaseToken;
            var firebaseUsuarioId = auth.User.LocalId; // Obtener el ID �nico del usuario de Firebase

            if (string.IsNullOrEmpty(token))
                throw new Exception("Token inv�lido");

            // Guardar preferencias de sesi�n y el Firebase User ID
            Preferences.Set("SesionIniciada", RecordarSesionCheck.IsChecked);
            Preferences.Set("UsuarioId", firebaseUsuarioId); // Guardar el Firebase User ID
            Preferences.Set("FirebaseToken", token); // Guardar el token para futuras peticiones

            // Inicializar servicios con el token
            var firebaseDb = new FirebaseDatabaseService(token);
            var sqliteDb = new SQLiteService();
            var sincronizacionService = new SincronizacionService(sqliteDb, firebaseDb);

            // Verificar si el usuario ya existe en la base de datos local por su Firebase User ID
            var usuarioExistente = await sqliteDb.GetUsuarioByFirebaseIdAsync(firebaseUsuarioId);
            if (usuarioExistente == null) {
                // Si el usuario no existe localmente, crear un nuevo registro con el Firebase User ID
                var nuevoUsuario = new Usuario { FirebaseUserId = firebaseUsuarioId };
                await sqliteDb.SaveUsuarioAsync(nuevoUsuario);
            }

            // Sincronizar datos desde Firebase si es necesario al iniciar sesi�n
            await sincronizacionService.SincronizarDesdeFirebaseSiNecesario();

            // Redirigir a la p�gina de Colecci�n
            await Shell.Current.GoToAsync($"//{nameof(ColeccionPage)}");
        }
        catch (FirebaseAuthException firebaseAuthEx) {
            string errorMessage = "Error de autenticaci�n: ";
            switch (firebaseAuthEx.Reason) {
                case AuthErrorReason.InvalidEmailAddress:
                    errorMessage += "Correo electr�nico inv�lido.";
                    break;
                case AuthErrorReason.WrongPassword:
                    errorMessage += "Contrase�a incorrecta.";
                    break;
                default:
                    errorMessage += firebaseAuthEx.Message;
                    break;
            }
            await DisplayAlert("Error", errorMessage, "OK");
        }
        catch (Exception ex) {
            await DisplayAlert("Error", $"Ocurri� un error: {ex.Message}", "OK");
        }
    }

    private async void OnIrARegistro(object sender, EventArgs e) {
        // Redirigir a la p�gina de registro
        await Shell.Current.GoToAsync("//RegisterPage");
    }

    private async void OnRestablecerContrase�aClicked(object sender, EventArgs e) {
        try {
            if (string.IsNullOrEmpty(UsernameEntry.Text)) {
                await DisplayAlert("Error", "Por favor, ingrese su correo electr�nico para restablecer la contrase�a.", "OK");
                return;
            }
            await _authService.SendPasswordResetEmailAsync(UsernameEntry.Text);
            await DisplayAlert("Correo Enviado", "Se ha enviado un correo electr�nico a su direcci�n de correo electr�nico con instrucciones para restablecer su contrase�a.", "OK");
        }
        catch (FirebaseAuthException ex) {
            await DisplayAlert("Error", $"Error al enviar el correo electr�nico de restablecimiento: {ex.Message}", "OK");
        }
        catch (Exception ex) {
            await DisplayAlert("Error", $"Ocurri� un error inesperado: {ex.Message}", "OK");
        }
    }
}
