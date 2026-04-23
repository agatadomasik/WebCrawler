using WebCrawler.Domain;
using WebCrawler.Graph.Algorithms;
using WebCrawler.Graph.Results;

namespace WebCrawler.Graph.Analysis
{
    /// <summary>
    /// Pure PageRank implementation. Returns a <see cref="PageRankAnalysis"/>
    /// containing the ranks, the iteration count and the full convergence path.
    /// Does not print to the console and does not draw charts.
    /// </summary>
    public static class PageRanker
    {
        public static readonly double[] DampingFactors = { 1.00, 0.99, 0.95, 0.90, 0.85, 0.70, 0.50 };

        public static PageRankAnalysis Compute(
            CrawlGraph graph,
            (string Name, VectorNorms.NormFunc Fn) norm,
            double d = 0.85,
            double epsilon = 1e-6)
        {
            var errors = new List<double>();

            double[] x = new double[graph.V];
            for (int v = 0; v < graph.V; v++) x[v] = 1.0 / graph.V;

            double[] xNew = new double[graph.V];
            int iterations = 0;

            while (true)
            {
                iterations++;

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

                double err = norm.Fn(xNew, x);
                errors.Add(err);
                Array.Copy(xNew, x, graph.V);

                if (err < epsilon) break;
            }

            return new PageRankAnalysis(x, iterations, d, norm.Name, errors);
        }
    }
}
