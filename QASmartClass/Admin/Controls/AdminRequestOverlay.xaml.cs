using System;
using System.Windows;
using System.Windows.Threading;

namespace QASmartClass.Admin.Controls
{
    public partial class AdminRequestOverlay : Window
    {
        private DispatcherTimer _timer;
        private int _timeLeft = 60;

        public AdminRequestOverlay(string targetMode)
        {
            InitializeComponent();
            txtMessage.Text = $"Quản trị viên (Admin) đang yêu cầu chuyển sang {targetMode}. Bạn có đồng ý không?";
            
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _timeLeft--;
            txtCountdown.Text = $"Tự động hủy sau: {_timeLeft}s";
            
            if (_timeLeft <= 0)
            {
                _timer.Stop();
                this.DialogResult = false; // Timeout means rejected
                this.Close();
            }
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            this.DialogResult = true;
            this.Close();
        }

        private void Reject_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            this.DialogResult = false;
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }
            base.OnClosed(e);
        }
    }
}
