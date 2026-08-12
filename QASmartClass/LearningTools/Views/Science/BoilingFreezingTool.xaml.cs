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
    public partial class BoilingFreezingTool : BaseToolControl
    {
        private bool _isUpdating;

        public BoilingFreezingTool()
        {
            InitializeComponent();
            // Attach TouchNumPad
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtC, step: 1);
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtF, step: 1);
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtK, step: 1);

            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextConverter != null) menuTextConverter.Text = isVN ? "Chuyển đổi" : "Converter";
                if (menuTextTable != null) menuTextTable.Text = isVN ? "Bảng nhiệt độ" : "Temperature Table";
                if (menuTextCompare != null) menuTextCompare.Text = isVN ? "So sánh" : "Comparison";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                BuildPresets();
                UpdateFromCelsius(100);
                BuildTempTable();
                BuildCompare();
                UpdateThermometer();
                LoadPracticalApps();
            };
        }

        // ═══ TEMP CONVERSION ═══
        private void TempBox_Changed(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating || sender is not TextBox tb || txtC == null || txtF == null || txtK == null) return;
            if (!ParsingHelper.TryParseDouble(tb.Text, out double val)) return;

            string tag = tb.Tag as string ?? "C";
            double celsius = tag switch
            {
                "F" => (val - 32) * 5.0 / 9.0,
                "K" => val - 273.15,
                _ => val
            };

            bool wasClamped = false;
            if (celsius < -273.15)
            {
                celsius = -273.15;
                wasClamped = true;
            }

            _isUpdating = true;
            if (tb != txtC || wasClamped) txtC.Text = $"{celsius:F2}";
            if (tb != txtF || wasClamped) txtF.Text = $"{celsius * 9.0 / 5.0 + 32:F2}";
            if (tb != txtK || wasClamped) txtK.Text = $"{celsius + 273.15:F2}";
            _isUpdating = false;

            UpdateThermometer();
        }

        private void UpdateFromCelsius(double c)
        {
            if (c < -273.15) c = -273.15;
            _isUpdating = true;
            txtC.Text = $"{c:F2}";
            txtF.Text = $"{c * 9.0 / 5.0 + 32:F2}";
            txtK.Text = $"{c + 273.15:F2}";
            _isUpdating = false;
        }

        // ═══ RESET ═══
        private void ResetAll_Click(object sender, MouseButtonEventArgs e)
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 1)
            {
                sideMenu.SelectedIndex = 0;
            }
            UpdateFromCelsius(100);
            UpdateThermometer();
        }

        // ═══ THERMOMETER VISUAL ═══
        private void UpdateThermometer()
        {
            if (thermometerPanel == null || txtC == null || txtF == null || txtK == null) return;
            thermometerPanel.Children.Clear();

            if (!ParsingHelper.TryParseDouble(txtC.Text, out double c)) return;

            // Warning banner visibility (S3.3)
            if (brdWarning != null)
            {
                brdWarning.Visibility = (c <= -273.15) ? Visibility.Visible : Visibility.Collapsed;
            }

            // Determine state description
            string state, stateColor, icon;
            if (c <= -273.15) { state = "Không tuyệt đối!"; stateColor = "#1A237E"; icon = "⚠️"; }
            else if (c < -100) { state = "Cực lạnh — khí hóa lỏng"; stateColor = "#0D47A1"; icon = "🧊"; }
            else if (c < 0) { state = "Đóng băng — nước đông đặc"; stateColor = "#1565C0"; icon = "❄️"; }
            else if (c < 36) { state = "Lạnh → Thân nhiệt"; stateColor = "#2E7D32"; icon = "🌿"; }
            else if (c >= 36 && c <= 37.5) { state = "Thân nhiệt"; stateColor = "#2E7D32"; icon = "🧑"; }
            else if (c < 100) { state = "Ấm → Nóng"; stateColor = "#E65100"; icon = "🔥"; }
            else if (c < 1000) { state = "Sôi — nước bay hơi"; stateColor = "#BF360C"; icon = "💨"; }
            else { state = "Cực nóng — nóng chảy kim loại"; stateColor = "#D50000"; icon = "🌋"; }

            // Educational dynamic notes (S3.4)
            if (txtEduText != null)
            {
                string eduText;
                if (c < -273.15)
                {
                    eduText = "⚠️ Lỗi Vật Lý: Nhiệt độ không thể dưới không tuyệt đối (-273.15°C hoặc 0 K). Đây là trạng thái năng lượng nhiệt tối thiểu lý thuyết của vật chất.";
                }
                else if (c == -273.15)
                {
                    eduText = "❄️ Không tuyệt đối (-273.15°C / 0 K): Giới hạn nhiệt độ thấp nhất trong vũ trụ. Tại đây, động năng nhiệt của các nguyên tử đạt giá trị cực tiểu. Con người chỉ có thể tiệm cận chứ chưa thể đạt tới chính xác nhiệt độ này.";
                }
                else if (c < -100)
                {
                    eduText = "🧊 Nhiệt độ cực lạnh: Ở vùng nhiệt độ này, các chất khí như Nitơ (-196°C) hóa lỏng. Nitơ lỏng được sử dụng phổ biến trong y tế để bảo quản tế bào, hoặc trong ẩm thực phân tử. Cần đeo kính bảo hộ và găng tay dày để tránh bỏng lạnh cực độ.";
                }
                else if (c < 0)
                {
                    eduText = "❄️ Trạng thái đông đặc của nước (dưới 0°C): Nước chuyển từ thể lỏng sang thể rắn (đá). Thể tích của nước đá tăng khoảng 9% so với nước lỏng, có thể tạo ra áp lực lớn làm vỡ chai thủy tinh hoặc ống nước nếu bị đông đá bên trong.";
                }
                else if (c == 0)
                {
                    eduText = "💧 Điểm đóng băng/nóng chảy của nước tinh khiết (0°C): Ở áp suất khí quyển tiêu chuẩn, nước đá bắt đầu nóng chảy hoặc nước lỏng bắt đầu đóng băng. Đây là một mốc quan trọng để định hình thang đo Celsius.";
                }
                else if (c < 36)
                {
                    eduText = "🌿 Nhiệt độ môi trường (0°C - 36°C): Thích hợp cho sự sống phát triển. Nhiệt độ phòng tiêu chuẩn thường khoảng 20°C - 25°C. Ở nhiệt độ này, hầu hết kim loại ở thể rắn (trừ Thủy ngân hóa lỏng ở -38.8°C).";
                }
                else if (c >= 36 && c <= 37.5)
                {
                    eduText = "🧑 Thân nhiệt con người (khoảng 36.6°C - 37°C): Là nhiệt độ tối ưu cho các enzyme trong cơ thể hoạt động bình thường. Cơ thể duy trì nhiệt độ này nhờ vùng dưới đồi trong não điều khiển các cơ chế toát mồ hôi hoặc run người.";
                }
                else if (c < 100)
                {
                    eduText = "🔥 Vùng nhiệt độ ấm/nóng (37.5°C - 100°C): Nước nóng lên, tốc độ bay hơi của các phân tử bề mặt tăng dần. Nhiệt độ nước khoảng 60°C trở lên có thể gây bỏng da người chỉ trong vài giây. Cần chú ý an toàn khi tiếp xúc.";
                }
                else if (c == 100)
                {
                    eduText = "💨 Điểm sôi của nước tinh khiết (100°C): Ở áp suất khí quyển tiêu chuẩn (1 atm), nước sôi mạnh, chuyển pha hoàn toàn từ thể lỏng sang thể khí (hơi nước). Mốc này được Anders Celsius dùng làm mốc 100 độ trong thang đo của mình.";
                }
                else if (c < 1000)
                {
                    eduText = "🌋 Nhiệt độ cao (100°C - 1000°C): Đủ nóng để làm nóng chảy nhiều kim loại có nhiệt độ nóng chảy thấp như Thiếc (232°C), Chì (327°C), Nhôm (660°C). Muối ăn (NaCl) nóng chảy ở 801°C.";
                }
                else
                {
                    eduText = "🔩 Cực nóng (trên 1000°C): Nhiệt độ của dung nham núi lửa, lò luyện kim hoặc bề mặt các thiên thể. Sắt nóng chảy ở 1538°C, Vonfram (kim loại có nhiệt độ nóng chảy cao nhất) nóng chảy ở 3422°C. Bề mặt Mặt Trời có nhiệt độ khoảng 5500°C.";
                }
                txtEduText.Text = eduText;
            }

            var accent = (Color)ColorConverter.ConvertFromString(stateColor);

            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, accent.R, accent.G, accent.B)),
                CornerRadius = new CornerRadius(12), Padding = new Thickness(20, 14, 20, 14),
                BorderBrush = new SolidColorBrush(accent), BorderThickness = new Thickness(0, 0, 0, 3)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Icon
            var iconTb = new TextBlock
            {
                Text = icon, FontSize = 32, VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0)
            };
            Grid.SetColumn(iconTb, 0);
            grid.Children.Add(iconTb);

            // Info
            var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(new TextBlock
            {
                Text = state, FontSize = 14, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(accent)
            });
            sp.Children.Add(new TextBlock
            {
                Text = $"{c:F1}°C = {c * 9.0 / 5.0 + 32:F1}°F = {c + 273.15:F1} K",
                FontSize = 13, FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)), Margin = new Thickness(0, 4, 0, 0)
            });

            // Temperature bar (S3.5 - Star-based responsive column widths)
            double barRatio = System.Math.Clamp((c + 273.15) / 2000.0, 0.0001, 0.9999);
            var barBg = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                CornerRadius = new CornerRadius(4), Height = 8, Margin = new Thickness(0, 8, 0, 0)
            };
            var barFill = new Border
            {
                CornerRadius = new CornerRadius(4), Height = 8,
                Background = new LinearGradientBrush(Color.FromRgb(33, 150, 243), accent, 0)
            };

            var barContainer = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            barContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(barRatio, GridUnitType.Star) });
            barContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0 - barRatio, GridUnitType.Star) });

            barContainer.Children.Add(barBg);
            Grid.SetColumnSpan(barBg, 2);

            barContainer.Children.Add(barFill);
            Grid.SetColumn(barFill, 0);

            sp.Children.Add(barContainer);

            Grid.SetColumn(sp, 1);
            grid.Children.Add(sp);
            card.Child = grid;
            thermometerPanel.Children.Add(card);
        }

        // ═══ PRESETS ═══
        private void BuildPresets()
        {
            if (presetPanel == null) return;
            presetPanel.Children.Clear();
            var presets = new (string Label, double Celsius, string Color)[]
            {
                ("❄️ Không tuyệt đối", -273.15, "#1A237E"),
                ("🧊 Nitơ lỏng", -196, "#0D47A1"),
                ("💧 Nước đóng băng", 0, "#1565C0"),
                ("🧑 Thân nhiệt", 36.6, "#2E7D32"),
                ("💧 Nước sôi", 100, "#E65100"),
                ("🔥 Chì nóng chảy", 327, "#BF360C"),
                ("⚙️ Nhôm nóng chảy", 660, "#795548"),
                ("🔩 Sắt nóng chảy", 1538, "#D50000"),
                ("🌟 Bề mặt Mặt Trời", 5500, "#FF6F00"),
            };

            foreach (var (label, celsius, color) in presets)
            {
                var c = (Color)ColorConverter.ConvertFromString(color);
                double val = celsius;
                UI.PresetButton(label, c, () =>
                {
                    UpdateFromCelsius(val);
                    UpdateThermometer();
                }, presetPanel);
            }
        }

        // ═══ TEMP TABLE (Tab 2) ═══
        private void BuildTempTable()
        {
            if (tempTablePanel == null) return;
            tempTablePanel.Children.Clear();

            tempTablePanel.Children.Add(new TextBlock
            {
                Text = "📋 Bảng Nhiệt Độ Nóng Chảy & Sôi của Các Chất",
                FontSize = 18, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(191, 54, 12)),
                Margin = new Thickness(0, 0, 0, 12)
            });

            var categories = new (string Title, string Icon, (string Name, double Melt, double Boil, string Note)[] Items)[]
            {
                ("Kim loại", "🔩", new[]
                {
                    ("Vonfram (W)", 3422.0, 5555.0, "cao nhất"),
                    ("Sắt (Fe)", 1538.0, 2862.0, "thép"),
                    ("Đồng (Cu)", 1085.0, 2562.0, "dây điện"),
                    ("Vàng (Au)", 1064.0, 2856.0, "quý"),
                    ("Nhôm (Al)", 660.0, 2519.0, "nhẹ"),
                    ("Chì (Pb)", 327.0, 1749.0, "nặng"),
                    ("Thiếc (Sn)", 232.0, 2602.0, "hàn"),
                    ("Thủy ngân (Hg)", -38.8, 356.7, "lỏng ở RT"),
                }),
                ("Phi kim & Hợp chất", "🧪", new[]
                {
                    ("Muối ăn (NaCl)", 801.0, 1413.0, ""),
                    ("Băng phiến (Naphthalene)", 80.2, 217.9, "thí nghiệm"),
                    ("Nước (H₂O)", 0.0, 100.0, "chuẩn"),
                    ("Rượu ethanol", -114.0, 78.4, ""),
                    ("Axít axetic (Giấm)", 16.6, 117.9, "chua"),
                }),
                ("Khí (hóa lỏng)", "💨", new[]
                {
                    ("Oxy (O₂)", -219.0, -183.0, "hô hấp"),
                    ("Nitơ (N₂)", -210.0, -196.0, "78% khí quyển"),
                    ("Hydro (H₂)", -259.0, -253.0, "nhẹ nhất"),
                    ("Khí Argon (Ar)", -189.3, -185.8, "khí hiếm"),
                }),
            };

            foreach (var (title, icon, items) in categories)
            {
                // Category header
                var header = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(191, 54, 12)),
                    CornerRadius = new CornerRadius(10, 10, 0, 0),
                    Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 12, 0, 0)
                };
                header.Child = new TextBlock
                {
                    Text = $"{icon} {title}", FontSize = 13, FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                };
                tempTablePanel.Children.Add(header);

                // Column headers
                var colHeader = MakeRowGrid("Chất", "Nóng chảy (°C)", "Sôi (°C)", "Ghi chú");
                tempTablePanel.Children.Add(new Border
                {
                    Child = colHeader,
                    Background = new SolidColorBrush(Color.FromRgb(239, 83, 80)),
                    Padding = new Thickness(12, 6, 12, 6)
                });

                // Rows
                for (int i = 0; i < items.Length; i++)
                {
                    var s = items[i];
                    var rowGrid = MakeRowGrid(s.Name, $"{s.Melt:F1}", $"{s.Boil:F1}", s.Note, false);
                    var rowBorder = new Border
                    {
                        Child = rowGrid,
                        Background = i % 2 == 0 ? Brushes.White : new SolidColorBrush(Color.FromRgb(251, 233, 231)),
                        Padding = new Thickness(12, 8, 12, 8),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(238, 238, 238)),
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        Cursor = Cursors.Hand, ToolTip = "Click để điền vào converter"
                    };
                    double melt = s.Melt;
                    rowBorder.MouseLeftButtonDown += (_, _) =>
                    {
                        UpdateFromCelsius(melt);
                        UpdateThermometer();
                    };
                    tempTablePanel.Children.Add(rowBorder);
                }
            }
        }

        // ═══ COMPARE (Tab 3) ═══
        private void BuildCompare()
        {
            if (comparePanel == null) return;
            comparePanel.Children.Clear();

            comparePanel.Children.Add(new TextBlock
            {
                Text = "📊 So Sánh Nhiệt Độ Trực Quan",
                FontSize = 18, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(191, 54, 12)),
                Margin = new Thickness(0, 0, 0, 12)
            });

            var items = new (string Name, double Temp, string Icon, string Color)[]
            {
                ("Không tuyệt đối", -273.15, "⬛", "#1A237E"),
                ("Heli sôi", -269, "🫧", "#0D47A1"),
                ("Nitơ sôi", -196, "💨", "#1565C0"),
                ("Argon sôi", -185.8, "🫧", "#0277BD"),
                ("Giấm đóng băng", 16.6, "❄️", "#00838F"),
                ("Phòng (20°C)", 20, "🏠", "#2E7D32"),
                ("Thân nhiệt", 36.6, "🧑", "#388E3C"),
                ("Ethanol sôi", 78.4, "🍷", "#F9A825"),
                ("Băng phiến nóng chảy", 80.2, "🧪", "#F9A825"),
                ("Nước sôi", 100, "💧", "#E65100"),
                ("Thiếc nóng chảy", 232, "⚙️", "#BF360C"),
                ("Chì nóng chảy", 327, "⚙️", "#BF360C"),
                ("Nhôm nóng chảy", 660, "🔧", "#D84315"),
                ("Vàng nóng chảy", 1064, "🥇", "#FF6F00"),
                ("Sắt nóng chảy", 1538, "🔩", "#C62828"),
                ("Vonfram nóng chảy", 3422, "💡", "#B71C1C"),
                ("Mặt Trời (bề mặt)", 5500, "☀️", "#D50000"),
            };

            double maxTemp = items.Max(x => x.Temp);
            double minTemp = items.Min(x => x.Temp);
            double range = maxTemp - minTemp;

            foreach (var (name, temp, icon, color) in items)
            {
                var c = (Color)ColorConverter.ConvertFromString(color);
                double ratio = (temp - minTemp) / range;

                var card = new Border
                {
                    Background = Brushes.White, CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 0, 4),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(238, 238, 238)),
                    BorderThickness = new Thickness(1), Cursor = Cursors.Hand,
                    ToolTip = "Click để điền vào converter"
                };

                var g = new Grid();
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var info = new DockPanel();
                info.Children.Add(new TextBlock
                {
                    Text = $"{icon} {name}", FontSize = 14, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(c)
                });
                var tempTb = new TextBlock
                {
                    Text = $"{temp:F1}°C  |  {temp * 9.0 / 5.0 + 32:F1}°F  |  {temp + 273.15:F1} K",
                    FontSize = 13, FontFamily = new FontFamily("Segoe UI"),
                    Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)),
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                DockPanel.SetDock(tempTb, Dock.Right);
                info.Children.Add(tempTb);
                Grid.SetRow(info, 0);
                g.Children.Add(info);

                // Bar
                var barBg = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                    CornerRadius = new CornerRadius(3), Height = 6, Margin = new Thickness(0, 6, 0, 0)
                };
                var barFill = new Border
                {
                    Background = new SolidColorBrush(c),
                    CornerRadius = new CornerRadius(3), Height = 6
                };
                var barCont = new Grid { Margin = new Thickness(0, 6, 0, 0) };
                double r = System.Math.Clamp(ratio, 0.0001, 0.9999);
                barCont.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(r, GridUnitType.Star) });
                barCont.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0 - r, GridUnitType.Star) });

                barCont.Children.Add(barBg);
                Grid.SetColumnSpan(barBg, 2);

                barCont.Children.Add(barFill);
                Grid.SetColumn(barFill, 0);

                Grid.SetRow(barCont, 1);
                g.Children.Add(barCont);

                card.Child = g;

                double t = temp;
                card.MouseLeftButtonDown += (_, _) =>
                {
                    UpdateFromCelsius(t);
                    UpdateThermometer();
                };

                comparePanel.Children.Add(card);
            }
        }

        // ═══ HELPERS ═══
        private static Grid MakeRowGrid(string c1, string c2, string c3, string c4, bool isHeader = true)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.8, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });

            var texts = new[] { c1, c2, c3, c4 };
            for (int i = 0; i < 4; i++)
            {
                var tb = new TextBlock
                {
                    Text = texts[i],
                    FontSize = isHeader ? 14 : 15,
                    FontWeight = isHeader || i == 0 ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = isHeader ? Brushes.White : new SolidColorBrush(Color.FromRgb(33, 33, 33))
                };
                Grid.SetColumn(tb, i);
                grid.Children.Add(tb);
            }
            return grid;
        }

        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                double c = 100;
                ParsingHelper.TryParseDouble(txtC?.Text, out c);
                double f = c * 9.0 / 5.0 + 32;
                double k = c + 273.15;
                var ci = System.Globalization.CultureInfo.InvariantCulture;

                string expJs = $@"
        calc.setExpression({{id:'cf', latex:'y=x\\cdot\\frac{{9}}{{5}}+32', color:'#E65100', lineWidth:3, label:'°C → °F', showLabel:true}});
        calc.setExpression({{id:'ck', latex:'y=x+273.15', color:'#2E7D32', lineWidth:3, label:'°C → K', showLabel:true}});
        calc.setExpression({{id:'id', latex:'y=x', color:'#9E9E9E', lineWidth:1, lineStyle:'DASHED'}});
        calc.setExpression({{id:'w0', latex:'x=0', color:'#1565C0', lineWidth:1, lineStyle:'DASHED'}});
        calc.setExpression({{id:'w100', latex:'x=100', color:'#C62828', lineWidth:1, lineStyle:'DASHED'}});
        calc.setExpression({{id:'ptf', latex:'({c.ToString(ci)},{f.ToString(ci)})', color:'#E65100', pointSize:12, label:'({c:F0}°C,{f:F0}°F)', showLabel:true}});
        calc.setExpression({{id:'ptk', latex:'({c.ToString(ci)},{k.ToString(ci)})', color:'#2E7D32', pointSize:12, label:'({c:F0}°C,{k:F0} K)', showLabel:true}});
        calc.setMathBounds({{ left: -300, right: 400, bottom: -100, top: 800 }});";

                var win = new Math.GraphWindow(expJs, $"🌡️ Chuyển đổi nhiệt độ — {c:F0}°C");
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (System.Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
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
                        Title = isVN ? "Bảo quản bằng Nitơ lỏng" : "Liquid Nitrogen Preservation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_boilingfreezing_1_{suffix}.png",
                        Description = isVN 
                            ? "Nitơ lỏng sôi ở -196°C, được dùng để đông lạnh cực nhanh và bảo quản lâu dài tế bào, mô sinh học và phôi y tế một cách an toàn." 
                            : "Liquid nitrogen boils at -196°C, used to rapidly freeze and safely preserve biological cells, tissues, and medical embryos in the long term."
                    },
                    new PracticalAppItem
                    {
                        Icon = "❄",
                        Title = isVN ? "️ Lực nở khi nước đóng băng" : "Expansion Force of Freezing Water",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_boilingfreezing_2_{suffix}.png",
                        Description = isVN 
                            ? "Nước đóng băng ở 0°C tăng khoảng 9% thể tích. Sự nở thể tích này tạo ra áp lực khổng lồ làm vỡ các ống dẫn nước hoặc nứt đá tự nhiên." 
                            : "Water expands by about 9% in volume when freezing at 0°C. This volume expansion creates immense pressure that can burst water pipes or crack natural rocks."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔩",
                        Title = isVN ? "Luyện kim & Đúc khuôn" : "Metallurgy & Casting",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_boilingfreezing_3_{suffix}.png",
                        Description = isVN 
                            ? "Lợi dụng nhiệt độ nóng chảy của kim loại (sắt nóng chảy ở 1538°C, nhôm 660°C, vàng 1064°C) để nấu chảy và đúc thành các chi tiết máy." 
                            : "Take advantage of metal melting points (iron melts at 1538°C, aluminum at 660°C, gold at 1064°C) to melt and cast them into mechanical parts."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌡",
                        Title = isVN ? "️ Đo nhiệt độ bằng Thủy ngân" : "Mercury Thermometers",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_boilingfreezing_4_{suffix}.png",
                        Description = isVN 
                            ? "Thủy ngân hóa lỏng ở -38.8°C và sôi ở 356.7°C, khoảng nhiệt độ lỏng lý tưởng này giúp nó được ứng dụng làm nhiệt kế y tế cổ điển." 
                            : "Mercury liquefies at -38.8°C and boils at 356.7°C. This ideal liquid temperature range makes it highly suitable for classic medical thermometers."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💨",
                        Title = isVN ? "Thăng hoa đá khô (CO₂ rắn)" : "Dry Ice Sublimation (Solid CO₂)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_boilingfreezing_5_{suffix}.png",
                        Description = isVN 
                            ? "Đá khô thăng hoa trực tiếp ở -78.5°C tạo làn khói trắng lạnh mát, được sử dụng để bảo quản thực phẩm và tạo hiệu ứng sân khấu." 
                            : "Dry ice sublimates directly at -78.5°C, creating a cold white mist used for food preservation and stage effects."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💻",
                        Title = isVN ? "Ép xung siêu máy tính" : "Supercomputer Overclocking",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_boilingfreezing_6_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng sự sôi và bay hơi hấp thụ nhiệt lượng cực lớn của Nitơ lỏng để làm mát tức thì chip xử lý khi ép xung tăng tốc độ tính toán." 
                            : "Use the massive heat absorption of liquid nitrogen during boiling and evaporation to instantly cool processing chips during high-speed computing overclocking."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💨",
                        Title = isVN ? "Hóa lỏng khí tự nhiên LNG" : "LNG Gas Liquefaction",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_boilingfreezing_7_{suffix}.png",
                        Description = isVN 
                            ? "Hạ nhiệt độ xuống âm 162 độ C để khí tự nhiên chuyển sang thể lỏng, giảm thể tích giúp vận chuyển dễ dàng qua đại dương." 
                            : "Cool natural gas to minus 162 degrees Celsius to liquefy it, reducing volume for easy transoceanic shipping."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🛢️",
                        Title = isVN ? "Chưng cất dầu mỏ chân không" : "Vacuum Petroleum Distillation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_boilingfreezing_8_{suffix}.png",
                        Description = isVN 
                            ? "Giảm áp suất hệ thống để hạ điểm sôi của các hydrocacbon nặng, giúp tách biệt các thành phần dầu mỏ mà không gây phân hủy nhiệt." 
                            : "Reduce system pressure to lower hydrocarbon boiling points, separating petroleum components without thermal cracking."
                    }};

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for BoilingFreezingTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewConverter == null || viewTable == null || viewCompare == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewConverter.Visibility = Visibility.Collapsed;
            viewTable.Visibility = Visibility.Collapsed;
            viewCompare.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewConverter.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewTable.Visibility = Visibility.Visible;
                    break;
                case 3:
                    viewCompare.Visibility = Visibility.Visible;
                    break;
                case 4:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
