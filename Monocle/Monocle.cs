﻿using Monocle.Data;
using Monocle.Math;
using Monocle.Peak;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Monocle
{
    public static class Monocle
    {
        /// <summary>
        /// Overload to handle all available scans allowing for Ms1 inclusion of before + after
        /// </summary>
        /// <param name="AllScans"></param>
        /// <param name="DependentScan"></param>
        /// <param name="Number_Of_Scans_To_Average"></param>
        public static void Run(ref List<Scan> scans, MonocleOptions Options)
        {
            foreach (Scan scan in scans)
            {
                if (scan.MsOrder != Options.MS_Level)
                {
                    continue;
                }

                if (scan.PrecursorMasterScanNumber <= 0)
                {
                    Console.WriteLine(String.Format("Scan {0} does not have a precursor scan number assigned.", scan.ScanNumber));
                    continue;
                }

                Scan precursorScan = scans[scan.PrecursorMasterScanNumber - 1];
                var nearbyScans = GetNearbyScans(ref scans, precursorScan, Options);

                // For low-res scans, or if ForceCharges is true, generate precursors with
                // a range of charges given by the ChargeRangeUnknown option.
                bool lowResPrecursor = precursorScan.FilterLine.Contains("ITMS");
                if (lowResPrecursor || Options.ForceCharges)
                {
                    int range = 1 + Options.ChargeRangeUnknown.High - Options.ChargeRangeUnknown.Low;
                    var precursors = new List<Precursor>(range);
                    foreach (var precursor in scan.Precursors)
                    {
                        for (int z = Options.ChargeRangeUnknown.Low; z <= Options.ChargeRangeUnknown.High; ++z)
                        {
                            var p = new Precursor(precursor);
                            p.Charge = z;
                            precursors.Add(p);
                        }
                    }
                    scan.Precursors = precursors;
                }

                if (!Options.SkipMono && !lowResPrecursor)
                {
                    foreach (var precursor in scan.Precursors)
                    {
                        if (!Options.Charge_Detection && precursor.Charge == 0)
                        {
                            Console.WriteLine(String.Format("Scan {0} does not have a charge state assigned.  Charge detection enabled.", scan.ScanNumber));
                        }
                        Run(nearbyScans, precursorScan, precursor, Options);
                    }
                }
            }
        }

        /// <summary>
        /// Gets nearby MS1 scans around the scan given by precursorScan.
        /// The window is provided as an opton, but it
        ///  should at least return the precursor scan.
        /// </summary>
        /// <param name="scans">The List of scans to filter</param>
        /// <param name="precursorScan">the target scan.</param>
        /// <param name="Options">Options for selecting scans.</param>
        /// <returns>A list of filtered scans.</returns>
        public static List<Scan> GetNearbyScans(ref List<Scan> scans, Scan precursorScan, MonocleOptions Options)
        {
            int window = Options.Number_Of_Scans_To_Average;
            var output = new List<Scan>(window * 2);
            int index = precursorScan.ScanNumber - 1;
            int scanCount = 0;
            if (Options.AveragingVector == AveragingVector.Before || Options.AveragingVector == AveragingVector.Both)
            {
                // Reel backward.
                for (; index > 0 && scanCount < window; --index)
                {
                    if (IncludeNearbyScan(scans[index], precursorScan))
                    {
                        ++scanCount;
                    }
                }
            }
            scanCount = 0;
            // Collect scans.
            for (; index < scans.Count && scanCount < window; ++index)
            {
                var scan = scans[index];
                if (IncludeNearbyScan(scan, precursorScan))
                {
                    if (scan.ScanNumber > precursorScan.ScanNumber)
                    {
                        if (Options.AveragingVector == AveragingVector.Before)
                        {
                            break;
                        }
                        ++scanCount;
                    }
                    output.Add(scan);
                }
            }
            return output;
        }

        /// <summary>
        /// Decides wheter the scan should be included in the analysis.
        /// Generally only allows MS1 full scans and if FAIMS mode is on, then
        /// include only scans with the same CV.
        /// </summary>
        /// <param name="scan">The scan in question</param>
        /// <param name="precursorScan">The taret scan to compare against.</param>
        /// <returns>Boolean whether to use the scan.</returns>
        public static bool IncludeNearbyScan(Scan scan, Scan precursorScan)
        {
            if (scan.MsOrder != 1) {
                return false;
            }

            // Faims scan matching.
            if (scan.FaimsState == Data.TriState.On && scan.FaimsCV != precursorScan.FaimsCV) {
                return false;
            }

            // SIM scan exclusion.
            // Using the filterline here since the scan type might not be read.
            if (scan.ScanNumber != precursorScan.ScanNumber && !scan.FilterLine.ToLower().Contains("full")) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Run a single Monocle scan.
        /// </summary>
        /// <param name="Ms1ScansCentroids"></param>
        /// <param name="ParentScan"></param>
        /// <param name="precursor"></param>
        public static void Run(List<Scan> Ms1ScansCentroids, Scan ParentScan, Precursor precursor, MonocleOptions Options)
        {
            int thecount = 0;
            double precursorMz = precursor.IsolationMz;
            if (precursorMz < 1)
            {
                precursorMz = precursor.Mz;
            }
            int precursorCharge = precursor.Charge;

            if (Options.UseMostIntense && precursor.IsolationWidth > 0)
            {
                // Re-assign the precursor m/z to that of the most intense peak in the isolation window.
                int peakIndex = PeakMatcher.MostIntenseIndex(ParentScan, precursor.IsolationMz, precursor.IsolationWidth / 2, PeakMatcher.DALTON);
                if (peakIndex >= 0)
                {
                    precursorMz = ParentScan.Centroids[peakIndex].Mz;
                }    
            }

            // Search for ion in parent scan, use parent ion mz for future peaks
            int index = PeakMatcher.Match(ParentScan, precursorMz, 50, PeakMatcher.PPM);
            if (index >= 0)
            {
                precursorMz = ParentScan.Centroids[index].Mz;
            }

            // For charge detection
            int bestCharge = 0;
            double bestScore = -1;
            //double bestScore = 1000000;
            int bestIndex = 0;
            bool containsSe = false;
            List<double> bestPeaks = new List<double>();
            List<double> bestPeakIntensities = new List<double>();

            //Create new class to maintain ref class options
            ChargeRange chargeRange = new ChargeRange(precursorCharge, precursorCharge);
            if (Options.Charge_Detection || precursorCharge == 0)
            {
                chargeRange.Low = Options.Charge_Range.Low;
                chargeRange.High = Options.Charge_Range.High;
            }
            List<double> scores = new List<double>();
            for (int charge = chargeRange.Low; charge <= chargeRange.High; charge++)
            {
                // Restrict number of isotopes to consider based on precursor mass.
                double mass = precursor.Mz * charge;

                List<bool> envelopeOptions = new List<bool>();
                envelopeOptions.Add(false);
                if (Options.SearchForSelenium)
                {
                    envelopeOptions.Add(true);
                }
                foreach (var includeSelenium in envelopeOptions)
                {
                    var isotopeRange = new IsotopeRange(mass, includeSelenium);
                    // Generate expected relative intensities.
                    var compareSize = isotopeRange.CompareSize;
                    // add 5% to give bias toward left peaks.
                    double biasFactor = 1.05;
                    if (includeSelenium)
                    {
                       // compareSize += 2;
                        //biasFactor = 1.0;
                    }
                    List<double> expected = PeptideEnvelopeCalculator.GetTheoreticalEnvelope(precursorMz, charge, compareSize, includeSelenium);
                    Vector.Scale(expected);

                   // Debug.Print("Isotope list");
                    for (int ii=0; ii< expected.Count; ii++)
                    {
                     //   Debug.Print("{0}", expected[ii]);
                    }

                    PeptideEnvelope envelope = PeptideEnvelopeExtractor.Extract(Ms1ScansCentroids, precursorMz, charge, isotopeRange.Left, isotopeRange.Isotopes);
                    thecount++;
                    // Get best match using dot product.
                    // Limit the number of isotopeRange peaks to test
                    for (int i = 0; i < (isotopeRange.Isotopes - (isotopeRange.CompareSize - 1)); ++i)
                    {
                        List<double> observed = envelope.averageIntensity.GetRange(i, compareSize);
                        Vector.Scale(observed);
                        PeptideEnvelopeExtractor.ScaleByPeakCount(observed, envelope, i);
                        double dot = Vector.Dot(observed, expected);
                        double score = Vector.L2(observed, expected);
                        score = dot;
                        scores.Add(score);
                       // score = dot;
                        if (i == 3)
                            thecount++;
                        Debug.Print("{0},{1},{2},{3},{4},{5}",i,charge,includeSelenium,score,dot, compareSize);

                       if (score > bestScore * biasFactor)
                       //if (score < bestScore)
                            {
                                bestScore = score;
                            if (score > 0.1)
                            {
                                // A peak to the left is included, so add
                                // offset to get monoisotopic index.
                                if (includeSelenium)
                                    bestIndex = i + 5;
                                else
                                    bestIndex = i + 1;
                                bestCharge = charge;
                                containsSe = includeSelenium;
                                bestPeaks = envelope.mzs[bestIndex];
                                bestPeakIntensities = envelope.intensities[bestIndex];
                            }
                        }
                    }
                }
            } // end charge for loop
            scores.Sort();
            Debug.Print("Winning distance: {0}", scores[1] - scores[0]);
            if (bestCharge > 0)
            {
                precursor.Charge = bestCharge;
            }

            // Calculate m/z
            if (bestPeaks.Count > 0)
            {
                precursor.Mz = Vector.WeightedAverage(bestPeaks, bestPeakIntensities);
            }
            else
            {
                precursor.Mz = precursorMz;
            }
            precursor.isSe = containsSe;


            precursor.IsolationSpecificity = IsolationSpecificityCalculator.calculate(
                ParentScan.Centroids,
                precursor.IsolationMz,
                precursor.Mz,
                precursor.Charge,
                precursor.IsolationWidth
            );
        }
    }
}
