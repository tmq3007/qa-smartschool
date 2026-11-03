using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;

namespace QASmartClass.Classroom.Views
{
    public class BroadcastPrivacyBorder : Window
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        private readonly Border _border;

        public BroadcastPrivacyBorder()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            IsHitTestVisible = false;
            ShowActivated = false;

            // Align to primary screen bounds by default
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;

            _border = new Border
            {
                BorderThickness = new Thickness(3),
                BorderBrush = Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            Content = _border;

            SourceInitialized += (_, _) =>
            {
                try
                {
                    var hwnd = new WindowInteropHelper(this).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        // Ẩn chính đường viền đỏ khỏi ảnh chụp màn hình để học sinh không thấy viền đỏ
                        SetWindowDisplayAffinity(hwnd, 0x00000011U);
                    }
                }
                catch { }
            };
        }

        /// <summary>Cập nhật vị trí khung viền theo vùng màn hình được chọn (hoặc toàn màn hình).</summary>
        public void SetBounds(double dipLeft, double dipTop, double dipWidth, double dipHeight)
        {
            Dispatcher.Invoke(() =>
            {
                if (dipWidth > 20 && dipHeight > 20)
                {
                    Left = dipLeft;
                    Top = dipTop;
                    Width = dipWidth;
                    Height = dipHeight;
                }
                else
                {
                    Left = 0;
                    Top = 0;
                    Width = SystemParameters.PrimaryScreenWidth;
                    Height = SystemParameters.PrimaryScreenHeight;
                }
            });
        }

        public void SetState(bool isPaused)
        {
            Dispatcher.Invoke(() =>
            {
                if (isPaused)
                {
                    // Softer orange-yellow brush for paused state
                    _border.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 124, 0));
                }
                else
                {
                    _border.BorderBrush = Brushes.Red;
                }
            });
        }
    }
}
