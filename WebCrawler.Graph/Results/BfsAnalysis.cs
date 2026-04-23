namespace WebCrawler.Graph.Results
{
    /// <summary>
    /// Aggregated BFS-exploration results from every vertex of the graph.
    /// </summary>
    /// <param name="AvgDistPerNode">Average shortest-path length from a given vertex (-1 = isolated).</param>
    /// <param name="Eccentricity">Vertex eccentricity (-1 = does not reach any other vertex).</param>
    /// <param name="Diameter">Diameter of the graph.</param>
    /// <param name="Radius">Radius of the graph (-1 = undefined).</param>
    /// <param name="PairDistanceHistogram">
    /// Histogram of pair distances: index = distance, value = number of ordered (u,v) pairs
    /// with shortest-path length equal to that distance. Index 0 is always 0 (pairs u→u skipped).
    /// </param>
    public record BfsAnalysis(
        double[] AvgDistPerNode,
        int[] Eccentricity,
        int Diameter,
        int Radius,
        long[] PairDistanceHistogram)
    {
        /// <summary>Average shortest-path length across the whole graph (isolated vertices excluded).</summary>
        public double GlobalAverageDistance =>
            AvgDistPerNode.Where(x => x >= 0).DefaultIfEmpty(0).Average();

        /// <summary>Total number of reachable ordered pairs (u,v), u ≠ v.</summary>
        public long ReachablePairs => PairDistanceHistogram.Sum();
    }
}
