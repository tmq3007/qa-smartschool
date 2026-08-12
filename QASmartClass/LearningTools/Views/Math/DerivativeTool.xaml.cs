using QASmartClass.LearningTools.Models;
using System;
using QASmartClass.LearningTools.Helpers;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class DerivativeTool : BaseToolControl
    {
        private Border tabCalc;
        private TextBlock tabCalcText;
        private Border tabGuide;
        private TextBlock tabGuideText;
        private Border tabApp;
        private TextBlock tabAppText;

        private enum Mode { Power, Polynomial, Special, Tangent }
        private Mode _mode = Mode.Power;

        // Cached selected special function index
        private int _specialIndex;

        private bool _isDarkMode = false;
        private string _solutionText = "";

        public DerivativeTool()
        {
            InitializeComponent();
            PreviewKeyDown += UserControl_PreviewKeyDown;
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextPractice != null) menuTextPractice.Text = isVN ? "Tính toán & Ví dụ" : "Calculation & Examples";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";

                if (sideMenu != null) sideMenu.SelectedIndex = 0;

                BuildModeButtons();
                BuildFormulas();
                BuildSpecialButtons();
                Calc();

                // Bàn phím số mini — đồng bộ với các tool khác
                TouchNumPad.Attach(txtCoeff, step: 1);
                TouchNumPad.Attach(txtPower, step: 1);
                TouchNumPad.Attach(txtPolyA, step: 1);
                TouchNumPad.Attach(txtPolyB, step: 1);
                TouchNumPad.Attach(txtPolyC, step: 1);
                TouchNumPad.Attach(txtPolyD, step: 1);
                TouchNumPad.Attach(txtTanA, step: 1);
                TouchNumPad.Attach(txtTanB, step: 1);
                TouchNumPad.Attach(txtTanC, step: 1);
                TouchNumPad.Attach(txtTanD, step: 1);
                TouchNumPad.Attach(txtTanX0, step: 0.5);

                LoadPracticalApps();
            };
        }

        // ═══ TAB CONTROL & PRESETS ═══
        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mainGrid == null || sideMenu == null || contentGuide == null || contentCalc == null || viewPractical == null)
                return;

            contentGuide.Visibility = Visibility.Collapsed;
            contentCalc.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

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
                viewPractical.Visibility = Visibility.Visible;
            }
        }

        private void Tab_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border clickedBorder && clickedBorder.Tag is string tag)
            {
                SwitchTab(tag);
            }
        }

        private void SwitchTab(string tag)
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
                        Icon = "🚀",
                        Title = isVN ? "Chuyển Động & Gia Tốc" : "Physics & Kinematics",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_derivative_1_{suffix}.png",
                        Description = isVN 
                            ? "Đạo hàm bậc nhất của quãng đường theo thời gian s(t) là vận tốc v(t) = s'(t), và đạo hàm bậc hai là gia tốc a(t) = v'(t) = s''(t)." 
                            : "Calculate velocity as the first derivative of position, and acceleration as the second derivative of position over time."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📊",
                        Title = isVN ? "Tối Ưu Hóa Doanh Thu" : "Optimization in Business",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_derivative_2_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng đạo hàm để tìm điểm cực đại của hàm doanh thu hoặc lợi nhuận (khi doanh thu cận biên bằng chi phí cận biên), là công cụ then chốt của các nhà kinh tế." 
                            : "Find maximum profit and minimum cost levels by locating the points where marginal cost and marginal revenue derivatives equal zero."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧠",
                        Title = isVN ? "Trí Tuệ Nhân Tạo (AI)" : "Machine Learning (Gradient Descent)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_derivative_3_{suffix}.png",
                        Description = isVN 
                            ? "Thuật toán Gradient Descent tối ưu hóa các tham số trọng số trong mạng neural nhân tạo bằng cách đi theo chiều ngược lại của đạo hàm riêng (gradient) hàm mất mát." 
                            : "Compute derivatives of loss functions to adjust neural network weights and minimize model prediction errors."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧪",
                        Title = isVN ? "Tốc Độ Phản Ứng Hóa Học" : "Chemistry Reaction Rates",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_derivative_4_{suffix}.png",
                        Description = isVN 
                            ? "Tốc độ tức thời của một phản ứng hóa học chính là đạo hàm của nồng độ chất tham gia hoặc sản phẩm theo thời gian d[A]/dt, hỗ trợ kiểm soát phản ứng trong công nghiệp." 
                            : "Determine instantaneous chemical reaction rates by taking the derivative of reactant concentration over time."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌉",
                        Title = isVN ? "Thiết Kế Độ Dốc Cầu Đường" : "Highway Gradient & Curvature Design",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_derivative_5_{suffix}.png",
                        Description = isVN 
                            ? "Kỹ sư sử dụng đạo hàm bậc hai để thiết kế đường cong chuyển tiếp mượt mà cho đường quốc lộ và đường sắt, tránh việc thay đổi lực ly tâm đột ngột gây tai nạn." 
                            : "Engineers use second derivatives to design smooth transition curves for highways and railways, avoiding sudden changes in centrifugal force."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🩺",
                        Title = isVN ? "Đo Tốc Độ Lan Truyền Dịch" : "Epidemiology Disease Modeling",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_derivative_6_{suffix}.png",
                        Description = isVN 
                            ? "Đạo hàm số ca nhiễm theo thời gian mô tả tốc độ lây lan tức thời của một dịch bệnh, là cơ sở để các nhà dịch tễ học dự báo đỉnh dịch và lên phương án cách ly." 
                            : "Track the growth and acceleration rate of viral outbreaks (derivatives of active cases) to plan public health responses and resource needs."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📊",
                        Title = isVN ? "Tối ưu hóa chi phí sản xuất" : "Production Cost Optimization",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_derivative_7_{suffix}.png",
                        Description = isVN 
                            ? "Tính đạo hàm của hàm chi phí biên để tìm quy mô sản xuất tối ưu giúp doanh nghiệp giảm thiểu hao phí vận hành." 
                            : "Differentiate marginal cost functions to find the optimal production scale that minimizes business expenses."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⏱️",
                        Title = isVN ? "Vận tốc rơi tự do tức thời" : "Instantaneous Fall Velocity",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_derivative_8_{suffix}.png",
                        Description = isVN 
                            ? "Đạo hàm quãng đường theo thời gian để xác định vận tốc và gia tốc tức thời của vật thể tại thời điểm bất kỳ." 
                            : "Take the derivative of position over time to determine the instantaneous velocity and acceleration of a falling object."
                    }};

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for DerivativeTool: {Err}", ex.Message);
            }
        }

        private void PresetExample_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                switch (tag)
                {
                    case "power":
                        _mode = Mode.Power;
                        if (txtCoeff != null) txtCoeff.Text = "3";
                        if (txtPower != null) txtPower.Text = "2";
                        break;
                    case "poly":
                        _mode = Mode.Polynomial;
                        if (txtPolyA != null) txtPolyA.Text = "1";
                        if (txtPolyB != null) txtPolyB.Text = "-3";
                        if (txtPolyC != null) txtPolyC.Text = "0";
                        if (txtPolyD != null) txtPolyD.Text = "2";
                        break;
                    case "spec_sin":
                        _mode = Mode.Special;
                        _specialIndex = 0; // sin(x)
                        BuildSpecialButtons();
                        break;
                    case "spec_exp":
                        _mode = Mode.Special;
                        _specialIndex = 4; // e^x
                        BuildSpecialButtons();
                        break;
                    case "tangent":
                        _mode = Mode.Tangent;
                        if (txtTanA != null) txtTanA.Text = "1";
                        if (txtTanB != null) txtTanB.Text = "0";
                        if (txtTanC != null) txtTanC.Text = "-3";
                        if (txtTanD != null) txtTanD.Text = "0";
                        if (txtTanX0 != null) txtTanX0.Text = "1";
                        break;
                }
                BuildModeButtons();
                UpdateUI();
                Calc();
            }
        }

        // ═══ MODE SELECTOR ═══
        private void BuildModeButtons()
        {
            modePanel.Children.Clear();
            var modes = new (string Label, Mode M)[]
            {
                ("🔢 Đơn thức axⁿ", Mode.Power),
                ("📊 Đa thức bậc 3", Mode.Polynomial),
                ("📐 Hàm đặc biệt", Mode.Special),
                ("📏 Tiếp tuyến", Mode.Tangent),
            };

            foreach (var (label, m) in modes)
            {
                bool active = m == _mode;
                Color bg, fg, borderBrush;
                if (_isDarkMode)
                {
                    bg = active ? Color.FromRgb(144, 202, 249) : Color.FromRgb(45, 45, 45);
                    fg = active ? Color.FromRgb(18, 18, 18) : Color.FromRgb(224, 224, 224);
                    borderBrush = active ? Colors.Transparent : Color.FromRgb(80, 80, 80);
                }
                else
                {
                    bg = active ? Color.FromRgb(21, 101, 192) : Color.FromArgb(20, 21, 101, 192);
                    fg = active ? Colors.White : Color.FromRgb(21, 101, 192);
                    borderBrush = active ? Colors.Transparent : Color.FromRgb(21, 101, 192);
                }

                var btn = new Border
                {
                    Background = new SolidColorBrush(bg),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16, 12, 16, 12),
                    Margin = new Thickness(0, 0, 8, 6),
                    Cursor = Cursors.Hand,
                    BorderBrush = new SolidColorBrush(borderBrush),
                    BorderThickness = new Thickness(active ? 0 : 1)
                };
                btn.Child = new TextBlock
                {
                    Text = label, FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(fg),
                    FontFamily = new FontFamily("Segoe UI")
                };
                var capturedMode = m;
                btn.MouseLeftButtonDown += (_, _) =>
                {
                    _mode = capturedMode;
                    BuildModeButtons();
                    UpdateUI();
                    Calc();
                };
                modePanel.Children.Add(btn);
            }
        }

        private void UpdateUI()
        {
            powerInput.Visibility = _mode == Mode.Power ? Visibility.Visible : Visibility.Collapsed;
            polyInput.Visibility = _mode == Mode.Polynomial ? Visibility.Visible : Visibility.Collapsed;
            specialInput.Visibility = _mode == Mode.Special ? Visibility.Visible : Visibility.Collapsed;
            tangentInput.Visibility = _mode == Mode.Tangent ? Visibility.Visible : Visibility.Collapsed;

            txtModeTitle.Text = _mode switch
            {
                Mode.Power => "📐 Đạo hàm đơn thức — (axⁿ)' = n·a·xⁿ⁻¹",
                Mode.Polynomial => "📐 Đạo hàm đa thức — f'(x), f''(x)",
                Mode.Special => "📐 Đạo hàm hàm đặc biệt",
                Mode.Tangent => "📏 Tiếp tuyến tại x₀ — y = f'(x₀)(x - x₀) + f(x₀)",
                _ => ""
            };
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
                case Mode.Power: CalcPower(); break;
                case Mode.Polynomial: CalcPoly(); break;
                case Mode.Special: CalcSpecial(); break;
                case Mode.Tangent: CalcTangent(); break;
            }
        }

        // — Power rule: (axⁿ)' = n·a·xⁿ⁻¹ —
        private void CalcPower()
        {
            variationPanel.Visibility = Visibility.Collapsed;
            if (string.IsNullOrWhiteSpace(txtCoeff?.Text) || string.IsNullOrWhiteSpace(txtPower?.Text))
            {
                UI.ResultRow("⚠️ Vui lòng nhập hệ số (a) và số mũ (n).", DS.ResultDanger, resultPanel);
                return;
            }
            if (!ParsingHelper.TryParseDouble(txtCoeff?.Text, out double a) ||
                !ParsingHelper.TryParseDouble(txtPower?.Text, out double n))
            {
                UI.ResultRow("⚠️ Vui lòng nhập hệ số (a) và số mũ (n) hợp lệ (dạng số).", DS.ResultDanger, resultPanel);
                return;
            }

            double newCoeff = n * a;
            double newPow = n - 1;

            UI.ResultRow($"f(x) = {FormatTerm(a, n, "x")}", "#1565C0", resultPanel);
            UI.ResultRow($"f'(x) = {(n == 0 ? "0" : FormatTerm(newCoeff, newPow, "x"))}", "#1565C0", resultPanel);

            if (n != 0 && n != 1)
            {
                double c2 = n * (n - 1) * a;
                double p2 = n - 2;
                UI.ResultRow($"f''(x) = {FormatTerm(c2, p2, "x")}", "#1565C0", resultPanel);
            }
            else
            {
                UI.ResultRow("f''(x) = 0", "#1565C0", resultPanel);
            }

            UI.ResultRow($"📌 Quy tắc: (axⁿ)' = n·a·xⁿ⁻¹  →  ({UI.Fmt(a)}x^{UI.Fmt(n)})' = {UI.Fmt(n)}·{UI.Fmt(a)}·x^{UI.Fmt(n - 1)}", "#E65100", resultPanel);

            RenderPowerVariation(a, n);
            _solutionText = GeneratePowerSolution(a, n);
            if (txtSolution != null) txtSolution.Text = _solutionText;
        }

        // — Polynomial: ax³ + bx² + cx + d —
        private void CalcPoly()
        {
            variationPanel.Visibility = Visibility.Collapsed;
            if (string.IsNullOrWhiteSpace(txtPolyA?.Text) || string.IsNullOrWhiteSpace(txtPolyB?.Text) ||
                string.IsNullOrWhiteSpace(txtPolyC?.Text) || string.IsNullOrWhiteSpace(txtPolyD?.Text))
            {
                UI.ResultRow("⚠️ Vui lòng nhập đầy đủ các hệ số a, b, c, d.", DS.ResultDanger, resultPanel);
                return;
            }
            if (!ParsingHelper.TryParseDouble(txtPolyA?.Text, out double a) ||
                !ParsingHelper.TryParseDouble(txtPolyB?.Text, out double b) ||
                !ParsingHelper.TryParseDouble(txtPolyC?.Text, out double c) ||
                !ParsingHelper.TryParseDouble(txtPolyD?.Text, out double _d))
            {
                UI.ResultRow("⚠️ Vui lòng nhập các hệ số đa thức hợp lệ (dạng số).", DS.ResultDanger, resultPanel);
                return;
            }

            // f(x)
            string fx = FormatPoly(a, b, c, _d);
            UI.ResultRow($"f(x) = {fx}", "#1565C0", resultPanel);

            // f'(x) = 3ax² + 2bx + c
            string fpx = FormatPoly(0, 3 * a, 2 * b, c);
            UI.ResultRow($"f'(x) = {fpx}", "#1565C0", resultPanel);

            // f''(x) = 6ax + 2b
            string fppx = FormatPoly(0, 0, 6 * a, 2 * b);
            UI.ResultRow($"f''(x) = {fppx}", "#1565C0", resultPanel);

            // Critical points: f'(x) = 0 → 3ax² + 2bx + c = 0
            double A = 3 * a, B = 2 * b, C = c;
            if (System.Math.Abs(A) > 1e-12)
            {
                // Cubic function: always has an inflection point
                double xI = -b / (3 * a);
                double yI = a * xI * xI * xI + b * xI * xI + c * xI + _d;
                UI.ResultRow($"🎯 Điểm uốn: I({UI.Fmt(xI)}; {UI.Fmt(yI)})", "#7B1FA2", resultPanel);

                double delta = B * B - 4 * A * C;
                if (delta > 0)
                {
                    double r1 = (-B - System.Math.Sqrt(delta)) / (2 * A);
                    double r2 = (-B + System.Math.Sqrt(delta)) / (2 * A);
                    double x1 = System.Math.Min(r1, r2);
                    double x2 = System.Math.Max(r1, r2);
                    double y1 = a * x1 * x1 * x1 + b * x1 * x1 + c * x1 + _d;
                    double y2 = a * x2 * x2 * x2 + b * x2 * x2 + c * x2 + _d;
                    double fpp1 = 6 * a * x1 + 2 * b;
                    double fpp2 = 6 * a * x2 + 2 * b;

                    string type1 = fpp1 > 0 ? "Cực tiểu" : "Cực đại";
                    string type2 = fpp2 > 0 ? "Cực tiểu" : "Cực đại";

                    UI.ResultRow($"✨ Cực trị: x₁ = {UI.Fmt(x1)} → f(x₁) = {UI.Fmt(y1)}  ({type1})", "#1565C0", resultPanel);
                    UI.ResultRow($"x₂ = {UI.Fmt(x2)} → f(x₂) = {UI.Fmt(y2)}  ({type2})", "#1565C0", resultPanel);
                }
                else if (System.Math.Abs(delta) < 1e-12)
                {
                    UI.ResultRow("✨ Cực trị: Δ' = 0 → Không có cực trị (điểm uốn trùng điểm dừng)", "#1565C0", resultPanel);
                }
                else
                {
                    UI.ResultRow("✨ Cực trị: Δ' < 0 → Không có cực trị (hàm số đơn điệu)", "#1565C0", resultPanel);
                }
            }
            else if (System.Math.Abs(b) > 1e-12)
            {
                // Quadratic function: vertex is the extremum
                double x0 = -c / (2 * b);
                double y0 = b * x0 * x0 + c * x0 + _d;
                string type = b > 0 ? "Cực tiểu" : "Cực đại";
                UI.ResultRow($"✨ Cực trị (đỉnh Parabol): x₀ = {UI.Fmt(x0)} → f(x₀) = {UI.Fmt(y0)}  ({type})", "#1565C0", resultPanel);
            }
            else
            {
                UI.ResultRow("✨ Cực trị: Hàm số bậc nhất hoặc hằng số, không có cực trị.", "#1565C0", resultPanel);
            }

            RenderPolyVariation(a, b, c, _d);
            _solutionText = GeneratePolySolution(a, b, c, _d);
            if (txtSolution != null) txtSolution.Text = _solutionText;
        }

        // — Special functions —
        private readonly (string Name, string Fx, string Fpx)[] _specials = new[]
        {
            ("sin(x)",   "sin(x)",   "cos(x)"),
            ("cos(x)",   "cos(x)",   "-sin(x)"),
            ("tan(x)",   "tan(x)",   "1/cos²(x)"),
            ("cot(x)",   "cot(x)",   "-1/sin²(x)"),
            ("eˣ",       "eˣ",       "eˣ"),
            ("ln(x)",    "ln(x)",    "1/x"),
            ("√x",       "√x = x^(1/2)",  "(1/2)·x^(-1/2) = 1/(2√x)"),
            ("1/x",      "x⁻¹",     "-x⁻² = -1/x²"),
            ("aˣ",       "aˣ  (a>0)", "aˣ · ln(a)"),
            ("logₐ(x)",  "logₐ(x)", "1 / (x · ln(a))"),
            ("sin²(x)",  "sin²(x)", "2·sin(x)·cos(x) = sin(2x)"),
            ("cos²(x)",  "cos²(x)", "-2·sin(x)·cos(x) = -sin(2x)"),
        };

        private void BuildSpecialButtons()
        {
            specialFuncPanel.Children.Clear();
            for (int i = 0; i < _specials.Length; i++)
            {
                var (name, _, _) = _specials[i];
                bool active = i == _specialIndex;
                Color bg, fg, borderBrush;
                if (_isDarkMode)
                {
                    bg = active ? Color.FromRgb(144, 202, 249) : Color.FromRgb(45, 45, 45);
                    fg = active ? Color.FromRgb(18, 18, 18) : Color.FromRgb(224, 224, 224);
                    borderBrush = active ? Colors.Transparent : Color.FromRgb(80, 80, 80);
                }
                else
                {
                    bg = active ? Color.FromRgb(21, 101, 192) : Color.FromArgb(20, 21, 101, 192);
                    fg = active ? Colors.White : Color.FromRgb(21, 101, 192);
                    borderBrush = active ? Colors.Transparent : Color.FromRgb(21, 101, 192);
                }

                var btn = new Border
                {
                    Background = new SolidColorBrush(bg),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16, 12, 16, 12),
                    Margin = new Thickness(0, 0, 6, 6),
                    Cursor = Cursors.Hand,
                    BorderBrush = new SolidColorBrush(borderBrush),
                    BorderThickness = new Thickness(active ? 0 : 1)
                };
                btn.Child = new TextBlock
                {
                    Text = name, FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(fg),
                    FontFamily = new FontFamily("Segoe UI")
                };
                int idx = i;
                btn.MouseLeftButtonDown += (_, _) =>
                {
                    _specialIndex = idx;
                    BuildSpecialButtons();
                    Calc();
                };
                specialFuncPanel.Children.Add(btn);
            }
        }

        private void CalcSpecial()
        {
            if (variationContent == null) return;
            variationContent.Children.Clear();

            if (_specialIndex < 0 || _specialIndex >= _specials.Length) return;
            var (name, fx, fpx) = _specials[_specialIndex];
            UI.ResultRow($"f(x) = {fx}", "#1565C0", resultPanel);
            UI.ResultRow($"f'(x) = {fpx}", "#1565C0", resultPanel);

            // Hiển thị gợi ý quy tắc đạo hàm hàm hợp
            UI.ResultRow($"📌 Hợp (Đạo hàm hàm hợp): Nếu f(g(x)) → [f(g(x))]' = f'(g(x)) · g'(x)", "#E65100", resultPanel);

            // Bổ sung Bảng biến thiên trực quan cho hàm đặc biệt
            if (_specialIndex == 4) // eˣ
            {
                variationPanel.Visibility = Visibility.Visible;
                AddVariationRow("(-∞; +∞)", "↗ Đồng biến", DS.ResultSuccess);
            }
            else if (_specialIndex == 5) // ln(x)
            {
                variationPanel.Visibility = Visibility.Visible;
                AddVariationRow("(0; +∞)", "↗ Đồng biến (Tập xác định x > 0)", DS.ResultSuccess);
            }
            else if (_specialIndex == 6) // √x
            {
                variationPanel.Visibility = Visibility.Visible;
                AddVariationRow("[0; +∞)", "↗ Đồng biến (Tập xác định x ≥ 0)", DS.ResultSuccess);
            }
            else if (_specialIndex == 7) // 1/x
            {
                variationPanel.Visibility = Visibility.Visible;
                AddVariationRow("(-∞; 0)", "↘ Nghịch biến", DS.ResultDanger);
                AddVariationRow("(0; +∞)", "↘ Nghịch biến", DS.ResultDanger);
            }
            else if (_specialIndex == 8) // aˣ
            {
                variationPanel.Visibility = Visibility.Visible;
                AddVariationRow("(-∞; +∞)", "↗ Đồng biến (với cơ số a > 1)", DS.ResultSuccess);
            }
            else if (_specialIndex == 9) // logₐ(x)
            {
                variationPanel.Visibility = Visibility.Visible;
                AddVariationRow("(0; +∞)", "↗ Đồng biến (với cơ số a > 1, x > 0)", DS.ResultSuccess);
            }
            else
            {
                // Các hàm lượng giác sin, cos, tan, cot tuần hoàn phức tạp sẽ ẩn bảng biến thiên
                variationPanel.Visibility = Visibility.Collapsed;
            }

            _solutionText = GenerateSpecialSolution(name, fx, fpx);
            if (txtSolution != null) txtSolution.Text = _solutionText;
        }

        // — Tangent line —
        private void CalcTangent()
        {
            variationPanel.Visibility = Visibility.Collapsed;
            if (string.IsNullOrWhiteSpace(txtTanA?.Text) || string.IsNullOrWhiteSpace(txtTanB?.Text) ||
                string.IsNullOrWhiteSpace(txtTanC?.Text) || string.IsNullOrWhiteSpace(txtTanD?.Text) ||
                string.IsNullOrWhiteSpace(txtTanX0?.Text))
            {
                UI.ResultRow("⚠️ Vui lòng nhập đầy đủ hệ số a, b, c, d và x₀.", DS.ResultDanger, resultPanel);
                return;
            }
            if (!ParsingHelper.TryParseDouble(txtTanA?.Text, out double a) ||
                !ParsingHelper.TryParseDouble(txtTanB?.Text, out double b) ||
                !ParsingHelper.TryParseDouble(txtTanC?.Text, out double c) ||
                !ParsingHelper.TryParseDouble(txtTanD?.Text, out double d) ||
                !ParsingHelper.TryParseDouble(txtTanX0?.Text, out double x0))
            {
                UI.ResultRow("⚠️ Vui lòng nhập các hệ số tiếp tuyến hợp lệ (dạng số).", DS.ResultDanger, resultPanel);
                return;
            }

            // f(x0)
            double fx0 = a * x0 * x0 * x0 + b * x0 * x0 + c * x0 + d;

            // f'(x0) = 3ax0² + 2bx0 + c
            double fpx0 = 3 * a * x0 * x0 + 2 * b * x0 + c;

            // Tangent: y = f'(x0)(x - x0) + f(x0) = f'(x0)·x + [f(x0) - f'(x0)·x0]
            double tanSlope = fpx0;
            double tanIntercept = fx0 - fpx0 * x0;

            UI.ResultRow($"f(x) = {FormatPoly(a, b, c, d)}", "#1565C0", resultPanel);
            UI.ResultRow($"f'(x) = {FormatPoly(0, 3 * a, 2 * b, c)}", "#1565C0", resultPanel);
            UI.ResultRow($"f({UI.Fmt(x0)}) = {UI.Fmt(fx0)}", "#1565C0", resultPanel);
            UI.ResultRow($"f'({UI.Fmt(x0)}) = {UI.Fmt(fpx0)}  (hệ số góc tiếp tuyến)", "#1565C0", resultPanel);
            UI.ResultRow($"📏 Tiếp tuyến: {FormatLineEquation(tanSlope, tanIntercept)}", "#1565C0", resultPanel);

            _solutionText = GenerateTangentSolution(a, b, c, d, x0, fx0, fpx0, tanSlope, tanIntercept);
            if (txtSolution != null) txtSolution.Text = _solutionText;
        }

        // ═══ FORMULA REFERENCE ═══
        private void BuildFormulas()
        {
            formulaPanel.Children.Clear();

            var sections = new (string Title, string[] Items)[]
            {
                ("Quy tắc cơ bản", new[]
                {
                    "(C)' = 0  (hằng số)",
                    "(xⁿ)' = n·xⁿ⁻¹",
                    "(u ± v)' = u' ± v'",
                    "(k·u)' = k·u'",
                }),
                ("Quy tắc tích — thương", new[]
                {
                    "(u·v)' = u'·v + u·v'",
                    "(u/v)' = (u'·v − u·v') / v²",
                }),
                ("Quy tắc đạo hàm hàm hợp", new[]
                {
                    "[f(g(x))]' = f'(g(x)) · g'(x)",
                    "VD: (sin(2x))' = cos(2x) · 2 = 2cos(2x)",
                }),
                ("Lượng giác", new[]
                {
                    "(sin x)' = cos x",
                    "(cos x)' = -sin x",
                    "(tan x)' = 1/cos²x = 1 + tan²x",
                    "(cot x)' = -1/sin²x",
                }),
                ("Mũ — Lôgarit", new[]
                {
                    "(eˣ)' = eˣ",
                    "(aˣ)' = aˣ · ln(a)",
                    "(ln x)' = 1/x",
                    "(logₐ x)' = 1/(x·ln a)",
                }),
                ("Ứng dụng đạo hàm", new[]
                {
                    "Tìm cực trị: f'(x) = 0, xét dấu f'",
                    "Tiếp tuyến: y = f'(x₀)(x − x₀) + f(x₀)",
                    "Tốc độ biến thiên: v(t) = s'(t)",
                    "Gia tốc: a(t) = v'(t) = s''(t)",
                }),
            };

            Color bg, titleFg, itemFg;
            if (_isDarkMode)
            {
                bg = Color.FromRgb(45, 45, 45);
                titleFg = Color.FromRgb(144, 202, 249);
                itemFg = Color.FromRgb(220, 220, 220);
            }
            else
            {
                bg = Color.FromArgb(15, 21, 101, 192);
                titleFg = Color.FromRgb(21, 101, 192);
                itemFg = Color.FromRgb(33, 33, 33);
            }

            foreach (var (title, items) in sections)
            {
                var section = new Border
                {
                    Background = new SolidColorBrush(bg),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 8)
                };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = title, FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(titleFg),
                    Margin = new Thickness(0, 0, 0, 4)
                });
                foreach (var item in items)
                    sp.Children.Add(new TextBlock
                    {
                        Text = $"  {item}", FontSize = 14,
                        FontFamily = new FontFamily("Segoe UI"),
                        Foreground = new SolidColorBrush(itemFg),
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
                string expJs;
                string title;
                var ci = CultureInfo.InvariantCulture;

                if (_mode == Mode.Tangent)
                {
                    ParsingHelper.TryParseDouble(txtTanA?.Text, out double a);
                    ParsingHelper.TryParseDouble(txtTanB?.Text, out double b);
                    ParsingHelper.TryParseDouble(txtTanC?.Text, out double c);
                    ParsingHelper.TryParseDouble(txtTanD?.Text, out double d);
                    ParsingHelper.TryParseDouble(txtTanX0?.Text, out double x0);
                    double fx0 = a * x0 * x0 * x0 + b * x0 * x0 + c * x0 + d;
                    double fpx0 = 3 * a * x0 * x0 + 2 * b * x0 + c;

                    string aS = a.ToString(ci), bS = b.ToString(ci), cS = c.ToString(ci), dS = d.ToString(ci);
                    string x0S = x0.ToString(ci), fx0S = fx0.ToString(ci), fpS = fpx0.ToString(ci);

                    double left = x0 - 6;
                    double right = x0 + 6;
                    double bottom = fx0 - 8;
                    double top = fx0 + 8;

                    string fxLatex = "f(x)=" + FormatPolyLaTeX(a, b, c, d);
                    string fpxLatex = "g(x)=" + FormatPolyLaTeX(0, 3 * a, 2 * b, c);

                    expJs = $@"
        calc.setExpression({{id:'fx', latex:'{fxLatex}', color:'#1565C0', lineWidth:3}});
        calc.setExpression({{id:'dfx', latex:'{fpxLatex}', color:'#C62828', lineWidth:2, lineStyle:'DASHED'}});
        calc.setExpression({{id:'tan', latex:'y={fpS}*(x-{x0S})+{fx0S}', color:'#2E7D32', lineWidth:2.5}});
        calc.setExpression({{id:'pt', latex:'({x0S},{fx0S})', color:'#E65100', pointSize:12, label:'T({x0S},{fx0S})', showLabel:true}});
        calc.setMathBounds({{ left: {left.ToString(ci)}, right: {right.ToString(ci)}, bottom: {bottom.ToString(ci)}, top: {top.ToString(ci)} }});";
                    title = "📈 f(x), f'(x), Tiếp tuyến";
                }
                else if (_mode == Mode.Polynomial)
                {
                    ParsingHelper.TryParseDouble(txtPolyA?.Text, out double a);
                    ParsingHelper.TryParseDouble(txtPolyB?.Text, out double b);
                    ParsingHelper.TryParseDouble(txtPolyC?.Text, out double c);
                    ParsingHelper.TryParseDouble(txtPolyD?.Text, out double d);
                    string aS = a.ToString(ci), bS = b.ToString(ci), cS = c.ToString(ci), dS = d.ToString(ci);

                    double centerX = 0;
                    double centerY = 0;
                    if (System.Math.Abs(a) > 1e-12)
                    {
                        centerX = -b / (3 * a);
                        centerY = a * centerX * centerX * centerX + b * centerX * centerX + c * centerX + d;
                    }
                    else if (System.Math.Abs(b) > 1e-12)
                    {
                        centerX = -c / (2 * b);
                        centerY = b * centerX * centerX + c * centerX + d;
                    }
                    double left = centerX - 6;
                    double right = centerX + 6;
                    double bottom = centerY - 10;
                    double top = centerY + 10;

                    string fxLatex = "f(x)=" + FormatPolyLaTeX(a, b, c, d);
                    string fpxLatex = "g(x)=" + FormatPolyLaTeX(0, 3 * a, 2 * b, c);
                    string fppxLatex = "h(x)=" + FormatPolyLaTeX(0, 0, 6 * a, 2 * b);

                    expJs = $@"
        calc.setExpression({{id:'fx', latex:'{fxLatex}', color:'#1565C0', lineWidth:3}});
        calc.setExpression({{id:'dfx', latex:'{fpxLatex}', color:'#C62828', lineWidth:2.5, lineStyle:'DASHED'}});
        calc.setExpression({{id:'ddfx', latex:'{fppxLatex}', color:'#7B1FA2', lineWidth:1.5, lineStyle:'DOTTED'}});
        calc.setMathBounds({{ left: {left.ToString(ci)}, right: {right.ToString(ci)}, bottom: {bottom.ToString(ci)}, top: {top.ToString(ci)} }});";
                    title = "📈 f(x), f'(x), f''(x)";
                }
                else if (_mode == Mode.Special)
                {
                    switch (_specialIndex)
                    {
                        case 0: // sin(x)
                            expJs = @"
        calc.setExpression({id:'fx', latex:'f(x)=\\sin(x)', color:'#1565C0', lineWidth:3});
        calc.setExpression({id:'dfx', latex:'g(x)=\\cos(x)', color:'#C62828', lineWidth:2.5, lineStyle:'DASHED'});
        calc.setMathBounds({ left: -7, right: 7, bottom: -4, top: 4 });";
                            title = "📈 f(x) = sin(x) và f'(x) = cos(x)";
                            break;
                        case 1: // cos(x)
                            expJs = @"
        calc.setExpression({id:'fx', latex:'f(x)=\\cos(x)', color:'#1565C0', lineWidth:3});
        calc.setExpression({id:'dfx', latex:'g(x)=-\\sin(x)', color:'#C62828', lineWidth:2.5, lineStyle:'DASHED'});
        calc.setMathBounds({ left: -7, right: 7, bottom: -4, top: 4 });";
                            title = "📈 f(x) = cos(x) và f'(x) = -sin(x)";
                            break;
                        case 2: // tan(x)
                            expJs = @"
        calc.setExpression({id:'fx', latex:'f(x)=\\tan(x)', color:'#1565C0', lineWidth:3});
        calc.setExpression({id:'dfx', latex:'g(x)=1/(\\cos(x))^2', color:'#C62828', lineWidth:2.5, lineStyle:'DASHED'});
        calc.setMathBounds({ left: -4, right: 4, bottom: -6, top: 6 });";
                            title = "📈 f(x) = tan(x) và f'(x) = 1/cos²(x)";
                            break;
                        case 3: // cot(x)
                            expJs = @"
        calc.setExpression({id:'fx', latex:'f(x)=\\cot(x)', color:'#1565C0', lineWidth:3});
        calc.setExpression({id:'dfx', latex:'g(x)=-1/(\\sin(x))^2', color:'#C62828', lineWidth:2.5, lineStyle:'DASHED'});
        calc.setMathBounds({ left: -4, right: 4, bottom: -6, top: 6 });";
                            title = "📈 f(x) = cot(x) và f'(x) = -1/sin²(x)";
                            break;
                        case 4: // e^x
                            expJs = @"
        calc.setExpression({id:'fx', latex:'f(x)=e^x', color:'#1565C0', lineWidth:3});
        calc.setExpression({id:'dfx', latex:'g(x)=e^x', color:'#C62828', lineWidth:2.5, lineStyle:'DASHED'});
        calc.setMathBounds({ left: -5, right: 5, bottom: -2, top: 10 });";
                            title = "📈 f(x) = eˣ và f'(x) = eˣ";
                            break;
                        case 5: // ln(x)
                            expJs = @"
        calc.setExpression({id:'fx', latex:'f(x)=\\ln(x)', color:'#1565C0', lineWidth:3});
        calc.setExpression({id:'dfx', latex:'g(x)=1/x', color:'#C62828', lineWidth:2.5, lineStyle:'DASHED'});
        calc.setMathBounds({ left: -1, right: 8, bottom: -4, top: 4 });";
                            title = "📈 f(x) = ln(x) và f'(x) = 1/x";
                            break;
                        case 6: // sqrt(x)
                            expJs = @"
        calc.setExpression({id:'fx', latex:'f(x)=\\sqrt{x}', color:'#1565C0', lineWidth:3});
        calc.setExpression({id:'dfx', latex:'g(x)=1/(2*\\sqrt{x})', color:'#C62828', lineWidth:2.5, lineStyle:'DASHED'});
        calc.setMathBounds({ left: -1, right: 8, bottom: -2, top: 6 });";
                            title = "📈 f(x) = √x và f'(x) = 1/(2√x)";
                            break;
                        case 7: // 1/x
                            expJs = @"
        calc.setExpression({id:'fx', latex:'f(x)=1/x', color:'#1565C0', lineWidth:3});
        calc.setExpression({id:'dfx', latex:'g(x)=-1/x^2', color:'#C62828', lineWidth:2.5, lineStyle:'DASHED'});
        calc.setMathBounds({ left: -6, right: 6, bottom: -6, top: 6 });";
                            title = "📈 f(x) = 1/x và f'(x) = -1/x²";
                            break;
                        case 8: // a^x (a=2)
                            expJs = @"
        calc.setExpression({id:'fx', latex:'f(x)=2^x', color:'#1565C0', lineWidth:3});
        calc.setExpression({id:'dfx', latex:'g(x)=2^x*\\ln(2)', color:'#C62828', lineWidth:2.5, lineStyle:'DASHED'});
        calc.setMathBounds({ left: -5, right: 5, bottom: -2, top: 10 });";
                            title = "📈 f(x) = 2ˣ và f'(x) = 2ˣ·ln(2)";
                            break;
                        case 9: // logₐ(x) (a=2)
                            expJs = @"
        calc.setExpression({id:'fx', latex:'f(x)=\\log_{2}(x)', color:'#1565C0', lineWidth:3});
        calc.setExpression({id:'dfx', latex:'g(x)=1/(x*\\ln(2))', color:'#C62828', lineWidth:2.5, lineStyle:'DASHED'});
        calc.setMathBounds({ left: -1, right: 8, bottom: -4, top: 4 });";
                            title = "📈 f(x) = log₂(x) và f'(x) = 1/(x·ln(2))";
                            break;
                        case 10: // sin²(x)
                            expJs = @"
        calc.setExpression({id:'fx', latex:'f(x)=(\\sin(x))^2', color:'#1565C0', lineWidth:3});
        calc.setExpression({id:'dfx', latex:'g(x)=\\sin(2*x)', color:'#C62828', lineWidth:2.5, lineStyle:'DASHED'});
        calc.setMathBounds({ left: -7, right: 7, bottom: -2, top: 3 });";
                            title = "📈 f(x) = sin²(x) và f'(x) = sin(2x)";
                            break;
                        case 11: // cos²(x)
                            expJs = @"
        calc.setExpression({id:'fx', latex:'f(x)=(\\cos(x))^2', color:'#1565C0', lineWidth:3});
        calc.setExpression({id:'dfx', latex:'g(x)=-\\sin(2*x)', color:'#C62828', lineWidth:2.5, lineStyle:'DASHED'});
        calc.setMathBounds({ left: -7, right: 7, bottom: -2, top: 3 });";
                            title = "📈 f(x) = cos²(x) và f'(x) = -sin(2x)";
                            break;
                        default:
                            return;
                    }
                }
                else
                {
                    ParsingHelper.TryParseDouble(txtCoeff?.Text, out double a);
                    ParsingHelper.TryParseDouble(txtPower?.Text, out double n);
                    string aS = a.ToString(ci), nS = n.ToString(ci);
                    double newC = n * a, newP = n - 1;
                    string ncS = newC.ToString(ci), npS = newP.ToString(ci);

                    expJs = $@"
        calc.setExpression({{id:'fx', latex:'f(x)={aS}x^{{{nS}}}', color:'#1565C0', lineWidth:3}});
        calc.setExpression({{id:'dfx', latex:'g(x)={ncS}x^{{{npS}}}', color:'#C62828', lineWidth:2.5, lineStyle:'DASHED'}});
        calc.setMathBounds({{ left: -5, right: 5, bottom: -10, top: 10 }});";
                    title = $"📈 f(x) = {a}x^{n} và f'(x)";
                }

                string invJs = _isDarkMode 
                    ? "calc.controller.dispatch({ type: 'set-graphs-inverted', inverted: true });" 
                    : "calc.controller.dispatch({ type: 'set-graphs-inverted', inverted: false });";
                expJs = expJs + "\n" + invJs;

                var win = new GraphWindow(expJs, title);
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở đồ thị: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ═══ HELPERS ═══
        /* AddResult / AddR replaced by UI.ResultRow */

        private static string Fmt(double v) =>
            v == (int)v ? $"{(int)v}" : $"{v:G6}";

        private static string GetSuperscript(double power)
        {
            if (System.Math.Abs(power - 2) < 1e-9) return "²";
            if (System.Math.Abs(power - 3) < 1e-9) return "³";
            if (System.Math.Abs(power - 4) < 1e-9) return "⁴";
            if (System.Math.Abs(power - 5) < 1e-9) return "⁵";
            if (System.Math.Abs(power - 1) < 1e-9) return "";
            if (System.Math.Abs(power - 0) < 1e-9) return "";
            return $"^{UI.Fmt(power)}";
        }

        private static string FormatTerm(double coeff, double power, string variable)
        {
            if (System.Math.Abs(coeff) < 1e-12) return "0";
            if (System.Math.Abs(power) < 1e-12) return UI.Fmt(coeff);
            if (System.Math.Abs(power - 1) < 1e-12)
                return System.Math.Abs(coeff - 1) < 1e-12 ? variable
                    : System.Math.Abs(coeff + 1) < 1e-12 ? $"-{variable}"
                    : $"{UI.Fmt(coeff)}{variable}";
            string c = System.Math.Abs(coeff - 1) < 1e-12 ? ""
                : System.Math.Abs(coeff + 1) < 1e-12 ? "-"
                : UI.Fmt(coeff);
            return $"{c}{variable}{GetSuperscript(power)}";
        }

        private static string FormatLineEquation(double m, double c)
        {
            bool mZero = System.Math.Abs(m) < 1e-12;
            bool cZero = System.Math.Abs(c) < 1e-12;
            if (mZero && cZero) return "y = 0";
            if (mZero) return $"y = {UI.Fmt(c)}";

            string mStr;
            if (System.Math.Abs(m - 1) < 1e-12) mStr = "";
            else if (System.Math.Abs(m + 1) < 1e-12) mStr = "-";
            else mStr = UI.Fmt(m);

            if (cZero) return $"y = {mStr}x";
            string cSign = c > 0 ? "+" : "-";
            return $"y = {mStr}x {cSign} {UI.Fmt(System.Math.Abs(c))}";
        }

        private static string FormatPoly(double a, double b, double c, double d)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (System.Math.Abs(a) > 1e-12) parts.Add(FormatTerm(a, 3, "x"));
            if (System.Math.Abs(b) > 1e-12)
            {
                string t = FormatTerm(System.Math.Abs(b), 2, "x");
                parts.Add(b > 0 ? (parts.Count > 0 ? $"+ {t}" : t) : (parts.Count > 0 ? $"- {t}" : $"-{t}"));
            }
            if (System.Math.Abs(c) > 1e-12)
            {
                string t = FormatTerm(System.Math.Abs(c), 1, "x");
                parts.Add(c > 0 ? (parts.Count > 0 ? $"+ {t}" : t) : (parts.Count > 0 ? $"- {t}" : $"-{t}"));
            }
            if (System.Math.Abs(d) > 1e-12)
            {
                string t = UI.Fmt(System.Math.Abs(d));
                parts.Add(d > 0 ? (parts.Count > 0 ? $"+ {t}" : t) : (parts.Count > 0 ? $"- {t}" : $"-{t}"));
            }
            return parts.Count > 0 ? string.Join(" ", parts) : "0";
        }

        private void AddVariationRow(string interval, string status, Color color)
        {
            var border = new Border
            {
                Background = DS.LightBg(color),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 0, 4),
                BorderBrush = DS.BrushAlpha(color, 80),
                BorderThickness = new Thickness(1)
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tbInterval = new TextBlock
            {
                Text = interval,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary,
                Foreground = _isDarkMode ? Brushes.White : new SolidColorBrush(Color.FromRgb(33, 33, 33))
            };

            var tbStatus = new TextBlock
            {
                Text = status,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(color)
            };
            Grid.SetColumn(tbInterval, 0);
            Grid.SetColumn(tbStatus, 1);
            grid.Children.Add(tbInterval);
            grid.Children.Add(tbStatus);
            border.Child = grid;
            variationContent.Children.Add(border);
        }

        private void OpenHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new HistoryWindow();
                win.Owner = Window.GetWindow(this);
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở lịch sử: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.D1:
                    case Key.NumPad1:
                        SelectModeByHotkey(Mode.Power);
                        e.Handled = true;
                        break;
                    case Key.D2:
                    case Key.NumPad2:
                        SelectModeByHotkey(Mode.Polynomial);
                        e.Handled = true;
                        break;
                    case Key.D3:
                    case Key.NumPad3:
                        SelectModeByHotkey(Mode.Special);
                        e.Handled = true;
                        break;
                    case Key.D4:
                    case Key.NumPad4:
                        SelectModeByHotkey(Mode.Tangent);
                        e.Handled = true;
                        break;
                }
            }
        }

        private void SelectModeByHotkey(Mode targetMode)
        {
            if (_mode == targetMode) return;
            _mode = targetMode;
            BuildModeButtons();
            UpdateUI();
            Calc();
        }

        private void CopySolution_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_solutionText))
                {
                    Clipboard.SetText(_solutionText);
                    MessageBox.Show("Đã sao chép lời giải chi tiết vào Clipboard!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Không có nội dung lời giải để sao chép.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi sao chép: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyLaTeX_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string latex = GenerateLaTeX();
                if (!string.IsNullOrEmpty(latex))
                {
                    Clipboard.SetText(latex);
                    MessageBox.Show("Đã sao chép công thức LaTeX vào Clipboard!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Không thể tạo biểu thức LaTeX cho trạng thái hiện tại.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi sao chép LaTeX: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string GenerateLaTeX()
        {
            switch (_mode)
            {
                case Mode.Power:
                    {
                        if (ParsingHelper.TryParseDouble(txtCoeff?.Text, out double a) &&
                            ParsingHelper.TryParseDouble(txtPower?.Text, out double n))
                        {
                            string fx = FormatTermLaTeX(a, n, "x");
                            string fpx = n == 0 ? "0" : FormatTermLaTeX(n * a, n - 1, "x");
                            return $"f(x) = {fx} \\Rightarrow f'(x) = {fpx}";
                        }
                        return "";
                    }
                case Mode.Polynomial:
                    {
                        if (ParsingHelper.TryParseDouble(txtPolyA?.Text, out double a) &&
                            ParsingHelper.TryParseDouble(txtPolyB?.Text, out double b) &&
                            ParsingHelper.TryParseDouble(txtPolyC?.Text, out double c) &&
                            ParsingHelper.TryParseDouble(txtPolyD?.Text, out double d))
                        {
                            string fx = FormatPolyLaTeX(a, b, c, d);
                            string fpx = FormatPolyLaTeX(0, 3 * a, 2 * b, c);
                            return $"f(x) = {fx} \\Rightarrow f'(x) = {fpx}";
                        }
                        return "";
                    }
                case Mode.Special:
                    {
                        if (_specialIndex < 0 || _specialIndex >= _specials.Length) return "";
                        string name = _specials[_specialIndex].Name;
                        string fxLatex = name switch
                        {
                            "sin(x)" => "\\sin(x)",
                            "cos(x)" => "\\cos(x)",
                            "tan(x)" => "\\tan(x)",
                            "cot(x)" => "\\cot(x)",
                            "eˣ" => "e^x",
                            "ln(x)" => "\\ln(x)",
                            "√x" => "\\sqrt{x}",
                            "1/x" => "\\frac{1}{x}",
                            "aˣ" => "a^x",
                            "logₐ(x)" => "\\log_a(x)",
                            "sin²(x)" => "\\sin^2(x)",
                            "cos²(x)" => "\\cos^2(x)",
                            _ => name
                        };
                        string fpxLatex = name switch
                        {
                            "sin(x)" => "\\cos(x)",
                            "cos(x)" => "-\\sin(x)",
                            "tan(x)" => "\\frac{1}{\\cos^2(x)}",
                            "cot(x)" => "-\\frac{1}{\\sin^2(x)}",
                            "eˣ" => "e^x",
                            "ln(x)" => "\\frac{1}{x}",
                            "√x" => "\\frac{1}{2\\sqrt{x}}",
                            "1/x" => "-\\frac{1}{x^2}",
                            "aˣ" => "a^x \\ln(a)",
                            "logₐ(x)" => "\\frac{1}{x \\ln(a)}",
                            "sin²(x)" => "\\sin(2x)",
                            "cos²(x)" => "-\\sin(2x)",
                            _ => ""
                        };
                        return $"f(x) = {fxLatex} \\Rightarrow f'(x) = {fpxLatex}";
                    }
                case Mode.Tangent:
                    {
                        if (ParsingHelper.TryParseDouble(txtTanA?.Text, out double a) &&
                            ParsingHelper.TryParseDouble(txtTanB?.Text, out double b) &&
                            ParsingHelper.TryParseDouble(txtTanC?.Text, out double c) &&
                            ParsingHelper.TryParseDouble(txtTanD?.Text, out double d) &&
                            ParsingHelper.TryParseDouble(txtTanX0?.Text, out double x0))
                        {
                            double fx0 = a * x0 * x0 * x0 + b * x0 * x0 + c * x0 + d;
                            double fpx0 = 3 * a * x0 * x0 + 2 * b * x0 + c;
                            double tanSlope = fpx0;
                            double tanIntercept = fx0 - fpx0 * x0;

                            string fx = FormatPolyLaTeX(a, b, c, d);
                            
                            string tangentLatex;
                            bool mZero = System.Math.Abs(tanSlope) < 1e-12;
                            bool cZero = System.Math.Abs(tanIntercept) < 1e-12;
                            if (mZero && cZero) tangentLatex = "y = 0";
                            else if (mZero) tangentLatex = $"y = {UI.Fmt(tanIntercept)}";
                            else
                            {
                                string mStr = System.Math.Abs(tanSlope - 1) < 1e-12 ? "" : (System.Math.Abs(tanSlope + 1) < 1e-12 ? "-" : UI.Fmt(tanSlope));
                                if (cZero) tangentLatex = $"y = {mStr}x";
                                else
                                {
                                    string cSign = tanIntercept > 0 ? "+" : "-";
                                    tangentLatex = $"y = {mStr}x {cSign} {UI.Fmt(System.Math.Abs(tanIntercept))}";
                                }
                            }

                            return $"f(x) = {fx}, x_0 = {UI.Fmt(x0)} \\Rightarrow y = f'(x_0)(x - x_0) + f(x_0) \\Rightarrow {tangentLatex}";
                        }
                        return "";
                    }
                default:
                    return "";
            }
        }

        private static string FormatTermLaTeX(double coeff, double power, string variable)
        {
            if (System.Math.Abs(coeff) < 1e-12) return "0";
            if (System.Math.Abs(power) < 1e-12) return UI.Fmt(coeff);
            
            string powerStr;
            if (System.Math.Abs(power - 1) < 1e-12)
            {
                powerStr = "";
            }
            else
            {
                string pVal = UI.Fmt(power);
                powerStr = pVal.Length > 1 || pVal.StartsWith("-") ? $"^{{{pVal}}}" : $"^{pVal}";
            }

            if (System.Math.Abs(coeff - 1) < 1e-12)
                return $"{variable}{powerStr}";
            if (System.Math.Abs(coeff + 1) < 1e-12)
                return $"-{variable}{powerStr}";
            
            return $"{UI.Fmt(coeff)}{variable}{powerStr}";
        }

        private static string FormatPolyLaTeX(double a, double b, double c, double d)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (System.Math.Abs(a) > 1e-12) parts.Add(FormatTermLaTeX(a, 3, "x"));
            if (System.Math.Abs(b) > 1e-12)
            {
                string t = FormatTermLaTeX(System.Math.Abs(b), 2, "x");
                parts.Add(b > 0 ? (parts.Count > 0 ? $"+ {t}" : t) : (parts.Count > 0 ? $"- {t}" : $"-{t}"));
            }
            if (System.Math.Abs(c) > 1e-12)
            {
                string t = FormatTermLaTeX(System.Math.Abs(c), 1, "x");
                parts.Add(c > 0 ? (parts.Count > 0 ? $"+ {t}" : t) : (parts.Count > 0 ? $"- {t}" : $"-{t}"));
            }
            if (System.Math.Abs(d) > 1e-12)
            {
                string t = UI.Fmt(System.Math.Abs(d));
                parts.Add(d > 0 ? (parts.Count > 0 ? $"+ {t}" : t) : (parts.Count > 0 ? $"- {t}" : $"-{t}"));
            }
            return parts.Count > 0 ? string.Join(" ", parts) : "0";
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PNG Image (*.png)|*.png",
                    FileName = "DaoHam_BaiGiang.png",
                    Title = "Xuất ảnh bài giảng"
                };

                if (sfd.ShowDialog() == true)
                {
                    double width = mainGrid.ActualWidth;
                    double height = mainGrid.ActualHeight;

                    if (width <= 0 || height <= 0)
                    {
                        width = 1000;
                        height = 800;
                    }

                    var rtb = new RenderTargetBitmap(
                        (int)width, 
                        (int)height, 
                        96, 
                        96, 
                        PixelFormats.Pbgra32);

                    rtb.Render(mainGrid);

                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));

                    using (var stream = System.IO.File.Create(sfd.FileName))
                    {
                        encoder.Save(stream);
                    }

                    MessageBox.Show("Đã xuất ảnh bài giảng thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể xuất ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ToggleDarkMode_Click(object sender, RoutedEventArgs e)
        {
            _isDarkMode = !_isDarkMode;
            ApplyThemeColors();
            BuildModeButtons();
            BuildSpecialButtons();
            BuildFormulas();
            Calc(); // Recalculate to apply colors to results
        }

        private void ApplyThemeColors()
        {
            if (_isDarkMode)
            {
                mainGrid.Background = new SolidColorBrush(Color.FromRgb(18, 18, 18));
                headerBorder.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                lblHeaderTitle.Foreground = new SolidColorBrush(Color.FromRgb(144, 202, 249)); // #90CAF9
                lblHeaderSub.Foreground = new SolidColorBrush(Color.FromRgb(100, 181, 246));
                
                btnHistory.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                btnHistory.Foreground = Brushes.White;
                btnHistory.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));

                btnExport.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                btnExport.Foreground = new SolidColorBrush(Color.FromRgb(129, 199, 132)); // light green
                btnExport.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));

                btnDarkMode.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                btnDarkMode.Foreground = new SolidColorBrush(Color.FromRgb(255, 183, 77)); // light orange
                btnDarkMode.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));
                btnDarkMode.Content = "🌙 Tối";

                resultsCard.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                resultsCard.BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                lblResultsTitle.Foreground = new SolidColorBrush(Color.FromRgb(129, 199, 132));

                solutionBorder.Background = new SolidColorBrush(Color.FromRgb(40, 38, 20));
                solutionBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 75, 40));
                txtSolution.Foreground = Brushes.White;
                
                UpdateVisualTree(mainGrid);
            }
            else
            {
                mainGrid.Background = new SolidColorBrush(Color.FromRgb(245, 247, 250));
                var lg = new LinearGradientBrush(Color.FromRgb(227, 242, 253), Color.FromRgb(187, 222, 251), 0);
                headerBorder.Background = lg;
                lblHeaderTitle.Foreground = (Brush)FindResource("BrandPrimary");
                lblHeaderSub.Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210));

                btnHistory.Background = Brushes.White;
                btnHistory.Foreground = (Brush)FindResource("BrandPrimary");
                btnHistory.BorderBrush = new SolidColorBrush(Color.FromRgb(187, 222, 251));

                btnExport.Background = Brushes.White;
                btnExport.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                btnExport.BorderBrush = new SolidColorBrush(Color.FromRgb(200, 230, 201));

                btnDarkMode.Background = Brushes.White;
                btnDarkMode.Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0));
                btnDarkMode.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 224, 178));
                btnDarkMode.Content = "☀️ Sáng";

                resultsCard.Background = Brushes.White;
                resultsCard.BorderBrush = new SolidColorBrush(Color.FromRgb(200, 230, 201));
                lblResultsTitle.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));

                solutionBorder.Background = new SolidColorBrush(Color.FromRgb(255, 253, 231));
                solutionBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 249, 196));
                txtSolution.Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51));

                UpdateVisualTree(mainGrid);
            }
        }

        private void UpdateVisualTree(DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is Border b)
                {
                    if (b != headerBorder && b != resultsCard && b != solutionBorder)
                    {
                        if (_isDarkMode)
                        {
                            if (b.Background is SolidColorBrush scb && scb.Color == Colors.White)
                            {
                                b.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                                b.BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                            }
                            else if (b.Background is SolidColorBrush scb2 && scb2.Color == Color.FromRgb(227, 242, 253))
                            {
                                b.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                                b.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));
                            }
                        }
                        else
                        {
                            if (b.Background is SolidColorBrush scb && scb.Color == Color.FromRgb(30, 30, 30))
                            {
                                b.Background = Brushes.White;
                                b.BorderBrush = new SolidColorBrush(Color.FromRgb(187, 222, 251));
                            }
                            else if (b.Background is SolidColorBrush scb2 && scb2.Color == Color.FromRgb(40, 40, 40))
                            {
                                b.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                                b.BorderBrush = new SolidColorBrush(Color.FromRgb(144, 202, 249));
                            }
                        }
                    }
                }
                else if (child is TextBlock tb)
                {
                    if (tb != lblHeaderTitle && tb != lblHeaderSub && tb != lblResultsTitle)
                    {
                        if (_isDarkMode)
                        {
                            if (tb.Foreground is SolidColorBrush scb && (scb.Color == Color.FromRgb(66, 66, 66) || scb.Color == Color.FromRgb(33, 33, 33) || scb.Color == Colors.Black))
                            {
                                tb.Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220));
                            }
                        }
                        else
                        {
                            if (tb.Foreground is SolidColorBrush scb && scb.Color == Color.FromRgb(220, 220, 220))
                            {
                                tb.Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66));
                            }
                        }
                    }
                }
                else if (child is TextBox txt)
                {
                    if (_isDarkMode)
                    {
                        txt.Background = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                        txt.Foreground = Brushes.White;
                        txt.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
                    }
                    else
                    {
                        txt.Background = new SolidColorBrush(Color.FromRgb(245, 249, 255));
                        txt.Foreground = Brushes.Black;
                        txt.BorderBrush = new SolidColorBrush(Color.FromRgb(187, 222, 251));
                    }
                }
                UpdateVisualTree(child);
            }
        }

        private void ToggleSolution_Click(object sender, RoutedEventArgs e)
        {
            if (solutionBorder != null && btnSolutionToggle != null)
            {
                if (solutionBorder.Visibility == Visibility.Visible)
                {
                    solutionBorder.Visibility = Visibility.Collapsed;
                    btnSolutionToggle.Content = "🔍 Xem lời giải chi tiết";
                }
                else
                {
                    solutionBorder.Visibility = Visibility.Visible;
                    btnSolutionToggle.Content = "🔍 Ẩn lời giải chi tiết";
                }
            }
        }

        private void RenderPowerVariation(double a, double n)
        {
            if (variationContent == null) return;
            variationContent.Children.Clear();
            variationPanel.Visibility = Visibility.Visible;

            if (n == 0)
            {
                AddVariationRow("(-∞; +∞)", "➡️ Hàm hằng (không đổi)", DS.ResultInfo);
                return;
            }

            if (n == 1)
            {
                if (a > 0)
                    AddVariationRow("(-∞; +∞)", "↗ Đồng biến", DS.ResultSuccess);
                else if (a < 0)
                    AddVariationRow("(-∞; +∞)", "↘ Nghịch biến", DS.ResultDanger);
                else
                    AddVariationRow("(-∞; +∞)", "➡️ Hàm hằng (không đổi)", DS.ResultInfo);
                return;
            }

            if (n > 0)
            {
                bool isEven = System.Math.Abs(n % 2) < 1e-9 && System.Math.Abs(System.Math.Round(n) - n) < 1e-9;
                
                if (isEven)
                {
                    if (a > 0)
                    {
                        AddVariationRow("(-∞; 0)", "↘ Nghịch biến", DS.ResultDanger);
                        AddVariationRow("(0; +∞)", "↗ Đồng biến", DS.ResultSuccess);
                    }
                    else
                    {
                        AddVariationRow("(-∞; 0)", "↗ Đồng biến", DS.ResultSuccess);
                        AddVariationRow("(0; +∞)", "↘ Nghịch biến", DS.ResultDanger);
                    }
                }
                else
                {
                    if (n % 1 != 0)
                    {
                        if (a > 0)
                            AddVariationRow("[0; +∞)", "↗ Đồng biến (Tập xác định x ≥ 0)", DS.ResultSuccess);
                        else
                            AddVariationRow("[0; +∞)", "↘ Nghịch biến (Tập xác định x ≥ 0)", DS.ResultDanger);
                    }
                    else
                    {
                        if (a > 0)
                            AddVariationRow("(-∞; +∞)", "↗ Đồng biến", DS.ResultSuccess);
                        else
                            AddVariationRow("(-∞; +∞)", "↘ Nghịch biến", DS.ResultDanger);
                    }
                }
            }
            else // n < 0
            {
                bool isEven = System.Math.Abs(n % 2) < 1e-9 && System.Math.Abs(System.Math.Round(n) - n) < 1e-9;
                if (isEven)
                {
                    if (a > 0)
                    {
                        AddVariationRow("(-∞; 0)", "↗ Đồng biến", DS.ResultSuccess);
                        AddVariationRow("(0; +∞)", "↘ Nghịch biến", DS.ResultDanger);
                    }
                    else
                    {
                        AddVariationRow("(-∞; 0)", "↘ Nghịch biến", DS.ResultDanger);
                        AddVariationRow("(0; +∞)", "↗ Đồng biến", DS.ResultSuccess);
                    }
                }
                else
                {
                    if (n % 1 != 0)
                    {
                        if (a > 0)
                            AddVariationRow("(0; +∞)", "↘ Nghịch biến (Tập xác định x > 0)", DS.ResultDanger);
                        else
                            AddVariationRow("(0; +∞)", "↗ Đồng biến (Tập xác định x > 0)", DS.ResultSuccess);
                    }
                    else
                    {
                        if (a > 0)
                        {
                            AddVariationRow("(-∞; 0)", "↘ Nghịch biến", DS.ResultDanger);
                            AddVariationRow("(0; +∞)", "↘ Nghịch biến", DS.ResultDanger);
                        }
                        else
                        {
                            AddVariationRow("(-∞; 0)", "↗ Đồng biến", DS.ResultSuccess);
                            AddVariationRow("(0; +∞)", "↗ Đồng biến", DS.ResultSuccess);
                        }
                    }
                }
            }
        }

        private void RenderPolyVariation(double a, double b, double c, double d)
        {
            if (variationContent == null) return;
            variationContent.Children.Clear();
            variationPanel.Visibility = Visibility.Visible;

            double A = 3 * a, B = 2 * b, C = c;
            if (System.Math.Abs(A) > 1e-12)
            {
                double delta = B * B - 4 * A * C;
                if (delta > 0)
                {
                    double r1 = (-B - System.Math.Sqrt(delta)) / (2 * A);
                    double r2 = (-B + System.Math.Sqrt(delta)) / (2 * A);
                    double x1 = System.Math.Min(r1, r2);
                    double x2 = System.Math.Max(r1, r2);

                    if (a > 0)
                    {
                        AddVariationRow($"(-∞; {UI.Fmt(x1)})", "↗ Đồng biến", DS.ResultSuccess);
                        AddVariationRow($"({UI.Fmt(x1)}; {UI.Fmt(x2)})", "↘ Nghịch biến", DS.ResultDanger);
                        AddVariationRow($"({UI.Fmt(x2)}; +∞)", "↗ Đồng biến", DS.ResultSuccess);
                    }
                    else
                    {
                        AddVariationRow($"(-∞; {UI.Fmt(x1)})", "↘ Nghịch biến", DS.ResultDanger);
                        AddVariationRow($"({UI.Fmt(x1)}; {UI.Fmt(x2)})", "↗ Đồng biến", DS.ResultSuccess);
                        AddVariationRow($"({UI.Fmt(x2)}; +∞)", "↘ Nghịch biến", DS.ResultDanger);
                    }
                }
                else
                {
                    if (a > 0)
                        AddVariationRow("(-∞; +∞)", "↗ Đồng biến (Không có cực trị)", DS.ResultSuccess);
                    else
                        AddVariationRow("(-∞; +∞)", "↘ Nghịch biến (Không có cực trị)", DS.ResultDanger);
                }
            }
            else if (System.Math.Abs(b) > 1e-12)
            {
                double x0 = -c / (2 * b);
                if (b > 0)
                {
                    AddVariationRow($"(-∞; {UI.Fmt(x0)})", "↘ Nghịch biến", DS.ResultDanger);
                    AddVariationRow($"({UI.Fmt(x0)}; +∞)", "↗ Đồng biến", DS.ResultSuccess);
                }
                else
                {
                    AddVariationRow($"(-∞; {UI.Fmt(x0)})", "↗ Đồng biến", DS.ResultSuccess);
                    AddVariationRow($"({UI.Fmt(x0)}; +∞)", "↘ Nghịch biến", DS.ResultDanger);
                }
            }
            else
            {
                if (c > 0)
                    AddVariationRow("(-∞; +∞)", "↗ Đồng biến", DS.ResultSuccess);
                else if (c < 0)
                    AddVariationRow("(-∞; +∞)", "↘ Nghịch biến", DS.ResultDanger);
                else
                    AddVariationRow("(-∞; +∞)", "➡️ Hàm hằng", DS.ResultInfo);
            }
        }

        private string GeneratePowerSolution(double a, double n)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("📝 HƯỚNG DẪN GIẢI CHI TIẾT ĐƠN THỨC:");
            sb.AppendLine($"Hàm số đã cho: f(x) = {FormatTerm(a, n, "x")}");
            sb.AppendLine();
            sb.AppendLine("● Bước 1: Áp dụng công thức đạo hàm đơn thức cơ bản:");
            sb.AppendLine("   (xⁿ)' = n · xⁿ⁻¹");
            sb.AppendLine("   (a · u)' = a · u'");
            sb.AppendLine();
            sb.AppendLine("● Bước 2: Thế số và tính đạo hàm bậc 1:");
            if (n == 0)
            {
                sb.AppendLine($"   f'(x) = ({UI.Fmt(a)})' = 0");
                sb.AppendLine();
                sb.AppendLine("● Bước 3: Đạo hàm bậc 2:");
                sb.AppendLine("   f''(x) = (0)' = 0");
            }
            else
            {
                double newC = n * a;
                double newP = n - 1;
                sb.AppendLine($"   f'(x) = {UI.Fmt(n)} · {UI.Fmt(a)} · x^({UI.Fmt(n)} - 1)");
                sb.AppendLine($"         = {FormatTerm(newC, newP, "x")}");
                sb.AppendLine();
                sb.AppendLine("● Bước 3: Tính đạo hàm bậc 2:");
                if (newP == 0)
                {
                    sb.AppendLine($"   f''(x) = ({FormatTerm(newC, 0, "x")})' = 0");
                }
                else
                {
                    double c2 = n * (n - 1) * a;
                    double p2 = n - 2;
                    sb.AppendLine($"   f''(x) = (f'(x))' = ({FormatTerm(newC, newP, "x")})'");
                    sb.AppendLine($"          = {UI.Fmt(newP)} · {UI.Fmt(newC)} · x^({UI.Fmt(newP)} - 1)");
                    sb.AppendLine($"          = {FormatTerm(c2, p2, "x")}");
                }
            }
            return sb.ToString();
        }

        private string GeneratePolySolution(double a, double b, double c, double d)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("📝 HƯỚNG DẪN GIẢI CHI TIẾT ĐA THỨC:");
            sb.AppendLine($"Hàm số đã cho: f(x) = {FormatPoly(a, b, c, d)}");
            sb.AppendLine();
            sb.AppendLine("● Bước 1: Tập xác định:");
            sb.AppendLine("   D = ℝ");
            sb.AppendLine();
            sb.AppendLine("● Bước 2: Tính đạo hàm bậc 1:");
            sb.AppendLine($"   f'(x) = ( {FormatPoly(a, b, c, d)} )'");
            sb.AppendLine($"         = 3 · {UI.Fmt(a)}·x² + 2 · {UI.Fmt(b)}·x + {UI.Fmt(c)}");
            sb.AppendLine($"         = {FormatPoly(0, 3 * a, 2 * b, c)}");
            sb.AppendLine();
            sb.AppendLine("● Bước 3: Tính đạo hàm bậc 2:");
            sb.AppendLine($"   f''(x) = ( {FormatPoly(0, 3 * a, 2 * b, c)} )'");
            sb.AppendLine($"          = 6 · {UI.Fmt(a)}·x + 2 · {UI.Fmt(b)}");
            sb.AppendLine($"          = {FormatPoly(0, 0, 6 * a, 2 * b)}");
            sb.AppendLine();
            sb.AppendLine("● Bước 4: Tìm điểm dừng và cực trị (Giải phương trình f'(x) = 0):");
            
            double A = 3 * a, B = 2 * b, C = c;
            if (System.Math.Abs(A) > 1e-12)
            {
                double delta = B * B - 4 * A * C;
                sb.AppendLine($"   Xét phương trình bậc hai f'(x) = 0: {FormatPoly(0, A, B, C)} = 0");
                sb.AppendLine($"   Ta có: Δ' = b'² - 3ac = {UI.Fmt(b)}² - 3 · {UI.Fmt(a)} · {UI.Fmt(c)} = {UI.Fmt(B * B / 4 - A * C)}");
                if (delta > 0)
                {
                    double r1 = (-B - System.Math.Sqrt(delta)) / (2 * A);
                    double r2 = (-B + System.Math.Sqrt(delta)) / (2 * A);
                    double x1 = System.Math.Min(r1, r2);
                    double x2 = System.Math.Max(r1, r2);
                    double y1 = a * x1 * x1 * x1 + b * x1 * x1 + c * x1 + d;
                    double y2 = a * x2 * x2 * x2 + b * x2 * x2 + c * x2 + d;
                    double fpp1 = 6 * a * x1 + 2 * b;
                    double fpp2 = 6 * a * x2 + 2 * b;
                    string type1 = fpp1 > 0 ? "cực tiểu" : "cực đại";
                    string type2 = fpp2 > 0 ? "cực tiểu" : "cực đại";
                    
                    sb.AppendLine($"   Phương trình f'(x) = 0 có hai nghiệm phân biệt:");
                    sb.AppendLine($"   x₁ = {UI.Fmt(x1)}  => f(x₁) = {UI.Fmt(y1)} ({type1})");
                    sb.AppendLine($"   x₂ = {UI.Fmt(x2)}  => f(x₂) = {UI.Fmt(y2)} ({type2})");
                }
                else if (System.Math.Abs(delta) < 1e-12)
                {
                    double x0 = -B / (2 * A);
                    sb.AppendLine($"   Phương trình f'(x) = 0 có nghiệm kép x = {UI.Fmt(x0)}");
                    sb.AppendLine("   Hàm số không có cực trị.");
                }
                else
                {
                    sb.AppendLine("   Phương trình f'(x) = 0 vô nghiệm. Hàm số không có cực trị.");
                }
                
                double xI = -b / (3 * a);
                double yI = a * xI * xI * xI + b * xI * xI + c * xI + d;
                sb.AppendLine();
                sb.AppendLine($"● Bước 5: Tìm điểm uốn (Giải phương trình f''(x) = 0):");
                sb.AppendLine($"   {FormatPoly(0, 0, 6 * a, 2 * b)} = 0  => x = {UI.Fmt(xI)}");
                sb.AppendLine($"   Tọa độ điểm uốn: I({UI.Fmt(xI)}; {UI.Fmt(yI)})");
            }
            else if (System.Math.Abs(b) > 1e-12)
            {
                double x0 = -c / (2 * b);
                double y0 = b * x0 * x0 + c * x0 + d;
                string type = b > 0 ? "Cực tiểu (đỉnh Parabol)" : "Cực đại (đỉnh Parabol)";
                sb.AppendLine($"   Vì a = 0, hàm số trở thành hàm bậc hai f(x) = {FormatPoly(0, b, c, d)}");
                sb.AppendLine($"   f'(x) = 0 => {FormatPoly(0, 0, 2 * b, c)} = 0 => x = {UI.Fmt(x0)}");
                sb.AppendLine($"   Tọa độ cực trị: x₀ = {UI.Fmt(x0)}  => f(x₀) = {UI.Fmt(y0)} ({type})");
            }
            else
            {
                sb.AppendLine("   Hàm số bậc nhất hoặc hằng số. Không có cực trị và không có điểm uốn.");
            }
            return sb.ToString();
        }


        private string GenerateSpecialSolution(string name, string fx, string fpx)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("📝 HƯỚNG DẪN GIẢI CHI TIẾT HÀM ĐẶC BIỆT:");
            sb.AppendLine($"Hàm số đã chọn: f(x) = {fx}");
            sb.AppendLine();

            sb.AppendLine("● Bước 1: Xác định quy tắc đạo hàm cơ bản:");
            switch (name)
            {
                case "sin(x)":
                    sb.AppendLine("   Công thức chuẩn: (sin x)' = cos x");
                    sb.AppendLine();
                    sb.AppendLine("● Bước 2: Hướng dẫn giải chi tiết:");
                    sb.AppendLine("   - Với hàm cơ bản f(x) = sin(x) thì đạo hàm bậc 1 là f'(x) = cos(x).");
                    sb.AppendLine("   - Với hàm hợp f(u) = sin(u), ta có công thức: (sin u)' = u' · cos u.");
                    sb.AppendLine("     Ví dụ: Tính đạo hàm của g(x) = sin(2x)");
                    sb.AppendLine("     Ta có: u = 2x  →  u' = (2x)' = 2");
                    sb.AppendLine("     Do đó: g'(x) = 2 · cos(2x)");
                    break;
                case "cos(x)":
                    sb.AppendLine("   Công thức chuẩn: (cos x)' = -sin x");
                    sb.AppendLine();
                    sb.AppendLine("● Bước 2: Hướng dẫn giải chi tiết:");
                    sb.AppendLine("   - Với hàm cơ bản f(x) = cos(x) thì đạo hàm bậc 1 là f'(x) = -sin(x).");
                    sb.AppendLine("   - Với hàm hợp f(u) = cos(u), ta có công thức: (cos u)' = -u' · sin u.");
                    sb.AppendLine("     Ví dụ: Tính đạo hàm của g(x) = cos(3x)");
                    sb.AppendLine("     Ta có: u = 3x  →  u' = (3x)' = 3");
                    sb.AppendLine("     Do đó: g'(x) = -3 · sin(3x)");
                    break;
                case "tan(x)":
                    sb.AppendLine("   Công thức chuẩn: (tan x)' = 1/cos²(x) = 1 + tan²(x)");
                    sb.AppendLine("   Điều kiện xác định: x ≠ π/2 + kπ (k ∈ ℤ)");
                    sb.AppendLine();
                    sb.AppendLine("● Bước 2: Hướng dẫn giải chi tiết:");
                    sb.AppendLine("   - Với hàm cơ bản f(x) = tan(x) thì đạo hàm là f'(x) = 1/cos²(x).");
                    sb.AppendLine("   - Với hàm hợp f(u) = tan(u), ta có công thức: (tan u)' = u' / cos²(u).");
                    sb.AppendLine("     Ví dụ: Tính đạo hàm của g(x) = tan(x²)");
                    sb.AppendLine("     Ta có: u = x²  →  u' = 2x");
                    sb.AppendLine("     Do đó: g'(x) = 2x / cos²(x²)");
                    break;
                case "cot(x)":
                    sb.AppendLine("   Công thức chuẩn: (cot x)' = -1/sin²(x) = -(1 + cot²(x))");
                    sb.AppendLine("   Điều kiện xác định: x ≠ kπ (k ∈ ℤ)");
                    sb.AppendLine();
                    sb.AppendLine("● Bước 2: Hướng dẫn giải chi tiết:");
                    sb.AppendLine("   - Với hàm cơ bản f(x) = cot(x) thì đạo hàm là f'(x) = -1/sin²(x).");
                    sb.AppendLine("   - Với hàm hợp f(u) = cot(u), ta có công thức: (cot u)' = -u' / sin²(u).");
                    sb.AppendLine("     Ví dụ: Tính đạo hàm của g(x) = cot(5x)");
                    sb.AppendLine("     Ta có: u = 5x  →  u' = 5");
                    sb.AppendLine("     Do đó: g'(x) = -5 / sin²(5x)");
                    break;
                case "eᵪ": // eˣ
                case "eˣ":
                    sb.AppendLine("   Công thức chuẩn: (eˣ)' = eˣ");
                    sb.AppendLine();
                    sb.AppendLine("● Bước 2: Hướng dẫn giải chi tiết:");
                    sb.AppendLine("   - Hàm số mũ cơ số tự nhiên e có tính chất đặc biệt: đạo hàm bằng chính nó.");
                    sb.AppendLine("   - Với hàm hợp f(u) = eᵘ, ta có công thức: (eᵘ)' = u' · eᵘ.");
                    sb.AppendLine("     Ví dụ: Tính đạo hàm của g(x) = e^(2x)");
                    sb.AppendLine("     Ta có: u = 2x  →  u' = 2");
                    sb.AppendLine("     Do đó: g'(x) = 2 · e^(2x)");
                    break;
                case "ln(x)":
                    sb.AppendLine("   Công thức chuẩn: (ln x)' = 1/x");
                    sb.AppendLine("   Điều kiện xác định: x > 0");
                    sb.AppendLine();
                    sb.AppendLine("● Bước 2: Hướng dẫn giải chi tiết:");
                    sb.AppendLine("   - Đạo hàm của hàm logarit tự nhiên: f'(x) = 1/x.");
                    sb.AppendLine("   - Với hàm hợp f(u) = ln(u), ta có công thức: (ln u)' = u' / u.");
                    sb.AppendLine("     Ví dụ: Tính đạo hàm của g(x) = ln(x² + 1)");
                    sb.AppendLine("     Ta có: u = x² + 1  →  u' = 2x");
                    sb.AppendLine("     Do đó: g'(x) = 2x / (x² + 1)");
                    break;
                case "√x":
                    sb.AppendLine("   Công thức chuẩn: (√x)' = 1/(2√x)");
                    sb.AppendLine("   Điều kiện xác định khi tính đạo hàm: x > 0");
                    sb.AppendLine();
                    sb.AppendLine("● Bước 2: Hướng dẫn giải chi tiết:");
                    sb.AppendLine("   - Công thức này được suy ra từ quy tắc số mũ: x^(1/2) → (1/2) · x^(-1/2) = 1/(2√x).");
                    sb.AppendLine("   - Với hàm hợp f(u) = √u, ta có công thức: (√u)' = u' / (2√u).");
                    sb.AppendLine("     Ví dụ: Tính đạo hàm của g(x) = √(3x + 1)");
                    sb.AppendLine("     Ta có: u = 3x + 1  →  u' = 3");
                    sb.AppendLine("     Do đó: g'(x) = 3 / (2√(3x + 1))");
                    break;
                case "1/x":
                    sb.AppendLine("   Công thức chuẩn: (1/x)' = -1/x²");
                    sb.AppendLine("   Điều kiện xác định: x ≠ 0");
                    sb.AppendLine();
                    sb.AppendLine("● Bước 2: Hướng dẫn giải chi tiết:");
                    sb.AppendLine("   - Công thức suy từ quy tắc số mũ: x⁻¹ → -1 · x⁻² = -1/x².");
                    sb.AppendLine("   - Với hàm hợp f(u) = 1/u, ta có công thức: (1/u)' = -u' / u².");
                    sb.AppendLine("     Ví dụ: Tính đạo hàm của g(x) = 1/(x² + x)");
                    sb.AppendLine("     Ta có: u = x² + x  →  u' = 2x + 1");
                    sb.AppendLine("     Do đó: g'(x) = -(2x + 1) / (x² + x)²");
                    break;
                case "aᵪ":
                case "aˣ":
                    sb.AppendLine("   Công thức chuẩn: (aˣ)' = aˣ · ln(a)");
                    sb.AppendLine("   Điều kiện: cơ số a > 0 và a ≠ 1");
                    sb.AppendLine();
                    sb.AppendLine("● Bước 2: Hướng dẫn giải chi tiết:");
                    sb.AppendLine("   - Với hàm số mũ tổng quát, ta nhân thêm hằng số ln(a) vào kết quả.");
                    sb.AppendLine("   - Với hàm hợp f(u) = aᵘ, ta có công thức: (aᵘ)' = u' · aᵘ · ln(a).");
                    sb.AppendLine("     Ví dụ: Tính đạo hàm của g(x) = 2^(x³)");
                    sb.AppendLine("     Ta có: u = x³  →  u' = 3x²");
                    sb.AppendLine("     Do đó: g'(x) = 3x² · 2^(x³) · ln(2)");
                    break;
                case "logᵪ(x)":
                case "logₐ(x)":
                    sb.AppendLine("   Công thức chuẩn: (logₐ x)' = 1 / (x · ln a)");
                    sb.AppendLine("   Điều kiện: x > 0, cơ số a > 0 và a ≠ 1");
                    sb.AppendLine();
                    sb.AppendLine("● Bước 2: Hướng dẫn giải chi tiết:");
                    sb.AppendLine("   - Với hàm logarit cơ số a, đạo hàm có thêm ln(a) ở mẫu số.");
                    sb.AppendLine("   - Với hàm hợp f(u) = logₐ(u), ta có công thức: (logₐ u)' = u' / (u · ln a).");
                    sb.AppendLine("     Ví dụ: Tính đạo hàm của g(x) = log₂(x² + 2)");
                    sb.AppendLine("     Ta có: u = x² + 2  →  u' = 2x");
                    sb.AppendLine("     Do đó: g'(x) = 2x / ((x² + 2) · ln 2)");
                    break;
                case "sin²(x)":
                    sb.AppendLine("   Công thức và phương pháp giải:");
                    sb.AppendLine("   Hàm số có dạng lũy thừa của hàm số: f(x) = (sin x)²");
                    sb.AppendLine();
                    sb.AppendLine("● Bước 2: Hướng dẫn giải chi tiết:");
                    sb.AppendLine("   - Áp dụng quy tắc đạo hàm hàm hợp lũy thừa: (u²)' = 2u · u'");
                    sb.AppendLine("     Đặt u = sin(x)  →  u' = cos(x)");
                    sb.AppendLine("     Do đó: f'(x) = 2 · sin(x) · (sin x)' = 2 · sin(x) · cos(x)");
                    sb.AppendLine("   - Áp dụng công thức nhân đôi lượng giác: 2 · sin(x) · cos(x) = sin(2x)");
                    sb.AppendLine("   - Kết quả cuối cùng: f'(x) = sin(2x)");
                    break;
                case "cos²(x)":
                    sb.AppendLine("   Công thức và phương pháp giải:");
                    sb.AppendLine("   Hàm số có dạng lũy thừa của hàm số: f(x) = (cos x)²");
                    sb.AppendLine();
                    sb.AppendLine("● Bước 2: Hướng dẫn giải chi tiết:");
                    sb.AppendLine("   - Áp dụng quy tắc đạo hàm hàm hợp lũy thừa: (u²)' = 2u · u'");
                    sb.AppendLine("     Đặt u = cos(x)  →  u' = -sin(x)");
                    sb.AppendLine("     Do đó: f'(x) = 2 · cos(x) · (cos x)' = 2 · cos(x) · (-sin(x)) = -2 · sin(x) · cos(x)");
                    sb.AppendLine("   - Áp dụng công thức nhân đôi lượng giác: -2 · sin(x) · cos(x) = -sin(2x)");
                    sb.AppendLine("   - Kết quả cuối cùng: f'(x) = -sin(2x)");
                    break;
                default:
                    sb.AppendLine($"   Công thức chuẩn: ({name})' = {fpx}");
                    sb.AppendLine();
                    sb.AppendLine("● Bước 2: Quy tắc xích tổng quát:");
                    sb.AppendLine("   Nếu f(u(x)) là hàm hợp, ta tính đạo hàm theo công thức:");
                    sb.AppendLine("   [f(u)]' = f'(u) · u'");
                    break;
            }
            return sb.ToString();
        }

        private string GenerateTangentSolution(double a, double b, double c, double d, double x0, double fx0, double fpx0, double m, double intercept)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("📝 HƯỚNG DẪN GIẢI CHI TIẾT TIẾP TUYẾN:");
            sb.AppendLine($"Hàm số đã cho: f(x) = {FormatPoly(a, b, c, d)}");
            sb.AppendLine($"Điểm tiếp xúc có hoành độ x₀ = {UI.Fmt(x0)}");
            sb.AppendLine();
            sb.AppendLine("● Bước 1: Công thức tổng quát của phương trình tiếp tuyến:");
            sb.AppendLine("   y = f'(x₀) · (x - x₀) + f(x₀)");
            sb.AppendLine();
            sb.AppendLine("● Bước 2: Tính tung độ tiếp điểm y₀ = f(x₀):");
            sb.AppendLine($"   y₀ = f({UI.Fmt(x0)}) = {UI.Fmt(a)}·({UI.Fmt(x0)})³ + {UI.Fmt(b)}·({UI.Fmt(x0)})² + {UI.Fmt(c)}·({UI.Fmt(x0)}) + {UI.Fmt(d)}");
            sb.AppendLine($"      = {UI.Fmt(fx0)}");
            sb.AppendLine();
            sb.AppendLine("● Bước 3: Tính hệ số góc tiếp tuyến k = f'(x₀):");
            sb.AppendLine($"   f'(x) = {FormatPoly(0, 3 * a, 2 * b, c)}");
            sb.AppendLine($"   k = f'({UI.Fmt(x0)}) = {UI.Fmt(fpx0)}");
            sb.AppendLine();
            sb.AppendLine("● Bước 4: Viết phương trình tiếp tuyến:");
            sb.AppendLine($"   y = {UI.Fmt(fpx0)} · (x - {UI.Fmt(x0)}) + {UI.Fmt(fx0)}");
            sb.AppendLine($"   y = {FormatLineEquation(m, intercept)}");
            return sb.ToString();
        }




    }
}