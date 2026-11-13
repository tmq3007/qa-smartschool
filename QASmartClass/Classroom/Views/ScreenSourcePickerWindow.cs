using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using QASmartClass.Classroom.Services;
using QASmartTouch.Services.Camera;

namespace QASmartClass.Classroom.Views
{
    /// <summary>
    /// Bảng chọn nội dung trình chiếu tới Học sinh (Thiết kế 2 tầng trực quan — Chuẩn Màn cảm ứng 86" & PC):
    /// - Tầng 1 (1 Chạm Là Chiếu Ngay): Toàn màn hình Giáo viên | Bảng viết SMART TOUCH (Giữ cố định) | Khoanh vùng màn hình
    /// - Tầng 2 (Lưới trực quan): Cửa sổ ứng dụng đang mở (PowerPoint, Word, PDF, Trình duyệt...) | Màn hình phụ & Trang SMART CLASS
    /// - Tầng 3 (Thanh đáy gọn sạch): Bật/tắt nhanh Âm thanh máy, Micro, Camera + Hủy / Bắt đầu trình chiếu.
    /// </summary>
    public sealed class ScreenSourcePickerWindow : Window
    {
        private static readonly Brush PrimaryBlue = CreateFrozenBrush(0x1D, 0x4E, 0xD8);
        private static readonly Brush PrimaryBlueBg = CreateFrozenBrush(0xEF, 0xF6, 0xFF);
        private static readonly Brush IndigoAccent = CreateFrozenBrush(0x43, 0x38, 0xCA);
        private static readonly Brush EmeraldAccent = CreateFrozenBrush(0x04, 0x78, 0x57);
        private static readonly Brush SlateBorder = CreateFrozenBrush(0xCB, 0xD5, 0xE1);
        private static readonly Brush SlateSubtleBg = CreateFrozenBrush(0xF8, 0xFA, 0xFC);
        private static readonly Brush TextPrimary = CreateFrozenBrush(0x0F, 0x17, 0x2A);
        private static readonly Brush TextSecondary = CreateFrozenBrush(0x47, 0x55, 0x69);

        private readonly IntPtr _excludeHwnd;
        private readonly Func<ScreenSourceInfo, bool> _isCurrent;
        private readonly WrapPanel _cards = new() { Margin = new Thickness(2) };
        private readonly StackPanel _emptyStatePanel = new()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20, 32, 20, 32),
            Visibility = Visibility.Collapsed
        };
        private readonly TextBlock _emptyTitle = new()
        {
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Foreground = TextPrimary,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };
        private readonly TextBlock _emptyDesc = new()
        {
            FontSize = 13,
            Foreground = TextSecondary,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 520,
            Margin = new Thickness(0, 6, 0, 0)
        };

        private readonly Button _btnShareSelected = new()
        {
            Content = "🚀 Bắt đầu trình chiếu",
            IsEnabled = false
        };

        private readonly CheckBox _chkAutoMinimizeShell = new()
        {
            Content = "Thu nhỏ cửa sổ Quản lý lớp khi chiếu",
            IsChecked = false,
            FontSize = 12,
            FontWeight = FontWeights.Medium,
            Foreground = TextSecondary,
            Margin = new Thickness(0, 6, 0, 0),
            Cursor = Cursors.Hand
        };

        private readonly CheckBox _chkHideShellFromStudents = new()
        {
            Content = "Ẩn cửa sổ Quản lý lớp khỏi màn hình Học sinh (khi chiếu Toàn màn hình)",
            IsChecked = false,
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextPrimary,
            Margin = new Thickness(0, 8, 0, 2),
            Cursor = Cursors.Hand
        };

        private readonly CheckBox _chkAutoOpenQaPage = new()
        {
            Content = "Tự động chuyển sang trang / bảng được chọn để thao tác ngay",
            IsChecked = true,
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextPrimary,
            Margin = new Thickness(0, 4, 0, 0),
            Cursor = Cursors.Hand
        };

        private readonly TabPillButton _tabApps = new("🪟 Ứng dụng đang mở (PowerPoint, Word, Web…)");
        private readonly TabPillButton _tabScreensAndPages = new("🖥️ Màn hình phụ & Trang SMART CLASS");

        private bool _showingAppsTab = true;
        private Border? _selectedCard;
        private int _loadVersion;
        private bool _suppressMediaUiEvents;

        private readonly ToggleButton _btnSysAudio = new();
        private readonly ToggleButton _btnMic = new();
        private readonly ToggleButton _btnCam = new();
        private readonly ComboBox _cmbMic = new();
        private readonly ComboBox _cmbCam = new();
        private readonly Popup _deviceSettingsPopup = new()
        {
            Placement = PlacementMode.Top,
            StaysOpen = false,
            AllowsTransparency = true,
            VerticalOffset = -8
        };

        /// <summary>Nguồn được chọn (null nếu hủy).</summary>
        public ScreenSourceInfo? Selected { get; private set; }

        /// <summary>Có tự động thu nhỏ cửa sổ Quản lý lớp sau khi bấm Chia sẻ hay không.</summary>
        public bool AutoMinimizeClassroomShell { get; private set; }

        /// <summary>Có ẩn cửa sổ Quản lý lớp khỏi luồng chụp màn hình của Học sinh hay không.</summary>
        public bool HideClassroomShellFromStudents { get; private set; }

        public ScreenSourcePickerWindow(
            IntPtr excludeHwnd,
            Func<ScreenSourceInfo, bool> isCurrent,
            ScreenSourceKind initialTab = ScreenSourceKind.Screen,
            bool initialHideClassroomShell = false)
        {
            _excludeHwnd = excludeHwnd;
            _isCurrent = isCurrent;
            _chkHideShellFromStudents.IsChecked = initialHideClassroomShell;

            // Nếu người dùng mở thẳng từ nút "Chiếu trang" thì sang tab Màn hình & Trang; mặc định mở tab Ứng dụng đang mở vì 3 chế độ chính đã nằm ngay ở Tầng 1!
            _showingAppsTab = initialTab != ScreenSourceKind.QaClassPage;

            Title = "Chọn nội dung trình chiếu tới Học sinh";
            double workW = SystemParameters.WorkArea.Width;
            double workH = SystemParameters.WorkArea.Height;
            Width = Math.Min(1040, Math.Max(680, workW * 0.88));
            Height = Math.Min(750, Math.Max(540, workH * 0.88));
            MinWidth = 640;
            MinHeight = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            Background = SlateSubtleBg;
            FontFamily = new FontFamily("Segoe UI");
            ShowInTaskbar = false;

            var root = new Grid { Margin = new Thickness(18, 14, 18, 16) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Header
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Tier 1 Quick-Start Hero Cards
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Tier 2 Section Bar (2 Tabs + Refresh)
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 3: Cards Grid
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 4: Clean Footer

            // ═══ ROW 0: HEADER RÕ RÀNG, DỄ HIỂU ═══
            var headerDock = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 0, 0, 12) };
            var headerTextStack = new StackPanel();
            headerTextStack.Children.Add(new TextBlock
            {
                Text = "📺 Chọn nội dung trình chiếu tới Học sinh",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = TextPrimary
            });
            headerTextStack.Children.Add(new TextBlock
            {
                Text = "Chạm 1 lần vào 1 trong 3 chế độ nhanh bên dưới để phát ngay, hoặc chọn riêng một cửa sổ ứng dụng đang mở.",
                FontSize = 13,
                Foreground = TextSecondary,
                Margin = new Thickness(0, 3, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
            headerDock.Children.Add(headerTextStack);
            Grid.SetRow(headerDock, 0);
            root.Children.Add(headerDock);

            // ═══ ROW 1: TẦNG 1 — 3 THẺ CHẾ ĐỘ GIẢNG DẠY NHANH (1 CHẠM LÀ CHIẾU NGAY) ═══
            var heroGrid = BuildHeroQuickActionsGrid();
            Grid.SetRow(heroGrid, 1);
            root.Children.Add(heroGrid);

            // ═══ ROW 2: THANH CHUYỂN TAB ỨNG DỤNG ĐANG MỞ vs MÀN HÌNH PHỤ & TRANG ═══
            var tabHeaderDock = new DockPanel { LastChildFill = false, Margin = new Thickness(0, 12, 0, 8) };

            var tabsStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _tabApps.Click += (_, _) => SwitchSection(showApps: true);
            _tabScreensAndPages.Click += (_, _) => SwitchSection(showApps: false);
            tabsStack.Children.Add(_tabApps);
            tabsStack.Children.Add(_tabScreensAndPages);
            DockPanel.SetDock(tabsStack, Dock.Left);
            tabHeaderDock.Children.Add(tabsStack);

            var btnRefresh = BuildActionButton("↻ Làm mới danh sách", isPrimary: false, minHeight: 38);
            btnRefresh.Margin = new Thickness(8, 0, 0, 0);
            btnRefresh.Click += (_, _) => RefreshCards();
            DockPanel.SetDock(btnRefresh, Dock.Right);
            tabHeaderDock.Children.Add(btnRefresh);

            Grid.SetRow(tabHeaderDock, 2);
            root.Children.Add(tabHeaderDock);

            // ═══ ROW 3: LƯỚI CỬA SỐ ỨNG DỤNG / MÀN HÌNH ═══
            _emptyStatePanel.Children.Add(_emptyTitle);
            _emptyStatePanel.Children.Add(_emptyDesc);

            var cardsHost = new Grid();
            cardsHost.Children.Add(_cards);
            cardsHost.Children.Add(_emptyStatePanel);

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                PanningMode = PanningMode.VerticalOnly,
                Content = cardsHost
            };

            var cardsFrame = new Border
            {
                Background = Brushes.White,
                BorderBrush = SlateBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10)
            };
            cardsFrame.Child = scroll;
            Grid.SetRow(cardsFrame, 3);
            root.Children.Add(cardsFrame);

            // ═══ ROW 4: THANH ĐÁY GỌN SẠCH (ÂM THANH / CAMERA + NÚT CHÍNH) ═══
            var footerBar = BuildCleanFooterBar();
            Grid.SetRow(footerBar, 4);
            root.Children.Add(footerBar);

            Content = root;

            PreviewKeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    if (_deviceSettingsPopup.IsOpen)
                    {
                        _deviceSettingsPopup.IsOpen = false;
                        e.Handled = true;
                        return;
                    }
                    DialogResult = false;
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter && _btnShareSelected.IsEnabled)
                {
                    ConfirmSelectedCard();
                    e.Handled = true;
                }
            };

            EventHandler mediaHandler = (_, _) => Dispatcher.BeginInvoke(new Action(SyncMediaPillsUi));
            Loaded += (_, _) =>
            {
                LiveBroadcastMediaService.Instance.StateChanged += mediaHandler;
                SyncMediaPillsUi();
                SwitchSection(_showingAppsTab);

                // Nếu vừa bấm từ Khoanh vùng thì mở luôn bộ kéo vùng cho nhanh
                if (initialTab == ScreenSourceKind.Region)
                {
                    Dispatcher.BeginInvoke(new Action(LaunchInteractiveRegionSelector), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            };
            Closed += (_, _) =>
            {
                _deviceSettingsPopup.IsOpen = false;
                LiveBroadcastMediaService.Instance.StateChanged -= mediaHandler;
            };
        }

        private UIElement BuildHeroQuickActionsGrid()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // 1. Toàn màn hình Giáo viên
            var fullScreenCard = CreateHeroCard(
                badgeText: "PHỔ BIẾN NHẤT",
                badgeBg: CreateFrozenBrush(0xDB, 0xEA, 0xFE),
                badgeFg: PrimaryBlue,
                icon: "🖥️",
                title: "Toàn màn hình Giáo viên",
                desc: "Học sinh thấy mọi thao tác, bài giảng hoặc video trên màn hình chính của Thầy/Cô.",
                buttonText: "▶️ Chiếu Toàn màn hình",
                accentBrush: PrimaryBlue,
                extraOption: _chkAutoMinimizeShell,
                onStartClick: SharePrimaryScreenImmediately);
            Grid.SetColumn(fullScreenCard, 0);
            grid.Children.Add(fullScreenCard);

            // 2. Bảng viết tương tác SMART TOUCH (Giữ cố định)
            var smartTouchCard = CreateHeroCard(
                badgeText: "GIỮ CỐ ĐỊNH BẢNG VIẾT",
                badgeBg: CreateFrozenBrush(0xE0, 0xE7, 0xFF),
                badgeFg: IndigoAccent,
                icon: "🎨",
                title: "Bảng viết SMART TOUCH",
                desc: "Tự mở bảng viết. Dù Thầy/Cô chuyển sang màn khác, Học sinh vẫn chỉ thấy bảng viết.",
                buttonText: "🖊️ Mở & Chiếu Bảng viết",
                accentBrush: IndigoAccent,
                extraOption: null,
                onStartClick: ShareSmartTouchImmediately);
            Grid.SetColumn(smartTouchCard, 2);
            grid.Children.Add(smartTouchCard);

            // 3. Khoanh vùng màn hình tùy chọn
            var regionCard = CreateHeroCard(
                badgeText: "CHỌN KHUNG HÌNH",
                badgeBg: CreateFrozenBrush(0xD1, 0xFA, 0xE5),
                badgeFg: EmeraldAccent,
                icon: "✂️",
                title: "Khoanh vùng màn hình",
                desc: "Kéo ngón tay hoặc chuột chọn đúng một vùng chữ nhật trên màn hình để chiếu.",
                buttonText: "✂️ Kéo chọn vùng chiếu…",
                accentBrush: EmeraldAccent,
                extraOption: null,
                onStartClick: LaunchInteractiveRegionSelector);
            Grid.SetColumn(regionCard, 4);
            grid.Children.Add(regionCard);

            return grid;
        }

        private Border CreateHeroCard(
            string badgeText,
            Brush badgeBg,
            Brush badgeFg,
            string icon,
            string title,
            string desc,
            string buttonText,
            Brush accentBrush,
            UIElement? extraOption,
            Action onStartClick)
        {
            var card = new Border
            {
                Background = Brushes.White,
                BorderBrush = SlateBorder,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(14, 12, 14, 12)
            };
            card.Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(0x0F, 0x17, 0x2A),
                BlurRadius = 10,
                ShadowDepth = 2,
                Opacity = 0.05
            };

            var dock = new Grid();
            dock.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            dock.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            dock.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Top: Badge + Icon + Title
            var topStack = new StackPanel();
            var badge = new Border
            {
                Background = badgeBg,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 3, 8, 3),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 8),
                Child = new TextBlock
                {
                    Text = badgeText,
                    FontSize = 10.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = badgeFg
                }
            };
            topStack.Children.Add(badge);

            var titleRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            titleRow.Children.Add(new TextBlock
            {
                Text = icon,
                FontSize = 20,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            titleRow.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = TextPrimary,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            topStack.Children.Add(titleRow);
            Grid.SetRow(topStack, 0);
            dock.Children.Add(topStack);

            // Middle: Description (+ optional compact checkbox)
            var midStack = new StackPanel { Margin = new Thickness(0, 6, 0, 10) };
            midStack.Children.Add(new TextBlock
            {
                Text = desc,
                FontSize = 12.2,
                Foreground = TextSecondary,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 17
            });
            if (extraOption != null)
            {
                midStack.Children.Add(extraOption);
            }
            Grid.SetRow(midStack, 1);
            dock.Children.Add(midStack);

            // Bottom: 1-Tap Action Button (40px touch target)
            var btn = new Button
            {
                Content = buttonText,
                MinHeight = 40,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = accentBrush,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            ApplyRoundedButtonTemplate(btn, 9);
            btn.Click += (_, _) => onStartClick();
            Grid.SetRow(btn, 2);
            dock.Children.Add(btn);

            card.Child = dock;
            return card;
        }

        private UIElement BuildCleanFooterBar()
        {
            var footerBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = SlateBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 12, 0, 0)
            };

            var dock = new DockPanel { LastChildFill = false };

            // LEFT: 3 Media Toggle Pills + Gear button for device selection & advanced options
            var mediaRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            StyleMediaTogglePill(_btnSysAudio);
            _btnSysAudio.ToolTip = "Truyền âm thanh video / bài giảng từ máy tính Giáo viên sang máy Học sinh";
            _btnSysAudio.Click += (_, _) =>
            {
                if (_suppressMediaUiEvents) return;
                LiveBroadcastMediaService.Instance.SetSystemAudioEnabled(_btnSysAudio.IsChecked == true);
                SyncMediaPillsUi();
            };
            mediaRow.Children.Add(_btnSysAudio);

            StyleMediaTogglePill(_btnMic);
            _btnMic.Margin = new Thickness(8, 0, 0, 0);
            _btnMic.ToolTip = "Bật/tắt truyền giọng nói Micro Giáo viên";
            _btnMic.Click += (_, _) =>
            {
                if (_suppressMediaUiEvents) return;
                LiveBroadcastMediaService.Instance.SetMicrophoneEnabled(_btnMic.IsChecked == true);
                SyncMediaPillsUi();
            };
            mediaRow.Children.Add(_btnMic);

            StyleMediaTogglePill(_btnCam);
            _btnCam.Margin = new Thickness(8, 0, 0, 0);
            _btnCam.ToolTip = "Bật/tắt ô hình ảnh Camera Giáo viên trên góc bài giảng";
            _btnCam.Click += async (_, _) =>
            {
                if (_suppressMediaUiEvents) return;
                await LiveBroadcastMediaService.Instance.SetCameraEnabledAsync(_btnCam.IsChecked == true);
                SyncMediaPillsUi();
            };
            mediaRow.Children.Add(_btnCam);

            var btnGear = BuildActionButton("⚙️ Tùy chọn", isPrimary: false, minHeight: 38);
            btnGear.Margin = new Thickness(8, 0, 0, 0);
            btnGear.ToolTip = "Chọn thiết bị Micro, Camera và tùy chọn nâng cao";
            _deviceSettingsPopup.PlacementTarget = btnGear;
            _deviceSettingsPopup.Child = BuildAdvancedDevicePopoverContent();
            btnGear.Click += (_, _) => _deviceSettingsPopup.IsOpen = !_deviceSettingsPopup.IsOpen;
            mediaRow.Children.Add(btnGear);

            DockPanel.SetDock(mediaRow, Dock.Left);
            dock.Children.Add(mediaRow);

            // RIGHT: Cancel + Share Selected Card
            var rightButtons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var btnCancel = BuildActionButton("Hủy", isPrimary: false, minHeight: 42);
            btnCancel.MinWidth = 96;
            btnCancel.Click += (_, _) => DialogResult = false;
            rightButtons.Children.Add(btnCancel);

            StyleActionButton(_btnShareSelected, isPrimary: true, minHeight: 42);
            _btnShareSelected.MinWidth = 185;
            _btnShareSelected.Margin = new Thickness(10, 0, 0, 0);
            _btnShareSelected.Click += (_, _) => ConfirmSelectedCard();
            rightButtons.Children.Add(_btnShareSelected);

            DockPanel.SetDock(rightButtons, Dock.Right);
            dock.Children.Add(rightButtons);

            footerBorder.Child = dock;
            return footerBorder;
        }

        private UIElement BuildAdvancedDevicePopoverContent()
        {
            var box = new Border
            {
                Width = 420,
                Background = Brushes.White,
                BorderBrush = SlateBorder,
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16, 14, 16, 14),
                Margin = new Thickness(8)
            };
            box.Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(0x0F, 0x17, 0x2A),
                BlurRadius = 18,
                ShadowDepth = 4,
                Opacity = 0.18
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = "⚙️ Thiết bị Âm thanh, Camera & Tùy chọn nâng cao",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = TextPrimary,
                Margin = new Thickness(0, 0, 0, 10)
            });

            stack.Children.Add(new TextBlock
            {
                Text = "Thiết bị Micro:",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextSecondary,
                Margin = new Thickness(0, 0, 0, 4)
            });
            _cmbMic.Height = 34;
            _cmbMic.FontSize = 12.5;
            _cmbMic.DisplayMemberPath = "FriendlyName";
            _cmbMic.SelectionChanged += (_, _) =>
            {
                if (_suppressMediaUiEvents) return;
                if (_cmbMic.SelectedItem is AudioEndpointOption opt)
                {
                    LiveBroadcastMediaService.Instance.SelectMicrophone(opt.DeviceId);
                }
            };
            stack.Children.Add(_cmbMic);

            stack.Children.Add(new TextBlock
            {
                Text = "Thiết bị Camera:",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextSecondary,
                Margin = new Thickness(0, 10, 0, 4)
            });
            _cmbCam.Height = 34;
            _cmbCam.FontSize = 12.5;
            _cmbCam.DisplayMemberPath = "FriendlyName";
            _cmbCam.SelectionChanged += async (_, _) =>
            {
                if (_suppressMediaUiEvents) return;
                if (_cmbCam.SelectedItem is CameraEndpointOption cam)
                {
                    await LiveBroadcastMediaService.Instance.SelectCameraAsync(cam.DeviceId);
                }
            };
            stack.Children.Add(_cmbCam);

            stack.Children.Add(new Border
            {
                Height = 1,
                Background = CreateFrozenBrush(0xE2, 0xE8, 0xF0),
                Margin = new Thickness(0, 12, 0, 4)
            });

            stack.Children.Add(_chkHideShellFromStudents);
            stack.Children.Add(_chkAutoOpenQaPage);

            box.Child = stack;
            return box;
        }

        private void SharePrimaryScreenImmediately()
        {
            var screens = ScreenSourceEnumerator.GetScreens();
            var primary = screens.FirstOrDefault(s => s.IsPrimary) ?? screens.FirstOrDefault();
            if (primary == null)
            {
                primary = new ScreenSourceInfo
                {
                    Kind = ScreenSourceKind.Screen,
                    Title = "Toàn màn hình Giáo viên",
                    X = 0,
                    Y = 0,
                    Width = (int)SystemParameters.PrimaryScreenWidth,
                    Height = (int)SystemParameters.PrimaryScreenHeight,
                    IsPrimary = true
                };
            }
            CommitAndClose(primary);
        }

        private void ShareSmartTouchImmediately()
        {
            var smartTouchSrc = new ScreenSourceInfo
            {
                Kind = ScreenSourceKind.QaClassPage,
                PageFormId = "SMART_TOUCH",
                IconGlyph = "🎨",
                Title = "Bảng trắng tương tác SMART TOUCH",
                Width = (int)SystemParameters.PrimaryScreenWidth,
                Height = (int)SystemParameters.PrimaryScreenHeight,
                AutoNavigateToPage = true
            };
            CommitAndClose(smartTouchSrc);
        }

        private void CommitAndClose(ScreenSourceInfo info)
        {
            if (info.Kind == ScreenSourceKind.QaClassPage && string.IsNullOrEmpty(info.PageFormId))
            {
                info = new ScreenSourceInfo
                {
                    Kind = info.Kind,
                    Title = info.Title,
                    Subtitle = info.Subtitle,
                    IconGlyph = info.IconGlyph,
                    Hwnd = info.Hwnd,
                    PageFormId = "CURRENT",
                    Width = info.Width,
                    Height = info.Height,
                    Thumbnail = info.Thumbnail
                };
            }
            if (info.Kind == ScreenSourceKind.QaClassPage && !string.Equals(info.PageFormId, "SMART_TOUCH", StringComparison.OrdinalIgnoreCase))
            {
                info.AutoNavigateToPage = _chkAutoOpenQaPage.IsChecked == true;
            }
            else if (info.Kind == ScreenSourceKind.QaClassPage)
            {
                info.AutoNavigateToPage = true;
            }

            AutoMinimizeClassroomShell = _chkAutoMinimizeShell.IsChecked == true && info.Kind == ScreenSourceKind.Screen;
            HideClassroomShellFromStudents = _chkHideShellFromStudents.IsChecked == true;
            Selected = info;
            DialogResult = true;
        }

        private void ConfirmSelectedCard()
        {
            if (_selectedCard?.Tag is ScreenSourceInfo info)
            {
                CommitAndClose(info);
            }
        }

        private void SwitchSection(bool showApps)
        {
            _showingAppsTab = showApps;
            _tabApps.IsChecked = showApps;
            _tabScreensAndPages.IsChecked = !showApps;
            RefreshCards();
        }

        private void RefreshCards()
        {
            _selectedCard = null;
            _btnShareSelected.IsEnabled = false;
            _btnShareSelected.Content = "🚀 Bắt đầu trình chiếu";
            _cards.Children.Clear();
            _emptyStatePanel.Visibility = Visibility.Collapsed;

            int version = ++_loadVersion;
            var dpi = VisualTreeHelper.GetDpi(this);

            List<ScreenSourceInfo> items;
            if (_showingAppsTab)
            {
                items = ScreenSourceEnumerator.GetWindows(_excludeHwnd);
            }
            else
            {
                items = new List<ScreenSourceInfo>();
                items.AddRange(ScreenSourceEnumerator.GetScreens());
                items.AddRange(ScreenSourceEnumerator.GetQaClassPages());
                items.AddRange(ScreenSourceEnumerator.GetRegionPresets(dpi.DpiScaleX, dpi.DpiScaleY));
            }

            if (items.Count == 0)
            {
                _emptyTitle.Text = _showingAppsTab
                    ? "Hiện chưa có cửa sổ ứng dụng ngoài nào đang mở"
                    : "Không tìm thấy màn hình hoặc trang bổ sung";
                _emptyDesc.Text = _showingAppsTab
                    ? "Hãy mở sẵn PowerPoint, Word, PDF hoặc Trình duyệt rồi bấm '↻ Làm mới danh sách', hoặc bấm trực tiếp vào '▶️ Chiếu Toàn màn hình' / '🖊️ Mở & Chiếu Bảng viết' ở ngay hàng trên."
                    : "Vui lòng chọn chế độ trình chiếu nhanh ở hàng trên.";
                _emptyStatePanel.Visibility = Visibility.Visible;
                return;
            }

            int screenNo = 0;
            foreach (var it in items)
            {
                string label = (it.Kind == ScreenSourceKind.Screen && string.IsNullOrWhiteSpace(it.Title))
                    ? $"Màn hình {++screenNo}{(it.IsPrimary ? " (Chính)" : "")} — {it.Width}×{it.Height}"
                    : it.Title;

                string kindBadgeText = it.Kind switch
                {
                    ScreenSourceKind.Window => "📌 CỬA SỔ CỐ ĐỊNH",
                    ScreenSourceKind.Screen => "🖥️ TOÀN MÀN HÌNH",
                    ScreenSourceKind.Region => "✂️ VÙNG DỰNG SẴN",
                    _ => "📌 CỐ ĐỊNH TRANG / BẢNG"
                };

                var previewBox = new Border
                {
                    Background = CreateFrozenBrush(0x0F, 0x17, 0x2A),
                    CornerRadius = new CornerRadius(8),
                    Height = 142,
                    Margin = new Thickness(0, 0, 0, 8),
                    ClipToBounds = true
                };

                var thumb = new Image { Stretch = Stretch.Uniform };
                RenderOptions.SetBitmapScalingMode(thumb, BitmapScalingMode.HighQuality);

                var placeholderStack = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                placeholderStack.Children.Add(new TextBlock
                {
                    Text = it.IconGlyph,
                    FontSize = 32,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                placeholderStack.Children.Add(new TextBlock
                {
                    Text = "Đang tải xem trước…",
                    FontSize = 11,
                    Foreground = CreateFrozenBrush(0x94, 0xA3, 0xB8),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                });

                var topLeftBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(205, 15, 23, 42)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(7, 3, 7, 3),
                    Margin = new Thickness(6),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Child = new TextBlock
                    {
                        Text = kindBadgeText,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = CreateFrozenBrush(0x93, 0xC5, 0xFD)
                    }
                };

                var previewGrid = new Grid();
                previewGrid.Children.Add(placeholderStack);
                previewGrid.Children.Add(thumb);
                previewGrid.Children.Add(topLeftBadge);
                previewBox.Child = previewGrid;

                // Tải ảnh xem trước bất đồng bộ để hộp thoại bật lên tức thì (<50ms), không đơ giao diện
                var captured = it;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (version != _loadVersion || !IsLoaded) return;
                    var bmp = ScreenSourceEnumerator.CaptureSourcePreview(captured, Owner);
                    if (bmp != null)
                    {
                        thumb.Source = bmp;
                        placeholderStack.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        if (placeholderStack.Children.Count > 1 && placeholderStack.Children[1] is TextBlock phTxt)
                        {
                            phTxt.Text = kindBadgeText;
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);

                var titleBlock = new TextBlock
                {
                    Text = label,
                    FontSize = 13.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = TextPrimary,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    ToolTip = label
                };

                var bottomActionRow = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 6, 0, 0) };
                var btnQuickShareCard = new Button
                {
                    Content = "▶️ Chiếu ngay",
                    MinHeight = 32,
                    Padding = new Thickness(10, 4, 10, 4),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Background = PrimaryBlue,
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };
                ApplyRoundedButtonTemplate(btnQuickShareCard, 7);
                btnQuickShareCard.Click += (_, ev) =>
                {
                    ev.Handled = true;
                    CommitAndClose(captured);
                };
                DockPanel.SetDock(btnQuickShareCard, Dock.Right);
                bottomActionRow.Children.Add(btnQuickShareCard);

                bottomActionRow.Children.Add(new TextBlock
                {
                    Text = _isCurrent(it) ? "● Đang được chọn" : (string.IsNullOrWhiteSpace(it.Subtitle) ? "Giữ cố định nguồn này" : it.Subtitle),
                    FontSize = 11.5,
                    Foreground = _isCurrent(it) ? PrimaryBlue : TextSecondary,
                    FontWeight = _isCurrent(it) ? FontWeights.Bold : FontWeights.Normal,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(0, 0, 8, 0)
                });

                var cardStack = new StackPanel();
                cardStack.Children.Add(previewBox);
                cardStack.Children.Add(titleBlock);
                cardStack.Children.Add(bottomActionRow);

                var card = new Border
                {
                    Width = 294,
                    Margin = new Thickness(6),
                    Padding = new Thickness(10),
                    CornerRadius = new CornerRadius(12),
                    BorderThickness = new Thickness(2),
                    BorderBrush = SlateBorder,
                    Background = Brushes.White,
                    Cursor = Cursors.Hand,
                    Focusable = true,
                    Tag = it,
                    Child = cardStack
                };

                if (_isCurrent(it))
                {
                    SelectCard(card);
                }

                card.MouseLeftButtonUp += (_, e) =>
                {
                    SelectCard(card);
                    if (e.ClickCount >= 2) ConfirmSelectedCard();
                };
                card.TouchUp += (_, _) => SelectCard(card);
                card.KeyDown += (_, e) =>
                {
                    if (e.Key == Key.Space)
                    {
                        SelectCard(card);
                        e.Handled = true;
                    }
                };
                _cards.Children.Add(card);
            }

            if (_selectedCard == null && _cards.Children.Count > 0 && _cards.Children[0] is Border firstCard)
            {
                SelectCard(firstCard);
            }
        }

        private void SelectCard(Border card)
        {
            if (_selectedCard != null)
            {
                _selectedCard.BorderBrush = SlateBorder;
                _selectedCard.Background = Brushes.White;
            }
            _selectedCard = card;
            card.BorderBrush = PrimaryBlue;
            card.Background = PrimaryBlueBg;
            _btnShareSelected.IsEnabled = true;
            if (card.Tag is ScreenSourceInfo info && !string.IsNullOrWhiteSpace(info.Title))
            {
                string shortName = info.Title.Length > 26 ? info.Title.Substring(0, 26) + "…" : info.Title;
                _btnShareSelected.Content = $"🚀 Chiếu: {shortName}";
            }
            else
            {
                _btnShareSelected.Content = "🚀 Bắt đầu trình chiếu";
            }
        }

        private void SyncMediaPillsUi()
        {
            _suppressMediaUiEvents = true;
            try
            {
                var svc = LiveBroadcastMediaService.Instance;

                bool sysOn = svc.HasSystemAudioHardware && svc.IsSystemAudioEnabled;
                _btnSysAudio.IsEnabled = svc.HasSystemAudioHardware;
                _btnSysAudio.IsChecked = sysOn;
                _btnSysAudio.Content = !svc.HasSystemAudioHardware
                    ? "🔇 Không loa"
                    : (sysOn ? "🔊 Âm thanh: BẬT" : "🔈 Âm thanh: TẮT");
                UpdateMediaPillColors(_btnSysAudio, sysOn);

                bool micOn = svc.HasMicrophoneHardware && svc.IsMicrophoneEnabled;
                _btnMic.IsEnabled = svc.HasMicrophoneHardware;
                _btnMic.IsChecked = micOn;
                _btnMic.Content = !svc.HasMicrophoneHardware
                    ? "🚫 Không Mic"
                    : (micOn ? "🎙️ Micro: BẬT" : "🔇 Micro: TẮT");
                UpdateMediaPillColors(_btnMic, micOn);

                _cmbMic.ItemsSource = svc.AvailableMicrophones;
                _cmbMic.IsEnabled = svc.HasMicrophoneHardware;
                if (svc.HasMicrophoneHardware)
                {
                    _cmbMic.SelectedItem = svc.AvailableMicrophones.FirstOrDefault(m => m.DeviceId == svc.SelectedMicrophoneId)
                                        ?? svc.AvailableMicrophones.FirstOrDefault();
                }

                bool camAvail = svc.HasCameraHardware || svc.IsScanningCameras;
                bool camOn = svc.HasCameraHardware && svc.IsCameraEnabled;
                _btnCam.IsEnabled = camAvail;
                _btnCam.IsChecked = camOn;
                _btnCam.Content = svc.IsScanningCameras && !svc.HasCameraHardware
                    ? "📷 Đang dò..."
                    : (!svc.HasCameraHardware ? "🚫 Không Cam" : (camOn ? "📷 Camera: BẬT" : "📷 Camera: TẮT"));
                UpdateMediaPillColors(_btnCam, camOn);

                _cmbCam.ItemsSource = svc.AvailableCameras;
                _cmbCam.IsEnabled = svc.HasCameraHardware;
                if (svc.HasCameraHardware)
                {
                    _cmbCam.SelectedItem = svc.AvailableCameras.FirstOrDefault(c => c.DeviceId == svc.SelectedCameraId)
                                        ?? svc.AvailableCameras.FirstOrDefault();
                }
            }
            finally
            {
                _suppressMediaUiEvents = false;
            }
        }

        private static void StyleMediaTogglePill(ToggleButton btn)
        {
            btn.MinHeight = 38;
            btn.Padding = new Thickness(12, 6, 12, 6);
            btn.FontSize = 12.5;
            btn.FontWeight = FontWeights.SemiBold;
            btn.BorderThickness = new Thickness(1.2);
            btn.Cursor = Cursors.Hand;
            ApplyRoundedButtonTemplate(btn, 9);
        }

        private static void UpdateMediaPillColors(ToggleButton btn, bool isActive)
        {
            if (!btn.IsEnabled)
            {
                btn.Background = CreateFrozenBrush(0xF1, 0xF5, 0xF9);
                btn.Foreground = CreateFrozenBrush(0x94, 0xA3, 0xB8);
                btn.BorderBrush = CreateFrozenBrush(0xE2, 0xE8, 0xF0);
                return;
            }
            if (isActive)
            {
                btn.Background = CreateFrozenBrush(0xDB, 0xEA, 0xFE);
                btn.Foreground = PrimaryBlue;
                btn.BorderBrush = PrimaryBlue;
            }
            else
            {
                btn.Background = CreateFrozenBrush(0xF8, 0xFA, 0xFC);
                btn.Foreground = TextSecondary;
                btn.BorderBrush = SlateBorder;
            }
        }

        private void LaunchInteractiveRegionSelector()
        {
            Visibility = Visibility.Hidden;
            try
            {
                var selector = new ScreenRegionSelectorWindow();
                if (selector.ShowDialog() == true && selector.SelectedRegion != null)
                {
                    AutoMinimizeClassroomShell = _chkAutoMinimizeShell.IsChecked == true;
                    HideClassroomShellFromStudents = _chkHideShellFromStudents.IsChecked == true;
                    Selected = selector.SelectedRegion;
                    DialogResult = true;
                    return;
                }
            }
            finally
            {
                if (DialogResult != true)
                {
                    Visibility = Visibility.Visible;
                    Activate();
                }
            }
        }

        private static Button BuildActionButton(string text, bool isPrimary, double minHeight = 40)
        {
            var b = new Button { Content = text };
            StyleActionButton(b, isPrimary, minHeight);
            return b;
        }

        private static void StyleActionButton(Button b, bool isPrimary, double minHeight)
        {
            b.MinHeight = minHeight;
            b.Padding = new Thickness(14, 7, 14, 7);
            b.FontSize = 13;
            b.FontWeight = FontWeights.SemiBold;
            b.Cursor = Cursors.Hand;
            b.Background = isPrimary ? PrimaryBlue : CreateFrozenBrush(0xF1, 0xF5, 0xF9);
            b.Foreground = isPrimary ? Brushes.White : TextPrimary;
            b.BorderBrush = isPrimary ? PrimaryBlue : SlateBorder;
            b.BorderThickness = new Thickness(1);
            ApplyRoundedButtonTemplate(b, 9);
        }

        private static void ApplyRoundedButtonTemplate(ButtonBase btn, double radius)
        {
            var template = new ControlTemplate(btn.GetType());
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "bdr";
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
            borderFactory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            borderFactory.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            borderFactory.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            borderFactory.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });

            var cpFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            cpFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cpFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(cpFactory);

            template.VisualTree = borderFactory;

            var disabledTrigger = new Trigger { Property = IsEnabledProperty, Value = false };
            disabledTrigger.Setters.Add(new Setter(OpacityProperty, 0.5));
            template.Triggers.Add(disabledTrigger);

            btn.Template = template;
        }

        private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        private sealed class TabPillButton : ToggleButton
        {
            public TabPillButton(string text)
            {
                Content = text;
                MinHeight = 40;
                Padding = new Thickness(16, 7, 16, 7);
                Margin = new Thickness(0, 0, 8, 0);
                FontSize = 13;
                FontWeight = FontWeights.SemiBold;
                Cursor = Cursors.Hand;
                BorderThickness = new Thickness(1.5);
                ApplyRoundedButtonTemplate(this, 9);
                Checked += (_, _) =>
                {
                    Background = PrimaryBlueBg;
                    Foreground = PrimaryBlue;
                    BorderBrush = PrimaryBlue;
                    FontWeight = FontWeights.Bold;
                };
                Unchecked += (_, _) =>
                {
                    Background = Brushes.White;
                    Foreground = TextPrimary;
                    BorderBrush = SlateBorder;
                    FontWeight = FontWeights.SemiBold;
                };
                Background = Brushes.White;
                Foreground = TextPrimary;
                BorderBrush = SlateBorder;
            }
        }
    }
}
