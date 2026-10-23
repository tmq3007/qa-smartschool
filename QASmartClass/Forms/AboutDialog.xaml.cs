using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace QASmartTouch.Forms
{
    /// <summary>
    /// About Dialog — Giới thiệu QA SmartTouch
    /// Hiển thị version, platform, tính năng, thông tin liên hệ
    /// </summary>
    public partial class AboutDialog : Window
    {
        public AboutDialog()
        {
            InitializeComponent();
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

            // Build date: ngày ghi của tệp thực thi (không dùng DateTime.Now vì sẽ luôn hiện tháng hiện tại)
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

            // Runtime thực tế
            tbPlatform.Text = $"WPF / .NET {Environment.Version.Major}.{Environment.Version.Minor}";

            // OS info
            tbOS.Text = $"Windows {Environment.OSVersion.Version.Major}";
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
