using marine_nav_trainer.Calculators.Core.Factory;
using marine_nav_trainer.Calculators.Modules.Crossbar;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Calculators.UI.Views.Tabs {
    public partial class CrossbarTabView : UserControl {
        private CalculatorFactory? _calculatorFactory;

        public CrossbarTabView() {
            InitializeComponent();
        }
        private void Oblicz_Click(object sender, RoutedEventArgs e) {
            double? positionLat, positionLon, poiLat, poiLon, kdd;
            if (PositionLat.Value == null) {
                MessageBox.Show("Pole \"Szerokość statk\" nie może być puste!");
                return;
            }
            if (PositionLon.Value == null) {
                MessageBox.Show("Pole \"Długość statk\" nie może być puste!");
                return;
            }
            if (PoiLat.Value == null) {
                MessageBox.Show("Pole \"Szerokość punktu\" nie może być puste!");
                return;
            }
            if (PoiLon.Value == null) {
                MessageBox.Show("Pole \"Długość punktu\" nie może być puste!");
                return;
            }
            if (Kdd.Value == null) {
                MessageBox.Show("Pole \"Kurs (KDD)\" nie może być puste!");
                return;
            }
            positionLat = PositionLat.Value;
            positionLon = PositionLon.Value;
            poiLat = PoiLat.Value;
            poiLon = PoiLon.Value;
            kdd = Kdd.Value;

            _calculatorFactory = new CalculatorFactory();
            _calculatorFactory.Register<CrossbarInput, CrossbarResult>(CalculatorType.Crossbar, new CrossbarCalculator());
            var crossbarCalc = _calculatorFactory.Get<CrossbarInput, CrossbarResult>(CalculatorType.Crossbar);

            var crossbar = crossbarCalc.Calculate(new CrossbarInput {
                PositionLat = (double)positionLat,
                PositionLon = (double)positionLon,
                PoiLat = (double)poiLat,
                PoiLon = (double)poiLon,
                Kdd = (double)kdd
            });
            CrossbarLat.Value = crossbar.Lat;
            CrossbarLon.Value = crossbar.Lon;
        }
    }
}
