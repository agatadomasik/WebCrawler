using System.Text.Json;
using WebCrawler.Domain;

namespace WebCrawler.Crawler
{
    public static class CrawlResultSerializer
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true
        };

        public static async Task SaveAsync(List<CrawlResult> results, string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            await using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, results, _options);

            Console.WriteLine($"Saved {results.Count} results to {filePath}");
        }

        public static async Task<List<CrawlResult>> LoadAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Results file not found: {filePath}");

            await using var stream = File.OpenRead(filePath);
            var results = await JsonSerializer.DeserializeAsync<List<CrawlResult>>(stream);

            if (results is null)
                throw new InvalidDataException($"Failed to deserialize results from {filePath}");

            Console.WriteLine($"Loaded {results.Count} results from {filePath}");
            return results;
        }
    }
}