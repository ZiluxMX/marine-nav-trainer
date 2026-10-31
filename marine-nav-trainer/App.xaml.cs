using System.Configuration;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace marine_nav_trainer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppPaths.EnsureSeeded();

            // NumberBox przestają dyskryminowac kropek + wklejanie txt 
            EventManager.RegisterClassHandler(
                typeof(Wpf.Ui.Controls.NumberBox),
                UIElement.PreviewTextInputEvent,
                new TextCompositionEventHandler(OnNumberBoxPreviewTextInput));
            EventManager.RegisterClassHandler(
                typeof(Wpf.Ui.Controls.NumberBox),
                DataObject.PastingEvent,
                new DataObjectPastingEventHandler(OnNumberBoxPaste));
        }

        private static void OnNumberBoxPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            string separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            if (separator == "." || !e.Text.Contains('.') || sender is not System.Windows.Controls.TextBox box)
                return;

            string text = e.Text.Replace(".", separator);
            int start = box.SelectionStart;
            box.Text = box.Text.Remove(start, box.SelectionLength).Insert(start, text);
            box.CaretIndex = start + text.Length;
            e.Handled = true;
        }

        private static void OnNumberBoxPaste(object sender, DataObjectPastingEventArgs e)
        {
            string separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            if (separator == "." || !e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true))
                return;

            string text = (string)e.SourceDataObject.GetData(DataFormats.UnicodeText);
            if (string.IsNullOrEmpty(text) || !text.Contains('.'))
                return;

            var replaced = new DataObject();
            replaced.SetData(DataFormats.UnicodeText, text.Replace(".", separator));
            e.DataObject = replaced;
        }
    }

}
