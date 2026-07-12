using marine_nav_trainer.Calculators.Core.Abstractions;
using marine_nav_trainer.Calculators.Core.Factory;
using marine_nav_trainer.Calculators.Modules.ObservedPosition;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Calculators.UI.Views.Tabs {
    public partial class ObservedPositionTabView : UserControl {
        private readonly ICalculator<ObservedPositionInput, ObservedPositionResult> _observedPositionCalc;
        public ObservedPositionTabView() {
            InitializeComponent();

            var calculatorFactory = new CalculatorFactory();
            calculatorFactory.Register<ObservedPositionInput, ObservedPositionResult>(CalculatorType.ObservedPosition, new ObservedPositionCalculator());
            _observedPositionCalc = calculatorFactory.Get<ObservedPositionInput, ObservedPositionResult>(CalculatorType.ObservedPosition);
        }

        private void Oblicz_Click(object sender, RoutedEventArgs e) {
            if (PoiALat.Value == null) { MessageBox.Show("Pole \"Szerokość punktu 1\" nie może być puste!"); return; }
            if (PoiALon.Value == null) { MessageBox.Show("Pole \"Długość punktu 1\" nie może być puste!"); return; }
            if (BearingA.Value == null) { MessageBox.Show("Pole \"Namiar punktu 1\" nie może być puste!"); return; }
            if (PoiBLat.Value == null) { MessageBox.Show("Pole \"Szerokość punktu 2\" nie może być puste!"); return; }
            if (PoiBLon.Value == null) { MessageBox.Show("Pole \"Długość punktu 2\" nie może być puste!"); return; }
            if (BearingB.Value == null) { MessageBox.Show("Pole \"Namiar punktu 2\" nie może być puste!"); return; }

            var result = _observedPositionCalc.Calculate(new ObservedPositionInput {
                PoiALat = (double)PoiALat.Value,
                PoiALon = (double)PoiALon.Value,
                BearingA = (double)BearingA.Value,
                PoiBLat = (double)PoiBLat.Value,
                PoiBLon = (double)PoiBLon.Value,
                BearingB = (double)BearingB.Value,
            });

            ObservedPosLat.Value = result.Lat;
            ObservedPosLon.Value = result.Lon;
        }
    }
}
