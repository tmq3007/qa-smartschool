using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class NumberBaseTool : BaseToolControl
    {
        private TextBox? _txtDec, _txtBin, _txtOct, _txtHex;
        private bool _isUpdating;

        public NumberBaseTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextConvert != null) menuTextConvert.Text = isVN ? "Chuyển đổi cơ số" : "Base Converter";
                if (menuTextCalc != null) menuTextCalc.Text = isVN ? "Phép tính nhị phân" : "Binary Math";
                if (menuTextAscii != null) menuTextAscii.Text = isVN ? "Bảng ASCII" : "ASCII Table";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                BuildConverter();
                
                // Attach TouchNumPad
                if (txtBinA != null) TouchNumPad.Attach(txtBinA, step: 1, allowDecimal: false, allowNegative: false);
                if (txtBinB != null) TouchNumPad.Attach(txtBinB, step: 1, allowDecimal: false, allowNegative: false);
                
                // Physical keyboard filtering for binary math inputs
                if (txtBinA != null) txtBinA.PreviewTextInput += (s, ev) => ev.Handled = !IsBinaryString(ev.Text);
                if (txtBinB != null) txtBinB.PreviewTextInput += (s, ev) => ev.Handled = !IsBinaryString(ev.Text);
                if (txtBinA != null) txtBinA.PreviewKeyDown += (s, ev) => { if (ev.Key == Key.Space) ev.Handled = true; };
                if (txtBinB != null) txtBinB.PreviewKeyDown += (s, ev) => { if (ev.Key == Key.Space) ev.Handled = true; };
                
                if (txtBinA != null)
                {
                    DataObject.AddPastingHandler(txtBinA, (s, ev) =>
                    {
                        if (ev.DataObject.GetDataPresent(DataFormats.Text))
                        {
                            string text = (string)ev.DataObject.GetData(DataFormats.Text);
                            if (!IsBinaryString(text)) ev.CancelCommand();
                        }
                        else ev.CancelCommand();
                    });
                }
                
                if (txtBinB != null)
                {
                    DataObject.AddPastingHandler(txtBinB, (s, ev) =>
                    {
                        if (ev.DataObject.GetDataPresent(DataFormats.Text))
                        {
                            string text = (string)ev.DataObject.GetData(DataFormats.Text);
                            if (!IsBinaryString(text)) ev.CancelCommand();
                        }
                        else ev.CancelCommand();
                    });
                }

                BuildAsciiTable();
                LoadPracticalApps();
            };
        }

        // Helper to load localized string
        private string GetStr(string key, string fallback)
        {
            try
            {
                return Application.Current.TryFindResource(key) as string ?? fallback;
            }
            catch
            {
                return fallback;
            }
        }

        // Input validation helpers
        private static bool IsBinaryString(string text)
        {
            foreach (char c in text)
            {
                if (c != '0' && c != '1') return false;
            }
            return true;
        }

        private static bool IsOctalString(string text)
        {
            foreach (char c in text)
            {
                if (c < '0' || c > '7') return false;
            }
            return true;
        }

        private static bool IsDecimalString(string text)
        {
            foreach (char c in text)
            {
                if (c < '0' || c > '9') return false;
            }
            return true;
        }

        private static bool IsHexadecimalString(string text)
        {
            foreach (char c in text)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    return false;
            }
            return true;
        }

        // ═══════════════════════════════════════════════════════════
        //  CONVERTER — 4 hệ số song song
        // ═══════════════════════════════════════════════════════════

        private void BuildConverter()
        {
            if (converterPanel == null) return;
            converterPanel.Children.Clear();

            // Localized base labels
            var bases = new (string Key, string Fallback, string Icon, string Color, int Base)[]
            {
                ("Base_DecLabel", "Thập phân (Base 10)", "🔟", "#E65100", 10),
                ("Base_BinLabel", "Nhị phân (Base 2)", "💾", "#1565C0", 2),
                ("Base_OctLabel", "Bát phân (Base 8)", "📊", "#2E7D32", 8),
                ("Base_HexLabel", "Thập lục phân (Base 16)", "🔤", "#6A1B9A", 16),
            };

            foreach (var (key, fallback, icon, color, numBase) in bases)
            {
                var accentColor = (Color)ColorConverter.ConvertFromString(color);
                var row = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(16, 12, 16, 12),
                    Margin = new Thickness(0, 0, 0, 10),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, accentColor.R, accentColor.G, accentColor.B)),
                    BorderThickness = new Thickness(0, 0, 4, 0)
                };

                var dp = new DockPanel();
                dp.Children.Add(new TextBlock
                {
                    Text = $"{icon} {GetStr(key, fallback)}",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(accentColor),
                    Width = 240,
                    VerticalAlignment = VerticalAlignment.Center
                });

                var tb = new TextBox
                {
                    FontSize = 22,
                    Padding = new Thickness(10, 6, 10, 6),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontWeight = FontWeights.Bold,
                    BorderBrush = new SolidColorBrush(Color.FromArgb(80, accentColor.R, accentColor.G, accentColor.B)),
                    BorderThickness = new Thickness(0, 0, 0, 2),
                    Background = Brushes.Transparent,
                    Tag = numBase,
                    MaxLength = numBase switch
                    {
                        2 => 64,
                        8 => 22,
                        10 => 20,
                        16 => 16,
                        _ => 0
                    }
                };

                // Add input filters to textbox
                tb.PreviewTextInput += (s, ev) =>
                {
                    int b = (int)((TextBox)s).Tag;
                    bool ok = b switch
                    {
                        2 => IsBinaryString(ev.Text),
                        8 => IsOctalString(ev.Text),
                        10 => IsDecimalString(ev.Text),
                        16 => IsHexadecimalString(ev.Text),
                        _ => true
                    };
                    ev.Handled = !ok;
                };

                tb.PreviewKeyDown += (s, ev) =>
                {
                    if (ev.Key == Key.Space) ev.Handled = true;
                };

                DataObject.AddPastingHandler(tb, (s, ev) =>
                {
                    if (ev.DataObject.GetDataPresent(DataFormats.Text))
                    {
                        string text = (string)ev.DataObject.GetData(DataFormats.Text);
                        int b = (int)((TextBox)s).Tag;
                        bool ok = b switch
                        {
                            2 => IsBinaryString(text),
                            8 => IsOctalString(text),
                            10 => IsDecimalString(text),
                            16 => IsHexadecimalString(text),
                            _ => true
                        };
                        if (!ok) ev.CancelCommand();
                    }
                    else ev.CancelCommand();
                });

                switch (numBase)
                {
                    case 10: _txtDec = tb; tb.Text = "255"; break;
                    case 2: _txtBin = tb; break;
                    case 8: _txtOct = tb; break;
                    case 16: _txtHex = tb; break;
                }

                tb.TextChanged += ConverterBox_Changed;
                tb.GotFocus += (_, _) => tb.Tag = numBase; // mark active

                dp.Children.Add(tb);
                row.Child = dp;
                converterPanel.Children.Add(row);
            }

            // Initial conversion
            UpdateFromDecimal(255);

            // TouchNumPad for decimal input on interactive display
            if (_txtDec != null) TouchNumPad.Attach(_txtDec, step: 1, min: 0, allowDecimal: false);

            // Localized Copy buttons
            var copyPanel = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
            var copyBases = new (string BaseName, Func<string> GetValue)[]
            {
                ("Dec", () => _txtDec?.Text ?? ""),
                ("Bin", () => _txtBin?.Text ?? ""),
                ("Oct", () => _txtOct?.Text ?? ""),
                ("Hex", () => _txtHex?.Text ?? ""),
            };
            foreach (var (baseName, getValue) in copyBases)
            {
                var btn = new Button
                {
                    Content = $"📋 {GetStr("Base_Copy", "Copy")} {baseName}",
                    Padding = new Thickness(14, 8, 14, 8),
                    Margin = new Thickness(0, 0, 8, 0),
                    Background = new SolidColorBrush(Color.FromRgb(255, 243, 224)),
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold
                };
                var captured = getValue;
                btn.Click += (_, _) =>
                {
                    try { Clipboard.SetText(captured()); } catch { }
                };
                copyPanel.Children.Add(btn);
            }
            converterPanel.Children.Add(copyPanel);
        }

        private void ConverterBox_Changed(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;
            if (sender is not TextBox tb) return;

            int fromBase = tb.Tag is int b ? b : 10;
            string text = tb.Text.Trim();
            
            if (string.IsNullOrEmpty(text))
            {
                _isUpdating = true;
                if (tb != _txtDec && _txtDec != null) _txtDec.Text = "";
                if (tb != _txtBin && _txtBin != null) _txtBin.Text = "";
                if (tb != _txtOct && _txtOct != null) _txtOct.Text = "";
                if (tb != _txtHex && _txtHex != null) _txtHex.Text = "";
                _isUpdating = false;
                if (txtConvertWarning != null) txtConvertWarning.Text = "";
                return;
            }

            try
            {
                ulong value = fromBase == 10
                    ? ulong.Parse(text)
                    : Convert.ToUInt64(text, fromBase);

                // Clear any warnings on successful conversion
                if (txtConvertWarning != null) txtConvertWarning.Text = "";

                _isUpdating = true;
                if (tb != _txtDec && _txtDec != null) _txtDec.Text = value.ToString();
                if (tb != _txtBin && _txtBin != null) _txtBin.Text = ToUnsignedBaseString(value, 2);
                if (tb != _txtOct && _txtOct != null) _txtOct.Text = ToUnsignedBaseString(value, 8);
                if (tb != _txtHex && _txtHex != null) _txtHex.Text = value.ToString("X");
                _isUpdating = false;
            }
            catch (OverflowException)
            {
                _isUpdating = false;
                if (txtConvertWarning != null)
                {
                    txtConvertWarning.Text = GetStr("Base_ErrOverflow", "⚠️ Giá trị quá lớn (vượt quá 64-bit)");
                }
            }
            catch (FormatException)
            {
                _isUpdating = false;
                if (txtConvertWarning != null)
                {
                    txtConvertWarning.Text = GetStr("Base_ErrFormat", "⚠️ Định dạng số không hợp lệ");
                }
            }
            catch (Exception ex)
            {
                _isUpdating = false;
                if (txtConvertWarning != null)
                {
                    txtConvertWarning.Text = $"⚠️ Lỗi: {ex.Message}";
                }
            }
        }

        private void UpdateFromDecimal(ulong value)
        {
            _isUpdating = true;
            if (_txtDec != null) _txtDec.Text = value.ToString();
            if (_txtBin != null) _txtBin.Text = ToUnsignedBaseString(value, 2);
            if (_txtOct != null) _txtOct.Text = ToUnsignedBaseString(value, 8);
            if (_txtHex != null) _txtHex.Text = value.ToString("X");
            _isUpdating = false;
        }

        private static string ToUnsignedBaseString(ulong value, int toBase)
        {
            if (value == 0) return "0";
            if (toBase == 10) return value.ToString();
            if (toBase == 16) return value.ToString("X");
            
            char[] digits = new char[64];
            int index = 64;
            while (value > 0)
            {
                ulong remainder = value % (ulong)toBase;
                digits[--index] = remainder < 10 ? (char)('0' + remainder) : (char)('A' + (remainder - 10));
                value /= (ulong)toBase;
            }
            return new string(digits, index, 64 - index);
        }

        // ═══════════════════════════════════════════════════════════
        //  BINARY ARITHMETIC
        // ═══════════════════════════════════════════════════════════

        private void BinCalc_Changed(object sender, object e)
        {
            if (binCalcResultPanel == null) return;
            binCalcResultPanel.Children.Clear();

            if (_isUpdating) return;

            int widthIdx = cboBinWidth?.SelectedIndex ?? 0;
            int bitWidth = widthIdx switch
            {
                1 => 8,
                2 => 16,
                3 => 32,
                _ => 64
            };

            // Dynamically adjust and truncate if necessary
            if (txtBinA != null)
            {
                txtBinA.MaxLength = bitWidth;
                if (txtBinA.Text.Length > bitWidth)
                {
                    _isUpdating = true;
                    txtBinA.Text = txtBinA.Text.Substring(txtBinA.Text.Length - bitWidth);
                    _isUpdating = false;
                }
            }
            if (txtBinB != null)
            {
                txtBinB.MaxLength = bitWidth;
                if (txtBinB.Text.Length > bitWidth)
                {
                    _isUpdating = true;
                    txtBinB.Text = txtBinB.Text.Substring(txtBinB.Text.Length - bitWidth);
                    _isUpdating = false;
                }
            }

            string aStr = txtBinA?.Text?.Trim() ?? "";
            string bStr = txtBinB?.Text?.Trim() ?? "";
            
            if (string.IsNullOrEmpty(aStr) || string.IsNullOrEmpty(bStr)) return;

            try
            {
                ulong a = Convert.ToUInt64(aStr, 2);
                ulong b = Convert.ToUInt64(bStr, 2);

                ulong mask = bitWidth == 64 ? ulong.MaxValue : (1UL << bitWidth) - 1;
                a &= mask;
                b &= mask;

                int opIdx = cboBinOp?.SelectedIndex ?? 0;
                string opName;
                ulong result;

                switch (opIdx)
                {
                    case 1: result = a - b; opName = "-"; break;
                    case 2: result = a & b; opName = "AND"; break;
                    case 3: result = a | b; opName = "OR"; break;
                    case 4: result = a ^ b; opName = "XOR"; break;
                    default: result = a + b; opName = "+"; break;
                }

                result &= mask;

                long signedA = (bitWidth < 64 && (a & (1UL << (bitWidth - 1))) != 0) ? (long)a - (long)(1UL << bitWidth) : (long)a;
                long signedB = (bitWidth < 64 && (b & (1UL << (bitWidth - 1))) != 0) ? (long)b - (long)(1UL << bitWidth) : (long)b;
                long signedResult = (bitWidth < 64 && (result & (1UL << (bitWidth - 1))) != 0) ? (long)result - (long)(1UL << bitWidth) : (long)result;

                if (bitWidth == 64)
                {
                    signedA = (long)a;
                    signedB = (long)b;
                    signedResult = (long)result;
                }

                string formatBinary(ulong val)
                {
                    string raw = ToUnsignedBaseString(val, 2);
                    if (bitWidth == 64) return raw;
                    return raw.PadLeft(bitWidth, '0');
                }

                string formatHex(ulong val)
                {
                    string raw = val.ToString("X");
                    if (bitWidth == 64) return raw;
                    return raw.PadLeft(bitWidth / 4, '0');
                }

                string lblDec = GetStr("Base_ResultDec", "Thập phân");
                string lblBin = GetStr("Base_ResultBin", "Nhị phân");
                string lblHex = GetStr("Base_ResultHex", "Hex");

                if (bitWidth < 64)
                {
                    AddCalcResult($"{lblDec}: {signedA} {opName} {signedB} = {signedResult}");
                }
                else
                {
                    if (a > (ulong)long.MaxValue || b > (ulong)long.MaxValue || result > (ulong)long.MaxValue)
                    {
                        AddCalcResult($"{lblDec} (Unsigned): {a} {opName} {b} = {result}");
                        AddCalcResult($"{lblDec} (Signed): {(long)a} {opName} {(long)b} = {(long)result}");
                    }
                    else
                    {
                        AddCalcResult($"{lblDec}: {signedA} {opName} {signedB} = {signedResult}");
                    }
                }

                AddCalcResult($"{lblBin}: {formatBinary(a)} {opName} {formatBinary(b)} = {formatBinary(result)}");
                AddCalcResult($"{lblHex}: {formatHex(a)} {opName} {formatHex(b)} = {formatHex(result)}");
            }
            catch (OverflowException)
            {
                binCalcResultPanel.Children.Clear();
                AddCalcResult(GetStr("Base_ErrCalcOverflow", "⚠️ Kết quả vượt quá giới hạn 64-bit"));
            }
            catch (FormatException)
            {
                binCalcResultPanel.Children.Clear();
                AddCalcResult(GetStr("Base_ErrInvalidBinary", "⚠️ Nhập chuỗi nhị phân hợp lệ (0 và 1)"));
            }
            catch (Exception ex)
            {
                binCalcResultPanel.Children.Clear();
                AddCalcResult($"⚠️ Lỗi: {ex.Message}");
            }
        }

        private void AddCalcResult(string text)
        {
            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 0, 6)
            };
            border.Child = new TextBlock
            {
                Text = text,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0))
            };
            binCalcResultPanel.Children.Add(border);
        }

        // ═══════════════════════════════════════════════════════════
        //  ASCII TABLE
        // ═══════════════════════════════════════════════════════════

        private int _selectedAsciiCode = 32;
        private Border? _activeAsciiBorder = null;

        private static string GetAsciiDisplay(int code)
        {
            if (code == 32) return "SP"; // Space
            if (code == 127) return "DEL";
            if (code >= 32 && code < 127) return ((char)code).ToString();
            
            string[] ctrlNames = {
                "NUL", "SOH", "STX", "ETX", "EOT", "ENQ", "ACK", "BEL",
                "BS", "HT", "LF", "VT", "FF", "CR", "SO", "SI",
                "DLE", "DC1", "DC2", "DC3", "DC4", "NAK", "SYN", "ETB",
                "CAN", "EM", "SUB", "ESC", "FS", "GS", "RS", "US"
            };
            return ctrlNames[code];
        }

        private static string GetAsciiCategoryDescription(int code)
        {
            if (code >= 48 && code <= 57) return "Ký tự số (Digit)";
            if ((code >= 65 && code <= 90) || (code >= 97 && code <= 122)) return "Ký tự chữ cái (Letter)";
            if (code == 32) return "Ký tự khoảng trắng (Space)";
            if (code < 32 || code == 127) return "Ký tự điều khiển (Control character)";
            return "Ký tự đặc biệt / Phép toán (Special character / Symbol)";
        }

        private void SelectAsciiChar(int code, Border cellBorder)
        {
            _selectedAsciiCode = code;

            // Reset previous highlight
            if (_activeAsciiBorder != null)
            {
                _activeAsciiBorder.BorderThickness = new Thickness(0);
                _activeAsciiBorder.BorderBrush = Brushes.Transparent;
            }

            // Apply glow highlight to the new selected cell
            cellBorder.BorderThickness = new Thickness(2);
            cellBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(251, 140, 0)); // BrandAccent
            _activeAsciiBorder = cellBorder;

            // Update right detail panel labels
            if (lblSelectedChar != null) lblSelectedChar.Text = GetAsciiDisplay(code);
            if (lblSelectedDec != null) lblSelectedDec.Text = code.ToString();
            if (lblSelectedHex != null) lblSelectedHex.Text = $"0x{code:X2}";
            if (lblSelectedBin != null) lblSelectedBin.Text = Convert.ToString(code, 2).PadLeft(8, '0');
            if (lblSelectedType != null) lblSelectedType.Text = GetAsciiCategoryDescription(code);
        }

        private void CopySelectedChar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                char cRaw = (char)_selectedAsciiCode;
                Clipboard.SetText(cRaw.ToString());
                string display = GetAsciiDisplay(_selectedAsciiCode);
                if (_selectedAsciiCode < 32 || _selectedAsciiCode == 127)
                {
                    ShowToast($"Đã sao chép ký tự điều khiển [{display}] (Mã {_selectedAsciiCode})!");
                }
                else
                {
                    ShowToast($"Đã sao chép ký tự '{display}' vào bộ nhớ tạm!");
                }
            }
            catch
            {
                ShowToast("⚠️ Không thể truy cập bộ nhớ tạm!");
            }
        }

        private void ShowToast(string message)
        {
            if (toastBorder == null || txtToastMessage == null) return;
            
            txtToastMessage.Text = message;
            toastBorder.Visibility = Visibility.Visible;
            toastBorder.Opacity = 0;
            
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            fadeIn.Completed += (s, ev) =>
            {
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(1500)
                };
                timer.Tick += (st, ev2) =>
                {
                    timer.Stop();
                    var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
                    fadeOut.Completed += (sf, ev3) =>
                    {
                        toastBorder.Visibility = Visibility.Collapsed;
                    };
                    toastBorder.BeginAnimation(OpacityProperty, fadeOut);
                };
                timer.Start();
            };
            toastBorder.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void BuildAsciiTable()
        {
            if (asciiPanel == null) return;
            asciiPanel.Children.Clear();

            _activeAsciiBorder = null;

            string tooltipFormat = GetStr("Base_AsciiToolTip", "Ký tự: {0} | Thập phân: {1} | Hex: 0x{2:X2} | Nhị phân: {3}");

            for (int i = 0; i < 128; i++)
            {
                int code = i;
                string display = GetAsciiDisplay(code);
                
                // Color coding background based on pedagogical categorization
                Brush bgBrush;
                if (code < 32 || code == 127)
                    bgBrush = new SolidColorBrush(Color.FromRgb(255, 253, 231)); // Control characters: Light yellow
                else if (code >= 48 && code <= 57)
                    bgBrush = new SolidColorBrush(Color.FromRgb(232, 245, 233)); // Digits: Light green
                else if ((code >= 65 && code <= 90) || (code >= 97 && code <= 122))
                    bgBrush = new SolidColorBrush(Color.FromRgb(227, 242, 253)); // Letters: Light blue
                else
                    bgBrush = Brushes.White; // Symbols: White

                var cell = new Border
                {
                    Width = 84,
                    Margin = new Thickness(3),
                    Background = bgBrush,
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(6, 6, 6, 6),
                    Cursor = Cursors.Hand,
                    ToolTip = string.Format(tooltipFormat, display, code, code, Convert.ToString(code, 2).PadLeft(8, '0')),
                    Tag = code // Tag for search filtering
                };

                var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                sp.Children.Add(new TextBlock
                {
                    Text = display,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
                });
                sp.Children.Add(new TextBlock
                {
                    Text = $"{code} | 0x{code:X2}",
                    FontSize = 13,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                cell.Child = sp;

                cell.MouseLeftButtonDown += (_, _) =>
                {
                    SelectAsciiChar(code, cell);
                    try
                    {
                        char cRaw = (char)code;
                        Clipboard.SetText(cRaw.ToString());
                        if (code < 32 || code == 127)
                        {
                            ShowToast($"Đã sao chép ký tự điều khiển [{display}] (Mã {code})!");
                        }
                        else
                        {
                            ShowToast($"Đã sao chép ký tự '{display}' vào bộ nhớ tạm!");
                        }
                    }
                    catch
                    {
                        ShowToast("⚠️ Không thể truy cập bộ nhớ tạm!");
                    }
                };

                // Add to table
                asciiPanel.Children.Add(cell);

                // Default selection is SP (space, code 32)
                if (code == 32)
                {
                    SelectAsciiChar(32, cell);
                }
            }
        }

        private void SearchAscii_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtSearchAscii == null || asciiPanel == null) return;
            
            string query = txtSearchAscii.Text.Trim().ToLower();
            
            foreach (UIElement child in asciiPanel.Children)
            {
                if (child is not Border cell || cell.Tag is not int code) continue;
                
                string display = GetAsciiDisplay(code).ToLower();
                string decStr = code.ToString();
                string hexStr = code.ToString("X").ToLower();
                string hexPrefixStr = "0x" + hexStr;
                
                bool isMatch = string.IsNullOrEmpty(query) ||
                               display == query ||
                               decStr == query ||
                               hexStr == query ||
                               hexPrefixStr == query;
                               
                cell.Visibility = isMatch ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private static string GetSpeakText(int code)
        {
            if (code == 32) return "Space";
            if (code == 127) return "Delete";
            if (code < 32)
            {
                string[] fullCtrlNames = {
                    "Null", "Start of heading", "Start of text", "End of text", "End of transmission", "Enquiry", "Acknowledge", "Bell",
                    "Backspace", "Horizontal tab", "Line feed", "Vertical tab", "Form feed", "Carriage return", "Shift out", "Shift in",
                    "Data link escape", "Device control one", "Device control two", "Device control three", "Device control four", "Negative acknowledge", "Synchronous idle", "End of transmission block",
                    "Cancel", "End of medium", "Substitute", "Escape", "File separator", "Group separator", "Record separator", "Unit separator"
                };
                return fullCtrlNames[code] + " control character";
            }
            
            char c = (char)code;
            if (char.IsUpper(c)) return "Capital " + c.ToString();
            if (char.IsLower(c)) return "Small " + c.ToString();
            if (char.IsDigit(c)) return c.ToString();
            
            return code switch
            {
                33 => "Exclamation mark", 34 => "Double quote", 35 => "Hash", 36 => "Dollar sign", 37 => "Percent", 38 => "Ampersand", 39 => "Single quote",
                40 => "Open parenthesis", 41 => "Close parenthesis", 42 => "Asterisk", 43 => "Plus", 44 => "Comma", 45 => "Minus", 46 => "Dot", 47 => "Slash",
                58 => "Colon", 59 => "Semicolon", 60 => "Less than", 61 => "Equals", 62 => "Greater than", 63 => "Question mark", 64 => "At sign",
                91 => "Open bracket", 92 => "Backslash", 93 => "Close bracket", 94 => "Caret", 95 => "Underscore", 96 => "Backquote",
                123 => "Open brace", 124 => "Vertical bar", 125 => "Close brace", 126 => "Tilde",
                _ => "Symbol"
            };
        }

        private void SpeakAscii_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Type synthType = Type.GetTypeFromProgID("SAPI.SpVoice");
                if (synthType != null)
                {
                    object synth = Activator.CreateInstance(synthType);
                    string textToSpeak = GetSpeakText(_selectedAsciiCode);
                    
                    // Call Speak asynchronously using late binding (flag 1 = SVSFlagsAsync)
                    synthType.InvokeMember("Speak", 
                        System.Reflection.BindingFlags.InvokeMethod, 
                        null, synth, new object[] { textToSpeak, 1 });
                }
            }
            catch (Exception ex)
            {
                ShowToast("⚠️ Không thể phát âm thanh!");
                Serilog.Log.Warning("Speech API error: {Err}", ex.Message);
            }
        }

        private void ResetAll_Click(object sender, MouseButtonEventArgs e)
        {
            _isUpdating = true;
            if (_txtDec != null) _txtDec.Text = "";
            if (_txtBin != null) _txtBin.Text = "";
            if (_txtOct != null) _txtOct.Text = "";
            if (_txtHex != null) _txtHex.Text = "";
            _isUpdating = false;
            
            if (txtBinA != null) txtBinA.Text = "";
            if (txtBinB != null) txtBinB.Text = "";
            if (txtConvertWarning != null) txtConvertWarning.Text = "";
            
            binCalcResultPanel?.Children.Clear();
            
            // Note: We do NOT call UpdateFromDecimal(255) to ensure all inputs remain empty.
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
                        Icon = "💻",
                        Title = isVN ? "Biểu diễn dữ liệu nhị phân (Binary)" : "Computer Binary Representation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_numberbase_1_{suffix}.png",
                        Description = isVN 
                            ? "Máy tính sử dụng hệ nhị phân (chỉ gồm 0 và 1) để biểu diễn mọi thông tin như văn bản, hình ảnh, âm thanh thông qua các cổng logic bật/tắt (transistor). Lập trình viên chuyển đổi hệ cơ số để đọc hiểu các mã nhị phân ở mức phần cứng cấp thấp." 
                            : "Computers use binary system (0 and 1 only) to represent all information like text, images, and audio through physical logic gates (transistors). Programmers convert bases to interpret binary data at a hardware level."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎨",
                        Title = isVN ? "Mã màu sắc Hex và Địa chỉ IP/MAC" : "Hex Colors & Network IP/MAC",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_numberbase_2_{suffix}.png",
                        Description = isVN 
                            ? "Hệ thập lục phân (Hex, cơ số 16) được sử dụng rộng rãi để rút gọn các chuỗi nhị phân dài. Ví dụ: mã màu CSS #FF5733 đại diện cho tỷ lệ Red-Green-Blue, hay địa chỉ IPv6 và MAC của card mạng máy tính được viết bằng mã Hex để quản lý dễ dàng." 
                            : "Hexadecimal (Hex, base 16) is widely used to shorten long binary strings. E.g., CSS color code #FF5733 represents Red-Green-Blue channels, and IPv6/MAC addresses are written in Hex for easy network administration."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔤",
                        Title = isVN ? "Bảng mã ký tự ASCII và UTF-8" : "ASCII & UTF-8 Character Encoding",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_numberbase_3_{suffix}.png",
                        Description = isVN 
                            ? "Các ký tự chữ viết trên bàn phím được máy tính lưu dưới dạng số nguyên (cơ số 10) theo tiêu chuẩn mã hóa ASCII hoặc UTF-8. Ví dụ: Chữ 'A' có mã thập phân là 65, hệ nhị phân là 01000001, và hệ Hex là 41. Việc chuyển đổi này giúp máy tính hiển thị đúng văn bản." 
                            : "Keyboard characters are stored as integers (base 10) in computers following ASCII or UTF-8 standards. E.g., character 'A' has decimal code 65, binary code 01000001, and Hex code 41. These base conversions allow text rendering."
                    }
                };

                if (practicalAppViewer != null) practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for NumberBaseTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewConvert == null || viewCalc == null || viewAscii == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewConvert.Visibility = Visibility.Collapsed;
            viewCalc.Visibility = Visibility.Collapsed;
            viewAscii.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewConvert.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewCalc.Visibility = Visibility.Visible;
                    break;
                case 3:
                    viewAscii.Visibility = Visibility.Visible;
                    break;
                case 4:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
