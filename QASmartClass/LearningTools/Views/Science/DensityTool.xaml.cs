using QASmartClass.LearningTools.Models;
using System;
using QASmartClass.LearningTools.Helpers;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Science
{
    public partial class DensityTool : BaseToolControl
    {
        private bool _isUpdating;
        private Math.GraphWindow? _activeGraphWin;

        private static readonly double[] MassMul = { 1, 0.001, 1000 };       // kg, g, tan -> kg
        private static readonly double[] VolMul = { 1, 1e-6, 0.001, 1e-6 };  // m3, cm3, L, mL -> m3
        private static readonly double[] DensMul = { 1, 1000 };              // kg/m3, g/cm3 -> kg/m3

        private static readonly (string Name, double Density, string Category, string Icon, string Color)[] Materials =
        {
            ("Hydrogen (Hydro)", 0.0899, "Khí", "\U0001F4A8", "#42A5F5"),
            ("Helium (Heli)", 0.1785, "Khí", "\U0001F388", "#42A5F5"),
            ("Không khí (ở 15 °C)", 1.225, "Khí", "\U0001F32C\uFE0F", "#42A5F5"),
            ("Carbon dioxide (CO₂)", 1.977, "Khí", "\u2601\uFE0F", "#42A5F5"),
            ("Xốp (Styrofoam)", 25, "Nhân tạo", "\U0001F4E6", "#AB47BC"),
            ("Gỗ thông", 500, "Chất rắn", "\U0001FAB5", "#8D6E63"),
            ("Xăng", 750, "Chất lỏng", "\u26FD", "#FF7043"),
            ("Gỗ sồi", 750, "Chất rắn", "\U0001F333", "#8D6E63"),
            ("Ethanol (Rượu/Cồn)", 789, "Chất lỏng", "\U0001F377", "#FF7043"),
            ("Dầu ăn", 920, "Chất lỏng", "\U0001FAD2", "#FF7043"),
            ("Nhựa PE", 950, "Nhân tạo", "\u267B\uFE0F", "#AB47BC"),
            ("Nước tinh khiết (ở 4 °C)", 1000, "Chất lỏng", "\U0001F4A7", "#2196F3"),
            ("Nước biển", 1025, "Chất lỏng", "\U0001F30A", "#2196F3"),
            ("Sữa tươi", 1030, "Chất lỏng", "\U0001F95B", "#FF7043"),
            ("Cao su", 1200, "Nhân tạo", "\U0001F534", "#AB47BC"),
            ("Nhựa PVC", 1400, "Nhân tạo", "\U0001F9F1", "#AB47BC"),
            ("Mật ong", 1420, "Chất lỏng", "\U0001F36F", "#FF7043"),
            ("Đá tự nhiên", 2500, "Chất rắn", "\U0001FAA8", "#8D6E63"),
            ("Thủy tinh (Kính)", 2500, "Chất rắn", "\U0001F50D", "#8D6E63"),
            ("Bê tông", 2400, "Chất rắn", "\U0001F3D7\uFE0F", "#8D6E63"),
            ("Nhôm (Aluminium)", 2700, "Kim loại", "\u2699\uFE0F", "#455A64"),
            ("Thép", 7850, "Kim loại", "\U0001F529", "#455A64"),
            ("Sắt (Iron)", 7874, "Kim loại", "\U0001F9F2", "#455A64"),
            ("Đồng (Copper)", 8960, "Kim loại", "\U0001F527", "#455A64"),
            ("Bạc (Silver)", 10490, "Kim loại", "\U0001FA99", "#455A64"),
            ("Chì (Lead)", 11340, "Kim loại", "\u2B1B", "#455A64"),
            ("Thủy ngân (Mercury)", 13546, "Chất lỏng", "\U0001F321\uFE0F", "#FF7043"),
            ("Vàng (Gold)", 19300, "Kim loại", "\U0001F947", "#FF6F00"),
            ("Bạch kim (Platinum)", 21450, "Kim loại", "\U0001F48E", "#455A64"),
            ("Osmium", 22590, "Kim loại", "\U0001F3C6", "#455A64"),
        };

        public DensityTool()
        {
            InitializeComponent();
            // Attach TouchNumPad
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtMass, step: 1);
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtVolume, step: 1);
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtDensity, step: 1);

            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextCalculator != null) menuTextCalculator.Text = isVN ? "Tính toán" : "Calculator";
                if (menuTextTable != null) menuTextTable.Text = isVN ? "Bảng tra cứu" : "Lookup Table";
                if (menuTextFloat != null) menuTextFloat.Text = isVN ? "Nổi/Chìm" : "Float / Sink";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                BuildPresets();
                BuildTable();
                BuildFloatSink();
                LoadPracticalApps();
                ShowWelcomeCard();

                if (sideMenu != null) sideMenu.SelectedIndex = 0;
            };
        }

        // === CALCULATOR ===
        private void Calc_Changed(object sender, EventArgs e)
        {
            if (_isUpdating || calcResultPanel == null || !IsLoaded) return;
            calcResultPanel.Children.Clear();

            // 1. Kiểm tra tính hợp lệ của dữ liệu nhập vào (Validation)
            bool hasMassText = !string.IsNullOrWhiteSpace(txtMass?.Text);
            bool hasVolText = !string.IsNullOrWhiteSpace(txtVolume?.Text);
            bool hasDensityText = !string.IsNullOrWhiteSpace(txtDensity?.Text);

            // Nếu không có ô nào được điền, hiện thị card chào mừng và reset đồ thị
            if (!hasMassText && !hasVolText && !hasDensityText)
            {
                ShowWelcomeCard();
                UpdateActiveGraph();
                return;
            }

            bool massValid = ParsingHelper.TryParseDouble(txtMass?.Text, out double mVal) && mVal > 0;
            bool volValid = ParsingHelper.TryParseDouble(txtVolume?.Text, out double vVal) && vVal > 0;
            bool densValid = ParsingHelper.TryParseDouble(txtDensity?.Text, out double dVal) && dVal > 0;

            // Phát hiện lỗi nhập số âm hoặc bằng 0
            if ((hasMassText && !massValid) || (hasVolText && !volValid) || (hasDensityText && !densValid))
            {
                UI.ResultRow("⚠️ Các giá trị nhập vào phải là số dương lớn hơn 0.", "#C62828", calcResultPanel);
                return;
            }

            double massKg = GetMassKg();
            double volM3 = GetVolM3();
            double densKgM3 = GetDensKgM3();
            bool hasM = massKg > 0;
            bool hasV = volM3 > 0;
            bool hasD = densKgM3 > 0;

            // Kiểm tra tính mâu thuẫn (bất nhất quán) khi điền cả 3 ô
            if (hasM && hasV && hasD)
            {
                double rhoCalc = massKg / volM3;
                double diff = System.Math.Abs(rhoCalc - densKgM3) / rhoCalc;
                if (diff > 0.01) // Sai lệch lớn hơn 1%
                {
                    string densUnitStr = cmbDensUnit.SelectedIndex == 0 ? "kg/m³" : "g/cm³";
                    string rCalcS = FormatToPedagogicalScientific(rhoCalc);
                    string rCalcSelectedS = FormatToPedagogicalScientific(rhoCalc / DensMul[cmbDensUnit.SelectedIndex]);
                    string rInputS = FormatToPedagogicalScientific(dVal);

                    UI.ResultRow($"⚠️ Nhập liệu mâu thuẫn: Khối lượng riêng tính từ m và V là {rCalcS} kg/m³ ({rCalcSelectedS} {densUnitStr}), khác với giá trị {rInputS} {densUnitStr} bạn đã nhập.", "#E65100", calcResultPanel);
                }
            }

            // 2. Thực hiện tính toán và định dạng đầu ra chuẩn sư phạm
            if (hasM && hasV)
            {
                double rho = massKg / volM3;
                double rhoGcm3 = rho / 1000.0;
                double rhoSelected = rho / DensMul[cmbDensUnit.SelectedIndex];
                string densUnitStr = cmbDensUnit.SelectedIndex == 0 ? "kg/m³" : "g/cm³";
                
                UI.ResultRow($"Khối lượng riêng ρ = m / V = {FormatToPedagogicalScientific(massKg)} kg / {FormatToPedagogicalScientific(volM3)} m³ = {FormatToPedagogicalScientific(rho)} kg/m³", "#1565C0", calcResultPanel);
                if (cmbDensUnit.SelectedIndex == 1)
                {
                    UI.ResultRow($"Quy đổi sang đơn vị đã chọn: ρ = {FormatToPedagogicalScientific(rhoSelected)} g/cm³", "#1565C0", calcResultPanel);
                }
                else
                {
                    UI.ResultRow($"Quy đổi đơn vị: ρ = {FormatToPedagogicalScientific(rho)} kg/m³ = {FormatToPedagogicalScientific(rhoGcm3)} g/cm³ (hoặc kg/L)", "#1565C0", calcResultPanel);
                }

                if (rho < 10) // Trường hợp đặc biệt: Chất khí
                {
                    if (rho < 1.225)
                    {
                        UI.ResultRow($"🎈 Chất khí nhẹ hơn không khí (ρ < 1,225 kg/m³) nên sẽ BAY LÊN (nổi trong không khí).", "#1565C0", calcResultPanel);
                    }
                    else
                    {
                        UI.ResultRow($"☁️ Chất khí nặng hơn không khí (ρ ≥ 1,225 kg/m³) nên sẽ CHÌM xuống và tích tụ sát mặt đất.", "#C62828", calcResultPanel);
                    }
                }
                else // Chất rắn/lỏng thông thường
                {
                    UI.ResultRow(rho < 1000
                        ? "🔵 Vật NỔI trên mặt nước (ρ < 1000 kg/m³)"
                        : "🔴 Vật CHÌM trong nước (ρ ≥ 1000 kg/m³)",
                        rho < 1000 ? "#1565C0" : "#C62828", calcResultPanel);
                }

                var closest = Materials.OrderBy(m => System.Math.Abs(m.Density - rho)).First();
                UI.ResultRow($"Gần giống chất liệu nhất: {closest.Icon} {closest.Name} (ρ ≈ {FormatToPedagogicalScientific(closest.Density)} kg/m³)", closest.Color, calcResultPanel);
            }
            else if (hasM && hasD)
            {
                double vol = massKg / densKgM3;
                UI.ResultRow($"Thể tích V = m / ρ = {FormatToPedagogicalScientific(massKg)} kg / {FormatToPedagogicalScientific(densKgM3)} kg/m³ = {FormatToPedagogicalScientific(vol)} m³", "#2E7D32", calcResultPanel);
                
                string convStr = $"Quy đổi đơn vị: V = {FormatToPedagogicalScientific(vol)} m³";
                if (vol >= 0.001) convStr += $" = {FormatToPedagogicalScientific(vol * 1000)} L";
                convStr += $" = {FormatToPedagogicalScientific(vol * 1e6)} cm³ (mL)";
                UI.ResultRow(convStr, "#1B5E20", calcResultPanel);
            }
            else if (hasV && hasD)
            {
                double mass = densKgM3 * volM3;
                UI.ResultRow($"Khối lượng m = ρ × V = {FormatToPedagogicalScientific(densKgM3)} kg/m³ × {FormatToPedagogicalScientific(volM3)} m³ = {FormatToPedagogicalScientific(mass)} kg", "#E65100", calcResultPanel);
                
                string convStr = $"Quy đổi đơn vị: m = {FormatToPedagogicalScientific(mass)} kg";
                convStr += $" = {FormatToPedagogicalScientific(mass * 1000)} g";
                if (mass >= 1000) convStr += $" = {FormatToPedagogicalScientific(mass / 1000.0)} tấn";
                UI.ResultRow(convStr, "#BF360C", calcResultPanel);
            }

            // Cập nhật đồ thị thời gian thực nếu đang mở
            UpdateActiveGraph();
        }

        private double GetMassKg()
        {
            if (!ParsingHelper.TryParseDouble(txtMass?.Text, out double v) || v <= 0) return -1;
            return v * MassMul[cmbMassUnit?.SelectedIndex ?? 0];
        }
        private double GetVolM3()
        {
            if (!ParsingHelper.TryParseDouble(txtVolume?.Text, out double v) || v <= 0) return -1;
            return v * VolMul[cmbVolUnit?.SelectedIndex ?? 0];
        }
        private double GetDensKgM3()
        {
            if (!ParsingHelper.TryParseDouble(txtDensity?.Text, out double v) || v <= 0) return -1;
            return v * DensMul[cmbDensUnit?.SelectedIndex ?? 0];
        }

        private void ShowWelcomeCard()
        {
            if (calcResultPanel == null) return;
            calcResultPanel.Children.Clear();

            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(24, 20, 24, 20),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1)
            };
            border.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 12, ShadowDepth = 2, Opacity = 0.08 };

            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = "👋 Chào mừng bạn đến với công cụ Khối Lượng Riêng!",
                FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(78, 52, 46)),
                Margin = new Thickness(0, 0, 0, 12)
            });
            sp.Children.Add(new TextBlock
            {
                Text = "Đây là công cụ tương tác thông minh giúp thầy cô và các em học sinh dễ dàng tính toán, quy đổi đơn vị và mô phỏng trực quan.",
                FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)), TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 16)
            });

            var steps = new string[]
            {
                "1️⃣ Nhập 2 trong 3 đại lượng ở ô bên trái để tự động tính đại lượng còn lại.",
                "2️⃣ Sử dụng phần 'Tính nhanh' để chọn nhanh các chất liệu phổ biến.",
                "3️⃣ Nhấp nút 'Xem đồ thị' để xem biểu diễn hình học tương ứng.",
                "4️⃣ Tra cứu hơn 30 chất liệu và xem thí nghiệm ảo Nổi/Chìm ở các Tab bên trên."
            };

            foreach (var step in steps)
            {
                sp.Children.Add(new TextBlock
                {
                    Text = step, FontSize = 13.5, Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                    TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8)
                });
            }

            border.Child = sp;
            calcResultPanel.Children.Add(border);
        }

        // === RESET ===
        private void ResetAll_Click(object sender, MouseButtonEventArgs e)
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 1)
            {
                sideMenu.SelectedIndex = 1;
            }
            _isUpdating = true;
            txtMass.Text = ""; txtVolume.Text = ""; txtDensity.Text = "";
            cmbMassUnit.SelectedIndex = 0; cmbVolUnit.SelectedIndex = 0; cmbDensUnit.SelectedIndex = 0;
            _isUpdating = false;
            calcResultPanel?.Children.Clear();
            
            // Hiện thị lại card chào mừng
            ShowWelcomeCard();
            
            // Đồng bộ hóa đồ thị Desmos về trạng thái mặc định ngay khi reset
            UpdateActiveGraph();
        }

        // === PRESETS ===
        private void BuildPresets()
        {
            if (presetPanel == null) return;
            var presets = new (string Label, double Density, string Color)[]
            {
                ("\U0001F4A7 Nước", 1000, "#2196F3"),
                ("\U0001F947 Vàng", 19300, "#FF6F00"),
                ("\u2699\uFE0F Nhôm", 2700, "#78909C"),
                ("\U0001F529 Thép", 7850, "#455A64"),
                ("\U0001FAB5 Gỗ thông", 500, "#8D6E63"),
                ("\U0001FAD2 Dầu ăn", 920, "#FF7043"),
                ("\U0001F9F2 Sắt", 7874, "#455A64"),
                ("\U0001F32C\uFE0F Không khí", 1.225, "#42A5F5"),
            };
            foreach (var (label, density, color) in presets)
            {
                double d = density;
                UI.PresetButton(label, (Color)ColorConverter.ConvertFromString(color), () =>
                {
                    _isUpdating = true;
                    txtDensity.Text = FormatVietnamese(d / DensMul[cmbDensUnit.SelectedIndex]);
                    txtMass.Text = "1";
                    cmbMassUnit.SelectedIndex = 0;
                    txtVolume.Text = "";
                    _isUpdating = false;
                    Calc_Changed(this, EventArgs.Empty);
                }, presetPanel);
            }
        }

        // === TABLE (Tab 2) ===
        private void BuildTable()
        {
            if (tablePanel == null) return;
            tablePanel.Children.Clear();

            tablePanel.Children.Add(new TextBlock
            {
                Text = "\U0001F4CB Bảng Khối Lượng Riêng — Hơn 30 Chất Liệu",
                FontSize = DS.FontTitle - 3, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary,
                Foreground = new SolidColorBrush(Color.FromRgb(78, 52, 46)),
                Margin = new Thickness(0, 0, 0, 12)
            });

            double maxDensity = Materials.Max(m => m.Density);
            string lastCat = "";

            foreach (var mat in Materials.OrderBy(m => m.Category).ThenBy(m => m.Density))
            {
                if (mat.Category != lastCat)
                {
                    lastCat = mat.Category;
                    var catHeader = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(78, 52, 46)),
                        CornerRadius = new CornerRadius(8, 8, 0, 0),
                        Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 12, 0, 0)
                    };
                    catHeader.Child = new TextBlock
                    {
                        Text = $"\U0001F4C2 {mat.Category}", FontSize = DS.FontResult, FontWeight = FontWeights.Bold,
                        FontFamily = DS.FontPrimary, Foreground = Brushes.White
                    };
                    tablePanel.Children.Add(catHeader);
                }

                bool floats = mat.Density < 1000;
                var c = (Color)ColorConverter.ConvertFromString(mat.Color);

                var row = new Border
                {
                    Background = Brushes.White, Padding = new Thickness(14, 8, 14, 8),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(238, 238, 238)),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Cursor = Cursors.Hand, ToolTip = "Nhấp để tính nhanh chất này"
                };

                var g = new Grid();
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var info = new DockPanel();
                info.Children.Add(new TextBlock
                {
                    Text = $"{mat.Icon} {mat.Name}", FontSize = DS.FontPreset, FontWeight = FontWeights.SemiBold,
                    FontFamily = DS.FontPrimary, Foreground = new SolidColorBrush(c)
                });
                var rightPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
                rightPanel.Children.Add(new TextBlock
                {
                    Text = $"{FormatVietnamese(mat.Density)} kg/m\u00B3", FontSize = DS.FontPreset, FontFamily = DS.FontPrimary,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                    Margin = new Thickness(0, 0, 12, 0)
                });
                rightPanel.Children.Add(new TextBlock
                {
                    Text = floats ? "\U0001F535 Nổi" : "\U0001F534 Chìm", FontSize = 10,
                    FontFamily = DS.FontPrimary, VerticalAlignment = VerticalAlignment.Center
                });
                DockPanel.SetDock(rightPanel, Dock.Right);
                info.Children.Add(rightPanel);
                Grid.SetRow(info, 0);
                g.Children.Add(info);

                // Density bar
                double ratio = System.Math.Log10(System.Math.Max(mat.Density, 0.01) + 1) /
                               System.Math.Log10(maxDensity + 1);
                var barBg = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                    CornerRadius = new CornerRadius(3), Height = 5, Margin = new Thickness(0, 4, 0, 0)
                };
                var barFill = new Border
                {
                    Background = new SolidColorBrush(c),
                    CornerRadius = new CornerRadius(3), Height = 5,
                    HorizontalAlignment = HorizontalAlignment.Left, Width = 0
                };
                var barCont = new Grid();
                barCont.Children.Add(barBg);
                barCont.Children.Add(barFill);
                Grid.SetRow(barCont, 1);
                g.Children.Add(barCont);

                row.Child = g;

                double r = ratio;
                row.Loaded += (_, _) => { barFill.Width = System.Math.Max(barCont.ActualWidth * r, 4); };

                double dens = mat.Density;
                row.MouseLeftButtonDown += (_, _) =>
                {
                    _isUpdating = true;
                    txtDensity.Text = FormatVietnamese(dens / DensMul[cmbDensUnit.SelectedIndex]);
                    txtMass.Text = "1"; cmbMassUnit.SelectedIndex = 0;
                    txtVolume.Text = "";
                    _isUpdating = false;
                    Calc_Changed(this, EventArgs.Empty);
                };

                tablePanel.Children.Add(row);
            }
        }

        // === FLOAT/SINK (Tab 3) ===
        private void BuildFloatSink()
        {
            if (floatPanel == null) return;
            floatPanel.Children.Clear();

            floatPanel.Children.Add(new TextBlock
            {
                Text = "\U0001F52C Thí Nghiệm Nổi / Chìm (So với nước)",
                FontSize = DS.FontTitle - 3, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary,
                Foreground = new SolidColorBrush(Color.FromRgb(78, 52, 46)),
                Margin = new Thickness(0, 0, 0, 6)
            });
            floatPanel.Children.Add(new TextBlock
            {
                Text = "Vật có \u03C1 < 1000 kg/m\u00B3 sẽ NỔI, \u03C1 \u2265 1000 kg/m\u00B3 sẽ CHÌM trong nước nguyên chất",
                FontSize = DS.FontPreset, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.ResultInfo),
                Margin = new Thickness(0, 0, 0, 16)
            });

            // Water line
            floatPanel.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                Height = 3, CornerRadius = new CornerRadius(2), Margin = new Thickness(0, 0, 0, 4)
            });
            floatPanel.Children.Add(new TextBlock
            {
                Text = "\U0001F4A7 \u2014\u2014\u2014 Mặt nước (\u03C1 = 1000 kg/m\u00B3) \u2014\u2014\u2014",
                FontSize = DS.FontPreset, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                Margin = new Thickness(0, 0, 0, 12)
            });

            // Float section
            AddFloatSection("\U0001F535 NỔI (\u03C1 < 1000 kg/m\u00B3)", "#E3F2FD", "#1565C0", true);
            // Sink section
            AddFloatSection("\U0001F534 CHÌM (\u03C1 \u2265 1000 kg/m\u00B3)", "#FFEBEE", "#C62828", false);
        }

        private void AddFloatSection(string title, string bgColor, string textColor, bool isFloat)
        {
            var tc = (Color)ColorConverter.ConvertFromString(textColor);
            var bc = (Color)ColorConverter.ConvertFromString(bgColor);

            floatPanel.Children.Add(new TextBlock
            {
                Text = title, FontSize = DS.FontLabel, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary,
                Foreground = new SolidColorBrush(tc), Margin = new Thickness(0, 8, 0, 6)
            });

            var filtered = Materials.Where(m => isFloat ? m.Density < 1000 : m.Density >= 1000)
                                    .OrderBy(m => m.Density);

            var wrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
            foreach (var mat in filtered)
            {
                var card = new Border
                {
                    Background = new SolidColorBrush(bc),
                    CornerRadius = new CornerRadius(DS.RadiusChip + 2),
                    Padding = new Thickness(DS.PadChip, 8, DS.PadChip, 8),
                    Margin = new Thickness(0, 0, DS.TouchGap, DS.TouchGap),
                    MinWidth = 140, MinHeight = DS.TouchMinHeight,
                    BorderBrush = DS.BrushAlpha(tc, 60),
                    BorderThickness = new Thickness(0, 0, 0, 2)
                };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = $"{mat.Icon} {mat.Name}", FontSize = DS.FontPreset, FontWeight = FontWeights.SemiBold,
                    FontFamily = DS.FontPrimary, Foreground = new SolidColorBrush(tc)
                });
                sp.Children.Add(new TextBlock
                {
                    Text = $"{FormatVietnamese(mat.Density)} kg/m\u00B3", FontSize = DS.FontNote, FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(DS.ResultInfo)
                });
                card.Child = sp;
                wrap.Children.Add(card);
            }
            floatPanel.Children.Add(wrap);
        }

        // === HELPERS ===
        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                GetGraphData(out double rho, out double massKg, out double volM3, out bool hasM, out bool hasV);

                string massUnit = (cmbMassUnit.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "kg";
                string volUnit = (cmbVolUnit.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "m³";
                double factor = VolMul[cmbVolUnit.SelectedIndex] / MassMul[cmbMassUnit.SelectedIndex];

                double volUser = volM3 / VolMul[cmbVolUnit.SelectedIndex];
                double massUser = massKg / MassMul[cmbMassUnit.SelectedIndex];
                double rhoUser = rho * factor;

                var ci = System.Globalization.CultureInfo.InvariantCulture;
                string sRhoUser = rhoUser.ToString("G6", ci);
                string sMassUser = massUser.ToString("G6", ci);
                string sVolUser = volUser.ToString("G6", ci);

                string sWaterSlope = (1000 * factor).ToString("G6", ci);
                string sAlumSlope = (2700 * factor).ToString("G6", ci);
                string sIronSlope = (7874 * factor).ToString("G6", ci);
                string sGoldSlope = (19300 * factor).ToString("G6", ci);

                double xMax = 0.01;
                double yMax = 200;
                if (hasM && hasV)
                {
                    xMax = volUser * 1.5;
                    yMax = massUser * 1.5;
                }
                else
                {
                    double refVol = 100.0;
                    if (cmbVolUnit.SelectedIndex == 0) refVol = 0.1; // m3
                    if (rho < 10 && cmbVolUnit.SelectedIndex == 0) refVol = 10.0;
                    else if (rho > 5000 && cmbVolUnit.SelectedIndex == 0) refVol = 0.01;

                    xMax = refVol * 1.5;
                    yMax = (rhoUser * refVol) * 1.5;
                }

                double xMin = -xMax * 0.15;
                double yMin = -yMax * 0.15;

                string sXMin = xMin.ToString("G6", ci);
                string sXMax = xMax.ToString("G6", ci);
                string sYMin = yMin.ToString("G6", ci);
                string sYMax = yMax.ToString("G6", ci);

                string wLabel = $"Nước ({FormatVietnamese(1000 * factor)} {massUnit}/{volUnit})";
                string alLabel = $"Nhôm ({FormatVietnamese(2700 * factor)} {massUnit}/{volUnit})";
                string feLabel = $"Sắt ({FormatVietnamese(7874 * factor)} {massUnit}/{volUnit})";
                string auLabel = $"Vàng ({FormatVietnamese(19300 * factor)} {massUnit}/{volUnit})";
                string currLabel = $"ρ = {FormatVietnamese(rhoUser)} {massUnit}/{volUnit} (Hiện tại)";

                string expJs = $@"
        calc.setExpression({{id:'d1', latex:'y={sWaterSlope}x', color:'#2196F3', lineWidth:2, label:'{wLabel}', showLabel:true}});
        calc.setExpression({{id:'d2', latex:'y={sAlumSlope}x', color:'#90A4AE', lineWidth:2, label:'{alLabel}', showLabel:true}});
        calc.setExpression({{id:'d3', latex:'y={sIronSlope}x', color:'#795548', lineWidth:2, label:'{feLabel}', showLabel:true}});
        calc.setExpression({{id:'d4', latex:'y={sGoldSlope}x', color:'#FFD700', lineWidth:2, label:'{auLabel}', showLabel:true}});
        calc.setExpression({{id:'d_curr', latex:'y={sRhoUser}x', color:'#E65100', lineWidth:4, label:'{currLabel}', showLabel:true}});
        calc.updateSettings({{ xAxisLabel: 'Thể tích V ({volUnit})', yAxisLabel: 'Khối lượng m ({massUnit})', showGrid: true, showDirections: true }});";

                if (hasM && hasV)
                {
                    expJs += $@"
        calc.setExpression({{id:'p_curr', latex:'({sVolUser}, {sMassUser})', color:'#C62828', pointSize:12, label:'m={sMassUser} {massUnit}, V={sVolUser} {volUnit}', showLabel:true}});";
                }

                expJs += $@"
        calc.setMathBounds({{ left: {sXMin}, right: {sXMax}, bottom: {sYMin}, top: {sYMax} }});";

                string windowTitle = "⚖️ m = ρ × V (Đồ thị khối lượng riêng)";

                if (_activeGraphWin == null)
                {
                    _activeGraphWin = new Math.GraphWindow(expJs, windowTitle);
                    _activeGraphWin.Owner = Window.GetWindow(this);
                    _activeGraphWin.Closed += (s, ev) => { _activeGraphWin = null; };
                    _activeGraphWin.Show();
                }
                else
                {
                    string infoText = $"ρ = {FormatVietnamese(rhoUser)} {massUnit}/{volUnit}" + (hasM && hasV ? $" | m = {sMassUser} {massUnit} | V = {sVolUser} {volUnit}" : "");
                    _activeGraphWin.UpdateExpressions(expJs, windowTitle, infoText);
                    _activeGraphWin.Activate();
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void GetGraphData(out double rho, out double massKg, out double volM3, out bool hasM, out bool hasV)
        {
            massKg = GetMassKg();
            volM3 = GetVolM3();
            rho = GetDensKgM3();
            if (rho <= 0) rho = 1000; // default to Water

            hasM = massKg > 0;
            hasV = volM3 > 0;

            if (hasM && hasV)
            {
                rho = massKg / volM3;
            }
            else if (hasM && rho > 0)
            {
                volM3 = massKg / rho;
            }
            else if (hasV && rho > 0)
            {
                massKg = rho * volM3;
            }
        }

        private void UpdateActiveGraph()
        {
            if (_activeGraphWin == null) return;
            try
            {
                GetGraphData(out double rho, out double massKg, out double volM3, out bool hasM, out bool hasV);

                string massUnit = (cmbMassUnit.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "kg";
                string volUnit = (cmbVolUnit.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "m³";
                double factor = VolMul[cmbVolUnit.SelectedIndex] / MassMul[cmbMassUnit.SelectedIndex];

                double volUser = volM3 / VolMul[cmbVolUnit.SelectedIndex];
                double massUser = massKg / MassMul[cmbMassUnit.SelectedIndex];
                double rhoUser = rho * factor;

                var ci = System.Globalization.CultureInfo.InvariantCulture;
                string sRhoUser = rhoUser.ToString("G6", ci);
                string sMassUser = massUser.ToString("G6", ci);
                string sVolUser = volUser.ToString("G6", ci);

                double xMax = 0.01;
                double yMax = 200;
                if (hasM && hasV)
                {
                    xMax = volUser * 1.5;
                    yMax = massUser * 1.5;
                }
                else
                {
                    double refVol = 100.0;
                    if (cmbVolUnit.SelectedIndex == 0) refVol = 0.1; // m3
                    if (rho < 10 && cmbVolUnit.SelectedIndex == 0) refVol = 10.0;
                    else if (rho > 5000 && cmbVolUnit.SelectedIndex == 0) refVol = 0.01;

                    xMax = refVol * 1.5;
                    yMax = (rhoUser * refVol) * 1.5;
                }

                double xMin = -xMax * 0.15;
                double yMin = -yMax * 0.15;

                string sXMin = xMin.ToString("G6", ci);
                string sXMax = xMax.ToString("G6", ci);
                string sYMin = yMin.ToString("G6", ci);
                string sYMax = yMax.ToString("G6", ci);

                string currLabel = $"ρ = {FormatVietnamese(rhoUser)} {massUnit}/{volUnit} (Hiện tại)";

                string expJs = $@"
        calc.setExpression({{id:'d_curr', latex:'y={sRhoUser}x', color:'#E65100', lineWidth:4, label:'{currLabel}', showLabel:true}});
        calc.updateSettings({{ xAxisLabel: 'Thể tích V ({volUnit})', yAxisLabel: 'Khối lượng m ({massUnit})' }});";

                if (hasM && hasV)
                {
                    expJs += $@"
        calc.setExpression({{id:'p_curr', latex:'({sVolUser}, {sMassUser})', color:'#C62828', pointSize:12, label:'m={sMassUser} {massUnit}, V={sVolUser} {volUnit}', showLabel:true}});";
                }
                else
                {
                    expJs += @"
        calc.removeExpression({id:'p_curr'});";
                }

                expJs += $@"
        calc.setMathBounds({{ left: {sXMin}, right: {sXMax}, bottom: {sYMin}, top: {sYMax} }});";

                string windowTitle = "⚖️ m = ρ × V (Đồ thị khối lượng riêng)";
                string infoText = $"ρ = {FormatVietnamese(rhoUser)} {massUnit}/{volUnit}" + (hasM && hasV ? $" | m = {sMassUser} {massUnit} | V = {sVolUser} {volUnit}" : "");
                _activeGraphWin.UpdateExpressions(expJs, windowTitle, infoText);
            }
            catch { }
        }

        public static string FormatVietnamese(double value)
        {
            var nfi = new System.Globalization.NumberFormatInfo
            {
                NumberDecimalSeparator = ",",
                NumberGroupSeparator = ""
            };
            return value.ToString("G10", nfi);
        }

        public static string FormatToPedagogicalScientific(double value)
        {
            if (value == 0) return "0";
            double abs = System.Math.Abs(value);
            if (abs >= 1000000 || abs < 0.0001)
            {
                string s = value.ToString("E4", System.Globalization.CultureInfo.InvariantCulture);
                int eIdx = s.IndexOf('E');
                if (eIdx != -1)
                {
                    string mantissa = s.Substring(0, eIdx);
                    string exponent = s.Substring(eIdx + 1);

                    double mantissaVal = double.Parse(mantissa, System.Globalization.CultureInfo.InvariantCulture);
                    string formattedMantissa = FormatVietnamese(mantissaVal);

                    int expVal = int.Parse(exponent, System.Globalization.CultureInfo.InvariantCulture);
                    string superscriptExp = GetSuperscriptString(expVal);

                    return $"{formattedMantissa} \u00B7 10{superscriptExp}";
                }
            }
            return FormatVietnamese(value);
        }

        private static string GetSuperscriptString(int value)
        {
            string s = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string res = "";
            foreach (char c in s)
            {
                res += c switch
                {
                    '-' => "\u207B",
                    '+' => "",
                    '0' => "\u2070",
                    '1' => "\u00B9",
                    '2' => "\u00B2",
                    '3' => "\u00B3",
                    '4' => "\u2074",
                    '5' => "\u2075",
                    '6' => "\u2076",
                    '7' => "\u2077",
                    '8' => "\u2078",
                    '9' => "\u2079",
                    _ => c.ToString()
                };
            }
            return res;
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
                        Icon = "🛳",
                        Title = isVN ? "️ Tàu ngầm lặn và nổi" : "Submarine Ballast Tanks",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_density_1_{suffix}.png",
                        Description = isVN 
                            ? "Tàu ngầm thay đổi khối lượng riêng trung bình của nó bằng cách bơm nước vào hoặc đẩy nước ra khỏi các bình ballast chứa khí để lặn xuống hoặc nổi lên." 
                            : "Fill ballast tanks with water to increase average density and submerge, or pump in air to decrease density and float."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎈",
                        Title = isVN ? "Khinh khí cầu" : "Hot Air Balloon Flight",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_density_2_{suffix}.png",
                        Description = isVN 
                            ? "Không khí nóng nở ra, có khối lượng riêng nhỏ hơn không khí lạnh bên ngoài, tạo lực đẩy Archimedes giúp khinh khí cầu bay lên." 
                            : "Heat air inside the balloon to lower its density below surrounding cool air, creating upward buoyant force."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🛟",
                        Title = isVN ? "Phao cứu sinh" : "Life Jacket Design",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_density_3_{suffix}.png",
                        Description = isVN 
                            ? "Được làm từ xốp đặc siêu nhẹ có khối lượng riêng rất nhỏ so với nước, cung cấp lực đẩy nổi lớn giúp nâng đỡ người gặp nạn trên biển." 
                            : "Manufacture life jackets using foam materials filled with air pockets to keep overall density much lower than water."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧪",
                        Title = isVN ? "Phân tầng chất lỏng" : "Liquid Layering in Science",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_density_4_{suffix}.png",
                        Description = isVN 
                            ? "Các chất lỏng không hòa tan có khối lượng riêng khác nhau sẽ phân tầng: dầu ăn nhẹ nổi lên trên, nước chìm xuống dưới cùng." 
                            : "Separate oil, water, and honey in tubes based on density differences, which is also used to separate blood components."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏆",
                        Title = isVN ? "Đo độ tinh khiết của vàng" : "Gold Purity Testing",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_density_5_{suffix}.png",
                        Description = isVN 
                            ? "Dựa trên đo thể tích nước dâng lên và khối lượng của vật để tính ra khối lượng riêng, giúp phát hiện vàng giả (vàng thật có ρ ≈ 19.300 kg/m³)." 
                            : "Verify gold purity by dividing its mass by volume and comparing to gold's standard density (19.3 g/cm³) to detect fakes."
                    },
                    new PracticalAppItem
                    {
                        Icon = "☁",
                        Title = isVN ? "️ Tích tụ khí độc trong giếng" : "Toxic Gas Accumulation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_density_6_{suffix}.png",
                        Description = isVN 
                            ? "Khí CO₂ sinh ra dưới đáy giếng có khối lượng riêng lớn hơn không khí (1,977 > 1,225 kg/m³) nên chìm xuống đáy, gây thiếu oxy nguy hiểm." 
                            : "Understand how heavy gases like CO₂ (density 1.977 kg/m³) sink to the bottom of wells, displacing oxygen and creating hazards."
                    }
                ,
                    new PracticalAppItem
                    {
                        Icon = "🔋",
                        Title = isVN ? "Đo nồng độ dung dịch ắc quy" : "Battery Acid Density Testing",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_density_7_{suffix}.png",
                        Description = isVN 
                            ? "Đo tỷ trọng của dung dịch axit sulfuric để đánh giá mức độ nạp điện và tình trạng hoạt động của ắc quy chì." 
                            : "Measure the density of sulfuric acid electrolyte to evaluate charge levels and health of lead-acid batteries."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧱",
                        Title = isVN ? "Tuyển khoáng sản bằng trọng lực" : "Gravity Ore Separation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_density_8_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng sự khác biệt về khối lượng riêng để tách quặng quý ra khỏi đất đá trong các bể nước tuyển khoáng." 
                            : "Use density differences to separate precious minerals from host rock in water-based gravity separation tanks."
                    }};

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for DensityTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewCalculator == null || viewTable == null || viewFloat == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewCalculator.Visibility = Visibility.Collapsed;
            viewTable.Visibility = Visibility.Collapsed;
            viewFloat.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewCalculator.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewTable.Visibility = Visibility.Visible;
                    break;
                case 3:
                    viewFloat.Visibility = Visibility.Visible;
                    break;
                case 4:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}