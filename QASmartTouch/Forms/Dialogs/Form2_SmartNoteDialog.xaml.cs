using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using QASmartTouch.Services.Tools;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_SmartNoteDialog : Window
    {
        public bool IsConfirmed { get; private set; } = false;
        public SmartNoteModel Model { get; private set; }

        private bool _isInitializing = true;

        public Form2_SmartNoteDialog(SmartNoteModel? initialModel = null)
        {
            InitializeComponent();

            TouchActivationHelper.ApplyToWindow(this);

            Model = initialModel != null ? CloneModel(initialModel) : new SmartNoteModel();

            Loaded += Form2_SmartNoteDialog_Loaded;
            Closed += (s, e) => TouchKeyboardHelper.HideTouchKeyboard();
        }

        private static SmartNoteModel CloneModel(SmartNoteModel source)
        {
            return new SmartNoteModel
            {
                Title = source.Title,
                BadgeText = source.BadgeText,
                Content = source.Content,
                FontFamily = source.FontFamily,
                FontSize = source.FontSize,
                IsBold = source.IsBold,
                IsItalic = source.IsItalic,
                ThemeType = source.ThemeType,
                Width = source.Width
            };
        }

        private void Form2_SmartNoteDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Nạp danh sách Font NoteBoard vào ComboBox
            cboFonts.ItemsSource = SmartNoteFontInfo.SupportedFonts;
            cboFonts.DisplayMemberPath = "DisplayName";
            cboFonts.SelectedValuePath = "FontFamilyName";

            // Chọn font hiện tại hoặc mặc định
            var matchedFont = SmartNoteFontInfo.SupportedFonts.FirstOrDefault(f => f.FontFamilyName.Equals(Model.FontFamily, StringComparison.OrdinalIgnoreCase))
                           ?? SmartNoteFontInfo.SupportedFonts.FirstOrDefault(f => f.FontFamilyName.StartsWith(Model.FontFamily.Split(',')[0].Trim(), StringComparison.OrdinalIgnoreCase))
                           ?? SmartNoteFontInfo.SupportedFonts.First();
            cboFonts.SelectedItem = matchedFont;

            // Đồng bộ dữ liệu lên giao diện
            txtTitleInput.Text = Model.Title;
            txtBadgeInput.Text = Model.BadgeText;
            txtContentInput.Text = Model.Content;
            txtFontSizeDisplay.Text = ((int)Model.FontSize).ToString();

            UpdateStyleButtonsState();

            // Gắn sự kiện kích hoạt bàn phím ảo đồng nhất cho mọi ô nhập văn bản
            WireKeyboardActivation(txtContentInput);
            WireKeyboardActivation(txtTitleInput);
            WireKeyboardActivation(txtBadgeInput);

            // Xử lý phím Enter trong các ô nhập
            txtTitleInput.KeyDown += (s, ev) =>
            {
                if (ev.Key == Key.Enter)
                {
                    txtContentInput.Focus();
                    ev.Handled = true;
                }
            };

            txtBadgeInput.KeyDown += (s, ev) =>
            {
                if (ev.Key == Key.Enter)
                {
                    txtContentInput.Focus();
                    ev.Handled = true;
                }
            };

            txtContentInput.KeyDown += (s, ev) =>
            {
                if (ev.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    btnConfirm_Click(btnConfirm, new RoutedEventArgs());
                    ev.Handled = true;
                }
            };

            _isInitializing = false;
            UpdatePreview();

            // Đặt trỏ chuột vào vùng soạn thảo
            txtContentInput.Focus();
            txtContentInput.SelectAll();
        }

        private void WireKeyboardActivation(TextBox tb)
        {
            tb.GotFocus += (s, e) => TouchKeyboardHelper.ShowTouchKeyboard();
            tb.PreviewMouseLeftButtonDown += (s, e) => TouchKeyboardHelper.ShowTouchKeyboard();
            tb.PreviewTouchDown += (s, e) => TouchKeyboardHelper.ShowTouchKeyboard();
        }

        private void OnInputChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing) return;

            Model.Title = string.IsNullOrWhiteSpace(txtTitleInput.Text) ? "GHI CHÚ" : txtTitleInput.Text;
            Model.BadgeText = string.IsNullOrWhiteSpace(txtBadgeInput.Text) ? "LƯU Ý" : txtBadgeInput.Text;
            Model.Content = string.IsNullOrWhiteSpace(txtContentInput.Text) ? "Nhập nội dung ghi chú tại đây..." : txtContentInput.Text;

            UpdatePreview();
        }

        private void PresetTitle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string title)
            {
                txtTitleInput.Text = title;
            }
        }

        private void PresetBadge_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string badge)
            {
                txtBadgeInput.Text = badge;
            }
        }

        private void cboFonts_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (cboFonts.SelectedItem is SmartNoteFontInfo fontInfo)
            {
                Model.FontFamily = fontInfo.FontFamilyName;
                UpdatePreview();
            }
        }

        private void btnSizeDown_Click(object sender, RoutedEventArgs e)
        {
            if (Model.FontSize > 16)
            {
                Model.FontSize -= 2;
                txtFontSizeDisplay.Text = ((int)Model.FontSize).ToString();
                UpdatePreview();
            }
        }

        private void btnSizeUp_Click(object sender, RoutedEventArgs e)
        {
            if (Model.FontSize < 72)
            {
                Model.FontSize += 2;
                txtFontSizeDisplay.Text = ((int)Model.FontSize).ToString();
                UpdatePreview();
            }
        }

        private void btnBold_Click(object sender, RoutedEventArgs e)
        {
            Model.IsBold = !Model.IsBold;
            UpdateStyleButtonsState();
            UpdatePreview();
        }

        private void btnItalic_Click(object sender, RoutedEventArgs e)
        {
            Model.IsItalic = !Model.IsItalic;
            UpdateStyleButtonsState();
            UpdatePreview();
        }

        private void UpdateStyleButtonsState()
        {
            btnBold.Background = Model.IsBold ? new SolidColorBrush(Color.FromRgb(224, 242, 254)) : Brushes.White;
            btnBold.BorderBrush = Model.IsBold ? new SolidColorBrush(Color.FromRgb(2, 132, 199)) : new SolidColorBrush(Color.FromRgb(203, 213, 225));

            btnItalic.Background = Model.IsItalic ? new SolidColorBrush(Color.FromRgb(224, 242, 254)) : Brushes.White;
            btnItalic.BorderBrush = Model.IsItalic ? new SolidColorBrush(Color.FromRgb(2, 132, 199)) : new SolidColorBrush(Color.FromRgb(203, 213, 225));
        }

        private void ThemeColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string themeStr && Enum.TryParse<SmartNoteThemeType>(themeStr, out var themeType))
            {
                Model.ThemeType = themeType;
                UpdatePreview();
            }
        }

        private void UpdatePreview()
        {
            if (previewCardBorder == null) return;

            var theme = SmartNoteTheme.Get(Model.ThemeType);

            // Nền & Viền
            previewCardBorder.Background = theme.Background;
            previewCardBorder.BorderBrush = theme.BorderBrush;

            // Header
            previewTitle.Text = Model.Title;
            previewTitle.Foreground = theme.TitleColor;

            previewBadgeText.Text = Model.BadgeText;
            previewBadgeText.Foreground = theme.BadgeFg;
            previewBadgeBorder.Background = theme.BadgeBg;

            // Đường chia
            previewDivider.Stroke = theme.DividerBrush;

            // Nội dung
            previewContent.Text = Model.Content;
            previewContent.FontFamily = new FontFamily(Model.FontFamily);
            previewContent.FontSize = Model.FontSize;
            previewContent.FontWeight = Model.IsBold ? FontWeights.Bold : FontWeights.Normal;
            previewContent.FontStyle = Model.IsItalic ? FontStyles.Italic : FontStyles.Normal;
            previewContent.Foreground = theme.TextColor;
            previewContent.LineHeight = Math.Max(20, Model.FontSize * 1.35);
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            Model.Title = string.IsNullOrWhiteSpace(txtTitleInput.Text) ? "GHI CHÚ" : txtTitleInput.Text;
            Model.BadgeText = string.IsNullOrWhiteSpace(txtBadgeInput.Text) ? "LƯU Ý" : txtBadgeInput.Text;
            Model.Content = string.IsNullOrWhiteSpace(txtContentInput.Text) ? "Nhập nội dung ghi chú tại đây..." : txtContentInput.Text;

            TouchKeyboardHelper.HideTouchKeyboard();

            IsConfirmed = true;
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            TouchKeyboardHelper.HideTouchKeyboard();

            IsConfirmed = false;
            DialogResult = false;
            Close();
        }
    }
}
