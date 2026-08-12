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
    public partial class VectorTool : BaseToolControl
    {
        private enum Mode { TwoVec, Scalar }
        private Mode _mode = Mode.TwoVec;

        private static readonly Color Accent = Color.FromRgb(21, 101, 192);

        public VectorTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextPractice != null) menuTextPractice.Text = isVN ? "Tính toán & Đồ thị" : "Calculator & Graph";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn chi tiết" : "User Guide";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";

                if (sideMenu != null)
                {
                    sideMenu.SelectionChanged += SideMenu_SelectionChanged;
                    sideMenu.SelectedIndex = 0;
                }

                BuildModeButtons();
                BuildPresets();
                BuildFormulas();
                LoadPracticalApps();
                Calc();

                TouchNumPad.Attach(txtX1, step: 1);
                TouchNumPad.Attach(txtY1, step: 1);
                TouchNumPad.Attach(txtX2, step: 1);
                TouchNumPad.Attach(txtY2, step: 1);
                TouchNumPad.Attach(txtSX, step: 1);
                TouchNumPad.Attach(txtSY, step: 1);
                TouchNumPad.Attach(txtK, step: 0.5);
            };
        }

        private void BuildModeButtons()
        {
            modePanel.Children.Clear();
            var modes = new (string Label, Mode M)[]
            {
                ("➕ Phép toán hai vectơ", Mode.TwoVec),
                ("✖️ Tích của một số với vectơ", Mode.Scalar),
            };
            foreach (var (label, m) in modes)
            {
                bool active = m == _mode;
                var btn = MakeChip(label, active);
                var cm = m;
                btn.MouseLeftButtonDown += (_, _) => { _mode = cm; BuildModeButtons(); BuildPresets(); UpdateUI(); Calc(); };
                modePanel.Children.Add(btn);
            }
        }

        private void UpdateUI()
        {
            twoVecInput.Visibility = _mode == Mode.TwoVec ? Visibility.Visible : Visibility.Collapsed;
            scalarInput.Visibility = _mode == Mode.Scalar ? Visibility.Visible : Visibility.Collapsed;
            txtModeTitle.Text = _mode switch
            {
                Mode.TwoVec => "➕ Phép toán hai vectơ (tổng, hiệu, tích vô hướng, góc)",
                Mode.Scalar => "✖️ Tích của một số với vectơ (độ dài, vectơ đơn vị)",
                _ => ""
            };
        }

        // ═══ PRESETS ═══
        private void BuildPresets()
        {
            presetPanel.Children.Clear();
            if (_mode == Mode.TwoVec)
            {
                var presets = new (string Label, double X1, double Y1, double X2, double Y2)[]
                {
                    ("(3,4) ⊕ (1,2)", 3, 4, 1, 2),
                    ("Cùng phương", 2, 4, 1, 2),
                    ("Ngược hướng", 3, 4, -3, -4),
                    ("Vuông góc", 3, 4, -4, 3),
                    ("Vectơ Không", 0, 0, 3, 4)
                };
                foreach (var (label, x1, y1, x2, y2) in presets)
                {
                    var btn = MakeChip(label, false);
                    var cx1 = x1; var cy1 = y1; var cx2 = x2; var cy2 = y2;
                    btn.MouseLeftButtonDown += (_, _) =>
                    {
                        txtX1.Text = UI.Fmt(cx1); txtY1.Text = UI.Fmt(cy1);
                        txtX2.Text = UI.Fmt(cx2); txtY2.Text = UI.Fmt(cy2);
                    };
                    presetPanel.Children.Add(btn);
                }
            }
            else
            {
                var presets = new (string Label, double X, double Y, double K)[]
                {
                    ("Gấp đôi (k=2)", 3, 4, 2),
                    ("Đảo hướng (k=-1)", 3, 4, -1),
                    ("Thu nhỏ (k=0.5)", 2, 4, 0.5),
                    ("Đảo rộng (k=-1.5)", 4, 0, -1.5)
                };
                foreach (var (label, x, y, k) in presets)
                {
                    var btn = MakeChip(label, false);
                    var cx = x; var cy = y; var ck = k;
                    btn.MouseLeftButtonDown += (_, _) =>
                    {
                        txtSX.Text = UI.Fmt(cx); txtSY.Text = UI.Fmt(cy);
                        txtK.Text = UI.Fmt(ck);
                    };
                    presetPanel.Children.Add(btn);
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
            resultPanel.Children.Clear();
            switch (_mode)
            {
                case Mode.TwoVec: CalcTwoVec(); break;
                case Mode.Scalar: CalcScalar(); break;
            }

            foreach (UIElement child in resultPanel.Children)
            {
                if (child is Border border)
                {
                    border.MouseLeftButtonDown += (_, _) => ShowToast();
                }
            }
        }

        private static string FormatExactRoot(double x, double y)
        {
            if (System.Math.Abs(x - System.Math.Round(x)) < 1e-9 && System.Math.Abs(y - System.Math.Round(y)) < 1e-9)
            {
                long ix = (long)System.Math.Round(x);
                long iy = (long)System.Math.Round(y);
                long n = ix * ix + iy * iy;
                if (n == 0) return "0";

                long outVal = 1;
                long inVal = n;
                for (long i = (long)System.Math.Sqrt(n); i >= 2; i--)
                {
                    if (n % (i * i) == 0)
                    {
                        outVal = i;
                        inVal = n / (i * i);
                        break;
                    }
                }

                double decimalVal = System.Math.Sqrt(n);
                if (inVal == 1)
                {
                    return $"{outVal}";
                }
                else if (outVal == 1)
                {
                    return $"√{inVal} ≈ {decimalVal:G6}";
                }
                else
                {
                    return $"{outVal}√{inVal} ≈ {decimalVal:G6}";
                }
            }
            else
            {
                double val = System.Math.Sqrt(x * x + y * y);
                return $"{val:G6}";
            }
        }

        private static string FormatExactRootLaTeX(double x, double y)
        {
            if (System.Math.Abs(x - System.Math.Round(x)) < 1e-9 && System.Math.Abs(y - System.Math.Round(y)) < 1e-9)
            {
                long ix = (long)System.Math.Round(x);
                long iy = (long)System.Math.Round(y);
                long n = ix * ix + iy * iy;
                if (n == 0) return "0";

                long outVal = 1;
                long inVal = n;
                for (long i = (long)System.Math.Sqrt(n); i >= 2; i--)
                {
                    if (n % (i * i) == 0)
                    {
                        outVal = i;
                        inVal = n / (i * i);
                        break;
                    }
                }

                double decimalVal = System.Math.Sqrt(n);
                if (inVal == 1)
                {
                    return $"{outVal}";
                }
                else if (outVal == 1)
                {
                    return $"\\sqrt{{{inVal}}} \\approx {decimalVal:G6}";
                }
                else
                {
                    return $"{outVal}\\sqrt{{{inVal}}} \\approx {decimalVal:G6}";
                }
            }
            else
            {
                double val = System.Math.Sqrt(x * x + y * y);
                return $"{val:G6}";
            }
        }

        private void CalcTwoVec()
        {
            if (!ParsingHelper.TryParseDouble(txtX1?.Text, out double x1)) return;
            if (!ParsingHelper.TryParseDouble(txtY1?.Text, out double y1)) return;
            if (!ParsingHelper.TryParseDouble(txtX2?.Text, out double x2)) return;
            if (!ParsingHelper.TryParseDouble(txtY2?.Text, out double y2)) return;

            if (System.Math.Abs(x1) > 100 || System.Math.Abs(y1) > 100 || System.Math.Abs(x2) > 100 || System.Math.Abs(y2) > 100)
            {
                UI.ResultRow("⚠️ Tọa độ lớn (>100) có thể làm đồ thị khó quan sát.", "#E65100", resultPanel);
            }

            UI.ResultRow($"$\\vec{{a}} = ({UI.Fmt(x1)}, {UI.Fmt(y1)})$", "#1565C0", resultPanel);
            UI.ResultRow($"$\\vec{{b}} = ({UI.Fmt(x2)}, {UI.Fmt(y2)})$", "#1565C0", resultPanel);

            // Addition
            UI.ResultRow($"$\\vec{{a}} + \\vec{{b}} = ({UI.Fmt(x1 + x2)}, {UI.Fmt(y1 + y2)})$", "#1565C0", resultPanel);

            // Subtraction
            UI.ResultRow($"$\\vec{{a}} - \\vec{{b}} = ({UI.Fmt(x1 - x2)}, {UI.Fmt(y1 - y2)})$", "#1565C0", resultPanel);

            // Magnitudes
            double magA = System.Math.Sqrt(x1 * x1 + y1 * y1);
            double magB = System.Math.Sqrt(x2 * x2 + y2 * y2);
            UI.ResultRow($"$|\\vec{{a}}| = {FormatExactRootLaTeX(x1, y1)}$", "#1565C0", resultPanel);
            UI.ResultRow($"$|\\vec{{b}}| = {FormatExactRootLaTeX(x2, y2)}$", "#1565C0", resultPanel);

            // Dot product
            double dot = x1 * x2 + y1 * y2;
            UI.ResultRow($"$\\vec{{a}} \\cdot \\vec{{b}} = {dot:G8}$", "#1565C0", resultPanel);

            // Angle
            if (magA > 1e-10 && magB > 1e-10)
            {
                double cosAngle = dot / (magA * magB);
                cosAngle = System.Math.Max(-1, System.Math.Min(1, cosAngle)); // clamp
                double angle = System.Math.Acos(cosAngle);
                double angleDeg = angle * 180.0 / System.Math.PI;
                UI.ResultRow($"$\\cos(\\vec{{a}}, \\vec{{b}}) = \\frac{{\\vec{{a}} \\cdot \\vec{{b}}}}{{|\\vec{{a}}| \\cdot |\\vec{{b}}|}} = \\frac{{{UI.Fmt(dot)}}}{{{UI.Fmt(magA)} \\cdot {UI.Fmt(magB)}}} \\approx {cosAngle:G6}$", "#1565C0", resultPanel);
                UI.ResultRow($"$\\angle(\\vec{{a}}, \\vec{{b}}) \\approx {angle:G6}\\text{{ rad}} = {angleDeg:F2}^\\circ$", "#1565C0", resultPanel);
            }

            // Collinear / Perpendicular check
            if (magA < 1e-10 || magB < 1e-10)
            {
                UI.ResultRow("📌 Quan hệ: Cùng phương và vuông góc với mọi vectơ (do có vectơ không)", "#E65100", resultPanel);
            }
            else
            {
                double cross = x1 * y2 - y1 * x2;
                if (System.Math.Abs(cross) < 1e-10)
                {
                    string dir = dot > 0 ? "cùng hướng" : "ngược hướng";
                    UI.ResultRow($"📌 Quan hệ: Cùng phương ({dir})  $x_1y_2 - y_1x_2 = 0$", "#E65100", resultPanel);
                }
                else if (System.Math.Abs(dot) < 1e-10)
                {
                    UI.ResultRow("📌 Quan hệ: Vuông góc  $\\vec{a} \\cdot \\vec{b} = 0$", "#2E7D32", resultPanel);
                }
                else
                {
                    UI.ResultRow("📌 Quan hệ: Không cùng phương, không vuông góc", "#757575", resultPanel);
                }
            }
        }

        private void CalcScalar()
        {
            if (!ParsingHelper.TryParseDouble(txtSX?.Text, out double x)) return;
            if (!ParsingHelper.TryParseDouble(txtSY?.Text, out double y)) return;
            if (!ParsingHelper.TryParseDouble(txtK?.Text, out double k)) return;

            if (System.Math.Abs(x) > 100 || System.Math.Abs(y) > 100)
            {
                UI.ResultRow("⚠️ Tọa độ lớn (>100) có thể làm đồ thị khó quan sát.", "#E65100", resultPanel);
            }

            double mag = System.Math.Sqrt(x * x + y * y);
            UI.ResultRow($"$\\vec{{a}} = ({UI.Fmt(x)}, {UI.Fmt(y)})$", "#1565C0", resultPanel);
            UI.ResultRow($"$|\\vec{{a}}| = {FormatExactRootLaTeX(x, y)}$", "#1565C0", resultPanel);

            // k * a
            UI.ResultRow($"{UI.Fmt(k)}\\cdot\\vec{{a}} = ({UI.Fmt(k * x)}, {UI.Fmt(k * y)})", "#1565C0", resultPanel);
            UI.ResultRow($"$|{UI.Fmt(k)}\\cdot\\vec{{a}}| = {System.Math.Abs(k) * mag:G8}$", "#1565C0", resultPanel);

            // Unit vector
            if (mag > 1e-12)
            {
                double ux = x / mag, uy = y / mag;
                UI.ResultRow($"$\\vec{{a}}_0\\text{{ (đơn vị)}} = ({ux:G6}, {uy:G6})$", "#1565C0", resultPanel);
            }

            // Direction angle
            double angle = System.Math.Atan2(y, x);
            double angleDeg = angle * 180.0 / System.Math.PI;
            UI.ResultRow($"Góc với trục hoành Ox = ${angle:G6}\\text{{ rad}} = {angleDeg:F2}^\\circ$", "#1565C0", resultPanel);
        }

        // ═══ FORMULAS ═══
        private void BuildFormulas()
        {
            formulaPanel.Children.Clear();
            var sections = new (string Title, string[] Items)[]
            {
                ("Phép toán cơ bản", new[]
                {
                    "$\\vec{a} + \\vec{b} = (x_1+x_2, y_1+y_2)$",
                    "$\\vec{a} - \\vec{b} = (x_1-x_2, y_1-y_2)$",
                    "$k \\cdot \\vec{a} = (kx, ky)$",
                }),
                ("Độ gia tăng & Tích vô hướng", new[]
                {
                    "$|\\vec{a}| = \\sqrt{x^2 + y^2}$",
                    "$\\vec{a} \\cdot \\vec{b} = x_1x_2 + y_1y_2$",
                    "$\\vec{a} \\cdot \\vec{b} = |\\vec{a}| \\cdot |\\vec{b}| \\cdot \\cos(\\vec{a},\\vec{b})$",
                }),
                ("Quan hệ hình học", new[]
                {
                    "Cùng phương: $x_1y_2 - y_1x_2 = 0$",
                    "Vuông góc: $\\vec{a} \\cdot \\vec{b} = 0$",
                    "$\\cos(\\vec{a},\\vec{b}) = \\frac{\\vec{a} \\cdot \\vec{b}}{|\\vec{a}| \\cdot |\\vec{b}|}$",
                }),
                ("Vectơ đơn vị", new[]
                {
                    "$\\vec{a}_0 = \\frac{\\vec{a}}{|\\vec{a}|}$  $(|\\vec{a}_0| = 1)$",
                    "$\\vec{i} = (1,0)$, $\\vec{j} = (0,1)$",
                    "$\\vec{a} = x \\cdot \\vec{i} + y \\cdot \\vec{j}$",
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
                    Text = title, FontSize = 12, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Accent), Margin = new Thickness(0, 0, 0, 4)
                });
                foreach (var item in items)
                {
                    var element = UI.RenderMixedContent(item, 12, new SolidColorBrush(Color.FromRgb(33, 33, 33)));
                    if (element is FrameworkElement fe)
                    {
                        fe.Margin = new Thickness(8, 2, 0, 2);
                    }
                    sp.Children.Add(element);
                }
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
                double x1, y1, x2, y2;
                double k = 1.0;

                if (_mode == Mode.TwoVec)
                {
                    ParsingHelper.TryParseDouble(txtX1?.Text, out x1);
                    ParsingHelper.TryParseDouble(txtY1?.Text, out y1);
                    ParsingHelper.TryParseDouble(txtX2?.Text, out x2);
                    ParsingHelper.TryParseDouble(txtY2?.Text, out y2);
                }
                else
                {
                    ParsingHelper.TryParseDouble(txtSX?.Text, out x1);
                    ParsingHelper.TryParseDouble(txtSY?.Text, out y1);
                    ParsingHelper.TryParseDouble(txtK?.Text, out k);
                    x2 = k * x1; y2 = k * y1;
                }

                double sx = x1 + x2, sy = y1 + y2;

                // Calculate bounds dynamically
                double maxVal = System.Math.Max(System.Math.Max(System.Math.Abs(x1), System.Math.Abs(y1)), System.Math.Max(System.Math.Abs(x2), System.Math.Abs(y2)));
                if (_mode == Mode.TwoVec)
                {
                    maxVal = System.Math.Max(maxVal, System.Math.Max(System.Math.Abs(sx), System.Math.Abs(sy)));
                }
                double margin = System.Math.Max(5, maxVal * 1.4);

                // Helper to generate arrowhead polygons in Desmos
                string GetArrowJs(string id, double endX, double endY, string color, double startX = 0, double startY = 0)
                {
                    double vx = endX - startX;
                    double vy = endY - startY;
                    double len = System.Math.Sqrt(vx * vx + vy * vy);
                    if (len < 1e-6) return "";
                    double ux = vx / len, uy = vy / len;
                    double wx = -uy, wy = ux;
                    double d = System.Math.Min(0.4, len * 0.25);
                    double w = d * 0.5;
                    double p1x = endX - d * ux + w * wx;
                    double p1y = endY - d * uy + w * wy;
                    double p2x = endX - d * ux - w * wx;
                    double p2y = endY - d * uy - w * wy;
                    return $"calc.setExpression({{id:'{id}_arr', latex:'polygon(({endX.ToString(ci)},{endY.ToString(ci)}),({p1x.ToString(ci)},{p1y.ToString(ci)}),({p2x.ToString(ci)},{p2y.ToString(ci)}))', color:'{color}', fillOpacity:1}});";
                }

                string expJs = "";
                if (_mode == Mode.TwoVec)
                {
                    double dx = x1 - x2;
                    double dy = y1 - y2;
                    expJs = $@"
        calc.setExpression({{id:'origin', latex:'(0,0)', color:'#9E9E9E', pointSize:8}});
        calc.setExpression({{id:'a', latex:'(t*{x1.ToString(ci)}, t*{y1.ToString(ci)})', color:'#1565C0', lineWidth:3, parametricDomain:{{min:'0',max:'1'}}, label:'a⃗=({UI.Fmt(x1)},{UI.Fmt(y1)})', showLabel:true}});
        {GetArrowJs("a", x1, y1, "#1565C0")}
        calc.setExpression({{id:'b', latex:'(t*{x2.ToString(ci)}, t*{y2.ToString(ci)})', color:'#C62828', lineWidth:3, parametricDomain:{{min:'0',max:'1'}}, label:'b⃗=({UI.Fmt(x2)},{UI.Fmt(y2)})', showLabel:true}});
        {GetArrowJs("b", x2, y2, "#C62828")}
        calc.setExpression({{id:'sum', latex:'(t*{sx.ToString(ci)}, t*{sy.ToString(ci)})', color:'#E65100', lineWidth:2.5, lineStyle:'DASHED', parametricDomain:{{min:'0',max:'1'}}, label:'a⃗+b⃗', showLabel:true}});
        {GetArrowJs("sum", sx, sy, "#E65100")}
        calc.setExpression({{id:'sub', latex:'({x2.ToString(ci)} + t*{dx.ToString(ci)}, {y2.ToString(ci)} + t*{dy.ToString(ci)})', color:'#8E24AA', lineWidth:2, lineStyle:'DASHED', parametricDomain:{{min:'0',max:'1'}}, label:'a⃗-b⃗', showLabel:true}});
        {GetArrowJs("sub", x1, y1, "#8E24AA", x2, y2)}
        calc.setExpression({{id:'help1', latex:'((1-t)*{x1.ToString(ci)} + t*{sx.ToString(ci)}, (1-t)*{y1.ToString(ci)} + t*{sy.ToString(ci)})', color:'#BDBDBD', lineStyle:'DASHED', lineWidth:1.5, parametricDomain:{{min:'0',max:'1'}}}});
        calc.setExpression({{id:'help2', latex:'((1-t)*{x2.ToString(ci)} + t*{sx.ToString(ci)}, (1-t)*{y2.ToString(ci)} + t*{sy.ToString(ci)})', color:'#BDBDBD', lineStyle:'DASHED', lineWidth:1.5, parametricDomain:{{min:'0',max:'1'}}}});
        calc.setExpression({{id:'xaxis', latex:'y=0', color:'#BDBDBD', lineWidth:0.5}});
        calc.setExpression({{id:'yaxis', latex:'x=0', color:'#BDBDBD', lineWidth:0.5}});
        calc.setMathBounds({{ left: {(-margin).ToString(ci)}, right: {margin.ToString(ci)}, bottom: {(-margin).ToString(ci)}, top: {margin.ToString(ci)} }});";
                }
                else
                {
                    expJs = $@"
        calc.setExpression({{id:'origin', latex:'(0,0)', color:'#9E9E9E', pointSize:8}});
        calc.setExpression({{id:'a', latex:'(t*{x1.ToString(ci)}, t*{y1.ToString(ci)})', color:'#1565C0', lineWidth:3, parametricDomain:{{min:'0',max:'1'}}, label:'a⃗=({UI.Fmt(x1)},{UI.Fmt(y1)})', showLabel:true}});
        {GetArrowJs("a", x1, y1, "#1565C0")}
        calc.setExpression({{id:'b', latex:'(t*{x2.ToString(ci)}, t*{y2.ToString(ci)})', color:'#C62828', lineWidth:3.5, parametricDomain:{{min:'0',max:'1'}}, label:'{UI.Fmt(k)}·a⃗=({UI.Fmt(x2)},{UI.Fmt(y2)})', showLabel:true}});
        {GetArrowJs("b", x2, y2, "#C62828")}
        calc.setExpression({{id:'xaxis', latex:'y=0', color:'#BDBDBD', lineWidth:0.5}});
        calc.setExpression({{id:'yaxis', latex:'x=0', color:'#BDBDBD', lineWidth:0.5}});
        calc.setMathBounds({{ left: {(-margin).ToString(ci)}, right: {margin.ToString(ci)}, bottom: {(-margin).ToString(ci)}, top: {margin.ToString(ci)} }});";
                }

                var win = new GraphWindow(expJs, "🏹 Vectơ trên mặt phẳng");
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở đồ thị: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ═══ HELPERS ═══
        private Border MakeChip(string text, bool active)
        {
            var btn = new Border
            {
                Background = new SolidColorBrush(active ? Accent : Color.FromArgb(20, Accent.R, Accent.G, Accent.B)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 6, 6),
                Cursor = Cursors.Hand,
                BorderBrush = new SolidColorBrush(Accent),
                BorderThickness = new Thickness(active ? 0 : 1)
            };
            btn.Child = new TextBlock
            {
                Text = text, FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(active ? Colors.White : Accent),
                FontFamily = new FontFamily("Segoe UI")
            };
            return btn;
        }

        /* AddResult / AddR replaced by UI.ResultRow */

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rootGrid == null || sideMenu == null || contentCalc == null || contentGuide == null || gridApp == null)
                return;

            contentCalc.Visibility = Visibility.Collapsed;
            contentGuide.Visibility = Visibility.Collapsed;
            gridApp.Visibility = Visibility.Collapsed;

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
            if (tag == "calc" || tag == "practice")
                sideMenu.SelectedIndex = 1;
            else if (tag == "guide")
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
                        Icon = "✈",
                        Title = isVN ? "️ Hàng Không & Hướng Gió" : "Navigation & Flight Paths",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_vector_1_{suffix}.png",
                        Description = isVN 
                            ? "Vectơ vận tốc thực tế của máy bay là tổng vectơ của vận tốc động cơ và vận tốc gió thổi. Giúp phi công điều chỉnh góc bay an toàn." 
                            : "Calculate airplane headings by adding engine thrust vectors and crosswind velocity vectors to stay on course."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💪",
                        Title = isVN ? "Phân Tích Lực Vật Lý" : "Game Engine Physics",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_vector_2_{suffix}.png",
                        Description = isVN 
                            ? "Hợp lực của các lực kéo, trọng lực hay phản lực tác dụng lên một vật thể được mô tả và tính toán chính xác bằng phép cộng vectơ." 
                            : "Simulate character movement, jumping forces, and collision impacts by tracking velocity and force vectors."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⛵",
                        Title = isVN ? "Thuyền Qua Sông" : "Structural Force Balancing",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_vector_3_{suffix}.png",
                        Description = isVN 
                            ? "Dưới tác động đồng thời của dòng chảy nước sông và lực đẩy động cơ thuyền, hướng đi thực của thuyền lệch theo quy tắc cộng vectơ vận tốc." 
                            : "Analyze mechanical loads on bridge trusses and building frames to ensure net force vectors sum to zero."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎮",
                        Title = isVN ? "Đồ Họa Máy Tính 3D" : "Satellite Orbit Control",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_vector_4_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng vectơ pháp tuyến bề mặt để tính toán phản xạ ánh sáng, đổ bóng 3D chân thực và mô phỏng động học chuyển động vật lý game." 
                            : "Adjust thrust vectors of thrusters to keep communication satellites locked in correct orbital positions."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📡",
                        Title = isVN ? "Định Vị Toàn Cầu GPS" : "Wind & Ocean Currents",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_vector_5_{suffix}.png",
                        Description = isVN 
                            ? "Hệ thống định vị GPS kết hợp các vectơ khoảng cách và hướng từ tối thiểu 3 vệ tinh ngoài không gian để xác định vị trí thực tế của bạn." 
                            : "Predict sailing paths and weather trends by mapping wind and ocean current vector fields on charts."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💨",
                        Title = isVN ? "Thuyền Đi Ngược Gió" : "Magnetic Fields in Physics",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_vector_6_{suffix}.png",
                        Description = isVN 
                            ? "Bằng cách phân tích vectơ lực đẩy cánh buồm và lực cản của bánh lái dưới nước, thuyền buồm có thể di chuyển chéo tiến lên phía trước ngược chiều gió." 
                            : "Map electromagnetic forces and field lines around generators and electric motors using vector field formulas."
                    }
                ,
                    new PracticalAppItem
                    {
                        Icon = "Bridge",
                        Title = isVN ? "Lực căng cáp cầu treo" : "Suspension Bridge Cable Tension",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_vector_7_{suffix}.png",
                        Description = isVN 
                            ? "Phân tích lực căng trong các sợi cáp treo bằng tổng vectơ lực để đảm bảo cầu chịu lực cân bằng dưới tải trọng xe." 
                            : "Analyze tension in suspension cables using vector addition to ensure structural equilibrium under heavy traffic."
                    },
                    new PracticalAppItem
                    {
                        Icon = "✈️",
                        Title = isVN ? "Vận tốc thực tế máy bay" : "Aircraft True Ground Velocity",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_vector_8_{suffix}.png",
                        Description = isVN 
                            ? "Cộng vectơ vận tốc của máy bay và vectơ vận tốc gió để xác định tốc độ và hướng bay thực tế trên mặt đất." 
                            : "Add the aircraft air velocity vector and wind velocity vector to determine the true ground speed and direction."
                    }};

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for VectorTool: {Err}", ex.Message);
            }
        }

        private void Reset_Click(object sender, MouseButtonEventArgs e)
        {
            if (_mode == Mode.TwoVec)
            {
                txtX1.Text = "3";
                txtY1.Text = "4";
                txtX2.Text = "-1";
                txtY2.Text = "2";
            }
            else
            {
                txtSX.Text = "3";
                txtSY.Text = "4";
                txtK.Text = "2";
            }
            Calc();
        }

        private async void ShowToast()
        {
            if (copyToast != null)
            {
                copyToast.IsOpen = true;
                await System.Threading.Tasks.Task.Delay(1000);
                copyToast.IsOpen = false;
            }
        }

        private static string Fmt(double v) => v == (int)v ? $"{(int)v}" : $"{v:G6}";
    }
}
