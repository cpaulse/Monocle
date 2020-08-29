
namespace Monocle.Peak {
    /// <summary>
    /// This class stores information about how many and which isotopes
    /// to consider within the isotopic envelope.
    /// </summary>
    class IsotopeRange {
        /// <summary>
        /// The total number of isotopes to consider.`
        /// </summary>
        public int Isotopes;

        /// <summary>
        /// The index offset of the original monoisotopic peak.
        /// </summary>
        public int MonoisotopicIndex;

        /// <summary>
        /// The number of alternate peaks to consider to the left 
        /// of the original monoisotopic index.
        /// </summary>
        public int Left;

        /// <summary>
        /// The number of isotopes to consider at a time during scoring.
        /// </summary>
        public int CompareSize;

        public IsotopeRange(double mass, bool foo = false) {
            bool containsSe = true;
            if (mass > 2900)
            {
                Isotopes = containsSe ? 23 : 14;
                Left = containsSe ? -12: -7;
                CompareSize = containsSe ? 14: 7;
                Left = -12;
                Isotopes = 23;
                CompareSize = 11;
            }
            else if (mass > 1200)
            {
                Isotopes = containsSe ? 17 : 10;
                Left = containsSe ? -8 : -5;
                CompareSize = containsSe ? 9 : 5;
                Isotopes = 17;
                Left = -8;
                CompareSize = 9;
            }
            else
            {
                Isotopes = containsSe ? 12: 7;
                Left = containsSe ? -5 : -3;
                CompareSize = containsSe ? 7 : 4;
                Isotopes = 12;
                Left = -5;
                CompareSize = 7;
            }
            MonoisotopicIndex = -1 * Left;
        }
    }
}
