using ScottPlot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WebCrawler.Graph
{
    public class PowerLawFitter
    {
        public static (double gamma, double r2) FitOLS(IReadOnlyList<int> degrees, string outputPath = "ols_fit.png")
        {
            var N = degrees.Count;
            var groups = degrees
                .Where(d => d > 0)
                .GroupBy(d => d)
                .OrderBy(g => g.Key)
                .ToList();
            int m = groups.Count;
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
                logk[i] = Math.Log(groups[i].Key);
                logPk[i] = Math.Log(ccdf[i]);
            }

            var logkAvg = logk.Average();
            var logPkAvg = logPk.Average();
            double ssXY = 0, ssXX = 0, ssYY = 0;
            for (int i = 0; i < m; i++)
            {
                ssXY += (logk[i] - logkAvg) * (logPk[i] - logPkAvg);
                ssXX += Math.Pow(logk[i] - logkAvg, 2);
                ssYY += Math.Pow(logPk[i] - logPkAvg, 2);
            }
            if (ssXX == 0 || ssYY == 0)
                return (double.NaN, double.NaN);

            double slope = ssXY / ssXX;
            double intercept = logPkAvg - slope * logkAvg;
            double gamma = 1 - slope;
            double r2 = Math.Pow(ssXY, 2) / (ssXX * ssYY);

            var plt = new Plot();

            var scatter = plt.Add.Scatter(logk, logPk);
            scatter.LegendText = "Empirical CCDF";
            scatter.LineWidth = 0;
            scatter.MarkerSize = 6;
            scatter.Color = Colors.SteelBlue;

            double[] fitX = { logk.Min(), logk.Max() };
            double[] fitY = { slope * fitX[0] + intercept, slope * fitX[1] + intercept };
            var line = plt.Add.Scatter(fitX, fitY);
            line.LegendText = $"OLS: γ={gamma:F3}, R²={r2:F4}";
            line.MarkerSize = 0;
            line.LineWidth = 2;
            line.Color = Colors.OrangeRed;

            plt.Title("Power law fit OLS (CCDF)");
            plt.XLabel("log(k)");
            plt.YLabel("log P(K ≥ k)");
            plt.ShowLegend();
            plt.SavePng(outputPath, 800, 550);

            return (gamma, r2);
        }

        public static (double gamma, double ks, double x_min) FitMLE(IReadOnlyList<int> degrees, string outputPath = "mle_fit.png")
        {
            double bestKs = double.MaxValue, bestXMin = 0, bestGamma = 0;
            List<int> bestTail = new();

            foreach (var x_min in degrees.Distinct().OrderBy(x => x))
            {
                var tail = degrees.Where(x => x >= x_min).ToList();
                if (tail.Count < 50)
                    continue;

                var gamma = 1 + (double)tail.Count * (1.0 / tail.Sum(x => Math.Log((double)x / x_min)));
                tail.Sort();
                double ks = 0;
                for (int i = 0; i < tail.Count; i++)
                {
                    double empiricalCDF = (double)(i + 1) / tail.Count;
                    double theoreticalCDF = 1 - Math.Pow(((double)tail[i] / x_min), (1 - gamma));
                    ks = Math.Max(ks, Math.Abs(empiricalCDF - theoreticalCDF));
                }
                if (ks < bestKs)
                {
                    bestKs = ks;
                    bestGamma = gamma;
                    bestXMin = x_min;
                    bestTail = new List<int>(tail);
                }
            }

            if (bestTail.Count > 0)
            {
                bestTail.Sort();
                int n = bestTail.Count;

                double[] empX = bestTail.Select(x => (double)x).ToArray();
                double[] empY = Enumerable.Range(1, n).Select(i => (double)i / n).ToArray();

                double[] theoY = empX
                    .Select(x => 1.0 - Math.Pow(x / bestXMin, 1 - bestGamma))
                    .ToArray();

                var plt = new Plot();

                var empScatter = plt.Add.Scatter(empX, empY);
                empScatter.LegendText = "Empirical CDF";
                empScatter.LineWidth = 1.5f;
                empScatter.MarkerSize = 0;
                empScatter.Color = Colors.SteelBlue;

                var theoScatter = plt.Add.Scatter(empX, theoY);
                theoScatter.LegendText = $"MLE: γ={bestGamma:F3}, x_min={bestXMin}, KS={bestKs:F4}";
                theoScatter.LineWidth = 2;
                theoScatter.MarkerSize = 0;
                theoScatter.Color = Colors.OrangeRed;
                theoScatter.LinePattern = LinePattern.Dashed;

                plt.Title("Power law fit MLE (CDF)");
                plt.XLabel("k");
                plt.YLabel("P(K ≤ k)");
                plt.ShowLegend();
                plt.SavePng(outputPath, 800, 550);
            }

            return (bestGamma, bestKs, bestXMin);
        }
    }
}