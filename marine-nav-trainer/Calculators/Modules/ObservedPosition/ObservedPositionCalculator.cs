using marine_nav_trainer.Calculators.Core.Abstractions;

namespace marine_nav_trainer.Calculators.Modules.ObservedPosition {
    public class ObservedPositionCalculator : ICalculator<ObservedPositionInput, ObservedPositionResult> {
        public ObservedPositionResult Calculate(ObservedPositionInput input) {
            double rad = Math.PI / 180.0;
            double meanLat = (input.PoiALat + input.PoiBLat) / 2.0;

            double ax = input.PoiALon * 60.0 * Math.Cos(meanLat * rad);
            double ay = input.PoiALat * 60.0;

            double bx = input.PoiBLon * 60.0 * Math.Cos(meanLat * rad);
            double by = input.PoiBLat * 60.0;

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
            double lon = px / (60.0 * Math.Cos(meanLat * rad));

            return new ObservedPositionResult {
                Lat = Math.Round(lat, 4),
                Lon = Math.Round(lon, 4)
            };
        }
    }
}
