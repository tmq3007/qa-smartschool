using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;
using QASmartClass.LearningTools.Helpers;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class CalculatorTool : BaseToolControl
    {
        private readonly List<string> _calcHistory = new();
        private string _lastResult = "0";
        private bool _useDegrees = true; // Default to DEG (Degrees) for secondary school math

        public CalculatorTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                menuTextPractice.Text = isVN ? "Máy tính khoa học" : "Scientific Calculator";
                menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";
                
                sideMenu.SelectedIndex = 0;

                BuildCalcButtons();
                LoadPracticalApps();
                
                // Tải lịch sử tính toán từ SQLite
                try
                {
                    _calcHistory.Clear();
                    var savedHistory = DbManager.LoadCalculatorHistory();
                    _calcHistory.AddRange(savedHistory);
                    RefreshHistoryList();
                }
                catch { }

                // Track when input text changes to reset output display if input is cleared
                txtCalcInput.TextChanged += (s, _) =>
                {
                    if (string.IsNullOrWhiteSpace(txtCalcInput.Text))
                    {
                        txtCalcDisplay.Text = "0";
                    }
                };
            };
        }

        private string GetStr(string key, string fallback)
        {
            try
            {
                return Application.Current.TryFindResource(key) as string ?? fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private void BuildCalcButtons()
        {
            if (calcButtons == null) return;
            calcButtons.Children.Clear();

            // Replaced '%' with 'Ans' for a much more standard scientific calculator layout
            var buttons = new (string Label, string Bg, string Fg)[]
            {
                ("sin",  "#374151", "#E5E7EB"), ("cos",  "#374151", "#E5E7EB"),
                ("tan",  "#374151", "#E5E7EB"), ("log",  "#374151", "#E5E7EB"),
                ("ln",   "#374151", "#E5E7EB"), ("√",    "#374151", "#E5E7EB"),

                ("7",    "#FFFFFF", "#1F2937"), ("8",    "#FFFFFF", "#1F2937"),
                ("9",    "#FFFFFF", "#1F2937"), ("÷",    "#DBEAFE", "#1E40AF"),
                ("(",    "#F3F4F6", "#6B7280"), (")",    "#F3F4F6", "#6B7280"),

                ("4",    "#FFFFFF", "#1F2937"), ("5",    "#FFFFFF", "#1F2937"),
                ("6",    "#FFFFFF", "#1F2937"), ("×",    "#DBEAFE", "#1E40AF"),
                ("^",    "#DBEAFE", "#1E40AF"), ("Ans",  "#DBEAFE", "#1E40AF"),

                ("1",    "#FFFFFF", "#1F2937"), ("2",    "#FFFFFF", "#1F2937"),
                ("3",    "#FFFFFF", "#1F2937"), ("-",    "#DBEAFE", "#1E40AF"),
                ("⌫",    "#FEF3C7", "#92400E"), ("C",    "#FEE2E2", "#991B1B"),

                ("0",    "#FFFFFF", "#1F2937"), (".",    "#FFFFFF", "#1F2937"),
                ("π",    "#F3F4F6", "#6B7280"), ("e",    "#F3F4F6", "#6B7280"),
                ("+",    "#DBEAFE", "#1E40AF"), ("=",    "#2563EB", "#FFFFFF"),
            };

            foreach (var (label, bg, fg) in buttons)
            {
                var btn = new Button
                {
                    Content = label,
                    FontSize = 20,
                    Height = 64,
                    Margin = new Thickness(4),
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Tag = label
                };

                var bgColor = (Color)ColorConverter.ConvertFromString(bg);
                var fgColor = (Color)ColorConverter.ConvertFromString(fg);

                var template = new ControlTemplate(typeof(Button));
                var bd = new FrameworkElementFactory(typeof(Border), "bd");
                bd.SetValue(Border.BackgroundProperty, new SolidColorBrush(bgColor));
                bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
                bd.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)));
                bd.SetValue(Border.BorderThicknessProperty, new Thickness(1));
                
                var cp = new FrameworkElementFactory(typeof(ContentPresenter));
                cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                cp.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(fgColor));
                cp.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold); // Bold for visibility
                bd.AppendChild(cp);
                template.VisualTree = bd;

                // Hover style trigger
                var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                byte r = (byte)System.Math.Max(0, bgColor.R - 20);
                byte g = (byte)System.Math.Max(0, bgColor.G - 20);
                byte b = (byte)System.Math.Max(0, bgColor.B - 20);
                hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(r, g, b)), "bd"));
                template.Triggers.Add(hoverTrigger);

                btn.Template = template;
                btn.Click += CalcBtn_Click;
                calcButtons.Children.Add(btn);
            }
        }

        private void CalcBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string label) return;

            SoundHelper.PlayClick();

            switch (label)
            {
                case "C":
                    txtCalcInput.Text = "";
                    txtCalcDisplay.Text = "0";
                    break;
                case "⌫":
                    if (txtCalcInput.Text.Length > 0)
                        txtCalcInput.Text = txtCalcInput.Text[..^1];
                    break;
                case "=":
                    EvaluateExpression();
                    break;
                case "π":
                    txtCalcInput.Text += "π";
                    break;
                case "e":
                    txtCalcInput.Text += "e";
                    break;
                case "√":
                    txtCalcInput.Text += "Sqrt(";
                    break;
                case "log":
                    txtCalcInput.Text += "Log10(";
                    break;
                case "ln":
                    txtCalcInput.Text += "Ln(";
                    break;
                case "±":
                    try
                    {
                        var eval = new MathEvaluator(txtCalcDisplay.Text, _useDegrees);
                        double val = eval.Parse();
                        txtCalcDisplay.Text = (-val).ToString(CultureInfo.InvariantCulture);
                    }
                    catch { }
                    break;
                case "÷": txtCalcInput.Text += "/"; break;
                case "×": txtCalcInput.Text += "*"; break;
                case "^": txtCalcInput.Text += "^"; break;
                case "sin": txtCalcInput.Text += "Sin("; break;
                case "cos": txtCalcInput.Text += "Cos("; break;
                case "tan": txtCalcInput.Text += "Tan("; break;
                case "Ans": txtCalcInput.Text += _lastResult; break;
                default:
                    txtCalcInput.Text += label;
                    break;
            }
        }

        private void CalcInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                EvaluateExpression();
            }
            else if (e.Key == Key.Escape)
            {
                // Escape key clears the calculator (identical to 'C' button)
                txtCalcInput.Text = "";
                txtCalcDisplay.Text = "0";
                e.Handled = true;
            }
        }

        private void EvaluateExpression()
        {
            try
            {
                string expr = txtCalcInput.Text.Trim();
                if (string.IsNullOrWhiteSpace(expr)) return;

                // Case-insensitive normalization for keyboard inputs
                expr = expr.Replace("sin(", "Sin(", StringComparison.OrdinalIgnoreCase)
                           .Replace("cos(", "Cos(", StringComparison.OrdinalIgnoreCase)
                           .Replace("tan(", "Tan(", StringComparison.OrdinalIgnoreCase)
                           .Replace("sqrt(", "Sqrt(", StringComparison.OrdinalIgnoreCase)
                           .Replace("log(", "Log10(", StringComparison.OrdinalIgnoreCase)
                           .Replace("log10(", "Log10(", StringComparison.OrdinalIgnoreCase)
                           .Replace("ln(", "Ln(", StringComparison.OrdinalIgnoreCase)
                           .Replace("pi", "π", StringComparison.OrdinalIgnoreCase);

                // Use the compiler-grade MathEvaluator parser
                var eval = new MathEvaluator(expr, _useDegrees);
                double result = eval.Parse();

                _lastResult = result.ToString("G10", CultureInfo.InvariantCulture);
                txtCalcDisplay.Text = _lastResult;

                // Save to history
                _calcHistory.Insert(0, $"{expr} = {_lastResult}");
                if (_calcHistory.Count > 50) _calcHistory.RemoveAt(50);
                try
                {
                    DbManager.SaveCalculatorHistory(expr, _lastResult);
                }
                catch { }
                
                // Refresh history UI if panel is open
                if (calcHistoryPanel.Visibility == Visibility.Visible)
                {
                    RefreshHistoryList();
                }
            }
            catch (DivideByZeroException)
            {
                txtCalcDisplay.Text = GetStr("Calc_ErrDivideByZero", "Lỗi chia cho 0");
            }
            catch (ArgumentException ex) when (ex.Message.Contains("Căn bậc hai số âm"))
            {
                txtCalcDisplay.Text = GetStr("Calc_ErrNegativeSqrt", "Căn bậc hai số âm");
            }
            catch (ArgumentException ex) when (ex.Message.Contains("Log số âm/bằng 0"))
            {
                txtCalcDisplay.Text = GetStr("Calc_ErrLogRange", "Lỗi miền giá trị Log");
            }
            catch
            {
                txtCalcDisplay.Text = GetStr("Calc_ErrExpression", "Lỗi biểu thức");
            }
        }

        private void ToggleUnit_Click(object sender, MouseButtonEventArgs e)
        {
            _useDegrees = !_useDegrees;
            txtCalcUnit.Text = _useDegrees ? "DEG" : "RAD";
            borderUnit.Background = _useDegrees 
                ? new SolidColorBrush(Color.FromRgb(21, 101, 192)) // Blue
                : new SolidColorBrush(Color.FromRgb(216, 67, 21));  // Deep Orange

            // Re-evaluate current expression with the new angle unit if it exists
            if (!string.IsNullOrWhiteSpace(txtCalcInput.Text))
            {
                EvaluateExpression();
            }
        }

        private void CalcHistory_Click(object sender, RoutedEventArgs e)
        {
            calcHistoryPanel.Visibility = calcHistoryPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed : Visibility.Visible;

            if (calcHistoryPanel.Visibility == Visibility.Visible)
            {
                RefreshHistoryList();
            }
        }

        private void RefreshHistoryList()
        {
            if (calcHistoryList == null) return;
            calcHistoryList.Children.Clear();

            if (_calcHistory.Count == 0)
            {
                calcHistoryList.Children.Add(new TextBlock
                {
                    Text = GetStr("Calc_NoHistory", "Chưa có lịch sử"),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 10)
                });
                return;
            }

            foreach (var item in _calcHistory)
            {
                var row = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 4),
                    Cursor = Cursors.Hand
                };
                
                var tb = new TextBlock
                {
                    Text = item,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    FontFamily = new FontFamily("Segoe UI"),
                    Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55))
                };
                row.Child = tb;

                row.MouseLeftButtonDown += (s, _) =>
                {
                    var parts = item.Split(" = ");
                    if (parts.Length == 2)
                    {
                        txtCalcInput.Text = parts[0];
                        txtCalcInput.Focus();
                        txtCalcInput.CaretIndex = txtCalcInput.Text.Length;
                    }
                };

                row.MouseEnter += (s, _) => row.Background = new SolidColorBrush(Color.FromRgb(232, 234, 246));
                row.MouseLeave += (s, _) => row.Background = Brushes.White;
                calcHistoryList.Children.Add(row);
            }
        }

        private void CalcClearAll_Click(object sender, RoutedEventArgs e)
        {
            txtCalcInput.Text = "";
            txtCalcDisplay.Text = "0";
            _calcHistory.Clear();
            try
            {
                DbManager.ClearCalculatorHistory();
            }
            catch { }
            calcHistoryPanel.Visibility = Visibility.Collapsed;
            RefreshHistoryList();
        }

        // ═══════════════════════════════════════════════════════════
        //  RECURSIVE DESCENT MATH PARSER (Compiler-grade logic)
        // ═══════════════════════════════════════════════════════════
        private class MathEvaluator
        {
            private readonly string _expr;
            private int _pos = -1;
            private int _ch;
            private readonly bool _useDegrees;

            public MathEvaluator(string expr, bool useDegrees)
            {
                _expr = expr;
                _useDegrees = useDegrees;
            }

            private void NextChar()
            {
                _ch = (++_pos < _expr.Length) ? _expr[_pos] : -1;
            }

            private bool Eat(int charToEat)
            {
                while (_ch == ' ') NextChar();
                if (_ch == charToEat)
                {
                    NextChar();
                    return true;
                }
                return false;
            }

            public double Parse()
            {
                NextChar();
                double x = ParseExpression();
                while (_ch == ' ') NextChar();
                if (_pos < _expr.Length) throw new FormatException("Ký tự không hợp lệ: " + (char)_ch);
                return x;
            }

            // Expression = Term | Expression `+` Term | Expression `-` Term
            private double ParseExpression()
            {
                double x = ParseTerm();
                for (; ; )
                {
                    if (Eat('+')) x += ParseTerm(); // Cộng
                    else if (Eat('-')) x -= ParseTerm(); // Trừ
                    else return x;
                }
            }

            // Term = Factor | Term `*` Factor | Term `/` Factor | Term `%` Factor
            private double ParseTerm()
            {
                double x = ParseFactor();
                for (; ; )
                {
                    if (Eat('*') || Eat('×')) x *= ParseFactor(); // Nhân
                    else if (Eat('/') || Eat('÷'))
                    {
                        double divisor = ParseFactor();
                        if (divisor == 0) throw new DivideByZeroException();
                        x /= divisor; // Chia
                    }
                    else if (Eat('%'))
                    {
                        double divisor = ParseFactor();
                        if (divisor == 0) throw new DivideByZeroException();
                        x %= divisor; // Modulo
                    }
                    else return x;
                }
            }

            // Factor = `+` Factor | `-` Factor | `(` Expression `)` | Number | Function Factor | Factor `^` Factor
            private double ParseFactor()
            {
                if (Eat('+')) return ParseFactor(); // Phép cộng đơn trị
                if (Eat('-')) return -ParseFactor(); // Phép trừ đơn trị

                double x;
                int startPos = _pos;
                if (Eat('('))
                { // Ngoặc đơn
                    x = ParseExpression();
                    if (!Eat(')')) throw new FormatException("Thiếu dấu ngoặc đóng )");
                }
                else if ((_ch >= '0' && _ch <= '9') || _ch == '.')
                { // Số thập phân
                    while ((_ch >= '0' && _ch <= '9') || _ch == '.') NextChar();
                    x = double.Parse(_expr[startPos.._pos], CultureInfo.InvariantCulture);
                }
                else if ((_ch >= 'a' && _ch <= 'z') || (_ch >= 'A' && _ch <= 'Z') || _ch == 'π')
                { // Tên hàm hoặc hằng số
                    while ((_ch >= 'a' && _ch <= 'z') || (_ch >= 'A' && _ch <= 'Z') || _ch == 'π') NextChar();
                    string name = _expr[startPos.._pos];
                    if (name == "pi" || name == "π")
                    {
                        x = System.Math.PI;
                    }
                    else if (name == "e")
                    {
                        x = System.Math.E;
                    }
                    else
                    {
                        if (!Eat('(')) throw new FormatException("Thiếu dấu ngoặc mở (");
                        double arg = ParseExpression();
                        if (!Eat(')')) throw new FormatException("Thiếu dấu ngoặc đóng )");

                        string lowerName = name.ToLower();
                        if (lowerName == "sin")
                        {
                            double angle = _useDegrees ? (arg * System.Math.PI / 180.0) : arg;
                            x = System.Math.Sin(angle);
                        }
                        else if (lowerName == "cos")
                        {
                            double angle = _useDegrees ? (arg * System.Math.PI / 180.0) : arg;
                            x = System.Math.Cos(angle);
                        }
                        else if (lowerName == "tan")
                        {
                            double angle = _useDegrees ? (arg * System.Math.PI / 180.0) : arg;
                            x = System.Math.Tan(angle);
                        }
                        else if (lowerName == "sqrt")
                        {
                            if (arg < 0) throw new ArgumentException("Căn bậc hai số âm");
                            x = System.Math.Sqrt(arg);
                        }
                        else if (lowerName == "log" || lowerName == "log10")
                        {
                            if (arg <= 0) throw new ArgumentException("Log số âm/bằng 0");
                            x = System.Math.Log10(arg);
                        }
                        else if (lowerName == "ln")
                        {
                            if (arg <= 0) throw new ArgumentException("Log số âm/bằng 0");
                            x = System.Math.Log(arg);
                        }
                        else
                        {
                            throw new FormatException("Hàm không xác định");
                        }
                    }
                }
                else
                {
                    throw new FormatException("Ký tự không hợp lệ");
                }

                if (Eat('^'))
                { // Toán tử luỹ thừa
                    double exponent = ParseFactor();
                    x = System.Math.Pow(x, exponent);
                }

                return x;
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
                        Icon = "📐",
                        Title = isVN ? "Đo đạc công trình & Trắc địa" : "Construction & Surveying",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_calculator_1_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng các hàm lượng giác (sin, cos, tan) trên máy tính để tính toán chiều cao công trình, khoảng cách bản đồ và góc nghiêng của mái nhà hoặc dốc dầm đường." 
                            : "Use trigonometric functions (sin, cos, tan) on scientific calculators to compute building heights, surveying distances, and structural slope angles."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧪",
                        Title = isVN ? "Tính nồng độ & Phản ứng hóa học" : "Chemical Concentration & Reactions",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_calculator_2_{suffix}.png",
                        Description = isVN 
                            ? "Tính toán tỷ lệ chất phản ứng, nồng độ dung dịch (hàm log để tính pH) và cân bằng phương trình hóa học trong thực nghiệm phòng thí nghiệm." 
                            : "Calculate reactant ratios, solution concentrations (logarithmic functions for pH calculation), and balance chemical reaction equations in laboratory experiments."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💵",
                        Title = isVN ? "Tài chính & Tính lãi suất" : "Financial Interest & Growth",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_calculator_3_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng các hàm lũy thừa và logarit để tính toán lãi kép ngân hàng, lập kế hoạch trả nợ vay và ước tính thời gian tăng trưởng dòng tiền." 
                            : "Use exponentiation and logarithmic equations to compute compound interest, amortize loans, and project financial asset growth over time."
                    }
                };

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for CalculatorTool: {Err}", ex.Message);
            }
        }
    }
}