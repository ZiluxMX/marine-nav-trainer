using marine_nav_trainer.Calculators.Core.Abstractions;
using marine_nav_trainer.Calculators.Core.Factory;
using marine_nav_trainer.Calculators.Modules.Course;
using marine_nav_trainer.Navigation.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace marine_nav_trainer.Calculators.UI.Views.Tabs {
    public partial class CourseTabView : UserControl, INavigationDataUser {
        private readonly ICalculator<CourseInput, CourseResult> _courseCalc;

        public event EventHandler<NavigationData>? NavigationDataRequested;

        public CourseTabView() {
            InitializeComponent();

            var calculatorFactory = new CalculatorFactory();
            calculatorFactory.Register<CourseInput, CourseResult>(CalculatorType.Course, new CourseCalculator());
            _courseCalc = calculatorFactory.Get<CourseInput, CourseResult>(CalculatorType.Course);

            Loaded += CourseTabViewOnLoaded;
        }

        // deklinacja globalna wchodzi w pola póki użytkownik ich nie ruszy
        private void CourseTabViewOnLoaded(object sender, RoutedEventArgs e) {
            if (VariationDegBox.Value == null && VariationMinBox.Value == null)
                ShowVariation();
        }

        private void RefreshVariation_Click(object sender, RoutedEventArgs e) {
            ShowVariation();
        }

        // deklinacja z ramki, przeliczona na rok z daty rozpoczęcia rejsu
        private void ShowVariation() {
            NavigationData navigation = RequestNavigationData();
            double variation = navigation.Variation.ValueForYear(navigation.StartDate.Year);
            int degrees = (int)variation;

            VariationDegBox.Value = degrees;
            VariationMinBox.Value = Math.Round(Math.Abs(variation - degrees) * 60, 1);
        }

        private void Oblicz_Click(object sender, RoutedEventArgs e) {
            var source = Enum.Parse<CourseSource>((string)((ComboBoxItem)SourceBox.SelectedItem).Tag);
            NumberBox sourceBox = BoxFor(source);

            if (sourceBox.Value == null) {
                SummaryText.Text = "Uzupełnij kurs wskazany do wyliczeń";
                return;
            }

            double degrees = VariationDegBox.Value ?? 0;
            double minutes = (VariationMinBox.Value ?? 0) / 60;
            NavigationData navigation = RequestNavigationData();

            CourseResult result = _courseCalc.Calculate(new CourseInput {
                Source = source,
                Course = sourceBox.Value.Value,
                VariationDeg = degrees < 0 ? degrees - minutes : degrees + minutes,
                WindDirectionDeg = WindDirectionBox.Value,
                DeviationTable = navigation.DeviationTable,
                Leeway = navigation.Leeway
            });

            ShowCourse(CompassCourseBox, result.CompassCourse);
            ShowCourse(MagneticCourseBox, result.MagneticCourse);
            ShowCourse(TrueCourseBox, result.TrueCourse);
            ShowCourse(GroundCourseBox, result.COG);
            ShowSummary(result);
        }

        private NumberBox BoxFor(CourseSource source) {
            switch (source) {
                case CourseSource.KK:
                    return CompassCourseBox;
                case CourseSource.KM:
                    return MagneticCourseBox;
                case CourseSource.KR:
                    return TrueCourseBox;
                case CourseSource.KDD:
                    return GroundCourseBox;
                default:
                    throw new Exception("Nieznany kurs źródłowy");
            }
        }

        private NavigationData RequestNavigationData() {
            var navigation = new NavigationData();
            NavigationDataRequested?.Invoke(this, navigation);

            return navigation;
        }

        private static void ShowCourse(NumberBox box, double? course) {
            box.Value = course == null ? null : Math.Round(course.Value, 1);
        }

        private void ShowSummary(CourseResult result) {
            var parts = new List<string>();

            if (result.DeviationDeg != null)
                parts.Add($"dewiacja {Signed(result.DeviationDeg.Value)}");

            if (result.WindAngleDeg != null)
                parts.Add($"kąt do wiatru {result.WindAngleDeg.Value.ToString("0.#", CultureInfo.CurrentCulture)}°");

            if (result.DriftDeg != null)
                parts.Add($"dryf {Signed(result.DriftDeg.Value)}");

            if (!result.IsDeviationExact)
                parts.Add("tabela dewiacji bez dokładnego dopasowania - KK przybliżone");

            SummaryText.Text = parts.Count > 0
                ? string.Join(", ", parts)
                : "Brak danych do przeliczenia - sprawdź tabele, a dla KDd także wiatr";
        }

        private static string Signed(double value) {
            return $"{(value < 0 ? "-" : "+")}{Math.Abs(value).ToString("0.#", CultureInfo.CurrentCulture)}°";
        }
    }
}
