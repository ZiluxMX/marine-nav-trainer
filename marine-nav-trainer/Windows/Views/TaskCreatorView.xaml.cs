using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Windows.Views {
    public partial class TaskCreatorView : UserControl, IDisposable {
        private bool _disposed;

        public event EventHandler? CloseRequested;

        public TaskCreatorView() {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) =>
            CloseRequested?.Invoke(this, EventArgs.Empty);

        public void Dispose() {
            if (_disposed)
                return;
            _disposed = true;

            if (MapViewControl is IDisposable mapDisposable)
                mapDisposable.Dispose();

            if (CalculatorViewControl is IDisposable calcDisposable)
                calcDisposable.Dispose();
        }
    }
}
