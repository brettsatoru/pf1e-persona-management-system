namespace Persona.Engine.Formulas;

public sealed record ParseResult(Formula? Formula, string? Error)
{
    public bool IsSuccess => Formula is not null;
}