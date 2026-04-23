namespace WebCrawler.Graph.Results
{
    /// <summary>
    /// Result of a single step in the node-removal simulation
    /// (random failure or targeted attack).
    /// </summary>
    /// <param name="Fraction">Fraction of nodes removed.</param>
    /// <param name="LargestWCC">Size of the largest WCC after removal.</param>
    /// <param name="LargestSCC">Size of the largest SCC after removal.</param>
    /// <param name="AvgDist">Average shortest-path length inside the largest WCC.</param>
    /// <param name="Diameter">Diameter of the largest WCC.</param>
    /// <param name="InDegrees">In-degrees of the still-active vertices (edges to removed vertices are excluded).</param>
    /// <param name="OutDegrees">Out-degrees of the still-active vertices (edges to removed vertices are excluded).</param>
    public record RobustnessAnalysisResult(
        double Fraction,
        int LargestWCC,
        int LargestSCC,
        double AvgDist,
        int Diameter,
        int[] InDegrees,
        int[] OutDegrees)
    {
        public double AvgInDegree  => InDegrees.Length  == 0 ? 0 : InDegrees.Average();
        public double AvgOutDegree => OutDegrees.Length == 0 ? 0 : OutDegrees.Average();
        public int MaxInDegree  => InDegrees.Length  == 0 ? 0 : InDegrees.Max();
        public int MaxOutDegree => OutDegrees.Length == 0 ? 0 : OutDegrees.Max();
    }
}
