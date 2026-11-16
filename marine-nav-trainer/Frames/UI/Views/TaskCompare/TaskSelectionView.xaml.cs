using marine_nav_trainer.Map.UI.Views;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Windows.Views {
    // wybór dwóch plików do porównania - zadania lub rozwiązania

    public partial class TaskSelectionView : UserControl {
        private string? _referenceJson;
        private string? _checkedJson;

        // [Porównaj zadania]
        public event EventHandler? CompareRequested;

        public string? ReferenceTaskPath { get; private set; }
        public string? CheckedTaskPath { get; private set; }

        public TaskSelectionView() {
            InitializeComponent();
        }

        private void SelectReferenceOnClick(object sender, RoutedEventArgs e) {
            string? path = SelectTaskFile("Wybierz zadanie wzorcowe");
            if (path == null)
                return;

            ReferenceTaskPath = path;
            _referenceJson = File.ReadAllText(path);
            ReferenceFileBox.Text = Path.GetFileName(path);
            UpdateDifferences();
        }

        private void SelectCheckedOnClick(object sender, RoutedEventArgs e) {
            string? path = SelectTaskFile("Wybierz zadanie do sprawdzenia");
            if (path == null)
                return;

            CheckedTaskPath = path;
            _checkedJson = File.ReadAllText(path);
            CheckedFileBox.Text = Path.GetFileName(path);
            UpdateDifferences();
        }

        // plik musi być JSON-em z obiektem "task"
        private string? SelectTaskFile(string title) {
            var dialog = new TaskSelectionWindow(AppPaths.JsonsDir) {
                Title = title,
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true || dialog.SelectedTaskPath == null)
                return null;

            string fileName = Path.GetFileName(dialog.SelectedTaskPath);
            try {
                using var document = JsonDocument.Parse(File.ReadAllText(dialog.SelectedTaskPath));
                if (document.RootElement.TryGetProperty("task", out var task) && task.ValueKind == JsonValueKind.Object)
                    return dialog.SelectedTaskPath;

                MessageBox.Show(
                    $"Plik \"{fileName}\" nie zawiera sekcji \"task\".",
                    "Błąd wczytywania zadania",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex) {
                MessageBox.Show(
                    $"Nie udało się odczytać pliku zadania \"{fileName}\":\n{ex.Message}",
                    "Błąd wczytywania zadania",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            return null;
        }

        private void UpdateDifferences() {
            CompareButton.IsEnabled = _referenceJson != null && _checkedJson != null;

            if (_referenceJson == null || _checkedJson == null) {
                DifferencesText.Text = "Wybierz oba pliki, aby sprawdzić zgodność zadań.";
                return;
            }

            using var referenceDocument = JsonDocument.Parse(_referenceJson);
            using var checkedDocument = JsonDocument.Parse(_checkedJson);
            var referenceTask = referenceDocument.RootElement.GetProperty("task");
            var checkedTask = checkedDocument.RootElement.GetProperty("task");

            string meta = DescribeSection(referenceTask, checkedTask, "meta");
            string chart = DescribeSection(referenceTask, checkedTask, "chart");
            string navigation = DescribeSection(referenceTask, checkedTask, "navigation");

            DifferencesText.Text = $"meta: {meta}\nchart: {chart}\nnavigation: {navigation}";
        }

        // nazwy pól sekcji, które różnią się w obu plikach - bez wartości
        private static string DescribeSection(JsonElement referenceTask, JsonElement checkedTask, string section) {
            bool hasReference = referenceTask.TryGetProperty(section, out var referenceSection);
            bool hasChecked = checkedTask.TryGetProperty(section, out var checkedSection);

            if (!hasReference && !hasChecked)
                return "brak w obu plikach";
            if (!hasReference)
                return "brak w zadaniu wzorcowym";
            if (!hasChecked)
                return "brak w zadaniu do sprawdzenia";

            var differences = new List<string>();
            foreach (var property in referenceSection.EnumerateObject()) {
                if (!checkedSection.TryGetProperty(property.Name, out var checkedProperty) ||
                    property.Value.GetRawText() != checkedProperty.GetRawText())
                    differences.Add(property.Name);
            }

            // pola, których nie ma we wzorcu
            foreach (var property in checkedSection.EnumerateObject()) {
                if (!referenceSection.TryGetProperty(property.Name, out _))
                    differences.Add(property.Name);
            }

            if (differences.Count == 0)
                return "zgodne";

            return $"rozbieżności ({string.Join(", ", differences)})";
        }

        private void CompareOnClick(object sender, RoutedEventArgs e) =>
            CompareRequested?.Invoke(this, EventArgs.Empty);
    }
}
