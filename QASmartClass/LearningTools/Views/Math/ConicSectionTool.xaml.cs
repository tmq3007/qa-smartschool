using QASmartClass.LearningTools.Models;
using System;
using QASmartClass.LearningTools.Helpers;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class ConicSectionTool : BaseToolControl
    {
        private enum Mode { Ellipse, Hyperbola, Parabola }
        private Mode _mode = Mode.Ellipse;

        private static readonly Color Accent = DS.CatMath;

        private Border tabCalc;
        private TextBlock tabCalcText;
        private Border tabGuide;
        private TextBlock tabGuideText;
        private Border tabApp;
        private TextBlock tabAppText;

        public ConicSectionTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                // Fallback for unit testing where backing fields might be null
                tabCalc ??= (Border)FindName("tabCalc");
                tabCalcText ??= (TextBlock)FindName("tabCalcText");
                tabGuide ??= (Border)FindName("tabGuide");
                tabGuideText ??= (TextBlock)FindName("tabGuideText");
                tabApp ??= (Border)FindName("tabApp");
                tabAppText ??= (TextBlock)FindName("tabAppText");
                contentCalc ??= (ScrollViewer)FindName("contentCalc");
                contentGuide ??= (ScrollViewer)FindName("contentGuide");
                contentApp ??= (Controls.PracticalAppViewer)FindName("contentApp");
                modePanel ??= (WrapPanel)FindName("modePanel");
                txtModeTitle ??= (TextBlock)FindName("txtModeTitle");
                ellipseInput ??= (StackPanel)FindName("ellipseInput");
                txtEA ??= (TextBox)FindName("txtEA");
                txtEB ??= (TextBox)FindName("txtEB");
                hyperbolaInput ??= (StackPanel)FindName("hyperbolaInput");
                txtHA ??= (TextBox)FindName("txtHA");
                txtHB ??= (TextBox)FindName("txtHB");
                parabolaInput ??= (StackPanel)FindName("parabolaInput");
                txtPP ??= (TextBox)FindName("txtPP");
                presetPanel ??= (WrapPanel)FindName("presetPanel");
                resultPanel ??= (StackPanel)FindName("resultPanel");
                formulaPanel ??= (StackPanel)FindName("formulaPanel");
                guidePanel ??= (StackPanel)FindName("guidePanel");

                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextPractice != null) menuTextPractice.Text = isVN ? "Tính toán & Ví dụ" : "Calculation & Examples";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";

                if (sideMenu != null) sideMenu.SelectedIndex = 0;

                BuildModeButtons();
                BuildPresets();
                BuildFormulas();
                BuildGuide();
                LoadPracticalApps();
                Calc();

                TouchNumPad.Attach(txtEA, step: 0.5, min: 0.1);
                TouchNumPad.Attach(txtEB, step: 0.5, min: 0.1);
                TouchNumPad.Attach(txtHA, step: 0.5, min: 0.1);
                TouchNumPad.Attach(txtHB, step: 0.5, min: 0.1);
                TouchNumPad.Attach(txtPP, step: 0.5, min: 0.1);
            };
        }

        // ═══ MODE SELECTOR ═══
        private void BuildModeButtons()
        {
            modePanel.Children.Clear();
            var modes = new (string Label, Mode M)[]
            {
                ("🔴 Elip", Mode.Ellipse),
                ("📐 Hyperbol", Mode.Hyperbola),
                ("🌙 Parabol", Mode.Parabola),
            };
            foreach (var (label, m) in modes)
            {
                bool active = m == _mode;
                var btn = MakeChip(label, active);
                var cm = m;
                btn.MouseLeftButtonDown += (_, _) => { _mode = cm; BuildModeButtons(); BuildPresets(); UpdateUI(); Calc(); BuildGuide(); };
                modePanel.Children.Add(btn);
            }
        }

        private void UpdateUI()
        {
            ellipseInput.Visibility = _mode == Mode.Ellipse ? Visibility.Visible : Visibility.Collapsed;
            hyperbolaInput.Visibility = _mode == Mode.Hyperbola ? Visibility.Visible : Visibility.Collapsed;
            parabolaInput.Visibility = _mode == Mode.Parabola ? Visibility.Visible : Visibility.Collapsed;
            txtModeTitle.Text = _mode switch
            {
                Mode.Ellipse => "🔴 Elip — x²/a² + y²/b² = 1",
                Mode.Hyperbola => "📐 Hyperbol — x²/a² − y²/b² = 1",
                Mode.Parabola => "🌙 Parabol — y² = 2px",
                _ => ""
            };
        }

        // ═══ PRESETS ═══
        private void BuildPresets()
        {
            presetPanel.Children.Clear();
            switch (_mode)
            {
                case Mode.Ellipse:
                    AddPreset("🩺 Tán sỏi y khoa (a=5, b=3)", () => { txtEA.Text = "5"; txtEB.Text = "3"; });
                    AddPreset("🪐 Quỹ đạo hành tinh (a=6, b=5.8)", () => { txtEA.Text = "6"; txtEB.Text = "5.8"; });
                    AddPreset("📐 Ví dụ 3 (a=13, b=5)", () => { txtEA.Text = "13"; txtEB.Text = "5"; });
                    AddPreset("🔴 Đường tròn (a=b=4)", () => { txtEA.Text = "4"; txtEB.Text = "4"; });
                    break;
                case Mode.Hyperbola:
                    AddPreset("🗼 Tháp giải nhiệt (a=3, b=4)", () => { txtHA.Text = "3"; txtHB.Text = "4"; });
                    AddPreset("📐 Nhánh dẹt (a=5, b=3)", () => { txtHA.Text = "5"; txtHB.Text = "3"; });
                    AddPreset("📐 Hyperbol vuông (a=b=1)", () => { txtHA.Text = "1"; txtHB.Text = "1"; });
                    break;
                case Mode.Parabola:
                    AddPreset("📡 Chảo vệ tinh (p=2)", () => { txtPP.Text = "2"; });
                    AddPreset("🚘 Đèn pha ô tô (p=4)", () => { txtPP.Text = "4"; });
                    AddPreset("🌉 Cáp cầu treo (p=8)", () => { txtPP.Text = "8"; });
                    break;
            }
        }

        private void AddPreset(string label, Action action)
        {
            var btn = MakeChip(label, false);
            btn.MouseLeftButtonDown += (_, _) =>
            {
                action();
                SwitchToTab("calc");
            };
            presetPanel.Children.Add(btn);
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
                case Mode.Ellipse: CalcEllipse(); break;
                case Mode.Hyperbola: CalcHyperbola(); break;
                case Mode.Parabola: CalcParabola(); break;
            }
        }

        private void CalcEllipse()
        {
            if (string.IsNullOrWhiteSpace(txtEA?.Text) || string.IsNullOrWhiteSpace(txtEB?.Text))
            {
                UI.ResultRow("⚠️ Vui lòng nhập đầy đủ giá trị cho a và b.", DS.ResultDanger, resultPanel);
                return;
            }
            if (!ParsingHelper.TryParseDouble(txtEA?.Text, out double a) || a <= 0 ||
                !ParsingHelper.TryParseDouble(txtEB?.Text, out double b) || b <= 0)
            {
                UI.ResultRow("⚠️ Lỗi: Các hệ số a, b phải là số thực dương (a, b > 0).", DS.ResultDanger, resultPanel);
                return;
            }
            if (a < 0.01 || a > 1000 || b < 0.01 || b > 1000)
            {
                UI.ResultRow("⚠️ Lỗi: Các giá trị a, b phải nằm trong khoảng từ 0.01 đến 1000.", DS.ResultDanger, resultPanel);
                return;
            }

            bool isCircle = System.Math.Abs(a - b) < 1e-10;

            if (a < b)
            {
                UI.ResultRow("⚠️ Cảnh báo: Theo chương trình Toán 10 chính quy, Elip yêu cầu a > b > 0.", DS.ResultDanger, resultPanel);
                UI.ResultRow("Vui lòng điều chỉnh để nửa trục lớn a lớn hơn nửa trục nhỏ b.", DS.ResultInfo, resultPanel);
                return;
            }

            UI.ResultRow($"Phương trình chính tắc: x²/{UI.Fmt(a)}² + y²/{UI.Fmt(b)}² = 1", DS.ResultPrimary, resultPanel);
            UI.ResultRow($"a = {UI.Fmt(a)}", DS.ResultPrimary, resultPanel);
            UI.ResultRow($"b = {UI.Fmt(b)}", DS.ResultPrimary, resultPanel);

            if (!isCircle)
            {
                double c2 = a * a - b * b;
                double c = System.Math.Sqrt(c2);

                UI.ResultRow($"c = √({UI.Fmt(a)}²−{UI.Fmt(b)}²) = {c:G8}", DS.ResultPrimary, resultPanel);
                UI.ResultRow($"Tiêu điểm: F₁(-{c:G6}, 0)  F₂({c:G6}, 0)", DS.ResultPrimary, resultPanel);
                UI.ResultRow($"Đỉnh: A₁(-{UI.Fmt(a)}, 0)  A₂({UI.Fmt(a)}, 0)  B₁(0, -{UI.Fmt(b)})  B₂(0, {UI.Fmt(b)})", DS.ResultPrimary, resultPanel);

                double ecc = c / a;
                UI.ResultRow($"Tâm sai: e = c/a = {ecc:G6}  (0 < e < 1)", DS.ResultPrimary, resultPanel);
                UI.ResultRow($"Trục lớn: 2a = {UI.Fmt(2 * a)}", DS.ResultPrimary, resultPanel);
                UI.ResultRow($"Trục nhỏ: 2b = {UI.Fmt(2 * b)}", DS.ResultPrimary, resultPanel);
                UI.ResultRow($"Tiêu cự: 2c = {2 * c:G8}", DS.ResultPrimary, resultPanel);

                // Đường chuẩn
                double dVal = a / ecc;
                UI.ResultRow($"Đường chuẩn: Δ₁: x = -{dVal:G6} và Δ₂: x = {dVal:G6}", DS.ResultPrimary, resultPanel);

                // Area
                double area = System.Math.PI * a * b;
                UI.ResultRow($"Diện tích: S = π·a·b = {area:G8}", DS.ResultPrimary, resultPanel);

                UI.ResultRow($"📌 Tính chất: |MF₁| + |MF₂| = 2a = {UI.Fmt(2 * a)} (∀M ∈ Elip)", DS.ResultWarning, resultPanel);
            }
            else
            {
                UI.ResultRow($"📌 Đường tròn (Trường hợp đặc biệt a = b): R = {UI.Fmt(a)}", DS.ResultWarning, resultPanel);
                UI.ResultRow($"Tâm: O(0, 0)", DS.ResultPrimary, resultPanel);
                UI.ResultRow($"Bán kính: R = {UI.Fmt(a)}", DS.ResultPrimary, resultPanel);
                UI.ResultRow($"Tâm sai: e = 0", DS.ResultPrimary, resultPanel);
                UI.ResultRow($"Chu vi: C = 2·π·R = {2 * System.Math.PI * a:G8}", DS.ResultPrimary, resultPanel);
                UI.ResultRow($"Diện tích: S = π·R² = {System.Math.PI * a * a:G8}", DS.ResultPrimary, resultPanel);
                UI.ResultRow($"Phương trình thu gọn: x² + y² = {UI.Fmt(a * a)}", DS.ResultPrimary, resultPanel);
            }
        }

        private void CalcHyperbola()
        {
            if (string.IsNullOrWhiteSpace(txtHA?.Text) || string.IsNullOrWhiteSpace(txtHB?.Text))
            {
                UI.ResultRow("⚠️ Vui lòng nhập đầy đủ giá trị cho a và b.", DS.ResultDanger, resultPanel);
                return;
            }
            if (!ParsingHelper.TryParseDouble(txtHA?.Text, out double a) || a <= 0 ||
                !ParsingHelper.TryParseDouble(txtHB?.Text, out double b) || b <= 0)
            {
                UI.ResultRow("⚠️ Lỗi: Các hệ số a, b phải là số thực dương (a, b > 0).", DS.ResultDanger, resultPanel);
                return;
            }
            if (a < 0.01 || a > 1000 || b < 0.01 || b > 1000)
            {
                UI.ResultRow("⚠️ Lỗi: Các giá trị a, b phải nằm trong khoảng từ 0.01 đến 1000.", DS.ResultDanger, resultPanel);
                return;
            }

            UI.ResultRow($"Phương trình chính tắc: x²/{UI.Fmt(a)}² − y²/{UI.Fmt(b)}² = 1", DS.ResultPrimary, resultPanel);

            double c2 = a * a + b * b;
            double c = System.Math.Sqrt(c2);

            UI.ResultRow($"a = {UI.Fmt(a)}", DS.ResultPrimary, resultPanel);
            UI.ResultRow($"b = {UI.Fmt(b)}", DS.ResultPrimary, resultPanel);
            UI.ResultRow($"c = √({UI.Fmt(a)}²+{UI.Fmt(b)}²) = {c:G8}", DS.ResultPrimary, resultPanel);

            UI.ResultRow($"Tiêu điểm: F₁(-{c:G6}, 0)  F₂({c:G6}, 0)", DS.ResultPrimary, resultPanel);
            UI.ResultRow($"Đỉnh thực: A₁(-{UI.Fmt(a)}, 0)  A₂({UI.Fmt(a)}, 0)", DS.ResultPrimary, resultPanel);
            UI.ResultRow($"Đỉnh ảo: B₁(0, -{UI.Fmt(b)})  B₂(0, {UI.Fmt(b)})", DS.ResultPrimary, resultPanel);

            double ecc = c / a;
            UI.ResultRow($"Tâm sai: e = c/a = {ecc:G6}  (e > 1)", DS.ResultPrimary, resultPanel);
            UI.ResultRow($"Trục thực: 2a = {UI.Fmt(2 * a)}", DS.ResultPrimary, resultPanel);
            UI.ResultRow($"Trục ảo: 2b = {UI.Fmt(2 * b)}", DS.ResultPrimary, resultPanel);
            UI.ResultRow($"Tiêu cự: 2c = {2 * c:G8}", DS.ResultPrimary, resultPanel);

            // Đường chuẩn
            double dVal = a / ecc;
            UI.ResultRow($"Đường chuẩn: Δ₁: x = -{dVal:G6} và Δ₂: x = {dVal:G6}", DS.ResultPrimary, resultPanel);

            // Asymptotes
            UI.ResultRow($"Tiệm cận: y = ±({UI.Fmt(b)}/{UI.Fmt(a)})x = ±{b / a:G6}x", DS.ResultPrimary, resultPanel);

            bool isRect = System.Math.Abs(a - b) < 1e-10;
            string note = isRect
                ? $"Hyperbol vuông (a = b), tiệm cận y = ±x"
                : $"||MF₁| − |MF₂|| = 2a = {UI.Fmt(2 * a)} (∀M ∈ Hyperbol)";
            UI.ResultRow($"📌 Tính chất: {note}", DS.ResultWarning, resultPanel);
        }

        private void CalcParabola()
        {
            if (string.IsNullOrWhiteSpace(txtPP?.Text))
            {
                UI.ResultRow("⚠️ Vui lòng nhập tham số tiêu p.", DS.ResultDanger, resultPanel);
                return;
            }
            if (!ParsingHelper.TryParseDouble(txtPP?.Text, out double p) || p <= 0)
            {
                UI.ResultRow("⚠️ Lỗi: Tham số tiêu p phải là số thực dương (p > 0).", DS.ResultDanger, resultPanel);
                return;
            }
            if (p < 0.01 || p > 1000)
            {
                UI.ResultRow("⚠️ Lỗi: Tham số tiêu p phải nằm trong khoảng từ 0.01 đến 1000.", DS.ResultDanger, resultPanel);
                return;
            }

            UI.ResultRow($"Phương trình chính tắc: y² = 2·{UI.Fmt(p)}·x = {UI.Fmt(2 * p)}x", DS.ResultPrimary, resultPanel);

            double pHalf = p / 2.0;
            UI.ResultRow($"p = {UI.Fmt(p)}", DS.ResultPrimary, resultPanel);
            UI.ResultRow($"p/2 = {pHalf:G8}", DS.ResultPrimary, resultPanel);

            UI.ResultRow($"Tiêu điểm: F({pHalf:G6}, 0)", DS.ResultPrimary, resultPanel);
            UI.ResultRow($"Đỉnh: O(0, 0)", DS.ResultPrimary, resultPanel);
            UI.ResultRow($"Đường chuẩn: x = -{pHalf:G6}", DS.ResultPrimary, resultPanel);
            UI.ResultRow($"Trục đối xứng: Ox (trục hoành)", DS.ResultPrimary, resultPanel);
            UI.ResultRow("Tâm sai: e = 1", DS.ResultPrimary, resultPanel);

            UI.ResultRow($"📌 Tính chất: |MF| = d(M, đường chuẩn) (∀M ∈ Parabol)", DS.ResultWarning, resultPanel);
        }

        // ═══ FORMULAS ═══
        private void BuildFormulas()
        {
            formulaPanel.Children.Clear();
            var sections = new (string Title, string[] Items)[]
            {
                ("Elip", new[]
                {
                    "PT chính tắc: x²/a² + y²/b² = 1  (a > b > 0)",
                    "c² = a² − b², e = c/a < 1",
                    "Tiêu điểm: F₁(-c,0), F₂(c,0)",
                    "|MF₁| + |MF₂| = 2·a",
                }),
                ("Hyperbol", new[]
                {
                    "PT chính tắc: x²/a² − y²/b² = 1",
                    "c² = a² + b², e = c/a > 1",
                    "Tiệm cận: y = ±(b/a)·x",
                    "||MF₁| − |MF₂|| = 2·a",
                }),
                ("Parabol", new[]
                {
                    "PT chính tắc: y² = 2·p·x  (p > 0)",
                    "Tiêu điểm: F(p/2, 0)",
                    "Đường chuẩn: x = −p/2",
                    "|MF| = d(M, đường chuẩn)",
                }),
                ("Tâm sai & Phân loại", new[]
                {
                    "e = 0 → Đường tròn",
                    "0 < e < 1 → Elip",
                    "e = 1 → Parabol",
                    "e > 1 → Hyperbol",
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
                {
                    sp.Children.Add(CreateMathTextBlock(item));
                }
                section.Child = sp;
                formulaPanel.Children.Add(section);
            }
        }

        private static TextBlock CreateMathTextBlock(string text)
        {
            var tb = new TextBlock
            {
                FontSize = 13,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                Margin = new Thickness(0, 1, 0, 1),
                TextWrapping = TextWrapping.Wrap
            };
            tb.Inlines.Add(new Run("  ")); // indentation

            string pattern = @"(\b[xyabcpeMF]\b|\bF[₁₂]\b|\bMF[₁₂]\b|\bMF\b|(?<=\b)[xyabcpeMF](?=²))";
            var parts = System.Text.RegularExpressions.Regex.Split(text, pattern);

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;

                bool isVar = System.Text.RegularExpressions.Regex.IsMatch(part, @"^([xyabcpeMF]|F₁|F₂|MF₁|MF₂|MF)$");
                if (isVar)
                {
                    tb.Inlines.Add(new Run(part)
                    {
                        FontStyle = FontStyles.Italic,
                        FontWeight = FontWeights.SemiBold
                    });
                }
                else
                {
                    tb.Inlines.Add(new Run(part));
                }
            }

            return tb;
        }

        // ═══ GUIDE & APPLICATIONS ═══
        private void BuildGuide()
        {
            guidePanel.Children.Clear();
            string guideTitle = "Hướng dẫn sử dụng";
            string[] guideItems = Array.Empty<string>();
            string appTitle = "Ứng dụng thực tế";
            string[] appItems = Array.Empty<string>();

            switch (_mode)
            {
                case Mode.Ellipse:
                    guideItems = new[]
                    {
                        "1. Nhập hệ số a (nửa trục lớn) và b (nửa trục nhỏ) thỏa mãn a > b > 0.",
                        "2. Click 'Xem đồ thị' để mở màn hình vẽ trực quan hệ trục tọa độ.",
                        "3. Di chuột lên các điểm F₁, F₂ (tiêu điểm), A₁, A₂, B₁, B₂ (đỉnh) hoặc đường nét đứt Δ₁, Δ₂ (đường chuẩn) để xem chi tiết."
                    };
                    appItems = new[]
                    {
                        "🪐 Vũ trụ học: Quỹ đạo chuyển động của các hành tinh quanh Mặt Trời là các đường Elip (Mặt Trời là một tiêu điểm).",
                        "🩺 Y học: Máy tán sỏi ngoài cơ thể dùng chảo phản xạ elip để tập trung sóng âm tại tiêu điểm thứ hai (nơi có sỏi) để phá sỏi không xâm lấn.",
                        "🏛️ Kiến trúc: Các phòng thì thầm (Whispering Gallery) có trần dạng elip. Âm thanh nói từ một tiêu điểm sẽ phản xạ hội tụ tại tiêu điểm kia."
                    };
                    break;
                case Mode.Hyperbola:
                    guideItems = new[]
                    {
                        "1. Nhập hệ số a (nửa trục thực) và b (nửa trục ảo) thỏa mãn a, b > 0.",
                        "2. Click 'Xem đồ thị' để quan sát 2 nhánh đường cong Hyperbol ôm lấy 2 tiêu điểm.",
                        "3. Chú ý 2 đường tiệm cận nét đứt màu cam (y = ±b/a x) và 2 đường chuẩn nét đứt màu lục (x = ±a/e)."
                    };
                    appItems = new[]
                    {
                        "🗼 Kỹ thuật: Tháp giải nhiệt của các nhà máy điện hạt nhân thường có hình dạng xoay một hyperboloid để tối ưu hóa khả năng chịu lực và thoát khí.",
                        "📡 Định vị: Hệ thống định vị hàng hải LORAN xác định vị trí tàu bằng cách tính hiệu thời gian nhận tín hiệu từ các trạm phát (tạo ra giao điểm các đường Hyperbol).",
                        "💡 Đời sống: Bóng của một chiếc chụp đèn hình nón hoặc đèn bàn hắt lên tường phẳng tạo ra một đường Hyperbol rất sắc nét."
                    };
                    break;
                case Mode.Parabola:
                    guideItems = new[]
                    {
                        "1. Nhập tham số tiêu p thỏa mãn p > 0.",
                        "2. Click 'Xem đồ thị' để quan sát parabol y² = 2px mở rộng sang phải.",
                        "3. Điểm F(p/2,0) là Tiêu điểm, đường thẳng đứng nét đứt x = -p/2 là Đường chuẩn."
                    };
                    appItems = new[]
                    {
                        "📡 Viễn thông: Chảo vệ tinh dạng parabol giúp hội tụ toàn bộ sóng tín hiệu song song chiếu tới vào bộ thu đặt tại Tiêu điểm F.",
                        "🚘 Thiết bị chiếu sáng: Kính đèn pha ô tô có dạng parabol, bóng đèn đặt ở Tiêu điểm F để chùm sáng phát ra phản xạ thành chùm song song chiếu xa.",
                        "🌉 Xây dựng: Cáp treo của cầu treo dây võng (như cầu cổng Vàng) chịu tải trọng phân bố đều sẽ tự động võng xuống theo dạng một đường Parabol."
                    };
                    break;
            }

            // Render Hướng dẫn
            var guideSection = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(15, Accent.R, Accent.G, Accent.B)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 8)
            };
            var guideSp = new StackPanel();
            guideSp.Children.Add(new TextBlock
            {
                Text = "📝 " + guideTitle, FontSize = 13, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Accent), Margin = new Thickness(0, 0, 0, 4)
            });
            foreach (var item in guideItems)
            {
                guideSp.Children.Add(new TextBlock
                {
                    Text = item, FontSize = 12,
                    FontFamily = new FontFamily("Segoe UI"),
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 2)
                });
            }
            guideSection.Child = guideSp;
            guidePanel.Children.Add(guideSection);

            // Render Ứng dụng thực tế
            var appSection = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(15, 76, 175, 80)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 8)
            };
            var appSp = new StackPanel();
            appSp.Children.Add(new TextBlock
            {
                Text = "🌍 " + appTitle, FontSize = 13, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)), Margin = new Thickness(0, 0, 0, 4)
            });
            foreach (var item in appItems)
            {
                appSp.Children.Add(new TextBlock
                {
                    Text = item, FontSize = 12,
                    FontFamily = new FontFamily("Segoe UI"),
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 2)
                });
            }
            appSection.Child = appSp;
            guidePanel.Children.Add(appSection);
        }

        // ═══ Graph ═══
        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var ci = CultureInfo.InvariantCulture;
                string expJs;

                switch (_mode)
                {
                    case Mode.Ellipse:
                    {
                        if (!ParsingHelper.TryParseDouble(txtEA?.Text, out double a) || a <= 0 ||
                            !ParsingHelper.TryParseDouble(txtEB?.Text, out double b) || b <= 0)
                        {
                            MessageBox.Show("Vui lòng nhập các hệ số a, b là số thực dương (a, b > 0).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        if (a < 0.01 || a > 1000 || b < 0.01 || b > 1000)
                        {
                            MessageBox.Show("Các giá trị a, b phải nằm trong khoảng từ 0.01 đến 1000.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        if (a < b)
                        {
                            MessageBox.Show("Để vẽ Elip chuẩn theo chương trình Lớp 10, vui lòng nhập a > b > 0.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        bool isCircle = System.Math.Abs(a - b) < 1e-10;
                        string aS = a.ToString(ci), bS = b.ToString(ci);

                        if (isCircle)
                        {
                            expJs = $@"
        calc.setExpression({{id:'circle', latex:'x^2+y^2={aS}^2', color:'#1565C0', lineWidth:3}});
        calc.setExpression({{id:'center', latex:'(0,0)', color:'#C62828', pointSize:10, label:'O', showLabel:true}});
        calc.setMathBounds({{ left: -{(a + 2).ToString(ci)}, right: {(a + 2).ToString(ci)}, bottom: -{(a + 2).ToString(ci)}, top: {(a + 2).ToString(ci)} }});";
                        }
                        else
                        {
                            double c = System.Math.Sqrt(a * a - b * b);
                            double ecc = c / a;
                            double dVal = a / ecc;
                            string cS = c.ToString(ci), dS = dVal.ToString(ci);

                            double limitX = (dVal <= 3 * a) ? (dVal + 2) : (a + 2);
                            double limitY = a + 2;

                            expJs = $@"
        calc.setExpression({{id:'ellipse', latex:'\\frac{{x^2}}{{{aS}^2}}+\\frac{{y^2}}{{{bS}^2}}=1', color:'#1565C0', lineWidth:3}});
        calc.setExpression({{id:'f1', latex:'(-{cS},0)', color:'#C62828', pointSize:10, label:'F₁', showLabel:true}});
        calc.setExpression({{id:'f2', latex:'({cS},0)', color:'#C62828', pointSize:10, label:'F₂', showLabel:true}});
        calc.setExpression({{id:'a1', latex:'(-{aS},0)', color:'#1B5E20', pointSize:8, label:'A₁', showLabel:true}});
        calc.setExpression({{id:'a2', latex:'({aS},0)', color:'#1B5E20', pointSize:8, label:'A₂', showLabel:true}});
        calc.setExpression({{id:'b1', latex:'(0,-{bS})', color:'#1B5E20', pointSize:8, label:'B₁', showLabel:true}});
        calc.setExpression({{id:'b2', latex:'(0,{bS})', color:'#1B5E20', pointSize:8, label:'B₂', showLabel:true}});
        calc.setExpression({{id:'dir1', latex:'x=-{dS}', color:'#E65100', lineWidth:1.5, lineStyle:'DASHED', label:'Δ₁', showLabel:true}});
        calc.setExpression({{id:'dir2', latex:'x={dS}', color:'#E65100', lineWidth:1.5, lineStyle:'DASHED', label:'Δ₂', showLabel:true}});
        calc.setMathBounds({{ left: -{limitX.ToString(ci)}, right: {limitX.ToString(ci)}, bottom: -{limitY.ToString(ci)}, top: {limitY.ToString(ci)} }});";
                        }
                        break;
                    }
                    case Mode.Hyperbola:
                    {
                        if (!ParsingHelper.TryParseDouble(txtHA?.Text, out double a) || a <= 0 ||
                            !ParsingHelper.TryParseDouble(txtHB?.Text, out double b) || b <= 0)
                        {
                            MessageBox.Show("Vui lòng nhập các hệ số a, b là số thực dương (a, b > 0).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        if (a < 0.01 || a > 1000 || b < 0.01 || b > 1000)
                        {
                            MessageBox.Show("Các giá trị a, b phải nằm trong khoảng từ 0.01 đến 1000.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        double c = System.Math.Sqrt(a * a + b * b);
                        double ecc = c / a;
                        double dVal = a / ecc;
                        string aS = a.ToString(ci), bS = b.ToString(ci), cS = c.ToString(ci), dS = dVal.ToString(ci);
                        double slope = b / a;

                        expJs = $@"
        calc.setExpression({{id:'hyp', latex:'\\frac{{x^2}}{{{aS}^2}}-\\frac{{y^2}}{{{bS}^2}}=1', color:'#1565C0', lineWidth:3}});
        calc.setExpression({{id:'asy1', latex:'y={slope.ToString(ci)}x', color:'#E65100', lineWidth:1.5, lineStyle:'DASHED', label:'Tiệm cận', showLabel:true}});
        calc.setExpression({{id:'asy2', latex:'y=-{slope.ToString(ci)}x', color:'#E65100', lineWidth:1.5, lineStyle:'DASHED'}});
        calc.setExpression({{id:'f1', latex:'(-{cS},0)', color:'#C62828', pointSize:10, label:'F₁', showLabel:true}});
        calc.setExpression({{id:'f2', latex:'({cS},0)', color:'#C62828', pointSize:10, label:'F₂', showLabel:true}});
        calc.setExpression({{id:'a1', latex:'(-{aS},0)', color:'#1B5E20', pointSize:8, label:'A₁', showLabel:true}});
        calc.setExpression({{id:'a2', latex:'({aS},0)', color:'#1B5E20', pointSize:8, label:'A₂', showLabel:true}});
        calc.setExpression({{id:'dir1', latex:'x=-{dS}', color:'#00796B', lineWidth:1.5, lineStyle:'DASHED', label:'Δ₁', showLabel:true}});
        calc.setExpression({{id:'dir2', latex:'x={dS}', color:'#00796B', lineWidth:1.5, lineStyle:'DASHED', label:'Δ₂', showLabel:true}});
        calc.setMathBounds({{ left: -{(c + 3).ToString(ci)}, right: {(c + 3).ToString(ci)}, bottom: -{(c + 3).ToString(ci)}, top: {(c + 3).ToString(ci)} }});";
                        break;
                    }
                    default: // Parabola
                    {
                        if (!ParsingHelper.TryParseDouble(txtPP?.Text, out double p) || p <= 0)
                        {
                            MessageBox.Show("Vui lòng nhập tham số tiêu p là số thực dương (p > 0).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        if (p < 0.01 || p > 1000)
                        {
                            MessageBox.Show("Tham số tiêu p phải nằm trong khoảng từ 0.01 đến 1000.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        double ph = p / 2;
                        string pS = (2 * p).ToString(ci), phS = ph.ToString(ci);

                        expJs = $@"
        calc.setExpression({{id:'para', latex:'y^2={pS}x', color:'#1565C0', lineWidth:3}});
        calc.setExpression({{id:'focus', latex:'({phS},0)', color:'#C62828', pointSize:10, label:'F', showLabel:true}});
        calc.setExpression({{id:'vertex', latex:'(0,0)', color:'#1B5E20', pointSize:8, label:'O', showLabel:true}});
        calc.setExpression({{id:'dir', latex:'x=-{phS}', color:'#E65100', lineWidth:1.5, lineStyle:'DASHED', label:'Δ', showLabel:true}});
        calc.setMathBounds({{ left: -{(p + 2).ToString(ci)}, right: {(3 * p).ToString(ci)}, bottom: -{(2 * p).ToString(ci)}, top: {(2 * p).ToString(ci)} }});";
                        break;
                    }
                }

                var win = new GraphWindow(expJs, "🔵 Đường Conic");
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
                Text = text, FontSize = 14, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(active ? Colors.White : Accent),
                FontFamily = new FontFamily("Segoe UI")
            };
            return btn;
        }

        /* AddResult / AddR replaced by UI.ResultRow */

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
                SwitchToTab(tag);
            }
        }

        private void SwitchToTab(string tag)
        {
            if (sideMenu == null) return;
            if (tag == "guide")
                sideMenu.SelectedIndex = 0;
            else if (tag == "calc" || tag == "practice")
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
                        Icon = "🪐",
                        Title = isVN ? "Quỹ Đạo Hành Tinh" : "Planetary Orbits",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_conic_1_{suffix}.png",
                        Description = isVN 
                            ? "Theo định luật I Kepler, tất cả các hành tinh trong Hệ Mặt Trời đều chuyển động theo quỹ đạo Elip với Mặt Trời nằm ở một trong hai tiêu điểm." 
                            : "Model planetary orbits, moons, and artificial satellites around the Sun or Earth as elliptical paths following Kepler's laws."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🩺",
                        Title = isVN ? "Máy Tán Sỏi Thận Y Khoa" : "Satellite Dish Receivers",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_conic_2_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng gương phản xạ elip để hội tụ các sóng âm phát ra từ tiêu điểm thứ nhất trực tiếp vào tiêu điểm thứ hai (nơi có sỏi thận) để tán sỏi không xâm lấn." 
                            : "Design parabolic antenna surfaces that reflect incoming parallel electromagnetic signals directly to a single focus receiver."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📡",
                        Title = isVN ? "Chảo Thu Sóng Parabol" : "Whispering Gallery Effect",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_conic_3_{suffix}.png",
                        Description = isVN 
                            ? "Mặt cong parabol xoay của chảo giúp tập trung toàn bộ sóng vô tuyến song song từ vệ tinh vũ trụ phản xạ hội tụ vào một điểm thu đặt tại Tiêu điểm." 
                            : "Create elliptical domes where sound waves emitted from one focus bounce off walls and converge perfectly at the other focus."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🚘",
                        Title = isVN ? "Gương Pha Đèn Ô Tô" : "GPS & Loran Navigation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_conic_4_{suffix}.png",
                        Description = isVN 
                            ? "Ứng dụng tính chất phản xạ parabol: Đặt nguồn sáng (bóng đèn) tại Tiêu điểm, chùm ánh sáng phát ra sẽ phản xạ qua chóa thành chùm song song chiếu cực xa." 
                            : "Determine receiver locations by calculating the intersection points of hyperbolic curves representing signal arrival time differences."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏢",
                        Title = isVN ? "Tháp Giải Nhiệt Nhà Máy" : "Searchlight Reflector Design",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_conic_5_{suffix}.png",
                        Description = isVN 
                            ? "Cấu trúc tháp giải nhiệt có hình dạng một mặt xoay hyperboloid giúp tối ưu hoá lưu lượng khí đối lưu và tăng khả năng chịu lực tác động vật lý rất lớn." 
                            : "Utilize parabolic mirrors to project light from a source placed at the focal point into a powerful parallel beam."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💡",
                        Title = isVN ? "Bóng Đèn Chụp Hyperbol" : "Comet Trajectories",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_conic_6_{suffix}.png",
                        Description = isVN 
                            ? "Khi chụp đèn hình nón phát ra chùm sáng, phần ranh giới ánh sáng và bóng tối chiếu lên mặt tường thẳng đứng sẽ tạo nên một đường hyperbol rõ nét." 
                            : "Analyze trajectories of non-returning comets as hyperbolic orbits that enter and escape the Solar System's gravity."
                    }
                ,
                    new PracticalAppItem
                    {
                        Icon = "📡",
                        Title = isVN ? "Ăng-ten chảo parabol" : "Parabolic Dish Antennas",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_conic_7_{suffix}.png",
                        Description = isVN 
                            ? "Ứng dụng đặc tính tiêu cự của đường parabol để hội tụ sóng vô tuyến từ vũ trụ về đầu thu tín hiệu trung tâm." 
                            : "Use the focal property of parabolic curves to focus space radio waves directly into a central signal receiver."
                    },
                    new PracticalAppItem
                    {
                        Icon = "☄️",
                        Title = isVN ? "Quỹ đạo sao chổi" : "Comet Orbits",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_conic_8_{suffix}.png",
                        Description = isVN 
                            ? "Mô tả chuyển động của các thiên thể và sao chổi không chu kỳ quanh Mặt Trời bằng các đường parabol hoặc hyperbol." 
                            : "Model the motion of non-periodic comets and celestial bodies around the Sun using parabolic or hyperbolic paths."
                    }};

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for ConicSectionTool: {Err}", ex.Message);
            }
        }

        private string GenerateReportText()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("==================================================");
            sb.AppendLine("BÁO CÁO PHÂN TÍCH ĐƯỜNG CONIC - QA SMART CLASS");
            sb.AppendLine("==================================================");
            sb.AppendLine($"Thời gian xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine();

            switch (_mode)
            {
                case Mode.Ellipse:
                    sb.AppendLine("- Loại đường conic: Elip (Ellipse)");
                    if (ParsingHelper.TryParseDouble(txtEA?.Text, out double ea) &&
                        ParsingHelper.TryParseDouble(txtEB?.Text, out double eb))
                    {
                        sb.AppendLine($"- Nửa trục lớn a = {ea}");
                        sb.AppendLine($"- Nửa trục nhỏ b = {eb}");
                        if (System.Math.Abs(ea - eb) < 1e-10)
                        {
                            sb.AppendLine("- Trường hợp đặc biệt: Đường tròn");
                            sb.AppendLine($"- Tâm: O(0, 0)");
                            sb.AppendLine($"- Bán kính R = {ea}");
                            sb.AppendLine($"- Tâm sai e = 0");
                            sb.AppendLine($"- Chu vi C = {2 * System.Math.PI * ea:G8}");
                            sb.AppendLine($"- Diện tích S = {System.Math.PI * ea * ea:G8}");
                            sb.AppendLine($"- Phương trình thu gọn: x² + y² = {ea * ea}");
                        }
                        else
                        {
                            sb.AppendLine($"- Phương trình chính tắc: x²/{ea}² + y²/{eb}² = 1");
                            double ec = System.Math.Sqrt(ea * ea - eb * eb);
                            double eecc = ec / ea;
                            double edVal = ea / eecc;
                            sb.AppendLine($"- Tiêu cự 2c = {2 * ec}");
                            sb.AppendLine($"- Tiêu điểm: F₁(-{ec:G6}, 0), F₂({ec:G6}, 0)");
                            sb.AppendLine($"- Đỉnh thực: A₁(-{ea}, 0), A₂({ea}, 0)");
                            sb.AppendLine($"- Đỉnh ảo: B₁(0, -{eb}), B₂(0, {eb})");
                            sb.AppendLine($"- Tâm sai: e = {eecc:G6}");
                            sb.AppendLine($"- Đường chuẩn: x = ±{edVal:G6}");
                            sb.AppendLine($"- Diện tích Elip: S = {System.Math.PI * ea * eb:G8}");
                        }
                    }
                    break;
                case Mode.Hyperbola:
                    sb.AppendLine("- Loại đường conic: Hyperbol (Hyperbola)");
                    if (ParsingHelper.TryParseDouble(txtHA?.Text, out double ha) &&
                        ParsingHelper.TryParseDouble(txtHB?.Text, out double hb))
                    {
                        sb.AppendLine($"- Nửa trục thực a = {ha}");
                        sb.AppendLine($"- Nửa trục ảo b = {hb}");
                        sb.AppendLine($"- Phương trình chính tắc: x²/{ha}² - y²/{hb}² = 1");
                        double hc = System.Math.Sqrt(ha * ha + hb * hb);
                        double hecc = hc / ha;
                        double hdVal = ha / hecc;
                        sb.AppendLine($"- Tiêu cự 2c = {2 * hc}");
                        sb.AppendLine($"- Tiêu điểm: F₁(-{hc:G6}, 0), F₂({hc:G6}, 0)");
                        sb.AppendLine($"- Đỉnh thực: A₁(-{ha}, 0), A₂({ha}, 0)");
                        sb.AppendLine($"- Đỉnh ảo: B₁(0, -{hb}), B₂(0, {hb})");
                        sb.AppendLine($"- Tâm sai: e = {hecc:G6}");
                        sb.AppendLine($"- Đường chuẩn: x = ±{hdVal:G6}");
                        sb.AppendLine($"- Tiệm cận: y = ±{hb/ha:G6}x");
                    }
                    break;
                case Mode.Parabola:
                    sb.AppendLine("- Loại đường conic: Parabol (Parabola)");
                    if (ParsingHelper.TryParseDouble(txtPP?.Text, out double pp))
                    {
                        sb.AppendLine($"- Tham số tiêu p = {pp}");
                        sb.AppendLine($"- Phương trình chính tắc: y² = {2 * pp}x");
                        sb.AppendLine($"- Tiêu điểm: F({pp / 2.0:G6}, 0)");
                        sb.AppendLine($"- Đỉnh: O(0, 0)");
                        sb.AppendLine($"- Đường chuẩn: x = -{pp / 2.0:G6}");
                    }
                    break;
            }

            sb.AppendLine("==================================================");
            sb.AppendLine("Xuất từ ứng dụng Học tập Đường Conic - QA SmartClass");
            return sb.ToString();
        }

        private void ExportReport_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                switch (_mode)
                {
                    case Mode.Ellipse:
                        if (!ParsingHelper.TryParseDouble(txtEA?.Text, out double ea) || ea <= 0 ||
                            !ParsingHelper.TryParseDouble(txtEB?.Text, out double eb) || eb <= 0 ||
                            ea < 0.01 || ea > 1000 || eb < 0.01 || eb > 1000 || ea < eb)
                        {
                            MessageBox.Show("Vui lòng nhập các thông số Elip hợp lệ trước khi xuất báo cáo.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        break;
                    case Mode.Hyperbola:
                        if (!ParsingHelper.TryParseDouble(txtHA?.Text, out double ha) || ha <= 0 ||
                            !ParsingHelper.TryParseDouble(txtHB?.Text, out double hb) || hb <= 0 ||
                            ha < 0.01 || ha > 1000 || hb < 0.01 || hb > 1000)
                        {
                            MessageBox.Show("Vui lòng nhập các thông số Hyperbol hợp lệ trước khi xuất báo cáo.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        break;
                    case Mode.Parabola:
                        if (!ParsingHelper.TryParseDouble(txtPP?.Text, out double pp) || pp <= 0 ||
                            pp < 0.01 || pp > 1000)
                        {
                            MessageBox.Show("Vui lòng nhập tham số tiêu Parabol hợp lệ trước khi xuất báo cáo.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        break;
                }

                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Tệp văn bản (*.txt)|*.txt|Tệp Markdown (*.md)|*.md",
                    FileName = $"BaoCao_DuongConic_{_mode}.txt",
                    Title = "Lưu báo cáo phân tích đường Conic"
                };

                if (sfd.ShowDialog() == true)
                {
                    string reportText = GenerateReportText();
                    System.IO.File.WriteAllText(sfd.FileName, reportText, System.Text.Encoding.UTF8);
                    MessageBox.Show("Xuất báo cáo phân tích thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất báo cáo: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string Fmt(double v) => v == (int)v ? $"{(int)v}" : $"{v:G6}";
    }
}
