public class CondensationGraph
{
    public Dictionary<int, HashSet<int>> Adjacency { get; set; } = new();
    public int V => Adjacency.Count;
}