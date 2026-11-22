using System.Text;

using Persona.Engine.Formulas.Syntax;

namespace Persona.Engine.Formulas;

public sealed class Formula
{
    private readonly Expr _treeRoot;

    public string Source { get; }
    public IReadOnlySet<string> ExprVariables { get; }

    private Formula(string source, Expr root, IReadOnlySet<string> variables)
    {
        _treeRoot = root;
        Source = source;
        ExprVariables = variables;
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

        // Parse for Tokens
        var (exprRoot, parsedVariables, error) = Parser.Parse(text);
        if (exprRoot != null && parsedVariables != null)
        {
            return new ParseResult(new Formula(text, exprRoot, parsedVariables), error);
        }
        else
        {
            return new ParseResult(null, error);
        }
    }

    public EvaluationResult Evaluate(IReadOnlyDictionary<string, decimal> variables)
    {
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
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Cannot evaluate the formula " + Source + " due to missing variable definitions:");
            foreach (var m in missingVariables)
            {
                sb.AppendLine($"{m}");
            }
            return new EvaluationResult(null, sb.ToString());
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
        catch (Exception ex)
        {
            return new EvaluationResult(null, ex.Message);
        }

        return new EvaluationResult(null, "Could not evaluate a valid result from the formula");
    }

    public override string ToString()
    {
        return Source;
    }
}