using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Wpf.Ui.Controls;

namespace marine_nav_trainer.Calculators.UI.Controls {
    /// <summary>
    /// Ikona wektorowa rysowana z geometrii opisanej w siatce 24x24.
    /// Dziedziczy po <see cref="IconElement"/>, więc można ją podać jako Icon
    /// w kontrolkach WPF-UI (NavigationViewItem, Button, CardControl...).
    /// Kolor pobiera z Foreground, dzięki czemu podąża za motywem i stanem kontrolki.
    /// Segment przy pomocy AI
    /// </summary>
    public class PathIcon : IconElement {
        public const double DesignSize = 24d;

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(Geometry), typeof(PathIcon),
                new PropertyMetadata(null, OnGeometryChanged));

        public static readonly DependencyProperty FillDataProperty =
            DependencyProperty.Register(nameof(FillData), typeof(Geometry), typeof(PathIcon),
                new PropertyMetadata(null, OnGeometryChanged));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(PathIcon),
                new PropertyMetadata(1.5d, OnStrokeChanged));

        public static readonly DependencyProperty StrokeDashArrayProperty =
            DependencyProperty.Register(nameof(StrokeDashArray), typeof(DoubleCollection), typeof(PathIcon),
                new PropertyMetadata(null, OnStrokeChanged));

        private Path? _strokePath;
        private Path? _fillPath;

        public PathIcon() {
            Width = 20d;
            Height = 20d;
        }

        // Geometria rysowana konturem (linią)
        public Geometry? Data {
            get => (Geometry?)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        // Geometria rysowana wypełnieniem - kropki, pełne znaczniki
        public Geometry? FillData {
            get => (Geometry?)GetValue(FillDataProperty);
            set => SetValue(FillDataProperty, value);
        }

        // Grubość linii w jednostkach siatki 24x24
        public double StrokeThickness {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        // Wzór kreskowania linii - w wielokrotnościach grubości linii
        public DoubleCollection? StrokeDashArray {
            get => (DoubleCollection?)GetValue(StrokeDashArrayProperty);
            set => SetValue(StrokeDashArrayProperty, value);
        }

        protected override UIElement InitializeChildren() {
            _strokePath = new Path {
                Fill = null,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeDashCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            };

            _fillPath = new Path { Stroke = null };

            Canvas canvas = new() { Width = DesignSize, Height = DesignSize };
            canvas.Children.Add(_strokePath);
            canvas.Children.Add(_fillPath);

            ApplyGeometry();
            ApplyStroke();
            ApplyForeground();

            return new Viewbox { Stretch = Stretch.Uniform, Child = canvas };
        }

        protected override void OnForegroundChanged(DependencyPropertyChangedEventArgs e) {
            base.OnForegroundChanged(e);
            ApplyForeground();
        }

        private static void OnGeometryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((PathIcon)d).ApplyGeometry();

        private static void OnStrokeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((PathIcon)d).ApplyStroke();

        private void ApplyGeometry() {
            if (_strokePath is null || _fillPath is null) {
                return;
            }

            _strokePath.Data = Data;
            _fillPath.Data = FillData;
        }

        private void ApplyStroke() {
            if (_strokePath is null) {
                return;
            }

            _strokePath.StrokeThickness = StrokeThickness;
            _strokePath.StrokeDashArray = StrokeDashArray;
        }

        private void ApplyForeground() {
            if (_strokePath is null || _fillPath is null) {
                return;
            }

            _strokePath.Stroke = Foreground;
            _fillPath.Fill = Foreground;
        }
    }
}
