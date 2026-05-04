using marine_nav_trainer.Calculators.Core.Abstractions;

namespace marine_nav_trainer.Calculators.Modules.Crossbar {
    public class CrossbarCalculator : ICalculator<CrossbarInput, CrossbarResult> {
        public CrossbarResult Calculate(CrossbarInput input) {
            double rad = Math.PI / 180.0;

            double meanLat = (input.PositionLat + input.PoiLat) / 2.0;

            double differenceLat = input.PositionLat - input.PoiLat;
            double differenceLon = (input.PositionLon - input.PoiLon) * Math.Cos(rad * meanLat);

            double distanceToCrossbar = differenceLat * Math.Cos(rad * input.Kdd) +
                                        differenceLon * Math.Sin(rad * input.Kdd);

            double crossLat = input.PositionLat + (distanceToCrossbar / 60.0) * Math.Cos(rad * input.Kdd);

            double crossLon = input.PositionLon +
                              ((distanceToCrossbar / 60.0) * Math.Sin(rad * input.Kdd) /
                               Math.Cos(rad * meanLat));

            return new CrossbarResult {
                Lat = Math.Round(crossLat, 4),
                Lon = Math.Round(crossLon, 4)
            };
        }
    }
}