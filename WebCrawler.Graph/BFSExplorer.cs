using WebCrawler.Domain;

namespace WebCrawler.Graph
{
    public class BFSExplorer
    {
        public static (double[] avgDist, int diameter, int[] ecc, int radius) ExploreBFS(CrawlGraph graph, HashSet<int> active = null!)
        {
            active ??= new HashSet<int>(Enumerable.Range(0, graph.V));
            int n = graph.V;

            double[] avgDist = new double[n];
            int[] ecc = new int[n];

            int diameter = 0;
            int radius = int.MaxValue;

            for (int s = 0; s < n; s++)
            {
                if (!active.Contains(s)) continue;
                var (dist, _) = BFS(graph, s, active);

                long sum = 0;
                int count = 0;
                int maxDist = 0;

                for (int v = 0; v < n; v++)
                {
                    if (dist[v] != -1)
                    {
                        sum += dist[v];
                        count++;
                        maxDist = Math.Max(maxDist, dist[v]);
                    }
                }

                if (count > 0)
                    avgDist[s] = (double)sum / count;
                else
                    avgDist[s] = -1;

                if (count > 1)
                {
                    ecc[s] = maxDist;

                    diameter = Math.Max(diameter, maxDist);
                    radius = Math.Min(radius, maxDist);
                }
                else
                {
                    ecc[s] = -1; // node reaches only itself
                }
            }

            if (radius == int.MaxValue)
                radius = -1;

            return (avgDist, diameter, ecc, radius);
        }
        public static (int[] dist, int[] parent) BFS(CrawlGraph graph, int source, HashSet<int> active)
        {
            var dist = new int[graph.V];
            var parent = new int[graph.V];
            for (int v = 0; v < graph.V; v++)
            {
                dist[v] = -1;
                parent[v] = -1;
            }

            dist[source] = 0;
            var queue = new Queue<int>();

            queue.Enqueue(source);

            while(queue.Count > 0)
            {
                int u = queue.Dequeue();
                foreach (var v in graph.Adjacency[u])
                {
                    if (active.Contains(v) && dist[v] == -1) {        // v not visited?
                        dist[v] = dist[u] + 1;
                        parent[v] = u;
                        queue.Enqueue(v);
                    }
                }
            }

            return (dist, parent);
        }
    }
}
