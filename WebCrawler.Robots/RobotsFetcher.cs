namespace WebCrawler.Robots
{
    public class RobotsFetcher
    {
        private readonly HttpClient _client;
        public RobotsFetcher(HttpClient client)
        {
            _client = client;
        }
        public async Task<string> FetchRobotsAsync(String url)
        {
            try
            {
                return await _client.GetStringAsync($"{url}/robots.txt");
            }
            catch (HttpRequestException ex)
            {
                // TODO: zaloguj ex
                return null!;
            }
        }
    }
}
