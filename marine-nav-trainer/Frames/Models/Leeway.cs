namespace marine_nav_trainer.Navigation.Models {
    public class Leeway {
        public const string CloseHauledKey = "close_hauled";
        public const string BeamReachKey = "beam_reach";
        public const string BroadReachKey = "broad_reach";
        public const string RunningKey = "running";

        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double MinDeg { get; set; }
        public double MaxDeg { get; set; }
        public double ValueDeg { get; set; }

        // wartości domyślne
        public static List<Leeway> LoadDefaultValues() => [
            new() { Key = CloseHauledKey, Name = "Bajdewind", MinDeg = 0, MaxDeg = 44.9999, ValueDeg = 10 },
            new() { Key = BeamReachKey, Name = "Półwiatr", MinDeg = 45, MaxDeg = 89.9999, ValueDeg = 5 },
            new() { Key = BroadReachKey, Name = "Baksztag", MinDeg = 90, MaxDeg = 134.9999, ValueDeg = 3 },
            new() { Key = RunningKey, Name = "Fordewind", MinDeg = 135, MaxDeg = 180, ValueDeg = 0 }
        ];
    }
}
