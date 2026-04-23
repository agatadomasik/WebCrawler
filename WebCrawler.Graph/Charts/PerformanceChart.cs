using ScottPlot;

namespace WebCrawler.Graph.Charts
{
    /// <summary>
    /// Charts for the multi-threaded crawler performance test (Z2).
    /// Takes pre-computed (threads, throughput, speedup) tuples;
    /// does not run any crawl itself.
    /// </summary>
    public static class PerformanceChart
    {
        public static void Render(
            IReadOnlyList<(int Threads, double Throughput, double Seconds, int Pages)> results,
            string throughputFilename = "perf_throughput.png",
            string speedupFilename = "perf_speedup.png")
        {
            if (results.Count == 0) return;

            double baseline = results[0].Throughput;

            double[] threads = results.Select(r => (double)r.Threads).ToArray();
            double[] throughput = results.Select(r => r.Throughput).ToArray();
            double[] speedup = results.Select(r => baseline > 0 ? r.Throughput / baseline : 1.0).ToArray();

            RenderThroughput(threads, throughput, throughputFilename);
            RenderSpeedup(threads, speedup, speedupFilename);
        }

        private static void RenderThroughput(double[] threads, double[] throughput, string filename)
        {
            var plt = new Plot();

            var sc = plt.Add.Scatter(threads, throughput);
            sc.Color = Colors.CornflowerBlue;
            sc.LineWidth = 2;
            sc.MarkerShape = MarkerShape.FilledCircle;
            sc.MarkerSize = 8;
            sc.LegendText = "Throughput";

            string[] labels = threads.Select(t => ((int)t).ToString()).ToArray();
            plt.Axes.Bottom.SetTicks(threads, labels);
            plt.Axes.Bottom.Label.Text = "Thread count";
            plt.Axes.Left.Label.Text = "Throughput [pages/s]";
            plt.Title("Crawler throughput vs thread count");
            plt.ShowLegend();

            string path = ChartPaths.Ensure(filename);
            plt.SavePng(path, 900, 500);
            ChartPaths.LogSaved(path);
        }

        private static void RenderSpeedup(double[] threads, double[] speedup, string filename)
        {
            var plt = new Plot();

            var actual = plt.Add.Scatter(threads, speedup);
            actual.Color = Colors.CornflowerBlue;
            actual.LineWidth = 2;
            actual.MarkerShape = MarkerShape.FilledCircle;
            actual.MarkerSize = 8;
            actual.LegendText = "Speedup";

            string[] labels = threads.Select(t => ((int)t).ToString()).ToArray();
            plt.Axes.Bottom.SetTicks(threads, labels);
            plt.Axes.Bottom.Label.Text = "Thread count";
            plt.Axes.Left.Label.Text = "Speedup (relative to 1 thread)";
            plt.Title("Crawler speedup vs thread count");
            plt.ShowLegend();

            string path = ChartPaths.Ensure(filename);
            plt.SavePng(path, 900, 500);
            ChartPaths.LogSaved(path);
        }
    }
}
