using System.ComponentModel;
using WebCrawler.Domain;

namespace WebCrawler.Graph
{
    public class SCCFinder
    {
        public static Dictionary<int, int> FindKosaraju(CrawlGraph graph, HashSet<int> active = null!)
        {
            active ??= new HashSet<int>(Enumerable.Range(0, graph.V));
            var comp = new Dictionary<int, int>();

            var order = new List<int>();
            var visited = new HashSet<int>();

            for (int v = 0; v < graph.V; v++) {
                if (active.Contains(v) && !visited.Contains(v))
                    DFS1(graph, v, visited, order, active);
            }

            visited = new HashSet<int>();
            int scc_id = 0;
            order.Reverse();
            foreach (var v in order)
            {
                if (active.Contains(v) && !visited.Contains(v))
                {
                    scc_id = scc_id + 1;
                    DFS2(graph, v, visited, scc_id, comp, active);
                }
            }

            return comp;
        }

        private static void DFS1(CrawlGraph G, int u, HashSet<int> visited, List<int> order, HashSet<int> active)
        {
            visited.Add(u);

            foreach (var w in G.Adjacency[u])
                if (active.Contains(w) && !visited.Contains(w))
                    DFS1(G, w, visited, order, active);
            order.Add(u);
        }

        private static void DFS2 (CrawlGraph G, int u, HashSet<int> visited, int scc_id, Dictionary<int, int> comp, HashSet<int> active)
        {
            visited.Add(u);
            comp[u] = scc_id;
            foreach (var w in G.AdjacencyReversed[u])
                if (active.Contains(w) && !visited.Contains(w))
                    DFS2(G, w, visited, scc_id, comp, active);
        }
    }
}
