using marine_nav_trainer.Calculators.Core.Abstractions;

namespace marine_nav_trainer.Calculators.Modules.RunningFix {
    public class RunningFixCalculator : ICalculator<RunningFixInput, RunningFixResult> {
        public RunningFixResult Calculate(RunningFixInput input) {
            double rad = Math.PI / 180.0;

            double poix = input.PoiLon * 60.0 * Math.Cos(input.PoiLat * rad);
            double poiy = input.PoiLat * 60.0;

            double lop1 = (input.BearingA + 90.0) % 360.0;
            double dx1 = Math.Sin(lop1 * rad);
            double dy1 = Math.Cos(lop1 * rad);

            double moveX = input.DistanceNm * Math.Sin(input.Kdd * rad);
            double moveY = input.DistanceNm * Math.Cos(input.Kdd * rad);

            double ax = poix + moveX;
            double ay = poiy + moveY;

            double lop2 = (input.BearingB + 90.0) % 360.0;
            double dx2 = Math.Sin(lop2 * rad);
            double dy2 = Math.Cos(lop2 * rad);

            double denominator = dx1 * dy2 - dy1 * dx2;
            if (Math.Abs(denominator) < 0.000001)
                throw new Exception("Linie pozycyjne są równoległe.");

            double t = ((poix - ax) * dy2 - (poiy - ay) * dx2) / denominator;
            double rx = ax + t * dx1;
            double ry = ay + t * dy1;

            double lat = ry / 60.0;
            double lon = rx / (60.0 * Math.Cos(input.PoiLat * rad));

            return new RunningFixResult {
                Lat = Math.Round(lat, 4),
                Lon = Math.Round(lon, 4)
            };
        }
    }
}
