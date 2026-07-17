namespace marine_nav_trainer.Calculators.UI {
    public class InsertPointEventArgs : EventArgs {
        public double Lat { get; }
        public double Lon { get; }

        public InsertPointEventArgs(double lat, double lon) {
            Lat = lat;
            Lon = lon;
        }
    }

    public interface ICoordinatePointProducer {
        event EventHandler<InsertPointEventArgs>? InsertPointRequested;
    }
}
