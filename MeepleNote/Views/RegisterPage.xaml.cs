using Firebase.Auth;
using Microsoft.Maui.Storage;
using MeepleNote.Services;
using MeepleNote.Models;
using System;
using System.Threading.Tasks;

namespace MeepleNote.Views;

public partial class RegisterPage : ContentPage {
    // Usa la misma API key que en LoginPage
    private const string ApiKey = "AIzaSyCmcqsaPemAyArjJBBiV7nFm2TeXLFp9cI"; // Reemplaza con tu API Key real
    private readonly SQLiteService _sqliteService;

    public RegisterPage() {
        InitializeComponent();
        _sqliteService = new SQLiteService();
    }

    private async void OnRegisterClicked(object sender, EventArgs e) {
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

            await _sqliteService.SaveUsuarioAsync(nuevoUsuario);

            await DisplayAlert("Éxito", "Usuario registrado correctamente.", "OK");
            await Shell.Current.GoToAsync("//LoginPage");
        }
        catch (FirebaseAuthException ex) {
            await DisplayAlert("Error", $"Firebase error: {ex.Reason}", "OK");
        }
        catch (Exception ex) {
            await DisplayAlert("Error", $"No se pudo registrar: {ex.Message}", "OK");
        }
    }
}

