using ScottPlot;
using WebCrawler.Graph.Results;

namespace WebCrawler.Graph.Charts
{
    /// <summary>
    /// Network robustness chart: size of the largest WCC and SCC
    /// as a function of the fraction of nodes removed.
    /// </summary>
    public static class RobustnessChart
    {
        public static void Render(
            IReadOnlyList<RobustnessAnalysisResult> results,
            int totalNodes,
            string scenarioLabel,
            string filename)
        {
            double[] fractions = results.Select(r => r.Fraction).ToArray();
            double[] wccFractions = results.Select(r => (double)r.LargestWCC / totalNodes).ToArray();
            double[] sccFractions = results.Select(r => (double)r.LargestSCC / totalNodes).ToArray();

            var plt = new Plot();

            var wccLine = plt.Add.Scatter(fractions, wccFractions);
            wccLine.LegendText = "Largest WCC";
            wccLine.Color = Colors.CornflowerBlue;
            wccLine.MarkerShape = MarkerShape.FilledCircle;
            wccLine.MarkerSize = 7;
            wccLine.LineWidth = 2;

            var sccLine = plt.Add.Scatter(fractions, sccFractions);
            sccLine.LegendText = "Largest SCC";
            sccLine.Color = Colors.Coral;
            sccLine.MarkerShape = MarkerShape.FilledSquare;
            sccLine.MarkerSize = 7;
            sccLine.LineWidth = 2;

            plt.Axes.Bottom.Label.Text = "Fraction of nodes removed";
            plt.Axes.Left.Label.Text = "Relative size of largest component";
            plt.Title($"Robustness – {scenarioLabel}");
            plt.ShowLegend();

            string path = ChartPaths.Ensure(filename);
            plt.SavePng(path, 900, 500);
            ChartPaths.LogSaved(path);
        }

        /// <summary>
        /// Plots how average and maximum in/out degree evolve as nodes are removed.
        /// Gives a quick visual summary of the degree distribution per removal step.
        /// </summary>
        public static void RenderDegreeEvolution(
            IReadOnlyList<RobustnessAnalysisResult> results,
            string scenarioLabel,
            string filename)
        {
            double[] fractions = results.Select(r => r.Fraction).ToArray();
            double[] avgIn  = results.Select(r => r.AvgInDegree).ToArray();
            double[] avgOut = results.Select(r => r.AvgOutDegree).ToArray();
            double[] maxIn  = results.Select(r => (double)r.MaxInDegree).ToArray();
            double[] maxOut = results.Select(r => (double)r.MaxOutDegree).ToArray();

            var plt = new Plot();

            var a1 = plt.Add.Scatter(fractions, avgIn);
            a1.LegendText = "avg in-degree";
            a1.Color = Colors.CornflowerBlue;
            a1.MarkerShape = MarkerShape.FilledCircle;

            var a2 = plt.Add.Scatter(fractions, avgOut);
            a2.LegendText = "avg out-degree";
            a2.Color = Colors.Coral;
            a2.MarkerShape = MarkerShape.FilledSquare;

            var m1 = plt.Add.Scatter(fractions, maxIn);
            m1.LegendText = "max in-degree";
            m1.Color = Colors.CornflowerBlue;
            m1.LinePattern = LinePattern.Dashed;
            m1.MarkerShape = MarkerShape.OpenCircle;

            var m2 = plt.Add.Scatter(fractions, maxOut);
            m2.LegendText = "max out-degree";
            m2.Color = Colors.Coral;
            m2.LinePattern = LinePattern.Dashed;
            m2.MarkerShape = MarkerShape.OpenSquare;

            plt.Axes.Bottom.Label.Text = "Fraction of nodes removed";
            plt.Axes.Left.Label.Text = "Degree";
            plt.Title($"Degree evolution – {scenarioLabel}");
            plt.ShowLegend();

            string path = ChartPaths.Ensure(filename);
            plt.SavePng(path, 900, 500);
            ChartPaths.LogSaved(path);
        }
    }
}
