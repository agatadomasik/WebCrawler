using WebCrawler.Domain;
using WebCrawler.Graph.Charts;
using WebCrawler.Graph.Algorithms;
using WebCrawler.Graph.Reporting;
using WebCrawler.Graph.Results;
using WebCrawler.Graph.Analysis;
using WebCrawler.Graph.Building;

namespace WebCrawler.Graph
{
    /// <summary>
    /// Orchestrates the whole graph analysis pipeline:
    /// analyzer → DTO → reporter (console) + chart (PNG).
    /// Program.cs calls only the methods of this class — it knows nothing about ScottPlot.
    /// </summary>
    public sealed class GraphAnalysisService
    {
        private readonly CrawlGraph _graph;

        public GraphAnalysisService(CrawlGraph graph) => _graph = graph;

        public void RunBasicStatistics()
        {
            GraphReporter.PrintBasicStats(_graph);

            HistogramChart.Render("Out-degree distribution (linear)", _graph.OutDegrees.ToList(),
                xAxisLabel: "Degree", filename: "out-degree.png");
            HistogramChart.Render("In-degree distribution (linear)", _graph.InDegrees.ToList(),
                xAxisLabel: "Degree", filename: "in-degree.png");

            HistogramChart.RenderLogLog("Out-degree distribution (log-log)", _graph.OutDegrees.ToList(),
                xAxisLabel: "degree", filename: "out-degree_loglog.png");
            HistogramChart.RenderLogLog("In-degree distribution (log-log)", _graph.InDegrees.ToList(),
                xAxisLabel: "degree", filename: "in-degree_loglog.png");

            RunPowerLawFits("In-degree",  _graph.InDegrees,  "in");
            RunPowerLawFits("Out-degree", _graph.OutDegrees, "out");
        }

        public ComponentAnalysis RunSccAnalysis()
        {
            var scc = ComponentAnalyzer.AnalyzeScc(_graph);
            ComponentReporter.Print(scc);
            HistogramChart.Render("SCC size distribution", scc.Sizes,
                xAxisLabel: "Component size", filename: "SCC.png");
            return scc;
        }

        public ComponentAnalysis RunWccAnalysis()
        {
            var wcc = ComponentAnalyzer.AnalyzeWcc(_graph);
            ComponentReporter.Print(wcc);
            HistogramChart.Render("WCC size distribution", wcc.Sizes,
                xAxisLabel: "Component size", filename: "WCC.png");
            return wcc;
        }

        public BowTieResult RunBowTieAnalysis()
        {
            var result = BowTieAnalyzer.Analyze(_graph);
            BowTieReporter.Print(result, _graph.IndexToUrl);
            return result;
        }

        public CondensationGraph BuildCondensation(ComponentAnalysis scc)
        {
            var dag = CondensationBuilder.Build(_graph, scc.NodeToComponent);
            Console.WriteLine($"DAG nodes: {dag.V}");
            Console.WriteLine($"DAG edges: {dag.Adjacency.Sum(x => x.Value.Count)}");
            return dag;
        }

        public void RunBfsAnalysis()
        {
            var bfs = BFSExplorer.Explore(_graph);
            var (slope, intercept, r2) = FitDistanceHistogram(bfs.PairDistanceHistogram);
            BfsReporter.Print(bfs, _graph.V, slope, r2);
            HistogramChart.Render("Eccentricity distribution", bfs.Eccentricity,
                xAxisLabel: "Eccentricity", filename: "Eccentricity.png");
            BfsCharts.RenderPairDistanceHistogram(bfs.PairDistanceHistogram, slope, intercept,
                "pair_distance_histogram.png");
        }

        /// <summary>
        /// OLS regression of log₁₀(count) against distance d on the tail of the pair distance histogram.
        /// Returns slope, intercept (base-10) and R².
        /// </summary>
        private static (double slope, double intercept, double r2) FitDistanceHistogram(long[] histogram)
        {
            // Use indices d ≥ 1 where count > 0.
            var points = Enumerable.Range(1, System.Math.Max(histogram.Length - 1, 0))
                .Where(d => histogram[d] > 0)
                .Select(d => (x: (double)d, y: System.Math.Log10(histogram[d])))
                .ToArray();

            if (points.Length < 2) return (double.NaN, double.NaN, double.NaN);

            int n = points.Length;
            double sx = points.Sum(p => p.x);
            double sy = points.Sum(p => p.y);
            double sxy = points.Sum(p => p.x * p.y);
            double sx2 = points.Sum(p => p.x * p.x);

            double denom = n * sx2 - sx * sx;
            if (denom == 0) return (double.NaN, double.NaN, double.NaN);

            double slope = (n * sxy - sx * sy) / denom;
            double intercept = (sy - slope * sx) / n;

            double yMean = sy / n;
            double ssTot = points.Sum(p => (p.y - yMean) * (p.y - yMean));
            double ssRes = points.Sum(p => { double yp = slope * p.x + intercept; return (p.y - yp) * (p.y - yp); });
            double r2 = ssTot == 0 ? 1 : 1 - ssRes / ssTot;

            return (slope, intercept, r2);
        }

        public void RunClusteringAnalysis()
        {
            var clustering = ClusteringAnalyzer.Analyze(_graph);
            ClusteringReporter.Print(clustering);
            ClusteringCharts.RenderLocalHistogram(clustering, "LocalClusteringCoefficients.png");
            ClusteringCharts.RenderLogLog(clustering, "ck_loglog.png");
        }

        public void RunPageRankAnalysis(int topCount = 20)
        {
            // 1) Iterations-to-converge vs d (for each norm)
            var iterSeries = VectorNorms.All.Select(norm =>
            {
                var ds = PageRanker.DampingFactors;
                var iters = ds.Select(d => PageRanker.Compute(_graph, norm, d).Iterations).ToArray();
                return (norm.Name, Dampings: ds, Iterations: iters);
            }).ToList();

            PageRankCharts.RenderIterationsVsDamping(iterSeries, "pr_iters_vs_d.png");

            // 2) Reference run: d=0.85, L1
            var reference = PageRanker.Compute(_graph, VectorNorms.L1, 0.85);
            PageRankReporter.PrintConvergence(reference);

            // 3) PageRank distribution in log-log
            var (slope, intercept) = FitPageRankLogLog(reference);
            PageRankReporter.PrintDistributionFit(slope);
            PageRankCharts.RenderDistributionLogLog(reference, slope, intercept, "pr_loglog.png");

            // 4) Convergence comparison for the different norms (d=0.85)
            var perNorm = VectorNorms.All
                .Select(n => PageRanker.Compute(_graph, n, 0.85))
                .ToList();
            PageRankCharts.RenderNormConvergence(perNorm, "pr_norm_convergence.png");

            // 5) Top-N
            var top = reference.Scores
                .Select((score, idx) => (Node: idx, Score: score))
                .OrderByDescending(x => x.Score)
                .Take(topCount)
                .ToList();

            PageRankReporter.PrintTopN(top, _graph.IndexToUrl, reference.DampingFactor, reference.NormName);
            PageRankCharts.RenderTopN(top, _graph.IndexToUrl, reference.DampingFactor, "pr_top20.png");
        }

        public void RunConnectivityAnalysis()
        {
            var result = ArticulationFinder.Find(_graph);
            ConnectivityReporter.Print(result);
        }

        public void RunRobustnessAnalysis()
        {
            var randomResults = RobustnessAnalyzer.SimulateRemoval(_graph, RemovalStrategy.Random);
            var attackResults = RobustnessAnalyzer.SimulateRemoval(_graph, RemovalStrategy.TargetedAttack);

            RobustnessReporter.Print("Random removal", randomResults);
            RobustnessReporter.Print("Targeted attack", attackResults);

            RobustnessChart.Render(randomResults, _graph.V, "random removal", "robustness_wcc_scc_random.png");
            RobustnessChart.Render(attackResults, _graph.V, "targeted attack", "robustness_wcc_scc_attack.png");

            RobustnessChart.RenderDegreeEvolution(randomResults, "random removal", "robustness_degree_random.png");
            RobustnessChart.RenderDegreeEvolution(attackResults, "targeted attack", "robustness_degree_attack.png");
        }

        private static void RunPowerLawFits(string label, IReadOnlyList<int> degrees, string filePrefix)
        {
            var ols = PowerLawFitter.FitOls(degrees);
            GraphReporter.PrintPowerLawOls(label, ols);
            if (ols is not null)
                PowerLawChart.RenderOls(ols, $"{label} – power law OLS (CCDF)", $"{filePrefix}_ols.png");

            var mle = PowerLawFitter.FitMle(degrees);
            GraphReporter.PrintPowerLawMle(label, mle);
            if (mle is not null)
                PowerLawChart.RenderMle(mle, $"{label} – power law MLE (CDF)", $"{filePrefix}_mle.png");
        }

        private static (double slope, double intercept) FitPageRankLogLog(PageRankAnalysis pr)
        {
            double[] sorted = pr.Scores.Where(v => v > 0).OrderByDescending(v => v).ToArray();
            if (sorted.Length < 2) return (double.NaN, double.NaN);

            double[] logRank = Enumerable.Range(1, sorted.Length).Select(r => System.Math.Log10(r)).ToArray();
            double[] logPr = sorted.Select(v => System.Math.Log10(v)).ToArray();

            int n = logRank.Length;
            double sx = logRank.Sum();
            double sy = logPr.Sum();
            double sxy = logRank.Zip(logPr, (x, y) => x * y).Sum();
            double sx2 = logRank.Select(x => x * x).Sum();

            double denom = n * sx2 - sx * sx;
            if (denom == 0) return (double.NaN, double.NaN);

            double slope = (n * sxy - sx * sy) / denom;
            double intercept = (sy - slope * sx) / n;
            return (slope, intercept);
        }
    }
}
