using marine_nav_trainer.Calculators.Core.Abstractions;
using marine_nav_trainer.Calculators.Core.Factory;
using marine_nav_trainer.Calculators.Modules.RunningFix;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Calculators.UI.Views.Tabs {
    public partial class RunningFixTabView : UserControl, ICoordinatePointProducer {
        private readonly ICalculator<RunningFixInput, RunningFixResult> _runningFixCalc;

        public event EventHandler<InsertPointEventArgs>? InsertPointRequested;

        public RunningFixTabView() {
            InitializeComponent();

            var calculatorFactory = new CalculatorFactory();
            calculatorFactory.Register<RunningFixInput, RunningFixResult>(CalculatorType.RunningFix, new RunningFixCalculator());
            _runningFixCalc = calculatorFactory.Get<RunningFixInput, RunningFixResult>(CalculatorType.RunningFix);
        }

        private void Oblicz_Click(object sender, RoutedEventArgs e) {
            if (PoiLat.Coordinate == null) {
                MessageBox.Show("Pole \"Szerokość punktu\" nie może być puste!");
                return;
            }
            if (PoiLon.Coordinate == null) {
                MessageBox.Show("Pole \"Długość punktu\" nie może być puste!");
                return;
            }
            if (BearingA.Value == null) {
                MessageBox.Show("Pole \"Namiar 1 na punkt\" nie może być puste!");
                return;
            }
            if (BearingB.Value == null) {
                MessageBox.Show("Pole \"Namiar 2 na punkt\" nie może być puste!");
                return;
            }
            if (Kdd.Value == null) {
                MessageBox.Show("Pole \"KdD\" nie może być puste!");
                return;
            }
            if (Distance.Value == null) {
                MessageBox.Show("Pole \"Dystans (Nm)\" nie może być puste!");
                return;
            }
            if (Distance.Value == 0) {
                MessageBox.Show("Wartość w Polu \"Dystans (Nm)\" nie może być równa 0!");
                return;
            }

            RunningFixResult result;
            try {
                result = _runningFixCalc.Calculate(new RunningFixInput {
                    PoiLat = PoiLat.Coordinate.Value,
                    PoiLon = PoiLon.Coordinate.Value,
                    BearingA = (double)BearingA.Value,
                    BearingB = (double)BearingB.Value,
                    Kdd = (double)Kdd.Value,
                    DistanceNm = (double)Distance.Value,
                });
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, "Nie można wyznaczyć pozycji",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ObservedPosLat.Coordinate = result.Lat;
            ObservedPosLon.Coordinate = result.Lon;
        }

        private void WstawPunkt_Click(object sender, RoutedEventArgs e) {
            if (ObservedPosLat.Coordinate is not { } lat || ObservedPosLon.Coordinate is not { } lon) {
                MessageBox.Show("Najpierw oblicz pozycję.");
                return;
            }
            InsertPointRequested?.Invoke(this, new InsertPointEventArgs(lat.DecimalDegrees, lon.DecimalDegrees));
        }
    }
}
