using System.Collections.Concurrent;
using System.Diagnostics;
using WebCrawler.Domain;
using WebCrawler.Robots;

namespace WebCrawler.Crawler
{
    public class CrawlerEngine
    {
        private readonly CrawlerOptions _options;
        private readonly RobotsService _robotsService;
        private readonly HttpClient _httpClient;

        // Wyniki crawlowania — thread-safe kolekcja
        private readonly ConcurrentBag<CrawlResult> _results = new();

        // Cache robots.txt per domena — żeby nie pobierać za każdym razem
        private RobotsFile? _robots;

        public CrawlerEngine(CrawlerOptions options, RobotsService robotsService, HttpClient httpClient)
        {
            _options = options;
            _robotsService = robotsService;
            _httpClient = httpClient;
        }

        public async Task<List<CrawlResult>> CrawlAsync(string seedUrl)
        {
            // Normalizujemy seed URL
            var normalizedSeed = UrlNormalizer.Normalize(seedUrl, seedUrl)!;
            var seedDomain = UrlNormalizer.GetDomain(normalizedSeed);
            _robots ??= await _robotsService.GetRobotsFileAsync($"https://{seedDomain}");

            var frontier = new UrlFrontier();
            frontier.Enqueue(normalizedSeed);

            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine($"Crawling {seedUrl} with {_options.ThreadCount} threads...");


            // N workerów startuje jednocześnie i każdy sam pobiera URL-e z kolejki
            var workers = Enumerable
                .Range(0, _options.ThreadCount)
                .Select(_ => WorkerAsync(frontier, seedDomain));

            await Task.WhenAll(workers);

            stopwatch.Stop();

            var elapsed = stopwatch.Elapsed.TotalSeconds;
            var throughput = _results.Count / elapsed;

            Console.WriteLine($"\n=== Crawl complete ===");
            Console.WriteLine($"Pages: {_results.Count}");
            Console.WriteLine($"Time: {elapsed:F1}s");
            Console.WriteLine($"Throughput: {throughput:F2} pages/s");

            return _results.ToList();
        }

        private async Task WorkerAsync(UrlFrontier frontier, string seedDomain)
        {
            int emptyRetries = 0;

            while (_results.Count < _options.MaxPages)
            {
                if (!frontier.TryDequeue(out var url))
                {
                    if (++emptyRetries > 10) break;
                    await Task.Delay(100);
                    continue;
                }

                emptyRetries = 0;

                // TODO add vertex
                await CrawlSinglePageAsync(url, frontier, seedDomain);
            }
        }

        private async Task CrawlSinglePageAsync(
            string url,
            UrlFrontier frontier,
            string allowedDomain)
        {
            try
            {
                // Sprawdzamy robots.txt (z cache)
                if (_robots != null && !_robots.IsAllowed(url)) return;

                // Czekamy zgodnie z polityką grzeczności
                await Task.Delay(_options.PolitenessDelay);

                // Pobieramy stronę
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", _options.UserAgent);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return;

                // Sprawdzamy czy to HTML (nie PDF, obrazek itp.)
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                if (!contentType.Contains("text/html")) return;

                var html = await response.Content.ReadAsStringAsync();

                // Wyciągamy linki
                var links = LinkExtractor.ExtractLinks(html, url);

                // Filtrujemy tylko linki do tej samej domeny i dodajemy do frontieru
                foreach (var link in links)
                {
                    if (UrlNormalizer.GetDomain(link) == allowedDomain)
                    {
                        frontier.Enqueue(link);
                        // TODO add out edges
                    }
                }

                // Zapisujemy wynik
                var result = new CrawlResult(url);
                result.OutLinks.AddRange(links.Where(l => UrlNormalizer.GetDomain(l) == allowedDomain));
                _results.Add(result);

                if (_results.Count % 100 == 0)
                    Console.WriteLine($"  Crawled {_results.Count} pages...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error crawling {url}: {ex.Message}");
            }
        }
    }
}