using ScottPlot;
using System;
using System.Collections.Generic;
using System.Text;
using WebCrawler.Domain;
using static SkiaSharp.HarfBuzz.SKShaper;

namespace WebCrawler.Graph
{
    public class RobustnessAnalyzer
    {
        private static readonly double[] Fractions = { 0.01, 0.02, 0.05, 0.10, 0.20, 0.30, 0.50 };
        public static List<RobustnessAnalysisResult> SimulateRemoval(CrawlGraph graph, bool attack)
        {
            var results = new List<RobustnessAnalysisResult>();

            var active = new HashSet<int>(Enumerable.Range(0, graph.V));
            var adjacency = graph.Adjacency.Select(l => l.ToList()).ToList();
            var adjacencyReversed = graph.AdjacencyReversed.Select(l => l.ToList()).ToList();

            var rnd = new Random();
            int removedCount = 0;

            foreach (double fraction in Fractions)
            {
                int toRemoveCount = (int)(graph.V * fraction) - removedCount;
                var toRemove = new List<int>();

                if (attack)
                    toRemove = active
                        .OrderByDescending(v => graph.InDegrees[v])
                        .Take(toRemoveCount)
                        .ToList();
                else
                    toRemove = rnd.GetItems(active.ToArray(), toRemoveCount).ToList<int>();

                foreach (int v in toRemove)
                {
                    active.Remove(v);
                    removedCount++;
                }

                var wccComp = WCCFinder.FindWCC(graph, active);
                int largestWCC = wccComp.Values.GroupBy(id => id).Max(g => g.Count());

                var sccComp = SCCFinder.FindKosaraju(graph, active);
                int largestSCC = sccComp.Values.GroupBy(id => id).Max(g => g.Count());

                var largestWCCNodes = wccComp
                    .GroupBy(kv => kv.Value)
                    .OrderByDescending(g => g.Count())
                    .First()
                    .Select(kv => kv.Key)
                    .ToHashSet();

                var (avgDists, diameter, _, _) = BFSExplorer.ExploreBFS(graph, largestWCCNodes);
                double avgDist = avgDists.Where(d => d >= 0).DefaultIfEmpty(0).Average();

                results.Add(new RobustnessAnalysisResult(fraction, largestWCC, largestSCC, avgDist, diameter));
            }

            return results;
        }

        public double[] ComputeBetweennessCentralityBrandes(CrawlGraph graph)
        {
            var cb = new double[graph.V];

            for (int s = 0; s < graph.V; s++)
            {
                var stack = new Stack<int>();

                var sigma = new double[graph.V];
                sigma[s] = 1;

                var d = Enumerable.Repeat(-1, graph.V).ToArray();
                d[s] = 0;

                var queue = new Queue<int>();
                queue.Enqueue(s);

                var Pred = new List<int>[graph.V];
                for (int i = 0; i < graph.V; i++)
                    Pred[i] = new List<int>();

                while (queue.Count != 0)
                {
                    var v = queue.Dequeue();

                    stack.Push(v);
                    foreach (var w in graph.Adjacency[v])
                    {
                        if (d[w] < 0) // w not visited
                        {
                            queue.Enqueue(w);
                            d[w] = d[v] + 1;
                        }

                        if (d[w] == d[v] + 1) // w in next level
                        {
                            sigma[w] += sigma[v];
                            Pred[w].Add(v);
                        }
                    }
                }

                var delta = new double[graph.V];

                while(stack.Count != 0)
                {
                    var w = stack.Pop();

                     foreach(var v in Pred[w])
                        delta[v] += (sigma[v] / sigma[w]) * (1 + delta[w]);

                    if (w != s)
                    {
                        cb[w] += delta[w];
                    }
                }
            }

            return cb;
        }

        public record RobustnessAnalysisResult(double Fraction, int LargestWCC, int LargestSCC, double AvgDist, int Diameter);
    }
}
