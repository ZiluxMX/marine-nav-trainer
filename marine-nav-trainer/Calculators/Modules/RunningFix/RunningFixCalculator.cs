using marine_nav_trainer.Calculators.Core;
using marine_nav_trainer.Calculators.Core.Abstractions;

namespace marine_nav_trainer.Calculators.Modules.RunningFix {
    public class RunningFixCalculator : ICalculator<RunningFixInput, RunningFixResult> {
        public RunningFixResult Calculate(RunningFixInput input) {
            double rad = Math.PI / 180.0;

            double poiLat = input.PoiLat.DecimalDegrees;
            double poiLon = input.PoiLon.DecimalDegrees;
            double cosRef = Math.Cos(poiLat * rad);

            double poix = poiLon * 60.0 * cosRef;
            double poiy = poiLat * 60.0;

            // Pierwsza LOP przeniesiona o wektor przebytej drogi
            double lop1 = (input.BearingA + 180.0) % 360.0;
            double dx1 = Math.Sin(lop1 * rad);
            double dy1 = Math.Cos(lop1 * rad);

            double moveX = input.DistanceNm * Math.Sin(input.Kdd * rad);
            double moveY = input.DistanceNm * Math.Cos(input.Kdd * rad);

            double moveCrossLop1 = moveX * dy1 - moveY * dx1;
            if (Math.Abs(moveCrossLop1) < 0.000001)
                throw new Exception("Nie da się wyznaczyć pozycji");

            double movedX = poix + moveX;
            double movedY = poiy + moveY;

            // Druga LOP przez punkt w czasie t2
            double lop2 = (input.BearingB + 180.0) % 360.0;
            double dx2 = Math.Sin(lop2 * rad);
            double dy2 = Math.Cos(lop2 * rad);

            double denominator = dx1 * dy2 - dy1 * dx2;
            if (Math.Abs(denominator) < 0.000001)
                throw new Exception("Linie pozycyjne są równoległe.");

            double t = ((poix - movedX) * dy2 - (poiy - movedY) * dx2) / denominator;
            double fixX = movedX + t * dx1;
            double fixY = movedY + t * dy1;

            double lat = fixY / 60.0;
            double lon = fixX / (60.0 * cosRef);

            return new RunningFixResult {
                Lat = GeoCoordinate.FromDecimal(lat, CoordinateAxis.Latitude),
                Lon = GeoCoordinate.FromDecimal(lon, CoordinateAxis.Longitude)
            };
        }
    }
}
