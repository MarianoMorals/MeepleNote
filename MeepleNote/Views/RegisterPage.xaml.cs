using Firebase.Auth;
using Microsoft.Maui.Storage;
using MeepleNote.Services;
using MeepleNote.Models;
using System;
using System.Threading.Tasks;

namespace MeepleNote.Views;

public partial class RegisterPage : ContentPage {
    // Usa la misma API key que en LoginPage
    private const string ApiKey = "AIzaSyCmcqsaPemAyArjJBBiV7nFm2TeXLFp9cI";
    private readonly SQLiteService _sqliteService;
    private string usuarioID;
    private string token;
    public RegisterPage() {
        InitializeComponent();
        _sqliteService = new SQLiteService();

        // Configurar el botón de retroceso
        SetupBackButton();
    }
    private void SetupBackButton() {
        // Para Android e iOS
        NavigationPage.SetHasBackButton(this, true);

        // Personalizar el comportamiento del botón de retroceso
        if (DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.WinUI) {
            NavigationPage.SetBackButtonTitle(this, "Volver");
        }
    }

    protected override bool OnBackButtonPressed() {
        Dispatcher.Dispatch(async () => {
            await VolverALogin();
        });
        return true; // Indica que hemos manejado el evento
    }

    private async void OnBackClicked(object sender, EventArgs e) {
        await VolverALogin();
    }

    private async Task VolverALogin() {
        if (Navigation.NavigationStack.Count > 1) {
            await Navigation.PopAsync();
        }
        else {
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }

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
            // Aquí podrías implementar un sistema de reintentos o notificación de error
        }
    }
}

