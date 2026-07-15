using marine_nav_trainer.Map.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Point = System.Windows.Point;

namespace marine_nav_trainer.Map.UI.Controls {
    internal class NavTriangle : Canvas {
        public const double PointSnapRadius = 20;
        public double AngleMain { get; private set; }
        public double AngleRed { get; private set; }
        private readonly double _size;
        private readonly double _center;
        private bool _isDragging = false;
        private bool _isRotating = false;
        private Point _dragStartMouse;
        private Point _dragStartElement;
        private Point _rotationPoint;
        private Line? _handleLine;
        private Ellipse? _rotationHandle;
        private RotateTransform _rotateTransform = new RotateTransform(0);

        public NavTriangle(double size) {
            _size = size;
            _center = size / 2;
            Width = size;
            Height = size;
            AngleMain = 135;
            AngleRed = 315;

            _rotateTransform = new RotateTransform(0);
            this.RenderTransform = _rotateTransform;
            this.RenderTransformOrigin = new Point(0.5, 0.5);

            Panel.SetZIndex(this, int.MaxValue-1); // pod kroczkiem

            DrawTriangle();
            DrawScale();
            DrawHandle();
            DrawRotorHandle();
        }

        private void DrawTriangle() {
            var triangle = new Polygon {
                Points = new PointCollection
                {
                    new Point(0, _size),
                    new Point(_size, _size),
                    new Point(0, 0)
                },
                Fill = new SolidColorBrush(Color.FromArgb(60, 200, 200, 200)),
                Stroke = Brushes.Gray,
                StrokeThickness = 2
            };

            Children.Add(triangle);
        }

        private void DrawScale() {
            double radius = _size * 0.5;

            for (int angle = 0; angle <= 180; angle++) {
                double rad = (angle - 135) * Math.PI / 180;
                bool dirrection = (angle % 45 == 0) && angle > 0 && angle < 180;
                bool major = angle % 10 == 0;
                bool midMajor = angle % 5 == 0;
                double x1, y1, x2, y2, thicknes;

                if (dirrection) {
                    x1 = _center + Math.Sin(rad) * (radius - _size / 2);
                    y1 = _center + Math.Cos(rad) * (radius - _size / 2);
                    thicknes = 2;
                } else if (major) {
                    x1 = _center + Math.Sin(rad) * (radius - (_size / 7));
                    y1 = _center + Math.Cos(rad) * (radius - (_size / 7));
                    thicknes = 2;
                } else if (midMajor) {
                    x1 = _center + Math.Sin(rad) * (radius - (_size / 10));
                    y1 = _center + Math.Cos(rad) * (radius - (_size / 10));
                    thicknes = 1;
                } else {
                    x1 = _center + Math.Sin(rad) * (radius - (_size / 15));
                    y1 = _center + Math.Cos(rad) * (radius - (_size / 15));
                    thicknes = 0.5;
                }
                x2 = _center + Math.Sin(rad) * radius;
                y2 = _center + Math.Cos(rad) * radius;

                var line = new Line {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    Stroke = Brushes.Black,
                    StrokeThickness = thicknes
                };
                Children.Add(line);

                if (major) {
                    int displayAngle = angle <= 180 ? angle : angle + 180;
                    double textAngle = 135 - angle;
                    double modX = 0, modY = 0;

                    //Debug.WriteLine($"angle={angle}; text={textAngle}");
                    var text = new TextBlock {
                        Text = displayAngle.ToString(),
                        FontSize = 30,
                        Foreground = Brushes.Black,
                        RenderTransform = new RotateTransform(textAngle),
                        RenderTransformOrigin = new Point(0.5, 0.5)
                    };

                    double tx = _center + Math.Sin(rad) * (radius - (_size / 7) - 35);
                    double ty = _center + Math.Cos(rad) * (radius - (_size / 7) - 35);

                    if (displayAngle > 90) {
                        modX = -displayAngle / 10;
                        modY = displayAngle / 20;
                        if (displayAngle >= 130) {
                            modY = displayAngle / 15;
                        }
                        if (displayAngle == 170) {
                            modX = -displayAngle / 13;
                            modY = displayAngle / 13;
                        }
                        if (displayAngle >= 180) {
                            modY = displayAngle / 10;
                        }
                    }
                    Canvas.SetLeft(text, tx - 20 + modX);
                    Canvas.SetTop(text, ty - 20 + modY);

                    Children.Add(text);
                    ////////////////////////////// red scale

                    text = new TextBlock {
                        Text = (displayAngle + 180).ToString(),
                        FontSize = 30,
                        Foreground = Brushes.Red,
                        RenderTransform = new RotateTransform(textAngle),
                        RenderTransformOrigin = new Point(0.5, 0.5)
                    };

                    tx = _center + Math.Sin(rad) * (radius - (_size / 7) - 70);
                    ty = _center + Math.Cos(rad) * (radius - (_size / 7) - 70);

                    if (displayAngle <= 110) {
                        modX = -16 + displayAngle / 10;
                        modY = displayAngle / 10;
                        if (displayAngle == 0) {
                            modX = -25;
                            modY = 9;
                        }
                    } else if (displayAngle > 110) {
                        modX = -displayAngle / 10;
                        modY = displayAngle / 15;
                        if (displayAngle >= 130) {
                            modY = displayAngle / 15;
                        }
                        if (displayAngle == 170) {
                            modX = -displayAngle / 13;
                            modY = displayAngle / 13;
                        }
                        if (displayAngle >= 180) {
                            modY = displayAngle / 10;
                        }
                    }
                    Canvas.SetLeft(text, tx - 20 + modX);
                    Canvas.SetTop(text, ty - 20 + modY);

                    Children.Add(text);
                }
            }
        }
        private void DrawHandle() {
            double length = _size / 3;
            double angleRad = 45 * Math.PI / 180;
            double dx = Math.Cos(angleRad) * (length / 2);
            double dy = Math.Sin(angleRad) * (length / 2);

            double centerBaseX = _size / 3.0;
            double centerBaseY = _size * 2.0 / 3.0;
            double t = 0.18;
            double centerX = centerBaseX + (_size / 2.0 - centerBaseX) * t;
            double centerY = centerBaseY + (_size / 2.0 - centerBaseY) * t;

            _handleLine = new Line {
                X1 = centerX - dx,
                Y1 = centerY - dy,
                X2 = centerX + dx,
                Y2 = centerY + dy,
                Stroke = Brushes.Red,
                StrokeThickness = 60,
                Cursor = Cursors.SizeAll
            };
            _handleLine.MouseLeftButtonDown += Handle_MouseLeftButtonDown;
            _handleLine.MouseMove += Handle_MouseMove;
            _handleLine.MouseLeftButtonUp += Handle_MouseLeftButtonUp;

            Children.Add(_handleLine);
        }
        private void DrawRotorHandle() {
            double ringThickness = 20;
            double size = _size / 17;
            _rotationPoint = new Point(_size / 2, _size / 2);

            var rotationPoint = new Ellipse {
                Width = 1,
                Height = 1,
                Fill = Brushes.Lime
            };
            Canvas.SetLeft(rotationPoint, (_size / 2));
            Canvas.SetTop(rotationPoint, (_size / 2));
            Children.Add(rotationPoint);

            _rotationHandle = new Ellipse {
                Width = size,
                Height = size,
                Stroke = Brushes.Red,
                StrokeThickness = ringThickness,
                Fill = Brushes.Transparent,
                Cursor = Cursors.Hand
            };
            Canvas.SetLeft(_rotationHandle, -(size + 20));
            Canvas.SetTop(_rotationHandle, _size + 20);

            _rotationHandle.MouseLeftButtonDown += RotationHandle_MouseLeftButtonDown;
            _rotationHandle.MouseMove += RotationHandle_MouseMove;
            _rotationHandle.MouseLeftButtonUp += RotationHandle_MouseLeftButtonUp;

            Children.Add(_rotationHandle);
        }

        // Kolizja

        // Zwraca 3 wierzchołki trójkąta        
        public Point[] GetTriangleWorldPoints()
            => GetTriangleWorldPoints(Canvas.GetLeft(this), Canvas.GetTop(this), _rotateTransform.Angle);

        public Point[] GetTriangleWorldPoints(double left, double top, double angleDeg) {
            if (double.IsNaN(left)) left = 0;
            if (double.IsNaN(top)) top = 0;

            Point[] local = {
                new Point(0, _size),
                new Point(_size, _size),
                new Point(0, 0)
            };

            double rad = angleDeg * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);

            var result = new Point[local.Length];
            for (int i = 0; i < local.Length; i++) {
                double px = local[i].X - _center;
                double py = local[i].Y - _center;
                double rx = px * cos - py * sin;
                double ry = px * sin + py * cos;
                result[i] = new Point(left + _center + rx, top + _center + ry);
            }
            return result;
        }

        // Czy trójkąt w zadanej pozycji/obrocie najedzie na inny trójkąt
        private bool CollidesWithSiblings(double left, double top, double angleDeg) {
            var parent = Parent as Canvas;
            if (parent == null) return false;

            Point[] mine = GetTriangleWorldPoints(left, top, angleDeg);
            foreach (var child in parent.Children) {
                if (child is NavTriangle other &&
                    !ReferenceEquals(other, this) &&
                    other.Visibility == Visibility.Visible) {
                    if (TrianglesOverlap(mine, other.GetTriangleWorldPoints()))
                        return true;
                }
            }
            return false;
        }

        private static bool TrianglesOverlap(Point[] a, Point[] b)
            => !HasSeparatingAxis(a, b) && !HasSeparatingAxis(b, a);

        private static bool HasSeparatingAxis(Point[] poly, Point[] other) {
            for (int i = 0; i < poly.Length; i++) {
                Point p1 = poly[i];
                Point p2 = poly[(i + 1) % poly.Length];
                var axis = new Vector(-(p2.Y - p1.Y), p2.X - p1.X);

                Project(poly, axis, out double minA, out double maxA);
                Project(other, axis, out double minB, out double maxB);

                if (maxA <= minB || maxB <= minA)
                    return true;
            }
            return false;
        }

        private static void Project(Point[] poly, Vector axis, out double min, out double max) {
            min = max = axis.X * poly[0].X + axis.Y * poly[0].Y;
            for (int i = 1; i < poly.Length; i++) {
                double d = axis.X * poly[i].X + axis.Y * poly[i].Y;
                if (d < min) min = d;
                else if (d > max) max = d;
            }
        }

        // Przyciaganie trójkata do punktu
        private bool TrySnapHypotenuseToPoint(ref double left, ref double top, double angleDeg) {
            var parent = Parent as Canvas;
            if (parent == null) return false;

            Point[] tri = GetTriangleWorldPoints(left, top, angleDeg);
            Point a = tri[2]; // (0, 0)      — koniec przeciwprostokątnej
            Point b = tri[1]; // (size,size) — koniec przeciwprostokątnej

            Vector dir = b - a;
            double length = dir.Length;
            if (length < 1e-6) return false;
            dir /= length;
            var normal = new Vector(-dir.Y, dir.X);

            double bestAbsDist = double.MaxValue;
            double bestSigned = 0;
            bool found = false;

            foreach (var child in parent.Children) {
                if (child is PositionMark mark && mark.Tag is Position pos) {
                    var p = new Point(pos.X, pos.Y);
                    double along = (p - a) * dir;
                    if (along < 0 || along > length) continue; // rzut poza przeciwprostokątną

                    double signed = (p - a) * normal;
                    double absDist = Math.Abs(signed);
                    if (absDist <= PointSnapRadius && absDist < bestAbsDist) {
                        bestAbsDist = absDist;
                        bestSigned = signed;
                        found = true;
                    }
                }
            }

            if (!found) return false;

            left += bestSigned * normal.X;
            top += bestSigned * normal.Y;
            return true;
        }

        // Czy przeciwprostokątna aktualnie przylega do zadanego punktu
        public bool IsAttachedToPoint(Point p, out Vector direction) {
            // + Zwraca wektor kierunku przeciwprostokątnej
            direction = new Vector(0, 0);

            Point[] tri = GetTriangleWorldPoints();
            Point a = tri[2]; // (0, 0)
            Point b = tri[1]; // (size, size)

            Vector dir = b - a;
            double length = dir.Length;
            if (length < 1e-6) return false;
            dir /= length;
            var normal = new Vector(-dir.Y, dir.X);

            double along = (p - a) * dir;
            if (along < 0 || along > length) return false;                   // punkt poza krawędzią
            if (Math.Abs((p - a) * normal) > PointSnapRadius) return false;  // krawędź nie przylega

            direction = dir;
            return true;
        }

        // Movement
        private void Handle_MouseLeftButtonDown(object sender, MouseButtonEventArgs mouse) {
            _isDragging = true;
            var parent = Parent as Canvas;
            if (parent == null) return;

            _dragStartMouse = mouse.GetPosition(parent);
            _dragStartElement = new Point(
                Canvas.GetLeft(this),
                Canvas.GetTop(this)
            );
            _handleLine?.CaptureMouse();
        }
        private void Handle_MouseMove(object sender, MouseEventArgs mouse) {
            if (!_isDragging) return;

            var parent = Parent as Canvas;
            if (parent == null) return;

            Point current = mouse.GetPosition(parent);
            double dx = current.X - _dragStartMouse.X;
            double dy = current.Y - _dragStartMouse.Y;

            double targetLeft = _dragStartElement.X + dx;
            double targetTop = _dragStartElement.Y + dy;
            double curLeft = Canvas.GetLeft(this);
            double curTop = Canvas.GetTop(this);
            double angle = _rotateTransform.Angle;

            // Przyciąganie do punktu
            bool snapped = TrySnapHypotenuseToPoint(ref targetLeft, ref targetTop, angle);

            if (!CollidesWithSiblings(targetLeft, targetTop, angle)) {
                Canvas.SetLeft(this, targetLeft);
                Canvas.SetTop(this, targetTop);
            }
            else if (!snapped && !CollidesWithSiblings(targetLeft, curTop, angle)) {
                // ruch wzdłuż osi X, gdy oś Y jest zablokowana
                Canvas.SetLeft(this, targetLeft);
            }
            else if (!snapped && !CollidesWithSiblings(curLeft, targetTop, angle)) {
                // ruch wzdłuż osi Y, gdy oś X jest zablokowana
                Canvas.SetTop(this, targetTop);
            }
            // przyklejony/kolizja/zablokowany -> brak ruchu
        }
        private void Handle_MouseLeftButtonUp(object sender, MouseButtonEventArgs mousee) {
            _isDragging = false;
            _handleLine?.ReleaseMouseCapture();
        }
        private void RotationHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs mouse) {
            _isRotating = true;
            (sender as UIElement)?.CaptureMouse();
            Mouse.OverrideCursor = Cursors.Cross;
        }
        private void RotationHandle_MouseMove(object sender, MouseEventArgs mouse) {
            if (!_isRotating) return;

            var parent = Parent as Canvas;
            if (parent == null) return;

            Point mousePos = mouse.GetPosition(parent);
            double left = Canvas.GetLeft(this);
            double top = Canvas.GetTop(this);

            double pointX = left + _rotationPoint.X;
            double pointY = top + _rotationPoint.Y;
            double dx = mousePos.X - pointX;
            double dy = mousePos.Y - pointY;

            double angle = (Math.Atan2(dy, dx) * 180 / Math.PI - 135); // -45 -> 90
            angle = Math.Round(angle * 2) / 2;  // round do 0.5

            double realAngle = (angle + 135) % 360;
            double realAngleRed = (realAngle + 180) % 360;
            //Debug.WriteLine($"angle={angle}; oblicz={realAngle}; oblicz RED={realAngleRed}");

            // odrzuć rotacje, jak najedzie na inny trójkąt
            if (CollidesWithSiblings(left, top, angle))
                return;

            AngleMain = realAngle;
            AngleRed = realAngleRed;

            _rotateTransform.Angle = angle;
        }
        private void RotationHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs mouse) {
            _isRotating = false;
            (sender as UIElement)?.ReleaseMouseCapture();
            Mouse.OverrideCursor = null;
        }
    }
}
