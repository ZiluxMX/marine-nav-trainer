namespace marine_nav_trainer.Navigation.Models {
    public class Deviation {
        public int HeadingDeg { get; set; }
        public double DeviationDeg { get; set; }

        // TODO: wartości domyślne do pliku konfiguracyjnego edytowalnego przez użytkownika
        private static readonly double[] DefaultDeviations = [
             8,  8,  7,  6,  4,  4,  2,  0, -2, -3, -4, -5,
            -5, -6, -6, -7, -6, -4, -2, -1, -1,  0,  1,  3,
             4,  4,  5,  6,  7,  9,  9, 10,  9,  9,  8,  8
        ];

        public static List<Deviation> LoadDefaultValues() {
            var entries = new List<Deviation>();

            for (int i = 0; i < DefaultDeviations.Length; i++) {
                entries.Add(new Deviation {
                    HeadingDeg = i * NavigationData.DefaultDeviationStepDeg,
                    DeviationDeg = DefaultDeviations[i]
                });
            }

            return entries;
        }
    }
}
