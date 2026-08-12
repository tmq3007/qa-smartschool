using System;
using System.Windows;
using System.Windows.Threading;
using SmartLibrary.Desktop.Views.Shared;

namespace SmartLibrary.Desktop.Services
{
    public static class ToastService
    {
        public static void ShowSuccess(string message)
        {
            Show(message, "✅", "#10B981");
        }

        public static void ShowError(string message)
        {
            Show(message, "❌", "#EF4444");
        }

        public static void ShowWarning(string message)
        {
            Show(message, "⚠️", "#F59E0B");
        }

        public static void ShowInfo(string message)
        {
            Show(message, "ℹ️", "#3B82F6");
        }

        private static void Show(string message, string icon, string bgHex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWin = Application.Current.MainWindow as MainWindow;
                if (mainWin == null) return;

                var container = mainWin.FindName("ToastContainer") as System.Windows.Controls.StackPanel;
                if (container == null) return;

                var toast = new ToastNotificationControl();
                toast.Setup(message, icon, bgHex);

                toast.OnCloseRequested += (ctrl) =>
                {
                    container.Children.Remove(ctrl);
                };

                container.Children.Add(toast);

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    toast.Dismiss();
                };
                timer.Start();
            });
        }
    }
}
