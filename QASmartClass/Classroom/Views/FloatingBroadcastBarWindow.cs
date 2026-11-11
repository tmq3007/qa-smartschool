using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QASmartClass.Classroom.Views
{
    /// <summary>
    /// [MYTHWARE_LITE] Thanh điều khiển trình chiếu nổi (Floating Share Bar) + Màn hình xem trước thu nhỏ (Live PiP Monitor).
    /// Thiết kế lai giữa Zoom / Microsoft Teams và Mythware Classroom:
    /// - Luôn nổi trên cùng (Topmost = true) khi Giáo viên chuyển sang PowerPoint, Chrome, IDE, SMART TOUCH hoặc DESKTOP.
    /// - Tự gắn cờ WDA_EXCLUDEFROMCAPTURE (0x11) cho chính thanh nổi này để học sinh KHÔNG BAO GIỜ nhìn thấy thanh điều khiển/PiP của Giáo viên.
    /// - Cho phép Giáo viên nhìn thấy trực tiếp 100% khung hình thực tế mà máy Học sinh đang nhận được (Live PiP).
    /// - Hỗ trợ đổi nguồn chiếu, kéo chọn vùng màn hình, đóng băng (Freeze), ẩn/hiện cửa sổ Quản lý lớp khỏi luồng phát và dừng chiếu chỉ bằng 1 chạm.
    /// </summary>
    public sealed class FloatingBroadcastBarWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011U;

        private readonly Border _mainBarBorder;
        private readonly Ellipse _statusDot;
        private readonly TextBlock _txtStatusBadge;
        private readonly TextBlock _txtSourceTitle;
        private readonly TextBlock _txtTimerAndStudents;
        private readonly ToggleButton _btnTogglePiP;
        private readonly Button _btnPauseResume;
        private readonly Border _pipContainer;
        private readonly Image _pipImage;
        private readonly TextBlock _txtPipOverlayBadge;
        private readonly TextBlock _txtPipSubtitle;
        private readonly CheckBox _chkHideClassroomShell;

        private bool _isPaused;
        private bool _suppressCheckboxEvent;
        private Point _touchDragStartScreen;
        private bool _isTouchDragging;

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

            // ═══════════════════════════════════════════════════════════════
            // 1. TOP CONTROL STRIP (THANH ĐIỀU KHIỂN NỔI KIỂU ZOOM / MYTHWARE)
            // ═══════════════════════════════════════════════════════════════
            _mainBarBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(245, 15, 23, 42)), // #0F172A Slate 900
                BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129)),    // #10B981 Emerald
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(12, 8, 12, 8),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 18,
                    ShadowDepth = 4,
                    Opacity = 0.45
                }
            };

            var barPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Cụm kéo di chuyển + Trạng thái phát
            var dragStatusZone = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)), // #1E293B
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(10, 6, 12, 6),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.SizeAll,
                ToolTip = "Giữ chuột hoặc ngón tay tại đây để kéo thanh điều khiển tới vị trí bất kỳ"
            };

            var statusStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var gripText = new TextBlock
            {
                Text = "⠿",
                FontSize = 15,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            statusStack.Children.Add(gripText);

            _statusDot = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            statusStack.Children.Add(_statusDot);

            _txtStatusBadge = new TextBlock
            {
                Text = "ĐANG CHIẾU:",
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(110, 231, 183)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            statusStack.Children.Add(_txtStatusBadge);

            _txtSourceTitle = new TextBlock
            {
                Text = "Toàn màn hình",
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                MaxWidth = 230,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            statusStack.Children.Add(_txtSourceTitle);

            _txtTimerAndStudents = new TextBlock
            {
                Text = "00:00 • 0 HS",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                VerticalAlignment = VerticalAlignment.Center
            };
            statusStack.Children.Add(_txtTimerAndStudents);

            dragStatusZone.Child = statusStack;
            EnableDragOnElement(dragStatusZone);
            barPanel.Children.Add(dragStatusZone);

            // Nút 1: Bật/tắt Màn hình xem trước (Live PiP Monitor)
            _btnTogglePiP = new ToggleButton
            {
                Content = "👁️ Xem hình HS thấy (PiP)",
                IsChecked = true,
                MinHeight = 36,
                Padding = new Thickness(11, 5, 11, 5),
                Margin = new Thickness(0, 0, 6, 0),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                ToolTip = "Ẩn/Hiện màn hình thu nhỏ cho biết chính xác Học sinh đang nhìn thấy gì"
            };
            _btnTogglePiP.Checked += (_, _) =>
            {
                if (_pipContainer != null) _pipContainer.Visibility = Visibility.Visible;
                _btnTogglePiP.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                _btnTogglePiP.Content = "👁️ Ẩn khung xem thử";
            };
            _btnTogglePiP.Unchecked += (_, _) =>
            {
                if (_pipContainer != null) _pipContainer.Visibility = Visibility.Collapsed;
                _btnTogglePiP.Background = new SolidColorBrush(Color.FromRgb(51, 65, 85));
                _btnTogglePiP.Content = "👁️ Xem hình HS thấy (PiP)";
            };
            _btnTogglePiP.Content = "👁️ Ẩn khung xem thử";
            barPanel.Children.Add(_btnTogglePiP);

            // Nút 2: Đổi nguồn chiếu (Toàn màn hình / Cửa sổ ứng dụng / Vùng chọn / Trang QA SmartClass)
            var btnChangeSource = CreateBarButton("🔄 Đổi nguồn chiếu", Color.FromRgb(51, 65, 85), Colors.White,
                "Chọn lại màn hình, cửa sổ ứng dụng (PowerPoint, Chrome...), vùng chọn hoặc trang bài giảng");
            btnChangeSource.Click += (_, _) => ChangeSourceRequested?.Invoke(this, EventArgs.Empty);
            barPanel.Children.Add(btnChangeSource);

            // Nút 3: Kéo chọn vùng màn hình trực tiếp
            var btnRegion = CreateBarButton("✂️ Kéo vùng", Color.FromRgb(14, 116, 144), Colors.White,
                "Kéo khung hình chữ nhật trên màn hình để chỉ chiếu đúng khu vực đó cho Học sinh");
            btnRegion.Click += (_, _) => SelectRegionRequested?.Invoke(this, EventArgs.Empty);
            barPanel.Children.Add(btnRegion);

            // Nút 4: Đóng băng / Tiếp tục (Freeze)
            _btnPauseResume = CreateBarButton("⏸️ Đóng băng", Color.FromRgb(180, 83, 9), Colors.White,
                "Đóng băng hình ảnh trên máy Học sinh để Giáo viên mở tài liệu/đáp án riêng tư");
            _btnPauseResume.Click += (_, _) => TogglePauseRequested?.Invoke(this, EventArgs.Empty);
            barPanel.Children.Add(_btnPauseResume);

            // Nút 5: Mở cửa sổ Quản lý lớp
            var btnRestoreShell = CreateBarButton("🏠 Quản lý lớp", Color.FromRgb(51, 65, 85), Colors.White,
                "Mở lại cửa sổ Quản lý lớp học QA SmartClass");
            btnRestoreShell.Click += (_, _) => RestoreShellRequested?.Invoke(this, EventArgs.Empty);
            barPanel.Children.Add(btnRestoreShell);

            // Nút 6: Dừng chiếu
            var btnStop = CreateBarButton("⏹️ Dừng chiếu", Color.FromRgb(220, 38, 38), Colors.White,
                "Dừng chia sẻ màn hình ngay lập tức");
            btnStop.Margin = new Thickness(0);
            btnStop.Click += (_, _) => StopRequested?.Invoke(this, EventArgs.Empty);
            barPanel.Children.Add(btnStop);

            _mainBarBorder.Child = barPanel;
            rootStack.Children.Add(_mainBarBorder);

            // ═══════════════════════════════════════════════════════════════
            // 2. COLLAPSIBLE LIVE PiP MONITOR (KHUNG XEM TRƯỚC TRỰC TIẾP)
            // ═══════════════════════════════════════════════════════════════
            _pipContainer = new Border
            {
                Width = 360,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 6, 0, 0),
                Background = new SolidColorBrush(Color.FromArgb(245, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 16,
                    ShadowDepth = 4,
                    Opacity = 0.4
                }
            };

            var pipStack = new StackPanel { Orientation = Orientation.Vertical };

            // Header của PiP
            var pipHeader = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            pipHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pipHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pipTitle = new TextBlock
            {
                Text = "📺 HÌNH ẢNH HỌC SINH ĐANG NHÌN THẤY (TRỰC TIẾP)",
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(147, 197, 253)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(pipTitle, 0);
            pipHeader.Children.Add(pipTitle);

            var btnMinimizePip = new Button
            {
                Content = "Thu gọn ▲",
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(6, 2, 6, 2),
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnMinimizePip.Click += (_, _) => _btnTogglePiP.IsChecked = false;
            Grid.SetColumn(btnMinimizePip, 1);
            pipHeader.Children.Add(btnMinimizePip);
            pipStack.Children.Add(pipHeader);

            // Khung chứa ảnh Live Preview
            var videoFrame = new Border
            {
                Height = 186,
                Background = new SolidColorBrush(Color.FromRgb(2, 6, 23)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
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
                Background = new SolidColorBrush(Color.FromArgb(200, 15, 23, 42)),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(7, 3, 7, 3)
            };
            _txtPipOverlayBadge = new TextBlock
            {
                Text = "🟢 LIVE STREAM",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(110, 231, 183))
            };
            badgeBorder.Child = _txtPipOverlayBadge;
            videoGrid.Children.Add(badgeBorder);

            videoFrame.Child = videoGrid;
            pipStack.Children.Add(videoFrame);

            // Dòng mô tả nguồn đang chiếu dưới ảnh PiP
            _txtPipSubtitle = new TextBlock
            {
                Text = "Đang khởi tạo luồng chụp màn hình...",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Margin = new Thickness(2, 6, 2, 4),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            pipStack.Children.Add(_txtPipSubtitle);

            // Checkbox bật/tắt ẩn cửa sổ QASmartClass khỏi màn hình Học sinh
            _chkHideClassroomShell = new CheckBox
            {
                Content = "Ẩn cửa sổ Quản lý lớp khỏi màn hình Học sinh",
                IsChecked = initialHideClassroomShell,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                Margin = new Thickness(2, 2, 2, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Khi bật: Học sinh chỉ nhìn thấy màn hình/bài giảng phía sau cửa sổ QASmartClass.\nKhi tắt: Học sinh nhìn thấy mọi thứ trên màn hình kể cả cửa sổ QASmartClass."
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

            SourceInitialized += OnSourceInitialized;
            Loaded += OnLoadedPosition;
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    // Bắt buộc ẩn riêng thanh điều khiển nổi này khỏi ảnh chụp màn hình để học sinh không thấy thanh công cụ của GV
                    SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
                }
            }
            catch { }
        }

        private void OnLoadedPosition(object sender, RoutedEventArgs e)
        {
            PositionTopCenter();
        }

        public void PositionTopCenter()
        {
            try
            {
                var workArea = SystemParameters.WorkArea;
                double w = ActualWidth > 0 ? ActualWidth : 920;
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

        private static Button CreateBarButton(string text, Color bg, Color fg, string tooltip)
        {
            return new Button
            {
                Content = text,
                MinHeight = 36,
                Padding = new Thickness(11, 5, 11, 5),
                Margin = new Thickness(0, 0, 6, 0),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(bg),
                Foreground = new SolidColorBrush(fg),
                BorderThickness = new Thickness(0),
                ToolTip = tooltip
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
            _txtTimerAndStudents.Text = $"{timerText} • 👥 {recipientCount} HS";

            if (isPaused)
            {
                _mainBarBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Amber #F59E0B
                _statusDot.Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                _txtStatusBadge.Text = "ĐANG ĐÓNG BĂNG:";
                _txtStatusBadge.Foreground = new SolidColorBrush(Color.FromRgb(253, 230, 138));
                _btnPauseResume.Content = "▶️ Tiếp tục chiếu";
                _btnPauseResume.Background = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                _txtPipOverlayBadge.Text = "⏸️ ĐÃ ĐÓNG BĂNG HÌNH TRÊN MÁY HS";
                _txtPipOverlayBadge.Foreground = new SolidColorBrush(Color.FromRgb(253, 230, 138));
            }
            else
            {
                _mainBarBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Emerald #10B981
                _statusDot.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                _txtStatusBadge.Text = "ĐANG CHIẾU:";
                _txtStatusBadge.Foreground = new SolidColorBrush(Color.FromRgb(110, 231, 183));
                _btnPauseResume.Content = "⏸️ Đóng băng";
                _btnPauseResume.Background = new SolidColorBrush(Color.FromRgb(180, 83, 9));
                _txtPipOverlayBadge.Text = "🟢 TRỰC TIẾP TRÊN MÁY HS";
                _txtPipOverlayBadge.Foreground = new SolidColorBrush(Color.FromRgb(110, 231, 183));
            }
        }
    }
}

