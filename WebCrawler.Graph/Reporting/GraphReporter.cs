using WebCrawler.Domain;
using WebCrawler.Graph.Results;

namespace WebCrawler.Graph.Reporting
{
    /// <summary>
    /// Reports basic graph statistics (|V|, |E|, average degrees, density)
    /// and power-law fit summaries.
    /// </summary>
    public static class GraphReporter
    {
        public static void PrintBasicStats(CrawlGraph graph)
        {
            double avgOutDegree = (double)graph.E / graph.V;
            double avgInDegree = avgOutDegree; // for a directed graph both averages are equal
            double density = (double)graph.E / ((double)graph.V * (graph.V - 1));

            Console.WriteLine("\n======== GRAPH STATS ========");
            Console.WriteLine($"|V| = {graph.V}");
            Console.WriteLine($"|E| = {graph.E}");
            Console.WriteLine($"In-degrees sum = {graph.InDegrees.Sum()}");
            Console.WriteLine($"Out-degrees sum = {graph.OutDegrees.Sum()}");
            int maxInIndex = graph.InDegrees
                .Select((value, index) => new { value, index })
                .MaxBy(x => x.value)
                .index;
            int maxOutIndex = graph.OutDegrees
                .Select((value, index) => new { value, index })
                .MaxBy(x => x.value)
                .index;
            Console.WriteLine($"Max in-degree = {graph.InDegrees.Max()} for page {graph.IndexToUrl[maxInIndex]}");
            Console.WriteLine($"Max out-degree = {graph.OutDegrees.Max()} for page {graph.IndexToUrl[maxOutIndex]}");
            Console.WriteLine("Dangling nodes: ");
            for (int v = 0; v < graph.V; v++)
                if (graph.OutDegrees[v] == 0)
                    Console.WriteLine($"{graph.IndexToUrl[v]}");
            Console.WriteLine($"Avg out-degree: {avgOutDegree:F2}");
            Console.WriteLine($"Avg in-degree:  {avgInDegree:F2}");
            Console.WriteLine($"Density:        {density:F6}");
        }

        public static void PrintPowerLawOls(string label, OlsPowerLawFit? fit)
        {
            if (fit is null)
            {
                Console.WriteLine($"{label} OLS: cannot fit (not enough distinct degrees).");
                return;
            }
            Console.WriteLine($"{label} OLS: γ={fit.Gamma:F3}, R²={fit.R2:F4}");
        }

        public static void PrintPowerLawMle(string label, MlePowerLawFit? fit)
        {
            if (fit is null)
            {
                Console.WriteLine($"{label} MLE: cannot fit (no tail ≥ 50 samples).");
                return;
            }
            Console.WriteLine($"{label} MLE: γ={fit.Gamma:F3}, x_min={fit.XMin}, KS={fit.Ks:F4}");
        }
    }
}
