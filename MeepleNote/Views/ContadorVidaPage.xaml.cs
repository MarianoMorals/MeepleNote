using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MeepleNote.Views 
{
    /// <summary>
    /// Página para llevar el conteo de vida/energía de jugadores.
    /// Permite configurar número de jugadores y vida inicial, con interfaz dinámica.
    /// </summary>
    public partial class ContadorVidaPage : ContentPage 
    {
        private int _numPlayers = 2;          // Número de jugadores (default: 2)
        private int _initialLife = 20;         // Vida inicial por jugador (default: 20)
        private readonly List<Label> _lifeLabels = new();  // Referencias a los Labels de vida

        // Paleta de colores para identificar jugadores visualmente
        private readonly List<Color> _playerColors = new() 
        {
            Colors.Red,    Colors.Blue,   Colors.Green,  Colors.Yellow,
            Colors.Purple, Colors.Orange, Colors.Cyan,   Colors.Magenta
        };

        public ContadorVidaPage() 
        {
            InitializeComponent(); 
            Loaded += OnPageLoaded;
        }

        /// <summary>
        /// Evento que se dispara cuando la página termina de cargar.
        /// Inicia el flujo de configuración inicial.
        /// </summary>
        private async void OnPageLoaded(object sender, EventArgs e) 
        {
            await ShowPlayerNumberPrompt();  // Muestra diálogo de configuración
        }

        /// <summary>
        /// Muestra diálogos para configurar número de jugadores y vida inicial.
        /// Valida los inputs y establece valores por defecto si son inválidos.
        /// </summary>
        private async Task ShowPlayerNumberPrompt() 
        {
            try 
            {
                // Diálogo para número de jugadores
                string result = await DisplayPromptAsync(
                    "Configuración",
                    "¿Cuántos jugadores? (2-8)",
                    maxLength: 1,               // Solo 1 dígito
                    keyboard: Keyboard.Numeric,  // Teclado numérico
                    initialValue: "2");          // Valor por defecto

                // Validación del input
                if (!int.TryParse(result, out _numPlayers) || _numPlayers < 2 || _numPlayers > 8) 
                {
                    await DisplayAlert("Error", "Número de jugadores inválido. Usando valor por defecto (2).", "OK");
                    _numPlayers = 2;  //Valor por defecto
                }

                // Diálogo para vida inicial
                string lifeResult = await DisplayPromptAsync(
                    "Configuración",
                    "Vida inicial para cada jugador:",
                    maxLength: 3,                // Hasta 3 dígitos (ej: 999)
                    keyboard: Keyboard.Numeric,
                    initialValue: "20");

                // Validación del input
                if (!int.TryParse(lifeResult, out _initialLife) || _initialLife <= 0) 
                {
                    await DisplayAlert("Error", "Valor de vida inválido. Usando 20 por defecto.", "OK");
                    _initialLife = 20;  // Valor por defecto
                }

                SetupLifeCounters();  // Construye la interfaz
            }
            catch (Exception ex) 
            {
                await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Crea dinámicamente los contadores de vida basado en la configuración.
        /// Diseño adaptable: 1 columna (2-4 jugadores) o 2 columnas (5+ jugadores).
        /// </summary>
        private void SetupLifeCounters() 
        {
            try 
            {
                // Limpia controles anteriores
                mainGrid.Clear();
                _lifeLabels.Clear();
                mainGrid.RowDefinitions.Clear();
                mainGrid.ColumnDefinitions.Clear();

                // Filtra colores con buen contraste visual
                var goodColors = GetContrastingColors();

                // Configura layout según número de jugadores
                if (_numPlayers <= 4) 
                {
                    // Diseño vertical (1 columna)
                    for (int i = 0; i < _numPlayers; i++) 
                    {
                        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
                    }
                }
                else 
                {
                    // Diseño de cuadrícula (2 columnas)
                    for (int i = 0; i < 2; i++) 
                    {
                        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                    }
                    for (int i = 0; i < (int)Math.Ceiling(_numPlayers / 2.0); i++) 
                    {
                        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
                    }
                }

                // Crea un contador por jugador
                for (int i = 0; i < _numPlayers; i++) 
                {
                    int playerIndex = i;  // Captura el índice para los eventos

                    // Label para mostrar la vida
                    var lifeLabel = new Label 
                    {
                        Text = _initialLife.ToString(),
                        FontSize = 40,
                        TextColor = Colors.White,
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center
                    };
                    _lifeLabels.Add(lifeLabel);  // Guarda referencia

                    // Botón para disminuir vida
                    var minusButton = new Button 
                    {
                        Text = "-",
                        FontSize = 30,
                        BackgroundColor = Colors.Transparent,
                        TextColor = Colors.White
                    };
                    minusButton.Clicked += (s, e) => SafeUpdateLife(playerIndex, -1);

                    // Botón para aumentar vida
                    var plusButton = new Button 
                    {
                        Text = "+",
                        FontSize = 30,
                        BackgroundColor = Colors.Transparent,
                        TextColor = Colors.White
                    };
                    plusButton.Clicked += (s, e) => SafeUpdateLife(playerIndex, 1);

                    // Contenedor horizontal para botones
                    var buttonStack = new StackLayout 
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.Center,
                        Spacing = 20,
                        Children = { minusButton, plusButton }
                    };

                    // Contenedor principal (vida + botones)
                    var contentStack = new StackLayout 
                    {
                        Children = { lifeLabel, buttonStack },
                        VerticalOptions = LayoutOptions.Center
                    };

                    // Marco estilizado para cada jugador
                    var frame = new Frame 
                    {
                        Content = contentStack,
                        BackgroundColor = goodColors[i % goodColors.Count].WithAlpha(0.7f),  // Color semitransparente
                        CornerRadius = 10,
                        Padding = 15,
                        Margin = 5
                    };

                    // Posicionamiento en el grid
                    if (_numPlayers <= 4) 
                    {
                        mainGrid.Add(frame, 0, i);  // Columna 0, fila i
                    }
                    else 
                    {
                        int row = i / 2;  // Distribuye en 2 columnas
                        int col = i % 2;
                        mainGrid.Add(frame, col, row);
                    }
                }
            }
            catch (Exception ex) 
            {
                DisplayAlert("Error", $"Error al configurar contadores: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Actualiza el valor de vida de un jugador de forma segura.
        /// </summary>
        /// <param name="playerIndex">Índice del jugador (0-based)</param>
        /// <param name="change">Cantidad a modificar (+1 o -1)</param>
        private void SafeUpdateLife(int playerIndex, int change) 
        {
            try 
            {
                if (playerIndex >= 0 && playerIndex < _lifeLabels.Count) 
                {
                    if (int.TryParse(_lifeLabels[playerIndex].Text, out int currentLife)) 
                    {
                        currentLife += change;
                        _lifeLabels[playerIndex].Text = currentLife.ToString();
                    }
                }
            }
            catch (Exception ex) 
            {
                DisplayAlert("Error", $"Error al actualizar vida: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Filtra la paleta de colores para mantener solo aquellos con buen contraste.
        /// </summary>
        private List<Color> GetContrastingColors() =>
            _playerColors.Where(c => HasGoodContrastWithWhite(c)).ToList();

        /// <summary>
        /// Calcula si un color tiene suficiente contraste con texto blanco.
        /// Usa fórmula estándar de luminancia.
        /// </summary>
        private bool HasGoodContrastWithWhite(Color color) 
        {
            // Fórmula de luminancia relativa (ITU-R BT.709)
            double luminance = 0.2126 * color.Red + 0.7152 * color.Green + 0.0722 * color.Blue;
            return luminance < 0.8;  // Umbral empírico
        }
    }
}