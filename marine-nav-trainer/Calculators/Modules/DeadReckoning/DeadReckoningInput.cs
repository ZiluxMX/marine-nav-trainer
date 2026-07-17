using marine_nav_trainer.Calculators.Core;

namespace marine_nav_trainer.Calculators.Modules.DeadReckoning {
    public class DeadReckoningInput {
        public GeoCoordinate StartLat { get; set; }
        public GeoCoordinate StartLon { get; set; }
        public double Kdd { get; set; }
        public double DistanceNm { get; set; }
    }
}
