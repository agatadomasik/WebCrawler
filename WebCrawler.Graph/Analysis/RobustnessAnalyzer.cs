using WebCrawler.Domain;
using WebCrawler.Graph.Results;

namespace WebCrawler.Graph.Analysis
{
    public enum RemovalStrategy
    {
        Random,
        TargetedAttack
    }

    /// <summary>
    /// Node-removal simulation — a pure analysis class.
    /// Returns a list of <see cref="RobustnessAnalysisResult"/> (no plots, no prints).
    /// Also exposes betweenness centrality computation via Brandes' algorithm.
    /// </summary>
    public static class RobustnessAnalyzer
    {
        public static readonly double[] DefaultFractions = { 0.01, 0.02, 0.05, 0.10, 0.20, 0.30, 0.50 };

        public static List<RobustnessAnalysisResult> SimulateRemoval(
            CrawlGraph graph,
            RemovalStrategy strategy,
            IReadOnlyList<double>? fractions = null,
            int? randomSeed = null)
        {
            fractions ??= DefaultFractions;
            var results = new List<RobustnessAnalysisResult>();

            var active = new HashSet<int>(Enumerable.Range(0, graph.V));
            var rnd = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
            int removedCount = 0;

            foreach (double fraction in fractions)
            {
                int toRemoveCount = (int)(graph.V * fraction) - removedCount;

                List<int> toRemove = strategy == RemovalStrategy.TargetedAttack
                    ? active.OrderByDescending(v => graph.InDegrees[v]).Take(toRemoveCount).ToList()
                    : SampleWithoutReplacement(active, toRemoveCount, rnd);

                foreach (int v in toRemove)
                {
                    active.Remove(v);
                    removedCount++;
                }

                var wcc = ComponentAnalyzer.AnalyzeWcc(graph, active);
                var scc = ComponentAnalyzer.AnalyzeScc(graph, active);

                var largestWccNodes = new HashSet<int>(wcc.LargestComponentNodes);
                var bfs = BFSExplorer.Explore(graph, largestWccNodes);

                var (inDeg, outDeg) = ComputeActiveDegrees(graph, active);

                results.Add(new RobustnessAnalysisResult(
                    fraction,
                    wcc.LargestSize,
                    scc.LargestSize,
                    bfs.GlobalAverageDistance,
                    bfs.Diameter,
                    inDeg,
                    outDeg));
            }

            return results;
        }

        /// <summary>
        /// Fisher-Yates partial shuffle: samples exactly <paramref name="count"/> distinct elements
        /// from <paramref name="source"/> without replacement in O(count) time.
        /// </summary>
        private static List<int> SampleWithoutReplacement(IReadOnlyCollection<int> source, int count, Random rnd)
        {
            int[] array = source.ToArray();
            int take = System.Math.Min(count, array.Length);
            for (int i = 0; i < take; i++)
            {
                int j = rnd.Next(i, array.Length);
                (array[i], array[j]) = (array[j], array[i]);
            }
            return array.Take(take).ToList();
        }

        /// <summary>
        /// Re-computes in/out degrees restricted to the set of still-active vertices,
        /// so that edges to removed vertices are excluded from the distribution.
        /// </summary>
        private static (int[] inDeg, int[] outDeg) ComputeActiveDegrees(CrawlGraph graph, HashSet<int> active)
        {
            var activeList = active.ToArray();
            var outDeg = new int[activeList.Length];
            var inDeg  = new int[activeList.Length];

            for (int i = 0; i < activeList.Length; i++)
            {
                int v = activeList[i];
                foreach (var u in graph.Adjacency[v])          if (active.Contains(u)) outDeg[i]++;
                foreach (var u in graph.AdjacencyReversed[v])  if (active.Contains(u)) inDeg[i]++;
            }

            return (inDeg, outDeg);
        }

        /// <summary>Betweenness centrality computed with Brandes' algorithm.</summary>
        public static double[] ComputeBetweennessBrandes(CrawlGraph graph)
        {
            var cb = new double[graph.V];

            for (int s = 0; s < graph.V; s++)
            {
                var stack = new Stack<int>();

                var sigma = new double[graph.V];
                sigma[s] = 1;

                var d = Enumerable.Repeat(-1, graph.V).ToArray();
                d[s] = 0;

                var queue = new Queue<int>();
                queue.Enqueue(s);

                var pred = new List<int>[graph.V];
                for (int i = 0; i < graph.V; i++)
                    pred[i] = new List<int>();

                while (queue.Count != 0)
                {
                    int v = queue.Dequeue();
                    stack.Push(v);

                    foreach (var w in graph.Adjacency[v])
                    {
                        if (d[w] < 0)
                        {
                            queue.Enqueue(w);
                            d[w] = d[v] + 1;
                        }
                        if (d[w] == d[v] + 1)
                        {
                            sigma[w] += sigma[v];
                            pred[w].Add(v);
                        }
                    }
                }

                var delta = new double[graph.V];

                while (stack.Count != 0)
                {
                    int w = stack.Pop();
                    foreach (var v in pred[w])
                        delta[v] += (sigma[v] / sigma[w]) * (1 + delta[w]);

                    if (w != s)
                        cb[w] += delta[w];
                }
            }

            return cb;
        }
    }
}
