namespace WebCrawler.Graph.Results
{
    /// <summary>
    /// Result of connected-component detection (SCC or WCC).
    /// Replaces the raw <see cref="Dictionary{Int32, Int32}"/> that used to
    /// leak through many method signatures.
    /// </summary>
    public sealed class ComponentAnalysis
    {
        /// <summary>Label (e.g. "SCC" / "WCC") — used in reports.</summary>
        public string Label { get; }

        /// <summary>Mapping: vertex id → component id.</summary>
        public IReadOnlyDictionary<int, int> NodeToComponent { get; }

        /// <summary>Sizes of all components (number of vertices).</summary>
        public IReadOnlyList<int> Sizes { get; }

        /// <summary>Set of vertices belonging to the largest component.</summary>
        public IReadOnlyCollection<int> LargestComponentNodes { get; }

        public int Count => Sizes.Count;
        public int LargestSize => LargestComponentNodes.Count;

        public ComponentAnalysis(string label, IReadOnlyDictionary<int, int> nodeToComponent)
        {
            Label = label;
            NodeToComponent = nodeToComponent;

            var byComponent = nodeToComponent
                .GroupBy(kv => kv.Value)
                .ToList();

            Sizes = byComponent.Select(g => g.Count()).ToArray();

            LargestComponentNodes = byComponent.Count == 0
                ? Array.Empty<int>()
                : byComponent
                    .OrderByDescending(g => g.Count())
                    .First()
                    .Select(kv => kv.Key)
                    .ToHashSet();
        }
    }
}
