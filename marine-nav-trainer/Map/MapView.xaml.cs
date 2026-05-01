using PdfiumViewer;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Drawing.Color;
using Drawing = System.Drawing.Imaging;
using Point = System.Windows.Point;
using Rectangle = System.Drawing.Rectangle;
using Threading = System.Windows.Threading;

namespace marine_nav_trainer.Map {
    public partial class MapView : UserControl {
        private const double ZoomStep = 1.1;
        private const double MinZoom = 0.08;
        private const double MaxZoom = 2.0;
        private const double BitmapScalingStep = 0.35;
        private const double MarkSize = 30;

        private Point _panStart;
        private Line? _activeEdge = null;
        private Line? _currentLine = null;
        private int _nextIdPosition = 0;
        private int _nextIdCourse = 0;
        private double _hOffsetStart;
        private double _vOffsetStart;
        private bool _isPanning;
        private bool _edgesVisible = true;
        private bool _isLightMode = true;
        private bool _isMarkMode = false;
        private bool _isCourseMode = false; 
        private bool _isDrawingLine = false;
        private Position? _startPosition = null;
        private Position? _selectedPosition = null;
        private CourseLine? _selectedCourseLine = null;
        private List<Position> _positions = new();
        private List<CourseLine> _courseLines = new();


        // --sekcja TEMP <- JSON
        private const string MapFile = "Kart-312-3-2021.pdf";
        private const double LatTop = 71.333333;
        private const double LatBottom = 69.000000;
        private const double LonLeft = 12.000000;
        private const double LonRight = 21.000000;
        // -- koniec sekcji TEMP
        public MapView() {
            InitializeComponent();
            LoadPdfMap();
            Dispatcher.InvokeAsync(() => {
                ResizeMapToMainWindow();
            }, Threading.DispatcherPriority.Loaded);
        }

        private void LoadPdfMap() {
            string pdfPath = System.IO.Path.Combine(
                Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.FullName,
                "Assets",
                MapFile
            );

            using var document = PdfDocument.Load(pdfPath);
            int pageIndex = 0;
            int dpi = 300; //400
            var size = document.PageSizes[pageIndex];
            int width = (int)(size.Width / 72.0 * dpi);
            int height = (int)(size.Height / 72.0 * dpi);
            using var bitmap = new Bitmap(width, height, Drawing.PixelFormat.Format32bppRgb);

            using (var g = Graphics.FromImage(bitmap)) {
                g.Clear(Color.White);
                document.Render(
                    pageIndex,
                    g,
                    dpi,
                    dpi,
                    new Rectangle(0, 0, width, height),
                    false);
            }
            var wb = ConvertToWriteableBitmap(bitmap);
            MapImage.Source = wb;
            MapCanvas.Width = wb.PixelWidth;
            MapCanvas.Height = wb.PixelHeight;
            MapImage.Width = wb.PixelWidth;
            MapImage.Height = wb.PixelHeight;

            //Debug.WriteLine($"Height={height}  Width={width}");

            InitCalibrationEdges();
        }

        private WriteableBitmap ConvertToWriteableBitmap(Bitmap bitmap) {
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bmpData = bitmap.LockBits(
                rect,
                Drawing.ImageLockMode.ReadOnly,
                Drawing.PixelFormat.Format32bppRgb);
            try {
                var wb = new WriteableBitmap(
                    bitmap.Width,
                    bitmap.Height,
                    96, 96,
                    PixelFormats.Bgr32,
                    null);
                wb.WritePixels(
                    new Int32Rect(0, 0, bitmap.Width, bitmap.Height),
                    bmpData.Scan0,
                    bmpData.Stride * bitmap.Height,
                    bmpData.Stride);
                return wb;
            } finally {
                bitmap.UnlockBits(bmpData);
            }
        }

        private void InitCalibrationEdges() {
            double w = MapCanvas.Width;
            double h = MapCanvas.Height;

            LeftEdge.X1 = LeftEdge.X2 = 0;
            LeftEdge.Y1 = 0;
            LeftEdge.Y2 = h;

            RightEdge.X1 = RightEdge.X2 = w;
            RightEdge.Y1 = 0;
            RightEdge.Y2 = h;

            TopEdge.Y1 = TopEdge.Y2 = 0;
            TopEdge.X1 = 0;
            TopEdge.X2 = w;

            BottomEdge.Y1 = BottomEdge.Y2 = h;
            BottomEdge.X1 = 0;
            BottomEdge.X2 = w;
        }

        private void ResizeMapToMainWindow() {
            double windowWidth = MapScrollViewer.ViewportWidth;
            double windowHeight = MapScrollViewer.ViewportHeight;
            //Debug.WriteLine($"WIN_Height={windowHeight}  WIN_Width={windowWidth}");
            FitToWindow(windowHeight, windowWidth);
        }

        private void FitToWindow(double maxHeight, double maxWidth) {
            if (maxHeight == 0 || maxWidth == 0)
                return;
            double scaleY = maxHeight / MapImage.Height;
            double scaleX = maxWidth / MapImage.Width;
            double scale = Math.Min(scaleX, scaleY);
            scale = Math.Max(scale, MinZoom);
            if (scale <= BitmapScalingStep)
                RenderOptions.SetBitmapScalingMode(MapImage, BitmapScalingMode.Fant);
            else
                RenderOptions.SetBitmapScalingMode(MapImage, BitmapScalingMode.LowQuality);

            MapScale.ScaleX = scale;
            MapScale.ScaleY = scale;

            MapCanvas.Width = MapImage.Width * scale;
            MapCanvas.Height = MapImage.Height * scale;

            MapScrollViewer.ScrollToHorizontalOffset(0);
            MapScrollViewer.ScrollToVerticalOffset(0);
        }

        private void MapScrollViewer_MouseWheel_Zoom(object sender, MouseWheelEventArgs mouse) {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                return;

            double zoom = mouse.Delta > 0 ? ZoomStep : 1 / ZoomStep;
            double oldScale = MapScale.ScaleX;
            double newScale = oldScale * zoom;
            if (newScale < MinZoom || newScale > MaxZoom)
                return;

            var mousePos = mouse.GetPosition(MapScrollViewer);
            double absX = (MapScrollViewer.HorizontalOffset + mousePos.X) / oldScale;
            double absY = (MapScrollViewer.VerticalOffset + mousePos.Y) / oldScale;

            if (newScale <= BitmapScalingStep)
                RenderOptions.SetBitmapScalingMode(MapImage, BitmapScalingMode.Fant);
            else
                RenderOptions.SetBitmapScalingMode(MapImage, BitmapScalingMode.LowQuality);

            MapScale.ScaleX = newScale;
            MapScale.ScaleY = newScale;
            MapCanvas.Width = MapImage.Width * newScale;
            MapCanvas.Height = MapImage.Height * newScale;
            MapScrollViewer.ScrollToHorizontalOffset(absX * newScale - mousePos.X);
            MapScrollViewer.ScrollToVerticalOffset(absY * newScale - mousePos.Y);

            //Debug.WriteLine($"Zoom={zoom}  Scala={newScale}");

            mouse.Handled = true;
        }

        private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs mouse) {
            if (Keyboard.IsKeyDown(Key.LeftCtrl)) {
                _isPanning = true;
                _panStart = mouse.GetPosition(MapScrollViewer);
                _hOffsetStart = MapScrollViewer.HorizontalOffset;
                _vOffsetStart = MapScrollViewer.VerticalOffset;
                MapCanvas.CaptureMouse();
                return;
            }
            if (Keyboard.IsKeyDown(Key.LeftAlt) && _edgesVisible) {
                Point pos = mouse.GetPosition(MapCanvas);
                _activeEdge = GetEdgeNearPoint(pos);
                if (_activeEdge != null)
                    MapCanvas.CaptureMouse();
                return;
            }
            ClearSelected();
            if (_isCourseMode) {
                SetCourseLine(mouse);
                return;
            }
            if (_isMarkMode) {
                SetPositionMark(mouse);
                return;
            }
            //Debug.WriteLine($"Source:{mouse.OriginalSource}");
            if (MarkObject(mouse))
                return;
        }

        private void ClearSelected() {
            if (_selectedPosition != null) {
                _selectedPosition.Mark.Color = Brushes.Blue;
                _selectedPosition = null;
            }
            if (_selectedCourseLine != null) {
                _selectedCourseLine.Line.Stroke = Brushes.Blue;
                _selectedCourseLine = null;
            }
        }

        private bool MarkObject(MouseButtonEventArgs mouse) {
            Position? markPos = GetMarkPositionFromSource(mouse.OriginalSource);
            if (markPos != null) {
                markPos.Mark.Color = Brushes.Orange;
                _selectedPosition = markPos;
                return true;
            }
            CourseLine? markCourse = GetCourseFromSource(mouse.OriginalSource);
            if (markCourse != null) {
                markCourse.Line.Stroke = Brushes.Orange;
                _selectedCourseLine = markCourse;
                return true;
            }
            return false;
        }

        private void MapCanvas_MouseMove(object sender, MouseEventArgs mouse) {
            if (_activeEdge != null && _edgesVisible) {
                Point pos = mouse.GetPosition(MapCanvas);

                if (_activeEdge == LeftEdge || _activeEdge == RightEdge)
                    _activeEdge.X1 = _activeEdge.X2 = Clamp(pos.X, 0, MapCanvas.Width);
                else
                    _activeEdge.Y1 = _activeEdge.Y2 = Clamp(pos.Y, 0, MapCanvas.Height);
                return;
            }
            if (_isPanning) {
                Point cur = mouse.GetPosition(MapScrollViewer);
                Vector d = cur - _panStart;
                MapScrollViewer.ScrollToHorizontalOffset(_hOffsetStart - d.X);
                MapScrollViewer.ScrollToVerticalOffset(_vOffsetStart - d.Y);
                return;
            }
            if (_isDrawingLine && _currentLine != null) {
                Point pos = mouse.GetPosition(MapCanvas);
                _currentLine.X2 = pos.X;
                _currentLine.Y2 = pos.Y;
            }
        }

        private void MapCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs mouse) {
            _isPanning = false;
            _activeEdge = null;
            MapCanvas.ReleaseMouseCapture();
        }

        private void MapCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs mouse) {
            Point pos = mouse.GetPosition(MapCanvas);
            var geo = PixelToGeoCalibrated(pos.X, pos.Y);

            //Debug.WriteLine($"LAT={geo.lat:F5}  LON={geo.lon:F5}");

            string latStr = DegToDegMin(geo.lat, isLat: true);
            string lonStr = DegToDegMin(geo.lon, isLat: false);

            //Debug.WriteLine($"LAT = {latStr}   LON = {lonStr}");
            CoordsText.Text = $"φ:{latStr}  λ:{lonStr}";
        }
                
        private (double lat, double lon) PixelToGeoCalibrated(double x, double y) {
            double left = LeftEdge.X1;
            double right = RightEdge.X1;
            double top = TopEdge.Y1;
            double bottom = BottomEdge.Y1;
            //Debug.WriteLine($"L = {left};  R = {right};  T = {top};  B = {bottom}");

            double xn = (x - left) / (right - left);
            double yn = (y - top) / (bottom - top);

            double lon = LonLeft + xn * (LonRight - LonLeft);

            double mTop = Math.Log(Math.Tan(Math.PI / 4 + DegToRad(LatTop) / 2));
            double mBot = Math.Log(Math.Tan(Math.PI / 4 + DegToRad(LatBottom) / 2));
            double my = mTop - yn * (mTop - mBot);

            double lat = RadToDeg(2 * Math.Atan(Math.Exp(my)) - Math.PI / 2);

            return (lat, lon);
        }

        private static string DegToDegMin(double degValue, bool isLat) {
            char cardinal;
            if (isLat)
                cardinal = degValue >= 0 ? 'N' : 'S';
            else
                cardinal = degValue >= 0 ? 'E' : 'W';
            degValue = Math.Abs(degValue);
            int deg = (int)Math.Floor(degValue);
            double min = (degValue - deg) * 60.0;

            return $"{deg}° {min:00.0000}′{cardinal}";
        }


        private Line? GetEdgeNearPoint(Point pos) {
            const double tol = 8;
            if (Math.Abs(pos.X - LeftEdge.X1) < tol) return LeftEdge;
            if (Math.Abs(pos.X - RightEdge.X1) < tol) return RightEdge;
            if (Math.Abs(pos.Y - TopEdge.Y1) < tol) return TopEdge;
            if (Math.Abs(pos.Y - BottomEdge.Y1) < tol) return BottomEdge;

            return null;
        }

        private void SetPositionMark(MouseButtonEventArgs mouse) {
            Point pos = mouse.GetPosition(MapCanvas);

            PositionMark mark = new PositionMark(30);
            Canvas.SetLeft(mark, pos.X - MarkSize / 2);
            Canvas.SetTop(mark, pos.Y - MarkSize / 2);

            MapCanvas.Children.Add(mark);
            _isMarkMode = false;

            var geo = PixelToGeoCalibrated(pos.X, pos.Y);
            var markPos = new Position {
                Id = _nextIdPosition,
                X = pos.X,
                Y = pos.Y,
                Lat = geo.lat,
                Lon = geo.lon,
                Label = $"P{_nextIdPosition}",
                Mark = mark
            };

            _positions.Add(markPos);
            _nextIdPosition++;
            mark.Tag = markPos;
        }

        private void SetCourseLine(MouseButtonEventArgs mouse) {
            var markPos = GetMarkPositionFromSource(mouse.OriginalSource);
            bool isCourseComplete = false;

            //Debug.WriteLine($"Source:{mouse.OriginalSource}");
            //if (markPos != null)
            //    Debug.WriteLine($"Klik w marker ID={markPos.Id}");

            if (markPos != null && !_isDrawingLine) {
                _isDrawingLine = true;
                _startPosition = markPos;

                _currentLine = new Line {
                    X1 = markPos.X,
                    Y1 = markPos.Y,
                    X2 = markPos.X,
                    Y2 = markPos.Y,
                    Stroke = Brushes.Blue,
                    StrokeThickness = 4,
                    IsHitTestVisible = false
                };
                MapCanvas.Children.Add(_currentLine);

            } else if (_isDrawingLine) {
                _isDrawingLine = false;
                if (markPos == null) {
                    Point pos = mouse.GetPosition(MapCanvas);
                    if (_currentLine != null && _startPosition != null) {
                        _currentLine.X2 = pos.X;
                        _currentLine.Y2 = pos.Y;
                        isCourseComplete = true;                        
                    }
                } else {
                    if (_currentLine != null && _startPosition != null) {
                        _currentLine.X2 = markPos.X;
                        _currentLine.Y2 = markPos.Y;
                        isCourseComplete = true;                        
                    }
                }
                if (isCourseComplete) {
                    var course = new CourseLine {
                        Id = _nextIdCourse,
                        StartPosition = _startPosition,
                        StartX = _currentLine.X1,
                        StartY = _currentLine.Y1,
                        EndX = _currentLine.X2,
                        EndY = _currentLine.Y2,
                        CourseOverGround = null, // TODO do dokończenia
                        CourseCompas = null,
                        Line = _currentLine
                    };
                    if (markPos != null) {
                        course.EndPosition = markPos;
                    }
                    _currentLine.Tag = course;
                    _courseLines.Add(course);
                    _nextIdCourse++;
                    _currentLine.IsHitTestVisible = true;
                }                    
                _isCourseMode = false;
                _currentLine = null;
                _startPosition = null;
            }           
        }

        // Panel sterowania
        private void ToggleCalibrationLines() {
            _edgesVisible = !_edgesVisible;
            var isVisible = _edgesVisible ? Visibility.Visible : Visibility.Collapsed;

            LeftEdge.Visibility = isVisible;
            RightEdge.Visibility = isVisible;
            TopEdge.Visibility = isVisible;
            BottomEdge.Visibility = isVisible;
        }

        private void ToggleThermeStyle() {
            if (_isLightMode) {
                ApplicationThemeManager.Apply(
                    ApplicationTheme.Dark,
                    WindowBackdropType.Mica,
                    true);
            } else {
                ApplicationThemeManager.Apply(
                    ApplicationTheme.Light,
                    WindowBackdropType.Mica,
                    true);
            }
            _isLightMode = !_isLightMode;
        }

        private void ToggleNavTriangles() {
            double size = 1300;
            var triangle = new NavTriangle(size);

            Point center = GetVisibleCenter();
            double centerX = center.X - size / 2;
            double centerY = center.Y - size / 2;

            Canvas.SetLeft(triangle, centerX );
            Canvas.SetTop(triangle, centerY );

            MapCanvas.Children.Add(triangle);
        }

        private void ToggleEdgesOnClick(object sender, RoutedEventArgs e) {
            ToggleCalibrationLines();
        }
        private void ResizeMapOnClick(object sender, RoutedEventArgs e) {
            ResizeMapToMainWindow();
        }
        private void ToggleThermeOnClick(object sender, RoutedEventArgs e) {
            ToggleThermeStyle();
        }
        private void SetMarkOnClick(object sender, RoutedEventArgs e) {
            _isMarkMode = !_isMarkMode;
            _isCourseMode = false;
            ClearSelected();
        }
        private void SetCourseOnClick(object sender, RoutedEventArgs e) {
            _isCourseMode = !_isCourseMode;
            _isMarkMode = false;
            ClearSelected();
        }
        private void DeleteSelectedOnClick(object sender, RoutedEventArgs e) {
            RemoveSelectedObject();
        }
        private void ToggleNavTrianglesOnClick(object sender, RoutedEventArgs e) {
            ToggleNavTriangles();
        }

        // Static
        private static double Clamp(double v, double min, double max)
            => Math.Max(min, Math.Min(max, v));
        private static double DegToRad(double d)
            => d * Math.PI / 180.0;
        private static double RadToDeg(double r)
            => r * 180.0 / Math.PI;
        

        // inne        
        private Position? GetMarkPositionFromSource(object source) {
            DependencyObject? current = source as DependencyObject;
            while (current != null) {
                if (current is FrameworkElement fe && fe.Tag is Position pos)
                    return pos;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
        private CourseLine? GetCourseFromSource(object source) {
            DependencyObject? current = source as DependencyObject;
            while (current != null) {
                if (current is FrameworkElement fe && fe.Tag is CourseLine course)
                    return course;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
        private Point GetVisibleCenter() {
            double centerX = MapScrollViewer.HorizontalOffset + MapScrollViewer.ViewportWidth / 2;
            double centerY = MapScrollViewer.VerticalOffset + MapScrollViewer.ViewportHeight / 2;

            return new Point(
                centerX / MapScale.ScaleX,
                centerY / MapScale.ScaleY
            );
        }

        private void RemoveSelectedObject() {
            if (_selectedPosition != null) {
                if (_selectedPosition.Mark != null) {
                    MapCanvas.Children.Remove(_selectedPosition.Mark);
                }
                List<CourseLine>? toRemove =
                    _courseLines.Where(course => course.StartPosition == _selectedPosition ||
                    course.EndPosition == _selectedPosition).ToList();
                foreach (CourseLine course in toRemove) {
                    if (course.Line != null)
                        MapCanvas.Children.Remove(course.Line);
                    _courseLines.Remove(course);
                }
                _positions.Remove(_selectedPosition);
                _selectedPosition.Mark = null;
                _selectedPosition = null;

                return;
            }
            if (_selectedCourseLine != null) {
                if (_selectedCourseLine.Line != null) {
                    MapCanvas.Children.Remove(_selectedCourseLine.Line);
                }
                _courseLines.Remove(_selectedCourseLine);
                _selectedCourseLine.Line = null;
                _selectedCourseLine = null;

                return;
            }
        }
    }
}
