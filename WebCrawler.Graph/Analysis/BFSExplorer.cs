using WebCrawler.Domain;
using WebCrawler.Graph.Results;

namespace WebCrawler.Graph.Analysis
{
    /// <summary>
    /// Runs BFS from every vertex and returns a <see cref="BfsAnalysis"/>.
    /// Pure class: no console output, no charts.
    /// </summary>
    public static class BFSExplorer
    {
        public static BfsAnalysis Explore(CrawlGraph graph, HashSet<int>? active = null)
        {
            active ??= new HashSet<int>(Enumerable.Range(0, graph.V));
            int n = graph.V;

            double[] avgDist = new double[n];
            int[] ecc = new int[n];

            int diameter = 0;
            int radius = int.MaxValue;

            // Pre-size distance histogram generously; we grow on demand.
            var pairHistogram = new long[16];

            for (int s = 0; s < n; s++)
            {
                if (!active.Contains(s))
                {
                    avgDist[s] = -1;
                    ecc[s] = -1;
                    continue;
                }

                var (dist, _) = Bfs(graph, s, active);

                long sum = 0;
                int count = 0;
                int maxDist = 0;

                for (int v = 0; v < n; v++)
                {
                    int d = dist[v];
                    if (d > 0)
                    {
                        sum += d;
                        count++;
                        if (d > maxDist) maxDist = d;

                        if (d >= pairHistogram.Length)
                        {
                            int newSize = pairHistogram.Length;
                            while (newSize <= d) newSize *= 2;
                            Array.Resize(ref pairHistogram, newSize);
                        }
                        pairHistogram[d]++;
                    }
                }

                avgDist[s] = count > 0 ? (double)sum / count : -1;

                if (count >= 1)
                {
                    ecc[s] = maxDist;
                    diameter = System.Math.Max(diameter, maxDist);
                    radius = System.Math.Min(radius, maxDist);
                }
                else
                {
                    ecc[s] = -1; // vertex can only reach itself
                }
            }

            if (radius == int.MaxValue) radius = -1;

            // Trim trailing zeros for a cleaner histogram.
            int lastNonZero = pairHistogram.Length - 1;
            while (lastNonZero >= 0 && pairHistogram[lastNonZero] == 0) lastNonZero--;
            var trimmed = new long[lastNonZero + 1];
            Array.Copy(pairHistogram, trimmed, trimmed.Length);

            return new BfsAnalysis(avgDist, ecc, diameter, radius, trimmed);
        }

        public static (int[] dist, int[] parent) Bfs(CrawlGraph graph, int source, HashSet<int> active)
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

            while (queue.Count > 0)
            {
                int u = queue.Dequeue();
                foreach (var v in graph.Adjacency[u])
                {
                    if (active.Contains(v) && dist[v] == -1)
                    {
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
