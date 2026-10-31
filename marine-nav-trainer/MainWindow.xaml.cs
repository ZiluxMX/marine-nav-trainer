using marine_nav_trainer.Map;
using marine_nav_trainer.Map.UI.Views;
using marine_nav_trainer.Windows.Views;
using System.Windows.Controls;
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
                    case "compareExercise":
                        CompareExercise();
                        break;
                    case "fileList":
                        OpenFileList();
                        break;
                    case "showControlsHelp":
                        HelpControlsWindow.ShowSingle(this);
                        break;
                    case "showAbout":
                        AboutWindow.ShowSingle(this);
                        break;
                }
                CollapseMenu(item);
            }
        }

        private static void CollapseMenu(NavigationViewItem item) {
            item.IsActive = false;

            var parent = item.NavigationViewItemParent as NavigationViewItem;
            while (parent != null) {
                parent.IsExpanded = false;
                parent.IsActive = false;
                parent = parent.NavigationViewItemParent as NavigationViewItem;
            }
        }

        private void CreateExercise() {
            DisposeCurrentContent();
            _taskCreatorView = new TaskCreatorView();
            _taskCreatorView.CloseRequested += OnCloseRequested;
            SetContent(_taskCreatorView);
        }

        private void CloseFrame() {
            DisposeCurrentContent();
        }

        // uczeń rozwiązuje zadanie - mapa startuje dopiero po poprawnym wczytaniu pliku
        private void SolveExercise() {
            var dialog = new TaskSelectionWindow(AppPaths.JsonsDir) {
                Owner = this
            };

            if (dialog.ShowDialog() != true || dialog.SelectedTaskPath == null)
                return;

            var view = new TaskSolveView();
            if (!view.TryLoadTask(dialog.SelectedTaskPath)) {
                view.Dispose();
                return;
            }

            DisposeCurrentContent();
            view.CloseRequested += OnCloseRequested;
            SetContent(view);
        }

        // porównanie zadania
        private void CompareExercise() {
            var box = new Wpf.Ui.Controls.MessageBox {
                Title = "Info",
                Content = "Okno jeszcze w budowie :)",
                CloseButtonText = "OK",
                Owner = this
            };
            box.ShowDialogAsync();

            //var dialog = new TaskSelectionWindow(AppPaths.JsonsDir) {
            //    Owner = this
            //};

            //if (dialog.ShowDialog() != true || dialog.SelectedTaskPath == null)
            //    return;

            //var view = new TaskCompareView();
            //if (!view.TryLoadTask(dialog.SelectedTaskPath)) {
            //    view.Dispose();
            //    return;
            //}

            //DisposeCurrentContent();
            //view.CloseRequested += OnCloseRequested;
            //SetContent(view);
        }

        private void OpenFileList() {
            DisposeCurrentContent();
            var view = new FileListView();
            view.CloseRequested += OnCloseRequested;
            SetContent(view);
        }

        private void OnCloseRequested(object? sender, EventArgs e) {
            DisposeCurrentContent();
        }

        private void SetContent(object content) {
            MainContent.Content = content;
            MainWindowMapBackground.Visibility = Visibility.Collapsed;
        }

        private void DisposeCurrentContent() {
            if (MainContent.Content is IDisposable disposable)
                disposable.Dispose();
            MainContent.Content = null;
            _taskCreatorView = null;
            MainWindowMapBackground.Visibility = Visibility.Visible;
        }

    }
}