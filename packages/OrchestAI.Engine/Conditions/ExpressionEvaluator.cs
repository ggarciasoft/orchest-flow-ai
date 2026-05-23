namespace OrchestAI.Engine.Conditions;

public sealed class ExpressionEvaluator
{
    public bool Evaluate(string expression, Dictionary<string, object?> scope)
    {
        var tokens = Tokenize(expression.Trim());
        var pos = 0;
        return ParseOr(tokens, ref pos, scope);
    }

    private static bool ParseOr(List<string> tokens, ref int pos, Dictionary<string, object?> scope)
    {
        var left = ParseAnd(tokens, ref pos, scope);
        while (pos < tokens.Count && tokens[pos] == "||") { pos++; var right = ParseAnd(tokens, ref pos, scope); left = left || right; }
        return left;
    }

    private static bool ParseAnd(List<string> tokens, ref int pos, Dictionary<string, object?> scope)
    {
        var left = ParseNot(tokens, ref pos, scope);
        while (pos < tokens.Count && tokens[pos] == "&&") { pos++; var right = ParseNot(tokens, ref pos, scope); left = left && right; }
        return left;
    }

    private static bool ParseNot(List<string> tokens, ref int pos, Dictionary<string, object?> scope)
    {
        if (pos < tokens.Count && tokens[pos] == "!") { pos++; return !ParseComparison(tokens, ref pos, scope); }
        return ParseComparison(tokens, ref pos, scope);
    }

    private static bool ParseComparison(List<string> tokens, ref int pos, Dictionary<string, object?> scope)
    {
        var left = ParseValue(tokens, ref pos, scope);
        if (pos >= tokens.Count) return left is bool b && b;
        var op = tokens[pos];
        if (op is "==" or "!=" or ">" or "<" or ">=" or "<=")
        {
            pos++;
            var right = ParseValue(tokens, ref pos, scope);
            return op switch
            {
                "==" => Equals(left, right),
                "!=" => !Equals(left, right),
                ">" => Compare(left, right) > 0,
                "<" => Compare(left, right) < 0,
                ">=" => Compare(left, right) >= 0,
                "<=" => Compare(left, right) <= 0,
                _ => false
            };
        }
        return left is bool bv && bv;
    }

    private static object? ParseValue(List<string> tokens, ref int pos, Dictionary<string, object?> scope)
    {
        if (pos >= tokens.Count) return null;
        var token = tokens[pos++];
        if (token.Length >= 2 && token[0] == '\'' && token[^1] == '\'') return token[1..^1];
        if (token == "true") return true;
        if (token == "false") return false;
        if (double.TryParse(token, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var num)) return num;
        return scope.GetValueOrDefault(token);
    }

    private static bool Equals(object? a, object? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.ToString() == b.ToString();
    }

    private static int Compare(object? a, object? b)
    {
        if (a is double da && b is double db) return da.CompareTo(db);
        return string.Compare(a?.ToString(), b?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> Tokenize(string expr)
    {
        var tokens = new List<string>();
        var i = 0;
        while (i < expr.Length)
        {
            if (char.IsWhiteSpace(expr[i])) { i++; continue; }

            // String literal in single quotes
            if (expr[i] == '\'')
            {
                var end = expr.IndexOf('\'', i + 1);
                if (end < 0) end = expr.Length - 1;
                tokens.Add(expr[i..(end + 1)]);
                i = end + 1;
                continue;
            }

            // Two-char operators
            if (i + 1 < expr.Length)
            {
                var two = expr[i..(i + 2)];
                if (two is "==" or "!=" or ">=" or "<=" or "&&" or "||")
                {
                    tokens.Add(two);
                    i += 2;
                    continue;
                }
            }

            // Single-char operators
            if (expr[i] is '>' or '<' or '!')
            {
                tokens.Add(expr[i].ToString());
                i++;
                continue;
            }

            // Identifier or number
            var start = i;
            while (i < expr.Length && !char.IsWhiteSpace(expr[i]) &&
                   expr[i] != '\'' && expr[i] != '=' && expr[i] != '!' &&
                   expr[i] != '>' && expr[i] != '<' && expr[i] != '&' && expr[i] != '|')
                i++;
            if (i > start) tokens.Add(expr[start..i]);
        }
        return tokens;
    }
}
