using QASmartClass.LearningTools.Models;
using System;
using QASmartClass.LearningTools.Helpers;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class ComplexNumberTool : BaseToolControl
    {
        private enum Mode { Arithmetic, Properties, PowerRoots }
        private Mode _mode = Mode.Arithmetic;

        public ComplexNumberTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                menuTextPractice.Text = isVN ? "Tính toán & Ví dụ" : "Calculation & Examples";
                menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";
                
                sideMenu.SelectedIndex = 0;

                BuildModeButtons();
                UpdateUI();
                BuildFormulas();
                LoadPracticalApps();

                // Bàn phím số mini
                TouchNumPad.Attach(txtA1, step: 1);
                TouchNumPad.Attach(txtB1, step: 1);
                TouchNumPad.Attach(txtA2, step: 1);
                TouchNumPad.Attach(txtB2, step: 1);
                TouchNumPad.Attach(txtSA, step: 1);
                TouchNumPad.Attach(txtSB, step: 1);
                TouchNumPad.Attach(txtN, step: 1);
            };
        }

        // ═══ MODE SELECTOR ═══
        private void BuildModeButtons()
        {
            modePanel.Children.Clear();
            var modes = new (string Label, Mode M)[]
            {
                ("➕ Phép toán z₁ ⊕ z₂", Mode.Arithmetic),
                ("📐 Mô-đun — Acgumen", Mode.Properties),
                ("⚡ Lũy thừa & Căn thức", Mode.PowerRoots),
            };

            var accent = DS.CatMath; // Màu xanh dương toán học chủ đạo
            foreach (var (label, m) in modes)
            {
                bool active = m == _mode;
                var btn = new Border
                {
                    Background = new SolidColorBrush(active ? accent : Color.FromArgb(20, accent.R, accent.G, accent.B)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(18, 10, 18, 10),
                    Margin = new Thickness(0, 0, 8, 6),
                    Cursor = Cursors.Hand,
                    BorderBrush = new SolidColorBrush(accent),
                    BorderThickness = new Thickness(active ? 0 : 1)
                };
                btn.Child = new TextBlock
                {
                    Text = label, FontSize = 16, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(active ? Colors.White : accent),
                    FontFamily = new FontFamily("Segoe UI")
                };
                var cm = m;
                btn.MouseLeftButtonDown += (_, _) =>
                {
                    _mode = cm;
                    BuildModeButtons();
                    UpdateUI();
                };
                modePanel.Children.Add(btn);
            }
        }

        private void UpdateUI()
        {
            twoInput.Visibility = _mode == Mode.Arithmetic ? Visibility.Visible : Visibility.Collapsed;
            singleInput.Visibility = (_mode == Mode.Properties || _mode == Mode.PowerRoots) ? Visibility.Visible : Visibility.Collapsed;
            powerInput.Visibility = _mode == Mode.PowerRoots ? Visibility.Visible : Visibility.Collapsed;

            txtModeTitle.Text = _mode switch
            {
                Mode.Arithmetic => "➕ Phép toán — z₁ + z₂, z₁ − z₂, z₁ × z₂, z₁ / z₂",
                Mode.Properties => "📐 Mô-đun |z|, Acgumen φ, Liên hợp z̄",
                Mode.PowerRoots => "⚡ Lũy thừa zⁿ & Căn bậc n của z",
                _ => ""
            };

            BuildPresets();
            Calc();
        }

        // ═══ PRESETS ═══
        private void BuildPresets()
        {
            presetPanel.Children.Clear();
            var accent = DS.CatMath; // Màu xanh dương chủ đạo

            if (_mode == Mode.Arithmetic)
            {
                var presets = new (string Label, double A1, double B1, double A2, double B2)[]
                {
                    ("(3+4i) + (1−2i)", 3, 4, 1, -2),
                    ("(1+i) × (1−i)",   1, 1, 1, -1),
                    ("(2+3i) × (2−3i)", 2, 3, 2, -3),
                    ("(1+i)²",          1, 1, 1, 1),
                    ("(3+4i) / (1+2i)", 3, 4, 1, 2),
                };
                foreach (var (label, a1, b1, a2, b2) in presets)
                {
                    var ca1 = a1; var cb1 = b1; var ca2 = a2; var cb2 = b2;
                    UI.PresetButton(label, accent, () =>
                    {
                        txtA1.Text = UI.Fmt(ca1); txtB1.Text = UI.Fmt(cb1);
                        txtA2.Text = UI.Fmt(ca2); txtB2.Text = UI.Fmt(cb2);
                    }, presetPanel);
                }
            }
            else if (_mode == Mode.Properties)
            {
                var presets = new (string Label, double A, double B)[]
                {
                    ("z = 3 + 4i (|z| = 5)", 3, 4),
                    ("z = 1 + i (φ = 45°)", 1, 1),
                    ("z = −1 + i√3 (φ = 120°)", -1, 1.7320508),
                    ("z = i (Thuần ảo)", 0, 1),
                    ("z = 2 (Thuần thực)", 2, 0),
                    ("z = 1 − i (φ = −45°)", 1, -1),
                };
                foreach (var (label, a, b) in presets)
                {
                    var ca = a; var cb = b;
                    UI.PresetButton(label, accent, () =>
                    {
                        txtSA.Text = UI.Fmt(ca); txtSB.Text = UI.Fmt(cb);
                    }, presetPanel);
                }
            }
            else // Mode.PowerRoots
            {
                var presets = new (string Label, double A, double B, int N)[]
                {
                    ("z = 1 + i, n = 4", 1, 1, 4),
                    ("z = 3 + 4i, n = 2", 3, 4, 2),
                    ("z = 1 − i, n = 3", 1, -1, 3),
                    ("z = i, n = 2", 0, 1, 2),
                    ("z = −2, n = 3", -2, 0, 3),
                };
                foreach (var (label, a, b, n) in presets)
                {
                    var ca = a; var cb = b; var cn = n;
                    UI.PresetButton(label, accent, () =>
                    {
                        txtSA.Text = UI.Fmt(ca); txtSB.Text = UI.Fmt(cb);
                        if (txtN != null) txtN.Text = cn.ToString();
                    }, presetPanel);
                }
            }
        }

        private void Input_Changed(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) Calc();
        }

        // ═══ CALCULATE ═══
        private void Calc()
        {
            resultPanel?.Children.Clear();
            solutionPanel?.Children.Clear();
            switch (_mode)
            {
                case Mode.Arithmetic: CalcArith(); break;
                case Mode.Properties: CalcProps(); break;
                case Mode.PowerRoots: CalcPowerRoots(); break;
            }
        }

        private void AddSolutionStep(string desc, string formula = "")
        {
            if (solutionPanel == null) return;
            if (!string.IsNullOrEmpty(desc))
            {
                var textBlock = UI.RenderMixedContent(desc, 17, new SolidColorBrush(Color.FromRgb(33, 33, 33)));
                solutionPanel.Children.Add(textBlock);
            }
            if (!string.IsNullOrEmpty(formula))
            {
                var latexText = $"${formula}$";
                var control = UI.RenderMixedContent(latexText, 22, DS.Brush(DS.CatMath));
                var border = new Border
                {
                    Margin = new Thickness(16, 6, 16, 12),
                    Padding = new Thickness(16, 10, 16, 10),
                    Background = new SolidColorBrush(Color.FromArgb(10, DS.CatMath.R, DS.CatMath.G, DS.CatMath.B)),
                    CornerRadius = new CornerRadius(6)
                };
                border.Child = control;
                solutionPanel.Children.Add(border);
            }
        }

        private void CalcArith()
        {
            if (!ParsingHelper.TryParseDouble(txtA1?.Text, out double a1) ||
                !ParsingHelper.TryParseDouble(txtB1?.Text, out double b1) ||
                !ParsingHelper.TryParseDouble(txtA2?.Text, out double a2) ||
                !ParsingHelper.TryParseDouble(txtB2?.Text, out double b2))
            {
                UI.ResultRow("⚠️ Vui lòng nhập số thực hợp lệ cho tất cả hệ số", DS.ResultWarning, resultPanel, fontSize: 18);
                return;
            }

            UI.ResultRow($"z₁ = {FmtComplex(a1, b1)}", DS.ResultPrimary, resultPanel, fontSize: 18);
            UI.ResultRow($"z₂ = {FmtComplex(a2, b2)}", DS.ResultPrimary, resultPanel, fontSize: 18);

            // Addition
            UI.ResultRow($"z₁ + z₂ = {FmtComplex(a1 + a2, b1 + b2)}", DS.ResultPrimary, resultPanel, fontSize: 18);

            // Subtraction
            UI.ResultRow($"z₁ − z₂ = {FmtComplex(a1 - a2, b1 - b2)}", DS.ResultPrimary, resultPanel, fontSize: 18);

            // Multiplication: (a1+b1i)(a2+b2i) = (a1a2 - b1b2) + (a1b2 + a2b1)i
            double mr = a1 * a2 - b1 * b2;
            double mi = a1 * b2 + a2 * b1;
            UI.ResultRow($"z₁ × z₂ = {FmtComplex(mr, mi)}", DS.ResultPrimary, resultPanel, fontSize: 18);

            // Division: z1/z2 = z1·z̄2 / |z2|²
            double d2 = a2 * a2 + b2 * b2;
            if (d2 > 1e-12)
            {
                double dr = (a1 * a2 + b1 * b2) / d2;
                double di = (b1 * a2 - a1 * b2) / d2;
                UI.ResultRow($"z₁ / z₂ = {FmtComplex(dr, di)}", DS.ResultPrimary, resultPanel, fontSize: 18);
            }
            else
            {
                UI.ResultRow($"z₁ / z₂ = Không xác định (z₂ = 0)", DS.ResultError, resultPanel, fontSize: 18);
            }

            // Modules
            double m1 = System.Math.Sqrt(a1 * a1 + b1 * b1);
            double m2 = System.Math.Sqrt(a2 * a2 + b2 * b2);
            UI.ResultRow($"|z₁| = {UI.Fmt(m1)}", DS.ResultPrimary, resultPanel, fontSize: 18);
            UI.ResultRow($"|z₂| = {UI.Fmt(m2)}", DS.ResultPrimary, resultPanel, fontSize: 18);

            // --- Detailed solution steps ---
            AddSolutionStep("• Hai số phức đã cho:");
            AddSolutionStep("", $"z_1 = {FmtComplex(a1, b1)} \\quad \\text{{và}} \\quad z_2 = {FmtComplex(a2, b2)}");

            // Phép cộng
            AddSolutionStep("• Phép cộng: Cộng phần thực với thực, ảo với ảo:");
            AddSolutionStep("", $"z_1 + z_2 = ({FmtSign(a1)} + {FmtSign(a2)}) + ({FmtSign(b1)} + {FmtSign(b2)})i = {FmtComplex(a1 + a2, b1 + b2)}");

            // Phép trừ
            AddSolutionStep("• Phép trừ: Trừ phần thực với thực, ảo với ảo:");
            AddSolutionStep("", $"z_1 - z_2 = ({FmtSign(a1)} - ({FmtSign(a2)})) + ({FmtSign(b1)} - ({FmtSign(b2)}))i = {FmtComplex(a1 - a2, b1 - b2)}");

            // Phép nhân
            AddSolutionStep("• Phép nhân: Nhân phân phối và áp dụng hệ thức $i^2 = -1$:");
            AddSolutionStep("", $"z_1 \\times z_2 = ({FmtSign(a1)})({FmtSign(a2)}) - ({FmtSign(b1)})({FmtSign(b2)}) + [({FmtSign(a1)})({FmtSign(b2)}) + ({FmtSign(a2)})({FmtSign(b1)})]i");
            AddSolutionStep("", $"= ({FmtSign(a1 * a2)}) - ({FmtSign(b1 * b2)}) + ({FmtSign(a1 * b2)} + {FmtSign(a2 * b1)})i = {FmtComplex(mr, mi)}");

            // Phép chia
            if (d2 > 1e-12)
            {
                double dr = (a1 * a2 + b1 * b2) / d2;
                double di = (b1 * a2 - a1 * b2) / d2;
                AddSolutionStep("• Phép chia: Nhân cả tử và mẫu với số phức liên hợp của mẫu $z_2$:");
                AddSolutionStep("", $"\\bar{{z}}_2 = {FmtComplex(a2, -b2)} \\quad \\text{{và}} \\quad |z_2|^2 = {UI.Fmt(d2)}");
                AddSolutionStep("", $"\\frac{{z_1}}{{z_2}} = \\frac{{z_1 \\cdot \\bar{{z}}_2}}{{|z_2|^2}} = \\frac{{({FmtComplex(a1, b1)})({FmtComplex(a2, -b2)})}}{{{UI.Fmt(d2)}}}");
                AddSolutionStep("", $"= \\frac{{({FmtSign(a1 * a2 + b1 * b2)}) + ({FmtSign(b1 * a2 - a1 * b2)})i}}{{{UI.Fmt(d2)}}} = {FmtComplex(dr, di)}");
            }
            else
            {
                AddSolutionStep("• Phép chia: Không xác định vì mẫu số $z_2 = 0$.");
            }
        }

        private void CalcProps()
        {
            if (!ParsingHelper.TryParseDouble(txtSA?.Text, out double a) ||
                !ParsingHelper.TryParseDouble(txtSB?.Text, out double b))
            {
                UI.ResultRow("⚠️ Vui lòng nhập số thực hợp lệ cho phần thực và phần ảo", DS.ResultWarning, resultPanel, fontSize: 18);
                return;
            }

            UI.ResultRow($"z = {FmtComplex(a, b)}", DS.ResultPrimary, resultPanel, fontSize: 18);

            // Conjugate
            UI.ResultRow($"z̄ (liên hợp) = {FmtComplex(a, -b)}", DS.ResultPrimary, resultPanel, fontSize: 18);

            // Module
            double mod = System.Math.Sqrt(a * a + b * b);
            UI.ResultRow($"|z| = {UI.Fmt(mod)}", DS.ResultPrimary, resultPanel, fontSize: 18);
            UI.ResultRow($"|z|² = {UI.Fmt(a * a + b * b)}", DS.ResultPrimary, resultPanel, fontSize: 18);

            // Argument & Trig form
            if (a == 0 && b == 0)
            {
                UI.ResultRow("arg(z) = Không xác định (do z = 0)", DS.ResultWarning, resultPanel, fontSize: 18);
            }
            else
            {
                double arg = System.Math.Atan2(b, a);
                double argDeg = arg * 180.0 / System.Math.PI;
                string radStr = FmtRadian(arg);
                UI.ResultRow($"arg(z) = {radStr} rad = {argDeg:F2}°", DS.ResultPrimary, resultPanel, fontSize: 18);

                // Trig form
                if (mod > 1e-12)
                {
                    UI.ResultRow($"Dạng lượng giác (rad) = {UI.Fmt(mod)}·(cos({radStr}) + i·sin({radStr}))", DS.ResultPrimary, resultPanel, fontSize: 18);
                    UI.ResultRow($"Dạng lượng giác (độ) = {UI.Fmt(mod)}·(cos({argDeg:F2}°) + i·sin({argDeg:F2}°))", DS.ResultPrimary, resultPanel, fontSize: 18);
                }
            }

            // Powers of i
            UI.ResultRow($"📌 Lũy thừa i: i¹ = i,  i² = -1,  i³ = -i,  i⁴ = 1  (chu kỳ 4)", DS.ResultWarning, resultPanel, fontSize: 18);

            // --- Detailed solution steps ---
            AddSolutionStep("• Số phức đã cho:");
            AddSolutionStep("", $"z = {FmtComplex(a, b)}");

            // Số liên hợp
            AddSolutionStep("• Số phức liên hợp $\\bar{z}$ (đổi dấu phần ảo):");
            AddSolutionStep("", $"\\bar{{z}} = {FmtComplex(a, -b)}");

            // Mô-đun
            AddSolutionStep("• Mô-đun của số phức $|z|$ (độ dài vectơ biểu diễn):");
            AddSolutionStep("", $"|z| = \\sqrt{{a^2 + b^2}} = \\sqrt{{{FmtSign(a)}^2 + ({FmtSign(b)})^2}} = \\sqrt{{{UI.Fmt(a * a + b * b)}}} \\approx {UI.Fmt(mod)}");

            // Acgumen
            if (a == 0 && b == 0)
            {
                AddSolutionStep("• Góc Acgumen $\\varphi$: Không xác định do $z = 0$.");
            }
            else
            {
                double arg = System.Math.Atan2(b, a);
                double argDeg = arg * 180.0 / System.Math.PI;
                string radStr = FmtRadian(arg);
                AddSolutionStep("• Góc Acgumen $\\varphi = \\arg(z)$ thỏa mãn:");
                AddSolutionStep("", $"\\cos\\varphi = \\frac{{a}}{{|z|}} = \\frac{{{FmtSign(a)}}}{{{UI.Fmt(mod)}}} \\quad \\text{{và}} \\quad \\sin\\varphi = \\frac{{b}}{{|z|}} = \\frac{{{FmtSign(b)}}}{{{UI.Fmt(mod)}}}");
                AddSolutionStep("", $"\\Rightarrow \\varphi \\approx {argDeg:F2}^\\circ \\quad \\left({radStr} \\text{{ rad}}\\right)");

                // Dạng lượng giác
                if (mod > 1e-12)
                {
                    AddSolutionStep("• Biểu diễn dạng lượng giác của số phức:");
                    AddSolutionStep("", $"z = r(\\cos\\varphi + i\\sin\\varphi) = {UI.Fmt(mod)}\\left(\\cos({radStr}) + i\\sin({radStr})\\right)");
                    AddSolutionStep("• Biểu diễn dạng số mũ Euler:");
                    AddSolutionStep("", $"z = r \\cdot e^{{i\\varphi}} = {UI.Fmt(mod)} \\cdot e^{{i \\cdot {radStr}}}");
                }
            }
        }

        private void CalcPowerRoots()
        {
            if (!ParsingHelper.TryParseDouble(txtSA?.Text, out double a) ||
                !ParsingHelper.TryParseDouble(txtSB?.Text, out double b))
            {
                UI.ResultRow("⚠️ Vui lòng nhập số thực hợp lệ cho phần thực và phần ảo", DS.ResultWarning, resultPanel, fontSize: 18);
                return;
            }

            if (!int.TryParse(txtN?.Text, out int n) || n < 1 || n > 20)
            {
                n = 3;
                if (txtN != null) txtN.Text = "3";
            }

            UI.ResultRow($"z = {FmtComplex(a, b)}", DS.ResultPrimary, resultPanel, fontSize: 18);

            double r = System.Math.Sqrt(a * a + b * b);
            double phi = System.Math.Atan2(b, a);

            // 1. Power z^n
            double rn = System.Math.Pow(r, n);
            double nphi = n * phi;
            double rx = rn * System.Math.Cos(nphi);
            double ry = rn * System.Math.Sin(nphi);
            UI.ResultRow($"z^{{{n}}} = {FmtComplex(rx, ry)}", DS.ResultPrimary, resultPanel, fontSize: 18);

            // 2. Square roots
            if (a == 0 && b == 0)
            {
                UI.ResultRow("Căn bậc hai: w₁ = w₂ = 0", DS.ResultPrimary, resultPanel, fontSize: 18);
            }
            else
            {
                double x = System.Math.Sqrt((r + a) / 2.0);
                double y = System.Math.Sqrt((r - a) / 2.0);
                if (b < 0) y = -y;

                UI.ResultRow($"Căn bậc hai w₁ = {FmtComplex(x, y)}", DS.ResultPrimary, resultPanel, fontSize: 18);
                UI.ResultRow($"Căn bậc hai w₂ = {FmtComplex(-x, -y)}", DS.ResultPrimary, resultPanel, fontSize: 18);
            }

            // 3. Cube roots
            if (a == 0 && b == 0)
            {
                UI.ResultRow("Căn bậc ba: w₀ = w₁ = w₂ = 0", DS.ResultPrimary, resultPanel, fontSize: 18);
            }
            else
            {
                double r3 = System.Math.Pow(r, 1.0 / 3.0);
                for (int k = 0; k < 3; k++)
                {
                    double angle = (phi + 2 * k * System.Math.PI) / 3.0;
                    double cr = r3 * System.Math.Cos(angle);
                    double ci = r3 * System.Math.Sin(angle);
                    UI.ResultRow($"Căn bậc ba w_{{{k}}} = {FmtComplex(cr, ci)}", DS.ResultPrimary, resultPanel, fontSize: 18);
                }
            }

            // --- Detailed solution steps ---
            AddSolutionStep("• Số phức đã cho:");
            AddSolutionStep("", $"z = {FmtComplex(a, b)}");
            AddSolutionStep("", $"|z| = r = {UI.Fmt(r)} \\quad \\text{{và}} \\quad \\varphi = \\arg(z) \\approx {phi * 180.0 / System.Math.PI:F2}^\\circ \\, ({FmtRadian(phi)} \\text{{ rad}})");

            // Power z^n Step
            AddSolutionStep($"• Tính lũy thừa $z^{{{n}}}$ (Công thức Moivre):");
            AddSolutionStep("", $"z^{{{n}}} = r^{{{n}}} \\cdot \\left(\\cos({n}\\varphi) + i\\sin({n}\\varphi)\\right)");
            AddSolutionStep("", $"= {UI.Fmt(r)}^{{{n}}} \\cdot \\left(\\cos({(nphi * 180.0 / System.Math.PI):F2}^\\circ) + i\\sin({(nphi * 180.0 / System.Math.PI):F2}^\\circ)\\right)");
            AddSolutionStep("", $"\\approx {UI.Fmt(rn)} \\cdot \\left({UI.Fmt(System.Math.Cos(nphi))} + {UI.Fmt(System.Math.Sin(nphi))}i\\right) = {FmtComplex(rx, ry)}");

            AddSolutionStep("• Tìm căn bậc hai của số phức (giải hệ phương trình đại số):");
            AddSolutionStep("Gọi $w = x + yi \\ (x, y \\in \\mathbb{R})$ là căn bậc hai của $z$. Ta giải hệ phương trình:");
            AddSolutionStep("", $"\\begin{{cases}} x^2 - y^2 = a = {FmtSign(a)} \\\\ 2xy = b = {FmtSign(b)} \\end{{cases}}");

            if (a == 0 && b == 0)
            {
                AddSolutionStep("Hệ phương trình có nghiệm duy nhất $x = 0, y = 0$. Căn bậc hai là $w = 0$.");
            }
            else
            {
                double x_val = System.Math.Sqrt((r + a) / 2.0);
                double y_val = System.Math.Sqrt((r - a) / 2.0);
                if (b < 0) y_val = -y_val;

                AddSolutionStep("", $"x^2 = \\frac{{r + a}}{{2}} = \\frac{{{UI.Fmt(r)} + ({FmtSign(a)})}}{{2}} = {UI.Fmt((r + a) / 2.0)} \\Rightarrow x = \\pm {UI.Fmt(x_val)}");
                if (System.Math.Abs(x_val) > 1e-12)
                {
                    AddSolutionStep("", $"y = \\frac{{b}}{{2x}} = \\frac{{{FmtSign(b)}}}{{2 \\cdot ({UI.Fmt(x_val)})}} = \\pm {UI.Fmt(y_val)}");
                }
                AddSolutionStep("Do $2xy = b$, nên $x$ và $y$ cùng dấu nếu $b > 0$, trái dấu nếu $b < 0$. Ta có 2 căn bậc hai:");
                AddSolutionStep("", $"w_1 = {FmtComplex(x_val, y_val)} \\quad \\text{{và}} \\quad w_2 = {FmtComplex(-x_val, -y_val)}");
            }

            // Cube Root step
            AddSolutionStep("• Tìm các căn bậc ba (sử dụng công thức lượng giác Moivre):");
            AddSolutionStep("Công thức tìm căn bậc ba $w_k$ ($k = 0, 1, 2$):");
            AddSolutionStep("", $"w_k = \\sqrt[3]{{r}} \\cdot \\left(\\cos \\frac{{\\varphi + 2k\\pi}}{{3}} + i\\sin \\frac{{\\varphi + 2k\\pi}}{{3}}\\right)");
            double r_c = System.Math.Pow(r, 1.0 / 3.0);
            AddSolutionStep("", $"\\sqrt[3]{{r}} = \\sqrt[3]{{{UI.Fmt(r)}}} \\approx {UI.Fmt(r_c)}");

            for (int k = 0; k < 3; k++)
            {
                double angle = (phi + 2 * k * System.Math.PI) / 3.0;
                double angleDeg = angle * 180.0 / System.Math.PI;
                double cr = r_c * System.Math.Cos(angle);
                double ci = r_c * System.Math.Sin(angle);
                AddSolutionStep($"Với $k = {k}$:");
                AddSolutionStep("", $"w_{{{k}}} = {UI.Fmt(r_c)} \\cdot \\left(\\cos({angleDeg:F2}^\\circ) + i\\sin({angleDeg:F2}^\\circ)\\right) \\approx {FmtComplex(cr, ci)}");
            }
        }

        // ═══ FORMULAS ═══
        private void BuildFormulas()
        {
            formulaPanel.Children.Clear();
            var accent = DS.CatMath; // Xanh dương chủ đạo

            var sections = new (string Title, string[] Items)[]
            {
                ("Định nghĩa số phức", new[]
                {
                    "z = a + bi  (a, b ∈ ℝ; i² = -1)",
                    "a: phần thực, b: phần ảo, i: đơn vị ảo",
                    "z̄ = a − bi  (số phức liên hợp)",
                }),
                ("Mô-đun & Acgumen", new[]
                {
                    "|z| = √(a² + b²)  (độ dài vectơ biểu diễn)",
                    "arg(z) = φ: cosφ = a/|z|, sinφ = b/|z|  (z ≠ 0)",
                    "|z|² = z · z̄ = a² + b²",
                }),
                ("Phép toán cơ bản", new[]
                {
                    "z₁ + z₂ = (a₁+a₂) + (b₁+b₂)i",
                    "z₁ · z₂ = (a₁a₂ − b₁b₂) + (a₁b₂ + a₂b₁)i",
                    "z₁/z₂ = (z₁·z̄₂) / |z₂|²  (với z₂ ≠ 0)",
                }),
                ("Dạng lượng giác", new[]
                {
                    "z = r(cosφ + i·sinφ)  (r = |z|, φ = arg z)",
                    "z₁·z₂ = r₁r₂ [cos(φ₁+φ₂) + i·sin(φ₁+φ₂)]",
                    "zⁿ = rⁿ [cos(nφ) + i·sin(nφ)]  (Công thức Moivre)",
                }),
                ("Lũy thừa của đơn vị ảo i", new[]
                {
                    "i⁰ = 1,  i¹ = i,  i² = -1,  i³ = -i",
                    "iⁿ: chu kỳ 4 → phụ thuộc vào số dư khi n chia cho 4",
                }),
            };

            foreach (var (title, items) in sections)
            {
                var section = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(15, accent.R, accent.G, accent.B)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 8),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(40, accent.R, accent.G, accent.B)),
                    BorderThickness = new Thickness(1)
                };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = title, FontSize = 17, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(accent),
                    Margin = new Thickness(0, 0, 0, 4)
                });
                foreach (var item in items)
                    sp.Children.Add(new TextBlock
                    {
                        Text = $"  {item}", FontSize = 15,
                        FontFamily = new FontFamily("Segoe UI"),
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                        Margin = new Thickness(0, 1, 0, 1)
                    });
                section.Child = sp;
                formulaPanel.Children.Add(section);
            }
        }

        // ═══ Graph ═══
        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var ci = CultureInfo.InvariantCulture;
                string expJs;

                if (_mode == Mode.Arithmetic)
                {
                    ParsingHelper.TryParseDouble(txtA1?.Text, out double a1);
                    ParsingHelper.TryParseDouble(txtB1?.Text, out double b1);
                    ParsingHelper.TryParseDouble(txtA2?.Text, out double a2);
                    ParsingHelper.TryParseDouble(txtB2?.Text, out double b2);

                    double sr = a1 + a2, si = b1 + b2;  // sum
                    double mr = a1 * a2 - b1 * b2, mi2 = a1 * b2 + a2 * b1;  // product

                    expJs = $@"
        calc.setExpression({{id:'z1', latex:'({a1.ToString(ci)},{b1.ToString(ci)})', color:'#1565C0', pointSize:14, label:'z₁ = {FmtComplex(a1, b1)}', showLabel:true}});
        calc.setExpression({{id:'z2', latex:'({a2.ToString(ci)},{b2.ToString(ci)})', color:'#C62828', pointSize:14, label:'z₂ = {FmtComplex(a2, b2)}', showLabel:true}});
        calc.setExpression({{id:'sum', latex:'({sr.ToString(ci)},{si.ToString(ci)})', color:'#2E7D32', pointSize:12, label:'z₁+z₂', showLabel:true}});
        calc.setExpression({{id:'prod', latex:'({mr.ToString(ci)},{mi2.ToString(ci)})', color:'#E65100', pointSize:12, label:'z₁×z₂', showLabel:true}});
        calc.setExpression({{id:'v_z1', latex:'(t*{a1.ToString(ci)}, t*{b1.ToString(ci)})', color:'#1565C0', lineWidth:1.5, lineStyle:'DASHED', parametricDomain:{{min:'0',max:'1'}}}});
        calc.setExpression({{id:'v_z2', latex:'(t*{a2.ToString(ci)}, t*{b2.ToString(ci)})', color:'#C62828', lineWidth:1.5, lineStyle:'DASHED', parametricDomain:{{min:'0',max:'1'}}}});
        calc.setExpression({{id:'v_sum', latex:'(t*{sr.ToString(ci)}, t*{si.ToString(ci)})', color:'#2E7D32', lineWidth:1.5, lineStyle:'DASHED', parametricDomain:{{min:'0',max:'1'}}}});
        calc.setExpression({{id:'p_z1', latex:'((1-t)*{a1.ToString(ci)} + t*{sr.ToString(ci)}, (1-t)*{b1.ToString(ci)} + t*{si.ToString(ci)})', color:'#9E9E9E', lineWidth:1.2, lineStyle:'DASHED', parametricDomain:{{min:'0',max:'1'}}}});
        calc.setExpression({{id:'p_z2', latex:'((1-t)*{a2.ToString(ci)} + t*{sr.ToString(ci)}, (1-t)*{b2.ToString(ci)} + t*{si.ToString(ci)})', color:'#9E9E9E', lineWidth:1.2, lineStyle:'DASHED', parametricDomain:{{min:'0',max:'1'}}}});
        calc.setExpression({{id:'xaxis', latex:'y=0', color:'#9E9E9E', lineWidth:1}});
        calc.setExpression({{id:'yaxis', latex:'x=0', color:'#9E9E9E', lineWidth:1}});
        calc.setMathBounds({{ left: -8, right: 8, bottom: -8, top: 8 }});";
                }
                else if (_mode == Mode.Properties)
                {
                    ParsingHelper.TryParseDouble(txtSA?.Text, out double a);
                    ParsingHelper.TryParseDouble(txtSB?.Text, out double b);
                    double mod = System.Math.Sqrt(a * a + b * b);

                    expJs = $@"
        calc.setExpression({{id:'z', latex:'({a.ToString(ci)},{b.ToString(ci)})', color:'#1565C0', pointSize:14, label:'z = {FmtComplex(a, b)}', showLabel:true}});
        calc.setExpression({{id:'zbar', latex:'({a.ToString(ci)},{(-b).ToString(ci)})', color:'#C62828', pointSize:12, label:'z̄', showLabel:true}});
        calc.setExpression({{id:'circle', latex:'x^2+y^2={mod.ToString(ci)}^2', color:'#7B1FA2', lineWidth:1, lineStyle:'DASHED'}});
        calc.setExpression({{id:'line', latex:'(t*{a.ToString(ci)}, t*{b.ToString(ci)})', color:'#E65100', lineWidth:2, lineStyle:'DASHED', parametricDomain:{{min:'0',max:'1'}}}});
        calc.setExpression({{id:'xaxis', latex:'y=0', color:'#9E9E9E', lineWidth:1}});
        calc.setExpression({{id:'yaxis', latex:'x=0', color:'#9E9E9E', lineWidth:1}});
        calc.setMathBounds({{ left: -8, right: 8, bottom: -8, top: 8 }});";
                }
                else // Mode.PowerRoots
                {
                    ParsingHelper.TryParseDouble(txtSA?.Text, out double a);
                    ParsingHelper.TryParseDouble(txtSB?.Text, out double b);
                    if (!int.TryParse(txtN?.Text, out int n) || n < 1 || n > 20) n = 3;

                    double mod = System.Math.Sqrt(a * a + b * b);
                    double phi = System.Math.Atan2(b, a);

                    double rn = System.Math.Pow(mod, n);
                    double nphi = n * phi;
                    double rx = rn * System.Math.Cos(nphi);
                    double ry = rn * System.Math.Sin(nphi);

                    double r2 = System.Math.Sqrt(mod);
                    double x1 = r2 * System.Math.Cos(phi / 2.0);
                    double y1 = r2 * System.Math.Sin(phi / 2.0);

                    expJs = $@"
        calc.setExpression({{id:'z', latex:'({a.ToString(ci)},{b.ToString(ci)})', color:'#1565C0', pointSize:14, label:'z', showLabel:true}});
        calc.setExpression({{id:'zn', latex:'({rx.ToString(ci)},{ry.ToString(ci)})', color:'#E65100', pointSize:12, label:'z^{{{n}}}', showLabel:true}});
        calc.setExpression({{id:'w1', latex:'({x1.ToString(ci)},{y1.ToString(ci)})', color:'#2E7D32', pointSize:12, label:'w₁', showLabel:true}});
        calc.setExpression({{id:'w2', latex:'({(-x1).ToString(ci)},{(-y1).ToString(ci)})', color:'#2E7D32', pointSize:12, label:'w₂', showLabel:true}});
        calc.setExpression({{id:'circle_z', latex:'x^2+y^2={mod.ToString(ci)}^2', color:'#1565C0', lineWidth:1, lineStyle:'DASHED'}});
        calc.setExpression({{id:'circle_roots', latex:'x^2+y^2={r2.ToString(ci)}^2', color:'#2E7D32', lineWidth:1, lineStyle:'DASHED'}});
        calc.setExpression({{id:'xaxis', latex:'y=0', color:'#9E9E9E', lineWidth:1}});
        calc.setExpression({{id:'yaxis', latex:'x=0', color:'#9E9E9E', lineWidth:1}});
        var bounds = Math.max({mod.ToString(ci)}, 5);
        calc.setMathBounds({{ left: -bounds - 2, right: bounds + 2, bottom: -bounds - 2, top: bounds + 2 }});";
                }

                var win = new GraphWindow(expJs, "ℂ Mặt phẳng phức");
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở đồ thị: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rootGrid == null || sideMenu == null || viewGuide == null || viewPractice == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewPractice.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            int index = sideMenu.SelectedIndex;
            if (index == 0)
            {
                viewGuide.Visibility = Visibility.Visible;
            }
            else if (index == 1)
            {
                viewPractice.Visibility = Visibility.Visible;
            }
            else if (index == 2)
            {
                viewPractical.Visibility = Visibility.Visible;
            }
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
                        Icon = "⚡",
                        Title = isVN ? "Mạch Điện Xoay Chiều RLC" : "AC Circuit Impedance Analysis",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_complex_1_{suffix}.png",
                        Description = isVN 
                            ? "Trong kỹ thuật điện, số phức biểu diễn trở kháng tổng hợp Z = R + jX của mạch xoay chiều nối tiếp chứa Điện trở, Cuộn cảm và Tụ điện." 
                            : "In electrical engineering, complex numbers represent the total impedance Z = R + jX of AC circuits containing resistors, inductors, and capacitors."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎵",
                        Title = isVN ? "Xử Lý Tín Hiệu & Fourier" : "Signal Processing & Fourier Transform",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_complex_2_{suffix}.png",
                        Description = isVN 
                            ? "Biến đổi tín hiệu âm thanh và hình ảnh từ miền thời gian sang miền tần số thông qua số phức, làm nền tảng cho nén MP3 và lọc nhiễu." 
                            : "Use Fourier Transform with complex exponentials to analyze and convert audio, image, and telecom signals from the time domain to the frequency domain."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⚛",
                        Title = isVN ? "Cơ Học Lượng Tử & Hàm Sóng" : "Quantum Mechanics & Wave Functions",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_complex_3_{suffix}.png",
                        Description = isVN 
                            ? "Trạng thái vi mô của hạt được mô tả bằng hàm sóng phức Psi(x,t). Phương trình Schrödinger dùng đơn vị ảo i để mô tả tiến trình hạt." 
                            : "Utilize wave functions defined over complex numbers to solve the Schrödinger equation and describe quantum states of microscopic particles."
                    },
                    new PracticalAppItem
                    {
                        Icon = "✈",
                        Title = isVN ? "Khí Động Học Cánh Máy Bay" : "Aerodynamics & Wing Lift Simulation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_complex_4_{suffix}.png",
                        Description = isVN 
                            ? "Phép biến đổi conformal Joukowsky chuyển vòng tròn thành mặt cắt cánh máy bay, cho phép tính toán và mô phỏng lực nâng khí động học." 
                            : "Apply conformal Joukowsky mapping using complex functions to model airflow around airplane wings and predict aerodynamic lift forces."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌀",
                        Title = isVN ? "Hình Học Fractal & Mandelbrot" : "Fractal Geometry & Mandelbrot Set",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_complex_5_{suffix}.png",
                        Description = isVN 
                            ? "Phép lặp số phức z[n+1] = z[n]² + c tạo ra tập Mandelbrot fractal vô cùng kỳ vĩ, ứng dụng trong đồ họa máy tính và mô phỏng tự nhiên." 
                            : "Generate stunning and infinitely complex geometric structures like the Mandelbrot set by iterating mathematical functions in the complex plane."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🤖",
                        Title = isVN ? "Hệ Thống Điều Khiển Tự Động" : "Automatic Control Systems",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_complex_6_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng biến số phức s = sigma + jw để phân tích điểm cực và điểm không trên mặt phẳng s để đánh giá độ ổn định của hệ thống phản hồi." 
                            : "Analyze poles and zeros in the complex s-plane to evaluate the stability and response of feedback control loops, auto-pilots, and robotics."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎨",
                        Title = isVN ? "Đồ họa Fractal" : "Fractal Art Graphics",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_complex_7_{suffix}.png",
                        Description = isVN 
                            ? "Biến đổi các số phức thông qua phép lặp toán học để vẽ nên những hình ảnh Fractal nghệ thuật vô cùng phức tạp." 
                            : "Transform complex coordinates through recursive mathematical formulas to render beautiful, highly detailed, and artistic fractal patterns."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔌",
                        Title = isVN ? "Phân tích dòng xoay chiều" : "AC Circuit Impedance Analysis",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_complex_8_{suffix}.png",
                        Description = isVN 
                            ? "Biểu diễn điện áp và dòng điện dưới dạng số phức (phasor) để tính toán trở kháng và độ lệch pha trong mạng lưới điện." 
                            : "Represent voltage and current as complex numbers (phasors) to calculate impedance and phase shifts in power grids."
                    }};

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for ComplexNumberTool: {Err}", ex.Message);
            }
        }

        // ═══ HELPERS ═══
        /* AddResult / AddR replaced by UI.ResultRow */

        private static string FmtRadian(double rad)
        {
            double deg = rad * 180.0 / System.Math.PI;
            double roundedDeg = System.Math.Round(deg, 2);
            double absDeg = System.Math.Abs(roundedDeg);
            string sign = roundedDeg < 0 ? "−" : ""; // Dấu trừ chuẩn toán học

            if (absDeg < 1e-2) return "0";
            if (System.Math.Abs(absDeg - 15) < 1e-2) return sign + "π/12";
            if (System.Math.Abs(absDeg - 30) < 1e-2) return sign + "π/6";
            if (System.Math.Abs(absDeg - 45) < 1e-2) return sign + "π/4";
            if (System.Math.Abs(absDeg - 60) < 1e-2) return sign + "π/3";
            if (System.Math.Abs(absDeg - 75) < 1e-2) return sign + "5π/12";
            if (System.Math.Abs(absDeg - 90) < 1e-2) return sign + "π/2";
            if (System.Math.Abs(absDeg - 105) < 1e-2) return sign + "7π/12";
            if (System.Math.Abs(absDeg - 120) < 1e-2) return sign + "2π/3";
            if (System.Math.Abs(absDeg - 135) < 1e-2) return sign + "3π/4";
            if (System.Math.Abs(absDeg - 150) < 1e-2) return sign + "5π/6";
            if (System.Math.Abs(absDeg - 165) < 1e-2) return sign + "11π/12";
            if (System.Math.Abs(absDeg - 180) < 1e-2) return sign + "π";
            if (System.Math.Abs(absDeg - 360) < 1e-2) return sign + "2π";

            return rad < 0 ? $"{rad:G6}".Replace("-", "−") : $"{rad:G6}";
        }

        private static string Fmt(double v) => v == (int)v ? $"{(int)v}" : $"{v:G6}";

        private static string FmtComplex(double a, double b)
        {
            if (System.Math.Abs(b) < 1e-12) return FmtSign(a);
            if (System.Math.Abs(a) < 1e-12)
            {
                if (System.Math.Abs(b - 1) < 1e-12) return "i";
                if (System.Math.Abs(b + 1) < 1e-12) return "−i"; // Dấu trừ chuẩn
                string bStr = UI.Fmt(System.Math.Abs(b)) + "i";
                return b < 0 ? "−" + bStr : bStr; // Dấu trừ chuẩn
            }
            string sign = b > 0 ? "+" : "−"; // Dấu trừ chuẩn
            string bStr2;
            double absB = System.Math.Abs(b);
            if (System.Math.Abs(absB - 1) < 1e-12) bStr2 = "";
            else bStr2 = UI.Fmt(absB);
            return $"{FmtSign(a)} {sign} {bStr2}i";
        }

        private static string FmtSign(double v)
        {
            string s = UI.Fmt(v);
            return v < 0 ? s.Replace("-", "−") : s; // Đồng bộ dấu trừ chuẩn toán học
        }
    }
}


