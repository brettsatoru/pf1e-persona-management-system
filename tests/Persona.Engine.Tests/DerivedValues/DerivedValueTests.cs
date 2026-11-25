using Persona.Engine.Derived;
using Persona.Engine.Personas;
using Persona.Engine.Rules;
using Persona.Engine.Tests.Support;

using Shouldly;

namespace Persona.Engine.Tests.DerivedValues;

public class DerivedValueTests
{

    private static readonly RuleParameters Rules = TestRuleSets.AsWritten;
    private static readonly IReadOnlyCollection<CapOverride> NoOverrides = [];

    private static PersonaStateBuilder Persona() => new();

    // ---- DV-01 -------------------------------------------------------------

    [Theory]
    [Trait("TestId", "DV-01")]
    [InlineData(2, 0, 0, 4)]
    [InlineData(2, 1, 0, 6)]
    [InlineData(4, 3, 3, 20)]

    public void MaximumAgents_TotalRanks_IsAgentsPerRankTimesTotal(
        int charm, int heroism, int genius, int expected)
    {
        var state = Persona()
            .WithRank(Facet.Charm, charm)
            .WithRank(Facet.Heroism, heroism)
            .WithRank(Facet.Genius, genius)
            .Build();

        var maximumAgents = PersonaDerivations.MaximumAgents(state, Rules);

        maximumAgents.ShouldBe(expected);
    }


    // ---- DV-02 -------------------------------------------------------------

    [Fact]
    [Trait("TestId", "DV-02")]
    public void CheckCaps_FacetRankAtLevel_NoViolation()
    {
        var state = Persona().AtLevel(5).WithRank(Facet.Charm, 5).Build();

        var violations = PersonaDerivations.CheckCaps(state, Rules, NoOverrides);

        violations.ShouldBeEmpty();
    }

    [Fact]
    [Trait("TestId", "DV-02")]
    public void CheckCaps_FacetRankAboveLevel_ReportsFacetViolation()
    {
        var state = Persona().AtLevel(5).WithRank(Facet.Charm, 6).Build();

        var violations = PersonaDerivations.CheckCaps(state, Rules, NoOverrides);

        violations.ShouldBe([new FacetRankAboveCap(Facet.Charm, Rank: 6, Cap: 5, Level: 5)]);
    }

    [Fact]
    [Trait("TestId", "DV-02")]
    public void CheckCaps_BooleanCapFormula_Throws()
    {
        var rules = Rules with { PerFacetCap = FormulaAssertions.ParseOrFail("level > 1") };
        var state = Persona().AtLevel(5).Build();

        Should.Throw<InvalidOperationException>(() => PersonaDerivations.CheckCaps(state, rules, NoOverrides));
    }

    // ---- DV-03 -------------------------------------------------------------

    [Fact]
    [Trait("TestId", "DV-03")]
    public void CheckCaps_TotalRanksAgainstLevel_NoViolation()
    {
        var state = Persona().AtLevel(5)
            .WithRank(Facet.Charm, 5)
            .WithRank(Facet.Heroism, 5)
            .Build();

        var violations = PersonaDerivations.CheckCaps(state, Rules, NoOverrides);

        violations.ShouldBeEmpty();
    }

    [Fact]
    [Trait("TestId", "DV-03")]
    public void CheckCaps_TotalRanksAboveTwiceLevel_ReportsTotalViolation()
    {
        var state = Persona().AtLevel(5)
            .WithRank(Facet.Charm, 5)
            .WithRank(Facet.Heroism, 5)
            .WithRank(Facet.Genius, 1)
            .Build();

        var violations = PersonaDerivations.CheckCaps(state, Rules, NoOverrides);

        violations.ShouldBe([new TotalRanksAboveCap(Total: 11, Cap: 10, Level: 5)]);
    }

    // ---- DV-04 -------------------------------------------------------------

    [Fact]
    [Trait("TestId", "DV-04")]
    public void CheckCaps_MatchingOverrideWithNote_SuppressesThatViolation()
    {
        var state = Persona().AtLevel(5).WithRank(Facet.Charm, 6).Build();
        var violation = new FacetRankAboveCap(Facet.Charm, Rank: 6, Cap: 5, Level: 5);
        var approved = CapOverride.Create(violation, "Granted for backstory").Override.ShouldNotBeNull();

        var violations = PersonaDerivations.CheckCaps(state, Rules, [approved]);

        violations.ShouldBeEmpty();
    }

    [Fact]
    [Trait("TestId", "DV-04")]
    public void CheckCaps_OverrideForDifferentViolation_StillReportsViolation()
    {
        // Approved at rank 6; the Facet has since risen to 7, which is a new violation.
        var state = Persona().AtLevel(5).WithRank(Facet.Charm, 7).Build();
        var stale = new FacetRankAboveCap(Facet.Charm, Rank: 6, Cap: 5, Level: 5);
        var approved = CapOverride.Create(stale, "Granted for backstory").Override.ShouldNotBeNull();

        var violations = PersonaDerivations.CheckCaps(state, Rules, [approved]);

        violations.ShouldBe([new FacetRankAboveCap(Facet.Charm, Rank: 7, Cap: 5, Level: 5)]);
    }

    [Theory]
    [Trait("TestId", "DV-04")]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankNote_ReturnsError(string note)
    {
        var violation = new FacetRankAboveCap(Facet.Charm, Rank: 6, Cap: 5, Level: 5);

        var result = CapOverride.Create(violation, note);

        result.Override.ShouldBeNull();
        result.Error.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [Trait("TestId", "DV-04")]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankNote_Throws(string note)
    {
        var violation = new FacetRankAboveCap(Facet.Charm, Rank: 6, Cap: 5, Level: 5);

        Should.Throw<ArgumentException>(() => new CapOverride(violation, note));
    }

    // ---- DV-05 -------------------------------------------------------------

    [Fact]
    [Trait("TestId", "DV-05")]
    public void CheckCaps_LevelLoweredBelowRanks_FlagsWithoutChangingRanks()
    {
        var builder = Persona().AtLevel(6).WithRank(Facet.Charm, 6);
        PersonaDerivations.CheckCaps(builder.Build(), Rules, NoOverrides).ShouldBeEmpty();

        var lowered = builder.AtLevel(5).Build();
        var violations = PersonaDerivations.CheckCaps(lowered, Rules, NoOverrides);

        lowered.Ranks[Facet.Charm].ShouldBe(6);
        violations.ShouldContain(new FacetRankAboveCap(Facet.Charm, Rank: 6, Cap: 5, Level: 5));
    }

    // ---- DV-06 -------------------------------------------------------------

    [Theory]
    [Trait("TestId", "DV-06")]
    [InlineData(9, 0, 1)]
    [InlineData(10, 0, 2)]
    [InlineData(10, 10, 2)]
    public void OperationsPerPhase_HighestFacetRank_AddsExtraAtThreshold(int charm, int heroism, int expected)
    {
        var state = Persona()
            .WithRank(Facet.Charm, charm)
            .WithRank(Facet.Heroism, heroism)
            .Build();

        var operations = PersonaDerivations.OperationsPerPhase(state, Rules);

        operations.ShouldBe(expected);
    }

    // ---- DV-07 -------------------------------------------------------------

    [Fact]
    [Trait("TestId", "DV-07")]
    public void AvailableAgents_WithCommittedAndMissing_IsMaximumMinusBoth()
    {
        var state = Persona()
            .WithRank(Facet.Charm, 4)   // maximum 8
            .WithOperation(agents: 3)
            .WithMissing(2)
            .Build();

        var available = PersonaDerivations.Available(state, Rules);

        available.ShouldBe(3);
    }

    // ---- DV-08 -------------------------------------------------------------

    [Fact]
    [Trait("TestId", "DV-08")]
    public void CommittedAgents_NoOperations_IsZero()
    {
        var state = Persona().WithRank(Facet.Charm, 2).Build();

        state.Committed.ShouldBe(0);
    }

    [Fact]
    [Trait("TestId", "DV-08")]
    public void CommittedAgents_ResolvedAndPersistent_SumsBoth()
    {
        var state = Persona()
            .WithRank(Facet.Charm, 4)
            .WithOperation(agents: 2, status: OperationStatus.ResolvedThisPhase)
            .WithOperation(agents: 3, status: OperationStatus.Persistent)
            .Build();

        state.Committed.ShouldBe(5);
    }

    // ---- DV-09 -------------------------------------------------------------

    [Theory]
    [Trait("TestId", "DV-09")]
    [InlineData(null, 2)]
    [InlineData(OperationTier.Basic, 2)]
    [InlineData(OperationTier.Advanced, 6)]
    public void RequiredRank_OperationTier_UsesTierThreshold(OperationTier? tier, int expected)
    {
        var operation = new OperationDefinition("example-operation", tier);

        var required = PersonaDerivations.RequiredRank(operation, Rules);

        required.ShouldBe(expected);
    }

    // ---- DV-10 -------------------------------------------------------------

    [Theory]
    [Trait("TestId", "DV-10")]
    [InlineData(4, 0, new string[] { })]
    [InlineData(5, 0, new[] { "Example Charm Feat" })]
    [InlineData(5, 6, new[] { "Example Charm Feat", "Example Heroism Feat" })]
    public void BonusFeatsShown_FacetRanks_ShowsFirstFeatPerFacetAtMinRank(
        int charm, int heroism, string[] expected)
    {
        var state = Persona()
            .WithRank(Facet.Charm, charm)
            .WithRank(Facet.Heroism, heroism)
            .Build();

        var feats = PersonaDerivations.BonusFeatsShown(state, TestRuleSets.ExampleFacets, Rules);

        feats.ShouldBe(expected, ignoreOrder: true);
    }
}
