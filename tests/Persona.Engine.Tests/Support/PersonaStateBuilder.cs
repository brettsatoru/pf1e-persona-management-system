using Persona.Engine.Personas;

namespace Persona.Engine.Tests.Support;

/// <summary>
/// Arrange-step builder, Builds state directly for now, once events exist it will
/// replay them instead, without changing the tests that use it.
/// </summary>
internal class PersonaStateBuilder
{
    // Pathfinder's maximum level (not really), so the caps never fire unless a test sets the level.
    private int _level = 20;
    private FacetRanks _ranks = FacetRanks.Empty;
    private int _missing;
    private readonly List<ActiveOperation> _operations = [];

    public PersonaStateBuilder AtLevel(int level) {  _level = level; return this; }

    public PersonaStateBuilder WithRank(Facet facet, int rank)
    {
        _ranks = _ranks.With(facet, rank);
        return this;
    }

    public PersonaStateBuilder WithMissing(int missing) { _missing = missing; return this; }

    public PersonaStateBuilder WithOperation(
        int agents,
        OperationStatus status = OperationStatus.ResolvedThisPhase,
        Facet facet = Facet.Charm,
        string operationId = "example-operation")
    {
        _operations.Add(new ActiveOperation(Guid.NewGuid(), operationId, facet, agents, status));
        return this;
    }

    public PersonaState Build() => new(_level, _ranks, _missing, _operations.ToArray());
}
