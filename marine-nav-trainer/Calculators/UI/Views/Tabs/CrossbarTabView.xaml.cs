using marine_nav_trainer.Calculators.Core.Abstractions;
using marine_nav_trainer.Calculators.Core.Factory;
using marine_nav_trainer.Calculators.Modules.Crossbar;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Calculators.UI.Views.Tabs {
    public partial class CrossbarTabView : UserControl, ICoordinatePointProducer {
        private readonly ICalculator<CrossbarInput, CrossbarResult> _crossbarCalc;

        public event EventHandler<InsertPointEventArgs>? InsertPointRequested;

        public CrossbarTabView() {
            InitializeComponent();

            var calculatorFactory = new CalculatorFactory();
            calculatorFactory.Register<CrossbarInput, CrossbarResult>(CalculatorType.Crossbar, new CrossbarCalculator());
            _crossbarCalc = calculatorFactory.Get<CrossbarInput, CrossbarResult>(CalculatorType.Crossbar);
        }

        private void Oblicz_Click(object sender, RoutedEventArgs e) {
            if (PositionLat.Coordinate == null) {
                MessageBox.Show("Pole \"Szerokość statku\" nie może być puste!");
                return;
            }
            if (PositionLon.Coordinate == null) {
                MessageBox.Show("Pole \"Długość statku\" nie może być puste!");
                return;
            }
            if (PoiLat.Coordinate == null) {
                MessageBox.Show("Pole \"Szerokość punktu\" nie może być puste!");
                return;
            }
            if (PoiLon.Coordinate == null) {
                MessageBox.Show("Pole \"Długość punktu\" nie może być puste!");
                return;
            }
            if (Kdd.Value == null) {
                MessageBox.Show("Pole \"Kurs (KDD)\" nie może być puste!");
                return;
            }

            var result = _crossbarCalc.Calculate(new CrossbarInput {
                PositionLat = PositionLat.Coordinate.Value,
                PositionLon = PositionLon.Coordinate.Value,
                PoiLat = PoiLat.Coordinate.Value,
                PoiLon = PoiLon.Coordinate.Value,
                Kdd = (double)Kdd.Value
            });

            CrossbarLat.Coordinate = result.Lat;
            CrossbarLon.Coordinate = result.Lon;
        }

        private void WstawPunkt_Click(object sender, RoutedEventArgs e) {
            if (CrossbarLat.Coordinate is not { } lat || CrossbarLon.Coordinate is not { } lon) {
                MessageBox.Show("Najpierw oblicz pozycję.");
                return;
            }
            InsertPointRequested?.Invoke(this, new InsertPointEventArgs(lat.DecimalDegrees, lon.DecimalDegrees));
        }
    }
}
