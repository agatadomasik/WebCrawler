namespace WebCrawler.Graph.Algorithms
{
    /// <summary>
    /// Vector norms used to measure convergence (PageRank, etc.).
    /// Extracted from <c>PageRanker</c> so this logic is not duplicated
    /// across analysis and reporting classes.
    /// </summary>
    public static class VectorNorms
    {
        public delegate double NormFunc(double[] a, double[] b);

        public static readonly (string Name, NormFunc Fn) L1   = ("L1",   NormL1);
        public static readonly (string Name, NormFunc Fn) L2   = ("L2",   NormL2);
        public static readonly (string Name, NormFunc Fn) LInf = ("LInf", NormLInf);

        public static readonly IReadOnlyList<(string Name, NormFunc Fn)> All =
            new[] { L1, L2, LInf };

        public static double NormL1(double[] a, double[] b)
        {
            double s = 0;
            for (int i = 0; i < a.Length; i++) s += System.Math.Abs(a[i] - b[i]);
            return s;
        }

        public static double NormL2(double[] a, double[] b)
        {
            double s = 0;
            for (int i = 0; i < a.Length; i++)
            {
                double d = a[i] - b[i];
                s += d * d;
            }
            return System.Math.Sqrt(s);
        }

        public static double NormLInf(double[] a, double[] b)
        {
            double m = 0;
            for (int i = 0; i < a.Length; i++)
                m = System.Math.Max(m, System.Math.Abs(a[i] - b[i]));
            return m;
        }
    }
}
