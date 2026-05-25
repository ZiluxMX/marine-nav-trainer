using marine_nav_trainer.Calculators.Core.Abstractions;
using marine_nav_trainer.Calculators.Core.Factory;
using marine_nav_trainer.Calculators.Modules.DeadReckoning;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Calculators.UI.Views.Tabs {
    public partial class DeadReckoningTabView : UserControl {
        private readonly ICalculator<DeadReckoningInput, DeadReckoningResult> _deadReckoningCalc;
        public DeadReckoningTabView() {
            InitializeComponent();

            var calculatorFactory = new CalculatorFactory();
            calculatorFactory.Register<DeadReckoningInput, DeadReckoningResult>(CalculatorType.DeadReckoning, new DeadReckoningCalculator());
            _deadReckoningCalc = calculatorFactory.Get<DeadReckoningInput, DeadReckoningResult>(CalculatorType.DeadReckoning);
        }
        private void Oblicz_Click(object sender, RoutedEventArgs e) {
            double? positionLat, positionLon, kdd, distance;
            if (StartLat.Value == null) {
                MessageBox.Show("Pole \"Szerokość statku\" nie może być puste!");
                return;
            }
            if (StartLon.Value == null) {
                MessageBox.Show("Pole \"Długość statku\" nie może być puste!");
                return;
            }
            if (Kdd.Value == null) {
                MessageBox.Show("Pole \"Kurs (KDD)\" nie może być puste!");
                return;
            }
            if (DistanceNm.Value == null) {
                MessageBox.Show("Pole \"Droga (Nm)\" nie może być puste!");
                return;
            }
            positionLat = StartLat.Value;
            positionLon = StartLon.Value;
            kdd = Kdd.Value;
            distance = DistanceNm.Value;

            var deadReckoningPos = _deadReckoningCalc.Calculate(new DeadReckoningInput {
                StartLat = (double)positionLat,
                StartLon = (double)positionLon,
                Kdd = (double)kdd,
                DistanceNm = (double)distance
            });
            DeadReckoningLat.Value = deadReckoningPos.Lat;
            DeadReckoningLon.Value = deadReckoningPos.Lon;
        }
    }
}
