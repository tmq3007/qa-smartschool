using System;
using QASmartClass.Classroom.Helpers;
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
    /// <summary>Các tác vụ Quảng bá / Chia sẻ màn hình nhanh có thể kích hoạt xuyên suốt từ FloatingModeBar ở mọi chế độ.</summary>
    public enum QuickBroadcastAction
    {
        ToggleQuickShare,
        PickScreenOrWindow,
        PickRegion,
        PickQaPage,
        ShareSmartTouchNow,
        TogglePause,
        ShowFloatingPip,
        OpenBroadcastHub,
        OpenStudentDemo,
        OpenNoticeTab,
        OpenWebsiteTab
    }

    public sealed class ScreenShareController
    {
        public static ScreenShareController Instance { get; } = new();

        private bool _lastIsLive;
        private int _isStoppingFlag;

        public event EventHandler? LiveChanged;

        /// <summary>Yêu cầu dừng mọi hoạt động chiếu màn hình (chiếu bảng, chiếu màn hình, HS trình bày, VNC).</summary>
        public event EventHandler? StopRequested;

        /// <summary>Yêu cầu thực thi tác vụ Quảng bá / Chia sẻ màn hình từ thanh nổi FloatingModeBar.</summary>
        public event EventHandler<QuickBroadcastAction>? ActionRequested;

        public void RequestStop() => StopAllActiveBroadcasts();

        /// <summary>
        /// Chốt chặn dừng trung tâm duy nhất: dừng sạch mọi bộ phát đang hoạt động (BroadcastPage, Student Demo, LessonRunner, VNC)
        /// và đảm bảo gửi lệnh STOP kèm Token phiên đã đóng tới toàn bộ máy Học sinh, kể cả khi trang phát đang Unloaded.
        /// </summary>
        public void StopAllActiveBroadcasts()
        {
            if (System.Threading.Interlocked.Exchange(ref _isStoppingFlag, 1) == 1)
            {
                return;
            }

            try
            {
                try
                {
                    StopRequested?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Log.Warning("[ScreenShare] StopRequested handler error: {Err}", ex.Message);
                }

                try
                {
                    QASmartClass.Classroom.Views.BroadcastPage.StopAllLiveInstances();
                }
                catch (Exception ex)
                {
                    Log.Warning("[ScreenShare] StopAllLiveInstances error: {Err}", ex.Message);
                }

                try
                {
                    if (VncBroadcastService.Instance.IsVncBroadcasting)
                    {
                        VncBroadcastService.Instance.StopBroadcast();
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("[ScreenShare] VNC stop error: {Err}", ex.Message);
                }

                // Fail-Safe Sweep: Nếu có bất kỳ luồng mồ côi nào vẫn giữ cờ IsScreenBroadcastActive = true,
                // cưỡng bức đóng phiên ngay lập tức và phát lệnh STOP tới toàn bộ Học sinh.
                if (BroadcastStateService.Instance.IsScreenBroadcastActive
                    || ClassroomAppContext.BroadcastState.IsScreenBroadcastActive)
                {
                    string stoppedToken = BroadcastStateService.Instance.StopScreenBroadcastSession();
                    string tokenSuffix = string.IsNullOrEmpty(stoppedToken) ? "0" : stoppedToken;
                    ClassroomAppContext.BroadcastState.IsScreenBroadcastActive = false;
                    ClassroomAppContext.BroadcastState.ScreenCapturePath = string.Empty;

                    string stopCmd1 = $"CMD|SCREEN_BROADCAST_STOP|{tokenSuffix}";
                    string stopCmd2 = "CMD|BROADCAST_STOP|0";
                    ClassroomAppContext.LessonState.LastTeacherCommand = stopCmd1;
                    ClassroomAppContext.LessonState.LastCommandTime = DateTime.Now;

                    ClassroomAppContext.DispatchCommand(stopCmd1);
                    ClassroomAppContext.DispatchCommand(stopCmd2);

                    var net = ClassroomAppContext.Network;
                    if (net?.IsBroadcasting == true)
                    {
                        _ = net.SendCommandAsync("CMD|STUDENT_DEMO_STOP");
                        _ = net.SendCommandAsync(stopCmd1);
                        _ = net.SendCommandAsync(stopCmd2);
                    }
                    Log.Information("[ScreenShare] Fail-safe broadcast stop executed (Token={Token})", tokenSuffix);
                }
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref _isStoppingFlag, 0);
                RaiseIfChanged();
            }
        }

        public void RequestAction(QuickBroadcastAction action) => ActionRequested?.Invoke(this, action);

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
