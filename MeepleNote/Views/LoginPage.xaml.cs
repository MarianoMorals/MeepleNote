using Firebase.Auth;
using MeepleNote.Models;
using MeepleNote.Services;
using Microsoft.Maui.Storage; // Para Preferences
using System;
using System.Threading.Tasks;

namespace MeepleNote.Views;

public partial class LoginPage : ContentPage {
    private readonly FirebaseAuthService _authService = new FirebaseAuthService();
    private SQLiteService _dbService = new SQLiteService();

    public LoginPage() {
        InitializeComponent();
        //_dbService.ResetearBaseDatosCompleta();
    }

    private async void OnLoginClicked(object sender, EventArgs e) {
        var email = UsernameEntry.Text;
        var password = PasswordEntry.Text;

        try {

            if (!NetworkUtils.TieneConexionInternet()) {
                await DisplayAlert(
                    "Sin conexión",
                    "Necesitas conexión a Internet para iniciar sesión",
                    "OK");
                return;
            }

            var sqliteDb = new SQLiteService();
            //await sqliteDb.LimpiarDatosUsuario();


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
            var sincronizacionService = new SincronizacionService(sqliteDb, firebaseDb);

            // Sincronizar datos desde Firebase si es necesario al iniciar sesión
            await sincronizacionService.SincronizarDesdeFirebaseSiNecesario();


            // Verificar si el usuario ya existe en la base de datos local por su Firebase User ID
            var usuarioExistente = await sqliteDb.GetUsuarioByFirebaseIdAsync(firebaseUsuarioId);
            if (usuarioExistente == null) {
                // Si el usuario no existe localmente, crear un nuevo registro con el Firebase User ID
                var nuevoUsuario = new Usuario { FirebaseUserId = firebaseUsuarioId };
                await sqliteDb.SaveUsuarioAsync(nuevoUsuario);

                Preferences.Set("IdUsuario", nuevoUsuario.IdUsuario);

            }


            //Guardar preferenias de IdUsuario
            //Preferences.Set("IdUsuario", usuarioExistente.IdUsuario);

            // Redirigir a la página de Colección
            await Shell.Current.GoToAsync($"//PrincipalPage");
        }
        catch (FirebaseAuthException firebaseAuthEx) {
            string errorMessage = "Error de autenticación: ";

            switch (firebaseAuthEx.Reason.ToString()) {
                case "InvalidEmailAddress":
                    errorMessage += "El correo electrónico no tiene un formato válido.";
                    break;
                case "MissingEmail":
                    errorMessage += "Debes ingresar un correo electrónico.";
                    break;
                case "MissingPassword":
                    errorMessage += "Debes ingresar una contraseña.";
                    break;
                case "WrongPassword":
                    errorMessage += "La contraseña es incorrecta.";
                    break;
                case "EmailNotFound":
                    errorMessage += "No existe una cuenta con ese correo electrónico.";
                    break;
                case "UserDisabled":
                    errorMessage += "La cuenta ha sido deshabilitada.";
                    break;
                case "TooManyAttemptsTryLater":
                    errorMessage += "Demasiados intentos fallidos. Intenta más tarde.";
                    break;
                case "OperationNotAllowed":
                    errorMessage += "Este tipo de inicio de sesión no está permitido.";
                    break;
                default:
                    errorMessage += "Compruebe que los datos sean correctos.";
                    break;
            }

            await DisplayAlert("Error", errorMessage, "OK");
        }


    }

    private async void OnIrARegistro(object sender, EventArgs e) {

        if (!NetworkUtils.TieneConexionInternet()) {
            await DisplayAlert(
                "Sin conexión",
                "Necesitas conexión a Internet para registrarte.",
                "OK");
            return;
        }

        // Redirigir a la página de registro
        await Shell.Current.GoToAsync($"//RegisterPage");
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
