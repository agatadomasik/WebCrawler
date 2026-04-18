namespace WebCrawler.Domain
{
    public class CrawlResult
    {
        public string Url { get; set; }
        public List<string> OutLinks { get; set; } = new();

        public CrawlResult(string url)
        {
            Url = url;
        }
    }
}