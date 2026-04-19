using ScottPlot;
using System.Runtime.InteropServices;
using WebCrawler.Domain;
using WebCrawler.Graph;
using static WebCrawler.Graph.RobustnessAnalyzer;

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

            PrintHistogram("Out-degree", outDegrees, "plots/out-degree.png");
            PrintHistogram("In-degree", inDegrees, "plots/in-degree.png");

            PowerLawFitter.FitOLS(inDegrees, "plots/inOSL.png");
            PowerLawFitter.FitMLE(inDegrees, "plots/inMLE.png");

            PowerLawFitter.FitOLS(outDegrees, "plots/outOLS.png");
            PowerLawFitter.FitMLE(outDegrees, "plots/outMLE.png");
        }

        public static void PlotRobustnessStats(List<RobustnessAnalysisResult> results, int totalNodes, string title)
        {
            Directory.CreateDirectory("plots");

            double[] fractions = results.Select(r => r.Fraction).ToArray();
            double[] wccFractions = results.Select(r => (double)r.LargestWCC / totalNodes).ToArray();
            double[] sccFractions = results.Select(r => (double)r.LargestSCC / totalNodes).ToArray();

            var plt = new Plot();

            var wccLine = plt.Add.Scatter(fractions, wccFractions);
            wccLine.LegendText = "Largest WCC";
            wccLine.Color = Colors.CornflowerBlue;
            wccLine.MarkerShape = MarkerShape.FilledCircle;
            wccLine.MarkerSize = 7;
            wccLine.LineWidth = 2;

            var sccLine = plt.Add.Scatter(fractions, sccFractions);
            sccLine.LegendText = "Largest SCC";
            sccLine.Color = Colors.Coral;
            sccLine.MarkerShape = MarkerShape.FilledSquare;
            sccLine.MarkerSize = 7;
            sccLine.LineWidth = 2;

            plt.Axes.Bottom.Label.Text = "Fraction of nodes removed";
            plt.Axes.Left.Label.Text = "Relative size of largest component";
            plt.ShowLegend();

            plt.SavePng($"plots/robustness_wcc_scc_{title}.png", 900, 500);
            Console.WriteLine($"Saved: plots/robustness_wcc_scc_{title}.png");
        }

        public static void PrintRubustnessStats(List<RobustnessAnalysisResult> results)
        {
            foreach (var res in results)
            {
                Console.WriteLine($"Fraction: {res.Fraction}");
                Console.WriteLine($"Largest WCC: {res.LargestWCC}");
                Console.WriteLine($"Largest SCC: {res.LargestSCC}");
                Console.WriteLine($"Average distance: {res.AvgDist}");
                Console.WriteLine($"Diameter: {res.Diameter}");
            }
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

            PrintHistogram("Eccentricity", ecc, "plots/Eccentricity.png");
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

            PrintHistogram($"{label} size distribution", sizes, $"plots/{label}.png");
        }

        private static void PrintHistogram(string label, IReadOnlyList<int> degrees, string filename)
        {
            var groups = degrees
                .GroupBy(d => d)
                .OrderBy(g => g.Key)
                .ToList();

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
            plt.Axes.Bottom.Label.Text = "Degree";
            plt.Axes.Left.Label.Text = "Count";
            plt.Title(label);

            Directory.CreateDirectory("plots");
            plt.SavePng(filename, 900, 500);
            Console.WriteLine($"Saved: {filename}");
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
                .Select(i => $"{i * binSize:F1}-{(i + 1) * binSize:F1}")
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

            double[] fitX = new[] { logK.Min(), logK.Max() };
            double[] fitY = fitX.Select(x => slope * x + intercept).ToArray();

            plt = new Plot();

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

        public static void PrintPageRankStats(CrawlGraph graph)
        {
            Directory.CreateDirectory("plots");

            var norms = new (string Name, Func<double[], double[], double> Fn)[]
            {
        ("L1",   PageRanker.NormL1),
        ("L2",   PageRanker.NormL2),
        ("LInf", PageRanker.NormLInf),
            };

            // ── 1. Iteracje do zbieżności vs d ──────────────────────────────────────
            var plt1 = new Plot();
            var markers = new MarkerShape[] {
        MarkerShape.FilledCircle,
        MarkerShape.FilledSquare,
        MarkerShape.FilledDiamond
    };
            var colors = new ScottPlot.Color[] {
        Colors.CornflowerBlue,
        Colors.Coral,
        Colors.MediumSeaGreen
    };

            for (int ni = 0; ni < norms.Length; ni++)
            {
                double[] xs = new double[PageRanker.DampingFactors.Length];
                double[] ys = new double[PageRanker.DampingFactors.Length];

                for (int di = 0; di < PageRanker.DampingFactors.Length; di++)
                {
                    double d = PageRanker.DampingFactors[di];
                    var (_, iters) = PageRanker.ComputePageRank(graph, norms[ni].Fn, d);
                    xs[di] = d;
                    ys[di] = Math.Log10(iters);
                }

                var sc = plt1.Add.Scatter(xs, ys);
                sc.LegendText = norms[ni].Name;
                sc.MarkerShape = markers[ni];
                sc.Color = colors[ni];
                sc.LineWidth = 1.5f;
                sc.MarkerSize = 8;
            }

            plt1.Axes.Bottom.Label.Text = "Damping factor d";
            plt1.Axes.Left.Label.Text = "log₁₀(iterations to converge)";
            plt1.Title("Iterations vs damping factor (ε = 1e-6)");
            plt1.ShowLegend();
            plt1.SavePng("plots/pr_iters_vs_d.png", 900, 500);
            Console.WriteLine("Saved: plots/pr_iters_vs_d.png");

            var (pr85, iters85) = PageRanker.ComputePageRank(graph, PageRanker.NormL1, 0.85);
            Console.WriteLine($"\nPageRank (d=0.85, L1): converged in {iters85} iterations");

            double[] sorted = pr85
                .Where(v => v > 0)
                .OrderByDescending(v => v)
                .ToArray();

            double[] logRank = Enumerable.Range(1, sorted.Length)
                .Select(r => Math.Log10(r))
                .ToArray();
            double[] logPR = sorted.Select(v => Math.Log10(v)).ToArray();

            int n = logRank.Length;
            double sx = logRank.Sum();
            double sy = logPR.Sum();
            double sxy = logRank.Zip(logPR, (x, y) => x * y).Sum();
            double sx2 = logRank.Select(x => x * x).Sum();
            double slope = (n * sxy - sx * sy) / (n * sx2 - sx * sx);
            double intercept = (sy - slope * sx) / n;

            Console.WriteLine($"PR distribution log-log slope: {slope:F4}  (expected ≈ -1 for power law)");

            double[] fitX = new[] { logRank.First(), logRank.Last() };
            double[] fitY = fitX.Select(x => slope * x + intercept).ToArray();

            var plt2 = new Plot();

            var sc2 = plt2.Add.Scatter(logRank, logPR);
            sc2.LineWidth = 0;
            sc2.MarkerSize = 4;
            sc2.Color = Colors.CornflowerBlue;
            sc2.LegendText = "PR values";

            var fit2 = plt2.Add.Scatter(fitX, fitY);
            fit2.MarkerSize = 0;
            fit2.LineWidth = 2;
            fit2.LinePattern = LinePattern.Dashed;
            fit2.Color = Colors.Coral;
            fit2.LegendText = $"OLS fit: slope={slope:F2}";

            plt2.Axes.Bottom.Label.Text = "log₁₀(rank)";
            plt2.Axes.Left.Label.Text = "log₁₀(PageRank)";
            plt2.Title("PageRank distribution (log-log), d=0.85");
            plt2.ShowLegend();
            plt2.SavePng("plots/pr_loglog.png", 900, 500);
            Console.WriteLine("Saved: plots/pr_loglog.png");

            var plt3 = new Plot();

            for (int ni = 0; ni < norms.Length; ni++)
            {
                var errors = CollectConvergencePath(graph, norms[ni].Fn, 0.85);
                double[] xs = Enumerable.Range(1, errors.Count).Select(i => (double)i).ToArray();
                double[] ys = errors.Select(e => Math.Log10(Math.Max(e, 1e-15))).ToArray();

                var sc3 = plt3.Add.Scatter(xs, ys);
                sc3.LegendText = norms[ni].Name;
                sc3.Color = colors[ni];
                sc3.MarkerShape = markers[ni];
                sc3.MarkerSize = 5;
                sc3.LineWidth = 1.5f;
            }

            plt3.Axes.Bottom.Label.Text = "Iteration";
            plt3.Axes.Left.Label.Text = "log₁₀(norm error)";
            plt3.Title("Norm convergence comparison (d=0.85)");
            plt3.ShowLegend();
            plt3.SavePng("plots/pr_norm_convergence.png", 900, 500);
            Console.WriteLine("Saved: plots/pr_norm_convergence.png");

            Console.WriteLine("\n======== TOP-20 PAGES (PageRank, d=0.85, L1) ========");
            var top20 = pr85
                .Select((score, idx) => (idx, score))
                .OrderByDescending(x => x.score)
                .Take(20)
                .ToArray();

            Console.WriteLine($"{"Rank",-6} {"Node",-10} {"PageRank",14}");
            Console.WriteLine(new string('-', 32));
            for (int i = 0; i < top20.Length; i++)
                Console.WriteLine($"{i + 1,-6} {top20[i].idx,-10} {top20[i].score,14:E6}");

            // Bar chart Top-20
            double[] barValues = top20.Select(x => x.score * 1e6).ToArray(); // scale for readability
            double[] barPos = Enumerable.Range(0, 20).Select(i => (double)i).ToArray();
            string[] barLabels = top20.Select(x => x.idx.ToString()).ToArray();

            var plt4 = new Plot();
            var bars = plt4.Add.Bars(barPos, barValues);
            bars.Color = Colors.CornflowerBlue;

            plt4.Axes.Bottom.SetTicks(barPos, barLabels);
            plt4.Axes.Bottom.Label.Text = "Node ID";
            plt4.Axes.Left.Label.Text = "PageRank × 10⁻⁶";
            plt4.Title("Top-20 pages by PageRank (d=0.85)");
            plt4.SavePng("plots/pr_top20.png", 900, 500);
            Console.WriteLine("Saved: plots/pr_top20.png");
        }

        private static List<double> CollectConvergencePath(
            CrawlGraph graph,
            Func<double[], double[], double> norm,
            double d,
            double epsilon = 1e-6)
        {
            var errors = new List<double>();

            double[] x = new double[graph.V];
            for (int v = 0; v < graph.V; v++) x[v] = 1.0 / graph.V;

            double[] xNew = new double[graph.V];

            while (true)
            {
                double danglingSum = 0;
                for (int v = 0; v < graph.V; v++)
                    if (graph.OutDegrees[v] == 0)
                        danglingSum += x[v];

                for (int v = 0; v < graph.V; v++)
                {
                    xNew[v] = (1.0 - d) / graph.V + d * danglingSum / graph.V;
                    foreach (var u in graph.AdjacencyReversed[v])
                        xNew[v] += d * x[u] / graph.OutDegrees[u];
                }

                double err = norm(xNew, x);
                errors.Add(err);
                Array.Copy(xNew, x, graph.V);

                if (err < epsilon) break;
            }

            return errors;
        }
    }
}