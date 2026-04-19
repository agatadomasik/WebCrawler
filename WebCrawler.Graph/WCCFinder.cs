using WebCrawler.Domain;

public class WCCFinder
{
    public static Dictionary<int, int> FindWCC(CrawlGraph graph, HashSet<int> active = null!)
    {
        active ??= new HashSet<int>(Enumerable.Range(0, graph.V));
        var comp = new Dictionary<int, int>();
        var visited = new HashSet<int>();

        int wcc_id = 0;

        for (int v = 0; v < graph.V; v++)
        {
            if (active.Contains(v) && !visited.Contains(v))
            {
                wcc_id++;
                DFS(graph, v, visited, comp, wcc_id, active);
            }
        }

        return comp;
    }

    private static void DFS(CrawlGraph graph, int u, HashSet<int> visited,
                     Dictionary<int, int> comp, int wcc_id, HashSet<int> active)
    {
        visited.Add(u);
        comp[u] = wcc_id;

        foreach (var v in graph.Adjacency[u])
        {
            if (active.Contains(v) && !visited.Contains(v))
                DFS(graph, v, visited, comp, wcc_id, active);
        }

        foreach (var v in graph.AdjacencyReversed[u])
        {
            if (active.Contains(v) && !visited.Contains(v))
                DFS(graph, v, visited, comp, wcc_id, active);
        }
    }
}