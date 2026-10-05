using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace QASmartClass.Classroom.Views
{
    // ═══════════════════════════════════════════════════════════
    //  LESSON EDITOR — Mini Toolbar (Rich Text Formatting Tools)
    //  Tách từ LessonEditorPage.xaml.cs (lines 4599–4996)
    //  Gồm: CreateMiniToolbar, MakeToolbarBtn, MakeColorBtn,
    //       MakeHighlightBtn, MakeClearFormatBtn, MakeSeparator,
    //       WireMiniToolbar
    // ═══════════════════════════════════════════════════════════
    public partial class LessonEditorPage : Page
    {
        /// <summary>
        /// Tạo mini toolbar cho Rich Text formatting.
        /// Gồm: B I U | H1 H2 | FontSize [▼ 14 ▲] | Colors | Highlight | Clear
        /// </summary>
        private static StackPanel CreateMiniToolbar()
        {
            var toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 6)
            };

            // ── Group 1: Bold / Italic / Underline ──
            var biu = new (string content, string tag, string tooltip)[]
            {
                ("B",  "Bold",      "In đậm (Ctrl+B)"),
                ("I",  "Italic",    "In nghiêng (Ctrl+I)"),
                ("U",  "Underline", "Gạch chân (Ctrl+U)"),
            };
            foreach (var (content, tag, tooltip) in biu)
            {
                toolbar.Children.Add(MakeToolbarBtn(content, tag, tooltip,
                    tag == "Bold"   ? FontWeights.Bold  : FontWeights.Normal,
                    tag == "Italic" ? FontStyles.Italic : FontStyles.Normal));
            }

            toolbar.Children.Add(MakeSeparator());

            // ── Group 2: H1 / H2 ──
            toolbar.Children.Add(MakeToolbarBtn("H1", "H1", "Tiêu đề lớn (20px)"));
            toolbar.Children.Add(MakeToolbarBtn("H2", "H2", "Tiêu đề vừa (16px)"));

            toolbar.Children.Add(MakeSeparator());

            // ── Group 3: Font Size Spinbox [▼ 14 ▲] ──
            var fontSizePanel = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 3, 0), VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Cỡ chữ — Nhập số hoặc nhấn ▲▼ để điều chỉnh"
            };
            var fsInner = new StackPanel { Orientation = Orientation.Horizontal };

            var btnDown = new Button
            {
                Content = "▼", Tag = "FontDown", FontSize = 8, Width = 20, Height = 26,
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                BorderThickness = new Thickness(0, 0, 1, 0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                Cursor = System.Windows.Input.Cursors.Hand, Padding = new Thickness(0),
                ToolTip = "Giảm cỡ chữ (-2)"
            };
            var txtFontSize = new TextBox
            {
                Text = "14", Width = 32, Height = 26, FontSize = 11,
                TextAlignment = TextAlignment.Center, BorderThickness = new Thickness(0),
                Background = Brushes.White, VerticalContentAlignment = VerticalAlignment.Center,
                Tag = "FontSizeBox", ToolTip = "Nhập cỡ chữ (8-72)"
            };
            var btnUp = new Button
            {
                Content = "▲", Tag = "FontUp", FontSize = 8, Width = 20, Height = 26,
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                BorderThickness = new Thickness(1, 0, 0, 0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                Cursor = System.Windows.Input.Cursors.Hand, Padding = new Thickness(0),
                ToolTip = "Tăng cỡ chữ (+2)"
            };

            fsInner.Children.Add(btnDown);
            fsInner.Children.Add(txtFontSize);
            fsInner.Children.Add(btnUp);
            fontSizePanel.Child = fsInner;
            toolbar.Children.Add(fontSizePanel);

            toolbar.Children.Add(MakeSeparator());

            // ── Group 4: Colors ──
            var colorDefs = new (string tag, string tooltip, byte r, byte g, byte b)[]
            {
                ("Blue",   "Chữ xanh dương",     25, 118, 210),
                ("Red",    "Chữ đỏ",             198,  40,  40),
                ("Green",  "Chữ xanh lá",         46, 125,  50),
                ("Orange", "Chữ cam",             230, 126,  34),
                ("Purple", "Chữ tím",             142,  68, 173),
                ("Black",  "Chữ đen (mặc định)",   33,  33,  33),
            };
            foreach (var (tag, tooltip, r, g, b) in colorDefs)
                toolbar.Children.Add(MakeColorBtn(tag, tooltip, r, g, b));

            toolbar.Children.Add(MakeSeparator());

            // ── Group 5: Highlight + Clear Format ──
            toolbar.Children.Add(MakeHighlightBtn());
            toolbar.Children.Add(MakeClearFormatBtn());

            return toolbar;
        }

        /// <summary>Tạo 1 nút toolbar text (Bold/Italic/Underline/H1/H2).</summary>
        private static Button MakeToolbarBtn(
            string content, string tag, string tooltip,
            FontWeight? fontWeight = null, FontStyle? fontStyle = null)
        {
            return new Button
            {
                Content = content, Tag = tag, ToolTip = tooltip,
                Width = 30, Height = 26,
                FontSize = content.Length <= 2 ? 12 : 11,
                FontWeight = fontWeight ?? FontWeights.Normal,
                FontStyle  = fontStyle  ?? FontStyles.Normal,
                Background      = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                Margin          = new Thickness(0, 0, 3, 0),
                Cursor          = System.Windows.Input.Cursors.Hand,
                Padding         = new Thickness(0)
            };
        }

        /// <summary>Tạo nút màu chữ với ô tròn màu thực.</summary>
        private static Button MakeColorBtn(string tag, string tooltip, byte r, byte g, byte b)
        {
            var colorCircle = new System.Windows.Shapes.Ellipse
            {
                Width = 14, Height = 14,
                Fill   = new SolidColorBrush(Color.FromRgb(r, g, b)),
                Stroke = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                StrokeThickness = 1
            };

            return new Button
            {
                Content = colorCircle, Tag = tag, ToolTip = tooltip,
                Width = 26, Height = 26,
                Background      = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                Margin          = new Thickness(0, 0, 2, 0),
                Cursor          = System.Windows.Input.Cursors.Hand,
                Padding         = new Thickness(0)
            };
        }

        /// <summary>Nút Highlight nền vàng.</summary>
        private static Button MakeHighlightBtn()
        {
            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var swatch = new Border
            {
                Width = 16, Height = 10,
                Background      = new SolidColorBrush(Color.FromRgb(255, 245, 157)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(200, 190, 100)),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(2),
                VerticalAlignment = VerticalAlignment.Center
            };
            var txt = new TextBlock
            {
                Text = "A", FontSize = 10, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 0)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 0, 0)
            };
            stack.Children.Add(swatch);
            stack.Children.Add(txt);

            return new Button
            {
                Content = stack, Tag = "Highlight", ToolTip = "Đánh dấu nền vàng",
                Width = 38, Height = 26,
                Background      = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                Margin          = new Thickness(0, 0, 2, 0),
                Cursor          = System.Windows.Input.Cursors.Hand,
                Padding         = new Thickness(2, 0, 2, 0)
            };
        }

        /// <summary>Nút Clear Format (Xóa định dạng).</summary>
        private static Button MakeClearFormatBtn()
        {
            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var txt = new TextBlock
            {
                Text = "Aa", FontSize = 10,
                Foreground      = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                TextDecorations = TextDecorations.Strikethrough,
                VerticalAlignment = VerticalAlignment.Center
            };
            stack.Children.Add(txt);

            return new Button
            {
                Content = stack, Tag = "ClearFormat", ToolTip = "Xóa định dạng (reset về mặc định)",
                Width = 30, Height = 26,
                Background      = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                Margin          = new Thickness(0, 0, 3, 0),
                Cursor          = System.Windows.Input.Cursors.Hand,
                Padding         = new Thickness(0)
            };
        }

        /// <summary>Separator dọc giữa các nhóm nút toolbar.</summary>
        private static Border MakeSeparator()
        {
            return new Border
            {
                Width = 1, Height = 20,
                Background = new SolidColorBrush(Color.FromRgb(210, 210, 210)),
                Margin = new Thickness(4, 3, 4, 3),
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        /// <summary>
        /// Wire tất cả toolbar buttons vào RichTextBox để apply formatting.
        /// </summary>
        private static void WireMiniToolbar(StackPanel toolbar, RichTextBox rtb)
        {
            // Tìm FontSize TextBox trong toolbar
            TextBox? fontSizeBox = null;
            foreach (var tbChild in toolbar.Children)
            {
                if (tbChild is Border border && border.Child is StackPanel sp)
                {
                    foreach (var spChild in sp.Children)
                    {
                        if (spChild is TextBox tb && tb.Tag?.ToString() == "FontSizeBox")
                        {
                            fontSizeBox = tb;
                            break;
                        }
                    }
                }
            }

            // Sync font size display khi cursor di chuyển
            if (fontSizeBox != null)
            {
                var fsBox = fontSizeBox;
                rtb.SelectionChanged += (s, e) =>
                {
                    try
                    {
                        var val = rtb.Selection.GetPropertyValue(TextElement.FontSizeProperty);
                        if (val is double d)
                            fsBox.Text = ((int)d).ToString();
                    }
                    catch { /* ignore */ }
                };

                fontSizeBox.KeyDown += (s, e) =>
                {
                    if (e.Key == System.Windows.Input.Key.Enter)
                    {
                        if (double.TryParse(fsBox.Text,
                                System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out double sz))
                        {
                            sz = Math.Clamp(sz, 8, 72);
                            fsBox.Text = ((int)sz).ToString();
                            if (!rtb.Selection.IsEmpty)
                                rtb.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, sz);
                            rtb.Focus();
                        }
                    }
                };
            }

            // Wire tất cả buttons
            void WireAllButtons(UIElementCollection children)
            {
                foreach (var child in children)
                {
                    if (child is Button btn && btn.Tag != null)
                    {
                        btn.Click += (s, e) =>
                        {
                            var tag = (s as Button)?.Tag?.ToString();
                            var sel = rtb.Selection;

                            switch (tag)
                            {
                                case "Bold":
                                    if (sel.IsEmpty) return;
                                    var curW = sel.GetPropertyValue(TextElement.FontWeightProperty);
                                    sel.ApplyPropertyValue(TextElement.FontWeightProperty,
                                        curW is FontWeight fw && fw == FontWeights.Bold
                                            ? FontWeights.Normal : FontWeights.Bold);
                                    break;

                                case "Italic":
                                    if (sel.IsEmpty) return;
                                    var curS = sel.GetPropertyValue(TextElement.FontStyleProperty);
                                    sel.ApplyPropertyValue(TextElement.FontStyleProperty,
                                        curS is FontStyle fs && fs == FontStyles.Italic
                                            ? FontStyles.Normal : FontStyles.Italic);
                                    break;

                                case "Underline":
                                    if (sel.IsEmpty) return;
                                    var curDec = sel.GetPropertyValue(Inline.TextDecorationsProperty);
                                    sel.ApplyPropertyValue(Inline.TextDecorationsProperty,
                                        curDec == TextDecorations.Underline ? null : TextDecorations.Underline);
                                    break;

                                case "H1":
                                    if (sel.IsEmpty) return;
                                    sel.ApplyPropertyValue(TextElement.FontSizeProperty,   20.0);
                                    sel.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                                    if (fontSizeBox != null) fontSizeBox.Text = "20";
                                    break;

                                case "H2":
                                    if (sel.IsEmpty) return;
                                    sel.ApplyPropertyValue(TextElement.FontSizeProperty,   16.0);
                                    sel.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.SemiBold);
                                    if (fontSizeBox != null) fontSizeBox.Text = "16";
                                    break;

                                case "FontUp":
                                {
                                    double cur = 14;
                                    if (fontSizeBox != null &&
                                        double.TryParse(fontSizeBox.Text,
                                            System.Globalization.NumberStyles.Any,
                                            System.Globalization.CultureInfo.InvariantCulture, out double cs))
                                        cur = cs;
                                    cur = Math.Min(cur + 2, 72);
                                    if (fontSizeBox != null) fontSizeBox.Text = ((int)cur).ToString();
                                    if (!sel.IsEmpty)
                                        sel.ApplyPropertyValue(TextElement.FontSizeProperty, cur);
                                    break;
                                }

                                case "FontDown":
                                {
                                    double cur = 14;
                                    if (fontSizeBox != null &&
                                        double.TryParse(fontSizeBox.Text,
                                            System.Globalization.NumberStyles.Any,
                                            System.Globalization.CultureInfo.InvariantCulture, out double cs))
                                        cur = cs;
                                    cur = Math.Max(cur - 2, 8);
                                    if (fontSizeBox != null) fontSizeBox.Text = ((int)cur).ToString();
                                    if (!sel.IsEmpty)
                                        sel.ApplyPropertyValue(TextElement.FontSizeProperty, cur);
                                    break;
                                }

                                case "Blue":
                                    if (!sel.IsEmpty)
                                        sel.ApplyPropertyValue(TextElement.ForegroundProperty,
                                            new SolidColorBrush(Color.FromRgb(25, 118, 210)));
                                    break;
                                case "Red":
                                    if (!sel.IsEmpty)
                                        sel.ApplyPropertyValue(TextElement.ForegroundProperty,
                                            new SolidColorBrush(Color.FromRgb(198, 40, 40)));
                                    break;
                                case "Green":
                                    if (!sel.IsEmpty)
                                        sel.ApplyPropertyValue(TextElement.ForegroundProperty,
                                            new SolidColorBrush(Color.FromRgb(46, 125, 50)));
                                    break;
                                case "Orange":
                                    if (!sel.IsEmpty)
                                        sel.ApplyPropertyValue(TextElement.ForegroundProperty,
                                            new SolidColorBrush(Color.FromRgb(230, 126, 34)));
                                    break;
                                case "Purple":
                                    if (!sel.IsEmpty)
                                        sel.ApplyPropertyValue(TextElement.ForegroundProperty,
                                            new SolidColorBrush(Color.FromRgb(142, 68, 173)));
                                    break;
                                case "Black":
                                    if (!sel.IsEmpty)
                                        sel.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
                                    break;

                                case "Highlight":
                                    if (sel.IsEmpty) return;
                                    var curBg = sel.GetPropertyValue(TextElement.BackgroundProperty);
                                    var yellow = new SolidColorBrush(Color.FromRgb(255, 245, 157));
                                    sel.ApplyPropertyValue(TextElement.BackgroundProperty,
                                        curBg is SolidColorBrush sb && sb.Color == yellow.Color
                                            ? null : (object)yellow);
                                    break;

                                case "ClearFormat":
                                    if (sel.IsEmpty) return;
                                    sel.ApplyPropertyValue(TextElement.FontWeightProperty,  FontWeights.Normal);
                                    sel.ApplyPropertyValue(TextElement.FontStyleProperty,   FontStyles.Normal);
                                    sel.ApplyPropertyValue(Inline.TextDecorationsProperty,  null);
                                    sel.ApplyPropertyValue(TextElement.ForegroundProperty,  Brushes.Black);
                                    sel.ApplyPropertyValue(TextElement.BackgroundProperty,  null);
                                    sel.ApplyPropertyValue(TextElement.FontSizeProperty,    14.0);
                                    if (fontSizeBox != null) fontSizeBox.Text = "14";
                                    break;
                            }
                            rtb.Focus();
                        };
                    }
                    else if (child is Border brd && brd.Child is StackPanel innerSp)
                    {
                        WireAllButtons(innerSp.Children);
                    }
                }
            }

            WireAllButtons(toolbar.Children);
        }
    }
}
