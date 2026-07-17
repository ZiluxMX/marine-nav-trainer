using marine_nav_trainer.Calculators.Core;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Calculators.UI.Controls {
    public partial class CoordinateInputBox : UserControl {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double?), typeof(CoordinateInputBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public static readonly DependencyProperty IsLatitudeProperty =
            DependencyProperty.Register(nameof(IsLatitude), typeof(bool), typeof(CoordinateInputBox),
                new PropertyMetadata(true, OnIsLatitudeChanged));

        private bool _updating;

        public CoordinateInputBox() {
            InitializeComponent();
            ApplyAxis();
        }

        public double? Value {
            get => (double?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public bool IsLatitude {
            get => (bool)GetValue(IsLatitudeProperty);
            set => SetValue(IsLatitudeProperty, value);
        }

        public GeoCoordinate? Coordinate {
            get => Value is double d
                ? GeoCoordinate.FromDecimal(d, IsLatitude ? CoordinateAxis.Latitude : CoordinateAxis.Longitude)
                : null;
            set => Value = value?.DecimalDegrees;
        }

        private double MaxDegrees => IsLatitude ? 90 : 180;

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var box = (CoordinateInputBox)d;
            if (box._updating)
                return;
            box.ShowValue((double?)e.NewValue);
        }

        private static void OnIsLatitudeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((CoordinateInputBox)d).ApplyAxis();

        private void ApplyAxis() {
            _updating = true;
            try {
                DecimalBox.Minimum = -MaxDegrees;
                DecimalBox.Maximum = MaxDegrees;
                DegBox.Minimum = 0;
                DegBox.Maximum = MaxDegrees;
                MinBox.Minimum = 0;
                MinBox.Maximum = 59.9999;

                int selected = CardinalBox.SelectedIndex < 0 ? 0 : CardinalBox.SelectedIndex;
                CardinalBox.ItemsSource = IsLatitude ? new[] { "N", "S" } : new[] { "E", "W" };
                CardinalBox.SelectedIndex = selected;
            } finally {
                _updating = false;
            }
        }

        private void ShowValue(double? value) {
            _updating = true;
            try {
                DecimalBox.Value = value;
                UpdateDegMinBoxes(value);
            } finally {
                _updating = false;
            }
        }

        private void UpdateDegMinBoxes(double? value) {
            if (value is not double d) {
                DegBox.Value = null;
                MinBox.Value = null;
                return;
            }
            double abs = Math.Abs(d);
            int deg = (int)Math.Floor(abs);
            double min = Math.Round((abs - deg) * 60.0, 4);

            if (min >= 60) { 
                deg++; 
                min = 0; 
            }
            DegBox.Value = deg;
            MinBox.Value = min;
            CardinalBox.SelectedIndex = d < 0 ? 1 : 0;
        }

        private void DecimalBox_TextChanged(object sender, TextChangedEventArgs e) {
            if (_updating)
                return;
            _updating = true;
            try {
                if (TryParse(DecimalBox.Text, out double d)) {
                    d = Math.Clamp(d, -MaxDegrees, MaxDegrees);
                    SetCurrentValue(ValueProperty, (double?)d);
                    UpdateDegMinBoxes(d);
                } else {
                    SetCurrentValue(ValueProperty, null);
                    if (string.IsNullOrWhiteSpace(DecimalBox.Text))
                        UpdateDegMinBoxes(null);
                }
            } finally {
                _updating = false;
            }
        }

        private void DegMin_TextChanged(object sender, TextChangedEventArgs e) => RecomputeFromDegMin();

        private void Cardinal_SelectionChanged(object sender, SelectionChangedEventArgs e) => RecomputeFromDegMin();

        private void RecomputeFromDegMin() {
            if (_updating)
                return;
            _updating = true;
            try {
                bool hasDeg = TryParse(DegBox.Text, out double deg);
                bool hasMin = TryParse(MinBox.Text, out double min);
                if (!hasDeg && !hasMin) {
                    SetCurrentValue(ValueProperty, null);
                    DecimalBox.Value = null;
                    return;
                }
                double sign = CardinalBox.SelectedIndex == 1 ? -1.0 : 1.0;
                double d = sign * ((hasDeg ? deg : 0) + (hasMin ? min : 0) / 60.0);
                d = Math.Clamp(Math.Round(d, 6), -MaxDegrees, MaxDegrees);
                SetCurrentValue(ValueProperty, (double?)d);
                DecimalBox.Value = d;
            } finally {
                _updating = false;
            }
        }

        private static bool TryParse(string? s, out double value) {
            s = (s ?? string.Empty).Trim().Replace(',', '.');
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
