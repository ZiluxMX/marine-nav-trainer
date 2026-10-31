using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace marine_nav_trainer.Map.UI.Controls {
    internal class PositionLeader : Canvas {
        private const double FontSize = 30;
        private const double StemLength = 45;
        private const double LineThickness = 3;
        private const double TextGap = 3;
        private const double BarPadding = 12;
        private const double MinBarLength = 70;
        private readonly double _startGap;
        private readonly TextBlock _timeText;
        private readonly TextBlock _logText;
        private readonly Line _beam;
        private readonly Line _bar;
        private Brush _color = Brushes.Blue;

        public PositionLeader(double markSize) {
            _startGap = markSize / 2 + 2;

            _beam = new Line { StrokeThickness = LineThickness };
            _bar = new Line { StrokeThickness = LineThickness };

            _timeText = CreateText();
            _logText = CreateText();

            Children.Add(_beam);
            Children.Add(_bar);
            Children.Add(_timeText);
            Children.Add(_logText);

            ApplyColor();
            Update(null, null);
        }

        public Brush Color {
            get => _color;
            set {
                _color = value;
                ApplyColor();
            }
        }

        public void Update(string? time, double? log) {
            _timeText.Text = FormatTime(time);
            _logText.Text = FormatLog(log);

            bool hasContent = _timeText.Text.Length > 0 || _logText.Text.Length > 0;
            Visibility = hasContent ? Visibility.Visible : Visibility.Collapsed;
            if (!hasContent)
                return;

            // ukośna kreska w prawo górę od pozycji
            double diagonal = Math.Sqrt(0.5);
            double startX = _startGap * diagonal;
            double startY = -_startGap * diagonal;
            double endX = (_startGap + StemLength) * diagonal;
            double endY = -(_startGap + StemLength) * diagonal;

            _beam.X1 = startX;
            _beam.Y1 = startY;
            _beam.X2 = endX;
            _beam.Y2 = endY;

            _timeText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            _logText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            double textWidth = Math.Max(_timeText.DesiredSize.Width, _logText.DesiredSize.Width);
            double barLength = Math.Max(MinBarLength, textWidth + BarPadding);

            _bar.X1 = endX;
            _bar.Y1 = endY;
            _bar.X2 = endX + barLength;
            _bar.Y2 = endY;

            PlaceText(_timeText, endX, endY, barLength, isAboveBar: true);
            PlaceText(_logText, endX, endY, barLength, isAboveBar: false);
        }

        private static void PlaceText(TextBlock text, double barX, double barY, double barLength, bool isAboveBar) {
            SetLeft(text, barX + (barLength - text.DesiredSize.Width) / 2);
            SetTop(text, isAboveBar ? barY - text.DesiredSize.Height - TextGap : barY + TextGap);
        }

        private static TextBlock CreateText() {
            return new TextBlock {
                FontSize = FontSize,
                FontWeight = FontWeights.SemiBold,
                IsHitTestVisible = false
            };
        }

        private static string FormatTime(string? time) {
            if (string.IsNullOrEmpty(time))
                return string.Empty;

            return time.PadLeft(4, '0');
        }

        private static string FormatLog(double? log) {
            if (log == null)
                return string.Empty;

            return log.Value.ToString("0.#", CultureInfo.CurrentCulture);
        }

        private void ApplyColor() {
            _beam.Stroke = _color;
            _bar.Stroke = _color;
            _timeText.Foreground = _color;
            _logText.Foreground = _color;
        }
    }
}
