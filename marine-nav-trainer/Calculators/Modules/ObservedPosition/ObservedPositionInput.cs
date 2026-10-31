using marine_nav_trainer.Calculators.Core;

namespace marine_nav_trainer.Calculators.Modules.ObservedPosition {
    public class ObservedPositionInput {
        public GeoCoordinate PoiALat { get; set; }
        public GeoCoordinate PoiALon { get; set; }
        public double BearingA { get; set; }

        public GeoCoordinate PoiBLat { get; set; }
        public GeoCoordinate PoiBLon { get; set; }
        public double BearingB { get; set; }
    }
}
