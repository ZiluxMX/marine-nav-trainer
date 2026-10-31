namespace marine_nav_trainer.Calculators.Core {
    public enum CoordinateAxis {
        Latitude,
        Longitude
    }

    public readonly struct GeoCoordinate {
        public CoordinateAxis Axis { get; }
        public int Degrees { get; }
        public double Minutes { get; }
        public char Cardinal { get; }

        public GeoCoordinate(CoordinateAxis axis, int degrees, double minutes, char cardinal) {
            if (degrees < 0)
                throw new ArgumentOutOfRangeException(nameof(degrees), "Stopnie nie mogą być ujemne.");
            if (minutes < 0 || minutes >= 60)
                throw new ArgumentOutOfRangeException(nameof(minutes), "Minuty muszą być w zakresie [0, 60).");

            cardinal = char.ToUpperInvariant(cardinal);
            bool validCardinal = axis == CoordinateAxis.Latitude
                ? cardinal is 'N' or 'S'
                : cardinal is 'E' or 'W';
            if (!validCardinal)
                throw new ArgumentException($"Niepoprawna półkula '{cardinal}' dla osi {axis}.", nameof(cardinal));

            Axis = axis;
            Degrees = degrees;
            Minutes = minutes;
            Cardinal = cardinal;
        }

        // S/W = ujemna
        public double DecimalDegrees {
            get {
                double magnitude = Degrees + Minutes / 60.0;
                return Cardinal is 'S' or 'W' ? -magnitude : magnitude;
            }
        }

        public static GeoCoordinate FromDecimal(double decimalDegrees, CoordinateAxis axis) {
            char cardinal = axis == CoordinateAxis.Latitude
                ? (decimalDegrees < 0 ? 'S' : 'N')
                : (decimalDegrees < 0 ? 'W' : 'E');

            double magnitude = Math.Abs(decimalDegrees);
            int degrees = (int)Math.Floor(magnitude);
            double minutes = (magnitude - degrees) * 60.0;

            if (minutes >= 60.0) {
                degrees++;
                minutes = 0.0;
            }

            return new GeoCoordinate(axis, degrees, minutes, cardinal);
        }

        public override string ToString() => Axis == CoordinateAxis.Latitude ?
                                        CoordinateFormatter.ToLatDegMin(DecimalDegrees) :
                                        CoordinateFormatter.ToLonDegMin(DecimalDegrees);
    }
}
