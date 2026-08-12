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
                tbVersion.Text = $"{version?.Major ?? 3}.{version?.Minor ?? 0}.{version?.Build ?? 0}";
            }
            catch
            {
                tbVersion.Text = "3.0.0";
            }

            // Build date
            tbBuildDate.Text = DateTime.Now.ToString("MM/yyyy");

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
