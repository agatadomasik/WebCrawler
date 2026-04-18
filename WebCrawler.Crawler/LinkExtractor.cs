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

            // XPath: znajdź wszystkie tagi <a> które mają atrybut href
            var nodes = doc.DocumentNode.SelectNodes("//a[@href]");
            if (nodes == null) return links;

            foreach (var node in nodes)
            {
                var href = node.GetAttributeValue("href", string.Empty);
                if (string.IsNullOrWhiteSpace(href)) continue;

                // Normalizujemy — zamieniamy na pełny, absolutny URL
                var normalized = UrlNormalizer.Normalize(href, baseUrl);
                if (normalized != null)
                    links.Add(normalized);
            }

            return links;
        }
    }
}