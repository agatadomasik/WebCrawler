using WebCrawler.Graph.Results;

namespace WebCrawler.Graph.Reporting
{
    public static class PageRankReporter
    {
        public static void PrintConvergence(PageRankAnalysis analysis)
        {
            Console.WriteLine($"\nPageRank (d={analysis.DampingFactor}, {analysis.NormName}): converged in {analysis.Iterations} iterations");
        }

        public static void PrintDistributionFit(double slope)
        {
            Console.WriteLine($"PR distribution log-log slope: {slope:F4}  (expected ≈ -1 for power law)");
        }

        public static void PrintTopN(
            IReadOnlyList<(int Node, double Score)> top,
            IReadOnlyList<string> urls,
            double damping,
            string normName)
        {
            Console.WriteLine($"\n======== TOP-{top.Count} PAGES (PageRank, d={damping}, {normName}) ========");
            Console.WriteLine($"{"Rank",-6} {"PageRank",14}  URL");
            Console.WriteLine(new string('-', 80));
            for (int i = 0; i < top.Count; i++)
                Console.WriteLine($"{i + 1,-6} {top[i].Score,14:E6}  {urls[top[i].Node]}");
        }
    }
}
