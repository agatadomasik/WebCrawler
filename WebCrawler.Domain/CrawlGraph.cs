namespace WebCrawler.Domain
{
    public class CrawlGraph
    {
        public int V { get; }
        public int E { get; }
        public IReadOnlyList<int> OutDegrees { get; }
        public IReadOnlyList<int> InDegrees { get; }
        public List<List<int>> Adjacency { get; }
        public List<List<int>> AdjacencyReversed { get; }
        public List<string> IndexToUrl { get; }
        public Dictionary<string, int> UrlToIndex { get; }

        public CrawlGraph(
            List<List<int>> adjacency,
            List<List<int>> adjacencyReversed,
            List<string> indexToUrl,
            Dictionary<string, int> urlToIndex)
        {
            Adjacency = adjacency;
            AdjacencyReversed = adjacencyReversed;
            IndexToUrl = indexToUrl;
            UrlToIndex = urlToIndex;

            V = indexToUrl.Count;
            E = Adjacency.Sum(neighbors => neighbors.Count);
            OutDegrees = adjacency.Select(n => n.Count).ToArray();
            InDegrees = adjacencyReversed.Select(n => n.Count).ToArray();
        }

    }
}