using System;
using System.Windows;
using System.Windows.Input;

namespace SmartLibrary.Desktop.Views.Shared
{
    public partial class StreakCelebrationWindow : Window
    {
        public StreakCelebrationWindow(int streak, string message)
        {
            InitializeComponent();
            TxtStreakFire.Text = $"🔥 {streak} Ngày Liên Tiếp 🔥";
            TxtMessage.Text = message;

            // Đóng cửa sổ khi bấm phím Space, Enter hoặc ESC
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Space || e.Key == Key.Enter || e.Key == Key.Escape)
                {
                    DialogResult = true;
                    Close();
                }
            };

            Loaded += (s, e) =>
            {
                System.Media.SystemSounds.Exclamation.Play();
            };
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
