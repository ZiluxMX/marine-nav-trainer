using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Point = System.Windows.Point;

namespace marine_nav_trainer.Map.UI.Controls {
    internal enum NavDividerModel {
        Classic,    // <---o--->
        Compass     // podstawa + ramiona cyrkla
    }

    internal class NavDivider : Canvas {
        // model wizualny kroczka
        private const NavDividerModel Model = NavDividerModel.Compass;

        private static readonly SolidColorBrush RedBrush = new SolidColorBrush(Color.FromArgb(180, 255, 0, 0));
        private static readonly SolidColorBrush BlackBrush = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0));
        private static readonly SolidColorBrush LimeBrush = new SolidColorBrush(Color.FromArgb(180, 100, 255, 0));

        public double Size;

        private readonly double _centerY;
        private readonly double _lineThickness;
        private readonly double _arrowLength;
        private readonly double _arrowHalfWidth;
        private readonly double _circleRadius;
        private readonly double _armHeight;
        private Line? _line;
        private Line? _arm1;
        private Line? _arm2;
        private Line? _centerLink;
        private Ellipse? _handleCircle;
        private Ellipse? _apexRing;
        private Polygon? _arrowHead1;
        private Polygon? _arrowHead2;
        private RotateTransform _rotateTransform = new RotateTransform(0);

        private bool _isMovingDivider;
        private double _moveStartLeft;
        private double _moveStartTop;
        private Point _moveStartMouse;

        private bool _isRotating;

        private bool _isDraggingArrow;
        private bool _isHoldArrowHead1;
        private Point _pivotAnchor;

        public NavDivider(double size) {
            Size = size;
            _lineThickness = 4;
            _arrowLength = 40;
            _arrowHalfWidth = 10;
            _circleRadius = 10;
            _armHeight = 120;
            Width = size;
            Height = _circleRadius * 2;
            _centerY = _circleRadius;

            _rotateTransform = new RotateTransform(0);
            this.RenderTransform = _rotateTransform;
            this.RenderTransformOrigin = new Point(0.5, 0.5);

            // zawsze na wierchu innych obiektów canvy
            Panel.SetZIndex(this, int.MaxValue);

            DrawLine();
            if (Model == NavDividerModel.Compass)
                DrawArms();
            DrawArrowHeads();
            DrawCircleHandle();
            if (Model == NavDividerModel.Compass)
                DrawApexHandle();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void DrawLine() {
            _line = new Line {
                X1 = _arrowLength,
                Y1 = _centerY,
                X2 = Size - _arrowLength,
                Y2 = _centerY,
                Stroke = BlackBrush,
                StrokeThickness = _lineThickness,
                IsHitTestVisible = false
            };
            Children.Add(_line);
        }

        private void DrawArrowHeads() {
            // lewy grot
            _arrowHead1 = new Polygon {
                Points = new PointCollection
                {
                    new Point(0, _centerY),
                    new Point(_arrowLength, _centerY - _arrowHalfWidth),
                    new Point(_arrowLength, _centerY + _arrowHalfWidth)
                },
                Fill = RedBrush,
                Cursor = Cursors.Hand
            };
            _arrowHead1.MouseLeftButtonDown += ArrowHead1_MouseLeftButtonDown;
            _arrowHead1.MouseMove += ArrowHead_MouseMove;
            _arrowHead1.MouseLeftButtonUp += ArrowHead_MouseLeftButtonUp;
            Children.Add(_arrowHead1);

            // prawy grot
            _arrowHead2 = new Polygon {
                Points = new PointCollection
                {
                    new Point(Size, _centerY),
                    new Point(Size - _arrowLength, _centerY - _arrowHalfWidth),
                    new Point(Size - _arrowLength, _centerY + _arrowHalfWidth)
                },
                Fill = RedBrush,
                Cursor = Cursors.Hand
            };
            _arrowHead2.MouseLeftButtonDown += ArrowHead2_MouseLeftButtonDown;
            _arrowHead2.MouseMove += ArrowHead_MouseMove;
            _arrowHead2.MouseLeftButtonUp += ArrowHead_MouseLeftButtonUp;
            Children.Add(_arrowHead2);
        }

        private void DrawCircleHandle() {
            _handleCircle = CreateCircleHandle();
            Canvas.SetLeft(_handleCircle, Size / 2 - _circleRadius);
            Canvas.SetTop(_handleCircle, _centerY - _circleRadius);
            Children.Add(_handleCircle);
        }

        // ramiona cyrkla: od podstaw grotów do złączenia nad środkiem + cieńszy łącznik między kropką środkową a złączeniem
        private void DrawArms() {
            _arm1 = CreateArm();
            _arm2 = CreateArm();
            _centerLink = CreateArm();
            _centerLink.StrokeThickness = _lineThickness / 2;
            UpdateArms();
            Children.Add(_arm1);
            Children.Add(_arm2);
            Children.Add(_centerLink);
        }

        // pierścień na złączeniu ramion - obraca kroczek względem kropki środkowej
        private void DrawApexHandle() {
            double diameter = _circleRadius * 2;

            _apexRing = new Ellipse {
                Width = diameter,
                Height = diameter,
                Stroke = Brushes.Red,
                StrokeThickness = 4,
                Fill = Brushes.Transparent,
                Cursor = Cursors.Hand
            };
            Canvas.SetLeft(_apexRing, Size / 2 - _circleRadius);
            Canvas.SetTop(_apexRing, _centerY - _armHeight - _circleRadius);
            _apexRing.MouseLeftButtonDown += ApexRing_MouseLeftButtonDown;
            _apexRing.MouseMove += ApexRing_MouseMove;
            _apexRing.MouseLeftButtonUp += ApexRing_MouseLeftButtonUp;
            Children.Add(_apexRing);
        }

        private Ellipse CreateCircleHandle() {
            double diameter = _circleRadius * 2;

            var circle = new Ellipse {
                Width = diameter,
                Height = diameter,
                Fill = Brushes.Red,
                Cursor = Cursors.SizeAll
            };
            circle.MouseLeftButtonDown += Circle_MouseLeftButtonDown;
            circle.MouseMove += Circle_MouseMove;
            circle.MouseLeftButtonUp += Circle_MouseLeftButtonUp;
            return circle;
        }

        private Line CreateArm() {
            return new Line {
                Stroke = BlackBrush,
                StrokeThickness = _lineThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false
            };
        }

        private void UpdateArms() {
            double apexX = Size / 2;
            double apexY = _centerY - _armHeight;

            if (_arm1 != null) {
                _arm1.X1 = _arrowLength;
                _arm1.Y1 = _centerY;
                _arm1.X2 = apexX;
                _arm1.Y2 = apexY;
            }
            if (_arm2 != null) {
                _arm2.X1 = Size - _arrowLength;
                _arm2.Y1 = _centerY;
                _arm2.X2 = apexX;
                _arm2.Y2 = apexY;
            }
            if (_centerLink != null) {
                _centerLink.X1 = apexX;
                _centerLink.Y1 = _centerY;
                _centerLink.X2 = apexX;
                _centerLink.Y2 = apexY;
            }
        }

        private void ApplyGeometry() {
            Width = Size;

            if (_line != null) {
                _line.X1 = _arrowLength;
                _line.X2 = Size - _arrowLength;
            }
            if (_arrowHead2 != null) {
                _arrowHead2.Points = new PointCollection
                {
                    new Point(Size, _centerY),
                    new Point(Size - _arrowLength, _centerY - _arrowHalfWidth),
                    new Point(Size - _arrowLength, _centerY + _arrowHalfWidth)
                };
            }
            if (_handleCircle != null) {
                Canvas.SetLeft(_handleCircle, Size / 2 - _circleRadius);
            }
            if (_apexRing != null) {
                Canvas.SetLeft(_apexRing, Size / 2 - _circleRadius);
            }
            UpdateArms();
        }

        // pozycja wierzchołka grotu
        private Point GetAnchorArrowHead(bool isArrowHead1) {
            double left = GetLeftOrZero();
            double top = GetTopOrZero();

            double cx = left + Size / 2;
            double cy = top + _centerY;
            double a = _rotateTransform.Angle * Math.PI / 180.0;
            double half = (Size / 2) * (isArrowHead1 ? -1 : 1);

            return new Point(cx + Math.Cos(a) * half, cy + Math.Sin(a) * half);
        }

        private void PositionFromEndpoints(Point p1, Point p2) {
            Vector dir = p2 - p1;
            double angle = Math.Atan2(dir.Y, dir.X) * 180.0 / Math.PI;
            double cx = (p1.X + p2.X) / 2.0;
            double cy = (p1.Y + p2.Y) / 2.0;

            Canvas.SetLeft(this, cx - Size / 2);
            Canvas.SetTop(this, cy - _centerY);
            _rotateTransform.Angle = angle;
        }

        // Ruch cyrkla
        private void Circle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var parent = Parent as Canvas;
            if (parent == null) return;

            _isMovingDivider = true;
            _moveStartMouse = e.GetPosition(parent);
            _moveStartLeft = GetLeftOrZero();
            _moveStartTop = GetTopOrZero();
            (sender as UIElement)?.CaptureMouse();
            e.Handled = true;
        }
        private void Circle_MouseMove(object sender, MouseEventArgs e) {
            if (!_isMovingDivider) return;
            var parent = Parent as Canvas;
            if (parent == null) return;

            Point position = e.GetPosition(parent);
            Canvas.SetLeft(this, _moveStartLeft + (position.X - _moveStartMouse.X));
            Canvas.SetTop(this, _moveStartTop + (position.Y - _moveStartMouse.Y));
        }
        private void Circle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            _isMovingDivider = false;
            (sender as UIElement)?.ReleaseMouseCapture();
        }

        // Obrót względem kropki środkowej (pierścień na złączeniu)
        private void ApexRing_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (Parent is not Canvas) return;

            _isRotating = true;
            (sender as UIElement)?.CaptureMouse();
            e.Handled = true;
        }
        private void ApexRing_MouseMove(object sender, MouseEventArgs e) {
            if (!_isRotating) return;
            var parent = Parent as Canvas;
            if (parent == null) return;

            Point mouse = e.GetPosition(parent);
            double dx = mouse.X - (GetLeftOrZero() + Size / 2);
            double dy = mouse.Y - (GetTopOrZero() + _centerY);
            if (dx * dx + dy * dy < 1e-6) return;

            // złączenie leży lokalnie "w górę" (0, -1) od środka
            _rotateTransform.Angle = Math.Atan2(dx, -dy) * 180.0 / Math.PI;
        }
        private void ApexRing_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            _isRotating = false;
            (sender as UIElement)?.ReleaseMouseCapture();
        }

        private void ArrowHead1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => BeginArrowDrag(true, sender, e);
        private void ArrowHead2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => BeginArrowDrag(false, sender, e);

        private void BeginArrowDrag(bool isArrowHead1, object sender, MouseButtonEventArgs e) {
            var parent = Parent as Canvas;
            if (parent == null) return;

            _isDraggingArrow = true;
            _isHoldArrowHead1 = isArrowHead1;
            _pivotAnchor = GetAnchorArrowHead(!isArrowHead1);

            (sender as UIElement)?.CaptureMouse();
            e.Handled = true;
        }

        private void ArrowHead_MouseMove(object sender, MouseEventArgs e) {
            if (!_isDraggingArrow) return;
            var parent = Parent as Canvas;
            if (parent == null) return;

            Point mouse = e.GetPosition(parent);
            Vector d = mouse - _pivotAnchor;
            double len = d.Length;
            if (len < 1e-6) return;

            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            Point movingPoint;

            if (shift) {                                    // resize
                double minSize = 2 * _arrowLength + 20;
                if (len < minSize) {
                    d = d / len * minSize;
                    len = minSize;
                }
                Size = len;
                movingPoint = _pivotAnchor + d;
            }
            else {                                          // ruch cyrkla
                movingPoint = _pivotAnchor + d / len * Size;
            }

            ApplyGeometry();

            Point p1 = _isHoldArrowHead1 ? movingPoint : _pivotAnchor;
            Point p2 = _isHoldArrowHead1 ? _pivotAnchor : movingPoint;
            PositionFromEndpoints(p1, p2);
        }

        private void ArrowHead_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            _isDraggingArrow = false;
            (sender as UIElement)?.ReleaseMouseCapture();
        }

        // kosmetyka, podświetlanie
        private void OnLoaded(object sender, RoutedEventArgs e) {
            var window = Window.GetWindow(this);
            if (window != null) {
                window.PreviewKeyDown += Window_KeyChanged;
                window.PreviewKeyUp += Window_KeyChanged;
            }
        }
        private void OnUnloaded(object sender, RoutedEventArgs e) {
            var window = Window.GetWindow(this);
            if (window != null) {
                window.PreviewKeyDown -= Window_KeyChanged;
                window.PreviewKeyUp -= Window_KeyChanged;
            }
        }
        private void Window_KeyChanged(object sender, KeyEventArgs e) {
            if (e.Key != Key.LeftShift && e.Key != Key.RightShift) return;
            bool shiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            SetArrowHighlight(shiftDown);
        }
        private void SetArrowHighlight(bool on) {
            var brush = on ? LimeBrush : RedBrush;
            if (_arrowHead1 != null) _arrowHead1.Fill = brush;
            if (_arrowHead2 != null) _arrowHead2.Fill = brush;
        }

        private double GetLeftOrZero() {
            double v = Canvas.GetLeft(this);
            return double.IsNaN(v) ? 0 : v;
        }
        private double GetTopOrZero() {
            double v = Canvas.GetTop(this);
            return double.IsNaN(v) ? 0 : v;
        }
    }
}
