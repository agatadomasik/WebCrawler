namespace WebCrawler.Graph.Results
{
    /// <summary>
    /// Results of the clustering-coefficient analysis.
    /// </summary>
    /// <param name="LocalCoefficients">C(v) for each vertex; -1 when undefined (deg &lt; 2).</param>
    /// <param name="Global">Global clustering coefficient (transitivity).</param>
    /// <param name="AverageCByDegree">Average C(k) for every degree k &gt; 0.</param>
    /// <param name="LogLogSlope">Slope of the log(C(k)) vs log(k) regression.</param>
    /// <param name="LogLogIntercept">Intercept of the log-log regression.</param>
    public record ClusteringAnalysis(
        double[] LocalCoefficients,
        double Global,
        IReadOnlyDictionary<int, double> AverageCByDegree,
        double LogLogSlope,
        double LogLogIntercept);
}
