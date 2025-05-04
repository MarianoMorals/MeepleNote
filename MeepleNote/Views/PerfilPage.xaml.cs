namespace MeepleNote.Views;

public partial class PerfilPage : ContentPage
{
	public PerfilPage()
	{
		InitializeComponent();
	}
    private void OnGuardarClicked(object sender, EventArgs e) {
        // Guardar nuevos datos del usuario
        Preferences.Set("UsuarioActual", NombreEntry.Text);
        DisplayAlert("Guardado", "Perfil actualizado", "OK");
    }
    private async void OnCerrarSesionClicked(object sender, EventArgs e) {
        Preferences.Remove("SesionIniciada");
        Preferences.Remove("UsuarioActual");

        // Limpia el historial de navegación y vuelve a Login
        await Shell.Current.GoToAsync("//LoginPage");
    }
}