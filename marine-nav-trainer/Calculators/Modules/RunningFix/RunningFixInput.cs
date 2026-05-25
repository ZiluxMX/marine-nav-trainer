namespace marine_nav_trainer.Calculators.Modules.RunningFix {
    public class RunningFixInput {
        public double PoiLat { get; set; }
        public double PoiLon { get; set; }

        public double BearingA { get; set; }
        public double BearingB { get; set; }

        public double Kdd { get; set; }
        public double DistanceNm { get; set; }
    }
}
