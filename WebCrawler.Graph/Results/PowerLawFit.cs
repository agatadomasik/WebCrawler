namespace WebCrawler.Graph.Results
{
    /// <summary>
    /// Power-law fit obtained via OLS on the CCDF.
    /// </summary>
    /// <param name="Gamma">Power-law exponent γ.</param>
    /// <param name="R2">Coefficient of determination of the fit.</param>
    /// <param name="LogK">log(k) — X-axis points (for plotting).</param>
    /// <param name="LogCcdf">log P(K ≥ k) — empirical points.</param>
    /// <param name="Slope">Slope of the regression line.</param>
    /// <param name="Intercept">Intercept of the regression line.</param>
    public record OlsPowerLawFit(
        double Gamma,
        double R2,
        double[] LogK,
        double[] LogCcdf,
        double Slope,
        double Intercept);

    /// <summary>
    /// Power-law fit obtained via MLE on the CDF.
    /// </summary>
    /// <param name="Gamma">Exponent γ.</param>
    /// <param name="Ks">Kolmogorov–Smirnov distance (lower = better fit).</param>
    /// <param name="XMin">Lower threshold of the fit.</param>
    /// <param name="Tail">Sorted sample used for the fit.</param>
    public record MlePowerLawFit(
        double Gamma,
        double Ks,
        double XMin,
        IReadOnlyList<int> Tail);
}
