using marine_nav_trainer.Calculators.Core.Abstractions;

namespace marine_nav_trainer.Calculators.Modules.ApparentWind {
    public class ApparentWindCalculator : ICalculator<ApparentWindInput, ApparentWindResult> {
        public ApparentWindResult Calculate(ApparentWindInput input) {
            double rad = Math.PI / 180.0;

            // Wiatr pozorny
            double trueWindHeading = (input.WindDirection + 180.0) % 360.0;

            double boatHeadingRad = input.BoatHeading * rad;
            double trueWindHeadingRad = trueWindHeading * rad;

            double trueWindVectorX = input.WindSpeed * Math.Sin(trueWindHeadingRad);
            double trueWindVectorY = input.WindSpeed * Math.Cos(trueWindHeadingRad);

            double boatVectorX = input.BoatSpeed * Math.Sin(boatHeadingRad);
            double boatVectorY = input.BoatSpeed * Math.Cos(boatHeadingRad);

            double apparentWindVectorX = trueWindVectorX - boatVectorX;
            double apparentWindVectorY = trueWindVectorY - boatVectorY;

            double apparentWindDirection = NormalizeDeg(Math.Atan2(apparentWindVectorX, apparentWindVectorY) / rad);

            // Kierunek, z którego czuć wieje względem dziobu
            double relativeWindDirection = NormalizeDeg(input.WindDirection - input.BoatHeading);

            return new ApparentWindResult {
                ApparentWindDirection = apparentWindDirection,
                RelativeWindDirection = relativeWindDirection
            };
        }

        private static double NormalizeDeg(double d) {
            d %= 360;
            return d < 0 ? d + 360 : d;
        }
    }
}
