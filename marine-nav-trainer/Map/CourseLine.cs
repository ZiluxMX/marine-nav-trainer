using System;
using System.Collections.Generic;
using System.Text;

namespace marine_nav_trainer.Map {
    public class CourseLine {
        public int Id { get; set; }
        public Position StartPosition { get; set; }
        public Position? EndPosition { get; set; }
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public double CourseOverGround { get; set; }
        public double CourseCompas {  get; set; }
    }
}
