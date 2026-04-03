using PdfiumViewer;
using System.Diagnostics;
using System.Drawing;
using Drawing = System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Rectangle = System.Drawing.Rectangle;
using Color = System.Drawing.Color;
using Point = System.Windows.Point;
using Threading = System.Windows.Threading;

namespace marine_nav_trainer {
    public partial class MainWindow : Window {
        private const double ZoomStep = 1.1;
        private const double MinZoom = 0.08;
        private const double MaxZoom = 2.0;
        private const double BitmapScalingStep = 0.35;

        private Point _panStart;
        private bool _isPanning;
        private double _hOffsetStart;
        private double _vOffsetStart;
        private bool _edgesVisible = true;
        private Line? _activeEdge;

        // --sekcja TEMP
        private const string MapFile = "Kart-312-3-2021.pdf";
        private const double LatTop = 71.333333;
        private const double LatBottom = 69.000000;
        private const double LonLeft = 12.000000;
        private const double LonRight = 21.000000;
        // -- koniec sekcji TEMP
               
        public MainWindow() {
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

            Debug.WriteLine($"Height={height}  Width={width}");

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
            Debug.WriteLine($"WIN_Height={windowHeight}  WIN_Width={windowWidth}");
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

            Debug.WriteLine($"Zoom={zoom}  Scala={newScale}");

            mouse.Handled = true;
        }

        private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs mouse) {
            if (Keyboard.IsKeyDown(Key.LeftAlt) && _edgesVisible) {
                Point pos = mouse.GetPosition(MapCanvas);
                _activeEdge = GetEdgeNearPoint(pos);
                if (_activeEdge != null)
                    MapCanvas.CaptureMouse();
                return;
            }
            if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                return;

            _isPanning = true;
            _panStart = mouse.GetPosition(MapScrollViewer);
            _hOffsetStart = MapScrollViewer.HorizontalOffset;
            _vOffsetStart = MapScrollViewer.VerticalOffset;
            MapCanvas.CaptureMouse();
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
            if (!_isPanning)
                return;

            Point cur = mouse.GetPosition(MapScrollViewer);
            Vector d = cur - _panStart;
            MapScrollViewer.ScrollToHorizontalOffset(_hOffsetStart - d.X);
            MapScrollViewer.ScrollToVerticalOffset(_vOffsetStart - d.Y);
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

            Debug.WriteLine($"LAT = {latStr}   LON = {lonStr}");
            CoordsText.Text = $"φ:{latStr}  λ:{lonStr}";
        }

        private (double lat, double lon) PixelToGeoCalibrated(double x, double y) {
            double left = LeftEdge.X1;
            double right = RightEdge.X1;
            double top = TopEdge.Y1;
            double bottom = BottomEdge.Y1;
            Debug.WriteLine($"L = {left};  R = {right};  T = {top};  B = {bottom}");

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

        //Panel sterowania

        private void ToggleEdges_Click(object sender, RoutedEventArgs e) {
            ToggleCalibrationLines();
        }

        private void ToggleCalibrationLines() {
            _edgesVisible = !_edgesVisible;
            var isVisible = _edgesVisible ? Visibility.Visible : Visibility.Collapsed;

            LeftEdge.Visibility = isVisible;
            RightEdge.Visibility = isVisible;
            TopEdge.Visibility = isVisible;
            BottomEdge.Visibility = isVisible;
        }
        private void ResetZoom_Click(object sender, RoutedEventArgs e) {
            MapScale.ScaleX = 1.0;
            MapScale.ScaleY = 1.0;
        }
        private void ResizeMap_Click(object sender, RoutedEventArgs e) {
            ToggleCalibrationLines();
        }

        // Static
        private static double Clamp(double v, double min, double max)
            => Math.Max(min, Math.Min(max, v));
        private static double DegToRad(double d) 
            => d * Math.PI / 180.0;
        private static double RadToDeg(double r) 
            => r * 180.0 / Math.PI;
    }
}
//