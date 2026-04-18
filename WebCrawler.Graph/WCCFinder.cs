using WebCrawler.Domain;

public class WCCFinder
{
    public Dictionary<int, int> FindWCC(CrawlGraph graph)
    {
        var comp = new Dictionary<int, int>();
        var visited = new HashSet<int>();

        int wcc_id = 0;

        for (int v = 0; v < graph.V; v++)
        {
            if (!visited.Contains(v))
            {
                wcc_id++;
                DFS(graph, v, visited, comp, wcc_id);
            }
        }

        return comp;
    }

    private void DFS(CrawlGraph graph, int u, HashSet<int> visited,
                     Dictionary<int, int> comp, int wcc_id)
    {
        visited.Add(u);
        comp[u] = wcc_id;

        foreach (var v in graph.Adjacency[u])
        {
            if (!visited.Contains(v))
                DFS(graph, v, visited, comp, wcc_id);
        }

        foreach (var v in graph.AdjacencyReversed[u])
        {
            if (!visited.Contains(v))
                DFS(graph, v, visited, comp, wcc_id);
        }
    }
}