using WebCrawler.Domain;

namespace WebCrawler.Graph.Building
{
    public static class GraphBuilder
    {
        public static CrawlGraph Build(List<CrawlResult> results)
        {
            // step 1 — assign an index to every URL
            var urlToIndex = new Dictionary<string, int>();
            var indexToUrl = new List<string>();

            var visited = results.Select(r => r.Url).ToHashSet();

            foreach (var result in results)
            {
                urlToIndex[result.Url] = indexToUrl.Count;
                indexToUrl.Add(result.Url);
            }

            int n = indexToUrl.Count;

            // step 2 — initialise adjacency lists
            var adjacency = Enumerable.Range(0, n)
                .Select(_ => new List<int>())
                .ToList();

            var adjacencyReversed = Enumerable.Range(0, n)
                .Select(_ => new List<int>())
                .ToList();

            // step 3 — add edges
            foreach (var result in results)
            {
                int from = urlToIndex[result.Url];

                foreach (var link in result.OutLinks.Distinct())
                {
                    // ignore links to pages we never visited
                    if (!urlToIndex.TryGetValue(link, out int to))
                        continue;

                    adjacency[from].Add(to);
                    adjacencyReversed[to].Add(from);
                }
            }

            return new CrawlGraph(adjacency, adjacencyReversed, indexToUrl, urlToIndex);
        }
    }
}