using marine_nav_trainer.Calculators.UI.Controls;
using marine_nav_trainer.Navigation.Models;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using TextBlock = Wpf.Ui.Controls.TextBlock;

namespace marine_nav_trainer.Navigation.UI.Views {
    public partial class VariationTableView : UserControl {
        private readonly List<LocalVariationRow> _localRows = [];
        public event EventHandler<DateTime>? StartDateChanged;

        public VariationTableView() {
            InitializeComponent();

            TimeZoneBox.ItemsSource = TimeZoneOption.All;
            BaseYearBox.Value = DateTime.Today.Year;
            StartDate = DateTime.Today;
            SelectTimeZone("UTC", "+00:00");
            UpdateEmptyState();
        }

        // deklinacja główna
        public MagneticVariation GetVariation() => new() {
            BaseDeg = BaseVariationBox.Value ?? 0,
            BaseYear = (int)Math.Round(BaseYearBox.Value ?? DateTime.Today.Year),
            AnnualChangeMinute = AnnualChangeBox.Value ?? 0
        };

        public void SetVariation(MagneticVariation variation) {
            BaseVariationBox.Value = variation.BaseDeg;
            BaseYearBox.Value = variation.BaseYear;
            AnnualChangeBox.Value = Math.Round(variation.AnnualChangeMinute, 2);
        }

        // strefa czasowa
        public TaskTimeZone GetTimeZone() {
            if (TimeZoneBox.SelectedItem is TimeZoneOption option) {
                return new TaskTimeZone {
                    Name = option.Name,
                    Offset = option.Offset
                };
            }

            return new TaskTimeZone();
        }

        public void SetTimeZone(TaskTimeZone timeZone) {
            SelectTimeZone(timeZone.Name, timeZone.Offset);
        }

        public DateTime StartDate {
            get => StartDatePicker.SelectedDate ?? DateTime.Today;
            set => StartDatePicker.SelectedDate = value;
        }

        // deklinacje lokalne
        public List<LocalMagneticVariation> GetLocalVariations() {
            var entries = new List<LocalMagneticVariation>();

            foreach (var row in _localRows) {
                entries.Add(new LocalMagneticVariation {
                    BaseDeg = row.BaseDegBox.Value ?? 0,
                    BaseYear = (int)Math.Round(row.BaseYearBox.Value ?? DateTime.Today.Year),
                    AnnualChangeMinute = row.AnnualChangeBox.Value ?? 0,
                    Lat = row.LatBox.Value ?? 0,
                    Lon = row.LonBox.Value ?? 0,
                    RangeNm = row.RangeBox.Value ?? 0
                });
            }

            return entries;
        }

        public void SetLocalVariations(IEnumerable<LocalMagneticVariation> entries) {
            _localRows.Clear();
            LocalVariationsPanel.Children.Clear();

            foreach (var entry in entries) {
                AddLocalVariation(entry);
            }

            UpdateEmptyState();
        }

        private void AddLocalVariationOnClick(object sender, RoutedEventArgs e) {
            AddLocalVariation(null);
            UpdateEmptyState();
        }

        private void StartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e) {
            StartDateChanged?.Invoke(this, StartDate);
        }

        private void SelectTimeZone(string name, string? offset) {
            TimeZoneOption? selected = null;

            foreach (var option in TimeZoneOption.All) {
                if (option.Name == name && option.Offset == offset) {
                    selected = option;
                    break;
                }
                if (selected == null && (option.Name == name || option.Offset == offset)) {
                    selected = option;
                }
            }
            TimeZoneBox.SelectedItem = selected;
        }

        private void AddLocalVariation(LocalMagneticVariation? entry) {
            Border card = new() {
                Margin = new Thickness(0, 4, 0, 4),
                Padding = new Thickness(8),
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1),
                BorderBrush = (System.Windows.Media.Brush)FindResource("ControlStrokeColorDefaultBrush")
            };

            Grid grid = new();
            for (int i = 0; i < 2; i++) {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            }
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            for (int i = 0; i < 6; i++) {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            var latBox = CreateCoordinateBox(entry?.Lat, true);
            var lonBox = CreateCoordinateBox(entry?.Lon, false);

            var baseDegBox = CreateCoordinateBox(entry?.BaseDeg, false);
            var rangeBox = CreateNumberBox(entry?.RangeNm, 0, 9999, 4);

            var baseYearBox = CreateNumberBox(entry?.BaseYear, 1900, 2100, 0);
            var annualChangeBox = CreateNumberBox(entry == null ? null : Math.Round(entry.AnnualChangeMinute, 2), -600, 600, 2);

            AddField(grid, 0, 0, "Szerokość φ", latBox);
            AddField(grid, 0, 2, "Długość λ", lonBox);

            AddField(grid, 2, 0, "Deklinacja bazowa", baseDegBox);
            AddField(grid, 2, 2, "Promień (Nm)", rangeBox);

            AddField(grid, 4, 0, "Rok bazowy", baseYearBox);
            AddField(grid, 4, 2, "Zmiana roczna (′)", annualChangeBox);

            LocalVariationRow localVariation = new(card, latBox, lonBox, baseDegBox, rangeBox, baseYearBox, annualChangeBox);

            var removeButton = new Wpf.Ui.Controls.Button {
                Icon = new SymbolIcon { Symbol = SymbolRegular.Delete24 },
                ToolTip = "Usuń lokalną deklinację",
                Appearance = ControlAppearance.Secondary,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(8, 2, 0, 0),
                Height = 31,
                VerticalAlignment = VerticalAlignment.Top,
                Tag = localVariation
            };
            removeButton.Click += RemoveLocalVariationOnClick;
            Grid.SetRow(removeButton, 1);
            Grid.SetColumn(removeButton, 4);
            grid.Children.Add(removeButton);

            card.Child = grid;
            LocalVariationsPanel.Children.Add(card);
            _localRows.Add(localVariation);
        }

        private void RemoveLocalVariationOnClick(object sender, RoutedEventArgs e) {
            LocalVariationRow localVariation = (LocalVariationRow)((Wpf.Ui.Controls.Button)sender).Tag;

            LocalVariationsPanel.Children.Remove(localVariation.Card);
            _localRows.Remove(localVariation);
            UpdateEmptyState();
        }

        private static void AddField(Grid grid, int row, int col, string label, FrameworkElement field) {
            TextBlock text = new() {
                Text = label,
                FontSize = 13
            };
            Grid.SetRow(text, row);
            Grid.SetColumn(text, col);

            Grid.SetRow(field, row + 1);
            Grid.SetColumn(field, col);

            grid.Children.Add(text);
            grid.Children.Add(field);
        }

        private static CoordinateInputBox CreateCoordinateBox(double? value, bool isLatitude) => new() {
            IsLatitude = isLatitude,
            Value = value,
            Margin = new Thickness(0, 4, 0, 4),
            VerticalAlignment = VerticalAlignment.Top
        };

        private static NumberBox CreateNumberBox(double? value, double minimum, double maximum, int decimalPlaces) => new() {
            Value = value,
            Minimum = minimum,
            Maximum = maximum,
            MaxDecimalPlaces = decimalPlaces,
            ClearButtonEnabled = false,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 4, 0, 4),
            MinWidth = 50
        };

        private void UpdateEmptyState() {
            if (_localRows.Count == 0) {
                NoLocalVariationsText.Visibility = Visibility.Visible;
            }
            else {
                NoLocalVariationsText.Visibility = Visibility.Collapsed;
            }
        }

        private record LocalVariationRow(Border Card,
                                         CoordinateInputBox LatBox,
                                         CoordinateInputBox LonBox,
                                         CoordinateInputBox BaseDegBox,
                                         NumberBox RangeBox,
                                         NumberBox BaseYearBox,
                                         NumberBox AnnualChangeBox);
    }
}
