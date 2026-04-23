using WebCrawler.Graph.Results;

namespace WebCrawler.Graph.Reporting
{
    public static class BfsReporter
    {
        public static void Print(BfsAnalysis bfs, int nodeCount, double regressionSlope, double regressionR2)
        {
            Console.WriteLine("\n======== BFS STATS ========");
            Console.WriteLine($"Average distance: {bfs.GlobalAverageDistance:F4}");
            Console.WriteLine($"Diameter: {bfs.Diameter}");
            Console.WriteLine($"Radius: {bfs.Radius}");
            Console.WriteLine($"Reachable ordered pairs: {bfs.ReachablePairs:N0}");

            // Small-world commentary: compare avg distance to log(N)
            double logN = System.Math.Log(System.Math.Max(nodeCount, 2));
            double log2N = System.Math.Log2(System.Math.Max(nodeCount, 2));
            Console.WriteLine($"\nSmall-world check:");
            Console.WriteLine($"  avg distance / ln(N)   = {bfs.GlobalAverageDistance / logN:F2}");
            Console.WriteLine($"  avg distance / log₂(N) = {bfs.GlobalAverageDistance / log2N:F2}");
            Console.WriteLine($"  (ratios near 1 → small-world; typical web graphs fall in this range)");

            Console.WriteLine($"\nRegression on pair distance histogram (log₁₀(count) vs d):");
            Console.WriteLine($"  slope = {regressionSlope:F4}   R² = {regressionR2:F4}");
            Console.WriteLine($"  negative slope ⇒ exponential-like decay; steeper ⇒ more concentrated distances");

            Console.WriteLine("\nAverage distance per node (first 10):");
            for (int i = 0; i < Math.Min(10, bfs.AvgDistPerNode.Length); i++)
                Console.WriteLine($"  {i}: {bfs.AvgDistPerNode[i]:F2}");

            Console.WriteLine("\nEccentricity per node (first 10):");
            for (int i = 0; i < Math.Min(10, bfs.Eccentricity.Length); i++)
                Console.WriteLine($"  {i}: {bfs.Eccentricity[i]}");
        }
    }
}
