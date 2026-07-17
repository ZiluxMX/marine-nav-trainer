using marine_nav_trainer.Calculators.Core;
using marine_nav_trainer.Calculators.Core.Abstractions;

namespace marine_nav_trainer.Calculators.Modules.Crossbar {
    public class CrossbarCalculator : ICalculator<CrossbarInput, CrossbarResult> {
        public CrossbarResult Calculate(CrossbarInput input) {
            double rad = Math.PI / 180.0;

            double shipLat = input.PositionLat.DecimalDegrees;
            double shipLon = input.PositionLon.DecimalDegrees;
            double poiLat = input.PoiLat.DecimalDegrees;
            double poiLon = input.PoiLon.DecimalDegrees;

            double refLat = (shipLat + poiLat) / 2.0;
            double cosRef = Math.Cos(refLat * rad);

            double shipX = shipLon * 60.0 * cosRef;
            double shipY = shipLat * 60.0;
            double poiX = poiLon * 60.0 * cosRef;
            double poiY = poiLat * 60.0;

            double courseX = Math.Sin(input.Kdd * rad);
            double courseY = Math.Cos(input.Kdd * rad);

            double alongTrack = (poiX - shipX) * courseX + (poiY - shipY) * courseY;

            double crossX = shipX + alongTrack * courseX;
            double crossY = shipY + alongTrack * courseY;

            double crossLat = crossY / 60.0;
            double crossLon = crossX / (60.0 * cosRef);

            return new CrossbarResult {
                Lat = GeoCoordinate.FromDecimal(crossLat, CoordinateAxis.Latitude),
                Lon = GeoCoordinate.FromDecimal(crossLon, CoordinateAxis.Longitude)
            };
        }
    }
}
