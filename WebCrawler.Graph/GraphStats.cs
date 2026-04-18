using ScottPlot;
using System.Runtime.InteropServices;
using WebCrawler.Domain;
using WebCrawler.Graph;

namespace WebCrawler.Graph
{
    public static class GraphStats
    {
        public static void Print(CrawlGraph graph)
        {
            int V = graph.V;
            int E = graph.E;
            var outDegrees = graph.OutDegrees;
            var inDegrees = graph.InDegrees;

            double avgOutDegree = (double)E / V;
            double avgInDegree = (double)E / V; // always equals avgOutDegree for directed graphs
            double density = (double)E / ((double)V * (V - 1));

            Console.WriteLine("\n======== GRAPH STATS ========");
            Console.WriteLine($"|V| = {V}");
            Console.WriteLine($"|E| = {E}");
            Console.WriteLine($"Avg out-degree: {avgOutDegree:F2}");
            Console.WriteLine($"Avg in-degree:  {avgInDegree:F2}");
            Console.WriteLine($"Density:        {density:F6}");

            PrintHistogram("Out-degree", outDegrees);
            PrintHistogram("In-degree", inDegrees);

            PowerLawFitter.FitOLS(inDegrees, "plots/inOSL.png");
            PowerLawFitter.FitMLE(inDegrees, "plots/inMLE.png");

            PowerLawFitter.FitOLS(outDegrees, "plots/outOLS.png");
            PowerLawFitter.FitMLE(outDegrees, "plots/outMLE.png");

            PrintBFSStats(graph);
            PrintClusteringStats(graph);
        }

        public static void PrintBFSStats(CrawlGraph graph)
        {
            var (avgDist, diameter, ecc, radius) = BFSExplorer.ExploreBFS(graph);

            Console.WriteLine("\n======== BFS STATS ========");

            double globalAvg = avgDist.Where(x => x >= 0).Average();

            Console.WriteLine($"Average distance: {globalAvg:F4}");
            Console.WriteLine($"Diameter: {diameter}");
            Console.WriteLine($"Radius: {radius}");

            Console.WriteLine($"\nAverage distance per node:");
            for (int i = 0; i < Math.Min(10, avgDist.Length); i++)
            {
                Console.WriteLine($"  {i}: {avgDist[i]:F2}");
            }

            Console.WriteLine($"\nEccentricity per node:");
            for (int i = 0; i < Math.Min(10, ecc.Length); i++)
            {
                Console.WriteLine($"  {i}: {ecc[i]}");
            }

            PrintHistogram("Eccentricity", ecc);
        }

        public static void PrintComponentStats(string label, Dictionary<int, int> components)
        {
            var sizes = components
                .GroupBy(kv => kv.Value)
                .Select(g => g.Count())
                .ToList();

            int count = sizes.Count;
            int largest = sizes.Max();

            Console.WriteLine($"\n======== {label} ========");
            Console.WriteLine($"Number of components: {count}");
            Console.WriteLine($"The largest: {largest} vertices");

            PrintHistogram($"{label} size distribution", sizes);
        }

        private static void PrintHistogram(string label, IReadOnlyList<int> degrees)
        {
            var groups = degrees
                .GroupBy(d => d)
                .OrderBy(g => g.Key)
                .ToList();

            Console.WriteLine($"\n{label} histogram (degree : count):");
            foreach (var g in groups)
                Console.WriteLine($"  {g.Key,4} : {g.Count(),5}  {new string('█', Math.Min(g.Count() / 2, 40))}");
        }

        public static void PrintClusteringStats(CrawlGraph graph)
        {
            double[] localCoeffs = ClusteringAnalyzer.ComputeLocalClusteringCoefficients(graph);

            int binCount = 10;
            double binSize = 0.1;
            double[] counts = new double[binCount];

            foreach (var val in localCoeffs.Where(v => v >= 0))
            {
                int bin = (int)(val / binSize);
                if (bin >= binCount) bin = binCount - 1;
                counts[bin]++;
            }

            string[] labels = Enumerable.Range(0, binCount)
                .Select(i => $"{i * binSize:F1}–{(i + 1) * binSize:F1}")
                .ToArray();

            var plt = new Plot();

            double[] positions = Enumerable.Range(0, binCount).Select(i => (double)i).ToArray();
            var bar = plt.Add.Bars(positions, counts);

            plt.Axes.Bottom.SetTicks(positions, labels);
            plt.Axes.Bottom.Label.Text = "Interval";
            plt.Axes.Left.Label.Text = "Number of values";
            plt.Title("Local clustering coefficients");

            plt.SavePng("plots/LocalClusteringCoefficients.png", 800, 500);

            var avgCByDegree = graph.OutDegrees
                .Zip(localCoeffs, (deg, c) => (deg, c))
                .Where(x => x.c != -1)
                .GroupBy(x => x.deg)
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(x => x.c)
                );
            var points = avgCByDegree
                .Where(x => x.Key > 0 && x.Value > 0)
                .OrderBy(x => x.Key)
                .ToArray();

            double[] ks = points.Select(x => (double)x.Key).ToArray();
            double[] avgCs = points.Select(x => x.Value).ToArray();

            double[] logK = ks.Select(Math.Log10).ToArray();
            double[] logC = avgCs.Select(Math.Log10).ToArray();

            int n = logK.Length;
            double sumX = logK.Sum();
            double sumY = logC.Sum();
            double sumXY = logK.Zip(logC, (x, y) => x * y).Sum();
            double sumX2 = logK.Select(x => x * x).Sum();

            double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            double intercept = (sumY - slope * sumX) / n;

            Console.WriteLine($"Nachylenie (exponent): {slope:F4}  (oczekiwane: -1)");
            Console.WriteLine($"Współczynnik:          {Math.Pow(10, intercept):F4}");

            // Linia regresji
            double[] fitX = new[] { logK.Min(), logK.Max() };
            double[] fitY = fitX.Select(x => slope * x + intercept).ToArray();

            // Wykres
            plt = new Plot();

            // Punkty danych
            var scatter = plt.Add.Scatter(logK, logC);
            scatter.LineWidth = 0;
            scatter.MarkerSize = 6;
            scatter.LegendText = "C(k) empiryczne";

            // Linia dopasowania
            var line = plt.Add.Scatter(fitX, fitY);
            line.MarkerSize = 0;
            line.LineWidth = 2;
            line.LinePattern = LinePattern.Dashed;
            line.LegendText = $"fit: k^{slope:F2}";

            plt.Axes.Bottom.Label.Text = "log(k)";
            plt.Axes.Left.Label.Text = "log(C(k))";
            plt.Title("C(k) vs k  [log-log]");
            plt.ShowLegend();

            plt.SavePng("plots/ck_loglog.png", 800, 500);
        }
    }
}