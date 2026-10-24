using marine_nav_trainer.Navigation.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Navigation.UI.Views {
    // tabela dewiacji z zadania - uczeń
    public partial class TaskSolveDeviationTableView : UserControl {
        private const string EmptyCellText = "";

        public TaskSolveDeviationTableView() {
            InitializeComponent();
        }

        public void SetEntries(IEnumerable<Deviation> entries) {
            TableGrid.Children.Clear();
            TableGrid.RowDefinitions.Clear();

            var headings = entries.OrderBy(entry => entry.HeadingDeg).ToList();
            if (headings.Count == 0)
                return;

            AddRow("KK (°)", "Dewiacja (°)", "KK (°)", "Dewiacja (°)", isHeader: true);

            // przy nieparzystej liczbie wpisów lewa para kolumn jest dłuższa
            int leftCount = (headings.Count + 1) / 2;
            for (int i = 0; i < leftCount; i++) {
                int right = i + leftCount;
                bool hasRight = right < headings.Count;

                AddRow(FormatHeading(headings[i]),
                       FormatDeviation(headings[i].DeviationDeg),
                       hasRight ? FormatHeading(headings[right]) : EmptyCellText,
                       hasRight ? FormatDeviation(headings[right].DeviationDeg) : EmptyCellText,
                       isHeader: false);
            }
        }

        private void AddRow(string leftHeading, string leftDeviation, string rightHeading, string rightDeviation, bool isHeader) {
            int row = TableGrid.RowDefinitions.Count;
            TableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddCell(leftHeading, row, 0, isHeader);
            AddCell(leftDeviation, row, 1, isHeader);
            AddCell(rightHeading, row, 2, isHeader);
            AddCell(rightDeviation, row, 3, isHeader);
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

        private static string FormatHeading(Deviation entry) => $"{entry.HeadingDeg}°";

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
