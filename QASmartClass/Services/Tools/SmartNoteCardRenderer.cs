using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace QASmartClass.Services.Tools
{
    /// <summary>
    /// Trình dựng Thẻ Ghi Chú Thông Minh (SmartNoteCard) chuẩn vector tương tác cho Form2_MainDashboard.
    /// Kế thừa phong cách thẻ cao cấp của MathCard (viền bo tròn 12px, bóng đổ, Header + Badge viên thuốc),
    /// tích hợp khả năng sửa chữ trực tiếp (In-place editing), bàn phím ảo và đa dạng phông chữ / màu sắc từ NoteBoard.
    /// </summary>
    public static class SmartNoteCardRenderer
    {
        public const string CardTag = "SmartNoteCard";

        /// <summary>
        /// Tạo Border Thẻ Ghi Chú Thông Minh
        /// </summary>
        public static Border CreateNoteCard(SmartNoteModel model, 
            Action<Border>? onRequestEdit = null,
            Action<Border>? onRequestFormat = null)
        {
            var theme = SmartNoteTheme.Get(model.ThemeType);

            var cardBorder = new Border
            {
                Name = "SmartNoteCardRoot",
                Background = theme.Background,
                BorderBrush = theme.BorderBrush,
                BorderThickness = new Thickness(2.5),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(18, 14, 18, 16),
                MinWidth = 320,
                MaxWidth = 900,
                Width = model.Width > 0 ? model.Width : 420,
                Tag = CardTag,
                DataContext = model,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 4,
                    BlurRadius = 12,
                    Opacity = 0.20
                }
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 0: Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 1: Divider
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Row 2: Content

            // ================= 1. HEADER (Phong cách MathCard) =================
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Tiêu đề bên trái: Icon + Tiêu đề
            var titleStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var iconBlock = new TextBlock
            {
                Text = "📌",
                FontSize = 18,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            var titleBlock = new TextBlock
            {
                Name = "txtCardTitle",
                Text = model.Title,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = theme.TitleColor,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleStack.Children.Add(iconBlock);
            titleStack.Children.Add(titleBlock);
            Grid.SetColumn(titleStack, 0);
            headerGrid.Children.Add(titleStack);

            // Nút công cụ nhanh Mini Toolbar (Format: Đổi font / Đổi màu)
            var formatBtn = new Button
            {
                Name = "btnFormatNote",
                Content = "🎨",
                FontSize = 13,
                Width = 28,
                Height = 28,
                Margin = new Thickness(0, 0, 8, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                ToolTip = "Đổi phông chữ, cỡ chữ & màu thẻ"
            };
            formatBtn.Click += (s, e) =>
            {
                e.Handled = true;
                onRequestFormat?.Invoke(cardBorder);
            };
            Grid.SetColumn(formatBtn, 1);
            headerGrid.Children.Add(formatBtn);

            // Badge viên thuốc bên phải (giống MathCard "BẢNG BIẾN THIÊN")
            var badgeBorder = new Border
            {
                Name = "badgeBorder",
                Background = theme.BadgeBg,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 3, 10, 3),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand,
                ToolTip = "Chạm để đổi nhãn"
            };
            var badgeText = new TextBlock
            {
                Name = "txtBadge",
                Text = model.BadgeText,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = theme.BadgeFg
            };
            badgeBorder.Child = badgeText;
            Grid.SetColumn(badgeBorder, 2);
            headerGrid.Children.Add(badgeBorder);

            Grid.SetRow(headerGrid, 0);
            mainGrid.Children.Add(headerGrid);

            // ================= 2. DIVIDER LINE =================
            var dividerLine = new Line
            {
                Name = "dividerLine",
                X1 = 0,
                Y1 = 0,
                X2 = 1,
                Y2 = 0,
                Stretch = Stretch.Fill,
                Stroke = theme.DividerBrush,
                StrokeThickness = 1,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(dividerLine, 1);
            mainGrid.Children.Add(dividerLine);

            // ================= 3. CONTENT AREA =================
            var contentGrid = new Grid { MinHeight = 60 };

            var tbDisplay = new TextBlock
            {
                Name = "tbDisplay",
                Text = model.Content,
                FontFamily = new FontFamily(model.FontFamily),
                FontSize = model.FontSize,
                FontWeight = model.IsBold ? FontWeights.Bold : FontWeights.Normal,
                FontStyle = model.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                Foreground = theme.TextColor,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = Math.Max(20, model.FontSize * 1.35),
                Visibility = Visibility.Visible,
                Cursor = Cursors.IBeam
            };

            var txtEditor = new TextBox
            {
                Name = "txtEditor",
                Text = model.Content,
                FontFamily = new FontFamily(model.FontFamily),
                FontSize = model.FontSize,
                FontWeight = model.IsBold ? FontWeights.Bold : FontWeights.Normal,
                FontStyle = model.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                Foreground = theme.TextColor,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Visibility = Visibility.Collapsed
            };

            // Kết thúc chỉnh sửa khi mất focus
            txtEditor.TextChanged += (s, e) =>
            {
                if (cardBorder.DataContext is SmartNoteModel model)
                {
                    model.Content = txtEditor.Text;
                }
            };

            txtEditor.LostFocus += (s, e) =>
            {
                FinishEditing(cardBorder);
            };

            txtEditor.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    CancelEditing(cardBorder);
                    e.Handled = true;
                }
            };

            contentGrid.Children.Add(tbDisplay);
            contentGrid.Children.Add(txtEditor);

            Grid.SetRow(contentGrid, 2);
            mainGrid.Children.Add(contentGrid);

            cardBorder.Child = mainGrid;

            // Xử lý chạm/click đúp để sửa chữ
            DateTime lastClickTime = DateTime.MinValue;
            cardBorder.MouseLeftButtonDown += (s, e) =>
            {
                var now = DateTime.Now;
                if ((now - lastClickTime).TotalMilliseconds <= 350)
                {
                    if (onRequestEdit != null)
                    {
                        onRequestEdit(cardBorder);
                    }
                    else
                    {
                        StartEditing(cardBorder);
                    }
                    e.Handled = true;
                }
                lastClickTime = now;
            };

            tbDisplay.MouseLeftButtonDown += (s, e) =>
            {
                var now = DateTime.Now;
                if ((now - lastClickTime).TotalMilliseconds <= 350)
                {
                    if (onRequestEdit != null)
                    {
                        onRequestEdit(cardBorder);
                    }
                    else
                    {
                        StartEditing(cardBorder);
                    }
                    e.Handled = true;
                }
                lastClickTime = now;
            };

            DateTime lastTouchTime = DateTime.MinValue;
            cardBorder.TouchDown += (s, e) =>
            {
                var now = DateTime.Now;
                if ((now - lastTouchTime).TotalMilliseconds <= 350)
                {
                    if (onRequestEdit != null)
                    {
                        onRequestEdit(cardBorder);
                    }
                    else
                    {
                        StartEditing(cardBorder);
                    }
                    e.Handled = true;
                }
                lastTouchTime = now;
            };

            tbDisplay.TouchDown += (s, e) =>
            {
                var now = DateTime.Now;
                if ((now - lastTouchTime).TotalMilliseconds <= 350)
                {
                    if (onRequestEdit != null)
                    {
                        onRequestEdit(cardBorder);
                    }
                    else
                    {
                        StartEditing(cardBorder);
                    }
                    e.Handled = true;
                }
                lastTouchTime = now;
            };

            return cardBorder;
        }

        /// <summary>
        /// Nhân bản (Clone) toàn diện Thẻ Ghi Chú Thông Minh kèm đầy đủ dữ liệu, phông chữ, màu sắc và tương tác
        /// </summary>
        public static Border? TryCloneNoteCard(Border sourceBorder)
        {
            if (sourceBorder == null) return null;
            if (!string.Equals(sourceBorder.Tag?.ToString(), CardTag, StringComparison.OrdinalIgnoreCase)) return null;
            if (sourceBorder.DataContext is not SmartNoteModel noteModel) return null;

            var clonedModel = new SmartNoteModel
            {
                Title = noteModel.Title,
                BadgeText = noteModel.BadgeText,
                Content = noteModel.Content,
                FontFamily = noteModel.FontFamily,
                FontSize = noteModel.FontSize,
                IsBold = noteModel.IsBold,
                IsItalic = noteModel.IsItalic,
                ThemeType = noteModel.ThemeType,
                Width = sourceBorder.ActualWidth > 0 ? sourceBorder.ActualWidth : (sourceBorder.Width > 0 ? sourceBorder.Width : noteModel.Width)
            };

            var newCard = CreateNoteCard(
                clonedModel,
                onRequestEdit: (card) =>
                {
                    if (Application.Current.MainWindow is QASmartTouch.Forms.Form2_MainDashboard md)
                    {
                        md.StartInPlaceSmartNoteEditing(card);
                    }
                },
                onRequestFormat: (card) =>
                {
                    if (Application.Current.MainWindow is QASmartTouch.Forms.Form2_MainDashboard md)
                    {
                        md.OpenSmartNoteFormatDialog(card);
                    }
                }
            );

            newCard.Width = sourceBorder.ActualWidth > 0 ? sourceBorder.ActualWidth : sourceBorder.Width;
            newCard.Height = sourceBorder.ActualHeight > 0 ? sourceBorder.ActualHeight : sourceBorder.Height;
            return newCard;
        }

        public static void StartEditing(Border cardBorder)
        {
            if (FindElements(cardBorder, out _, out _, out var tbDisplay, out var txtEditor))
            {
                txtEditor.Text = tbDisplay.Text;
                tbDisplay.Visibility = Visibility.Collapsed;
                txtEditor.Visibility = Visibility.Visible;
                txtEditor.Focus();
                txtEditor.SelectAll();
            }
        }

        public static void FinishEditing(Border cardBorder, string? explicitText = null)
        {
            if (FindElements(cardBorder, out _, out _, out var tbDisplay, out var txtEditor))
            {
                string textToApply = explicitText ?? txtEditor.Text;
                tbDisplay.Text = textToApply;
                txtEditor.Text = textToApply;
                if (cardBorder.DataContext is SmartNoteModel model)
                {
                    model.Content = textToApply;
                }
                txtEditor.Visibility = Visibility.Collapsed;
                tbDisplay.Visibility = Visibility.Visible;
            }
        }

        public static void CancelEditing(Border cardBorder)
        {
            if (FindElements(cardBorder, out _, out _, out var tbDisplay, out var txtEditor))
            {
                txtEditor.Text = tbDisplay.Text;
                txtEditor.Visibility = Visibility.Collapsed;
                tbDisplay.Visibility = Visibility.Visible;
            }
        }

        public static void ApplyTheme(Border cardBorder, SmartNoteThemeType themeType)
        {
            var theme = SmartNoteTheme.Get(themeType);
            cardBorder.Background = theme.Background;
            cardBorder.BorderBrush = theme.BorderBrush;

            if (cardBorder.DataContext is SmartNoteModel model)
            {
                model.ThemeType = themeType;
            }

            if (FindElements(cardBorder, out var titleBlock, out var badgeBorder, out var tbDisplay, out var txtEditor))
            {
                titleBlock.Foreground = theme.TitleColor;
                badgeBorder.Background = theme.BadgeBg;
                if (badgeBorder.Child is TextBlock txtBadge)
                {
                    txtBadge.Foreground = theme.BadgeFg;
                }
                tbDisplay.Foreground = theme.TextColor;
                txtEditor.Foreground = theme.TextColor;

                if (cardBorder.Child is Grid g && g.Children.Count > 1 && g.Children[1] is Line divider)
                {
                    divider.Stroke = theme.DividerBrush;
                }
            }
        }

        public static void ApplyFont(Border cardBorder, string fontName)
        {
            var ff = new FontFamily(fontName);
            if (cardBorder.DataContext is SmartNoteModel model)
            {
                model.FontFamily = fontName;
            }

            if (FindElements(cardBorder, out _, out _, out var tbDisplay, out var txtEditor))
            {
                tbDisplay.FontFamily = ff;
                txtEditor.FontFamily = ff;
            }
        }

        public static void ApplyFontSize(Border cardBorder, double size)
        {
            if (size < 14) size = 14;
            if (size > 96) size = 96;

            if (cardBorder.DataContext is SmartNoteModel model)
            {
                model.FontSize = size;
            }

            if (FindElements(cardBorder, out _, out _, out var tbDisplay, out var txtEditor))
            {
                tbDisplay.FontSize = size;
                tbDisplay.LineHeight = Math.Max(20, size * 1.35);
                txtEditor.FontSize = size;
            }
        }

        public static void ApplyFontStyle(Border cardBorder, bool isBold, bool isItalic)
        {
            if (cardBorder.DataContext is SmartNoteModel model)
            {
                model.IsBold = isBold;
                model.IsItalic = isItalic;
            }

            if (FindElements(cardBorder, out _, out _, out var tbDisplay, out var txtEditor))
            {
                tbDisplay.FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal;
                txtEditor.FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal;
                tbDisplay.FontStyle = isItalic ? FontStyles.Italic : FontStyles.Normal;
                txtEditor.FontStyle = isItalic ? FontStyles.Italic : FontStyles.Normal;
            }
        }

        public static void ApplyTitleAndBadge(Border cardBorder, string title, string badge)
        {
            if (cardBorder.DataContext is SmartNoteModel model)
            {
                model.Title = title;
                model.BadgeText = badge;
            }

            if (FindElements(cardBorder, out var titleBlock, out var badgeBorder, out _, out _))
            {
                titleBlock.Text = title;
                if (badgeBorder.Child is TextBlock txtBadge)
                {
                    txtBadge.Text = badge;
                }
            }
        }

        public static TextBlock? GetDisplayTextBlock(Border cardBorder)
        {
            if (FindElements(cardBorder, out _, out _, out var tbDisplay, out _))
            {
                return tbDisplay;
            }
            return null;
        }

        public static TextBox? GetEditorTextBox(Border cardBorder)
        {
            if (FindElements(cardBorder, out _, out _, out _, out var txtEditor))
            {
                return txtEditor;
            }
            return null;
        }

        private static bool FindElements(Border cardBorder,
            out TextBlock titleBlock,
            out Border badgeBorder,
            out TextBlock tbDisplay,
            out TextBox txtEditor)
        {
            titleBlock = null!;
            badgeBorder = null!;
            tbDisplay = null!;
            txtEditor = null!;

            if (cardBorder.Child is not Grid mainGrid || mainGrid.Children.Count < 3)
                return false;

            if (mainGrid.Children[0] is Grid headerGrid)
            {
                if (headerGrid.Children[0] is StackPanel sp && sp.Children.Count > 1 && sp.Children[1] is TextBlock tb)
                {
                    titleBlock = tb;
                }
                if (headerGrid.Children.Count > 2 && headerGrid.Children[2] is Border bb)
                {
                    badgeBorder = bb;
                }
            }

            if (mainGrid.Children[2] is Grid contentGrid)
            {
                if (contentGrid.Children.Count > 0 && contentGrid.Children[0] is TextBlock tb)
                {
                    tbDisplay = tb;
                }
                if (contentGrid.Children.Count > 1 && contentGrid.Children[1] is TextBox te)
                {
                    txtEditor = te;
                }
            }

            return titleBlock != null && badgeBorder != null && tbDisplay != null && txtEditor != null;
        }
    }
}

