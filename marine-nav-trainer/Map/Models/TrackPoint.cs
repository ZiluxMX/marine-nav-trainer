namespace marine_nav_trainer.Map.Models {
    // punkt trasy z pliku zadania
    internal class TrackPoint {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public DateTime Date { get; set; }
        public string? Time { get; set; }
        public double? Log { get; set; }
        public double Distance { get; set; }
        public double CourseCompas { get; set; }
        public double CourseOverGround { get; set; }
        public double Velocity { get; set; }
    }
}
