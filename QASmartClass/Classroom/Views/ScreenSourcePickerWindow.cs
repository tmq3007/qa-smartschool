using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.Classroom.Services;

namespace QASmartClass.Classroom.Views
{
    /// <summary>
    /// Hộp chọn nguồn chiếu kiểu Google Meet: tab "Màn hình" / "Cửa sổ", mỗi nguồn là một thẻ có ảnh xem trước.
    /// Dùng được bằng chuột, bàn phím (Tab/Enter/Esc) và cảm ứng (thẻ lớn, cuộn bằng ngón tay).
    /// </summary>
    public sealed class ScreenSourcePickerWindow : Window
    {
        private static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(0x19, 0x76, 0xD2));
        private static readonly Brush AccentBg = new SolidColorBrush(Color.FromRgb(0xE3, 0xF2, 0xFD));
        private static readonly Brush Line = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));

        private readonly IntPtr _excludeHwnd;
        private readonly Func<ScreenSourceInfo, bool> _isCurrent;
        private readonly WrapPanel _cards = new() { Margin = new Thickness(4) };
        private readonly Button _btnShare = new() { Content = "Chia sẻ", IsEnabled = false };
        private readonly TextBlock _empty = new() { Foreground = Brushes.Gray, FontSize = 14, Margin = new Thickness(12), Visibility = Visibility.Collapsed };
        private readonly ToggleButton_ _tabScreen = new("🖥️ Toàn màn hình");
        private readonly ToggleButton_ _tabWindow = new("🪟 Cửa sổ ứng dụng");

        private ScreenSourceKind _tab = ScreenSourceKind.Screen;
        private Border? _selectedCard;
        private int _loadVersion;

        /// <summary>Nguồn được chọn (null nếu hủy).</summary>
        public ScreenSourceInfo? Selected { get; private set; }

        public ScreenSourcePickerWindow(IntPtr excludeHwnd, Func<ScreenSourceInfo, bool> isCurrent)
        {
            _excludeHwnd = excludeHwnd;
            _isCurrent = isCurrent;

            Title = "Chọn màn hình để chiếu";
            Width = 900; Height = 640; MinWidth = 520; MinHeight = 420;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Brushes.White;
            FontFamily = new FontFamily("Segoe UI");
            ShowInTaskbar = false;

            var root = new Grid { Margin = new Thickness(18) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var title = new TextBlock { Text = "Chọn màn hình để chiếu", FontSize = 20, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0x21, 0x21, 0x21)) };
            Grid.SetRow(title, 0);
            root.Children.Add(title);

            var tabs = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 10) };
            _tabScreen.Click += (_, _) => SwitchTab(ScreenSourceKind.Screen);
            _tabWindow.Click += (_, _) => SwitchTab(ScreenSourceKind.Window);
            tabs.Children.Add(_tabScreen);
            tabs.Children.Add(_tabWindow);
            tabs.Visibility = Visibility.Collapsed; // chỉ chọn màn hình, không chọn cửa sổ
            Grid.SetRow(tabs, 1);
            root.Children.Add(tabs);

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                PanningMode = PanningMode.VerticalOnly,
                Content = new StackPanel { Children = { _empty, _cards } }
            };
            var frame = new Border { BorderBrush = Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Child = scroll };
            Grid.SetRow(frame, 2);
            root.Children.Add(frame);

            var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
            var btnRefresh = MakeButton("↻ Làm mới", false);
            btnRefresh.ToolTip = "Quét lại danh sách (khi bạn vừa mở thêm ứng dụng)";
            btnRefresh.Click += (_, _) => SwitchTab(_tab);
            var btnCancel = MakeButton("Hủy", false);
            btnCancel.Click += (_, _) => { Selected = null; DialogResult = false; };
            StyleButton(_btnShare, true);
            _btnShare.Click += (_, _) => Confirm();
            footer.Children.Add(btnRefresh);
            footer.Children.Add(btnCancel);
            footer.Children.Add(_btnShare);
            Grid.SetRow(footer, 3);
            root.Children.Add(footer);

            Content = root;
            PreviewKeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape) { Selected = null; DialogResult = false; e.Handled = true; }
                else if (e.Key == Key.Enter && _btnShare.IsEnabled) { Confirm(); e.Handled = true; }
            };
            Loaded += (_, _) => SwitchTab(ScreenSourceKind.Screen);
        }

        private void Confirm()
        {
            if (_selectedCard?.Tag is ScreenSourceInfo info)
            {
                Selected = info;
                DialogResult = true;
            }
        }

        private void SwitchTab(ScreenSourceKind kind)
        {
            _tab = kind;
            _tabScreen.IsChecked = kind == ScreenSourceKind.Screen;
            _tabWindow.IsChecked = kind == ScreenSourceKind.Window;
            _selectedCard = null;
            _btnShare.IsEnabled = false;
            _cards.Children.Clear();
            _empty.Visibility = Visibility.Collapsed;

            int version = ++_loadVersion;
            List<ScreenSourceInfo> items = kind == ScreenSourceKind.Screen
                ? ScreenSourceEnumerator.GetScreens()
                : ScreenSourceEnumerator.GetWindows(_excludeHwnd);

            if (items.Count == 0)
            {
                _empty.Text = kind == ScreenSourceKind.Screen ? "Không tìm thấy màn hình nào." : "Không có cửa sổ ứng dụng nào đang mở để chiếu.";
                _empty.Visibility = Visibility.Visible;
                return;
            }

            int screenNo = 0;
            foreach (var it in items)
            {
                string label = it.Kind == ScreenSourceKind.Screen
                    ? $"Màn hình {++screenNo}{(it.IsPrimary ? " (chính)" : "")} — {it.Width}×{it.Height}"
                    : it.Title;
                var thumb = new Image { Stretch = Stretch.Uniform, Height = 130, Margin = new Thickness(0, 0, 0, 6) };
                RenderOptions.SetBitmapScalingMode(thumb, BitmapScalingMode.HighQuality);
                var hold = new Border { Background = new SolidColorBrush(Color.FromRgb(0x0D, 0x1B, 0x2A)), CornerRadius = new CornerRadius(6), Height = 130, Child = thumb };
                var name = new TextBlock { Text = label, FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = label, FontWeight = FontWeights.SemiBold };

                var card = new Border
                {
                    Width = 230, MinHeight = 44, Margin = new Thickness(6),
                    Padding = new Thickness(8), CornerRadius = new CornerRadius(10),
                    BorderThickness = new Thickness(2), BorderBrush = Line, Background = Brushes.White,
                    Cursor = Cursors.Hand, Focusable = true, Tag = it,
                    Child = new StackPanel { Children = { hold, name } }
                };
                if (_isCurrent(it)) name.Text += "  ● đang chiếu";
                card.MouseLeftButtonUp += (_, e) => { SelectCard(card); if (e.ClickCount >= 2) Confirm(); };
                card.TouchUp += (_, _) => SelectCard(card);
                card.KeyDown += (_, e) => { if (e.Key == Key.Space) { SelectCard(card); e.Handled = true; } };
                _cards.Children.Add(card);

                var captured = it;
                Task.Run(() =>
                {
                    var bmp = ScreenSourceEnumerator.CaptureThumbnail(captured);
                    if (bmp == null) return;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (version == _loadVersion) thumb.Source = bmp;
                    }));
                });
            }
        }

        private void SelectCard(Border card)
        {
            if (_selectedCard != null) { _selectedCard.BorderBrush = Line; _selectedCard.Background = Brushes.White; }
            _selectedCard = card;
            card.BorderBrush = Accent;
            card.Background = AccentBg;
            _btnShare.IsEnabled = true;
            card.Focus();
        }

        private static Button MakeButton(string text, bool primary)
        {
            var b = new Button { Content = text };
            StyleButton(b, primary);
            return b;
        }

        private static void StyleButton(Button b, bool primary)
        {
            b.MinWidth = 110; b.MinHeight = 44; b.Padding = new Thickness(18, 8, 18, 8);
            b.Margin = new Thickness(8, 0, 0, 0); b.FontSize = 14; b.FontWeight = FontWeights.SemiBold;
            b.Cursor = Cursors.Hand;
            b.Background = primary ? Accent : new SolidColorBrush(Color.FromRgb(0xEC, 0xEF, 0xF1));
            b.Foreground = primary ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x37, 0x47, 0x4F));
            b.BorderThickness = new Thickness(0);
        }

        /// <summary>Tab dạng nút chọn, đủ lớn để chạm.</summary>
        private sealed class ToggleButton_ : System.Windows.Controls.Primitives.ToggleButton
        {
            public ToggleButton_(string text)
            {
                Content = text; MinHeight = 44; Padding = new Thickness(18, 8, 18, 8);
                Margin = new Thickness(0, 0, 8, 0); FontSize = 14; FontWeight = FontWeights.SemiBold; Cursor = Cursors.Hand;
                BorderThickness = new Thickness(1);
                BorderBrush = Line;
                Checked += (_, _) => { Background = AccentBg; Foreground = Accent; BorderBrush = Accent; };
                Unchecked += (_, _) => { Background = Brushes.White; Foreground = Brushes.Black; BorderBrush = Line; };
                Background = Brushes.White;
            }
        }
    }
}
