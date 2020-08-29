
using Monocle.Math;
using System.Collections.Generic;

namespace Monocle.Peak
{
    public static class PeptideEnvelopeCalculator
    {
        /// <summary>
        /// Returns the intensity distribution of the isotopes a peptide based on the
        /// binomial probability of how many c13 are included.
        /// 
        /// The first element of the return value is zero - for comparisons
        /// we wouldnt expect a peak to the left.
        /// </summary>
        /// 
        /// <returns>The theoretical envelope.</returns>
        /// <param name="precursorMz">Precursor mz.</param>
        /// <param name="charge">Charge.</param>
        /// <param name="compareSize">The number of isotopes to consider</param>
        public static List<double> GetTheoreticalEnvelope(double precursorMz, int charge, int compareSize, bool hasSelenium = false)
        {
            int numCarbons = EstimateCarbons(precursorMz, charge, hasSelenium);
            List<double> output = new List<double>(new double[compareSize]);
            output[0] = 0.0;
            for (int i = 1; i < compareSize; ++i)
            {
                output[i] = Binomial.P(numCarbons, i - 1, 0.011);
            }

            if (hasSelenium)
            {
                return incorporateSelenium(output);
            }
            else
                return output;
        }

        /// <summary>
        /// Estimates the number of carbons in a peptide based only on its
		/// precursor m/z and charge.
        /// </summary>
		/// 
        /// Original monocle: mz = 111; carbons = 5.1
        /// Senko et al 1995: mz = 111.1254; carbons = 4.9384
        /// DKS Uniprot TREMBL 2019_08: mz = 110.3963; carbons = 4.9243
        /// Quantities represent frequency weighted means of mz and # of carbons
        /// 
        /// <returns>Number of carbons</returns>
        /// <param name="mz">mz</param>
        /// <param name="charge">charge</param>
        private static int EstimateCarbons(double mz, int charge, bool hasSelenium = false)
        {
            return (int)System.Math.Floor((((mz * charge) - (Data.Mass.ProtonMass * charge)) / 111) * 5.1);
        }

        //
        // add a single Selenium atom to the model formula and alter the isotope
        // distribution accordingly.
        private static List<double> incorporateSelenium(List<double> source)
        {
            var compareSize = source.Count;
            List<double> output = new List<double>(new double[compareSize]);
            int i = 0;
            foreach (var e in Data.Mass.SeleniumIsotopes)
            {
                for (int k = i; k < compareSize; ++k)
                {
                    output[k] += e.Item2 * source[k - i];
                }
                i++;
            }
            return output;
        }
    }
}