using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Windows.Views {
    public partial class FileListView : UserControl {
        private static readonly string MapsDir = AppPaths.MapsDir;
        private static readonly string JsonsDir = AppPaths.JsonsDir;

        private List<FileEntry> _allMaps = new();
        private List<FileEntry> _allJsons = new();

        public event EventHandler? CloseRequested;

        public FileListView() {
            InitializeComponent();
            EnsureDirectories();
            Refresh();
        }

        private static void EnsureDirectories() {
            Directory.CreateDirectory(MapsDir);
            Directory.CreateDirectory(JsonsDir);
        }

        public void Refresh() {
            _allMaps = LoadEntries(MapsDir, "*.pdf");
            _allJsons = LoadEntries(JsonsDir, "*.json");
            ApplyMapsFilter();
            ApplyJsonsFilter();
        }

        private static List<FileEntry> LoadEntries(string dir, string pattern) =>
            Directory.EnumerateFiles(dir, pattern)
                .Select(path => new FileEntry(path))
                .OrderBy(f => f.Name)
                .ToList();

        private void ApplyMapsFilter() =>
            ApplyFilter(_allMaps, MapsSearchBox.Text, MapsListBox, MapsCountLabel);

        private void ApplyJsonsFilter() =>
            ApplyFilter(_allJsons, JsonsSearchBox.Text, JsonsListBox, JsonsCountLabel);

        private static void ApplyFilter(List<FileEntry> all, string? search, ListBox listBox, TextBlock countLabel) {
            search = search?.Trim() ?? string.Empty;
            bool isFiltering = search.Length > 0;

            var items = isFiltering
                ? all.Where(f => f.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList()
                : all;

            listBox.ItemsSource = items;

            if (isFiltering)
                countLabel.Text = $"{items.Count} z {all.Count}";
            else
                countLabel.Text = all.Count switch {
                    0 => "Brak plików",
                    1 => "1 plik",
                    _ => $"{all.Count} pliki/plików"
                };
        }

        private void MapsSearchBox_TextChanged(object sender, TextChangedEventArgs e) {
            if (IsInitialized) ApplyMapsFilter();
        }

        private void JsonsSearchBox_TextChanged(object sender, TextChangedEventArgs e) {
            if (IsInitialized) ApplyJsonsFilter();
        }

        private void ImportMap_Click(object sender, RoutedEventArgs e) =>
            ImportFile("Mapy nawigacyjne (*.pdf)|*.pdf", MapsDir);

        private void ImportJson_Click(object sender, RoutedEventArgs e) =>
            ImportFile("Pliki zadań (*.json)|*.json", JsonsDir);

        private void ExportMap_Click(object sender, RoutedEventArgs e) =>
            ExportFile(MapsListBox, "Mapy nawigacyjne (*.pdf)|*.pdf");

        private void ExportJson_Click(object sender, RoutedEventArgs e) =>
            ExportFile(JsonsListBox, "Pliki zadań (*.json)|*.json");

        private void DeleteMap_Click(object sender, RoutedEventArgs e) =>
            DeleteFile(MapsListBox, "mapę");

        private void DeleteJson_Click(object sender, RoutedEventArgs e) =>
            DeleteFile(JsonsListBox, "zadanie");

        private void DeleteFile(ListBox listBox, string what) {
            if (listBox.SelectedItem is not FileEntry entry) {
                MessageBox.Show($"Zaznacz {what} do usunięcia.", "Nie zaznaczono pliku",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Czy na pewno usunąć plik \"{entry.Name}\"?\nTej operacji nie można cofnąć.",
                "Potwierdź usunięcie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            try {
                File.Delete(entry.FullPath);
            }
            catch (Exception ex) {
                MessageBox.Show($"Nie udało się usunąć pliku:\n{ex.Message}", "Błąd usuwania",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Refresh();
        }

        private void ExportFile(ListBox listBox, string filter) {
            if (listBox.SelectedItem is not FileEntry entry) {
                MessageBox.Show("Zaznacz plik do eksportu.", "Nie zaznaczono pliku",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog {
                Title = "Eksportuj plik",
                Filter = filter,
                FileName = entry.Name
            };

            if (dialog.ShowDialog() != true)
                return;

            File.Copy(entry.FullPath, dialog.FileName, overwrite: true);

            MessageBox.Show($"Wyeksportowano \"{entry.Name}\".", "Eksport zakończony",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ImportFile(string filter, string targetDir) {
            var dialog = new OpenFileDialog {
                Title = "Wybierz plik do importu",
                Filter = filter,
                Multiselect = true
            };

            if (dialog.ShowDialog() != true)
                return;

            int copied = 0;
            int skipped = 0;
            foreach (string src in dialog.FileNames) {
                string dest = Path.Combine(targetDir, Path.GetFileName(src));
                if (File.Exists(dest)) {
                    var result = MessageBox.Show(
                        $"Plik o nazwie \"{Path.GetFileName(src)}\" już istnieje. Czy nadpisać?",
                        "Plik istnieje",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) {
                        skipped++;
                        continue;
                    }
                }
                File.Copy(src, dest, overwrite: true);
                copied++;
            }

            Refresh();

            if (copied > 0) {
                MessageBox.Show(
                    $"Zaimportowano plików: {copied}." + (skipped > 0 ? $"\nPominięto: {skipped}." : ""),
                    "Import zakończony",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) =>
            CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    internal sealed class FileEntry {
        public string Name { get; }
        public string SizeText { get; }
        public string FullPath { get; }

        public FileEntry(string path) {
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
