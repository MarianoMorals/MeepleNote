using MeepleNote.Models;
using MeepleNote.Services;
using System;

namespace MeepleNote.Views {

    /// <summary>
    /// Página para crear una nueva partida pública de un juego.
    /// Permite configurar la fecha, hora, ciudad, email de contacto y número de jugadores.
    /// </summary>
    public partial class NuevaPartidaPublicaPage : ContentPage {
        private readonly SQLiteService _dbService;
        private readonly FirebaseDatabaseService _firebaseService;
        private readonly Juego _juego;
        private string emailUsuario;

        /// <summary>
        /// Fecha actual (hoy) para uso en bindings.
        /// </summary>
        public DateTime TodayDate => DateTime.Today;

        /// <summary>
        /// Juego para el cual se crea la partida.
        /// </summary>
        public Juego Juego => _juego;

        /// <summary>
        /// Constructor que inicializa la página con el juego pasado como parámetro.
        /// Inicializa servicios, configura valores por defecto y obtiene email del organizador.
        /// </summary>
        /// <param name="juego">Juego para el que se crea la partida pública.</param>
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

        /// <summary>
        /// Obtiene el email del organizador (usuario) a partir de su ID y lo muestra en el campo correspondiente.
        /// </summary>
        /// <param name="idUsuarioOrganizador">ID del usuario organizador.</param>
        private async void ObtenerEmailOrganizador(string idUsuarioOrganizador) {
            var email = await _dbService.GetEmailUsuarioAsync(idUsuarioOrganizador);

            if (string.IsNullOrEmpty(email)) {
                await DisplayAlert("Info", "No se pudo obtener el email de contacto", "OK");
                emailUsuario = string.Empty;
            }

            EmailEntry.Text = email;
        }

        /// <summary>
        /// Evento que se ejecuta al pulsar el botón "Publicar".
        /// Valida los datos introducidos, crea la partida pública en Firebase y guarda una copia localmente.
        /// </summary>
        /// <param name="sender">El botón que disparó el evento.</param>
        /// <param name="e">Argumentos del evento.</param>
        private async void OnPublicarClicked(object sender, EventArgs e) {

            // Comprobar conexión a Internet
            if (!NetworkUtils.TieneConexionInternet()) {
                await DisplayAlert(
                    "Sin conexión",
                    "Necesitas conexión a Internet para publicar una partida.",
                    "OK");
                return;
            }

            // Validar que se indique una ciudad
            if (string.IsNullOrWhiteSpace(CiudadEntry.Text)) {
                await DisplayAlert("Error", "Debes indicar una ciudad", "OK");
                return;
            }

            // Validar email con formato básico
            if (string.IsNullOrWhiteSpace(EmailEntry.Text) || !EmailEntry.Text.Contains("@")) {
                await DisplayAlert("Error", "Debes indicar un email válido", "OK");
                return;
            }

            // Validar que la fecha y hora sean futuras
            var fechaCompleta = FechaPicker.Date.Add(HoraPicker.Time);
            if (fechaCompleta < DateTime.Now) {
                await DisplayAlert("Error", "La fecha debe ser futura", "OK");
                return;
            }

            var idUsuario = Preferences.Get("UsuarioId", "1");
            Usuario usuario = await _dbService.GetUsuarioByFirebaseIdAsync(idUsuario);

            // Crear objeto partida pública con datos del formulario
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
                // 1. Crear partida en Firebase para obtener ID único
                partida.IdFirebase = await _firebaseService.CrearPartidaPublicaEnFirebase(partida);

                // 2. Guardar partida localmente en SQLite
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
