using System;
using Serilog;

namespace QASmartClass.Services
{
    /// <summary>
    /// State Service quản lý đánh giá: survey, poll, assignment.
    /// GV tạo/gửi, HS đọc khi mở/chuyển cửa sổ.
    /// </summary>
    public class AssessmentStateService
    {
        public static AssessmentStateService Instance { get; } = new();
        private readonly object _lock = new();

        // Assignment
        private string _assignmentDescription = string.Empty;
        public string AssignmentDescription
        {
            get { lock (_lock) return _assignmentDescription; }
            set { lock (_lock) _assignmentDescription = value; }
        }
        public DateTime? AssignmentDeadline { get; set; }
        public DateTime AssignmentSentTime { get; set; } = DateTime.MinValue;
        
        private bool _isLocked = false;
        public bool IsLocked
        {
            get { lock (_lock) return _isLocked; }
            set { lock (_lock) _isLocked = value; }
        }

        // Survey
        private string _activeSurveyQuestion = string.Empty;
        public string ActiveSurveyQuestion
        {
            get { lock (_lock) return _activeSurveyQuestion; }
            set { lock (_lock) _activeSurveyQuestion = value; }
        }
        public DateTime ActiveSurveyTime { get; set; } = DateTime.MinValue;
        public volatile bool SurveyAnswered;

        // Poll
        private string _activePollId = string.Empty;
        public string ActivePollId
        {
            get { lock (_lock) return _activePollId; }
            set { lock (_lock) _activePollId = value; }
        }

        private string[] _activePollOptions = Array.Empty<string>();
        public string[] ActivePollOptions
        {
            get { lock (_lock) return _activePollOptions; }
            set { lock (_lock) _activePollOptions = value; }
        }

        public event EventHandler? StateChanged;

        public void Reset()
        {
            AssignmentDescription = string.Empty;
            AssignmentDeadline = null;
            AssignmentSentTime = DateTime.MinValue;
            IsLocked = false;
            ActiveSurveyQuestion = string.Empty;
            ActiveSurveyTime = DateTime.MinValue;
            SurveyAnswered = false;
            ActivePollId = string.Empty;
            ActivePollOptions = Array.Empty<string>();
            Log.Information("[AssessmentState] Reset");
        }

        public void NotifyChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
