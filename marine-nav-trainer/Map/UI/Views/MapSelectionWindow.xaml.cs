using System.IO;
using System.Windows;
using System.Windows.Input;

namespace marine_nav_trainer.Map.UI.Views {
    public partial class MapSelectionWindow : Window {
        public string? SelectedMapPath { get; private set; }

        public MapSelectionWindow(string mapsDirectory) {
            InitializeComponent();

            var items = Directory.Exists(mapsDirectory)
                ? Directory.EnumerateFiles(mapsDirectory, "*.pdf")
                    .Select(path => new MapFileEntry(path))
                    .OrderBy(f => f.Name)
                    .ToList()
                : new List<MapFileEntry>();

            MapsListBox.ItemsSource = items;
            if (items.Count > 0)
                MapsListBox.SelectedIndex = 0;
        }

        private void Select_Click(object sender, RoutedEventArgs e) => Confirm();

        private void MapsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) => Confirm();

        private void Confirm() {
            if (MapsListBox.SelectedItem is not MapFileEntry entry) {
                MessageBox.Show("Wybierz plik mapy z listy.", "Brak zaznaczenia",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SelectedMapPath = entry.FullPath;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }
    }

    internal sealed class MapFileEntry {
        public string Name { get; }
        public string FullPath { get; }
        public string SizeText { get; }

        public MapFileEntry(string path) {
            FullPath = path;
            Name = Path.GetFileName(path);
            long bytes = new FileInfo(path).Length;
            SizeText = bytes switch {
                < 1024 => $"{bytes} B",
                < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
                _ => $"{bytes / (1024.0 * 1024):F1} MB"
            };
        }
    }
}
