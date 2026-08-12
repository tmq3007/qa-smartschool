using QASmartClass.LearningTools.Models;
using System;
using QASmartClass.LearningTools.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Science
{
    public partial class UnitConverterTool : BaseToolControl
    {
        private string _activeType = "📏 Chiều dài";
        private readonly List<string> _history = new();
        private bool _loadingPreset = false;

        public UnitConverterTool()
        {
            InitializeComponent();
            // Attach TouchNumPad
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtFromValue, step: 1);

            txtFromValue.KeyDown += (s, e) => { if (e.Key == Key.Enter) CommitConversionToHistory(); };
            txtFromValue.LostFocus += (s, e) => CommitConversionToHistory();

            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextConverter != null) menuTextConverter.Text = isVN ? "Quy đổi" : "Converter";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                BuildUnitTypeButtons();
                LoadUnitsForType(_activeType);
                LoadPracticalApps();
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  UNIT DATA — 10 loại đơn vị, mở rộng từ STEM
        // ═══════════════════════════════════════════════════════════

        private static readonly Dictionary<string, List<(string Name, double Factor)>> Units = new()
        {
            ["📏 Chiều dài"] = new()
            {
                ("m", 1), ("cm", 0.01), ("mm", 0.001), ("km", 1000),
                ("inch", 0.0254), ("ft", 0.3048), ("yard", 0.9144),
                ("mile", 1609.34), ("hải lý", 1852),
                ("nm", 1e-9), ("μm", 1e-6), ("dm", 0.1), ("dam", 10), ("hm", 100)
            },
            ["⚖️ Khối lượng"] = new()
            {
                ("kg", 1), ("g", 0.001), ("mg", 1e-6), ("μg", 1e-9),
                ("tấn", 1000), ("tạ", 100), ("yến", 10),
                ("lb", 0.453592), ("oz", 0.0283495), ("carat", 0.0002),
                ("stone", 6.35029)
            },
            ["🌡️ Nhiệt độ"] = new() { ("°C", 1), ("°F", 1), ("K", 1) },
            ["📐 Diện tích"] = new()
            {
                ("m²", 1), ("cm²", 1e-4), ("mm²", 1e-6), ("km²", 1e6),
                ("ha", 1e4), ("acre", 4046.86), 
                ("sào Bắc", 360), ("sào Trung", 500), ("sào Nam", 1000),
                ("mẫu Bắc", 3600), ("mẫu Trung", 5000), ("mẫu Nam", 10000),
                ("ft²", 0.092903), ("inch²", 6.4516e-4)
            },
            ["🧊 Thể tích"] = new()
            {
                ("lít", 1), ("ml", 0.001), ("m³", 1000), ("cm³", 0.001),
                ("gallon (US)", 3.78541), ("gallon (UK)", 4.54609),
                ("fl oz", 0.0295735), ("cc", 0.001), ("barrel", 158.987)
            },
            ["⏱️ Thời gian"] = new()
            {
                ("giây", 1), ("phút", 60), ("giờ", 3600), ("ngày", 86400),
                ("tuần", 604800), ("tháng", 2592000), ("năm", 31536000),
                ("ms", 0.001), ("μs", 1e-6), ("ns", 1e-9)
            },
            ["⚡ Năng lượng"] = new()
            {
                ("J", 1), ("kJ", 1000), ("MJ", 1e6),
                ("cal", 4.184), ("kcal", 4184), ("kWh", 3.6e6),
                ("eV", 1.602e-19), ("BTU", 1055.06), ("Wh", 3600)
            },
            ["🚀 Tốc độ"] = new()
            {
                ("m/s", 1), ("km/h", 1.0 / 3.6), ("mph", 0.44704),
                ("knot", 1852.0 / 3600.0), ("ft/s", 0.3048),
                ("Mach", 343), ("c (ánh sáng)", 299792458)
            },
            ["🔩 Áp suất"] = new()
            {
                ("Pa", 1), ("kPa", 1000), ("MPa", 1e6),
                ("atm", 101325), ("bar", 1e5), ("mmHg", 133.322),
                ("psi", 6894.76), ("Torr", 133.322)
            },
            ["💾 Dữ liệu"] = new()
            {
                ("byte", 1), ("KB", 1024), ("MB", 1048576),
                ("GB", 1073741824), ("TB", 1099511627776),
                ("bit", 0.125), ("Kbit", 128), ("Mbit", 131072)
            },
        };

        private static readonly Dictionary<string, string> TypeIcons = new()
        {
            ["📏 Chiều dài"] = "#7B1FA2",
            ["⚖️ Khối lượng"] = "#E65100",
            ["🌡️ Nhiệt độ"] = "#C62828",
            ["📐 Diện tích"] = "#1565C0",
            ["🧊 Thể tích"] = "#00695C",
            ["⏱️ Thời gian"] = "#F57F17",
            ["⚡ Năng lượng"] = "#AD1457",
            ["🚀 Tốc độ"] = "#283593",
            ["🔩 Áp suất"] = "#4E342E",
            ["💾 Dữ liệu"] = "#00838F",
        };

        // ═══════════════════════════════════════════════════════════
        //  UNIT TYPE BUTTONS
        // ═══════════════════════════════════════════════════════════

        private void BuildUnitTypeButtons()
        {
            if (unitTypePanel == null) return;
            unitTypePanel.Children.Clear();

            foreach (var (type, colorHex) in TypeIcons)
            {
                var color = (Color)ColorConverter.ConvertFromString(colorHex);
                bool isActive = type == _activeType;

                var btn = new Border
                {
                    Background = isActive
                        ? new SolidColorBrush(color)
                        : new SolidColorBrush(Color.FromArgb(20, color.R, color.G, color.B)),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 6, 6),
                    Cursor = Cursors.Hand,
                    Tag = type,
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, color.R, color.G, color.B)),
                    BorderThickness = isActive ? new Thickness(2) : new Thickness(1)
                };

                var sp = new StackPanel { Orientation = Orientation.Horizontal };
                sp.Children.Add(new TextBlock
                {
                    Text = type.Split(' ')[0], // emoji
                    FontSize = 14, Margin = new Thickness(0, 0, 4, 0),
                    FontFamily = new FontFamily("Segoe UI")
                });
                sp.Children.Add(new TextBlock
                {
                    Text = string.Join(' ', type.Split(' ').Skip(1)), // name
                    FontSize = 12, FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = isActive ? Brushes.White : new SolidColorBrush(color),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Segoe UI")
                });
                btn.Child = sp;

                string capturedType = type;
                btn.MouseLeftButtonDown += (_, _) =>
                {
                    _activeType = capturedType;
                    BuildUnitTypeButtons();
                    LoadUnitsForType(capturedType);
                };

                // Hover
                btn.MouseEnter += (_, _) =>
                {
                    if (capturedType != _activeType)
                        btn.Background = new SolidColorBrush(Color.FromArgb(40, color.R, color.G, color.B));
                };
                btn.MouseLeave += (_, _) =>
                {
                    if (capturedType != _activeType)
                        btn.Background = new SolidColorBrush(Color.FromArgb(20, color.R, color.G, color.B));
                };

                unitTypePanel.Children.Add(btn);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  LOAD UNITS FOR SELECTED TYPE
        // ═══════════════════════════════════════════════════════════

        private void LoadUnitsForType(string type)
        {
            if (!Units.ContainsKey(type)) return;
            if (cboFromUnit == null || cboToUnit == null) return;

            txtCurrentType.Text = type;
            if (TypeIcons.TryGetValue(type, out var hex))
                txtCurrentType.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(hex)!;

            cboFromUnit.Items.Clear();
            cboToUnit.Items.Clear();
            foreach (var (name, _) in Units[type])
            {
                cboFromUnit.Items.Add(new ComboBoxItem { Content = name });
                cboToUnit.Items.Add(new ComboBoxItem { Content = name });
            }
            if (cboFromUnit.Items.Count > 0) cboFromUnit.SelectedIndex = 0;
            if (cboToUnit.Items.Count > 1) cboToUnit.SelectedIndex = 1;

            BuildQuickRef(type);
        }

        // ═══════════════════════════════════════════════════════════
        //  CONVERSION LOGIC
        // ═══════════════════════════════════════════════════════════

        private void Convert_UnitChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loadingPreset) return;
            DoConversion();
            CommitConversionToHistory();
        }
        private void Convert_Changed(object sender, TextChangedEventArgs e)
        {
            if (_loadingPreset) return;
            DoConversion();
        }

        private void PresetConversion_Click(object sender, RoutedEventArgs e)
        {
            string tag = (sender as Button)?.Tag?.ToString() ?? "";
            if (string.IsNullOrEmpty(tag)) return;

            switch (tag)
            {
                case "LengthGeo":
                    LoadConversionPreset("📏 Chiều dài", "km", "m", "1");
                    break;
                case "MassComm":
                    LoadConversionPreset("⚖️ Khối lượng", "tấn", "kg", "1.5");
                    break;
                case "DataTech":
                    LoadConversionPreset("💾 Dữ liệu", "GB", "MB", "1");
                    break;
            }
        }

        private void LoadConversionPreset(string typeName, string fromUnit, string toUnit, string valueStr)
        {
            _loadingPreset = true;
            try
            {
                _activeType = typeName;
                BuildUnitTypeButtons();
                LoadUnitsForType(typeName);
                
                SetComboBoxItem(cboFromUnit, fromUnit);
                SetComboBoxItem(cboToUnit, toUnit);

                if (txtFromValue != null)
                {
                    txtFromValue.Text = valueStr;
                }
            }
            finally
            {
                _loadingPreset = false;
            }

            DoConversion();
            CommitConversionToHistory();
        }

        private void SetComboBoxItem(ComboBox cbo, string unitName)
        {
            if (cbo == null) return;
            for (int i = 0; i < cbo.Items.Count; i++)
            {
                if (cbo.Items[i] is ComboBoxItem item && item.Content?.ToString() == unitName)
                {
                    cbo.SelectedIndex = i;
                    break;
                }
            }
        }

        private void DoConversion()
        {
            try
            {
                if (!Units.ContainsKey(_activeType)) return;
                if (txtToValue == null || txtConvertFormula == null) return;

                string fromName = (cboFromUnit?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                string toName = (cboToUnit?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

                // Tránh lỗi ném ngoại lệ khi ComboBox bị clear trong LoadUnitsForType
                if (string.IsNullOrEmpty(fromName) || string.IsNullOrEmpty(toName)) return;

                if (!ParsingHelper.TryParseDouble(txtFromValue?.Text, out double value)) 
                { 
                    txtToValue.Text = ""; 
                    txtConvertFormula.Text = "";
                    return; 
                }

                // Kiểm tra giới hạn độ không tuyệt đối cho nhiệt độ
                if (_activeType == "🌡️ Nhiệt độ")
                {
                    bool isBelowAbsoluteZero = false;
                    if (fromName == "K" && value < 0) isBelowAbsoluteZero = true;
                    else if (fromName == "°C" && value < -273.15) isBelowAbsoluteZero = true;
                    else if (fromName == "°F" && value < -459.67) isBelowAbsoluteZero = true;

                    if (isBelowAbsoluteZero)
                    {
                        txtToValue.Text = string.Empty;
                        txtConvertFormula.Text = "Nhiệt độ không thể thấp hơn độ không tuyệt đối!";
                        txtConvertFormula.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD32F2F")); // Màu đỏ báo lỗi
                        return;
                    }
                    else
                    {
                        txtConvertFormula.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF616161")); // Reset màu xám mặc định
                    }
                }
                // Kiểm tra giới hạn âm cho các loại đơn vị không thể có giá trị âm
                else if (value < 0 && 
                         (_activeType == "📏 Chiều dài" || 
                          _activeType == "⚖️ Khối lượng" || 
                          _activeType == "📐 Diện tích" || 
                          _activeType == "🧊 Thể tích" || 
                          _activeType == "⏱️ Thời gian" || 
                          _activeType == "🚀 Tốc độ" || 
                          _activeType == "🔩 Áp suất" || 
                          _activeType == "💾 Dữ liệu"))
                {
                    txtToValue.Text = string.Empty;
                    txtConvertFormula.Text = $"{string.Join(' ', _activeType.Split(' ').Skip(1))} không thể nhận giá trị âm!";
                    txtConvertFormula.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD32F2F")); // Màu đỏ báo lỗi
                    return;
                }
                else
                {
                    txtConvertFormula.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF616161")); // Reset màu xám mặc định
                }

                double result;
                if (_activeType == "🌡️ Nhiệt độ")
                {
                    double celsius = fromName switch
                    {
                        "°F" => (value - 32) * 5.0 / 9,
                        "K" => value - 273.15,
                        _ => value
                    };
                    result = toName switch
                    {
                        "°F" => celsius * 9.0 / 5 + 32,
                        "K" => celsius + 273.15,
                        _ => celsius
                    };
                }
                else
                {
                    var fromFactor = Units[_activeType].First(u => u.Name == fromName).Factor;
                    var toFactor = Units[_activeType].First(u => u.Name == toName).Factor;
                    if (toFactor == 0) return;
                    result = value * fromFactor / toFactor;
                }

                txtToValue.Text = FormatToPedagogicalScientific(result);

                if (_activeType == "🌡️ Nhiệt độ")
                    txtConvertFormula.Text = $"{FormatToPedagogicalScientific(value)} {fromName} = {FormatToPedagogicalScientific(result)} {toName}";
                else
                {
                    var ratio = Units[_activeType].First(u => u.Name == fromName).Factor /
                                Units[_activeType].First(u => u.Name == toName).Factor;
                    txtConvertFormula.Text = $"1 {fromName} = {FormatToPedagogicalScientific(ratio)} {toName}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi quy đổi: {ex.Message}");
            }
        }

        private void CommitConversionToHistory()
        {
            try
            {
                if (string.IsNullOrEmpty(txtToValue.Text)) return;

                string fromName = (cboFromUnit?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                string toName = (cboToUnit?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                if (string.IsNullOrEmpty(fromName) || string.IsNullOrEmpty(toName)) return;

                if (!ParsingHelper.TryParseDouble(txtFromValue?.Text, out double value)) return;

                if (_activeType == "🌡️ Nhiệt độ")
                {
                    bool isBelowAbsoluteZero = false;
                    if (fromName == "K" && value < 0) isBelowAbsoluteZero = true;
                    else if (fromName == "°C" && value < -273.15) isBelowAbsoluteZero = true;
                    else if (fromName == "°F" && value < -459.67) isBelowAbsoluteZero = true;
                    if (isBelowAbsoluteZero) return;
                }
                else if (value < 0 && 
                         (_activeType == "📏 Chiều dài" || 
                          _activeType == "⚖️ Khối lượng" || 
                          _activeType == "📐 Diện tích" || 
                          _activeType == "🧊 Thể tích" || 
                          _activeType == "⏱️ Thời gian" || 
                          _activeType == "🚀 Tốc độ" || 
                          _activeType == "🔩 Áp suất" || 
                          _activeType == "💾 Dữ liệu"))
                {
                    return;
                }

                double result;
                if (_activeType == "🌡️ Nhiệt độ")
                {
                    double celsius = fromName switch
                    {
                        "°F" => (value - 32) * 5.0 / 9,
                        "K" => value - 273.15,
                        _ => value
                    };
                    result = toName switch
                    {
                        "°F" => celsius * 9.0 / 5 + 32,
                        "K" => celsius + 273.15,
                        _ => celsius
                    };
                }
                else
                {
                    var fromFactor = Units[_activeType].First(u => u.Name == fromName).Factor;
                    var toFactor = Units[_activeType].First(u => u.Name == toName).Factor;
                    result = value * fromFactor / toFactor;
                }

                string entry = $"{FormatToPedagogicalScientific(value)} {fromName} = {FormatToPedagogicalScientific(result)} {toName}";

                if (_history.Count > 0 && _history[0] == entry) return; // Tránh trùng lặp

                _history.Insert(0, entry);
                if (_history.Count > 15) _history.RemoveAt(15);
                RenderHistory();
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

        private void SwapUnits_Click(object sender, RoutedEventArgs e)
        {
            if (cboFromUnit == null || cboToUnit == null) return;
            int fromIdx = cboFromUnit.SelectedIndex;
            int toIdx = cboToUnit.SelectedIndex;
            string oldToValue = txtToValue?.Text ?? string.Empty;

            _loadingPreset = true;
            try
            {
                cboFromUnit.SelectedIndex = toIdx;
                cboToUnit.SelectedIndex = fromIdx;
                if (txtFromValue != null && !string.IsNullOrEmpty(oldToValue))
                {
                    txtFromValue.Text = oldToValue;
                }
            }
            finally
            {
                _loadingPreset = false;
            }

            DoConversion();
            CommitConversionToHistory();
        }

        // ═══════════════════════════════════════════════════════════
        //  QUICK REFERENCE TABLE
        // ═══════════════════════════════════════════════════════════

        private void BuildQuickRef(string type)
        {
            if (quickRefPanel == null) return;
            quickRefPanel.Children.Clear();

            if (!Units.ContainsKey(type)) return;
            var units = Units[type];
            if (type == "🌡️ Nhiệt độ")
            {
                // Special: temp conversion table
                var temps = new double[] { -40, -20, 0, 10, 20, 25, 30, 37, 50, 100, 200, 500 };
                var headerGrid = MakeRefRow("°C", "°F", "K", true);
                quickRefPanel.Children.Add(headerGrid);
                foreach (var c in temps)
                {
                    double f = c * 9.0 / 5 + 32;
                    double k = c + 273.15;
                    quickRefPanel.Children.Add(MakeRefRow(FormatVietnamese(c), FormatVietnamese(f), FormatVietnamese(k), false));
                }
                return;
            }

            // General: show base unit conversion for first 10 units
            string baseUnit = units[0].Name;
            var headerRow = MakeRefRow("Đơn vị", $"Thuận (= ? {baseUnit})", $"Nghịch (1 {baseUnit} = ?)", true);
            quickRefPanel.Children.Add(headerRow);
            foreach (var (name, factor) in units.Take(10))
            {
                string factorStr = FormatToPedagogicalScientific(factor);
                string inverseStr = FormatToPedagogicalScientific(1.0 / factor);
                var row = MakeRefRow(name, $"1 {name} = {factorStr} {baseUnit}", $"1 {baseUnit} = {inverseStr} {name}", false);
                quickRefPanel.Children.Add(row);
            }
        }

        private static Border MakeRefRow(string c1, string c2, string c3, bool isHeader)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

            var texts = new[] { c1, c2, c3 };
            for (int i = 0; i < 3; i++)
            {
                var tb = new TextBlock
                {
                    Text = texts[i],
                    FontSize = isHeader ? 11 : 12,
                    FontWeight = isHeader ? FontWeights.Bold : (i == 0 ? FontWeights.SemiBold : FontWeights.Normal),
                    Foreground = isHeader
                        ? Brushes.White
                        : (i == 0 ? new SolidColorBrush(Color.FromRgb(74, 20, 140)) : Brushes.Black),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0),
                    FontFamily = new FontFamily("Segoe UI")
                };
                Grid.SetColumn(tb, i);
                grid.Children.Add(tb);
            }

            return new Border
            {
                Child = grid,
                Background = isHeader
                    ? new SolidColorBrush(Color.FromRgb(74, 20, 140))
                    : Brushes.Transparent,
                Padding = new Thickness(0, 7, 0, 7),
                CornerRadius = isHeader ? new CornerRadius(8, 8, 0, 0) : new CornerRadius(0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(238, 238, 238)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  HISTORY
        // ═══════════════════════════════════════════════════════════

        private void RenderHistory()
        {
            if (historyPanel == null) return;
            historyPanel.Children.Clear();

            if (_history.Count == 0)
            {
                historyPanel.Children.Add(new TextBlock
                {
                    Text = "Chưa có lịch sử quy đổi",
                    FontSize = 11, Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 8, 0, 0)
                });
                return;
            }

            for (int i = 0; i < System.Math.Min(_history.Count, 10); i++)
            {
                var entry = _history[i];
                var row = new Border
                {
                    Background = i % 2 == 0
                        ? new SolidColorBrush(Color.FromRgb(250, 248, 255))
                        : Brushes.White,
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 5, 10, 5),
                    Margin = new Thickness(0, 0, 0, 2),
                    Cursor = Cursors.Hand,
                    ToolTip = "Click để copy"
                };

                var dp = new DockPanel();
                dp.Children.Add(new TextBlock
                {
                    Text = $"#{i + 1}",
                    FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                    Width = 25, VerticalAlignment = VerticalAlignment.Center
                });
                dp.Children.Add(new TextBlock
                {
                    Text = entry,
                    FontSize = 12, FontFamily = new FontFamily("Segoe UI"),
                    Foreground = new SolidColorBrush(Color.FromRgb(74, 20, 140)),
                    VerticalAlignment = VerticalAlignment.Center
                });

                row.Child = dp;
                string capturedEntry = entry;
                row.MouseLeftButtonDown += (_, _) =>
                {
                    try { Clipboard.SetText(capturedEntry); } catch { }
                };

                historyPanel.Children.Add(row);
            }
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
                        Icon = "📍",
                        Title = isVN ? "Bản đồ & Định vị (Chiều dài)" : "Map & Navigation (Length)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_unitconverter_1_{suffix}.png",
                        Description = isVN 
                            ? "Quy đổi giữa hải lý, dặm (mile), kilômét và mét giúp xác định khoảng cách chính xác trên hải đồ hàng hải và thiết bị định vị GPS toàn cầu." 
                            : "Convert between nautical miles, miles, kilometers, and meters to determine precise distances on marine charts and global GPS navigation devices."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⚖",
                        Title = isVN ? "️ Vận tải & Giao nhận (Khối lượng)" : "Logistics & Shipping (Mass)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_unitconverter_2_{suffix}.png",
                        Description = isVN 
                            ? "Quy đổi khối lượng container giữa tấn, tạ, kilôgam và pound giúp tính toán tải trọng hàng hóa chuẩn xác trong vận chuyển hàng hải." 
                            : "Convert container weights between tons, quintals, kilograms, and pounds to calculate precise cargo loads in maritime shipping."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌡",
                        Title = isVN ? "️ Dự báo thời tiết (Nhiệt độ)" : "Weather Forecasting (Temperature)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_unitconverter_3_{suffix}.png",
                        Description = isVN 
                            ? "Quy đổi nhiệt độ không khí giữa độ Celsius (°C), Fahrenheit (°F) và Kelvin (K) trong các bản tin thời tiết quốc tế và nghiên cứu khoa học." 
                            : "Convert air temperatures between Celsius (°C), Fahrenheit (°F), and Kelvin (K) for international weather reports and scientific research."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📐",
                        Title = isVN ? "Quy hoạch đất đai (Diện tích)" : "Land Planning (Area)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_unitconverter_4_{suffix}.png",
                        Description = isVN 
                            ? "Chuyển đổi diện tích đất nông nghiệp từ sào, mẫu cổ truyền sang mét vuông (m²) hoặc héc-ta (ha) phục vụ đo đạc địa chính và sản xuất." 
                            : "Convert agricultural land area from traditional units (sao, mau) to square meters (m²) or hectares (ha) for cadastre measurement and production."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧪",
                        Title = isVN ? "Định lượng hóa học (Thể tích)" : "Chemical Quantities (Volume)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_unitconverter_5_{suffix}.png",
                        Description = isVN 
                            ? "Quy đổi chính xác thể tích chất lỏng giữa lít (L), mililít (mL), cm³ (cc) và gallon phục vụ các công thức hóa học và pha chế dung dịch." 
                            : "Convert liquid volume accurately between liters (L), milliliters (mL), cubic centimeters (cc), and gallons for chemical formulas and solution preparation."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💾",
                        Title = isVN ? "Quản lý máy chủ (Dữ liệu)" : "Server Management (Data)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_unitconverter_6_{suffix}.png",
                        Description = isVN 
                            ? "Chuyển đổi giữa byte, kilobyte (KB), megabyte (MB), gigabyte (GB) và terabyte (TB) để quản lý dung lượng ổ đĩa và hạ tầng cloud máy chủ." 
                            : "Convert between bytes, kilobytes (KB), megabytes (MB), gigabytes (GB), and terabytes (TB) to manage disk space and cloud server infrastructure."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💱",
                        Title = isVN ? "Tải lượng giao dịch quốc tế" : "International Trade Valuation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_unitconverter_7_{suffix}.png",
                        Description = isVN 
                            ? "Quy đổi đơn vị tiền tệ và trọng lượng nông sản xuất khẩu để đồng bộ hóa hóa đơn thanh toán hải quan quốc tế." 
                            : "Convert currencies and agricultural export weights to synchronize international customs billing documents."
                    },
                    new PracticalAppItem
                    {
                        Icon = "✈️",
                        Title = isVN ? "Đổi đơn vị trong hàng không" : "Aviation Unit Conversions",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_unitconverter_8_{suffix}.png",
                        Description = isVN 
                            ? "Quy đổi nhanh đơn vị độ cao bay từ feet sang mét và vận tốc từ knots sang km/h để kiểm soát không lưu an toàn." 
                            : "Convert flight altitudes from feet to meters and speeds from knots to km/h to maintain safe air traffic control."
                    }};

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for UnitConverterTool: {Err}", ex.Message);
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            _history.Clear();
            RenderHistory();
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewConverter == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewConverter.Visibility = Visibility.Collapsed;
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
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
