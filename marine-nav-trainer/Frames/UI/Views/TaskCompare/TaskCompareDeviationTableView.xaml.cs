using marine_nav_trainer.Navigation.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Navigation.UI.Views {
    // tabela dewiacji przy porównaniu - przy różnicach obie tabele, jedna pod drugą
    public partial class TaskCompareDeviationTableView : UserControl {
        private const string EmptyCellText = "";

        public TaskCompareDeviationTableView() {
            InitializeComponent();
        }

        public void SetEntries(List<Deviation> referenceEntries, List<Deviation> checkedEntries) {
            List<Deviation> reference = SortByHeading(referenceEntries);
            List<Deviation> checkedSorted = SortByHeading(checkedEntries);

            Visibility checkedVisibility = AreEqual(reference, checkedSorted) ? Visibility.Collapsed : Visibility.Visible;
            ReferenceCaption.Visibility = checkedVisibility;
            CheckedSection.Visibility = checkedVisibility;

            FillTable(ReferenceTable, reference);
            FillTable(CheckedTable, checkedSorted);
        }

        private static List<Deviation> SortByHeading(List<Deviation> entries) {
            var sorted = new List<Deviation>(entries);
            sorted.Sort(CompareHeading);
            return sorted;
        }

        private static int CompareHeading(Deviation first, Deviation second) {
            return first.HeadingDeg.CompareTo(second.HeadingDeg);
        }

        private static bool AreEqual(List<Deviation> reference, List<Deviation> checkedSorted) {
            if (reference.Count != checkedSorted.Count)
                return false;

            for (int i = 0; i < reference.Count; i++) {
                if (reference[i].HeadingDeg != checkedSorted[i].HeadingDeg ||
                    reference[i].DeviationDeg != checkedSorted[i].DeviationDeg)
                    return false;
            }

            return true;
        }

        private void FillTable(Grid table, List<Deviation> headings) {
            table.Children.Clear();
            table.RowDefinitions.Clear();

            if (headings.Count == 0)
                return;

            AddRow(table, "KK (°)", "Dewiacja (°)", "KK (°)", "Dewiacja (°)", isHeader: true);

            // przy nieparzystej liczbie wpisów lewa para kolumn jest dłuższa
            int leftCount = (headings.Count + 1) / 2;
            for (int i = 0; i < leftCount; i++) {
                int right = i + leftCount;
                bool hasRight = right < headings.Count;

                AddRow(table,
                       FormatHeading(headings[i]),
                       FormatDeviation(headings[i].DeviationDeg),
                       hasRight ? FormatHeading(headings[right]) : EmptyCellText,
                       hasRight ? FormatDeviation(headings[right].DeviationDeg) : EmptyCellText,
                       isHeader: false);
            }
        }

        private void AddRow(Grid table, string leftHeading, string leftDeviation, string rightHeading, string rightDeviation, bool isHeader) {
            int row = table.RowDefinitions.Count;
            table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddCell(table, leftHeading, row, 0, isHeader);
            AddCell(table, leftDeviation, row, 1, isHeader);
            AddCell(table, rightHeading, row, 2, isHeader);
            AddCell(table, rightDeviation, row, 3, isHeader);
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

        private static string FormatHeading(Deviation entry) {
            return $"{entry.HeadingDeg}°";
        }

        // dewiacja ze znakiem
        private static string FormatDeviation(double value) {
            string text = Math.Abs(value).ToString("0.#", CultureInfo.CurrentCulture);
            if (value == 0)
                return $"{text}°";

            string sign = value < 0 ? "-" : "+";
            return $"{sign}{text}°";
        }
    }
}
