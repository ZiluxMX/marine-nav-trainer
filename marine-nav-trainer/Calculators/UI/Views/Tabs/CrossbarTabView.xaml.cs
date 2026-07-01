using marine_nav_trainer.Calculators.Core;
using marine_nav_trainer.Calculators.Core.Abstractions;
using marine_nav_trainer.Calculators.Core.Factory;
using marine_nav_trainer.Calculators.Modules.Crossbar;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Calculators.UI.Views.Tabs {
    public partial class CrossbarTabView : UserControl {
        private readonly ICalculator<CrossbarInput, CrossbarResult> _crossbarCalc;

        public CrossbarTabView() {
            InitializeComponent();

            var calculatorFactory = new CalculatorFactory();
            calculatorFactory.Register<CrossbarInput, CrossbarResult>(CalculatorType.Crossbar, new CrossbarCalculator());
            _crossbarCalc = calculatorFactory.Get<CrossbarInput, CrossbarResult>(CalculatorType.Crossbar);
        }

        private void Oblicz_Click(object sender, RoutedEventArgs e) {
            if (PositionLat.Value == null) { MessageBox.Show("Pole \"Szerokość statku\" nie może być puste!"); return; }
            if (PositionLon.Value == null) { MessageBox.Show("Pole \"Długość statku\" nie może być puste!"); return; }
            if (PoiLat.Value == null) { MessageBox.Show("Pole \"Szerokość punktu\" nie może być puste!"); return; }
            if (PoiLon.Value == null) { MessageBox.Show("Pole \"Długość punktu\" nie może być puste!"); return; }
            if (Kdd.Value == null) { MessageBox.Show("Pole \"Kurs (KDD)\" nie może być puste!"); return; }

            var result = _crossbarCalc.Calculate(new CrossbarInput {
                PositionLat = (double)PositionLat.Value,
                PositionLon = (double)PositionLon.Value,
                PoiLat = (double)PoiLat.Value,
                PoiLon = (double)PoiLon.Value,
                Kdd = (double)Kdd.Value
            });

            CrossbarLat.Value = result.Lat;
            CrossbarLatDegMin.Text = CoordinateFormatter.ToLatDegMin(result.Lat);

            CrossbarLon.Value = result.Lon;
            CrossbarLonDegMin.Text = CoordinateFormatter.ToLonDegMin(result.Lon);
        }
    }
}
