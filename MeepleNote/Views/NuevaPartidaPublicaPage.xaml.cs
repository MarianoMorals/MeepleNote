using MeepleNote.Models;
using MeepleNote.Services;
using System;

namespace MeepleNote.Views {
    public partial class NuevaPartidaPublicaPage : ContentPage {
        private readonly SQLiteService _dbService;
        private readonly FirebaseDatabaseService _firebaseService;
        private readonly Juego _juego;
        private string emailUsuario;

        public DateTime TodayDate => DateTime.Today;
        public Juego Juego => _juego;

        public NuevaPartidaPublicaPage(Juego juego) {
            InitializeComponent();

            string authToken = Preferences.Get("FirebaseToken", null);
            if (!string.IsNullOrEmpty(authToken)) {
                _firebaseService = new FirebaseDatabaseService(authToken);
            }

            _dbService = new SQLiteService();
            _juego = juego;
            BindingContext = this;

            // Configurar valores por defecto
            FechaPicker.Date = DateTime.Today.AddDays(1);
            HoraPicker.Time = new TimeSpan(18, 0, 0);
            JugadoresPicker.SelectedIndex = 3; // 4 jugadores por defecto

            var idUsuario = Preferences.Get("UsuarioId", "0");

            ObtenerEmailOrganizador(idUsuario);

        }
        private async void ObtenerEmailOrganizador(string idUsuarioOrganizador) {
            var email = await _dbService.GetEmailUsuarioAsync(idUsuarioOrganizador);

            if (string.IsNullOrEmpty(email)) {
                await DisplayAlert("Info", "No se pudo obtener el email de contacto", "OK");
                emailUsuario = string.Empty;
            }

            EmailEntry.Text = email;
        }
        private async void OnPublicarClicked(object sender, EventArgs e) {
            if (string.IsNullOrWhiteSpace(CiudadEntry.Text)) {
                await DisplayAlert("Error", "Debes indicar una ciudad", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(EmailEntry.Text) || !EmailEntry.Text.Contains("@")) {
                await DisplayAlert("Error", "Debes indicar un email válido", "OK");
                return;
            }

            var fechaCompleta = FechaPicker.Date.Add(HoraPicker.Time);
            if (fechaCompleta < DateTime.Now) {
                await DisplayAlert("Error", "La fecha debe ser futura", "OK");
                return;
            }

            var idUsuario = Preferences.Get("UsuarioId", "1");
            Usuario usuario = await _dbService.GetUsuarioByFirebaseIdAsync(idUsuario);

            var partida = new PartidaPublica {
                IdFirebase = null, // Temporal hasta obtener ID de Firebase
                IdJuego = _juego.IdJuego,
                IdUsuarioOrganizador = idUsuario,
                NombreOrganizador = usuario.Nombre,
                EmailContacto = EmailEntry.Text,
                Fecha = fechaCompleta,
                Ciudad = CiudadEntry.Text,
                JugadoresRequeridos = JugadoresPicker.SelectedIndex + 1,
                Completada = false
            };

            try {
                // 1. Crear en Firebase primero para obtener ID
                partida.IdFirebase = await _firebaseService.CrearPartidaPublicaEnFirebase(partida);

                // 2. Guardar localmente (IdLocal se autoincrementará)
                await _dbService.SavePartidaPublicaAsync(partida);

                await DisplayAlert("Éxito", "Partida pública creada", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex) {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}