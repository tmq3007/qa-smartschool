﻿using System.Windows;
using System.Windows.Controls;
using QASmartClass.TeacherHub.Views;

namespace QASmartClass.HomeroomHub.Views
{
    public partial class HomeroomHubWindow : Window
    {
        public HomeroomHubWindow()
        {
            InitializeComponent();
        }

        private void NavFeature_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string feature)
            {
                switch (feature)
                {
                    case "diary":
                        var diaryPage = new HomeroomDiaryPage();
                        var content = this.FindName("MainContent") as ContentControl;
                        // Navigate using ViewModel pattern — set page directly
                        MessageBox.Show("📖 Đang mở Sổ Chủ Nhiệm...", "Điều hướng", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case "attendance":
                        MessageBox.Show("📝 Tính năng Điểm danh lớp\n\nĐang tích hợp dữ liệu từ AttendanceRecords...", "Điểm danh", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case "emulation":
                        MessageBox.Show("🏆 Tính năng Thi đua lớp\n\nĐang tích hợp từ EmulationService...", "Thi đua", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case "stats":
                        MessageBox.Show("📊 Tính năng Thống kê lớp\n\nĐang phát triển dashboard thống kê...", "Thống kê", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }
            }
        }
    }
}

