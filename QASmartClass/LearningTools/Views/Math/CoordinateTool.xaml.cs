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
    public partial class CoordinateTool : BaseToolControl
    {
        private enum Mode { TwoPoints, Line }
        private Mode _mode = Mode.TwoPoints;

        private static readonly Color Accent = Color.FromRgb(26, 35, 126);

        private Border tabCalc;
        private TextBlock tabCalcText;
        private Border tabGuide;
        private TextBlock tabGuideText;
        private Border tabApp;
        private TextBlock tabAppText;

        public CoordinateTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextPractice != null) menuTextPractice.Text = isVN ? "Tính toán & Đồ thị" : "Calculation & Graph";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";
                
                if (sideMenu != null) sideMenu.SelectedIndex = 0;

                BuildModeButtons();
                BuildPresets();
                BuildFormulas();
                LoadPracticalApps();
                Calc();

                // Bàn phím số mini
                TouchNumPad.Attach(txtX1, step: 1);
                TouchNumPad.Attach(txtY1, step: 1);
                TouchNumPad.Attach(txtX2, step: 1);
                TouchNumPad.Attach(txtY2, step: 1);
                TouchNumPad.Attach(txtK, step: 0.5);
                TouchNumPad.Attach(txtM, step: 0.5);
            };
        }

        // ═══ CHỌN CHỨC NĂNG CON ═══
        private void BuildModeButtons()
        {
            modePanel.Children.Clear();
            var modes = new (string Label, Mode M)[]
            {
                ("📍 Hai Điểm A & B", Mode.TwoPoints),
                ("📐 PT Đường Thẳng y = kx + m", Mode.Line)
            };
            foreach (var (label, m) in modes)
            {
                bool active = m == _mode;
                var btn = MakeChip(label, active);
                var cm = m;
                btn.MouseLeftButtonDown += (_, _) =>
                {
                    _mode = cm;
                    BuildModeButtons();
                    BuildPresets();
                    UpdateUI();
                    Calc();
                };
                modePanel.Children.Add(btn);
            }
        }

        private void UpdateUI()
        {
            twoPointsInput.Visibility = _mode == Mode.TwoPoints ? Visibility.Visible : Visibility.Collapsed;
            lineInput.Visibility = _mode == Mode.Line ? Visibility.Visible : Visibility.Collapsed;
            txtModeTitle.Text = _mode switch
            {
                Mode.TwoPoints => "📍 Hai Điểm A & B — Khoảng cách, trung điểm, PT đường thẳng",
                Mode.Line => "📐 Đường Thẳng y = kx + m — Giao điểm trục, góc, dạng tổng quát",
                _ => ""
            };
        }

        // ═══ VÍ DỤ NHANH (PRESETS) ═══
        private void BuildPresets()
        {
            presetPanel.Children.Clear();
            if (_mode == Mode.TwoPoints)
            {
                var presets = new (string Label, double X1, double Y1, double X2, double Y2)[]
                {
                    ("Số đẹp (1,2) & (4,6)", 1, 2, 4, 6),
                    ("Căn thức (1,1) & (3,3)", 1, 1, 3, 3),
                    ("Đường đứng (3,2) & (3,7)", 3, 2, 3, 7),
                    ("Đường ngang (1,3) & (5,3)", 1, 3, 5, 3),
                    ("Trùng điểm (2,3) & (2,3)", 2, 3, 2, 3)
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
                var presets = new (string Label, double K, double M)[]
                {
                    ("Dốc lên (k=2, m=-1)", 2, -1),
                    ("Dốc xuống (k=-0.5, m=4)", -0.5, 4),
                    ("Nằm ngang (k=0, m=3)", 0, 3)
                };
                foreach (var (label, k, m) in presets)
                {
                    var btn = MakeChip(label, false);
                    var ck = k; var cm = m;
                    btn.MouseLeftButtonDown += (_, _) =>
                    {
                        txtK.Text = UI.Fmt(ck); txtM.Text = UI.Fmt(cm);
                    };
                    presetPanel.Children.Add(btn);
                }
            }
        }

        private void TwoPoints_Changed(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) Calc();
        }

        private void Line_Changed(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) Calc();
        }

        // ═══ TÍNH TOÁN ═══
        private void Calc()
        {
            resultPanel.Children.Clear();
            switch (_mode)
            {
                case Mode.TwoPoints: CalcTwoPoints(); break;
                case Mode.Line: CalcLine(); break;
            }

            foreach (UIElement child in resultPanel.Children)
            {
                if (child is Border border)
                {
                    border.MouseLeftButtonDown += (_, _) => ShowToast();
                }
            }
        }

        private static string FormatExactRoot(double dx, double dy)
        {
            if (System.Math.Abs(dx - System.Math.Round(dx)) < 1e-9 && System.Math.Abs(dy - System.Math.Round(dy)) < 1e-9)
            {
                long idx = (long)System.Math.Round(dx);
                long idy = (long)System.Math.Round(dy);
                long n = idx * idx + idy * idy;
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
                double val = System.Math.Sqrt(dx * dx + dy * dy);
                return $"{val:G6}";
            }
        }

        private void CalcTwoPoints()
        {
            if (!ParsingHelper.TryParseDouble(txtX1?.Text, out double x1)) return;
            if (!ParsingHelper.TryParseDouble(txtY1?.Text, out double y1)) return;
            if (!ParsingHelper.TryParseDouble(txtX2?.Text, out double x2)) return;
            if (!ParsingHelper.TryParseDouble(txtY2?.Text, out double y2)) return;

            if (System.Math.Abs(x1) > 100 || System.Math.Abs(y1) > 100 || System.Math.Abs(x2) > 100 || System.Math.Abs(y2) > 100)
            {
                UI.ResultRow("⚠️ Tọa độ lớn (>100) có thể làm đồ thị khó quan sát.", "#E65100", resultPanel);
            }

            UI.ResultRow($"📍 A({UI.Fmt(x1)}; {UI.Fmt(y1)}), B({UI.Fmt(x2)}; {UI.Fmt(y2)})", "#1A237E", resultPanel);

            // Khoảng cách AB
            UI.ResultRow($"📏 Khoảng cách AB = √[({UI.Fmt(x2)}−{UI.Fmt(x1)})² + ({UI.Fmt(y2)}−{UI.Fmt(y1)})²] = {FormatExactRoot(x2 - x1, y2 - y1)}", "#1565C0", resultPanel);

            // Trung điểm M
            double mx = (x1 + x2) / 2, my = (y1 + y2) / 2;
            UI.ResultRow($"⊕ Trung điểm M({UI.Fmt(mx)}; {UI.Fmt(my)})", "#2E7D32", resultPanel);

            // Hệ số góc & Đường thẳng & Góc
            if (System.Math.Abs(x2 - x1) > 1e-12)
            {
                double k = (y2 - y1) / (x2 - x1);
                double m = y1 - k * x1;
                UI.ResultRow($"📐 Hệ số góc k = (y₂−y₁)/(x₂−x₁) = {UI.Fmt(k)}", "#7B1FA2", resultPanel);
                UI.ResultRow($"📏 PT đường thẳng AB: y = {UI.Fmt(k)}x + ({UI.Fmt(m)})", "#E65100", resultPanel);
                UI.ResultRow($"📝 Dạng tổng quát: {UI.Fmt(k)}x + (−1)y + ({UI.Fmt(m)}) = 0", "#004D40", resultPanel);

                // Sửa lỗi sư phạm góc âm
                double angleDeg = System.Math.Atan(k) * 180 / System.Math.PI;
                if (angleDeg < 0) angleDeg += 180;
                UI.ResultRow($"📐 Góc với trục hoành Ox: α = {UI.Fmt(angleDeg)}°", "#AD1457", resultPanel);
            }
            else
            {
                if (System.Math.Abs(y2 - y1) <= 1e-12)
                {
                    UI.ResultRow("📐 Hệ số góc k: Không xác định (hai điểm trùng nhau)", "#7B1FA2", resultPanel);
                    UI.ResultRow("📏 PT đường thẳng AB: Không xác định", "#E65100", resultPanel);
                }
                else
                {
                    UI.ResultRow("📐 Hệ số góc k: Không xác định (đường thẳng đứng)", "#7B1FA2", resultPanel);
                    UI.ResultRow("📐 Góc với trục hoành Ox: α = 90°", "#AD1457", resultPanel);
                    UI.ResultRow($"📏 PT đường thẳng AB: x = {UI.Fmt(x1)}", "#E65100", resultPanel);
                    UI.ResultRow($"📝 Dạng tổng quát: 1x + 0y + ({UI.Fmt(-x1)}) = 0", "#004D40", resultPanel);
                }
            }

            // Vector AB
            UI.ResultRow($"→ Vector AB = ({UI.Fmt(x2 - x1)}; {UI.Fmt(y2 - y1)})", "#004D40", resultPanel);
        }

        private void CalcLine()
        {
            if (!ParsingHelper.TryParseDouble(txtK?.Text, out double k)) return;
            if (!ParsingHelper.TryParseDouble(txtM?.Text, out double m)) return;

            if (System.Math.Abs(k) > 100 || System.Math.Abs(m) > 100)
            {
                UI.ResultRow("⚠️ Hệ số lớn (>100) có thể làm đồ thị khó quan sát.", "#E65100", resultPanel);
            }

            UI.ResultRow($"📏 PT đường thẳng: y = {UI.Fmt(k)}x + ({UI.Fmt(m)})", "#1A237E", resultPanel);

            // Giao Ox
            if (System.Math.Abs(k) > 1e-12)
            {
                double xInt = -m / k;
                UI.ResultRow($"🔴 Giao Ox: ({UI.Fmt(xInt)}; 0)", "#C62828", resultPanel);
            }
            else
            {
                UI.ResultRow("🔴 Song song Ox (không cắt)", "#C62828", resultPanel);
            }

            // Giao Oy
            UI.ResultRow($"🔵 Giao Oy: (0; {UI.Fmt(m)})", "#1565C0", resultPanel);

            // Sửa góc âm Ox
            double angleDeg = System.Math.Atan(k) * 180 / System.Math.PI;
            if (angleDeg < 0) angleDeg += 180;
            UI.ResultRow($"📐 Góc với trục hoành Ox: α = {UI.Fmt(angleDeg)}°", "#7B1FA2", resultPanel);

            // Dạng tổng quát
            UI.ResultRow($"📝 Dạng tổng quát: {UI.Fmt(k)}x + (−1)y + ({UI.Fmt(m)}) = 0", "#004D40", resultPanel);

            // Tính chất đi lên/xuống
            if (k > 0) UI.ResultRow("📈 Đường thẳng đi lên (k > 0)", "#2E7D32", resultPanel);
            else if (k < 0) UI.ResultRow("📉 Đường thẳng đi xuống (k < 0)", "#E65100", resultPanel);
            else UI.ResultRow("➡️ Đường nằm ngang (k = 0)", "#757575", resultPanel);
        }

        // ═══ CÔNG THỨC THAM KHẢO ═══
        private void BuildFormulas()
        {
            formulaPanel.Children.Clear();
            var sections = new (string Title, string[] Items)[]
            {
                ("Đại lượng cơ bản", new[]
                {
                    "Độ dài AB = √[(x₂−x₁)² + (y₂−y₁)²]",
                    "Trung điểm M = ((x₁+x₂)/2; (y₁+y₂)/2)",
                    "Vector AB = (x₂−x₁; y₂−y₁)"
                }),
                ("Hệ số góc & Góc", new[]
                {
                    "Hệ số góc k = (y₂−y₁)/(x₂−x₁)",
                    "Góc với Ox: α = atan(k)  (α ≥ 0°)"
                }),
                ("Phương trình đường thẳng", new[]
                {
                    "Dạng hệ số góc: y = kx + m",
                    "Dạng tổng quát: ax + by + c = 0"
                })
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
                    sp.Children.Add(new TextBlock
                    {
                        Text = $"  {item}", FontSize = 12,
                        FontFamily = new FontFamily("Segoe UI"),
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                        Margin = new Thickness(0, 1, 0, 1)
                    });
                }
                section.Child = sp;
                formulaPanel.Children.Add(section);
            }
        }

        // ═══ ĐỒ THỊ ═══
        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            if (_mode == Mode.TwoPoints) OpenGraph_TwoPoints();
            else OpenGraph_Line();
        }

        private void OpenGraph_TwoPoints()
        {
            try
            {
                ParsingHelper.TryParseDouble(txtX1?.Text, out double x1);
                ParsingHelper.TryParseDouble(txtY1?.Text, out double y1);
                ParsingHelper.TryParseDouble(txtX2?.Text, out double x2);
                ParsingHelper.TryParseDouble(txtY2?.Text, out double y2);
                var ci = CultureInfo.InvariantCulture;
                double mx = (x1 + x2) / 2, my = (y1 + y2) / 2;

                // Tính toán bounds an toàn
                double maxD = System.Math.Max(System.Math.Abs(x1 - mx), System.Math.Abs(y1 - my));
                maxD = System.Math.Max(maxD, System.Math.Max(System.Math.Abs(x2 - mx), System.Math.Abs(y2 - my)));
                double margin = System.Math.Max(5, maxD * 1.5);

                string lineExpr = "";
                if (System.Math.Abs(x2 - x1) > 1e-12)
                {
                    double k = (y2 - y1) / (x2 - x1);
                    double m = y1 - k * x1;
                    lineExpr = $"calc.setExpression({{id:'line', latex:'y={k.ToString(ci)}x+{m.ToString(ci)}', color:'#9E9E9E', lineWidth:1.5, lineStyle:'DASHED'}});";
                }
                else
                {
                    lineExpr = $"calc.setExpression({{id:'line', latex:'x={x1.ToString(ci)}', color:'#9E9E9E', lineWidth:1.5, lineStyle:'DASHED'}});";
                }

                string expJs = $@"
        calc.setExpression({{id:'a', latex:'({x1.ToString(ci)},{y1.ToString(ci)})', color:'#1565C0', pointSize:12, label:'A', showLabel:true}});
        calc.setExpression({{id:'b', latex:'({x2.ToString(ci)},{y2.ToString(ci)})', color:'#2E7D32', pointSize:12, label:'B', showLabel:true}});
        calc.setExpression({{id:'m', latex:'({mx.ToString(ci)},{my.ToString(ci)})', color:'#E65100', pointSize:10, label:'M (trung điểm)', showLabel:true}});
        calc.setExpression({{id:'ab_segment', latex:'polygon(({x1.ToString(ci)},{y1.ToString(ci)}),({x2.ToString(ci)},{y2.ToString(ci)}))', color:'#1565C0', lineWidth:3}});
        {lineExpr}
        calc.setExpression({{id:'xaxis', latex:'y=0', color:'#BDBDBD', lineWidth:0.5}});
        calc.setExpression({{id:'yaxis', latex:'x=0', color:'#BDBDBD', lineWidth:0.5}});
        calc.setMathBounds({{ left: {(mx - margin).ToString(ci)}, right: {(mx + margin).ToString(ci)}, bottom: {(my - margin).ToString(ci)}, top: {(my + margin).ToString(ci)} }});";
                
                var win = new GraphWindow(expJs, $"📐 Đoạn thẳng AB và trung điểm M", $"A({UI.Fmt(x1)}; {UI.Fmt(y1)})  |  B({UI.Fmt(x2)}; {UI.Fmt(y2)})  |  M({UI.Fmt(mx)}; {UI.Fmt(my)})");
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở đồ thị: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OpenGraph_Line()
        {
            try
            {
                ParsingHelper.TryParseDouble(txtK?.Text, out double k);
                ParsingHelper.TryParseDouble(txtM?.Text, out double m);
                var ci = CultureInfo.InvariantCulture;

                double margin = System.Math.Max(10, System.Math.Abs(m) * 1.5);

                string pointJs = "";
                if (System.Math.Abs(k) > 1e-12)
                {
                    double xInt = -m / k;
                    pointJs += $"\n        calc.setExpression({{id:'ox', latex:'({xInt.ToString(ci)},0)', color:'#C62828', pointSize:10, label:'Giao Ox ({UI.Fmt(xInt)};0)', showLabel:true}});";
                }

                string expJs = $@"
        calc.setExpression({{id:'line', latex:'y={k.ToString(ci)}x+{m.ToString(ci)}', color:'#1A237E', lineWidth:3}});
        calc.setExpression({{id:'oy', latex:'(0,{m.ToString(ci)})', color:'#1565C0', pointSize:10, label:'Giao Oy (0;{m})', showLabel:true}});
        {pointJs}
        calc.setExpression({{id:'xaxis', latex:'y=0', color:'#BDBDBD', lineWidth:0.5}});
        calc.setExpression({{id:'yaxis', latex:'x=0', color:'#BDBDBD', lineWidth:0.5}});
        calc.setMathBounds({{ left: {(-margin).ToString(ci)}, right: {margin.ToString(ci)}, bottom: {(m - margin).ToString(ci)}, top: {(m + margin).ToString(ci)} }});";
                
                var win = new GraphWindow(expJs, $"📏 y = {UI.Fmt(k)}x + ({UI.Fmt(m)})");
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
                        Icon = "✈",
                        Title = isVN ? "️ Radar & Không Lưu" : "Digital Mapping & GPS",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_coordinate_1_{suffix}.png",
                        Description = isVN 
                            ? "Mỗi máy bay trên màn hình radar được định vị bằng một điểm tọa độ Oxy. Bộ điều khiển tính khoảng cách và hệ số góc đường bay để tránh va chạm." 
                            : "Locate coordinates (latitude, longitude) of any point on Earth using GPS satellite triangulation systems."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🗺",
                        Title = isVN ? "️ Bản Đồ & Trắc Địa" : "Computer-Aided Design (CAD)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_coordinate_2_{suffix}.png",
                        Description = isVN 
                            ? "Đo đạc đất đai sử dụng mốc tọa độ Oxy để vẽ ranh giới thửa đất đa giác, tính chu vi và diện tích khu đất bằng phương trình hình học tọa độ." 
                            : "Create architectural blueprints, mechanical parts, and 3D models using precise coordinate systems in CAD software."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏗",
                        Title = isVN ? "️ Thiết Kế CAD" : "Urban & Land Planning",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_coordinate_3_{suffix}.png",
                        Description = isVN 
                            ? "Các phần mềm bản vẽ CAD biểu diễn dầm, cột, tường bằng phương trình đường thẳng và cung tròn Oxy, giúp kỹ sư tính toán thi công chuẩn xác." 
                            : "Divide land parcels and map utility grids (electricity, water) based on regional coordinate systems."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📡",
                        Title = isVN ? "Định Vị Toàn Cầu GPS" : "3D Gaming & CGI Animation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_coordinate_4_{suffix}.png",
                        Description = isVN 
                            ? "GPS định vị máy thu bằng cách lập hệ phương trình các đường tròn tọa độ phát ra từ các vệ tinh, xác định giao điểm là vị trí thiết bị." 
                            : "Control character positioning, camera movement, and lighting angles using X, Y, Z coordinate matrices."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🖥",
                        Title = isVN ? "️ Tọa Độ Pixel Màn Hình" : "Air Traffic Control",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_coordinate_5_{suffix}.png",
                        Description = isVN 
                            ? "Đồ họa game hiển thị hình ảnh bằng hệ tọa độ pixel với điểm gốc (0;0) ở góc trái trên. Máy tính vẽ vector và điểm ảnh bằng tọa độ màn hình." 
                            : "Track altitude, latitude, and longitude coordinates of airplanes in real-time to prevent collisions and schedule landings."
                    },
                    new PracticalAppItem
                    {
                        Icon = "☄",
                        Title = isVN ? "️ Quỹ Đạo Parabol Vật Lý" : "Robotic Pathfinding",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_coordinate_6_{suffix}.png",
                        Description = isVN 
                            ? "Đường đi vật ném xiên hoặc quỹ đạo chuyển động thiên văn được mô phỏng bằng phương trình toán học trên tọa độ phẳng Oxy trong vật lý." 
                            : "Program autonomous robots and self-driving cars to navigate along specified coordinate paths using sensor feedback."
                    }
                ,
                    new PracticalAppItem
                    {
                        Icon = "🗺️",
                        Title = isVN ? "Bản đồ số GIS" : "GIS Digital Mapping",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_coordinate_7_{suffix}.png",
                        Description = isVN 
                            ? "Định vị tọa độ các điểm trên bề mặt Trái Đất để xây dựng hệ thống bản đồ số, đo đạc địa lý và quản lý đất đai." 
                            : "Locate points on the Earth's surface to construct digital maps, geographic surveys, and manage land parcels."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🤖",
                        Title = isVN ? "Điều khiển cánh tay robot" : "Robotic Arm Control",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_coordinate_8_{suffix}.png",
                        Description = isVN 
                            ? "Xác định tọa độ các khớp và điểm cuối trong không gian để lập trình đường đi chính xác cho robot công nghiệp." 
                            : "Determine joints and end-effector coordinates in space to program precise paths for industrial robots."
                    }};

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for CoordinateTool: {Err}", ex.Message);
            }
        }

        private void Reset_Click(object sender, MouseButtonEventArgs e)
        {
            if (_mode == Mode.TwoPoints)
            {
                txtX1.Text = "1";
                txtY1.Text = "2";
                txtX2.Text = "4";
                txtY2.Text = "6";
            }
            else
            {
                txtK.Text = "2";
                txtM.Text = "-1";
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
    }
}