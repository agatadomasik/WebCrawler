using WebCrawler.Domain;

public class CondensationBuilder
{
    public CondensationGraph Build(CrawlGraph graph, Dictionary<int, int> comp)
    {
        var dag = new CondensationGraph();

        var sccIds = comp.Values.Distinct();

        foreach (var id in sccIds)
            dag.Adjacency[id] = new HashSet<int>();

        for (int u = 0; u < graph.V; u++)
        {
            foreach (var v in graph.Adjacency[u])
            {
                int cu = comp[u];
                int cv = comp[v];

                if (cu != cv)
                {
                    dag.Adjacency[cu].Add(cv);
                }
            }
        }

        return dag;
    }
}