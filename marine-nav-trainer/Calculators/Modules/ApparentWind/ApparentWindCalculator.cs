using marine_nav_trainer.Calculators.Core.Abstractions;

namespace marine_nav_trainer.Calculators.Modules.ApparentWind {
    public class ApparentWindCalculator : ICalculator<ApparentWindInput, ApparentWindResult> {
        public ApparentWindResult Calculate(ApparentWindInput input) {
            double rad = Math.PI / 180.0;

            double trueWindHeading = (input.WindDirection + 180.0) % 360.0;

            double boatHeadingRad = input.BoatHeading * rad;
            double trueWindHeadingRad = trueWindHeading * rad;

            double trueWindVectorX = input.WindSpeed * Math.Sin(trueWindHeadingRad);
            double trueWindVectorY = input.WindSpeed * Math.Cos(trueWindHeadingRad);

            double boatVectorX = input.BoatSpeed * Math.Sin(boatHeadingRad);
            double boatVectorY = input.BoatSpeed * Math.Cos(boatHeadingRad);

            double apparentWindVectorX = trueWindVectorX - boatVectorX;
            double apparentWindVectorY = trueWindVectorY - boatVectorY;

            double compasWindDirection = NormalizeDeg(Math.Atan2(apparentWindVectorX, apparentWindVectorY) / rad);
            double apparentWindDirection = NormalizeSignedDeg(compasWindDirection - input.BoatHeading);

            return new ApparentWindResult {
                CompasWindDirection = compasWindDirection,
                ApparentWindDirection = apparentWindDirection
            };
        }

        private double NormalizeDeg(double d) {
            d %= 360;
            return d < 0 ? d + 360 : d;
        }

        private double NormalizeSignedDeg(double d) {
            d = NormalizeDeg(d);
            return d > 180 ? d - 360 : d;
        }
    }
}
