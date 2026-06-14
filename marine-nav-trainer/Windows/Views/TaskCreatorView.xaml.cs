using System.Windows.Controls;

namespace marine_nav_trainer.Windows.Views {
    public partial class TaskCreatorView : UserControl, IDisposable {
        public TaskCreatorView() {
            InitializeComponent();
        }

        public void Dispose() {
            if (MapViewControl is IDisposable mapDisposable)
                mapDisposable.Dispose();

            if (CalculatorViewControl is IDisposable calcDisposable)
                calcDisposable.Dispose();
        }
    }
}
