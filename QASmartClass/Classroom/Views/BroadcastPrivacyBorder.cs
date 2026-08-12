using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.Classroom.Views
{
    public class BroadcastPrivacyBorder : Window
    {
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

            // Align to primary screen bounds
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
