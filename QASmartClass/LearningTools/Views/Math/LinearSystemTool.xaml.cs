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
    public partial class LinearSystemTool : BaseToolControl
    {
        private Border tabCalc;
        private TextBlock tabCalcText;
        private Border tabGuide;
        private TextBlock tabGuideText;
        private Border tabApp;
        private TextBlock tabAppText;
        public LinearSystemTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextPractice != null) menuTextPractice.Text = isVN ? "Tính toán & Đồ thị" : "Calculation & Graph";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";

                if (sideMenu != null)
                {
                    sideMenu.SelectionChanged += SideMenu_SelectionChanged;
                    sideMenu.SelectedIndex = 0;
                }

                BuildPresets(); BuildTheory(); Solve(); LoadPracticalApps();
                // Bàn phím số mini
                TouchNumPad.Attach(txtA1, step: 1);
                TouchNumPad.Attach(txtB1, step: 1);
                TouchNumPad.Attach(txtC1, step: 1);
                TouchNumPad.Attach(txtA2, step: 1);
                TouchNumPad.Attach(txtB2, step: 1);
                TouchNumPad.Attach(txtC2, step: 1);

                // Show submit button if running inside StudentShell
                var win = Window.GetWindow(this);
                if (win != null && win.GetType().Name == "StudentShell")
                {
                    btnSubmit.Visibility = Visibility.Visible;
                }
            };
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ParsingHelper.TryParseDouble(txtA1.Text, out double a1)) return;
                if (!ParsingHelper.TryParseDouble(txtB1.Text, out double b1)) return;
                if (!ParsingHelper.TryParseDouble(txtC1.Text, out double c1)) return;
                if (!ParsingHelper.TryParseDouble(txtA2.Text, out double a2)) return;
                if (!ParsingHelper.TryParseDouble(txtB2.Text, out double b2)) return;
                if (!ParsingHelper.TryParseDouble(txtC2.Text, out double c2)) return;

                string system = $"Hệ PT: {{{a1}x + {b1}y = {c1}; {a2}x + {b2}y = {c2}}}";
                string result = txtResult.Text.Replace("\n", ", ");
                string data = $"{system} -> Kết quả: {result}";

                var win = Window.GetWindow(this);
                if (win != null && win.GetType().Name == "StudentShell")
                {
                    var method = win.GetType().GetMethod("SendToolSubmission");
                    if (method != null)
                    {
                        method.Invoke(win, new object[] { "linear_system", data });
                        MessageBox.Show("Đã gửi hệ phương trình và nghiệm của bạn lên máy giáo viên thành công!", "Nộp bài giải", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nộp bài: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  PRESETS
        // ═══════════════════════════════════════════════════════════

        private void BuildPresets()
        {
            if (presetPanel == null) return;
            presetPanel.Children.Clear();

            var presets = new (string Lbl, double A1, double B1, double C1, double A2, double B2, double C2, string Desc)[]
            {
                ("1 nghiệm",    2, 3, 8,   1, -1, 1,    "Hệ có nghiệm duy nhất"),
                ("Vô số",       1, 2, 3,   2, 4, 6,     "Hệ vô số nghiệm"),
                ("Vô nghiệm",   1, 2, 3,   2, 4, 5,    "Hệ vô nghiệm"),
                ("Đơn giản",    1, 1, 5,   1, -1, 1,    "x+y=5, x-y=1"),
                ("Thập phân",   1.5, 2.5, 7.5, 0.5, -1.5, -1.5, "Hệ số thập phân"),
                ("Suy biến 1",  0, 0, 2,   1, 1, 3,     "Suy biến vô nghiệm (0=2)"),
                ("Suy biến 2",  0, 0, 0,   1, -1, 2,    "Suy biến vô số nghiệm (0=0)"),
                ("Hệ số lớn",   3, -2, 7,  5, 4, 23,    "Hệ số lớn"),
                ("Âm",         -1, 3, 4,   2, -1, 3,    "Có hệ số âm"),
            };

            var presetColor = DS.CatMath; // #1565C0
            foreach (var (lbl, a1, b1, c1, a2, b2, c2, desc) in presets)
            {
                double ca1 = a1, cb1 = b1, cc1 = c1, ca2 = a2, cb2 = b2, cc2 = c2;
                var btn = UI.PresetButton($"📌 {lbl}", presetColor, () =>
                {
                    txtA1.Text = ca1.ToString(); txtB1.Text = cb1.ToString(); txtC1.Text = cc1.ToString();
                    txtA2.Text = ca2.ToString(); txtB2.Text = cb2.ToString(); txtC2.Text = cc2.ToString();
                }, presetPanel);
                btn.ToolTip = $"{FormatEq(a1, b1, c1)}, {FormatEq(a2, b2, c2)} — {desc}";
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  SOLVER
        // ═══════════════════════════════════════════════════════════

        private void Solve_Changed(object sender, TextChangedEventArgs e) => Solve();

        private double _a1, _b1, _c1, _a2, _b2, _c2;

        private void Solve()
        {
            try
            {
                if (txtA1 == null || txtResult == null) return;
                if (!ParsingHelper.TryParseDouble(txtA1.Text, out _a1) || !ParsingHelper.TryParseDouble(txtB1.Text, out _b1) || !ParsingHelper.TryParseDouble(txtC1.Text, out _c1)) { txtResult.Text = "Nhập chưa hợp lệ"; return; }
                if (!ParsingHelper.TryParseDouble(txtA2.Text, out _a2) || !ParsingHelper.TryParseDouble(txtB2.Text, out _b2) || !ParsingHelper.TryParseDouble(txtC2.Text, out _c2)) { txtResult.Text = "Nhập chưa hợp lệ"; return; }

                // Cập nhật live preview hệ phương trình
                if (lblEq1Preview != null) lblEq1Preview.Text = FormatEq(_a1, _b1, _c1);
                if (lblEq2Preview != null) lblEq2Preview.Text = FormatEq(_a2, _b2, _c2);

                bool isEq1Invalid = (_a1 == 0 && _b1 == 0);
                bool isEq2Invalid = (_a2 == 0 && _b2 == 0);

                if (isEq1Invalid || isEq2Invalid)
                {
                    // Trường hợp có phương trình suy biến a = b = 0
                    if (txtDet != null) txtDet.Text = "Không xác định";
                    if (txtDetX != null) txtDetX.Text = "Dx = KXD";
                    if (txtDetY != null) txtDetY.Text = "Dy = KXD";

                    if ((isEq1Invalid && _c1 != 0) || (isEq2Invalid && _c2 != 0))
                    {
                        // Dạng 0x + 0y = c (c khác 0) => Vô lý => Vô nghiệm
                        txtResult.Text = "Hệ vô nghiệm";
                        txtClassify.Text = "❌ Hệ vô nghiệm (chứa phương trình vô lý)";
                        SetStyle("#FFEBEE", "#C62828");
                        BuildStepsInvalid(isEq1Invalid, isEq2Invalid);
                    }
                    else
                    {
                        // Các phương trình suy biến đều có dạng 0x + 0y = 0 (luôn đúng)
                        if (isEq1Invalid && isEq2Invalid)
                        {
                            // Cả hai phương trình đều là 0 = 0
                            txtResult.Text = "Hệ có vô số nghiệm";
                            txtClassify.Text = "♾️ Hệ có vô số nghiệm (mọi cặp x, y đều thỏa mãn)";
                            SetStyle("#FFF3E0", "#E65100");
                            BuildStepsAllZero();
                        }
                        else
                        {
                            // Một phương trình là 0 = 0, phương trình còn lại là ax + by = c hợp lệ
                            txtResult.Text = "Hệ có vô số nghiệm";
                            txtClassify.Text = "♾️ Hệ có vô số nghiệm (các điểm thuộc đường thẳng còn lại)";
                            SetStyle("#FFF3E0", "#E65100");
                            BuildStepsOneZero(isEq1Invalid ? 2 : 1);
                        }
                    }
                }
                else
                {
                    // Trường hợp hệ phương trình bình thường
                    double D = _a1 * _b2 - _a2 * _b1;
                    double Dx = _c1 * _b2 - _c2 * _b1;
                    double Dy = _a1 * _c2 - _a2 * _c1;

                    if (txtDet != null) txtDet.Text = $"D = a₁b₂ − a₂b₁ = {D:G8}";
                    if (txtDetX != null) txtDetX.Text = $"Dx = {Dx:G8}";
                    if (txtDetY != null) txtDetY.Text = $"Dy = {Dy:G8}";

                    if (D != 0)
                    {
                        double x = Dx / D;
                        double y = Dy / D;
                        txtResult.Text = $"x = {x:G8}\ny = {y:G8}";
                        txtClassify.Text = "✅ Hệ có nghiệm duy nhất — 2 đường thẳng cắt nhau tại 1 điểm";
                        SetStyle("#E8F5E9", "#1B5E20");
                        BuildSteps(D, Dx, Dy, x, y);
                    }
                    else
                    {
                        if (Dx == 0 && Dy == 0)
                        {
                            txtResult.Text = "Hệ có vô số nghiệm";
                            txtClassify.Text = "♾️ 2 đường thẳng trùng nhau — vô số nghiệm";
                            SetStyle("#FFF3E0", "#E65100");
                            BuildStepsSpecial("vô số nghiệm", D);
                        }
                        else
                        {
                            txtResult.Text = "Hệ vô nghiệm";
                            txtClassify.Text = "❌ 2 đường thẳng song song — không có giao điểm";
                            SetStyle("#FFEBEE", "#C62828");
                            BuildStepsSpecial("vô nghiệm", D);
                        }
                    }
                }
            }
            catch { }
        }

        private void BuildStepsInvalid(bool eq1Inv, bool eq2Inv)
        {
            if (stepsPanel == null) return;
            stepsPanel.Children.Clear();

            AddStepCard("Bước 1: Nhận diện hệ phương trình",
                $"⎧ {FormatEq(_a1, _b1, _c1)}\n⎩ {FormatEq(_a2, _b2, _c2)}", "#1565C0", "ls_inv_1");
            
            string reason = "";
            if (eq1Inv && _c1 != 0)
                reason += $"Phương trình 1 tương đương với: 0x + 0y = {_c1} (Vô lý vì 0 không thể bằng {_c1}).\n";
            if (eq2Inv && _c2 != 0)
                reason += $"Phương trình 2 tương đương với: 0x + 0y = {_c2} (Vô lý vì 0 không thể bằng {_c2}).\n";

            AddStepCard("Bước 2: Biện luận hệ số suy biến",
                reason + "Do hệ phương trình chứa mệnh đề toán học vô lý, không có cặp số (x; y) nào thỏa mãn hệ này.", 
                "#C62828", "ls_inv_2");
                
            AddStepCard("Kết luận",
                "Hệ phương trình VÔ NGHIỆM.", "#7B1FA2", "ls_inv_3");
        }

        private void BuildStepsAllZero()
        {
            if (stepsPanel == null) return;
            stepsPanel.Children.Clear();

            AddStepCard("Bước 1: Nhận diện hệ phương trình",
                $"⎧ 0x + 0y = 0\n⎩ 0x + 0y = 0", "#1565C0", "ls_allz_1");
            AddStepCard("Bước 2: Biện luận",
                "Cả hai phương trình đều có dạng đặc biệt: 0x + 0y = 0 (luôn đúng với mọi giá trị của x và y).", "#E65100", "ls_allz_2");
            AddStepCard("Kết luận",
                "Hệ phương trình có VÔ SỐ NGHIỆM. Tập nghiệm là toàn bộ mặt phẳng tọa độ R².", "#2E7D32", "ls_allz_3");
        }

        private void BuildStepsOneZero(int validEqIndex)
        {
            if (stepsPanel == null) return;
            stepsPanel.Children.Clear();

            AddStepCard("Bước 1: Nhận diện hệ phương trình",
                $"⎧ {FormatEq(_a1, _b1, _c1)}\n⎩ {FormatEq(_a2, _b2, _c2)}", "#1565C0", "ls_onez_1");

            double a = validEqIndex == 1 ? _a1 : _a2;
            double b = validEqIndex == 1 ? _b1 : _b2;
            double c = validEqIndex == 1 ? _c1 : _c2;
            int invIdx = validEqIndex == 1 ? 2 : 1;

            AddStepCard($"Bước 2: Rút gọn phương trình vô hiệu PT{invIdx}",
                $"Phương trình {invIdx} có dạng 0x + 0y = 0 (luôn đúng với mọi x, y).\nHệ phương trình được rút gọn về một phương trình đường thẳng duy nhất: {FormatEq(a, b, c)}.", 
                "#E65100", "ls_onez_2");

            AddStepCard("Kết luận",
                $"Hệ phương trình có VÔ SỐ NGHIỆM. Tập nghiệm của hệ là tất cả các điểm nằm trên đường thẳng: {FormatEq(a, b, c)}.", "#2E7D32", "ls_onez_3");
        }

        private void SetStyle(string bg, string fg)
        {
            if (resultCard != null)
                resultCard.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(bg)!;
            if (txtResult != null)
                txtResult.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(fg)!;
        }

        // ═══════════════════════════════════════════════════════════
        //  STEPS — Cramer
        // ═══════════════════════════════════════════════════════════

        private void BuildSteps(double D, double Dx, double Dy, double x, double y)
        {
            if (stepsPanel == null) return;
            stepsPanel.Children.Clear();

            var steps = new (string Title, string Content, string Color)[]
            {
                ("Bước 1: Viết hệ phương trình",
                 $"⎧ {FormatEq(_a1, _b1, _c1)}\n⎩ {FormatEq(_a2, _b2, _c2)}",
                 "#1565C0"),
                ("Bước 2: Tính định thức D",
                 $"D = a₁·b₂ − a₂·b₁\nD = ({_a1})·({_b2}) − ({_a2})·({_b1}) = {D:G8}\nD ≠ 0 → Hệ có nghiệm duy nhất",
                 "#7B1FA2"),
                ("Bước 3: Tính Dx, Dy",
                 $"Dx = c₁·b₂ − c₂·b₁ = ({_c1})·({_b2}) − ({_c2})·({_b1}) = {Dx:G8}\nDy = a₁·c₂ − a₂·c₁ = ({_a1})·({_c2}) − ({_a2})·({_c1}) = {Dy:G8}",
                 "#00695C"),
                ("Bước 4: Tính nghiệm",
                 $"x = Dx / D = {Dx:G6} / {D:G6} = {x:G8}\ny = Dy / D = {Dy:G6} / {D:G6} = {y:G8}",
                 "#2E7D32"),
                ("Bước 5: Thử lại",
                 $"PT1: {_a1}·({x:G6}) + {_b1}·({y:G6}) = {(_a1 * x + _b1 * y):G8} = {_c1} ✓\nPT2: {_a2}·({x:G6}) + {_b2}·({y:G6}) = {(_a2 * x + _b2 * y):G8} = {_c2} ✓",
                 "#E65100"),
            };

            for (int i = 0; i < steps.Length; i++)
            {
                var (title, content, colorHex) = steps[i];
                AddStepCard(title, content, colorHex, $"ls_step_{i}");
            }
        }

        private void BuildStepsSpecial(string type, double D)
        {
            if (stepsPanel == null) return;
            stepsPanel.Children.Clear();

            AddStepCard("Bước 1: Viết hệ phương trình",
                $"⎧ {FormatEq(_a1, _b1, _c1)}\n⎩ {FormatEq(_a2, _b2, _c2)}", "#1565C0", "ls_sp_1");
            AddStepCard("Bước 2: Tính D",
                $"D = ({_a1})·({_b2}) − ({_a2})·({_b1}) = {D:G8}\nD = 0 → Hệ {type}", "#C62828", "ls_sp_2");
            AddStepCard("Kết luận",
                type == "vô số nghiệm"
                    ? "Hai phương trình tỉ lệ → 2 đường thẳng trùng nhau\nMọi điểm trên đường thẳng đều là nghiệm."
                    : "Hai phương trình có hệ số tỉ lệ nhưng hằng số không tỉ lệ\n→ 2 đường thẳng song song, không có giao điểm.",
                "#7B1FA2", "ls_sp_3");
        }

        private void AddStepCard(string title, string content, string colorHex, string secId)
        {
            var color = (Color)ColorConverter.ConvertFromString(colorHex);
            var card = new Border
            {
                CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 6),
                Background = new SolidColorBrush(Color.FromArgb(15, color.R, color.G, color.B)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, color.R, color.G, color.B)),
                BorderThickness = new Thickness(0, 0, 0, 2)
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(color) });
            sp.Children.Add(new TextBlock { Text = content, FontSize = 14, Margin = new Thickness(0, 4, 0, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(55, 71, 79)),
                FontFamily = new FontFamily("Segoe UI"), TextWrapping = TextWrapping.Wrap, LineHeight = 22 });
            card.Child = sp;
            stepsPanel.Children.Add(WrapWithSectionToolbar(card, "linear_system", secId, title));
        }

        // ═══════════════════════════════════════════════════════════
        //  Graph
        // ═══════════════════════════════════════════════════════════

        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if ((_a1 == 0 && _b1 == 0) || (_a2 == 0 && _b2 == 0))
                {
                    MessageBox.Show("Không thể vẽ đồ thị khi hệ số của phương trình không hợp lệ (cả hai hệ số a và b đều bằng 0).", "Lỗi vẽ đồ thị", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var ci = CultureInfo.InvariantCulture;
                // y = (-a1*x + c1) / b1  and  y = (-a2*x + c2) / b2
                string eq1 = _b1 != 0
                    ? $"{_a1.ToString(ci)}x+{_b1.ToString(ci)}y={_c1.ToString(ci)}"
                    : $"x={(_c1 / _a1).ToString("G6", ci)}";
                string eq2 = _b2 != 0
                    ? $"{_a2.ToString(ci)}x+{_b2.ToString(ci)}y={_c2.ToString(ci)}"
                    : $"x={(_c2 / _a2).ToString("G6", ci)}";

                double D = _a1 * _b2 - _a2 * _b1;
                string pointJs = "";
                if (D != 0)
                {
                    double x = (_c1 * _b2 - _c2 * _b1) / D;
                    double y = (_a1 * _c2 - _a2 * _c1) / D;
                    pointJs = $"calc.setExpression({{id:'pt', latex:'({x.ToString("G6", ci)},{y.ToString("G6", ci)})', color:'#E53935', pointStyle:'POINT', pointSize:14, label:'({x.ToString("G4", ci)}; {y.ToString("G4", ci)})', showLabel:true}});";
                }

                var win = new GraphWindow(eq1, eq2, pointJs,
                    $"📐 Hệ PT: {_a1}x+{_b1}y={_c1} & {_a2}x+{_b2}y={_c2}");
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở đồ thị: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  THEORY
        // ═══════════════════════════════════════════════════════════

        private void BuildTheory()
        {
            if (theoryPanel == null) return;
            theoryPanel.Children.Clear();

            var items = new (string Title, string Content, string Bg)[]
            {
                ("📐 Dạng tổng quát",
                 "⎧ a₁x + b₁y = c₁\n⎩ a₂x + b₂y = c₂\nNghiệm (x; y) thỏa mãn cả 2 phương trình.",
                 "#E3F2FD"),
                ("📊 Phân loại hệ",
                 "• D ≠ 0: Nghiệm duy nhất (2 đường thẳng cắt nhau)\n• D = 0, Dx=Dy=0: Vô số nghiệm (2 đường thẳng trùng nhau)\n• D = 0, (Dx² + Dy² ≠ 0): Vô nghiệm (2 đường thẳng song song)\n(Điều kiện: a₁² + b₁² ≠ 0 và a₂² + b₂² ≠ 0)",
                 "#E8F5E9"),
                ("🔢 Phương pháp Cramer",
                 "D = a₁b₂ − a₂b₁\nDx = c₁b₂ − c₂b₁\nDy = a₁c₂ − a₂c₁\nx = Dx/D,  y = Dy/D",
                 "#FFF3E0"),
                ("🔄 Phương pháp thế",
                 "1. Từ PT1 rút x = (c₁ − b₁y) / a₁\n2. Thế vào PT2 → tìm y\n3. Thế y vào PT1 → tìm x",
                 "#F3E5F5"),
                ("➕ Phương pháp cộng đại số",
                 "1. Nhân PT sao cho hệ số cùng ẩn bằng nhau\n2. Trừ 2 PT → triệt tiêu 1 ẩn\n3. Giải PT 1 ẩn còn lại",
                 "#E0F7FA"),
            };

            foreach (var (title, content, bgHex) in items)
            {
                var bgColor = (Color)ColorConverter.ConvertFromString(bgHex);
                var card = new Border
                {
                    Background = new SolidColorBrush(bgColor), CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 8),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)), BorderThickness = new Thickness(1)
                };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)) });
                sp.Children.Add(new TextBlock { Text = content, FontSize = 13, Margin = new Thickness(0, 6, 0, 0),
                    Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                    FontFamily = new FontFamily("Segoe UI"), TextWrapping = TextWrapping.Wrap, LineHeight = 22 });
                card.Child = sp;
                string secId = title.Split(' ')[^1].ToLowerInvariant();
                theoryPanel.Children.Add(WrapWithSectionToolbar(card, "ls_theory", secId, title));
            }
        }
        private string FormatEq(double a, double b, double c)
        {
            string sa = a == 1 ? "x" : a == -1 ? "-x" : $"{a}x";
            if (a == 0) sa = "";

            string sb = b == 1 ? "+ y" : b == -1 ? "- y" : b > 0 ? $"+ {b}y" : $"- {-b}y";
            if (b == 0) sb = "";
            else if (a == 0 && b > 0) sb = b == 1 ? "y" : $"{b}y";

            if (a == 0 && b == 0) return $"0 = {c}";
            return $"{sa} {sb}".Trim() + $" = {c}";
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mainGrid == null || sideMenu == null || contentGuide == null || contentCalcScroll == null || viewPractical == null)
                return;

            contentGuide.Visibility = Visibility.Collapsed;
            contentCalcScroll.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            int index = sideMenu.SelectedIndex;
            if (index == 0)
            {
                contentGuide.Visibility = Visibility.Visible;
            }
            else if (index == 1)
            {
                contentCalcScroll.Visibility = Visibility.Visible;
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
                        Icon = "🧪",
                        Title = isVN ? "Pha Trộn Hóa Chất" : "Chemical Mixture Blending",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_linearsystem_1_{suffix}.png",
                        Description = isVN 
                            ? "Tính toán thể tích cần dùng của 2 dung dịch axit có nồng độ khác nhau để thu được hỗn hợp dung dịch mới có nồng độ chính xác yêu cầu." 
                            : "Calculate the required volume of two acid solutions of different concentrations to obtain a new mixture with the exact target concentration."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⛵",
                        Title = isVN ? "Chuyển Động Dòng Sông" : "River Current Motion",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_linearsystem_2_{suffix}.png",
                        Description = isVN 
                            ? "Tính toán vận tốc thực của ca nô và vận tốc của dòng chảy sông khi biết tổng thời gian và quãng đường ca nô đi xuôi dòng và ngược dòng." 
                            : "Calculate the actual speed of a boat and river current given the total time and distance for traveling upstream and downstream."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💼",
                        Title = isVN ? "Điểm Hòa Vốn Kinh Tế" : "Economic Break-Even Point",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_linearsystem_3_{suffix}.png",
                        Description = isVN 
                            ? "Xác định số lượng sản phẩm cần bán để doanh thu bằng với tổng chi phí sản xuất cố định và biến đổi (giao điểm của 2 đường thẳng đại số)." 
                            : "Determine the number of units to sell so that revenue equals total fixed and variable production costs (the intersection of two algebraic lines)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔌",
                        Title = isVN ? "Mạch Điện Kirchhoff" : "Kirchhoff's Electrical Circuit",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_linearsystem_4_{suffix}.png",
                        Description = isVN 
                            ? "Áp dụng định luật dòng điện tại các nút mạch và hiệu điện thế trong vòng kín để tìm cường độ dòng điện trong các nhánh mạch song song." 
                            : "Apply Kirchhoff's current law at junctions and voltage law in loops to find the electrical current in parallel circuit branches."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🥗",
                        Title = isVN ? "Bài Toán Dinh Dưỡng" : "Nutrition & Diet Formulation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_linearsystem_5_{suffix}.png",
                        Description = isVN 
                            ? "Thiết lập chế độ ăn kiêng hoặc khẩu phần dinh dưỡng dựa trên hàm lượng Vitamin, Protein của các loại thực phẩm để đạt chỉ số mong muốn." 
                            : "Formulate diet plans or nutritional portions based on vitamin and protein content in food items to reach target metrics."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🚦",
                        Title = isVN ? "Phân Lưu Lượng Xe" : "Traffic Flow Analysis",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_linearsystem_6_{suffix}.png",
                        Description = isVN 
                            ? "Phân tích lưu lượng xe đi vào và đi ra tại các nút giao lộ thành phố nhằm tối ưu hóa chu kỳ đèn tín hiệu giao thông đô thị." 
                            : "Analyze vehicle flow entering and exiting city intersections to optimize traffic light signaling cycles."
                    }
                ,
                    new PracticalAppItem
                    {
                        Icon = "🚦",
                        Title = isVN ? "Lưu lượng giao thông đô thị" : "Urban Traffic Flow",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_linearsystem_7_{suffix}.png",
                        Description = isVN 
                            ? "Giải hệ phương trình tuyến tính để tính toán lưu lượng xe cộ qua các ngã tư, giúp thiết kế chu kỳ đèn tín hiệu tối ưu." 
                            : "Solve systems of linear equations to calculate vehicle flow at intersections, helping design optimal traffic light timing."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧪",
                        Title = isVN ? "Cân bằng phản ứng hóa học" : "Chemical Equation Balancing",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_linearsystem_8_{suffix}.png",
                        Description = isVN 
                            ? "Thiết lập hệ phương trình bảo toàn nguyên tố để tìm các hệ số cân bằng chính xác cho những phản ứng hóa học phức tạp." 
                            : "Set up elemental conservation equations to find precise stoichiometric coefficients for complex chemical reactions."
                    }};

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for LinearSystemTool: {Err}", ex.Message);
            }
        }
    }
}