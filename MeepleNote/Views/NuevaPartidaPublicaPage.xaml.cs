using MeepleNote.Models;
using MeepleNote.Services;
using System;

namespace MeepleNote.Views {
    public partial class NuevaPartidaPublicaPage : ContentPage {
        private readonly SQLiteService _dbService;
        private readonly Juego _juego;

        public DateTime TodayDate => DateTime.Today;
        public Juego Juego => _juego;

        public NuevaPartidaPublicaPage(Juego juego) {
            InitializeComponent();
            _dbService = new SQLiteService();
            _juego = juego;
            BindingContext = this;

            // Configurar valores por defecto
            FechaPicker.Date = DateTime.Today.AddDays(1);
            HoraPicker.Time = new TimeSpan(18, 0, 0);
            JugadoresPicker.SelectedIndex = 3; // 4 jugadores por defecto
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

            var partida = new PartidaPublica {
                IdJuego = _juego.IdJuego,
                IdUsuarioOrganizador = Preferences.Get("IdUsuario", 0),
                NombreOrganizador = Preferences.Get("NombreUsuario", "Anónimo"),
                EmailContacto = EmailEntry.Text,
                Fecha = fechaCompleta,
                Ciudad = CiudadEntry.Text,
                JugadoresRequeridos = JugadoresPicker.SelectedIndex + 1,
                Completada = false
            };

            try {
                await _dbService.SavePartidaPublicaAsync(partida);
                await DisplayAlert("Éxito", "Partida pública creada", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex) {
                await DisplayAlert("Error", $"No se pudo publicar: {ex.Message}", "OK");
            }
        }
    }
}