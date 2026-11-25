using Persona.Engine.Formulas;

namespace Persona.Engine.Rules;

public sealed record RuleParameters(
    int AgentsPerRank,
    int OperationsPerPhase,
    int BasicTierRank, int AdvancedTierRank,
    Formula PerFacetCap, Formula TotalCap,
    int BonusFeatMinRank,
    int ExtraOperationMinRank, int ExtraOperations
    );
