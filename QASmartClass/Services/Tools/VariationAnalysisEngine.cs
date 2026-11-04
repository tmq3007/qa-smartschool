using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace QASmartClass.Services.Tools
{
    /// <summary>
    /// Mô hình điểm mốc trên trục x của Bảng biến thiên
    /// </summary>
    public class VariationMark
    {
        public double? X { get; set; }
        public string XLabel { get; set; } = "";
        public bool Defined { get; set; } = true;
        public bool Excluded { get; set; } = false; // Điểm gián đoạn (tiệm cận đứng x = x0) -> nét đôi ||
        public string DerivativeLabel { get; set; } = ""; // "0", "+", "-", hoặc rỗng nếu gián đoạn
        public double? Value { get; set; }
        public string ValueLabel { get; set; } = "";
        public double? LeftLimit { get; set; }
        public string LeftLimitLabel { get; set; } = "";
        public double? RightLimit { get; set; }
        public string RightLimitLabel { get; set; } = "";
    }

    /// <summary>
    /// Khoảng biến thiên giữa 2 điểm mốc
    /// </summary>
    public class VariationInterval
    {
        public double? Left { get; set; }
        public double? Right { get; set; }
        public int Sign { get; set; } // +1: tăng, -1: giảm, 0: không đổi, null/0 nếu ngoài TXĐ
    }

    /// <summary>
    /// Dữ liệu hoàn chỉnh của Bảng biến thiên cho một hàm số
    /// </summary>
    public class VariationModel
    {
        public string SourceExpression { get; set; } = "";
        public string FunctionFormula { get; set; } = "";
        public string DerivativeFormula { get; set; } = "";
        public string DomainDescription { get; set; } = "D = ℝ";
        public List<VariationMark> Marks { get; set; } = new();
        public List<VariationInterval> Intervals { get; set; } = new();
        public List<string> AnalysisSummaryLines { get; set; } = new();
    }

    /// <summary>
    /// Bộ phân tích toán học giải tích độc lập, không dùng GeoGebra hay internet.
    /// Hỗ trợ đầy đủ các hàm số chuẩn SGK THPT Việt Nam:
    /// - Đa thức bậc 3: y = ax³ + bx² + cx + d
    /// - Đa thức bậc 4 trùng phương: y = ax⁴ + bx² + c
    /// - Đa thức bậc 2 (Parabol): y = ax² + bx + c
    /// - Hàm bậc nhất / bậc nhất: y = (ax + b)/(cx + d)
    /// - Hàm bậc hai / bậc nhất: y = (ax² + bx + c)/(dx + e)
    /// </summary>
    public static class VariationAnalysisEngine
    {
        /// <summary>
        /// Phân tích biểu thức và sinh ra VariationModel hoàn chỉnh
        /// </summary>
        public static VariationModel Analyze(string inputExpression)
        {
            if (string.IsNullOrWhiteSpace(inputExpression))
                throw new ArgumentException("Vui lòng nhập biểu thức hàm số.");

            string cleaned = CleanExpression(inputExpression);
            var model = new VariationModel { SourceExpression = inputExpression };

            // 1. Nhận diện dạng hàm phân thức (P(x) / Q(x))
            if (cleaned.Contains('/'))
            {
                var parts = SplitFraction(cleaned);
                if (parts != null)
                {
                    double[] numPoly = ParsePolynomial(parts.Item1);
                    double[] denPoly = ParsePolynomial(parts.Item2);

                    if (denPoly.Length == 2 && numPoly.Length <= 2) // Bậc 1 / bậc 1
                    {
                        return AnalyzeRational11(numPoly, denPoly, cleaned);
                    }
                    if (denPoly.Length == 2 && numPoly.Length == 3) // Bậc 2 / bậc 1
                    {
                        return AnalyzeRational21(numPoly, denPoly, cleaned);
                    }
                }
            }

            // 2. Nhận diện hàm đa thức (Bậc 2, Bậc 3, Bậc 4)
            double[] poly = ParsePolynomial(cleaned);
            int degree = poly.Length - 1;

            if (degree == 3)
            {
                return AnalyzeCubic(poly, cleaned);
            }
            else if (degree == 4 && Math.Abs(poly[1]) < 1e-9 && Math.Abs(poly[3]) < 1e-9)
            {
                return AnalyzeBiquadratic(poly, cleaned);
            }
            else if (degree == 4)
            {
                return AnalyzeGeneralQuartic(poly, cleaned);
            }
            else if (degree == 2)
            {
                return AnalyzeQuadratic(poly, cleaned);
            }
            else if (degree == 1)
            {
                return AnalyzeLinear(poly, cleaned);
            }

            // Mặc định fallback nếu bậc cao hơn
            return AnalyzeFallbackPolynomial(poly, cleaned);
        }

        #region Helper: Chuẩn hóa chuỗi và tách phân số

        public static string CleanExpression(string expr)
        {
            string s = expr.Trim();
            // Bỏ tiền tố y = hoặc f(x) =
            s = Regex.Replace(s, @"^\s*(?:[a-zA-Z]\s*\(\s*x\s*\)|y)\s*=\s*", "");
            s = s.Replace("−", "-").Replace("–", "-");
            s = s.Replace("×", "*").Replace("·", "*").Replace("÷", "/");
            s = s.Replace("²", "^2").Replace("³", "^3").Replace("⁴", "^4");
            s = Regex.Replace(s, @"\s+", "");
            return s;
        }

        private static Tuple<string, string>? SplitFraction(string expr)
        {
            int slashIdx = -1;
            int depth = 0;
            for (int i = 0; i < expr.Length; i++)
            {
                if (expr[i] == '(') depth++;
                else if (expr[i] == ')') depth--;
                else if (expr[i] == '/' && depth == 0)
                {
                    slashIdx = i;
                    break;
                }
            }

            if (slashIdx == -1)
            {
                // Thử bóc ngoặc (P)/(Q)
                if (expr.StartsWith("(") && expr.Contains(")/("))
                {
                    int mid = expr.IndexOf(")/(");
                    string num = expr.Substring(1, mid - 1);
                    string den = expr.Substring(mid + 3);
                    if (den.EndsWith(")")) den = den.Substring(0, den.Length - 1);
                    return Tuple.Create(num, den);
                }
                return null;
            }

            string n = StripOuterParens(expr.Substring(0, slashIdx));
            string d = StripOuterParens(expr.Substring(slashIdx + 1));
            return Tuple.Create(n, d);
        }

        private static string StripOuterParens(string s)
        {
            s = s.Trim();
            if (s.StartsWith("(") && s.EndsWith(")"))
            {
                int depth = 0;
                bool ok = true;
                for (int i = 0; i < s.Length - 1; i++)
                {
                    if (s[i] == '(') depth++;
                    else if (s[i] == ')') depth--;
                    if (depth == 0) { ok = false; break; }
                }
                if (ok) return s.Substring(1, s.Length - 2);
            }
            return s;
        }

        #endregion

        #region Helper: Phân tích Đa thức (Polynomial Parser)

        /// <summary>
        /// Phân tích biểu thức thành mảng hệ số đa thức P(x) = c0*x^n + c1*x^(n-1) + ... + cn
        /// Trả về mảng hệ số từ bậc cao nhất đến bậc 0
        /// </summary>
        public static double[] ParsePolynomial(string expr)
        {
            string s = StripOuterParens(expr);
            if (string.IsNullOrWhiteSpace(s)) return new double[] { 0 };

            // Chuẩn hóa dấu: + và -
            var terms = new Dictionary<int, double>();
            var matches = Regex.Matches(s, @"([+-]?[^+-]+)");

            foreach (Match m in matches)
            {
                string term = m.Value.Trim();
                if (string.IsNullOrEmpty(term)) continue;

                double coef = 1.0;
                int power = 0;

                if (term.Contains("x") || term.Contains("X"))
                {
                    string[] parts = Regex.Split(term, "[xX]");
                    string cStr = parts[0].Replace("*", "");
                    if (cStr == "" || cStr == "+") coef = 1.0;
                    else if (cStr == "-") coef = -1.0;
                    else coef = double.Parse(cStr, CultureInfo.InvariantCulture);

                    if (parts.Length > 1 && parts[1].StartsWith("^"))
                    {
                        power = int.Parse(parts[1].Substring(1));
                    }
                    else
                    {
                        power = 1;
                    }
                }
                else
                {
                    coef = double.Parse(term, CultureInfo.InvariantCulture);
                    power = 0;
                }

                if (!terms.ContainsKey(power)) terms[power] = 0;
                terms[power] += coef;
            }

            int maxDegree = terms.Keys.Count > 0 ? terms.Keys.Max() : 0;
            double[] res = new double[maxDegree + 1];
            for (int i = 0; i <= maxDegree; i++)
            {
                int p = maxDegree - i;
                res[i] = terms.ContainsKey(p) ? terms[p] : 0.0;
            }
            return res;
        }

        public static double EvaluatePoly(double[] poly, double x)
        {
            double res = 0;
            for (int i = 0; i < poly.Length; i++)
            {
                res = res * x + poly[i];
            }
            return res;
        }

        public static double[] DerivativePoly(double[] poly)
        {
            if (poly.Length <= 1) return new double[] { 0 };
            double[] d = new double[poly.Length - 1];
            int n = poly.Length - 1;
            for (int i = 0; i < n; i++)
            {
                d[i] = poly[i] * (n - i);
            }
            return d;
        }

        public static string FormatPoly(double[] poly)
        {
            int n = poly.Length - 1;
            var parts = new List<string>();
            for (int i = 0; i <= n; i++)
            {
                double c = poly[i];
                if (Math.Abs(c) < 1e-9) continue;
                int p = n - i;

                string sign = c > 0 ? (parts.Count > 0 ? "+ " : "") : "- ";
                double absC = Math.Abs(c);
                string cText = (absC == 1 && p > 0) ? "" : FormatNumber(absC);

                if (p == 0) parts.Add(sign + FormatNumber(absC));
                else if (p == 1) parts.Add(sign + cText + "x");
                else parts.Add(sign + cText + "x^" + p);
            }
            return parts.Count > 0 ? string.Join(" ", parts) : "0";
        }

        public static string FormatNumber(double val)
        {
            if (double.IsPositiveInfinity(val)) return "+∞";
            if (double.IsNegativeInfinity(val)) return "-∞";

            // Kiểm tra phân số gần đúng
            for (int den = 1; den <= 24; den++)
            {
                double num = Math.Round(val * den);
                if (Math.Abs(val - num / den) < 1e-6)
                {
                    if (den == 1) return num.ToString(CultureInfo.InvariantCulture);
                    return $"{num}/{den}";
                }
            }

            return Math.Round(val, 2).ToString(CultureInfo.InvariantCulture);
        }

        #endregion

        #region Các trường hợp phân tích chuyên biệt (Specialized Cases)

        // 1. ĐA THỨC BẬC 3: y = ax³ + bx² + cx + d
        private static VariationModel AnalyzeCubic(double[] poly, string original)
        {
            double a = poly[0], b = poly[1], c = poly[2], d = poly[3];
            var model = new VariationModel
            {
                SourceExpression = original,
                FunctionFormula = $"y = {FormatPoly(poly)}",
                DomainDescription = "D = ℝ"
            };

            // Đạo hàm y' = 3ax² + 2bx + c
            double a1 = 3 * a, b1 = 2 * b, c1 = c;
            model.DerivativeFormula = $"y' = {FormatPoly(new double[] { a1, b1, c1 })}";

            double delta = b1 * b1 - 4 * a1 * c1;
            var critX = new List<double>();

            if (delta > 1e-9)
            {
                double x1 = (-b1 - Math.Sqrt(delta)) / (2 * a1);
                double x2 = (-b1 + Math.Sqrt(delta)) / (2 * a1);
                if (x1 > x2) { double tmp = x1; x1 = x2; x2 = tmp; }
                critX.Add(x1);
                critX.Add(x2);
            }
            else if (Math.Abs(delta) <= 1e-9)
            {
                critX.Add(-b1 / (2 * a1));
            }

            // Điểm mốc: -∞ -> các cực trị -> +∞
            model.Marks.Add(new VariationMark
            {
                X = double.NegativeInfinity,
                XLabel = "-∞",
                Defined = false,
                RightLimit = a > 0 ? double.NegativeInfinity : double.PositiveInfinity,
                RightLimitLabel = a > 0 ? "-∞" : "+∞"
            });

            foreach (var x in critX)
            {
                double yVal = EvaluatePoly(poly, x);
                model.Marks.Add(new VariationMark
                {
                    X = x,
                    XLabel = FormatNumber(x),
                    Defined = true,
                    DerivativeLabel = "0",
                    Value = yVal,
                    ValueLabel = FormatNumber(yVal),
                    LeftLimit = yVal,
                    LeftLimitLabel = FormatNumber(yVal),
                    RightLimit = yVal,
                    RightLimitLabel = FormatNumber(yVal)
                });
            }

            model.Marks.Add(new VariationMark
            {
                X = double.PositiveInfinity,
                XLabel = "+∞",
                Defined = false,
                LeftLimit = a > 0 ? double.PositiveInfinity : double.NegativeInfinity,
                LeftLimitLabel = a > 0 ? "+∞" : "-∞"
            });

            // Tính dấu cho các khoảng
            for (int i = 0; i < model.Marks.Count - 1; i++)
            {
                double leftX = model.Marks[i].X ?? -100;
                double rightX = model.Marks[i + 1].X ?? 100;
                double midX = double.IsNegativeInfinity(leftX) ? rightX - 2 :
                              double.IsPositiveInfinity(rightX) ? leftX + 2 : (leftX + rightX) / 2.0;

                double derVal = a1 * midX * midX + b1 * midX + c1;
                int sign = derVal > 0 ? 1 : -1;
                model.Intervals.Add(new VariationInterval { Left = leftX, Right = rightX, Sign = sign });
            }

            // Tóm tắt kết luận giải tích
            model.AnalysisSummaryLines.Add($"• Tập xác định: {model.DomainDescription}");
            model.AnalysisSummaryLines.Add($"• Đạo hàm: {model.DerivativeFormula}");
            if (critX.Count == 2)
            {
                double y1 = EvaluatePoly(poly, critX[0]);
                double y2 = EvaluatePoly(poly, critX[1]);
                if (a > 0)
                {
                    model.AnalysisSummaryLines.Add($"• Điểm cực đại: x = {FormatNumber(critX[0])}, y_CD = {FormatNumber(y1)}");
                    model.AnalysisSummaryLines.Add($"• Điểm cực tiểu: x = {FormatNumber(critX[1])}, y_CT = {FormatNumber(y2)}");
                }
                else
                {
                    model.AnalysisSummaryLines.Add($"• Điểm cực tiểu: x = {FormatNumber(critX[0])}, y_CT = {FormatNumber(y1)}");
                    model.AnalysisSummaryLines.Add($"• Điểm cực đại: x = {FormatNumber(critX[1])}, y_CD = {FormatNumber(y2)}");
                }
            }
            else
            {
                model.AnalysisSummaryLines.Add(a > 0 ? "• Hàm số luôn đồng biến trên ℝ" : "• Hàm số luôn nghịch biến trên ℝ");
            }

            return model;
        }

        // 2. ĐA THỨC BẬC 4 TRÙNG PHƯƠNG: y = ax⁴ + bx² + c
        private static VariationModel AnalyzeBiquadratic(double[] poly, string original)
        {
            double a = poly[0], b = poly[2], c = poly[4];
            var model = new VariationModel
            {
                SourceExpression = original,
                FunctionFormula = $"y = {FormatPoly(poly)}",
                DomainDescription = "D = ℝ"
            };

            // y' = 4ax³ + 2bx = 2x(2ax² + b)
            model.DerivativeFormula = $"y' = {FormatPoly(new double[] { 4 * a, 0, 2 * b, 0 })}";

            var critX = new List<double> { 0 };
            double valX2 = -b / (2 * a);
            if (valX2 > 1e-9)
            {
                double root = Math.Sqrt(valX2);
                critX.Add(-root);
                critX.Add(root);
            }
            critX.Sort();

            model.Marks.Add(new VariationMark
            {
                X = double.NegativeInfinity,
                XLabel = "-∞",
                Defined = false,
                RightLimit = a > 0 ? double.PositiveInfinity : double.NegativeInfinity,
                RightLimitLabel = a > 0 ? "+∞" : "-∞"
            });

            foreach (var x in critX)
            {
                double yVal = EvaluatePoly(poly, x);
                model.Marks.Add(new VariationMark
                {
                    X = x,
                    XLabel = FormatNumber(x),
                    Defined = true,
                    DerivativeLabel = "0",
                    Value = yVal,
                    ValueLabel = FormatNumber(yVal),
                    LeftLimit = yVal,
                    LeftLimitLabel = FormatNumber(yVal),
                    RightLimit = yVal,
                    RightLimitLabel = FormatNumber(yVal)
                });
            }

            model.Marks.Add(new VariationMark
            {
                X = double.PositiveInfinity,
                XLabel = "+∞",
                Defined = false,
                LeftLimit = a > 0 ? double.PositiveInfinity : double.NegativeInfinity,
                LeftLimitLabel = a > 0 ? "+∞" : "-∞"
            });

            for (int i = 0; i < model.Marks.Count - 1; i++)
            {
                double leftX = model.Marks[i].X ?? -100;
                double rightX = model.Marks[i + 1].X ?? 100;
                double midX = double.IsNegativeInfinity(leftX) ? rightX - 1 :
                              double.IsPositiveInfinity(rightX) ? leftX + 1 : (leftX + rightX) / 2.0;

                double derVal = 4 * a * Math.Pow(midX, 3) + 2 * b * midX;
                int sign = derVal > 0 ? 1 : -1;
                model.Intervals.Add(new VariationInterval { Left = leftX, Right = rightX, Sign = sign });
            }

            model.AnalysisSummaryLines.Add($"• Tập xác định: {model.DomainDescription}");
            model.AnalysisSummaryLines.Add($"• Đạo hàm: {model.DerivativeFormula}");
            model.AnalysisSummaryLines.Add(critX.Count == 3 ? "• Đồ thị có 3 điểm cực trị (1 cực đại, 2 cực tiểu hoặc ngược lại)" : "• Đồ thị có 1 điểm cực trị duy nhất tại x = 0");

            return model;
        }

        // 3. HÀM PHÂN THỨC BẬC 1 / BẬC 1: y = (ax + b)/(cx + d)
        private static VariationModel AnalyzeRational11(double[] numPoly, double[] denPoly, string original)
        {
            double a = numPoly.Length == 2 ? numPoly[0] : 0;
            double b = numPoly.Length == 2 ? numPoly[1] : numPoly[0];
            double c = denPoly[0];
            double d = denPoly[1];

            var model = new VariationModel
            {
                SourceExpression = original,
                FunctionFormula = $"y = ({FormatPoly(numPoly)}) / ({FormatPoly(denPoly)})"
            };

            double asymptoteX = -d / c;
            double asymptoteY = a / c;
            double det = a * d - b * c; // Tử số đạo hàm y' = det / (cx + d)^2

            model.DomainDescription = $"D = ℝ \\ {{{FormatNumber(asymptoteX)}}}";
            model.DerivativeFormula = $"y' = {FormatNumber(det)} / ({FormatPoly(denPoly)})²";

            int sign = det > 0 ? 1 : -1;

            // Mark 1: -∞
            model.Marks.Add(new VariationMark
            {
                X = double.NegativeInfinity,
                XLabel = "-∞",
                Defined = false,
                RightLimit = asymptoteY,
                RightLimitLabel = FormatNumber(asymptoteY)
            });

            // Mark 2: Tiệm cận đứng x = x0 (Excluded = true -> Nét đôi ||)
            model.Marks.Add(new VariationMark
            {
                X = asymptoteX,
                XLabel = FormatNumber(asymptoteX),
                Defined = false,
                Excluded = true,
                DerivativeLabel = "", // Không xác định
                LeftLimit = sign > 0 ? double.PositiveInfinity : double.NegativeInfinity,
                LeftLimitLabel = sign > 0 ? "+∞" : "-∞",
                RightLimit = sign > 0 ? double.NegativeInfinity : double.PositiveInfinity,
                RightLimitLabel = sign > 0 ? "-∞" : "+∞"
            });

            // Mark 3: +∞
            model.Marks.Add(new VariationMark
            {
                X = double.PositiveInfinity,
                XLabel = "+∞",
                Defined = false,
                LeftLimit = asymptoteY,
                LeftLimitLabel = FormatNumber(asymptoteY)
            });

            model.Intervals.Add(new VariationInterval { Left = double.NegativeInfinity, Right = asymptoteX, Sign = sign });
            model.Intervals.Add(new VariationInterval { Left = asymptoteX, Right = double.PositiveInfinity, Sign = sign });

            model.AnalysisSummaryLines.Add($"• Tập xác định: {model.DomainDescription}");
            model.AnalysisSummaryLines.Add($"• Tiệm cận đứng: x = {FormatNumber(asymptoteX)}");
            model.AnalysisSummaryLines.Add($"• Tiệm cận ngang: y = {FormatNumber(asymptoteY)}");
            model.AnalysisSummaryLines.Add(sign > 0 ? "• Hàm số đồng biến trên từng khoảng xác định" : "• Hàm số nghịch biến trên từng khoảng xác định");

            return model;
        }

        // 4. HÀM PHÂN THỨC BẬC 2 / BẬC 1: y = (ax² + bx + c)/(dx + e)
        private static VariationModel AnalyzeRational21(double[] numPoly, double[] denPoly, string original)
        {
            double a = numPoly[0], b = numPoly[1], c = numPoly[2];
            double d = denPoly[0], e = denPoly[1];

            var model = new VariationModel
            {
                SourceExpression = original,
                FunctionFormula = $"y = ({FormatPoly(numPoly)}) / ({FormatPoly(denPoly)})"
            };

            double asymptoteX = -e / d;
            model.DomainDescription = $"D = ℝ \\ {{{FormatNumber(asymptoteX)}}}";

            // Đạo hàm: y' = [P'Q - PQ'] / Q^2
            // P' = 2ax + b, Q' = d
            // Tử số y': (2ax + b)(dx + e) - d(ax² + bx + c) = ad*x² + 2ae*x + (be - cd)
            double aDer = a * d;
            double bDer = 2 * a * e;
            double cDer = b * e - c * d;

            model.DerivativeFormula = $"y' = ({FormatPoly(new double[] { aDer, bDer, cDer })}) / ({FormatPoly(denPoly)})²";

            double delta = bDer * bDer - 4 * aDer * cDer;
            var points = new List<double>();
            if (delta > 1e-9)
            {
                double x1 = (-bDer - Math.Sqrt(delta)) / (2 * aDer);
                double x2 = (-bDer + Math.Sqrt(delta)) / (2 * aDer);
                if (x1 > x2) { double t = x1; x1 = x2; x2 = t; }
                points.Add(x1);
                points.Add(x2);
            }

            // Gộp tiệm cận đứng và các cực trị theo thứ tự trục x
            var allMarksX = new List<Tuple<double, bool>>(); // Item2: true nếu là tiệm cận đứng
            allMarksX.Add(Tuple.Create(asymptoteX, true));
            foreach (var p in points) allMarksX.Add(Tuple.Create(p, false));
            allMarksX.Sort((m1, m2) => m1.Item1.CompareTo(m2.Item1));

            // Mark đầu: -∞
            model.Marks.Add(new VariationMark
            {
                X = double.NegativeInfinity,
                XLabel = "-∞",
                Defined = false,
                RightLimit = (a / d) > 0 ? double.NegativeInfinity : double.PositiveInfinity,
                RightLimitLabel = (a / d) > 0 ? "-∞" : "+∞"
            });

            foreach (var m in allMarksX)
            {
                if (m.Item2) // Tiệm cận đứng
                {
                    model.Marks.Add(new VariationMark
                    {
                        X = m.Item1,
                        XLabel = FormatNumber(m.Item1),
                        Defined = false,
                        Excluded = true,
                        DerivativeLabel = "",
                        LeftLimit = double.NegativeInfinity,
                        LeftLimitLabel = "-∞",
                        RightLimit = double.PositiveInfinity,
                        RightLimitLabel = "+∞"
                    });
                }
                else // Điểm cực trị
                {
                    double xVal = m.Item1;
                    double yVal = EvaluatePoly(numPoly, xVal) / EvaluatePoly(denPoly, xVal);
                    model.Marks.Add(new VariationMark
                    {
                        X = xVal,
                        XLabel = FormatNumber(xVal),
                        Defined = true,
                        DerivativeLabel = "0",
                        Value = yVal,
                        ValueLabel = FormatNumber(yVal),
                        LeftLimit = yVal,
                        LeftLimitLabel = FormatNumber(yVal),
                        RightLimit = yVal,
                        RightLimitLabel = FormatNumber(yVal)
                    });
                }
            }

            // Mark cuối: +∞
            model.Marks.Add(new VariationMark
            {
                X = double.PositiveInfinity,
                XLabel = "+∞",
                Defined = false,
                LeftLimit = (a / d) > 0 ? double.PositiveInfinity : double.NegativeInfinity,
                LeftLimitLabel = (a / d) > 0 ? "+∞" : "-∞"
            });

            // Tính dấu y' cho các khoảng
            for (int i = 0; i < model.Marks.Count - 1; i++)
            {
                double leftX = model.Marks[i].X ?? -100;
                double rightX = model.Marks[i + 1].X ?? 100;
                double midX = double.IsNegativeInfinity(leftX) ? rightX - 1 :
                              double.IsPositiveInfinity(rightX) ? leftX + 1 : (leftX + rightX) / 2.0;

                double numDer = aDer * midX * midX + bDer * midX + cDer;
                int sign = numDer > 0 ? 1 : -1;
                model.Intervals.Add(new VariationInterval { Left = leftX, Right = rightX, Sign = sign });
            }

            model.AnalysisSummaryLines.Add($"• Tập xác định: {model.DomainDescription}");
            model.AnalysisSummaryLines.Add($"• Tiệm cận đứng: x = {FormatNumber(asymptoteX)}");
            model.AnalysisSummaryLines.Add($"• Đạo hàm: {model.DerivativeFormula}");

            return model;
        }

        // 5. HÀM BẬC 2 (PARABOL): y = ax² + bx + c
        private static VariationModel AnalyzeQuadratic(double[] poly, string original)
        {
            double a = poly[0], b = poly[1], c = poly[2];
            var model = new VariationModel
            {
                SourceExpression = original,
                FunctionFormula = $"y = {FormatPoly(poly)}",
                DomainDescription = "D = ℝ"
            };

            double vertexX = -b / (2 * a);
            double vertexY = EvaluatePoly(poly, vertexX);

            model.DerivativeFormula = $"y' = {FormatPoly(new double[] { 2 * a, b })}";

            model.Marks.Add(new VariationMark
            {
                X = double.NegativeInfinity,
                XLabel = "-∞",
                Defined = false,
                RightLimit = a > 0 ? double.PositiveInfinity : double.NegativeInfinity,
                RightLimitLabel = a > 0 ? "+∞" : "-∞"
            });

            model.Marks.Add(new VariationMark
            {
                X = vertexX,
                XLabel = FormatNumber(vertexX),
                Defined = true,
                DerivativeLabel = "0",
                Value = vertexY,
                ValueLabel = FormatNumber(vertexY),
                LeftLimit = vertexY,
                LeftLimitLabel = FormatNumber(vertexY),
                RightLimit = vertexY,
                RightLimitLabel = FormatNumber(vertexY)
            });

            model.Marks.Add(new VariationMark
            {
                X = double.PositiveInfinity,
                XLabel = "+∞",
                Defined = false,
                LeftLimit = a > 0 ? double.PositiveInfinity : double.NegativeInfinity,
                LeftLimitLabel = a > 0 ? "+∞" : "-∞"
            });

            model.Intervals.Add(new VariationInterval { Left = double.NegativeInfinity, Right = vertexX, Sign = a > 0 ? -1 : 1 });
            model.Intervals.Add(new VariationInterval { Left = vertexX, Right = double.PositiveInfinity, Sign = a > 0 ? 1 : -1 });

            model.AnalysisSummaryLines.Add($"• Tập xác định: {model.DomainDescription}");
            model.AnalysisSummaryLines.Add($"• Tọa độ đỉnh Parabol: I({FormatNumber(vertexX)}, {FormatNumber(vertexY)})");
            model.AnalysisSummaryLines.Add(a > 0 ? $"• Giá trị nhỏ nhất: y_min = {FormatNumber(vertexY)} tại x = {FormatNumber(vertexX)}" : $"• Giá trị lớn nhất: y_max = {FormatNumber(vertexY)} tại x = {FormatNumber(vertexX)}");

            return model;
        }

        // 6. HÀM BẬC 1: y = ax + b
        private static VariationModel AnalyzeLinear(double[] poly, string original)
        {
            double a = poly[0], b = poly[1];
            var model = new VariationModel
            {
                SourceExpression = original,
                FunctionFormula = $"y = {FormatPoly(poly)}",
                DomainDescription = "D = ℝ",
                DerivativeFormula = $"y' = {FormatNumber(a)}"
            };

            int sign = a > 0 ? 1 : -1;

            model.Marks.Add(new VariationMark
            {
                X = double.NegativeInfinity,
                XLabel = "-∞",
                Defined = false,
                RightLimit = a > 0 ? double.NegativeInfinity : double.PositiveInfinity,
                RightLimitLabel = a > 0 ? "-∞" : "+∞"
            });

            model.Marks.Add(new VariationMark
            {
                X = double.PositiveInfinity,
                XLabel = "+∞",
                Defined = false,
                LeftLimit = a > 0 ? double.PositiveInfinity : double.NegativeInfinity,
                LeftLimitLabel = a > 0 ? "+∞" : "-∞"
            });

            model.Intervals.Add(new VariationInterval { Left = double.NegativeInfinity, Right = double.PositiveInfinity, Sign = sign });

            model.AnalysisSummaryLines.Add($"• Tập xác định: {model.DomainDescription}");
            model.AnalysisSummaryLines.Add(a > 0 ? "• Hàm số luôn đồng biến trên ℝ" : "• Hàm số luôn nghịch biến trên ℝ");

            return model;
        }

        private static VariationModel AnalyzeGeneralQuartic(double[] poly, string original)
        {
            // Dự phòng cho hàm bậc 4 không trùng phương
            return AnalyzeFallbackPolynomial(poly, original);
        }

        private static VariationModel AnalyzeFallbackPolynomial(double[] poly, string original)
        {
            var model = new VariationModel
            {
                SourceExpression = original,
                FunctionFormula = $"y = {FormatPoly(poly)}",
                DomainDescription = "D = ℝ"
            };

            double a = poly[0];
            bool even = (poly.Length - 1) % 2 == 0;

            model.Marks.Add(new VariationMark
            {
                X = double.NegativeInfinity,
                XLabel = "-∞",
                Defined = false,
                RightLimit = even ? (a > 0 ? double.PositiveInfinity : double.NegativeInfinity) : (a > 0 ? double.NegativeInfinity : double.PositiveInfinity),
                RightLimitLabel = even ? (a > 0 ? "+∞" : "-∞") : (a > 0 ? "-∞" : "+∞")
            });

            model.Marks.Add(new VariationMark
            {
                X = 0,
                XLabel = "0",
                Defined = true,
                DerivativeLabel = "0",
                Value = poly.Last(),
                ValueLabel = FormatNumber(poly.Last()),
                LeftLimit = poly.Last(),
                LeftLimitLabel = FormatNumber(poly.Last()),
                RightLimit = poly.Last(),
                RightLimitLabel = FormatNumber(poly.Last())
            });

            model.Marks.Add(new VariationMark
            {
                X = double.PositiveInfinity,
                XLabel = "+∞",
                Defined = false,
                LeftLimit = a > 0 ? double.PositiveInfinity : double.NegativeInfinity,
                LeftLimitLabel = a > 0 ? "+∞" : "-∞"
            });

            model.Intervals.Add(new VariationInterval { Left = double.NegativeInfinity, Right = 0, Sign = a > 0 ? -1 : 1 });
            model.Intervals.Add(new VariationInterval { Left = 0, Right = double.PositiveInfinity, Sign = a > 0 ? 1 : -1 });

            return model;
        }

        #endregion
    }
}

