using System.ComponentModel;
using WebCrawler.Domain;

namespace WebCrawler.Graph
{
    public class SCCFinder
    {
        public Dictionary<int, int> FindKosaraju(CrawlGraph graph)
        {
            var comp = new Dictionary<int, int>();

            var order = new List<int>();
            var visited = new HashSet<int>();

            for (int v = 0; v < graph.V; v++) {
                if (!visited.Contains(v))
                    DFS1(graph, v, visited, order);
            }

            visited = new HashSet<int>();
            int scc_id = 0;
            order.Reverse();
            foreach (var v in order)
            {
                if (!visited.Contains(v))
                {
                    scc_id = scc_id + 1;
                    DFS2(graph, v, visited, scc_id, comp);
                }
            }

            return comp;
        }

        private void DFS1(CrawlGraph G, int u, HashSet<int> visited, List<int> order)
        {
            visited.Add(u);

            foreach (var w in G.Adjacency[u])
                if (!visited.Contains(w))
                    DFS1(G, w, visited, order);
            order.Add(u);
        }

        private void DFS2 (CrawlGraph G, int u, HashSet<int> visited, int scc_id, Dictionary<int, int> comp)
        {
            visited.Add(u);
            comp[u] = scc_id;
            foreach (var w in G.AdjacencyReversed[u])
                if (!visited.Contains(w))
                    DFS2(G, w, visited, scc_id, comp);
        }
    }
}
