namespace WebCrawler.Domain
{
    public class CrawlerOptions
    {
        public int MaxPages { get; set; } = 1000;
        public int ThreadCount { get; set; } = 4;

        public TimeSpan PolitenessDelay { get; set; } = TimeSpan.FromSeconds(1);

        public string UserAgent { get; set; } = "MyCrawler/1.0";
    }
}