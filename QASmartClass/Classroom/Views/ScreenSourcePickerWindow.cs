using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.Classroom.Services;
using QASmartTouch.Services.Camera;

namespace QASmartClass.Classroom.Views
{
    /// <summary>
    /// Hộp chọn nguồn chiếu nâng cao:
    /// 1. Toàn màn hình
    /// 2. Chọn vùng màn hình (Kéo chọn khu vực bất kỳ hoặc chọn vùng dựng sẵn)
    /// 3. Trang bên trong QA SmartClass (Chiếu trực tiếp trang bài học / SMART TOUCH / Công cụ STEM...)
    /// 4. Cửa sổ ứng dụng
    /// Dùng được bằng chuột, bàn phím (Tab/Enter/Esc) và màn hình cảm ứng phòng học.
    /// </summary>
    public sealed class ScreenSourcePickerWindow : Window
    {
        private static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(0x19, 0x76, 0xD2));
        private static readonly Brush AccentBg = new SolidColorBrush(Color.FromRgb(0xE3, 0xF2, 0xFD));
        private static readonly Brush Line = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));

        private readonly IntPtr _excludeHwnd;
        private readonly Func<ScreenSourceInfo, bool> _isCurrent;
        private readonly StackPanel _topExtraPanel = new() { Margin = new Thickness(6, 4, 6, 8), Visibility = Visibility.Collapsed };
        private readonly WrapPanel _cards = new() { Margin = new Thickness(4) };
        private readonly Button _btnShare = new() { Content = "🚀 Chia sẻ nguồn này", IsEnabled = false };
        private readonly CheckBox _chkAutoOpenQaPage = new()
        {
            Content = "Tự động mở trang này trên màn hình Giáo viên để thao tác ngay",
            IsChecked = true,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)),
            Margin = new Thickness(4, 2, 4, 2),
            Visibility = Visibility.Collapsed
        };
        private readonly CheckBox _chkAutoMinimizeShell = new()
        {
            Content = "Tự động thu nhỏ cửa sổ Quản lý lớp khi bắt đầu chiếu (để lộ bài giảng / màn hình chính)",
            IsChecked = false,
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)),
            Margin = new Thickness(4, 2, 4, 2)
        };
        private readonly CheckBox _chkHideShellFromStudents = new()
        {
            Content = "Ẩn cửa sổ Quản lý lớp khỏi màn hình Học sinh (Học sinh chỉ thấy nội dung phía sau cửa sổ QASmartClass)",
            IsChecked = false,
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)),
            Margin = new Thickness(4, 2, 4, 2)
        };
        private readonly TextBlock _empty = new() { Foreground = Brushes.Gray, FontSize = 14, Margin = new Thickness(12), Visibility = Visibility.Collapsed };

        private readonly ToggleButton_ _tabAll = new("🌟 Tất cả");
        private readonly ToggleButton_ _tabQaPage = new("📑 Trang / Bảng viết (Giữ cố định)");
        private readonly ToggleButton_ _tabWindow = new("🪟 Cửa sổ ứng dụng (Giữ cố định)");
        private readonly ToggleButton_ _tabScreen = new("🖥️ Toàn màn hình");
        private readonly ToggleButton_ _tabRegion = new("✂️ Chọn vùng màn hình");

        private ScreenSourceKind _tab = ScreenSourceKind.Screen;
        private bool _isAllTab = true;
        private Border? _selectedCard;
        private int _loadVersion;
        private bool _suppressMediaUiEvents;

        private readonly ToggleButton _btnMeetSysAudio = new();
        private readonly ToggleButton _btnMeetMic = new();
        private readonly ComboBox _cmbMeetMic = new();
        private readonly ToggleButton _btnMeetCam = new();
        private readonly ComboBox _cmbMeetCam = new();

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
            _tab = initialTab;
            _isAllTab = (initialTab == ScreenSourceKind.Screen);
            _chkHideShellFromStudents.IsChecked = initialHideClassroomShell;

            Title = "Chọn nguồn chiếu màn hình & trang bài giảng";
            Width = 1020; Height = 740; MinWidth = 560; MinHeight = 480;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Brushes.White;
            FontFamily = new FontFamily("Segoe UI");
            ShowInTaskbar = false;

            var root = new Grid { Margin = new Thickness(18) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var headerStack = new StackPanel { Orientation = Orientation.Vertical };
            headerStack.Children.Add(new TextBlock
            {
                Text = "Chọn nội dung trình chiếu tới Học sinh",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A))
            });
            headerStack.Children.Add(new TextBlock
            {
                Text = "Chuẩn Google Meet: Chọn 'Trang / Bảng viết' hoặc 'Cửa sổ ứng dụng' để giữ cố định màn đó (chuyển sang màn khác Học sinh vẫn chỉ thấy màn đã chọn). Chọn 'Toàn màn hình' khi muốn chiếu mọi thao tác.",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 0, 0)
            });
            Grid.SetRow(headerStack, 0);
            root.Children.Add(headerStack);

            var tabs = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 10) };
            _tabAll.Click += (_, _) => { _isAllTab = true; SwitchTab(ScreenSourceKind.Screen); };
            _tabQaPage.Click += (_, _) => { _isAllTab = false; SwitchTab(ScreenSourceKind.QaClassPage); };
            _tabWindow.Click += (_, _) => { _isAllTab = false; SwitchTab(ScreenSourceKind.Window); };
            _tabScreen.Click += (_, _) => { _isAllTab = false; SwitchTab(ScreenSourceKind.Screen); };
            _tabRegion.Click += (_, _) => { _isAllTab = false; SwitchTab(ScreenSourceKind.Region); };
            tabs.Children.Add(_tabAll);
            tabs.Children.Add(_tabQaPage);
            tabs.Children.Add(_tabWindow);
            tabs.Children.Add(_tabScreen);
            tabs.Children.Add(_tabRegion);
            Grid.SetRow(tabs, 1);
            root.Children.Add(tabs);

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                PanningMode = PanningMode.VerticalOnly,
                Content = new StackPanel { Children = { _topExtraPanel, _empty, _cards } }
            };
            var frame = new Border
            {
                BorderBrush = Line,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC)),
                Padding = new Thickness(6),
                Child = scroll
            };
            Grid.SetRow(frame, 2);
            root.Children.Add(frame);

            var meetAvStrip = BuildGoogleMeetAvStrip();
            Grid.SetRow(meetAvStrip, 3);
            root.Children.Add(meetAvStrip);

            var footerGrid = new Grid { Margin = new Thickness(0, 12, 0, 0) };
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var leftOptsStack = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center };
            leftOptsStack.Children.Add(_chkAutoOpenQaPage);
            leftOptsStack.Children.Add(_chkAutoMinimizeShell);
            leftOptsStack.Children.Add(_chkHideShellFromStudents);
            Grid.SetColumn(leftOptsStack, 0);
            footerGrid.Children.Add(leftOptsStack);

            var footerBtns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            var btnCustomRegion = MakeButton("✂️ Kéo chọn vùng màn hình…", false);
            btnCustomRegion.Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xF2, 0xFE));
            btnCustomRegion.Foreground = new SolidColorBrush(Color.FromRgb(0x03, 0x69, 0xA1));
            btnCustomRegion.Click += (_, _) => LaunchInteractiveRegionSelector();

            var btnRefresh = MakeButton("↻ Làm mới", false);
            btnRefresh.ToolTip = "Quét lại danh sách màn hình, cửa sổ, Micro và Camera";
            btnRefresh.Click += (_, _) =>
            {
                LiveBroadcastMediaService.Instance.RefreshAudioHardwareCatalog();
                _ = LiveBroadcastMediaService.Instance.RefreshCameraHardwareCatalogAsync();
                SwitchTab(_tab);
            };

            var btnCancel = MakeButton("Hủy", false);
            btnCancel.Click += (_, _) => { Selected = null; DialogResult = false; };

            StyleButton(_btnShare, true);
            _btnShare.Click += (_, _) => Confirm();

            footerBtns.Children.Add(btnCustomRegion);
            footerBtns.Children.Add(btnRefresh);
            footerBtns.Children.Add(btnCancel);
            footerBtns.Children.Add(_btnShare);
            Grid.SetColumn(footerBtns, 1);
            footerGrid.Children.Add(footerBtns);

            Grid.SetRow(footerGrid, 4);
            root.Children.Add(footerGrid);

            Content = root;
            PreviewKeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape) { Selected = null; DialogResult = false; e.Handled = true; }
                else if (e.Key == Key.Enter && _btnShare.IsEnabled) { Confirm(); e.Handled = true; }
            };
            EventHandler mediaStateHandler = (_, _) => Dispatcher.BeginInvoke(new Action(SyncGoogleMeetAvStripUi));
            LiveBroadcastMediaService.Instance.StateChanged += mediaStateHandler;
            Closed += (_, _) => LiveBroadcastMediaService.Instance.StateChanged -= mediaStateHandler;
            Loaded += (_, _) =>
            {
                LiveBroadcastMediaService.Instance.RefreshAudioHardwareCatalog();
                SyncGoogleMeetAvStripUi();
                SwitchTab(_tab);
            };
        }

        private Border BuildGoogleMeetAvStrip()
        {
            var container = new Border
            {
                Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(0x20, 0x21, 0x24)), // Google Meet Surface #202124
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x3C, 0x40, 0x43)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(14, 10, 14, 10)
            };

            var wrap = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 1. Nút bật/tắt Chia sẻ âm thanh hệ thống (Chuẩn Google Meet "Also share system audio")
            StyleMeetPillToggle(_btnMeetSysAudio);
            _btnMeetSysAudio.Click += (_, _) =>
            {
                if (_suppressMediaUiEvents) return;
                LiveBroadcastMediaService.Instance.SetSystemAudioEnabled(_btnMeetSysAudio.IsChecked == true);
                SyncGoogleMeetAvStripUi();
            };
            wrap.Children.Add(_btnMeetSysAudio);

            // 2. Nút Micro + Chọn nguồn Micro
            StyleMeetPillToggle(_btnMeetMic);
            _btnMeetMic.Click += (_, _) =>
            {
                if (_suppressMediaUiEvents) return;
                LiveBroadcastMediaService.Instance.SetMicrophoneEnabled(_btnMeetMic.IsChecked == true);
                SyncGoogleMeetAvStripUi();
            };
            wrap.Children.Add(_btnMeetMic);

            _cmbMeetMic.Width = 205;
            _cmbMeetMic.MinHeight = 36;
            _cmbMeetMic.Margin = new Thickness(0, 2, 10, 2);
            _cmbMeetMic.VerticalContentAlignment = VerticalAlignment.Center;
            _cmbMeetMic.FontSize = 12.5;
            _cmbMeetMic.ToolTip = "Chọn nguồn Micro thu tiếng giảng bài của Giáo viên";
            _cmbMeetMic.SelectionChanged += (_, _) =>
            {
                if (_suppressMediaUiEvents) return;
                if (_cmbMeetMic.SelectedItem is AudioEndpointOption opt)
                {
                    LiveBroadcastMediaService.Instance.SelectMicrophone(opt.DeviceId);
                }
            };
            wrap.Children.Add(_cmbMeetMic);

            // 3. Nút Camera + Chọn Camera
            StyleMeetPillToggle(_btnMeetCam);
            _btnMeetCam.Click += async (_, _) =>
            {
                if (_suppressMediaUiEvents) return;
                await LiveBroadcastMediaService.Instance.SetCameraEnabledAsync(_btnMeetCam.IsChecked == true);
                SyncGoogleMeetAvStripUi();
            };
            wrap.Children.Add(_btnMeetCam);

            _cmbMeetCam.Width = 195;
            _cmbMeetCam.MinHeight = 36;
            _cmbMeetCam.Margin = new Thickness(0, 2, 0, 2);
            _cmbMeetCam.VerticalContentAlignment = VerticalAlignment.Center;
            _cmbMeetCam.FontSize = 12.5;
            _cmbMeetCam.ToolTip = "Chọn thiết bị Camera Giáo viên hiển thị góc màn hình";
            _cmbMeetCam.SelectionChanged += async (_, _) =>
            {
                if (_suppressMediaUiEvents) return;
                if (_cmbMeetCam.SelectedItem is CameraEndpointOption cam)
                {
                    await LiveBroadcastMediaService.Instance.SelectCameraAsync(cam.DeviceId);
                }
            };
            wrap.Children.Add(_cmbMeetCam);

            container.Child = wrap;
            return container;
        }

        private static void StyleMeetPillToggle(ToggleButton btn)
        {
            btn.MinHeight = 38;
            btn.Padding = new Thickness(13, 6, 13, 6);
            btn.Margin = new Thickness(0, 2, 8, 2);
            btn.FontSize = 12.5;
            btn.FontWeight = FontWeights.SemiBold;
            btn.Cursor = Cursors.Hand;
            btn.BorderThickness = new Thickness(1);
        }

        private void SyncGoogleMeetAvStripUi()
        {
            _suppressMediaUiEvents = true;
            try
            {
                var svc = LiveBroadcastMediaService.Instance;

                // System Audio Pill
                _btnMeetSysAudio.IsEnabled = svc.HasSystemAudioHardware;
                _btnMeetSysAudio.IsChecked = svc.HasSystemAudioHardware && svc.IsSystemAudioEnabled;
                _btnMeetSysAudio.Content = !svc.HasSystemAudioHardware
                    ? "🔇 Không có loa hệ thống"
                    : (svc.IsSystemAudioEnabled ? "🔊 Chia sẻ âm thanh hệ thống: BẬT" : "🔈 Âm thanh hệ thống: TẮT");
                ApplyMeetPillVisual(_btnMeetSysAudio, svc.HasSystemAudioHardware && svc.IsSystemAudioEnabled, isSystemAudio: true);

                // Microphone Pill + Selector
                _btnMeetMic.IsEnabled = svc.HasMicrophoneHardware;
                _btnMeetMic.IsChecked = svc.HasMicrophoneHardware && svc.IsMicrophoneEnabled;
                _btnMeetMic.Content = !svc.HasMicrophoneHardware
                    ? "🚫 Không có Micro"
                    : (svc.IsMicrophoneEnabled ? "🎙️ Micro: BẬT" : "🔇 Micro: TẮT");
                ApplyMeetPillVisual(_btnMeetMic, svc.HasMicrophoneHardware && svc.IsMicrophoneEnabled, isSystemAudio: false);

                _cmbMeetMic.ItemsSource = svc.AvailableMicrophones;
                _cmbMeetMic.IsEnabled = svc.HasMicrophoneHardware;
                if (svc.HasMicrophoneHardware)
                {
                    _cmbMeetMic.SelectedItem = svc.AvailableMicrophones.FirstOrDefault(m => m.DeviceId == svc.SelectedMicrophoneId)
                                            ?? svc.AvailableMicrophones.FirstOrDefault();
                }

                // Camera Pill + Selector
                bool camAvailable = svc.HasCameraHardware || svc.IsScanningCameras;
                _btnMeetCam.IsEnabled = camAvailable;
                _btnMeetCam.IsChecked = svc.HasCameraHardware && svc.IsCameraEnabled;
                _btnMeetCam.Content = svc.IsScanningCameras && !svc.HasCameraHardware
                    ? "📷 Đang dò Camera..."
                    : (!svc.HasCameraHardware ? "🚫 Không có Camera" : (svc.IsCameraEnabled ? "📷 Camera: BẬT" : "📷 Camera: TẮT"));
                ApplyMeetPillVisual(_btnMeetCam, svc.HasCameraHardware && svc.IsCameraEnabled, isSystemAudio: false);

                _cmbMeetCam.ItemsSource = svc.AvailableCameras;
                _cmbMeetCam.IsEnabled = svc.HasCameraHardware;
                if (svc.HasCameraHardware)
                {
                    _cmbMeetCam.SelectedItem = svc.AvailableCameras.FirstOrDefault(c => c.DeviceId == svc.SelectedCameraId)
                                            ?? svc.AvailableCameras.FirstOrDefault();
                }
            }
            finally
            {
                _suppressMediaUiEvents = false;
            }
        }

        private static void ApplyMeetPillVisual(ToggleButton btn, bool isActive, bool isSystemAudio)
        {
            if (!btn.IsEnabled)
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(0x30, 0x31, 0x34));
                btn.Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0xA0, 0xA6));
                btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x5F, 0x63, 0x68));
                return;
            }

            if (isActive)
            {
                btn.Background = isSystemAudio
                    ? new SolidColorBrush(Color.FromRgb(0x1A, 0x73, 0xE8)) // Google Meet Active Blue
                    : new SolidColorBrush(Color.FromRgb(0x3C, 0x40, 0x43)); // Google Meet Active Pill
                btn.Foreground = Brushes.White;
                btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0xB4, 0xF8));
            }
            else
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(0xEA, 0x43, 0x35)); // Google Meet Muted Red #EA4335
                btn.Foreground = Brushes.White;
                btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0xEA, 0x43, 0x35));
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

        private void Confirm()
        {
            if (_selectedCard?.Tag is ScreenSourceInfo info)
            {
                if (info.Kind == ScreenSourceKind.QaClassPage)
                {
                    info.AutoNavigateToPage = _chkAutoOpenQaPage.IsChecked == true;
                }
                AutoMinimizeClassroomShell = _chkAutoMinimizeShell.IsChecked == true;
                HideClassroomShellFromStudents = _chkHideShellFromStudents.IsChecked == true;
                Selected = info;
                DialogResult = true;
            }
        }

        private void SwitchTab(ScreenSourceKind kind)
        {
            _tab = kind;
            _tabAll.IsChecked = _isAllTab && kind == ScreenSourceKind.Screen;
            _tabScreen.IsChecked = !_isAllTab && kind == ScreenSourceKind.Screen;
            _tabRegion.IsChecked = !_isAllTab && kind == ScreenSourceKind.Region;
            _tabQaPage.IsChecked = !_isAllTab && kind == ScreenSourceKind.QaClassPage;
            _tabWindow.IsChecked = !_isAllTab && kind == ScreenSourceKind.Window;

            _chkAutoOpenQaPage.Visibility = kind == ScreenSourceKind.QaClassPage ? Visibility.Visible : Visibility.Collapsed;
            _chkAutoMinimizeShell.Visibility = kind == ScreenSourceKind.QaClassPage ? Visibility.Collapsed : Visibility.Visible;
            _chkHideShellFromStudents.Visibility = (kind == ScreenSourceKind.Screen || _isAllTab) ? Visibility.Visible : Visibility.Collapsed;
            _topExtraPanel.Children.Clear();
            _topExtraPanel.Visibility = Visibility.Collapsed;

            _selectedCard = null;
            _btnShare.IsEnabled = false;
            _cards.Children.Clear();
            _empty.Visibility = Visibility.Collapsed;

            int version = ++_loadVersion;
            var dpi = VisualTreeHelper.GetDpi(this);

            if (!_isAllTab && (kind == ScreenSourceKind.Window || kind == ScreenSourceKind.QaClassPage))
            {
                var lockBanner = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0xEC, 0xFD, 0xF5)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x6E, 0xE7, 0xB7)),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(14, 10, 14, 10)
                };
                lockBanner.Child = new TextBlock
                {
                    Text = "📌 Cơ chế Giữ cố định chuẩn Google Meet: Học sinh CHỈ nhìn thấy đúng Cửa sổ hoặc Trang bạn chọn dưới đây. Dù bạn Alt+Tab sang ứng dụng khác, thu nhỏ cửa sổ, hay chuyển chế độ làm việc, màn hình Học sinh vẫn giữ cố định nguồn này.",
                    FontSize = 12.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x06, 0x5F, 0x46)),
                    TextWrapping = TextWrapping.Wrap
                };
                _topExtraPanel.Children.Add(lockBanner);
                _topExtraPanel.Visibility = Visibility.Visible;
            }
            else if (!_isAllTab && kind == ScreenSourceKind.Screen)
            {
                var screenBanner = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xFB, 0xEB)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0xFC, 0xD3, 0x4D)),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(14, 10, 14, 10)
                };
                screenBanner.Child = new TextBlock
                {
                    Text = "🖥️ Chế độ Toàn màn hình: Học sinh sẽ nhìn thấy mọi cửa sổ mở trên màn hình này. Nếu bạn chỉ muốn chiếu cố định 1 ứng dụng hoặc 1 bảng viết (chuyển màn khác không bị đổi theo), hãy chọn tab '📑 Trang / Bảng viết (Giữ cố định)' hoặc '🪟 Cửa sổ ứng dụng (Giữ cố định)'.",
                    FontSize = 12.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x92, 0x40, 0x0E)),
                    TextWrapping = TextWrapping.Wrap
                };
                _topExtraPanel.Children.Add(screenBanner);
                _topExtraPanel.Visibility = Visibility.Visible;
            }
            else if (kind == ScreenSourceKind.Region)
            {
                // Banner nút to để kéo vẽ vùng chọn trực tiếp trên màn hình
                var drawBanner = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0xEF, 0xF6, 0xFF)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x93, 0xC5, 0xFD)),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(16, 12, 16, 12)
                };
                var bannerGrid = new Grid();
                bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var txtStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                txtStack.Children.Add(new TextBlock
                {
                    Text = "✂️ Kéo chọn vùng bất kỳ trên màn hình (Hỗ trợ chuột & cảm ứng)",
                    FontSize = 15,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x1E, 0x3A, 0x8A))
                });
                txtStack.Children.Add(new TextBlock
                {
                    Text = "Bấm nút bên phải để vẽ khung hình chữ nhật quanh phần màn hình, bài giảng hoặc video bạn muốn học sinh nhìn thấy.",
                    FontSize = 12.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)),
                    Margin = new Thickness(0, 2, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                });
                Grid.SetColumn(txtStack, 0);
                bannerGrid.Children.Add(txtStack);

                var btnDrawNow = MakeButton("✂️ Kéo chọn vùng ngay", true);
                btnDrawNow.Click += (_, _) => LaunchInteractiveRegionSelector();
                Grid.SetColumn(btnDrawNow, 1);
                bannerGrid.Children.Add(btnDrawNow);

                drawBanner.Child = bannerGrid;
                _topExtraPanel.Children.Add(drawBanner);
                _topExtraPanel.Visibility = Visibility.Visible;
            }

            List<ScreenSourceInfo> items = _isAllTab
                ? ScreenSourceEnumerator.GetAllShareableSources(_excludeHwnd, dpi.DpiScaleX, dpi.DpiScaleY)
                : kind switch
                {
                    ScreenSourceKind.Screen => ScreenSourceEnumerator.GetScreens(),
                    ScreenSourceKind.Region => ScreenSourceEnumerator.GetRegionPresets(dpi.DpiScaleX, dpi.DpiScaleY),
                    ScreenSourceKind.QaClassPage => ScreenSourceEnumerator.GetQaClassPages(),
                    _ => ScreenSourceEnumerator.GetWindows(_excludeHwnd)
                };

            if (items.Count == 0)
            {
                _empty.Text = kind == ScreenSourceKind.Screen
                    ? "Không tìm thấy màn hình nào."
                    : "Không có nguồn nào khả dụng trong mục này.";
                _empty.Visibility = Visibility.Visible;
                return;
            }

            int screenNo = 0;
            foreach (var it in items)
            {
                string label = (it.Kind == ScreenSourceKind.Screen && string.IsNullOrWhiteSpace(it.Title))
                    ? $"Màn hình {++screenNo}{(it.IsPrimary ? " (chính)" : "")} — {it.Width}×{it.Height}"
                    : it.Title;

                var hold = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x0D, 0x1B, 0x2A)),
                    CornerRadius = new CornerRadius(8),
                    Height = 136,
                    Margin = new Thickness(0, 0, 0, 8),
                    ClipToBounds = true
                };

                var thumb = new Image { Stretch = Stretch.Uniform };
                RenderOptions.SetBitmapScalingMode(thumb, BitmapScalingMode.HighQuality);

                var overlayGrid = new Grid();
                var iconBadge = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                iconBadge.Children.Add(new TextBlock
                {
                    Text = it.IconGlyph,
                    FontSize = 34,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                iconBadge.Children.Add(new TextBlock
                {
                    Text = it.Kind switch
                    {
                        ScreenSourceKind.Screen => "TOÀN MÀN HÌNH",
                        ScreenSourceKind.Window => "📌 CỬA SỔ CỐ ĐỊNH",
                        ScreenSourceKind.Region => "VÙNG MÀN HÌNH",
                        _ => "📌 TRANG / BẢNG CỐ ĐỊNH"
                    },
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x93, 0xC5, 0xFD)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                });
                overlayGrid.Children.Add(iconBadge);
                overlayGrid.Children.Add(thumb);
                hold.Child = overlayGrid;

                var captured = it;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (version != _loadVersion) return;
                    var bmp = ScreenSourceEnumerator.CaptureSourcePreview(captured, Owner);
                    if (bmp != null)
                    {
                        thumb.Source = bmp;
                        iconBadge.Visibility = Visibility.Collapsed;
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);

                var name = new TextBlock
                {
                    Text = label,
                    FontSize = 13.5,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    ToolTip = label,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A))
                };

                var cardStack = new StackPanel { Children = { hold, name } };
                if (!string.IsNullOrWhiteSpace(it.Subtitle))
                {
                    cardStack.Children.Add(new TextBlock
                    {
                        Text = it.Subtitle,
                        FontSize = 11.5,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)),
                        TextWrapping = TextWrapping.Wrap,
                        MaxHeight = 34,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        Margin = new Thickness(0, 3, 0, 0)
                    });
                }

                var card = new Border
                {
                    Width = 282,
                    MinHeight = 48,
                    Margin = new Thickness(6),
                    Padding = new Thickness(10),
                    CornerRadius = new CornerRadius(10),
                    BorderThickness = new Thickness(2),
                    BorderBrush = Line,
                    Background = Brushes.White,
                    Cursor = Cursors.Hand,
                    Focusable = true,
                    Tag = it,
                    Child = cardStack
                };
                if (_isCurrent(it))
                {
                    name.Text += "  ● đang chọn";
                    SelectCard(card);
                }
                card.MouseLeftButtonUp += (_, e) => { SelectCard(card); if (e.ClickCount >= 2) Confirm(); };
                card.TouchUp += (_, _) => SelectCard(card);
                card.KeyDown += (_, e) => { if (e.Key == Key.Space) { SelectCard(card); e.Handled = true; } };
                _cards.Children.Add(card);
            }

            if (_selectedCard == null && _cards.Children.Count > 0 && _cards.Children[0] is Border firstCard)
            {
                SelectCard(firstCard);
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
            b.MinWidth = 110; b.MinHeight = 44; b.Padding = new Thickness(16, 8, 16, 8);
            b.Margin = new Thickness(8, 0, 0, 0); b.FontSize = 13.5; b.FontWeight = FontWeights.SemiBold;
            b.Cursor = Cursors.Hand;
            b.Background = primary ? Accent : new SolidColorBrush(Color.FromRgb(0xEC, 0xEF, 0xF1));
            b.Foreground = primary ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x37, 0x47, 0x4F));
            b.BorderThickness = new Thickness(0);
        }

        /// <summary>Tab dạng nút chọn, đủ lớn để chạm trên màn hình tương tác.</summary>
        private sealed class ToggleButton_ : System.Windows.Controls.Primitives.ToggleButton
        {
            public ToggleButton_(string text)
            {
                Content = text; MinHeight = 44; Padding = new Thickness(16, 8, 16, 8);
                Margin = new Thickness(0, 0, 8, 6); FontSize = 13.5; FontWeight = FontWeights.SemiBold; Cursor = Cursors.Hand;
                BorderThickness = new Thickness(1.5);
                BorderBrush = Line;
                Checked += (_, _) => { Background = AccentBg; Foreground = Accent; BorderBrush = Accent; };
                Unchecked += (_, _) => { Background = Brushes.White; Foreground = Brushes.Black; BorderBrush = Line; };
                Background = Brushes.White;
            }
        }
    }
}
