using marine_nav_trainer.Calculators.Core.Abstractions;
using marine_nav_trainer.Calculators.Core.Factory;
using marine_nav_trainer.Calculators.Modules.ObservedPosition;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Calculators.UI.Views.Tabs {
    public partial class ObservedPositionTabView : UserControl, ICoordinatePointProducer {
        private readonly ICalculator<ObservedPositionInput, ObservedPositionResult> _observedPositionCalc;

        public event EventHandler<InsertPointEventArgs>? InsertPointRequested;

        public ObservedPositionTabView() {
            InitializeComponent();

            var calculatorFactory = new CalculatorFactory();
            calculatorFactory.Register<ObservedPositionInput, ObservedPositionResult>(CalculatorType.ObservedPosition, new ObservedPositionCalculator());
            _observedPositionCalc = calculatorFactory.Get<ObservedPositionInput, ObservedPositionResult>(CalculatorType.ObservedPosition);
        }

        private void Oblicz_Click(object sender, RoutedEventArgs e) {
            if (PoiALat.Coordinate == null) { MessageBox.Show("Pole \"Szerokość punktu 1\" nie może być puste!"); return; }
            if (PoiALon.Coordinate == null) { MessageBox.Show("Pole \"Długość punktu 1\" nie może być puste!"); return; }
            if (BearingA.Value == null) { MessageBox.Show("Pole \"Namiar punktu 1\" nie może być puste!"); return; }
            if (PoiBLat.Coordinate == null) { MessageBox.Show("Pole \"Szerokość punktu 2\" nie może być puste!"); return; }
            if (PoiBLon.Coordinate == null) { MessageBox.Show("Pole \"Długość punktu 2\" nie może być puste!"); return; }
            if (BearingB.Value == null) { MessageBox.Show("Pole \"Namiar punktu 2\" nie może być puste!"); return; }

            var result = _observedPositionCalc.Calculate(new ObservedPositionInput {
                PoiALat = PoiALat.Coordinate.Value,
                PoiALon = PoiALon.Coordinate.Value,
                BearingA = (double)BearingA.Value,
                PoiBLat = PoiBLat.Coordinate.Value,
                PoiBLon = PoiBLon.Coordinate.Value,
                BearingB = (double)BearingB.Value,
            });

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
