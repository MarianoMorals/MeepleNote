using MeepleNote.Models;
using MeepleNote.Services;
using System.Diagnostics;

namespace MeepleNote.Views {
    /// <summary>
    /// Página que muestra la colección de juegos del usuario.
    /// Permite ver detalles y eliminar juegos seleccionados.
    /// </summary>
    public partial class ColeccionPage : ContentPage {
        // Servicio para interactuar con la base de datos local
        private SQLiteService dbService;

        // Bandera para controlar navegaciones duplicadas (pulsaciones multiples)
        private bool _isNavigating = false;

        /// <summary>
        /// Constructor: Inicializa componentes y crea instancia del servicio SQLite.
        /// </summary>
        public ColeccionPage() {
            InitializeComponent(); 
            dbService = new SQLiteService(); // Inicializa servicio de base de datos
        }

        /// <summary>
        /// Evento que se ejecuta cuando la página aparece en pantalla.
        /// Carga automáticamente la colección del usuario.
        /// </summary>
        protected override async void OnAppearing() {
            base.OnAppearing();
            await CargarColeccion(); // Carga los juegos de la colección
        }

        /// <summary>
        /// Carga los juegos que están en la colección del usuario desde la base de datos.
        /// Actualiza la interfaz según los resultados.
        /// </summary>
        private async Task CargarColeccion() {
            // Obtiene el ID del usuario desde las preferencias
            var idUsuario = Preferences.Get("UsuarioId", "0");

            // Consulta los juegos marcados como "EnColeccion"
            var juegos = await dbService.GetJuegosAsyncEnColeccion(idUsuario.ToString());

            // Asigna los juegos al ListView
            ColeccionList.ItemsSource = juegos;

            // Muestra/Oculta elementos según existan juegos
            EliminarButton.IsVisible = juegos?.Any() == true;
            EmptyLabel.IsVisible = !juegos.Any();
        }

        /// <summary>
        /// Evento al hacer clic en "Ver Detalles" de un juego.
        /// Navega a la página de detalles del juego seleccionado.
        /// </summary>
        private async void OnVerDetallesClicked(object sender, EventArgs e) {
            // Evita navegaciones duplicadas
            if (_isNavigating)
                return;

            try {
                _isNavigating = true; // Bloquea navegación adicional
                var button = sender as Button;
                button.IsEnabled = false; // Deshabilita el botón temporalmente

                // Obtiene el juego asociado al botón
                if (button.BindingContext is Juego juego) {
                    if (juego == null) {
                        await DisplayAlert("Error", "Juego no disponible", "OK");
                        return;
                    }

                    // Navega a la página de detalles
                    var detailPage = new DetalleJuegoPage(juego);
                    await Navigation.PushAsync(detailPage);
                }
            }
            catch (Exception ex) {
                // Registra errores técnicos
                Debug.WriteLine($"ERROR: {ex.ToString()}");
                await DisplayAlert("Error", $"Error técnico: {ex.Message}", "OK");
            }
            finally {
                _isNavigating = false; // Libera el bloqueo

                // Reactiva el botón si sigue existiendo
                if (sender is Button btn)
                    btn.IsEnabled = true;
            }
        }

        /// <summary>
        /// Evento al hacer clic en "Eliminar Seleccionados".
        /// Elimina los juegos marcados de la colección del usuario.
        /// </summary>
        private async void OnEliminarSeleccionadosClicked(object sender, EventArgs e) {
            // Obtiene la lista actual de juegos
            var juegos = (List<Juego>)ColeccionList.ItemsSource;

            // Filtra los juegos seleccionados
            var seleccionados = juegos.Where(j => j.IsSelected).ToList();

            // Valida si hay selección
            if (!seleccionados.Any()) {
                await DisplayAlert("Aviso", "No has seleccionado ningún juego.", "OK");
                return;
            }

            // Pide confirmación
            bool confirmar = await DisplayAlert(
                "Eliminar",
                $"¿Eliminar {seleccionados.Count} juego(s) de tu colección?",
                "Sí",
                "No"
            );

            if (confirmar) {
                // Procesa cada juego seleccionado
                foreach (var juego in seleccionados) {
                    var idUsuario = Preferences.Get("UsuarioId", "0");

                    // Marca el juego como "no en colección"
                    await dbService.QuitarJuegoExistenteDeColeccion(juego.IdJuego, idUsuario);
                }

                // Recarga la lista actualizada
                await CargarColeccion();
            }
        }
    }
}