using System;
using QASmartClass.LearningTools.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class LogarithmTool : BaseToolControl
    {
        public LogarithmTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextCalc != null) menuTextCalc.Text = isVN ? "Tính toán" : "Calculate";
                if (menuTextTable != null) menuTextTable.Text = isVN ? "Bảng giá trị" : "Values Table";
                if (menuTextFormula != null) menuTextFormula.Text = isVN ? "Công thức" : "Formulas";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                Calculate();
                BuildQuickRef();
                BuildTable();
                BuildFormulas();
                LoadPracticalApps();
                // Bàn phím số mini
                if (txtLogBase != null) TouchNumPad.Attach(txtLogBase, step: 1, min: 0.1);
                if (txtLogValue != null) TouchNumPad.Attach(txtLogValue, step: 1, min: 0.01);
                if (txtPowBase != null) TouchNumPad.Attach(txtPowBase, step: 1);
                if (txtPowExp != null) TouchNumPad.Attach(txtPowExp, step: 1);
                if (txtExpX != null) TouchNumPad.Attach(txtExpX, step: 0.5);
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  CALCULATOR
        // ═══════════════════════════════════════════════════════════

        private void Calc_Changed(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) Calculate();
        }

        private void Calculate()
        {
            // Log
            if (ParsingHelper.TryParseDouble(txtLogBase?.Text, out double logBase) &&
                ParsingHelper.TryParseDouble(txtLogValue?.Text, out double logVal) &&
                logBase > 0 && logBase != 1 && logVal > 0)
            {
                double result = System.Math.Log(logVal, logBase);
                if (txtLogResult != null) txtLogResult.Text = $"= {result:G8}";
            }
            else if (txtLogResult != null)
            {
                txtLogResult.Text = "= (Cơ số a > 0, a ≠ 1; x > 0)";
            }

            // Power
            if (ParsingHelper.TryParseDouble(txtPowBase?.Text, out double powBase) &&
                ParsingHelper.TryParseDouble(txtPowExp?.Text, out double powExp))
            {
                double result = System.Math.Pow(powBase, powExp);
                if (txtPowResult != null)
                {
                    if (double.IsNaN(result))
                        txtPowResult.Text = "= (Không xác định trong tập số thực)";
                    else if (double.IsInfinity(result))
                        txtPowResult.Text = "= ∞";
                    else
                        txtPowResult.Text = $"= {result:G10}";
                }
            }
            else if (txtPowResult != null)
            {
                txtPowResult.Text = "= (giá trị không hợp lệ)";
            }

            // e^x
            if (ParsingHelper.TryParseDouble(txtExpX?.Text, out double expX))
            {
                double result = System.Math.Exp(expX);
                if (txtExpResult != null)
                {
                    if (double.IsInfinity(result))
                        txtExpResult.Text = "= ∞";
                    else
                        txtExpResult.Text = $"= {result:G10}";
                }
            }
            else if (txtExpResult != null)
            {
                txtExpResult.Text = "= (giá trị không hợp lệ)";
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  QUICK REFERENCE PANEL
        // ═══════════════════════════════════════════════════════════

        private void BuildQuickRef()
        {
            if (quickRefPanel == null) return;
            quickRefPanel.Children.Clear();

            quickRefPanel.Children.Add(MakeHeader("📌 Giá trị đặc biệt"));

            var specials = new (string Label, string Value)[]
            {
                ("e", $"{System.Math.E:F10}"),
                ("ln(1)", "0"),
                ("ln(e)", "1"),
                ("ln(e²)", "2"),
                ("log₁₀(10)", "1"),
                ("log₁₀(100)", "2"),
                ("log₁₀(1000)", "3"),
                ("log₂(2)", "1"),
                ("log₂(8)", "3"),
                ("log₂(1024)", "10"),
                ("2¹⁰", "1,024"),
                ("2²⁰", "1,048,576"),
                ("10³", "1,000"),
                ("10⁶", "1,000,000"),
            };

            foreach (var (label, value) in specials)
            {
                var row = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 5, 10, 5),
                    Margin = new Thickness(0, 0, 0, 3),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    ToolTip = "Click để copy"
                };
                var dp = new DockPanel();
                dp.Children.Add(new TextBlock
                {
                    Text = label, FontSize = 12,
                    FontFamily = new FontFamily("Segoe UI"),
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 105, 92)),
                    VerticalAlignment = VerticalAlignment.Center
                });
                var valTb = new TextBlock
                {
                    Text = $"= {value}", FontSize = 12, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };
                DockPanel.SetDock(valTb, Dock.Right);
                dp.Children.Add(valTb);
                row.Child = dp;

                string copyVal = value;
                row.MouseLeftButtonDown += (_, _) =>
                {
                    try { Clipboard.SetText(copyVal); } catch { }
                };

                quickRefPanel.Children.Add(row);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  BẢNG GIÁ TRỊ
        // ═══════════════════════════════════════════════════════════

        private void BuildTable()
        {
            if (tablePanel == null) return;
            tablePanel.Children.Clear();

            // log₁₀ table
            tablePanel.Children.Add(MakeHeader("📊 Bảng log₁₀(x)"));
            var logGrid = MakeTableGrid(new[] { "x", "log₁₀(x)", "ln(x)" });
            double[] vals = { 0.01, 0.1, 0.5, 1, 2, 3, 5, 7, 10, 20, 50, 100, 1000, 10000 };
            foreach (double x in vals)
            {
                double log10Val = System.Math.Log10(x);
                double lnVal = System.Math.Log(x);
                string[] rowVals = { $"{x}", $"{log10Val:G6}", $"{lnVal:G6}" };
                string[] rowTooltips = {
                    $"Biến số x = {x}",
                    $"Logarit cơ số 10 của {x}: log₁₀({x}) ≈ {log10Val:F6} (10^{log10Val:F2} ≈ {x})",
                    $"Logarit tự nhiên của {x}: ln({x}) ≈ {lnVal:F6} (e^{lnVal:F2} ≈ {x})"
                };
                AddTableRow(logGrid, rowVals, rowTooltips);
            }
            tablePanel.Children.Add(WrapGrid(logGrid));

            // Powers of 2
            tablePanel.Children.Add(MakeHeader("📊 Lũy thừa của 2"));
            var pow2Grid = MakeTableGrid(new[] { "n", "2ⁿ", "log₂(2ⁿ)" });
            for (int n = 0; n <= 20; n++)
            {
                long val = 1L << n;
                string[] rowVals = { $"{n}", $"{val:N0}", $"{n}" };
                string[] rowTooltips = {
                    $"Số mũ n = {n}",
                    $"Lũy thừa của 2: 2^{n} = {val:N0}",
                    $"Logarit cơ số 2: log₂(2^{n}) = {n}"
                };
                AddTableRow(pow2Grid, rowVals, rowTooltips);
            }
            tablePanel.Children.Add(WrapGrid(pow2Grid));

            // Powers of 10
            tablePanel.Children.Add(MakeHeader("📊 Lũy thừa của 10"));
            var pow10Grid = MakeTableGrid(new[] { "n", "10ⁿ", "Tên gọi" });
            var names = new[] { "Một", "Mười", "Trăm", "Nghìn", "Mười nghìn", "Trăm nghìn", "Triệu", 
                               "Chục triệu", "Trăm triệu", "Tỷ", "Chục tỷ", "Trăm tỷ", "Nghìn tỷ" };
            for (int n = 0; n < names.Length; n++)
            {
                double val = System.Math.Pow(10, n);
                string[] rowVals = { $"{n}", $"{val:N0}", names[n] };
                string[] rowTooltips = {
                    $"Số mũ n = {n}",
                    $"Lũy thừa của 10: 10^{n} = {val:N0}",
                    $"Tên gọi số 10^{n}: {names[n]}"
                };
                AddTableRow(pow10Grid, rowVals, rowTooltips);
            }
            tablePanel.Children.Add(WrapGrid(pow10Grid));
        }

        // ═══════════════════════════════════════════════════════════
        //  FORMULAS
        // ═══════════════════════════════════════════════════════════

        private void BuildFormulas()
        {
            if (formulaPanel == null) return;
            formulaPanel.Children.Clear();

            var sections = new (string Title, string[] Items)[]
            {
                ("Tính chất Logarithm", new[]
                {
                    "logₐ(1) = 0",
                    "logₐ(a) = 1",
                    "logₐ(xy) = logₐ(x) + logₐ(y)",
                    "logₐ(x/y) = logₐ(x) - logₐ(y)",
                    "logₐ(xⁿ) = n·logₐ(x)",
                    "logₐ(x) = logᵦ(x) / logᵦ(a)  [đổi cơ số]",
                }),
                ("Tính chất Lũy thừa", new[]
                {
                    "aᵐ × aⁿ = aᵐ⁺ⁿ",
                    "aᵐ / aⁿ = aᵐ⁻ⁿ",
                    "(aᵐ)ⁿ = aᵐⁿ",
                    "(ab)ⁿ = aⁿbⁿ",
                    "a⁰ = 1  (a ≠ 0)",
                    "a⁻ⁿ = 1/aⁿ",
                    "a^(1/n) = ⁿ√a",
                }),
                ("Hàm mũ tự nhiên", new[]
                {
                    "e ≈ 2.71828 18284 59045...",
                    "eˣ × eʸ = eˣ⁺ʸ",
                    "ln(eˣ) = x",
                    "e^(ln x) = x",
                    "d/dx [eˣ] = eˣ",
                    "d/dx [ln x] = 1/x",
                    "∫ eˣ dx = eˣ + C",
                    "∫ (1/x) dx = ln|x| + C",
                }),
            };

            foreach (var (title, items) in sections)
            {
                var section = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(16, 12, 16, 12),
                    Margin = new Thickness(0, 0, 0, 12),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(128, 203, 196)),
                    BorderThickness = new Thickness(0, 0, 0, 2)
                };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = $"📐 {title}",
                    FontSize = 13, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 105, 92)),
                    Margin = new Thickness(0, 0, 0, 8)
                });
                foreach (var item in items)
                    sp.Children.Add(new TextBlock
                    {
                        Text = $"  {item}", FontSize = 13,
                        FontFamily = new FontFamily("Segoe UI"),
                        Margin = new Thickness(0, 2, 0, 2)
                    });
                section.Child = sp;
                formulaPanel.Children.Add(section);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════

        private static TextBlock MakeHeader(string text) => new()
        {
            Text = text, FontSize = 14, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0, 105, 92)),
            Margin = new Thickness(0, 12, 0, 6)
        };

        private static string GetHeaderTooltip(string header)
        {
            return header switch
            {
                "x" => "Giá trị biến số x",
                "log₁₀(x)" => "Logarit cơ số 10 của x (thập phân)",
                "ln(x)" => "Logarit tự nhiên (cơ số e ≈ 2.718) của x",
                "n" => "Số mũ (n nguyên)",
                "2ⁿ" => "Giá trị lũy thừa cơ số 2 với số mũ n",
                "log₂(2ⁿ)" => "Logarit cơ số 2 của 2ⁿ (bằng n)",
                "10ⁿ" => "Giá trị lũy thừa cơ số 10 với số mũ n",
                "Tên gọi" => "Tên gọi tương ứng của giá trị 10ⁿ trong tiếng Việt",
                _ => ""
            };
        }

        private static Grid MakeTableGrid(string[] headers)
        {
            var grid = new Grid();
            for (int i = 0; i < headers.Length; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Subtle background and border under headers
            var headerBg = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 0, 105, 92)), // 10% theme color
                BorderBrush = new SolidColorBrush(Color.FromRgb(128, 203, 196)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Height = 32,
                Margin = new Thickness(-8, -4, -8, 4)
            };
            Grid.SetRow(headerBg, 0);
            Grid.SetColumnSpan(headerBg, headers.Length);
            grid.Children.Add(headerBg);

            for (int i = 0; i < headers.Length; i++)
            {
                var tb = new TextBlock
                {
                    Text = headers[i], FontSize = 12, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 105, 92)), // Dark green theme color instead of white
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 4, 4, 4),
                    ToolTip = GetHeaderTooltip(headers[i])
                };
                Grid.SetColumn(tb, i);
                Grid.SetRow(tb, 0);
                grid.Children.Add(tb);
            }
            return grid;
        }

        private static void AddTableRow(Grid grid, string[] values, string[] tooltips = null)
        {
            int row = grid.RowDefinitions.Count;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int i = 0; i < values.Length; i++)
            {
                var tb = new TextBlock
                {
                    Text = values[i], FontSize = 12,
                    FontFamily = new FontFamily("Segoe UI"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(4, 3, 4, 3)
                };
                if (tooltips != null && i < tooltips.Length && !string.IsNullOrEmpty(tooltips[i]))
                {
                    tb.ToolTip = tooltips[i];
                }
                Grid.SetColumn(tb, i);
                Grid.SetRow(tb, row);
                grid.Children.Add(tb);
            }
        }

        private static Border WrapGrid(Grid grid) => new()
        {
            Child = grid,
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 0, 0, 16),
            Padding = new Thickness(8, 4, 8, 4),
            BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
            BorderThickness = new Thickness(1)
        };
        private void ResetAll_Click(object sender, MouseButtonEventArgs e)
        {
            if (txtLogBase != null) txtLogBase.Text = "10";
            if (txtLogValue != null) txtLogValue.Text = "100";
            if (txtPowBase != null) txtPowBase.Text = "2";
            if (txtPowExp != null) txtPowExp.Text = "10";
            if (txtExpX != null) txtExpX.Text = "1";
        }

        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string baseStr = txtLogBase?.Text ?? "10";
                if (!ParsingHelper.TryParseDouble(baseStr, out double b) || b <= 0 || b == 1) b = 10;
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                string bS = b.ToString(ci);

                string expJs = $@"
        calc.setExpression({{id:'log', latex:'y=\\log_{{{bS}}}(x)', color:'#00695C', lineWidth:3}});
        calc.setExpression({{id:'ln', latex:'y=\\ln(x)', color:'#1565C0', lineWidth:2.5}});
        calc.setExpression({{id:'exp', latex:'y=e^x', color:'#C62828', lineWidth:2.5}});
        calc.setExpression({{id:'pow', latex:'y={bS}^x', color:'#7B1FA2', lineWidth:2, lineStyle:'DASHED'}});
        calc.setExpression({{id:'id', latex:'y=x', color:'#9E9E9E', lineWidth:1, lineStyle:'DASHED'}});
        calc.setExpression({{id:'ptLog', latex:'(1, 0)', color:'#00695C', pointSize:8, label:'(1,0)', showLabel:true}});
        calc.setExpression({{id:'ptExp', latex:'(0, 1)', color:'#C62828', pointSize:8, label:'(0,1)', showLabel:true}});
        calc.setMathBounds({{ left: -3, right: 10, bottom: -5, top: 8 }});";

                var win = new GraphWindow(expJs, $"📈 Logarithm & Lũy thừa — cơ số {bS}");
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Không thể mở đồ thị: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        Icon = "🔊",
                        Title = isVN ? "Đo lường độ ồn Decibel (dB)" : "Decibel Sound Level (dB)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_logarithm_1_{suffix}.png",
                        Description = isVN 
                            ? "Cường độ âm thanh tai người cảm nhận được tăng theo thang mũ, do đó được đo bằng đơn vị Decibel sử dụng hàm logarit cơ số 10: L = 10 * log10(I/I0). Mỗi lần tăng thêm 10 dB tương ứng với cường độ âm thanh thực tế tăng gấp 10 lần." 
                            : "Sound intensity perceived by human ears grows exponentially, so it is measured on a logarithmic decibel scale: L = 10 * log10(I/I0). An increase of 10 dB corresponds to a tenfold increase in physical sound intensity."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌍",
                        Title = isVN ? "Thang đo động đất Richter" : "Richter Earthquake Scale",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_logarithm_2_{suffix}.png",
                        Description = isVN 
                            ? "Độ lớn động đất Richter được tính bằng hàm logarit: M = log10(A/A0). Một trận động đất mạnh 6 độ Richter có biên độ sóng địa chấn lớn gấp 10 lần trận động đất 5 độ Richter, và giải phóng năng lượng lớn gấp khoảng 32 lần." 
                            : "The Richter magnitude of an earthquake is calculated logarithmically: M = log10(A/A0). A magnitude 6 earthquake has seismic wave amplitude 10 times larger and releases about 32 times more energy than a magnitude 5 one."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧪",
                        Title = isVN ? "Độ axit dung dịch pH" : "pH Acidity Level",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_logarithm_3_{suffix}.png",
                        Description = isVN 
                            ? "Độ axit hoặc kiềm của dung dịch được xác định bằng nồng độ ion H+ theo hàm logarit âm cơ số 10: pH = -log10[H+]. Nước cất có pH = 7, trong khi chanh có pH = 2 (axit mạnh hơn nước cất 100.000 lần)." 
                            : "The acidity or alkalinity of a solution is determined by the hydrogen ion concentration using negative base-10 logarithm: pH = -log10[H+]. Distilled water has pH = 7, while lemon juice has pH = 2 (10^5 times more acidic)."
                    }
                };

                if (practicalAppViewer != null) practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for LogarithmTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewCalc == null || viewTable == null || viewFormula == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewCalc.Visibility = Visibility.Collapsed;
            viewTable.Visibility = Visibility.Collapsed;
            viewFormula.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewCalc.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewTable.Visibility = Visibility.Visible;
                    break;
                case 3:
                    viewFormula.Visibility = Visibility.Visible;
                    break;
                case 4:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}

