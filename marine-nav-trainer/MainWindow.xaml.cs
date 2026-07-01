using marine_nav_trainer.Map;
using marine_nav_trainer.Windows.Views;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace marine_nav_trainer {
    public partial class MainWindow : Window {
        private TaskCreatorView? _taskCreatorView;
        public MainWindow() {
            InitializeComponent();
        }

        private void NavItemOnClick(object sender, MouseButtonEventArgs e) {
            if (sender is NavigationViewItem item) {
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
                    case "solveExercise":
                        SolveExercise();
                        break;
                }
            }
        }

        private void CreateExercise() {
            DisposeCurrentContent();
            _taskCreatorView = new TaskCreatorView();
            MainContent.Content = _taskCreatorView;
        }

        private void CloseFrame() {
            DisposeCurrentContent();
        }

        private void SolveExercise() {
            DisposeCurrentContent();
            MainContent.Content = new MapView();
        }

        private void DisposeCurrentContent() {
            if (MainContent.Content is IDisposable disposable)
                disposable.Dispose();
            MainContent.Content = null;
            _taskCreatorView = null;
        }

    }
}