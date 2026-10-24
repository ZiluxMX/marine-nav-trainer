using marine_nav_trainer.Navigation.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace marine_nav_trainer.Windows.Views {
    public partial class TaskDescriptionView : UserControl {

        // bez opisu w formatce
        private string _desc = string.Empty;

        private bool _isPreviewVisible;

        public TaskDescriptionView() {
            InitializeComponent();
        }

        public string TaskTitle {
            get => TitleBox.Text.Trim();
            set => TitleBox.Text = value;
        }

        public string Author {
            get => AuthorBox.Text.Trim();
            set => AuthorBox.Text = value;
        }

        public string ContentMd {
            get => ContentBox.Text;
            set {
                ContentBox.Text = value;
                if (_isPreviewVisible)
                    ContentPreview.Markdown = value;
            }
        }

        public void FillMeta(TaskMeta meta) {
            meta.Title = TaskTitle;
            meta.Desc = _desc;
            meta.Author = Author;
            meta.ContentMd = ContentMd;
        }

        public void SetMeta(TaskMeta meta) {
            TaskTitle = meta.Title;
            Author = meta.Author;
            ContentMd = meta.ContentMd;
            _desc = meta.Desc;
        }
        public void Clear() {
            TitleBox.Text = string.Empty;
            AuthorBox.Text = string.Empty;
            ContentMd = string.Empty;
            _desc = string.Empty;
        }

        private void TogglePreviewOnClick(object sender, RoutedEventArgs e) {
            _isPreviewVisible = !_isPreviewVisible;

            if (_isPreviewVisible)
                ContentPreview.Markdown = ContentBox.Text;

            ContentBox.Visibility = _isPreviewVisible ? Visibility.Collapsed : Visibility.Visible;
            PreviewBorder.Visibility = _isPreviewVisible ? Visibility.Visible : Visibility.Collapsed;
            TogglePreviewButton.Appearance = _isPreviewVisible ? Wpf.Ui.Controls.ControlAppearance.Info
                                                               : Wpf.Ui.Controls.ControlAppearance.Secondary;

            MarkdownToolbar.IsEnabled = !_isPreviewVisible;
            SymbolsToolbar.IsEnabled = !_isPreviewVisible;

            if (!_isPreviewVisible)
                ContentBox.Focus();
        }

        private void ContentBox_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.KeyboardDevice.Modifiers != ModifierKeys.Control)
                return;

            switch (e.Key) {
                case Key.B:
                    WrapSelected("**", "pogrubienie");
                    e.Handled = true;
                    break;
                case Key.I:
                    WrapSelected("*", "kursywa");
                    e.Handled = true;
                    break;
            }
        }

        private void BoldOnClick(object sender, RoutedEventArgs e) {
            WrapSelected("**", "pogrubienie");
        }

        private void ItalicOnClick(object sender, RoutedEventArgs e) {
            WrapSelected("*", "kursywa");
        }

        private void HeaderOnClick(object sender, RoutedEventArgs e) {
            PrefixLine("## ");
        }

        private void BulletListOnClick(object sender, RoutedEventArgs e) {
            PrefixLine("- ");
        }

        private void QuoteOnClick(object sender, RoutedEventArgs e) {
            PrefixLine("> ");
        }

        private void NumberListOnClick(object sender, RoutedEventArgs e) {
            PrefixLine(null);
        }

        private void WrapSelected(string marker, string placeholder) {
            int start = ContentBox.SelectionStart;
            string selected = ContentBox.SelectionLength > 0 ? ContentBox.SelectedText : placeholder;

            ContentBox.SelectedText = marker + selected + marker;
            ContentBox.Select(start + marker.Length, selected.Length);
            ContentBox.Focus();
        }

        // znacznik na zaznaczonej linii
        private void PrefixLine(string? prefix) { //null -> numeracja
            string text = ContentBox.Text;
            int selectionStart = ContentBox.SelectionStart;
            int selectionEnd = selectionStart + ContentBox.SelectionLength;
            int blockStart = 0;
            int blockEnd = -1;

            if (selectionStart > 0) {
                blockStart = text.LastIndexOf('\n', selectionStart - 1) + 1;
            }
            if (selectionEnd < text.Length) {
                blockEnd = text.IndexOf('\n', selectionEnd);
            }
            if (blockEnd < 0)
                blockEnd = text.Length;

            string[] lines = text[blockStart..blockEnd].Split('\n');

            int number = 1;
            for (int i = 0; i < lines.Length; i++) {
                lines[i] = lines[i].TrimEnd('\r');
                if (lines[i].Length == 0 && lines.Length > 1)
                    continue;

                lines[i] = (prefix ?? $"{number++}. ") + lines[i];
            }

            string block = string.Join("\r\n", lines);
            ContentBox.Select(blockStart, blockEnd - blockStart);
            ContentBox.SelectedText = block;
            ContentBox.Select(blockStart, block.Length);
            ContentBox.Focus();
        }

        // znak z Tag przycisku, w miejscu kursora
        private void InsertSymbolOnClick(object sender, RoutedEventArgs e) {
            if (sender is not Wpf.Ui.Controls.Button button || button.Tag is not string symbol)
                return;

            int start = ContentBox.SelectionStart;
            ContentBox.SelectedText = symbol;
            ContentBox.Select(start + symbol.Length, 0);
            ContentBox.Focus();
        }
    }
}
