using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using QASmartClass.Classroom.Services;
using QASmartTouch.Services.Camera;

namespace QASmartClass.Classroom.Views
{
    /// <summary>
    /// [GOOGLE MEET & MYTHWARE STYLE] Thanh điều khiển trình chiếu nổi (Floating Meet Dock) + Chọn nguồn Âm thanh / Camera.
    /// - Thiết kế chuẩn Google Meet Dark Pill (#202124, nút tròn/viên thuốc #3C4043, trạng thái tắt màu đỏ #EA4335, đang phát xanh #8AB4F8).
    /// - Cho phép Bật/Tắt Micro, Bật/Tắt Âm thanh hệ thống, Bật/Tắt Camera Giáo viên, chọn thiết bị Micro / Loa / Camera trực tiếp khi đang chia sẻ.
    /// - Tự gắn cờ WDA_EXCLUDEFROMCAPTURE (0x11) cho toàn bộ cửa sổ (bao gồm cả bảng chọn thiết bị và khung PiP) để Học sinh KHÔNG BAO GIỜ nhìn thấy thanh điều khiển của Giáo viên.
    /// </summary>
    public sealed class FloatingBroadcastBarWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011U;

        // Google Meet Color Tokens
        private static readonly Color MeetSurfaceColor = Color.FromArgb(248, 0x20, 0x21, 0x24);
        private static readonly Color MeetSubSurfaceColor = Color.FromRgb(0x30, 0x31, 0x34);
        private static readonly Color MeetPillDarkColor = Color.FromRgb(0x3C, 0x40, 0x43);
        private static readonly Color MeetBorderColor = Color.FromRgb(0x5F, 0x63, 0x68);
        private static readonly Color MeetMutedRedColor = Color.FromRgb(0xEA, 0x43, 0x35);
        private static readonly Color MeetActiveBlueColor = Color.FromRgb(0x1A, 0x73, 0xE8);
        private static readonly Color MeetLightBlueColor = Color.FromRgb(0x8A, 0xB4, 0xF8);
        private static readonly Color MeetLiveGreenColor = Color.FromRgb(0x34, 0xA8, 0x53);
        private static readonly Color MeetWarningAmberColor = Color.FromRgb(0xFB, 0xBC, 0x04);

        private readonly Border _mainBarBorder;
        private readonly Ellipse _statusDot;
        private readonly TextBlock _txtStatusBadge;
        private readonly TextBlock _txtSourceTitle;
        private readonly TextBlock _txtTimerAndStudents;

        // Google Meet Split-Pill: Microphone
        private readonly Border _micSplitPill;
        private readonly Button _btnMicToggle;
        private readonly TextBlock _txtMicIcon;
        private readonly Rectangle _micBar1;
        private readonly Rectangle _micBar2;
        private readonly Rectangle _micBar3;
        private readonly Button _btnMicMenu;

        // Google Meet Pill: System Audio
        private readonly Border _sysAudioPill;
        private readonly Button _btnSysAudioToggle;
        private readonly TextBlock _txtSysAudioLabel;
        private readonly Rectangle _sysBar1;
        private readonly Rectangle _sysBar2;

        // Google Meet Split-Pill: Camera
        private readonly Border _camSplitPill;
        private readonly Button _btnCamToggle;
        private readonly TextBlock _txtCamLabel;
        private readonly Button _btnCamMenu;

        // Action Pills
        private readonly Button _btnShareActiveInstead;
        private readonly Button _btnPauseResume;
        private readonly ToggleButton _btnTogglePiP;

        // Expandable Google Meet Device Settings Popover Card (Audio / Camera)
        private readonly Border _devicePopoverBorder;
        private readonly StackPanel _audioSettingsSection;
        private readonly StackPanel _cameraSettingsSection;
        private readonly ComboBox _cmbMicrophone;
        private readonly ComboBox _cmbSystemOutput;
        private readonly CheckBox _chkPopoverSystemAudio;
        private readonly Border _micMeterFill;
        private readonly Border _sysMeterFill;
        private readonly ComboBox _cmbCamera;
        private readonly ComboBox _cmbCamShape;
        private readonly ComboBox _cmbCamCorner;
        private readonly CheckBox _chkCamMirror;
        private readonly Image _imgSelfCamPreview;
        private readonly TextBlock _txtCamPreviewHint;
        private readonly Border _warningBanner;
        private readonly TextBlock _txtWarningBanner;

        // Collapsible Live Student PiP Monitor
        private readonly Border _pipContainer;
        private readonly Image _pipImage;
        private readonly TextBlock _txtPipOverlayBadge;
        private readonly TextBlock _txtPipSubtitle;
        private readonly CheckBox _chkHideClassroomShell;

        private readonly DispatcherTimer _meterTimer;
        private string _openPopoverMode = ""; // "", "AUDIO", "CAMERA"
        private bool _isPaused;
        private bool _isLockedSource;
        private bool _suppressCheckboxEvent;
        private bool _suppressDeviceSelectionEvent;
        private Point _touchDragStartScreen;
        private bool _isTouchDragging;

        public event EventHandler? ShareCurrentInsteadRequested;
        public event EventHandler? ChangeSourceRequested;
        public event EventHandler? SelectRegionRequested;
        public event EventHandler? TogglePauseRequested;
        public event EventHandler? StopRequested;
        public event EventHandler? RestoreShellRequested;
        public event EventHandler<bool>? HideClassroomShellChanged;

        public FloatingBroadcastBarWindow(bool initialHideClassroomShell = false)
        {
            Title = "Thanh điều khiển trình chiếu QA SmartClass";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.WidthAndHeight;
            FontFamily = new FontFamily("Segoe UI");

            var rootStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(8)
            };

            // ═══════════════════════════════════════════════════════════════════
            // 1. GOOGLE MEET FLOATING PILL DOCK (#202124)
            // ═══════════════════════════════════════════════════════════════════
            _mainBarBorder = new Border
            {
                Background = new SolidColorBrush(MeetSurfaceColor),
                BorderBrush = new SolidColorBrush(MeetBorderColor),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(26),
                Padding = new Thickness(12, 7, 12, 7),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 20,
                    ShadowDepth = 5,
                    Opacity = 0.5
                }
            };

            var barPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            // ─── Cụm kéo di chuyển + Trạng thái phát (Google Meet Info Pill) ───
            var dragStatusZone = new Border
            {
                Background = new SolidColorBrush(MeetSubSurfaceColor),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.SizeAll,
                ToolTip = "Giữ chuột hoặc ngón tay tại đây để kéo thanh điều khiển tới vị trí bất kỳ"
            };

            var statusStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            statusStack.Children.Add(new TextBlock
            {
                Text = "⠿",
                FontSize = 15,
                Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0xA0, 0xA6)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 7, 0)
            });

            _statusDot = new Ellipse
            {
                Width = 9,
                Height = 9,
                Fill = new SolidColorBrush(MeetLiveGreenColor),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            statusStack.Children.Add(_statusDot);

            _txtStatusBadge = new TextBlock
            {
                Text = "ĐANG TRÌNH BÀY",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(MeetLightBlueColor),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            statusStack.Children.Add(_txtStatusBadge);

            _txtSourceTitle = new TextBlock
            {
                Text = "Toàn màn hình",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                MaxWidth = 175,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            statusStack.Children.Add(_txtSourceTitle);

            _txtTimerAndStudents = new TextBlock
            {
                Text = "00:00 • 👥 0",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xDA, 0xDC, 0xE0)),
                VerticalAlignment = VerticalAlignment.Center
            };
            statusStack.Children.Add(_txtTimerAndStudents);

            dragStatusZone.Child = statusStack;
            EnableDragOnElement(dragStatusZone);
            barPanel.Children.Add(dragStatusZone);

            // ─── Google Meet Split-Pill 1: MICROPHONE [ 🎙️ + 3-bar equalizer | ▾ ] ───
            (_micSplitPill, _btnMicToggle, _btnMicMenu) = CreateMeetSplitPill(
                mainTooltip: "Bật / Tắt Micro Giáo viên (giống Google Meet)",
                chevronTooltip: "Chọn thiết bị Micro & Nguồn âm thanh");

            var micContentStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _txtMicIcon = new TextBlock
            {
                Text = "🔇 Mic",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            micContentStack.Children.Add(_txtMicIcon);

            var eqStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 16
            };
            _micBar1 = CreateVoiceBar(4);
            _micBar2 = CreateVoiceBar(4);
            _micBar3 = CreateVoiceBar(4);
            eqStack.Children.Add(_micBar1);
            eqStack.Children.Add(_micBar2);
            eqStack.Children.Add(_micBar3);
            micContentStack.Children.Add(eqStack);
            _btnMicToggle.Content = micContentStack;

            _btnMicToggle.Click += (_, _) =>
            {
                var svc = LiveBroadcastMediaService.Instance;
                svc.SetMicrophoneEnabled(!svc.IsMicrophoneEnabled);
                SyncMediaControlsState();
            };
            _btnMicMenu.Click += (_, _) => ToggleDevicePopover("AUDIO");
            barPanel.Children.Add(_micSplitPill);

            // ─── Google Meet Pill 2: SYSTEM AUDIO [ 🔊 Tiếng máy + sóng âm ] ───
            _sysAudioPill = new Border
            {
                Background = new SolidColorBrush(MeetActiveBlueColor),
                CornerRadius = new CornerRadius(19),
                Height = 38,
                Margin = new Thickness(0, 0, 6, 0),
                ClipToBounds = true
            };
            _btnSysAudioToggle = CreateTransparentInnerButton(
                new Thickness(12, 0, 12, 0),
                "Bật / Tắt chia sẻ âm thanh hệ thống (video, bài nghe trên máy Giáo viên)");
            var sysStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _txtSysAudioLabel = new TextBlock
            {
                Text = "🔊 Tiếng máy",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            sysStack.Children.Add(_txtSysAudioLabel);
            _sysBar1 = CreateVoiceBar(5);
            _sysBar2 = CreateVoiceBar(5);
            sysStack.Children.Add(_sysBar1);
            sysStack.Children.Add(_sysBar2);
            _btnSysAudioToggle.Content = sysStack;
            _btnSysAudioToggle.Click += (_, _) =>
            {
                var svc = LiveBroadcastMediaService.Instance;
                svc.SetSystemAudioEnabled(!svc.IsSystemAudioEnabled);
                SyncMediaControlsState();
            };
            _sysAudioPill.Child = _btnSysAudioToggle;
            barPanel.Children.Add(_sysAudioPill);

            // ─── Google Meet Split-Pill 3: CAMERA [ 📷 Cam | ▾ ] ───
            (_camSplitPill, _btnCamToggle, _btnCamMenu) = CreateMeetSplitPill(
                mainTooltip: "Bật / Tắt Camera Giáo viên hiển thị góc màn hình Học sinh",
                chevronTooltip: "Chọn Camera, kiểu khung & góc hiển thị");

            _txtCamLabel = new TextBlock
            {
                Text = "🚫 Cam",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            _btnCamToggle.Content = _txtCamLabel;
            _btnCamToggle.Click += async (_, _) =>
            {
                var svc = LiveBroadcastMediaService.Instance;
                await svc.SetCameraEnabledAsync(!svc.IsCameraEnabled);
                SyncMediaControlsState();
            };
            _btnCamMenu.Click += (_, _) => ToggleDevicePopover("CAMERA");
            barPanel.Children.Add(_camSplitPill);

            // Vạch ngăn mảnh chuẩn Google Meet
            barPanel.Children.Add(new Rectangle
            {
                Width = 1,
                Height = 24,
                Fill = new SolidColorBrush(MeetBorderColor),
                Margin = new Thickness(2, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            // ─── Cụm nút điều khiển trình bày (Chiếu màn này thay thế [Google Meet], Đổi nguồn, Vùng chọn, Đóng băng, Xem thử, Lớp học) ───
            _btnShareActiveInstead = CreateMeetActionPill("🔄 Chiếu màn này thay thế", MeetActiveBlueColor, Colors.White,
                "Học sinh vẫn đang xem cố định màn hình cũ. Bấm nút này nếu muốn chuyển sang chiếu màn hình hiện tại (Chuẩn Google Meet)");
            _btnShareActiveInstead.Visibility = Visibility.Collapsed;
            _btnShareActiveInstead.Click += (_, _) => ShareCurrentInsteadRequested?.Invoke(this, EventArgs.Empty);
            barPanel.Children.Add(_btnShareActiveInstead);

            var btnChangeSource = CreateMeetActionPill("🖥️ Nguồn chiếu", MeetPillDarkColor, Colors.White,
                "Chọn lại màn hình, cửa sổ ứng dụng, vùng chọn hoặc trang bài giảng");
            btnChangeSource.Click += (_, _) => ChangeSourceRequested?.Invoke(this, EventArgs.Empty);
            barPanel.Children.Add(btnChangeSource);

            var btnRegion = CreateMeetActionPill("✂️ Vùng", MeetPillDarkColor, Colors.White,
                "Kéo khung hình chữ nhật trên màn hình để chỉ chiếu đúng khu vực đó cho Học sinh");
            btnRegion.Click += (_, _) => SelectRegionRequested?.Invoke(this, EventArgs.Empty);
            barPanel.Children.Add(btnRegion);

            _btnPauseResume = CreateMeetActionPill("⏸️ Đóng băng", MeetPillDarkColor, Colors.White,
                "Đóng băng hình ảnh trên máy Học sinh để Giáo viên mở tài liệu riêng tư");
            _btnPauseResume.Click += (_, _) => TogglePauseRequested?.Invoke(this, EventArgs.Empty);
            barPanel.Children.Add(_btnPauseResume);

            _btnTogglePiP = new ToggleButton
            {
                Content = "👁️ Xem thử",
                IsChecked = false,
                Height = 38,
                Padding = new Thickness(12, 0, 12, 0),
                Margin = new Thickness(0, 0, 6, 0),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(MeetPillDarkColor),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                ToolTip = "Ẩn/Hiện màn hình thu nhỏ cho biết chính xác Học sinh đang nhìn thấy gì"
            };
            ApplyRoundedButtonTemplate(_btnTogglePiP, 19);
            _btnTogglePiP.Checked += (_, _) =>
            {
                if (_pipContainer != null) _pipContainer.Visibility = Visibility.Visible;
                _btnTogglePiP.Background = new SolidColorBrush(MeetActiveBlueColor);
                _btnTogglePiP.Content = "👁️ Ẩn xem thử";
            };
            _btnTogglePiP.Unchecked += (_, _) =>
            {
                if (_pipContainer != null) _pipContainer.Visibility = Visibility.Collapsed;
                _btnTogglePiP.Background = new SolidColorBrush(MeetPillDarkColor);
                _btnTogglePiP.Content = "👁️ Xem thử";
            };
            barPanel.Children.Add(_btnTogglePiP);

            var btnRestoreShell = CreateMeetActionPill("🏠 Lớp học", MeetPillDarkColor, Colors.White,
                "Mở lại cửa sổ Quản lý lớp học QA SmartClass");
            btnRestoreShell.Click += (_, _) => RestoreShellRequested?.Invoke(this, EventArgs.Empty);
            barPanel.Children.Add(btnRestoreShell);

            // ─── Nút kết thúc đỏ chuẩn Google Meet (#EA4335) ───
            var btnStop = CreateMeetActionPill("📞 Dừng chia sẻ", MeetMutedRedColor, Colors.White,
                "Dừng chia sẻ màn hình & âm thanh ngay lập tức");
            btnStop.Margin = new Thickness(2, 0, 0, 0);
            btnStop.Padding = new Thickness(15, 0, 15, 0);
            btnStop.Click += (_, _) => StopRequested?.Invoke(this, EventArgs.Empty);
            barPanel.Children.Add(btnStop);

            _mainBarBorder.Child = barPanel;
            rootStack.Children.Add(_mainBarBorder);

            // ═══════════════════════════════════════════════════════════════════
            // 2. GOOGLE MEET AUDIO / CAMERA SETTINGS POPOVER CARD
            //    (Nằm trong cùng cửa sổ WDA_EXCLUDEFROMCAPTURE -> Học sinh không thấy)
            // ═══════════════════════════════════════════════════════════════════
            _devicePopoverBorder = new Border
            {
                Width = 430,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 0),
                Background = new SolidColorBrush(MeetSurfaceColor),
                BorderBrush = new SolidColorBrush(MeetBorderColor),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(16, 14, 16, 14),
                Visibility = Visibility.Collapsed,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 18,
                    ShadowDepth = 4,
                    Opacity = 0.45
                }
            };

            var popoverStack = new StackPanel { Orientation = Orientation.Vertical };

            // Banner cảnh báo thiết bị (ẩn mặc định)
            _warningBanner = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x5C, 0x2B, 0x29)),
                BorderBrush = new SolidColorBrush(MeetMutedRedColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 0, 10),
                Visibility = Visibility.Collapsed
            };
            _txtWarningBanner = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap
            };
            _warningBanner.Child = _txtWarningBanner;
            popoverStack.Children.Add(_warningBanner);

            // Header của Popover (Tab chuyển nhanh Âm thanh / Camera + nút Đóng)
            var popHeader = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            popHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            popHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            popHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            popHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var btnTabAudio = CreateMeetSmallButton("🎙️ Nguồn âm thanh");
            btnTabAudio.Click += (_, _) => ShowDevicePopoverSection("AUDIO");
            Grid.SetColumn(btnTabAudio, 0);
            popHeader.Children.Add(btnTabAudio);

            var btnTabCamera = CreateMeetSmallButton("📷 Cài đặt Camera");
            btnTabCamera.Margin = new Thickness(6, 0, 0, 0);
            btnTabCamera.Click += (_, _) => ShowDevicePopoverSection("CAMERA");
            Grid.SetColumn(btnTabCamera, 1);
            popHeader.Children.Add(btnTabCamera);

            var headRight = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnRescan = CreateMeetSmallButton("↻ Quét lại");
            btnRescan.Click += async (_, _) =>
            {
                LiveBroadcastMediaService.Instance.RefreshAudioHardwareCatalog();
                await LiveBroadcastMediaService.Instance.RefreshCameraHardwareCatalogAsync();
                SyncMediaControlsState();
            };
            headRight.Children.Add(btnRescan);

            var btnClosePop = CreateMeetSmallButton("✕");
            btnClosePop.Margin = new Thickness(6, 0, 0, 0);
            btnClosePop.Click += (_, _) => ToggleDevicePopover("");
            headRight.Children.Add(btnClosePop);
            Grid.SetColumn(headRight, 3);
            popHeader.Children.Add(headRight);
            popoverStack.Children.Add(popHeader);

            // ─── SECTION A: CÀI ĐẶT ÂM THANH (MICRO + HỆ THỐNG) ───
            _audioSettingsSection = new StackPanel { Orientation = Orientation.Vertical };

            _audioSettingsSection.Children.Add(CreateSectionLabel("🎙️ MICRO GIÁO VIÊN"));
            _cmbMicrophone = CreateDarkComboBox();
            _cmbMicrophone.SelectionChanged += (_, _) =>
            {
                if (_suppressDeviceSelectionEvent) return;
                if (_cmbMicrophone.SelectedItem is AudioEndpointOption opt)
                {
                    LiveBroadcastMediaService.Instance.SelectMicrophone(opt.DeviceId);
                }
            };
            _audioSettingsSection.Children.Add(_cmbMicrophone);
            _audioSettingsSection.Children.Add(CreateHorizontalMeter(out _micMeterFill, MeetLiveGreenColor));

            _audioSettingsSection.Children.Add(CreateSectionLabel("🔊 ÂM THANH HỆ THỐNG (VIDEO / BÀI NGHE TRÊN MÁY)", topMargin: 10));
            _chkPopoverSystemAudio = new CheckBox
            {
                Content = "Chia sẻ âm thanh phát ra từ máy Giáo viên tới Học sinh",
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 6),
                Cursor = Cursors.Hand
            };
            _chkPopoverSystemAudio.Click += (_, _) =>
            {
                if (_suppressDeviceSelectionEvent) return;
                LiveBroadcastMediaService.Instance.SetSystemAudioEnabled(_chkPopoverSystemAudio.IsChecked == true);
                SyncMediaControlsState();
            };
            _audioSettingsSection.Children.Add(_chkPopoverSystemAudio);

            _cmbSystemOutput = CreateDarkComboBox();
            _cmbSystemOutput.SelectionChanged += (_, _) =>
            {
                if (_suppressDeviceSelectionEvent) return;
                if (_cmbSystemOutput.SelectedItem is AudioEndpointOption opt)
                {
                    LiveBroadcastMediaService.Instance.SelectSystemOutput(opt.DeviceId);
                }
            };
            _audioSettingsSection.Children.Add(_cmbSystemOutput);
            _audioSettingsSection.Children.Add(CreateHorizontalMeter(out _sysMeterFill, MeetLightBlueColor));

            popoverStack.Children.Add(_audioSettingsSection);

            // ─── SECTION B: CÀI ĐẶT CAMERA GIÁO VIÊN ───
            _cameraSettingsSection = new StackPanel { Orientation = Orientation.Vertical, Visibility = Visibility.Collapsed };
            _cameraSettingsSection.Children.Add(CreateSectionLabel("📷 THIẾT BỊ CAMERA"));
            _cmbCamera = CreateDarkComboBox();
            _cmbCamera.SelectionChanged += async (_, _) =>
            {
                if (_suppressDeviceSelectionEvent) return;
                if (_cmbCamera.SelectedItem is CameraEndpointOption cam)
                {
                    await LiveBroadcastMediaService.Instance.SelectCameraAsync(cam.DeviceId);
                }
            };
            _cameraSettingsSection.Children.Add(_cmbCamera);

            // Khung xem trước Camera của Giáo viên
            var selfPreviewBorder = new Border
            {
                Height = 138,
                Margin = new Thickness(0, 8, 0, 8),
                Background = new SolidColorBrush(Color.FromRgb(0x11, 0x13, 0x15)),
                BorderBrush = new SolidColorBrush(MeetBorderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                ClipToBounds = true
            };
            var selfPreviewGrid = new Grid();
            _txtCamPreviewHint = new TextBlock
            {
                Text = "Camera đang tắt — Bấm nút 📷 Cam trên thanh điều khiển để bật",
                Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0xA0, 0xA6)),
                FontSize = 11.5,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(12)
            };
            selfPreviewGrid.Children.Add(_txtCamPreviewHint);
            _imgSelfCamPreview = new Image
            {
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            selfPreviewGrid.Children.Add(_imgSelfCamPreview);
            selfPreviewBorder.Child = selfPreviewGrid;
            _cameraSettingsSection.Children.Add(selfPreviewBorder);

            var camOptsGrid = new Grid { Margin = new Thickness(0, 2, 0, 6) };
            camOptsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            camOptsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            camOptsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _cmbCamShape = CreateDarkComboBox();
            _cmbCamShape.Items.Add("Khung chữ nhật Meet (16:9)");
            _cmbCamShape.Items.Add("Khung tròn SmartTouch");
            _cmbCamShape.SelectedIndex = 0;
            _cmbCamShape.SelectionChanged += (_, _) =>
            {
                LiveBroadcastMediaService.Instance.CameraShape = _cmbCamShape.SelectedIndex == 1
                    ? MeetCameraShape.SmartTouchCircle
                    : MeetCameraShape.GoogleMeetRect;
            };
            Grid.SetColumn(_cmbCamShape, 0);
            camOptsGrid.Children.Add(_cmbCamShape);

            _cmbCamCorner = CreateDarkComboBox();
            _cmbCamCorner.Items.Add("Vị trí: Góc dưới phải");
            _cmbCamCorner.Items.Add("Vị trí: Góc dưới trái");
            _cmbCamCorner.Items.Add("Vị trí: Góc trên phải");
            _cmbCamCorner.SelectedIndex = 0;
            _cmbCamCorner.SelectionChanged += (_, _) =>
            {
                LiveBroadcastMediaService.Instance.CameraCorner = _cmbCamCorner.SelectedIndex switch
                {
                    1 => MeetCameraCorner.BottomLeft,
                    2 => MeetCameraCorner.TopRight,
                    _ => MeetCameraCorner.BottomRight
                };
            };
            Grid.SetColumn(_cmbCamCorner, 2);
            camOptsGrid.Children.Add(_cmbCamCorner);
            _cameraSettingsSection.Children.Add(camOptsGrid);

            _chkCamMirror = new CheckBox
            {
                Content = "Lật gương Camera (giống Google Meet)",
                IsChecked = true,
                Foreground = Brushes.White,
                FontSize = 12,
                Cursor = Cursors.Hand
            };
            _chkCamMirror.Click += (_, _) =>
            {
                LiveBroadcastMediaService.Instance.IsCameraMirrored = _chkCamMirror.IsChecked == true;
            };
            _cameraSettingsSection.Children.Add(_chkCamMirror);

            popoverStack.Children.Add(_cameraSettingsSection);
            _devicePopoverBorder.Child = popoverStack;
            rootStack.Children.Add(_devicePopoverBorder);

            // ═══════════════════════════════════════════════════════════════════
            // 3. COLLAPSIBLE LIVE STUDENT PiP MONITOR
            // ═══════════════════════════════════════════════════════════════════
            _pipContainer = new Border
            {
                Width = 360,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 6, 0, 0),
                Background = new SolidColorBrush(MeetSurfaceColor),
                BorderBrush = new SolidColorBrush(MeetBorderColor),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(10),
                Visibility = Visibility.Collapsed,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 16,
                    ShadowDepth = 4,
                    Opacity = 0.4
                }
            };

            var pipStack = new StackPanel { Orientation = Orientation.Vertical };
            var pipHeader = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            pipHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pipHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pipTitle = new TextBlock
            {
                Text = "📺 MÀN HÌNH HỌC SINH ĐANG NHẬN",
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(MeetLightBlueColor),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(pipTitle, 0);
            pipHeader.Children.Add(pipTitle);

            var btnMinimizePip = CreateMeetSmallButton("Thu gọn ▲");
            btnMinimizePip.Click += (_, _) => _btnTogglePiP.IsChecked = false;
            Grid.SetColumn(btnMinimizePip, 1);
            pipHeader.Children.Add(btnMinimizePip);
            pipStack.Children.Add(pipHeader);

            var videoFrame = new Border
            {
                Height = 184,
                Background = new SolidColorBrush(Color.FromRgb(0x0B, 0x0D, 0x10)),
                BorderBrush = new SolidColorBrush(MeetSubSurfaceColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                ClipToBounds = true
            };

            var videoGrid = new Grid();
            _pipImage = new Image
            {
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(_pipImage, BitmapScalingMode.Linear);
            videoGrid.Children.Add(_pipImage);

            var badgeBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(6),
                Background = new SolidColorBrush(Color.FromArgb(205, 0x20, 0x21, 0x24)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(7, 3, 7, 3)
            };
            _txtPipOverlayBadge = new TextBlock
            {
                Text = "🟢 TRỰC TIẾP TRÊN MÁY HS",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x81, 0xC9, 0x95))
            };
            badgeBorder.Child = _txtPipOverlayBadge;
            videoGrid.Children.Add(badgeBorder);

            videoFrame.Child = videoGrid;
            pipStack.Children.Add(videoFrame);

            _txtPipSubtitle = new TextBlock
            {
                Text = "Đang khởi tạo luồng chụp màn hình...",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0xA0, 0xA6)),
                Margin = new Thickness(2, 6, 2, 4),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            pipStack.Children.Add(_txtPipSubtitle);

            _chkHideClassroomShell = new CheckBox
            {
                Content = "Ẩn cửa sổ Quản lý lớp khỏi màn hình Học sinh",
                IsChecked = initialHideClassroomShell,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xEA, 0xED)),
                Margin = new Thickness(2, 2, 2, 0),
                Cursor = Cursors.Hand
            };
            _chkHideClassroomShell.Checked += (_, _) =>
            {
                if (_suppressCheckboxEvent) return;
                HideClassroomShellChanged?.Invoke(this, true);
            };
            _chkHideClassroomShell.Unchecked += (_, _) =>
            {
                if (_suppressCheckboxEvent) return;
                HideClassroomShellChanged?.Invoke(this, false);
            };
            pipStack.Children.Add(_chkHideClassroomShell);

            _pipContainer.Child = pipStack;
            rootStack.Children.Add(_pipContainer);

            Content = rootStack;

            // Timer 80ms cập nhật sóng âm Google Meet & Camera Self-Preview mượt mà
            _meterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
            _meterTimer.Tick += OnMeterTimerTick;

            EventHandler stateHandler = (_, _) => Dispatcher.BeginInvoke(new Action(SyncMediaControlsState));
            EventHandler<string> warnHandler = (_, msg) => Dispatcher.BeginInvoke(new Action(() => ShowHardwareWarning(msg)));

            LiveBroadcastMediaService.Instance.StateChanged += stateHandler;
            LiveBroadcastMediaService.Instance.HardwareWarning += warnHandler;

            SourceInitialized += OnSourceInitialized;
            Loaded += (_, _) =>
            {
                SyncMediaControlsState();
                PositionTopCenter();
                _meterTimer.Start();
            };
            Closed += (_, _) =>
            {
                _meterTimer.Stop();
                LiveBroadcastMediaService.Instance.StateChanged -= stateHandler;
                LiveBroadcastMediaService.Instance.HardwareWarning -= warnHandler;
            };
        }

        private void ToggleDevicePopover(string mode)
        {
            if (string.IsNullOrEmpty(mode) || _openPopoverMode == mode)
            {
                _openPopoverMode = "";
                _devicePopoverBorder.Visibility = Visibility.Collapsed;
                _warningBanner.Visibility = Visibility.Collapsed;
                return;
            }

            ShowDevicePopoverSection(mode);
        }

        private void ShowDevicePopoverSection(string mode)
        {
            _openPopoverMode = mode;
            _devicePopoverBorder.Visibility = Visibility.Visible;
            _audioSettingsSection.Visibility = mode == "AUDIO" ? Visibility.Visible : Visibility.Collapsed;
            _cameraSettingsSection.Visibility = mode == "CAMERA" ? Visibility.Visible : Visibility.Collapsed;
            SyncMediaControlsState();
        }

        private void ShowHardwareWarning(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            _txtWarningBanner.Text = "⚠️ " + message;
            _warningBanner.Visibility = Visibility.Visible;
            if (_devicePopoverBorder.Visibility != Visibility.Visible)
            {
                ShowDevicePopoverSection("AUDIO");
            }
        }

        private void SyncMediaControlsState()
        {
            _suppressDeviceSelectionEvent = true;
            try
            {
                var svc = LiveBroadcastMediaService.Instance;

                // 1. Microphone Split-Pill
                bool micOn = svc.HasMicrophoneHardware && svc.IsMicrophoneEnabled;
                _btnMicToggle.IsEnabled = svc.HasMicrophoneHardware;
                _micSplitPill.Background = new SolidColorBrush(!svc.HasMicrophoneHardware
                    ? MeetSubSurfaceColor
                    : (micOn ? MeetPillDarkColor : MeetMutedRedColor));
                _txtMicIcon.Text = !svc.HasMicrophoneHardware
                    ? "🚫 Không Mic"
                    : (micOn ? "🎙️ Mic" : "🔇 Tắt mic");

                // 2. System Audio Pill
                bool sysOn = svc.HasSystemAudioHardware && svc.IsSystemAudioEnabled;
                _btnSysAudioToggle.IsEnabled = svc.HasSystemAudioHardware;
                _sysAudioPill.Background = new SolidColorBrush(!svc.HasSystemAudioHardware
                    ? MeetSubSurfaceColor
                    : (sysOn ? MeetActiveBlueColor : MeetMutedRedColor));
                _txtSysAudioLabel.Text = !svc.HasSystemAudioHardware
                    ? "🔇 Không loa"
                    : (sysOn ? "🔊 Tiếng máy" : "🔈 Tắt tiếng máy");

                // 3. Camera Split-Pill
                bool camOn = svc.HasCameraHardware && svc.IsCameraEnabled;
                _btnCamToggle.IsEnabled = svc.HasCameraHardware || svc.IsScanningCameras;
                _camSplitPill.Background = new SolidColorBrush(!svc.HasCameraHardware && !svc.IsScanningCameras
                    ? MeetSubSurfaceColor
                    : (camOn ? MeetPillDarkColor : MeetMutedRedColor));
                _txtCamLabel.Text = svc.IsScanningCameras && !svc.HasCameraHardware
                    ? "📷 Đang dò..."
                    : (!svc.HasCameraHardware ? "🚫 Không Cam" : (camOn ? "📷 Đang bật" : "🚫 Tắt cam"));

                // 4. Sync Dropdowns in Popover
                _cmbMicrophone.ItemsSource = svc.AvailableMicrophones;
                _cmbMicrophone.IsEnabled = svc.HasMicrophoneHardware;
                if (svc.HasMicrophoneHardware)
                {
                    _cmbMicrophone.SelectedItem = svc.AvailableMicrophones.FirstOrDefault(m => m.DeviceId == svc.SelectedMicrophoneId)
                                               ?? svc.AvailableMicrophones.FirstOrDefault();
                }

                _chkPopoverSystemAudio.IsEnabled = svc.HasSystemAudioHardware;
                _chkPopoverSystemAudio.IsChecked = sysOn;
                _cmbSystemOutput.ItemsSource = svc.AvailableSystemOutputs;
                _cmbSystemOutput.IsEnabled = svc.HasSystemAudioHardware;
                if (svc.HasSystemAudioHardware)
                {
                    _cmbSystemOutput.SelectedItem = svc.AvailableSystemOutputs.FirstOrDefault(o => o.DeviceId == svc.SelectedSystemOutputId)
                                                 ?? svc.AvailableSystemOutputs.FirstOrDefault();
                }

                _cmbCamera.ItemsSource = svc.AvailableCameras;
                _cmbCamera.IsEnabled = svc.HasCameraHardware;
                if (svc.HasCameraHardware)
                {
                    _cmbCamera.SelectedItem = svc.AvailableCameras.FirstOrDefault(c => c.DeviceId == svc.SelectedCameraId)
                                           ?? svc.AvailableCameras.FirstOrDefault();
                }

                _cmbCamShape.SelectedIndex = svc.CameraShape == MeetCameraShape.SmartTouchCircle ? 1 : 0;
                _cmbCamCorner.SelectedIndex = svc.CameraCorner switch
                {
                    MeetCameraCorner.BottomLeft => 1,
                    MeetCameraCorner.TopRight => 2,
                    _ => 0
                };
                _chkCamMirror.IsChecked = svc.IsCameraMirrored;
                _txtCamPreviewHint.Text = !svc.HasCameraHardware
                    ? "⚠️ Không tìm thấy thiết bị Camera trên máy Giáo viên."
                    : (camOn ? "Đang khởi tạo hình ảnh Camera..." : "Camera đang tắt — Bấm nút 📷 trên thanh công cụ để bật");
            }
            finally
            {
                _suppressDeviceSelectionEvent = false;
            }
        }

        private void OnMeterTimerTick(object? sender, EventArgs e)
        {
            var svc = LiveBroadcastMediaService.Instance;

            // Animate 3 Google Meet voice equalizer bars on Mic Pill
            double micPct = svc.IsMicrophoneEnabled ? svc.CurrentMicLevelPercent : 0;
            _micBar1.Height = Math.Clamp(3 + micPct * 0.10, 3, 14);
            _micBar2.Height = Math.Clamp(3 + micPct * 0.14, 3, 16);
            _micBar3.Height = Math.Clamp(3 + micPct * 0.09, 3, 13);

            // Animate System Audio bars
            double sysPct = svc.IsSystemAudioEnabled ? svc.CurrentSystemLevelPercent : 0;
            _sysBar1.Height = Math.Clamp(3 + sysPct * 0.12, 3, 15);
            _sysBar2.Height = Math.Clamp(3 + sysPct * 0.09, 3, 13);

            if (_devicePopoverBorder.Visibility == Visibility.Visible)
            {
                double maxMeterW = 390;
                _micMeterFill.Width = Math.Clamp(maxMeterW * (micPct / 100.0), 0, maxMeterW);
                _sysMeterFill.Width = Math.Clamp(maxMeterW * (sysPct / 100.0), 0, maxMeterW);

                if (_cameraSettingsSection.Visibility == Visibility.Visible)
                {
                    var camBmp = svc.LatestCameraPreviewBitmap;
                    if (svc.IsCameraEnabled && camBmp != null)
                    {
                        _imgSelfCamPreview.Source = camBmp;
                        _imgSelfCamPreview.Visibility = Visibility.Visible;
                        _txtCamPreviewHint.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        _imgSelfCamPreview.Source = null;
                        _imgSelfCamPreview.Visibility = Visibility.Collapsed;
                        _txtCamPreviewHint.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private static (Border pill, Button mainBtn, Button chevronBtn) CreateMeetSplitPill(string mainTooltip, string chevronTooltip)
        {
            var pill = new Border
            {
                Background = new SolidColorBrush(MeetMutedRedColor),
                CornerRadius = new CornerRadius(19),
                Height = 38,
                Margin = new Thickness(0, 0, 6, 0),
                ClipToBounds = true
            };

            var stack = new StackPanel { Orientation = Orientation.Horizontal };

            var mainBtn = CreateTransparentInnerButton(new Thickness(12, 0, 8, 0), mainTooltip);
            stack.Children.Add(mainBtn);

            stack.Children.Add(new Rectangle
            {
                Width = 1,
                Height = 20,
                Fill = new SolidColorBrush(Color.FromArgb(95, 255, 255, 255)),
                VerticalAlignment = VerticalAlignment.Center
            });

            var chevBtn = CreateTransparentInnerButton(new Thickness(7, 0, 9, 0), chevronTooltip);
            chevBtn.Content = new TextBlock
            {
                Text = "▾",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            stack.Children.Add(chevBtn);

            pill.Child = stack;
            return (pill, mainBtn, chevBtn);
        }

        private static Button CreateTransparentInnerButton(Thickness padding, string tooltip)
        {
            var btn = new Button
            {
                Padding = padding,
                Height = 38,
                Cursor = Cursors.Hand,
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                ToolTip = tooltip
            };
            ApplyRoundedButtonTemplate(btn, 0);
            return btn;
        }

        private static Rectangle CreateVoiceBar(double initialHeight)
        {
            return new Rectangle
            {
                Width = 3,
                Height = initialHeight,
                RadiusX = 1.5,
                RadiusY = 1.5,
                Margin = new Thickness(1.2, 0, 1.2, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Fill = Brushes.White
            };
        }

        private static Button CreateMeetActionPill(string text, Color bg, Color fg, string tooltip)
        {
            var btn = new Button
            {
                Content = text,
                Height = 38,
                Padding = new Thickness(12, 0, 12, 0),
                Margin = new Thickness(0, 0, 6, 0),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(bg),
                Foreground = new SolidColorBrush(fg),
                BorderThickness = new Thickness(0),
                ToolTip = tooltip
            };
            ApplyRoundedButtonTemplate(btn, 19);
            return btn;
        }

        private static Button CreateMeetSmallButton(string text)
        {
            var btn = new Button
            {
                Content = text,
                Height = 28,
                Padding = new Thickness(10, 0, 10, 0),
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(MeetPillDarkColor),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            ApplyRoundedButtonTemplate(btn, 14);
            return btn;
        }

        private static void ApplyRoundedButtonTemplate(ButtonBase btn, double cornerRadius)
        {
            var template = new ControlTemplate(btn.GetType());
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(cornerRadius));
            borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentFactory);

            template.VisualTree = borderFactory;
            btn.Template = template;
        }

        private static TextBlock CreateSectionLabel(string text, double topMargin = 0)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(MeetLightBlueColor),
                Margin = new Thickness(0, topMargin, 0, 5)
            };
        }

        private static ComboBox CreateDarkComboBox()
        {
            return new ComboBox
            {
                MinHeight = 34,
                FontSize = 12,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6),
                Cursor = Cursors.Hand
            };
        }

        private static Border CreateHorizontalMeter(out Border fillBar, Color fillColor)
        {
            var track = new Border
            {
                Height = 6,
                Background = new SolidColorBrush(MeetSubSurfaceColor),
                CornerRadius = new CornerRadius(3),
                ClipToBounds = true,
                Margin = new Thickness(0, 0, 0, 2)
            };
            fillBar = new Border
            {
                Width = 0,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(fillColor),
                CornerRadius = new CornerRadius(3)
            };
            track.Child = fillBar;
            return track;
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    // Ẩn thanh điều khiển nổi Google Meet & toàn bộ bảng chọn thiết bị khỏi ảnh chụp màn hình Học sinh
                    SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
                }
            }
            catch { }
        }

        public void PositionTopCenter()
        {
            try
            {
                var workArea = SystemParameters.WorkArea;
                double w = ActualWidth > 0 ? ActualWidth : 980;
                Left = workArea.Left + Math.Max(12, (workArea.Width - w) / 2.0);
                Top = workArea.Top + 6;
            }
            catch { }
        }

        private void EnableDragOnElement(UIElement element)
        {
            element.MouseLeftButtonDown += (_, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    try { DragMove(); } catch { }
                }
            };

            element.TouchDown += (_, e) =>
            {
                try
                {
                    var pt = e.GetTouchPoint(this).Position;
                    _touchDragStartScreen = PointToScreen(pt);
                    _isTouchDragging = true;
                    element.CaptureTouch(e.TouchDevice);
                    e.Handled = true;
                }
                catch { }
            };

            element.TouchMove += (_, e) =>
            {
                if (!_isTouchDragging) return;
                try
                {
                    var curScreen = PointToScreen(e.GetTouchPoint(this).Position);
                    var dpi = VisualTreeHelper.GetDpi(this);
                    double dx = (curScreen.X - _touchDragStartScreen.X) / dpi.DpiScaleX;
                    double dy = (curScreen.Y - _touchDragStartScreen.Y) / dpi.DpiScaleY;
                    Left += dx;
                    Top += dy;
                    _touchDragStartScreen = curScreen;
                    e.Handled = true;
                }
                catch { }
            };

            element.TouchUp += (_, e) =>
            {
                if (_isTouchDragging)
                {
                    _isTouchDragging = false;
                    element.ReleaseTouchCapture(e.TouchDevice);
                    e.Handled = true;
                }
            };
        }

        public void SetHideClassroomShellCheck(bool hideShell)
        {
            _suppressCheckboxEvent = true;
            try
            {
                _chkHideClassroomShell.IsChecked = hideShell;
            }
            finally
            {
                _suppressCheckboxEvent = false;
            }
        }

        public void UpdateLiveFrame(ImageSource? frame, string sourceDescription, string resolutionAndFps)
        {
            if (frame != null)
            {
                _pipImage.Source = frame;
            }
            _txtSourceTitle.Text = sourceDescription;
            _txtPipSubtitle.Text = $"Nguồn: {sourceDescription} ({resolutionAndFps})";
        }

        public void UpdateStatus(bool isPaused, string timerText, int recipientCount, string sourceDescription)
        {
            _isPaused = isPaused;
            _txtSourceTitle.Text = sourceDescription;
            _txtTimerAndStudents.Text = $"{timerText} • 👥 {recipientCount}";

            if (isPaused)
            {
                _mainBarBorder.BorderBrush = new SolidColorBrush(MeetWarningAmberColor);
                _statusDot.Fill = new SolidColorBrush(MeetWarningAmberColor);
                _txtStatusBadge.Text = "ĐANG ĐÓNG BĂNG";
                _txtStatusBadge.Foreground = new SolidColorBrush(MeetWarningAmberColor);
                _btnPauseResume.Content = "▶️ Tiếp tục";
                _btnPauseResume.Background = new SolidColorBrush(MeetLiveGreenColor);
                _txtPipOverlayBadge.Text = "⏸️ ĐÃ ĐÓNG BĂNG TRÊN MÁY HS";
                _txtPipOverlayBadge.Foreground = new SolidColorBrush(MeetWarningAmberColor);
            }
            else
            {
                _mainBarBorder.BorderBrush = new SolidColorBrush(MeetBorderColor);
                _statusDot.Fill = new SolidColorBrush(MeetLiveGreenColor);
                _txtStatusBadge.Text = _isLockedSource ? "📌 GIỮ CỐ ĐỊNH" : "ĐANG TRÌNH BÀY";
                _txtStatusBadge.Foreground = new SolidColorBrush(MeetLightBlueColor);
                _btnPauseResume.Content = "⏸️ Đóng băng";
                _btnPauseResume.Background = new SolidColorBrush(MeetPillDarkColor);
                _txtPipOverlayBadge.Text = _isLockedSource ? "📌 CỐ ĐỊNH TRÊN MÁY HS" : "🟢 TRỰC TIẾP TRÊN MÁY HS";
                _txtPipOverlayBadge.Foreground = new SolidColorBrush(Color.FromRgb(0x81, 0xC9, 0x95));
            }
        }

        public void UpdateSourceLockIndicator(bool isLockedSource, bool canSwitchToCurrentScreen, string switchTargetLabel)
        {
            _isLockedSource = isLockedSource;
            if (!_isPaused)
            {
                _txtStatusBadge.Text = _isLockedSource ? "📌 GIỮ CỐ ĐỊNH" : "ĐANG TRÌNH BÀY";
                _txtPipOverlayBadge.Text = _isLockedSource ? "📌 CỐ ĐỊNH TRÊN MÁY HS" : "🟢 TRỰC TIẾP TRÊN MÁY HS";
            }

            if (isLockedSource && canSwitchToCurrentScreen && !string.IsNullOrWhiteSpace(switchTargetLabel))
            {
                string shortLabel = switchTargetLabel.Length > 20
                    ? switchTargetLabel.Substring(0, 20) + "…"
                    : switchTargetLabel;
                _btnShareActiveInstead.Content = $"🔄 Chiếu '{shortLabel}' thay thế";
                _btnShareActiveInstead.ToolTip = $"Đang giữ cố định nguồn cũ cho Học sinh. Bấm để chuyển sang chiếu: {switchTargetLabel} (Chuẩn Google Meet)";
                _btnShareActiveInstead.Visibility = Visibility.Visible;
            }
            else
            {
                _btnShareActiveInstead.Visibility = Visibility.Collapsed;
            }
        }
    }
}
