using marine_nav_trainer.Map.Models;
using marine_nav_trainer.Map.UI.Controls;
using marine_nav_trainer.Map.UI.Views;
using marine_nav_trainer.Navigation.Models;
using PdfiumViewer;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
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
using MessageBoxResult = System.Windows.MessageBoxResult;
using Path = System.IO.Path;
using Point = System.Windows.Point;
using Rectangle = System.Drawing.Rectangle;
using Threading = System.Windows.Threading;

namespace marine_nav_trainer.Map {
    public partial class MapView : UserControl, IDisposable {
        public bool _isTeacherMode = true;

        private const string DefaultMapFile = "ChatGPT_BackgroundMap.pdf";
        private const int MaxTaskFileNameLength = 80;

        private double ZoomStep = 1.1;
        private double MinZoom = 0.08;
        private double MaxZoom = 2.0;
        private double BitmapScalingStep = 0.35;
        private double MarkSize = 30;
        private double CourseLabelFontSize = 30;
        private double CourseLabelOffset = 6;

        private Point _panStart;
        private Line? _activeEdge = null;
        private Line? _currentLine = null;
        private int _nextIdPosition = 0;
        private int _nextIdCourse = 0;
        private int _nextIdGuide = 0;
        private double _hOffsetStart;
        private double _vOffsetStart;
        private bool _isPanning;
        private bool _edgesVisible = true;
        private bool _isLightMode = true;
        private bool _isMarkMode = false;
        private bool _isCourseMode = false;
        private bool _isGuideLineMode = false;
        private bool _isDrawingLine = false;
        private bool _courseDirectionLocked = false;
        private bool _isDisposed;
        private bool _isUpdating;
        private bool _isSyncingSelection;
        private Vector _courseDirection;
        private Position? _startPosition = null;
        private Position? _selectedPosition = null;
        private CourseLine? _selectedCourseLine = null;
        private GuideLine? _selectedGuideLine = null;
        private List<Position> _positions = new();
        private List<CourseLine> _courseLines = new();
        private List<GuideLine> _guideLines = new();
        private List<NavTriangle> _navTriangles = new();
        private List<NavDivider> _navDividers = new();
        private string _currentMapPath = string.Empty;
        private DateTime _defaultPointDate = DateTime.Today;
        private readonly HashSet<MapFrame> _visibleFrames = [];

        // JSON        
        private double LatTop = 0;
        private double LatBottom = 0;
        private double LonLeft = 0;
        private double LonRight = 0;
        private double MarginLeft = 0;
        private double MarginRight = 0;
        private double MarginTop = 0;
        private double MarginBottom = 0;

        private static readonly string BackgroundDir = AppPaths.BackgroundDir;
        private static readonly string MapsDir = AppPaths.MapsDir;
        private static readonly string JsonsDir = AppPaths.JsonsDir;

        private double MapContentWidth => ContentExtent(MapImage.Width);
        private double MapContentHeight => ContentExtent(MapImage.Height);

        // ramki obok mapy
        public event EventHandler<MapFrame>? FrameVisibilityChanged;

        // dane ramek przy zapisie i wczytaniu zadania
        public event EventHandler<NavigationData>? NavigationDataRequested;
        public event EventHandler<NavigationData>? NavigationDataLoaded;
        public event EventHandler<TaskMeta>? TaskMetaRequested;
        public event EventHandler<TaskMeta>? TaskMetaLoaded;

        public void SetDefaultPointDate(DateTime date) {
            _defaultPointDate = date.Date;
        }

        public bool IsFrameVisible(MapFrame frame) {
            return _visibleFrames.Contains(frame);
        }

        public MapView() {
            InitializeComponent();
            Loaded += MapViewOnLoaded;
            InitMapBoundsInputs();
            LoadPdfMap(Path.Combine(BackgroundDir, DefaultMapFile), true);
            Dispatcher.InvokeAsync(ResizeMapToMainWindow, Threading.DispatcherPriority.Loaded);
        }

        private void InitMapBoundsInputs() {
            LatTopCardinalBox.ItemsSource = new[] { "N", "S" };
            LatBottomCardinalBox.ItemsSource = new[] { "N", "S" };
            LonLeftCardinalBox.ItemsSource = new[] { "E", "W" };
            LonRightCardinalBox.ItemsSource = new[] { "E", "W" };

            _isUpdating = true;
            try {
                ShowBound(LatTop, LatTopDegBox, LatTopMinBox, LatTopCardinalBox);
                ShowBound(LatBottom, LatBottomDegBox, LatBottomMinBox, LatBottomCardinalBox);
                ShowBound(LonLeft, LonLeftDegBox, LonLeftMinBox, LonLeftCardinalBox);
                ShowBound(LonRight, LonRightDegBox, LonRightMinBox, LonRightCardinalBox);
            }
            finally {
                _isUpdating = false;
            }
        }

        private void ShowBound(double value, Wpf.Ui.Controls.NumberBox degBox,
                                      Wpf.Ui.Controls.NumberBox minBox, System.Windows.Controls.ComboBox cardinalBox) {
            double abs = Math.Abs(value);
            int deg = (int)Math.Floor(abs);
            double min = Math.Round((abs - deg) * 60.0, 4);

            if (min >= 60) {
                deg++;
                min = 0;
            }
            degBox.Value = deg;
            minBox.Value = min;
            cardinalBox.SelectedIndex = value < 0 ? 1 : 0;
        }

        private void LoadPdfMap(string pdfPath, bool IsDefaultMap = false, bool resetMargins = true) {
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

            //InitCalibrationEdges(!IsDefaultMap);
            InitCalibrationEdges(false, resetMargins);
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

        private void InitCalibrationEdges(bool IsEdgesVisible, bool ResetMargins = true) {
            const double margin = 100;
            double w = MapImage.Width;
            double h = MapImage.Height;

            _edgesVisible = IsEdgesVisible;

            ToggleEdgesButton.Appearance = _edgesVisible ? Wpf.Ui.Controls.ControlAppearance.Info
                                                         : Wpf.Ui.Controls.ControlAppearance.Secondary;

            TogglePointsTabButton.Appearance = _edgesVisible ? Wpf.Ui.Controls.ControlAppearance.Info
                                                             : Wpf.Ui.Controls.ControlAppearance.Secondary;

            ToggleCourseTabButton.Appearance = _edgesVisible ? Wpf.Ui.Controls.ControlAppearance.Info
                                                             : Wpf.Ui.Controls.ControlAppearance.Secondary;

            var isVisible = _edgesVisible ? Visibility.Visible : Visibility.Collapsed;
            LeftEdge.Visibility = isVisible;
            RightEdge.Visibility = isVisible;
            TopEdge.Visibility = isVisible;
            BottomEdge.Visibility = isVisible;
            BorderValueTab.Visibility = isVisible;
            PositionValuesTab.Visibility = isVisible;
            CourseValuesTab.Visibility = isVisible;

            if (ResetMargins) {
                MarginLeft = 0;
                MarginRight = 0;
                MarginTop = 0;
                MarginBottom = 0;
            }

#pragma warning disable CS0162 // Wykryto nieosiągalny kod
            if (MarginLeft > 0)
                LeftEdge.X1 = LeftEdge.X2 = MarginLeft;
            else
                LeftEdge.X1 = LeftEdge.X2 = margin;
            LeftEdge.Y1 = 0;
            LeftEdge.Y2 = h;

            if (MarginRight > 0 && MarginRight < MapImage.Width)
                RightEdge.X1 = RightEdge.X2 = MarginRight;
            else
                RightEdge.X1 = RightEdge.X2 = w - margin;
            RightEdge.Y1 = 0;
            RightEdge.Y2 = h;

            if (MarginTop > 0)
                TopEdge.Y1 = TopEdge.Y2 = MarginTop;
            else
                TopEdge.Y1 = TopEdge.Y2 = margin;
            TopEdge.X1 = 0;
            TopEdge.X2 = w;

            if (MarginBottom > 0 && MarginBottom < MapImage.Height)
                BottomEdge.Y1 = BottomEdge.Y2 = MarginBottom;
            else
                BottomEdge.Y1 = BottomEdge.Y2 = h - margin;
            BottomEdge.X1 = 0;
            BottomEdge.X2 = w;
#pragma warning restore CS0162 // Wykryto nieosiągalny kod

            UpdateMarginInputsFromEdges();
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
                _activeEdge = GetEdgeNearPoint(pos, 20);
                if (_activeEdge != null) {
                    MapCanvas.CaptureMouse();
                    //Debug.WriteLine($"Margins: L:{LeftEdge.X1}; R:{RightEdge.X1}; T:{TopEdge.Y1}; B:{BottomEdge.Y1};");
                }
                return;
            }
            ClearSelected();
            if (_isCourseMode) {
                SetCourseLine(mouse);
                return;
            }
            if (_isGuideLineMode) {
                SetGuideLine(mouse);
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
                ApplyPositionSelection(null);
            }
            if (_selectedCourseLine != null) {
                ApplyCourseSelection(null);
            }
            if (_selectedGuideLine != null) {
                if (_selectedGuideLine.Line != null) {
                    _selectedGuideLine.Line.Stroke = Brushes.Gold;
                }
                _selectedGuideLine = null;
                DeleteSelectedButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            }
        }

        private bool MarkObject(MouseButtonEventArgs mouse) {
            Position? markPos = GetMarkPositionFromSource(mouse.OriginalSource);
            if (markPos != null) {
                ApplyPositionSelection(markPos);
                return true;
            }
            CourseLine? markCourse = GetCourseFromSource(mouse.OriginalSource);
            if (markCourse != null) {
                ApplyCourseSelection(markCourse);
                return true;
            }
            GuideLine? markGuide = GetGuideFromSource(mouse.OriginalSource);
            if (markGuide != null && markGuide.Line != null) {
                markGuide.Line.Stroke = Brushes.OrangeRed;
                _selectedGuideLine = markGuide;
                DeleteSelectedButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Danger;
                return true;
            }
            return false;
        }

        private void MapCanvas_MouseMove(object sender, MouseEventArgs mouse) {
            if (_activeEdge != null && _edgesVisible) {
                Point pos = mouse.GetPosition(MapCanvas);

                if (_activeEdge == LeftEdge)
                    LeftEdge.X1 = LeftEdge.X2 = Clamp(pos.X, 0, RightEdge.X1);
                else if (_activeEdge == RightEdge)
                    RightEdge.X1 = RightEdge.X2 = Clamp(pos.X, LeftEdge.X1, MapContentWidth);
                else if (_activeEdge == TopEdge)
                    TopEdge.Y1 = TopEdge.Y2 = Clamp(pos.Y, 0, BottomEdge.Y1);
                else
                    BottomEdge.Y1 = BottomEdge.Y2 = Clamp(pos.Y, TopEdge.Y1, MapContentHeight);

                UpdateMarginInputsFromEdges();
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

        private void MapMargin_LostFocus(object sender, RoutedEventArgs e) {
            UpdateMarginInputsFromEdges();
        }

        private void MapBoundsDegMin_TextChanged(object sender, TextChangedEventArgs e) {
            UpdateMapBoundsFromInputs();
        }

        private void MapBoundsCardinal_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            UpdateMapBoundsFromInputs();
        }

        // współrzędne mieszczą się w granicach mapy
        public bool IsWithinMapBounds(double lat, double lon)
            => lat >= LatBottom && lat <= LatTop && lon >= LonLeft && lon <= LonRight;

        public bool TryInsertPoint(double lat, double lon) {
            if (MapImage.Source == null || MapCanvas.Width <= 0 || MapCanvas.Height <= 0)
                return false;
            if (!IsWithinMapBounds(lat, lon))
                return false;

            var (x, y) = GeoToPixelCalibrated(lat, lon);

            var mark = new PositionMark(30);
            Canvas.SetLeft(mark, x - MarkSize / 2);
            Canvas.SetTop(mark, y - MarkSize / 2);
            MapCanvas.Children.Add(mark);

            var markPos = new Position {
                Id = _nextIdPosition,
                X = x,
                Y = y,
                Lat = lat,
                Lon = lon,
                Date = _defaultPointDate,
                Label = $"P{_nextIdPosition}",
                Mark = mark
            };

            _positions.Add(markPos);
            _nextIdPosition++;
            mark.Tag = markPos;
            AttachPositionLeader(markPos);
            RefreshPositionList();
            return true;
        }

        // Odwrotność PixelToGeoCalibrated: współrzędne geograficzne -> piksel mapy
        private (double x, double y) GeoToPixelCalibrated(double lat, double lon) {
            double left = LeftEdge.X1;
            double right = RightEdge.X1;
            double top = TopEdge.Y1;
            double bottom = BottomEdge.Y1;

            double xn = (lon - LonLeft) / (LonRight - LonLeft);
            double x = left + xn * (right - left);

            double mTop = Math.Log(Math.Tan(Math.PI / 4 + DegToRad(LatTop) / 2));
            double mBot = Math.Log(Math.Tan(Math.PI / 4 + DegToRad(LatBottom) / 2));
            double my = Math.Log(Math.Tan(Math.PI / 4 + DegToRad(lat) / 2));
            double yn = (mTop - my) / (mTop - mBot);
            double y = top + yn * (bottom - top);

            return (x, y);
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

        private Line? GetEdgeNearPoint(Point pos, Double tolerance = 8) {
            if (Math.Abs(pos.X - LeftEdge.X1) < tolerance) return LeftEdge;
            if (Math.Abs(pos.X - RightEdge.X1) < tolerance) return RightEdge;
            if (Math.Abs(pos.Y - TopEdge.Y1) < tolerance) return TopEdge;
            if (Math.Abs(pos.Y - BottomEdge.Y1) < tolerance) return BottomEdge;

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
                Date = _defaultPointDate,
                Label = $"P{_nextIdPosition}",
                Mark = mark
            };

            _positions.Add(markPos);
            _nextIdPosition++;
            mark.Tag = markPos;
            AttachPositionLeader(markPos);
            RefreshPositionList();
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
                    double? KdD = null;
                    double? distance = null;

                    if (_isTeacherMode) {
                        double dx = _currentLine.X2 - _currentLine.X1;
                        double dy = _currentLine.Y2 - _currentLine.Y1;

                        // kierunek kompasowy wektora, północ = góra mapy
                        double bearing = RadToDeg(Math.Atan2(dx, -dy));
                        if (bearing < 0)
                            bearing += 360;

                        KdD = Math.Round(bearing, 1);

                        // dystans z proporcji mapy: piksele -> geo -> loksodroma Nm (merkator)
                        var (lat1, lon1) = PixelToGeoCalibrated(_currentLine.X1, _currentLine.Y1);
                        var (lat2, lon2) = PixelToGeoCalibrated(_currentLine.X2, _currentLine.Y2);
                        double phi1 = DegToRad(lat1);
                        double phi2 = DegToRad(lat2);
                        double dPhi = phi2 - phi1;
                        double dLambda = DegToRad(lon2 - lon1);
                        double dPsi = Math.Log(Math.Tan(Math.PI / 4 + phi2 / 2) / Math.Tan(Math.PI / 4 + phi1 / 2));
                        double q = Math.Abs(dPsi) > 1e-12 ? dPhi / dPsi : Math.Cos(phi1);
                        distance = Math.Round(Math.Sqrt(dPhi * dPhi + q * q * dLambda * dLambda) * 3440.065, 1);
                    }

                    var course = new CourseLine {
                        Id = _nextIdCourse,
                        StartPosition = _startPosition,
                        StartX = _currentLine.X1,
                        StartY = _currentLine.Y1,
                        EndX = _currentLine.X2,
                        EndY = _currentLine.Y2,
                        CourseOverGround = KdD,
                        Distance = distance,
                        Line = _currentLine
                    };
                    if (markPos != null) {
                        course.EndPosition = markPos;
                    }
                    _currentLine.Tag = course;
                    _courseLines.Add(course);
                    _nextIdCourse++;
                    _currentLine.IsHitTestVisible = true;
                    UpdateCourseLabels(course);
                    RefreshCourseList();
                }
                SetCourseButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
                _isCourseMode = false;
                _currentLine = null;
                _startPosition = null;
                _courseDirectionLocked = false;
            }
        }

        /////////////////////////////////////////

        private void SetGuideLine(MouseButtonEventArgs mouse) {
            var markPos = GetMarkPositionFromSource(mouse.OriginalSource);
            if (!_isDrawingLine) {
                Point start = mouse.GetPosition(MapCanvas);
                if (markPos != null) {
                    start = new Point(markPos.X, markPos.Y);
                }

                _courseDirectionLocked = false;
                foreach (var triangle in _navTriangles) {
                    if (triangle.Visibility == Visibility.Visible &&
                        triangle.IsAttachedToPoint(start, out Vector dir)) {
                        _courseDirection = dir;
                        _courseDirectionLocked = true;
                        break;
                    }
                }

                _isDrawingLine = true;
                _currentLine = new Line {
                    X1 = start.X,
                    Y1 = start.Y,
                    X2 = start.X,
                    Y2 = start.Y,
                    Stroke = Brushes.Gold,
                    StrokeThickness = 4,
                    IsHitTestVisible = false
                };
                MapCanvas.Children.Add(_currentLine);
            }
            else {
                _isDrawingLine = false;
                if (_currentLine != null) {
                    Point end = mouse.GetPosition(MapCanvas);

                    if (_courseDirectionLocked) {
                        end = ProjectOntoCourseDirection(_currentLine, end);
                    }
                    else if (markPos != null) {
                        end = new Point(markPos.X, markPos.Y);
                    }

                    _currentLine.X2 = end.X;
                    _currentLine.Y2 = end.Y;

                    var guide = new GuideLine {
                        Id = _nextIdGuide,
                        StartX = _currentLine.X1,
                        StartY = _currentLine.Y1,
                        EndX = _currentLine.X2,
                        EndY = _currentLine.Y2,
                        Line = _currentLine
                    };
                    _currentLine.Tag = guide;
                    _guideLines.Add(guide);
                    _nextIdGuide++;
                    _currentLine.IsHitTestVisible = true;
                }
                SetGuideLineButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
                _isGuideLineMode = false;
                _currentLine = null;
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

            BorderValueTab.Visibility = isVisible;
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
        private void TogglePointsTabOnClick(object sender, RoutedEventArgs e) {
            TogglePointsTab();
        }

        private void TogglePointsTab() {
            bool isVisible = PositionValuesTab.Visibility != Visibility.Visible;
            PositionValuesTab.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            TogglePointsTabButton.Appearance = isVisible ? Wpf.Ui.Controls.ControlAppearance.Info
                                                         : Wpf.Ui.Controls.ControlAppearance.Secondary;
        }

        private void ToggleCourseTabOnClick(object sender, RoutedEventArgs e) {
            ToggleCourseTab();
        }

        private void ToggleCourseTab() {
            bool isVisible = CourseValuesTab.Visibility != Visibility.Visible;
            CourseValuesTab.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            ToggleCourseTabButton.Appearance = isVisible ? Wpf.Ui.Controls.ControlAppearance.Info
                                                         : Wpf.Ui.Controls.ControlAppearance.Secondary;
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
            if (_isGuideLineMode)
                SetGuideLineButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            _isCourseMode = false;
            _isGuideLineMode = false;
            SetMarkButton.Appearance = _isMarkMode ? Wpf.Ui.Controls.ControlAppearance.Info
                                                   : Wpf.Ui.Controls.ControlAppearance.Secondary;
            ClearSelected();
        }
        private void SetCourseOnClick(object sender, RoutedEventArgs e) {
            _isCourseMode = !_isCourseMode;
            if (_isMarkMode)
                SetMarkButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            if (_isGuideLineMode)
                SetGuideLineButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            _isMarkMode = false;
            _isGuideLineMode = false;
            SetCourseButton.Appearance = _isCourseMode ? Wpf.Ui.Controls.ControlAppearance.Info
                                                       : Wpf.Ui.Controls.ControlAppearance.Secondary;
            ClearSelected();
        }
        private void SetGuideOnClick(object sender, RoutedEventArgs e) {
            _isGuideLineMode = !_isGuideLineMode;
            if (_isMarkMode)
                SetMarkButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            if (_isCourseMode)
                SetCourseButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            _isMarkMode = false;
            _isCourseMode = false;
            SetGuideLineButton.Appearance = _isGuideLineMode ? Wpf.Ui.Controls.ControlAppearance.Info
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

        private void SaveTaskOnClick(object sender, RoutedEventArgs e) {
            SaveTask();
        }
        private void LoadTaskOnClick(object sender, RoutedEventArgs e) {
            LoadTask();
        }
        private void MapViewOnLoaded(object sender, RoutedEventArgs e) {
            if (FrameVisibilityChanged != null)
                return;

            foreach (var child in ControlPanel.Children)
                if (child is Wpf.Ui.Controls.Button button && button.Tag is string)
                    button.Visibility = Visibility.Collapsed;
        }

        // ramka z Tag przycisku
        private void ToggleFrameOnClick(object sender, RoutedEventArgs e) {
            var button = (Wpf.Ui.Controls.Button)sender;
            var frame = Enum.Parse<MapFrame>((string)button.Tag);

            bool isVisible = !_visibleFrames.Remove(frame);
            if (isVisible)
                _visibleFrames.Add(frame);

            button.Appearance = isVisible ? Wpf.Ui.Controls.ControlAppearance.Info
                                          : Wpf.Ui.Controls.ControlAppearance.Secondary;
            FrameVisibilityChanged?.Invoke(this, frame);
        }

        // JSON
        private NavigationData CollectNavigationData() {
            var navigation = NavigationData.LoadDefaultValues();
            NavigationDataRequested?.Invoke(this, navigation);
            return navigation;
        }

        private TaskMeta CollectTaskMeta() {
            var meta = new TaskMeta();
            TaskMetaRequested?.Invoke(this, meta);
            return meta;
        }

        private static TaskMeta ReadTaskMeta(JsonElement task) {
            if (!task.TryGetProperty("meta", out var meta) || meta.ValueKind != JsonValueKind.Object)
                return new TaskMeta();

            return new TaskMeta {
                Title = ReadString(meta, "title"),
                Desc = ReadString(meta, "desc"),
                Author = ReadString(meta, "author"),
                ContentMd = ReadString(meta, "content_md")
            };
        }

        private static NavigationData ReadNavigationData(JsonElement task) {
            var navigation = NavigationData.LoadDefaultValues();

            if (!task.TryGetProperty("navigation", out var navigationElement))
                return navigation;

            if (navigationElement.TryGetProperty("timezone", out var timezoneElement) &&
                timezoneElement.ValueKind == JsonValueKind.Object) {

                string name = ReadString(timezoneElement, "name");
                string offset = ReadString(timezoneElement, "offset");

                if (name.Length > 0)
                    navigation.TimeZone.Name = name;
                if (offset.Length > 0)
                    navigation.TimeZone.Offset = offset;
            }

            if (navigationElement.TryGetProperty("magnetic_variation", out var variationElement) &&
                variationElement.ValueKind == JsonValueKind.Object) {

                navigation.Variation = new MagneticVariation {
                    BaseDeg = ReadNumber(variationElement, "base_deg"),
                    BaseYear = ReadYear(variationElement),
                    AnnualChangeDeg = ReadNumber(variationElement, "annual_change_deg")
                };
            }

            if (navigationElement.TryGetProperty("magnetic_variation_list", out var variationListElement) &&
                variationListElement.ValueKind == JsonValueKind.Array) {

                foreach (var entry in variationListElement.EnumerateArray())
                    navigation.VariationList.Add(new LocalMagneticVariation {
                        BaseDeg = ReadNumber(entry, "base_deg"),
                        BaseYear = ReadYear(entry),
                        AnnualChangeDeg = ReadNumber(entry, "annual_change_deg"),
                        Lat = ReadNumber(entry, "lat"),
                        Lon = ReadNumber(entry, "lon"),
                        RangeNm = ReadNumber(entry, "range_nm")
                    });
            }

            if (navigationElement.TryGetProperty("compass_deviation_table", out var deviationElement) &&
                deviationElement.ValueKind == JsonValueKind.Array) {

                var deviationTable = new List<Deviation>();
                foreach (var entry in deviationElement.EnumerateArray())
                    deviationTable.Add(new Deviation {
                        HeadingDeg = (int)Math.Round(ReadNumber(entry, "heading_deg")),
                        DeviationDeg = ReadNumber(entry, "deviation_deg")
                    });

                if (deviationTable.Count > 0)
                    navigation.DeviationTable = deviationTable;
            }

            if (navigationElement.TryGetProperty("leeway", out var leewayElement) &&
                leewayElement.ValueKind == JsonValueKind.Object) {

                foreach (var entry in navigation.Leeway) {
                    if (!leewayElement.TryGetProperty(entry.Key, out var pointOfSail) ||
                        pointOfSail.ValueKind != JsonValueKind.Object)
                        continue;

                    if (pointOfSail.TryGetProperty("range", out var range) &&
                        range.ValueKind == JsonValueKind.Object) {
                        entry.MinDeg = ReadNumber(range, "min_deg");
                        entry.MaxDeg = ReadNumber(range, "max_deg");
                    }
                    entry.ValueDeg = ReadNumber(pointOfSail, "value_deg");
                }
            }

            return navigation;
        }

        // nazwa pliku z tytułu, bez tytułu - pierwsza wolna zad_01.json
        private static string BuildTaskFileName(string title) {
            string name = title.Trim();

            foreach (char invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid, '_');

            name = string.Join(' ', name.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries));
            name = name.TrimEnd('.', ' ');

            if (name.Length > MaxTaskFileNameLength)
                name = name[..MaxTaskFileNameLength].TrimEnd();

            return name.Length > 0 ? $"{name}.json" : NextFreeTaskFileName();
        }

        private static string NextFreeTaskFileName() {
            int index = 1;
            string name;
            do {
                name = $"zad_{index:00}.json";
                index++;
            } while (File.Exists(Path.Combine(JsonsDir, name)));

            return name;
        }

        private void SaveTask() {
            TaskMeta meta = CollectTaskMeta();
            NavigationData navigation = CollectNavigationData();

            // punkty do solution
            List<object> trk = new();
            for (int i = 0; i < _positions.Count; i++) {
                Position pos = _positions[i];

                // czas + data
                int hh = 0, mm = 0;
                if (!string.IsNullOrWhiteSpace(pos.Time)) {
                    string timeText = pos.Time.Trim().PadLeft(4, '0');
                    int.TryParse(timeText.Substring(0, 2), out hh);
                    int.TryParse(timeText.Substring(2, 2), out mm);
                }
                DateTime date = pos.Date.Date;
                DateTime time = new(date.Year, date.Month, date.Day, hh % 24, mm % 60, 0);

                // kurs wychodzący z punktu
                CourseLine? course = _courseLines.FirstOrDefault(line => line.StartPosition == pos);

                trk.Add(new {
                    id = $"trk-{i + 1}",
                    time = time.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture),
                    position = new {
                        lat = Math.Round(pos.Lat, 4),
                        lon = Math.Round(pos.Lon, 4),
                        position_txt = $"{DegToDegMin(pos.Lat, true)} {DegToDegMin(pos.Lon, false)}"
                    },
                    distance_nm = course?.Distance ?? 0,
                    log_nm = pos.Log,
                    course_compass_deg = course?.CourseCompas ?? 0,
                    course_true_deg = 0.0,                                            // TODO dodać do formatki
                    course_over_ground_deg = course?.CourseOverGround ?? 0,
                    speed_kn = course?.Velocity ?? 0
                });
            }

            List<object> deviationTable = [];
            foreach (var entry in navigation.DeviationTable) {
                deviationTable.Add(new {
                    heading_deg = entry.HeadingDeg,
                    deviation_deg = entry.DeviationDeg
                });
            }

            Dictionary<string, object> leeway = [];
            foreach (var entry in navigation.Leeway) {
                leeway[entry.Key] = new {
                    range = new {
                        min_deg = entry.MinDeg,
                        max_deg = entry.MaxDeg
                    },
                    value_deg = entry.ValueDeg
                };
            }

            Dictionary<string, object> navigationSection = new() {
                ["timezone"] = new {
                    name = navigation.TimeZone.Name,
                    offset = navigation.TimeZone.Offset
                },
                ["magnetic_variation"] = new {
                    base_deg = Math.Round(navigation.Variation.BaseDeg, 4),
                    base_year = navigation.Variation.BaseYear,
                    annual_change_deg = Math.Round(navigation.Variation.AnnualChangeDeg, 4)
                }
            };

            if (navigation.VariationList.Count > 0) {
                List<object> variationList = [];
                foreach (var entry in navigation.VariationList) {
                    variationList.Add(new {
                        base_deg = Math.Round(entry.BaseDeg, 4),
                        base_year = entry.BaseYear,
                        annual_change_deg = Math.Round(entry.AnnualChangeDeg, 4),
                        lat = Math.Round(entry.Lat, 4),
                        lon = Math.Round(entry.Lon, 4),
                        range_nm = Math.Round(entry.RangeNm, 4)
                    });
                }

                navigationSection["magnetic_variation_list"] = variationList;
            }

            navigationSection["compass_deviation_table"] = deviationTable;
            navigationSection["leeway"] = leeway;

            // Plik Json'a
            var task = new {
                task = new {
                    meta = new {
                        title = meta.Title,
                        desc = meta.Desc,
                        author = meta.Author,
                        content_md = meta.ContentMd
                    },
                    navigation = navigationSection,
                    chart = new {
                        file_name = Path.GetFileName(_currentMapPath),
                        bounds = new {
                            north_lat_deg = Math.Round(LatTop, 4),
                            south_lat_deg = Math.Round(LatBottom, 4),
                            west_lon_deg = Math.Round(LonLeft, 4),
                            east_lon_deg = Math.Round(LonRight, 4)
                        },
                        margins = new {
                            top = Math.Round(MarginTop, 4),
                            bottom = Math.Round(MarginBottom, 4),
                            left = Math.Round(MarginLeft, 4),
                            right = Math.Round(MarginRight, 4)
                        }
                    }
                },
                solution = new {
                    trk = trk,
                    comment = ""
                }
            };

            try {
                Directory.CreateDirectory(JsonsDir);

                // zapis/nadpis pliku
                string path = Path.Combine(JsonsDir, BuildTaskFileName(meta.Title));
                string fileName = Path.GetFileName(path);
                bool isOverwrite = File.Exists(path);

                if (isOverwrite) {
                    var answer = MessageBox.Show(
                        $"Zadanie \"{fileName}\" już istnieje.\nCzy nadpisać zapisany plik?",
                        "Zapis zadania",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (answer != MessageBoxResult.Yes)
                        return;
                }

                string json = JsonSerializer.Serialize(task, new JsonSerializerOptions {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                File.WriteAllText(path, json);

                MessageBox.Show(
                    isOverwrite ? $"Nadpisano zadanie \"{fileName}\"."
                                : $"Zapisano zadanie jako \"{fileName}\".",
                    "Zapis zadania",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex) {
                MessageBox.Show(
                    $"Nie udało się zapisać zadania:\n{ex.Message}",
                    "Błąd zapisu zadania",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadTask() {
            var dialog = new TaskSelectionWindow(JsonsDir) {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true || dialog.SelectedTaskPath == null)
                return;

            string mapFile, taskPath, fileName;
            double boundNorth, boundSouth, boundWest, boundEast, marginTop, marginBottom, marginLeft, marginRight;
            NavigationData navigation;
            TaskMeta meta;
            var trkPosition = new List<(double lat, double lon, DateTime date, string? time, double? log,
                                        double distance, double kk, double kdd, double speed)>();

            taskPath = dialog.SelectedTaskPath;
            fileName = Path.GetFileName(taskPath);
            try {
                using var jsonFile = JsonDocument.Parse(File.ReadAllText(taskPath));
                var taskElement = jsonFile.RootElement.GetProperty("task");
                var chart = taskElement.GetProperty("chart");

                meta = ReadTaskMeta(taskElement);
                navigation = ReadNavigationData(taskElement);

                mapFile = chart.GetProperty("file_name").GetString() ?? string.Empty;

                var bounds = chart.GetProperty("bounds");
                boundNorth = bounds.GetProperty("north_lat_deg").GetDouble();
                boundSouth = bounds.GetProperty("south_lat_deg").GetDouble();
                boundWest = bounds.GetProperty("west_lon_deg").GetDouble();
                boundEast = bounds.GetProperty("east_lon_deg").GetDouble();

                var margins = chart.GetProperty("margins");
                marginTop = margins.GetProperty("top").GetDouble();
                marginBottom = margins.GetProperty("bottom").GetDouble();
                marginLeft = margins.GetProperty("left").GetDouble();
                marginRight = margins.GetProperty("right").GetDouble();

                if (jsonFile.RootElement.TryGetProperty("solution", out var solution)) {
                    solution.TryGetProperty("trk", out var trk);

                    if (trk.ValueKind == JsonValueKind.Array) {
                        foreach (var trkElement in trk.EnumerateArray()) {
                            var position = trkElement.GetProperty("position");
                            double lat = position.GetProperty("lat").GetDouble();
                            double lon = position.GetProperty("lon").GetDouble();

                            // czas ISO -> data + godzina - string HHMM
                            DateTime date = DateTime.Today;
                            string? time = null;
                            if (trkElement.TryGetProperty("time", out var timeElement)) {
                                if (timeElement.ValueKind == JsonValueKind.String) {
                                    bool isTimeParsed = DateTime.TryParse(
                                        timeElement.GetString(),
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                                        out var timeStamp);

                                    if (isTimeParsed) {
                                        date = timeStamp.Date;
                                        time = timeStamp.ToString("HHmm", CultureInfo.InvariantCulture);
                                    }
                                }
                            }

                            double? log = null;
                            if (trkElement.TryGetProperty("log_nm", out var logElement)) {
                                if (logElement.ValueKind == JsonValueKind.Number)
                                    log = logElement.GetDouble();
                            }

                            double distance = ReadNumber(trkElement, "distance_nm");
                            double kk = ReadNumber(trkElement, "course_compass_deg");
                            double kdd = ReadNumber(trkElement, "course_over_ground_deg");
                            double speed = ReadNumber(trkElement, "speed_kn");

                            trkPosition.Add((lat, lon, date, time, log, distance, kk, kdd, speed));
                        }
                    }
                }

                if (trkPosition.Count > 0)
                    navigation.StartDate = trkPosition[0].date;
            }
            catch (Exception ex) {
                MessageBox.Show(
                    $"Nie udało się odczytać pliku zadania \"{fileName}\":\n{ex.Message}!",
                    "Błąd wczytywania zadania",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Mapa
            string mapPath = Path.Combine(MapsDir, mapFile);
            if (!File.Exists(mapPath)) {
                MessageBox.Show(
                    $"Aby wczytać zadanie: '{fileName}' należy zaimportować mapę: '{mapFile}'.",
                    "Brak wymaganej mapy",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            try {
                ClearMap();
                LatTop = boundNorth;
                LatBottom = boundSouth;
                LonLeft = boundWest;
                LonRight = boundEast;
                MarginTop = marginTop;
                MarginBottom = marginBottom;
                MarginLeft = marginLeft;
                MarginRight = marginRight;

                LoadPdfMap(mapPath, resetMargins: false);
                ResizeMapToMainWindow();
                InitMapBoundsInputs();

                // opis zadania, tabele dewiacji i wiatrów do paneli bocznych
                TaskMetaLoaded?.Invoke(this, meta);
                NavigationDataLoaded?.Invoke(this, navigation);
            }
            catch (Exception ex) {
                MessageBox.Show(
                    $"Nie udało się wczytać trasy \"{mapFile}\":\n{ex.Message}",
                    "Błąd wczytywania mapy",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // wstawianie punktów
            List<Position?> placedPositions = new List<Position?>();
            try {
                foreach (var x in trkPosition) {
                    if (TryInsertPoint(x.lat, x.lon)) {
                        Position pos = _positions[^1];
                        pos.Date = x.date;
                        pos.Time = x.time;
                        pos.Log = x.log;
                        UpdatePositionLeader(pos);
                        placedPositions.Add(pos);
                    }
                    else {
                        placedPositions.Add(null);
                    }
                }
            }
            catch (Exception ex) {
                MessageBox.Show(
                    $"Nie udało się wczytać punktów z zadania \"{fileName}\":\n{ex.Message}",
                    "Błąd wczytywania mapy",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // wstawianie lini kursów
            try {
                for (int i = 0; i < trkPosition.Count; i++) {
                    Position? start = placedPositions[i];
                    double distance = trkPosition[i].distance;
                    if (start == null || distance <= 0)
                        continue;

                    // koniec loksodromy z punktu startu wg kierunku KdD i dystansu
                    double bearing = DegToRad(trkPosition[i].kdd);
                    double angDist = distance / 3440.065;                // dystans kątowy -  rad
                    double phi1 = DegToRad(start.Lat);
                    double dPhi = angDist * Math.Cos(bearing);
                    double phi2 = phi1 + dPhi;
                    double dPsi = Math.Log(Math.Tan(Math.PI / 4 + phi2 / 2) / Math.Tan(Math.PI / 4 + phi1 / 2));
                    double q = Math.Abs(dPsi) > 1e-12 ? dPhi / dPsi : Math.Cos(phi1);
                    double lat2 = RadToDeg(phi2);
                    double lon2 = start.Lon + RadToDeg(angDist * Math.Sin(bearing) / q);

                    var (endX, endY) = GeoToPixelCalibrated(lat2, lon2);

                    var line = new Line {
                        X1 = start.X,
                        Y1 = start.Y,
                        X2 = endX,
                        Y2 = endY,
                        Stroke = Brushes.Blue,
                        StrokeThickness = 4,
                        IsHitTestVisible = true
                    };
                    MapCanvas.Children.Add(line);

                    var course = new CourseLine {
                        Id = _nextIdCourse,
                        StartPosition = start,
                        StartX = start.X,
                        StartY = start.Y,
                        EndX = endX,
                        EndY = endY,
                        CourseCompas = trkPosition[i].kk,
                        CourseOverGround = trkPosition[i].kdd,
                        Distance = distance,
                        Velocity = trkPosition[i].speed,
                        Line = line
                    };
                    line.Tag = course;
                    _courseLines.Add(course);
                    _nextIdCourse++;
                    UpdateCourseLabels(course);
                }
                RefreshCourseList();
            }
            catch (Exception ex) {
                MessageBox.Show(
                    $"Nie udało się wczytać trasy z zadania \"{fileName}\":\n{ex.Message}",
                    "Błąd wczytywania mapy",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
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

        private void MapMargin_TextChanged(object sender, TextChangedEventArgs e) {
            if (_isUpdating)
                return;
            if (sender is not Wpf.Ui.Controls.NumberBox box)
                return;
            if (!TryParseBound(box.Text, out double value))
                return;

            _isUpdating = true;
            try {
                if (box == MarginLeftBox) {
                    MarginLeft = Clamp(value, 0, Math.Min(MapContentWidth, MarginRight));
                    LeftEdge.X1 = LeftEdge.X2 = MarginLeft;
                }
                else if (box == MarginRightBox) {
                    MarginRight = Clamp(value, MarginLeft, MapContentWidth);
                    RightEdge.X1 = RightEdge.X2 = MarginRight;
                }
                else if (box == MarginTopBox) {
                    MarginTop = Clamp(value, 0, Math.Min(MapContentHeight, MarginBottom));
                    TopEdge.Y1 = TopEdge.Y2 = MarginTop;
                }
                else if (box == MarginBottomBox) {
                    MarginBottom = Clamp(value, MarginTop, MapContentHeight);
                    BottomEdge.Y1 = BottomEdge.Y2 = MarginBottom;
                }
            }
            finally {
                _isUpdating = false;
            }
        }

        private void UpdateMapBoundsFromInputs() {
            if (_isUpdating)
                return;
            _isUpdating = true;
            try {
                if (TryReadBound(LatTopDegBox, LatTopMinBox, LatTopCardinalBox, 90, out double latTop)
                    && latTop != LatBottom)
                    LatTop = latTop;

                if (TryReadBound(LatBottomDegBox, LatBottomMinBox, LatBottomCardinalBox, 90, out double latBottom)
                    && latBottom != LatTop)
                    LatBottom = latBottom;

                if (TryReadBound(LonLeftDegBox, LonLeftMinBox, LonLeftCardinalBox, 180, out double lonLeft)
                    && lonLeft != LonRight)
                    LonLeft = lonLeft;

                if (TryReadBound(LonRightDegBox, LonRightMinBox, LonRightCardinalBox, 180, out double lonRight)
                    && lonRight != LonLeft)
                    LonRight = lonRight;
            }
            finally {
                _isUpdating = false;
            }
        }

        private bool TryReadBound(Wpf.Ui.Controls.NumberBox degBox, Wpf.Ui.Controls.NumberBox minBox,
                                  System.Windows.Controls.ComboBox cardinalBox, double maxDegrees, out double value) {
            value = 0;
            bool hasDeg = TryParseBound(degBox.Text, out double deg);
            bool hasMin = TryParseBound(minBox.Text, out double min);
            if (!hasDeg && !hasMin)
                return false;

            double sign = cardinalBox.SelectedIndex == 1 ? -1.0 : 1.0;
            double d = sign * ((hasDeg ? deg : 0) + (hasMin ? min : 0) / 60.0);
            value = Math.Clamp(Math.Round(d, 6), -maxDegrees, maxDegrees);
            return true;
        }

        private bool TryParseBound(string? s, out double value) {
            s = (s ?? string.Empty).Trim().Replace(',', '.');
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private void ClearMap() {
            foreach (var pos in _positions) {
                if (pos.Mark != null) {
                    pos.Mark.Tag = null;
                    MapCanvas.Children.Remove(pos.Mark);
                }
                RemovePositionLeader(pos);
            }
            foreach (var course in _courseLines) {
                if (course.Line != null) {
                    course.Line.Tag = null;
                    MapCanvas.Children.Remove(course.Line);
                }
                RemoveCourseLabels(course);
            }
            foreach (var guide in _guideLines) {
                if (guide.Line != null) {
                    guide.Line.Tag = null;
                    MapCanvas.Children.Remove(guide.Line);
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
            _guideLines.Clear();
            _navTriangles.Clear();
            _navDividers.Clear();

            _nextIdPosition = 0;
            _nextIdCourse = 0;
            _nextIdGuide = 0;

            _selectedPosition = null;
            _selectedCourseLine = null;
            _selectedGuideLine = null;
            _currentLine = null;
            _startPosition = null;
            _isDrawingLine = false;
            _courseDirectionLocked = false;
            _isMarkMode = false;
            _isCourseMode = false;
            _isGuideLineMode = false;

            RefreshPositionList();
            ShowPositionDetails(null);
            RefreshCourseList();
            ShowCourseDetails(null);

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
        private static double ContentExtent(double extent)
            => double.IsNaN(extent) || extent <= 0 ? double.PositiveInfinity : extent;

        private static int ReadYear(JsonElement element) {
            int year = (int)Math.Round(ReadNumber(element, "base_year"));
            return year > 0 ? year : DateTime.Today.Year;
        }

        private static string ReadString(JsonElement element, string name) {
            if (element.TryGetProperty(name, out var value)) {
                if (value.ValueKind == JsonValueKind.String)
                    return value.GetString() ?? string.Empty;
            }

            return string.Empty;
        }

        private static double ReadNumber(JsonElement element, string name) {
            if (element.TryGetProperty(name, out var value)) {
                if (value.ValueKind == JsonValueKind.Number)
                    return value.GetDouble();
            }

            return 0;
        }


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
        private GuideLine? GetGuideFromSource(object source) {
            DependencyObject? current = source as DependencyObject;
            while (current != null) {
                if (current is FrameworkElement fe && fe.Tag is GuideLine guide)
                    return guide;
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
                RemovePositionLeader(_selectedPosition);
                List<CourseLine>? toRemove = _courseLines.Where(course => course.StartPosition == _selectedPosition ||
                                             course.EndPosition == _selectedPosition).ToList(); // TODO ten or do skasowania jak przestane wiązać endPosition
                foreach (CourseLine course in toRemove) {
                    if (course.Line != null)
                        MapCanvas.Children.Remove(course.Line);
                    RemoveCourseLabels(course);
                    _courseLines.Remove(course);
                }
                _positions.Remove(_selectedPosition);
                _selectedPosition.Mark = null;
                _selectedPosition = null;
                DeleteSelectedButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
                RefreshPositionList();
                ShowPositionDetails(null);
                RefreshCourseList();
                if (_selectedCourseLine == null)
                    ShowCourseDetails(null);
                return;
            }
            if (_selectedCourseLine != null) {
                if (_selectedCourseLine.Line != null) {
                    MapCanvas.Children.Remove(_selectedCourseLine.Line);
                }
                RemoveCourseLabels(_selectedCourseLine);
                _courseLines.Remove(_selectedCourseLine);
                _selectedCourseLine.Line = null;
                _selectedCourseLine = null;
                DeleteSelectedButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
                RefreshCourseList();
                ShowCourseDetails(null);
                return;
            }
            if (_selectedGuideLine != null) {
                if (_selectedGuideLine.Line != null) {
                    MapCanvas.Children.Remove(_selectedGuideLine.Line);
                }
                _guideLines.Remove(_selectedGuideLine);
                _selectedGuideLine.Line = null;
                _selectedGuideLine = null;
                DeleteSelectedButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
                return;
            }
        }

        // Zakładka punktów pozycji
        private void ApplyPositionSelection(Position? pos) {
            _isSyncingSelection = true;
            try {
                if (_selectedPosition?.Mark != null)
                    _selectedPosition.Mark.Color = Brushes.Blue;
                if (_selectedPosition?.Leader != null)
                    _selectedPosition.Leader.Color = Brushes.Blue;

                _selectedPosition = pos;

                if (pos?.Mark != null)
                    pos.Mark.Color = Brushes.OrangeRed;
                if (pos?.Leader != null)
                    pos.Leader.Color = Brushes.OrangeRed;

                PositionList.SelectedItem = FindListItem(pos);
                DeletePositionButton.IsEnabled = pos != null;
                DeletePositionButton.Appearance = DeletePositionButton.IsEnabled ? Wpf.Ui.Controls.ControlAppearance.Danger
                                                                                 : Wpf.Ui.Controls.ControlAppearance.Secondary;
                DeleteSelectedButton.Appearance = pos != null ? Wpf.Ui.Controls.ControlAppearance.Danger
                                                              : Wpf.Ui.Controls.ControlAppearance.Secondary;
                UpdatePositionMoveButtons();

                ShowPositionDetails(pos);
            }
            finally {
                _isSyncingSelection = false;
            }
        }

        private void RefreshPositionList() {
            _isSyncingSelection = true;
            try {
                PositionList.Items.Clear();
                foreach (var pos in _positions) {
                    PositionList.Items.Add(new ListBoxItem {
                        Content = string.IsNullOrWhiteSpace(pos.Label) ? $"P{pos.Id}" : pos.Label,
                        Tag = pos
                    });
                }

                PositionList.SelectedItem = FindListItem(_selectedPosition);

                DeletePositionButton.IsEnabled = _selectedPosition != null;
                DeletePositionButton.Appearance = DeletePositionButton.IsEnabled ? Wpf.Ui.Controls.ControlAppearance.Danger
                                                                                 : Wpf.Ui.Controls.ControlAppearance.Secondary;
                UpdatePositionMoveButtons();
            }
            finally {
                _isSyncingSelection = false;
            }
        }

        private ListBoxItem? FindListItem(Position? pos) {
            if (pos == null)
                return null;

            foreach (ListBoxItem item in PositionList.Items)
                if (ReferenceEquals(item.Tag, pos))
                    return item;

            return null;
        }

        private void ShowPositionDetails(Position? pos) {
            if (pos == null) {
                PositionDetailPanel.IsEnabled = false;
                PosIdText.Text = string.Empty;
                PosLabelBox.Text = string.Empty;
                PosLatText.Text = string.Empty;
                PosLonText.Text = string.Empty;
                PosDatePicker.SelectedDate = null;
                PosTimeBox.Text = string.Empty;
                PosLogBox.Value = null;

                return;
            }

            PositionDetailPanel.IsEnabled = true;
            PosIdText.Text = pos.Id.ToString();
            PosLabelBox.Text = pos.Label ?? string.Empty;
            PosLatText.Text = DegToDegMin(pos.Lat, isLat: true);
            PosLonText.Text = DegToDegMin(pos.Lon, isLat: false);
            PosDatePicker.SelectedDate = pos.Date;
            PosTimeBox.Text = pos.Time ?? string.Empty;
            PosLogBox.Value = pos.Log;
        }

        private void PositionList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (_isSyncingSelection)
                return;

            var pos = (PositionList.SelectedItem as ListBoxItem)?.Tag as Position;

            if (_selectedCourseLine != null)
                ApplyCourseSelection(null);

            DeselectGuide();
            ApplyPositionSelection(pos);
        }

        private void DeletePositionOnClick(object sender, RoutedEventArgs e) {
            if (_selectedPosition != null)
                RemoveSelectedObject();
        }

        private void MovePositionUpOnClick(object sender, RoutedEventArgs e) {
            MoveSelectedPosition(-1);
        }
        private void MovePositionDownOnClick(object sender, RoutedEventArgs e) {
            MoveSelectedPosition(1);
        }

        // strzałki zmiany ID
        private void MoveSelectedPosition(int direction) {
            if (_selectedPosition == null)
                return;
            int i = _positions.IndexOf(_selectedPosition);
            int j = i + direction;
            if (i < 0 || j < 0 || j >= _positions.Count)
                return;

            Position posA = _positions[i];
            Position posB = _positions[j];

            int idA = posA.Id;
            int idB = posB.Id;
            posA.Id = idB;
            posB.Id = idA;

            // zmiana puste/domy labele
            if (posA.Label == $"P{idA}")
                posA.Label = $"P{posA.Id}";

            if (posB.Label == $"P{idB}")
                posB.Label = $"P{posB.Id}";

            _positions[i] = posB;
            _positions[j] = posA;

            RefreshPositionList();
            _isSyncingSelection = true;

            try {
                ShowPositionDetails(_selectedPosition);
            }
            finally {
                _isSyncingSelection = false;
            }
        }

        // strzałki aktywne ino jak jest kaj przesunąć wybrany punkt
        private void UpdatePositionMoveButtons() {
            int i = _selectedPosition == null ? -1 : _positions.IndexOf(_selectedPosition);

            MovePositionUpButton.IsEnabled = i > 0;
            MovePositionDownButton.IsEnabled = i >= 0 && i < _positions.Count - 1;
        }

        private void DeselectGuide() {
            if (_selectedGuideLine?.Line != null) {
                _selectedGuideLine.Line.Stroke = Brushes.Gold;
                _selectedGuideLine = null;
            }
        }

        private void PosLabel_TextChanged(object sender, TextChangedEventArgs e) {
            if (_isSyncingSelection || _selectedPosition == null)
                return;

            _selectedPosition.Label = PosLabelBox.Text;

            var item = FindListItem(_selectedPosition);
            if (item != null)
                item.Content = string.IsNullOrWhiteSpace(_selectedPosition.Label) ? $"P{_selectedPosition.Id}" : _selectedPosition.Label;
        }

        // HHMM
        private void PosTime_TextChanged(object sender, TextChangedEventArgs e) {
            if (_isSyncingSelection || _selectedPosition == null)
                return;

            string digits = new string(PosTimeBox.Text.Where(char.IsDigit).ToArray());
            if (digits != PosTimeBox.Text) {
                int caret = PosTimeBox.SelectionStart;
                PosTimeBox.Text = digits;                       // odpala handler ponownie
                PosTimeBox.SelectionStart = Math.Min(caret, digits.Length);

                return;
            }
            _selectedPosition.Time = digits.Length == 0 ? "0000" : digits;
            UpdatePositionLeader(_selectedPosition);
        }

        private void PosDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e) {
            if (_isSyncingSelection || _selectedPosition == null)
                return;

            _selectedPosition.Date = PosDatePicker.SelectedDate ?? _selectedPosition.Date;
        }

        private void PosLog_TextChanged(object sender, TextChangedEventArgs e) {
            if (_isSyncingSelection || _selectedPosition == null)
                return;

            _selectedPosition.Log = TryParseBound(PosLogBox.Text, out double v) ? Math.Round(v, 1) : null;
            UpdatePositionLeader(_selectedPosition);
        }

        // Zakładka linii kursów
        private void ApplyCourseSelection(CourseLine? course) {
            _isSyncingSelection = true;
            try {
                var previous = _selectedCourseLine;
                if (previous?.Line != null)
                    previous.Line.Stroke = Brushes.Blue;

                _selectedCourseLine = course;

                if (course?.Line != null)
                    course.Line.Stroke = Brushes.OrangeRed;

                // opisy w kolorze linii
                if (previous != null)
                    UpdateCourseLabels(previous);
                if (course != null)
                    UpdateCourseLabels(course);

                CourseList.SelectedItem = FindCourseListItem(course);
                DeleteCourseButton.IsEnabled = course != null;
                DeleteCourseButton.Appearance = course != null ? Wpf.Ui.Controls.ControlAppearance.Danger
                                                               : Wpf.Ui.Controls.ControlAppearance.Secondary;
                DeleteSelectedButton.Appearance = course != null ? Wpf.Ui.Controls.ControlAppearance.Danger
                                                                 : Wpf.Ui.Controls.ControlAppearance.Secondary;

                ShowCourseDetails(course);
            }
            finally {
                _isSyncingSelection = false;
            }
        }

        // odnośnik punktu - nowy nie ma godziny ani logu, więc startuje schowany
        private void AttachPositionLeader(Position pos) {
            var leader = new PositionLeader(MarkSize) { IsHitTestVisible = false };
            Canvas.SetLeft(leader, pos.X);
            Canvas.SetTop(leader, pos.Y);
            MapCanvas.Children.Add(leader);

            pos.Leader = leader;
        }

        private static void UpdatePositionLeader(Position pos) {
            pos.Leader?.Update(pos.Time, pos.Log);
        }

        private void RemovePositionLeader(Position pos) {
            if (pos.Leader == null)
                return;

            MapCanvas.Children.Remove(pos.Leader);
            pos.Leader = null;
        }

        // opis kursów
        private void UpdateCourseLabels(CourseLine course) {
            if (course.Line is not Line line) {
                RemoveCourseLabels(course);
                return;
            }

            double dx = line.X2 - line.X1;
            double dy = line.Y2 - line.Y1;
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 1) {
                RemoveCourseLabels(course);
                return;
            }

            double midX = (line.X1 + line.X2) / 2;
            double midY = (line.Y1 + line.Y2) / 2;

            // normalna do odcinka, zwrócona ku górze mapy
            double normalX = -dy / length;
            double normalY = dx / length;
            if (normalY > 0) {
                normalX = -normalX;
                normalY = -normalY;
            }

            // tekst równolegle do odcinka
            double angle = RadToDeg(Math.Atan2(dy, dx));
            if (angle > 90)
                angle -= 180;
            else if (angle < -90)
                angle += 180;

            var brush = ReferenceEquals(course, _selectedCourseLine) ? Brushes.OrangeRed : Brushes.Blue;

            course.CourseOverGroundLabel = PlaceCourseLabel(course.CourseOverGroundLabel,
                                                            FormatCourseLabel("KdD", course.CourseOverGround),
                                                            brush, midX, midY, normalX, normalY, angle,
                                                            isAboveLine: true);

            course.CourseCompasLabel = PlaceCourseLabel(course.CourseCompasLabel,
                                                        FormatCourseLabel("KK", course.CourseCompas),
                                                        brush, midX, midY, normalX, normalY, angle,
                                                        isAboveLine: false);
        }

        private System.Windows.Controls.TextBlock? PlaceCourseLabel(System.Windows.Controls.TextBlock? label, string? text,
                                            System.Windows.Media.Brush brush,
                                            double midX, double midY, double normalX, double normalY,
                                            double angle, bool isAboveLine) {
            if (text == null) {
                if (label != null)
                    MapCanvas.Children.Remove(label);

                return null;
            }

            if (label == null) {
                label = new System.Windows.Controls.TextBlock {
                    FontSize = CourseLabelFontSize,
                    FontWeight = FontWeights.SemiBold,
                    IsHitTestVisible = false,
                    RenderTransformOrigin = new Point(0.5, 0.5)
                };
                MapCanvas.Children.Add(label);
            }

            label.Text = text;
            label.Foreground = brush;
            label.RenderTransform = new RotateTransform(angle);
            label.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));

            double side = isAboveLine ? 1 : -1;
            double distance = label.DesiredSize.Height / 2 + CourseLabelOffset;
            double centerX = midX + normalX * distance * side;
            double centerY = midY + normalY * distance * side;

            Canvas.SetLeft(label, centerX - label.DesiredSize.Width / 2);
            Canvas.SetTop(label, centerY - label.DesiredSize.Height / 2);

            return label;
        }

        private static string? FormatCourseLabel(string name, double? value) {
            if (value == null)
                return null;

            return $"{name} {value.Value:000.#}°";
        }

        private void RemoveCourseLabels(CourseLine course) {
            if (course.CourseOverGroundLabel != null) {
                MapCanvas.Children.Remove(course.CourseOverGroundLabel);
                course.CourseOverGroundLabel = null;
            }

            if (course.CourseCompasLabel != null) {
                MapCanvas.Children.Remove(course.CourseCompasLabel);
                course.CourseCompasLabel = null;
            }
        }

        private void RefreshCourseList() {
            _isSyncingSelection = true;

            try {
                CourseList.Items.Clear();
                foreach (var course in _courseLines) {
                    CourseList.Items.Add(new ListBoxItem {
                        Content = $"K{course.Id}",
                        Tag = course
                    });
                }

                CourseList.SelectedItem = FindCourseListItem(_selectedCourseLine);
                DeleteCourseButton.IsEnabled = _selectedCourseLine != null;
                DeleteCourseButton.Appearance = DeleteCourseButton.IsEnabled ? Wpf.Ui.Controls.ControlAppearance.Danger
                                                                             : Wpf.Ui.Controls.ControlAppearance.Secondary;
            }
            finally {
                _isSyncingSelection = false;
            }
        }

        private ListBoxItem? FindCourseListItem(CourseLine? course) {
            if (course == null)
                return null;

            foreach (ListBoxItem item in CourseList.Items) {
                if (ReferenceEquals(item.Tag, course))
                    return item;
            }
            return null;
        }

        // panel edycji kursu
        private void ShowCourseDetails(CourseLine? course) {
            if (course == null) {
                CourseDetailPanel.IsEnabled = false;
                CourseIdText.Text = string.Empty;
                CourseKKBox.Value = null;
                CourseKdDBox.Value = null;
                CourseVelocityBox.Value = null;
                CourseDistanceBox.Value = null;
                CourseTrueWindBox.Value = null;
                CourseAppWindBox.Value = null;

                return;
            }

            CourseDetailPanel.IsEnabled = true;
            CourseIdText.Text = course.Id.ToString();
            CourseKKBox.Value = course.CourseCompas;
            CourseKdDBox.Value = course.CourseOverGround;
            CourseVelocityBox.Value = course.Velocity;
            CourseDistanceBox.Value = course.Distance;
            CourseTrueWindBox.Value = course.TrueWindHeading;
            CourseAppWindBox.Value = course.AppWindHeading;
        }

        private void CourseList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (_isSyncingSelection)
                return;

            var course = (CourseList.SelectedItem as ListBoxItem)?.Tag as CourseLine;

            if (_selectedPosition != null)
                ApplyPositionSelection(null);

            DeselectGuide();
            ApplyCourseSelection(course);
        }

        private void DeleteCourseOnClick(object sender, RoutedEventArgs e) {
            if (_selectedCourseLine != null)
                RemoveSelectedObject();
        }

        // jeden handler - testy
        private void CourseField_TextChanged(object sender, TextChangedEventArgs e) {
            if (_isSyncingSelection || _selectedCourseLine == null)
                return;

            if (sender == CourseKKBox) {
                _selectedCourseLine.CourseCompas = ReadOptionalNumber(CourseKKBox);
                UpdateCourseLabels(_selectedCourseLine);
            }
            else if (sender == CourseKdDBox) {
                _selectedCourseLine.CourseOverGround = ReadOptionalNumber(CourseKdDBox);
                UpdateCourseLabels(_selectedCourseLine);
            }
            else if (sender == CourseVelocityBox) {
                _selectedCourseLine.Velocity = ReadOptionalNumber(CourseVelocityBox);
            }
            else if (sender == CourseDistanceBox) {
                _selectedCourseLine.Distance = ReadOptionalNumber(CourseDistanceBox);
            }
            else if (sender == CourseTrueWindBox) {
                _selectedCourseLine.TrueWindHeading = ReadOptionalNumber(CourseTrueWindBox);
            }
            else if (sender == CourseAppWindBox) {
                _selectedCourseLine.AppWindHeading = ReadOptionalNumber(CourseAppWindBox);
            }
        }

        private double? ReadOptionalNumber(Wpf.Ui.Controls.NumberBox box) {
            if (TryParseBound(box.Text, out double v))
                return Math.Round(v, 1);

            return null;
        }

        private void UpdateMarginInputsFromEdges() {
            MarginLeft = LeftEdge.X1;
            MarginRight = RightEdge.X1;
            MarginTop = TopEdge.Y1;
            MarginBottom = BottomEdge.Y1;

            _isUpdating = true;
            try {
                MarginLeftBox.Value = Math.Round(MarginLeft, 2);
                MarginRightBox.Value = Math.Round(MarginRight, 2);
                MarginTopBox.Value = Math.Round(MarginTop, 2);
                MarginBottomBox.Value = Math.Round(MarginBottom, 2);
            }
            finally {
                _isUpdating = false;
            }
        }

        public void Dispose() {
            if (_isDisposed)
                return;
            _isDisposed = true;

            ClearMap();
        }
    }
}
