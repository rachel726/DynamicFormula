// ╔══════════════════════════════════════════════════════════════════╗
// ║  FormulaCompiler.cs                                               ║
// ║  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  ║
// ║            ⚡  הלב של הפתרון המקצועי  ⚡                         ║
// ║  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  ║
// ║                                                                  ║
// ║  מפרש ומקמפל נוסחה טקסטואלית ל-Delegate מקומפל ב-IL              ║
// ║                                                                  ║
// ║  תהליך:                                                          ║
// ║  1. Tokenize: "a+b*c" → [a][+][b][*][c]                         ║
// ║  2. Parse:    טוקנים → Expression Tree (AST)                     ║
// ║  3. Compile:  AST → Func<double,double,double,double,double>     ║
// ║                                                                  ║
// ║  היתרון העצום: מקמפל פעם אחת, רץ מיליון פעם במהירות native!    ║
// ║                                                                  ║
// ║  אלגוריתם: Shunting-Yard של Dijkstra (1961) — טיפול נכון        ║
// ║  בסדר קדימות ואסוציאטיביות של אופרטורים.                         ║
// ╚══════════════════════════════════════════════════════════════════╝
using System.Linq.Expressions;
using DynamicFormula.Engine.Parser;

namespace DynamicFormula.Engine.Compiler
{
    /// <summary>
    /// מקמפל נוסחה טקסטואלית ל-<see cref="CompiledFormula"/> — delegate מהיר במהירות native.
    /// שימוש:
    ///   var fn = FormulaCompiler.Compile("sqrt(a^2 + b^2)");
    ///   double result = fn(3, 4, 0, 0);   //  → 5
    /// </summary>
    public delegate double CompiledFormula(double a, double b, double c, double d);

    public static class FormulaCompiler
    {
        // פרמטרים של ה-Lambda (a, b, c, d) — משותפים לכל הנוסחאות
        private static readonly ParameterExpression _a = Expression.Parameter(typeof(double), "a");
        private static readonly ParameterExpression _b = Expression.Parameter(typeof(double), "b");
        private static readonly ParameterExpression _c = Expression.Parameter(typeof(double), "c");
        private static readonly ParameterExpression _d = Expression.Parameter(typeof(double), "d");

        // ═══════════════════════════════════════════════════════════
        //   Public API
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// מקמפל נוסחה רגילה (ללא תנאי) ל-delegate מהיר.
        /// </summary>
        public static CompiledFormula Compile(string formula)
        {
            var tokens  = Tokenizer.Tokenize(formula);
            var body    = ParseExpression(tokens, 0, out _);
            var lambda  = Expression.Lambda<CompiledFormula>(body, _a, _b, _c, _d);
            return lambda.Compile();  // ← קומפילציה ל-IL במהלך runtime
        }

        /// <summary>
        /// מקמפל נוסחה מותנית:  if(tnai) then formula else targilFalse
        /// </summary>
        public static CompiledFormula CompileConditional(string formula, string tnai, string targilFalse)
        {
            var tnaiTokens  = Tokenizer.Tokenize(tnai);
            var trueTokens  = Tokenizer.Tokenize(formula);
            var falseTokens = Tokenizer.Tokenize(targilFalse);

            var condExpr  = ParseExpression(tnaiTokens,  0, out _);
            var trueExpr  = ParseExpression(trueTokens,  0, out _);
            var falseExpr = ParseExpression(falseTokens, 0, out _);

            // Conditional expression: condition ? trueBranch : falseBranch
            var body   = Expression.Condition(condExpr, trueExpr, falseExpr);
            var lambda = Expression.Lambda<CompiledFormula>(body, _a, _b, _c, _d);
            return lambda.Compile();
        }

        // ═══════════════════════════════════════════════════════════
        //   Parser — Shunting-Yard with Recursive Descent
        //   מחזיר Expression Tree ממוין לפי קדימות ואסוציאטיביות
        // ═══════════════════════════════════════════════════════════

        /// <summary>פרסר Expression: רמה תחתונה (אופרטורים לפי קדימות).</summary>
        private static Expression ParseExpression(List<Token> tokens, int pos, out int next)
        {
            // קדימות: comparison < +/- < *// < ^ < unary < primary
            var output = new Stack<Expression>();
            var ops    = new Stack<Token>();

            int i = pos;
            while (tokens[i].Type != TokenType.End && tokens[i].Type != TokenType.Comma
                   && tokens[i].Type != TokenType.RightParen)
            {
                var tk = tokens[i];

                if (tk.Type == TokenType.Number)
                {
                    output.Push(Expression.Constant(double.Parse(tk.Value,
                        System.Globalization.CultureInfo.InvariantCulture)));
                    i++;
                }
                else if (tk.Type == TokenType.Variable)
                {
                    output.Push(ResolveVariable(tk.Value));
                    i++;
                }
                else if (tk.Type == TokenType.Function)
                {
                    // פונקציה: name( arg1, arg2, ... )
                    string name = tk.Value;
                    i++; // skip name
                    if (tokens[i].Type != TokenType.LeftParen)
                        throw new FormatException($"Expected '(' after function '{name}'");
                    i++; // skip (

                    var args = new List<Expression>();
                    if (tokens[i].Type != TokenType.RightParen)
                    {
                        args.Add(ParseExpression(tokens, i, out i));
                        while (tokens[i].Type == TokenType.Comma)
                        {
                            i++; // skip ,
                            args.Add(ParseExpression(tokens, i, out i));
                        }
                    }
                    if (tokens[i].Type != TokenType.RightParen)
                        throw new FormatException($"Expected ')' after function arguments");
                    i++; // skip )

                    output.Push(BuildFunctionCall(name, args));
                }
                else if (tk.Type == TokenType.LeftParen)
                {
                    i++; // skip (
                    var inner = ParseExpression(tokens, i, out i);
                    if (tokens[i].Type != TokenType.RightParen)
                        throw new FormatException("Missing closing parenthesis");
                    i++; // skip )
                    output.Push(inner);
                }
                else if (tk.Type == TokenType.Operator || tk.Type == TokenType.Comparison)
                {
                    while (ops.Count > 0 &&
                           Precedence(ops.Peek()) >= Precedence(tk) &&
                           IsLeftAssociative(tk))
                    {
                        ApplyTopOperator(output, ops.Pop());
                    }
                    ops.Push(tk);
                    i++;
                }
                else
                {
                    break;
                }
            }

            while (ops.Count > 0)
                ApplyTopOperator(output, ops.Pop());

            next = i;
            if (output.Count != 1)
                throw new FormatException("Invalid expression — operand/operator mismatch");
            return output.Pop();
        }

        // ═══════════════════════════════════════════════════════════
        //   Helpers
        // ═══════════════════════════════════════════════════════════

        private static Expression ResolveVariable(string name) => name.ToLowerInvariant() switch
        {
            "a" => _a,
            "b" => _b,
            "c" => _c,
            "d" => _d,
            _   => throw new FormatException($"Unknown variable: {name}")
        };

        private static Expression BuildFunctionCall(string name, List<Expression> args)
        {
            return name.ToLowerInvariant() switch
            {
                "sqrt"  when args.Count == 1 => Expression.Call(typeof(Math), nameof(Math.Sqrt), null, args[0]),
                "log"   when args.Count == 1 => Expression.Call(typeof(Math), nameof(Math.Log),  null, args[0]),
                "abs"   when args.Count == 1 => Expression.Call(typeof(Math), nameof(Math.Abs),  null, args[0]),
                "power" when args.Count == 2 => Expression.Call(typeof(Math), nameof(Math.Pow),  null, args[0], args[1]),
                "pow"   when args.Count == 2 => Expression.Call(typeof(Math), nameof(Math.Pow),  null, args[0], args[1]),
                _ => throw new FormatException($"Unknown or bad-arity function: {name}({args.Count})")
            };
        }

        private static void ApplyTopOperator(Stack<Expression> output, Token op)
        {
            if (output.Count < 2)
                throw new FormatException($"Operator '{op.Value}' missing operands");
            var right = output.Pop();
            var left  = output.Pop();
            output.Push(BuildBinary(op, left, right));
        }

        private static Expression BuildBinary(Token op, Expression left, Expression right)
        {
            return op.Value switch
            {
                // חשבוני
                "+" => Expression.Add(left, right),
                "-" => Expression.Subtract(left, right),
                "*" => Expression.Multiply(left, right),
                "/" => Expression.Divide(left, right),
                "^" => Expression.Call(typeof(Math), nameof(Math.Pow), null, left, right),

                // השוואה (מחזיר bool)
                ">"  => Expression.GreaterThan(left, right),
                "<"  => Expression.LessThan(left, right),
                ">=" => Expression.GreaterThanOrEqual(left, right),
                "<=" => Expression.LessThanOrEqual(left, right),
                "==" => Expression.Equal(left, right),
                "!=" => Expression.NotEqual(left, right),

                _ => throw new FormatException($"Unknown operator: {op.Value}")
            };
        }

        private static int Precedence(Token op) => op.Value switch
        {
            ">" or "<" or ">=" or "<=" or "==" or "!=" => 1,
            "+" or "-"                                 => 2,
            "*" or "/"                                 => 3,
            "^"                                        => 4,
            _                                          => 0
        };

        private static bool IsLeftAssociative(Token op) => op.Value != "^";
    }
}
