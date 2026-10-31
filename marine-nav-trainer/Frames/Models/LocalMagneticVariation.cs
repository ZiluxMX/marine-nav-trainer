namespace marine_nav_trainer.Navigation.Models {
    public class LocalMagneticVariation : MagneticVariation {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double RangeNm { get; set; }
    }
}
