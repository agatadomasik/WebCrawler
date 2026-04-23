using WebCrawler.Domain;

namespace WebCrawler.Graph.Analysis
{
    /// <summary>
    /// Iterative Hopcroft-Tarjan implementation that finds articulation points
    /// (cut vertices) and bridges (cut edges) on the undirected version of the
    /// directed crawl graph (adjacency ∪ adjacencyReversed).
    ///
    /// Iterative DFS is used instead of recursion to avoid StackOverflowException
    /// on graphs with long paths (3000+ vertices).
    /// </summary>
    public sealed record ConnectivityAnalysis(
        HashSet<int> ArticulationPoints,
        List<(int U, int V)> Bridges);

    public static class ArticulationFinder
    {
        public static ConnectivityAnalysis Find(CrawlGraph graph)
        {
            int n = graph.V;

            // Build an undirected neighbour list (deduplicated, no self-loops).
            var neighbours = new List<int>[n];
            for (int v = 0; v < n; v++)
            {
                var set = new HashSet<int>(graph.Adjacency[v]);
                foreach (var u in graph.AdjacencyReversed[v]) set.Add(u);
                set.Remove(v);
                neighbours[v] = set.ToList();
            }

            var disc   = new int[n];
            var low    = new int[n];
            var parent = new int[n];
            for (int i = 0; i < n; i++) { disc[i] = -1; parent[i] = -1; }

            var articulations = new HashSet<int>();
            var bridges = new List<(int U, int V)>();
            int timer = 0;

            for (int start = 0; start < n; start++)
            {
                if (disc[start] != -1) continue;

                int rootChildren = 0;
                disc[start] = low[start] = timer++;

                // Stack of (node, indexOfNextNeighbourToVisit)
                var stack = new Stack<(int Node, int Iter)>();
                stack.Push((start, 0));

                while (stack.Count > 0)
                {
                    var (u, i) = stack.Pop();

                    if (i < neighbours[u].Count)
                    {
                        int v = neighbours[u][i];
                        // Resume u's iteration after we are done with v.
                        stack.Push((u, i + 1));

                        if (disc[v] == -1)
                        {
                            parent[v] = u;
                            disc[v] = low[v] = timer++;
                            if (u == start) rootChildren++;
                            stack.Push((v, 0));
                        }
                        else if (v != parent[u])
                        {
                            // Back edge: update low using disc, not low.
                            if (disc[v] < low[u]) low[u] = disc[v];
                        }
                    }
                    else
                    {
                        // Post-order processing for u.
                        int p = parent[u];
                        if (p != -1)
                        {
                            if (low[u] < low[p]) low[p] = low[u];

                            // Non-root articulation rule (root handled below).
                            if (low[u] >= disc[p] && p != start)
                                articulations.Add(p);

                            // Bridge rule.
                            if (low[u] > disc[p])
                                bridges.Add((p, u));
                        }
                    }
                }

                if (rootChildren > 1) articulations.Add(start);
            }

            return new ConnectivityAnalysis(articulations, bridges);
        }
    }
}
