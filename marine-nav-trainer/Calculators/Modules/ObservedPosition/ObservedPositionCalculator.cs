using marine_nav_trainer.Calculators.Core;
using marine_nav_trainer.Calculators.Core.Abstractions;

namespace marine_nav_trainer.Calculators.Modules.ObservedPosition {
    public class ObservedPositionCalculator : ICalculator<ObservedPositionInput, ObservedPositionResult> {
        public ObservedPositionResult Calculate(ObservedPositionInput input) {
            double rad = Math.PI / 180.0;

            double aLat = input.PoiALat.DecimalDegrees;
            double aLon = input.PoiALon.DecimalDegrees;
            double bLat = input.PoiBLat.DecimalDegrees;
            double bLon = input.PoiBLon.DecimalDegrees;

            double meanLat = (aLat + bLat) / 2.0;
            double cosRef = Math.Cos(meanLat * rad);

            double ax = aLon * 60.0 * cosRef;
            double ay = aLat * 60.0;

            double bx = bLon * 60.0 * cosRef;
            double by = bLat * 60.0;

            double bearingA = (input.BearingA + 180.0) % 360.0;
            double bearingB = (input.BearingB + 180.0) % 360.0;

            double dx1 = Math.Sin(bearingA * rad);
            double dy1 = Math.Cos(bearingA * rad);

            double dx2 = Math.Sin(bearingB * rad);
            double dy2 = Math.Cos(bearingB * rad);

            double denominator = dx1 * dy2 - dy1 * dx2;

            if (Math.Abs(denominator) < 0.000001)
                throw new Exception("Linie pozycyjne są równoległe.");

            double t = ((bx - ax) * dy2 - (by - ay) * dx2) / denominator;

            double px = ax + t * dx1;
            double py = ay + t * dy1;

            double lat = py / 60.0;
            double lon = px / (60.0 * cosRef);

            return new ObservedPositionResult {
                Lat = GeoCoordinate.FromDecimal(lat, CoordinateAxis.Latitude),
                Lon = GeoCoordinate.FromDecimal(lon, CoordinateAxis.Longitude)
            };
        }
    }
}
