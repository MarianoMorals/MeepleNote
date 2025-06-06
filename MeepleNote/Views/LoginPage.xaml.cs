using Firebase.Auth;
using MeepleNote.Models;
using MeepleNote.Services;
using Microsoft.Maui.Storage;
using System;
using System.Threading.Tasks;

namespace MeepleNote.Views {
    /// <summary>
    /// Página que permite al usuario iniciar sesión en la aplicación usando Firebase Authentication.
    /// También ofrece opciones para ir a la página de registro y para restablecer la contraseña.
    /// </summary>
    public partial class LoginPage : ContentPage {
        // Servicio para autenticación Firebase
        private readonly FirebaseAuthService _authService = new FirebaseAuthService();

        // Servicio para la base de datos local SQLite
        private SQLiteService _dbService = new SQLiteService();

        /// <summary>
        /// Constructor que inicializa los componentes de la página.
        /// </summary>
        public LoginPage() {
            InitializeComponent();
        }

        /// <summary>
        /// Evento que se ejecuta al pulsar el botón de iniciar sesión.
        /// Valida la conexión, intenta autenticar al usuario y sincroniza datos.
        /// </summary>
        /// <param name="sender">Objeto que envió el evento.</param>
        /// <param name="e">Argumentos del evento.</param>
        private async void OnLoginClicked(object sender, EventArgs e) {
            var email = UsernameEntry.Text;    // Obtiene el correo electrónico ingresado por el usuario
            var password = PasswordEntry.Text; // Obtiene la contraseña ingresada por el usuario

            try {
                // Verifica que el dispositivo tenga conexión a Internet antes de intentar iniciar sesión
                if (!NetworkUtils.TieneConexionInternet()) {
                    await DisplayAlert(
                        "Sin conexión",
                        "Necesitas conexión a Internet para iniciar sesión",
                        "OK");
                    return; // Sale si no hay conexión
                }

                var sqliteDb = new SQLiteService();

                // Intenta autenticar al usuario en Firebase con el email y contraseña proporcionados
                var auth = await _authService.LoginAsync(email, password);

                var token = auth.FirebaseToken;         // Obtiene el token de autenticación Firebase
                var firebaseUsuarioId = auth.User.LocalId; // Obtiene el ID único de usuario en Firebase

                // Validación para asegurarse que el token es válido
                if (string.IsNullOrEmpty(token))
                    throw new Exception("Token inválido");

                // Guarda en las preferencias locales si se desea mantener la sesión iniciada
                Preferences.Set("SesionIniciada", RecordarSesionCheck.IsChecked);

                // Guarda el ID de usuario Firebase para futuras referencias
                Preferences.Set("UsuarioId", firebaseUsuarioId);

                // Guarda el token para usar en futuras peticiones autenticadas
                Preferences.Set("FirebaseToken", token);

                // Inicializa el servicio para acceso a la base de datos Firebase, usando el token
                var firebaseDb = new FirebaseDatabaseService(token);

                // Inicializa el servicio de sincronización entre base local y Firebase
                var sincronizacionService = new SincronizacionService(sqliteDb, firebaseDb);

                // Sincroniza los datos desde Firebase si la sincronización es necesaria
                await sincronizacionService.SincronizarDesdeFirebaseSiNecesario();

                // Verifica si el usuario ya existe en la base local según su Firebase User ID
                var usuarioExistente = await sqliteDb.GetUsuarioByFirebaseIdAsync(firebaseUsuarioId);

                if (usuarioExistente == null) {
                    // Si no existe, crea un nuevo registro local para este usuario
                    var nuevoUsuario = new Usuario { FirebaseUserId = firebaseUsuarioId };
                    await sqliteDb.SaveUsuarioAsync(nuevoUsuario);

                    // Guarda el ID local del usuario en las preferencias
                    Preferences.Set("IdUsuario", nuevoUsuario.IdUsuario);
                }

                // Redirige a la página principal después de iniciar sesión correctamente
                await Shell.Current.GoToAsync($"//PrincipalPage");
            }
            catch (FirebaseAuthException firebaseAuthEx) {
                // Maneja errores comunes de autenticación Firebase para dar mensajes claros al usuario
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

        /// <summary>
        /// Evento que se ejecuta al pulsar para ir a la página de registro.
        /// Valida la conexión y redirige a la página de registro.
        /// </summary>
        private async void OnIrARegistro(object sender, EventArgs e) {
            // Verifica que haya conexión a Internet para permitir registro
            if (!NetworkUtils.TieneConexionInternet()) {
                await DisplayAlert(
                    "Sin conexión",
                    "Necesitas conexión a Internet para registrarte.",
                    "OK");
                return;
            }

            // Redirige a la página de registro usando Shell navigation
            await Shell.Current.GoToAsync($"//RegisterPage");
        }

        /// <summary>
        /// Evento para restablecer la contraseña.
        /// Envía un correo electrónico con instrucciones para restablecer la contraseña si el email es válido.
        /// </summary>
        private async void OnRestablecerContraseñaClicked(object sender, EventArgs e) {
            try {
                // Comprueba conexión a Internet antes de enviar correo de restablecimiento
                if (!NetworkUtils.TieneConexionInternet()) {
                    await DisplayAlert(
                        "Sin conexión",
                        "Necesitas conexión a Internet para restablecer contraseña.",
                        "OK");
                    return;
                }

                // Valida que se haya ingresado un correo electrónico
                if (string.IsNullOrEmpty(UsernameEntry.Text)) {
                    await DisplayAlert("Error", "Por favor, ingrese su correo electrónico para restablecer la contraseña.", "OK");
                    return;
                }

                // Envía el correo para restablecer contraseña usando el servicio FirebaseAuth
                await _authService.SendPasswordResetEmailAsync(UsernameEntry.Text);

                // Notifica al usuario que se ha enviado el correo
                await DisplayAlert("Correo Enviado", "Se ha enviado un correo electrónico a su dirección de correo electrónico con instrucciones para restablecer su contraseña.", "OK");
            }
            catch (FirebaseAuthException ex) {
                // Maneja errores específicos de Firebase al enviar correo de restablecimiento
                await DisplayAlert("Error", $"Error al enviar el correo electrónico de restablecimiento: {ex.Message}", "OK");
            }
            catch (Exception ex) {
                // Maneja errores generales inesperados
                await DisplayAlert("Error", $"Ocurrió un error inesperado: {ex.Message}", "OK");
            }
        }
    }
}
