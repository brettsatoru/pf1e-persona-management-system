using Persona.Engine.Formulas.Syntax;

namespace Persona.Engine.Formulas;

public enum FormulaType { Number, Boolean }

public sealed class Formula
{
    private const int MAX_SOURCE_LENGTH = 2048;
    private readonly Expr _treeRoot;

    public string Source { get; }
    public IReadOnlySet<string> ExprVariables { get; }
    public FormulaType ResultType { get; }

    private Formula(string source, Expr root, IReadOnlySet<string> variables, FormulaType typeOf)
    {
        _treeRoot = root;
        Source = source;
        ExprVariables = variables;
        ResultType = typeOf;
    }

    /// <summary>
    /// Parses text based on lexical requirements and returns a ParseResult.  This should never throw.
    /// </summary>
    /// <param name="text">Text to attempt to parse</param>
    /// <returns>A ParseResult</returns>
    public static ParseResult Parse(string text)
    {
        // Check for empty string, can't parse nothing.
        if (string.IsNullOrEmpty(text)) return new ParseResult(null, "No expression provided, cannot parse");

        text = text.Trim(); // White space on either side doesn't matter, so remove it.

        if (text.Length > MAX_SOURCE_LENGTH)
        {
            return new ParseResult(null, $"Formula is longer than {MAX_SOURCE_LENGTH} characters.");
        }

        // Parse for Tokens
        var (exprRoot, parsedVariables, formulaType, error) = Parser.Parse(text);
        if (exprRoot != null && parsedVariables != null && formulaType != null)
        {
            return new ParseResult(new Formula(text, exprRoot, parsedVariables, formulaType.Value), error);
        }
        else
        {
            return new ParseResult(null, error);
        }
    }

    public EvaluationResult Evaluate(IReadOnlyDictionary<string, decimal> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        List<string> missingVariables = new List<string>();
        foreach (var symbol in this.ExprVariables)
        {
            if (!variables.ContainsKey(symbol))
            {
                missingVariables.Add(symbol);
            }
        }
        if (missingVariables.Count > 0)
        {
            string mVars = string.Join("\n", missingVariables);
            return Error("Missing Variable Error", $"Cannot evaluate the formula due to missing variables:\n{mVars}");
        }

        // Evaluate the tree using the variables provided.
        try
        {
            var (dRes, bRes) = _treeRoot.Eval(variables);
            if (dRes != null)
            {
                return new EvaluationResult(new FormulaValue.Number(dRes.Value), null);
            }
            else if (bRes != null)
            {
                return new EvaluationResult(new FormulaValue.Boolean(bRes.Value), null);
            }
        }
        catch (DivideByZeroException dbzEx)
        {
            return Error("Divide By Zero Error", dbzEx.Message);
        }
        catch (OverflowException ofEx)
        {
            return Error("Overflow Error", ofEx.Message);
        }
        catch (KeyNotFoundException knfEx)
        {
            return Error("Key Not Found Error", knfEx.Message);
        }

        return new EvaluationResult(null, $"Could not evaluate a valid result from the formula {Source}");
    }

    public override string ToString()
    {
        return Source;
    }

    private EvaluationResult Error(string kind, string message) =>
        new(null, $"{kind}: {message}\nFormula: {Source}");
}
