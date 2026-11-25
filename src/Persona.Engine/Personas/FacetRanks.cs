namespace Persona.Engine.Personas;

public sealed class FacetRanks : IEquatable<FacetRanks>
{
    private readonly int[] _ranks = new int[Enum.GetValues<Facet>().Length]; // index by (int)Facet

    public static FacetRanks Empty { get; } = new();

    private FacetRanks()
    {
        // Empty for now, may need more later.
    }

    public int this[Facet facet] => _ranks[(int)facet];
    public int Total => _ranks.Sum();
    public int Highest => _ranks.Max();

    public FacetRanks With(Facet facet, int rank)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rank);
        var copy = new FacetRanks();
        _ranks.CopyTo(copy._ranks, 0);
        copy._ranks[(int)facet] = rank;
        return copy;
    }

    public IEnumerable<(Facet facet, int rank)> All =>
        Enum.GetValues<Facet>().Select(f => (f, this[f]));

    public bool Equals(FacetRanks? other) =>
        other is not null && _ranks.AsSpan().SequenceEqual(other._ranks);

    public override bool Equals(object? obj) => Equals(obj as FacetRanks);

    // So it's easier to compare for difference in state.
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var r in _ranks) hash.Add(r);
        return hash.ToHashCode();
    }
}
