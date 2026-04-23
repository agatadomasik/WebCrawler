using WebCrawler.Domain;

namespace WebCrawler.Graph.Analysis
{
    public sealed record BowTieResult(
        HashSet<int> CORE,
        HashSet<int> IN,
        HashSet<int> OUT,
        HashSet<int> TENDRILS);

    /// <summary>
    /// Bow-tie structure analysis (CORE / IN / OUT / TENDRILS) based on SCC + BFS.
    /// </summary>
    public static class BowTieAnalyzer
    {
        public static BowTieResult Analyze(CrawlGraph graph)
        {
            var scc = ComponentAnalyzer.AnalyzeScc(graph);
            var core = new HashSet<int>(scc.LargestComponentNodes);

            var reachableFromCore = TraverseBfs(graph, core, reversed: false);
            var OUT = reachableFromCore.Except(core).ToHashSet();

            var canReachCore = TraverseBfs(graph, core, reversed: true);
            var IN = canReachCore.Except(core).ToHashSet();

            var all = Enumerable.Range(0, graph.V).ToHashSet();
            var tendrils = all.Except(core).Except(IN).Except(OUT).ToHashSet();

            return new BowTieResult(core, IN, OUT, tendrils);
        }

        private static HashSet<int> TraverseBfs(CrawlGraph graph, IEnumerable<int> start, bool reversed)
        {
            var visited = new HashSet<int>();
            var queue = new Queue<int>();

            foreach (var s in start)
            {
                visited.Add(s);
                queue.Enqueue(s);
            }

            while (queue.Count > 0)
            {
                int u = queue.Dequeue();
                var neighbors = reversed ? graph.AdjacencyReversed[u] : graph.Adjacency[u];
                foreach (var v in neighbors)
                {
                    if (visited.Add(v))
                        queue.Enqueue(v);
                }
            }

            return visited;
        }
    }
}
