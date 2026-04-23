using WebCrawler.Domain;
using WebCrawler.Graph.Results;

namespace WebCrawler.Graph.Analysis
{
    /// <summary>
    /// Thin façade over <see cref="SCCFinder"/> / <see cref="WCCFinder"/>
    /// that returns a <see cref="ComponentAnalysis"/>, so the rest of the
    /// code no longer operates on raw <c>Dictionary&lt;int,int&gt;</c> values.
    /// </summary>
    public static class ComponentAnalyzer
    {
        public static ComponentAnalysis AnalyzeScc(CrawlGraph graph, HashSet<int>? active = null)
            => new("SCC", SCCFinder.FindKosaraju(graph, active));

        public static ComponentAnalysis AnalyzeWcc(CrawlGraph graph, HashSet<int>? active = null)
            => new("WCC", WCCFinder.FindWCC(graph, active));
    }
}
