using System.Text.RegularExpressions;

namespace WebCrawler.Domain
{
    public class RobotsFile
    {
        public string Host { get; set; }
        public List<Regex> Allows { get; set; } = new List<Regex>();
        public List<Regex> Disallows { get; set; } = new List<Regex>();
        public RobotsFile(string host)
        {
            Host = host;
        }

        public bool IsAllowed(string url)
        {
            var path = new Uri(url).PathAndQuery;

            bool disallowed = Disallows.Any(r => r.IsMatch(path));
            bool allowed = Allows.Any(r => r.IsMatch(path));

            return !disallowed || allowed;
        }
    }
}
