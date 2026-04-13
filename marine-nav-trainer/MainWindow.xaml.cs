using marine_nav_trainer.Map;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace marine_nav_trainer {
    public partial class MainWindow : Window {
               
        public MainWindow() {
            InitializeComponent();
        }

        private void NavItemOnClick(object sender, MouseButtonEventArgs e) {
            if (sender is Wpf.Ui.Controls.NavigationViewItem item) {
                switch (item.Tag) {
                    case "closeAppllication":
                        Application.Current.Shutdown();
                        break;
                    case "createExercise":
                        CreateExercise();
                        break;
                    case "closeFrame":
                        CloseFrame();
                        break;
                }
            }
        }

        private void CreateExercise() {
            MainContent.Content = new MapView();
        }

        private void CloseFrame() {
            MainContent.Content = null;
        }
    }
}