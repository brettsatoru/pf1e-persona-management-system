using Persona.Engine.Formulas;

using Shouldly;

namespace Persona.Engine.Tests.Support;

internal static class FormulaAssertions
{
    public static Formula ParseOrFail(string text)
    {
        var parsed = Formula.Parse(text);
        parsed.Error.ShouldBeNull($"Expected '{text}' to parse");
        return parsed.Formula.ShouldNotBeNull();
    }
}
