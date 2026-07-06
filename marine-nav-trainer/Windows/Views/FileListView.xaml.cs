using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Windows.Views {
    public partial class FileListView : UserControl {
        private static readonly string AssetsBase = Path.Combine(
            Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.FullName,
            "Assets"
        );
        private static readonly string MapsDir = Path.Combine(AssetsBase, "Maps");
        private static readonly string JsonsDir = Path.Combine(AssetsBase, "Jsons");

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
            LoadFiles(MapsDir, "*.pdf", MapsListBox, MapsCountLabel);
            LoadFiles(JsonsDir, "*.json", JsonsListBox, JsonsCountLabel);
        }

        private static void LoadFiles(string dir, string pattern, ListBox listBox, TextBlock countLabel) {
            var items = Directory.EnumerateFiles(dir, pattern)
                .Select(path => new FileEntry(path))
                .OrderBy(f => f.Name)
                .ToList();

            listBox.ItemsSource = items;
            countLabel.Text = items.Count switch {
                0 => "Brak plików",
                1 => "1 plik",
                _ => $"{items.Count} pliki/plików"
            };
        }

        private void ImportMap_Click(object sender, RoutedEventArgs e) =>
            ImportFile("Mapy nawigacyjne (*.pdf)|*.pdf", MapsDir);

        private void ImportJson_Click(object sender, RoutedEventArgs e) =>
            ImportFile("Pliki zadań (*.json)|*.json", JsonsDir);

        private void ExportMap_Click(object sender, RoutedEventArgs e) =>
            ExportFile(MapsListBox, "Mapy nawigacyjne (*.pdf)|*.pdf");

        private void ExportJson_Click(object sender, RoutedEventArgs e) =>
            ExportFile(JsonsListBox, "Pliki zadań (*.json)|*.json");

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
