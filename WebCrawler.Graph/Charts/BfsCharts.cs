using ScottPlot;

namespace WebCrawler.Graph.Charts
{
    /// <summary>
    /// BFS-related charts: pair distance histogram with an exponential decay fit.
    /// </summary>
    public static class BfsCharts
    {
        public static void RenderPairDistanceHistogram(
            long[] histogram,
            double slope,
            double intercept,
            string filename)
        {
            if (histogram.Length <= 1) return;

            // index 0 is unused (u→u not counted); plot distances 1..Dmax
            double[] xs = Enumerable.Range(1, histogram.Length - 1).Select(i => (double)i).ToArray();
            double[] counts = xs.Select(d => (double)histogram[(int)d]).ToArray();

            var plt = new Plot();

            var bars = plt.Add.Bars(xs, counts);
            bars.Color = Colors.CornflowerBlue;

            // Overlay regression line (fitted on log10 of count)
            if (!double.IsNaN(slope) && !double.IsNaN(intercept))
            {
                double[] fitX = { xs.First(), xs.Last() };
                double[] fitY = fitX.Select(x => System.Math.Pow(10, slope * x + intercept)).ToArray();
                var line = plt.Add.Scatter(fitX, fitY);
                line.MarkerSize = 0;
                line.LineWidth = 2;
                line.LinePattern = LinePattern.Dashed;
                line.Color = Colors.OrangeRed;
                line.LegendText = $"log₁₀(count) ≈ {slope:F2}·d + {intercept:F2}";
            }

            plt.Axes.Bottom.Label.Text = "Shortest-path distance d";
            plt.Axes.Left.Label.Text = "Number of ordered pairs";
            plt.Title("Pair distance histogram");
            plt.ShowLegend();

            string path = ChartPaths.Ensure(filename);
            plt.SavePng(path, 900, 500);
            ChartPaths.LogSaved(path);
        }
    }
}
