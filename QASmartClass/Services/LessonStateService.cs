using System;
using Serilog;

namespace QASmartClass.Services
{
    /// <summary>
    /// State Service quản lý trạng thái tiết học.
    /// GV cập nhật khi bắt đầu/chuyển stage/kết thúc tiết.
    /// HS đọc để đồng bộ ngay khi mở giao diện.
    /// </summary>
    public class LessonStateService
    {
        public static LessonStateService Instance { get; } = new();
        private readonly object _lock = new();

        public int ActiveLessonId { get; set; }
        public int ActiveLessonStage { get; set; }
        public volatile bool IsLessonActive;
        public string? ActiveToolId { get; set; }
        public string? ActiveToolFocusId { get; set; }

        private string _lastTeacherCommand = string.Empty;
        public string LastTeacherCommand
        {
            get { lock (_lock) return _lastTeacherCommand; }
            set { lock (_lock) _lastTeacherCommand = value; }
        }

        public DateTime LastCommandTime { get; set; } = DateTime.MinValue;

        public event EventHandler? StateChanged;

        public void Reset()
        {
            ActiveLessonId = 0;
            ActiveLessonStage = 0;
            IsLessonActive = false;
            ActiveToolId = null;
            ActiveToolFocusId = null;
            LastTeacherCommand = string.Empty;
            LastCommandTime = DateTime.MinValue;
            Log.Information("[LessonState] Reset");
        }

        public void NotifyChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
