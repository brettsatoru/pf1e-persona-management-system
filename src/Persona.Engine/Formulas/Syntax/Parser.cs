namespace Persona.Engine.Formulas.Syntax;

internal sealed class Parser
{
    private const int MaxDepth = 64;

    private readonly List<Token> _tokens;
    private readonly HashSet<string> _variables = new(StringComparer.Ordinal);
    private int _pos;
    private int _depth;

    private Parser(List<Token> tokens) => _tokens = tokens;

    internal static (Expr?, IReadOnlySet<string>?, string?) Parse(string text)
    {
        var (tokens, err) = Tokenizer.GetTokens(text);
        if (err is not null || tokens is null)
            return (null, null, err ?? "No tokens could be produced.");

        var p = new Parser(tokens);
        try
        {
            var root = p.ParseComparison();
            p.Expect(TokenType.End, "end of formula");
            return (root, p._variables, null);
        }
        catch (FormulaSyntaxException ex)
        {
            return (null, null, ex.Message);
        }
    }

    // comparison := additive ( compOp additive )?
    private Expr ParseComparison()
    {
        var left = ParseAdditive();
        ComparisonOp? op = Peek.type switch
        {
            TokenType.Gt => ComparisonOp.GRTTHN,
            TokenType.GtEq => ComparisonOp.GRTEQ,
            TokenType.Lt => ComparisonOp.LSSTHN,
            TokenType.LtEq => ComparisonOp.LSSEQ,
            TokenType.Eq => ComparisonOp.EQL,
            TokenType.NotEq => ComparisonOp.NOTEQ,
            _ => null,
        };
        if (op is null) return left;
        _pos++;
        return new Expr.Comparison(op.Value, left, ParseAdditive());
        // No loop here, so "a < b < c" ends up failing the End check.
    }

    // additive := term ( (+|-) term )*
    private Expr ParseAdditive()
    {
        var left = ParseTerm();
        while (Peek.type is TokenType.Plus or TokenType.Minus)
        {
            var op = Advance().type == TokenType.Plus ? BinaryOp.ADD : BinaryOp.SUB;
            left = new Expr.Arithmetic(op, left, ParseTerm());
        }
        return left;
    }

    // term := unary ( (*|/) unary )*
    private Expr ParseTerm()
    {
        var left = ParseUnary();
        while (Peek.type is TokenType.Star or TokenType.Slash)
        {
            var op = Advance().type == TokenType.Star ? BinaryOp.MULT : BinaryOp.DIV;
            left = new Expr.Arithmetic(op, left, ParseUnary());
        }
        return left;
    }

    // unary := '-' unary | primary
    private Expr ParseUnary()
    {
        using var _ = Enter();
        if (Match(TokenType.Minus))
            return new Expr.Negate(ParseUnary());
        return ParsePrimary();
    }

    private Expr ParsePrimary()
    {
        var t = Advance();
        switch (t.type)
        {
            case TokenType.Number:
                return new Expr.Number(t.Number);

            case TokenType.Identifier:
                if (Peek.type == TokenType.OPara)
                    throw Fail($"Unknown function '{t.Text}' at position {t.Position}.");
                _variables.Add(t.Text!);
                return new Expr.Variable(t.Text!);

            case TokenType.OPara:
                {
                    var inner = ParseComparison();
                    Expect(TokenType.CPara, "')'");
                    return inner;
                }

            case TokenType.Min or TokenType.Max:
                {
                    Expect(TokenType.OPara, "'('");
                    var a = ParseComparison();
                    Expect(TokenType.Comma, "','");
                    var b = ParseComparison();
                    Expect(TokenType.CPara, "')'");
                    return t.type == TokenType.Min ? new Expr.Min(a, b) : new Expr.Max(a, b);
                }

            case TokenType.Floor:
                {
                    Expect(TokenType.OPara, "'('");
                    var a = ParseComparison();
                    Expect(TokenType.CPara, "')'");
                    return new Expr.Floor(a);
                }

            case TokenType.Has:
                {
                    Expect(TokenType.OPara, "'('");
                    var id = Expect(TokenType.Identifier, "a variable name");
                    Expect(TokenType.CPara, "')'");
                    return new Expr.Has(id.Text!);   // deliberately NOT added to _variables
                }

            default:
                throw Fail($"Unexpected {Describe(t)} at position {t.Position}.");
        }
    }

    // ---- helpers ----
    private Token Peek => _tokens[_pos];

    private Token Advance()
    {
        var t = _tokens[_pos];
        if (t.type != TokenType.End) _pos++;
        return t;
    }

    private bool Match(TokenType type)
    {
        if (Peek.type != type) return false;
        _pos++;
        return true;
    }

    private Token Expect(TokenType type, string what)
    {
        if (Peek.type != type)
            throw Fail($"Expected {what} but found {Describe(Peek)} at position {Peek.Position}.");
        return Advance();
    }

    private static string Describe(Token t) => t.type switch
    {
        TokenType.End => "end of formula",
        TokenType.Number => $"number {t.Number}",
        TokenType.Identifier => $"'{t.Text}'",
        _ => t.type.ToString(),
    };

    private DepthScope Enter()
    {
        if (++_depth > MaxDepth)
            throw Fail($"Formula is nested too deeply (limit {MaxDepth}).");
        return new DepthScope(this);
    }

    private readonly struct DepthScope(Parser p) : IDisposable
    {
        public void Dispose() => p._depth--;
    }

    private static FormulaSyntaxException Fail(string message) => new(message);

    private sealed class FormulaSyntaxException(string message) : Exception(message);
}