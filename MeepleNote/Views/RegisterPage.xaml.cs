using Firebase.Auth;
using Microsoft.Maui.Storage;

namespace MeepleNote.Views;

public partial class RegisterPage : ContentPage {
    // Usa la misma API key que en LoginPage
    private const string ApiKey = "AIzaSyCmcqsaPemAyArjJBBiV7nFm2TeXLFp9cI";

    public RegisterPage() {
        InitializeComponent();
    }

    private async void OnRegisterClicked(object sender, EventArgs e) {
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;
        var confirm = ConfirmPasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) {
            await DisplayAlert("Error", "Email y contraseña son obligatorios.", "OK");
            return;
        }

        if (password != confirm) {
            await DisplayAlert("Error", "Las contraseñas no coinciden.", "OK");
            return;
        }

        try {
            var authProvider = new FirebaseAuthProvider(new FirebaseConfig(ApiKey));
            var result = await authProvider.CreateUserWithEmailAndPasswordAsync(email, password);

            await DisplayAlert("Éxito", "Usuario registrado correctamente.", "OK");

            // Redirige al login
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
