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

        private const double ApparentAngleLineDistanceRatio = 0.98;
        private const double RelativeAngleLineDistanceRatio = 1.02;
        private const double AngleLineThickness = 3;
        private const double AngleLineOutlineThickness = 1;
        private Brush AngleLineOutlineBrush = Brushes.Black;

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
        private Brush ApparentWindDirectionLineBrush = Brushes.Violet;
        private Brush ApparentWindLineBrush = Brushes.Gold;
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

            // Kąty 0-180 do kursu
            double twa = ToHalfAngle(result.RelativeWindDirection);
            double awa = ToHalfAngle(result.ApparentWindAngle);

            RelativeWindDirection.Value = twa;
            RelativeWindDirectionDegMin.Text = CoordinateFormatter.ToDegreesMinutes(twa);

            ApparentWindDirection.Value = result.ApparentWindDirection;
            ApparentWindDirectionDegMin.Text = CoordinateFormatter.ToDegreesMinutes(result.ApparentWindDirection);

            ApparentWindSpeed.Value = Math.Round(result.ApparentWindSpeed, 2);

            ApparentWindAngle.Value = awa;
            ApparentWindAngleDegMin.Text = CoordinateFormatter.ToDegreesMinutes(awa);

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
            DrawBearingLine(centerX, centerY, _boatHeadingDeg, BoatHeadingLineLength, BoatHeadingLineBrush, false);
            DrawBearingLine(centerX, centerY, _windDirectionDeg, WindDirectionLineLength, WindDirectionLineBrush);
            // Różowa linia wiatru pozornego idzie z tej samej strony co różowy łuk kąta (obrót o 180)
            DrawBearingLine(centerX, centerY, (_apparentWindDeg + 180) % 360, ApparentWindLineLength, ApparentWindDirectionLineBrush);

            DrawAngleLine(centerX, centerY, (double)_boatHeadingDeg, (double)(_apparentWindDeg + 180) % 360, PointDistance * ApparentAngleLineDistanceRatio, ApparentWindLineBrush);
            DrawAngleLine(centerX, centerY, (double)_boatHeadingDeg, (double)_relativeWindDeg, PointDistance * RelativeAngleLineDistanceRatio, RelativeWindLineBrush);
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

        private void DrawAngleLine(double centerX, double centerY, double boatHeadingDeg, double windAngleDeg, double radius, Brush brush) {
            double angleDeg = (windAngleDeg - boatHeadingDeg + 360.0) % 360.0;
            if (angleDeg < 0.1)
                return;

            var direction = SweepDirection.Clockwise;
            if (angleDeg > 180) {
                angleDeg = 360 - angleDeg;
                direction = SweepDirection.Counterclockwise;
            }

            double endBearing = direction == SweepDirection.Clockwise ? boatHeadingDeg + angleDeg : boatHeadingDeg - angleDeg;
            Point start = PointOnCircle(centerX, centerY, radius, boatHeadingDeg);
            Point end = PointOnCircle(centerX, centerY, radius, endBearing);

            var figure = new PathFigure { StartPoint = start, IsClosed = false };
            figure.Segments.Add(new ArcSegment {
                Point = end,
                Size = new Size(radius, radius),
                IsLargeArc = false,
                SweepDirection = direction
            });

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            CompassOverlay.Children.Add(new Path {
                Data = geometry,
                Stroke = AngleLineOutlineBrush,
                StrokeThickness = AngleLineThickness + 2 * AngleLineOutlineThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            });

            CompassOverlay.Children.Add(new Path {
                Data = geometry,
                Stroke = brush,
                StrokeThickness = AngleLineThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            });
        }

        private Point PointOnCircle(double centerX, double centerY, double radius, double bearingDeg) {
            double rad = bearingDeg * Math.PI / 180.0;
            return new Point(centerX + radius * Math.Sin(rad), centerY - radius * Math.Cos(rad));
        }

        private double ToHalfAngle(double angle) {
            angle = (angle % 360 + 360) % 360; 
            return angle <= 180 ? angle : 360 - angle;
        }
    }
}
