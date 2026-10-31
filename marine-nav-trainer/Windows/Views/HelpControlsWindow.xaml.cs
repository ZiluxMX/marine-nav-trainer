using System.Windows;

namespace marine_nav_trainer.Windows.Views {
    public partial class HelpControlsWindow : Window {
        private static HelpControlsWindow? _instance;

        public HelpControlsWindow() {
            InitializeComponent();
        }

        public static void ShowSingle(Window owner) {
            if (_instance == null) {
                _instance = new HelpControlsWindow { Owner = owner };
                _instance.Closed += (_, _) => _instance = null;
                _instance.Show();
                return;
            }

            if (_instance.WindowState == WindowState.Minimized)
                _instance.WindowState = WindowState.Normal;
            _instance.Activate();
        }
    }
}
