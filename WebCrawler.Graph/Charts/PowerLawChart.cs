using ScottPlot;
using WebCrawler.Graph.Results;

namespace WebCrawler.Graph.Charts
{
    /// <summary>
    /// Power-law fit charts.
    /// The maths live in <see cref="Analysis.PowerLawFitter"/>; this class only renders.
    /// </summary>
    public static class PowerLawChart
    {
        public static void RenderOls(OlsPowerLawFit fit, string title, string filename)
        {
            var plt = new Plot();

            var scatter = plt.Add.Scatter(fit.LogK, fit.LogCcdf);
            scatter.LegendText = "Empirical CCDF";
            scatter.LineWidth = 0;
            scatter.MarkerSize = 6;
            scatter.Color = Colors.SteelBlue;

            double[] fitX = { fit.LogK.Min(), fit.LogK.Max() };
            double[] fitY = { fit.Slope * fitX[0] + fit.Intercept, fit.Slope * fitX[1] + fit.Intercept };
            var line = plt.Add.Scatter(fitX, fitY);
            line.LegendText = $"OLS: γ={fit.Gamma:F3}, R²={fit.R2:F4}";
            line.MarkerSize = 0;
            line.LineWidth = 2;
            line.Color = Colors.OrangeRed;

            plt.Title(title);
            plt.XLabel("log(k)");
            plt.YLabel("log P(K ≥ k)");
            plt.ShowLegend();

            string path = ChartPaths.Ensure(filename);
            plt.SavePng(path, 800, 550);
            ChartPaths.LogSaved(path);
        }

        public static void RenderMle(MlePowerLawFit fit, string title, string filename)
        {
            var tail = fit.Tail;
            if (tail.Count == 0) return;

            int n = tail.Count;
            double[] empX = tail.Select(x => (double)x).ToArray();
            double[] empY = Enumerable.Range(1, n).Select(i => (double)i / n).ToArray();
            double[] theoY = empX
                .Select(x => 1.0 - System.Math.Pow(x / fit.XMin, 1 - fit.Gamma))
                .ToArray();

            var plt = new Plot();

            var emp = plt.Add.Scatter(empX, empY);
            emp.LegendText = "Empirical CDF";
            emp.LineWidth = 1.5f;
            emp.MarkerSize = 0;
            emp.Color = Colors.SteelBlue;

            var theo = plt.Add.Scatter(empX, theoY);
            theo.LegendText = $"MLE: γ={fit.Gamma:F3}, x_min={fit.XMin}, KS={fit.Ks:F4}";
            theo.LineWidth = 2;
            theo.MarkerSize = 0;
            theo.Color = Colors.OrangeRed;
            theo.LinePattern = LinePattern.Dashed;

            plt.Title(title);
            plt.XLabel("k");
            plt.YLabel("P(K ≤ k)");
            plt.ShowLegend();

            string path = ChartPaths.Ensure(filename);
            plt.SavePng(path, 800, 550);
            ChartPaths.LogSaved(path);
        }
    }
}
