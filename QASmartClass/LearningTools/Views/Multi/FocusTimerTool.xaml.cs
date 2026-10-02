using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Input;
using System.IO;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Multi
{
    public partial class FocusTimerTool : UserControl, IDisposable
    {
        private readonly DispatcherTimer _timer;
        private int _focusMinutes = 25;
        private int _breakMinutes = 5;
        private int _remainingSeconds;
        private int _totalSeconds;
        private int _sessionCount = 0;
        private bool _isRunning = false;
        private bool _isBreak = false;
        private readonly System.Collections.ObjectModel.ObservableCollection<FocusGoalItem> _goalsList = new();
        private Ellipse? _thumbEllipse;

        // Dependency Properties for Data Binding
        public static readonly DependencyProperty DeleteGoalVisibilityProperty =
            DependencyProperty.Register(nameof(DeleteGoalVisibility), typeof(Visibility), typeof(FocusTimerTool), new PropertyMetadata(Visibility.Visible));

        public Visibility DeleteGoalVisibility
        {
            get => (Visibility)GetValue(DeleteGoalVisibilityProperty);
            set => SetValue(DeleteGoalVisibilityProperty, value);
        }

        public static readonly DependencyProperty ClassProgressVisibilityProperty =
            DependencyProperty.Register(nameof(ClassProgressVisibility), typeof(Visibility), typeof(FocusTimerTool), new PropertyMetadata(Visibility.Visible));

        public Visibility ClassProgressVisibility
        {
            get => (Visibility)GetValue(ClassProgressVisibilityProperty);
            set => SetValue(ClassProgressVisibilityProperty, value);
        }

        // Student Progress tracking for Teacher
        public class StudentProgressItem
        {
            public string StudentCode { get; set; } = "";
            public string StudentName { get; set; } = "";
            public List<bool> GoalStates { get; set; } = new();

            public string ProgressText
            {
                get
                {
                    int completed = 0;
                    foreach (var state in GoalStates)
                    {
                        if (state) completed++;
                    }
                    return $"{completed}/{GoalStates.Count}";
                }
            }
        }

        private readonly Dictionary<string, StudentProgressItem> _studentProgressMap = new();
        private readonly System.Collections.ObjectModel.ObservableCollection<StudentProgressItem> _studentProgressList = new();

        // Ambient sound
        private MediaPlayer _ambientPlayer;
        private string _currentAmbientTag = "";

        // Motivational quotes
        private static readonly string[] Quotes = new[]
        {
            "💡 \"Kỷ luật là cầu nối giữa mục tiêu và thành tựu.\" — Jim Rohn",
            "💡 \"Học không phải là nghĩa vụ mà là cơ hội.\" — Albert Einstein",
            "💡 \"Mỗi bước nhỏ đều đưa bạn gần hơn đến đích.\" — Khổng Tử",
            "💡 \"Tập trung vào hành trình, kết quả sẽ tự đến.\" — Đức Phật",
            "💡 \"Thành công là tổng của những nỗ lực nhỏ mỗi ngày.\" — Robert Collier",
            "💡 \"Đừng xem đồng hồ, hãy làm như đồng hồ — luôn tiến về phía trước.\" — Sam Levenson",
            "💡 \"Giáo dục là vũ khí mạnh nhất để thay đổi thế giới.\" — Nelson Mandela",
            "💡 \"Tri thức là sức mạnh.\" — Francis Bacon",
        };

        private static readonly string[] QuotesEN = new[]
        {
            "💡 \"Discipline is the bridge between goals and accomplishment.\" — Jim Rohn",
            "💡 \"Education is not the learning of facts, but the training of the mind to think.\" — Albert Einstein",
            "💡 \"It does not matter how slowly you go as long as you do not stop.\" — Confucius",
            "💡 \"Focus on the journey, not the destination.\" — Buddha",
            "💡 \"Success is the sum of small efforts, repeated day in and day out.\" — Robert Collier",
            "💡 \"Don't watch the clock; do what it does. Keep going.\" — Sam Levenson",
            "💡 \"Education is the most powerful weapon which you can use to change the world.\" — Nelson Mandela",
            "💡 \"Knowledge is power.\" — Francis Bacon",
        };

        // Ring drawing
        private System.Windows.Shapes.Path _arcPath;
        private readonly Color _focusColor = Color.FromRgb(76, 175, 80);   // Green
        private readonly Color _breakColor = Color.FromRgb(255, 183, 77);   // Amber

        public FocusTimerTool()
        {
            InitializeComponent();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;

            _ambientPlayer = new MediaPlayer();
            _ambientPlayer.Volume = sliderVolume.Value / 100.0;
            _ambientPlayer.MediaEnded += AmbientPlayer_MediaEnded;

            // Khởi tạo đối tượng vẽ viền tiến trình một lần duy nhất để tối ưu hiệu năng
            _arcPath = new System.Windows.Shapes.Path
            {
                StrokeThickness = 12,
                Fill = Brushes.Transparent,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 15,
                    ShadowDepth = 0,
                    Opacity = 0.5
                }
            };
            RingCanvas.Children.Add(_arcPath);

            _remainingSeconds = _focusMinutes * 60;
            _totalSeconds = _remainingSeconds;

            lstGoals.ItemsSource = _goalsList;
            lstClassProgress.ItemsSource = _studentProgressList;

            UpdateDisplay();
            DrawRing(1.0);

            Loaded += (_, _) =>
            {
                // Highlight default presets
                HighlightPresetButton(btnPreset25);
                HighlightBreakButton(btnBreak5);
                ShowRandomQuote();
                LoadPracticalApps();
                UpdateControlsState();

                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Ghi chú" : "Study Guide & Notes";
                if (menuTextFocus != null) menuTextFocus.Text = isVN ? "Tập trung" : "Focus Zone";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                if (sideMenu != null)
                {
                    sideMenu.SelectedIndex = 0; // Default to main Focus timer workspace
                }

                // Đăng ký nhận tin nhắn nếu là Giáo viên
                var app = Application.Current as QASmartTouch.App;
                if (app?.UserRoleService?.CurrentRole == QASmartClass.Shared.UserRole.Teacher && app.NetworkService != null)
                {
                    app.NetworkService.MessageReceived += NetworkService_MessageReceived;
                }

            };
            Unloaded += (_, _) =>
            {
                var app = Application.Current as QASmartTouch.App;
                if (app?.NetworkService != null)
                {
                    app.NetworkService.MessageReceived -= NetworkService_MessageReceived;
                }
                Dispose();
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  TIMER LOGIC
        // ═══════════════════════════════════════════════════════════

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_remainingSeconds <= 0)
            {
                _remainingSeconds = 0;
                _timer.Stop();
                _isRunning = false;
            }
            else
            {
                _remainingSeconds--;
            }

            if (_remainingSeconds <= 0)
            {
                _remainingSeconds = 0;
                _timer.Stop();
                _isRunning = false;

                PlayAlertSound();

                if (!_isBreak)
                {
                    // Focus session completed -> switch to break
                    _sessionCount++;
                    _isBreak = true;
                    int breakSecs = _breakMinutes * 60;
                    if (_sessionCount > 0 && _sessionCount % 4 == 0)
                    {
                        breakSecs = 15 * 60; // 15 mins long break
                        txtSessionLabel.Text = QASmartClass.Shared.LanguageManager.Get("FocusTimer_LongBreak");
                        btnStart.Content = QASmartClass.Shared.LanguageManager.Get("FocusTimer_BtnStartLongBreak");
                    }
                    else
                    {
                        txtSessionLabel.Text = QASmartClass.Shared.LanguageManager.Get("FocusTimer_StatusBreakCompleted");
                        btnStart.Content = QASmartClass.Shared.LanguageManager.Get("FocusTimer_BtnStartBreak");
                    }
                    _remainingSeconds = breakSecs;
                    _totalSeconds = _remainingSeconds;
                    txtSessionLabel.Foreground = new SolidColorBrush(_breakColor);
                    btnStart.Background = new SolidColorBrush(_breakColor);

                    // Ghi nhận hoàn thành phiên tập trung bằng Telemetry
                    QASmartClass.Services.TelemetryService.Instance.Track("FOCUS_SESSION_COMPLETED", new
                    {
                        FocusMinutes = _focusMinutes,
                        BreakMinutes = _breakMinutes,
                        SessionCount = _sessionCount
                    }, _focusMinutes * 60 * 1000);

                    // Ghi nhận vào SQLite EventLogs cục bộ
                    int completedGoalsCount = 0;
                    foreach (var g in _goalsList)
                    {
                        if (g.IsCompleted) completedGoalsCount++;
                    }
                    SaveSessionToDb(_focusMinutes, _breakMinutes, _sessionCount, completedGoalsCount, _goalsList.Count);

                    if (chkAutoStartBreak.IsChecked == true)
                    {
                        _timer.Start();
                        _isRunning = true;
                        bool isLongBreak = (_sessionCount > 0 && _sessionCount % 4 == 0);
                        txtSessionLabel.Text = isLongBreak
                            ? QASmartClass.Shared.LanguageManager.Get("FocusTimer_StatusLongBreaking")
                            : QASmartClass.Shared.LanguageManager.Get("FocusTimer_StatusBreaking");
                        btnStart.Content = QASmartClass.Shared.LanguageManager.Get("FocusTimer_BtnPause");
                    }
                }
                else
                {
                    // Break completed -> ready for next focus
                    _isBreak = false;
                    if (_sessionCount > 0 && _sessionCount % 4 == 0)
                    {
                        _sessionCount = 0; // Reset session count after long break completed
                    }
                    _remainingSeconds = _focusMinutes * 60;
                    _totalSeconds = _remainingSeconds;
                    txtSessionLabel.Text = QASmartClass.Shared.LanguageManager.Get("FocusTimer_StatusBreakOver");
                    txtSessionLabel.Foreground = new SolidColorBrush(_focusColor);
                    btnStart.Content = QASmartClass.Shared.LanguageManager.Get("FocusTimer_BtnStart");
                    btnStart.Background = new SolidColorBrush(_focusColor);
                    ShowRandomQuote();
                }
            }

            UpdateControlsState();
            UpdateDisplay();
            double progress = (_totalSeconds > 0) ? (double)_remainingSeconds / _totalSeconds : 0;
            DrawRing(progress);
            
            if (_remainingSeconds % 10 == 0 || _remainingSeconds == 0)
            {
                BroadcastTimerState();
            }
        }

        private void PlayAlertSound()
        {
            try
            {
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Không thể phát âm báo kết thúc phiên: " + ex.Message);
            }
        }

        private void UpdateDisplay()
        {
            int displaySeconds = System.Math.Max(0, _remainingSeconds);
            int min = displaySeconds / 60;
            int sec = displaySeconds % 60;
            txtTime.Text = $"{min:D2}:{sec:D2}";
            txtSessionCount.Text = string.Format(QASmartClass.Shared.LanguageManager.Get("FocusTimer_SessionCountFormat"), _sessionCount);
        }

        // ═══════════════════════════════════════════════════════════
        //  RING DRAWING (Arc using ArcSegment)
        // ═══════════════════════════════════════════════════════════

        private void DrawRing(double progress)
        {
            if (progress <= 0)
            {
                _arcPath.Visibility = Visibility.Collapsed;
                if (_thumbEllipse != null) _thumbEllipse.Visibility = Visibility.Collapsed;
                return;
            }
            _arcPath.Visibility = Visibility.Visible;
            if (progress > 1) progress = 1;

            double cx = 200, cy = 200, r = 180;

            // Start from top (12 o'clock)
            double startAngle = -90;
            double sweepAngle = progress * 360;

            if (sweepAngle >= 360) sweepAngle = 359.99;

            double endAngleRad = (startAngle + sweepAngle) * System.Math.PI / 180;
            double startRad = startAngle * System.Math.PI / 180;

            double x1 = cx + r * System.Math.Cos(startRad);
            double y1 = cy + r * System.Math.Sin(startRad);
            double x2 = cx + r * System.Math.Cos(endAngleRad);
            double y2 = cy + r * System.Math.Sin(endAngleRad);

            bool isLargeArc = sweepAngle > 180;

            var figure = new PathFigure
            {
                StartPoint = new Point(x1, y1),
                IsClosed = false
            };
            figure.Segments.Add(new ArcSegment
            {
                Point = new Point(x2, y2),
                Size = new Size(r, r),
                IsLargeArc = isLargeArc,
                SweepDirection = SweepDirection.Clockwise
            });

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            Color ringColor = _isBreak ? _breakColor : _focusColor;
            if (!_isBreak)
            {
                if (progress > 0.5) ringColor = Color.FromRgb(76, 175, 80); // Green
                else if (progress > 0.15) ringColor = Color.FromRgb(255, 152, 0); // Orange
                else ringColor = Color.FromRgb(244, 67, 54); // Red
            }

            // Cập nhật lại các thuộc tính của đối tượng vẽ cũ thay vì tạo mới
            _arcPath.Data = geometry;
            _arcPath.Stroke = new SolidColorBrush(ringColor);
            if (_arcPath.Effect is System.Windows.Media.Effects.DropShadowEffect shadow)
            {
                shadow.Color = ringColor;
            }

            // Draw thumb handle
            if (_thumbEllipse == null)
            {
                _thumbEllipse = new Ellipse
                {
                    Width = 16,
                    Height = 16,
                    Fill = Brushes.White,
                    StrokeThickness = 2.5,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = 8,
                        ShadowDepth = 0,
                        Opacity = 0.8
                    }
                };
                RingCanvas.Children.Add(_thumbEllipse);
            }

            _thumbEllipse.Stroke = new SolidColorBrush(ringColor);
            if (_thumbEllipse.Effect is System.Windows.Media.Effects.DropShadowEffect thumbShadow)
            {
                thumbShadow.Color = ringColor;
            }

            Canvas.SetLeft(_thumbEllipse, x2 - 8);
            Canvas.SetTop(_thumbEllipse, y2 - 8);
            _thumbEllipse.Visibility = Visibility.Visible;
        }

        // ═══════════════════════════════════════════════════════════
        //  BUTTON HANDLERS & INTERACTION
        // ═══════════════════════════════════════════════════════════

        private bool _isDraggingRing = false;

        private void Ring_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isRunning) return; // Only adjust when paused/stopped
            _isDraggingRing = true;
            RingInteractionOverlay.CaptureMouse();
            UpdateTimerFromMouse(e.GetPosition(RingInteractionOverlay));
        }

        private void Ring_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingRing)
            {
                UpdateTimerFromMouse(e.GetPosition(RingInteractionOverlay));
            }
        }

        private void Ring_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingRing)
            {
                _isDraggingRing = false;
                RingInteractionOverlay.ReleaseMouseCapture();
            }
        }

        private void Ring_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isDraggingRing)
            {
                _isDraggingRing = false;
                RingInteractionOverlay.ReleaseMouseCapture();
            }
        }

        private void UpdateTimerFromMouse(Point p)
        {
            double cx = 200, cy = 200;
            double dx = p.X - cx;
            double dy = p.Y - cy;

            // Kiểm tra khoảng cách để tránh tương tác nhảy số bất ngờ khi click gần tâm
            double distance = System.Math.Sqrt(dx * dx + dy * dy);
            if (distance < 140 || distance > 220) return;

            double angle = System.Math.Atan2(dy, dx) * 180 / System.Math.PI; // -180 to 180
            
            // Adjust so 0 is at top (-90)
            angle += 90;
            if (angle < 0) angle += 360; // 0 to 360

            // Max 60 mins -> 360 deg. 1 min = 6 deg.
            int mins = (int)System.Math.Round(angle / 6.0);
            if (mins <= 0) mins = 1;
            if (mins > 60) mins = 60;

            if (_isBreak)
            {
                _breakMinutes = mins;
                _remainingSeconds = _breakMinutes * 60;
                _totalSeconds = _remainingSeconds;
                UpdateDisplay();
                DrawRing(1.0);
                UpdatePresetHighlightForMins(_breakMinutes, true);
            }
            else
            {
                _focusMinutes = mins;
                _remainingSeconds = _focusMinutes * 60;
                _totalSeconds = _remainingSeconds;
                UpdateDisplay();
                DrawRing(1.0);
                UpdatePresetHighlightForMins(_focusMinutes, false);
            }
            BroadcastTimerState();
        }

        private void UpdatePresetHighlightForMins(int mins, bool isBreak)
        {
            WrapPanel? wp = isBreak 
                ? (btnBreak5?.Parent as WrapPanel) 
                : (btnPreset25?.Parent as WrapPanel);
            if (wp == null) return;

            foreach (var child in wp.Children)
            {
                if (child is Button b && b.Tag is string tag && int.TryParse(tag, out int val))
                {
                    if (val == mins)
                    {
                        if (isBreak)
                        {
                            b.Background = new SolidColorBrush(Color.FromRgb(245, 124, 0));
                            b.Foreground = Brushes.White;
                        }
                        else
                        {
                            b.Background = new SolidColorBrush(Color.FromRgb(56, 142, 60));
                            b.Foreground = Brushes.White;
                        }
                    }
                    else
                    {
                        b.Background = new SolidColorBrush(Color.FromRgb(42, 42, 42));
                        b.Foreground = new SolidColorBrush(Color.FromRgb(238, 238, 238));
                    }
                }
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                // Pause
                _timer.Stop();
                _isRunning = false;
                btnStart.Content = QASmartClass.Shared.LanguageManager.Get("FocusTimer_BtnResume");
                txtSessionLabel.Text = QASmartClass.Shared.LanguageManager.Get("FocusTimer_StatusPaused");
                txtSessionLabel.Foreground = new SolidColorBrush(Color.FromRgb(255, 183, 77));
            }
            else
            {
                // Start / Resume
                _timer.Start();
                _isRunning = true;
                btnStart.Content = QASmartClass.Shared.LanguageManager.Get("FocusTimer_BtnPause");
                if (!_isBreak)
                {
                    txtSessionLabel.Text = QASmartClass.Shared.LanguageManager.Get("FocusTimer_StatusFocusing");
                    txtSessionLabel.Foreground = new SolidColorBrush(_focusColor);
                    btnStart.Background = new SolidColorBrush(Color.FromRgb(56, 142, 60));
                }
                else
                {
                    bool isLongBreak = (_sessionCount > 0 && _sessionCount % 4 == 0);
                    txtSessionLabel.Text = isLongBreak
                        ? QASmartClass.Shared.LanguageManager.Get("FocusTimer_StatusLongBreaking")
                        : QASmartClass.Shared.LanguageManager.Get("FocusTimer_StatusBreaking");
                    txtSessionLabel.Foreground = new SolidColorBrush(_breakColor);
                    btnStart.Background = new SolidColorBrush(Color.FromRgb(245, 124, 0));
                }
            }
            UpdateControlsState();
            BroadcastTimerState();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _isRunning = false;
            _isBreak = false;
            _sessionCount = 0;
            _remainingSeconds = _focusMinutes * 60;
            _totalSeconds = _remainingSeconds;

            btnStart.Content = QASmartClass.Shared.LanguageManager.Get("FocusTimer_BtnStart");
            btnStart.Background = new SolidColorBrush(_focusColor);
            txtSessionLabel.Text = QASmartClass.Shared.LanguageManager.Get("FocusTimer_StatusReady");
            txtSessionLabel.Foreground = new SolidColorBrush(Color.FromRgb(100, 181, 246));

            // Dừng nhạc nền và reset trạng thái nút bấm
            _currentAmbientTag = "";
            try
            {
                _ambientPlayer?.Stop();
            }
            catch {}

            btnSoundRain.IsChecked = false;
            btnSoundFire.IsChecked = false;
            btnSoundWave.IsChecked = false;
            btnSoundBird.IsChecked = false;

            UpdateDisplay();
            DrawRing(1.0);
            ShowRandomQuote();

            UpdateControlsState();
            BroadcastTimerState();
        }

        private void PresetTime_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning) return; // Don't change while running
            if (sender is Button btn && btn.Tag is string tag && int.TryParse(tag, out int min))
            {
                _focusMinutes = min;
                if (!_isBreak)
                {
                    _remainingSeconds = _focusMinutes * 60;
                    _totalSeconds = _remainingSeconds;
                    UpdateDisplay();
                    DrawRing(1.0);
                }
                HighlightPresetButton(btn);
                BroadcastTimerState();
            }
        }

        private void PresetBreak_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning) return;
            if (sender is Button btn && btn.Tag is string tag && int.TryParse(tag, out int min))
            {
                _breakMinutes = min;
                if (_isBreak)
                {
                    _remainingSeconds = _breakMinutes * 60;
                    _totalSeconds = _remainingSeconds;
                    UpdateDisplay();
                    DrawRing(1.0);
                }
                HighlightBreakButton(btn);
                BroadcastTimerState();
            }
        }

        private void HighlightPresetButton(Button active)
        {
            // Reset all siblings
            if (active.Parent is WrapPanel wp)
            {
                foreach (var child in wp.Children)
                {
                    if (child is Button b)
                    {
                        b.Background = new SolidColorBrush(Color.FromRgb(42, 42, 42));
                        b.Foreground = new SolidColorBrush(Color.FromRgb(238, 238, 238));
                    }
                }
            }
            active.Background = new SolidColorBrush(Color.FromRgb(56, 142, 60));
            active.Foreground = Brushes.White;
        }

        private void HighlightBreakButton(Button active)
        {
            if (active.Parent is WrapPanel wp)
            {
                foreach (var child in wp.Children)
                {
                    if (child is Button b)
                    {
                        b.Background = new SolidColorBrush(Color.FromRgb(42, 42, 42));
                        b.Foreground = new SolidColorBrush(Color.FromRgb(238, 238, 238));
                    }
                }
            }
            active.Background = new SolidColorBrush(Color.FromRgb(245, 124, 0));
            active.Foreground = Brushes.White;
        }

        // ═══════════════════════════════════════════════════════════
        //  AMBIENT SOUND
        // ═══════════════════════════════════════════════════════════

        private void AmbientSound_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton btn)
            {
                string tag = btn.Tag?.ToString() ?? "";

                // Uncheck other ambient buttons
                foreach (var sibling in new[] { btnSoundRain, btnSoundFire, btnSoundWave, btnSoundBird })
                {
                    if (sibling != btn) sibling.IsChecked = false;
                }

                if (btn.IsChecked == true)
                {
                    _currentAmbientTag = tag;
                    string soundPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio", $"{tag}.mp3");
                    if (System.IO.File.Exists(soundPath))
                    {
                        try
                        {
                            _ambientPlayer.Open(new Uri(soundPath));
                            _ambientPlayer.Volume = sliderVolume.Value / 100.0;
                            _ambientPlayer.Play();
                        }
                        catch (Exception ex)
                        {
                            Serilog.Log.Error(ex, "Lỗi khi phát nhạc nền: {Tag}", tag);
                        }
                    }
                    else
                    {
                        Serilog.Log.Warning("Không tìm thấy tệp âm thanh nền tại: {Path}", soundPath);
                        btn.IsChecked = false; // Nhả nút ra để thông báo trực quan tệp không sẵn sàng
                    }
                }
                else
                {
                    _currentAmbientTag = "";
                    try
                    {
                        _ambientPlayer.Stop();
                    }
                    catch {}
                }
            }
        }

        private void AmbientPlayer_MediaEnded(object? sender, EventArgs e)
        {
            if (_ambientPlayer != null && !string.IsNullOrEmpty(_currentAmbientTag))
            {
                try
                {
                    _ambientPlayer.Position = TimeSpan.Zero;
                    _ambientPlayer.Play();
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Lỗi lặp âm thanh nền");
                }
            }
        }

        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_ambientPlayer != null)
            {
                _ambientPlayer.Volume = e.NewValue / 100.0;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  QUOTES
        // ═══════════════════════════════════════════════════════════

        private void ShowRandomQuote()
        {
            var rnd = new Random();
            if (txtQuote != null)
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                var list = isVN ? Quotes : QuotesEN;
                txtQuote.Text = list[rnd.Next(list.Length)];
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  CLEANUP
        // ═══════════════════════════════════════════════════════════

        private void LoadPracticalApps()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            string suffix = isVN ? "VN" : "EN";

            var items = new List<PracticalAppItem>
            {
                new PracticalAppItem
                {
                    Icon = "📝",
                    Title = isVN ? "Luyện thi & Học tập Sâu" : "Deep Study & Exam Prep",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_focustimer_1_{suffix}.png",
                    Description = QASmartClass.Shared.LanguageManager.Get("FocusTimer_AppDescription1")
                },
                new PracticalAppItem
                {
                    Icon = "💻",
                    Title = isVN ? "Lập trình & Thiết kế Sprint" : "Software & Creative Sprints",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_focustimer_2_{suffix}.png",
                    Description = QASmartClass.Shared.LanguageManager.Get("FocusTimer_AppDescription2")
                },
                new PracticalAppItem
                {
                    Icon = "🧘",
                    Title = isVN ? "Cân bằng & Sức khỏe" : "Workplace Wellness",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_focustimer_3_{suffix}.png",
                    Description = QASmartClass.Shared.LanguageManager.Get("FocusTimer_AppDescription3")
                }
            };

            try
            {
                if (practicalAppViewer != null) practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for FocusTimerTool: {Err}", ex.Message);
            }
        }

        public bool IsStudentModeAndFocused()
        {
            if (QASmartClass.LearningTools.Helpers.TeachingActionHelper.IsStudent())
            {
                return QASmartClass.Services.ClassControlService.Instance.ActiveToolFocusId == "focus_timer";
            }
            return QASmartClass.Services.LessonStateService.Instance.ActiveToolFocusId == "focus_timer";
        }

        public void SyncFromTeacher(int focusMins, int breakMins, int remainingSecs, bool isRunning, bool isBreak, int sessionCount, bool autoStartBreak = false, string base64Goals = "")
        {
            _focusMinutes = focusMins;
            _breakMinutes = breakMins;

            // Chống hiển thị thời gian âm bằng cách chặn dưới bằng 0
            int sanitizedRemaining = System.Math.Max(0, remainingSecs);

            // Timer Drift Filter:
            // Nếu đồng hồ đang chạy và không đổi trạng thái chế độ đếm ngược,
            // ta chỉ đồng bộ lại nếu độ lệch thời gian lớn hơn 3 giây.
            // Điều này giúp giây đếm ngược chạy mượt mà, không bị giật lùi/nhảy số do độ trễ truyền gói tin.
            bool stateChanged = (_isBreak != isBreak) || (_isRunning != isRunning);
            if (_isRunning && !stateChanged && System.Math.Abs(_remainingSeconds - sanitizedRemaining) < 1.5)
            {
                // Giữ nguyên thời gian đếm ngược cục bộ để tránh giật hình
            }
            else
            {
                _remainingSeconds = sanitizedRemaining;
            }

            _isRunning = isRunning;
            _isBreak = isBreak;
            _sessionCount = sessionCount;
            _totalSeconds = _isBreak ? ((_sessionCount > 0 && _sessionCount % 4 == 0) ? 15 * 60 : _breakMinutes * 60) : _focusMinutes * 60;

            if (chkAutoStartBreak != null)
            {
                chkAutoStartBreak.Checked -= ChkAutoStartBreak_Changed;
                chkAutoStartBreak.Unchecked -= ChkAutoStartBreak_Changed;
                chkAutoStartBreak.IsChecked = autoStartBreak;
                chkAutoStartBreak.Checked += ChkAutoStartBreak_Changed;
                chkAutoStartBreak.Unchecked += ChkAutoStartBreak_Changed;
            }

            if (!string.IsNullOrEmpty(base64Goals))
            {
                try
                {
                    var bytes = Convert.FromBase64String(base64Goals);
                    var json = System.Text.Encoding.UTF8.GetString(bytes);
                    var list = System.Text.Json.JsonSerializer.Deserialize<List<FocusGoalItem>>(json);
                    if (list != null)
                    {
                        _goalsList.Clear();
                        foreach (var item in list)
                        {
                            _goalsList.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning("Error deserializing goals in SyncFromTeacher: " + ex.Message);
                }
            }
            else
            {
                _goalsList.Clear();
            }

            // Khóa/Mở UI học sinh
            UpdateControlsState();

            // Cập nhật giao diện đếm ngược
            UpdateDisplay();
            double progress = (_totalSeconds > 0) ? (double)_remainingSeconds / _totalSeconds : 0;
            DrawRing(progress);

            // Đồng bộ trạng thái chạy timer cục bộ của HS
            if (_isRunning)
            {
                _timer.Start();
                bool isLongBreak = (_isBreak && _sessionCount > 0 && _sessionCount % 4 == 0);
                txtSessionLabel.Text = _isBreak
                    ? (isLongBreak ? QASmartClass.Shared.LanguageManager.Get("FocusTimer_StatusLongBreaking") : QASmartClass.Shared.LanguageManager.Get("FocusTimer_StatusBreaking"))
                    : QASmartClass.Shared.LanguageManager.Get("FocusTimer_StatusFocusing");
                txtSessionLabel.Foreground = new SolidColorBrush(_isBreak ? _breakColor : _focusColor);
            }
            else
            {
                _timer.Stop();
                txtSessionLabel.Text = QASmartClass.Shared.LanguageManager.Get("FocusTimer_StatusPaused");
                txtSessionLabel.Foreground = new SolidColorBrush(Color.FromRgb(255, 183, 77));
            }
        }

        private void UpdateControlsState()
        {
            if (IsStudentModeAndFocused())
            {
                btnStart.Visibility = Visibility.Collapsed;
                btnReset.Visibility = Visibility.Collapsed;
                sliderVolume.IsEnabled = false;
                btnSoundRain.IsEnabled = false;
                btnSoundFire.IsEnabled = false;
                btnSoundWave.IsEnabled = false;
                btnSoundBird.IsEnabled = false;
                RingInteractionOverlay.Visibility = Visibility.Collapsed; // Chặn kéo xoay
                gridGoalInput.IsEnabled = false;
                chkAutoStartBreak.IsEnabled = false;
                DeleteGoalVisibility = Visibility.Collapsed; // Khóa nút xóa Goal
            }
            else
            {
                btnStart.Visibility = Visibility.Visible;
                btnReset.Visibility = Visibility.Visible;
                sliderVolume.IsEnabled = true;
                btnSoundRain.IsEnabled = true;
                btnSoundFire.IsEnabled = true;
                btnSoundWave.IsEnabled = true;
                btnSoundBird.IsEnabled = true;
                RingInteractionOverlay.Visibility = Visibility.Visible; // Cho phép kéo xoay
                RingInteractionOverlay.Cursor = _isRunning ? Cursors.Arrow : Cursors.Hand;
                gridGoalInput.IsEnabled = true;
                chkAutoStartBreak.IsEnabled = true;
                DeleteGoalVisibility = Visibility.Visible;
            }
        }

        private void BroadcastTimerState()
        {
            try
            {
                var app = Application.Current as QASmartTouch.App;
                if (app?.UserRoleService?.CurrentRole == QASmartClass.Shared.UserRole.Teacher)
                {
                    var net = app.NetworkService;
                    bool autoStart = chkAutoStartBreak.IsChecked == true;
                    string base64Goals = "";
                    try
                    {
                        var list = new List<FocusGoalItem>(_goalsList);
                        var json = System.Text.Json.JsonSerializer.Serialize(list);
                        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                        base64Goals = Convert.ToBase64String(bytes);
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Warning("Error serializing goals for broadcast: " + ex.Message);
                    }

                    var cmd = $"CMD|FOCUS_TIMER_SYNC|{_focusMinutes}|{_breakMinutes}|{_remainingSeconds}|{_isRunning}|{_isBreak}|{_sessionCount}|{autoStart}|{base64Goals}";
                    if (net?.IsBroadcasting == true)
                    {
                        _ = net.SendCommandAsync(cmd);
                    }
                    else
                    {
                        app.RaiseLocalCommand(cmd);
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Lỗi phát sóng trạng thái đồng hồ: " + ex.Message);
            }
        }

        private void ChkAutoStartBreak_Changed(object sender, RoutedEventArgs e)
        {
            BroadcastTimerState();
        }

        private void GoalCheck_Changed(object sender, RoutedEventArgs e)
        {
            var app = Application.Current as QASmartTouch.App;
            if (app?.UserRoleService?.CurrentRole == QASmartClass.Shared.UserRole.Teacher)
            {
                BroadcastTimerState();
            }

        }

        private void TxtNewGoal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddGoalFromInput();
                e.Handled = true;
            }
        }

        private void BtnAddGoal_Click(object sender, RoutedEventArgs e)
        {
            AddGoalFromInput();
        }

        private void BtnDeleteGoal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is FocusGoalItem item)
            {
                _goalsList.Remove(item);
                BroadcastTimerState();
            }
        }

        private void AddGoalFromInput()
        {
            if (txtNewGoal == null) return;
            string text = txtNewGoal.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(text))
            {
                var item = new FocusGoalItem { Text = text, IsCompleted = false };
                _goalsList.Add(item);
                txtNewGoal.Text = "";
                BroadcastTimerState();
            }
        }

        private void NetworkService_MessageReceived(object? sender, QASmartClass.Classroom.Services.StudentMessageEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Message)) return;

            if (e.Message.StartsWith("CMD|FOCUS_GOAL_SYNC|"))
            {
                var parts = e.Message.Split('|');
                // Kiểm tra chính xác 6 phần tử để ngăn chặn lỗi phân tích cú pháp hoặc tiêm nhiễm gói tin giả mạo
                if (parts.Length == 6)
                {
                    string stuCode = parts[2];
                    string stuName = parts[3];
                    if (int.TryParse(parts[4], out int goalIndex) && bool.TryParse(parts[5], out bool isCompleted))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            UpdateStudentProgress(stuCode, stuName, goalIndex, isCompleted);
                        });
                    }
                }
                else
                {
                    Serilog.Log.Warning("Gói tin FOCUS_GOAL_SYNC bị lỗi định dạng phân tách: {Msg}", e.Message);
                }
            }
            else if (e.Message == "CMD|REQUEST_FOCUS_SYNC")
            {
                Dispatcher.Invoke(() =>
                {
                    BroadcastTimerState();
                });
            }
        }

        private void UpdateStudentProgress(string stuCode, string stuName, int goalIndex, bool isCompleted)
        {
            if (_goalsList.Count == 0) return;

            if (!_studentProgressMap.TryGetValue(stuCode, out var item))
            {
                item = new StudentProgressItem
                {
                    StudentCode = stuCode,
                    StudentName = stuName,
                    GoalStates = new List<bool>()
                };
                _studentProgressMap[stuCode] = item;
                _studentProgressList.Add(item);
            }

            while (item.GoalStates.Count < _goalsList.Count)
            {
                item.GoalStates.Add(false);
            }
            if (item.GoalStates.Count > _goalsList.Count)
            {
                item.GoalStates = item.GoalStates.GetRange(0, _goalsList.Count);
            }

            if (goalIndex >= 0 && goalIndex < item.GoalStates.Count)
            {
                item.GoalStates[goalIndex] = isCompleted;
            }

            int idx = _studentProgressList.IndexOf(item);
            if (idx >= 0)
            {
                _studentProgressList[idx] = null!;
                _studentProgressList[idx] = item;
            }
        }

        private void BtnClassProgress_Click(object sender, RoutedEventArgs e)
        {
            if (popupProgress != null)
            {
                popupProgress.IsOpen = true;
            }
        }

        private void BtnCloseProgress_Click(object sender, RoutedEventArgs e)
        {
            if (popupProgress != null)
            {
                popupProgress.IsOpen = false;
            }
        }

        private void SaveSessionToDb(int focusMins, int breakMins, int sessionCount, int completedGoals, int totalGoals)
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    using (var db = new QASmartClass.Data.AppDbContext())
                    {
                        var log = new QASmartClass.Data.EventLog
                        {
                            EventType = "FOCUS_TIMER_SESSION_RECORD",
                            Actor = QASmartClass.LearningTools.Helpers.TeachingActionHelper.IsStudent() ? "Student" : "Teacher",
                            Details = $"{{\"FocusMins\":{focusMins},\"BreakMins\":{breakMins},\"TotalSessions\":{sessionCount},\"GoalsCompleted\":{completedGoals},\"GoalsTotal\":{totalGoals}}}",
                            Timestamp = DateTime.Now
                        };
                        db.EventLogs.Add(log);
                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Lỗi khi lưu lịch sử phiên tập trung vào CSDL SQLite bất đồng bộ");
                }
            });
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewPractice == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewPractice.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewPractice.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }

        public void Dispose()
        {
            _timer.Stop();
            _ambientPlayer?.Stop();
            _ambientPlayer?.Close();
        }
    }

    public class FocusGoalItem : System.ComponentModel.INotifyPropertyChanged
    {
        private string _text = "";
        private bool _isCompleted;

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged(nameof(Text));
                }
            }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    OnPropertyChanged(nameof(IsCompleted));
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}