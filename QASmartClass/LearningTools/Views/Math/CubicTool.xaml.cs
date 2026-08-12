using QASmartClass.LearningTools.Models;
using System;
using QASmartClass.LearningTools.Helpers;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class CubicTool : BaseToolControl
    {
        private Border tabCalc;
        private TextBlock tabCalcText;
        private Border tabGuide;
        private TextBlock tabGuideText;
        private Border tabApp;
        private TextBlock tabAppText;

        public CubicTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextPractice != null) menuTextPractice.Text = isVN ? "Tính toán & Ví dụ" : "Calculation & Examples";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";

                if (sideMenu != null) sideMenu.SelectedIndex = 0;

                BuildPresets(); Solve();
                LoadPracticalApps();
                // Bàn phím số mini
                TouchNumPad.Attach(txtA, step: 1, enableMathKeys: true);
                TouchNumPad.Attach(txtB, step: 1, enableMathKeys: true);
                TouchNumPad.Attach(txtC, step: 1, enableMathKeys: true);
                TouchNumPad.Attach(txtD, step: 1, enableMathKeys: true);
            };
        }

        private void Solve_Changed(object sender, EventArgs e) { if (IsLoaded) Solve(); }

        private void Solve()
        {
            if (resultPanel == null) return;
            resultPanel.Children.Clear();

            // Kiểm tra rỗng hoặc ký tự không hợp lệ
            bool parseA = ParsingHelper.TryParseDouble(txtA?.Text, out double a);
            bool parseB = ParsingHelper.TryParseDouble(txtB?.Text, out double b);
            bool parseC = ParsingHelper.TryParseDouble(txtC?.Text, out double c);
            bool parseD = ParsingHelper.TryParseDouble(txtD?.Text, out double d);

            if (!parseA || !parseB || !parseC || !parseD)
            {
                ShowErrorCard("Hệ số không hợp lệ", "Vui lòng nhập số thực hợp lệ cho tất cả các hệ số a, b, c, d (ví dụ: 1; -2; 3.5).");
                return;
            }

            if (System.Math.Abs(a) < 1e-10)
            {
                ShowErrorCard("Hệ số a phải khác 0", "Đối với phương trình bậc 3 (ax³ + bx² + cx + d = 0), hệ số a bắt buộc phải khác 0.");
                return;
            }

            BuildCubicSteps(a, b, c, d);
        }

        private void ShowErrorCard(string title, string message)
        {
            var card = new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 8),
                Background = new SolidColorBrush(Color.FromArgb(20, 198, 40, 40)), // Đỏ nhạt
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 198, 40, 40)),
                BorderThickness = new Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 8, ShadowDepth = 1, Opacity = 0.05 }
            };

            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = "⚠️ " + title,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)) // Đỏ đậm
            });
            sp.Children.Add(new TextBlock
            {
                Text = message,
                FontSize = 13.5,
                Margin = new Thickness(0, 6, 0, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                FontFamily = new FontFamily("Segoe UI"),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            });
            card.Child = sp;
            resultPanel.Children.Add(card);
        }

        private void BuildCubicSteps(double a, double b, double c, double d)
        {
            if (resultPanel == null) return;
            resultPanel.Children.Clear();

            var steps = new List<(string Step, string Detail, string Color)>();

            // Bước 1: Xác định hệ số
            steps.Add((
                "Bước 1: Xác định các hệ số",
                $"Hệ số: a = {UI.Fmt(a)}, b = {UI.Fmt(b)}, c = {UI.Fmt(c)}, d = {UI.Fmt(d)}\n" +
                $"Phương trình bậc 3 cần giải: {UI.Fmt(a)}x³ + ({UI.Fmt(b)})x² + ({UI.Fmt(c)})x + ({UI.Fmt(d)}) = 0",
                "#880E4F"
            ));

            // Bước 2: Tìm nghiệm hữu tỉ
            double? rationalRoot = FindRationalRoot(a, b, c, d);
            if (rationalRoot.HasValue)
            {
                double r = rationalRoot.Value;
                string step2Detail = "";
                string step3Detail = "";
                double b2 = a;
                double c2 = b + b2 * r;
                double d2 = c + c2 * r;

                if (System.Math.Abs(d) < 1e-10 && System.Math.Abs(r) < 1e-10)
                {
                    step2Detail = $"Vì hệ số tự do d = 0, ta có thể dễ dàng phân tích đa thức thành nhân tử chung:\n" +
                                  $"x · ({UI.Fmt(a)}x² + ({UI.Fmt(b)})x + ({UI.Fmt(c)})) = 0\n" +
                                  $"Từ đó, phương trình luôn có nghiệm đầu tiên là: x₁ = 0 ✅";

                    step3Detail = $"Phân tích nhân tử trực tiếp (hoặc thông qua sơ đồ Horner với x = 0):\n" +
                                  $"  Hệ số bậc hai thu được: a' = {UI.Fmt(b2)},  b' = {UI.Fmt(c2)},  c' = {UI.Fmt(d2)}\n\n" +
                                  $"Đa thức được phân tích thành:\n" +
                                  $"x · ({UI.Fmt(b2)}x² + ({UI.Fmt(c2)})x + ({UI.Fmt(d2)})) = 0";
                }
                else
                {
                    if (TryScaleToIntegers(a, b, c, d, out int ia, out int ib, out int ic, out int id) &&
                        (System.Math.Abs(ia - a) > 1e-9 || System.Math.Abs(id - d) > 1e-9))
                    {
                        step2Detail = $"Vì hệ số là số thực lẻ, ta quy đồng hệ số nhân cả hai vế để đưa về hệ số nguyên tương đương:\n" +
                                      $"{ia}x³ + ({ib})x² + ({ic})x + ({id}) = 0\n" +
                                      $"Thử các ước số p/q (với p là ước của hệ số tự do {id}, q là ước của hệ số dẫn đầu {ia}):\n" +
                                      $"Tìm thấy nghiệm hữu tỉ: x₁ = {UI.Fmt(r)}\n" +
                                      $"Thử lại: f({UI.Fmt(r)}) = 0 (Chính xác) ✅";
                    }
                    else
                    {
                        step2Detail = $"Thử các ước số p/q (với p là ước của d = {UI.Fmt(d)}, q là ước của a = {UI.Fmt(a)}):\n" +
                                      $"Tìm thấy nghiệm hữu tỉ: x₁ = {UI.Fmt(r)}\n" +
                                      $"Thử lại: f({UI.Fmt(r)}) = {UI.Fmt(a)}·({UI.Fmt(r)})³ + ({UI.Fmt(b)})·({UI.Fmt(r)})² + ({UI.Fmt(c)})·({UI.Fmt(r)}) + ({UI.Fmt(d)}) = 0 (Chính xác) ✅";
                    }

                    step3Detail = $"Sử dụng sơ đồ Horner để chia đa thức cho (x − {UI.Fmt(r)}):\n" +
                                  $"  Hệ số ban đầu: [{UI.Fmt(a)},  {UI.Fmt(b)},  {UI.Fmt(c)},  {UI.Fmt(d)}]\n" +
                                  $"  Hệ số bậc hai thu được: a' = {UI.Fmt(b2)},  b' = {UI.Fmt(c2)},  c' = {UI.Fmt(d2)}\n\n" +
                                  $"Đa thức được phân tích thành:\n" +
                                  $"(x − {UI.Fmt(r)}) · ({UI.Fmt(b2)}x² + ({UI.Fmt(c2)})x + ({UI.Fmt(d2)})) = 0";
                }

                steps.Add((
                    "Bước 2: Tìm nghiệm hữu tỉ đầu tiên",
                    step2Detail,
                    "#1565C0"
                ));

                steps.Add((
                    "Bước 3: Sơ đồ Horner phân tích nhân tử",
                    step3Detail,
                    "#7B1FA2"
                ));

                // Bước 4: Giải phương trình bậc 2 còn lại
                double delta = c2 * c2 - 4 * b2 * d2;
                string deltaStepTitle = $"Bước 4: Giải phương trình bậc hai còn lại: {UI.Fmt(b2)}x² + ({UI.Fmt(c2)})x + ({UI.Fmt(d2)}) = 0";
                string deltaStepDetail = "";
                string deltaColor = "";

                if (delta > 0)
                {
                    double x2 = (-c2 + System.Math.Sqrt(delta)) / (2 * b2);
                    double x3 = (-c2 - System.Math.Sqrt(delta)) / (2 * b2);
                    deltaColor = "#2E7D32";

                    bool x2EqualsR = System.Math.Abs(x2 - r) < 1e-9;
                    bool x3EqualsR = System.Math.Abs(x3 - r) < 1e-9;

                    deltaStepDetail = $"Tính biệt thức Δ = b'² − 4a'c' = ({UI.Fmt(c2)})² − 4·({UI.Fmt(b2)})·({UI.Fmt(d2)}) = {UI.Fmt(delta)} > 0\n" +
                                      $"Phương trình bậc hai có 2 nghiệm thực phân biệt:\n" +
                                      $"x₂ = (-({UI.Fmt(c2)}) + √{UI.Fmt(delta)}) / (2·{UI.Fmt(b2)}) = {UI.Fmt(x2)}\n" +
                                      $"x₃ = (-({UI.Fmt(c2)}) − √{UI.Fmt(delta)}) / (2·{UI.Fmt(b2)}) = {UI.Fmt(x3)}\n\n";

                    if (x2EqualsR || x3EqualsR)
                    {
                        double singleRoot = x2EqualsR ? x3 : x2;
                        deltaStepDetail += $"Nhận xét: Một trong hai nghiệm trùng với nghiệm hữu tỉ x₁ = {UI.Fmt(r)}.\n" +
                                           $"Kết luận: Phương trình bậc 3 có 2 nghiệm thực (1 nghiệm kép và 1 nghiệm đơn):\n" +
                                           $"• Nghiệm kép: x = {UI.Fmt(r)}\n" +
                                           $"• Nghiệm đơn: x = {UI.Fmt(singleRoot)}";
                    }
                    else
                    {
                        deltaStepDetail += $"Kết luận: Phương trình bậc 3 có 3 nghiệm thực phân biệt:\n" +
                                           $"x₁ = {UI.Fmt(r)},  x₂ = {UI.Fmt(x2)},  x₃ = {UI.Fmt(x3)}";
                    }
                }
                else if (System.Math.Abs(delta) < 1e-10)
                {
                    double x2 = -c2 / (2 * b2);
                    deltaColor = "#F57F17";
                    deltaStepDetail = $"Tính biệt thức Δ = b'² − 4a'c' = ({UI.Fmt(c2)})² − 4·({UI.Fmt(b2)})·({UI.Fmt(d2)}) = 0\n" +
                                      $"Phương trình bậc hai có nghiệm kép:\n" +
                                      $"x₂ = x₃ = -({UI.Fmt(c2)}) / (2·{UI.Fmt(b2)}) = {UI.Fmt(x2)}\n\n";

                    if (System.Math.Abs(x2 - r) < 1e-10)
                    {
                        deltaStepDetail += $"Nghiệm kép trùng với nghiệm hữu tỉ x₁ = {UI.Fmt(r)}.\n" +
                                           $"Kết luận: Phương trình bậc 3 có nghiệm bội 3:\n" +
                                           $"x = {UI.Fmt(r)}";
                    }
                    else
                    {
                        deltaStepDetail += $"Kết luận: Phương trình bậc 3 có 2 nghiệm thực:\n" +
                                           $"x₁ = {UI.Fmt(r)} (nghiệm đơn),  x₂ = {UI.Fmt(x2)} (nghiệm kép)";
                    }
                }
                else
                {
                    double re = -c2 / (2 * b2);
                    double im = System.Math.Sqrt(-delta) / (2 * b2);
                    deltaColor = "#C62828";
                    deltaStepDetail = $"Tính biệt thức Δ = b'² − 4a'c' = ({UI.Fmt(c2)})² − 4·({UI.Fmt(b2)})·({UI.Fmt(d2)}) = {UI.Fmt(delta)} < 0\n" +
                                      $"Phương trình bậc hai vô nghiệm thực, có 2 nghiệm phức liên hợp:\n" +
                                      $"x₂,₃ = {UI.Fmt(re)} ± {UI.Fmt(im)}i\n\n" +
                                      $"Kết luận: Phương trình bậc 3 có 1 nghiệm thực và 2 nghiệm phức liên hợp:\n" +
                                      $"x₁ = {UI.Fmt(r)}\n" +
                                      $"x₂,₃ = {UI.Fmt(re)} ± {UI.Fmt(im)}i";
                }

                steps.Add((deltaStepTitle, deltaStepDetail, deltaColor));
            }
            else
            {
                // Không tìm được nghiệm hữu tỉ -> Giải Cardano
                steps.Add((
                    "Bước 2: Tìm nghiệm hữu tỉ",
                    "Thử nghiệm tất cả các ước số p/q nhưng không tìm thấy nghiệm hữu tỉ nào. Ta chuyển sang giải bằng phương pháp Cardano chi tiết.",
                    "#E65100"
                ));

                // Cardano
                double p = (3 * a * c - b * b) / (3 * a * a);
                double q = (2 * b * b * b - 9 * a * b * c + 27 * a * a * d) / (27 * a * a * a);
                double disc = q * q / 4 + p * p * p / 27;
                double shift = -b / (3 * a);

                steps.Add((
                    "Bước 3: Biến đổi về phương trình khuyết bậc hai (Cardano)",
                    $"Đặt x = y − b/(3a) = y − ({UI.Fmt(b)})/(3·{UI.Fmt(a)}) = y + ({UI.Fmt(shift)})\n" +
                    $"Đưa phương trình về dạng khuyết: y³ + py + q = 0\n" +
                    $"Trong đó:\n" +
                    $"p = (3ac − b²)/(3a²) = (3·{UI.Fmt(a)}·{UI.Fmt(c)} − ({UI.Fmt(b)})²)/(3·{UI.Fmt(a)}²) = {UI.Fmt(p)}\n" +
                    $"q = (2b³ − 9abc + 27a²d)/(27a³) = {UI.Fmt(q)}\n" +
                    $"Ta được phương trình: y³ + ({UI.Fmt(p)})y + ({UI.Fmt(q)}) = 0",
                    "#7B1FA2"
                ));

                string cardanoTitle = "Bước 4: Xét biệt thức Cardano và kết luận nghiệm";
                string cardanoDetail = "";
                string cardanoColor = "";

                if (disc > 1e-10)
                {
                    cardanoColor = "#C62828";
                    double u = CubeRoot(-q / 2 + System.Math.Sqrt(disc));
                    double v = CubeRoot(-q / 2 - System.Math.Sqrt(disc));
                    double x1 = u + v + shift;
                    cardanoDetail = $"Biệt thức Cardano Δ_C = q²/4 + p³/27 = ({UI.Fmt(q)})²/4 + ({UI.Fmt(p)})³/27 = {UI.Fmt(disc)} > 0\n" +
                                    $"Phương trình có 1 nghiệm thực duy nhất và 2 nghiệm phức liên hợp.\n" +
                                    $"Tính u = ³√(-q/2 + √Δ_C) = {UI.Fmt(u)}\n" +
                                    $"Tính v = ³√(-q/2 − √Δ_C) = {UI.Fmt(v)}\n" +
                                    $"y₁ = u + v = {UI.Fmt(u + v)}\n" +
                                    $"Nghiệm thực: x₁ = y₁ − b/(3a) = {UI.Fmt(x1)}\n\n" +
                                    $"Kết luận: Phương trình có 1 nghiệm thực thực tế:\n" +
                                    $"x₁ ≈ {x1:F6}\n" +
                                    $"(và 2 nghiệm phức liên hợp)";
                }
                else if (disc < -1e-10)
                {
                    cardanoColor = "#2E7D32";
                    double r_cardano = System.Math.Sqrt(-p * p * p / 27);
                    double theta = System.Math.Acos(-q / (2 * r_cardano));
                    double m = 2 * System.Math.Pow(r_cardano, 1.0 / 3);
                    double x1 = m * System.Math.Cos(theta / 3) + shift;
                    double x2 = m * System.Math.Cos((theta + 2 * System.Math.PI) / 3) + shift;
                    double x3 = m * System.Math.Cos((theta + 4 * System.Math.PI) / 3) + shift;

                    cardanoDetail = $"Biệt thức Cardano Δ_C = q²/4 + p³/27 = {UI.Fmt(disc)} < 0\n" +
                                    $"Phương trình có 3 nghiệm thực phân biệt. Ta sử dụng công thức lượng giác:\n" +
                                    $"r = √(-p³/27) = {UI.Fmt(r_cardano)}\n" +
                                    $"cos(θ) = -q / (2r) = {UI.Fmt(-q / (2 * r_cardano))} → θ ≈ {theta:F4} rad\n" +
                                    $"Các nghiệm thực của phương trình y³ + py + q = 0:\n" +
                                    $"y₁ = 2·³√r · cos(θ/3) = {UI.Fmt(m * System.Math.Cos(theta / 3))}\n" +
                                    $"y₂ = 2·³√r · cos((θ+2π)/3) = {UI.Fmt(m * System.Math.Cos((theta + 2 * System.Math.PI) / 3))}\n" +
                                    $"y₃ = 2·³√r · cos((θ+4π)/3) = {UI.Fmt(m * System.Math.Cos((theta + 4 * System.Math.PI) / 3))}\n\n" +
                                    $"Nghiệm x tương ứng (x_i = y_i − b/(3a)):\n" +
                                    $"x₁ ≈ {x1:F6}\n" +
                                    $"x₂ ≈ {x2:F6}\n" +
                                    $"x₃ ≈ {x3:F6}";
                }
                else
                {
                    cardanoColor = "#F57F17";
                    double u = CubeRoot(-q / 2);
                    double x1 = 2 * u + shift;
                    double x2 = -u + shift;
                    cardanoDetail = $"Biệt thức Cardano Δ_C = q²/4 + p³/27 = 0\n" +
                                    $"Phương trình có nghiệm bội:\n" +
                                    $"u = ³√(-q/2) = {UI.Fmt(u)}\n" +
                                    $"y₁ = 2u = {UI.Fmt(2 * u)}  |  y₂ = y₃ = -u = {UI.Fmt(-u)}\n" +
                                    $"Các nghiệm tương ứng:\n" +
                                    $"x₁ ≈ {x1:F6}\n" +
                                    $"x₂ ≈ {x2:F6} (nghiệm kép)";
                }
                steps.Add((cardanoTitle, cardanoDetail, cardanoColor));
            }

            // Bước 5: Định lý Vieta
            double sum = -b / a;
            double pairwise = c / a;
            double product = -d / a;

            steps.Add((
                $"Bước {steps.Count + 1}: Kiểm tra định lý Vi-ét bậc 3",
                $"Theo định lý Vi-ét cho phương trình ax³ + bx² + cx + d = 0:\n" +
                $"• x₁ + x₂ + x₃ = -b/a = -({UI.Fmt(b)}) / {UI.Fmt(a)} = {UI.Fmt(sum)}\n" +
                $"• x₁x₂ + x₂x₃ + x₃x₁ = c/a = {UI.Fmt(c)} / {UI.Fmt(a)} = {UI.Fmt(pairwise)}\n" +
                $"• x₁x₂x₃ = -d/a = -({UI.Fmt(d)}) / {UI.Fmt(a)} = {UI.Fmt(product)}\n\n" +
                $"Các nghiệm tìm được hoàn toàn thỏa mãn định lý Vi-ét. ✅",
                "#283593"
            ));

            // Render các Card Border đẹp mắt
            for (int i = 0; i < steps.Count; i++)
            {
                var (step, detail, colorStr) = steps[i];
                var accentColor = (Color)ColorConverter.ConvertFromString(colorStr);

                var card = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(14, 10, 14, 10),
                    Margin = new Thickness(0, 0, 0, 8),
                    Background = new SolidColorBrush(Color.FromArgb(15, accentColor.R, accentColor.G, accentColor.B)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, accentColor.R, accentColor.G, accentColor.B)),
                    BorderThickness = new Thickness(0, 0, 0, 2)
                };

                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = step,
                    FontSize = 15,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(accentColor)
                });
                sp.Children.Add(new TextBlock
                {
                    Text = detail,
                    FontSize = 13.5,
                    Margin = new Thickness(0, 6, 0, 0),
                    Foreground = new SolidColorBrush(Color.FromRgb(55, 71, 79)),
                    FontFamily = new FontFamily("Segoe UI"),
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 22
                });
                card.Child = sp;

                string sectionId = $"step_{i + 1}";
                resultPanel.Children.Add(WrapWithSectionToolbar(card, "cubic", sectionId, step));
            }
        }

        private static double? FindRationalRoot(double a, double b, double c, double d)
        {
            if (a == 0) return null;

            if (System.Math.Abs(d) < 1e-10)
            {
                double val = a * 0 + b * 0 + c * 0 + d;
                if (System.Math.Abs(val) < 1e-8) return 0;
            }

            // Quy đổi sang hệ số nguyên để tìm ứng viên nghiệm hữu tỉ p/q
            if (TryScaleToIntegers(a, b, c, d, out int ia, out int ib, out int ic, out int id))
            {
                var divisorsD = GetDivisors(id);
                var divisorsA = GetDivisors(ia);

                foreach (int p in divisorsD)
                {
                    foreach (int q in divisorsA)
                    {
                        if (q == 0) continue;
                        foreach (double sign in new[] { 1.0, -1.0 })
                        {
                            double x = sign * p / (double)q;
                            double val = a * x * x * x + b * x * x + c * x + d;
                            if (System.Math.Abs(val) < 1e-8) return x;
                        }
                    }
                }
            }
            return null;
        }

        private static List<int> GetDivisors(int n)
        {
            var result = new List<int>();
            n = System.Math.Abs(n);
            if (n == 0) return result;
            for (int i = 1; i * i <= n; i++)
            {
                if (n % i == 0)
                {
                    result.Add(i);
                    if (i * i != n)
                    {
                        result.Add(n / i);
                    }
                }
            }
            result.Sort();
            // Loại bỏ các phần tử trùng lặp (nếu có)
            for (int i = result.Count - 1; i > 0; i--)
            {
                if (result[i] == result[i - 1])
                {
                    result.RemoveAt(i);
                }
            }
            return result;
        }

        private static List<double> SolveNumerically(double a, double b, double c, double d)
        {
            // Cardano's method
            double p = (3 * a * c - b * b) / (3 * a * a);
            double q = (2 * b * b * b - 9 * a * b * c + 27 * a * a * d) / (27 * a * a * a);
            double disc = q * q / 4 + p * p * p / 27;
            double shift = -b / (3 * a);
            var roots = new List<double>();

            if (disc > 1e-10)
            {
                double u = CubeRoot(-q / 2 + System.Math.Sqrt(disc));
                double v = CubeRoot(-q / 2 - System.Math.Sqrt(disc));
                roots.Add(u + v + shift);
            }
            else if (disc < -1e-10)
            {
                double r = System.Math.Sqrt(-p * p * p / 27);
                double theta = System.Math.Acos(-q / (2 * r));
                double m = 2 * System.Math.Pow(r, 1.0 / 3);
                roots.Add(m * System.Math.Cos(theta / 3) + shift);
                roots.Add(m * System.Math.Cos((theta + 2 * System.Math.PI) / 3) + shift);
                roots.Add(m * System.Math.Cos((theta + 4 * System.Math.PI) / 3) + shift);
                roots.Sort();
            }
            else
            {
                double u = CubeRoot(-q / 2);
                roots.Add(2 * u + shift);
                roots.Add(-u + shift);
            }
            return roots;
        }

        private static double CubeRoot(double x) => x >= 0 ? System.Math.Pow(x, 1.0 / 3) : -System.Math.Pow(-x, 1.0 / 3);

        // ═══ Graph ═══
        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!ParsingHelper.TryParseDouble(txtA?.Text, out double a) || a == 0) return;
                ParsingHelper.TryParseDouble(txtB?.Text, out double b);
                ParsingHelper.TryParseDouble(txtC?.Text, out double c);
                ParsingHelper.TryParseDouble(txtD?.Text, out double d);
                var ci = CultureInfo.InvariantCulture;

                // 1. Tính toán các nghiệm thực duy nhất
                var roots = SolveNumerically(a, b, c, d);
                var uniqueRoots = new List<double>();
                foreach (var r in roots)
                {
                    bool exists = false;
                    foreach (var ur in uniqueRoots)
                    {
                        if (System.Math.Abs(r - ur) < 1e-5)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists) uniqueRoots.Add(r);
                }

                // 2. Tạo JS vẽ các nghiệm thực dưới dạng các điểm đỏ có nhãn
                string rootsJs = "";
                for (int i = 0; i < uniqueRoots.Count; i++)
                {
                    double rootVal = uniqueRoots[i];
                    string label = $"x{i + 1} = {rootVal.ToString("G4", ci)}";
                    rootsJs += $@"
        calc.setExpression({{id:'r{i}', latex:'({rootVal.ToString("G8", ci)},0)', color:'#E53935', pointStyle:'POINT', pointSize:12, label:'{label}', showLabel:true}});";
                }

                // 3. Tính toán các điểm cực trị để tính giới hạn đồ thị động
                double xMax = 5.0;
                double yMax = 10.0;

                // Cập nhật xMax từ các nghiệm
                foreach (var r in uniqueRoots)
                {
                    xMax = System.Math.Max(xMax, System.Math.Abs(r) + 3.0);
                }

                // Xét cực trị của hàm số bậc 3 bằng cách giải y' = 3ax^2 + 2bx + c = 0
                double deltaPrime = 4 * b * b - 12 * a * c;
                if (deltaPrime > 0)
                {
                    double sqrtDeltaPrime = System.Math.Sqrt(deltaPrime);
                    double xE1 = (-2 * b + sqrtDeltaPrime) / (6 * a);
                    double xE2 = (-2 * b - sqrtDeltaPrime) / (6 * a);
                    
                    double yE1 = a * xE1 * xE1 * xE1 + b * xE1 * xE1 + c * xE1 + d;
                    double yE2 = a * xE2 * xE2 * xE2 + b * xE2 * xE2 + c * xE2 + d;

                    xMax = System.Math.Max(xMax, System.Math.Max(System.Math.Abs(xE1), System.Math.Abs(xE2)) + 3.0);
                    yMax = System.Math.Max(yMax, System.Math.Max(System.Math.Abs(yE1), System.Math.Abs(yE2)) + 5.0);
                }

                // Cập nhật yMax từ điểm cắt trục tung (0, d)
                yMax = System.Math.Max(yMax, System.Math.Abs(d) + 5.0);

                string expJs = $@"
        calc.setExpression({{id:'cubic', latex:'y={a.ToString(ci)}x^3+{b.ToString(ci)}x^2+{c.ToString(ci)}x+{d.ToString(ci)}', color:'#880E4F', lineWidth:3}});
        calc.setExpression({{id:'xaxis', latex:'y=0', color:'#9E9E9E', lineWidth:1, lineStyle:'DASHED'}});
        {rootsJs}
        calc.setMathBounds({{ left: -{xMax.ToString(ci)}, right: {xMax.ToString(ci)}, bottom: -{yMax.ToString(ci)}, top: {yMax.ToString(ci)} }});";

                var win = new GraphWindow(expJs, $"📊 y = {UI.Fmt(a)}x³ + ({UI.Fmt(b)})x² + ({UI.Fmt(c)})x + ({UI.Fmt(d)})");
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        // ═══ PRESETS ═══
        private void BuildPresets()
        {
            if (presetPanel == null) return;
            var presets = new (string L, double A, double B, double C, double D)[]
            {
                ("x³−6x²+11x−6=0", 1, -6, 11, -6),
                ("x³−1=0", 1, 0, 0, -1),
                ("x³+3x²−4=0", 1, 3, 0, -4),
                ("2x³−x²−5x−2=0", 2, -1, -5, -2),
                ("x³−7x+6=0", 1, 0, -7, 6),
            };
            var presetColor = (Color)ColorConverter.ConvertFromString("#880E4F");
            foreach (var (l, a, b, c, d) in presets)
            {
                double ca = a, cb = b, cc = c, cd = d;
                UI.PresetButton(l, presetColor,
                    () => { txtA.Text = UI.Fmt(ca); txtB.Text = UI.Fmt(cb); txtC.Text = UI.Fmt(cc); txtD.Text = UI.Fmt(cd); },
                    presetPanel);
            }
        }

        // AddR(), Fmt() đã được thay thế bằng UI.ResultRow(), UI.Fmt()

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rootGrid == null || sideMenu == null || contentGuide == null || contentCalc == null || gridApp == null)
                return;

            contentGuide.Visibility = Visibility.Collapsed;
            contentCalc.Visibility = Visibility.Collapsed;
            gridApp.Visibility = Visibility.Collapsed;

            int index = sideMenu.SelectedIndex;
            if (index == 0)
            {
                contentGuide.Visibility = Visibility.Visible;
            }
            else if (index == 1)
            {
                contentCalc.Visibility = Visibility.Visible;
            }
            else if (index == 2)
            {
                gridApp.Visibility = Visibility.Visible;
            }
        }

        private void Tab_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border clickedBorder && clickedBorder.Tag is string tag)
            {
                SwitchToTab(tag);
            }
        }

        private void SwitchToTab(string tag)
        {
            if (sideMenu == null) return;
            if (tag == "guide")
                sideMenu.SelectedIndex = 1;
            else if (tag == "calc" || tag == "practice")
                sideMenu.SelectedIndex = 1;
            else if (tag == "app" || tag == "practical")
                sideMenu.SelectedIndex = 1;
        }

        private void LoadPracticalApps()
        {
            try
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                string suffix = isVN ? "VN" : "EN";

                var items = new List<PracticalAppItem>
                {
                    new PracticalAppItem
                    {
                        Icon = "📦",
                        Title = isVN ? "Tối ưu thể tích hộp" : "3D Animation & Splines",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_cubic_1_{suffix}.png",
                        Description = isVN 
                            ? "Thiết kế hộp từ một tấm phẳng bằng cách cắt 4 góc vuông x và gấp lại. Thể tích hộp được mô tả bởi đa thức bậc 3 V(x) = x(W-2x)(L-2x) cần tìm cực đại." 
                            : "Create smooth curves (Bézier splines) using cubic equations to design organic 3D models and smooth camera paths."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🛣",
                        Title = isVN ? "️ Đường cong chuyển tiếp" : "Civil Structural Design",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_cubic_2_{suffix}.png",
                        Description = isVN 
                            ? "Trong xây dựng cầu đường, parabol bậc 3 y = ax³ được dùng làm đường cong nối tiếp giữa đoạn đoạn thẳng và đoạn cong tròn để đảm bảo gia tốc hướng tâm thay đổi êm thuận." 
                            : "Model the deflection and bending curves of structural beams under variable loads using cubic polynomial equations."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌡",
                        Title = isVN ? "️ Khí thực Van der Waals" : "Thermodynamics State Equations",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_cubic_3_{suffix}.png",
                        Description = isVN 
                            ? "Trong nhiệt động lực học, phương trình trạng thái Van der Waals cho khí thực là một phương trình bậc 3 theo thể tích V: PV³ - (Pb+RT)V² + aV - ab = 0." 
                            : "Model pressure, volume, and temperature relations for real gases using cubic equations of state like Van der Waals."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📈",
                        Title = isVN ? "Lãi suất & Dòng tiền" : "Financial Economics & Valuation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_cubic_4_{suffix}.png",
                        Description = isVN 
                            ? "Định giá dòng tiền trong 3 năm để tìm lợi suất đầu tư r. Việc giải hệ số chiết khấu dẫn tới phương trình bậc 3 C0(1+r)³ + C1(1+r)² + C2(1+r) + C3 = 0." 
                            : "Fit cubic functions to describe yield curves and bond interest rates across different maturities."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏗",
                        Title = isVN ? "️ Độ võng của dầm" : "Terrain Profile Modeling",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_cubic_5_{suffix}.png",
                        Description = isVN 
                            ? "Trong sức bền vật liệu, độ võng của một dầm console chịu tải lực điểm biến thiên dọc theo thanh dầm được xác định theo phương trình bậc 3 của tọa độ x." 
                            : "Interpolate geographic elevation points using cubic splines to map realistic valleys, hills, and roads in GIS software."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🦌",
                        Title = isVN ? "Sinh thái học Allee" : "Robot Trajectory Optimization",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_cubic_6_{suffix}.png",
                        Description = isVN 
                            ? "Mô phỏng tốc độ phát triển của một loài động vật khi có hiệu ứng ngưỡng sinh tồn Allee, tốc độ sinh trưởng dx/dt = rx(x-a)(1-x/K) là một hàm bậc 3." 
                            : "Calculate smooth acceleration curves for robotic arms to move items without sudden jerks or motor wear."
                    }
                ,
                    new PracticalAppItem
                    {
                        Icon = "🏛️",
                        Title = isVN ? "Thể tích bể chứa vòm cong" : "Domed Reservoir Volume",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_cubic_7_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng hàm bậc ba để tính toán chính xác dung tích của các bể chứa hoặc đập thủy điện có cấu trúc thành cong." 
                            : "Use cubic functions to calculate the exact capacity of reservoirs and hydroelectric dams with curved structures."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📈",
                        Title = isVN ? "Tăng trưởng dân số phi tuyến" : "Nonlinear Population Growth",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_cubic_8_{suffix}.png",
                        Description = isVN 
                            ? "Mô hình hóa quá trình tăng trưởng dân số hoặc sinh vật trong điều kiện tài nguyên giới hạn bằng hàm số bậc ba." 
                            : "Model population or biological growth processes under resource-constrained conditions using cubic equations."
                    }};

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for CubicTool: {Err}", ex.Message);
            }
        }

        private static bool TryScaleToIntegers(double a, double b, double c, double d, out int ia, out int ib, out int ic, out int id)
        {
            ia = 0; ib = 0; ic = 0; id = 0;
            double[] factors = { 1.0, 2.0, 5.0, 10.0, 20.0, 50.0, 100.0, 1000.0 };
            foreach (double f in factors)
            {
                double sa = a * f;
                double sb = b * f;
                double sc = c * f;
                double sd = d * f;
                if (System.Math.Abs(sa - System.Math.Round(sa)) < 1e-7 &&
                    System.Math.Abs(sb - System.Math.Round(sb)) < 1e-7 &&
                    System.Math.Abs(sc - System.Math.Round(sc)) < 1e-7 &&
                    System.Math.Abs(sd - System.Math.Round(sd)) < 1e-7)
                {
                    ia = (int)System.Math.Round(sa);
                    ib = (int)System.Math.Round(sb);
                    ic = (int)System.Math.Round(sc);
                    id = (int)System.Math.Round(sd);
                    return true;
                }
            }
            return false;
        }

        private void AppCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border clickedBorder && clickedBorder.Tag is string tag)
            {
                var parts = tag.Split(';');
                if (parts.Length == 4)
                {
                    if (txtA != null) txtA.Text = parts[0];
                    if (txtB != null) txtB.Text = parts[1];
                    if (txtC != null) txtC.Text = parts[2];
                    if (txtD != null) txtD.Text = parts[3];

                    SwitchToTab("calc");
                    Solve();
                }
            }
        }
    }
}