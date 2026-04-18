using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using WebCrawler.Domain;

namespace WebCrawler.Graph
{
    public class ClusteringAnalyzer
    {
        public static double[] ComputeLocalClusteringCoefficients(CrawlGraph graph)
        {
            double[] cv = new double[graph.V];
            for (int v = 0; v < graph.V; v++)
            {
                cv[v] = ComputeLocalClusteringCoefficient(v, graph);
            }
            return cv;
        }
        public static double ComputeLocalClusteringCoefficient(int v, CrawlGraph graph)
        {
            int connections = 0;
            foreach (var n in graph.Adjacency[v])
            {
                foreach (var nn in graph.Adjacency[n])
                {
                    if (graph.Adjacency[v].Contains(nn)) connections++;
                }
            }
            if (graph.Adjacency[v].Count == 0 || graph.Adjacency[v].Count == 1) return -1;
            else return (double) connections / (graph.Adjacency[v].Count * (graph.Adjacency[v].Count - 1)); // not divided by 2 because each edge is counted twice
        }

        public static double ComputeGlobalClusteringCoefficient(CrawlGraph graph)
        {
            long closedTriplets = 0;
            long allTriplets = 0;

            for (int v = 0; v < graph.Adjacency.Count; v++)
            {
                int degree = graph.Adjacency[v].Count;
                if (degree < 2) continue;

                allTriplets += (long)degree * (degree - 1) / 2;

                foreach (var n in graph.Adjacency[v])
                {
                    foreach (var m in graph.Adjacency[v])
                    {
                        if (n < m && graph.Adjacency[n].Contains(m))
                            closedTriplets++;
                    }
                }
            }

            if (allTriplets == 0) return 0;
            return (double)closedTriplets / allTriplets;
        }
    }
}
