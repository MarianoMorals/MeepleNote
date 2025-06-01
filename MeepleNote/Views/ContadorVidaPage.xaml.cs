using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MeepleNote.Views {
    public partial class ContadorVidaPage : ContentPage {
        private int _numPlayers = 2;
        private int _initialLife = 20;
        private readonly List<Label> _lifeLabels = new();

        // Paleta de colores original
        private readonly List<Color> _playerColors = new() {
            Colors.Red, Colors.Blue, Colors.Green, Colors.Yellow,
            Colors.Purple, Colors.Orange, Colors.Cyan, Colors.Magenta
        };

        public ContadorVidaPage() {
            InitializeComponent();
            Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, EventArgs e) {
            await ShowPlayerNumberPrompt();
        }

        private async Task ShowPlayerNumberPrompt() {
            try {
                string result = await DisplayPromptAsync(
                    "Configuración",
                    "¿Cuántos jugadores? (2-8)",
                    maxLength: 1,
                    keyboard: Keyboard.Numeric,
                    initialValue: "2");

                if (!int.TryParse(result, out _numPlayers) || _numPlayers < 2 || _numPlayers > 8) {
                    await DisplayAlert("Error", "Número de jugadores inválido. Usando valor por defecto (2).", "OK");
                    _numPlayers = 2;
                }

                string lifeResult = await DisplayPromptAsync(
                    "Configuración",
                    "Vida inicial para cada jugador:",
                    maxLength: 3,
                    keyboard: Keyboard.Numeric,
                    initialValue: "20");

                if (!int.TryParse(lifeResult, out _initialLife) || _initialLife <= 0) {
                    await DisplayAlert("Error", "Valor de vida inválido. Usando 20 por defecto.", "OK");
                    _initialLife = 20;
                }

                SetupLifeCounters();
            }
            catch (Exception ex) {
                await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
            }
        }

        private void SetupLifeCounters() {
            try {
                mainGrid.Clear();
                _lifeLabels.Clear();
                mainGrid.RowDefinitions.Clear();
                mainGrid.ColumnDefinitions.Clear();

                // Obtener solo colores con buen contraste
                var goodColors = GetContrastingColors();

                if (_numPlayers <= 4) {
                    for (int i = 0; i < _numPlayers; i++) {
                        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
                    }
                }
                else {
                    for (int i = 0; i < 2; i++) {
                        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                    }
                    for (int i = 0; i < (int)Math.Ceiling(_numPlayers / 2.0); i++) {
                        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
                    }
                }

                for (int i = 0; i < _numPlayers; i++) {
                    int playerIndex = i;

                    var lifeLabel = new Label {
                        Text = _initialLife.ToString(),
                        FontSize = 40,
                        TextColor = Colors.White,
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center
                    };
                    _lifeLabels.Add(lifeLabel);

                    var minusButton = new Button {
                        Text = "-",
                        FontSize = 30,
                        BackgroundColor = Colors.Transparent,
                        TextColor = Colors.White
                    };
                    minusButton.Clicked += (s, e) => SafeUpdateLife(playerIndex, -1);

                    var plusButton = new Button {
                        Text = "+",
                        FontSize = 30,
                        BackgroundColor = Colors.Transparent,
                        TextColor = Colors.White
                    };
                    plusButton.Clicked += (s, e) => SafeUpdateLife(playerIndex, 1);

                    var buttonStack = new StackLayout {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.Center,
                        Spacing = 20,
                        Children = { minusButton, plusButton }
                    };

                    var contentStack = new StackLayout {
                        Children = { lifeLabel, buttonStack },
                        VerticalOptions = LayoutOptions.Center
                    };

                    var frame = new Frame {
                        Content = contentStack,
                        BackgroundColor = goodColors[i % goodColors.Count].WithAlpha(0.7f),
                        CornerRadius = 10,
                        Padding = 15,
                        Margin = 5
                    };

                    if (_numPlayers <= 4) {
                        mainGrid.Add(frame, 0, i);
                    }
                    else {
                        int row = i / 2;
                        int col = i % 2;
                        mainGrid.Add(frame, col, row);
                    }
                }
            }
            catch (Exception ex) {
                DisplayAlert("Error", $"Error al configurar contadores: {ex.Message}", "OK");
            }
        }

        private void SafeUpdateLife(int playerIndex, int change) {
            try {
                if (playerIndex >= 0 && playerIndex < _lifeLabels.Count) {
                    if (int.TryParse(_lifeLabels[playerIndex].Text, out int currentLife)) {
                        currentLife += change;
                        _lifeLabels[playerIndex].Text = currentLife.ToString();
                    }
                }
            }
            catch (Exception ex) {
                DisplayAlert("Error", $"Error al actualizar vida: {ex.Message}", "OK");
            }
        }

        // Devuelve solo los colores con buen contraste respecto al blanco
        private List<Color> GetContrastingColors() =>
            _playerColors.Where(c => HasGoodContrastWithWhite(c)).ToList();

        // Determina si un color tiene buen contraste con el blanco (luminancia baja)
        private bool HasGoodContrastWithWhite(Color color) {
            double luminance = 0.2126 * color.Red + 0.7152 * color.Green + 0.0722 * color.Blue;
            return luminance < 0.8;
        }
    }
}
