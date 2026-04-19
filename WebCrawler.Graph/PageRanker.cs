using System;
using System.Collections.Generic;
using System.Text;
using WebCrawler.Domain;

namespace WebCrawler.Graph
{
    public class PageRanker
    {
        public static readonly double[] DampingFactors = { 1.00, 0.99, 0.95, 0.90, 0.85, 0.70, 0.50 };
        public static (double[], int) ComputePageRank(CrawlGraph graph, Func<double[], double[], double> norm, double d = 0.85, double epsilon = 1e-6)
        {


            double[] x = new double[graph.V];

            for (int v = 0; v < graph.V; v++)
            {
                x[v] = (double)1 / graph.V;
            }

            double[] x_new = new double[graph.V];
            int iterations = 0;
            while (true)
            {
                iterations++;

                double danglingSum = 0;
                for (int v = 0; v < graph.V; v++)
                    if (graph.OutDegrees[v] == 0)
                        danglingSum += x[v];

                for (int v = 0; v < graph.V; v++)
                    x_new[v] = (double)(1 - d) / graph.V;

                for (int v = 0; v < graph.V; v++)
                {
                    x_new[v] += d * danglingSum / graph.V;
                    foreach (var u in graph.AdjacencyReversed[v])
                        x_new[v] += d * x[u] / graph.OutDegrees[u];
                }
                //The dangling node has no links, so we assume that the surfer teleports from it to a random page
                // that is, with equal probability to each of the N pages.

                double diff = norm(x_new, x);
                x = x_new.ToArray();
                if (diff < epsilon) break;
            }

            return (x, iterations);

        }

        public static double NormL1(double[] a, double[] b)
        {
            double s = 0;
            for (int i = 0; i < a.Length; i++) s += Math.Abs(a[i] - b[i]);
            return s;
        }

        public static double NormL2(double[] a, double[] b)
        {
            double s = 0;
            for (int i = 0; i < a.Length; i++) { double d = a[i] - b[i]; s += d * d; }
            return Math.Sqrt(s);
        }

        public static double NormLInf(double[] a, double[] b)
        {
            double m = 0;
            for (int i = 0; i < a.Length; i++) m = Math.Max(m, Math.Abs(a[i] - b[i]));
            return m;
        }
    }
}
