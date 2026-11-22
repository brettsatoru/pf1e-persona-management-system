using System.Globalization;

namespace Persona.Engine.Formulas.Syntax;

internal enum TokenType
{
    Number, Identifier,
    // Unary Operators
    Plus, Minus, Star, Slash,
    // Functions
    Min, Max, Floor, Has,
    // Comparisons
    Eq, NotEq, Gt, GtEq, Lt, LtEq,
    // Container
    OPara, CPara, Comma,
    End
}

internal readonly record struct Token(TokenType type, int Position, string? Text = null, decimal Number = 0);

internal class Tokenizer
{
    private static readonly Dictionary<string, TokenType> Keywords = new(StringComparer.Ordinal)
    {
        ["min"] = TokenType.Min,
        ["max"] = TokenType.Max,
        ["floor"] = TokenType.Floor,
        ["has"] = TokenType.Has
    };

    /// <summary>
    /// Get tokens from a raw string.  Returns a list of token or an error if a malformed token is detected.
    /// </summary>
    /// <param name="text">The raw string</param>
    /// <returns>List of Tokens or an error string.</returns>
    internal static (List<Token>?, string?) GetTokens(string text)
    {
        var tokens = new List<Token>();
        int i = 0;

        while (i < text.Length)
        {
            char c = text[i];
            int start = i;

            // If whitespace, skip
            if (char.IsWhiteSpace(c)) 
            {
                i++;
                continue;
            }
            // If a digit, build a number
            else if (char.IsAsciiDigit(c))
            {
                while (i < text.Length && char.IsAsciiDigit(text[i])) i++;
                if (i < text.Length && text[i] == '.')
                {
                    i++;
                    if (i >= text.Length || !char.IsAsciiDigit(text[i]))
                        return (null, $"Expected digits after'.' at position {i}.");
                    while (i < text.Length && char.IsAsciiDigit(text[i])) i++;
                }
                if (i < text.Length && (char.IsAsciiLetterOrDigit(text[i]) || text[i] == '_'))
                    return (null, $"Malformed number at position {start}.");

                if (!decimal.TryParse(text.AsSpan(start, i - start), System.Globalization.NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var value))
                    return (null, $"Number cannot be parsed at {start}.");

                tokens.Add(new Token(TokenType.Number, start, Number: value));
                continue;
            }
            // If a letter, build an identifier or a function keyword token.
            else if (char.IsAsciiLetter(c) || c == '_')
            {
                while (i < text.Length && (char.IsAsciiLetterOrDigit(text[i]) || text[i] == '_')) i++;
                string word = text[start..i];
                tokens.Add(Keywords.TryGetValue(word, out var kw)
                    ? new Token(kw, start, word)
                    : new Token(TokenType.Identifier, start, word));
                continue;
            }


            // If we fall through to this point, attempt to check for operators.
            char next = i + 1 < text.Length ? text[i + 1] : '\0';
            (TokenType type, int len)? op = c switch
            {
                '+' => (TokenType.Plus, 1),
                '-' => (TokenType.Minus, 1),
                '*' => (TokenType.Star, 1),
                '/' => (TokenType.Slash, 1),
                '(' => (TokenType.OPara, 1),
                ')' => (TokenType.CPara, 1),
                ',' => (TokenType.Comma, 1),
                '>' => next == '=' ? (TokenType.GtEq, 2) : (TokenType.Gt, 1),
                '<' => next == '=' ? (TokenType.LtEq, 2) : (TokenType.Lt, 1),
                '=' when next == '=' => (TokenType.Eq, 2),
                '!' when next == '=' => (TokenType.NotEq, 2),
                _ => null,
            };

            if (op is null)
                return (null, $"Unexpected character `{c}' at position {start}.");

            tokens.Add(new Token(op.Value.type, start));
            i += op.Value.len;
        }

        // When everything is done being tokenized, add a token to confirm the end of the line.
        tokens.Add(new Token(TokenType.End, text.Length));
        return (tokens, null);
    }
}
