using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace QASmartClass.Controls
{
    public partial class NotificationWindow : Window
    {
        /// <summary>
        /// Loại âm thanh cảnh báo sư phạm (v4.4 — thay thế magic string).
        /// </summary>
        public enum AlertSoundType
        {
            /// <summary>Âm thanh nhẹ nhàng báo thành công (System.Media.SystemSounds.Asterisk).</summary>
            Success,
            /// <summary>Âm thanh trầm cảnh báo lỗi mạng (System.Media.SystemSounds.Hand).</summary>
            Warning
        }

        public class FailedStudentItem
        {
            public string Name { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
        }

        private DispatcherTimer? _autoCloseTimer;
        private Action<string>? _onResendClicked;
        public Action<List<string>>? OnResendAllClicked;
        private List<FailedStudentItem> _failedList = new List<FailedStudentItem>();

        public NotificationWindow()
        {
            InitializeComponent();
            QASmartTouch.Helpers.InputValidationHelper.ApplyTouchIsolation(this);
            Loaded += NotificationWindow_Loaded;
            Unloaded += NotificationWindow_Unloaded;
        }

        private void NotificationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Định vị trí cửa sổ ở góc dưới cùng bên phải màn hình làm việc
            double screenWidth = SystemParameters.WorkArea.Width;
            double screenHeight = SystemParameters.WorkArea.Height;
            this.Left = Math.Max(0, screenWidth - this.ActualWidth - 20);
            this.Top = Math.Max(0, screenHeight - this.ActualHeight - 20);
        }

        private void NotificationWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            StopTimer();
        }

        private void StopTimer()
        {
            if (_autoCloseTimer != null)
            {
                _autoCloseTimer.Stop();
                _autoCloseTimer = null;
            }
        }

        // ═══════════════════════════════════════════════════════
        //  CÁC PHƯƠNG THỨC THAY ĐỔI TRẠNG THÁI GIAO DIỆN
        // ═══════════════════════════════════════════════════════

        public void SetStatusProcessing(string title, string desc)
        {
            StopTimer();
            Dispatcher.Invoke(() =>
            {
                // Chuyển màu nền sang xanh dương nhạt (Processing)
                MainBorder.Background = new LinearGradientBrush(
                    Color.FromRgb(227, 242, 253), Color.FromRgb(187, 222, 250), 45);
                AnimateBorderBrush(Color.FromRgb(21, 101, 192));
                
                txtIcon.Text = "🔄";
                lblTitle.Text = title;
                lblDescription.Text = desc;
                
                progressAction.Visibility = Visibility.Visible;
                progressAction.IsIndeterminate = true;
                
                btnTroubleshoot.Visibility = Visibility.Collapsed;
                panelTroubleshoot.Visibility = Visibility.Collapsed;
                lblError.Visibility = Visibility.Collapsed;
            });
        }

        public void UpdateProgress(double percent)
        {
            Dispatcher.Invoke(() =>
            {
                progressAction.IsIndeterminate = false;
                progressAction.Value = percent;
            });
        }

        public void SetStatusSuccess(string title, string desc, int autoCloseMs = 2000)
        {
            StopTimer();
            PlayAlertSound(AlertSoundType.Success);
            Dispatcher.Invoke(() =>
            {
                // Chuyển màu nền sang xanh lá (Success)
                MainBorder.Background = new LinearGradientBrush(
                    Color.FromRgb(232, 245, 233), Color.FromRgb(200, 230, 201), 45);
                AnimateBorderBrush(Color.FromRgb(46, 125, 50));
                
                txtIcon.Text = "✅";
                lblTitle.Text = title;
                lblDescription.Text = desc;
                
                progressAction.Visibility = Visibility.Collapsed;
                btnTroubleshoot.Visibility = Visibility.Collapsed;
                btnResendAll.Visibility = Visibility.Collapsed;
                panelTroubleshoot.Visibility = Visibility.Collapsed;
                lblError.Visibility = Visibility.Collapsed;

                // Tự động đóng sau autoCloseMs
                if (autoCloseMs > 0)
                {
                    _autoCloseTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(autoCloseMs)
                    };
                    _autoCloseTimer.Tick += (s, e) =>
                    {
                        StopTimer();
                        Close();
                    };
                    _autoCloseTimer.Start();
                }
            });
        }

        public void SetStatusWarning(string title, string desc, List<FailedStudentItem> failedList, Action<string> onResend)
        {
            StopTimer();
            _onResendClicked = onResend;
            _failedList = failedList ?? new List<FailedStudentItem>();
            PlayAlertSound(AlertSoundType.Warning);

            Dispatcher.Invoke(() =>
            {
                // Chuyển màu nền sang cam/đỏ nhạt (Warning/Error)
                MainBorder.Background = new LinearGradientBrush(
                    Color.FromRgb(255, 235, 238), Color.FromRgb(255, 205, 210), 45);
                AnimateBorderBrush(Color.FromRgb(198, 40, 40));
                
                txtIcon.Text = "⚠️";
                lblTitle.Text = title;
                lblDescription.Text = desc;
                
                progressAction.Visibility = Visibility.Collapsed;
                lblError.Visibility = Visibility.Collapsed;

                // Hiển thị nút ResendAll nếu có nhiều hơn 1 học sinh lỗi
                btnResendAll.Visibility = (_failedList.Count >= 2) ? Visibility.Visible : Visibility.Collapsed;
                btnResendAll.IsEnabled = true;
                btnResendAll.Content = "Gửi lại tất cả lỗi 🔄";
                btnResendAll.ClearValue(Button.BackgroundProperty);
                btnResendAll.ClearValue(Button.ForegroundProperty);

                if (_failedList.Count > 0)
                {
                    failedStudentsItemsControl.ItemsSource = _failedList;
                    btnTroubleshoot.Visibility = Visibility.Visible;
                }
                else
                {
                    btnTroubleshoot.Visibility = Visibility.Collapsed;
                }
            });
        }

        // ═══════════════════════════════════════════════════════
        //  XỬ LÝ SỰ KIỆN TƯƠNG TÁC
        // ═══════════════════════════════════════════════════════

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Troubleshoot_Click(object sender, RoutedEventArgs e)
        {
            // Toggle hiển thị vùng hướng dẫn khắc phục sự cố lỗi
            if (panelTroubleshoot.Visibility == Visibility.Visible)
            {
                panelTroubleshoot.Visibility = Visibility.Collapsed;
                btnTroubleshoot.Content = "Xem hướng dẫn";
            }
            else
            {
                panelTroubleshoot.Visibility = Visibility.Visible;
                btnTroubleshoot.Content = "Ẩn hướng dẫn";
                
                // Cập nhật lại tọa độ Top để tránh phần mở rộng trôi xuống dưới vùng làm việc
                double screenHeight = SystemParameters.WorkArea.Height;
                // Buộc tính toán lại layout chiều cao mới
                this.UpdateLayout();
                this.Top = Math.Max(0, screenHeight - this.ActualHeight - 20);
            }
        }

        private void ResendItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string studentCode)
            {
                btn.IsEnabled = false;
                btn.Content = "⏳...";
                try
                {
                    _onResendClicked?.Invoke(studentCode);
                    btn.Background = new SolidColorBrush(Color.FromRgb(200, 230, 201)); // Đổi thành màu xanh khi gửi lại xong
                    btn.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                    btn.Content = "Gửi xong ✓";
                }
                catch (Exception ex)
                {
                    btn.IsEnabled = true;
                    btn.Content = "Lỗi 🔄";
                    ShowInlineError($"Không thể gửi lại: {ex.Message}");
                }
            }
        }

        private void ResendAll_Click(object sender, RoutedEventArgs e)
        {
            if (_failedList != null && _failedList.Count > 0)
            {
                btnResendAll.IsEnabled = false;
                btnResendAll.Content = "⏳...";
                try
                {
                    var codes = new List<string>();
                    foreach (var item in _failedList)
                    {
                        codes.Add(item.Code);
                    }
                    OnResendAllClicked?.Invoke(codes);

                    btnResendAll.Background = new SolidColorBrush(Color.FromRgb(200, 230, 201));
                    btnResendAll.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                    btnResendAll.Content = "Gửi lại xong ✓";
                }
                catch (Exception ex)
                {
                    btnResendAll.IsEnabled = true;
                    btnResendAll.Content = "Lỗi 🔄";
                    ShowInlineError($"Không thể gửi lại tất cả: {ex.Message}");
                }
            }
        }

        private void PlayAlertSound(AlertSoundType type)
        {
            try
            {
                if (type == AlertSoundType.Success)
                {
                    System.Media.SystemSounds.Asterisk.Play();
                }
                else if (type == AlertSoundType.Warning)
                {
                    System.Media.SystemSounds.Hand.Play();
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Could not play sound: {Err}", ex.Message);
            }
        }

        /// <summary>
        /// Hiển thị thông báo lỗi inline bên trong NotificationWindow (v4.4 — thay thế MessageBox.Show).
        /// Thông báo sẽ tự ẩn sau 5 giây.
        /// </summary>
        private void ShowInlineError(string message)
        {
            lblError.Text = $"⚠ {message}";
            lblError.Visibility = Visibility.Visible;

            // Tự ẩn thông báo lỗi inline sau 5 giây
            var hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            hideTimer.Tick += (s, e) =>
            {
                hideTimer.Stop();
                lblError.Visibility = Visibility.Collapsed;
            };
            hideTimer.Start();
        }

        /// <summary>
        /// Chuyển đổi màu viền MainBorder với animation mượt mà 0.3 giây (v4.4).
        /// </summary>
        private void AnimateBorderBrush(Color targetColor)
        {
            var animation = new System.Windows.Media.Animation.ColorAnimation
            {
                To = targetColor,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase()
            };
            Color initialColor = Color.FromRgb(21, 101, 192); // default blue
            if (MainBorder.BorderBrush is SolidColorBrush solidBrush)
            {
                initialColor = solidBrush.Color;
            }
            var brush = new SolidColorBrush(initialColor);
            MainBorder.BorderBrush = brush;
            brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }
    }
}
