using marine_nav_trainer.Map;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace marine_nav_trainer {
    public partial class MainWindow : Window {
               
        public MainWindow() {
            InitializeComponent();
        }

        private void NavItemOnClick(object sender, MouseButtonEventArgs e) {
            if (sender is Wpf.Ui.Controls.NavigationViewItem item) {
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
            MainContent.Content = new MapView();
        }

        private void CloseFrame() {
            MainContent.Content = null;
        }

        private void SolveExercise() {
            SolveCrossbar();
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

            Debug.WriteLine($"Pozycja Trawersu Lat={Math.Round(crossbarLat, 4)}  Lon={Math.Round(crossbarLon, 4)}");
        }
    }
}