namespace WebCrawler.Robots
{
    public class RobotsFetcher
    {
        private readonly HttpClient _client;
        public RobotsFetcher(HttpClient client)
        {
            _client = client;
        }
        public async Task<string?> FetchRobotsAsync(string url)
        {
            try
            {
                return await _client.GetStringAsync($"{url}/robots.txt");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"  robots.txt fetch failed for {url}: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"  robots.txt fetch timed out for {url}");
                return null;
            }
        }
    }
}
