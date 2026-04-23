using ScottPlot;
using WebCrawler.Graph.Results;

namespace WebCrawler.Graph.Charts
{
    /// <summary>
    /// Clustering-related charts (histogram of C(v) and the log-log plot of C(k) vs k).
    /// </summary>
    public static class ClusteringCharts
    {
        private const int BinCount = 10;
        private const double BinSize = 0.1;

        public static void RenderLocalHistogram(ClusteringAnalysis analysis, string filename)
        {
            double[] counts = new double[BinCount];
            foreach (var val in analysis.LocalCoefficients.Where(v => v >= 0))
            {
                int bin = (int)(val / BinSize);
                if (bin >= BinCount) bin = BinCount - 1;
                counts[bin]++;
            }

            string[] labels = Enumerable.Range(0, BinCount)
                .Select(i => $"{i * BinSize:F1}-{(i + 1) * BinSize:F1}")
                .ToArray();
            double[] positions = Enumerable.Range(0, BinCount).Select(i => (double)i).ToArray();

            var plt = new Plot();
            var bar = plt.Add.Bars(positions, counts);
            bar.Color = Colors.CornflowerBlue;

            plt.Axes.Bottom.SetTicks(positions, labels);
            plt.Axes.Bottom.Label.Text = "Interval";
            plt.Axes.Left.Label.Text = "Number of values";
            plt.Title("Local clustering coefficients");

            string path = ChartPaths.Ensure(filename);
            plt.SavePng(path, 800, 500);
            ChartPaths.LogSaved(path);
        }

        public static void RenderLogLog(ClusteringAnalysis analysis, string filename)
        {
            var points = analysis.AverageCByDegree
                .Where(x => x.Key > 0 && x.Value > 0)
                .OrderBy(x => x.Key)
                .ToArray();

            if (points.Length < 2) return;

            double[] logK = points.Select(x => System.Math.Log10(x.Key)).ToArray();
            double[] logC = points.Select(x => System.Math.Log10(x.Value)).ToArray();

            double[] fitX = { logK.Min(), logK.Max() };
            double[] fitY = fitX.Select(x => analysis.LogLogSlope * x + analysis.LogLogIntercept).ToArray();

            var plt = new Plot();

            var scatter = plt.Add.Scatter(logK, logC);
            scatter.LineWidth = 0;
            scatter.MarkerSize = 6;
            scatter.LegendText = "C(k) empirical";

            var line = plt.Add.Scatter(fitX, fitY);
            line.MarkerSize = 0;
            line.LineWidth = 2;
            line.LinePattern = LinePattern.Dashed;
            line.LegendText = $"fit: k^{analysis.LogLogSlope:F2}";

            plt.Axes.Bottom.Label.Text = "log(k)";
            plt.Axes.Left.Label.Text = "log(C(k))";
            plt.Title("C(k) vs k  [log-log]");
            plt.ShowLegend();

            string path = ChartPaths.Ensure(filename);
            plt.SavePng(path, 800, 500);
            ChartPaths.LogSaved(path);
        }
    }
}
