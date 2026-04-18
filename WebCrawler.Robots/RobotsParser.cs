using System.Text.RegularExpressions;
using WebCrawler.Domain;

namespace WebCrawler.Robots
{
    public static class RobotsParser
    {
        public static RobotsFile ParseRobots(string host, string content)
        {
            RobotsFile robots = new RobotsFile(host);
            var lines = content.Split("\n", StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.StartsWith("#") || line.IsWhiteSpace())
                    continue;

                if (line.StartsWith("Allow"))
                {
                    var path = line.Substring("Allow:".Length).Trim();
                    robots.Allows.Add(PatternToRegex(path));
                }
                if (line.StartsWith("Disallow"))
                {
                    var path = line.Substring("Disallow:".Length).Trim();
                    robots.Disallows.Add(PatternToRegex(path));
                }
            }

            return robots;
        }

        private static Regex PatternToRegex(string pattern)
        {
            var escaped = Regex.Escape(pattern)
                               .Replace(@"\*", ".*")
                               .Replace(@"\$", "$");

            if (!pattern.EndsWith("$"))
                escaped += ".*";

            return new Regex("^" + escaped, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
    }
}
