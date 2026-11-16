using marine_nav_trainer.Navigation.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Navigation.UI.Views {
    // tabela wiatrów przy porównaniu - przy różnicach obie tabele, jedna pod drugą
    public partial class TaskCompareLeewayTableView : UserControl {
        private const string DeadZoneName = "Martwy";
        private const double DeadZoneGap = 0.1;
        private const string NoDriftText = "-";

        public TaskCompareLeewayTableView() {
            InitializeComponent();
        }

        public void SetEntries(List<Leeway> reference, List<Leeway> checkedEntries) {
            Visibility checkedVisibility = AreEqual(reference, checkedEntries) ? Visibility.Collapsed : Visibility.Visible;
            ReferenceCaption.Visibility = checkedVisibility;
            CheckedSection.Visibility = checkedVisibility;

            FillTable(ReferenceTable, reference);
            FillTable(CheckedTable, checkedEntries);
        }

        private static bool AreEqual(List<Leeway> reference, List<Leeway> checkedEntries) {
            if (reference.Count != checkedEntries.Count)
                return false;

            for (int i = 0; i < reference.Count; i++) {
                if (reference[i].Key != checkedEntries[i].Key ||
                    reference[i].MinDeg != checkedEntries[i].MinDeg ||
                    reference[i].MaxDeg != checkedEntries[i].MaxDeg ||
                    reference[i].ValueDeg != checkedEntries[i].ValueDeg)
                    return false;
            }

            return true;
        }

        private void FillTable(Grid table, List<Leeway> rows) {
            table.Children.Clear();
            table.RowDefinitions.Clear();

            AddRow(table, "Wiatr", "Od (°)", "Do (°)", "Dryf (°)", isHeader: true);
            AddDeadZoneRow(table, rows);

            foreach (var entry in rows)
                AddRow(table,
                       entry.Name,
                       FormatDegrees(entry.MinDeg),
                       FormatDegrees(entry.MaxDeg),
                       FormatDegrees(entry.ValueDeg),
                       isHeader: false);
        }

        // strefa martwa - od 0 do początku bajdewindu, bez dryfu
        private void AddDeadZoneRow(Grid table, List<Leeway> rows) {
            double max = 0;
            foreach (var entry in rows)
                if (entry.Key == Leeway.CloseHauledKey)
                    max = Math.Max(0, entry.MinDeg - DeadZoneGap);

            AddRow(table, DeadZoneName, FormatDegrees(0), FormatDegrees(max), NoDriftText, isHeader: false);
        }

        private void AddRow(Grid table, string name, string min, string max, string value, bool isHeader) {
            int row = table.RowDefinitions.Count;
            table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddCell(table, name, row, 0, isHeader);
            AddCell(table, min, row, 1, isHeader);
            AddCell(table, max, row, 2, isHeader);
            AddCell(table, value, row, 3, isHeader);
        }

        private void AddCell(Grid table, string text, int row, int column, bool isHeader) {
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
            table.Children.Add(cell);
        }

        private static string FormatDegrees(double value) {
            return $"{value.ToString("0.#", CultureInfo.CurrentCulture)}°";
        }
    }
}
