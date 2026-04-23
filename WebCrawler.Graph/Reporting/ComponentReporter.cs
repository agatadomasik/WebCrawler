using WebCrawler.Graph.Results;

namespace WebCrawler.Graph.Reporting
{
    public static class ComponentReporter
    {
        public static void Print(ComponentAnalysis analysis)
        {
            Console.WriteLine($"\n======== {analysis.Label} ========");
            Console.WriteLine($"Number of components: {analysis.Count}");
            Console.WriteLine($"The largest: {analysis.LargestSize} vertices");
        }
    }
}
