using marine_nav_trainer.Navigation.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace marine_nav_trainer.Windows.Views {
    // rozwiązywanie zadania - tryb ucznia
    public partial class TaskSolveView : UserControl, IDisposable {
        private bool _disposed;

        public event EventHandler? CloseRequested;

        public TaskSolveView() {
            InitializeComponent();
            MapViewControl.IsTeacherMode = false;
            MapViewControl.FrameVisibilityChanged += OnFrameVisibilityChanged;
            MapViewControl.TaskMetaLoaded += OnTaskMetaLoaded;
            MapViewControl.NavigationDataLoaded += OnNavigationDataLoaded;
            MapViewControl.StudentSolutionRequested += OnStudentSolutionRequested;
            MapViewControl.TaskFinished += OnTaskFinished;
            TaskDescriptionViewControl.StartDateChanged += OnStartDateChanged;

            foreach (MapFrame frame in Enum.GetValues<MapFrame>())
                ApplyFrameVisibility(frame);
        }

        public bool TryLoadTask(string taskPath) {
            return MapViewControl.LoadTaskFromFile(taskPath);
        }

        private void OnFrameVisibilityChanged(object? sender, MapFrame frame) {
            ApplyFrameVisibility(frame);
        }

        private void ApplyFrameVisibility(MapFrame frame) {
            UIElement? control = frame switch {
                MapFrame.TaskDescription => TaskDescriptionViewControl,
                MapFrame.DeviationTable => DeviationTableViewControl,
                MapFrame.LeewayTable => LeewayTableViewControl,
                // kalkulatory i deklinacja - brak dla ucznia
                _ => null
            };

            if (control == null)
                return;

            control.Visibility = MapViewControl.IsFrameVisible(frame) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnTaskMetaLoaded(object? sender, TaskMeta meta) {
            if (string.IsNullOrWhiteSpace(meta.Title))
                TaskTitleText.Text = "Zadanie";
            else
                TaskTitleText.Text = $"Zadanie: {meta.Title}";

            TaskDescriptionViewControl.SetMeta(meta);
        }

        // wczytanie zadania - dane do ramek
        private void OnNavigationDataLoaded(object? sender, NavigationData navigation) {
            DeviationTableViewControl.SetEntries(navigation.DeviationTable);
            LeewayTableViewControl.SetEntries(navigation.Leeway);
        }

        // notatki ucznia
        private void OnStudentSolutionRequested(object? sender, StudentSolution solution) {
            solution.CommentMd = TaskDescriptionViewControl.Notes;
        }

        private void OnTaskFinished(object? sender, EventArgs e) {
            TaskDescriptionViewControl.LockInput();
        }

        // data z ramki - domyślna data nowych punktów
        private void OnStartDateChanged(object? sender, DateTime date) {
            MapViewControl.SetDefaultPointDate(date);
        }

        // ramki nie mają własnego przewijania - scroll rusza całym panelem
        private void SidePanelScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            SidePanelScroll.OnMouseWheel(SidePanelScrollViewer, e);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose() {
            if (_disposed)
                return;
            _disposed = true;

            MapViewControl.FrameVisibilityChanged -= OnFrameVisibilityChanged;
            MapViewControl.TaskMetaLoaded -= OnTaskMetaLoaded;
            MapViewControl.NavigationDataLoaded -= OnNavigationDataLoaded;
            MapViewControl.StudentSolutionRequested -= OnStudentSolutionRequested;
            MapViewControl.TaskFinished -= OnTaskFinished;
            TaskDescriptionViewControl.StartDateChanged -= OnStartDateChanged;

            if (MapViewControl is IDisposable mapDisposable)
                mapDisposable.Dispose();
        }
    }
}
