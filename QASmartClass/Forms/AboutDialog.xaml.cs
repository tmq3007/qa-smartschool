using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace QASmartTouch.Forms
{
    /// <summary>
    /// About Dialog — Giới thiệu QA SmartTouch
    /// Hiển thị version, platform, thông tin nhà phát triển ngắn gọn
    /// </summary>
    public partial class AboutDialog : Window
    {
        public AboutDialog()
        {
            InitializeComponent();
            QASmartTouch.Helpers.TouchActivationHelper.ApplyToWindow(this);
            LoadVersionInfo();
        }

        private void LoadVersionInfo()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                tbVersion.Text = $"{version?.Major ?? 1}.{version?.Minor ?? 0}.{version?.Build ?? 0}";
            }
            catch
            {
                tbVersion.Text = "1.0.0";
            }

            try
            {
                string asmPath = Assembly.GetExecutingAssembly().Location;
                tbBuildDate.Text = !string.IsNullOrEmpty(asmPath) && System.IO.File.Exists(asmPath)
                    ? System.IO.File.GetLastWriteTime(asmPath).ToString("MM/yyyy")
                    : "—";
            }
            catch
            {
                tbBuildDate.Text = "—";
            }

            tbPlatform.Text = $"WPF / .NET {Environment.Version.Major}.{Environment.Version.Minor} (x64)";

            int build = Environment.OSVersion.Version.Build;
            tbOS.Text = build >= 22000 ? "Windows 11 (64-bit)" : $"Windows {Environment.OSVersion.Version.Major} (64-bit)";
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Website_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://qatmc.com") { UseShellExecute = true });
            }
            catch { }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
