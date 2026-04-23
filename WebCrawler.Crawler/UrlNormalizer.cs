namespace WebCrawler.Crawler
{
    public static class UrlNormalizer
    {
        public static string? Normalize(string rawUrl, string baseUrl)
        {
            try
            {
                // Resolve a relative URL (e.g. "/about") against the baseUrl
                // to produce an absolute URL
                var uri = new Uri(new Uri(baseUrl), rawUrl);

                // Accept http and https only
                if (uri.Scheme != "http" && uri.Scheme != "https")
                    return null;

                // Drop the fragment (#section) — it does not affect page content
                var builder = new UriBuilder(uri)
                {
                    Fragment = string.Empty,
                    // Lowercase host (Uri already lowercases the Host, but do it explicitly)
                    Host = uri.Host.ToLowerInvariant(),
                    Scheme = uri.Scheme.ToLowerInvariant(),
                    Query = SortQueryParams(uri.Query)
                };

                // Strip default ports (:80 for http, :443 for https)
                if ((builder.Scheme == "http" && builder.Port == 80) ||
                    (builder.Scheme == "https" && builder.Port == 443))
                {
                    builder.Port = -1; // -1 = do not append the port to the URL
                }

                return builder.Uri.ToString().TrimEnd('/');
            }
            catch
            {
                return null; // malformed URL — ignore it
            }
        }
        private static string SortQueryParams(string query)
        {
            if (string.IsNullOrEmpty(query)) return "";

            var parsed = System.Web.HttpUtility.ParseQueryString(query);
            var sorted = parsed.AllKeys
                .Where(k => k != null)
                .OrderBy(k => k)
                .Select(k => $"{Uri.EscapeDataString(k!)}={Uri.EscapeDataString(parsed[k] ?? "")}");

            return "?" + string.Join("&", sorted);
        }

        public static string GetDomain(string url)
        {
            return new Uri(url).Host.ToLowerInvariant();
        }
    }
}