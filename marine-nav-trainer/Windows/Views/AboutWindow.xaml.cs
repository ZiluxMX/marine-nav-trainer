using System.Reflection;
using System.Windows;

namespace marine_nav_trainer.Windows.Views {
    public partial class AboutWindow : Window {
        private static AboutWindow? _instance;

        public AboutWindow() {
            InitializeComponent();

            var assembly = Assembly.GetExecutingAssembly();

            // nazwa i wersja prosto z assembly - nie trzeba ich pilnować w dwóch miejscach
            ProductText.Text = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
                               ?? "Marine Nav Trainer";

            //string version = assembly.GetName().Version?.ToString(3) ?? "0.0.10";
            string version = "0.1.3";
            VersionText.Text = $"Wersja {version}";
        }

        public static void ShowSingle(Window owner) {
            if (_instance == null) {
                _instance = new AboutWindow { Owner = owner };
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
