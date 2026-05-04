using marine_nav_trainer.Calculators.UI.Views.Tabs;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace marine_nav_trainer.Calculators.UI.Views {
    public partial class CalculatorView : UserControl {
        private readonly Dictionary<string, UserControl> _viewCache = [];
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
                    _ => throw new Exception("Unknown view")
                };
                _viewCache[key] = view;
            }
            return view;
        }
    }
}
