
namespace Monocle.Data {
    static class Mass {
        public const double ProtonMass = 1.007276466879000;
        public const double SeleniumWeight = 78.9;

        public static readonly (double, double, int)[] SeleniumIsotopes
                                                    = {(73.922477, 0.0009, 0),
                                                    (75.919207,	0.090, 2),
                                                    (76.919908,	0.0760, 3),
                                                    (77.917304,	0.2350, 4),
                                                    (79.916521,	0.4960, 6),
                                                    (81.916709,	0.0940, 8)};

        public const double AVERAGINE_DIFF = 1.00286864;
    }
}
