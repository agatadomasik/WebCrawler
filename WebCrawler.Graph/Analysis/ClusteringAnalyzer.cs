using WebCrawler.Domain;
using WebCrawler.Graph.Results;

namespace WebCrawler.Graph.Analysis
{
    /// <summary>
    /// Computes clustering coefficients (local + global) and the log-log regression of C(k) vs k.
    /// Returns a <see cref="ClusteringAnalysis"/>; does not print or plot.
    /// </summary>
    public static class ClusteringAnalyzer
    {
        public static ClusteringAnalysis Analyze(CrawlGraph graph)
        {
            double[] local = ComputeLocalCoefficients(graph);
            double global = ComputeGlobalCoefficient(graph);

            var avgByDegree = graph.OutDegrees
                .Zip(local, (deg, c) => (deg, c))
                .Where(x => x.c != -1)
                .GroupBy(x => x.deg)
                .ToDictionary(g => g.Key, g => g.Average(x => x.c));

            var (slope, intercept) = FitLogLog(avgByDegree);

            return new ClusteringAnalysis(local, global, avgByDegree, slope, intercept);
        }

        public static double[] ComputeLocalCoefficients(CrawlGraph graph)
        {
            double[] cv = new double[graph.V];
            for (int v = 0; v < graph.V; v++)
                cv[v] = ComputeLocalCoefficient(v, graph);
            return cv;
        }

        public static double ComputeLocalCoefficient(int v, CrawlGraph graph)
        {
            int degree = graph.Adjacency[v].Count;
            if (degree < 2) return -1;

            int connections = 0;
            foreach (var n in graph.Adjacency[v])
                foreach (var nn in graph.Adjacency[n])
                    if (graph.Adjacency[v].Contains(nn)) connections++;

            // denominator without /2 — each edge is counted twice
            return (double)connections / (degree * (degree - 1));
        }

        public static double ComputeGlobalCoefficient(CrawlGraph graph)
        {
            long closedTriplets = 0;
            long allTriplets = 0;

            for (int v = 0; v < graph.Adjacency.Count; v++)
            {
                int degree = graph.Adjacency[v].Count;
                if (degree < 2) continue;

                allTriplets += (long)degree * (degree - 1) / 2;

                foreach (var n in graph.Adjacency[v])
                    foreach (var m in graph.Adjacency[v])
                        if (n < m && graph.Adjacency[n].Contains(m))
                            closedTriplets++;
            }

            return allTriplets == 0 ? 0 : (double)closedTriplets / allTriplets;
        }

        private static (double slope, double intercept) FitLogLog(IReadOnlyDictionary<int, double> avgByDegree)
        {
            var points = avgByDegree
                .Where(x => x.Key > 0 && x.Value > 0)
                .OrderBy(x => x.Key)
                .ToArray();

            if (points.Length < 2) return (double.NaN, double.NaN);

            double[] logK = points.Select(x => System.Math.Log10(x.Key)).ToArray();
            double[] logC = points.Select(x => System.Math.Log10(x.Value)).ToArray();

            int n = logK.Length;
            double sumX = logK.Sum();
            double sumY = logC.Sum();
            double sumXY = logK.Zip(logC, (x, y) => x * y).Sum();
            double sumX2 = logK.Select(x => x * x).Sum();

            double denom = n * sumX2 - sumX * sumX;
            if (denom == 0) return (double.NaN, double.NaN);

            double slope = (n * sumXY - sumX * sumY) / denom;
            double intercept = (sumY - slope * sumX) / n;
            return (slope, intercept);
        }
    }
}
