using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace QASmartClass.LearningTools
{
    /// <summary>
    /// ══════════════════════════════════════════════════════════════════
    ///  QA SMART CLASS — UI FACTORY
    ///  Tạo các component UI chuẩn hóa cho LearningTools.
    ///  
    ///  ⚠️ LUÔN dùng các method này thay vì tạo Border/TextBlock thủ công.
    ///  Đảm bảo: đúng font, đúng size, đúng spacing, copy-to-clipboard.
    /// ══════════════════════════════════════════════════════════════════
    /// </summary>
    public static class UI
    {
        // ═══════════════════════════════════════════════════════════
        //  📝 BASIC TEXT ELEMENTS
        // ═══════════════════════════════════════════════════════════

        /// <summary>Tạo TextBlock tiêu đề chuẩn</summary>
        public static TextBlock Title(string text, double size = DS.FontTitle, Color? color = null)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = size,
                FontFamily = DS.FontPrimary,
                FontWeight = FontWeights.Bold,
                Foreground = DS.Brush(color ?? DS.TextPrimary),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };
        }

        /// <summary>Tạo TextBlock nội dung chuẩn</summary>
        public static TextBlock Text(string text, double size = DS.FontLabel, Color? color = null)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = size,
                FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(color ?? DS.TextSecondary),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  📊 RESULT ROW — Dòng kết quả click-to-copy
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Tạo dòng kết quả chuẩn với click-to-copy.
        /// Dùng trong mọi tool để hiển thị output.
        /// </summary>
        /// <param name="text">Nội dung (VD: "📏 x = 5.0")</param>
        /// <param name="color">Màu foreground (dùng DS.ResultPrimary, DS.ResultSuccess...)</param>
        /// <param name="panel">StackPanel chứa kết quả (resultPanel)</param>
        public static Border ResultRow(string text, Color color, StackPanel? panel = null, double fontSize = DS.FontResult)
        {
            var border = new Border
            {
                Background = DS.LightBg(color),
                CornerRadius = new CornerRadius(DS.RadiusChip),
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 0, DS.MarginResult),
                Cursor = Cursors.Hand,
                ToolTip = "📋 Nhấn để copy"
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            UIElement contentElement;
            bool isLatex = text != null && (text.Contains("$") || text.Contains("\\") || text.Contains("_") || text.Contains("^") || text.Contains("{") || text.Contains("}"));

            if (isLatex)
            {
                contentElement = RenderMixedContent(text!, fontSize, DS.Brush(color));
            }
            else if (text != null && text.Contains("**"))
            {
                var tb = new TextBlock
                {
                    FontSize = fontSize,
                    FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(color),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };
                ParseRichText(text, tb);
                contentElement = tb;
            }
            else
            {
                contentElement = new TextBlock
                {
                    Text = text,
                    FontSize = fontSize,
                    FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(color),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
            Grid.SetColumn(contentElement, 0);
            grid.Children.Add(contentElement);

            var copyIcon = new TextBlock
            {
                Text = "📋",
                FontSize = fontSize,
                Foreground = DS.BrushAlpha(color, 150),
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "📋 Nhấn để copy"
            };
            border.MouseEnter += (_, _) => copyIcon.Foreground = DS.Brush(color);
            border.MouseLeave += (_, _) => copyIcon.Foreground = DS.BrushAlpha(color, 150);

            Grid.SetColumn(copyIcon, 1);
            grid.Children.Add(copyIcon);

            border.Child = grid;
            string ct = text?.Replace("**", "") ?? "";
            border.MouseLeftButtonDown += (_, _) => { try { Clipboard.SetText(ct); } catch { } };
            panel?.Children.Add(border);
            return border;
        }

        /// <summary>Tạo UI hiển thị văn bản hỗn hợp chứa công thức LaTeX</summary>
        public static UIElement RenderMixedContent(string text, double fontSize = 14, Brush? foreground = null)
        {
            if (string.IsNullOrEmpty(text)) return new TextBlock();

            // 1. Check if it contains $ for mixed content
            if (text.Contains("$"))
            {
                var wrapPanel = new WrapPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var parts = System.Text.RegularExpressions.Regex.Split(text, @"(\$.*?\$)");
                foreach (var part in parts)
                {
                    if (part.StartsWith("$") && part.EndsWith("$") && part.Length > 2)
                    {
                        var latex = part.Substring(1, part.Length - 2);
                        try
                        {
                            var formulaControl = new WpfMath.Controls.FormulaControl
                            {
                                Formula = latex,
                                Scale = fontSize * 1.25,
                                Margin = new Thickness(2, 0, 2, 0),
                                VerticalAlignment = VerticalAlignment.Center
                            };
                            if (foreground != null)
                            {
                                formulaControl.Foreground = foreground;
                            }
                            wrapPanel.Children.Add(formulaControl);
                        }
                        catch (Exception ex)
                        {
                            Serilog.Log.Warning("Failed to render LaTeX: {Formula}. Error: {Err}", latex, ex.Message);
                            wrapPanel.Children.Add(new TextBlock
                            {
                                Text = part,
                                FontSize = fontSize,
                                FontFamily = DS.FontPrimary,
                                FontStyle = FontStyles.Italic,
                                Foreground = Brushes.Red,
                                VerticalAlignment = VerticalAlignment.Center
                            });
                        }
                    }
                    else if (!string.IsNullOrEmpty(part))
                    {
                        wrapPanel.Children.Add(new TextBlock
                        {
                            Text = part,
                            FontSize = fontSize,
                            FontFamily = DS.FontPrimary,
                            Foreground = foreground ?? DS.Brush(DS.TextSecondary),
                            TextWrapping = TextWrapping.Wrap,
                            VerticalAlignment = VerticalAlignment.Center
                        });
                    }
                }
                return wrapPanel;
            }

            // 2. Check if the entire text is pure LaTeX (contains \, _, ^, {, })
            bool isPureLatex = text.Contains("\\") || text.Contains("_") || text.Contains("^") || text.Contains("{") || text.Contains("}");
            if (isPureLatex)
            {
                try
                {
                    var formulaControl = new WpfMath.Controls.FormulaControl
                    {
                        Formula = text,
                        Scale = fontSize * 1.3,
                        Margin = new Thickness(0, 4, 0, 4),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    if (foreground != null)
                    {
                        formulaControl.Foreground = foreground;
                    }
                    return formulaControl;
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning("Failed to render pure LaTeX: {Formula}. Error: {Err}", text, ex.Message);
                }
            }

            // 3. Fallback: standard TextBlock
            return new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontFamily = DS.FontPrimary,
                Foreground = foreground ?? DS.Brush(DS.TextSecondary),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        /// <summary>Overload: truyền colorHex string</summary>
        public static Border ResultRow(string text, string colorHex, StackPanel? panel = null)
            => ResultRow(text, (Color)ColorConverter.ConvertFromString(colorHex), panel);

        /// <summary>
        /// Tạo dòng kết quả cuộn ngang với click-to-copy.
        /// </summary>
        public static Border ResultRowScrolling(string text, Color color, StackPanel? panel = null)
        {
            var border = new Border
            {
                Background = DS.LightBg(color),
                CornerRadius = new CornerRadius(DS.RadiusChip),
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 0, DS.MarginResult),
                Cursor = Cursors.Hand,
                ToolTip = "📋 Nhấn để copy"
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tb = new TextBlock
            {
                FontSize = DS.FontResult,
                FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(color),
                TextWrapping = TextWrapping.NoWrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            if (text != null && text.Contains("**"))
            {
                ParseRichText(text, tb);
            }
            else
            {
                tb.Text = text;
            }
            var scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = tb
            };
            Grid.SetColumn(scrollViewer, 0);
            grid.Children.Add(scrollViewer);

            var copyIcon = new TextBlock
            {
                Text = "📋",
                FontSize = DS.FontResult,
                Foreground = DS.BrushAlpha(color, 150),
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "📋 Nhấn để copy"
            };
            border.MouseEnter += (_, _) => copyIcon.Foreground = DS.Brush(color);
            border.MouseLeave += (_, _) => copyIcon.Foreground = DS.BrushAlpha(color, 150);

            Grid.SetColumn(copyIcon, 1);
            grid.Children.Add(copyIcon);

            border.Child = grid;
            string ct = text?.Replace("**", "") ?? "";
            border.MouseLeftButtonDown += (_, _) => { try { Clipboard.SetText(ct); } catch { } };
            panel?.Children.Add(border);
            return border;
        }

        /// <summary>Overload: truyền colorHex string cho dòng cuộn ngang</summary>
        public static Border ResultRowScrolling(string text, string colorHex, StackPanel? panel = null)
            => ResultRowScrolling(text, (Color)ColorConverter.ConvertFromString(colorHex), panel);

        private static void ParseRichText(string text, TextBlock tb)
        {
            if (string.IsNullOrEmpty(text)) return;
            string[] parts = text.Split(new[] { "**" }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
            {
                if (i % 2 == 1)
                {
                    tb.Inlines.Add(new System.Windows.Documents.Run(parts[i]) { FontWeight = FontWeights.Bold });
                }
                else
                {
                    tb.Inlines.Add(new System.Windows.Documents.Run(parts[i]));
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  ⚡ PRESET BUTTON — Nút ví dụ nhanh
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Tạo nút preset chuẩn (ví dụ nhanh).
        /// </summary>
        /// <param name="label">Text hiển thị</param>
        /// <param name="color">Màu accent</param>
        /// <param name="onClick">Action khi click</param>
        /// <param name="panel">WrapPanel chứa presets</param>
        public static Border PresetButton(string label, Color color, Action onClick, Panel? panel = null)
        {
            var btn = new Border
            {
                Background = DS.LightBg(color),
                BorderBrush = DS.Brush(color),
                BorderThickness = new Thickness(0, 0, 0, 2),
                CornerRadius = new CornerRadius(DS.RadiusChip),
                Padding = new Thickness(DS.PadChip, 8, DS.PadChip, 8),
                Margin = new Thickness(0, 0, DS.TouchGap, DS.TouchGap),
                Cursor = Cursors.Hand,
                MinHeight = DS.TouchMinHeight
            };
            btn.Child = new TextBlock
            {
                Text = label,
                FontSize = DS.FontPreset,
                FontWeight = FontWeights.SemiBold,
                FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(color),
                VerticalAlignment = VerticalAlignment.Center
            };
            btn.MouseLeftButtonDown += (_, _) => onClick();

            // Hover
            btn.MouseEnter += (_, _) => btn.Background = DS.MediumBg(color);
            btn.MouseLeave += (_, _) => btn.Background = DS.LightBg(color);

            panel?.Children.Add(btn);
            return btn;
        }

        // ═══════════════════════════════════════════════════════════
        //  📈 Graph BUTTON — Nút mở đồ thị
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Tạo nút Graph chuẩn hóa.
        /// </summary>
        public static Border GraphButton(string subtitle, Color color, MouseButtonEventHandler onClick)
        {
            var border = new Border
            {
                Background = DS.LightBg(color),
                CornerRadius = new CornerRadius(DS.RadiusHeader),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, DS.MarginSection * 0.75, 0, 0),
                Cursor = Cursors.Hand,
                BorderBrush = DS.BrushAlpha(color, 100),
                BorderThickness = new Thickness(1),
                MinHeight = DS.TouchMinHeight
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            sp.Children.Add(new TextBlock
            {
                Text = "📈", FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            });
            var textSp = new StackPanel();
            textSp.Children.Add(new TextBlock
            {
                Text = "Xem đồ thị",
                FontSize = DS.FontPreset, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(color)
            });
            textSp.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = DS.FontTag,
                FontFamily = DS.FontPrimary,
                Foreground = DS.BrushAlpha(color, 180)
            });
            sp.Children.Add(textSp);
            border.Child = sp;
            border.MouseLeftButtonDown += onClick;
            return border;
        }

        // ═══════════════════════════════════════════════════════════
        //  📋 SECTION CARD — Card chứa nội dung
        // ═══════════════════════════════════════════════════════════

        /// <summary>Tạo card section trắng chuẩn</summary>
        public static Border SectionCard(UIElement content, Color borderColor = default)
        {
            if (borderColor == default) borderColor = Color.FromRgb(224, 224, 224);
            return new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(DS.RadiusCard),
                Padding = new Thickness(DS.PadCard, 16, DS.PadCard, 16),
                Margin = new Thickness(0, 0, 0, DS.MarginSection),
                BorderBrush = DS.Brush(borderColor),
                BorderThickness = new Thickness(1),
                Child = content
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  🏷️ HEADER — Tiêu đề tool với gradient
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Tạo header gradient chuẩn cho tool.
        /// </summary>
        public static Border ToolHeader(string icon, string title, string subtitle,
            Color gradientStart, Color gradientEnd, Color textColor)
        {
            var header = new Border
            {
                CornerRadius = new CornerRadius(DS.RadiusHeader),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, DS.MarginSection * 0.75)
            };
            header.Background = new LinearGradientBrush(gradientStart, gradientEnd, 0);

            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = $"{icon} {title}",
                FontSize = DS.FontTitle, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary, Foreground = DS.Brush(textColor)
            });
            sp.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = DS.FontSubtitle, FontFamily = DS.FontPrimary,
                Foreground = DS.BrushAlpha(textColor, 200),
                Margin = new Thickness(0, 3, 0, 0)
            });
            header.Child = sp;
            return header;
        }

        // ═══════════════════════════════════════════════════════════
        //  📝 INPUT FIELD — Ô nhập liệu chuẩn
        // ═══════════════════════════════════════════════════════════

        /// <summary>Tạo TextBox input chuẩn</summary>
        public static TextBox InputField(string text = "", double fontSize = 0)
        {
            return new TextBox
            {
                Text = text,
                FontSize = fontSize > 0 ? fontSize : DS.FontInput,
                FontFamily = DS.FontPrimary,
                Padding = new Thickness(8, 6, 8, 6),
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                VerticalContentAlignment = VerticalAlignment.Center
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  📅 DATA GRID — Bảng dữ liệu chuẩn
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Tạo DataGrid chuẩn hóa theo Design System.
        /// </summary>
        public static DataGrid StandardDataGrid(System.Collections.IEnumerable itemsSource)
        {
            var grid = new DataGrid
            {
                ItemsSource = itemsSource,
                AutoGenerateColumns = false,
                IsReadOnly = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                RowBackground = Brushes.Transparent,
                AlternatingRowBackground = new SolidColorBrush(Color.FromArgb(10, 0, 0, 0)),
                GridLinesVisibility = DataGridGridLinesVisibility.None,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                FontSize = DS.FontLabel,
                FontFamily = DS.FontPrimary,
                RowHeight = 36,
                CanUserAddRows = false,
                SelectionMode = DataGridSelectionMode.Single,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            // Header Style
            var headerStyle = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(Control.BackgroundProperty, DS.LightBg(DS.ResultInfo)));
            headerStyle.Setters.Add(new Setter(Control.ForegroundProperty, DS.Brush(DS.ResultDark)));
            headerStyle.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
            headerStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 5, 10, 5)));
            headerStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
            headerStyle.Setters.Add(new Setter(Control.BorderBrushProperty, DS.BrushAlpha(DS.ResultInfo, 100)));
            grid.ColumnHeaderStyle = headerStyle;

            // Row Style (Hover effect)
            var rowStyle = new Style(typeof(DataGridRow));
            rowStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
            var trigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(Control.BackgroundProperty, DS.LightBg(DS.ResultPrimary)));
            rowStyle.Triggers.Add(trigger);
            grid.RowStyle = rowStyle;

            return grid;
        }

        // ═══════════════════════════════════════════════════════════
        //  🔢 HELPER: Format số hiển thị
        // ═══════════════════════════════════════════════════════════

        /// <summary>Format số: nguyên → không thập phân, thực → G6</summary>
        public static string Fmt(double v)
            => Math.Abs(v - Math.Round(v)) < 1e-9
                ? ((long)Math.Round(v)).ToString()
                : v.ToString("G6");

        /// <summary>Chuyển đổi số nguyên dương thành dạng số mũ nhỏ (superscript)</summary>
        public static string ToSuperscript(int number)
        {
            if (number < 0) return $"^{number}";
            string numStr = number.ToString();
            var sb = new System.Text.StringBuilder();
            foreach (char c in numStr)
            {
                switch (c)
                {
                    case '0': sb.Append('⁰'); break;
                    case '1': sb.Append('¹'); break;
                    case '2': sb.Append('²'); break;
                    case '3': sb.Append('³'); break;
                    case '4': sb.Append('⁴'); break;
                    case '5': sb.Append('⁵'); break;
                    case '6': sb.Append('⁶'); break;
                    case '7': sb.Append('⁷'); break;
                    case '8': sb.Append('⁸'); break;
                    case '9': sb.Append('⁹'); break;
                }
            }
            return sb.ToString();
        }

        /// <summary>Chuyển đổi số nguyên dương thành dạng chỉ số dưới (subscript)</summary>
        public static string ToSubscript(int number)
        {
            if (number < 0) return $"_{number}";
            string numStr = number.ToString();
            var sb = new System.Text.StringBuilder();
            foreach (char c in numStr)
            {
                switch (c)
                {
                    case '0': sb.Append('₀'); break;
                    case '1': sb.Append('₁'); break;
                    case '2': sb.Append('₂'); break;
                    case '3': sb.Append('₃'); break;
                    case '4': sb.Append('₄'); break;
                    case '5': sb.Append('₅'); break;
                    case '6': sb.Append('₆'); break;
                    case '7': sb.Append('₇'); break;
                    case '8': sb.Append('₈'); break;
                    case '9': sb.Append('₉'); break;
                }
            }
            return sb.ToString();
        }

        /// <summary>Chuyển đổi số thực thành dạng chuỗi phân số tối giản (ví dụ 0.75 -> 3/4)</summary>
        public static string RealToFraction(double value, double tolerance = 1e-9)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "";
            if (System.Math.Abs(value - System.Math.Round(value)) < tolerance)
                return ""; // Không chuyển đổi số nguyên vẹn

            double x = value;
            long m00 = 1, m01 = 0, m10 = 0, m11 = 1;
            double absX = System.Math.Abs(x);
            double sign = x < 0 ? -1 : 1;
            double val = absX;

            for (int iter = 0; iter < 50; iter++)
            {
                long a = (long)System.Math.Floor(val);
                double remainder = val - a;

                long nextM00 = m00 * a + m01;
                long nextM10 = m10 * a + m11;

                if (System.Math.Abs(nextM10) >= 10000)
                    break;

                m01 = m00;
                m11 = m10;
                m00 = nextM00;
                m10 = nextM10;

                if (remainder < tolerance) break;
                val = 1.0 / remainder;
            }

            double error = System.Math.Abs(absX - (double)m00 / m10);
            if (error < 1e-6 && m10 > 1 && m10 < 10000)
            {
                return $"{(sign * m00)}/{m10}";
            }
            return "";
        }
    }
}

