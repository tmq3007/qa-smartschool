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

        private readonly Border _easyTouchBubble;
        private readonly Ellipse _easyTouchStatusDot;
        private readonly TextBlock _easyTouchMiniTimer;

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
        private readonly Button _btnAllowStudentExit;
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
        private bool _isEasyTouchExpanded = true;
        private bool _allowStudentExit;
        private bool _suppressAllowExitEvent;
        private bool _suppressCheckboxEvent;
        private bool _suppressDeviceSelectionEvent;
        private Point _touchDragStartScreen;
        private bool _isTouchDragging;
        private Point _bubbleDragStartScreen;
        private Point _bubbleWindowStartPos;
        private bool _isBubbleMouseDown;
        private bool _isBubbleTouchDown;
        private bool _hasBubbleMoved;
        private double _savedBubbleAnchorLeft = double.NaN;
        private double _savedBubbleAnchorTop = double.NaN;

        public event EventHandler? ShareCurrentInsteadRequested;
        public event EventHandler? ChangeSourceRequested;
        public event EventHandler? SelectRegionRequested;
        public event EventHandler? TogglePauseRequested;
        public event EventHandler? StopRequested;
        public event EventHandler? RestoreShellRequested;
        public event EventHandler<bool>? HideClassroomShellChanged;
        public event EventHandler<bool>? AllowStudentExitChanged;

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
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(8)
            };

            // ═══════════════════════════════════════════════════════════════════
            // 0. EASYTOUCH CIRCULAR FLOATING BUTTON (58x58 — Kéo thả & Bung/Thu)
            // ═══════════════════════════════════════════════════════════════════
            _easyTouchBubble = new Border
            {
                Width = 58,
                Height = 58,
                CornerRadius = new CornerRadius(29),
                Background = new SolidColorBrush(Color.FromArgb(236, 0x20, 0x21, 0x24)),
                BorderBrush = new SolidColorBrush(MeetLightBlueColor),
                BorderThickness = new Thickness(2.0),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Left,
                Visibility = Visibility.Collapsed,
                ToolTip = "Đang chia sẻ màn hình • Chạm để mở bảng điều khiển, giữ kéo để di chuyển",
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 16,
                    ShadowDepth = 3,
                    Opacity = 0.55
                }
            };

            var bubbleGrid = new Grid();
            bubbleGrid.Children.Add(new Ellipse
            {
                Width = 42,
                Height = 42,
                Fill = new SolidColorBrush(Color.FromArgb(46, 255, 255, 255)),
                Stroke = new SolidColorBrush(Color.FromArgb(135, 255, 255, 255)),
                StrokeThickness = 1.4,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

            var bubbleInnerStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            bubbleInnerStack.Children.Add(new TextBlock
            {
                Text = "📺",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, -1, 0, 0)
            });
            _easyTouchMiniTimer = new TextBlock
            {
                Text = "00:00",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, -2, 0, 0)
            };
            bubbleInnerStack.Children.Add(_easyTouchMiniTimer);
            bubbleGrid.Children.Add(bubbleInnerStack);

            _easyTouchStatusDot = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = new SolidColorBrush(MeetLiveGreenColor),
                Stroke = new SolidColorBrush(MeetSurfaceColor),
                StrokeThickness = 2,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 1, 1, 0)
            };
            bubbleGrid.Children.Add(_easyTouchStatusDot);
            _easyTouchBubble.Child = bubbleGrid;
            AttachEasyTouchBubbleGesture(_easyTouchBubble);
            rootStack.Children.Add(_easyTouchBubble);

            // ═══════════════════════════════════════════════════════════════════
            // 1. COMPACT EASYTOUCH HUB CARD (Gọn 2–3 hàng, KHÔNG trải dài ngang)
            // ═══════════════════════════════════════════════════════════════════
            _mainBarBorder = new Border
            {
                Width = 446,
                Background = new SolidColorBrush(MeetSurfaceColor),
                BorderBrush = new SolidColorBrush(MeetBorderColor),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(22),
                Padding = new Thickness(12, 10, 12, 10),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 20,
                    ShadowDepth = 5,
                    Opacity = 0.5
                }
            };

            var hubStack = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            // ─── HÀNG 1: Thanh tiêu đề kéo di chuyển + Nút Thu gọn thành hình tròn EasyTouch ───
            var headerDock = new DockPanel
            {
                LastChildFill = true,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var btnCollapseBubble = CreateMeetActionPill("● Thu gọn", MeetSubSurfaceColor, Color.FromRgb(0x8A, 0xB4, 0xF8),
                "Thu gọn bảng điều khiển thành nút tròn nổi EasyTouch gọn nhẹ");
            btnCollapseBubble.Height = 32;
            btnCollapseBubble.Padding = new Thickness(11, 0, 11, 0);
            btnCollapseBubble.Margin = new Thickness(6, 0, 0, 0);
            btnCollapseBubble.Click += (_, _) => SetEasyTouchExpanded(false);
            DockPanel.SetDock(btnCollapseBubble, Dock.Right);
            headerDock.Children.Add(btnCollapseBubble);

            var dragStatusZone = new Border
            {
                Background = new SolidColorBrush(MeetSubSurfaceColor),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(10, 5, 10, 5),
                Cursor = Cursors.SizeAll,
                ToolTip = "Giữ chuột hoặc ngón tay tại đây để kéo bảng điều khiển tới vị trí bất kỳ"
            };

            var statusStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            statusStack.Children.Add(new TextBlock
            {
                Text = "⠿",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0xA0, 0xA6)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            });

            _statusDot = new Ellipse
            {
                Width = 9,
                Height = 9,
                Fill = new SolidColorBrush(MeetLiveGreenColor),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            statusStack.Children.Add(_statusDot);

            _txtStatusBadge = new TextBlock
            {
                Text = "ĐANG TRÌNH BÀY",
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(MeetLightBlueColor),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            statusStack.Children.Add(_txtStatusBadge);

            _txtSourceTitle = new TextBlock
            {
                Text = "Toàn màn hình",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                MaxWidth = 108,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            statusStack.Children.Add(_txtSourceTitle);

            _txtTimerAndStudents = new TextBlock
            {
                Text = "00:00 • 👥 0",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xDA, 0xDC, 0xE0)),
                VerticalAlignment = VerticalAlignment.Center
            };
            statusStack.Children.Add(_txtTimerAndStudents);

            dragStatusZone.Child = statusStack;
            EnableDragOnElement(dragStatusZone);
            headerDock.Children.Add(dragStatusZone);
            hubStack.Children.Add(headerDock);

            // Nút chuyển nhanh nguồn chiếu màn hình hiện tại (chỉ hiện khi cần)
            _btnShareActiveInstead = CreateMeetActionPill("🔄 Chiếu màn này thay thế", MeetActiveBlueColor, Colors.White,
                "Học sinh vẫn đang xem cố định màn hình cũ. Bấm nút này nếu muốn chuyển sang chiếu màn hình hiện tại");
            _btnShareActiveInstead.Visibility = Visibility.Collapsed;
            _btnShareActiveInstead.Margin = new Thickness(0, 0, 0, 6);
            _btnShareActiveInstead.HorizontalAlignment = HorizontalAlignment.Stretch;
            _btnShareActiveInstead.Click += (_, _) => ShareCurrentInsteadRequested?.Invoke(this, EventArgs.Empty);
            hubStack.Children.Add(_btnShareActiveInstead);

            // ─── HÀNG 2: Âm thanh / Camera + Cấp quyền Học sinh ra ngoài ───
            var mediaRow = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };

            // ─── Google Meet Split-Pill 1: MICROPHONE [ 🎙️ + 3-bar equalizer | ▾ ] ───
            (_micSplitPill, _btnMicToggle, _btnMicMenu) = CreateMeetSplitPill(
                mainTooltip: "Bật / Tắt Micro Giáo viên",
                chevronTooltip: "Chọn thiết bị Micro & Nguồn âm thanh");

            var micContentStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _txtMicIcon = new TextBlock
            {
                Text = "🔇 Mic",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
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
            mediaRow.Children.Add(_micSplitPill);

            // ─── Google Meet Pill 2: SYSTEM AUDIO [ 🔊 Âm thanh + sóng âm ] ───
            _sysAudioPill = new Border
            {
                Background = new SolidColorBrush(MeetActiveBlueColor),
                CornerRadius = new CornerRadius(19),
                Height = 36,
                Margin = new Thickness(0, 0, 5, 4),
                ClipToBounds = true
            };
            _btnSysAudioToggle = CreateTransparentInnerButton(
                new Thickness(10, 0, 10, 0),
                "Bật / Tắt chia sẻ âm thanh hệ thống (video, bài nghe trên máy Giáo viên)");
            var sysStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _txtSysAudioLabel = new TextBlock
            {
                Text = "🔊 Âm thanh",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
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
            mediaRow.Children.Add(_sysAudioPill);

            // ─── Google Meet Split-Pill 3: CAMERA [ 📷 Cam | ▾ ] ───
            (_camSplitPill, _btnCamToggle, _btnCamMenu) = CreateMeetSplitPill(
                mainTooltip: "Bật / Tắt Camera Giáo viên hiển thị góc màn hình Học sinh",
                chevronTooltip: "Chọn Camera, kiểu khung & góc hiển thị");

            _txtCamLabel = new TextBlock
            {
                Text = "🚫 Cam",
                FontSize = 11.5,
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
            mediaRow.Children.Add(_camSplitPill);

            // ─── Pill 4: Cấp quyền Học sinh bấm ra ngoài (Mặc định Khóa toàn màn hình) ───
            _btnAllowStudentExit = CreateMeetActionPill("🔒 Khóa HS", MeetPillDarkColor, Color.FromRgb(0xFC, 0xA5, 0xA5),
                "Đang khóa cứng toàn màn hình Học sinh (Học sinh không làm gì khác được). Bấm để CHO PHÉP học sinh thu nhỏ / bấm ra ngoài.");
            _btnAllowStudentExit.Height = 36;
            _btnAllowStudentExit.Padding = new Thickness(10, 0, 10, 0);
            _btnAllowStudentExit.Margin = new Thickness(0, 0, 0, 4);
            _btnAllowStudentExit.Click += (_, _) =>
            {
                if (_suppressAllowExitEvent) return;
                SetAllowStudentExitState(!_allowStudentExit);
                AllowStudentExitChanged?.Invoke(this, _allowStudentExit);
            };
            mediaRow.Children.Add(_btnAllowStudentExit);
            hubStack.Children.Add(mediaRow);

            // ─── HÀNG 3: Điều khiển Nguồn chiếu, Vùng, Đóng băng, Xem thử, Lớp học & Dừng ───
            var barPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var btnChangeSource = CreateMeetActionPill("🖥️ Nguồn", MeetPillDarkColor, Colors.White,
                "Chọn lại màn hình, cửa sổ ứng dụng, vùng chọn hoặc trang bài giảng");
            btnChangeSource.Height = 36;
            btnChangeSource.Padding = new Thickness(10, 0, 10, 0);
            btnChangeSource.Margin = new Thickness(0, 0, 5, 4);
            btnChangeSource.Click += (_, _) => ChangeSourceRequested?.Invoke(this, EventArgs.Empty);
            barPanel.Children.Add(btnChangeSource);

            var btnRegion = CreateMeetActionPill("✂️ Vùng", MeetPillDarkColor, Colors.White,
                "Kéo khung hình chữ nhật trên màn hình để chỉ chiếu đúng khu vực đó cho Học sinh");
            btnRegion.Height = 36;
            btnRegion.Padding = new Thickness(10, 0, 10, 0);
            btnRegion.Margin = new Thickness(0, 0, 5, 4);
            btnRegion.Click += (_, _) => SelectRegionRequested?.Invoke(this, EventArgs.Empty);
            barPanel.Children.Add(btnRegion);

            _btnPauseResume = CreateMeetActionPill("⏸️ Đóng băng", MeetPillDarkColor, Colors.White,
                "Đóng băng hình ảnh trên máy Học sinh để Giáo viên mở tài liệu riêng tư");
            _btnPauseResume.Height = 36;
            _btnPauseResume.Padding = new Thickness(10, 0, 10, 0);
            _btnPauseResume.Margin = new Thickness(0, 0, 5, 4);
            _btnPauseResume.Click += (_, _) => TogglePauseRequested?.Invoke(this, EventArgs.Empty);
            barPanel.Children.Add(_btnPauseResume);

            _btnTogglePiP = new ToggleButton
            {
                Content = "👁️ Xem thử",
                IsChecked = false,
                Height = 36,
                Padding = new Thickness(10, 0, 10, 0),
                Margin = new Thickness(0, 0, 5, 4),
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(MeetPillDarkColor),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                ToolTip = "Ẩn/Hiện màn hình thu nhỏ cho biết chính xác Học sinh đang nhìn thấy gì"
            };
            ApplyRoundedButtonTemplate(_btnTogglePiP, 18);
            _btnTogglePiP.Checked += (_, _) =>
            {
                if (_pipContainer != null) _pipContainer.Visibility = Visibility.Visible;
                _btnTogglePiP.Background = new SolidColorBrush(MeetActiveBlueColor);
                _btnTogglePiP.Content = "👁️ Ẩn thử";
            };
            _btnTogglePiP.Unchecked += (_, _) =>
            {
                if (_pipContainer != null) _pipContainer.Visibility = Visibility.Collapsed;
                _btnTogglePiP.Background = new SolidColorBrush(MeetPillDarkColor);
                _btnTogglePiP.Content = "👁️ Xem thử";
            };
            barPanel.Children.Add(_btnTogglePiP);

            var btnRestoreShell = CreateMeetActionPill("🏠 Lớp", MeetPillDarkColor, Colors.White,
                "Mở lại cửa sổ Quản lý lớp học QA SmartClass");
            btnRestoreShell.Height = 36;
            btnRestoreShell.Padding = new Thickness(10, 0, 10, 0);
            btnRestoreShell.Margin = new Thickness(0, 0, 5, 4);
            btnRestoreShell.Click += (_, _) => RestoreShellRequested?.Invoke(this, EventArgs.Empty);
            barPanel.Children.Add(btnRestoreShell);

            // ─── Nút kết thúc đỏ chuẩn Google Meet (#EA4335) ───
            var btnStop = CreateMeetActionPill("⏹️ Dừng chiếu", MeetMutedRedColor, Colors.White,
                "Dừng chia sẻ màn hình / bảng viết & âm thanh ngay lập tức");
            btnStop.Height = 36;
            btnStop.Margin = new Thickness(0, 0, 0, 4);
            btnStop.Padding = new Thickness(12, 0, 12, 0);
            btnStop.Click += (_, _) =>
            {
                StopRequested?.Invoke(this, EventArgs.Empty);
                ScreenShareController.Instance.StopAllActiveBroadcasts();
            };
            btnStop.PreviewTouchDown += (_, ev) =>
            {
                ev.Handled = true;
                StopRequested?.Invoke(this, EventArgs.Empty);
                ScreenShareController.Instance.StopAllActiveBroadcasts();
            };
            barPanel.Children.Add(btnStop);

            hubStack.Children.Add(barPanel);
            _mainBarBorder.Child = hubStack;
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
            Deactivated += (_, _) =>
            {
                // Giống EasyTouch / AssistiveTouch: khi Giáo viên bấm ra ngoài bảng giảng / slide để dạy thì tự thu về nút tròn gọn nhẹ
                if (_isEasyTouchExpanded && string.IsNullOrEmpty(_openPopoverMode) && _btnTogglePiP.IsChecked != true)
                {
                    SetEasyTouchExpanded(false);
                }
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
                    : (sysOn ? "🔊 Âm thanh" : "🔈 Tắt âm thanh");

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
                CornerRadius = new CornerRadius(18),
                Height = 36,
                Margin = new Thickness(0, 0, 5, 4),
                ClipToBounds = true
            };

            var stack = new StackPanel { Orientation = Orientation.Horizontal };

            var mainBtn = CreateTransparentInnerButton(new Thickness(9, 0, 6, 0), mainTooltip);
            mainBtn.Height = 36;
            stack.Children.Add(mainBtn);

            stack.Children.Add(new Rectangle
            {
                Width = 1,
                Height = 18,
                Fill = new SolidColorBrush(Color.FromArgb(95, 255, 255, 255)),
                VerticalAlignment = VerticalAlignment.Center
            });

            var chevBtn = CreateTransparentInnerButton(new Thickness(6, 0, 8, 0), chevronTooltip);
            chevBtn.Height = 36;
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
                double w = ActualWidth > 0 ? ActualWidth : 462;
                Left = Math.Max(workArea.Left + 10, workArea.Right - w - 16);
                Top = workArea.Top + 14;
                _savedBubbleAnchorLeft = workArea.Right - 82;
                _savedBubbleAnchorTop = workArea.Top + 14;
            }
            catch { }
        }

        private void ClampWindowToWorkArea()
        {
            try
            {
                var workArea = SystemParameters.WorkArea;
                double w = ActualWidth > 0 ? ActualWidth : (_isEasyTouchExpanded ? 462 : 74);
                double h = ActualHeight > 0 ? ActualHeight : (_isEasyTouchExpanded ? 160 : 74);
                Left = Math.Max(workArea.Left + 6, Math.Min(Left, workArea.Right - w - 6));
                Top = Math.Max(workArea.Top + 6, Math.Min(Top, workArea.Bottom - h - 6));
            }
            catch { }
        }

        public void SetEasyTouchExpanded(bool expanded)
        {
            if (_isEasyTouchExpanded == expanded) return;
            _isEasyTouchExpanded = expanded;

            var workArea = SystemParameters.WorkArea;
            if (!expanded)
            {
                // Lưu mỏ neo tại góc phải của bảng đang mở để đặt nút tròn đúng tầm tay
                double curW = ActualWidth > 0 ? ActualWidth : 462;
                double targetBubbleLeft = Left + curW - 74;
                if (!double.IsNaN(_savedBubbleAnchorLeft) && Math.Abs(_savedBubbleAnchorLeft - Left) < 520)
                {
                    targetBubbleLeft = Math.Max(workArea.Left + 8, Math.Min(targetBubbleLeft, workArea.Right - 76));
                }

                _openPopoverMode = "";
                _devicePopoverBorder.Visibility = Visibility.Collapsed;
                _warningBanner.Visibility = Visibility.Collapsed;
                if (_btnTogglePiP.IsChecked == true) _btnTogglePiP.IsChecked = false;
                if (_pipContainer != null) _pipContainer.Visibility = Visibility.Collapsed;

                _mainBarBorder.Visibility = Visibility.Collapsed;
                _easyTouchBubble.Visibility = Visibility.Visible;

                Left = Math.Max(workArea.Left + 8, Math.Min(targetBubbleLeft, workArea.Right - 76));
                Top = Math.Max(workArea.Top + 8, Math.Min(Top, workArea.Bottom - 76));
                _savedBubbleAnchorLeft = Left;
                _savedBubbleAnchorTop = Top;
            }
            else
            {
                _savedBubbleAnchorLeft = Left;
                _savedBubbleAnchorTop = Top;

                _easyTouchBubble.Visibility = Visibility.Collapsed;
                _mainBarBorder.Visibility = Visibility.Visible;

                const double estExpandedWidth = 462;
                const double estExpandedHeight = 165;
                // Nếu nút tròn đang ở nửa bên phải màn hình thì bung sang trái để không tràn mép
                double targetLeft = (_savedBubbleAnchorLeft > workArea.Left + workArea.Width * 0.5)
                    ? (_savedBubbleAnchorLeft + 74 - estExpandedWidth)
                    : _savedBubbleAnchorLeft;

                Left = Math.Max(workArea.Left + 8, Math.Min(targetLeft, workArea.Right - estExpandedWidth - 8));
                Top = Math.Max(workArea.Top + 8, Math.Min(_savedBubbleAnchorTop, workArea.Bottom - estExpandedHeight - 8));
                Activate();
            }
        }

        private void AttachEasyTouchBubbleGesture(UIElement bubble)
        {
            bubble.MouseLeftButtonDown += (_, e) =>
            {
                if (e.StylusDevice != null) return;
                try
                {
                    _bubbleDragStartScreen = PointToScreen(e.GetPosition(this));
                    _bubbleWindowStartPos = new Point(Left, Top);
                    _isBubbleMouseDown = true;
                    _hasBubbleMoved = false;
                    bubble.CaptureMouse();
                    e.Handled = true;
                }
                catch { }
            };

            bubble.MouseMove += (_, e) =>
            {
                if (!_isBubbleMouseDown) return;
                try
                {
                    var curScreen = PointToScreen(e.GetPosition(this));
                    var dpi = VisualTreeHelper.GetDpi(this);
                    double dx = (curScreen.X - _bubbleDragStartScreen.X) / dpi.DpiScaleX;
                    double dy = (curScreen.Y - _bubbleDragStartScreen.Y) / dpi.DpiScaleY;
                    if (Math.Sqrt(dx * dx + dy * dy) >= 6.0)
                    {
                        _hasBubbleMoved = true;
                        Left = _bubbleWindowStartPos.X + dx;
                        Top = _bubbleWindowStartPos.Y + dy;
                        ClampWindowToWorkArea();
                        _savedBubbleAnchorLeft = Left;
                        _savedBubbleAnchorTop = Top;
                    }
                    e.Handled = true;
                }
                catch { }
            };

            bubble.MouseLeftButtonUp += (_, e) =>
            {
                if (!_isBubbleMouseDown) return;
                bool wasMoved = _hasBubbleMoved;
                _isBubbleMouseDown = false;
                _hasBubbleMoved = false;
                bubble.ReleaseMouseCapture();
                e.Handled = true;
                if (!wasMoved)
                {
                    SetEasyTouchExpanded(true);
                }
            };

            bubble.TouchDown += (_, e) =>
            {
                try
                {
                    _bubbleDragStartScreen = PointToScreen(e.GetTouchPoint(this).Position);
                    _bubbleWindowStartPos = new Point(Left, Top);
                    _isBubbleTouchDown = true;
                    _hasBubbleMoved = false;
                    bubble.CaptureTouch(e.TouchDevice);
                    e.Handled = true;
                }
                catch { }
            };

            bubble.TouchMove += (_, e) =>
            {
                if (!_isBubbleTouchDown) return;
                try
                {
                    var curScreen = PointToScreen(e.GetTouchPoint(this).Position);
                    var dpi = VisualTreeHelper.GetDpi(this);
                    double dx = (curScreen.X - _bubbleDragStartScreen.X) / dpi.DpiScaleX;
                    double dy = (curScreen.Y - _bubbleDragStartScreen.Y) / dpi.DpiScaleY;
                    if (Math.Sqrt(dx * dx + dy * dy) >= 8.0)
                    {
                        _hasBubbleMoved = true;
                        Left = _bubbleWindowStartPos.X + dx;
                        Top = _bubbleWindowStartPos.Y + dy;
                        ClampWindowToWorkArea();
                        _savedBubbleAnchorLeft = Left;
                        _savedBubbleAnchorTop = Top;
                    }
                    e.Handled = true;
                }
                catch { }
            };

            bubble.TouchUp += (_, e) =>
            {
                if (!_isBubbleTouchDown) return;
                bool wasMoved = _hasBubbleMoved;
                _isBubbleTouchDown = false;
                _hasBubbleMoved = false;
                bubble.ReleaseTouchCapture(e.TouchDevice);
                e.Handled = true;
                if (!wasMoved)
                {
                    SetEasyTouchExpanded(true);
                }
            };
        }

        public void SetAllowStudentExitState(bool allowExit)
        {
            _suppressAllowExitEvent = true;
            try
            {
                _allowStudentExit = allowExit;
                if (allowExit)
                {
                    _btnAllowStudentExit.Content = "🔓 Cho HS ra ngoài";
                    _btnAllowStudentExit.Background = new SolidColorBrush(MeetLiveGreenColor);
                    _btnAllowStudentExit.Foreground = Brushes.White;
                    _btnAllowStudentExit.ToolTip = "Đang CHO PHÉP học sinh thu nhỏ / bấm ra ngoài màn chiếu. Bấm để KHÓA lại toàn màn hình 100%.";
                }
                else
                {
                    _btnAllowStudentExit.Content = "🔒 Khóa HS";
                    _btnAllowStudentExit.Background = new SolidColorBrush(MeetPillDarkColor);
                    _btnAllowStudentExit.Foreground = new SolidColorBrush(Color.FromRgb(0xFC, 0xA5, 0xA5));
                    _btnAllowStudentExit.ToolTip = "Đang KHÓA CỨNG toàn màn hình Học sinh (Học sinh không làm gì khác được). Bấm để cho phép học sinh bấm ra ngoài.";
                }
            }
            finally
            {
                _suppressAllowExitEvent = false;
            }
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
            _easyTouchMiniTimer.Text = timerText;

            if (isPaused)
            {
                _mainBarBorder.BorderBrush = new SolidColorBrush(MeetWarningAmberColor);
                _easyTouchBubble.BorderBrush = new SolidColorBrush(MeetWarningAmberColor);
                _easyTouchStatusDot.Fill = new SolidColorBrush(MeetWarningAmberColor);
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
                _easyTouchBubble.BorderBrush = new SolidColorBrush(MeetLightBlueColor);
                _easyTouchStatusDot.Fill = new SolidColorBrush(MeetLiveGreenColor);
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
