using System;
using QASmartClass.LearningTools.Helpers;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class SolidGeometryTool : BaseToolControl
    {
        private int _shape; // 0-7
        private readonly List<TextBox> _inputs = new();

        private static readonly Color Accent = DS.CatMath;

        // Shape definitions
        private static readonly ShapeDef[] Shapes = new ShapeDef[]
        {
            new("📦 Hộp chữ nhật",    "V = a · b · c, S_tp = 2(ab+bc+ca)",
                new[] { ("Chiều dài (a)", "3"), ("Chiều rộng (b)", "4"), ("Chiều cao (c)", "5") },
                new[] { ("3,4,5", 3.0, 4.0, 5.0), ("1,1,1 (lập phương)", 1.0, 1.0, 1.0), ("2,3,6", 2.0, 3.0, 6.0) }),

            new("🧊 Lập phương",      "V = a³, S_tp = 6a²",
                new[] { ("Cạnh (a)", "4") },
                new[] { ("a=3", 3.0, 0.0, 0.0), ("a=5", 5.0, 0.0, 0.0), ("a=10", 10.0, 0.0, 0.0) }),

            new("🔺 Chóp tam giác đều", "V = (1/3) · S_đáy · h, S_xq = (1/2) · C_đáy · l",
                new[] { ("Cạnh đáy (a)", "6"), ("Chiều cao chóp (h)", "8") },
                new[] { ("a=6, h=8", 6.0, 8.0, 0.0), ("a=4, h=5", 4.0, 5.0, 0.0) }),

            new("🔺 Chóp tứ giác đều", "V = (1/3) · a² · h, S_xq = 2 · a · l",
                new[] { ("Cạnh đáy (a)", "6"), ("Chiều cao chóp (h)", "9") },
                new[] { ("a=6, h=9", 6.0, 9.0, 0.0), ("a=4, h=6", 4.0, 6.0, 0.0), ("a=10, h=12", 10.0, 12.0, 0.0) }),

            new("📐 Lăng trụ tam giác đều", "V = S_đáy · h = (a²√3/4) · h",
                new[] { ("Cạnh đáy (a)", "4"), ("Chiều cao (h)", "10") },
                new[] { ("a=4, h=10", 4.0, 10.0, 0.0), ("a=6, h=8", 6.0, 8.0, 0.0) }),

            new("🟡 Hình trụ",         "V = πr²h, S_xq = 2πrh, S_tp = 2πr(r+h)",
                new[] { ("Bán kính đáy (r)", "3"), ("Chiều cao (h)", "7") },
                new[] { ("r=3, h=7", 3.0, 7.0, 0.0), ("r=5, h=10", 5.0, 10.0, 0.0), ("r=1, h=1", 1.0, 1.0, 0.0) }),

            new("🔻 Hình nón",         "V = (1/3)πr²h, S_xq = πrl",
                new[] { ("Bán kính đáy (r)", "3"), ("Chiều cao (h)", "4") },
                new[] { ("r=3, h=4", 3.0, 4.0, 0.0), ("r=5, h=12", 5.0, 12.0, 0.0), ("r=6, h=8", 6.0, 8.0, 0.0) }),

            new("🔴 Hình cầu",         "V = (4/3)πr³, S = 4πr²",
                new[] { ("Bán kính (r)", "5") },
                new[] { ("r=5", 5.0, 0.0, 0.0), ("r=1", 1.0, 0.0, 0.0), ("r=10", 10.0, 0.0, 0.0), ("Trái Đất ≈ 6371km", 6371.0, 0.0, 0.0) }),
        };

        public SolidGeometryTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextPractice != null) menuTextPractice.Text = isVN ? "Tính toán & Lý thuyết" : "Calculator & Theory";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn sử dụng" : "User Guide";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";

                if (sideMenu != null)
                {
                    sideMenu.SelectionChanged += SideMenu_SelectionChanged;
                    sideMenu.SelectedIndex = 0;
                }

                BuildModeButtons();
                BuildFormulas();
                RebuildInputs();
                LoadPracticalApps();
            };
        }

        private void BuildModeButtons()
        {
            modePanel.Children.Clear();
            for (int i = 0; i < Shapes.Length; i++)
            {
                bool active = i == _shape;
                var btn = MakeChip(Shapes[i].Name, active);
                int ci = i;
                btn.MouseLeftButtonDown += (_, _) => { _shape = ci; BuildModeButtons(); RebuildInputs(); };
                modePanel.Children.Add(btn);
            }
        }

        private void RebuildInputs()
        {
            var def = Shapes[_shape];
            txtModeTitle.Text = def.Name;
            txtFormula.Text = def.FormulaText;

            // Build dynamic input fields
            inputPanel.Children.Clear();
            _inputs.Clear();
            foreach (var (label, defVal) in def.Params)
            {
                var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 16, 8) };
                sp.Children.Add(new TextBlock
                {
                    Text = $"{label} =", FontSize = 16,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                    Width = 160, // Đặt chiều rộng cố định để thẳng hàng
                    TextAlignment = TextAlignment.Right, // Căn lề phải nhãn sát dấu '='
                    Margin = new Thickness(0, 0, 8, 0)
                });

                var controlPanel = new StackPanel { Orientation = Orientation.Horizontal };

                var btnMinus = new Button
                {
                    Content = "-",
                    Width = 30,
                    Height = 32,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(207, 216, 220)),
                    Cursor = Cursors.Hand
                };

                var tb = new TextBox
                {
                    FontSize = 18, Padding = new Thickness(8, 2, 8, 2),
                    FontFamily = new FontFamily("Segoe UI"),
                    Width = 70, Text = defVal,
                    TextAlignment = TextAlignment.Center,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(207, 216, 220)),
                    BorderThickness = new Thickness(0, 1, 0, 1),
                    Background = new SolidColorBrush(Color.FromRgb(245, 248, 250))
                };

                var btnPlus = new Button
                {
                    Content = "+",
                    Width = 30,
                    Height = 32,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(207, 216, 220)),
                    Cursor = Cursors.Hand
                };

                btnMinus.Click += (s, e) =>
                {
                    if (ParsingHelper.TryParseDouble(tb.Text, out double val))
                    {
                        val = System.Math.Max(0.1, val - 0.5);
                        tb.Text = UI.Fmt(val);
                    }
                };

                btnPlus.Click += (s, e) =>
                {
                    if (ParsingHelper.TryParseDouble(tb.Text, out double val))
                    {
                        val += 0.5;
                        tb.Text = UI.Fmt(val);
                    }
                };

                tb.TextChanged += (_, _) => { if (IsLoaded) Calc(); };
                tb.PreviewTextInput += InputBlock_PreviewTextInput;
                tb.PreviewKeyDown += InputBlock_PreviewKeyDown;
                DataObject.AddPastingHandler(tb, OnPaste);
                TouchNumPad.Attach(tb, step: 0.5, min: 0.1);

                controlPanel.Children.Add(btnMinus);
                controlPanel.Children.Add(tb);
                controlPanel.Children.Add(btnPlus);

                sp.Children.Add(controlPanel);
                inputPanel.Children.Add(sp);
                _inputs.Add(tb);
            }

            // Build presets using Design System's UI.PresetButton
            presetPanel.Children.Clear();
            foreach (var (label, v1, v2, v3) in def.Presets)
            {
                double cv1 = v1, cv2 = v2, cv3 = v3;
                UI.PresetButton(label, Accent, () =>
                {
                    if (_inputs.Count > 0) _inputs[0].Text = UI.Fmt(cv1);
                    if (_inputs.Count > 1) _inputs[1].Text = UI.Fmt(cv2);
                    if (_inputs.Count > 2) _inputs[2].Text = UI.Fmt(cv3);
                    DbManager.SaveCalculatorHistory($"Hình học 3D: {Shapes[_shape].Name} (Ví dụ)", $"Bộ tham số: {label}");
                }, presetPanel);
            }

            Calc();
        }

        // ─── CALCULATE ───
        private void Calc()
        {
            resultPanel.Children.Clear();
            var vals = new double[_inputs.Count];
            for (int i = 0; i < _inputs.Count; i++)
            {
                if (!ParsingHelper.TryParseDouble(_inputs[i].Text, out double v) || v <= 0)
                {
                    UI.ResultRow("⚠️ Vui lòng nhập số dương hợp lệ", "#C62828", resultPanel);
                    return;
                }
                vals[i] = v;
            }

            switch (_shape)
            {
                case 0: CalcBox(vals[0], vals[1], vals[2]); break;
                case 1: CalcCube(vals[0]); break;
                case 2: CalcTriPyramid(vals[0], vals[1]); break;
                case 3: CalcQuadPyramid(vals[0], vals[1]); break;
                case 4: CalcTriPrism(vals[0], vals[1]); break;
                case 5: CalcCylinder(vals[0], vals[1]); break;
                case 6: CalcCone(vals[0], vals[1]); break;
                case 7: CalcSphere(vals[0]); break;
            }
        }

        private static string FmtResult(double val)
        {
            if (double.IsNaN(val) || double.IsInfinity(val)) return val.ToString();
            
            // Format very large numbers in standard pedagogical scientific notation
            if (val >= 10000000)
            {
                int exponent = (int)System.Math.Floor(System.Math.Log10(val));
                double baseVal = val / System.Math.Pow(10, exponent);
                return $"{baseVal:F3} × 10{GetSuperscript(exponent)}";
            }
            return UI.Fmt(val);
        }

        private static string GetSuperscript(int val)
        {
            string s = val.ToString();
            string res = "";
            foreach (char c in s)
            {
                switch (c)
                {
                    case '-': res += "⁻"; break;
                    case '0': res += "⁰"; break;
                    case '1': res += "¹"; break;
                    case '2': res += "²"; break;
                    case '3': res += "³"; break;
                    case '4': res += "⁴"; break;
                    case '5': res += "⁵"; break;
                    case '6': res += "⁶"; break;
                    case '7': res += "⁷"; break;
                    case '8': res += "⁸"; break;
                    case '9': res += "⁹"; break;
                }
            }
            return res;
        }

        private void CalcBox(double a, double b, double c)
        {
            double v = a * b * c;
            double sxq = 2 * (a + b) * c;
            double stp = 2 * (a * b + b * c + c * a);
            double diag = System.Math.Sqrt(a * a + b * b + c * c);
            UI.ResultRow($"V = a · b · c = {UI.Fmt(a)} · {UI.Fmt(b)} · {UI.Fmt(c)} = {FmtResult(v)}", "#1565C0", resultPanel);
            UI.ResultRow($"S_xq = 2(a+b)c = 2 · ({UI.Fmt(a)} + {UI.Fmt(b)}) · {UI.Fmt(c)} = {FmtResult(sxq)}", "#1565C0", resultPanel);
            UI.ResultRow($"S_tp = 2(ab+bc+ca) = 2 · ({UI.Fmt(a)}·{UI.Fmt(b)} + {UI.Fmt(b)}·{UI.Fmt(c)} + {UI.Fmt(c)}·{UI.Fmt(a)}) = {FmtResult(stp)}", "#1565C0", resultPanel);
            UI.ResultRow($"Đường chéo = √(a²+b²+c²) = √({UI.Fmt(a)}²+{UI.Fmt(b)}²+{UI.Fmt(c)}²) = {FmtResult(diag)}", "#1565C0", resultPanel);
        }

        private void CalcCube(double a)
        {
            double v = a * a * a;
            double stp = 6 * a * a;
            double diag = a * System.Math.Sqrt(3);
            UI.ResultRow($"V = a³ = {UI.Fmt(a)}³ = {FmtResult(v)}", "#1565C0", resultPanel);
            UI.ResultRow($"S_tp = 6a² = 6 · {UI.Fmt(a)}² = {FmtResult(stp)}", "#1565C0", resultPanel);
            UI.ResultRow($"Đường chéo = a√3 = {UI.Fmt(a)}√3 ≈ {FmtResult(diag)}", "#1565C0", resultPanel);
        }

        private void CalcTriPyramid(double a, double h)
        {
            double sBase = a * a * System.Math.Sqrt(3) / 4.0;
            double v = sBase * h / 3.0;
            double pBase = 3 * a;
            double apothemPyramid = System.Math.Sqrt(h * h + (a * System.Math.Sqrt(3) / 6.0) * (a * System.Math.Sqrt(3) / 6.0));
            double sxq = 0.5 * pBase * apothemPyramid;
            double lateralEdge = System.Math.Sqrt(h * h + (a * System.Math.Sqrt(3) / 3.0) * (a * System.Math.Sqrt(3) / 3.0));

            UI.ResultRow($"S_đáy = a²√3/4 = {UI.Fmt(a)}²√3/4 = {FmtExactSqrt3(a * a / 4.0)} ≈ {FmtResult(sBase)}", "#1565C0", resultPanel);
            UI.ResultRow($"V = (1/3) · S_đáy · h = (1/3) · ({FmtExactSqrt3(a * a / 4.0)}) · {UI.Fmt(h)} = {FmtExactSqrt3(a * a * h / 12.0)} ≈ {FmtResult(v)}", "#1565C0", resultPanel);
            UI.ResultRow($"Trung đoạn = √(h²+(a√3/6)²) = √({UI.Fmt(h)}²+({UI.Fmt(a)}√3/6)²) = {FmtResult(apothemPyramid)}", "#1565C0", resultPanel);
            UI.ResultRow($"S_xq = (1/2) · Chu vi đáy · trung đoạn = (1/2) · {UI.Fmt(pBase)} · {UI.Fmt(apothemPyramid)} = {FmtResult(sxq)}", "#1565C0", resultPanel);
            UI.ResultRow($"S_tp = S_đáy + S_xq = {FmtResult(sBase + sxq)}", "#1565C0", resultPanel);
            UI.ResultRow($"📌 Cạnh bên = √(h²+(a√3/3)²) = √({UI.Fmt(h)}²+({UI.Fmt(a)}√3/3)²) = {FmtResult(lateralEdge)}", "#E65100", resultPanel);
        }

        private void CalcQuadPyramid(double a, double h)
        {
            double sBase = a * a;
            double v = sBase * h / 3.0;
            double pBase = 4 * a;
            double apothemPyramid = System.Math.Sqrt(h * h + (a / 2.0) * (a / 2.0));
            double sxq = 0.5 * pBase * apothemPyramid;

            UI.ResultRow($"S_đáy = a² = {UI.Fmt(a)}² = {FmtResult(sBase)}", "#1565C0", resultPanel);
            UI.ResultRow($"V = (1/3) · a² · h = (1/3) · {UI.Fmt(a)}² · {UI.Fmt(h)} = {FmtResult(v)}", "#1565C0", resultPanel);
            UI.ResultRow($"Trung đoạn = √(h²+(a/2)²) = √({UI.Fmt(h)}²+({UI.Fmt(a)}/2)²) = {FmtResult(apothemPyramid)}", "#1565C0", resultPanel);
            UI.ResultRow($"S_xq = (1/2) · 4a · trung đoạn = 2 · {UI.Fmt(a)} · {UI.Fmt(apothemPyramid)} = {FmtResult(sxq)}", "#1565C0", resultPanel);
            UI.ResultRow($"S_tp = S_đáy + S_xq = {FmtResult(sBase + sxq)}", "#1565C0", resultPanel);
            UI.ResultRow($"📌 Cạnh bên = √(h²+(a√2/2)²) = √({UI.Fmt(h)}²+({UI.Fmt(a)}√2/2)²) = {FmtResult(System.Math.Sqrt(h * h + a * a / 2.0))}", "#E65100", resultPanel);
        }

        private void CalcTriPrism(double a, double h)
        {
            double sBase = a * a * System.Math.Sqrt(3) / 4.0;
            double v = sBase * h;
            double pBase = 3 * a;
            double sxq = pBase * h;
            double diagFace = System.Math.Sqrt(a * a + h * h);

            UI.ResultRow($"S_đáy = a²√3/4 = {UI.Fmt(a)}²√3/4 = {FmtExactSqrt3(a * a / 4.0)} ≈ {FmtResult(sBase)}", "#1565C0", resultPanel);
            UI.ResultRow($"V = S_đáy · h = ({FmtExactSqrt3(a * a / 4.0)}) · {UI.Fmt(h)} = {FmtExactSqrt3(a * a * h / 4.0)} ≈ {FmtResult(v)}", "#1565C0", resultPanel);
            UI.ResultRow($"S_xq = 3a · h = 3 · {UI.Fmt(a)} · {UI.Fmt(h)} = {FmtResult(sxq)}", "#1565C0", resultPanel);
            UI.ResultRow($"S_tp = 2 · S_đáy + S_xq = 2 · ({FmtExactSqrt3(a * a / 4.0)}) + {UI.Fmt(sxq)} = {FmtResult(2 * sBase + sxq)}", "#1565C0", resultPanel);
            UI.ResultRow($"📌 Đường chéo mặt bên = √(a²+h²) = √({UI.Fmt(a)}²+{UI.Fmt(h)}²) = {FmtResult(diagFace)}", "#E65100", resultPanel);
        }

        private void CalcCylinder(double r, double h)
        {
            double v = System.Math.PI * r * r * h;
            double sxq = 2 * System.Math.PI * r * h;
            double stp = 2 * System.Math.PI * r * (r + h);
            double sAxial = 2 * r * h;

            UI.ResultRow($"V = πr²h = π · {UI.Fmt(r)}² · {UI.Fmt(h)} = {FmtExactPi(r * r * h)} ≈ {FmtResult(v)}", "#1565C0", resultPanel);
            UI.ResultRow($"S_xq = 2πrh = 2π · {UI.Fmt(r)} · {UI.Fmt(h)} = {FmtExactPi(2 * r * h)} ≈ {FmtResult(sxq)}", "#1565C0", resultPanel);
            UI.ResultRow($"S_tp = 2πr(r+h) = 2π · {UI.Fmt(r)} · ({UI.Fmt(r)}+{UI.Fmt(h)}) = {FmtExactPi(2 * r * (r + h))} ≈ {FmtResult(stp)}", "#1565C0", resultPanel);
            UI.ResultRow($"📌 Diện tích thiết diện qua trục = 2r · h = 2 · {UI.Fmt(r)} · {UI.Fmt(h)} = {FmtResult(sAxial)}", "#E65100", resultPanel);
        }

        private void CalcCone(double r, double h)
        {
            double l = System.Math.Sqrt(r * r + h * h); // slant height
            double v = System.Math.PI * r * r * h / 3.0;
            double sxq = System.Math.PI * r * l;
            double stp = System.Math.PI * r * (r + l);
            double sAxial = r * h;

            UI.ResultRow($"Đường sinh l = √(r²+h²) = √({UI.Fmt(r)}²+{UI.Fmt(h)}²) = {FmtResult(l)}", "#1565C0", resultPanel);
            UI.ResultRow($"V = (1/3)πr²h = (1/3)π · {UI.Fmt(r)}² · {UI.Fmt(h)} = {FmtExactPi(r * r * h / 3.0)} ≈ {FmtResult(v)}", "#1565C0", resultPanel);
            UI.ResultRow($"S_xq = πrl = π · {UI.Fmt(r)} · {UI.Fmt(l)} = {FmtExactPi(r * l)} ≈ {FmtResult(sxq)}", "#1565C0", resultPanel);
            UI.ResultRow($"S_tp = πr(r+l) = π · {UI.Fmt(r)} · ({UI.Fmt(r)}+{UI.Fmt(l)}) = {FmtExactPi(r * (r + l))} ≈ {FmtResult(stp)}", "#1565C0", resultPanel);
            UI.ResultRow($"📌 Diện tích thiết diện qua trục = r · h = {UI.Fmt(r)} · {UI.Fmt(h)} = {FmtResult(sAxial)}", "#E65100", resultPanel);
        }

        private void CalcSphere(double r)
        {
            double v = 4.0 / 3.0 * System.Math.PI * r * r * r;
            double s = 4 * System.Math.PI * r * r;

            UI.ResultRow($"V = (4/3)πr³ = (4/3)π · {UI.Fmt(r)}³ = {FmtExactPi(4.0 * r * r * r / 3.0)} ≈ {FmtResult(v)}", "#1565C0", resultPanel);
            UI.ResultRow($"S = 4πr² = 4π · {UI.Fmt(r)}² = {FmtExactPi(4.0 * r * r)} ≈ {FmtResult(s)}", "#1565C0", resultPanel);
            UI.ResultRow($"d (đường kính) = 2r = 2 · {UI.Fmt(r)} = {FmtResult(2 * r)}", "#1565C0", resultPanel);
            UI.ResultRow($"📌 Diện tích thiết diện lớn nhất = πr² = π · {UI.Fmt(r)}² = {FmtExactPi(r * r)} ≈ {FmtResult(System.Math.PI * r * r)} (đường tròn lớn)", "#E65100", resultPanel);
        }

        // ─── FORMULA REFERENCE ───
        private void BuildFormulas()
        {
            formulaPanel.Children.Clear();
            var sections = new (string Title, string[] Items)[]
            {
                ("Khối đa diện", new[]
                {
                    "Hộp chữ nhật: V = abc, S_tp = 2(ab+bc+ca), S_xq = 2(a+b)c",
                    "Lập phương: V = a³, S_tp = 6a²",
                    "Lăng trụ: V = S_đáy · h",
                    "Chóp: V = (1/3) · S_đáy · h",
                }),
                ("Khối tròn xoay", new[]
                {
                    "Hình trụ: V = πr²h, S_xq = 2πrh",
                    "Hình nón: V = (1/3)πr²h, S_xq = πrl",
                    "Hình cầu: V = (4/3)πr³, S_tp = 4πr²",
                }),
                ("Công thức bổ trợ", new[]
                {
                    "Đường sinh nón: l = √(r² + h²)",
                    "Đường chéo hộp: d = √(a²+b²+c²)",
                    "S tam giác đều: a²√3/4",
                    "Trung đoạn chóp đều: d = √(h² + r²) (r: bán kính đường tròn nội tiếp đáy)",
                }),
            };

            foreach (var (title, items) in sections)
            {
                var section = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(15, Accent.R, Accent.G, Accent.B)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 8)
                };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = title, FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Accent), Margin = new Thickness(0, 0, 0, 4)
                });
                foreach (var item in items)
                    sp.Children.Add(new TextBlock
                    {
                        Text = $"  {item}", FontSize = 13,
                        FontFamily = new FontFamily("Segoe UI"),
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                        Margin = new Thickness(0, 1, 0, 1)
                    });
                section.Child = sp;
                formulaPanel.Children.Add(section);
            }
        }

        // ─── HELPERS ───
        private Border MakeChip(string text, bool active)
        {
            var btn = new Border
            {
                Background = new SolidColorBrush(active ? Accent : Color.FromArgb(20, Accent.R, Accent.G, Accent.B)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 6, 6),
                Cursor = Cursors.Hand,
                BorderBrush = new SolidColorBrush(Accent),
                BorderThickness = new Thickness(active ? 0 : 1)
            };
            btn.Child = new TextBlock
            {
                Text = text, FontSize = 14, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(active ? Colors.White : Accent),
                FontFamily = new FontFamily("Segoe UI")
            };
            return btn;
        }

        private static string FmtExact(double val, string symbol)
        {
            if (System.Math.Abs(val - System.Math.Round(val)) < 1e-7)
            {
                int intVal = (int)System.Math.Round(val);
                if (intVal == 1) return symbol;
                if (intVal == -1) return $"-{symbol}";
                return $"{intVal}{symbol}";
            }

            for (int d = 2; d <= 10000; d++)
            {
                double nDouble = val * d;
                if (System.Math.Abs(nDouble - System.Math.Round(nDouble)) < 1e-6)
                {
                    int n = (int)System.Math.Round(nDouble);
                    int g = Gcd(System.Math.Abs(n), d);
                    n /= g;
                    int den = d / g;

                    if (den == 1)
                    {
                        if (n == 1) return symbol;
                        if (n == -1) return $"-{symbol}";
                        return $"{n}{symbol}";
                    }
                    else
                    {
                        if (n == 1) return $"({symbol}/{den})";
                        if (n == -1) return $"(-{symbol}/{den})";
                        return $"({n}{symbol}/{den})";
                    }
                }
            }
            return $"{UI.Fmt(val)}{symbol}";
        }

        private static int Gcd(int a, int b)
        {
            while (b != 0)
            {
                int t = b;
                b = a % b;
                a = t;
            }
            return a;
        }

        private static string FmtExactPi(double val) => FmtExact(val, "π");
        private static string FmtExactSqrt3(double val) => FmtExact(val, "√3");

        // ─── Graph — Cross-Section Visualization ───
        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var ci = CultureInfo.InvariantCulture;
                var vals = new double[_inputs.Count];
                for (int i = 0; i < _inputs.Count; i++)
                {
                    if (!ParsingHelper.TryParseDouble(_inputs[i].Text, out double v) || v <= 0)
                    {
                        MessageBox.Show("Vui lòng nhập các giá trị kích thước là số dương hợp lệ trước khi xem đồ thị.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    vals[i] = v;
                }

                string expJs;
                string expJs3D;
                string title;
                string infoStr;

                switch (_shape)
                {
                    case 0: // Hộp chữ nhật — mặt cắt ngang
                    {
                        double a = vals[0], b = vals[1], c = vals[2];
                        string ha = (a / 2).ToString(ci), hb = (b / 2).ToString(ci), hc = (c / 2).ToString(ci);
                        string na = (-a / 2).ToString(ci), nb = (-b / 2).ToString(ci), nc = (-c / 2).ToString(ci);
                        expJs = $@"
        calc.setExpression({{id:'r', latex:'\\operatorname{{polygon}}(({na},{nb}), ({ha},{nb}), ({ha},{hb}), ({na},{hb}))', color:'#37474F', fillOpacity:0.15, lineWidth:3}});
        calc.setExpression({{id:'o', latex:'(0,0)', color:'#E65100', pointSize:6, label:'Tâm O', showLabel:true}});
        calc.setExpression({{id:'lbl_a', latex:'(0,{nb})', color:'#283593', pointSize:0.1, label:'a={a.ToString(ci)}', showLabel:true}});
        calc.setExpression({{id:'lbl_b', latex:'({ha},0)', color:'#283593', pointSize:0.1, label:'b={b.ToString(ci)}', showLabel:true}});
        calc.setExpression({{id:'lbl_info', latex:'({ha},{hb})', color:'#1565C0', pointSize:6, label:'Hộp chữ nhật: a={a.ToString(ci)}, b={b.ToString(ci)}, c={c.ToString(ci)}', showLabel:true}});
        calc.setMathBounds({{left:{(-a * 1.2).ToString(ci)},right:{(a * 1.2).ToString(ci)},bottom:{(-b * 1.2).ToString(ci)},top:{(b * 1.2).ToString(ci)}}});";

                        expJs3D = $@"
        calc.setExpression({{id:'prism', latex:'\left|x\right|\le{ha}\left\{{\left|y\right|\le{hb}\right\}}\left\{{\left|z\right|\le{hc}\right\}}', color:'#37474F'}});
        calc.setExpression({{id:'plane', latex:'z=0', color:'#E65100'}});";

                        title = $"📦 Mặt cắt hộp {a.ToString(ci)}×{b.ToString(ci)}×{c.ToString(ci)}";
                        infoStr = $"Hộp chữ nhật: a={a.ToString(ci)}, b={b.ToString(ci)}, c={c.ToString(ci)} (Mặt cắt tại z=0)";
                        break;
                    }
                    case 1: // Hình lập phương — mặt cắt ngang là hình vuông
                    {
                        double a = vals[0];
                        string ha = (a / 2).ToString(ci);
                        string na = (-a / 2).ToString(ci);
                        expJs = $@"
        calc.setExpression({{id:'sq', latex:'\\operatorname{{polygon}}(({na},{na}), ({ha},{na}), ({ha},{ha}), ({na},{ha}))', color:'#1565C0', fillOpacity:0.15, lineWidth:3}});
        calc.setExpression({{id:'o', latex:'(0,0)', color:'#E65100', pointSize:6, label:'Tâm O', showLabel:true}});
        calc.setExpression({{id:'lbl_a', latex:'({ha},0)', color:'#283593', pointSize:0.1, label:'a={a.ToString(ci)}', showLabel:true}});
        calc.setExpression({{id:'lbl_info', latex:'({ha},{ha})', color:'#1565C0', pointSize:6, label:'Lập phương: a={a.ToString(ci)}', showLabel:true}});
        calc.setMathBounds({{left:{(-a * 1.2).ToString(ci)},right:{(a * 1.2).ToString(ci)},bottom:{(-a * 1.2).ToString(ci)},top:{(a * 1.2).ToString(ci)}}});";

                        expJs3D = $@"
        calc.setExpression({{id:'cube', latex:'\left|x\right|\le{ha}\left\{{\left|y\right|\le{ha}\right\}}\left\{{\left|z\right|\le{ha}\right\}}', color:'#1565C0'}});
        calc.setExpression({{id:'plane', latex:'z=0', color:'#E65100'}});";

                        title = $"🧊 Mặt cắt hình lập phương a={a.ToString(ci)}";
                        infoStr = $"Hình lập phương: a={a.ToString(ci)} (Mặt cắt tại z=0)";
                        break;
                    }
                    case 2: // Chóp tam giác đều — mặt cắt song song đáy là tam giác đều
                    {
                        double a = vals[0], h = vals[1];
                        double centroidToVertex = a * System.Math.Sqrt(3) / 3.0;
                        double centroidToEdge = a * System.Math.Sqrt(3) / 6.0;
                        double halfA = a / 2.0;

                        // 3D coordinates use the full base 'a'
                        string sCentroidToVertex3D = centroidToVertex.ToString(ci);
                        string sCentroidToEdge3D = centroidToEdge.ToString(ci);
                        string sHalfA3D = halfA.ToString(ci);
                        string sMinusHalfA3D = (-halfA).ToString(ci);
                        string sMinusCentroidToEdge3D = (-centroidToEdge).ToString(ci);
                        string sh = h.ToString(ci);

                        // 2D slice uses aSlice = a / 2.0 (since slice is at z = h / 2)
                        double aSlice = a / 2.0;
                        double centroidToVertex2D = aSlice * System.Math.Sqrt(3) / 3.0;
                        double centroidToEdge2D = aSlice * System.Math.Sqrt(3) / 6.0;
                        double halfA2D = aSlice / 2.0;

                        string sCentroidToVertex2D = centroidToVertex2D.ToString(ci);
                        string sCentroidToEdge2D = centroidToEdge2D.ToString(ci);
                        string sHalfA2D = halfA2D.ToString(ci);
                        string sMinusHalfA2D = (-halfA2D).ToString(ci);
                        string sMinusCentroidToEdge2D = (-centroidToEdge2D).ToString(ci);

                        expJs = $@"
        calc.setExpression({{id:'tri', latex:'\\operatorname{{polygon}}(({sMinusHalfA2D},{sMinusCentroidToEdge2D}), ({sHalfA2D},{sMinusCentroidToEdge2D}), (0,{sCentroidToVertex2D}))', color:'#1565C0', fillOpacity:0.15, lineWidth:3}});
        calc.setExpression({{id:'g', latex:'(0,0)', color:'#E65100', pointSize:6, label:'Trọng tâm G', showLabel:true}});
        calc.setExpression({{id:'lbl_a', latex:'(0,-{sCentroidToEdge2D}-0.15)', color:'#283593', pointSize:0.1, label:'a\'={aSlice.ToString(ci)}', showLabel:true}});
        calc.setExpression({{id:'lbl_info', latex:'(0,{sCentroidToVertex2D})', color:'#1565C0', pointSize:6, label:'Chóp tam giác đều: a={a.ToString(ci)}, h={h.ToString(ci)}', showLabel:true}});
        calc.setMathBounds({{left:{(-a * 1.2).ToString(ci)},right:{(a * 1.2).ToString(ci)},bottom:{(-a * 1.2).ToString(ci)},top:{(a * 1.2).ToString(ci)}}});";

                        expJs3D = $@"
        calc.setExpression({{id:'A', latex:'A=({sMinusHalfA3D},{sMinusCentroidToEdge3D},0)'}});
        calc.setExpression({{id:'B', latex:'B=({sHalfA3D},{sMinusCentroidToEdge3D},0)'}});
        calc.setExpression({{id:'C', latex:'C=(0,{sCentroidToVertex3D},0)'}});
        calc.setExpression({{id:'S', latex:'S=(0,0,{sh})'}});
        calc.setExpression({{id:'base', latex:'\\operatorname{{polygon}}(A, B, C)', color:'#1565C0', fillOpacity:0.2}});
        calc.setExpression({{id:'face1', latex:'\\operatorname{{polygon}}(A, B, S)', color:'#1565C0', fillOpacity:0.2}});
        calc.setExpression({{id:'face2', latex:'\\operatorname{{polygon}}(B, C, S)', color:'#1565C0', fillOpacity:0.2}});
        calc.setExpression({{id:'face3', latex:'\\operatorname{{polygon}}(C, A, S)', color:'#1565C0', fillOpacity:0.2}});
        calc.setExpression({{id:'plane', latex:'z={ (h/2).ToString(ci) }', color:'#E65100'}});";

                        title = $"🔺 Mặt cắt đáy chóp tam giác đều a={a.ToString(ci)}, h={h.ToString(ci)}";
                        infoStr = $"Chóp tam giác đều: a={a.ToString(ci)}, h={h.ToString(ci)} (Mặt cắt song song đáy tại z={ (h/2).ToString(ci) })";
                        break;
                    }
                    case 3: // Chóp tứ giác đều — mặt cắt song song đáy là hình vuông
                    {
                        double a = vals[0], h = vals[1];
                        // 3D coordinates use the full base 'a'
                        string ha3D = (a / 2).ToString(ci);
                        string na3D = (-a / 2).ToString(ci);
                        string sh = h.ToString(ci);

                        // 2D slice uses aSlice = a / 2.0 (since slice is at z = h / 2)
                        double aSlice = a / 2.0;
                        string haSlice = (aSlice / 2.0).ToString(ci);
                        string naSlice = (-aSlice / 2.0).ToString(ci);

                        expJs = $@"
        calc.setExpression({{id:'sq', latex:'\\operatorname{{polygon}}(({naSlice},{naSlice}), ({haSlice},{naSlice}), ({haSlice},{haSlice}), ({naSlice},{haSlice}))', color:'#1565C0', fillOpacity:0.15, lineWidth:3}});
        calc.setExpression({{id:'o', latex:'(0,0)', color:'#E65100', pointSize:6, label:'Tâm O', showLabel:true}});
        calc.setExpression({{id:'lbl_a', latex:'({haSlice},0)', color:'#283593', pointSize:0.1, label:'a\'={aSlice.ToString(ci)}', showLabel:true}});
        calc.setExpression({{id:'lbl_info', latex:'({haSlice},{haSlice})', color:'#1565C0', pointSize:6, label:'Chóp tứ giác đều: a={a.ToString(ci)}, h={h.ToString(ci)}', showLabel:true}});
        calc.setMathBounds({{left:{(-a * 1.2).ToString(ci)},right:{(a * 1.2).ToString(ci)},bottom:{(-a * 1.2).ToString(ci)},top:{(a * 1.2).ToString(ci)}}});";

                        expJs3D = $@"
        calc.setExpression({{id:'A', latex:'A=({na3D},{na3D},0)'}});
        calc.setExpression({{id:'B', latex:'B=({ha3D},{na3D},0)'}});
        calc.setExpression({{id:'C', latex:'C=({ha3D},{ha3D},0)'}});
        calc.setExpression({{id:'D', latex:'D=({na3D},{ha3D},0)'}});
        calc.setExpression({{id:'S', latex:'S=(0,0,{sh})'}});
        calc.setExpression({{id:'base', latex:'\\operatorname{{polygon}}(A, B, C, D)', color:'#1565C0', fillOpacity:0.2}});
        calc.setExpression({{id:'face1', latex:'\\operatorname{{polygon}}(A, B, S)', color:'#1565C0', fillOpacity:0.2}});
        calc.setExpression({{id:'face2', latex:'\\operatorname{{polygon}}(B, C, S)', color:'#1565C0', fillOpacity:0.2}});
        calc.setExpression({{id:'face3', latex:'\\operatorname{{polygon}}(C, D, S)', color:'#1565C0', fillOpacity:0.2}});
        calc.setExpression({{id:'face4', latex:'\\operatorname{{polygon}}(D, A, S)', color:'#1565C0', fillOpacity:0.2}});
        calc.setExpression({{id:'plane', latex:'z={ (h/2).ToString(ci) }', color:'#E65100'}});";

                        title = $"🔺 Mặt cắt đáy chóp tứ giác đều a={a.ToString(ci)}, h={h.ToString(ci)}";
                        infoStr = $"Chóp tứ giác đều: a={a.ToString(ci)}, h={h.ToString(ci)} (Mặt cắt song song đáy tại z={ (h/2).ToString(ci) })";
                        break;
                    }
                    case 4: // Lăng trụ tam giác đều — mặt cắt song song đáy là tam giác đều
                    {
                        double a = vals[0], h = vals[1];
                        double centroidToVertex = a * System.Math.Sqrt(3) / 3.0;
                        double centroidToEdge = a * System.Math.Sqrt(3) / 6.0;
                        double halfA = a / 2.0;

                        string sCentroidToVertex = centroidToVertex.ToString(ci);
                        string sCentroidToEdge = centroidToEdge.ToString(ci);
                        string sHalfA = halfA.ToString(ci);
                        string sMinusHalfA = (-halfA).ToString(ci);
                        string sMinusCentroidToEdge = (-centroidToEdge).ToString(ci);
                        string sh2 = (h / 2).ToString(ci);
                        string nmh2 = (-h / 2).ToString(ci);

                        expJs = $@"
        calc.setExpression({{id:'tri', latex:'\\operatorname{{polygon}}(({sMinusHalfA},{sMinusCentroidToEdge}), ({sHalfA},{sMinusCentroidToEdge}), (0,{sCentroidToVertex}))', color:'#1565C0', fillOpacity:0.15, lineWidth:3}});
        calc.setExpression({{id:'g', latex:'(0,0)', color:'#E65100', pointSize:6, label:'Trọng tâm G', showLabel:true}});
        calc.setExpression({{id:'lbl_a', latex:'(0,-{sCentroidToEdge}-0.15)', color:'#283593', pointSize:0.1, label:'a={a.ToString(ci)}', showLabel:true}});
        calc.setExpression({{id:'lbl_info', latex:'(0,{sCentroidToVertex})', color:'#1565C0', pointSize:6, label:'Lăng trụ tam giác đều: a={a.ToString(ci)}, h={h.ToString(ci)}', showLabel:true}});
        calc.setMathBounds({{left:{(-a * 1.2).ToString(ci)},right:{(a * 1.2).ToString(ci)},bottom:{(-a * 1.2).ToString(ci)},top:{(a * 1.2).ToString(ci)}}});";

                        expJs3D = $@"
        calc.setExpression({{id:'A', latex:'A=({sMinusHalfA},{sMinusCentroidToEdge},{nmh2})'}});
        calc.setExpression({{id:'B', latex:'B=({sHalfA},{sMinusCentroidToEdge},{nmh2})'}});
        calc.setExpression({{id:'C', latex:'C=(0,{sCentroidToVertex},{nmh2})'}});
        calc.setExpression({{id:'A1', latex:'A1=({sMinusHalfA},{sMinusCentroidToEdge},{sh2})'}});
        calc.setExpression({{id:'B1', latex:'B1=({sHalfA},{sMinusCentroidToEdge},{sh2})'}});
        calc.setExpression({{id:'C1', latex:'C1=(0,{sCentroidToVertex},{sh2})'}});
        calc.setExpression({{id:'base1', latex:'\\operatorname{{polygon}}(A, B, C)', color:'#1565C0', fillOpacity:0.2}});
        calc.setExpression({{id:'base2', latex:'\\operatorname{{polygon}}(A1, B1, C1)', color:'#1565C0', fillOpacity:0.2}});
        calc.setExpression({{id:'side1', latex:'\\operatorname{{polygon}}(A, B, B1, A1)', color:'#1565C0', fillOpacity:0.2}});
        calc.setExpression({{id:'side2', latex:'\\operatorname{{polygon}}(B, C, C1, B1)', color:'#1565C0', fillOpacity:0.2}});
        calc.setExpression({{id:'side3', latex:'\\operatorname{{polygon}}(C, A, A1, C1)', color:'#1565C0', fillOpacity:0.2}});
        calc.setExpression({{id:'plane', latex:'z=0', color:'#E65100'}});";

                        title = $"📏 Mặt cắt đáy lăng trụ tam giác đều a={a.ToString(ci)}, h={h.ToString(ci)}";
                        infoStr = $"Lăng trụ tam giác đều: a={a.ToString(ci)}, h={h.ToString(ci)} (Mặt cắt song song đáy tại z=0)";
                        break;
                    }
                    case 5: // Hình trụ — mặt cắt là hình tròn
                    {
                        double r = vals[0], h = vals[1];
                        string rS = r.ToString(ci);
                        string sh2 = (h / 2).ToString(ci);
                        string nmh2 = (-h / 2).ToString(ci);
                        expJs = $@"
        calc.setExpression({{id:'c', latex:'x^2+y^2={rS}^2', color:'#1565C0', lineWidth:3}});
        calc.setExpression({{id:'o', latex:'(0,0)', color:'#E65100', pointSize:6, label:'O', showLabel:true}});
        calc.setExpression({{id:'rad_line', latex:'\\operatorname{{polygon}}((0,0), ({rS},0))', color:'#283593', lineWidth:2}});
        calc.setExpression({{id:'lbl_r', latex:'({(r/2.0).ToString(ci)},{(r*0.08).ToString(ci)})', color:'#283593', pointSize:0.1, label:'r={r.ToString(ci)}', showLabel:true}});
        calc.setExpression({{id:'lbl_info', latex:'({rS},{rS})', color:'#1565C0', pointSize:6, label:'Hình trụ: r={r.ToString(ci)}, h={h.ToString(ci)}', showLabel:true}});
        calc.setMathBounds({{left:{(-r * 1.5).ToString(ci)},right:{(r * 1.5).ToString(ci)},bottom:{(-r * 1.5).ToString(ci)},top:{(r * 1.5).ToString(ci)}}});";

                        expJs3D = $@"
        calc.setExpression({{id:'cyl', latex:'x^2+y^2\le{rS}^2\left\{{ {nmh2} \le z \le {sh2} \right\}}', color:'#1565C0'}});
        calc.setExpression({{id:'plane', latex:'z=0', color:'#E65100'}});";

                        title = $"🟡 Mặt cắt trụ — hình tròn r={r.ToString(ci)}, h={h.ToString(ci)}";
                        infoStr = $"Hình trụ: r={r.ToString(ci)}, h={h.ToString(ci)} (Mặt cắt song song đáy tại z=0)";
                        break;
                    }
                    case 6: // Hình nón — mặt cắt là tam giác cân
                    {
                        double r = vals[0], h = vals[1];
                        double l = System.Math.Sqrt(r * r + h * h);
                        string rS = r.ToString(ci), hS = h.ToString(ci), nrS = (-r).ToString(ci);
                        expJs = $@"
        calc.setExpression({{id:'tri', latex:'\\operatorname{{polygon}}(({nrS},0), ({rS},0), (0,{hS}))', color:'#E65100', fillOpacity:0.15, lineWidth:3}});
        calc.setExpression({{id:'apex', latex:'(0,{hS})', color:'#C62828', pointSize:8, label:'Đỉnh', showLabel:true}});
        calc.setExpression({{id:'lbl_r', latex:'({rS}/2,0)', color:'#283593', pointSize:0.1, label:'r={r.ToString(ci)}', showLabel:true}});
        calc.setExpression({{id:'h_line', latex:'\\operatorname{{polygon}}((0,0), (0,{hS}))', color:'#78909C', lineWidth:2, lineStyle:'dashed'}});
        calc.setExpression({{id:'lbl_h', latex:'(0,{hS}/2)', color:'#283593', pointSize:0.1, label:'h={h.ToString(ci)}', showLabel:true}});
        calc.setExpression({{id:'lbl_l', latex:'({(r/2.0 + r*0.08).ToString(ci)},{(h/2.0 + h*0.08).ToString(ci)})', color:'#283593', pointSize:0.1, label:'l={l.ToString(ci)}', showLabel:true}});
        calc.setExpression({{id:'lbl_info', latex:'({rS},{hS})', color:'#1565C0', pointSize:6, label:'Hình nón: r={r.ToString(ci)}, h={h.ToString(ci)}', showLabel:true}});
        calc.setMathBounds({{left:{(-r * 1.5).ToString(ci)},right:{(r * 1.5).ToString(ci)},bottom:{(-h * 0.2).ToString(ci)},top:{(h * 1.3).ToString(ci)}}});";

                        expJs3D = $@"
        calc.setExpression({{id:'cone', latex:'x^2+y^2\le\left({rS}\cdot\left(1-\frac{{z}}{{{hS}}}\right)\right)^2\left\{{0\le z\le{hS}\right\}}', color:'#E65100'}});
        calc.setExpression({{id:'plane', latex:'y=0', color:'#C62828'}});";

                        title = $"🔻 Mặt cắt nón r={r.ToString(ci)}, h={h.ToString(ci)}";
                        infoStr = $"Hình nón: r={r.ToString(ci)}, h={h.ToString(ci)} (Mặt cắt dọc đi qua trục)";
                        break;
                    }
                    case 7: // Hình cầu — mặt cắt là hình tròn lớn
                    {
                        double r = vals[0];
                        double rSlice = r * System.Math.Sqrt(3) / 2.0;
                        string rS = r.ToString(ci);
                        string rSliceS = rSlice.ToString("G6", ci);
                        expJs = $@"
        calc.setExpression({{id:'c', latex:'x^2+y^2={rSliceS}^2', color:'#C62828', lineWidth:3}});
        calc.setExpression({{id:'rad_line', latex:'\\operatorname{{polygon}}((0,0), ({rSliceS},0))', color:'#283593', lineWidth:2}});
        calc.setExpression({{id:'lbl_r', latex:'({(rSlice/2.0).ToString(ci)},{(rSlice*0.08).ToString(ci)})', color:'#283593', pointSize:0.1, label:'r\'={rSlice.ToString("G4", ci)}', showLabel:true}});
        calc.setExpression({{id:'lbl_info', latex:'({rSliceS},{rSliceS})', color:'#1565C0', pointSize:6, label:'Hình cầu: r={r.ToString(ci)}', showLabel:true}});
        calc.setMathBounds({{left:{(-r * 1.5).ToString(ci)},right:{(r * 1.5).ToString(ci)},bottom:{(-r * 1.5).ToString(ci)},top:{(r * 1.5).ToString(ci)}}});";

                        expJs3D = $@"
        calc.setExpression({{id:'sphere', latex:'x^2+y^2+z^2\le{rS}^2', color:'#C62828'}});
        calc.setExpression({{id:'plane', latex:'z={ (r/2).ToString(ci) }', color:'#E65100'}});";

                        title = $"🔴 Tiết diện cầu r={r.ToString(ci)}";
                        infoStr = $"Hình cầu: r={r.ToString(ci)} (Mặt cắt tại z={ (r/2).ToString(ci) })";
                        break;
                    }
                    default:
                    {
                        expJs = @"
        calc.setExpression({id:'c', latex:'x^2+y^2=25', color:'#37474F', lineWidth:3});
        calc.setMathBounds({left:-8,right:8,bottom:-6,top:6});";

                        expJs3D = @"
        calc.setExpression({id:'sphere', latex:'x^2+y^2+z^2\le25', color:'#37474F'});
        calc.setExpression({id:'plane', latex:'z=0', color:'#E65100'});";

                        title = "🧩 Mặt cắt hình khối";
                        infoStr = "Mặt cắt hình học không gian";
                        break;
                    }
                }

                var win = new GraphWindow(expJs, expJs3D, title, infoStr, true);
                win.Owner = Window.GetWindow(this);
                win.Show();
                DbManager.SaveCalculatorHistory($"Hình học 3D: {Shapes[_shape].Name} (Xem đồ thị)", title);
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rootGrid == null || sideMenu == null || contentCalc == null || contentGuide == null || viewPractical == null)
                return;

            contentCalc.Visibility = Visibility.Collapsed;
            contentGuide.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            int index = sideMenu.SelectedIndex;
            if (index == 0)
            {
                contentCalc.Visibility = Visibility.Visible;
            }
            else if (index == 1)
            {
                contentGuide.Visibility = Visibility.Visible;
            }
            else if (index == 2)
            {
                viewPractical.Visibility = Visibility.Visible;
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
            if (tag == "calc" || tag == "practice")
                sideMenu.SelectedIndex = 0;
            else if (tag == "guide")
                sideMenu.SelectedIndex = 1;
            else if (tag == "app" || tag == "practical")
                sideMenu.SelectedIndex = 2;
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
                        Icon = "🥫",
                        Title = isVN ? "Tối ưu hóa vỏ lon hình trụ" : "Cylinder Packaging Optimization",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_solidgeometry_1_{suffix}.png",
                        Description = isVN 
                            ? "Hình học không gian được ứng dụng trong thiết kế công nghiệp để tối ưu hóa vật liệu sản xuất. Ví dụ: Để thiết kế lon nước thể tích 330ml có diện tích toàn phần (lượng nhôm cần dùng) nhỏ nhất, tỷ lệ bán kính r và chiều cao h tối ưu phải thỏa mãn h = 2r. Đây là bài toán cực trị hình trụ." 
                            : "Solid geometry is applied in industrial design to minimize manufacturing material. For example, to design a 330ml beverage can (cylinder) with the minimum total surface area (aluminum used), the optimal radius-to-height ratio must satisfy h = 2r."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏠",
                        Title = isVN ? "Thiết kế mái nhà hình chóp và nón" : "Architectural Cones & Pyramids",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_solidgeometry_2_{suffix}.png",
                        Description = isVN 
                            ? "Khi xây dựng các mái nhà, tháp chuông chùa, hoặc silo chứa thóc, các kỹ sư tính toán diện tích xung quanh của hình nón hoặc hình chóp để biết lượng tôn/ngói cần lợp, và tính thể tích hình khối để định mức sức chứa hoặc thiết kế hệ thống thông gió." 
                            : "When building conical roofs, church steeples, or grain silos, engineers calculate the lateral surface area of cones and pyramids to estimate roofing tiles or sheets needed, and calculate volume to define storage capacity or design ventilation."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌍",
                        Title = isVN ? "Thể tích Trái Đất và Vỏ khí quyển" : "Earth Volume & Atmosphere Layer",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_solidgeometry_3_{suffix}.png",
                        Description = isVN 
                            ? "Trái Đất được mô hình hóa như một hình cầu bán kính R ≈ 6371 km. Các nhà khoa học sử dụng công thức thể tích hình cầu V = 4/3 * pi * R^3 để tính toán khối lượng riêng trung bình của Trái Đất, hoặc tính hiệu thể tích hai hình cầu đồng tâm để đo thể tích lớp khí quyển bao quanh." 
                            : "Earth is modeled as a sphere of radius R ≈ 6371 km. Scientists use the sphere volume formula V = 4/3 * pi * R^3 to determine Earth's average density, or calculate the difference between two concentric sphere volumes to measure the volume of the atmosphere layer."
                    }
                };

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for SolidGeometryTool: {Err}", ex.Message);
            }
        }

        private void InputBlock_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var isAllowed = System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^[0-9.,]+$");
            e.Handled = !isAllowed;
        }

        private void InputBlock_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                var text = (string)e.DataObject.GetData(DataFormats.Text);
                if (!System.Text.RegularExpressions.Regex.IsMatch(text ?? "", @"^[0-9.,]*$"))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        // ─── DATA CLASS ───
        private record ShapeDef(
            string Name,
            string FormulaText,
            (string Label, string Default)[] Params,
            (string Label, double V1, double V2, double V3)[] Presets
        );
    }
}