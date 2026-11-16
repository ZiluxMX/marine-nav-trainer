using marine_nav_trainer.Calculators.Core;
using marine_nav_trainer.Navigation.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Navigation.UI.Views {
    // deklinacja i czas przy porównaniu - przy różnicach oba zestawy, jeden pod drugim
    public partial class TaskCompareVariationView : UserControl {
        private const int MainColumns = 3;
        private const int TimeColumns = 2;

        public TaskCompareVariationView() {
            InitializeComponent();
        }

        public void SetNavigation(NavigationData reference, NavigationData checkedNavigation) {
            Visibility checkedVisibility = AreEqual(reference, checkedNavigation) ? Visibility.Collapsed : Visibility.Visible;
            ReferenceCaption.Visibility = checkedVisibility;
            CheckedSection.Visibility = checkedVisibility;

            FillPanel(ReferencePanel, reference);
            FillPanel(CheckedPanel, checkedNavigation);
        }

        private static bool AreEqual(NavigationData reference, NavigationData checkedNavigation) {
            if (reference.TimeZone.Name != checkedNavigation.TimeZone.Name ||
                reference.TimeZone.Offset != checkedNavigation.TimeZone.Offset)
                return false;

            if (reference.StartDate.Date != checkedNavigation.StartDate.Date)
                return false;

            if (!AreVariationsEqual(reference.Variation, checkedNavigation.Variation))
                return false;

            if (reference.VariationList.Count != checkedNavigation.VariationList.Count)
                return false;

            for (int i = 0; i < reference.VariationList.Count; i++) {
                LocalMagneticVariation first = reference.VariationList[i];
                LocalMagneticVariation second = checkedNavigation.VariationList[i];

                if (!AreVariationsEqual(first, second) ||
                    first.Lat != second.Lat ||
                    first.Lon != second.Lon ||
                    first.RangeNm != second.RangeNm)
                    return false;
            }

            return true;
        }

        private static bool AreVariationsEqual(MagneticVariation first, MagneticVariation second) {
            return first.BaseDeg == second.BaseDeg
                && first.BaseYear == second.BaseYear
                && first.AnnualChangeMinute == second.AnnualChangeMinute;
        }

        private void FillPanel(StackPanel panel, NavigationData navigation) {
            panel.Children.Clear();

            // strefa czasowa
            AddSectionTitle(panel, "Strefa czasowa");
            Grid timeTable = AddTable(panel, TimeColumns);
            AddRow(timeTable, true, "Strefa", "Data rozpoczęcia rejsu");
            AddRow(timeTable, false,
                   $"{navigation.TimeZone.Name} ({navigation.TimeZone.Offset})",
                   navigation.StartDate.ToString("d", CultureInfo.CurrentCulture));

            // deklinacja główna
            AddSectionTitle(panel, "Deklinacja główna");
            Grid mainTable = AddTable(panel, MainColumns);
            AddRow(mainTable, true, "Deklinacja bazowa", "Rok bazowy", "Zmiana roczna (′)");
            AddRow(mainTable, false,
                   CoordinateFormatter.ToLonDegMin(navigation.Variation.BaseDeg),
                   navigation.Variation.BaseYear.ToString(),
                   FormatNumber(navigation.Variation.AnnualChangeMinute));

            // deklinacje lokalne
            AddSectionTitle(panel, "Deklinacje lokalne");
            if (navigation.VariationList.Count == 0) {
                panel.Children.Add(new TextBlock {
                    Text = "Brak deklinacji lokalnych - obowiązuje deklinacja główna.",
                    Margin = new Thickness(0, 0, 0, 8),
                    TextWrapping = TextWrapping.Wrap
                });
                return;
            }

            foreach (var entry in navigation.VariationList) {
                Grid localTable = AddTable(panel, MainColumns);
                AddRow(localTable, true, "Szerokość φ", "Długość λ", "Promień (Nm)");
                AddRow(localTable, false,
                       CoordinateFormatter.ToLatDegMin(entry.Lat),
                       CoordinateFormatter.ToLonDegMin(entry.Lon),
                       FormatNumber(entry.RangeNm));
                AddRow(localTable, true, "Deklinacja bazowa", "Rok bazowy", "Zmiana roczna (′)");
                AddRow(localTable, false,
                       CoordinateFormatter.ToLonDegMin(entry.BaseDeg),
                       entry.BaseYear.ToString(),
                       FormatNumber(entry.AnnualChangeMinute));
            }
        }

        private static void AddSectionTitle(StackPanel panel, string title) {
            panel.Children.Add(new Wpf.Ui.Controls.TextBlock {
                Text = title,
                FontTypography = Wpf.Ui.Controls.FontTypography.BodyStrong,
                Margin = new Thickness(0, 0, 0, 6)
            });
        }

        private Grid AddTable(StackPanel panel, int columns) {
            Grid table = new();
            for (int i = 0; i < columns; i++)
                table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            panel.Children.Add(new Border {
                Margin = new Thickness(0, 0, 0, 8),
                VerticalAlignment = VerticalAlignment.Top,
                Style = (Style)FindResource("TableFrame"),
                Child = table
            });

            return table;
        }

        private void AddRow(Grid table, bool isHeader, params string[] cells) {
            int row = table.RowDefinitions.Count;
            table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            for (int column = 0; column < cells.Length; column++)
                AddCell(table, cells[column], row, column, isHeader);
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

        private static string FormatNumber(double value) {
            return value.ToString("0.##", CultureInfo.CurrentCulture);
        }
    }
}
