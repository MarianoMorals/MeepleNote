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
            // Iniciar sesión en Firebase Authentication
            var auth = await _authService.LoginAsync(email, password);
            var token = auth.FirebaseToken;
            var firebaseUsuarioId = auth.User.LocalId; // Obtener el ID único del usuario de Firebase

            if (string.IsNullOrEmpty(token))
                throw new Exception("Token inválido");

            // Guardar preferencias de sesión y el Firebase User ID
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

            // Sincronizar datos desde Firebase si es necesario al iniciar sesión
            await sincronizacionService.SincronizarDesdeFirebaseSiNecesario();

            // Redirigir a la página de Colección
            await Shell.Current.GoToAsync($"//{nameof(ColeccionPage)}");
        }
        catch (FirebaseAuthException firebaseAuthEx) {
            string errorMessage = "Error de autenticación: ";
            switch (firebaseAuthEx.Reason) {
                case AuthErrorReason.InvalidEmailAddress:
                    errorMessage += "Correo electrónico inválido.";
                    break;
                case AuthErrorReason.WrongPassword:
                    errorMessage += "Contraseña incorrecta.";
                    break;
                default:
                    errorMessage += firebaseAuthEx.Message;
                    break;
            }
            await DisplayAlert("Error", errorMessage, "OK");
        }
        catch (Exception ex) {
            await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
        }
    }

    private async void OnIrARegistro(object sender, EventArgs e) {
        // Redirigir a la página de registro
        await Shell.Current.GoToAsync("//RegisterPage");
    }

    private async void OnRestablecerContraseñaClicked(object sender, EventArgs e) {
        try {
            if (string.IsNullOrEmpty(UsernameEntry.Text)) {
                await DisplayAlert("Error", "Por favor, ingrese su correo electrónico para restablecer la contraseña.", "OK");
                return;
            }
            await _authService.SendPasswordResetEmailAsync(UsernameEntry.Text);
            await DisplayAlert("Correo Enviado", "Se ha enviado un correo electrónico a su dirección de correo electrónico con instrucciones para restablecer su contraseña.", "OK");
        }
        catch (FirebaseAuthException ex) {
            await DisplayAlert("Error", $"Error al enviar el correo electrónico de restablecimiento: {ex.Message}", "OK");
        }
        catch (Exception ex) {
            await DisplayAlert("Error", $"Ocurrió un error inesperado: {ex.Message}", "OK");
        }
    }
}
