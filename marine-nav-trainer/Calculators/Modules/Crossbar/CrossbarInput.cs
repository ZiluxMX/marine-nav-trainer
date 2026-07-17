using marine_nav_trainer.Calculators.Core;

namespace marine_nav_trainer.Calculators.Modules.Crossbar {
    public class CrossbarInput {
        public GeoCoordinate PositionLat { get; set; }
        public GeoCoordinate PositionLon { get; set; }
        public GeoCoordinate PoiLat { get; set; }
        public GeoCoordinate PoiLon { get; set; }
        public double Kdd { get; set; }
    }
}
