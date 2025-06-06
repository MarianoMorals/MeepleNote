using System.Timers;

namespace MeepleNote.Views {
    /// <summary>
    /// Página para simular el lanzamiento de dados (d4, d6, d8, d10, d12, d20).
    /// Incluye temporizador para resetear la visualización.
    /// </summary>
    public partial class DadosPage : ContentPage {
        private readonly Random _random = new();  // Generador de números aleatorios
        private readonly System.Timers.Timer _timer;  // Temporizador para resetear dados
        private bool _isRolling = false;  // Bandera para evitar lanzamientos simultáneos

        public DadosPage() {
            InitializeComponent();  // Carga los componentes XAML

            // Configuración del temporizador:
            // - Intervalo: 2 segundos
            // - AutoReset: False (solo se ejecuta una vez)
            _timer = new System.Timers.Timer(2000) { AutoReset = false };

            // Suscribe el evento que se dispara cuando termina el tiempo
            _timer.Elapsed += (s, e) => ResetDiceViews();
        }

        /// <summary>
        /// Simula el lanzamiento de un dado con animaciones.
        /// </summary>
        /// <param name="diceImage">Imagen del dado a animar</param>
        /// <param name="resultLabel">Label donde mostrar el resultado</param>
        /// <param name="sides">Número de caras del dado (4, 6, 8, etc.)</param>
        private async void RollDice(Image diceImage, Label resultLabel, int sides) {
            // Evita lanzamientos mientras hay otra animación en curso
            if (_isRolling) return;

            try {
                _isRolling = true;  // Activa el bloqueo
                _timer.Stop();  // Detiene cualquier temporizador activo

                // ----- ANIMACIÓN DE LANZAMIENTO -----
                // 1. Efecto de "comprimir" la imagen, como si se aleja.
                await diceImage.ScaleTo(0.8, 150, Easing.SpringOut);

                // ----- MOSTRAR RESULTADO -----
                // Oculta la imagen y muestra el número
                diceImage.IsVisible = false;
                resultLabel.IsVisible = true;

                // Genera un resultado aleatorio (1-N caras)
                resultLabel.Text = _random.Next(1, sides + 1).ToString();

                // 2. Efecto de "rebote" al terminar
                await diceImage.ScaleTo(1, 150, Easing.SpringIn);

                // Reinicia el temporizador para ocultar resultados
                _timer.Start();
            }
            finally {
                _isRolling = false;  // Libera el bloqueo
            }
        }

        /// <summary>
        /// Restablece todas las vistas de dados a su estado inicial.
        /// Se ejecuta automáticamente 2 segundos después del último lanzamiento.
        /// </summary>
        private void ResetDiceViews() {
            // Debe ejecutarse en el hilo principal (UI Thread)
            MainThread.BeginInvokeOnMainThread(() => {
                // Muestra todas las imágenes de dados
                d4Image.IsVisible = true;
                d6Image.IsVisible = true;
                d8Image.IsVisible = true;
                d10Image.IsVisible = true;
                d12Image.IsVisible = true;
                d20Image.IsVisible = true;

                // Oculta todos los resultados
                d4Result.IsVisible = false;
                d6Result.IsVisible = false;
                d8Result.IsVisible = false;
                d10Result.IsVisible = false;
                d12Result.IsVisible = false;
                d20Result.IsVisible = false;
            });
        }

        // Métodos que responden a los taps en cada dado:
        private void OnD4Tapped(object sender, EventArgs e) => RollDice(d4Image, d4Result, 4);
        private void OnD6Tapped(object sender, EventArgs e) => RollDice(d6Image, d6Result, 6);
        private void OnD8Tapped(object sender, EventArgs e) => RollDice(d8Image, d8Result, 8);
        private void OnD10Tapped(object sender, EventArgs e) => RollDice(d10Image, d10Result, 10);
        private void OnD12Tapped(object sender, EventArgs e) => RollDice(d12Image, d12Result, 12);
        private void OnD20Tapped(object sender, EventArgs e) => RollDice(d20Image, d20Result, 20);
    }
}