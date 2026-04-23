using WebCrawler.Graph.Results;

namespace WebCrawler.Graph.Reporting
{
    public static class ClusteringReporter
    {
        public static void Print(ClusteringAnalysis analysis)
        {
            Console.WriteLine("\n======== CLUSTERING STATS ========");
            Console.WriteLine($"Global clustering coefficient: {analysis.Global:F4}");
            Console.WriteLine($"Log-log slope (exponent): {analysis.LogLogSlope:F4}  (expected: -1)");
            Console.WriteLine($"Log-log coefficient:      {System.Math.Pow(10, analysis.LogLogIntercept):F4}");
        }
    }
}
