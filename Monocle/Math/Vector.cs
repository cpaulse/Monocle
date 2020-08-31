
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Monocle.Math {
    public static class Vector {
        /// <summary>
        /// Calculate the dot product of two lists.
        /// </summary>
        public static double Dot(List<double> a, List<double> b) {
            double result = 0;
            for(int i = 0; i < a.Count && i < b.Count; i++) {
                result += a[i] * b[i];
            }
            return result;
        }

        public static double RMS(List<double> a, List<double> b)
        {
            double result = 0;
            for (int i = 0; i < a.Count && i < b.Count; i++)
            {
                 result += (a[i] - b[i]) * (a[i] - b[i]);
            }
            return System.Math.Sqrt(result)/a.Count;
        }

        public static double ChiSquared(List<double> a, List<double> b)
        {
            double result = 0;
            for (int i = 0; i < a.Count && i < b.Count; i++)
            {
                if (b[i] == 0)
                {
                    if (a[i] == 0)
                        continue;
                    else
                    {
                        double minNonzero = b.OrderBy(j => j).Take(2).ToList()[1];
                        Debug.Assert(minNonzero > 0);
                        result += a[i] / minNonzero;
                    }
                }
                else 
                {
                    result += (a[i] - b[i]) * (a[i] - b[i]) / b[i];
                }
            }
            return result;
        }
        /// <summary>
        /// Calculate average value
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static double Average(List<double> x)
        {
            double sum = 0;
            int count = 0;
            foreach (var v in x)
            {
                sum += v;
                ++count;
            }
            return count > 0 ? sum / count : 0;
        }

        /// <summary>
        /// Calculated weighted average of x
        /// </summary>
        /// <param name="x">The list to average</param>
        /// <param name="weights">the weights of each value in x</param>
        /// <returns>The weighted average</returns>
        public static double WeightedAverage(List<double> x, List<double> weights) {
            if (x.Count == 0 || x.Count != weights.Count) {
                return 0;
            }
            double sumWeightedX = 0;
            double sumX = 0;
            double sumWeights = 0;
            for (int i = 0; i < x.Count; i++)
            {
                sumWeightedX += x[i] * weights[i];
                sumX += x[i];
                sumWeights += weights[i];
            }
            if (sumWeights > 0)
            {
                return sumWeightedX / sumWeights;
            }
            return sumX / x.Count;
        }

        /// <summary>
        /// Scales all values in the input so that the max is 1
        /// </summary>
        ///
        /// <param name="x">The input list.</param>
        public static void Scale(List<double> x, bool takeLog = false)
        {
            double max = 0;
            for (int j = 0; j < x.Count; j++)
            {
                if (x[j] > max)
                {
                    max = x[j];
                }
                if (takeLog && x[j] == 0)
                    x[j] = 1;
            }
            if (max > 0)
            {
                for (int j = 0; j < x.Count; j++)
                {
                    x[j] = x[j] / max;
                }
            }
            if (takeLog)
                for (int j = 0; j < x.Count; j++)
                    x[j] = System.Math.Log(x[j]);
        }

        /// <summary>
        /// Scales all values in the input so that the sum is 1
        /// </summary>
        ///
        /// <param name="x">The input list.</param>
        public static void Normalize(List<double> x, int start = 0, bool takeLog = false)
        {
            double sum = 0;
            for (int j = start; j < x.Count; j++)
            {
                if (takeLog && x[j] == 0)
                    x[j] = 1;
                if (takeLog)
                    sum += System.Math.Log(x[j]);
                else
                    sum += x[j];
            }
            if (sum == 0)
                return;
            for (int j = 0; j < x.Count; j++)
            {
                if (takeLog)
                {
                    if (x[j] == 0)
                        x[j] = 1;
                    x[j] = System.Math.Log(x[j]) / sum;
                }
                else
                    x[j] /= sum;
            }
        }


        public static void Log(List<double> x)
        {
            for (int j = 0; j < x.Count; j++)
            {
                if (x[j] != 0)
                    x[j] = System.Math.Sqrt(x[j]);
            }
        }
    }
}
