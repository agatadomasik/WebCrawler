using WebCrawler.Graph.Analysis;

namespace WebCrawler.Graph.Reporting
{
    public static class BowTieReporter
    {
        public static void Print(BowTieResult result, IReadOnlyList<string> urls)
        {
            Console.WriteLine("\n======== BOW-TIE STRUCTURE ========");
            Console.WriteLine($"CORE: {result.CORE.Count}");
            Console.WriteLine($"IN: {result.IN.Count}");
            Console.WriteLine($"OUT: {result.OUT.Count}");
            Console.WriteLine($"TENDRILS: {result.TENDRILS.Count}");

            Console.WriteLine("\n-- OUT nodes --");
            foreach (var node in result.OUT.OrderBy(n => n))
                Console.WriteLine($"  [{node}]  {urls[node]}");
        }
    }
}
