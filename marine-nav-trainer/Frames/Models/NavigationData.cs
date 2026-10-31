namespace marine_nav_trainer.Navigation.Models {
    public class NavigationData {
        public const int DefaultDeviationStepDeg = 10;
        public TaskTimeZone TimeZone { get; set; } = new();
        public MagneticVariation Variation { get; set; } = new();
        public List<LocalMagneticVariation> VariationList { get; set; } = [];
        public List<Deviation> DeviationTable { get; set; } = [];
        public List<Leeway> Leeway { get; set; } = [];
        public DateTime StartDate { get; set; } = DateTime.Today;

        public static NavigationData LoadDefaultValues() {
            var deviation = new List<Deviation>();
            for (int heading = 0; heading < 360; heading += DefaultDeviationStepDeg) {
                deviation.Add(new Deviation { HeadingDeg = heading, DeviationDeg = 0 });
            }

            return new NavigationData {
                DeviationTable = deviation,
                Leeway = Models.Leeway.LoadDefaultValues()
            };
        }
    }
}
