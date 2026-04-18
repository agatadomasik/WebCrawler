using WebCrawler.Domain;

namespace WebCrawler.Graph
{
    public static class GraphBuilder
    {
        public static CrawlGraph Build(List<CrawlResult> results)
        {
            // krok 1 — przypisz każdemu URL indeks
            var urlToIndex = new Dictionary<string, int>();
            var indexToUrl = new List<string>();

            var visited = results.Select(r => r.Url).ToHashSet();

            foreach (var result in results)
            {
                urlToIndex[result.Url] = indexToUrl.Count;
                indexToUrl.Add(result.Url);
            }

            int n = indexToUrl.Count;

            // krok 2 — zainicjalizuj listy sąsiedztwa
            var adjacency = Enumerable.Range(0, n)
                .Select(_ => new List<int>())
                .ToList();

            var adjacencyReversed = Enumerable.Range(0, n)
                .Select(_ => new List<int>())
                .ToList();

            // krok 3 — dodaj krawędzie
            foreach (var result in results)
            {
                int from = urlToIndex[result.Url];

                foreach (var link in result.OutLinks)
                {
                    // ignorujemy linki do stron których nie odwiedziliśmy
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