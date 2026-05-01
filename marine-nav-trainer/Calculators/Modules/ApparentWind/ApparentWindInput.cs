using System;
using System.Collections.Generic;
using System.Text;

namespace marine_nav_trainer.Calculators.Modules.ApparentWind {
    public class ApparentWindInput {
        public double BoatSpeed { get; set; }
        public double BoatHeading { get; set; }
        public double WindSpeed { get; set; }
        public double WindDirection { get; set; }
    }
}