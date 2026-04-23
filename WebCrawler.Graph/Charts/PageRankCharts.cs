using ScottPlot;
using WebCrawler.Graph.Results;

namespace WebCrawler.Graph.Charts
{
    /// <summary>
    /// All PageRank-related charts (iterations vs d, log-log distribution,
    /// norm convergence, Top-N). Receives ready-made data; does not compute anything.
    /// </summary>
    public static class PageRankCharts
    {
        private static readonly MarkerShape[] NormMarkers =
        {
            MarkerShape.FilledCircle,
            MarkerShape.FilledSquare,
            MarkerShape.FilledDiamond
        };

        private static readonly Color[] NormColors =
        {
            Colors.CornflowerBlue,
            Colors.Coral,
            Colors.MediumSeaGreen
        };

        public static void RenderIterationsVsDamping(
            IReadOnlyList<(string NormName, double[] Dampings, int[] Iterations)> series,
            string filename)
        {
            var plt = new Plot();

            for (int i = 0; i < series.Count; i++)
            {
                var s = series[i];
                double[] ys = s.Iterations.Select(it => System.Math.Log10(it)).ToArray();

                var sc = plt.Add.Scatter(s.Dampings, ys);
                sc.LegendText = s.NormName;
                sc.MarkerShape = NormMarkers[i % NormMarkers.Length];
                sc.Color = NormColors[i % NormColors.Length];
                sc.LineWidth = 1.5f;
                sc.MarkerSize = 8;
            }

            plt.Axes.Bottom.Label.Text = "Damping factor d";
            plt.Axes.Left.Label.Text = "log₁₀(iterations to converge)";
            plt.Title("Iterations vs damping factor (ε = 1e-6)");
            plt.ShowLegend();

            string path = ChartPaths.Ensure(filename);
            plt.SavePng(path, 900, 500);
            ChartPaths.LogSaved(path);
        }

        public static void RenderDistributionLogLog(
            PageRankAnalysis analysis,
            double slope,
            double intercept,
            string filename)
        {
            double[] sorted = analysis.Scores.Where(v => v > 0).OrderByDescending(v => v).ToArray();
            if (sorted.Length < 2) return;

            double[] logRank = Enumerable.Range(1, sorted.Length).Select(r => System.Math.Log10(r)).ToArray();
            double[] logPr = sorted.Select(v => System.Math.Log10(v)).ToArray();

            double[] fitX = { logRank.First(), logRank.Last() };
            double[] fitY = fitX.Select(x => slope * x + intercept).ToArray();

            var plt = new Plot();

            var sc = plt.Add.Scatter(logRank, logPr);
            sc.LineWidth = 0;
            sc.MarkerSize = 4;
            sc.Color = Colors.CornflowerBlue;
            sc.LegendText = "PR values";

            var fit = plt.Add.Scatter(fitX, fitY);
            fit.MarkerSize = 0;
            fit.LineWidth = 2;
            fit.LinePattern = LinePattern.Dashed;
            fit.Color = Colors.Coral;
            fit.LegendText = $"OLS fit: slope={slope:F2}";

            plt.Axes.Bottom.Label.Text = "log₁₀(rank)";
            plt.Axes.Left.Label.Text = "log₁₀(PageRank)";
            plt.Title($"PageRank distribution (log-log), d={analysis.DampingFactor}");
            plt.ShowLegend();

            string path = ChartPaths.Ensure(filename);
            plt.SavePng(path, 900, 500);
            ChartPaths.LogSaved(path);
        }

        public static void RenderNormConvergence(
            IReadOnlyList<PageRankAnalysis> perNormRuns,
            string filename)
        {
            var plt = new Plot();

            for (int i = 0; i < perNormRuns.Count; i++)
            {
                var run = perNormRuns[i];
                double[] xs = Enumerable.Range(1, run.ConvergencePath.Count)
                    .Select(j => (double)j)
                    .ToArray();
                double[] ys = run.ConvergencePath
                    .Select(e => System.Math.Log10(System.Math.Max(e, 1e-15)))
                    .ToArray();

                var sc = plt.Add.Scatter(xs, ys);
                sc.LegendText = run.NormName;
                sc.Color = NormColors[i % NormColors.Length];
                sc.MarkerShape = NormMarkers[i % NormMarkers.Length];
                sc.MarkerSize = 5;
                sc.LineWidth = 1.5f;
            }

            plt.Axes.Bottom.Label.Text = "Iteration";
            plt.Axes.Left.Label.Text = "log₁₀(norm error)";
            plt.Title("Norm convergence comparison (d=0.85)");
            plt.ShowLegend();

            string path = ChartPaths.Ensure(filename);
            plt.SavePng(path, 900, 500);
            ChartPaths.LogSaved(path);
        }

        public static void RenderTopN(
            IReadOnlyList<(int Node, double Score)> top,
            IReadOnlyList<string> urls,
            double dampingFactor,
            string filename)
        {
            double[] values = top.Select(x => x.Score * 1e6).ToArray();
            double[] positions = Enumerable.Range(0, top.Count).Select(i => (double)i).ToArray();
            string[] labels = top.Select(x => ShortenUrl(urls[x.Node])).ToArray();

            var plt = new Plot();
            var bars = plt.Add.Bars(positions, values);
            bars.Color = Colors.CornflowerBlue;

            plt.Axes.Bottom.SetTicks(positions, labels);
            plt.Axes.Bottom.TickLabelStyle.Rotation = 45;
            plt.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleRight;
            plt.Axes.Bottom.TickLabelStyle.FontSize = 10;
            plt.Axes.Bottom.Label.Text = "URL";
            plt.Axes.Left.Label.Text = "PageRank × 10⁻⁶";
            plt.Title($"Top-{top.Count} pages by PageRank (d={dampingFactor})");

            string path = ChartPaths.Ensure(filename);
            plt.SavePng(path, 1400, 800);
            ChartPaths.LogSaved(path);
        }

        /// <summary>
        /// Strips scheme / trailing slash and truncates long URLs for chart labels.
        /// </summary>
        private static string ShortenUrl(string url, int maxLen = 60)
        {
            if (string.IsNullOrEmpty(url)) return "";
            string s = url;
            if (s.StartsWith("https://")) s = s.Substring(8);
            else if (s.StartsWith("http://")) s = s.Substring(7);
            if (s.EndsWith("/") && s.Length > 1) s = s.Substring(0, s.Length - 1);
            if (s.Length > maxLen) s = s.Substring(0, maxLen - 1) + "…";
            return s;
        }
    }
}
