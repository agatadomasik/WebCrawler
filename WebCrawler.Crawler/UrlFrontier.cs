using System.Collections.Concurrent;
using WebCrawler.Domain;

namespace WebCrawler.Crawler
{
    public class UrlFrontier
    {
        private readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
        private readonly ConcurrentDictionary<string, byte> _visited = new ConcurrentDictionary<string, byte>();

        public void Enqueue(string url)
        {
            if(_visited.TryAdd(url, 0))
            {
                _queue.Enqueue(url);
            }
        }

        public bool TryDequeue(out string url)
        {
            return _queue.TryDequeue(out url!);
        }
    }
}