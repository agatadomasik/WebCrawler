using System;
using System.Collections.Generic;
using System.Linq;
using WebCrawler.Domain;

namespace WebCrawler.Graph
{
    public class BowTieResult
    {
        public HashSet<int> CORE { get; set; }
        public HashSet<int> IN { get; set; }
        public HashSet<int> OUT { get; set; }
        public HashSet<int> TENDRILS { get; set; }
    }

    public class BowTieAnalyzer
    {
        private readonly SCCFinder _sccFinder = new SCCFinder();

        public BowTieResult Analyze(CrawlGraph graph)
        {
            var comp = _sccFinder.FindKosaraju(graph);

            var counts = new Dictionary<int, int>();
            foreach (var c in comp.Values)
            {
                if (!counts.ContainsKey(c))
                    counts[c] = 0;
                counts[c]++;
            }

            int mainSccId = counts
                .OrderByDescending(x => x.Value)
                .First().Key;

            var core = comp
                .Where(kv => kv.Value == mainSccId)
                .Select(kv => kv.Key)
                .ToHashSet();

            // 3. OUT (BFS normalny)
            var reachableFromCore = BFS(graph, core, reversed: false);
            var OUT = reachableFromCore.Except(core).ToHashSet();

            // 4. IN (BFS po odwróconym grafie)
            var canReachCore = BFS(graph, core, reversed: true);
            var IN = canReachCore.Except(core).ToHashSet();

            // 5. TENDRILS (reszta)
            var all = Enumerable.Range(0, graph.V).ToHashSet();

            var TENDRILS = all
                .Except(core)
                .Except(IN)
                .Except(OUT)
                .ToHashSet();

            return new BowTieResult
            {
                CORE = core,
                IN = IN,
                OUT = OUT,
                TENDRILS = TENDRILS
            };
        }

        private HashSet<int> BFS(CrawlGraph graph, IEnumerable<int> start, bool reversed)
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
                var u = queue.Dequeue();

                var neighbors = reversed
                    ? graph.AdjacencyReversed[u]
                    : graph.Adjacency[u];

                foreach (var v in neighbors)
                {
                    if (!visited.Contains(v))
                    {
                        visited.Add(v);
                        queue.Enqueue(v);
                    }
                }
            }

            return visited;
        }
    }
}