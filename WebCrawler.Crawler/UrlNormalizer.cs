namespace WebCrawler.Crawler
{
    public static class UrlNormalizer
    {
        public static string? Normalize(string rawUrl, string baseUrl)
        {
            try
            {
                // Zamienia URL względny (np. "/about") na bezwzględny
                // używając baseUrl jako punktu odniesienia
                var uri = new Uri(new Uri(baseUrl), rawUrl);

                // Akceptujemy tylko http i https
                if (uri.Scheme != "http" && uri.Scheme != "https")
                    return null;

                // Odcinamy fragment (#section) — nie wpływa na treść strony
                var builder = new UriBuilder(uri)
                {
                    Fragment = string.Empty,
                    // Lowercase hosta (Host jest już lowercase w Uri, ale dla pewności)
                    Host = uri.Host.ToLowerInvariant(),
                    Scheme = uri.Scheme.ToLowerInvariant(),
                    Query = SortQueryParams(uri.Query)
                };

                // Usuwamy domyślne porty (:80 dla http, :443 dla https)
                if ((builder.Scheme == "http" && builder.Port == 80) ||
                    (builder.Scheme == "https" && builder.Port == 443))
                {
                    builder.Port = -1; // -1 = nie dołączaj portu do URL-a
                }

                return builder.Uri.ToString().TrimEnd('/');
            }
            catch
            {
                return null; // nieprawidłowy URL — ignorujemy
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