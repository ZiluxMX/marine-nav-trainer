using marine_nav_trainer.Navigation.Models;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Windows.Views {
    // opis zadania w trybie ucznia

    public partial class TaskSolveDescriptionView : UserControl {
        public event EventHandler<DateTime>? StartDateChanged;

        public TaskSolveDescriptionView() {
            InitializeComponent();

            TimeZoneBox.ItemsSource = TimeZoneOption.All;
            StartDate = DateTime.Today;
        }

        public DateTime StartDate {
            get => StartDatePicker.SelectedDate ?? DateTime.Today;
            set => StartDatePicker.SelectedDate = value;
        }

        public string Notes => NotesBox.Text;

        public TaskTimeZone? GetTimeZone() {
            if (TimeZoneBox.SelectedItem is not TimeZoneOption option)
                return null;

            return new TaskTimeZone {
                Name = option.Name,
                Offset = option.Offset
            };
        }

        public void SetMeta(TaskMeta meta) {
            TitleText.Text = meta.Title;
            AuthorText.Text = meta.Author;
            ContentPreview.Markdown = meta.ContentMd;
        }

        // po zakończeniu zadania notatki zostają do wglądu, ale bez edycji
        public void LockInput() {
            StartDatePicker.IsEnabled = false;
            TimeZoneBox.IsEnabled = false;
            SymbolsToolbar.IsEnabled = false;
            NotesBox.IsReadOnly = true;
        }

        private void StartDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e) {
            StartDateChanged?.Invoke(this, StartDate);
        }

        // znak z Tag przycisku, w miejscu kursora
        private void InsertSymbolOnClick(object sender, RoutedEventArgs e) {
            if (sender is not Wpf.Ui.Controls.Button button || button.Tag is not string symbol)
                return;

            int start = NotesBox.SelectionStart;
            NotesBox.SelectedText = symbol;
            NotesBox.Select(start + symbol.Length, 0);
            NotesBox.Focus();
        }
    }
}
