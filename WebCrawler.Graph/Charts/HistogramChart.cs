using ScottPlot;

namespace WebCrawler.Graph.Charts
{
    /// <summary>
    /// Histogram of integer values (e.g. degree distributions, component sizes).
    /// Automatically thins X-axis labels when there are many distinct values.
    /// </summary>
    public static class HistogramChart
    {
        public static void Render(
            string title,
            IReadOnlyList<int> values,
            string xAxisLabel,
            string filename)
        {
            var groups = values
                .GroupBy(d => d)
                .OrderBy(g => g.Key)
                .ToList();

            if (groups.Count == 0) return;

            double[] positions = groups.Select(g => (double)g.Key).ToArray();
            double[] counts = groups.Select(g => (double)g.Count()).ToArray();
            string[] labels = groups.Select(g => g.Key.ToString()).ToArray();

            double minGap = (positions.Last() - positions.First()) / 40.0;
            var visiblePos = new List<double>();
            var visibleLabels = new List<string>();
            double lastVisible = positions[0] - minGap - 1;

            for (int i = 0; i < positions.Length; i++)
            {
                double prevDist = positions[i] - lastVisible;
                double nextDist = i < positions.Length - 1 ? positions[i + 1] - positions[i] : double.MaxValue;
                if (prevDist >= minGap || nextDist >= minGap)
                {
                    visiblePos.Add(positions[i]);
                    visibleLabels.Add(labels[i]);
                    lastVisible = positions[i];
                }
            }

            var plt = new Plot();
            var bars = plt.Add.Bars(positions, counts);
            bars.Color = Colors.CornflowerBlue;

            plt.Axes.Bottom.SetTicks(visiblePos.ToArray(), visibleLabels.ToArray());
            plt.Axes.Bottom.TickLabelStyle.Rotation = 90;
            plt.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleLeft;
            plt.Axes.Bottom.Label.Text = xAxisLabel;
            plt.Axes.Left.Label.Text = "Count";
            plt.Title(title);

            string path = ChartPaths.Ensure(filename);
            plt.SavePng(path, 900, 500);
            ChartPaths.LogSaved(path);
        }

        /// <summary>
        /// Log-log scatter plot of a count distribution (e.g. degree distribution).
        /// Zero-valued bins are dropped (log is undefined at 0). Suitable for Z3.
        /// </summary>
        public static void RenderLogLog(
            string title,
            IReadOnlyList<int> values,
            string xAxisLabel,
            string filename)
        {
            var groups = values
                .Where(d => d > 0)
                .GroupBy(d => d)
                .OrderBy(g => g.Key)
                .ToList();

            if (groups.Count < 2) return;

            double[] logK = groups.Select(g => System.Math.Log10(g.Key)).ToArray();
            double[] logP = groups.Select(g => System.Math.Log10(g.Count())).ToArray();

            var plt = new Plot();

            var scatter = plt.Add.Scatter(logK, logP);
            scatter.LineWidth = 0;
            scatter.MarkerSize = 6;
            scatter.Color = Colors.CornflowerBlue;
            scatter.LegendText = "Empirical";

            plt.Axes.Bottom.Label.Text = $"log₁₀({xAxisLabel})";
            plt.Axes.Left.Label.Text = "log₁₀(count)";
            plt.Title(title);
            plt.ShowLegend();

            string path = ChartPaths.Ensure(filename);
            plt.SavePng(path, 800, 550);
            ChartPaths.LogSaved(path);
        }
    }
}
