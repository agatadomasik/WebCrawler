using WebCrawler.Domain;

namespace WebCrawler.Graph.Building
{
    /// <summary>
    /// Builds the condensation (DAG) based on an SCC result.
    /// </summary>
    public static class CondensationBuilder
    {
        public static CondensationGraph Build(CrawlGraph graph, IReadOnlyDictionary<int, int> comp)
        {
            var dag = new CondensationGraph();

            foreach (var id in comp.Values.Distinct())
                dag.Adjacency[id] = new HashSet<int>();

            for (int u = 0; u < graph.V; u++)
            {
                foreach (var v in graph.Adjacency[u])
                {
                    int cu = comp[u];
                    int cv = comp[v];
                    if (cu != cv) dag.Adjacency[cu].Add(cv);
                }
            }

            return dag;
        }
    }
}
