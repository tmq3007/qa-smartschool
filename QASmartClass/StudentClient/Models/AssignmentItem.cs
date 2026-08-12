using System;
using System.ComponentModel;
using System.Windows.Media;

namespace QASmartClass.StudentClient.Views
{
    /// <summary>
    /// Model hiển thị bài tập trong danh sách.
    /// Hỗ trợ INotifyPropertyChanged cho countdown timer real-time.
    /// </summary>
    public class AssignmentItem : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
        public string DeadlineStr => $"⏳ Hạn: {Deadline:HH:mm dd/MM}";
        public bool IsSubmitted { get; set; }
        /// <summary>GV cho phép nộp lại bài</summary>
        public bool AllowResubmit { get; set; }

        private string _countdownStr = string.Empty;
        public string CountdownStr { get => _countdownStr; set { _countdownStr = value; OnPropertyChanged(nameof(CountdownStr)); } }

        private Brush _deadlineColor = Brushes.Black;
        public Brush DeadlineColor { get => _deadlineColor; set { _deadlineColor = value; OnPropertyChanged(nameof(DeadlineColor)); } }

        public string ButtonText => IsSubmitted
            ? (AllowResubmit ? "📝 Nộp lại" : "✅ Đã nộp")
            : "📤 Nộp bài";
        public Brush ButtonBg => IsSubmitted
            ? (AllowResubmit
                ? new SolidColorBrush(Color.FromRgb(245, 158, 11)) // Amber for resubmit
                : new SolidColorBrush(Color.FromRgb(76, 175, 80)))  // Green for done
            : new SolidColorBrush(Color.FromRgb(25, 118, 210));     // Blue for submit
        public bool IsEnabled => !IsSubmitted || AllowResubmit;

        public void UpdateCountdown()
        {
            var remaining = Deadline - DateTime.Now;
            if (IsSubmitted)
            {
                CountdownStr = "🎉 Đã hoàn thành";
                DeadlineColor = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            }
            else if (remaining.TotalSeconds <= 0)
            {
                CountdownStr = "⚠ QUÁ HẠN NỘP BÀI!";
                DeadlineColor = new SolidColorBrush(Color.FromRgb(198, 40, 40));
            }
            else if (remaining.TotalHours < 1)
            {
                CountdownStr = $"⏳ Còn {remaining.Minutes} phút";
                DeadlineColor = new SolidColorBrush(Color.FromRgb(198, 40, 40));
            }
            else if (remaining.TotalHours < 24)
            {
                CountdownStr = $"⏳ Còn {(int)remaining.TotalHours} giờ {remaining.Minutes} phút";
                DeadlineColor = new SolidColorBrush(Color.FromRgb(230, 81, 0));
            }
            else
            {
                CountdownStr = $"⏳ Còn {(int)remaining.TotalDays} ngày {remaining.Hours} giờ";
                DeadlineColor = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

