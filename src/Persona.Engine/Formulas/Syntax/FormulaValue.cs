namespace Persona.Engine.Formulas.Syntax;

public abstract record FormulaValue
{
    public sealed record Number(decimal Value) : FormulaValue;
    public sealed record Boolean(bool Value) : FormulaValue;
}