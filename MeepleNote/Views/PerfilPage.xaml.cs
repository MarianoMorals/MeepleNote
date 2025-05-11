using Firebase.Auth;
using MeepleNote.Models;
using MeepleNote.Services;

namespace MeepleNote.Views;

public partial class PerfilPage : ContentPage
{
    private const string ApiKey = "AIzaSyCmcqsaPemAyArjJBBiV7nFm2TeXLFp9cI";

    public PerfilPage()
	{
		InitializeComponent();
	}
    private async void OnGuardarClicked(object sender, EventArgs e) {
        var usuarioId = Preferences.Get("UsuarioId", null);
        if (usuarioId == null)
            return;

        var sqliteDb = new SQLiteService();
        var usuario = new Usuario {
            IdUsuario = 1,
            Nombre = NombreEntry.Text,
            // Otros campos necesarios
        };
        await sqliteDb.SaveUsuarioAsync(usuario);

        var token = await new FirebaseAuthProvider(new FirebaseConfig(ApiKey))
            .SignInWithEmailAndPasswordAsync("TU_EMAIL", "TU_PASSWORD")
            .ContinueWith(t => t.Result.FirebaseToken);

        var firebaseDb = new FirebaseDatabaseService(token);
        var datosLocales = await sqliteDb.ObtenerTodo();

        var fechaActual = DateTime.UtcNow;
        await firebaseDb.SubirDatosUsuario(usuarioId, datosLocales.Perfil, datosLocales.Coleccion,
            datosLocales.Juegos, datosLocales.Partidas, datosLocales.JugadoresPartida, fechaActual);

        sqliteDb.GuardarFechaUltimaSync(fechaActual);

        await DisplayAlert("Guardado", "Perfil actualizado", "OK");
    }

    private async void OnCerrarSesionClicked(object sender, EventArgs e) {
        var usuarioId = Preferences.Get("UsuarioId", null);
        if (usuarioId != null) {
            var sqliteDb = new SQLiteService();
            var token = await new FirebaseAuthProvider(new FirebaseConfig(ApiKey))
                .SignInWithEmailAndPasswordAsync("TU_EMAIL", "TU_PASSWORD") // Si guardas token, úsalo mejor
                .ContinueWith(t => t.Result.FirebaseToken);

            var firebaseDb = new FirebaseDatabaseService(token);
            var datosLocales = await sqliteDb.ObtenerTodo();

            var fechaActual = DateTime.UtcNow;
            await firebaseDb.SubirDatosUsuario(usuarioId, datosLocales.Perfil, datosLocales.Coleccion,
                datosLocales.Juegos, datosLocales.Partidas, datosLocales.JugadoresPartida, fechaActual);

            sqliteDb.GuardarFechaUltimaSync(fechaActual);
        }

        Preferences.Clear();
        await Shell.Current.GoToAsync("//LoginPage");
    }

}