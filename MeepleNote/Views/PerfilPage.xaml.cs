using Firebase.Auth;
using MeepleNote.Models;
using MeepleNote.Services;
using Microsoft.Maui.Storage;
using System;
using System.Threading.Tasks;
namespace MeepleNote.Views;
public partial class PerfilPage : ContentPage {
    private SincronizacionService _sincronizacionService;
    private readonly SQLiteService _sqliteDb;
    private readonly FirebaseAuthService _firebaseAuthService = new FirebaseAuthService();
    private Usuario _usuarioActual; // Para almacenar el usuario que se est� editando
    public PerfilPage() {
        InitializeComponent();
        _sqliteDb = new SQLiteService();
    }
    protected override async void OnAppearing() {
        base.OnAppearing();
        // Cargar la informaci�n del usuario al aparecer la p�gina
        await CargarPerfilUsuario();
    }
    private async Task CargarPerfilUsuario() {
        var firebaseUsuarioId = Preferences.Get("UsuarioId", null);
        if (!string.IsNullOrEmpty(firebaseUsuarioId)) {
            _usuarioActual = await _sqliteDb.GetUsuarioByFirebaseIdAsync(firebaseUsuarioId);
            if (_usuarioActual != null) {
                BindingContext = _usuarioActual; // Establecer el contexto de binding para mostrar los datos
            }
        }
    }
    private async Task InicializarSincronizacionService() {
        string authToken = Preferences.Get("FirebaseToken", null);
        if (!string.IsNullOrEmpty(authToken)) {
            var firebaseDb = new FirebaseDatabaseService(authToken);
            _sincronizacionService = new SincronizacionService(_sqliteDb, firebaseDb);
        }
        else {
            _sincronizacionService = null;
            await DisplayAlert("Error", "No se encontr� el token de Firebase. La sincronizaci�n no estar� disponible.", "OK");
        }
    }
    private async void OnRestablecerContrase�aClicked(object sender, EventArgs e) {
        try {
            await _firebaseAuthService.SendPasswordResetEmailAsync(_usuarioActual.Email);
            await DisplayAlert("Correo Enviado", "Se ha enviado un correo electr�nico a su direcci�n de correo electr�nico con instrucciones para restablecer su contrase�a.", "OK");
        }
        catch (FirebaseAuthException ex) {
            await DisplayAlert("Error", $"Error al enviar el correo electr�nico de restablecimiento: {ex.Message}", "OK");
        }
        catch (Exception ex) {
            await DisplayAlert("Error", $"Ocurri� un error inesperado: {ex.Message}", "OK");
        }
    }
    private async void OnCerrarSesionClicked(object sender, EventArgs e) {
        await InicializarSincronizacionService();
        if (_sincronizacionService != null) {
            await _sincronizacionService.SincronizarConFirebase();
        }
        _firebaseAuthService.Logout();
        Preferences.Remove("SesionIniciada");
        Preferences.Remove("UsuarioId");
        Preferences.Remove("FirebaseToken");
        // Redirigir a la p�gina de inicio de sesi�n
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }
}
