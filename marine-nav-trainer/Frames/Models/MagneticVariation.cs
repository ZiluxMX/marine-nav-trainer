namespace marine_nav_trainer.Navigation.Models {
    public class MagneticVariation {
        private const double MinutesPerDegree = 60;
        public double BaseDeg { get; set; }
        public int BaseYear { get; set; } = DateTime.Today.Year;
        public double AnnualChangeMinute { get; set; }

        public double AnnualChangeDeg {
            get => AnnualChangeMinute / MinutesPerDegree;
            set => AnnualChangeMinute = value * MinutesPerDegree;
        }
    }
}
