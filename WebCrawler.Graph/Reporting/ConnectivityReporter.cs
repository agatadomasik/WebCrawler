using WebCrawler.Graph.Analysis;

namespace WebCrawler.Graph.Reporting
{
    /// <summary>
    /// Prints articulation points and bridges (Hopcroft-Tarjan) for the undirected
    /// version of the crawl graph. These are the "weak spots" whose removal
    /// disconnects the network — directly relevant to the robustness section (Z9).
    /// </summary>
    public static class ConnectivityReporter
    {
        public static void Print(ConnectivityAnalysis result, int topToShow = 10)
        {
            Console.WriteLine("\n======== CONNECTIVITY (Hopcroft-Tarjan) ========");
            Console.WriteLine($"Articulation points: {result.ArticulationPoints.Count}");
            Console.WriteLine($"Bridges:             {result.Bridges.Count}");

            if (result.ArticulationPoints.Count > 0)
            {
                Console.WriteLine($"\nFirst {Math.Min(topToShow, result.ArticulationPoints.Count)} articulation points (node ID):");
                foreach (var v in result.ArticulationPoints.Take(topToShow))
                    Console.WriteLine($"  {v}");
            }

            if (result.Bridges.Count > 0)
            {
                Console.WriteLine($"\nFirst {Math.Min(topToShow, result.Bridges.Count)} bridges (u — v):");
                foreach (var (u, v) in result.Bridges.Take(topToShow))
                    Console.WriteLine($"  {u} — {v}");
            }
        }
    }
}
