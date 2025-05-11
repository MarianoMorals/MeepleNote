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
    private Usuario _usuarioActual; // Para almacenar el usuario que se está editando
    public PerfilPage() {
        InitializeComponent();
        _sqliteDb = new SQLiteService();
    }
    protected override async void OnAppearing() {
        base.OnAppearing();
        // Cargar la información del usuario al aparecer la página
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
            await DisplayAlert("Error", "No se encontró el token de Firebase. La sincronización no estará disponible.", "OK");
        }
    }
    private async void OnRestablecerContraseñaClicked(object sender, EventArgs e) {
        try {
            await _firebaseAuthService.SendPasswordResetEmailAsync(_usuarioActual.Email);
            await DisplayAlert("Correo Enviado", "Se ha enviado un correo electrónico a su dirección de correo electrónico con instrucciones para restablecer su contraseña.", "OK");
        }
        catch (FirebaseAuthException ex) {
            await DisplayAlert("Error", $"Error al enviar el correo electrónico de restablecimiento: {ex.Message}", "OK");
        }
        catch (Exception ex) {
            await DisplayAlert("Error", $"Ocurrió un error inesperado: {ex.Message}", "OK");
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
        // Redirigir a la página de inicio de sesión
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }
}
