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

        // Crawl results — thread-safe collection
        private readonly ConcurrentBag<CrawlResult> _results = new();

        // Per-domain robots.txt cache — avoid refetching on every request
        private RobotsFile? _robots;

        // Host-wide politeness gate: ensures at most one request per PolitenessDelay
        // to the target host regardless of how many worker threads are running.
        private readonly SemaphoreSlim _hostLock = new(1, 1);
        private DateTime _lastHitUtc = DateTime.MinValue;

        public CrawlerEngine(CrawlerOptions options, RobotsService robotsService, HttpClient httpClient)
        {
            _options = options;
            _robotsService = robotsService;
            _httpClient = httpClient;
        }

        public async Task<List<CrawlResult>> CrawlAsync(string seedUrl)
        {
            // Normalize the seed URL
            var normalizedSeed = UrlNormalizer.Normalize(seedUrl, seedUrl)!;
            var seedDomain = UrlNormalizer.GetDomain(normalizedSeed);
            _robots ??= await _robotsService.GetRobotsFileAsync($"https://{seedDomain}");

            var frontier = new UrlFrontier();
            frontier.Enqueue(normalizedSeed);

            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine($"Crawling {seedUrl} with {_options.ThreadCount} threads...");


            // N workers start at the same time and each pulls URLs from the queue
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
                // Check robots.txt (from cache)
                if (_robots != null && !_robots.IsAllowed(url)) return;

                // Fetch the page (politeness gate enforces host-wide rate limiting)
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", _options.UserAgent);

                var response = await SendPolitelyAsync(request);
                if (!response.IsSuccessStatusCode) return;

                // Verify that the response is HTML (not PDF, image, etc.)
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                if (!contentType.Contains("text/html")) return;

                var html = await response.Content.ReadAsStringAsync();

                // Extract links
                var links = LinkExtractor.ExtractLinks(html, url);

                // Keep only same-domain links and enqueue them in the frontier
                foreach (var link in links)
                {
                    if (UrlNormalizer.GetDomain(link) == allowedDomain)
                    {
                        frontier.Enqueue(link);
                    }
                }

                // Record the result
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

        /// <summary>
        /// Sends an HTTP request while honouring a host-wide politeness delay.
        /// Only one request at a time can enter the gate; the next one is released
        /// after PolitenessDelay has elapsed since the previous request started,
        /// so the effective rate stays at 1 / PolitenessDelay regardless of how
        /// many worker threads compete for the gate.
        /// </summary>
        private async Task<HttpResponseMessage> SendPolitelyAsync(HttpRequestMessage request)
        {
            await _hostLock.WaitAsync();
            try
            {
                var sinceLast = DateTime.UtcNow - _lastHitUtc;
                var waitFor = _options.PolitenessDelay - sinceLast;
                if (waitFor > TimeSpan.Zero)
                    await Task.Delay(waitFor);

                _lastHitUtc = DateTime.UtcNow;
            }
            finally
            {
                _hostLock.Release();
            }

            return await _httpClient.SendAsync(request);
        }
    }
}