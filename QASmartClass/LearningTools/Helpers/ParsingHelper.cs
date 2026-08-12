using System;
using System.Globalization;

namespace QASmartClass.LearningTools.Helpers
{
    /// <summary>
    /// Helper class providing region-agnostic parsing methods for decimal numbers,
    /// ensuring correct parsing of dot and comma separators regardless of OS culture settings.
    /// </summary>
    public static class ParsingHelper
    {
        /// <summary>
        /// Attempts to parse a double from a string, supporting both '.' and ',' as decimal separators.
        /// </summary>
        /// <param name="input">The raw input string to parse.</param>
        /// <param name="value">The resulting double value if parsing is successful.</param>
        /// <returns>True if parsing succeeds, otherwise false.</returns>
        public static bool TryParseDouble(string input, out double value)
        {
            value = 0.0;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // Normalize both ',' and '.' to '.' and 'π' to 'pi' for standard evaluation
            string expr = input.Replace(',', '.').Replace("π", "pi").Trim().ToLowerInvariant();

            try
            {
                return EvalMath(expr, out value);
            }
            catch
            {
                return false;
            }
        }

        private static bool EvalMath(string expr, out double value)
        {
            value = 0.0;
            if (string.IsNullOrWhiteSpace(expr)) return false;

            // Find division '/' that is not inside parentheses
            int divIdx = -1;
            int parenDepth = 0;
            int divCount = 0;
            for (int i = 0; i < expr.Length; i++)
            {
                if (expr[i] == '(') parenDepth++;
                else if (expr[i] == ')') parenDepth--;
                else if (expr[i] == '/' && parenDepth == 0)
                {
                    divIdx = i;
                    divCount++;
                }
            }

            if (divCount > 1) return false; // Reject ambiguous multiple division like 1/2/3

            if (divIdx != -1)
            {
                string left = expr.Substring(0, divIdx).Trim();
                string right = expr.Substring(divIdx + 1).Trim();
                if (EvalMath(left, out double num) && EvalMath(right, out double den))
                {
                    if (System.Math.Abs(den) < 1e-12) return false;
                    value = num / den;
                    return true;
                }
                return false;
            }

            // Find multiplication '*' that is not inside parentheses
            int multIdx = -1;
            parenDepth = 0;
            for (int i = 0; i < expr.Length; i++)
            {
                if (expr[i] == '(') parenDepth++;
                else if (expr[i] == ')') parenDepth--;
                else if (expr[i] == '*' && parenDepth == 0)
                {
                    multIdx = i;
                    break;
                }
            }

            if (multIdx != -1)
            {
                string left = expr.Substring(0, multIdx).Trim();
                string right = expr.Substring(multIdx + 1).Trim();
                if (EvalMath(left, out double v1) && EvalMath(right, out double v2))
                {
                    value = v1 * v2;
                    return true;
                }
                return false;
            }

            // Handle unary minus
            if (expr.StartsWith("-"))
            {
                string sub = expr.Substring(1).Trim();
                if (EvalMath(sub, out double subVal))
                {
                    value = -subVal;
                    return true;
                }
                return false;
            }
            if (expr.StartsWith("+"))
            {
                string sub = expr.Substring(1).Trim();
                return EvalMath(sub, out value);
            }

            // Handle parentheses
            if (expr.StartsWith("(") && expr.EndsWith(")"))
            {
                string sub = expr.Substring(1, expr.Length - 2).Trim();
                return EvalMath(sub, out value);
            }

            // Handle sqrt(...)
            if (expr.StartsWith("sqrt(") && expr.EndsWith(")"))
            {
                string arg = expr.Substring(5, expr.Length - 6).Trim();
                if (EvalMath(arg, out double argVal))
                {
                    if (argVal < 0) return false;
                    value = System.Math.Sqrt(argVal);
                    return true;
                }
                return false;
            }

            // Handle constant pi
            if (expr == "pi")
            {
                value = System.Math.PI;
                return true;
            }

            // Handle implicit multiplication (e.g. "2pi" -> 2 * pi)
            if (expr.EndsWith("pi") && expr.Length > 2)
            {
                string prefix = expr.Substring(0, expr.Length - 2).Trim();
                if (EvalMath(prefix, out double prefVal))
                {
                    value = prefVal * System.Math.PI;
                    return true;
                }
            }

            // Handle constant e
            if (expr == "e")
            {
                value = System.Math.E;
                return true;
            }

            // Handle implicit multiplication (e.g. "2e" -> 2 * e)
            if (expr.EndsWith("e") && expr.Length > 1)
            {
                string prefix = expr.Substring(0, expr.Length - 1).Trim();
                if (EvalMath(prefix, out double prefVal))
                {
                    value = prefVal * System.Math.E;
                    return true;
                }
            }

            // Fallback to basic double parsing
            return double.TryParse(expr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value);
        }
    }
}
