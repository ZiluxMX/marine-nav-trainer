using marine_nav_trainer.Calculators.Core.Abstractions;

namespace marine_nav_trainer.Calculators.Modules.ApparentWind {
    public class ApparentWindCalculator : ICalculator<ApparentWindInput, ApparentWindResult> {
        public ApparentWindResult Calculate(ApparentWindInput input) {
            double rad = Math.PI / 180.0;

            double tws = input.WindSpeed; 
            double stw = input.BoatSpeed;   

            // Kąt wiatru rzeczywistego względem dziobu
            double twa = (input.WindDirection - input.BoatHeading + 360.0) % 360.0; 
            double alpha = twa * rad;

            // AWS = sqrt((W cos a + V)^2 + (W sin a)^2)
            // AWA = atan2(W sin a, W cos a + V)
            double alongBow = tws * Math.Cos(alpha) + stw;
            double acrossBow = tws * Math.Sin(alpha);

            double apparentWindSpeed = Math.Sqrt(alongBow * alongBow + acrossBow * acrossBow);
            double apparentWindAngle = (Math.Atan2(acrossBow, alongBow) / rad + 360.0) % 360.0;

            // Kierunek wiatru pozornego jako namiar (z którego wieje)
            double apparentWindDirection = (input.BoatHeading + apparentWindAngle) % 360.0;

            return new ApparentWindResult {
                ApparentWindDirection = apparentWindDirection,
                RelativeWindDirection = twa,   
                ApparentWindSpeed = apparentWindSpeed,
                ApparentWindAngle = apparentWindAngle
            };
        }
    }
}

// wzory z https://www.katamarans.com/wind-speed-calculator-true-apparent/