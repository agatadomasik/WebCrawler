using WebCrawler.Graph.Results;

namespace WebCrawler.Graph.Reporting
{
    public static class RobustnessReporter
    {
        public static void Print(string scenarioLabel, IEnumerable<RobustnessAnalysisResult> results)
        {
            Console.WriteLine($"\n======== ROBUSTNESS – {scenarioLabel.ToUpperInvariant()} ========");
            Console.WriteLine(
                $"{"f",-6} | {"WCC",6} | {"SCC",6} | {"avg dist",10} | {"diam",6} | " +
                $"{"avg in",8} | {"max in",7} | {"avg out",8} | {"max out",8}");
            Console.WriteLine(new string('-', 90));
            foreach (var res in results)
            {
                Console.WriteLine(
                    $"{res.Fraction,-6:F2} | {res.LargestWCC,6} | {res.LargestSCC,6} | " +
                    $"{res.AvgDist,10:F3} | {res.Diameter,6} | " +
                    $"{res.AvgInDegree,8:F2} | {res.MaxInDegree,7} | " +
                    $"{res.AvgOutDegree,8:F2} | {res.MaxOutDegree,8}");
            }
        }
    }
}
