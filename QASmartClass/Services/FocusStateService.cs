using System;
using Serilog;

namespace QASmartClass.Services
{
    /// <summary>
    /// State Service quản lý focus state.
    /// GV focus block nào, HS đọc khi reload.
    /// </summary>
    public class FocusStateService
    {
        public static FocusStateService Instance { get; } = new();
        private readonly object _lock = new();

        public volatile int ActiveFocusSort = -1;

        private string _activeFocusType = string.Empty;
        public string ActiveFocusType
        {
            get { lock (_lock) return _activeFocusType; }
            set { lock (_lock) _activeFocusType = value; }
        }

        public event EventHandler? StateChanged;

        public void Reset()
        {
            ActiveFocusSort = -1;
            ActiveFocusType = string.Empty;
            Log.Information("[FocusState] Reset");
        }

        public void NotifyChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
