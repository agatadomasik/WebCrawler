using WebCrawler.Domain;

namespace WebCrawler.Graph.Analysis
{
    /// <summary>
    /// Finds weakly connected components (DFS on the undirected graph).
    /// Returns the raw node→WCC id mapping.
    /// </summary>
    public static class WCCFinder
    {
        public static Dictionary<int, int> FindWCC(CrawlGraph graph, HashSet<int>? active = null)
        {
            active ??= new HashSet<int>(Enumerable.Range(0, graph.V));
            var comp = new Dictionary<int, int>();
            var visited = new HashSet<int>();

            int wccId = 0;
            for (int v = 0; v < graph.V; v++)
            {
                if (active.Contains(v) && !visited.Contains(v))
                {
                    wccId++;
                    Dfs(graph, v, visited, comp, wccId, active);
                }
            }

            return comp;
        }

        private static void Dfs(CrawlGraph graph, int u, HashSet<int> visited, Dictionary<int, int> comp, int wccId, HashSet<int> active)
        {
            visited.Add(u);
            comp[u] = wccId;

            foreach (var v in graph.Adjacency[u])
                if (active.Contains(v) && !visited.Contains(v))
                    Dfs(graph, v, visited, comp, wccId, active);

            foreach (var v in graph.AdjacencyReversed[u])
                if (active.Contains(v) && !visited.Contains(v))
                    Dfs(graph, v, visited, comp, wccId, active);
        }
    }
}
