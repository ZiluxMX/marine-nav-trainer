using marine_nav_trainer.Calculators.Core.Abstractions;
using marine_nav_trainer.Calculators.Core.Factory;
using marine_nav_trainer.Calculators.Modules.DeadReckoning;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Calculators.UI.Views.Tabs {
    public partial class DeadReckoningTabView : UserControl, ICoordinatePointProducer {
        private readonly ICalculator<DeadReckoningInput, DeadReckoningResult> _deadReckoningCalc;

        public event EventHandler<InsertPointEventArgs>? InsertPointRequested;

        public DeadReckoningTabView() {
            InitializeComponent();

            var calculatorFactory = new CalculatorFactory();
            calculatorFactory.Register<DeadReckoningInput, DeadReckoningResult>(CalculatorType.DeadReckoning, new DeadReckoningCalculator());
            _deadReckoningCalc = calculatorFactory.Get<DeadReckoningInput, DeadReckoningResult>(CalculatorType.DeadReckoning);
        }

        private void Oblicz_Click(object sender, RoutedEventArgs e) {
            if (StartLat.Coordinate == null) { MessageBox.Show("Pole \"Szerokość statku\" nie może być puste!"); return; }
            if (StartLon.Coordinate == null) { MessageBox.Show("Pole \"Długość statku\" nie może być puste!"); return; }
            if (Kdd.Value == null) { MessageBox.Show("Pole \"Kurs (KDD)\" nie może być puste!"); return; }
            if (DistanceNm.Value == null) { MessageBox.Show("Pole \"Droga (Nm)\" nie może być puste!"); return; }

            var result = _deadReckoningCalc.Calculate(new DeadReckoningInput {
                StartLat = StartLat.Coordinate.Value,
                StartLon = StartLon.Coordinate.Value,
                Kdd = (double)Kdd.Value,
                DistanceNm = (double)DistanceNm.Value
            });

            DeadReckoningLat.Coordinate = result.Lat;
            DeadReckoningLon.Coordinate = result.Lon;
        }

        private void WstawPunkt_Click(object sender, RoutedEventArgs e) {
            if (DeadReckoningLat.Coordinate is not { } lat || DeadReckoningLon.Coordinate is not { } lon) {
                MessageBox.Show("Najpierw oblicz pozycję.");
                return;
            }
            InsertPointRequested?.Invoke(this, new InsertPointEventArgs(lat.DecimalDegrees, lon.DecimalDegrees));
        }
    }
}
