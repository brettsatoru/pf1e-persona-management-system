using Persona.Engine.Formulas;
using Persona.Engine.Formulas.Syntax;

using Shouldly;

namespace Persona.Engine.Tests.FormulaEvaluator;

public class FormulaEvaluatorTests
{
    private static readonly IReadOnlyDictionary<string, decimal> NoVariables =
        new Dictionary<string, decimal>();

    private static Formula ParseOrFail(string text)
    {
        var parsed = Formula.Parse(text);
        parsed.Error.ShouldBeNull();
        return parsed.Formula.ShouldNotBeNull();
    }

    [Fact]
    [Trait("TestId", "FE-01")]
    public void Evaluate_ArithmeticWithParentheses_RespectsPrecedence()
    {
        var formula = ParseOrFail("15 + 2 * (3 + 1)");

        var result = formula.Evaluate(NoVariables);

        result.Value.ShouldBe(new FormulaValue.Number(23));
    }

    [Theory]
    [Trait("TestId", "FE-02")]
    [InlineData("min(10 + 12, 20)", 20)]
    [InlineData("max(0, -3)", 0)]
    public void Evaluate_MinAndMax_ReturnsSmallerAndLarger(string text, int expected)
    {
        var formula = ParseOrFail(text);

        var result = formula.Evaluate(NoVariables);

        result.Value.ShouldBe(new FormulaValue.Number(expected));
    }

    [Theory]
    [Trait("TestId", "FE-03")]
    [InlineData("floor(7 / 5)", 1)]
    [InlineData("floor(4 / 5)", 0)]
    [InlineData("floor(-4 / 5)", -1)]
    public void Evaluate_Floor_RoundsDown(string text, int expected)
    {
        var formula = ParseOrFail(text);

        var result = formula.Evaluate(NoVariables);

        result.Value.ShouldBe(new FormulaValue.Number(expected));
    }

    [Theory]
    [Trait("TestId", "FE-04")]
    [InlineData(-4, 2)]
    [InlineData(5, 3)]
    [InlineData(15, 5)]
    public void Evaluate_MarginFormula_HasFloorOfTwo(int margin, int expected)
    {
        var formula = ParseOrFail("2 + max(0, floor(margin / 5))");
        var variables = new Dictionary<string, decimal> { ["margin"] = margin };

        var result = formula.Evaluate(variables);

        result.Value.ShouldBe(new FormulaValue.Number(expected));
    }

    [Theory]
    [Trait("TestId", "FE-05")]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void Evaluate_Comparison_ReturnsBoolean(int missingAgents, bool expected)
    {
        var formula = ParseOrFail("missingAgents > 0");
        var variables = new Dictionary<string, decimal> { ["missingAgents"] = missingAgents };

        var result = formula.Evaluate(variables);

        result.Value.ShouldBe(new FormulaValue.Boolean(expected));
    }

    [Theory]
    [Trait("TestId", "FE-05")]
    [InlineData(true)]
    [InlineData(false)]
    public void Evaluate_NullCheck_TrueOnlyWhenSupplied(bool supplied)
    {
        var formula = ParseOrFail("has(targetRank)");
        var variables = supplied
            ? new Dictionary<string, decimal> { ["targetRank"] = 3 }
            : new Dictionary<string, decimal>();

        var result = formula.Evaluate(variables);

        result.Value.ShouldBe(new FormulaValue.Boolean(supplied));
    }

    [Fact]
    [Trait("TestId", "FE-06")]
    public void Evaluate_NamedVariables_ResolveFromContext()
    {
        var formula = ParseOrFail("committedAgents + targetRank");
        var variables = new Dictionary<string, decimal>
        {
            ["committedAgents"] = 3,
            ["targetRank"] = 4,
        };

        var result = formula.Evaluate(variables);

        result.Value.ShouldBe(new FormulaValue.Number(7));
    }

    [Fact]
    [Trait("TestId", "FE-07")]
    public void Evaluate_UnknownVariable_ReturnsErrorNamingIt()
    {
        var formula = ParseOrFail("rank + 1");

        var result = formula.Evaluate(NoVariables);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull().ShouldContain("rank");
    }

    [Fact]
    [Trait("TestId", "FE-07")]
    public void Evaluate_SeveralUnknownVariables_ListedInSortedOrder()
    {
        var formula = ParseOrFail("zeta + alpha");

        var result = formula.Evaluate(NoVariables);

        result.IsSuccess.ShouldBeFalse();
        var lines = result.Error.ShouldNotBeNull()
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        lines.Skip(1).ShouldBe(new[] { "alpha", "zeta" });
    }

    [Theory]
    [Trait("TestId", "FE-08")]
    [InlineData("Math.Max(1, 2)")]
    [InlineData("agents.Count")]
    [InlineData("x = 1")]
    [InlineData("x += 1")]
    [InlineData("foo(1)")]
    [InlineData("File.Delete(\"x\")")]
    public void Parse_DisallowedConstruct_IsRejected(string text)
    {
        var parsed = Should.NotThrow(() => Formula.Parse(text));

        parsed.IsSuccess.ShouldBeFalse();
        parsed.Error.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [Trait("TestId", "FE-09")]
    [InlineData("1 / 0")]
    [InlineData("x / y")]
    public void Evaluate_DivisionByZero_ReturnsError(string text)
    {
        var formula = ParseOrFail(text);
        var variables = new Dictionary<string, decimal> { ["x"] = 1, ["y"] = 0 };

        var result = Should.NotThrow(() => formula.Evaluate(variables));

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [Trait("TestId", "FE-09")]
    [InlineData("79228162514264337593543950335 * 2", "Overflow")]
    [InlineData("79228162514264337593543950335 + 1", "Overflow")]
    [InlineData("1 / 0", "Divide By Zero")]
    public void Evaluate_ArithmeticFault_ReturnsLabelledError(string text, string expectedLabel)
    {
        var formula = ParseOrFail(text);

        var result = Should.NotThrow(() => formula.Evaluate(NoVariables));

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull().ShouldContain(expectedLabel);
    }

    [Theory]
    [Trait("TestId", "FE-10")]
    [InlineData("42")]
    [InlineData("1.5")]
    [InlineData("-3")]
    [InlineData("-x")]
    [InlineData("x + y")]
    [InlineData("x - y")]
    [InlineData("x * y")]
    [InlineData("x / y")]
    [InlineData("(x)")]
    [InlineData("((x + 1) * 2)")]
    [InlineData("x > y")]
    [InlineData("x >= y")]
    [InlineData("x < y")]
    [InlineData("x <= y")]
    [InlineData("x == y")]
    [InlineData("x != y")]
    [InlineData("min(x, y)")]
    [InlineData("max(x, y)")]
    [InlineData("floor(x)")]
    [InlineData("has(x)")]
    [InlineData("  x+y  ")]
    [InlineData("min(max(0, floor(x / 5)), y)")]
    public void Parse_SupportedConstruct_Succeeds(string text)
    {
        var parsed = Should.NotThrow(() => Formula.Parse(text));

        parsed.Error.ShouldBeNull();
        parsed.IsSuccess.ShouldBeTrue();
        parsed.Formula.ShouldNotBeNull();
    }

    [Theory]
    [Trait("TestId", "FE-11")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1 +")]
    [InlineData("* 2")]
    [InlineData("(1 + 2")]
    [InlineData("1 + 2)")]
    [InlineData("1 2")]
    [InlineData("x y")]
    [InlineData("min(1)")]
    [InlineData("min(1, 2, 3)")]
    [InlineData("floor()")]
    [InlineData("floor(1, 2)")]
    [InlineData("has(1)")]
    [InlineData("has(x + 1)")]
    [InlineData("1 # 2")]
    [InlineData("x; y")]
    [InlineData("\"text\"")]
    [InlineData("x ? 1 : 2")]
    [InlineData("x && y")]
    [InlineData("!x")]
    [InlineData("+x")]
    [InlineData("1e5")]
    [InlineData("0x10")]
    [InlineData(".5")]
    public void Parse_MalformedSyntax_IsRejected(string text)
    {
        var parsed = Should.NotThrow(() => Formula.Parse(text));

        parsed.IsSuccess.ShouldBeFalse();
        parsed.Error.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("TestId", "FE-11")]
    public void Parse_NullText_IsRejected()
    {
        var parsed = Should.NotThrow(() => Formula.Parse(null!));

        parsed.IsSuccess.ShouldBeFalse();
        parsed.Error.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("TestId", "FE-12")]
    public void Evaluate_NullVariables_Throws()
    {
        var formula = ParseOrFail("1 + 1");

        Should.Throw<ArgumentNullException>(() => formula.Evaluate(null!));
    }
}