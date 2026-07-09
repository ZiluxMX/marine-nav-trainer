using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace marine_nav_trainer.Map.UI.Controls {
    internal class PositionMark : Canvas {
        private readonly Ellipse _dot;
        private readonly Ellipse _ring;
        private readonly Line _lineLeft;
        private readonly Line _lineRight;
        private readonly Line _lineTop;
        private readonly Line _lineBottom;
        private Brush _color = Brushes.Blue;

        public PositionMark(double size) {
            double center = size / 2;
            double ringRadius = size / 2;
            double ringThickness = 5;
            double lineThickness = 6;
            double lineGap = -1;
            double lineLength = 6;
            double dotSize = 5;

            _ring = new Ellipse {
                Width = size,
                Height = size,
                StrokeThickness = ringThickness,
                Fill = Brushes.Transparent
            };

            _lineLeft = new Line {
                X1 = center - ringRadius - lineGap - lineLength,
                Y1 = center,
                X2 = center - ringRadius - lineGap,
                Y2 = center,
                StrokeThickness = lineThickness
            };

            _lineRight = new Line {
                X1 = center + ringRadius + lineGap,
                Y1 = center,
                X2 = center + ringRadius + lineGap + lineLength,
                Y2 = center,
                StrokeThickness = lineThickness
            };

            _lineTop = new Line {
                X1 = center,
                Y1 = center - ringRadius - lineGap - lineLength,
                X2 = center,
                Y2 = center - ringRadius - lineGap,
                StrokeThickness = lineThickness
            };

            _lineBottom = new Line {
                X1 = center,
                Y1 = center + ringRadius + lineGap,
                X2 = center,
                Y2 = center + ringRadius + lineGap + lineLength,
                StrokeThickness = lineThickness
            };

            _dot = new Ellipse {
                Width = dotSize,
                Height = dotSize
            };

            Canvas.SetLeft(_dot, center - dotSize / 2);
            Canvas.SetTop(_dot, center - dotSize / 2);

            Children.Add(_ring);
            Children.Add(_lineLeft);
            Children.Add(_lineRight);
            Children.Add(_lineTop);
            Children.Add(_lineBottom);
            Children.Add(_dot);

            ApplyColor();
        }

        public Brush Color {
            get => _color;
            set {
                _color = value;
                ApplyColor();
            }
        }

        private void ApplyColor() {
            _ring.Stroke = _color;

            _lineLeft.Stroke = _color;
            _lineRight.Stroke = _color;
            _lineTop.Stroke = _color;
            _lineBottom.Stroke = _color;

            _dot.Fill = _color;
        }
    }
}
