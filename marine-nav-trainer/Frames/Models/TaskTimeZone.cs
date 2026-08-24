namespace marine_nav_trainer.Navigation.Models {
    public class TaskTimeZone {
        public const string DefaultName = "UTC";
        public const string DefaultOffset = "+00:00";

        public string Name { get; set; } = DefaultName;
        public string Offset { get; set; } = DefaultOffset;
    }
}
