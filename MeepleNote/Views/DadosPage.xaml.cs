using System.Timers;

namespace MeepleNote.Views {
    public partial class DadosPage : ContentPage {
        private readonly Random _random = new();
        private readonly System.Timers.Timer _timer;
        private bool _isRolling = false;

        public DadosPage() {
            InitializeComponent();
            _timer = new System.Timers.Timer(2000) { AutoReset = false };
            _timer.Elapsed += (s, e) => ResetDiceViews();
        }

        private async void RollDice(Image diceImage, Label resultLabel, int sides) {
            if (_isRolling) return;

            _isRolling = true;
            _timer.Stop();

            // Animación de lanzamiento
            await diceImage.ScaleTo(0.8, 150, Easing.SpringOut);

            // Mostrar resultado
            diceImage.IsVisible = false;
            resultLabel.IsVisible = true;
            resultLabel.Text = _random.Next(1, sides + 1).ToString();

            await diceImage.ScaleTo(1, 150, Easing.SpringIn);
            _timer.Start();
            _isRolling = false;
        }

        private void ResetDiceViews() {
            MainThread.BeginInvokeOnMainThread(() => {
                d4Image.IsVisible = true;
                d6Image.IsVisible = true;
                d8Image.IsVisible = true;
                d10Image.IsVisible = true;
                d12Image.IsVisible = true;
                d20Image.IsVisible = true;

                d4Result.IsVisible = false;
                d6Result.IsVisible = false;
                d8Result.IsVisible = false;
                d10Result.IsVisible = false;
                d12Result.IsVisible = false;
                d20Result.IsVisible = false;
            });
        }

        private void OnD4Tapped(object sender, EventArgs e) => RollDice(d4Image, d4Result, 4);
        private void OnD6Tapped(object sender, EventArgs e) => RollDice(d6Image, d6Result, 6);
        private void OnD8Tapped(object sender, EventArgs e) => RollDice(d8Image, d8Result, 8);
        private void OnD10Tapped(object sender, EventArgs e) => RollDice(d10Image, d10Result, 10);
        private void OnD12Tapped(object sender, EventArgs e) => RollDice(d12Image, d12Result, 12);
        private void OnD20Tapped(object sender, EventArgs e) => RollDice(d20Image, d20Result, 20);
    }
}