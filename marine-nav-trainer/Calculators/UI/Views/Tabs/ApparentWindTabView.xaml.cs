using marine_nav_trainer.Calculators.Core;
using marine_nav_trainer.Calculators.Core.Abstractions;
using marine_nav_trainer.Calculators.Core.Factory;
using marine_nav_trainer.Calculators.Modules.ApparentWind;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace marine_nav_trainer.Calculators.UI.Views.Tabs {
    public partial class ApparentWindTabView : UserControl {
        private readonly ICalculator<ApparentWindInput, ApparentWindResult> _apparentWindCalc;
        private const double LineLengthRatio = 0.72;
        private const double PointDistanceRatio = 0.98;
        private const double AnglePointSize = 10;
        private const double AnglePointOutlineThickness = 1;
        private Brush AnglePointOutlineBrush = Brushes.Black;

        private const double ArrowAngleDeg = 30;
        private const double ArrowLength = 10;
        private const double ArrowThickness = 1;
        private Brush ArrowBrush = Brushes.Black;

        private double MaxLineLength;
        private double PointDistance;
        private double BoatHeadingLineLength;
        private double WindDirectionLineLength;
        private double ApparentWindLineLength;
        //private double RelativeWindLineLength;
        private Brush BoatHeadingLineBrush = Brushes.Red;
        private Brush WindDirectionLineBrush = Brushes.Blue;
        private Brush ApparentWindLineBrush = Brushes.Violet;
        private Brush RelativeWindLineBrush = Brushes.Lime;

        private double? _boatHeadingDeg;
        private double? _windDirectionDeg;
        private double? _apparentWindDeg;
        private double? _relativeWindDeg;

        public ApparentWindTabView() {
            InitializeComponent();

            var calculatorFactory = new CalculatorFactory();
            calculatorFactory.Register<ApparentWindInput, ApparentWindResult>(CalculatorType.ApparentWind, new ApparentWindCalculator());
            _apparentWindCalc = calculatorFactory.Get<ApparentWindInput, ApparentWindResult>(CalculatorType.ApparentWind);
        }

        private void UpdateCompassMetrics() {
            double radius = Math.Min(CompassOverlay.ActualWidth, CompassOverlay.ActualHeight) / 2.0;
            if (radius > 0) {
                MaxLineLength = radius * LineLengthRatio;
                PointDistance = radius * PointDistanceRatio;
            }
        }

        private void Oblicz_Click(object sender, RoutedEventArgs e) {
            double dif = 1, apparentDif = 1;
            if (BoatSpeed.Value == null) BoatSpeed.Value = 1;
            if (WindSpeed.Value == null) WindSpeed.Value = 1;
            if (BoatHeading.Value == null) {
                MessageBox.Show("Pole \"Kurs rzeczywisty statku\" nie może być puste!");
                return;
            }
            if (WindDirection.Value == null) {
                MessageBox.Show("Pole \"Kierunek wiatru\" nie może być puste!");
                return;
            }

            var result = _apparentWindCalc.Calculate(new ApparentWindInput {
                BoatSpeed = (double)BoatSpeed.Value,
                BoatHeading = (double)BoatHeading.Value,
                WindSpeed = (double)WindSpeed.Value,
                WindDirection = (double)WindDirection.Value
            });

            UpdateCompassMetrics();
            BoatHeadingLineLength = MaxLineLength;
            WindDirectionLineLength = MaxLineLength;
            ApparentWindLineLength = MaxLineLength;
            //RelativeWindLineLength = MaxLineLength;

            if (WindSpeed.Value != BoatSpeed.Value) {
                apparentDif = ((double)BoatSpeed.Value + (double)WindSpeed.Value) / 2;
                if (WindSpeed.Value > BoatSpeed.Value) {
                    dif = (double)BoatSpeed.Value / (double)WindSpeed.Value;
                    BoatHeadingLineLength = WindDirectionLineLength * dif;
                    apparentDif = apparentDif / (double)WindSpeed.Value;
                    ApparentWindLineLength = WindDirectionLineLength * apparentDif;
                }
                else {
                    dif = (double)WindSpeed.Value / (double)BoatSpeed.Value;
                    WindDirectionLineLength = BoatHeadingLineLength * dif;
                    apparentDif = apparentDif / (double)BoatSpeed.Value;
                    ApparentWindLineLength = BoatHeadingLineLength * apparentDif;
                }
            }

            RelativeWindDirection.Value = result.RelativeWindDirection;
            RelativeWindDirectionDegMin.Text = CoordinateFormatter.ToDegreesMinutes(result.RelativeWindDirection);

            ApparentWindDirection.Value = result.ApparentWindDirection;
            ApparentWindDirectionDegMin.Text = CoordinateFormatter.ToDegreesMinutes(result.ApparentWindDirection);

            _boatHeadingDeg = (double)BoatHeading.Value;
            _windDirectionDeg = (double)WindDirection.Value;
            _apparentWindDeg = (result.ApparentWindDirection + 180) % 360;
            _relativeWindDeg = result.RelativeWindDirection + (double)BoatHeading.Value;

            RedrawCompassLines();
        }

        private void CompassOverlay_SizeChanged(object sender, SizeChangedEventArgs e) => RedrawCompassLines();

        private void RedrawCompassLines() {
            CompassOverlay.Children.Clear();

            double width = CompassOverlay.ActualWidth;
            double height = CompassOverlay.ActualHeight;
            if (width <= 0 || height <= 0)
                return;

            UpdateCompassMetrics();

            double centerX = width / 2;
            double centerY = height / 2;

            //DrawBearingLine(centerX, centerY, _relativeWindDeg, RelativeWindLineLength, RelativeWindLineBrush);
            DrawBearingLine(centerX, centerY, _boatHeadingDeg, BoatHeadingLineLength, BoatHeadingLineBrush);
            DrawBearingLine(centerX, centerY, _windDirectionDeg, WindDirectionLineLength, WindDirectionLineBrush);
            DrawBearingLine(centerX, centerY, _apparentWindDeg, ApparentWindLineLength, ApparentWindLineBrush, false);

            DrawAnglePoint(centerX, centerY, (_apparentWindDeg + 180) % 360, AnglePointSize, ApparentWindLineBrush);
            DrawAnglePoint(centerX, centerY, _relativeWindDeg, AnglePointSize, RelativeWindLineBrush);
        }

        private void DrawBearingLine(double centerX, double centerY, double? bearingDeg, double length, Brush brush, bool isArrowCenter = true) {
            if (bearingDeg is not double bearing)
                return;

            length = Math.Clamp(length, 0, MaxLineLength);
            if (length <= 0)
                return;
            double rad = bearing * Math.PI / 180.0;

            // Wektor jednostkowy kreski, zwrócony od środka na zewnątrz (Y ekranu rośnie w dół).
            double unitX = Math.Sin(rad);
            double unitY = -Math.Cos(rad);

            double endX = centerX + length * unitX;
            double endY = centerY + length * unitY;

            CompassOverlay.Children.Add(new Line {
                X1 = centerX,
                Y1 = centerY,
                X2 = endX,
                Y2 = endY,
                Stroke = brush,
                StrokeThickness = 3,
                StrokeEndLineCap = PenLineCap.Round
            });
            DrawArrowHead(centerX + length / 2 * unitX, centerY + length / 2 * unitY, unitX, unitY, isArrowCenter);
        }

        private void DrawArrowHead(double centerX, double centerY, double endX, double endY, bool isArrowCenter) {
            double tipX = isArrowCenter ? -endX : endX;
            double tipY = isArrowCenter ? -endY : endY;

            // Ramiona
            double backX = -tipX;
            double backY = -tipY;
            double angle = ArrowAngleDeg * Math.PI / 180.0;
            double angleCos = Math.Cos(angle);
            double angleSin = Math.Sin(angle);

            // Obrót wektora
            double leftX = backX * angleCos - backY * angleSin;
            double leftY = backX * angleSin + backY * angleCos;
            double rightX = backX * angleCos + backY * angleSin;
            double rightY = -backX * angleSin + backY * angleCos;

            CompassOverlay.Children.Add(new Line {
                X1 = centerX,
                Y1 = centerY,
                X2 = centerX + leftX * ArrowLength,
                Y2 = centerY + leftY * ArrowLength,
                Stroke = ArrowBrush,
                StrokeThickness = ArrowThickness
            });

            CompassOverlay.Children.Add(new Line {
                X1 = centerX,
                Y1 = centerY,
                X2 = centerX + rightX * ArrowLength,
                Y2 = centerY + rightY * ArrowLength,
                Stroke = ArrowBrush,
                StrokeThickness = ArrowThickness
            });
        }

        private void DrawAnglePoint(double centerX, double centerY, double? angleDeg, double size, Brush brush) {
            if (angleDeg is not double angle)
                return;

            double length = PointDistance;
            if (!(length > 0) || !(size > 0))   // odrzuca NaN
                return;

            double rad = angle * Math.PI / 180.0;
            double pointX = centerX + length * Math.Sin(rad);
            double pointY = centerY - length * Math.Cos(rad);

            var dot = new Ellipse {
                Width = size,
                Height = size,
                Fill = brush,
                Stroke = AnglePointOutlineBrush,
                StrokeThickness = AnglePointOutlineThickness
            };

            Canvas.SetLeft(dot, pointX - size / 2);
            Canvas.SetTop(dot, pointY - size / 2);

            CompassOverlay.Children.Add(dot);
        }
    }
}
