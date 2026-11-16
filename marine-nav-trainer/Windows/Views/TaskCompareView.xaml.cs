using marine_nav_trainer.Navigation.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace marine_nav_trainer.Windows.Views {
    // widok porównania zadań - mapa nauczyciela bez edycji, z kalkulatorami
    public partial class TaskCompareView : UserControl, IDisposable {
        private bool _disposed;
        private TaskMeta? _referenceMeta;
        private NavigationData? _referenceNavigation;

        public event EventHandler? CloseRequested;

        public TaskCompareView() {
            InitializeComponent();
            MapViewControl.IsCompareMode = true;
            CalculatorViewControl.IsInsertPointEnabled = false;
            CalculatorViewControl.NavigationDataRequested += OnCalculatorNavigationDataRequested;
            MapViewControl.FrameVisibilityChanged += OnFrameVisibilityChanged;
            MapViewControl.TaskMetaLoaded += OnTaskMetaLoaded;
            MapViewControl.NavigationDataLoaded += OnNavigationDataLoaded;
            MapViewControl.CheckedTaskMetaLoaded += OnCheckedTaskMetaLoaded;
            MapViewControl.CheckedNavigationDataLoaded += OnCheckedNavigationDataLoaded;
            TaskSelectionViewControl.CompareRequested += OnCompareRequested;

            foreach (MapFrame frame in Enum.GetValues<MapFrame>())
                ApplyFrameVisibility(frame);
        }

        // [Porównaj zadania] - obie trasy na mapie wzorca
        private void OnCompareRequested(object? sender, EventArgs e) {
            string? referencePath = TaskSelectionViewControl.ReferenceTaskPath;
            string? checkedPath = TaskSelectionViewControl.CheckedTaskPath;
            if (referencePath == null || checkedPath == null)
                return;

            MapViewControl.LoadCompareTasks(referencePath, checkedPath);
        }

        private void OnFrameVisibilityChanged(object? sender, MapFrame frame) => ApplyFrameVisibility(frame);

        private void ApplyFrameVisibility(MapFrame frame) {
            UIElement control = frame switch {
                MapFrame.TaskDescription => TaskDescriptionViewControl,
                MapFrame.Calculators => CalculatorViewControl,
                MapFrame.DeviationTable => DeviationTableViewControl,
                MapFrame.LeewayTable => LeewayTableViewControl,
                MapFrame.VariationTable => VariationViewControl,
                _ => throw new Exception("Nieznana ramka")
            };

            control.Visibility = MapViewControl.IsFrameVisible(frame) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnTaskMetaLoaded(object? sender, TaskMeta meta) {
            TaskTitleText.Text = string.IsNullOrWhiteSpace(meta.Title)
                ? "Zadanie"
                : $"Zadanie: {meta.Title}";

            _referenceMeta = meta;
        }

        // dane nawigacyjne zadania wzorcowego - tylko do odczytu
        private void OnNavigationDataLoaded(object? sender, NavigationData navigation) {
            _referenceNavigation = navigation;
        }

        // ramki dostają oba zadania i same decydują, czy pokazać drugie
        private void OnCheckedTaskMetaLoaded(object? sender, TaskMeta meta) {
            if (_referenceMeta != null)
                TaskDescriptionViewControl.SetMeta(_referenceMeta, meta);
        }

        private void OnCheckedNavigationDataLoaded(object? sender, NavigationData navigation) {
            if (_referenceNavigation == null)
                return;

            DeviationTableViewControl.SetEntries(_referenceNavigation.DeviationTable, navigation.DeviationTable);
            LeewayTableViewControl.SetEntries(_referenceNavigation.Leeway, navigation.Leeway);
            VariationViewControl.SetNavigation(_referenceNavigation, navigation);
        }

        // kalkulator pyta o tabele i deklinację - bierze je z wczytanego zadania
        private void OnCalculatorNavigationDataRequested(object? sender, NavigationData navigation) {
            if (_referenceNavigation == null)
                return;

            navigation.DeviationTable = _referenceNavigation.DeviationTable;
            navigation.Leeway = _referenceNavigation.Leeway;
            navigation.Variation = _referenceNavigation.Variation;
            navigation.VariationList = _referenceNavigation.VariationList;
            navigation.TimeZone = _referenceNavigation.TimeZone;
            navigation.StartDate = _referenceNavigation.StartDate;
        }

        // naprawa scrolla
        private void SidePanelScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            var source = e.OriginalSource as DependencyObject;

            // CmbBox przejmuje priorytet scrolla
            if (IsInsidePopup(source))
                return;

            if (!IsInsideSidePanel(source))
                return;

            SidePanelScrollViewer.ScrollToVerticalOffset(SidePanelScrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        // rozwinięta lista ComboBoxa - scroll musi zostać przy liście
        private static bool IsInsidePopup(DependencyObject? source) {
            while (source != null) {
                if (source is System.Windows.Controls.Primitives.Popup)
                    return true;

                source = GetParentObject(source);
            }

            return false;
        }

        private bool IsInsideSidePanel(DependencyObject? source) {
            while (source != null) {
                if (ReferenceEquals(source, SidePanelScrollViewer))
                    return true;

                source = GetParentObject(source);
            }

            return false;
        }

        // źródłem zdarzenia bywa element treści - VisualTreeHelper rzuca na nim wyjątkiem
        private static DependencyObject? GetParentObject(DependencyObject source) {
            if (source is Visual or System.Windows.Media.Media3D.Visual3D)
                return VisualTreeHelper.GetParent(source) ?? LogicalTreeHelper.GetParent(source);

            return LogicalTreeHelper.GetParent(source);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) =>
            CloseRequested?.Invoke(this, EventArgs.Empty);

        public void Dispose() {
            if (_disposed)
                return;
            _disposed = true;

            CalculatorViewControl.NavigationDataRequested -= OnCalculatorNavigationDataRequested;
            MapViewControl.FrameVisibilityChanged -= OnFrameVisibilityChanged;
            MapViewControl.TaskMetaLoaded -= OnTaskMetaLoaded;
            MapViewControl.NavigationDataLoaded -= OnNavigationDataLoaded;
            MapViewControl.CheckedTaskMetaLoaded -= OnCheckedTaskMetaLoaded;
            MapViewControl.CheckedNavigationDataLoaded -= OnCheckedNavigationDataLoaded;
            TaskSelectionViewControl.CompareRequested -= OnCompareRequested;

            if (MapViewControl is IDisposable mapDisposable)
                mapDisposable.Dispose();

            if (CalculatorViewControl is IDisposable calcDisposable)
                calcDisposable.Dispose();
        }
    }
}
