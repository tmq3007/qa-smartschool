using QASmartClass.LearningTools.Models;
using System;
using QASmartClass.LearningTools.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Science
{
    public partial class PhScaleTool : BaseToolControl
    {
        private bool _isUpdatingFromCode = false;

        public PhScaleTool()
        {
            InitializeComponent();
            // Attach TouchNumPad
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtHConc, step: 1);

            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextScale != null) menuTextScale.Text = isVN ? "Thang pH" : "pH Scale";
                if (menuTextCalculator != null) menuTextCalculator.Text = isVN ? "Tính pH" : "pH Calculator";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                BuildPhScale();
                BuildExamples();
                BuildPhPresets();
                CalculatePh();
                LoadPracticalApps();
            };
        }

        private void SliderPh_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingFromCode) return;
            _isUpdatingFromCode = true;
            try
            {
                double sliderVal = e.NewValue;
                if (lblSliderVal != null)
                {
                    lblSliderVal.Text = $"pH = {FormatVietnamese(System.Math.Round(sliderVal, 1))}";
                }
                double hConc = System.Math.Pow(10, -sliderVal);
                if (txtHConc != null)
                {
                    string formatted = hConc.ToString("E3", System.Globalization.CultureInfo.InvariantCulture);
                    if (formatted.Contains("E-00")) formatted = formatted.Replace("E-00", "E-0");
                    if (formatted.Contains("E+00")) formatted = formatted.Replace("E+00", "E+0");
                    txtHConc.Text = formatted;
                }
            }
            finally
            {
                _isUpdatingFromCode = false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  VISUAL pH SCALE
        // ═══════════════════════════════════════════════════════════

        private static readonly Color[] PhColors =
        {
            Color.FromRgb(255, 0, 0),       // 0
            Color.FromRgb(255, 51, 0),      // 1
            Color.FromRgb(255, 102, 0),     // 2
            Color.FromRgb(255, 153, 0),     // 3
            Color.FromRgb(255, 204, 0),     // 4
            Color.FromRgb(255, 255, 0),     // 5
            Color.FromRgb(204, 255, 0),     // 6
            Color.FromRgb(0, 200, 0),       // 7
            Color.FromRgb(0, 180, 100),     // 8
            Color.FromRgb(0, 150, 150),     // 9
            Color.FromRgb(0, 100, 200),     // 10
            Color.FromRgb(0, 50, 220),      // 11
            Color.FromRgb(50, 0, 200),      // 12
            Color.FromRgb(100, 0, 180),     // 13
            Color.FromRgb(120, 0, 160),     // 14
        };

        private static readonly string[] PhLabels =
        {
            "Axit mạnh", "Axit mạnh", "Axit mạnh",
            "Axit vừa", "Axit vừa",
            "Axit yếu", "Axit yếu",
            "Trung tính",
            "Kiềm yếu", "Kiềm yếu",
            "Kiềm vừa", "Kiềm vừa",
            "Kiềm mạnh", "Kiềm mạnh", "Kiềm mạnh"
        };

        private void BuildPhScale()
        {
            if (phScalePanel == null) return;
            phScalePanel.Children.Clear();

            // Title
            phScalePanel.Children.Add(new TextBlock
            {
                Text = "📊 Thang pH (0 → 14)",
                FontSize = 14, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(106, 27, 154)),
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Scale bar
            var scaleGrid = new Grid { Height = 50, Margin = new Thickness(0, 0, 0, 4) };
            for (int i = 0; i <= 14; i++)
                scaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i <= 14; i++)
            {
                var cell = new Border
                {
                    Background = new SolidColorBrush(PhColors[i]),
                    CornerRadius = i == 0 ? new CornerRadius(8, 0, 0, 8)
                        : i == 14 ? new CornerRadius(0, 8, 8, 0)
                        : new CornerRadius(0),
                    Child = new TextBlock
                    {
                        Text = $"{i}",
                        FontSize = 14, FontWeight = FontWeights.Bold,
                        Foreground = i < 5 || i > 11 ? Brushes.White : Brushes.Black,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    ToolTip = $"pH {i} — {PhLabels[i]} — [H⁺] = 10⁻{i} mol/L"
                };
                Grid.SetColumn(cell, i);
                scaleGrid.Children.Add(cell);
            }
            phScalePanel.Children.Add(scaleGrid);

            // Labels
            var labelGrid = new Grid();
            labelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            labelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            labelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labels = new[] {
                ("🔴 AXIT (0-6)", HorizontalAlignment.Left),
                ("🟢 TRUNG TÍNH (7)", HorizontalAlignment.Center),
                ("🔵 KIỀM (8-14)", HorizontalAlignment.Right)
            };
            for (int i = 0; i < 3; i++)
            {
                var tb = new TextBlock
                {
                    Text = labels[i].Item1, FontSize = 11, FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = labels[i].Item2,
                    Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)),
                    Margin = new Thickness(0, 4, 0, 0)
                };
                Grid.SetColumn(tb, i);
                labelGrid.Children.Add(tb);
            }
            phScalePanel.Children.Add(labelGrid);
        }

        // ═══════════════════════════════════════════════════════════
        //  EXAMPLES
        // ═══════════════════════════════════════════════════════════

        private void BuildExamples()
        {
            if (phExamplesPanel == null) return;
            phExamplesPanel.Children.Clear();

            phExamplesPanel.Children.Add(new TextBlock
            {
                Text = "📋 Ví dụ các chất",
                FontSize = 14, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(106, 27, 154)),
                Margin = new Thickness(0, 0, 0, 8)
            });

            var examples = new (string Name, double Ph, string Category)[]
            {
                ("Axit HCl đậm đặc", 0, "Axit mạnh"),
                ("Axit dạ dày", 1, "Axit mạnh"),
                ("Nước chanh", 2, "Axit mạnh"),
                ("Giấm ăn", 2.9, "Axit vừa"),
                ("Nước cam", 3.5, "Axit vừa"),
                ("Cà phê", 5, "Axit yếu"),
                ("Sữa bò", 6.5, "Axit yếu"),
                ("Nước tinh khiết", 7, "Trung tính"),
                ("Máu người", 7.4, "Kiềm yếu"),
                ("Nước biển", 8.1, "Kiềm yếu"),
                ("Baking soda", 9, "Kiềm yếu"),
                ("Thuốc tẩy", 10, "Kiềm vừa"),
                ("Amoniac", 11, "Kiềm vừa"),
                ("Vôi tôi", 12.5, "Kiềm mạnh"),
                ("NaOH đậm đặc", 14, "Kiềm mạnh"),
            };

            var wrapPanel = new WrapPanel();
            foreach (var (name, ph, category) in examples)
            {
                int colorIdx = System.Math.Clamp((int)System.Math.Round(ph), 0, 14);
                var color = PhColors[colorIdx];

                var card = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(30, color.R, color.G, color.B)),
                    BorderBrush = new SolidColorBrush(color),
                    BorderThickness = new Thickness(0, 0, 0, 2),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 8, 8),
                    MinWidth = 140,
                    ToolTip = $"{name} — pH {ph:F1} — {category}"
                };

                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = name, FontSize = 12, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
                });
                sp.Children.Add(new TextBlock
                {
                    Text = $"pH {ph:F1}", FontSize = 11,
                    FontFamily = new FontFamily("Segoe UI"),
                    Foreground = new SolidColorBrush(color)
                });
                card.Child = sp;
                wrapPanel.Children.Add(card);
            }
            phExamplesPanel.Children.Add(wrapPanel);
        }

        // ═══════════════════════════════════════════════════════════
        //  CALCULATOR
        // ═══════════════════════════════════════════════════════════

        private void PhCalc_Changed(object sender, TextChangedEventArgs e)
        {
            if (phCalcResultPanel != null) CalculatePh();
        }

        private void CalculatePh()
        {
            if (phCalcResultPanel == null) return;
            phCalcResultPanel.Children.Clear();

            if (!ParsingHelper.TryParseDouble(txtHConc?.Text, out double hConc) || hConc <= 0)
            {
                UI.ResultRow("⚠️ Nhập nồng độ [H⁺] > 0", "#E65100", phCalcResultPanel);
                return;
            }

            double ph = -System.Math.Log10(hConc);
            double poh = 14 - ph;
            double ohConc = System.Math.Pow(10, -poh);

            string classification = ph < 7 ? "🔴 AXIT" : ph > 7 ? "🔵 KIỀM" : "🟢 TRUNG TÍNH";
            int colorIdx = System.Math.Clamp((int)System.Math.Round(ph), 0, 14);

            // Hiển thị cảnh báo ngoài khoảng đo chuẩn (0 - 14) nếu vượt biên
            if (ph < 0 || ph > 14)
            {
                var warningBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(255, 243, 224)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 8)
                };
                warningBorder.Child = new TextBlock
                {
                    Text = "⚠️ Nồng độ đặc biệt: ngoài khoảng đo chuẩn (0 - 14)",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0)),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                phCalcResultPanel.Children.Add(warningBorder);
            }

            UI.ResultRow($"📊 pH = {FormatVietnamese(System.Math.Round(ph, 4))}", "#6A1B9A", phCalcResultPanel);
            UI.ResultRow($"📊 pOH = {FormatVietnamese(System.Math.Round(poh, 4))}", "#1565C0", phCalcResultPanel);
            UI.ResultRow($"📊 [OH⁻] = {FormatToPedagogicalScientific(ohConc)} mol/L", "#2E7D32", phCalcResultPanel);
            UI.ResultRow($"📊 Phân loại: {classification}", "#424242", phCalcResultPanel);

            // Visual indicator
            var indicator = new Border
            {
                Background = new SolidColorBrush(PhColors[colorIdx]),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 10, 16, 10),
                Margin = new Thickness(0, 8, 0, 0)
            };
            indicator.Child = new TextBlock
            {
                Text = $"pH ≈ {FormatVietnamese(System.Math.Round(ph, 1))} — {PhLabels[colorIdx]}",
                FontSize = 16, FontWeight = FontWeights.Bold,
                Foreground = colorIdx < 5 || colorIdx > 11 ? Brushes.White : Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            phCalcResultPanel.Children.Add(indicator);

            // Đồng bộ ngược lại Slider
            if (!_isUpdatingFromCode && sliderPhControl != null)
            {
                _isUpdatingFromCode = true;
                try
                {
                    sliderPhControl.Value = System.Math.Clamp(ph, 0, 14);
                    if (lblSliderVal != null)
                    {
                        lblSliderVal.Text = $"pH = {FormatVietnamese(System.Math.Round(ph, 1))}";
                    }
                }
                finally
                {
                    _isUpdatingFromCode = false;
                }
            }
        }

        // AddResult() đã được thay thế bằng UI.ResultRow()
        // ═══ RESET ═══
        private void ResetAll_Click(object sender, MouseButtonEventArgs e)
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 2)
            {
                sideMenu.SelectedIndex = 0;
            }
            if (txtHConc != null) txtHConc.Text = "0.001";
        }

        // ═══ PRESETS ═══
        private void BuildPhPresets()
        {
            if (phPresetPanel == null) return;
            var presets = new (string Label, double HConc, string Color)[]
            {
                ("🔴 Axit HCl (pH≈0)", 1.0, "#D50000"),
                ("🟠 Giấm (pH≈3)", 0.001, "#FF6D00"),
                ("🟡 Cà phê (pH≈5)", 0.00001, "#FFD600"),
                ("🟢 Nước tinh khiết (pH=7)", 0.0000001, "#00C853"),
                ("🔵 Nước biển (pH≈8)", 0.00000001, "#2962FF"),
                ("🟣 Amoniac (pH≈11)", 0.00000000001, "#AA00FF"),
                ("⚫ NaOH (pH≈14)", 0.00000000000001, "#6A1B9A"),
            };
            foreach (var (label, hConc, color) in presets)
            {
                var c = (Color)ColorConverter.ConvertFromString(color);
                double val = hConc;
                UI.PresetButton(label, c, () =>
                {
                    if (txtHConc != null) txtHConc.Text = val.ToString("E2");
                }, phPresetPanel);
            }
        }

        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                double h = 0.001;
                ParsingHelper.TryParseDouble(txtHConc?.Text, out h);
                if (h <= 0) h = 0.001;
                double ph = -System.Math.Log10(h);
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                string expJs = $@"
        calc.setExpression({{id:'ph', latex:'y=-\\log_{{10}}(x)', color:'#7B1FA2', lineWidth:3}});
        calc.setExpression({{id:'n7', latex:'y=7', color:'#4CAF50', lineWidth:1, lineStyle:'DASHED'}});
        calc.setExpression({{id:'pt', latex:'({h.ToString(ci)},{ph.ToString(ci)})', color:'#E53935', pointSize:14, label:'pH={ph:F1}', showLabel:true}});
        calc.setMathBounds({{ left: -0.05, right: 1.2, bottom: -1, top: 16 }});";
                var win = new Math.GraphWindow(expJs, $"🧪 pH = −log₁₀[H⁺]");
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (System.Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
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
            if (abs >= 1e12 || abs < 1e-6)
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

        private static System.Windows.Media.Imaging.BitmapImage LoadImageSafe(string path)
        {
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
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
                        Icon = "🍔",
                        Title = isVN ? "Dạ dày & Tiêu hóa (pH 1.5 - 3.5)" : "Gastric Acid & Antacids",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_phscale_1_{suffix}.png",
                        Description = isVN 
                            ? "Dịch vị dạ dày có tính axit cực mạnh giúp tiêu hóa thức ăn, tiêu diệt vi khuẩn có hại và kích hoạt các enzyme tiêu hóa." 
                            : "Neutralize excess stomach acid (pH 1.5 - 3.5) using alkaline antacid tablets (containing Mg(OH)₂ or CaCO₃) to relieve heartburn."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌱",
                        Title = isVN ? "Nông nghiệp & Đất trồng" : "Agricultural Soil Adjustment",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_phscale_2_{suffix}.png",
                        Description = isVN 
                            ? "Độ pH của đất quyết định khả năng hấp thụ dinh dưỡng của cây. Mỗi loài cây cần một dải pH đất thích hợp để sinh trưởng tốt." 
                            : "Measure soil pH and apply lime (to raise pH) or sulfur (to lower pH) to optimize nutrient absorption for crops."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧴",
                        Title = isVN ? "Chăm sóc da & Mỹ phẩm (pH 5.5)" : "Skin Care & Cosmetics",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_phscale_3_{suffix}.png",
                        Description = isVN 
                            ? "Da người có lớp màng bảo vệ axit nhẹ (pH ~5.5). Mỹ phẩm cần có độ pH tương đương để không làm tổn hại hoặc kích ứng da." 
                            : "Formulate face washes and soaps near skin's natural acidic pH (around 5.5) to protect the acid mantle barrier."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏊",
                        Title = isVN ? "Xử lý nước & Bể bơi (pH 7.2 - 7.6)" : "Swimming Pool Maintenance",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_phscale_4_{suffix}.png",
                        Description = isVN 
                            ? "Kiểm soát pH nước bể bơi giúp tối ưu hiệu quả diệt khuẩn của Clo, tránh gây xót mắt và bảo vệ thiết bị lọc khỏi bị ăn mòn." 
                            : "Monitor and adjust pool water pH between 7.2 and 7.6 to prevent skin irritation and optimize chlorine sanitizing power."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌧",
                        Title = isVN ? "️ Mưa axit & Môi trường" : "Acid Rain & Environment",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_phscale_5_{suffix}.png",
                        Description = isVN 
                            ? "Mưa có độ pH < 5.6 (do khí thải SO2, NOx tạo ra) gây hủy hoại rừng cây, làm chua nguồn nước hồ và giết hại các loài thủy sinh." 
                            : "Acid rain (pH < 5.6, caused by SO₂ and NOx emissions) destroys forests, acidifies lakes, and harms aquatic ecosystems."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧀",
                        Title = isVN ? "Công nghệ thực phẩm & Lên men" : "Food Technology & Fermentation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_phscale_6_{suffix}.png",
                        Description = isVN 
                            ? "Giám sát pH chặt chẽ trong quá trình sản xuất sữa chua, bia, nước ngọt giúp kiểm soát sự phát triển của vi sinh vật và giữ hương vị." 
                            : "Keep canned food pH below 4.6 (highly acidic) to inhibit the growth of dangerous botulinum bacteria and extend shelf life."
                    }
                ,
                    new PracticalAppItem
                    {
                        Icon = "🦐",
                        Title = isVN ? "Nuôi thủy sản công nghệ cao" : "High-Tech Aquaculture pH Control",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_phscale_7_{suffix}.png",
                        Description = isVN 
                            ? "Theo dõi và tự động điều chỉnh độ pH của nước ao nuôi tôm cá ở mức tối ưu 7.5 - 8.5 để ngăn ngừa dịch bệnh." 
                            : "Monitor and automatically adjust pond water pH to the optimal range of 7.5 - 8.5 to prevent aquaculture diseases."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧪",
                        Title = isVN ? "Xử lý nước thải dệt nhuộm" : "Textile Wastewater Neutralization",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_phscale_8_{suffix}.png",
                        Description = isVN 
                            ? "Trung hòa nước thải kiềm tính bằng axit để đưa độ pH về trung tính trước khi đưa qua hệ thống lọc vi sinh." 
                            : "Neutralize highly alkaline dye wastewater with acids to return pH to neutral before microbiological filtration."
                    }};

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for PhScaleTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewScale == null || viewCalculator == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewScale.Visibility = Visibility.Collapsed;
            viewCalculator.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewScale.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewCalculator.Visibility = Visibility.Visible;
                    break;
                case 3:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
