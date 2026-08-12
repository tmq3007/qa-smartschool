using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace QASmartTouch.Controls
{
    /// <summary>
    /// Rich Text Box Control with formatting toolbar
    /// </summary>
    public partial class RichTextBoxControl : UserControl
    {
        private bool _isPlaceholderActive = true;
        private const string PlaceholderText = "Nhập nội dung văn bản...";

        public event EventHandler? ContentChanged;

        public RichTextBoxControl()
        {
            InitializeComponent();
            // ✅ QC_4.2_SMART_TOUCH_TEXTBOX_ISOLATION_FIX: Gỡ bỏ ApplyTouchIsolation khỏi RichTextBoxControl 
            // để ComboBox (cmbFontFamily, cmbFontSize) và RichTextBox nhận đầy đủ sự kiện Mouse/Touch từ hệ thống.
            
            // Set default placeholder
            SetPlaceholder();

            Loaded += (s, e) =>
            {
                if (txtContent != null)
                {
                    txtContent.GotFocus += (s2, e2) => QASmartTouch.Forms.Form2_MainDashboard.ShowTouchKeyboard();
                    // ✅ QC_4.2_SMART_TOUCH_TEXTBOX_TOUCH_FIX: Đảm bảo chạm ngón tay trên SMART TOUCH đặt con trỏ cuối văn bản và bật bàn phím
                    txtContent.PreviewTouchDown += (s2, e2) =>
                    {
                        txtContent.Focus();
                        if (txtContent.Selection != null)
                        {
                            // Giải phóng vệt bôi xanh toàn bộ, chuyển con trỏ về vị trí nhập liệu
                            txtContent.Selection.Select(txtContent.Document.ContentEnd, txtContent.Document.ContentEnd);
                        }
                        QASmartTouch.Forms.Form2_MainDashboard.ShowTouchKeyboard();
                    };
                }

                // ✅ QC_4.2_COMBOBOX_TOUCH_FIX: Hỗ trợ mở ComboBox Font & Size bằng cảm ứng trực tiếp trên bảng SMART TOUCH
                if (cmbFontFamily != null)
                {
                    cmbFontFamily.PreviewTouchDown += (s2, e2) =>
                    {
                        cmbFontFamily.IsDropDownOpen = !cmbFontFamily.IsDropDownOpen;
                        e2.Handled = true;
                    };
                }
                if (cmbFontSize != null)
                {
                    cmbFontSize.PreviewTouchDown += (s2, e2) =>
                    {
                        cmbFontSize.IsDropDownOpen = !cmbFontSize.IsDropDownOpen;
                        e2.Handled = true;
                    };
                }
            };
        }

        #region Public Properties

        /// <summary>
        /// Get or set the text content
        /// </summary>
        public string Text
        {
            get
            {
                if (txtContent == null) return string.Empty;
                TextRange textRange = new TextRange(
                    txtContent.Document.ContentStart,
                    txtContent.Document.ContentEnd);
                string currentText = textRange.Text.Trim();
                if (_isPlaceholderActive && (string.IsNullOrEmpty(currentText) || currentText.Equals(PlaceholderText, StringComparison.OrdinalIgnoreCase)))
                    return string.Empty;
                return textRange.Text;
            }
            set
            {
                if (txtContent == null) return;
                txtContent.Document.Blocks.Clear();
                if (string.IsNullOrEmpty(value))
                {
                    SetPlaceholder();
                }
                else
                {
                    _isPlaceholderActive = false;
                    txtContent.Foreground = Brushes.Black;
                    txtContent.FontStyle = FontStyles.Normal;
                    txtContent.Document.Blocks.Add(new Paragraph(new Run(value)));
                }
            }
        }

        /// <summary>
        /// Get the FlowDocument for advanced manipulation
        /// </summary>
        public FlowDocument Document => txtContent.Document;

        #endregion

        #region Placeholder

        private void SetPlaceholder()
        {
            _isPlaceholderActive = true;
            if (txtContent == null) return;
            txtContent.Document.Blocks.Clear();
            var paragraph = new Paragraph(new Run(PlaceholderText))
            {
                Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
                FontStyle = FontStyles.Italic
            };
            txtContent.Document.Blocks.Add(paragraph);
        }

        /// <summary>
        /// Explicitly clear placeholder flag when content is loaded or applied externally
        /// </summary>
        public void ClearPlaceholder()
        {
            _isPlaceholderActive = false;
            if (txtContent != null)
            {
                txtContent.Foreground = Brushes.Black;
                txtContent.FontStyle = FontStyles.Normal;
            }
        }

        private void txtContent_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtContent == null) return;

            if (_isPlaceholderActive)
            {
                TextRange textRange = new TextRange(txtContent.Document.ContentStart, txtContent.Document.ContentEnd);
                string currentText = textRange.Text.Trim();

                // Only clear document if the current text is ACTUALLY the placeholder text
                if (currentText.Equals(PlaceholderText, StringComparison.OrdinalIgnoreCase))
                {
                    txtContent.Document.Blocks.Clear();
                    txtContent.Foreground = Brushes.Black;
                    txtContent.FontStyle = FontStyles.Normal;
                }
                _isPlaceholderActive = false;
            }
        }

        private void txtContent_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txtContent == null) return;
            TextRange textRange = new TextRange(txtContent.Document.ContentStart, txtContent.Document.ContentEnd);
            string currentText = textRange.Text.Trim();

            if (string.IsNullOrEmpty(currentText))
            {
                SetPlaceholder();
            }
        }

        private void Content_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isPlaceholderActive && txtContent != null)
            {
                TextRange textRange = new TextRange(txtContent.Document.ContentStart, txtContent.Document.ContentEnd);
                string currentText = textRange.Text.Trim();
                if (!string.IsNullOrEmpty(currentText) && !currentText.Equals(PlaceholderText, StringComparison.OrdinalIgnoreCase))
                {
                    _isPlaceholderActive = false;
                    txtContent.Foreground = Brushes.Black;
                    txtContent.FontStyle = FontStyles.Normal;
                }
            }
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Font Formatting

        private void FontFamily_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFontFamily.SelectedItem is ComboBoxItem item && txtContent != null)
            {
                string fontFamily = item.Content.ToString() ?? "Arial";
                txtContent.FontFamily = new FontFamily(fontFamily);
                if (!txtContent.Selection.IsEmpty)
                {
                    txtContent.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, 
                        new FontFamily(fontFamily));
                }
                txtContent.Focus();
            }
        }

        private void FontSize_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFontSize.SelectedItem is ComboBoxItem item && txtContent != null)
            {
                if (double.TryParse(item.Content.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double size))
                {
                    txtContent.FontSize = size;
                    if (!txtContent.Selection.IsEmpty)
                    {
                        txtContent.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, size);
                    }
                    txtContent.Focus();
                }
            }
        }

        #endregion

        #region Text Style

        private void Bold_Click(object sender, RoutedEventArgs e)
        {
            if (txtContent.Selection.GetPropertyValue(TextElement.FontWeightProperty) is FontWeight currentWeight)
            {
                FontWeight newWeight = (currentWeight == FontWeights.Bold) ? FontWeights.Normal : FontWeights.Bold;
                txtContent.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, newWeight);
            }
            txtContent.Focus();
        }

        private void Italic_Click(object sender, RoutedEventArgs e)
        {
            if (txtContent.Selection.GetPropertyValue(TextElement.FontStyleProperty) is FontStyle currentStyle)
            {
                FontStyle newStyle = (currentStyle == FontStyles.Italic) ? FontStyles.Normal : FontStyles.Italic;
                txtContent.Selection.ApplyPropertyValue(TextElement.FontStyleProperty, newStyle);
            }
            txtContent.Focus();
        }

        private void Underline_Click(object sender, RoutedEventArgs e)
        {
            var currentDecoration = txtContent.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
            
            if (currentDecoration == DependencyProperty.UnsetValue || currentDecoration == null)
            {
                txtContent.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty, TextDecorations.Underline);
            }
            else
            {
                txtContent.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty, null);
            }
            txtContent.Focus();
        }

        #endregion

        #region Colors

        private void TextColor_Click(object sender, RoutedEventArgs e)
        {
            // Simple color picker with predefined colors
            var colors = new[] {
                ("Đen", Colors.Black),
                ("Đỏ", Colors.Red),
                ("Xanh lá", Colors.Green),
                ("Xanh dương", Colors.Blue),
                ("Vàng", Colors.Gold),
                ("Cam", Colors.Orange),
                ("Tím", Colors.Purple),
                ("Xám", Colors.Gray)
            };

            // Create simple selection (for now, cycle through colors)
            // TODO: Create proper color picker dialog
            var currentColor = txtContent.Selection.GetPropertyValue(TextElement.ForegroundProperty) as SolidColorBrush;
            int currentIndex = 0;
            
            if (currentColor != null)
            {
                for (int i = 0; i < colors.Length; i++)
                {
                    if (currentColor.Color == colors[i].Item2)
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }
            
            // Cycle to next color
            int nextIndex = (currentIndex + 1) % colors.Length;
            txtContent.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, 
                new SolidColorBrush(colors[nextIndex].Item2));
            
            txtContent.Focus();
        }

        private void BgColor_Click(object sender, RoutedEventArgs e)
        {
            // Simple background color picker
            var colors = new[] {
                ("Trắng", Colors.White),
                ("Vàng nhạt", Colors.LightYellow),
                ("Xanh nhạt", Colors.LightBlue),
                ("Xanh lá nhạt", Colors.LightGreen),
                ("Hồng nhạt", Colors.LightPink),
                ("Cam nhạt", Color.FromRgb(255, 235, 205)),
                ("Xám nhạt", Colors.LightGray)
            };

            var currentColor = txtContent.Selection.GetPropertyValue(TextElement.BackgroundProperty) as SolidColorBrush;
            int currentIndex = 0;
            
            if (currentColor != null)
            {
                for (int i = 0; i < colors.Length; i++)
                {
                    if (currentColor.Color == colors[i].Item2)
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }
            
            // Cycle to next color
            int nextIndex = (currentIndex + 1) % colors.Length;
            txtContent.Selection.ApplyPropertyValue(TextElement.BackgroundProperty, 
                new SolidColorBrush(colors[nextIndex].Item2));
            
            txtContent.Focus();
        }

        #endregion

        #region Alignment

        private void AlignLeft_Click(object sender, RoutedEventArgs e)
        {
            txtContent.Selection.ApplyPropertyValue(Paragraph.TextAlignmentProperty, TextAlignment.Left);
            txtContent.Focus();
        }

        private void AlignCenter_Click(object sender, RoutedEventArgs e)
        {
            txtContent.Selection.ApplyPropertyValue(Paragraph.TextAlignmentProperty, TextAlignment.Center);
            txtContent.Focus();
        }

        private void AlignRight_Click(object sender, RoutedEventArgs e)
        {
            txtContent.Selection.ApplyPropertyValue(Paragraph.TextAlignmentProperty, TextAlignment.Right);
            txtContent.Focus();
        }

        #endregion

        #region Preset Templates

        /// <summary>
        /// Apply a preset template to the text box
        /// </summary>
        public void ApplyTemplate(TextBoxTemplate template)
        {
            switch (template)
            {
                case TextBoxTemplate.Note:
                    ApplyNoteTemplate();
                    break;
                case TextBoxTemplate.Warning:
                    ApplyWarningTemplate();
                    break;
                case TextBoxTemplate.Tip:
                    ApplyTipTemplate();
                    break;
                case TextBoxTemplate.Conclusion:
                    ApplyConclusionTemplate();
                    break;
                case TextBoxTemplate.Definition:
                    ApplyDefinitionTemplate();
                    break;
            }
        }

        private void ApplyNoteTemplate()
        {
            ClearPlaceholder();
            // Yellow note style
            txtContent.Background = new SolidColorBrush(Color.FromRgb(255, 251, 230)); // Light yellow
            this.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Yellow
            this.BorderThickness = new Thickness(2);
            
            txtContent.Document.Blocks.Clear();
            var paragraph = new Paragraph(new Run("📌 Ghi chú: "))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            txtContent.Document.Blocks.Add(paragraph);
        }

        private void ApplyWarningTemplate()
        {
            ClearPlaceholder();
            // Red warning style
            txtContent.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238)); // Light red
            this.BorderBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
            this.BorderThickness = new Thickness(3);
            
            txtContent.Document.Blocks.Clear();
            var paragraph = new Paragraph(new Run("⚠️ Cảnh báo: "))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(183, 28, 28)) // Dark red
            };
            txtContent.Document.Blocks.Add(paragraph);
        }

        private void ApplyTipTemplate()
        {
            ClearPlaceholder();
            // Blue tip style
            txtContent.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253)); // Light blue
            this.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Blue
            this.BorderThickness = new Thickness(2);
            
            txtContent.Document.Blocks.Clear();
            var paragraph = new Paragraph(new Run("💡 Mẹo: "))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 71, 161)) // Dark blue
            };
            txtContent.Document.Blocks.Add(paragraph);
        }

        private void ApplyConclusionTemplate()
        {
            ClearPlaceholder();
            // Green conclusion style
            txtContent.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)); // Light green
            this.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
            this.BorderThickness = new Thickness(3);
            
            txtContent.Document.Blocks.Clear();
            var paragraph = new Paragraph(new Run("✅ Kết luận: "))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(27, 94, 32)) // Dark green
            };
            txtContent.Document.Blocks.Add(paragraph);
        }

        private void ApplyDefinitionTemplate()
        {
            ClearPlaceholder();
            // Clean definition style
            txtContent.Background = Brushes.White;
            this.BorderBrush = new SolidColorBrush(Color.FromRgb(96, 125, 139)); // Blue grey
            this.BorderThickness = new Thickness(3);
            
            txtContent.Document.Blocks.Clear();
            var paragraph = new Paragraph(new Run("📖 Định nghĩa: "))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromRgb(38, 50, 56)) // Dark grey
            };
            txtContent.Document.Blocks.Add(paragraph);
        }

        #endregion
    }

    /// <summary>
    /// Text box template types
    /// </summary>
    public enum TextBoxTemplate
    {
        Note,        // Ghi chú
        Warning,     // Cảnh báo
        Tip,         // Mẹo
        Conclusion,  // Kết luận
        Definition   // Định nghĩa
    }
}
