using marine_nav_trainer.Navigation.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Navigation.UI.Views {
    // tabela wiatrów z zadania - uczeń tylko odczytuje
    public partial class TaskSolveLeewayTableView : UserControl {
        private const string DeadZoneName = "Martwy";
        private const double DeadZoneGap = 0.1;
        private const string NoDriftText = "-";

        public TaskSolveLeewayTableView() {
            InitializeComponent();
        }

        public void SetEntries(IEnumerable<Leeway> entries) {
            TableGrid.Children.Clear();
            TableGrid.RowDefinitions.Clear();

            var rows = entries.ToList();

            AddRow("Wiatr", "Od (°)", "Do (°)", "Dryf (°)", isHeader: true);
            AddDeadZoneRow(rows);

            foreach (var entry in rows)
                AddRow(entry.Name,
                       FormatDegrees(entry.MinDeg),
                       FormatDegrees(entry.MaxDeg),
                       FormatDegrees(entry.ValueDeg),
                       isHeader: false);
        }

        // strefa martwa - od 0 do początku bajdewindu, bez dryfu
        private void AddDeadZoneRow(List<Leeway> rows) {
            var closeHauled = rows.FirstOrDefault(entry => entry.Key == Leeway.CloseHauledKey);
            double max = closeHauled == null ? 0 : Math.Max(0, closeHauled.MinDeg - DeadZoneGap);

            AddRow(DeadZoneName, FormatDegrees(0), FormatDegrees(max), NoDriftText, isHeader: false);
        }

        private void AddRow(string name, string min, string max, string value, bool isHeader) {
            int row = TableGrid.RowDefinitions.Count;
            TableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddCell(name, row, 0, isHeader);
            AddCell(min, row, 1, isHeader);
            AddCell(max, row, 2, isHeader);
            AddCell(value, row, 3, isHeader);
        }

        private void AddCell(string text, int row, int column, bool isHeader) {
            TextBlock content = new() {
                Text = text,
                Style = (Style)FindResource(isHeader ? "TableHeaderText" : "TableCellText")
            };

            Border cell = new() {
                Style = (Style)FindResource(isHeader ? "TableHeaderCell" : "TableCell"),
                Child = content
            };

            Grid.SetRow(cell, row);
            Grid.SetColumn(cell, column);
            TableGrid.Children.Add(cell);
        }

        private static string FormatDegrees(double value) =>
            $"{value.ToString("0.#", CultureInfo.CurrentCulture)}°";
    }
}
