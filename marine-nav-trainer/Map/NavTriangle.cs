using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Point = System.Windows.Point;

namespace marine_nav_trainer.Map
{
    internal class NavTriangle : Canvas {
        private readonly double _size;
        private readonly double _center;

        public NavTriangle(double size) {
            _size = size;
            _center = size / 2;
            Width = size;
            Height = size;

            DrawTriangle();
            DrawScale();
            DrawHandle();
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

            for (int angle = 0; angle <= 180; angle++)
            {
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
                        modX = - displayAngle / 10;
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
                        modX = - 16 + displayAngle / 10;
                        modY = displayAngle / 10;
                        if (displayAngle == 0) {
                            modX = -25;
                            modY = 9;
                        }
                    } else if (displayAngle > 110) {
                        modX = - displayAngle / 10;
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

            var handleLine = new Line {
                X1 = centerX - dx,
                Y1 = centerY - dy,
                X2 = centerX + dx,
                Y2 = centerY + dy,
                Stroke = Brushes.Red,
                StrokeThickness = 60
            };
            Children.Add(handleLine);
        }
    }
}
