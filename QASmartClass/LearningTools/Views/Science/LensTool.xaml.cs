using System;
using QASmartClass.LearningTools.Helpers;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;
using System.Collections.Generic;

namespace QASmartClass.LearningTools.Views.Science
{
    public partial class LensTool : BaseToolControl
    {
        private bool _updating = false;
        private TextBox? _solvedTextBox;

        public LensTool()
        {
            InitializeComponent();
            // Attach TouchNumPad
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtF, step: 1);
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtD, step: 1);
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtDp, step: 1);

            
            // Build Static UI
            headerContainer.Child = UI.ToolHeader("", "Thấu Kính & Quang Hình Học", 
                "Lớp 11 • 1/f = 1/d + 1/d' • ảnh thật/ảo, phóng đại", 
                Color.FromRgb(232, 245, 233), Color.FromRgb(200, 230, 201), DS.CatScience);
            
            GraphPanel.Children.Add(UI.GraphButton("d' = fd/(d-f)  đường cong ảnh-vật", DS.CatScience, OpenGraph_Click));

            BuildPresets();
            Calc();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextPractice != null) menuTextPractice.Text = isVN ? "Tính toán & Lý thuyết" : "Calculator & Theory";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";

                if (sideMenu != null)
                {
                    sideMenu.SelectionChanged += SideMenu_SelectionChanged;
                    sideMenu.SelectedIndex = 0;
                }

                Calc();
                LoadPracticalApps();
            };
        }

        private void Lens_Changed(object sender, TextChangedEventArgs e)
        {
            if (txtF == null || txtD == null || txtDp == null || _updating) return;

            if (sender is TextBox tb && _solvedTextBox != null)
            {
                if (_solvedTextBox != tb)
                {
                    _updating = true;
                    _solvedTextBox.Text = "";
                    _solvedTextBox = null;
                    _updating = false;
                }
                else
                {
                    _solvedTextBox = null;
                }
            }

            Calc();
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsLoaded) Calc();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded || _updating) return;
            _updating = true;
            if (sender == sliderF && txtF != null) txtF.Text = Fmt(sliderF.Value);
            else if (sender == sliderD && txtD != null) txtD.Text = Fmt(sliderD.Value);
            else if (sender == sliderDp && txtDp != null) txtDp.Text = Fmt(sliderDp.Value);
            _updating = false;
            Calc();
        }

        private void ApplyAppParams_Click(object sender, MouseButtonEventArgs e)
        {
            if (cmbRealWorldApp == null) return;
            _updating = true;
            int idx = cmbRealWorldApp.SelectedIndex;
            if (idx == 0) // Kính lúp
            {
                txtF.Text = "20";
                txtD.Text = "12";
                txtDp.Text = "";
            }
            else if (idx == 1) // Máy ảnh
            {
                txtF.Text = "5";
                txtD.Text = "100";
                txtDp.Text = "";
            }
            else if (idx == 2) // Kính cận
            {
                txtF.Text = "-20";
                txtD.Text = "50";
                txtDp.Text = "";
            }
            else if (idx == 3) // Kính viễn
            {
                txtF.Text = "25";
                txtD.Text = "75";
                txtDp.Text = "";
            }
            else if (idx == 4) // Kính hiển vi
            {
                txtF.Text = "10";
                txtD.Text = "11";
                txtDp.Text = "";
            }
            else if (idx == 5) // Kính thiên văn khúc xạ
            {
                txtF.Text = "100";
                txtD.Text = "oo";
                txtDp.Text = "100";
            }
            _updating = false;
            Calc();
        }

        private void Calc()
        {
            if (resultPanel == null) return;
            resultPanel.Children.Clear();
            
            bool hasF = ParsingHelper.TryParseDouble(txtF?.Text, out double f);
            bool hasD = ParsingHelper.TryParseDouble(txtD?.Text, out double d);
            bool hasDp = ParsingHelper.TryParseDouble(txtDp?.Text, out double dp);

            int known = (hasF ? 1 : 0) + (hasD ? 1 : 0) + (hasDp ? 1 : 0);
            if (known < 2)
            {
                UI.ResultRow("ℹ️ Nhập ít nhất 2 giá trị (f, d, d')", DS.Info, resultPanel);
                DrawPlaceholder("⚠️ Nhập đầy đủ thông tin để hiển thị sơ đồ quang học");
                return;
            }

            // Kiểm soát lỗi biên f = 0
            if (hasF && System.Math.Abs(f) < 1e-6)
            {
                UI.ResultRow("⚠️ Tiêu cự f phải khác 0!", DS.Danger, resultPanel);
                DrawPlaceholder("Tiêu cự f phải khác 0!");
                return;
            }

            // Kiểm soát lỗi biên d < 0 (Vật ảo không hỗ trợ vẽ và không nằm trong THPT)
            if (hasD && d < -1e-6)
            {
                UI.ResultRow("⚠️ Khoảng cách vật d phải không âm (d ≥ 0) đối với vật thật!", DS.Danger, resultPanel);
                DrawPlaceholder("Khoảng cách vật d phải không âm!");
                return;
            }

            // Kiểm soát vật ở quang tâm d = 0
            if (hasD && System.Math.Abs(d) < 1e-6)
            {
                d = 0;
                if (hasF) dp = 0;
            }

            // Kiểm tra tính đồng nhất khi nhập cả 3 giá trị
            if (known == 3)
            {
                if (d != 0 && dp != 0)
                {
                    double left = 1.0 / f;
                    double right = 1.0 / d + 1.0 / dp;
                    if (System.Math.Abs(left - right) > 1e-4)
                    {
                        UI.ResultRow("⚠️ Ba giá trị f, d, d' không thỏa mãn hệ thức thấu kính 1/f = 1/d + 1/d'. Vui lòng kiểm tra lại!", DS.Danger, resultPanel);
                        DrawPlaceholder("Dữ liệu f, d, d' không nhất quán!");
                        return;
                    }
                }
            }

            bool isInfinity = false;

            // Giải giá trị còn thiếu
            _updating = true;
            if (!hasF && hasD && hasDp)
            {
                if (System.Math.Abs(d + dp) < 1e-6)
                {
                    UI.ResultRow("⚠️ Tổng khoảng cách d + d' = 0. Vị trí vật và ảnh không hợp lệ để tính tiêu cự f (vật và ảnh triệt tiêu tiêu cự)!", DS.Danger, resultPanel);
                    DrawPlaceholder("Vị trí vật và ảnh không hợp lệ!");
                    _updating = false;
                    return;
                }
                f = (d * dp) / (d + dp);
                if (txtF != null) txtF.Text = Fmt(f);
                _solvedTextBox = txtF;
                UI.ResultRow($"🔑 f = d·d'/(d+d') = {Fmt(d)}×{Fmt(dp)}/({Fmt(d)}+{Fmt(dp)}) = {Fmt(f)} cm", DS.CatScience, resultPanel);
            }
            else if (!hasDp && hasF && hasD)
            {
                if (System.Math.Abs(d - f) < 1e-10)
                {
                    isInfinity = true;
                    dp = double.PositiveInfinity;
                    if (txtDp != null) txtDp.Text = "oo";
                    _solvedTextBox = txtDp;
                    UI.ResultRow("ℹ️ d = f → Ảnh ở vô cực!", DS.Info, resultPanel);
                }
                else
                {
                    dp = (f * d) / (d - f);
                    if (txtDp != null) txtDp.Text = Fmt(dp);
                    _solvedTextBox = txtDp;
                    UI.ResultRow($"🔑 d' = f·d/(d−f) = {Fmt(f)}×{Fmt(d)}/({Fmt(d)}−{Fmt(f)}) = {Fmt(dp)} cm", DS.CatScience, resultPanel);
                }
            }
            else if (!hasD && hasF && hasDp)
            {
                if (System.Math.Abs(dp - f) < 1e-10)
                {
                    isInfinity = true;
                    d = double.PositiveInfinity;
                    if (txtD != null) txtD.Text = "oo";
                    _solvedTextBox = txtD;
                    UI.ResultRow("ℹ️ d' = f → Vật ở vô cực!", DS.Info, resultPanel);
                }
                else
                {
                    d = (f * dp) / (dp - f);
                    if (txtD != null) txtD.Text = Fmt(d);
                    _solvedTextBox = txtD;
                    UI.ResultRow($"🔑 d = f·d'/(d'−f) = {Fmt(f)}×{Fmt(dp)}/({Fmt(dp)}−{Fmt(f)}) = {Fmt(d)} cm", DS.CatScience, resultPanel);
                }
            }
            else
            {
                _solvedTextBox = null;
                UI.ResultRow($"⚡ f={Fmt(f)} cm, d={Fmt(d)} cm, d'={Fmt(dp)} cm", DS.CatScience, resultPanel);
            }
            _updating = false;

            // Đồng bộ hoá các Slider
            _updating = true;
            if (sliderF != null) sliderF.Value = System.Math.Clamp(f, -100.0, 100.0);
            if (sliderD != null) sliderD.Value = System.Math.Clamp(d, 0.0, 200.0);
            if (sliderDp != null) sliderDp.Value = double.IsInfinity(dp) || double.IsNaN(dp) ? 0 : System.Math.Clamp(dp, -200.0, 200.0);
            _updating = false;

            // Loại thấu kính
            if (f > 0) UI.ResultRow("🔍 Thấu kính HỘI TỤ (f > 0)", DS.Success, resultPanel);
            else UI.ResultRow("🔍 Thấu kính PHÂN KỲ (f < 0)", DS.Danger, resultPanel);

            // Độ phóng đại
            if (isInfinity)
            {
                UI.ResultRow("📐 Độ phóng đại k = Vô cực", DS.ResultSpecial, resultPanel);
            }
            else if (System.Math.Abs(d) < 1e-6)
            {
                UI.ResultRow("📐 Độ phóng đại k = 1 (Vật tại quang tâm)", DS.ResultSpecial, resultPanel);
                UI.ResultRow("📏 Ảnh BẰNG vật", DS.Info, resultPanel);
                UI.ResultRow("⬆️ Ảnh CÙNG CHIỀU (ảo)", DS.Warning, resultPanel);
            }
            else
            {
                double k = -dp / d;
                UI.ResultRow($"📐 Độ phóng đại k = −d'/d = −{Fmt(dp)}/{Fmt(d)} = {k.ToString("G6")}", DS.ResultSpecial, resultPanel);

                if (System.Math.Abs(System.Math.Abs(k) - 1) < 1e-9) UI.ResultRow("📏 Ảnh BẰNG vật", DS.Info, resultPanel);
                else if (System.Math.Abs(k) > 1) UI.ResultRow("📏 Ảnh LỚN hơn vật", DS.Primary, resultPanel);
                else UI.ResultRow("📏 Ảnh NHỎ hơn vật", DS.Warning, resultPanel);

                if (k > 0) UI.ResultRow("⬆️ Ảnh CÙNG CHIỀU (ảo)", DS.Warning, resultPanel);
                else UI.ResultRow("⬇️ Ảnh NGƯỢC CHIỀU (thật)", DS.Success, resultPanel);
            }

            // Loại ảnh
            if (isInfinity)
            {
                UI.ResultRow("🔮 Ảnh ở vô cực", DS.Info, resultPanel);
            }
            else if (dp > 0)
            {
                UI.ResultRow("✅ Ảnh THẬT (d' > 0) — hứng được trên màn", DS.Success, resultPanel);
            }
            else if (dp < 0)
            {
                UI.ResultRow("🔮 Ảnh ẢO (d' < 0) — không hứng được", DS.Danger, resultPanel);
            }
            else
            {
                UI.ResultRow("🔮 Ảnh trùng với vật tại quang tâm", DS.Info, resultPanel);
            }

            // Độ tụ
            if (System.Math.Abs(f) > 1e-10)
            {
                double diopter = 100.0 / f; // f in cm → D in diopter
                UI.ResultRow($"🔆 Độ tụ D = 1/f(m) = 100/f(cm) = 100/{Fmt(f)} = {diopter.ToString("G4")} dp (điốp)", DS.ResultDark, resultPanel);
            }

            // Vẽ sơ đồ quang học
            DrawOpticsDiagram(f, d, dp);
        }

        // ═══ Graph ═══
        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ParsingHelper.TryParseDouble(txtF?.Text, out double f);
                if (System.Math.Abs(f) < 0.1) f = 20;
                var ci = CultureInfo.InvariantCulture;

                string expJs = $@"
        calc.setExpression({{id:'curve', latex:'y=\\frac{{{f.ToString(ci)}x}}{{x-{f.ToString(ci)}}}', color:'#006064', lineWidth:3}});
        calc.setExpression({{id:'bisect', latex:'y=x', color:'#9E9E9E', lineWidth:1, lineStyle:'DASHED'}});
        calc.setExpression({{id:'asymptote_v', latex:'x={f.ToString(ci)}', color:'#E53935', lineStyle:'DASHED', lineWidth:1.5, label:'Tiệm cận d=f', showLabel:true}});
        calc.setExpression({{id:'asymptote_h', latex:'y={f.ToString(ci)}', color:'#E53935', lineStyle:'DASHED', lineWidth:1.5, label:'Tiệm cận d\'=f', showLabel:true}});
        calc.setExpression({{id:'point_2f', latex:'({(2*f).ToString(ci)},{(2*f).ToString(ci)})', color:'#2E7D32', pointSize:10, label:'A(2f, 2f)', showLabel:true}});
        calc.setMathBounds({{ left: {(-System.Math.Abs(f) * 2).ToString(ci)}, right: {(System.Math.Abs(f) * 6).ToString(ci)}, bottom: {(-System.Math.Abs(f) * 4).ToString(ci)}, top: {(System.Math.Abs(f) * 6).ToString(ci)} }});";
                var win = new QASmartClass.LearningTools.Views.Math.GraphWindow(expJs, $"🔭 Thấu kính f={f}cm");
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        // ═══ PRESETS ═══
        private void BuildPresets()
        {
            if (presetPanel == null) return;
            var presets = new (string L, string F, string D, string Dp)[]
            {
                ("f=20, d=30", "20", "30", ""),
                ("f=10, d=15", "10", "15", ""),
                ("f=−15, d=10", "-15", "10", ""),
                ("d=40, d'=40", "", "40", "40"),
                ("f=25, d=50", "25", "50", ""),
            };
            foreach (var (l, fv, dv, dpv) in presets)
            {
                string cf = fv, cd = dv, cdp = dpv;
                UI.PresetButton(l, DS.CatScience, () => { txtF.Text = cf; txtD.Text = cd; txtDp.Text = cdp; }, presetPanel);
            }
        }

        // ═══ DRAWING HELPERS ═══
        private void DrawPlaceholder(string msg)
        {
            if (opticsCanvas == null) return;
            opticsCanvas.Children.Clear();
            var tb = new TextBlock
            {
                Text = msg,
                FontSize = DS.FontLabel,
                FontWeight = FontWeights.Bold,
                Foreground = DS.Brush(DS.ResultInfo),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            var border = new Border
            {
                Width = opticsCanvas.ActualWidth > 0 ? opticsCanvas.ActualWidth : 350,
                Height = opticsCanvas.ActualHeight > 0 ? opticsCanvas.ActualHeight : 280,
                Child = tb
            };
            opticsCanvas.Children.Add(border);
        }

        private void DrawOpticsDiagram(double f, double d, double dp)
        {
            if (opticsCanvas == null) return;
            opticsCanvas.Children.Clear();

            double W = opticsCanvas.ActualWidth;
            double H = opticsCanvas.ActualHeight;
            if (W <= 0 || H <= 0)
            {
                W = 380;
                H = 280;
            }

            double cx = W / 2.0;
            double cy = H / 2.0;

            bool isDark = GetIsDarkMode();

            bool isObjectAtInfinity = double.IsInfinity(d) || double.IsNaN(d);
            bool isImageAtInfinity = double.IsInfinity(dp) || double.IsNaN(dp);

            // 1. Trục chính
            var axisColor = isDark ? new SolidColorBrush(Color.FromRgb(156, 163, 175)) : new SolidColorBrush(Color.FromRgb(117, 117, 117)); // #757575
            DrawLine(10, cy, W - 10, cy, axisColor, 1.5).SetResourceReference(Shape.StrokeProperty, "TextSecondary");
            DrawText("x", W - 20, cy - 20, axisColor).SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");

            // 2. Thấu kính
            var lensColor = DS.Brush(DS.CatScience);
            DrawLine(cx, cy - 100, cx, cy + 100, lensColor, 3).SetResourceReference(Shape.StrokeProperty, "CatScience");
            if (f > 0) // Hội tụ
            {
                DrawLine(cx - 8, cy - 92, cx, cy - 100, lensColor, 2).SetResourceReference(Shape.StrokeProperty, "CatScience");
                DrawLine(cx + 8, cy - 92, cx, cy - 100, lensColor, 2).SetResourceReference(Shape.StrokeProperty, "CatScience");
                DrawLine(cx - 8, cy + 92, cx, cy + 100, lensColor, 2).SetResourceReference(Shape.StrokeProperty, "CatScience");
                DrawLine(cx + 8, cy + 92, cx, cy + 100, lensColor, 2).SetResourceReference(Shape.StrokeProperty, "CatScience");
            }
            else // Phân kỳ
            {
                DrawLine(cx - 8, cy - 100, cx, cy - 92, lensColor, 2).SetResourceReference(Shape.StrokeProperty, "CatScience");
                DrawLine(cx + 8, cy - 100, cx, cy - 92, lensColor, 2).SetResourceReference(Shape.StrokeProperty, "CatScience");
                DrawLine(cx - 8, cy + 100, cx, cy + 92, lensColor, 2).SetResourceReference(Shape.StrokeProperty, "CatScience");
                DrawLine(cx + 8, cy + 100, cx, cy + 92, lensColor, 2).SetResourceReference(Shape.StrokeProperty, "CatScience");
            }
            DrawText("O", cx - 15, cy + 5, lensColor, true).SetResourceReference(TextBlock.ForegroundProperty, "CatScience");

            // 3. Tỷ lệ xích (Scale) chống nén hình ảnh
            double checkD = isObjectAtInfinity ? 0 : d;
            double checkDp = isImageAtInfinity ? 0 : dp;
            
            // Giới hạn tỷ lệ nén tối đa 5 lần tiêu cự
            double absF = System.Math.Abs(f);
            if (absF > 1e-6)
            {
                if (System.Math.Abs(checkD) > 5.0 * absF) checkD = 5.0 * absF * System.Math.Sign(checkD);
                if (System.Math.Abs(checkDp) > 5.0 * absF) checkDp = 5.0 * absF * System.Math.Sign(checkDp);
            }

            double maxVal = System.Math.Max(absF, System.Math.Max(System.Math.Abs(checkD), System.Math.Abs(checkDp)));
            if (maxVal < 10) maxVal = 10;
            double scale = (W / 2.0) / (maxVal * 1.35);

            // Tiêu điểm (Đặt chữ F, F' lên phía trên cy - 18 để tránh đè nhãn A, A')
            double xF = cx - f * scale;
            double xFp = cx + f * scale;
            var focusColor = new SolidColorBrush(Color.FromRgb(198, 40, 40)); // #C62828
            DrawDot(xF, cy, focusColor, 6).SetResourceReference(Shape.FillProperty, "StateError");
            DrawDot(xFp, cy, focusColor, 6).SetResourceReference(Shape.FillProperty, "StateError");
            DrawText("F", xF - 6, cy - 18, focusColor, true).SetResourceReference(TextBlock.ForegroundProperty, "StateError");
            DrawText("F'", xFp - 6, cy - 18, focusColor, true).SetResourceReference(TextBlock.ForegroundProperty, "StateError");

            // Nếu vật sáng ở quang tâm (d = 0), dừng vẽ chi tiết
            if (!isObjectAtInfinity && d < 1e-5)
            {
                return;
            }

            // 4. Vật sáng AB
            double h = 40;
            double yB = cy - h;
            double xA = 0;

            if (!isObjectAtInfinity)
            {
                xA = cx - d * scale;
                var objColor = isDark ? Brushes.White : new SolidColorBrush(Color.FromRgb(33, 33, 33)); // #212121
                DrawLine(xA, cy, xA, yB, objColor, 2.5).SetResourceReference(Shape.StrokeProperty, "TextPrimary");
                // Mũi tên vật sáng AB
                DrawLine(xA - 5, yB + 8, xA, yB, objColor, 2).SetResourceReference(Shape.StrokeProperty, "TextPrimary");
                DrawLine(xA + 5, yB + 8, xA, yB, objColor, 2).SetResourceReference(Shape.StrokeProperty, "TextPrimary");
                DrawText("A", xA - 12, cy + 8, objColor, true).SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
                DrawText("B", xA - 12, yB - 18, objColor, true).SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
            }
            else
            {
                DrawText("Vật ở vô cực (d = ∞)", 20, 20, lensColor, true).SetResourceReference(TextBlock.ForegroundProperty, "CatScience");
            }

            // 5. Ảnh A'B'
            double xAp = cx;
            double k = 1;
            double hp = 0;
            double yBp = cy;
            var imgColor = new SolidColorBrush(Color.FromRgb(46, 125, 50)); // #2E7D32 (Thật)
            DoubleCollection dashStyle = null;

            if (!isImageAtInfinity)
            {
                xAp = cx + dp * scale;
                k = (!isObjectAtInfinity && System.Math.Abs(d) > 1e-10) ? -dp / d : 1;
                hp = k * h;
                yBp = cy - hp;
                imgColor = (dp > 0) ? new SolidColorBrush(Color.FromRgb(46, 125, 50)) : new SolidColorBrush(Color.FromRgb(230, 81, 0)); // #E65100 (Ảo)
                dashStyle = (dp > 0) ? null : new DoubleCollection { 3, 3 };

                if (isObjectAtInfinity) // Ảnh của vật ở vô cực hội tụ tại tiêu điểm
                {
                    DrawDot(xAp, cy, imgColor, 8);
                    DrawText("A' ≡ B'", xAp - 15, cy + 8, imgColor, true);
                }
                else
                {
                    DrawLine(xAp, cy, xAp, yBp, imgColor, 2.5, dashStyle);
                    // Mũi tên ảnh A'B'
                    if (k < 0) // Ngược chiều (chỉ xuống)
                    {
                        DrawLine(xAp - 5, yBp - 8, xAp, yBp, imgColor, 2);
                        DrawLine(xAp + 5, yBp - 8, xAp, yBp, imgColor, 2);
                        DrawText("B'", xAp - 15, yBp + 5, imgColor, true);
                    }
                    else // Cùng chiều (chỉ lên)
                    {
                        DrawLine(xAp - 5, yBp + 8, xAp, yBp, imgColor, 2);
                        DrawLine(xAp + 5, yBp + 8, xAp, yBp, imgColor, 2);
                        DrawText("B'", xAp - 15, yBp - 18, imgColor, true);
                    }
                    DrawText("A'", xAp - 12, cy + 8, imgColor, true);
                }
            }
            else
            {
                DrawText("Ảnh ở vô cực (d' = ∞)", W - 140, 20, focusColor, true).SetResourceReference(TextBlock.ForegroundProperty, "StateError");
            }

            // 6. Vẽ các tia sáng
            var rayColor = new SolidColorBrush(Color.FromRgb(255, 179, 0)); // #FFB300
            double yI = yB;

            // Tia 1: Tia song song trục chính
            // Vật tới Thấu kính (Solid)
            if (!isObjectAtInfinity)
            {
                DrawLine(xA, yB, cx, yI, rayColor, 1.5);
                // Arrowhead
                double midX1 = (xA + cx) / 2.0;
                DrawLine(midX1 - 5, yI - 4, midX1, yI, rayColor, 1.5);
                DrawLine(midX1 - 5, yI + 4, midX1, yI, rayColor, 1.5);
            }
            else
            {
                DrawLine(10, yB, cx, yI, rayColor, 1.5);
                // Arrowhead
                double midX1 = (10 + cx) / 2.0;
                DrawLine(midX1 - 5, yI - 4, midX1, yI, rayColor, 1.5);
                DrawLine(midX1 - 5, yI + 4, midX1, yI, rayColor, 1.5);
            }

            // Khúc xạ Tia 1
            if (isImageAtInfinity)
            {
                double m = h / (f * scale);
                double endX = W - 10;
                double endY = yI + m * (endX - cx);
                DrawLine(cx, yI, endX, endY, rayColor, 1.5);
            }
            else
            {
                double m = (yBp - yI) / (xAp - cx);
                double endX = W - 10;
                double endY = yI + m * (endX - cx);
                DrawLine(cx, yI, endX, endY, rayColor, 1.5);

                if (dp < 0)
                {
                    DrawLine(cx, yI, xAp, yBp, rayColor, 1.5, new DoubleCollection { 2, 2 });
                }
            }

            // Tia 2: Tia qua quang tâm O
            if (isImageAtInfinity)
            {
                if (isObjectAtInfinity)
                {
                    DrawLine(10, cy, W - 10, cy, rayColor, 1.5);
                }
                else
                {
                    double endX = W - 10;
                    double endY = cy + (cy - yB) / (cx - xA) * (endX - cx);
                    DrawLine(xA, yB, endX, endY, rayColor, 1.5);
                }
            }
            else
            {
                if (isObjectAtInfinity)
                {
                    DrawLine(10, cy, W - 10, cy, rayColor, 1.5);
                }
                else
                {
                    DrawLine(xA, yB, cx, cy, rayColor, 1.5);
                    // Arrowhead
                    double midX2 = (xA + cx) / 2.0;
                    double midY2 = (yB + cy) / 2.0;
                    DrawLine(midX2 - 6, midY2, midX2, midY2, rayColor, 1.5);
                    DrawLine(midX2, midY2 - 6, midX2, midY2, rayColor, 1.5);

                    double m = (yBp - cy) / (xAp - cx);
                    double endX = W - 10;
                    double endY = cy + m * (endX - cx);
                    DrawLine(cx, cy, endX, endY, rayColor, 1.5);

                    if (dp < 0)
                    {
                        DrawLine(cx, cy, xAp, yBp, rayColor, 1.5, new DoubleCollection { 2, 2 });
                    }
                }
            }

            // Tia 3: Tia qua tiêu điểm vật chính F (Sư phạm: Giúp học sinh học 3 tia đặc biệt)
            if (!isObjectAtInfinity && !isImageAtInfinity)
            {
                if (System.Math.Abs(cx - xA) > 1e-5 && System.Math.Abs(xF - xA) > 1e-5)
                {
                    double m3 = (cy - yB) / (xF - xA);
                    double yJ = yB + m3 * (cx - xA);
                    
                    // Chỉ vẽ tia 3 nếu điểm tới nằm trong kích thước thấu kính (cy +/- 120) để hình vẽ gọn gàng
                    if (System.Math.Abs(yJ - cy) < 120)
                    {
                        // Vẽ tia tới
                        DrawLine(xA, yB, cx, yJ, rayColor, 1.5);
                        
                        // Vẽ tia ló song song trục chính sang phải
                        DrawLine(cx, yJ, W - 10, yJ, rayColor, 1.5);
                        
                        // Nếu là ảnh ảo (dp < 0), vẽ đường kéo dài của tia ló về phía ảnh B'
                        if (dp < 0)
                        {
                            DrawLine(cx, yJ, xAp, yBp, rayColor, 1.5, new DoubleCollection { 2, 2 });
                        }
                    }
                }
            }
        }

        private bool GetIsDarkMode()
        {
            try
            {
                var shellType = Type.GetType("QASmartClass.StudentClient.Views.StudentShell, QASmartClass");
                if (shellType != null)
                {
                    var prop = shellType.GetProperty("IsDarkMode", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (prop != null)
                    {
                        return (bool)prop.GetValue(null);
                    }
                }
            }
            catch
            {
                // Fallback to false if not found
            }
            return false;
        }

        public void ApplyTheme()
        {
            Calc();
        }

        private Line DrawLine(double x1, double y1, double x2, double y2, Brush stroke, double thickness, DoubleCollection dash = null)
        {
            var line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = stroke,
                StrokeThickness = thickness,
                StrokeDashArray = dash
            };
            opticsCanvas.Children.Add(line);
            return line;
        }

        private Ellipse DrawDot(double x, double y, Brush fill, double size)
        {
            var dot = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = fill
            };
            Canvas.SetLeft(dot, x - size / 2.0);
            Canvas.SetTop(dot, y - size / 2.0);
            opticsCanvas.Children.Add(dot);
            return dot;
        }

        private TextBlock DrawText(string text, double x, double y, Brush foreground, bool isBold = false)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = DS.FontSubtitle,
                FontFamily = DS.FontPrimary,
                Foreground = foreground,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal
            };
            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, y);
            opticsCanvas.Children.Add(tb);
            return tb;
        }

        // ═══ HELPERS ═══
        private static string Fmt(double v) => System.Math.Abs(v - System.Math.Round(v)) < 1e-9 ? ((long)System.Math.Round(v)).ToString() : v.ToString("G6");

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rootGrid == null || sideMenu == null || contentCalc == null || gridApp == null)
                return;

            contentCalc.Visibility = Visibility.Collapsed;
            gridApp.Visibility = Visibility.Collapsed;

            int index = sideMenu.SelectedIndex;
            if (index == 0)
            {
                contentCalc.Visibility = Visibility.Visible;
            }
            else if (index == 1)
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
                        Icon = "🔬",
                        Title = isVN ? "Kính lúp và Kính hiển vi" : "Magnifiers & Microscopes",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_lens_1_{suffix}.png",
                        Description = isVN 
                            ? "Thấu kính hội tụ có tiêu cự ngắn (f > 0) được dùng làm kính lúp. Khi đặt vật trong khoảng tiêu cự (d < f), thấu kính tạo ra một ảnh ảo cùng chiều, lớn hơn vật hiện ra trước mắt (d' < 0). Kính hiển vi kết hợp hai thấu kính hội tụ (vật kính và thị kính) để phóng đại các vật siêu nhỏ như tế bào, vi khuẩn." 
                            : "Convex lenses with short focal lengths (f > 0) act as magnifying glasses. Placing an object inside the focal point (d < f) produces an upright, magnified virtual image (d' < 0). Microscopes combine two convex lenses (objective and eyepiece) to view microscopic cells and bacteria."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📷",
                        Title = isVN ? "Thấu kính máy ảnh và Con mắt" : "Camera Lenses & Human Eye",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_lens_2_{suffix}.png",
                        Description = isVN 
                            ? "Máy ảnh sử dụng hệ thấu kính hội tụ để tạo ra ảnh thật, ngược chiều và nhỏ hơn vật trên cảm biến (d > 2f, d' > 0). Tương tự, thủy tinh thể của con mắt đóng vai trò là một thấu kính hội tụ tự điều chỉnh tiêu cự để hội tụ ánh sáng từ vật lên võng mạc, giúp chúng ta nhìn thấy hình ảnh rõ nét." 
                            : "Cameras use convex lens systems to project real, inverted, and minified images onto digital sensors (d > 2f, d' > 0). Similarly, the crystalline lens of the human eye acts as an adjustable convex lens that focuses incoming light onto the retina for clear vision."
                    },
                    new PracticalAppItem
                    {
                        Icon = "👓",
                        Title = isVN ? "Kính chữa tật khúc xạ mắt" : "Corrective Eyeglasses",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_lens_3_{suffix}.png",
                        Description = isVN 
                            ? "Thấu kính phân kỳ (f < 0) tạo ảnh ảo cùng chiều, nhỏ hơn vật. Kính phân kỳ được dùng để chữa tật cận thị, giúp dịch chuyển ảnh của các vật ở xa về tiêu điểm của mắt. Ngược lại, thấu kính hội tụ (f > 0) được dùng làm kính viễn thị để hỗ trợ người cao tuổi hoặc người viễn thị đọc sách rõ ràng ở cự ly gần." 
                            : "Concave lenses (f < 0) produce upright, minified virtual images and are used to correct myopia (nearsightedness) by shifting distant images to the eye's focal plane. Conversely, convex lenses (f > 0) correct hyperopia (farsightedness), helping people read books clearly at close distances."
                    }
                };

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for LensTool: {Err}", ex.Message);
            }
        }
    }
}