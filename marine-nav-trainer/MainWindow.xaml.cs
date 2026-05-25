using marine_nav_trainer.Calculators.Core.Factory;
using marine_nav_trainer.Calculators.Modules.Crossbar;
using marine_nav_trainer.Map;
using marine_nav_trainer.Windows.Views;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace marine_nav_trainer {
    public partial class MainWindow : Window {
        private TaskCreatorView? _taskCreatorView;
        public MainWindow() {
            InitializeComponent();
        }

        private void NavItemOnClick(object sender, MouseButtonEventArgs e) {
            if (sender is NavigationViewItem item) {
                switch (item.Tag) {
                    case "closeAppllication":
                        Application.Current.Shutdown();
                        break;
                    case "createExercise":
                        CreateExercise();
                        break;
                    case "closeFrame":
                        CloseFrame();
                        break;
                    case "solveExercise":
                        SolveExercise();
                        break;
                }
            }
        }

        private void CreateExercise() {
            if (_taskCreatorView != null)
                _taskCreatorView = null;
            _taskCreatorView = new TaskCreatorView();
            MainContent.Content = _taskCreatorView;
        }

        private void CloseFrame() {
            _taskCreatorView = null;
            MainContent.Content = null;
        }

        private void SolveExercise() {
            MainContent.Content = new MapView();
        }

        private void SolveCrossbar(double PositionLat = 55.8558, double PositionLon = 10.4872,
                                   double PoiLat = 55.8611, double PoiLon = 10.494,
                                   double Kdd = 347) {
            double rad = Math.PI / 180;
            double meanLat, differenceLat, differenceLon, distanceToCrossbar;
            double crossbarLat, crossbarLon;

            meanLat = (PositionLat + PoiLat) / 2;
            differenceLat = (PositionLat - PoiLat);
            differenceLon = (PositionLon - PoiLon) * Math.Cos(rad * meanLat);

            distanceToCrossbar = differenceLat * Math.Cos(rad * Kdd) + differenceLon * Math.Sin(rad * Kdd);

            crossbarLat = PositionLat + (distanceToCrossbar / 60) * Math.Cos(rad * Kdd);
            crossbarLon = PositionLon + ((distanceToCrossbar / 60) * Math.Sin(rad * Kdd) / Math.Cos(rad * meanLat));
            Debug.WriteLine($"1. Pozycja Trawersu Lat={Math.Round(crossbarLat, 4)}  Lon={Math.Round(crossbarLon, 4)}");


            var calculatorFactory = new CalculatorFactory();
            calculatorFactory.Register<CrossbarInput, CrossbarResult>(CalculatorType.Crossbar, new CrossbarCalculator());
            var crossbarCalc = calculatorFactory.Get<CrossbarInput, CrossbarResult>(CalculatorType.Crossbar);

            var crossbar = crossbarCalc.Calculate(new CrossbarInput {
                PositionLat = 55.8558,
                PositionLon = 10.4872,
                PoiLat = 55.8611,
                PoiLon = 10.494,
                Kdd = 347
            });
            Debug.WriteLine($"2. Pozycja Trawersu Lat={crossbar.Lat}  Lon={crossbar.Lon}");
        }
    }
}