using WebCrawler.Domain;

namespace WebCrawler.Robots
{
    public class RobotsService
    {
        private readonly RobotsFetcher _fetcher;

        public RobotsService(RobotsFetcher fetcher)
        {
            _fetcher = fetcher;
        }

        public async Task<RobotsFile?> GetRobotsFileAsync(string host)
        {
            var content = await _fetcher.FetchRobotsAsync(host);
            if (content is null) return null;
            return RobotsParser.ParseRobots(host, content);
        }
    }
}
