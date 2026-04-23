using HtmlAgilityPack;

namespace WebCrawler.Crawler
{
    public static class LinkExtractor
    {
        public static List<string> ExtractLinks(string html, string baseUrl)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var links = new List<string>();

            // XPath: find every <a> tag that has an href attribute
            var nodes = doc.DocumentNode.SelectNodes("//a[@href]");
            if (nodes == null) return links;

            foreach (var node in nodes)
            {
                var href = node.GetAttributeValue("href", string.Empty);
                if (string.IsNullOrWhiteSpace(href)) continue;

                // Normalize — convert to a full absolute URL
                var normalized = UrlNormalizer.Normalize(href, baseUrl);
                if (normalized != null)
                    links.Add(normalized);
            }

            return links;
        }
    }
}