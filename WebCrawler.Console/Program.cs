using System.Diagnostics;
using WebCrawler.Crawler;
using WebCrawler.Domain;
using WebCrawler.Graph;
using WebCrawler.Robots;
using static SkiaSharp.HarfBuzz.SKShaper;
using static WebCrawler.Graph.RobustnessAnalyzer;

public class Program
{
    public static async Task Main()
    {
        const string targetUrl = "https://warwick.ac.uk";

        //await RunPerformanceTests(targetUrl);

        Console.WriteLine("\n\nCrawling for graph construction...");
        var graphHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var graphEngine = new CrawlerEngine(
            new CrawlerOptions { ThreadCount = 16, MaxPages = 3000, PolitenessDelay = TimeSpan.FromMilliseconds(500) },
            new RobotsService(new RobotsFetcher(graphHttpClient)),
            graphHttpClient
        );

        var graphResults = await graphEngine.CrawlAsync(targetUrl);

        var graph = GraphBuilder.Build(graphResults);

        GraphStats.Print(graph);

        var scc = SCCFinder.FindKosaraju(graph);
        var wcc = WCCFinder.FindWCC(graph);
        GraphStats.PrintComponentStats("SCC", scc);
        GraphStats.PrintComponentStats("WCC", wcc);

        var analyzer = new BowTieAnalyzer();
        var result = analyzer.Analyze(graph);

        Console.WriteLine($"CORE: {result.CORE.Count}");
        Console.WriteLine($"IN: {result.IN.Count}");
        Console.WriteLine($"OUT: {result.OUT.Count}");
        Console.WriteLine($"TENDRILS: {result.TENDRILS.Count}");

        var builder = new CondensationBuilder();
        var dag = builder.Build(graph, scc);

        Console.WriteLine($"DAG nodes: {dag.V}");
        Console.WriteLine($"DAG edges: {dag.Adjacency.Sum(x => x.Value.Count)}");


        //GraphStats.PrintBFSStats(graph);
        //GraphStats.PrintClusteringStats(graph);
        //GraphStats.PrintPageRankStats(graph);

        var randomRemovalResults = RobustnessAnalyzer.SimulateRemoval(graph, false);
        var attackRemovalResults = RobustnessAnalyzer.SimulateRemoval(graph, true);

        Console.WriteLine("\n======== Random Removal Results ========");
        GraphStats.PrintRubustnessStats(randomRemovalResults);

        Console.WriteLine("\n======== Attack Results ========");
        GraphStats.PrintRubustnessStats(attackRemovalResults);

        GraphStats.PlotRobustnessStats(randomRemovalResults, graph.V, "random");
        GraphStats.PlotRobustnessStats(attackRemovalResults, graph.V, "attack");
    }

    private static async Task RunPerformanceTests(string targetUrl)
    {
        var threadCounts = new[] { 1, 2, 4, 8, 16, 32 };
        var results = new List<(int threads, double throughput, double seconds, int pages)>();

        double baselineThroughput = 0;

        foreach (var threadCount in threadCounts)
        {
            Console.WriteLine($"\n{'=',40}");
            Console.WriteLine($"Testing with {threadCount} thread(s)");
            Console.WriteLine(new string('=', 40));

            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var fetcher = new RobotsFetcher(httpClient);
            var robotsService = new RobotsService(fetcher);

            var options = new CrawlerOptions
            {
                ThreadCount = threadCount,
                MaxPages = 300, // for test purposes
                PolitenessDelay = TimeSpan.FromMilliseconds(500),
            };

            var engine = new CrawlerEngine(options, robotsService, httpClient);

            var sw = Stopwatch.StartNew();
            var crawled = await engine.CrawlAsync(targetUrl);
            sw.Stop();

            double seconds = sw.Elapsed.TotalSeconds;
            double throughput = crawled.Count / seconds;

            if (threadCount == 1)
                baselineThroughput = throughput;

            double speedup = baselineThroughput > 0 ? throughput / baselineThroughput : 1.0;

            results.Add((threadCount, throughput, seconds, crawled.Count));

            Console.WriteLine($"Result: {crawled.Count} pages in {seconds:F1}s");
            Console.WriteLine($"Throughput: {throughput:F2} pages/s | Speedup: {speedup:F2}x");
        }

        Console.WriteLine("\n\n=== PERFORMANCE SUMMARY ===");
        Console.WriteLine($"{"Threads",-10} {"Pages",-8} {"Time(s)",-10} {"Throughput",-15} {"Speedup",-10}");
        Console.WriteLine(new string('-', 55));

        double baseline = results[0].throughput;
        foreach (var (threads, throughput, seconds, pages) in results)
        {
            double speedup = throughput / baseline;
            Console.WriteLine($"{threads,-10} {pages,-8} {seconds,-10:F1} {throughput,-15:F2} {speedup,-10:F2}");
        }
    }
}