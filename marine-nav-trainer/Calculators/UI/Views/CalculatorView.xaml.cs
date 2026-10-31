using marine_nav_trainer.Calculators.UI.Views.Tabs;
using marine_nav_trainer.Navigation.Models;
using System.Windows.Controls;
using marine_nav_trainer.Calculators.UI;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace marine_nav_trainer.Calculators.UI.Views {
    public partial class CalculatorView : UserControl, IDisposable {
        private readonly Dictionary<string, UserControl> _viewCache = [];
        private bool _disposed;

        public event EventHandler<InsertPointEventArgs>? InsertPointRequested;
        public event EventHandler<NavigationData>? NavigationDataRequested;

        public CalculatorView() {
            InitializeComponent();
            CalculatorContent.Content = GetView("course");
        }

        private void NavItemOnClick(object sender, MouseButtonEventArgs e) {
            if (sender is NavigationViewItem item && item.Tag is string key) {
                CalculatorContent.Content = GetView(key);
            }
        }

        private void OnTabInsertPointRequested(object? sender, InsertPointEventArgs e) {
            InsertPointRequested?.Invoke(this, e);
        }

        // zakładka pyta o tabele i deklinację - dane siedzą w ramkach zadania
        private void OnTabNavigationDataRequested(object? sender, NavigationData navigation) {
            NavigationDataRequested?.Invoke(this, navigation);
        }

        private UserControl GetView(string key) {
            if (!_viewCache.TryGetValue(key, out var view)) {
                view = key switch {
                    "apparentWind" => new ApparentWindTabView(),
                    "course" => new CourseTabView(),
                    "crossbar" => new CrossbarTabView(),
                    "deadReckoning" => new DeadReckoningTabView(),
                    "observedPosition" => new ObservedPositionTabView(),
                    "runningFix" => new RunningFixTabView(),
                    _ => throw new Exception("Unknown view")
                };
                if (view is ICoordinatePointProducer producer)
                    producer.InsertPointRequested += OnTabInsertPointRequested;

                if (view is INavigationDataUser navigationUser)
                    navigationUser.NavigationDataRequested += OnTabNavigationDataRequested;

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
