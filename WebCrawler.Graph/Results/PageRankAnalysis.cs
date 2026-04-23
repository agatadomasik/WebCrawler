namespace WebCrawler.Graph.Results
{
    /// <summary>
    /// Full PageRank result together with the convergence trace.
    /// Thanks to <see cref="ConvergencePath"/> no client class has to
    /// duplicate the iterative loop just to collect the error history.
    /// </summary>
    /// <param name="Scores">PageRank vector.</param>
    /// <param name="Iterations">Number of iterations taken to converge.</param>
    /// <param name="DampingFactor">Damping factor d that was used.</param>
    /// <param name="NormName">Name of the norm used (e.g. "L1").</param>
    /// <param name="ConvergencePath">Error-norm value recorded at each iteration.</param>
    public record PageRankAnalysis(
        double[] Scores,
        int Iterations,
        double DampingFactor,
        string NormName,
        IReadOnlyList<double> ConvergencePath);
}
