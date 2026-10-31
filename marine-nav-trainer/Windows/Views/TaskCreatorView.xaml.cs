using marine_nav_trainer.Calculators.UI;
using marine_nav_trainer.Navigation.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace marine_nav_trainer.Windows.Views {
    public partial class TaskCreatorView : UserControl, IDisposable {
        private bool _disposed;

        public event EventHandler? CloseRequested;

        public TaskCreatorView() {
            InitializeComponent();
            CalculatorViewControl.InsertPointRequested += OnInsertPointRequested;
            CalculatorViewControl.NavigationDataRequested += OnNavigationDataRequested;
            MapViewControl.FrameVisibilityChanged += OnFrameVisibilityChanged;
            MapViewControl.NavigationDataRequested += OnNavigationDataRequested;
            MapViewControl.NavigationDataLoaded += OnNavigationDataLoaded;
            MapViewControl.TaskMetaRequested += OnTaskMetaRequested;
            MapViewControl.TaskMetaLoaded += OnTaskMetaLoaded;
            VariationTableViewControl.StartDateChanged += OnStartDateChanged;

            foreach (MapFrame frame in Enum.GetValues<MapFrame>())
                ApplyFrameVisibility(frame);
        }

        private void OnFrameVisibilityChanged(object? sender, MapFrame frame) {
            ApplyFrameVisibility(frame);
        }

        private void ApplyFrameVisibility(MapFrame frame) {
            UIElement control = frame switch {
                MapFrame.TaskDescription => TaskDescriptionViewControl,
                MapFrame.Calculators => CalculatorViewControl,
                MapFrame.DeviationTable => DeviationTableViewControl,
                MapFrame.LeewayTable => LeewayTableViewControl,
                MapFrame.VariationTable => VariationTableViewControl,
                _ => throw new Exception("Nieznana ramka")
            };

            control.Visibility = MapViewControl.IsFrameVisible(frame) ? Visibility.Visible : Visibility.Collapsed;
        }

        // zapis zadania - dane z ramek
        private void OnNavigationDataRequested(object? sender, NavigationData navigation) {
            navigation.DeviationTable = DeviationTableViewControl.GetEntries();
            navigation.Leeway = LeewayTableViewControl.GetEntries();

            navigation.Variation = VariationTableViewControl.GetVariation();
            navigation.VariationList = VariationTableViewControl.GetLocalVariations();
            navigation.TimeZone = VariationTableViewControl.GetTimeZone();
            navigation.StartDate = VariationTableViewControl.StartDate;
        }

        // wczytanie zadania - dane do ramek
        private void OnNavigationDataLoaded(object? sender, NavigationData navigation) {
            DeviationTableViewControl.SetEntries(navigation.DeviationTable);
            LeewayTableViewControl.SetEntries(navigation.Leeway);

            VariationTableViewControl.SetVariation(navigation.Variation);
            VariationTableViewControl.SetLocalVariations(navigation.VariationList);
            VariationTableViewControl.SetTimeZone(navigation.TimeZone);
            VariationTableViewControl.StartDate = navigation.StartDate;
        }

        private void OnTaskMetaRequested(object? sender, TaskMeta meta) {
            TaskDescriptionViewControl.FillMeta(meta);
        }

        // data z ramki - domyślna data nowych punktów
        private void OnStartDateChanged(object? sender, DateTime date) {
            MapViewControl.SetDefaultPointDate(date);
        }

        private void OnTaskMetaLoaded(object? sender, TaskMeta meta) {
            TaskDescriptionViewControl.SetMeta(meta);
        }

        // ramki nie mają własnego przewijania - scroll rusza całym panelem
        private void SidePanelScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            SidePanelScroll.OnMouseWheel(SidePanelScrollViewer, e);
        }

        private void OnInsertPointRequested(object? sender, InsertPointEventArgs e) {
            bool inserted = MapViewControl.TryInsertPoint(e.Lat, e.Lon);
            if (!inserted)
                MessageBox.Show(
                    "Punkt nie mieści się na mapie.",
                    "Wstaw punkt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose() {
            if (_disposed)
                return;
            _disposed = true;

            CalculatorViewControl.InsertPointRequested -= OnInsertPointRequested;
            CalculatorViewControl.NavigationDataRequested -= OnNavigationDataRequested;
            MapViewControl.FrameVisibilityChanged -= OnFrameVisibilityChanged;
            MapViewControl.NavigationDataRequested -= OnNavigationDataRequested;
            MapViewControl.NavigationDataLoaded -= OnNavigationDataLoaded;
            MapViewControl.TaskMetaRequested -= OnTaskMetaRequested;
            MapViewControl.TaskMetaLoaded -= OnTaskMetaLoaded;
            VariationTableViewControl.StartDateChanged -= OnStartDateChanged;

            if (MapViewControl is IDisposable mapDisposable)
                mapDisposable.Dispose();

            if (CalculatorViewControl is IDisposable calcDisposable)
                calcDisposable.Dispose();
        }
    }
}
