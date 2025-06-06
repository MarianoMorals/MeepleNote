using Firebase.Auth;
using Microsoft.Maui.Storage;
using MeepleNote.Services;
using MeepleNote.Models;
using System;
using System.Threading.Tasks;

namespace MeepleNote.Views;

/// <summary>
/// Página de registro de usuario en la aplicación MeepleNote.
/// Permite crear una nueva cuenta con nombre, email, contraseña y fecha de nacimiento.
/// También guarda los datos en SQLite y sincroniza con Firebase.
/// </summary>
public partial class RegisterPage : ContentPage {
    // Usa la misma API key que en LoginPage (en proxima actualizacion se guardara en parametros de la aplicacion)
    private const string ApiKey = "AIzaSyCmcqsaPemAyArjJBBiV7nFm2TeXLFp9cI";
    private readonly SQLiteService _sqliteService;
    private string usuarioID;
    private string token;

    /// <summary>
    /// Constructor. Inicializa componentes y configura botón de retroceso.
    /// </summary>
    public RegisterPage() {
        InitializeComponent();
        _sqliteService = new SQLiteService();

        // Configurar el botón de retroceso
        SetupBackButton();
    }

    /// <summary>
    /// Configura el botón de retroceso visual y su comportamiento.
    /// </summary>
    private void SetupBackButton() {
        // Para Android e iOS
        NavigationPage.SetHasBackButton(this, true);

        // Personalizar el comportamiento del botón de retroceso
        if (DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.WinUI) {
            NavigationPage.SetBackButtonTitle(this, "Volver");
        }
    }

    /// <summary>
    /// Captura el botón de retroceso físico (en Android/Windows) y navega manualmente a la pantalla de login.
    /// </summary>
    protected override bool OnBackButtonPressed() {
        Dispatcher.Dispatch(async () => {
            await VolverALogin();
        });
        return true; // Indica que hemos manejado el evento
    }

    /// <summary>
    /// Evento para el botón de retroceso en pantalla.
    /// </summary>
    private async void OnBackClicked(object sender, EventArgs e) {
        await VolverALogin();
    }

    /// <summary>
    /// Navega a la pantalla de login.
    /// </summary>
    private async Task VolverALogin() {
        if (Navigation.NavigationStack.Count > 1) {
            await Navigation.PopAsync();
        }
        else {
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }

    /// <summary>
    /// Evento que se lanza al pulsar el botón de registro.
    /// Valida los campos, registra el usuario en Firebase Auth, guarda en SQLite y sincroniza con Firebase.
    /// </summary>
    private async void OnRegisterClicked(object sender, EventArgs e) {

        if (!NetworkUtils.TieneConexionInternet()) {
            await DisplayAlert(
                "Sin conexión",
                "Necesitas conexión a Internet para registrarte.",
                "OK");
            return;
        }

        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;
        var confirm = ConfirmPasswordEntry.Text;
        var name = NameEntry.Text; // Obtener el nombre
        var birthDate = BirthDateEntry.Date; // Obtener la fecha de nacimiento

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) {
            await DisplayAlert("Error", "Email y contraseña son obligatorios.", "OK");
            return;
        }

        if (password != confirm) {
            await DisplayAlert("Error", "Las contraseñas no coinciden.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(name)) // Validar el nombre
        {
            await DisplayAlert("Error", "El nombre es obligatorio.", "OK");
            return;
        }

        try {
            var authProvider = new FirebaseAuthProvider(new FirebaseConfig(ApiKey));
            var result = await authProvider.CreateUserWithEmailAndPasswordAsync(email, password);
            var firebaseUserId = result.User.LocalId;

            var nuevoUsuario = new Usuario {
                FirebaseUserId = firebaseUserId,
                Email = email,
                Nombre = name, // Usar el nombre obtenido
                FechaNacimiento = birthDate, // Usar la fecha de nacimiento obtenida
            };

            usuarioID = firebaseUserId;
            token = result.FirebaseToken;

            await _sqliteService.SaveUsuarioAsync(nuevoUsuario);

            await RealizarSincronizacionInicial(); //Asi añadimos los datos de usuario a firebase, por si mas adelante cerramos sin guardar.

            await DisplayAlert("Éxito", "Usuario registrado correctamente.", "OK");
            await Shell.Current.GoToAsync($"//LoginPage");
        }
        catch (FirebaseAuthException firebaseAuthEx) {
            string errorMessage = "Error de autenticación: ";
            switch (firebaseAuthEx.Reason) {
                case AuthErrorReason.EmailExists:
                    errorMessage += "Ya existe un usuario registrado con este correo.";
                    break;
                default:
                    errorMessage += firebaseAuthEx.Message;
                    break;
            }
            await DisplayAlert("Error", errorMessage, "OK");
        }
        catch (Exception ex) {
            await DisplayAlert("Error", $"No se pudo registrar: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Realiza la sincronización inicial de datos del nuevo usuario con Firebase Database.
    /// </summary>
    private async Task RealizarSincronizacionInicial() {
        try {
            if (string.IsNullOrEmpty(usuarioID)) return;

            var firebaseDb = new FirebaseDatabaseService(token);
            var sincService = new SincronizacionService(_sqliteService, firebaseDb, usuarioID);

            // Sincronizar todos los datos locales con Firebase
            await sincService.SincronizarConFirebase();

            Console.WriteLine("Datos sincronizados correctamente con Firebase");
        }
        catch (Exception ex) {
            Console.WriteLine($"Error en sincronización: {ex.Message}");
        }
    }
}

