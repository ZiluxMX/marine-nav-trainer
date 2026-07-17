using marine_nav_trainer.Calculators.Core;
using marine_nav_trainer.Calculators.Core.Abstractions;

namespace marine_nav_trainer.Calculators.Modules.DeadReckoning {
    public class DeadReckoningCalculator : ICalculator<DeadReckoningInput, DeadReckoningResult> {
        public DeadReckoningResult Calculate(DeadReckoningInput input) {
            double rad = Math.PI / 180.0;

            double startLat = input.StartLat.DecimalDegrees;
            double startLon = input.StartLon.DecimalDegrees;

            double deltaLat = (input.DistanceNm * Math.Cos(input.Kdd * rad)) / 60.0;
            double meanLat = startLat + (deltaLat / 2.0);
            double deltaLon = (input.DistanceNm * Math.Sin(input.Kdd * rad)) / (60.0 * Math.Cos(meanLat * rad));

            return new DeadReckoningResult {
                Lat = GeoCoordinate.FromDecimal(startLat + deltaLat, CoordinateAxis.Latitude),
                Lon = GeoCoordinate.FromDecimal(startLon + deltaLon, CoordinateAxis.Longitude)
            };
        }
    }
}
