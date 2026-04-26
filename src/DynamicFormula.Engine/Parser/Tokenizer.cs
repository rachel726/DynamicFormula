// ╔══════════════════════════════════════════════════════════════════╗
// ║  Tokenizer.cs                                                     ║
// ║  ────────────────────────────────────────────────                ║
// ║  מפרק נוסחה טקסטואלית לרצף טוקנים (Lexical Analysis)            ║
// ║                                                                  ║
// ║  דוגמה:  "sqrt(a^2 + b)"  →                                     ║
// ║          [sqrt] [(] [a] [^] [2] [+] [b] [)]                      ║
// ║                                                                  ║
// ║  זהו הלב של מפרש שנבנה ידנית — מרשים מאוד 👑                    ║
// ╚══════════════════════════════════════════════════════════════════╝
namespace DynamicFormula.Engine.Parser
{
    public enum TokenType
    {
        Number,        // 3.14
        Variable,      // a, b, c, d
        Function,      // sqrt, log, abs, power
        Operator,      // + - * / ^
        Comparison,    // > < >= <= == !=
        LeftParen,     // (
        RightParen,    // )
        Comma,         // ,
        End
    }

    public readonly record struct Token(TokenType Type, string Value);

    /// <summary>
    /// מפרק ביטוי מתמטי לרצף טוקנים.
    /// O(n) — מעבר בודד על המחרוזת.
    /// </summary>
    public static class Tokenizer
    {
        public static List<Token> Tokenize(string expression)
        {
            var tokens = new List<Token>();
            int i = 0, len = expression.Length;

            while (i < len)
            {
                char c = expression[i];

                // דלג על רווחים
                if (char.IsWhiteSpace(c)) { i++; continue; }

                // ── מספר (כולל נקודה עשרונית) ──
                if (char.IsDigit(c) || (c == '.' && i + 1 < len && char.IsDigit(expression[i + 1])))
                {
                    int start = i;
                    while (i < len && (char.IsDigit(expression[i]) || expression[i] == '.')) i++;
                    tokens.Add(new Token(TokenType.Number, expression[start..i]));
                    continue;
                }

                // ── משתנה או פונקציה ──
                if (char.IsLetter(c))
                {
                    int start = i;
                    while (i < len && (char.IsLetterOrDigit(expression[i]) || expression[i] == '_')) i++;
                    string word = expression[start..i].ToLowerInvariant();

                    // פונקציה = יש סוגריים אחרי
                    if (i < len && expression[i] == '(')
                        tokens.Add(new Token(TokenType.Function, word));
                    else
                        tokens.Add(new Token(TokenType.Variable, word));
                    continue;
                }

                // ── סוגריים ──
                if (c == '(') { tokens.Add(new Token(TokenType.LeftParen,  "(")); i++; continue; }
                if (c == ')') { tokens.Add(new Token(TokenType.RightParen, ")")); i++; continue; }
                if (c == ',') { tokens.Add(new Token(TokenType.Comma,      ",")); i++; continue; }

                // ── אופרטור השוואה דו-תווי: >= <= == != ──
                if (i + 1 < len)
                {
                    string two = expression.Substring(i, 2);
                    if (two == ">=" || two == "<=" || two == "==" || two == "!=")
                    {
                        tokens.Add(new Token(TokenType.Comparison, two));
                        i += 2; continue;
                    }
                }

                // ── אופרטור השוואה חד-תווי: > < = ──
                if (c == '>' || c == '<')
                {
                    tokens.Add(new Token(TokenType.Comparison, c.ToString()));
                    i++; continue;
                }
                if (c == '=')
                {
                    tokens.Add(new Token(TokenType.Comparison, "=="));
                    i++; continue;
                }

                // ── אופרטור חשבוני ──
                if (c is '+' or '-' or '*' or '/' or '^')
                {
                    tokens.Add(new Token(TokenType.Operator, c.ToString()));
                    i++; continue;
                }

                throw new FormatException($"Unexpected character '{c}' at position {i} in formula: {expression}");
            }

            tokens.Add(new Token(TokenType.End, ""));
            return tokens;
        }
    }
}
