using System;
using System.Collections.Generic;
using System.Text;

namespace marine_nav_trainer.Calculators.Modules.Crossbar {
    public class CrossbarInput {
        public double PositionLat { get; set; }
        public double PositionLon { get; set; }
        public double PoiLat { get; set; }
        public double PoiLon { get; set; }
        public double Kdd { get; set; }
    }
}