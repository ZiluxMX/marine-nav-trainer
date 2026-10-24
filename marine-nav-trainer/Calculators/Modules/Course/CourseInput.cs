using marine_nav_trainer.Navigation.Models;

namespace marine_nav_trainer.Calculators.Modules.Course {
    public class CourseInput {
        public CourseSource Source { get; set; }
        public double Course { get; set; }
        public double VariationDeg { get; set; }
        public double? WindDirectionDeg { get; set; }
        public List<Deviation> DeviationTable { get; set; } = [];
        public List<Leeway> Leeway { get; set; } = [];
    }
}
