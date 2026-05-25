using marine_nav_trainer.Calculators.Core.Abstractions;
using marine_nav_trainer.Calculators.Core.Factory;
using marine_nav_trainer.Calculators.Modules.RunningFix;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Calculators.UI.Views.Tabs {
    public partial class RunningFixTabView : UserControl {
        private readonly ICalculator<RunningFixInput, RunningFixResult> _runningFixCalc;
        public RunningFixTabView() {
            InitializeComponent();

            var calculatorFactory = new CalculatorFactory();
            calculatorFactory.Register<RunningFixInput, RunningFixResult>(CalculatorType.RunningFix, new RunningFixCalculator());
            _runningFixCalc = calculatorFactory.Get<RunningFixInput, RunningFixResult>(CalculatorType.RunningFix);
        }
        private void Oblicz_Click(object sender, RoutedEventArgs e) {
            double? poiLat, poiLon, bearingA, bearingB, kdd, distance;
            if (PoiLat.Value == null) {
                MessageBox.Show("Pole \"Szerokość punktu\" nie może być puste!");
                return;
            }
            if (PoiLon.Value == null) {
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
                MessageBox.Show("Pole \"Dystans(Nm)\" nie może być puste!");
                return;
            }

            poiLat = PoiLat.Value;
            poiLon = PoiLon.Value;
            bearingA = BearingA.Value;
            bearingB = BearingB.Value;
            kdd = Kdd.Value;
            distance = Distance.Value;

            var observedPos = _runningFixCalc.Calculate(new RunningFixInput {
                PoiLat = (double)poiLat,
                PoiLon = (double)poiLon,
                BearingA = (double)bearingA,
                BearingB = (double)bearingB,
                Kdd = (double)kdd,
                DistanceNm = (double)distance,
            });
            ObservedPosLat.Value = observedPos.Lat;
            ObservedPosLon.Value = observedPos.Lon;
        }
    }
}
