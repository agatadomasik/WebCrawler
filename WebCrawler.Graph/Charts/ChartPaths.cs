namespace WebCrawler.Graph.Charts
{
    /// <summary>
    /// Central place for chart file paths.
    /// Guarantees that the "plots" directory exists before we save anything into it.
    /// </summary>
    public static class ChartPaths
    {
        public const string Directory = "plots";

        public static string Ensure(string filename)
        {
            System.IO.Directory.CreateDirectory(Directory);
            return System.IO.Path.Combine(Directory, filename);
        }

        public static void LogSaved(string fullPath)
            => System.Console.WriteLine($"Saved: {fullPath}");
    }
}
