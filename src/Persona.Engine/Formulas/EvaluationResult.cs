using Persona.Engine.Formulas.Syntax;

namespace Persona.Engine.Formulas;

public sealed record EvaluationResult(FormulaValue? Value, string? Error)
{
    public bool IsSuccess => Value is not null;
}