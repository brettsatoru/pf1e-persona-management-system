namespace Persona.Engine.Personas;

// At the moment this is a basic shell needed by the DV test suite.
// Will be filled in later.


/// <summary>
/// A snapshot of a persona's state at one point in time.
/// </summary>
public sealed record PersonaState
{
    public int Level { get; }
    public FacetRanks Ranks { get; }
    public int Missing { get; }
    public IReadOnlyList<ActiveOperation> Operations { get; }

    public PersonaState(int level, FacetRanks ranks, int missing,
        IReadOnlyList<ActiveOperation> operations)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(missing);
        ArgumentNullException.ThrowIfNull(ranks);
        ArgumentNullException.ThrowIfNull(operations);

        Level = level;
        Ranks = ranks;
        Missing = missing;
        Operations = operations;
    }

    // Facts about the state alone; no rule-set needed.
    public int Committed => Operations.Sum(o => o.AgentsCommitted);
}
