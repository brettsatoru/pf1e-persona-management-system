using Persona.Engine.Personas;
using Persona.Engine.Rules;

namespace Persona.Engine.Tests.Support;

/// <summary>
/// Rule values used by tests.  Kept in the test project so src/ ships no book content.
/// </summary>
internal static class TestRuleSets
{
    public static RuleParameters AsWritten { get; } = new(
        AgentsPerRank: 2,
        OperationsPerPhase: 1,
        BasicTierRank: 2,
        AdvancedTierRank: 6,
        PerFacetCap: FormulaAssertions.ParseOrFail("level"),
        TotalCap: FormulaAssertions.ParseOrFail("level * 2"),
        BonusFeatMinRank: 5,
        ExtraOperationMinRank: 10,
        ExtraOperations: 1
        );


    public static IReadOnlyDictionary<Facet, FacetDefinition> ExampleFacets { get; } =
        Enum.GetValues<Facet>().ToDictionary(
            f => f,
            f => new FacetDefinition(
                Skills: [$"Example {f} Skill"],
                BonusFeats: [$"Example {f} Feat", $"Alternate {f} Feat"]));
}
