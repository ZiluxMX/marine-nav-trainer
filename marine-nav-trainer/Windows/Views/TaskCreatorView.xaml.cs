using marine_nav_trainer.Calculators.UI;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Windows.Views {
    public partial class TaskCreatorView : UserControl, IDisposable {
        private bool _disposed;

        public event EventHandler? CloseRequested;

        public TaskCreatorView() {
            InitializeComponent();
            CalculatorViewControl.InsertPointRequested += OnInsertPointRequested;
        }

        private void OnInsertPointRequested(object? sender, InsertPointEventArgs e) {
            bool inserted = MapViewControl.TryInsertPoint(e.Lat, e.Lon);
            if (!inserted)
                MessageBox.Show(
                    "Punkt nie mieści się na mapie.",
                    "Wstaw punkt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) =>
            CloseRequested?.Invoke(this, EventArgs.Empty);

        public void Dispose() {
            if (_disposed)
                return;
            _disposed = true;

            CalculatorViewControl.InsertPointRequested -= OnInsertPointRequested;

            if (MapViewControl is IDisposable mapDisposable)
                mapDisposable.Dispose();

            if (CalculatorViewControl is IDisposable calcDisposable)
                calcDisposable.Dispose();
        }
    }
}
