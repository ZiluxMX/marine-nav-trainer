using marine_nav_trainer.Navigation.Models;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using TextBlock = Wpf.Ui.Controls.TextBlock;

namespace marine_nav_trainer.Navigation.UI.Views {
    public partial class DeviationTableView : UserControl {
        public const int DefaultStepDeg = 10;
        public int StepDeg { get; private set; } = DefaultStepDeg;
        private const int MinStepDeg = 1;
        private const int MaxStepDeg = 180;
        private Dictionary<int, double> _deviations = [];
        private List<(int Heading, NumberBox Box)> _rows = [];

        public DeviationTableView() {
            InitializeComponent();
            BuildTable(DefaultStepDeg);
        }

        public List<Deviation> GetEntries() {
            CaptureValues();
            return _rows.Select(row => new Deviation {
                HeadingDeg = row.Heading,
                DeviationDeg = _deviations.TryGetValue(row.Heading, out var deviation) ? deviation : 0
            }).ToList();
        }

        public void SetEntries(IEnumerable<Deviation> entries) {
            var headings = entries.OrderBy(entry => entry.HeadingDeg).ToList();
            if (headings.Count == 0)
                return;

            int i, step;

            _rows.Clear();
            _deviations.Clear();

            foreach (var entry in headings) {
                i = (entry.HeadingDeg + 360) % 360;
                _deviations[i] = entry.DeviationDeg;
            }

            if (headings.Count > 1) {
                step = headings[1].HeadingDeg - headings[0].HeadingDeg;
            }
            else {
                step = StepDeg;
            }

            DeviationDegBox.Value = step;
            BuildTable(step);
        }

        private void GenerateOnClick(object sender, RoutedEventArgs e) {
            int step = (int)(DeviationDegBox.Value ?? DefaultStepDeg);

            BuildTable(step);
        }

        private void ClearValuesOnClick(object sender, RoutedEventArgs e) {
            _deviations.Clear();
            foreach (var (_, box) in _rows) {
                box.Value = 0;
            }
        }

        private void BuildTable(int step) {
            step = Math.Clamp(step, MinStepDeg, MaxStepDeg);
            CaptureValues();

            StepDeg = step;
            _rows.Clear();
            LeftColumnPanel.Children.Clear();
            RightColumnPanel.Children.Clear();

            List<int> headings = [];
            for (int heading = 0; heading < 360; heading += step) {
                headings.Add(heading);
            }

            LeftColumnPanel.Children.Add(CreateHeaderRow());
            RightColumnPanel.Children.Add(CreateHeaderRow());

            int leftCount = (headings.Count + 1) / 2;
            for (int i = 0; i < headings.Count; i++) {
                var column = i < leftCount ? LeftColumnPanel : RightColumnPanel;
                column.Children.Add(CreateRow(headings[i]));
            }
        }

        private void CaptureValues() {
            foreach (var (heading, box) in _rows) {
                _deviations[heading] = box.Value ?? 0;
            }
        }

        private static Grid CreateRowGrid() {
            Grid grid = new() { 
                Margin = new Thickness(0, 0, 0, 6) 
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            return grid;
        }

        private static Grid CreateHeaderRow() {
            var grid = CreateRowGrid();

            TextBlock headingHeader = new() {
                Text = "KK (°)",
                FontTypography = FontTypography.BodyStrong,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(headingHeader, 0);

            TextBlock deviationHeader = new() {
                Text = "Dewiacja (°)",
                FontTypography = FontTypography.BodyStrong,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(deviationHeader, 2);

            grid.Children.Add(headingHeader);
            grid.Children.Add(deviationHeader);
            return grid;
        }

        private Grid CreateRow(int heading) {
            var grid = CreateRowGrid();

            TextBlock label = new() {
                Text = $"{heading}°",
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(label, 0);

            NumberBox box = new() {
                Value = _deviations.TryGetValue(heading, out var deviation) ? deviation : 0,
                Minimum = -180,
                Maximum = 180,
                MaxDecimalPlaces = 1,
                ClearButtonEnabled = false,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                Padding = new Thickness(8, 4, 8, 4),
                MinWidth = 60
            };
            Grid.SetColumn(box, 2);

            grid.Children.Add(label);
            grid.Children.Add(box);

            _rows.Add((heading, box));
            return grid;
        }
    }
}
