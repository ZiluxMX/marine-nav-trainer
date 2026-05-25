using marine_nav_trainer.Calculators.Core.Abstractions;
using marine_nav_trainer.Calculators.Core.Factory;
using marine_nav_trainer.Calculators.Modules.ApparentWind;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Calculators.UI.Views.Tabs {
    public partial class ApparentWindTabView : UserControl {
        private readonly ICalculator<ApparentWindInput, ApparentWindResult> _apparentWindCalc;
        public ApparentWindTabView() {
            InitializeComponent();

            var calculatorFactory = new CalculatorFactory();
            calculatorFactory.Register<ApparentWindInput, ApparentWindResult>(CalculatorType.ApparentWind, new ApparentWindCalculator());
            _apparentWindCalc = calculatorFactory.Get<ApparentWindInput, ApparentWindResult>(CalculatorType.ApparentWind);
        }

        private void Oblicz_Click(object sender, RoutedEventArgs e) {
            double? boatSpeed, boatHeading, windSpeed, windDirection;
            if (BoatSpeed.Value == null) {
                BoatSpeed.Value = 1;
            }
            if (BoatHeading.Value == null) {
                MessageBox.Show("Pole \"Kurs rzeczywisty statku\" nie może być puste!");
                return;
            }
            if (WindSpeed.Value == null) {
                WindSpeed.Value = 1;
            }
            if (WindDirection.Value == null) {
                MessageBox.Show("Pole \"Kierunek wiatru\" nie może być puste!");
                return;
            }
            boatSpeed = BoatSpeed.Value;
            boatHeading = BoatHeading.Value;
            windSpeed = WindSpeed.Value;
            windDirection = WindDirection.Value;

            var apparentWind = _apparentWindCalc.Calculate(new ApparentWindInput {
                BoatSpeed = (double)boatSpeed,
                BoatHeading = (double)boatHeading,
                WindSpeed = (double)windSpeed,
                WindDirection = (double)windDirection
            });
            CompasWindDirection.Value = apparentWind.CompasWindDirection;
            ApparentWindDirection.Value = apparentWind.ApparentWindDirection;
        }
    }
}
