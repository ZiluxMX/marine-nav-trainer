namespace marine_nav_trainer.Calculators.Core {
    public static class CoordinateFormatter {
        public static string ToLatDegMin(double decimalDegrees) => ToDegMinCardinal(decimalDegrees, true);
        public static string ToLonDegMin(double decimalDegrees) => ToDegMinCardinal(decimalDegrees, false);

        public static string ToDegreesMinutes(double degrees) {
            double abs = Math.Abs(degrees);
            int deg = (int)abs;
            double min = (abs - deg) * 60.0;
            return $"{deg:D3}° {min:00.00}'";
        }

        private static string ToDegMinCardinal(double decimalDegrees, bool isLatitude) {
            char cardinal;
            if (isLatitude)
                cardinal = decimalDegrees >= 0 ? 'N' : 'S';
            else
                cardinal = decimalDegrees >= 0 ? 'E' : 'W';
            decimalDegrees = Math.Abs(decimalDegrees);
            int deg = (int)Math.Floor(decimalDegrees);
            double min = (decimalDegrees - deg) * 60.0;

            if (isLatitude)
                return $"{deg:D2}° {min:00.0000}′{cardinal}";
            else
                return $"{deg:D3}° {min:00.0000}′{cardinal}";

        }
    }
}
