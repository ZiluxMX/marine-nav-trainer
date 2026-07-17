using marine_nav_trainer.Calculators.UI.Views.Tabs;
using System.Windows.Controls;
using marine_nav_trainer.Calculators.UI;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace marine_nav_trainer.Calculators.UI.Views {
    public partial class CalculatorView : UserControl, IDisposable {
        private readonly Dictionary<string, UserControl> _viewCache = [];
        private bool _disposed;

        public event EventHandler<InsertPointEventArgs>? InsertPointRequested;

        public CalculatorView() {
            InitializeComponent();
            CalculatorContent.Content = GetView("apparentWind");
        }

        private void NavItemOnClick(object sender, MouseButtonEventArgs e) {
            if (sender is NavigationViewItem item && item.Tag is string key) {
                CalculatorContent.Content = GetView(key);
            }
        }

        private UserControl GetView(string key) {
            if (!_viewCache.TryGetValue(key, out var view)) {
                view = key switch {
                    "apparentWind" => new ApparentWindTabView(),
                    "crossbar" => new CrossbarTabView(),
                    "deadReckoning" => new DeadReckoningTabView(),
                    "observedPosition" => new ObservedPositionTabView(),
                    "runningFix" => new RunningFixTabView(),
                    _ => throw new Exception("Unknown view")
                };
                if (view is ICoordinatePointProducer producer)
                    producer.InsertPointRequested += (_, args) => InsertPointRequested?.Invoke(this, args);
                _viewCache[key] = view;
            }
            return view;
        }

        public void Dispose() {
            if (_disposed)
                return;
            _disposed = true;

            CalculatorContent.Content = null;

            foreach (var view in _viewCache.Values)
                if (view is IDisposable disposable)
                    disposable.Dispose();

            _viewCache.Clear();
        }
    }
}
