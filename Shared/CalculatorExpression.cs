using System.Globalization;
using System.Text;

namespace WTExpCalc.Shared
{
    internal static class CalculatorExpression
    {
        private enum TokenType
        {
            Number,
            Plus,
            Minus,
            Multiply,
            Divide,
            Percent,
            OpenParen,
            CloseParen
        }

        private readonly record struct Token(TokenType Type, double Value);

        public static string NormalizeExpression(string raw)
        {
            var result = new StringBuilder(raw.Length);

            foreach (var c in raw)
            {
                var ch = c switch
                {
                    '*' => '×',
                    '/' => '÷',
                    _ => c
                };

                if (ch is '+' or '-' or '×' or '÷')
                {
                    if (result.Length > 0 && result[^1] is '+' or '-' or '×' or '÷')
                    {
                        result[^1] = ch;
                    }
                    else
                    {
                        result.Append(ch);
                    }

                    continue;
                }

                if (ch is '.' or ',')
                {
                    if (HasSeparatorInCurrentNumber(result.ToString()))
                    {
                        continue;
                    }

                    if (result.Length == 0 || (!char.IsDigit(result[^1]) && result[^1] is not ('.' or ',')))
                    {
                        result.Append('0');
                    }

                    result.Append(ch);
                    continue;
                }

                if (ch == '%')
                {
                    if (result.Length > 0 && (char.IsDigit(result[^1]) || result[^1] == ')'))
                    {
                        result.Append(ch);
                    }

                    continue;
                }

                if (ch == ')')
                {
                    var text = result.ToString();
                    var openCount = text.Count(x => x == '(');
                    var closeCount = text.Count(x => x == ')');
                    if (openCount > closeCount && result.Length > 0 && result[^1] is not ('(' or '+' or '-' or '×' or '÷'))
                    {
                        result.Append(ch);
                    }

                    continue;
                }

                if (ch is '(' || char.IsDigit(ch))
                {
                    result.Append(ch);
                }
            }

            return result.ToString();
        }

        public static bool IsPlainNumber(string expression)
        {
            var text = expression.Trim().Replace(',', '.');
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }

        private static bool HasSeparatorInCurrentNumber(string text)
        {
            for (var i = text.Length - 1; i >= 0; i--)
            {
                var c = text[i];
                if (char.IsDigit(c))
                {
                    continue;
                }

                return c is '.' or ',';
            }

            return false;
        }

        public static bool TryEvaluate(string expression, out double result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(expression))
            {
                return false;
            }

            try
            {
                var tokens = Tokenize(expression);
                if (tokens.Count == 0)
                {
                    return false;
                }

                var position = 0;
                var (value, isBarePercent) = ParseExpression(tokens, ref position);
                if (position != tokens.Count)
                {
                    return false;
                }

                result = isBarePercent ? value / 100.0 : value;
                return double.IsFinite(result);
            }
            catch (FormatException)
            {
                return false;
            }
            catch (DivideByZeroException)
            {
                return false;
            }
        }

        private static List<Token> Tokenize(string expression)
        {
            var tokens = new List<Token>();
            var index = 0;

            while (index < expression.Length)
            {
                var current = expression[index];

                if (char.IsWhiteSpace(current))
                {
                    index++;
                    continue;
                }

                if (char.IsDigit(current) || current == '.' || current == ',')
                {
                    var start = index;
                    var separatorCount = 0;

                    while (index < expression.Length &&
                           (char.IsDigit(expression[index]) || expression[index] == '.' || expression[index] == ','))
                    {
                        if (expression[index] == '.' || expression[index] == ',')
                        {
                            separatorCount++;
                        }

                        index++;
                    }

                    var numberText = expression.Substring(start, index - start).Replace(',', '.');
                    if (separatorCount > 1 ||
                        !double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                    {
                        throw new FormatException();
                    }

                    tokens.Add(new Token(TokenType.Number, number));
                    continue;
                }

                var type = current switch
                {
                    '+' => TokenType.Plus,
                    '-' => TokenType.Minus,
                    '*' or '×' => TokenType.Multiply,
                    '/' or '÷' => TokenType.Divide,
                    '%' => TokenType.Percent,
                    '(' => TokenType.OpenParen,
                    ')' => TokenType.CloseParen,
                    _ => throw new FormatException()
                };

                tokens.Add(new Token(type, 0));
                index++;
            }

            return tokens;
        }

        // Проценты считаются как в обычном калькуляторе: a + b% = a + a*b/100,
        // a - b% = a - a*b/100, a * b% = a*b/100, a / b% = a/(b/100).
        private static (double Value, bool IsBarePercent) ParseExpression(List<Token> tokens, ref int position)
        {
            var left = ParseTerm(tokens, ref position);

            while (position < tokens.Count &&
                   (tokens[position].Type == TokenType.Plus || tokens[position].Type == TokenType.Minus))
            {
                var operation = tokens[position].Type;
                position++;
                var right = ParseTerm(tokens, ref position);

                if (left.IsBarePercent)
                {
                    left = (left.Value / 100.0, false);
                }

                if (operation == TokenType.Plus)
                {
                    left = (right.IsBarePercent ? left.Value + left.Value * right.Value / 100.0 : left.Value + right.Value, false);
                }
                else
                {
                    left = (right.IsBarePercent ? left.Value - left.Value * right.Value / 100.0 : left.Value - right.Value, false);
                }
            }

            return left;
        }

        private static (double Value, bool IsBarePercent) ParseTerm(List<Token> tokens, ref int position)
        {
            var left = ParseFactor(tokens, ref position);

            while (position < tokens.Count)
            {
                var operation = tokens[position].Type;

                // Пропущенный знак умножения: "2(3+4)", "5%2", ")5".
                var isImplicitMultiply = operation is TokenType.Number or TokenType.OpenParen;
                if (operation != TokenType.Multiply && operation != TokenType.Divide && !isImplicitMultiply)
                {
                    break;
                }

                if (!isImplicitMultiply)
                {
                    position++;
                }

                var right = ParseFactor(tokens, ref position);

                if (left.IsBarePercent)
                {
                    left = (left.Value / 100.0, false);
                }

                if (operation == TokenType.Divide)
                {
                    var divisor = right.IsBarePercent ? right.Value / 100.0 : right.Value;
                    if (divisor == 0)
                    {
                        throw new DivideByZeroException();
                    }

                    left = (left.Value / divisor, false);
                }
                else
                {
                    left = (right.IsBarePercent ? left.Value * right.Value / 100.0 : left.Value * right.Value, false);
                }
            }

            return left;
        }

        private static (double Value, bool IsBarePercent) ParseFactor(List<Token> tokens, ref int position)
        {
            if (position < tokens.Count && tokens[position].Type == TokenType.Minus)
            {
                position++;
                var (value, isBarePercent) = ParseFactor(tokens, ref position);
                return (-value, isBarePercent);
            }

            if (position < tokens.Count && tokens[position].Type == TokenType.Plus)
            {
                position++;
                return ParseFactor(tokens, ref position);
            }

            return ParsePrimary(tokens, ref position);
        }

        private static (double Value, bool IsBarePercent) ParsePrimary(List<Token> tokens, ref int position)
        {
            if (position >= tokens.Count)
            {
                throw new FormatException();
            }

            var token = tokens[position];

            if (token.Type == TokenType.Number)
            {
                position++;
                var isBarePercent = position < tokens.Count && tokens[position].Type == TokenType.Percent;
                if (isBarePercent)
                {
                    position++;
                }

                return (token.Value, isBarePercent);
            }

            if (token.Type == TokenType.OpenParen)
            {
                position++;
                var inner = ParseExpression(tokens, ref position);

                if (position >= tokens.Count || tokens[position].Type != TokenType.CloseParen)
                {
                    throw new FormatException();
                }

                position++;
                var isBarePercent = inner.IsBarePercent;
                if (position < tokens.Count && tokens[position].Type == TokenType.Percent)
                {
                    position++;
                    isBarePercent = true;
                }

                return (inner.Value, isBarePercent);
            }

            throw new FormatException();
        }
    }
}
