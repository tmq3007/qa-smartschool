using System;
using QASmartClass.Services;
using Serilog;

namespace QASmartClass.Classroom.Services
{
    /// <summary>Nguồn đang chiếu màn hình.</summary>
    public enum ScreenShareSource
    {
        None,
        /// <summary>Chiếu thường / siêu tốc / chuyên chữ / đa thiết bị / bảng SMART TOUCH (cờ BroadcastStateService).</summary>
        Desktop,
        /// <summary>Chiếu qua VNC (VncBroadcastService).</summary>
        Vnc
    }

    /// <summary>
    /// Điểm duy nhất cho biết "lớp có đang được chiếu màn hình không" (Giai đoạn 1).
    /// Gom trạng thái từ các đường chiếu khác nhau (BroadcastStateService và VncBroadcastService)
    /// để các màn hình (Giám sát, Điều khiển lớp, thanh nổi...) không phải tự đoán.
    /// Việc bắt đầu/dừng chiếu vẫn do BroadcastPage thực hiện (chưa đổi hành vi);
    /// các giai đoạn sau sẽ chuyển logic bắt đầu/dừng vào đây.
    /// Lưu ý: <see cref="LiveChanged"/> có thể phát từ luồng nền — nơi nhận phải tự Dispatcher.Invoke.
    /// Chỉ truy cập <see cref="Instance"/> lần đầu từ luồng UI (VncBroadcastService dùng DispatcherTimer).
    /// </summary>
    public sealed class ScreenShareController
    {
        public static ScreenShareController Instance { get; } = new();

        private bool _lastIsLive;

        public event EventHandler? LiveChanged;

        /// <summary>Yêu cầu dừng mọi hoạt động chiếu màn hình (vd: khi kết thúc lớp). BroadcastPage lắng nghe và dừng đúng quy trình.</summary>
        public event EventHandler? StopRequested;

        public void RequestStop() => StopRequested?.Invoke(this, EventArgs.Empty);

        private ScreenShareController()
        {
            try
            {
                BroadcastStateService.Instance.StateChanged += (_, _) => RaiseIfChanged();
                VncBroadcastService.Instance.StatusChanged += (_, _) => RaiseIfChanged();
                _lastIsLive = IsLive;
            }
            catch (Exception ex)
            {
                Log.Warning("[ScreenShare] Controller init error: {Err}", ex.Message);
            }
        }

        public bool IsLive => Source != ScreenShareSource.None;

        public ScreenShareSource Source
        {
            get
            {
                try
                {
                    if (VncBroadcastService.Instance.IsVncBroadcasting) return ScreenShareSource.Vnc;
                    if (BroadcastStateService.Instance.IsScreenBroadcastActive) return ScreenShareSource.Desktop;
                }
                catch { }
                return ScreenShareSource.None;
            }
        }

        private void RaiseIfChanged()
        {
            bool now = IsLive;
            if (now == _lastIsLive) return;
            _lastIsLive = now;
            Log.Information("[ScreenShare] Live={Live} Source={Source}", now, Source);
            LiveChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
