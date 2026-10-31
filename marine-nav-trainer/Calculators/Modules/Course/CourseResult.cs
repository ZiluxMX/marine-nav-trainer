namespace marine_nav_trainer.Calculators.Modules.Course {
    public class CourseResult {
        public double? CompassCourse { get; set; }
        public double? MagneticCourse { get; set; }
        public double? TrueCourse { get; set; }
        public double? COG { get; set; }
        public double? DeviationDeg { get; set; }
        public double? DriftDeg { get; set; }
        public double? WindAngleDeg { get; set; }        
        public bool IsDeviationExact { get; set; } = true; // false jak tabela dewiacji nie ma KK wracającego dokładnie do podanego KM
    }
}
