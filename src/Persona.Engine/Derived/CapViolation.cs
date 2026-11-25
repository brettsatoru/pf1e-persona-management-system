using Persona.Engine.Personas;

namespace Persona.Engine.Derived;

// Violation Handling
public abstract record CapViolation(int Level);
public sealed record FacetRankAboveCap(Facet Facet, int Rank, int Cap, int Level) : CapViolation(Level);
public sealed record TotalRanksAboveCap(int Total, int Cap, int Level) : CapViolation(Level);