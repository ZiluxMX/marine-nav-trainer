using marine_nav_trainer.Calculators.Core;

namespace marine_nav_trainer.Calculators.Modules.RunningFix {
    public class RunningFixInput {
        public GeoCoordinate PoiLat { get; set; }
        public GeoCoordinate PoiLon { get; set; }

        public double BearingA { get; set; }
        public double BearingB { get; set; }

        public double Kdd { get; set; }
        public double DistanceNm { get; set; }
    }
}
