using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SmartLibrary.Desktop.Views.Shared
{
    public partial class ToastNotificationControl : UserControl
    {
        public event Action<ToastNotificationControl>? OnCloseRequested;

        public ToastNotificationControl()
        {
            InitializeComponent();
            Loaded += ToastNotificationControl_Loaded;
        }

        private void ToastNotificationControl_Loaded(object sender, RoutedEventArgs e)
        {
            var showAnim = Resources["ShowAnimation"] as Storyboard;
            showAnim?.Begin(this);
        }

        public void Setup(string message, string icon, string bgHex)
        {
            MessageText.Text = message;
            IconText.Text = icon;
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(bgHex);
                ToastBorder.Background = new SolidColorBrush(color);
            }
            catch
            {
                ToastBorder.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Emerald-500 default
            }
        }

        public void Dismiss()
        {
            var hideAnim = Resources["HideAnimation"] as Storyboard;
            if (hideAnim != null)
            {
                hideAnim.Completed += (s, e) =>
                {
                    OnCloseRequested?.Invoke(this);
                };
                hideAnim.Begin(this);
            }
            else
            {
                OnCloseRequested?.Invoke(this);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Dismiss();
        }
    }
}
