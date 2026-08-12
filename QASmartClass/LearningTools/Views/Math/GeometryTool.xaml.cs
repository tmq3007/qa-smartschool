using System;



using QASmartClass.LearningTools.Helpers;



using System.Collections.Generic;



using System.Windows;



using System.Windows.Controls;



using System.Windows.Input;



using System.Windows.Media;



using QASmartClass.LearningTools.Controls;



using QASmartClass.LearningTools.Models;







namespace QASmartClass.LearningTools.Views.Math



{



    public partial class GeometryTool : BaseToolControl



    {



        private string _selectedShape = "";



        private readonly Dictionary<string, TextBox> _inputBoxes = new();







        public GeometryTool()



        {



            InitializeComponent();



            Loaded += (_, _) => {



                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";



                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn sử dụng" : "User Guide";



                if (menuTextPractice != null) menuTextPractice.Text = isVN ? "Tính toán & Đồ thị" : "Calculation & Graph";



                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";







                if (sideMenu != null)



                {



                    sideMenu.SelectionChanged += SideMenu_SelectionChanged;



                    sideMenu.SelectedIndex = 0;



                }







                BuildShapeList();



            };



        }







        // ═══════════════════════════════════════════════════════════



        //  SHAPE DEFINITIONS — 12 hình dạng



        // ═══════════════════════════════════════════════════════════







        private static readonly (string Id, string Icon, string Name, string Category,



            string[] Params, string[] ParamLabels, string FormulaText)[] Shapes =



        {



            // ── 2D ──



            ("square",      "⬜", "Hình vuông",       "2D",



                new[]{"a"},      new[]{"Cạnh a"},



                "S = a² • C = 4a • d = a√2"),







            ("rectangle",   "▬", "Hình chữ nhật",    "2D",



                new[]{"a","b"},  new[]{"Chiều dài a","Chiều rộng b"},



                "S = a×b • C = 2(a+b) • d = √(a²+b²)"),







            ("triangle",    "△", "Tam giác",          "2D",



                new[]{"a","h","b","c"}, new[]{"Đáy a","Chiều cao h","Cạnh b (tùy chọn)","Cạnh c (tùy chọn)"},



                "S = ½×a×h • C = a+b+c"),







            ("circle",      "●", "Hình tròn",         "2D",



                new[]{"r"},      new[]{"Bán kính r"},



                "S = πr² • C = 2πr"),







            ("trapezoid",   "⏢", "Hình thang",        "2D",



                new[]{"a","b","h"}, new[]{"Đáy lớn a","Đáy nhỏ b","Chiều cao h"},



                "S = ½(a+b)×h"),







            ("parallelogram","▰", "Hình bình hành",   "2D",



                new[]{"a","h"},  new[]{"Đáy a","Chiều cao h"},



                "S = a×h"),







            ("rhombus",     "◆", "Hình thoi",         "2D",



                new[]{"d1","d2"}, new[]{"Đường chéo d₁","Đường chéo d₂"},



                "S = ½×d₁×d₂"),







            ("ellipse",     "⬭", "Hình elip",         "2D",



                new[]{"a","b"},  new[]{"Bán trục a","Bán trục b"},



                "S = π×a×b • C ≈ π(3(a+b)-√((3a+b)(a+3b)))"),







            // ── 3D ──



            ("cube",        "🧊", "Hình lập phương",  "3D",



                new[]{"a"},      new[]{"Cạnh a"},



                "V = a³ • Sxq = 4a² • Stp = 6a² • d = a√3"),







            ("cuboid",      "📦", "Hình hộp chữ nhật","3D",



                new[]{"a","b","c"}, new[]{"Dài a","Rộng b","Cao c"},



                "V = a×b×c • Sxq = 2(a+b)c • Stp = 2(ab+bc+ac)"),







            ("sphere",      "🔴", "Hình cầu",         "3D",



                new[]{"r"},      new[]{"Bán kính r"},



                "V = 4πr³/3 • S = 4πr²"),







            ("cylinder",    "🧪", "Hình trụ",         "3D",



                new[]{"r","h"},  new[]{"Bán kính r","Chiều cao h"},



                "V = πr²h • Sxq = 2πrh • Stp = 2πr(r+h)"),







            ("cone",        "🔺", "Hình nón",          "3D",



                new[]{"r","h"},  new[]{"Bán kính r","Chiều cao h"},



                "V = πr²h/3 • Sxq = πrl (l=√(r²+h²))"),







            ("pyramid",     "🔻", "Hình chóp tứ giác đều",    "3D",



                new[]{"a","h"},  new[]{"Cạnh đáy a","Chiều cao h"},



                "V = a²h/3 • Sday = a² • Sxq = 2a×slant • Stp = Sxq + Sday"),



        };







        // ═══════════════════════════════════════════════════════════



        //  BUILD SHAPE LIST



        // ═══════════════════════════════════════════════════════════







        private void BuildShapeList()



        {



            shapeListPanel.Children.Clear();







            string lastCategory = "";



            foreach (var shape in Shapes)



            {



                // Category header



                if (shape.Category != lastCategory)



                {



                    lastCategory = shape.Category;



                    var header = new TextBlock



                    {



                        Text = shape.Category == "2D" ? "📐 Hình phẳng (2D)" : "📦 Hình không gian (3D)",



                        FontSize = 13, FontWeight = FontWeights.Bold,



                        Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),



                        Margin = new Thickness(8, shape.Category == "2D" ? 4 : 16, 0, 8)



                    };



                    shapeListPanel.Children.Add(header);



                }







                var captured = shape;



                var btn = new Border



                {



                    Background = Brushes.Transparent,



                    CornerRadius = new CornerRadius(8),



                    Padding = new Thickness(10, 7, 10, 7),



                    Margin = new Thickness(0, 1, 0, 1),



                    Cursor = Cursors.Hand,



                    Tag = shape.Id



                };







                var sp = new StackPanel { Orientation = Orientation.Horizontal };



                sp.Children.Add(new TextBlock



                {



                    Text = shape.Icon, FontSize = 16,



                    VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0)



                });



                sp.Children.Add(new TextBlock



                {



                    Text = shape.Name, FontSize = 14,



                    FontWeight = FontWeights.SemiBold,



                    VerticalAlignment = VerticalAlignment.Center,



                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))



                });



                btn.Child = sp;







                btn.MouseLeftButtonDown += (_, _) => SelectShape(captured);



                btn.MouseEnter += (_, _) =>



                {



                    if (captured.Id != _selectedShape)



                        btn.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));



                };



                btn.MouseLeave += (_, _) =>



                {



                    if (captured.Id != _selectedShape)



                        btn.Background = Brushes.Transparent;



                };







                shapeListPanel.Children.Add(btn);



            }



        }







        // ═══════════════════════════════════════════════════════════



        //  SELECT SHAPE — build input fields



        // ═══════════════════════════════════════════════════════════







        private void SelectShape((string Id, string Icon, string Name, string Category,



            string[] Params, string[] ParamLabels, string FormulaText) shape)



        {



            _selectedShape = shape.Id;



            _inputBoxes.Clear();







            if (welcomePanel != null) welcomePanel.Visibility = Visibility.Collapsed;



            if (contentCalc != null) contentCalc.Visibility = Visibility.Visible;







            txtShapeTitle.Text = $"{shape.Icon} {shape.Name}";



            txtShapeFormula.Text = $"Công thức: {shape.FormulaText}";



            txtFormulaIconText.Text = shape.Icon;







            inputPanel.Children.Clear();



            inputContainer.Visibility = Visibility.Visible;







            for (int i = 0; i < shape.Params.Length; i++)



            {



                var paramName = shape.Params[i];



                var label = shape.ParamLabels[i];







                var row = new Border



                {



                    Background = new SolidColorBrush(Color.FromRgb(245, 247, 250)),



                    CornerRadius = new CornerRadius(10),



                    Padding = new Thickness(16, 12, 16, 12),



                    Margin = new Thickness(0, 0, 0, 8)



                };







                var grid = new Grid();



                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });



                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });



                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });







                var labelStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };



                labelStack.Children.Add(new TextBlock



                {



                    Text = label, FontSize = 15, FontWeight = FontWeights.Bold,



                    Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50))



                });



                labelStack.Children.Add(new TextBlock



                {



                    Text = $"({paramName})", FontSize = 12, FontStyle = FontStyles.Italic,



                    Foreground = new SolidColorBrush(Color.FromRgb(100, 180, 100))



                });



                Grid.SetColumn(labelStack, 0);



                grid.Children.Add(labelStack);







                var tb = new TextBox



                {



                    FontSize = 18, Padding = new Thickness(10, 8, 10, 8),



                    FontFamily = new FontFamily("Segoe UI"),



                    FontWeight = FontWeights.Bold,



                    BorderBrush = new SolidColorBrush(Color.FromRgb(165, 214, 167)),



                    BorderThickness = new Thickness(0, 0, 0, 2),



                    Background = Brushes.Transparent,



                    VerticalContentAlignment = VerticalAlignment.Center,



                    Tag = paramName



                };



                tb.TextChanged += (_, _) => Calculate();



                tb.PreviewTextInput += InputBlock_PreviewTextInput;



                tb.PreviewKeyDown += InputBlock_PreviewKeyDown;



                DataObject.AddPastingHandler(tb, OnPaste);



                TouchNumPad.Attach(tb, step: 1, min: 0.001); // touch numpad cho màn hình tương tác



                Grid.SetColumn(tb, 1);



                grid.Children.Add(tb);







                var unitTb = new TextBlock



                {



                    Text = "đơn vị",



                    FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),



                    FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center,



                    Margin = new Thickness(8, 0, 0, 0)



                };



                Grid.SetColumn(unitTb, 2);



                grid.Children.Add(unitTb);







                _inputBoxes[paramName] = tb;



                row.Child = grid;



                inputPanel.Children.Add(row);



            }







            // Highlight selected shape in list



            foreach (UIElement child in shapeListPanel.Children)



            {



                if (child is Border bd)



                {



                    var isSelected = (bd.Tag?.ToString() == shape.Id);



                    bd.Background = isSelected 



                        ? new SolidColorBrush(Color.FromRgb(200, 230, 201)) // #C8E6C9



                        : Brushes.Transparent;



                    bd.BorderBrush = isSelected



                        ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) // #4CAF50



                        : Brushes.Transparent;



                    bd.BorderThickness = new Thickness(isSelected ? 1 : 0);



                    



                    if (bd.Child is StackPanel sp && sp.Children.Count >= 2 && sp.Children[1] is TextBlock tb)



                    {



                        tb.Foreground = isSelected



                            ? new SolidColorBrush(Color.FromRgb(27, 94, 32)) // Dark green



                            : new SolidColorBrush(Color.FromRgb(33, 33, 33)); // Regular



                        tb.FontWeight = isSelected ? FontWeights.Bold : FontWeights.SemiBold;



                    }



                }



            }







            resultBorder.Visibility = Visibility.Collapsed;



            stepByStepBorder.Visibility = Visibility.Collapsed;



        }







        private void ResetAll_Click(object sender, MouseButtonEventArgs e)



        {



            _selectedShape = "";



            _inputBoxes.Clear();



            txtShapeTitle.Text = "Chọn một hình bên trái";



            txtShapeFormula.Text = "14 hình phẳng + không gian";



            txtFormulaIconText.Text = "📐";



            inputPanel.Children.Clear();



            inputContainer.Visibility = Visibility.Collapsed;



            resultBorder.Visibility = Visibility.Collapsed;



            outputPanel.Children.Clear();



            stepByStepBorder.Visibility = Visibility.Collapsed;



            stepByStepPanel.Children.Clear();







            // Reset tab visibility and styles



            if (tabCalc != null && tabCalcText != null && tabGuide != null && tabGuideText != null && tabApp != null && tabAppText != null)



            {



                tabCalc.Background = new SolidColorBrush(Color.FromRgb(46, 125, 50)); // #2E7D32



                tabCalcText.Foreground = Brushes.White;



                tabGuide.Background = Brushes.Transparent;



                tabGuideText.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));



                tabApp.Background = Brushes.Transparent;



                tabAppText.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));







                if (welcomePanel != null) welcomePanel.Visibility = Visibility.Visible;



                contentCalc.Visibility = Visibility.Collapsed;



                contentGuide.Visibility = Visibility.Collapsed;



                contentApp.Visibility = Visibility.Collapsed;



            }







            foreach (UIElement child in shapeListPanel.Children)



            {



                if (child is Border bd)



                {



                    bd.Background = Brushes.Transparent;



                    bd.BorderBrush = Brushes.Transparent;



                    bd.BorderThickness = new Thickness(0);



                    if (bd.Child is StackPanel sp && sp.Children.Count >= 2 && sp.Children[1] is TextBlock tb)



                    {



                        tb.Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33));



                        tb.FontWeight = FontWeights.SemiBold;



                    }



                }



            }



        }







        // ═══════════════════════════════════════════════════════════



        //  CALCULATE — tính toán theo hình



        // ═══════════════════════════════════════════════════════════







        private void Calculate()



        {



            UpdateInputFieldsState();



            outputPanel.Children.Clear();



            stepByStepPanel.Children.Clear();







            var vals = new Dictionary<string, double>();



            foreach (var (key, box) in _inputBoxes)



            {



                if (ParsingHelper.TryParseDouble(box.Text, out double v) && v > 0)



                    vals[key] = v;



            }







            // Need at least required params



            if (vals.Count == 0)



            {



                resultBorder.Visibility = Visibility.Collapsed;



                stepByStepBorder.Visibility = Visibility.Collapsed;



                return;



            }







            var results = new List<(string Label, string Value, string Unit)>();



            var stepTexts = new List<string>();







            switch (_selectedShape)



            {



                case "square" when vals.ContainsKey("a"):



                {



                    double a = vals["a"];



                    results.Add(("Diện tích", $"{a * a:G6}", "đơn vị²"));



                    results.Add(("Chu vi", $"{4 * a:G6}", "đơn vị"));



                    results.Add(("Đường chéo", $"{a * System.Math.Sqrt(2):G6}", "đơn vị"));







                    stepTexts.Add($"• Diện tích: S = a² = {a}² = {a * a:G6} (đơn vị²)");



                    stepTexts.Add($"• Chu vi: C = 4 × a = 4 × {a} = {4 * a:G6} (đơn vị)");



                    stepTexts.Add($"• Đường chéo: d = a√2 = {a} × 1.4142... = {a * System.Math.Sqrt(2):G6} (đơn vị)");



                    break;



                }



                case "rectangle" when vals.ContainsKey("a") && vals.ContainsKey("b"):



                {



                    double a = vals["a"], b = vals["b"];



                    results.Add(("Diện tích", $"{a * b:G6}", "đơn vị²"));



                    results.Add(("Chu vi", $"{2 * (a + b):G6}", "đơn vị"));



                    results.Add(("Đường chéo", $"{System.Math.Sqrt(a * a + b * b):G6}", "đơn vị"));







                    stepTexts.Add($"• Diện tích: S = a × b = {a} × {b} = {a * b:G6} (đơn vị²)");



                    stepTexts.Add($"• Chu vi: C = 2 × (a + b) = 2 × ({a} + {b}) = {2 * (a + b):G6} (đơn vị)");



                    stepTexts.Add($"• Đường chéo: d = √(a² + b²) = √({a}² + {b}²) = {System.Math.Sqrt(a * a + b * b):G6} (đơn vị)");



                    break;



                }



                case "triangle":



                {



                    if (vals.ContainsKey("a") && vals.ContainsKey("h"))



                    {



                        double a = vals["a"], h = vals["h"];



                        results.Add(("Diện tích", $"{0.5 * a * h:G6}", "đơn vị²"));



                        stepTexts.Add($"• Diện tích: S = ½ × a × h = ½ × {a} × {h} = {0.5 * a * h:G6} (đơn vị²)");







                        if (vals.ContainsKey("b") && vals.ContainsKey("c"))



                        {



                            double b = vals["b"], c = vals["c"];



                            if (a + b > c && a + c > b && b + c > a)



                            {



                                results.Add(("Chu vi", $"{a + b + c:G6}", "đơn vị"));



                                stepTexts.Add($"• Chu vi: C = a + b + c = {a} + {b} + {c} = {a + b + c:G6} (đơn vị)");



                            }



                            else



                            {



                                results.Add(("Cảnh báo", "Ba cạnh không tạo thành tam giác hợp lệ!", ""));



                            }



                        }



                    }



                    else if (vals.ContainsKey("a") && vals.ContainsKey("b") && vals.ContainsKey("c"))



                    {



                        double a = vals["a"], b = vals["b"], c = vals["c"];



                        if (a + b > c && a + c > b && b + c > a)



                        {



                            double p = (a + b + c) / 2.0;



                            double s = System.Math.Sqrt(p * (p - a) * (p - b) * (p - c));



                            double h = 2.0 * s / a;



                            results.Add(("Diện tích (Heron)", $"{s:G6}", "đơn vị²"));



                            results.Add(("Chu vi", $"{a + b + c:G6}", "đơn vị"));



                            results.Add(("Chiều cao h tương ứng đáy a", $"{h:G6}", "đơn vị"));







                            stepTexts.Add($"• Nửa chu vi: p = (a + b + c)/2 = ({a} + {b} + {c})/2 = {p:G6} (đơn vị)");



                            stepTexts.Add($"• Diện tích (Heron): S = √(p(p-a)(p-b)(p-c)) = √({p}({p}-{a})({p}-{b})({p}-{c})) = {s:G6} (đơn vị²)");



                            stepTexts.Add($"• Chu vi: C = a + b + c = {a} + {b} + {c} = {a + b + c:G6} (đơn vị)");



                            stepTexts.Add($"• Chiều cao h ứng với đáy a: h = 2S/a = (2 × {s:G6})/{a} = {h:G6} (đơn vị)");



                        }



                        else



                        {



                            results.Add(("Cảnh báo", "Ba cạnh không tạo thành tam giác hợp lệ!", ""));



                        }



                    }



                    break;



                }



                case "circle" when vals.ContainsKey("r"):



                {



                    double r = vals["r"];



                    results.Add(("Diện tích", $"{System.Math.PI * r * r:G6}", "đơn vị²"));



                    results.Add(("Chu vi", $"{2 * System.Math.PI * r:G6}", "đơn vị"));



                    results.Add(("Đường kính", $"{2 * r:G6}", "đơn vị"));







                    stepTexts.Add($"• Diện tích: S = π × r² = 3.14159... × {r}² = {System.Math.PI * r * r:G6} (đơn vị²)");



                    stepTexts.Add($"• Chu vi: C = 2 × π × r = 2 × 3.14159... × {r} = {2 * System.Math.PI * r:G6} (đơn vị)");



                    stepTexts.Add($"• Đường kính: d = 2 × r = 2 × {r} = {2 * r:G6} (đơn vị)");



                    break;



                }



                case "trapezoid" when vals.ContainsKey("a") && vals.ContainsKey("b") && vals.ContainsKey("h"):



                {



                    double a = vals["a"], b = vals["b"], h = vals["h"];



                    results.Add(("Diện tích", $"{0.5 * (a + b) * h:G6}", "đơn vị²"));







                    stepTexts.Add($"• Diện tích: S = ½ × (a + b) × h = ½ × ({a} + {b}) × {h} = {0.5 * (a + b) * h:G6} (đơn vị²)");



                    break;



                }



                case "parallelogram" when vals.ContainsKey("a") && vals.ContainsKey("h"):



                {



                    double a = vals["a"], h = vals["h"];



                    results.Add(("Diện tích", $"{a * h:G6}", "đơn vị²"));







                    stepTexts.Add($"• Diện tích: S = a × h = {a} × {h} = {a * h:G6} (đơn vị²)");



                    break;



                }



                case "rhombus" when vals.ContainsKey("d1") && vals.ContainsKey("d2"):



                {



                    double d1 = vals["d1"], d2 = vals["d2"];



                    results.Add(("Diện tích", $"{0.5 * d1 * d2:G6}", "đơn vị²"));



                    double side = System.Math.Sqrt(d1 * d1 / 4 + d2 * d2 / 4);



                    results.Add(("Cạnh", $"{side:G6}", "đơn vị"));



                    results.Add(("Chu vi", $"{4 * side:G6}", "đơn vị"));







                    stepTexts.Add($"• Diện tích: S = ½ × d₁ × d₂ = ½ × {d1} × {d2} = {0.5 * d1 * d2:G6} (đơn vị²)");



                    stepTexts.Add($"• Cạnh: a = √((d₁/2)² + (d₂/2)²) = √(({d1}/2)² + ({d2}/2)²) = {side:G6} (đơn vị)");



                    stepTexts.Add($"• Chu vi: C = 4 × a = 4 × {side:G6} = {4 * side:G6} (đơn vị)");



                    break;



                }



                case "ellipse" when vals.ContainsKey("a") && vals.ContainsKey("b"):



                {



                    double a = vals["a"], b = vals["b"];



                    results.Add(("Diện tích", $"{System.Math.PI * a * b:G6}", "đơn vị²"));



                    // Ramanujan approximation



                    double c = System.Math.PI * (3 * (a + b) - System.Math.Sqrt((3 * a + b) * (a + 3 * b)));



                    results.Add(("Chu vi (≈)", $"{c:G6}", "đơn vị"));







                    stepTexts.Add($"• Diện tích: S = π × a × b = 3.14159... × {a} × {b} = {System.Math.PI * a * b:G6} (đơn vị²)");



                    stepTexts.Add($"• Chu vi (Ramanujan): C ≈ π × [3(a+b) - √((3a+b)(a+3b))] = {c:G6} (đơn vị)");



                    break;



                }



                case "cube" when vals.ContainsKey("a"):



                {



                    double a = vals["a"];



                    results.Add(("Thể tích", $"{a * a * a:G6}", "đơn vị³"));



                    results.Add(("Diện tích xung quanh", $"{4 * a * a:G6}", "đơn vị²"));



                    results.Add(("Diện tích toàn phần", $"{6 * a * a:G6}", "đơn vị²"));



                    results.Add(("Đường chéo", $"{a * System.Math.Sqrt(3):G6}", "đơn vị"));







                    stepTexts.Add($"• Thể tích: V = a³ = {a}³ = {a * a * a:G6} (đơn vị³)");



                    stepTexts.Add($"• Diện tích xung quanh: Sxq = 4 × a² = 4 × {a}² = {4 * a * a:G6} (đơn vị²)");



                    stepTexts.Add($"• Diện tích toàn phần: Stp = 6 × a² = 6 × {a}² = {6 * a * a:G6} (đơn vị²)");



                    stepTexts.Add($"• Đường chéo: d = a√3 = {a} × 1.732... = {a * System.Math.Sqrt(3):G6} (đơn vị)");



                    break;



                }



                case "cuboid" when vals.ContainsKey("a") && vals.ContainsKey("b") && vals.ContainsKey("c"):



                {



                    double a = vals["a"], b = vals["b"], c = vals["c"];



                    results.Add(("Thể tích", $"{a * b * c:G6}", "đơn vị³"));



                    results.Add(("Diện tích xung quanh", $"{2 * (a + b) * c:G6}", "đơn vị²"));



                    results.Add(("Diện tích toàn phần", $"{2 * (a * b + b * c + a * c):G6}", "đơn vị²"));



                    results.Add(("Đường chéo", $"{System.Math.Sqrt(a * a + b * b + c * c):G6}", "đơn vị"));







                    stepTexts.Add($"• Thể tích: V = a × b × c = {a} × {b} × {c} = {a * b * c:G6} (đơn vị³)");



                    stepTexts.Add($"• Diện tích xung quanh: Sxq = 2 × (a + b) × c = 2 × ({a} + {b}) × {c} = {2 * (a + b) * c:G6} (đơn vị²)");



                    stepTexts.Add($"• Diện tích toàn phần: Stp = 2 × (ab + bc + ac) = 2 × ({a}×{b} + {b}×{c} + {a}×{c}) = {2 * (a * b + b * c + a * c):G6} (đơn vị²)");



                    stepTexts.Add($"• Đường chéo: d = √(a² + b² + c²) = √({a}² + {b}² + {c}²) = {System.Math.Sqrt(a * a + b * b + c * c):G6} (đơn vị)");



                    break;



                }



                case "sphere" when vals.ContainsKey("r"):



                {



                    double r = vals["r"];



                    results.Add(("Thể tích", $"{4.0 / 3 * System.Math.PI * r * r * r:G6}", "đơn vị³"));



                    results.Add(("Diện tích mặt cầu", $"{4 * System.Math.PI * r * r:G6}", "đơn vị²"));







                    stepTexts.Add($"• Thể tích: V = 4/3 × π × r³ = 4/3 × 3.14159... × {r}³ = {4.0 / 3.0 * System.Math.PI * r * r * r:G6} (đơn vị³)");



                    stepTexts.Add($"• Diện tích mặt cầu: S = 4 × π × r² = 4 × 3.14159... × {r}² = {4 * System.Math.PI * r * r:G6} (đơn vị²)");



                    break;



                }



                case "cylinder" when vals.ContainsKey("r") && vals.ContainsKey("h"):



                {



                    double r = vals["r"], h = vals["h"];



                    results.Add(("Thể tích", $"{System.Math.PI * r * r * h:G6}", "đơn vị³"));



                    results.Add(("Diện tích xung quanh", $"{2 * System.Math.PI * r * h:G6}", "đơn vị²"));



                    results.Add(("Diện tích toàn phần", $"{2 * System.Math.PI * r * (r + h):G6}", "đơn vị²"));







                    stepTexts.Add($"• Thể tích: V = π × r² × h = 3.14159... × {r}² × {h} = {System.Math.PI * r * r * h:G6} (đơn vị³)");



                    stepTexts.Add($"• Diện tích xung quanh: Sxq = 2 × π × r × h = 2 × 3.14159... × {r} × {h} = {2 * System.Math.PI * r * h:G6} (đơn vị²)");



                    stepTexts.Add($"• Diện tích toàn phần: Stp = 2 × π × r × (r + h) = 2 × 3.14159... × {r} × ({r} + {h}) = {2 * System.Math.PI * r * (r + h):G6} (đơn vị²)");



                    break;



                }



                case "cone" when vals.ContainsKey("r") && vals.ContainsKey("h"):



                {



                    double r = vals["r"], h = vals["h"];



                    double l = System.Math.Sqrt(r * r + h * h);



                    results.Add(("Thể tích", $"{System.Math.PI * r * r * h / 3:G6}", "đơn vị³"));



                    results.Add(("Đường sinh l", $"{l:G6}", "đơn vị"));



                    results.Add(("Diện tích xung quanh", $"{System.Math.PI * r * l:G6}", "đơn vị²"));



                    results.Add(("Diện tích toàn phần", $"{System.Math.PI * r * (r + l):G6}", "đơn vị²"));







                    stepTexts.Add($"• Thể tích: V = ⅓ × π × r² × h = ⅓ × 3.14159... × {r}² × {h} = {System.Math.PI * r * r * h / 3.0:G6} (đơn vị³)");



                    stepTexts.Add($"• Đường sinh: l = √(r² + h²) = √({r}² + {h}²) = {l:G6} (đơn vị)");



                    stepTexts.Add($"• Diện tích xung quanh: Sxq = π × r × l = 3.14159... × {r} × {l:G6} = {System.Math.PI * r * l:G6} (đơn vị²)");



                    stepTexts.Add($"• Diện tích toàn phần: Stp = π × r × (r + l) = 3.14159... × {r} × ({r} + {l:G6}) = {System.Math.PI * r * (r + l):G6} (đơn vị²)");



                    break;



                }



                case "pyramid" when vals.ContainsKey("a") && vals.ContainsKey("h"):



                {



                    double a = vals["a"], h = vals["h"];



                    double slant = System.Math.Sqrt(h * h + a * a / 4.0);



                    double sday = a * a;



                    double sxq = 2.0 * a * slant;



                    double stp = sxq + sday;



                    results.Add(("Thể tích", $"{a * a * h / 3.0:G6}", "đơn vị³"));



                    results.Add(("Diện tích đáy", $"{sday:G6}", "đơn vị²"));



                    results.Add(("Trung đoạn (chiều cao mặt bên)", $"{slant:G6}", "đơn vị"));



                    results.Add(("Diện tích xung quanh", $"{sxq:G6}", "đơn vị²"));



                    results.Add(("Diện tích toàn phần", $"{stp:G6}", "đơn vị²"));







                    stepTexts.Add($"• Thể tích: V = ⅓ × a² × h = ⅓ × {a}² × {h} = {a * a * h / 3.0:G6} (đơn vị³)");



                    stepTexts.Add($"• Diện tích đáy: Sđáy = a² = {a}² = {sday:G6} (đơn vị²)");



                    stepTexts.Add($"• Trung đoạn: slant = √(h² + (a/2)²) = √({h}² + ({a}/2)²) = {slant:G6} (đơn vị)");



                    stepTexts.Add($"• Diện tích xung quanh: Sxq = 2 × a × slant = 2 × {a} × {slant:G6} = {sxq:G6} (đơn vị²)");



                    stepTexts.Add($"• Diện tích toàn phần: Stp = Sxq + Sđáy = {sxq:G6} + {sday:G6} = {stp:G6} (đơn vị²)");



                    break;



                }



                default:



                    resultBorder.Visibility = Visibility.Collapsed;



                    stepByStepBorder.Visibility = Visibility.Collapsed;



                    return;



            }







            // Render results



            if (results.Count > 0)



            {



                resultBorder.Visibility = Visibility.Visible;



                outputPanel.Children.Add(new TextBlock



                {



                    Text = "📊 Kết quả tính toán",



                    FontSize = 15, FontWeight = FontWeights.Bold,



                    Foreground = new SolidColorBrush(Color.FromRgb(27, 94, 32)),



                    Margin = new Thickness(0, 0, 0, 12)



                });







                foreach (var (label, value, unit) in results)



                {



                    bool isWarning = label == "Cảnh báo";



                    var row = new Border



                    {



                        Background = Brushes.White,



                        CornerRadius = new CornerRadius(8),



                        Padding = new Thickness(12, 8, 12, 8),



                        Margin = new Thickness(0, 0, 0, 6),



                        Cursor = Cursors.Hand,



                        ToolTip = isWarning ? null : "Nhấn để sao chép"



                    };







                    var dp = new DockPanel();



                    if (isWarning)



                    {



                        var warnBlock = new TextBlock



                        {



                            Text = $"⚠️ {value}",



                            FontSize = 13,



                            FontWeight = FontWeights.Bold,



                            Foreground = Brushes.Red,



                            TextWrapping = TextWrapping.Wrap,



                            VerticalAlignment = VerticalAlignment.Center



                        };



                        dp.Children.Add(warnBlock);



                    }



                    else



                    {



                        dp.Children.Add(new TextBlock



                        {



                            Text = label, FontSize = 13, FontWeight = FontWeights.SemiBold,



                            Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),



                            VerticalAlignment = VerticalAlignment.Center



                        });







                        string displayVal = string.IsNullOrEmpty(unit) ? value : $"{value} {unit}";



                        var valBlock = new TextBlock



                        {



                            Text = displayVal,



                            FontSize = 15, FontWeight = FontWeights.Bold,



                            FontFamily = new FontFamily("Segoe UI"),



                            Foreground = new SolidColorBrush(Color.FromRgb(27, 94, 32)),



                            HorizontalAlignment = HorizontalAlignment.Right,



                            VerticalAlignment = VerticalAlignment.Center



                        };



                        DockPanel.SetDock(valBlock, Dock.Right);



                        dp.Children.Add(valBlock);







                        var copyIcon = new TextBlock



                        {



                            Text = " 📋",



                            FontSize = 13,



                            Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120)),



                            VerticalAlignment = VerticalAlignment.Center,



                            Margin = new Thickness(4, 0, 0, 0)



                        };



                        DockPanel.SetDock(copyIcon, Dock.Right);



                        dp.Children.Add(copyIcon);



                    }







                    row.Child = dp;







                    if (!isWarning)



                    {



                        string copyVal = value;



                        row.MouseLeftButtonDown += (_, _) =>



                        {



                            try



                            {



                                Clipboard.SetText(copyVal);



                                row.Background = new SolidColorBrush(Color.FromRgb(200, 230, 201));



                                var timer = new System.Windows.Threading.DispatcherTimer



                                {



                                    Interval = TimeSpan.FromMilliseconds(500)



                                };



                                timer.Tick += (_, _) =>



                                {



                                    row.Background = Brushes.White;



                                    timer.Stop();



                                };



                                timer.Start();



                            }



                            catch { }



                        };



                    }







                    outputPanel.Children.Add(row);



                }



            }







            // Render step-by-step



            bool hasWarningMsg = results.Exists(r => r.Label == "Cảnh báo");



            if (stepTexts.Count > 0 && !hasWarningMsg)



            {



                stepByStepBorder.Visibility = Visibility.Visible;



                foreach (var step in stepTexts)



                {



                    stepByStepPanel.Children.Add(new TextBlock



                    {



                        Text = step,



                        FontSize = 13,



                        Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50)),



                        Margin = new Thickness(0, 0, 0, 8),



                        TextWrapping = TextWrapping.Wrap



                    });



                }



            }



            else



            {



                stepByStepBorder.Visibility = Visibility.Collapsed;



            }



        }







        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)



        {



            try



            {



                if (string.IsNullOrEmpty(_selectedShape))



                {



                    MessageBox.Show("Vui lòng chọn một hình ở danh sách bên trái trước khi xem đồ thị.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);



                    return;



                }







                // Find shape name



                string shapeName = "";



                foreach (var shape in Shapes)



                {



                    if (shape.Id == _selectedShape)



                    {



                        shapeName = shape.Name;



                        break;



                    }



                }







                // Check if inputs are valid for the selected shape



                bool inputsValid = true;



                if (_selectedShape == "triangle")



                {



                    double aSide = 0, bSide = 0, cSide = 0;



                    bool hasAH = _inputBoxes.ContainsKey("a") && ParsingHelper.TryParseDouble(_inputBoxes["a"].Text, out double aVal) && aVal > 0 &&



                                  _inputBoxes.ContainsKey("h") && ParsingHelper.TryParseDouble(_inputBoxes["h"].Text, out double hVal) && hVal > 0;



                    bool hasABC = _inputBoxes.ContainsKey("a") && ParsingHelper.TryParseDouble(_inputBoxes["a"].Text, out aSide) && aSide > 0 &&



                                   _inputBoxes.ContainsKey("b") && ParsingHelper.TryParseDouble(_inputBoxes["b"].Text, out bSide) && bSide > 0 &&



                                   _inputBoxes.ContainsKey("c") && ParsingHelper.TryParseDouble(_inputBoxes["c"].Text, out cSide) && cSide > 0;



                    



                    if (hasABC)



                    {



                        if (aSide + bSide <= cSide || aSide + cSide <= bSide || bSide + cSide <= aSide)



                        {



                            MessageBox.Show("Ba cạnh a, b, c vi phạm bất đẳng thức tam giác! Vui lòng sửa lại kích thước trước khi vẽ đồ thị.", "Lỗi hình học", MessageBoxButton.OK, MessageBoxImage.Warning);



                            return;



                        }



                    }



                    else if (!hasAH)



                    {



                        inputsValid = false;



                    }



                }



                else



                {



                    foreach (var (key, box) in _inputBoxes)



                    {



                        if (!ParsingHelper.TryParseDouble(box.Text, out double val) || val <= 0)



                        {



                            inputsValid = false;



                            break;



                        }



                    }



                }







                if (!inputsValid)



                {



                    MessageBox.Show($"Vui lòng nhập đầy đủ các kích thước là số thực dương hợp lệ cho hình \"{shapeName}\" trước khi xem đồ thị.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);



                    return;



                }







                var ci = System.Globalization.CultureInfo.InvariantCulture;



                string expJs;



                string title;







                switch (_selectedShape)



                {



                    case "circle":



                    {



                        ParsingHelper.TryParseDouble(_inputBoxes["r"].Text, out double r);



                        string rS = r.ToString(ci);



                        expJs = $@"



        calc.setExpression({{id:'c', latex:'x^2+y^2={rS}^2', color:'#2E7D32', lineWidth:3}});



        calc.setExpression({{id:'ct', latex:'(0,0)', color:'#E53935', pointSize:8, label:'O', showLabel:true}});



        calc.setMathBounds({{left:{(-r * 1.5).ToString(ci)},right:{(r * 1.5).ToString(ci)},bottom:{(-r * 1.5).ToString(ci)},top:{(r * 1.5).ToString(ci)}}});";



                        title = $"● Hình tròn r={r}";



                        break;



                    }



                    case "ellipse":



                    {



                        ParsingHelper.TryParseDouble(_inputBoxes["a"].Text, out double ea);



                        ParsingHelper.TryParseDouble(_inputBoxes["b"].Text, out double eb);



                        string aS = ea.ToString(ci), bS = eb.ToString(ci);



                        double mx = ea * 1.4, my = eb * 1.4;



                        expJs = $@"



        calc.setExpression({{id:'e', latex:'x^2/{aS}^2+y^2/{bS}^2=1', color:'#7B1FA2', lineWidth:3}});



        calc.setExpression({{id:'ax', latex:'y=0', color:'#BDBDBD', lineWidth:1, lineStyle:'DASHED'}});



        calc.setExpression({{id:'ay', latex:'x=0', color:'#BDBDBD', lineWidth:1, lineStyle:'DASHED'}});



        calc.setMathBounds({{left:{(-mx).ToString(ci)},right:{mx.ToString(ci)},bottom:{(-my).ToString(ci)},top:{my.ToString(ci)}}});";



                        title = $"⬭ Elip a={ea}, b={eb}";



                        break;



                    }



                    case "square":



                    {



                        ParsingHelper.TryParseDouble(_inputBoxes["a"].Text, out double sa);



                        string aS = sa.ToString(ci), ha = (sa / 2).ToString(ci), neg = (-sa / 2).ToString(ci);



                        expJs = $@"



        calc.setExpression({{id:'s1', latex:'\\operatorname{{polygon}}(({neg},{neg}), ({ha},{neg}), ({ha},{ha}), ({neg},{ha}))', color:'#2E7D32', lineWidth:3, fillOpacity:0.1}});



        calc.setMathBounds({{left:{(-sa).ToString(ci)},right:{sa.ToString(ci)},bottom:{(-sa).ToString(ci)},top:{sa.ToString(ci)}}});";



                        title = $"⬜ Hình vuông a={sa}";



                        break;



                    }



                    case "rectangle":



                    {



                        ParsingHelper.TryParseDouble(_inputBoxes["a"].Text, out double ra);



                        ParsingHelper.TryParseDouble(_inputBoxes["b"].Text, out double rb);



                        string ha = (ra / 2).ToString(ci), hb = (rb / 2).ToString(ci), nha = (-ra / 2).ToString(ci), nhb = (-rb / 2).ToString(ci);



                        expJs = $@"



        calc.setExpression({{id:'r1', latex:'\\operatorname{{polygon}}(({nha},{nhb}), ({ha},{nhb}), ({ha},{hb}), ({nha},{hb}))', color:'#1565C0', lineWidth:3, fillOpacity:0.1}});



        calc.setMathBounds({{left:{(-ra).ToString(ci)},right:{ra.ToString(ci)},bottom:{(-rb).ToString(ci)},top:{rb.ToString(ci)}}});";



                        title = $"▬ Hình chữ nhật {ra}×{rb}";



                        break;



                    }



                    case "triangle":



                    {



                        double ta = 0, tb = 0, tc = 0, th = 0;



                        bool hasABC = _inputBoxes.ContainsKey("a") && ParsingHelper.TryParseDouble(_inputBoxes["a"].Text, out ta) && ta > 0 &&



                                       _inputBoxes.ContainsKey("b") && ParsingHelper.TryParseDouble(_inputBoxes["b"].Text, out tb) && tb > 0 &&



                                       _inputBoxes.ContainsKey("c") && ParsingHelper.TryParseDouble(_inputBoxes["c"].Text, out tc) && tc > 0;



                        if (hasABC)



                        {



                            // Plot triangle exactly using sides a, b, c



                            double x_A = (ta * ta + tc * tc - tb * tb) / (2.0 * ta);



                            double y_A = System.Math.Sqrt(tc * tc - x_A * x_A);



                            string sX_A = x_A.ToString(ci), sY_A = y_A.ToString(ci), sTa = ta.ToString(ci);



                            expJs = $@"



        calc.setExpression({{id:'t1', latex:'\\operatorname{{polygon}}((0,0), ({sTa},0), ({sX_A},{sY_A}))', color:'#E65100', lineWidth:3, fillOpacity:0.1}});



        calc.setMathBounds({{left:{(-ta * 0.3).ToString(ci)},right:{(ta * 1.3).ToString(ci)},bottom:{(-y_A * 0.3).ToString(ci)},top:{(y_A * 1.3).ToString(ci)}}});";



                            title = $"△ Tam giác a={ta}, b={tb}, c={tc}";



                        }



                        else



                        {



                            ParsingHelper.TryParseDouble(_inputBoxes["a"].Text, out ta);



                            ParsingHelper.TryParseDouble(_inputBoxes["h"].Text, out th);



                            string ha = (ta / 2).ToString(ci), nha = (-ta / 2).ToString(ci), s_h = th.ToString(ci);



                            expJs = $@"



        calc.setExpression({{id:'t1', latex:'\\operatorname{{polygon}}(({nha},0), ({ha},0), (0,{s_h}))', color:'#E65100', lineWidth:3, fillOpacity:0.1}});



        calc.setMathBounds({{left:{(-ta).ToString(ci)},right:{ta.ToString(ci)},bottom:{(-th * 0.5).ToString(ci)},top:{(th * 1.5).ToString(ci)}}});";



                            title = $"△ Tam giác a={ta}, h={th}";



                        }



                        break;



                    }



                    case "trapezoid":



                    {



                        ParsingHelper.TryParseDouble(_inputBoxes["a"].Text, out double tra);



                        ParsingHelper.TryParseDouble(_inputBoxes["b"].Text, out double trb);



                        ParsingHelper.TryParseDouble(_inputBoxes["h"].Text, out double trh);



                        string ha = (tra / 2).ToString(ci), nha = (-tra / 2).ToString(ci), hb = (trb / 2).ToString(ci), nhb = (-trb / 2).ToString(ci), s_h = trh.ToString(ci);



                        expJs = $@"



        calc.setExpression({{id:'t1', latex:'\\operatorname{{polygon}}(({nha},0), ({ha},0), ({hb},{s_h}), ({nhb},{s_h}))', color:'#7B1FA2', lineWidth:3, fillOpacity:0.1}});



        calc.setMathBounds({{left:{(-System.Math.Max(tra, trb)).ToString(ci)},right:{System.Math.Max(tra, trb).ToString(ci)},bottom:{(-trh * 0.5).ToString(ci)},top:{(trh * 1.5).ToString(ci)}}});";



                        title = $"⏢ Hình thang a={tra}, b={trb}, h={trh}";



                        break;



                    }



                    case "parallelogram":



                    {



                        ParsingHelper.TryParseDouble(_inputBoxes["a"].Text, out double pla);



                        ParsingHelper.TryParseDouble(_inputBoxes["h"].Text, out double plh);



                        string p3x = (pla + plh * 0.3).ToString(ci), p4x = (plh * 0.3).ToString(ci), p2x = pla.ToString(ci), s_h = plh.ToString(ci);



                        expJs = $@"



        calc.setExpression({{id:'p1', latex:'\\operatorname{{polygon}}((0,0), ({p2x},0), ({p3x},{s_h}), ({p4x},{s_h}))', color:'#1565C0', lineWidth:3, fillOpacity:0.1}});



        calc.setMathBounds({{left:{(-pla * 0.5).ToString(ci)},right:{(pla * 1.5 + plh * 0.3).ToString(ci)},bottom:{(-plh * 0.5).ToString(ci)},top:{(plh * 1.5).ToString(ci)}}});";



                        title = $"▰ Hình bình hành a={pla}, h={plh}";



                        break;



                    }



                    case "rhombus":



                    {



                        ParsingHelper.TryParseDouble(_inputBoxes["d1"].Text, out double rd1);



                        ParsingHelper.TryParseDouble(_inputBoxes["d2"].Text, out double rd2);



                        string h1 = (rd1 / 2).ToString(ci), nh1 = (-rd1 / 2).ToString(ci), h2 = (rd2 / 2).ToString(ci), nh2 = (-rd2 / 2).ToString(ci);



                        expJs = $@"



        calc.setExpression({{id:'r1', latex:'\\operatorname{{polygon}}(({nh1},0), (0,{h2}), ({h1},0), (0,{nh2}))', color:'#E65100', lineWidth:3, fillOpacity:0.1}});



        calc.setMathBounds({{left:{(-rd1).ToString(ci)},right:{rd1.ToString(ci)},bottom:{(-rd2).ToString(ci)},top:{rd2.ToString(ci)}}});";



                        title = $"◆ Hình thoi d1={rd1}, d2={rd2}";



                        break;



                    }



                    case "cube":



                    {



                        ParsingHelper.TryParseDouble(_inputBoxes["a"].Text, out double cba);



                        string s = cba.ToString(ci), dx = (cba * 0.5).ToString(ci), s_dx = (cba * 1.5).ToString(ci);



                        expJs = $@"



        calc.setExpression({{id:'c1', latex:'\\operatorname{{polygon}}((0,0), ({s},0), ({s},{s}), (0,{s}), (0,0), ({dx},{dx}), ({s_dx},{dx}), ({s_dx},{s_dx}), ({dx},{s_dx}), ({dx},{dx}), ({dx},{s_dx}), (0,{s}), ({s},{s}), ({s_dx},{s_dx}), ({s_dx},{dx}), ({s},0))', color:'#1565C0', lineWidth:3, fillOpacity:0}});



        calc.setMathBounds({{left:{(-cba*0.5).ToString(ci)},right:{(cba*2).ToString(ci)},bottom:{(-cba*0.5).ToString(ci)},top:{(cba*2).ToString(ci)}}});";



                        title = $"🧊 Hình lập phương a={cba}";



                        break;



                    }



                    case "cuboid":



                    {



                        ParsingHelper.TryParseDouble(_inputBoxes["a"].Text, out double cda);



                        ParsingHelper.TryParseDouble(_inputBoxes["b"].Text, out double cdb);



                        ParsingHelper.TryParseDouble(_inputBoxes["c"].Text, out double cdc);



                        string s_a = cda.ToString(ci), s_c = cdc.ToString(ci), dx = (cdb * 0.5).ToString(ci);



                        string a_dx = (cda + cdb * 0.5).ToString(ci), c_dx = (cdc + cdb * 0.5).ToString(ci);



                        expJs = $@"



        calc.setExpression({{id:'c1', latex:'\\operatorname{{polygon}}((0,0), ({s_a},0), ({s_a},{s_c}), (0,{s_c}), (0,0), ({dx},{dx}), ({a_dx},{dx}), ({a_dx},{c_dx}), ({dx},{c_dx}), ({dx},{dx}), ({dx},{c_dx}), (0,{s_c}), ({s_a},{s_c}), ({a_dx},{c_dx}), ({a_dx},{dx}), ({s_a},0))', color:'#1565C0', lineWidth:3, fillOpacity:0}});



        calc.setMathBounds({{left:{(-cdb*0.5).ToString(ci)},right:{(cda + cdb).ToString(ci)},bottom:{(-cdb*0.5).ToString(ci)},top:{(cdc + cdb).ToString(ci)}}});";



                        title = $"📦 Hình hộp chữ nhật {cda}×{cdb}×{cdc}";



                        break;



                    }



                    case "sphere":



                    {



                        ParsingHelper.TryParseDouble(_inputBoxes["r"].Text, out double spr);



                        string rS = spr.ToString(ci), r3 = (spr * 0.3).ToString(ci);



                        expJs = $@"



        calc.setExpression({{id:'s1', latex:'x^2+y^2={rS}^2', color:'#2E7D32', lineWidth:3, fillOpacity:0}});



        calc.setExpression({{id:'s2', latex:'x^2/{rS}^2+y^2/{r3}^2=1', color:'#2E7D32', lineWidth:2, lineStyle:'DASHED', fillOpacity:0}});



        calc.setMathBounds({{left:{(-spr * 1.5).ToString(ci)},right:{(spr * 1.5).ToString(ci)},bottom:{(-spr * 1.5).ToString(ci)},top:{(spr * 1.5).ToString(ci)}}});";



                        title = $"🔴 Hình cầu r={spr}";



                        break;



                    }



                    case "cylinder":



                    {



                        ParsingHelper.TryParseDouble(_inputBoxes["r"].Text, out double cyr);



                        ParsingHelper.TryParseDouble(_inputBoxes["h"].Text, out double cyh);



                        string rS = cyr.ToString(ci), hS = cyh.ToString(ci), r3 = (cyr * 0.3).ToString(ci), nr = (-cyr).ToString(ci);



                        expJs = $@"



        calc.setExpression({{id:'c1', latex:'x^2/{rS}^2+y^2/{r3}^2=1', color:'#1565C0', lineWidth:3, fillOpacity:0}});



        calc.setExpression({{id:'c2', latex:'x^2/{rS}^2+(y-{hS})^2/{r3}^2=1', color:'#1565C0', lineWidth:3, fillOpacity:0}});



        calc.setExpression({{id:'c3', latex:'\\operatorname{{polygon}}(({rS},0), ({rS},{hS}))', color:'#1565C0', lineWidth:3}});



        calc.setExpression({{id:'c4', latex:'\\operatorname{{polygon}}(({nr},0), ({nr},{hS}))', color:'#1565C0', lineWidth:3}});



        calc.setMathBounds({{left:{(-cyr * 1.5).ToString(ci)},right:{(cyr * 1.5).ToString(ci)},bottom:{(-cyr * 0.8).ToString(ci)},top:{(cyh + cyr * 0.8).ToString(ci)}}});";



                        title = $"🧪 Hình trụ r={cyr}, h={cyh}";



                        break;



                    }



                    case "cone":



                    {



                        ParsingHelper.TryParseDouble(_inputBoxes["r"].Text, out double cnr);



                        ParsingHelper.TryParseDouble(_inputBoxes["h"].Text, out double cnh);



                        string rS = cnr.ToString(ci), hS = cnh.ToString(ci), r3 = (cnr * 0.3).ToString(ci), nr = (-cnr).ToString(ci);



                        expJs = $@"



        calc.setExpression({{id:'c1', latex:'x^2/{rS}^2+y^2/{r3}^2=1', color:'#E65100', lineWidth:3, fillOpacity:0}});



        calc.setExpression({{id:'c2', latex:'\\operatorname{{polygon}}(({nr},0), (0,{hS}), ({rS},0))', color:'#E65100', lineWidth:3, fillOpacity:0}});



        calc.setMathBounds({{left:{(-cnr * 1.5).ToString(ci)},right:{(cnr * 1.5).ToString(ci)},bottom:{(-cnr * 0.8).ToString(ci)},top:{(cnh + cnr * 0.8).ToString(ci)}}});";



                        title = $"🔺 Hình nón r={cnr}, h={cnh}";



                        break;



                    }



                    case "pyramid":



                    {



                        ParsingHelper.TryParseDouble(_inputBoxes["a"].Text, out double pya);



                        ParsingHelper.TryParseDouble(_inputBoxes["h"].Text, out double pyh);



                        string s_a = pya.ToString(ci), dx = (pya * 0.5).ToString(ci), a_dx = (pya * 1.5).ToString(ci);



                        string tx = (pya * 0.75).ToString(ci), ty = (pyh + pya * 0.25).ToString(ci);



                        expJs = $@"



        calc.setExpression({{id:'p1', latex:'\\operatorname{{polygon}}((0,0), ({s_a},0), ({a_dx},{dx}), ({dx},{dx}))', color:'#7B1FA2', lineWidth:3, fillOpacity:0}});



        calc.setExpression({{id:'p2', latex:'\\operatorname{{polygon}}((0,0), ({tx},{ty}), ({s_a},0), ({tx},{ty}), ({a_dx},{dx}), ({tx},{ty}), ({dx},{dx}))', color:'#7B1FA2', lineWidth:3, fillOpacity:0}});



        calc.setMathBounds({{left:{(-pya*0.5).ToString(ci)},right:{(pya*2).ToString(ci)},bottom:{(-pya*0.5).ToString(ci)},top:{(pyh + pya*0.5).ToString(ci)}}});";



                        title = $"🔻 Hình chóp đều a={pya}, h={pyh}";



                        break;



                    }



                    default:



                    {



                        expJs = @"



        calc.setExpression({id:'c', latex:'x^2+y^2=25', color:'#2E7D32', lineWidth:3});



        calc.setMathBounds({left:-8,right:8,bottom:-6,top:6});";



                        title = "📏 Hình học trên tọa độ";



                        break;



                    }



                }







                var win = new GraphWindow(expJs, title);



                win.Owner = Window.GetWindow(this);



                win.Show();



            }



            catch (System.Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }



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







                private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)

        {

            if (rootGrid == null || sideMenu == null || contentGuide == null || contentCalc == null || viewPractical == null)

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

                // Only show contentCalc if a shape is currently selected

                if (_selectedShape != "")

                {

                    contentCalc.Visibility = Visibility.Visible;

                    if (welcomePanel != null) welcomePanel.Visibility = Visibility.Collapsed;

                }

                else

                {

                    if (welcomePanel != null) welcomePanel.Visibility = Visibility.Visible;

                }

            }

            else if (index == 2)

            {

                viewPractical.Visibility = Visibility.Visible;

                LoadPracticalApps();

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



                        Icon = "♟️",



                        Title = isVN ? "Bàn cờ vua" : "Chessboard",



                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_geometry_1_{suffix}.png",



                        Description = isVN 



                            ? "Bàn cờ vua gồm các ô vuông đen trắng xen kẽ nhau xếp thành lưới 8x8, là ví dụ điển hình về hình vuông trong thực tế." 



                            : "A chessboard consists of alternating black and white squares arranged in an 8x8 grid, a classic real-life example of squares."



                    },



                    new PracticalAppItem



                    {



                        Icon = "🎲",



                        Title = isVN ? "Khối Rubik" : "Rubik's Cube",



                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_geometry_2_{suffix}.png",



                        Description = isVN 



                            ? "Khối Rubik 3x3x3 gồm các hình lập phương nhỏ ghép lại với nhau, là mô hình trực quan sinh động của hình lập phương." 



                            : "A 3x3x3 Rubik's Cube composed of smaller cubes, serving as a vivid visual model of a cube."



                    },



                    new PracticalAppItem



                    {



                        Icon = "🥫",



                        Title = isVN ? "Lon nước ngọt" : "Soda Can",



                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_geometry_3_{suffix}.png",



                        Description = isVN 



                            ? "Lon nước ngọt được thiết kế dưới dạng hình trụ tròn giúp tối ưu hóa dung tích chứa và khả năng chịu lực nén tốt." 



                            : "A soda can designed as a cylinder to optimize storage capacity and compression resistance."



                    },



                    new PracticalAppItem



                    {



                        Icon = "🕒",



                        Title = isVN ? "Đồng hồ treo tường" : "Wall Clock",



                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_geometry_4_{suffix}.png",



                        Description = isVN 



                            ? "Mặt đồng hồ tròn được chia thành 12 phần bằng nhau tương ứng với 12 giờ, giúp đo lường thời gian trực quan." 



                            : "A circular clock face divided into 12 equal sectors representing the hours, providing visual time measurement."



                    },



                    new PracticalAppItem



                    {



                        Icon = "⚠️",



                        Title = isVN ? "Cột mốc giao thông" : "Traffic Cone",



                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_geometry_5_{suffix}.png",



                        Description = isVN 



                            ? "Cột mốc giao thông hình nón giúp tăng tính vững chãi, dễ nhìn thấy từ xa và có thể xếp chồng gọn gàng." 



                            : "A conical traffic cone that provides high stability, visibility from afar, and space-saving stackability."



                    },



                    new PracticalAppItem



                    {



                        Icon = "🌉",



                        Title = isVN ? "Kết cấu chịu lực cầu đường" : "Truss Structure",



                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_geometry_6_{suffix}.png",



                        Description = isVN 



                            ? "Sử dụng các hệ giàn tam giác liên kết chịu tải trọng lớn để xây dựng kết cấu khung thép cho cầu đường và nhà xưởng." 



                            : "Using interconnected triangular truss systems to distribute heavy loads for steel bridges and industrial roofs."



                    }



                };







                contentApp.SetItemsSource(items);



            }



            catch (Exception ex)



            {



                System.Diagnostics.Debug.WriteLine("Error loading practical apps: " + ex.Message);



            }



        }







        private void UpdateInputFieldsState()



        {



            if (_selectedShape == "triangle")



            {



                if (_inputBoxes.TryGetValue("h", out var boxH) &&



                    _inputBoxes.TryGetValue("b", out var boxB) &&



                    _inputBoxes.TryGetValue("c", out var boxC))



                {



                    bool hasH = !string.IsNullOrWhiteSpace(boxH.Text);



                    bool hasB = !string.IsNullOrWhiteSpace(boxB.Text);



                    bool hasC = !string.IsNullOrWhiteSpace(boxC.Text);







                    if (hasH)



                    {



                        boxB.IsEnabled = false;



                        boxC.IsEnabled = false;



                        boxB.Opacity = 0.5;



                        boxC.Opacity = 0.5;



                    }



                    else if (hasB || hasC)



                    {



                        boxH.IsEnabled = false;



                        boxH.Opacity = 0.5;



                    }



                    else



                    {



                        boxH.IsEnabled = true;



                        boxB.IsEnabled = true;



                        boxC.IsEnabled = true;



                        boxH.Opacity = 1.0;



                        boxB.Opacity = 1.0;



                        boxC.Opacity = 1.0;



                    }



                }



            }



        }



    }



}





