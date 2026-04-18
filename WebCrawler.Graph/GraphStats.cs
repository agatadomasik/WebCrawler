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
    }
}