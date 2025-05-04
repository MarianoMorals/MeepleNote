using Firebase.Auth;
using Microsoft.Maui.Storage; // Para Preferences
namespace MeepleNote.Views;

public partial class LoginPage : ContentPage {
    // Tu API Key de Firebase (sácala desde Configuración del proyecto en Firebase)
    private const string ApiKey = "AIzaSyCmcqsaPemAyArjJBBiV7nFm2TeXLFp9cI";

    public LoginPage() {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e) {
        var email = UsernameEntry.Text;
        var password = PasswordEntry.Text;

        try {
            var authProvider = new FirebaseAuthProvider(new FirebaseConfig(ApiKey));
            var auth = await authProvider.SignInWithEmailAndPasswordAsync(email, password);

            var token = auth.FirebaseToken;

            if (string.IsNullOrEmpty(token))
                throw new Exception("Token inválido");

            Preferences.Set("SesionIniciada", RecordarSesionCheck.IsChecked);
            Preferences.Set("UsuarioId", auth.User.LocalId); // UID del usuario

            // Navegar a la página principal (PerfilPage como default)
            await Shell.Current.GoToAsync($"//{nameof(PerfilPage)}");
            
        }
        catch (Exception ex) {
            await DisplayAlert("Error", "Usuario o contraseña incorrectos", "OK");
        }
    }

    private async void OnCerrarSesionClicked(object sender, EventArgs e) {
        Preferences.Remove("SesionIniciada");
        Preferences.Remove("UsuarioId");

        await Shell.Current.GoToAsync("//LoginPage");
    }

    private async void OnIrARegistro(object sender, EventArgs e) {
        await Shell.Current.GoToAsync("//RegisterPage");
    }
}
