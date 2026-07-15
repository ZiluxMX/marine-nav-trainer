using marine_nav_trainer.Map.Models;
using marine_nav_trainer.Map.UI.Controls;
using marine_nav_trainer.Map.UI.Views;
using PdfiumViewer;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Brushes = System.Windows.Media.Brushes;
using Drawing = System.Drawing.Imaging;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using Path = System.IO.Path;
using Point = System.Windows.Point;
using Rectangle = System.Drawing.Rectangle;
using Threading = System.Windows.Threading;

namespace marine_nav_trainer.Map {
    public partial class MapView : UserControl, IDisposable {
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
        private bool _courseDirectionLocked = false;
        private Vector _courseDirection;
        private Position? _startPosition = null;
        private Position? _selectedPosition = null;
        private CourseLine? _selectedCourseLine = null;
        private List<Position> _positions = new();
        private List<CourseLine> _courseLines = new();
        private List<NavTriangle> _navTriangles = new();
        private List<NavDivider> _navDividers = new();
        private string _currentMapPath = string.Empty;
        private bool _disposed;


        // --sekcja TEMP <- JSON        
        private const double LatTop = 71.333333;
        private const double LatBottom = 69.000000;
        private const double LonLeft = 12.000000;
        private const double LonRight = 21.000000;
        // -- koniec sekcji TEMP

        private const string DefaultMapFile = "DefaultSeaMap.pdf";
        private static readonly string BackgroundDir = AppPaths.BackgroundDir;
        private static readonly string MapsDir = AppPaths.MapsDir;

        public MapView() {
            InitializeComponent();
            LoadPdfMap(Path.Combine(BackgroundDir, DefaultMapFile), true);
            Dispatcher.InvokeAsync(() => {
                ResizeMapToMainWindow();
            }, Threading.DispatcherPriority.Loaded);
        }

        private void LoadPdfMap(string pdfPath, bool IsDefaultMap = false) {
            using var document = PdfDocument.Load(pdfPath);
            int pageIndex = 0;
            int dpi = 300; //400
            var size = document.PageSizes[pageIndex];
            int width = (int)(size.Width / 72.0 * dpi);
            int height = (int)(size.Height / 72.0 * dpi);

            using var bitmap = (Bitmap)document.Render(
                pageIndex,
                width,
                height,
                dpi,
                dpi,
                PdfRotation.Rotate0,
                PdfRenderFlags.None);

            var wb = ConvertToWriteableBitmap(bitmap);
            MapScale.ScaleX = 1;
            MapScale.ScaleY = 1;

            MapImage.Source = wb;
            MapCanvas.Width = wb.PixelWidth;
            MapCanvas.Height = wb.PixelHeight;
            MapImage.Width = wb.PixelWidth;
            MapImage.Height = wb.PixelHeight;

            _currentMapPath = pdfPath;

            //Debug.WriteLine($"Height={height}  Width={width}");

            InitCalibrationEdges(!IsDefaultMap);
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
            }
            finally {
                bitmap.UnlockBits(bmpData);
            }
        }

        private void InitCalibrationEdges(bool IsEdgesVisible) {
            const double margin = 100;
            double w = MapCanvas.Width;
            double h = MapCanvas.Height;

            _edgesVisible = IsEdgesVisible;
            ToggleEdgesButton.Appearance = _edgesVisible ? Wpf.Ui.Controls.ControlAppearance.Info
                                                         : Wpf.Ui.Controls.ControlAppearance.Secondary;
            var isVisible = _edgesVisible ? Visibility.Visible : Visibility.Collapsed;
            LeftEdge.Visibility = isVisible;
            RightEdge.Visibility = isVisible;
            TopEdge.Visibility = isVisible;
            BottomEdge.Visibility = isVisible;

            LeftEdge.X1 = LeftEdge.X2 = margin;
            LeftEdge.Y1 = 0;
            LeftEdge.Y2 = h;

            RightEdge.X1 = RightEdge.X2 = w - margin;
            RightEdge.Y1 = 0;
            RightEdge.Y2 = h;

            TopEdge.Y1 = TopEdge.Y2 = margin;
            TopEdge.X1 = 0;
            TopEdge.X2 = w;

            BottomEdge.Y1 = BottomEdge.Y2 = h - margin;
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
                DeleteSelectedButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            }
            if (_selectedCourseLine != null) {
                _selectedCourseLine.Line.Stroke = Brushes.Blue;
                _selectedCourseLine = null;
                DeleteSelectedButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            }
        }

        private bool MarkObject(MouseButtonEventArgs mouse) {
            Position? markPos = GetMarkPositionFromSource(mouse.OriginalSource);
            if (markPos != null) {
                markPos.Mark.Color = Brushes.Orange;
                _selectedPosition = markPos;
                DeleteSelectedButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Caution;
                return true;
            }
            CourseLine? markCourse = GetCourseFromSource(mouse.OriginalSource);
            if (markCourse != null) {
                markCourse.Line.Stroke = Brushes.Orange;
                _selectedCourseLine = markCourse;
                DeleteSelectedButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Caution;
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
                if (_courseDirectionLocked)
                    pos = ProjectOntoCourseDirection(_currentLine, pos);
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
            SetMarkButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;

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

                _courseDirectionLocked = false;
                foreach (var triangle in _navTriangles) {
                    if (triangle.Visibility == Visibility.Visible &&
                        triangle.IsAttachedToPoint(new Point(markPos.X, markPos.Y), out Vector dir)) {
                        _courseDirection = dir;
                        _courseDirectionLocked = true;
                        break;
                    }
                }

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

            }
            else if (_isDrawingLine) {
                _isDrawingLine = false;
                if (markPos == null) {
                    Point pos = mouse.GetPosition(MapCanvas);
                    if (_currentLine != null && _startPosition != null) {
                        if (_courseDirectionLocked)
                            pos = ProjectOntoCourseDirection(_currentLine, pos);
                        _currentLine.X2 = pos.X;
                        _currentLine.Y2 = pos.Y;
                        isCourseComplete = true;
                    }
                }
                else {
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
                SetCourseButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
                _isCourseMode = false;
                _currentLine = null;
                _startPosition = null;
                _courseDirectionLocked = false;
            }
        }

        // Rysowanie lini po wektorze przyległego trójkąta
        private Point ProjectOntoCourseDirection(Line line, Point mousePos) {
            var start = new Point(line.X1, line.Y1);
            Vector toMouse = mousePos - start;
            double t = toMouse.X * _courseDirection.X + toMouse.Y * _courseDirection.Y;
            return new Point(
                start.X + _courseDirection.X * t,
                start.Y + _courseDirection.Y * t);
        }

        // Panel sterowania
        private void ToggleCalibrationLines() {
            _edgesVisible = !_edgesVisible;
            ToggleEdgesButton.Appearance = _edgesVisible ? Wpf.Ui.Controls.ControlAppearance.Info
                                                         : Wpf.Ui.Controls.ControlAppearance.Secondary;
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
            }
            else {
                ApplicationThemeManager.Apply(
                    ApplicationTheme.Light,
                    WindowBackdropType.Mica,
                    true);
            }
            _isLightMode = !_isLightMode;
        }

        private void ToggleNavTriangles() {
            if (_navTriangles.Count > 0) {
                bool anyVisible = _navTriangles[0].Visibility == Visibility.Visible;
                var newVisibility = anyVisible ? Visibility.Collapsed : Visibility.Visible;
                foreach (var navTriangle in _navTriangles)
                    navTriangle.Visibility = newVisibility;
                ToggleNavTrianglesButton.Appearance = anyVisible ? Wpf.Ui.Controls.ControlAppearance.Secondary
                                                         : Wpf.Ui.Controls.ControlAppearance.Info;
                return;
            }
            double size = 1300;
            var triangle = new NavTriangle(size);

            ToggleNavTrianglesButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Info;

            Point center = GetVisibleCenter();
            double centerX = center.X - size / 2;
            double centerY = center.Y - (1.6 * size) / 2;

            Canvas.SetLeft(triangle, centerX);
            Canvas.SetTop(triangle, centerY);

            _navTriangles.Add(triangle);
            MapCanvas.Children.Add(triangle);

            triangle = new NavTriangle(size);

            centerX = center.X - size / 2;
            centerY = center.Y + (0.6 * size) / 2;

            Canvas.SetLeft(triangle, centerX);
            Canvas.SetTop(triangle, centerY);

            _navTriangles.Add(triangle);
            MapCanvas.Children.Add(triangle);
        }

        private void ToggleNavDivider() {
            if (_navDividers.Count > 0) {
                bool anyVisible = _navDividers[0].Visibility == Visibility.Visible;
                var newVisibility = anyVisible ? Visibility.Collapsed : Visibility.Visible;
                foreach (var navDivider in _navDividers)
                    navDivider.Visibility = newVisibility;
                ToggleNavDividerButton.Appearance = anyVisible ? Wpf.Ui.Controls.ControlAppearance.Secondary
                                                         : Wpf.Ui.Controls.ControlAppearance.Info;
                return;
            }
            double size = 500;
            var divider = new NavDivider(size);

            ToggleNavDividerButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Info;

            Point center = GetVisibleCenter();
            double centerX = center.X - size / 2;
            double centerY = center.Y - divider.Height / 2;

            Canvas.SetLeft(divider, centerX);
            Canvas.SetTop(divider, centerY);

            _navDividers.Add(divider);
            MapCanvas.Children.Add(divider);
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
            if (_isCourseMode)
                SetCourseButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            _isCourseMode = false;
            SetMarkButton.Appearance = _isMarkMode ? Wpf.Ui.Controls.ControlAppearance.Info
                                                   : Wpf.Ui.Controls.ControlAppearance.Secondary;
            ClearSelected();
        }
        private void SetCourseOnClick(object sender, RoutedEventArgs e) {
            _isCourseMode = !_isCourseMode;
            if (_isMarkMode)
                SetMarkButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            _isMarkMode = false;
            SetCourseButton.Appearance = _isCourseMode ? Wpf.Ui.Controls.ControlAppearance.Info
                                                       : Wpf.Ui.Controls.ControlAppearance.Secondary;
            ClearSelected();
        }
        private void DeleteSelectedOnClick(object sender, RoutedEventArgs e) {
            RemoveSelectedObject();
        }
        private void ToggleNavTrianglesOnClick(object sender, RoutedEventArgs e) {
            ToggleNavTriangles();
        }

        private void ToggleNavDividerOnClick(object sender, RoutedEventArgs e) {
            ToggleNavDivider();
        }
          
        private void ClearOnClick(object sender, RoutedEventArgs e) {
            ClearMap();
        }

        private void SelectMapOnClick(object sender, RoutedEventArgs e) {
            SelectMap();
        }

        private void SelectMap() {
            var dialog = new MapSelectionWindow(MapsDir) {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true || dialog.SelectedMapPath == null)
                return;

            string selectedPath = dialog.SelectedMapPath;
            SelectMapButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            try {
                ClearMap();
                LoadPdfMap(selectedPath);
                ResizeMapToMainWindow();
            }
            catch (Exception ex) {
                MessageBox.Show(
                    $"Nie udało się wczytać mapy \"{Path.GetFileName(selectedPath)}\":\n{ex.Message}",
                    "Błąd wczytywania mapy",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ClearMap() {
            foreach (var pos in _positions) {
                if (pos.Mark != null) {
                    pos.Mark.Tag = null;
                    MapCanvas.Children.Remove(pos.Mark);
                }
            }
            foreach (var course in _courseLines) {
                if (course.Line != null) {
                    course.Line.Tag = null;
                    MapCanvas.Children.Remove(course.Line);
                }
            }
            foreach (var triangle in _navTriangles) {
                MapCanvas.Children.Remove(triangle);
            }
            foreach (var divider in _navDividers) {
                MapCanvas.Children.Remove(divider);
            }
            if (_edgesVisible)
                ToggleCalibrationLines();

            _positions.Clear();
            _courseLines.Clear();
            _navTriangles.Clear();
            _navDividers.Clear();

            _selectedPosition = null;
            _selectedCourseLine = null;
            _currentLine = null;
            _startPosition = null;
            _isDrawingLine = false;
            _courseDirectionLocked = false;
            _isMarkMode = false;
            _isCourseMode = false;

            MapImage.Source = null;
            MapScale.ScaleX = 1;
            MapScale.ScaleY = 1;
            MapCanvas.Width = 0;
            MapCanvas.Height = 0;
            MapImage.Width = 0;
            MapImage.Height = 0;
            _currentMapPath = string.Empty;

            MapCanvas.UpdateLayout();
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
                DeleteSelectedButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
                return;
            }
            if (_selectedCourseLine != null) {
                if (_selectedCourseLine.Line != null) {
                    MapCanvas.Children.Remove(_selectedCourseLine.Line);
                }
                _courseLines.Remove(_selectedCourseLine);
                _selectedCourseLine.Line = null;
                _selectedCourseLine = null;
                DeleteSelectedButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
                return;
            }
        }

        public void Dispose() {
            if (_disposed)
                return;
            _disposed = true;

            ClearMap();
        }
    }
}
