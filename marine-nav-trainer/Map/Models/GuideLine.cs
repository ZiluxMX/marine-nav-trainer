using System.Windows.Shapes;

namespace marine_nav_trainer.Map.Models {
    internal class GuideLine {
        public int Id { get; set; }
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public Line? Line { get; set; }
    }
}
