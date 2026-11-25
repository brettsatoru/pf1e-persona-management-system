namespace Persona.Engine.Personas;

public sealed record ActiveOperation(Guid Id, string OperationId, Facet Facet, int AgentsCommitted, OperationStatus Status);
