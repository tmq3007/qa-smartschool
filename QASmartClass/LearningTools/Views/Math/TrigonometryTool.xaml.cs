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
    public partial class TrigonometryTool : BaseToolControl
    {
        private class HistoryItem
        {
            public string Text { get; set; } = string.Empty;
            public bool IsRadian { get; set; }
            public string DisplayText { get; set; } = string.Empty;
        }
        private System.Collections.Generic.List<HistoryItem> _history = new System.Collections.Generic.List<HistoryItem>();
        private bool _isUpdatingInternally = false;

        public TrigonometryTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextTable != null) menuTextTable.Text = isVN ? "Bảng giá trị" : "Trig Table";
                if (menuTextCalc != null) menuTextCalc.Text = isVN ? "Tính toán" : "Calculator";
                if (menuTextFormula != null) menuTextFormula.Text = isVN ? "Công thức" : "Formulas";
                if (menuTextTheory != null) menuTextTheory.Text = isVN ? "Hướng dẫn & Ví dụ" : "Guide & Examples";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                BuildTable();
                BuildQuickAngles();
                CalculateAngle();
                BuildFormulas();
                BuildGuide();
                LoadPracticalApps();
                // Bàn phím số mini cho góc
                if (txtAngle != null) TouchNumPad.Attach(txtAngle, step: 1, min: -720, max: 720);

                // Đăng ký sự kiện kéo thả từ Đường tròn lượng giác tương tác
                if (trigCircle != null)
                {
                    trigCircle.AngleChanged += TrigCircle_AngleChanged;
                }
            };
            Unloaded += (_, _) =>
            {
                if (trigCircle != null)
                {
                    trigCircle.AngleChanged -= TrigCircle_AngleChanged;
                }
            };
        }

        private void TrigCircle_AngleChanged(object sender, EventArgs e)
        {
            if (_isUpdatingInternally || trigCircle == null || txtAngle == null) return;

            _isUpdatingInternally = true;
            try
            {
                double angleDeg = trigCircle.Angle;
                if (rbRadian?.IsChecked == true)
                {
                    double angleRad = angleDeg * System.Math.PI / 180.0;
                    txtAngle.Text = angleRad.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    txtAngle.Text = angleDeg.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                }
                CalculateAngle();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error sync angle: " + ex.Message);
            }
            finally
            {
                _isUpdatingInternally = false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  BẢNG GIÁ TRỊ — các góc đặc biệt
        // ═══════════════════════════════════════════════════════════

        private void BuildTable()
        {
            if (tablePanel == null) return;
            tablePanel.Children.Clear();

            int[] angles = { 0, 30, 45, 60, 90, 120, 135, 150, 180, 210, 225, 240, 270, 300, 315, 330, 360 };

            // Header
            var headerGrid = CreateTableRow("Góc (°)", "Radian", "sin", "cos", "tan", "cot", true);
            tablePanel.Children.Add(headerGrid);

            foreach (var deg in angles)
            {
                double rad = deg * System.Math.PI / 180;
                string sinVal = FormatTrig(System.Math.Sin(rad));
                string cosVal = FormatTrig(System.Math.Cos(rad));
                string tanVal = System.Math.Abs(System.Math.Cos(rad)) < 1e-10 ? "∞" : FormatTrig(System.Math.Tan(rad));
                string cotVal = System.Math.Abs(System.Math.Sin(rad)) < 1e-10 ? "∞" : FormatTrig(1.0 / System.Math.Tan(rad));
                string radStr = FormatRadian(deg);

                var row = CreateTableRow(
                    $"{deg}°", radStr, sinVal, cosVal, tanVal, cotVal, false);

                // Highlight boundary angles differently from other special angles
                if (deg == 0 || deg == 90 || deg == 180 || deg == 270 || deg == 360)
                {
                    row.Background = new SolidColorBrush(Color.FromRgb(225, 190, 231)); // Purple 100
                }
                else
                {
                    row.Background = new SolidColorBrush(Color.FromRgb(243, 229, 245)); // Purple 50
                }

                tablePanel.Children.Add(row);
            }
        }

        private Border CreateTableRow(string c1, string c2, string c3, string c4, string c5, string c6, bool isHeader)
        {
            var grid = new Grid();
            for (int i = 0; i < 6; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var texts = new[] { c1, c2, c3, c4, c5, c6 };
            for (int i = 0; i < 6; i++)
            {
                var tb = new TextBlock
                {
                    Text = texts[i],
                    FontSize = isHeader ? 15 : 14,
                    FontWeight = isHeader ? FontWeights.Bold : (i == 0 ? FontWeights.Bold : FontWeights.Normal),
                    FontFamily = new FontFamily("Segoe UI"),
                    Foreground = isHeader
                        ? new SolidColorBrush(Color.FromRgb(106, 27, 154))
                        : Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(tb, i);
                grid.Children.Add(tb);
            }

            return new Border
            {
                Child = grid,
                Background = isHeader
                    ? new SolidColorBrush(Color.FromRgb(206, 147, 216))
                    : Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = isHeader ? new Thickness(12, 10, 12, 10) : new Thickness(12, 8, 12, 8),
                CornerRadius = isHeader ? new CornerRadius(8, 8, 0, 0) : new CornerRadius(0)
            };
        }

        private static string FormatTrig(double val)
        {
            if (double.IsNaN(val) || double.IsInfinity(val)) return "∞";
            if (System.Math.Abs(val) < 1e-10) return "0";
            if (System.Math.Abs(val - 1) < 1e-10) return "1";
            if (System.Math.Abs(val + 1) < 1e-10) return "-1";
            if (System.Math.Abs(val - 0.5) < 1e-10) return "1/2";
            if (System.Math.Abs(val + 0.5) < 1e-10) return "-1/2";

            double sqrt2half = System.Math.Sqrt(2) / 2;
            if (System.Math.Abs(val - sqrt2half) < 1e-10) return "√2/2";
            if (System.Math.Abs(val + sqrt2half) < 1e-10) return "-√2/2";

            double sqrt3half = System.Math.Sqrt(3) / 2;
            if (System.Math.Abs(val - sqrt3half) < 1e-10) return "√3/2";
            if (System.Math.Abs(val + sqrt3half) < 1e-10) return "-√3/2";

            double sqrt3 = System.Math.Sqrt(3);
            if (System.Math.Abs(val - sqrt3) < 1e-10) return "√3";
            if (System.Math.Abs(val + sqrt3) < 1e-10) return "-√3";

            double sqrt3over3 = System.Math.Sqrt(3) / 3;
            if (System.Math.Abs(val - sqrt3over3) < 1e-10) return "√3/3";
            if (System.Math.Abs(val + sqrt3over3) < 1e-10) return "-√3/3";

            return val.ToString("F4");
        }

        private static string FormatRadian(double deg)
        {
            if (System.Math.Abs(deg) < 1e-9) return "0";

            double x = deg / 180.0;
            for (int q = 1; q <= 12; q++)
            {
                double pDouble = x * q;
                double pRound = System.Math.Round(pDouble);
                if (System.Math.Abs(pDouble - pRound) < 1e-4)
                {
                    long p = (long)pRound;
                    long gcd = GCD(System.Math.Abs(p), q);
                    long num = p / gcd;
                    long den = q / gcd;

                    if (num == 0) return "0";

                    if (den == 1)
                    {
                        if (num == 1) return "π";
                        if (num == -1) return "-π";
                        return $"{num}π";
                    }
                    else
                    {
                        if (num == 1) return $"π/{den}";
                        if (num == -1) return $"-π/{den}";
                        return $"{num}π/{den}";
                    }
                }
            }

            double rad = deg * System.Math.PI / 180.0;
            return $"{rad:F3}";
        }

        private static long GCD(long a, long b)
        {
            while (b != 0)
            {
                long temp = b;
                b = a % b;
                a = temp;
            }
            return System.Math.Abs(a);
        }

        // ═══════════════════════════════════════════════════════════
        //  MÁY TÍNH LƯỢNG GIÁC
        // ═══════════════════════════════════════════════════════════

        private void Angle_Changed(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) CalculateAngle();
        }

        private void Unit_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                BuildQuickAngles();
                CalculateAngle();
            }
        }

        private void QuickAngle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string val)
            {
                if (txtAngle != null)
                {
                    txtAngle.Text = val;
                    CalculateAngle();
                }
            }
        }

        private void CalculateAngle()
        {
            if (resultPanel == null || txtAngle == null) return;

            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi" || 
                        System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase) ||
                        System.Globalization.CultureInfo.CurrentCulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(txtAngle.Text))
            {
                ShowWarningCard(isVN ? "Vui lòng nhập số đo góc θ." : "Please enter angle measurement θ.");
                return;
            }

            if (!ParsingHelper.TryParseDouble(txtAngle.Text, out double input))
            {
                ShowWarningCard(isVN 
                    ? "Cú pháp góc nhập không hợp lệ.\nHỗ trợ số hoặc biểu thức như pi/3, -45." 
                    : "Invalid angle syntax.\nSupports numbers or expressions like pi/3, -45.");
                return;
            }

            double rad = rbRadian?.IsChecked == true ? input : input * System.Math.PI / 180;
            double deg = rbRadian?.IsChecked == true ? input * 180 / System.Math.PI : input;

            // Đồng bộ sang Đường tròn lượng giác tương tác
            if (!_isUpdatingInternally && trigCircle != null)
            {
                _isUpdatingInternally = true;
                try
                {
                    // Chuẩn hóa góc quay về khoảng [0, 360) để vẽ chính xác
                    double normalizedDeg = ((deg % 360.0) + 360.0) % 360.0;
                    trigCircle.Angle = normalizedDeg;
                }
                finally
                {
                    _isUpdatingInternally = false;
                }
            }

            // Boundary validation
            if (rbRadian?.IsChecked == true)
            {
                if (System.Math.Abs(input) > 628.318)
                {
                    ShowWarningCard(isVN 
                        ? "Góc nhập quá lớn.\n(Giới hạn từ -628.3 rad đến 628.3 rad)" 
                        : "Angle too large.\n(Limit: -628.3 rad to 628.3 rad)");
                    return;
                }
            }
            else
            {
                if (System.Math.Abs(input) > 36000.0)
                {
                    ShowWarningCard(isVN 
                        ? "Góc nhập quá lớn.\n(Giới hạn từ -36000° đến 36000°)" 
                        : "Angle too large.\n(Limit: -36000° to 36000°)");
                    return;
                }
            }

            resultPanel.Children.Clear();

            bool isCosZero = System.Math.Abs(System.Math.Cos(rad)) < 1e-10;
            bool isSinZero = System.Math.Abs(System.Math.Sin(rad)) < 1e-10;

            var resultsList = new System.Collections.Generic.List<(string Name, string Value, string Color)>
            {
                ("sin", FormatTrig(System.Math.Sin(rad)), "#E91E63"),
                ("cos", FormatTrig(System.Math.Cos(rad)), "#2196F3"),
                ("tan", isCosZero ? (isVN ? "Không xác định (∞)" : "Undefined (∞)") : FormatTrig(System.Math.Tan(rad)), "#FF9800"),
                ("cot", isSinZero ? (isVN ? "Không xác định (∞)" : "Undefined (∞)") : FormatTrig(1.0 / System.Math.Tan(rad)), "#4CAF50")
            };

            if (cbShowExtended != null && cbShowExtended.IsChecked == true)
            {
                resultsList.Add(("sec (Mở rộng)", isCosZero ? (isVN ? "Không xác định (∞)" : "Undefined (∞)") : FormatTrig(1.0 / System.Math.Cos(rad)), "#9C27B0"));
                resultsList.Add(("csc (Mở rộng)", isSinZero ? (isVN ? "Không xác định (∞)" : "Undefined (∞)") : FormatTrig(1.0 / System.Math.Sin(rad)), "#00BCD4"));
            }

            foreach (var (name, value, color) in resultsList)
            {
                string angleStr;
                if (rbRadian?.IsChecked == true)
                {
                    angleStr = input.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + " rad";
                }
                else
                {
                    angleStr = (deg % 1 == 0 ? deg.ToString("F0") : deg.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)) + "°";
                }
                string label = $"{name}({angleStr}) = {value}";
                UI.ResultRow(label, color, resultPanel);
            }

            // Radian/Degree conversion info
            var infoCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(243, 229, 245)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 8, 0, 0)
            };
            string radStr = FormatRadian(deg);
            string displayText = radStr.Contains("π") 
                ? $"📐 {deg:F1}° = {radStr} rad (≈ {rad:F4} rad)"
                : $"📐 {deg:F1}° = {rad:F4} rad";
            infoCard.Child = new TextBlock
            {
                Text = displayText,
                FontSize = 13, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(106, 27, 154))
            };
            resultPanel.Children.Add(infoCard);

            // Add to search history
            AddHistoryItem(txtAngle.Text, rbRadian?.IsChecked == true);
        }

        private void ShowWarningCard(string message)
        {
            if (resultPanel == null) return;
            resultPanel.Children.Clear();

            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(253, 237, 237)), // Red 50
                BorderBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54)),   // Red 500
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 8, 0, 0)
            };

            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = "⚠️ Cảnh báo / Warning",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47)),
                Margin = new Thickness(0, 0, 0, 4)
            });
            sp.Children.Add(new TextBlock
            {
                Text = message,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(95, 33, 32)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 18
            });

            card.Child = sp;
            resultPanel.Children.Add(card);
        }

        private void AddHistoryItem(string text, bool isRadian)
        {
            // Clean up the text input representation
            string cleanText = text.Trim();
            if (string.IsNullOrEmpty(cleanText)) return;

            string display = isRadian ? $"{cleanText} rad" : $"{cleanText}°";

            // Check if already in history
            foreach (var existing in _history)
            {
                if (existing.DisplayText == display)
                {
                    // Move it to the front
                    _history.Remove(existing);
                    _history.Insert(0, existing);
                    UpdateHistoryUI();
                    return;
                }
            }

            // Create new
            var item = new HistoryItem
            {
                Text = cleanText,
                IsRadian = isRadian,
                DisplayText = display
            };

            _history.Insert(0, item);
            if (_history.Count > 5)
            {
                _history.RemoveAt(_history.Count - 1);
            }

            UpdateHistoryUI();
        }

        private void UpdateHistoryUI()
        {
            if (historyPanel == null || spHistory == null) return;
            historyPanel.Children.Clear();

            if (_history.Count == 0)
            {
                spHistory.Visibility = Visibility.Collapsed;
                return;
            }

            spHistory.Visibility = Visibility.Visible;

            foreach (var item in _history)
            {
                var btn = new Button
                {
                    Content = item.DisplayText,
                    Margin = new Thickness(4),
                    Padding = new Thickness(12, 8, 12, 8),
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Background = new SolidColorBrush(Color.FromRgb(243, 229, 245)), // Purple 50
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };

                btn.Click += (s, e) =>
                {
                    if (rbRadian != null && rbDegree != null && txtAngle != null)
                    {
                        rbRadian.IsChecked = item.IsRadian;
                        rbDegree.IsChecked = !item.IsRadian;
                        txtAngle.Text = item.Text;
                        CalculateAngle();
                    }
                };

                historyPanel.Children.Add(btn);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  CÔNG THỨC THAM KHẢO
        // ═══════════════════════════════════════════════════════════

        private void BuildFormulas()
        {
            if (formulaPanel == null) return;
            formulaPanel.Children.Clear();

            var formulas = new (string Title, string[] Items)[]
            {
                ("Hệ thức cơ bản", new[]
                {
                    "sin²α + cos²α = 1",
                    "1 + tan²α = 1/cos²α",
                    "1 + cot²α = 1/sin²α",
                    "tanα = sinα / cosα",
                    "cotα = cosα / sinα",
                }),
                ("Công thức cộng", new[]
                {
                    "sin(α±β) = sinα·cosβ ± cosα·sinβ",
                    "cos(α±β) = cosα·cosβ ∓ sinα·sinβ",
                    "tan(α±β) = (tanα ± tanβ) / (1 ∓ tanα·tanβ)",
                }),
                ("Công thức nhân đôi", new[]
                {
                    "sin2α = 2·sinα·cosα",
                    "cos2α = cos²α - sin²α",
                    "cos2α = 2cos²α - 1",
                    "cos2α = 1 - 2sin²α",
                    "tan2α = 2tanα / (1 - tan²α)",
                }),
                ("Công thức hạ bậc", new[]
                {
                    "sin²α = (1 - cos2α) / 2",
                    "cos²α = (1 + cos2α) / 2",
                }),
                ("Tích → Tổng", new[]
                {
                    "2sinα·cosβ = sin(α+β) + sin(α-β)",
                    "2cosα·cosβ = cos(α-β) + cos(α+β)",
                    "2sinα·sinβ = cos(α-β) - cos(α+β)",
                }),
                ("Tổng → Tích", new[]
                {
                    "cosα + cosβ = 2·cos((α+β)/2)·cos((α-β)/2)",
                    "cosα - cosβ = -2·sin((α+β)/2)·sin((α-β)/2)",
                    "sinα + sinβ = 2·sin((α+β)/2)·cos((α-β)/2)",
                    "sinα - sinβ = 2·cos((α+β)/2)·sin((α-β)/2)",
                }),
            };

            foreach (var (title, items) in formulas)
            {
                var section = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(16, 12, 16, 12),
                    Margin = new Thickness(0, 0, 0, 12),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(206, 147, 216)),
                    BorderThickness = new Thickness(0, 0, 0, 2)
                };

                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = $"📐 {title}",
                    FontSize = 16, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(106, 27, 154)),
                    Margin = new Thickness(0, 0, 0, 8)
                });

                foreach (var item in items)
                {
                    sp.Children.Add(new TextBlock
                    {
                        Text = $"  {item}",
                        FontSize = 15,
                        FontWeight = FontWeights.SemiBold,
                        FontFamily = new FontFamily("Segoe UI"),
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                        Margin = new Thickness(0, 3, 0, 3)
                    });
                }

                section.Child = sp;
                formulaPanel.Children.Add(section);
            }
        }

        private void BuildGuide()
        {
            if (guidePanel == null) return;
            guidePanel.Children.Clear();

            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi" || 
                        System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase) ||
                        System.Globalization.CultureInfo.CurrentCulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase);

            // Section 1: Hướng dẫn sử dụng nhanh / Quick Start Guide
            var spUsage = new StackPanel();
            spUsage.Children.Add(new TextBlock
            {
                Text = isVN ? "📖 HƯỚNG DẪN SỬ DỤNG NHANH" : "📖 QUICK START GUIDE",
                FontSize = 16, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(106, 27, 154)),
                Margin = new Thickness(0, 0, 0, 8)
            });
            spUsage.Children.Add(new TextBlock
            {
                Text = isVN 
                    ? "• Tra cứu: Sử dụng tab 'Bảng giá trị' để xem nhanh giá trị lượng giác của các góc đặc biệt (Độ và Radian).\n" +
                       "• Tính toán tự do: Nhập góc bất kỳ vào ô nhập liệu ở tab 'Tính toán'. Hỗ trợ nhập số thực lẻ hoặc biểu thức toán học chứa hằng số pi (như: 'pi/3', '2pi/3', '-pi/4', '3pi').\n" +
                       "• Bàn phím ảo: Nhấp vào ô nhập góc để mở bàn phím số mini (nhấn ▲/▼ để tăng giảm góc nhanh hoặc click nút số).\n" +
                       "• Đường tròn tương tác: Kéo thả trực tiếp điểm đỏ P trên đường tròn lượng giác (ở cột phải) để thay đổi số đo góc. Các giá trị hàm lượng giác tương ứng sẽ cập nhật thời gian thực.\n" +
                       "• Phân tích nâng cao: Nhấn nút 'Xem đồ thị' để mở đồ thị Desmos phóng to trên hệ tọa độ chuyên nghiệp."
                    : "• Reference: Use the 'Value Table' tab to quickly look up trigonometric values of special angles (Degrees and Radians).\n" +
                       "• Free Calculation: Enter any angle in the input box on the 'Calculate' tab. Supports decimals or mathematical expressions with pi (such as: 'pi/3', '2pi/3', '-pi/4', '3pi').\n" +
                       "• Mini Pad: Click the input box to open the mini number pad (press ▲/▼ to quickly adjust angles or click number buttons).\n" +
                       "• Interactive Circle: Drag point P directly on the trigonometric circle (right column) to change the angle value. The values will update in real-time.\n" +
                       "• Advanced Analysis: Click 'View graph' to open a maximized Desmos window on a professional coordinate grid.",
                FontSize = 14,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 22
            });

            var cardUsage = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 16),
                BorderBrush = new SolidColorBrush(Color.FromRgb(206, 147, 216)),
                BorderThickness = new Thickness(0, 0, 0, 2)
            };
            cardUsage.Child = spUsage;
            guidePanel.Children.Add(cardUsage);

            // Section 2: Sơ đồ Quy trình Trực quan / Visual Workflow
            var spSteps = new StackPanel();
            spSteps.Children.Add(new TextBlock
            {
                Text = isVN ? "👣 QUY TRÌNH TƯƠNG TÁC TRỰC QUAN" : "👣 VISUAL INTERACTION WORKFLOW",
                FontSize = 16, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(106, 27, 154)),
                Margin = new Thickness(0, 0, 0, 8)
            });

            var gridSteps = new System.Windows.Controls.Primitives.UniformGrid { Columns = 4, Margin = new Thickness(0, 4, 0, 8) };
            
            if (isVN)
            {
                gridSteps.Children.Add(CreateStepCard("1", "Chọn đơn vị", "Chọn Độ (°) hoặc Radian (rad) phù hợp với đề bài."));
                gridSteps.Children.Add(CreateStepCard("2", "Nhập hoặc Kéo", "Gõ góc (ví dụ: 45, pi/3) hoặc kéo điểm P trực tiếp trên đường tròn."));
                gridSteps.Children.Add(CreateStepCard("3", "Xem kết quả", "Bảng hiển thị sin, cos, tan, cot trong thời gian thực, tô màu sinh động."));
                gridSteps.Children.Add(CreateStepCard("4", "Xem đồ thị 📈", "Nhấp nút xem đồ thị để quan sát điểm biểu diễn P phóng to trên Desmos."));
            }
            else
            {
                gridSteps.Children.Add(CreateStepCard("1", "Choose Unit", "Select Degree (°) or Radian (rad) matching your problem."));
                gridSteps.Children.Add(CreateStepCard("2", "Input or Drag", "Type angle value (e.g. 45, pi/3) or drag point P directly on the circle."));
                gridSteps.Children.Add(CreateStepCard("3", "View Results", "Table shows sin, cos, tan, cot in real-time with dynamic highlights."));
                gridSteps.Children.Add(CreateStepCard("4", "View Graph 📈", "Click 'View graph' to visualize point P in a maximized Desmos window."));
            }

            spSteps.Children.Add(gridSteps);

            var cardSteps = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 16),
                BorderBrush = new SolidColorBrush(Color.FromRgb(206, 147, 216)),
                BorderThickness = new Thickness(0, 0, 0, 2)
            };
            cardSteps.Child = spSteps;
            guidePanel.Children.Add(cardSteps);

            // Section 3: Nhấp nhanh ví dụ mẫu (Preset Buttons)
            var spPreset = new StackPanel();
            spPreset.Children.Add(new TextBlock
            {
                Text = isVN ? "⚡ THỬ NGHIỆM NHANH CÁC VÍ DỤ" : "⚡ QUICK PRESETS & SAMPLES",
                FontSize = 16, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(106, 27, 154)),
                Margin = new Thickness(0, 0, 0, 4)
            });
            spPreset.Children.Add(new TextBlock
            {
                Text = isVN 
                    ? "Nhấp chuột vào một ví dụ dưới đây để phần mềm tự động nạp dữ liệu mẫu và tính toán trực quan:"
                    : "Click any example below to automatically load sample data and visualize calculations:",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                Margin = new Thickness(0, 0, 0, 12),
                TextWrapping = TextWrapping.Wrap
            });

            var wrapPresets = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
            
            UI.PresetButton(isVN ? "Ví dụ 1: Góc âm -120°" : "Example 1: Negative angle -120°", Color.FromRgb(233, 30, 99), () =>
            {
                rbDegree.IsChecked = true;
                txtAngle.Text = "-120";
                CalculateAngle();
                
                DependencyObject p = this;
                while (p != null && !(p is TabControl))
                {
                    p = VisualTreeHelper.GetParent(p) ?? LogicalTreeHelper.GetParent(p);
                }
                if (p is TabControl tc)
                {
                    tc.SelectedIndex = 1;
                }
            }, wrapPresets);

            UI.PresetButton(isVN ? "Ví dụ 2: Radian pi/3" : "Example 2: Radian pi/3", Color.FromRgb(33, 150, 243), () =>
            {
                rbRadian.IsChecked = true;
                txtAngle.Text = "pi/3";
                CalculateAngle();
                
                DependencyObject p = this;
                while (p != null && !(p is TabControl))
                {
                    p = VisualTreeHelper.GetParent(p) ?? LogicalTreeHelper.GetParent(p);
                }
                if (p is TabControl tc)
                {
                    tc.SelectedIndex = 1;
                }
            }, wrapPresets);

            UI.PresetButton(isVN ? "Ví dụ 3: Góc biên 90°" : "Example 3: Quadrantal angle 90°", Color.FromRgb(255, 152, 0), () =>
            {
                rbDegree.IsChecked = true;
                txtAngle.Text = "90";
                CalculateAngle();
                
                DependencyObject p = this;
                while (p != null && !(p is TabControl))
                {
                    p = VisualTreeHelper.GetParent(p) ?? LogicalTreeHelper.GetParent(p);
                }
                if (p is TabControl tc)
                {
                    tc.SelectedIndex = 1;
                }
            }, wrapPresets);

            UI.PresetButton(isVN ? "Ví dụ 4: Tuần hoàn 405°" : "Example 4: Periodic angle 405°", Color.FromRgb(76, 175, 80), () =>
            {
                rbDegree.IsChecked = true;
                txtAngle.Text = "405";
                CalculateAngle();
                
                DependencyObject p = this;
                while (p != null && !(p is TabControl))
                {
                    p = VisualTreeHelper.GetParent(p) ?? LogicalTreeHelper.GetParent(p);
                }
                if (p is TabControl tc)
                {
                    tc.SelectedIndex = 1;
                }
            }, wrapPresets);

            spPreset.Children.Add(wrapPresets);

            var cardPreset = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 16),
                BorderBrush = new SolidColorBrush(Color.FromRgb(206, 147, 216)),
                BorderThickness = new Thickness(0, 0, 0, 2)
            };
            cardPreset.Child = spPreset;
            guidePanel.Children.Add(cardPreset);

            // Section 4: Phân tích sư phạm / Pedagogical Analysis
            var spPedagogy = new StackPanel();
            spPedagogy.Children.Add(new TextBlock
            {
                Text = isVN ? "🎓 PHÂN TÍCH TOÁN HỌC & GIẢNG DẠY" : "🎓 MATHEMATICAL & PEDAGOGICAL ANALYSIS",
                FontSize = 16, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(106, 27, 154)),
                Margin = new Thickness(0, 0, 0, 8)
            });
            spPedagogy.Children.Add(new TextBlock
            {
                Text = isVN 
                    ? "• Khái niệm Góc lượng giác tổng quát: Khác với hình học tĩnh (góc luôn từ 0° đến 180°), lượng giác học lớp 11 làm việc với góc quay tùy ý. Hãy thử nhập '-45°' hoặc '405°' để thấy điểm biểu diễn P tự động xoay và tính ra các giá trị âm hoặc dương tương ứng.\n\n" +
                       "• Tính tuần hoàn: Khi nhập các góc θ hơn kém nhau bội của 360° (hoặc 2π rad), ví dụ 45° và 405°, các giá trị sin, cos, tan, cot hoàn toàn trùng khớp vì điểm biểu diễn P nằm tại cùng một tọa độ trên đường tròn đơn vị.\n\n" +
                       "• Bản chất hình học của Giá trị không xác định: Tại góc 90° (hoặc pi/2 rad), hoành độ cos = 0. Do tan = sin/cos có mẫu số bằng 0 nên không xác định. Giáo viên có thể hướng dẫn học sinh bấm 'Xem đồ thị' để thấy hình chiếu của điểm P(0; 1) lên trục Ox chính xác tại gốc tọa độ (0), giải thích trực quan tại sao tan(90°) không tồn tại."
                    : "• Concept of General Trigonometric Angles: Unlike static geometry (where angles are strictly between 0° and 180°), high school trigonometry works with arbitrary rotational angles. Try entering '-45°' or '405°' to see the point P rotate dynamically and yield positive or negative values.\n\n" +
                       "• Periodicity: When entering angles θ that differ by multiples of 360° (or 2π rad), such as 45° and 405°, the values of sin, cos, tan, and cot match perfectly because the terminal point P lands on the exact same coordinates on the unit circle.\n\n" +
                       "• Geometrical Nature of Undefined Values: At 90° (or pi/2 rad), the x-coordinate cos = 0. Since tan = sin/cos has a zero denominator, it is undefined. Teachers can guide students to click 'View graph' to observe that the projection of point P(0; 1) onto the x-axis lies exactly at the origin (0), explaining visually why tan(90°) does not exist.",
                FontSize = 14,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 22
            });

            var cardPedagogy = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(206, 147, 216)),
                BorderThickness = new Thickness(0, 0, 0, 2)
            };
            cardPedagogy.Child = spPedagogy;
            guidePanel.Children.Add(cardPedagogy);
        }

        private Border CreateStepCard(string stepNum, string title, string desc)
        {
            var sp = new StackPanel();
            
            var numBadge = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromRgb(106, 27, 154)),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 8)
            };
            numBadge.Child = new TextBlock
            {
                Text = stepNum,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            sp.Children.Add(numBadge);

            sp.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(106, 27, 154)),
                Margin = new Thickness(0, 0, 0, 4)
            });

            sp.Children.Add(new TextBlock
            {
                Text = desc,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 16
            });

            return new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 243, 249)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(4),
                BorderBrush = new SolidColorBrush(Color.FromRgb(230, 224, 240)),
                BorderThickness = new Thickness(1),
                Child = sp
            };
        }

        private void LoadPracticalApps()
        {
            try
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                string suffix = isVN ? "VN" : "EN";

                var items = new System.Collections.Generic.List<PracticalAppItem>
                {
                    new PracticalAppItem
                    {
                        Icon = "🧭",
                        Title = isVN ? "Định vị GPS hải trình" : "Marine GPS Navigation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_trig_1_{suffix}.png",
                        Description = isVN 
                            ? "Định vị tọa độ và dẫn đường cho tàu biển thông qua việc tính toán các hàm lượng giác trên hệ tọa độ cầu Trái Đất." 
                            : "Locating coordinates and navigating ships through calculating trigonometric functions on the Earth's spherical coordinate system."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔄",
                        Title = isVN ? "Dao Động Điều Hòa Vật Lý" : "Physical Harmonic Oscillation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_trig_2_{suffix}.png",
                        Description = isVN 
                            ? "Chuyển động của con lắc lò xo và pit-tông động cơ được mô tả bằng hàm số lượng giác hình sin tuần hoàn." 
                            : "Model spring-mass oscillators and piston motion in engines over time using periodic sinusoidal trigonometric equations."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌊",
                        Title = isVN ? "Thủy Triều Đại Dương" : "Oceanic Tides Prediction",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_trig_3_{suffix}.png",
                        Description = isVN 
                            ? "Độ cao của mực nước biển lên xuống tuần hoàn do lực hấp dẫn từ Mặt Trăng và Mặt Trời được dự báo qua mô hình sóng lượng giác." 
                            : "Model high and low tide periods over time using sinusoidal waves to schedule ship departures and arrivals safely."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎡",
                        Title = isVN ? "Vòng Quay Ferris Khổng Lồ" : "Giant Ferris Wheel",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_trig_4_{suffix}.png",
                        Description = isVN 
                            ? "Vị trí độ cao và tọa độ cabin trên đu quay theo thời gian được mô tả qua hàm số lượng giác sin/cos tròn." 
                            : "Model the height and coordinate position of a passenger cabin on a giant Ferris wheel over time using sinusoidal functions."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⚡",
                        Title = isVN ? "Dòng Điện Xoay Chiều (AC)" : "Alternating Current (AC)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_trig_5_{suffix}.png",
                        Description = isVN 
                            ? "Điện áp và cường độ dòng điện xoay chiều biến thiên hình sin theo thời gian, được tính toán thông qua lượng giác." 
                            : "Solve trigonometric equations to find peak voltage, frequency, and phase alignment in AC electrical circuits."
                    },
                    new PracticalAppItem
                    {
                        Icon = "☀",
                        Title = isVN ? "Chu Kỳ Khí Hậu & Thời Tiết" : "Climate & Weather Cycles",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_trig_6_{suffix}.png",
                        Description = isVN 
                            ? "Sự thay đổi nhiệt độ trung bình và số giờ nắng giữa các tháng lặp lại theo chu kỳ mùa và được mô hình hóa bằng lượng giác." 
                            : "Model periodic variations in average temperature and daylight hours throughout the seasons using trigonometric wave equations."
                    }
                };

                if (practicalAppViewer != null) practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading practical apps: " + ex.Message);
            }
        }

        private Border CreateAppCard(string title, string imagePath, string description)
        {
            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12),
                Margin = new Thickness(4),
                BorderBrush = new SolidColorBrush(Color.FromRgb(225, 190, 231)), // Purple 100
                BorderThickness = new Thickness(1)
            };
            card.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 8,
                ShadowDepth = 1,
                Opacity = 0.06
            };

            var sp = new StackPanel();

            // Title
            sp.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(106, 27, 154)), // Purple 800
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            });

            // Image Container
            var imgBorder = new Border
            {
                CornerRadius = new CornerRadius(8),
                ClipToBounds = true,
                Height = 150,
                Background = new SolidColorBrush(Color.FromRgb(245, 247, 250)),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var img = new Image
            {
                Stretch = Stretch.Uniform
            };

            try
            {
                if (System.IO.File.Exists(imagePath))
                {
                    img.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(imagePath));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading image: " + ex.Message);
            }

            imgBorder.Child = img;
            sp.Children.Add(imgBorder);

            // Description
            sp.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 16,
                MinHeight = 64
            });

            card.Child = sp;
            return card;
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            _history.Clear();
            UpdateHistoryUI();
        }

        private void ShowExtended_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded) CalculateAngle();
        }

        private void BuildQuickAngles()
        {
            if (wpQuickAngles == null) return;
            wpQuickAngles.Children.Clear();

            bool isRad = rbRadian?.IsChecked == true;
            if (isRad)
            {
                var radianAngles = new (string Display, string Value)[]
                {
                    ("0", "0"), ("π/6", "pi/6"), ("π/4", "pi/4"), ("π/3", "pi/3"), ("π/2", "pi/2"),
                    ("2π/3", "2*pi/3"), ("3π/4", "3*pi/4"), ("5π/6", "5*pi/6"), ("π", "pi"),
                    ("7π/6", "7*pi/6"), ("5π/4", "5*pi/4"), ("4π/3", "4*pi/3"), ("3π/2", "3*pi/2"),
                    ("5π/3", "5*pi/3"), ("7π/4", "7*pi/4"), ("11π/6", "11*pi/6"), ("2π", "2*pi")
                };

                foreach (var angle in radianAngles)
                {
                    wpQuickAngles.Children.Add(CreateQuickAngleButton(angle.Display, angle.Value));
                }
            }
            else
            {
                int[] degreeAngles = { 0, 30, 45, 60, 90, 120, 135, 150, 180, 210, 225, 240, 270, 300, 315, 330, 360 };
                foreach (var deg in degreeAngles)
                {
                    wpQuickAngles.Children.Add(CreateQuickAngleButton($"{deg}°", deg.ToString()));
                }
            }
        }

        private Button CreateQuickAngleButton(string display, string value)
        {
            var btn = new Button
            {
                Content = display,
                Tag = value,
                Margin = new Thickness(4),
                Padding = new Thickness(14, 10, 14, 10),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush(Color.FromRgb(243, 229, 245)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btn.Click += QuickAngle_Click;
            return btn;
        }

        private void ResetAll_Click(object sender, MouseButtonEventArgs e)
        {
            rbDegree.IsChecked = true;
            txtAngle.Text = "45";
            CalculateAngle();
        }

        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                double input = 45;
                ParsingHelper.TryParseDouble(txtAngle?.Text, out input);
                double rad = rbRadian?.IsChecked == true ? input : input * System.Math.PI / 180;
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                string rS = rad.ToString(ci);

                double cosVal = System.Math.Cos(rad);
                double sinVal = System.Math.Sin(rad);
                string cS = cosVal.ToString(ci);
                string sS = sinVal.ToString(ci);

                // Clamp the arc angle to [-2pi, 2pi] to prevent overlapping circles
                double arcRad = rad;
                if (arcRad > 2 * System.Math.PI)
                    arcRad = ((arcRad % (2 * System.Math.PI)) + 2 * System.Math.PI) % (2 * System.Math.PI);
                else if (arcRad < -2 * System.Math.PI)
                    arcRad = -(((System.Math.Abs(arcRad) % (2 * System.Math.PI)) + 2 * System.Math.PI) % (2 * System.Math.PI));
                string arcRS = arcRad.ToString(ci);

                string expJs = $@"
        calc.setExpression({{id:'circle', latex:'x^2+y^2=1', color:'#9E9E9E', lineWidth:2}});
        calc.setExpression({{id:'sin', latex:'y=\\sin(x)', color:'#E91E63', lineWidth:3, hidden:true}});
        calc.setExpression({{id:'cos', latex:'y=\\cos(x)', color:'#2196F3', lineWidth:3, hidden:true}});
        calc.setExpression({{id:'tan', latex:'y=\\tan(x)', color:'#FF9800', lineWidth:2, hidden:true}});
        calc.setExpression({{id:'ray', latex:'(t * {cS}, t * {sS})', color:'#4CAF50', lineWidth:3}});
        calc.setExpression({{id:'proj_cos', latex:'({cS}, t * {sS})', color:'#2196F3', lineStyle:'DASHED', lineWidth:2}});
        calc.setExpression({{id:'proj_sin', latex:'(t * {cS}, {sS})', color:'#E91E63', lineStyle:'DASHED', lineWidth:2}});
        calc.setExpression({{id:'pt_cos', latex:'({cS},0)', color:'#2196F3', pointSize:10, label:'cos(θ) = {FormatTrig(cosVal)}', showLabel:true}});
        calc.setExpression({{id:'pt_sin', latex:'(0,{sS})', color:'#E91E63', pointSize:10, label:'sin(θ) = {FormatTrig(sinVal)}', showLabel:true}});
        calc.setExpression({{id:'pt', latex:'({cS},{sS})', color:'#E53935', pointSize:14, label:'P({FormatTrig(cosVal)}; {FormatTrig(sinVal)})', showLabel:true}});
        
        // Quadrant labels
        calc.setExpression({{id:'q1', latex:'(1.2,1.2)', color:'#757575', pointSize:0, label:'I', showLabel:true}});
        calc.setExpression({{id:'q2', latex:'(-1.2,1.2)', color:'#757575', pointSize:0, label:'II', showLabel:true}});
        calc.setExpression({{id:'q3', latex:'(-1.2,-1.2)', color:'#757575', pointSize:0, label:'III', showLabel:true}});
        calc.setExpression({{id:'q4', latex:'(1.2,-1.2)', color:'#757575', pointSize:0, label:'IV', showLabel:true}});
        
        // Parametric arc representation for angle theta
        calc.setExpression({{id:'arc', latex:'(0.4 * \\cos(t), 0.4 * \\sin(t))', color:'#FF9800', lineWidth:3, parametricDomain:{{min:'0',max:'{arcRS}'}}}});

        calc.setMathBounds({{ left: -2, right: 2, bottom: -1.5, top: 1.5 }});";

                string unitStr = rbRadian?.IsChecked == true ? " rad" : "°";
                var win = new GraphWindow(expJs, $"📐 Đường tròn lượng giác — θ = {input}{unitStr}");
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Không thể mở đồ thị: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewTable == null || viewCalc == null || viewFormula == null || viewTheory == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewTable.Visibility = Visibility.Collapsed;
            viewCalc.Visibility = Visibility.Collapsed;
            viewFormula.Visibility = Visibility.Collapsed;
            viewTheory.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewTable.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewCalc.Visibility = Visibility.Visible;
                    break;
                case 3:
                    viewFormula.Visibility = Visibility.Visible;
                    break;
                case 4:
                    viewTheory.Visibility = Visibility.Visible;
                    break;
                case 5:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}

