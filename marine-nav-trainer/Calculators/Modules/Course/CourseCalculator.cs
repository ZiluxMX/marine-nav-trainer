using marine_nav_trainer.Calculators.Core.Abstractions;
using marine_nav_trainer.Navigation.Models;

namespace marine_nav_trainer.Calculators.Modules.Course {
    // KK -> dewiacja -> KM -> deklinacja -> KR -> dryf -> KDd
    public class CourseCalculator : ICalculator<CourseInput, CourseResult> {
        public CourseResult Calculate(CourseInput input) {
            switch (input.Source) {
                case CourseSource.KK:
                    return FromCompass(input);
                case CourseSource.KM:
                    return FromMagnetic(input);
                case CourseSource.KR:
                    return FromTrue(input);
                case CourseSource.KDD:
                    return FromCOG(input);
                default:
                    throw new Exception("Nieznany kurs źródłowy");
            }
        }

        public CourseResult FromCompass(CourseInput input) {
            var result = new CourseResult { CompassCourse = input.Course };
            if (input.DeviationTable.Count == 0)
                return result;

            double deviation = DeviationFor(input.Course, input.DeviationTable);
            double magnetic = (input.Course + deviation + 360) % 360;
            double trueCourse = (magnetic + input.VariationDeg + 360) % 360;

            result.DeviationDeg = deviation;
            result.MagneticCourse = magnetic;
            result.TrueCourse = trueCourse;
            AddCOG(result, trueCourse, input);

            return result;
        }

        public CourseResult FromMagnetic(CourseInput input) {
            var result = new CourseResult { MagneticCourse = input.Course };
            double trueCourse = (input.Course + input.VariationDeg + 360) % 360;

            result.TrueCourse = trueCourse;
            AddCompassCourse(result, input.Course, input.DeviationTable);
            AddCOG(result, trueCourse, input);

            return result;
        }

        public CourseResult FromTrue(CourseInput input) {
            var result = new CourseResult { TrueCourse = input.Course };
            double magnetic = (input.Course - input.VariationDeg + 360) % 360;

            result.MagneticCourse = magnetic;
            AddCompassCourse(result, magnetic, input.DeviationTable);
            AddCOG(result, input.Course, input);

            return result;
        }

        public CourseResult FromCOG(CourseInput input) {
            var result = new CourseResult { COG = input.Course };

            double? trueCourse = TrueFromCOG(input.Course, input);
            if (trueCourse == null)
                return result;

            double magnetic = (trueCourse.Value - input.VariationDeg + 360) % 360;

            result.TrueCourse = trueCourse;
            result.MagneticCourse = magnetic;
            AddCOG(result, trueCourse.Value, input);
            AddCompassCourse(result, magnetic, input.DeviationTable);

            return result;
        }

        // dewiację odczytuje się z KK, więc przy KM -> KK bierzemy tę poprawkę z tabeli, która przeliczona z powrotem daje wyjściowe KM
        private static void AddCompassCourse(CourseResult result, double magneticCourse, List<Deviation> table) {
            if (table.Count == 0)
                return;

            double estimate = (magneticCourse - DeviationFor(magneticCourse, table) + 360) % 360;
            double bestCourse = 0;
            double bestGap = double.MaxValue;
            double bestDistance = double.MaxValue;

            foreach (var entry in table) {
                double candidate = (magneticCourse - entry.DeviationDeg + 360) % 360;
                double back = (candidate + DeviationFor(candidate, table) + 360) % 360;
                double gap = AngleGap(back, magneticCourse);
                double distance = AngleGap(candidate, estimate);

                bool isBetter = gap < bestGap - 0.001 || (Math.Abs(gap - bestGap) <= 0.001 && distance < bestDistance);
                if (!isBetter)
                    continue;

                bestCourse = candidate;
                bestGap = gap;
                bestDistance = distance;
            }

            result.CompassCourse = bestCourse;
            result.DeviationDeg = DeviationFor(bestCourse, table);

            // skok dewiacji potrafi zostawić KM, do którego nie wraca żadne KK
            result.IsDeviationExact = bestGap <= 0.001;
        }

        // KR -> KDd, dryf z tabeli wiatrów
        private static void AddCOG(CourseResult result, double trueCourse, CourseInput input) {
            if (input.WindDirectionDeg == null)
                return;

            double bearing = (input.WindDirectionDeg.Value - trueCourse + 360) % 360;
            double? drift = DriftFor(bearing, input.Leeway);
            if (drift == null)
                return;

            result.WindAngleDeg = bearing <= 180 ? bearing : 360 - bearing;
            result.DriftDeg = drift;
            result.COG = (trueCourse + drift.Value + 360) % 360;
        }

        // wyszukanie dewiacji dla kursu
        private static double DeviationFor(double compassCourse, List<Deviation> table) {
            double deviation = 0;
            double bestDistance = double.MaxValue;
            bool bestIsAbove = false;

            foreach (var entry in table) {
                double forward = (entry.HeadingDeg - compassCourse + 360) % 360;
                double distance = Math.Min(forward, 360 - forward);
                bool isAbove = forward <= 180;

                bool isBetter = distance < bestDistance - 0.001 || (Math.Abs(distance - bestDistance) <= 0.001 && isAbove && !bestIsAbove);
                if (!isBetter)
                    continue;

                deviation = entry.DeviationDeg;
                bestDistance = distance;
                bestIsAbove = isAbove;
            }

            return deviation;
        }

        // kąt do wiatru liczony od dziobu, wiatr z prawej burty znosi w lewo
        private static double? DriftFor(double bearing, List<Leeway> table) {
            double angle = bearing <= 180 ? bearing : 360 - bearing;

            foreach (var leeway in table) {
                if (angle >= leeway.MinDeg && angle <= leeway.MaxDeg)
                    return bearing <= 180 ? -leeway.ValueDeg : leeway.ValueDeg;
            }

            return null;
        }

        // dryf zależy od KR, więc przy KDd -> KR sprawdzamy, który kurs wraca do KDd
        private static double? TrueFromCOG(double COG, CourseInput input) {
            if (input.WindDirectionDeg == null)
                return null;

            double? matched = null;
            double matchedDistance = double.MaxValue;

            foreach (var leeway in input.Leeway) {
                for (int sign = -1; sign <= 1; sign += 2) {
                    double drift = sign * leeway.ValueDeg;
                    double candidate = (COG - drift + 360) % 360;
                    double bearing = (input.WindDirectionDeg.Value - candidate + 360) % 360;

                    double? applied = DriftFor(bearing, input.Leeway);
                    if (applied == null || Math.Abs(applied.Value - drift) > 0.001)
                        continue;

                    double distance = AngleGap(candidate, COG);
                    if (distance >= matchedDistance)
                        continue;

                    matched = candidate;
                    matchedDistance = distance;
                }
            }

            return matched;
        }

        private static double AngleGap(double first, double second) {
            double gap = Math.Abs(first - second);

            return Math.Min(gap, 360 - gap);
        }
    }
}
