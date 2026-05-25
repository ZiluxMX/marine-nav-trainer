using marine_nav_trainer.Map.UI.Controls;

namespace marine_nav_trainer.Map.Models {
    internal class Position {
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string? Label { get; set; }
        public string? Time { get; set; }
        public double? Log { get; set; }
        public PositionMark? Mark { get; set; }
    }
}
