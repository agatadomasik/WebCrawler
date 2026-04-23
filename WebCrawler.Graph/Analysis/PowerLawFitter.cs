using WebCrawler.Graph.Results;

namespace WebCrawler.Graph.Analysis
{
    /// <summary>
    /// Fits a power-law to a degree distribution.
    /// Pure maths — returns <see cref="OlsPowerLawFit"/> / <see cref="MlePowerLawFit"/>.
    /// Rendering is handled by a dedicated class in the Charts namespace.
    /// </summary>
    public static class PowerLawFitter
    {
        public static OlsPowerLawFit? FitOls(IReadOnlyList<int> degrees)
        {
            int N = degrees.Count;
            var groups = degrees
                .Where(d => d > 0)
                .GroupBy(d => d)
                .OrderBy(g => g.Key)
                .ToList();

            int m = groups.Count;
            if (m < 2) return null;

            var logk = new double[m];
            var logPk = new double[m];
            var counts = groups.Select(g => g.Count()).ToArray();

            int tailSum = 0;
            var ccdf = new double[m];
            for (int i = m - 1; i >= 0; i--)
            {
                tailSum += counts[i];
                ccdf[i] = (double)tailSum / N;
            }

            for (int i = 0; i < m; i++)
            {
                logk[i] = System.Math.Log(groups[i].Key);
                logPk[i] = System.Math.Log(ccdf[i]);
            }

            double logkAvg = logk.Average();
            double logPkAvg = logPk.Average();
            double ssXY = 0, ssXX = 0, ssYY = 0;
            for (int i = 0; i < m; i++)
            {
                ssXY += (logk[i] - logkAvg) * (logPk[i] - logPkAvg);
                ssXX += System.Math.Pow(logk[i] - logkAvg, 2);
                ssYY += System.Math.Pow(logPk[i] - logPkAvg, 2);
            }

            if (ssXX == 0 || ssYY == 0) return null;

            double slope = ssXY / ssXX;
            double intercept = logPkAvg - slope * logkAvg;
            double gamma = 1 - slope;
            double r2 = System.Math.Pow(ssXY, 2) / (ssXX * ssYY);

            return new OlsPowerLawFit(gamma, r2, logk, logPk, slope, intercept);
        }

        public static MlePowerLawFit? FitMle(IReadOnlyList<int> degrees, int minTailSize = 50)
        {
            double bestKs = double.MaxValue;
            double bestGamma = 0;
            int bestXMin = 0;
            List<int>? bestTail = null;

            foreach (int xMin in degrees.Distinct().OrderBy(x => x))
            {
                var tail = degrees.Where(x => x >= xMin).ToList();
                if (tail.Count < minTailSize) continue;

                double gamma = 1 + tail.Count * (1.0 / tail.Sum(x => System.Math.Log((double)x / xMin)));
                tail.Sort();

                double ks = 0;
                for (int i = 0; i < tail.Count; i++)
                {
                    double empiricalCdf = (double)(i + 1) / tail.Count;
                    double theoreticalCdf = 1 - System.Math.Pow((double)tail[i] / xMin, 1 - gamma);
                    ks = System.Math.Max(ks, System.Math.Abs(empiricalCdf - theoreticalCdf));
                }

                if (ks < bestKs)
                {
                    bestKs = ks;
                    bestGamma = gamma;
                    bestXMin = xMin;
                    bestTail = tail;
                }
            }

            if (bestTail == null) return null;

            return new MlePowerLawFit(bestGamma, bestKs, bestXMin, bestTail);
        }
    }
}
