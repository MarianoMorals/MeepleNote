using System.Collections.ObjectModel;
using System.Diagnostics;
using MeepleNote.Models;
using MeepleNote.Services;
using System.Windows.Input;

namespace MeepleNote.Views {
    /// <summary>
    /// Página para explorar juegos, buscar y añadir a la colección.
    /// Implementa paginación, sugerencias y refresco.
    /// </summary>
    public partial class ExplorarPage : ContentPage {
        // Servicios para obtener juegos y gestionar base de datos local
        private readonly ExplorarService _explorarService;
        private readonly SQLiteService _dbService;

        // Variables para paginación y control de carga
        private int _currentPage = 0;          // Página actual en la búsqueda paginada
        private const int PageSize = 10;       // Número de elementos a cargar por página
        private bool _isLoading = false;       // Indica si se está cargando más datos actualmente
        private bool _hasMoreItems = true;     // Indica si quedan más resultados para cargar
        private string _currentQuery = string.Empty; // Última cadena de búsqueda usada

        // Colección observable para mostrar la lista de juegos en UI
        private ObservableCollection<Juego> _juegos = new ObservableCollection<Juego>();

        // Controla el estado del refresco para la interfaz (Pull to refresh)
        private bool _isRefreshing = false;
        /// <summary>
        /// Propiedad enlazada a la UI para mostrar el estado de refresco.
        /// Al cambiar, notifica para actualizar la interfaz.
        /// </summary>
        public bool IsRefreshing {
            get => _isRefreshing;
            set {
                if (_isRefreshing != value) {
                    _isRefreshing = value;
                    OnPropertyChanged(nameof(IsRefreshing)); // Notifica cambio para UI
                }
            }
        }

        // Controla que no haya navegación simultánea para evitar errores
        private bool _isNavigating = false;

        // Almacena el último usuario para detectar cambios de usuario y resetear búsquedas
        private string _ultimoUsuario;

        /// <summary>
        /// Comando que ejecuta la acción de refrescar la lista de juegos.
        /// Usado para Pull to Refresh.
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Constructor de la página ExplorarPage.
        /// Inicializa servicios, comandos, binding context y eventos.
        /// </summary>
        public ExplorarPage() {
            InitializeComponent();

            // Inicializa servicios de exploración y base de datos
            _explorarService = new ExplorarService();
            _dbService = new SQLiteService();

            // Crea comando RefreshCommand ligado a RefreshDataAsync
            RefreshCommand = new Command(async () => await RefreshDataAsync());

            // Establece el contexto de datos para la UI (data binding)
            BindingContext = this;

            // Asocia la colección de juegos con la lista visible en la UI
            ResultadosList.ItemsSource = _juegos;

            // Umbral para cargar más juegos cuando queda 1 item visible en lista
            ResultadosList.RemainingItemsThreshold = 1;

            // Evento que se lanza al alcanzar el umbral para cargar más resultados
            ResultadosList.RemainingItemsThresholdReached += ResultadosList_RemainingItemsThresholdReached;
        }

        /// <summary>
        /// Evento llamado cuando la página aparece en pantalla.
        /// Resetea estado si el usuario ha cambiado para evitar datos cruzados.
        /// </summary>
        protected override async void OnAppearing() {
            base.OnAppearing();

            // Obtiene usuario actual guardado en preferencias
            string usuarioActual = Preferences.Get("UsuarioId", "0");

            // Si es la primera vez o usuario ha cambiado
            if (string.IsNullOrWhiteSpace(_ultimoUsuario) || _ultimoUsuario != usuarioActual) {
                // Limpiar la caja de texto de búsqueda
                BuscarEntry.Text = string.Empty;

                // Ocultar lista de sugerencias
                SugerenciasList.IsVisible = false;

                // Ocultar mensaje de "Sin resultados"
                SinResultadosLabel.IsVisible = false;

                // Ocultar indicadores de carga
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                LoadingMoreIndicator.IsVisible = false;

                // Resetear paginación y búsqueda
                _currentQuery = string.Empty;
                _currentPage = 0;
                _hasMoreItems = true;

                // Limpiar la colección de juegos que se muestra
                _juegos.Clear();

                // Resetear indicador de refresco
                IsRefreshing = false;

                // Guardar usuario actual para futuras comparaciones
                _ultimoUsuario = usuarioActual;
            }
        }

        /// <summary>
        /// Método para refrescar los datos de la búsqueda actual.
        /// Resetea paginación y recarga datos.
        /// </summary>
        private async Task RefreshDataAsync() {
            // Si no hay consulta, no hace nada y para refresco
            if (string.IsNullOrWhiteSpace(_currentQuery)) {
                IsRefreshing = false;
                return;
            }

            try {
                // Resetear página y permitir cargar más
                _currentPage = 0;
                _hasMoreItems = true;

                // Limpiar resultados anteriores
                _juegos.Clear();

                // Ocultar mensaje de sin resultados
                SinResultadosLabel.IsVisible = false;

                // Cargar primeros resultados
                await LoadMoreItems();
            }
            catch (Exception ex) {
                // Mostrar error en debug y alerta al usuario
                Debug.WriteLine($"Error en RefreshDataAsync: {ex.Message}");
                await DisplayAlert("Error", "No se pudo actualizar la lista", "OK");
            }
            finally {
                // Finalizar estado de refresco (para UI)
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// Evento llamado al pulsar el botón de búsqueda.
        /// Valida conexión y lanza búsqueda.
        /// </summary>
        private async void OnBuscarClicked(object sender, EventArgs e) {
            // Verificar que hay conexión a internet
            if (!NetworkUtils.TieneConexionInternet()) {
                await DisplayAlert("Sin conexión", "Necesitas conexión a Internet para hacer una busqueda.", "OK");
                return;
            }

            // Guardar el texto actual de búsqueda (trim para evitar espacios)
            _currentQuery = BuscarEntry.Text?.Trim() ?? string.Empty;

            // Limpiar el cuadro de búsqueda para interfaz
            BuscarEntry.Text = string.Empty;

            // Ocultar sugerencias
            SugerenciasList.IsVisible = false;

            // Ocultar mensaje de sin resultados
            SinResultadosLabel.IsVisible = false;

            // Solo buscar si el texto no está vacío
            if (!string.IsNullOrWhiteSpace(_currentQuery)) {
                // Mostrar indicador de carga principal
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                // Refrescar datos con la búsqueda actual
                await RefreshDataAsync();

                // Ocultar indicador de carga
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        /// <summary>
        /// Carga más elementos en la lista según paginación y consulta actual.
        /// Controla estados para no hacer llamadas simultáneas.
        /// </summary>
        private async Task LoadMoreItems() {
            // Si ya está cargando, no hay más items o consulta vacía, salir
            if (_isLoading || !_hasMoreItems || string.IsNullOrWhiteSpace(_currentQuery))
                return;

            // Marcar que está cargando datos
            _isLoading = true;

            // Mostrar indicador de carga para paginación en UI (hilo UI)
            Device.BeginInvokeOnMainThread(() => LoadingMoreIndicator.IsVisible = true);

            try {
                // Obtener juegos nuevos desde el servicio con paginación
                var nuevosJuegos = await _explorarService.BuscarJuegosAsync(
                    _currentQuery,
                    _currentPage * PageSize,  // Offset calculado para la página actual
                    PageSize);

                // Si se obtienen juegos
                if (nuevosJuegos.Any()) {
                    Device.BeginInvokeOnMainThread(() => {
                        // Añadir juegos a la colección si no están ya (evita duplicados)
                        foreach (var juego in nuevosJuegos) {
                            if (!_juegos.Any(j => j.IdJuego == juego.IdJuego)) {
                                _juegos.Add(juego);
                            }
                        }
                    });
                    // Incrementar número de página solo si se obtuvieron resultados
                    _currentPage++;
                }
                else {
                    // No hay más items para cargar
                    _hasMoreItems = false;

                    // Mostrar "sin resultados" solo si es la primera página
                    if (_currentPage == 0) {
                        Device.BeginInvokeOnMainThread(() => {
                            SinResultadosLabel.IsVisible = true;
                        });
                    }
                }
            }
            catch (Exception ex) {
                // En caso de error, mostrar en debug y marcar que no hay más items
                Debug.WriteLine($"Error en LoadMoreItems: {ex.Message}");
                _hasMoreItems = false;
            }
            finally {
                // Ya no está cargando, ocultar indicador en UI
                _isLoading = false;
                Device.BeginInvokeOnMainThread(() => {
                    LoadingMoreIndicator.IsVisible = false;
                });
            }
        }

        /// <summary>
        /// Evento que se dispara cuando la lista está cerca del final (umbral).
        /// Llama a LoadMoreItems para cargar más juegos.
        /// </summary>
        private async void ResultadosList_RemainingItemsThresholdReached(object sender, EventArgs e) {
            await LoadMoreItems();
        }

        /// <summary>
        /// Evento llamado cuando cambia el texto del buscador.
        /// Muestra sugerencias si hay texto suficiente.
        /// </summary>
        private async void OnBuscarTextChanged(object sender, TextChangedEventArgs e) {
            // Texto actual limpio de espacios
            string texto = e.NewTextValue?.Trim() ?? string.Empty;

            // Si texto coincide con búsqueda actual, no hacer nada
            if (texto == _currentQuery)
                return;

            // Si texto vacío, ocultar sugerencias
            if (string.IsNullOrWhiteSpace(texto)) {
                SugerenciasList.IsVisible = false;
                return;
            }

            // Mostrar sugerencias solo si texto >= 2 caracteres
            if (texto.Length >= 2) {
                // Obtener sugerencias desde servicio
                var juegos = await _explorarService.BuscarSugerenciasAsync(texto);

                // Actualizar UI con sugerencias en hilo principal
                Device.BeginInvokeOnMainThread(() => {
                    SugerenciasList.ItemsSource = juegos;
                    SugerenciasList.IsVisible = juegos.Any();
                });
            }
            else {
                // Texto demasiado corto, ocultar sugerencias
                SugerenciasList.IsVisible = false;
            }
        }

        /// <summary>
        /// Evento al seleccionar una sugerencia de la lista.
        /// Realiza la búsqueda con el título seleccionado.
        /// </summary>
        private async void SugerenciasList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // Comprobar conexión a internet antes de buscar
            if (!NetworkUtils.TieneConexionInternet()) {
                await DisplayAlert("Sin conexión", "Necesitas conexión a Internet para hacer una busqueda.", "OK");
                return;
            }

            // Obtener el juego seleccionado (primer elemento seleccionado)
            if (e.CurrentSelection.FirstOrDefault() is Juego juego) {
                // Ocultar sugerencias inmediatamente
                SugerenciasList.IsVisible = false;

                // Limpiar cuadro de búsqueda y poner la consulta igual al título del juego seleccionado
                BuscarEntry.Text = string.Empty;
                _currentQuery = juego.Titulo;

                // Mostrar indicador de carga principal
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                // Limpiar resultados anteriores y resetear paginación
                _juegos.Clear();
                _currentPage = 0;
                _hasMoreItems = true;

                // Cargar resultados para el juego seleccionado
                await LoadMoreItems();

                // Ocultar indicador de carga
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        /// <summary>
        /// Evento al pulsar el botón de "Agregar" un juego a la colección.
        /// Verifica si el juego ya existe, y si no, lo añade a la base local.
        /// </summary>
        private async void OnAgregarClicked(object sender, EventArgs e) {
            // Evitar navegación simultánea que cause errores
            if (_isNavigating)
                return;

            // Validar que el sender es botón y contiene el juego en CommandParameter
            if (sender is Button button && button.CommandParameter is Juego juego) {
                try {
                    // Bloquear navegación y desactivar botón para evitar doble clic
                    _isNavigating = true;
                    button.IsEnabled = false;

                    // Obtener ID del usuario actual
                    var idUsuario = Preferences.Get("UsuarioId", "0");

                    // Verificar si el juego ya existe en la base de datos para el usuario
                    bool yaExiste = await _dbService.JuegoExisteAsync(juego.IdJuego, idUsuario);

                    if (yaExiste) {
                        // Si ya existe, verificar si está en la colección
                        bool enColeccion = await _dbService.JuegoEnColeccionAsync(juego.IdJuego, idUsuario);

                        if (enColeccion) {
                            // Avisar que ya está en colección
                            await DisplayAlert("Atención", "Este juego ya está en tu colección.", "OK");
                            return;
                        }
                        else {
                            // Añadir el juego existente a la colección
                            await _dbService.AnnadirJuegoExistenteAColeccion(juego.IdJuego, idUsuario);
                            await DisplayAlert("Éxito", $"{juego.Titulo} añadido a tu colección", "OK");
                            return;
                        }
                    }

                    // Si no existe, obtener detalles completos del juego desde servicio remoto
                    var juegoCompleto = await _explorarService.ObtenerDetallesJuegoAsync(juego.IdJuego);
                    if (juegoCompleto != null) {
                        // Asignar el usuario actual al juego antes de guardarlo
                        juegoCompleto.IdUsuario = idUsuario;

                        // Guardar juego completo en base local
                        await _dbService.SaveJuegoAsync(juegoCompleto);

                        // Confirmar al usuario que se agregó el juego
                        await DisplayAlert("Éxito", $"{juegoCompleto.Titulo} añadido a tu colección", "OK");
                    }
                }
                catch (Exception ex) {
                    // En caso de error, mostrar en debug y alerta
                    Debug.WriteLine($"Error en OnAgregarClicked: {ex.Message}");
                    await DisplayAlert("Error", "No se pudo agregar el juego. Inténtalo de nuevo.", "OK");
                }
                finally {
                    // Liberar navegación y habilitar botón
                    _isNavigating = false;
                    button.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Evento que se dispara al pulsar el botón para ver los detalles de un juego.
        /// Gestiona la navegación hacia la página de detalle del juego seleccionado.
        /// </summary>
        /// <param name="sender">El botón que disparó el evento.</param>
        /// <param name="e">Argumentos del evento.</param>
        private async void OnVerDetallesClicked(object sender, EventArgs e) {
            // Evitar múltiples navegaciones simultáneas
            if (_isNavigating)
                return;

            // Comprobar conexión a Internet antes de mostrar detalles online
            if (!NetworkUtils.TieneConexionInternet()) {
                await DisplayAlert(
                    "Sin conexión",
                    "Necesitas conexión a Internet para acceder a los detalles.",
                    "OK");
                return;
            }

            if (sender is Button button && button.CommandParameter is Juego juego) {
                try {
                    _isNavigating = true;
                    button.IsEnabled = false;

                    // Obtener el ID del usuario actual
                    var idUsuario = Preferences.Get("UsuarioId", "0");

                    // Comprobar si el juego ya está guardado localmente
                    if (await _dbService.JuegoExisteAsync(juego.IdJuego, idUsuario)) {
                        // Obtener juego local y navegar a detalle
                        Juego juegoExistente = await _dbService.GetJuegoByIdAsync(juego.IdJuego);
                        await Navigation.PushAsync(new DetalleJuegoPage(juegoExistente));
                    }
                    else {
                        // Si no está local, obtener detalles desde el servicio remoto
                        var detalles = await _explorarService.ObtenerDetallesJuegoAsync(juego.IdJuego);
                        if (detalles != null) {
                            await Navigation.PushAsync(new DetalleJuegoPage(detalles));
                        }
                    }
                }
                catch (Exception ex) {
                    Debug.WriteLine($"Error en OnVerDetallesClicked: {ex.Message}");
                    await DisplayAlert("Error", "No se pudo cargar los detalles", "OK");
                }
                finally {
                    _isNavigating = false;
                    button.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Evento que se ejecuta cuando la página Explorar desaparece de la vista.
        /// Se usa para eliminar el handler del evento de carga de más ítems y evitar memory leaks.
        /// </summary>
        protected override void OnDisappearing() {
            base.OnDisappearing();
            // Desuscribirse del evento para evitar fugas de memoria
            ResultadosList.RemainingItemsThresholdReached -= ResultadosList_RemainingItemsThresholdReached;
        }

    }
}
