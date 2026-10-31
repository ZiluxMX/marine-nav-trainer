using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace marine_nav_trainer.Map.UI.Views {
    internal sealed class TaskFile {
        public string Name { get; }
        public string FullPath { get; }
        public string Title { get; }
        public string Author { get; }

        public TaskFile(string path) {
            FullPath = path;
            Name = Path.GetFileName(path);
            Title = TryReadTitle(path);
            Author = TryGetAuthor(path);
        }
                
        private string TryReadTitle(string path) {
            try {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("task", out var task) &&
                    task.TryGetProperty("meta", out var meta) &&
                    meta.TryGetProperty("title", out var title) &&
                    title.ValueKind == JsonValueKind.String)
                    return title.GetString() ?? string.Empty;
            }
            catch {
                // niepoprawny json 
                return string.Empty;
            }
            return string.Empty;
        }
        private string? TryGetAuthor(string path) {
            try {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("task", out var task) &&
                    task.TryGetProperty("meta", out var meta) &&
                    meta.TryGetProperty("author", out var author) &&
                    author.ValueKind == JsonValueKind.String)
                    return author.GetString() ?? "N/A";
            }
            catch {
                // niepoprawny json
                return "N/A";
            }
            return "N/A";
        }
    }

    public partial class TaskSelectionWindow : Window {
        public string? SelectedTaskPath { get; private set; }

        public TaskSelectionWindow(string jsonsDirectory) {
            InitializeComponent();

            List<TaskFile> items;
            if (Directory.Exists(jsonsDirectory)) {
                items = Directory.EnumerateFiles(jsonsDirectory, "*.json")
                                 .Select(path => new TaskFile(path))
                                 .OrderBy(file => file.Name).ToList();
            }
            else {
                items = new List<TaskFile>();
            }

            TasksListBox.ItemsSource = items;
            if (items.Count > 0)
                TasksListBox.SelectedIndex = 0;
        }

        private void Select_Click(object sender, RoutedEventArgs e) {
            Confirm();
        }

        private void TasksListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            Confirm();
        }

        private void Confirm() {
            if (TasksListBox.SelectedItem is not TaskFile entry) {
                MessageBox.Show("Wybierz zadanie z listy.", "Nie wybrano pliku pliku!",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SelectedTaskPath = entry.FullPath;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }
    }

    
}
