using Persona.Engine.Formulas;
using Persona.Engine.Formulas.Syntax;
using Persona.Engine.Personas;
using Persona.Engine.Rules;

namespace Persona.Engine.Derived;

/// <summary>
/// Values derived from a persona's state and the table's rule parameters.
/// </summary>
public static class PersonaDerivations
{
    // TODO: Find a place for expected formula values to live
    // so that they can be documented properly for end users.
    private const string levelKeyword = "level";

    /// <summary>
    /// Gets the maximum number of agents a Persona qualifies for.
    /// </summary>
    /// <param name="s">The Persona</param>
    /// <param name="r">The Rules</param>
    /// <returns>The maximum number of agents, as an integer.</returns>
    public static int MaximumAgents(PersonaState s, RuleParameters r) => r.AgentsPerRank * s.Ranks.Total;

    /// <summary>
    /// Gets the available agents a Persona has by checking Committed and Missing values.
    /// </summary>
    /// <param name="s">The Persona</param>
    /// <param name="r">The Rules</param>
    /// <returns>The available agents value as an integer.</returns>
    public static int Available(PersonaState s, RuleParameters r) => MaximumAgents(s, r) - s.Committed - s.Missing;

    /// <summary>
    /// Checks the number of Operations a Persona can commit to per Persona Phase.
    /// </summary>
    /// <param name="s">The Persona</param>
    /// <param name="r">The Rules</param>
    /// <returns>The number of Operations a Persona can commit to.</returns>
    public static int OperationsPerPhase(PersonaState s, RuleParameters r) 
        => r.OperationsPerPhase + ((s.Ranks.Highest >= r.ExtraOperationMinRank) ? r.ExtraOperations : 0);

    /// <summary>
    /// Checks the required rank to perform an Operation.
    /// </summary>
    /// <param name="od">The Operation to check</param>
    /// <param name="r">The Rules</param>
    /// <returns>The minimum Facet rank needed to attempt the Operation.</returns>
    public static int RequiredRank(OperationDefinition od, RuleParameters r) => (od.Tier == OperationTier.Advanced) ? r.AdvancedTierRank : r.BasicTierRank;

    /// <summary>
    /// Gets the bonus feats to show to the player.
    /// </summary>
    /// <param name="s">The Persona</param>
    /// <param name="fd">The Facet Definitions</param>
    /// <param name="r">The Rules</param>
    /// <returns>A list of feats to show to the player at the table.</returns>
    public static IReadOnlyList<string> BonusFeatsShown(PersonaState s, IReadOnlyDictionary<Facet, FacetDefinition> fd, RuleParameters r)
    {
        List<string> bonusFeats = new List<string>();

        foreach (var facet in fd)
        {
            if (s.Ranks[facet.Key] >= r.BonusFeatMinRank)
            {
                // TODO: When player can make a choice include that choice as the index of the feat to display.
                var feat = facet.Value.BonusFeats.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(feat)) continue; // Skip a missing or blank feat.
                bonusFeats.Add(feat);
            }
        }

        return bonusFeats.AsReadOnly();
    }

    /// <summary>
    /// Checks for facet rank and total cap violations.
    /// </summary>
    /// <param name="s">The Persona</param>
    /// <param name="r">The Rules</param>
    /// <param name="o">Any already existing overrides to confirm against</param>
    /// <returns>Violations not covered by an override.</returns>
    public static IReadOnlyList<CapViolation> CheckCaps(PersonaState s, RuleParameters r, IReadOnlyCollection<CapOverride> o)
    {
        var variables = new Dictionary<string, decimal> {
            { "level", s.Level}
        };

        var (perFacetCap, pfcErr) = r.PerFacetCap.Evaluate(variables);
        var (totalCap, tcErr) = r.TotalCap.Evaluate(variables);

        if (perFacetCap == null || perFacetCap is not FormulaValue.Number pfcNum)
        {
            throw new InvalidOperationException($"Could not evaluate per facet caps:\n{(string.IsNullOrWhiteSpace(pfcErr) ? "Evaluated true/false" : pfcErr)}");
        }

        if (totalCap == null || totalCap is not FormulaValue.Number tcNum)
            throw new InvalidOperationException($"Could not evaluate total facet caps:\n{(string.IsNullOrWhiteSpace(tcErr) ? "Evaluated true/false" : tcErr)}");

        List<CapViolation> uncoveredViolations = new List<CapViolation>();

        // First, check per facet
        foreach (var (facet, rank) in s.Ranks.All)
        {
            if (rank > pfcNum.Value)
            {
                uncoveredViolations.Add(new FacetRankAboveCap(facet, rank, (int)pfcNum.Value, s.Level));
            }
        }

        // Next, check totals
        if (s.Ranks.Total > tcNum.Value)
        {
            uncoveredViolations.Add(new TotalRanksAboveCap(s.Ranks.Total, (int)tcNum.Value, s.Level));
        }

        return uncoveredViolations.Where(v => !o.Any(x => x.Violation == v)).ToList().AsReadOnly();
    }
}
