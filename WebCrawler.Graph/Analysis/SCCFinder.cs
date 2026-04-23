using WebCrawler.Domain;

namespace WebCrawler.Graph.Analysis
{
    /// <summary>
    /// Finds strongly connected components (Kosaraju's algorithm).
    /// Returns the raw node→SCC id mapping; wrapping into <see cref="Results.ComponentAnalysis"/>
    /// is done by <see cref="ComponentAnalyzer"/>.
    /// </summary>
    public static class SCCFinder
    {
        public static Dictionary<int, int> FindKosaraju(CrawlGraph graph, HashSet<int>? active = null)
        {
            active ??= new HashSet<int>(Enumerable.Range(0, graph.V));
            var comp = new Dictionary<int, int>();

            var order = new List<int>();
            var visited = new HashSet<int>();

            for (int v = 0; v < graph.V; v++)
            {
                if (active.Contains(v) && !visited.Contains(v))
                    Dfs1(graph, v, visited, order, active);
            }

            visited = new HashSet<int>();
            int sccId = 0;
            order.Reverse();
            foreach (var v in order)
            {
                if (active.Contains(v) && !visited.Contains(v))
                {
                    sccId++;
                    Dfs2(graph, v, visited, sccId, comp, active);
                }
            }

            return comp;
        }

        private static void Dfs1(CrawlGraph g, int u, HashSet<int> visited, List<int> order, HashSet<int> active)
        {
            visited.Add(u);
            foreach (var w in g.Adjacency[u])
                if (active.Contains(w) && !visited.Contains(w))
                    Dfs1(g, w, visited, order, active);
            order.Add(u);
        }

        private static void Dfs2(CrawlGraph g, int u, HashSet<int> visited, int sccId, Dictionary<int, int> comp, HashSet<int> active)
        {
            visited.Add(u);
            comp[u] = sccId;
            foreach (var w in g.AdjacencyReversed[u])
                if (active.Contains(w) && !visited.Contains(w))
                    Dfs2(g, w, visited, sccId, comp, active);
        }
    }
}
