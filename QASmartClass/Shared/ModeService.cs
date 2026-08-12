using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QASmartClass.Shared
{
    /// <summary>
    /// Enum định nghĩa 3 chế độ hoạt động của ứng dụng
    /// </summary>
    public enum AppMode
    {
        /// <summary>📚 Quản lý lớp học (F1-F19)</summary>
        SmartClass,
        /// <summary>🖊️ Bảng trắng tương tác (F20-F24, code từ Smart Touch)</summary>
        SmartScreen,
        /// <summary>🔲 Ẩn app, hiện Windows Desktop</summary>
        Desktop
    }

    /// <summary>
    /// Service quản lý chuyển đổi giữa 3 chế độ: Smart Class ↔ Smart Touch ↔ Desktop.
    /// Singleton — được inject qua DI container.
    /// 
    /// [LOI_VID_49] Bổ sung:
    /// - IsTransitioning: Cờ khóa trạng thái chống bấm đúp gây crash
    /// - SwitchToAsync: Chuyển mode bất đồng bộ với Stopwatch benchmark
    /// - Transition timeout safety: Tự reset sau 15s nếu bị kẹt
    /// </summary>
    public partial class ModeService : ObservableObject
    {
        [ObservableProperty]
        private AppMode _currentMode = AppMode.SmartScreen; // Default = bảng trắng (giữ tương thích Smart Touch)

        [ObservableProperty]
        private bool _isClassroomSessionActive = false;

        [ObservableProperty]
        private int _onlineStudentCount = 0;

        [ObservableProperty]
        private string _currentClassName = string.Empty;

        /// <summary>
        /// [LOI_VID_49] Cờ khóa trạng thái: true khi đang trong quá trình chuyển mode.
        /// Tất cả nút chuyển mode PHẢI kiểm tra cờ này trước khi xử lý.
        /// </summary>
        [ObservableProperty]
        private bool _isTransitioning = false;

        /// <summary>
        /// [LOI_VID_49] Thời gian chuyển mode lần gần nhất (ms) — để benchmark
        /// </summary>
        public long LastTransitionDurationMs { get; private set; }

        /// <summary>
        /// [LOI_VID_49] Timeout an toàn cho chuyển mode (ms).
        /// Nếu chuyển mode vượt quá thời gian này, IsTransitioning sẽ tự reset.
        /// </summary>
        private const int TRANSITION_TIMEOUT_MS = 15000;

        /// <summary>
        /// Event phát ra khi chế độ thay đổi
        /// </summary>
        public event EventHandler<ModeChangedEventArgs>? ModeChanged;

        /// <summary>
        /// [LOI_VID_49] Event phát ra khi bắt đầu chuyển mode (để hiển thị Transition Overlay)
        /// </summary>
        public event EventHandler<ModeChangedEventArgs>? ModeTransitionStarted;

        /// <summary>
        /// [LOI_VID_49] Event phát ra khi hoàn tất chuyển mode (để ẩn Transition Overlay)
        /// </summary>
        public event EventHandler<ModeChangedEventArgs>? ModeTransitionCompleted;

        /// <summary>
        /// Chuyển sang chế độ mới (đồng bộ — giữ tương thích ngược).
        /// Lưu ý: Method này không có Loading Overlay. Ưu tiên dùng SwitchToAsync().
        /// </summary>
        public void SwitchTo(AppMode newMode, bool force = false)
        {
            if (IsTransitioning)
            {
                Serilog.Log.Warning("SwitchTo BLOCKED: IsTransitioning=true (target={Mode})", newMode);
                return;
            }
            if (CurrentMode == newMode && !force) return;

            var oldMode = CurrentMode;
            CurrentMode = newMode;

            ModeChanged?.Invoke(this, new ModeChangedEventArgs(oldMode, newMode));

            Serilog.Log.Information("Mode switched: {OldMode} → {NewMode} (force: {Force})", oldMode, newMode, force);
        }

        /// <summary>
        /// [LOI_VID_49] Chuyển sang chế độ mới — phiên bản bất đồng bộ.
        /// - Đặt IsTransitioning = true ngay lập tức → chặn bấm đúp
        /// - Phát ModeTransitionStarted → App hiển thị Loading Overlay
        /// - Phát ModeChanged → App thực hiện chuyển Window
        /// - Phát ModeTransitionCompleted → App ẩn Loading Overlay
        /// - Có timeout safety 15s để tự reset nếu bị kẹt
        /// </summary>
        public async Task SwitchToAsync(AppMode newMode, bool force = false)
        {
            // Chặn bấm đúp
            if (IsTransitioning)
            {
                Serilog.Log.Warning("SwitchToAsync BLOCKED: IsTransitioning=true (target={Mode})", newMode);
                return;
            }
            if (CurrentMode == newMode && !force) return;

            var sw = Stopwatch.StartNew();
            var oldMode = CurrentMode;
            var args = new ModeChangedEventArgs(oldMode, newMode);

            try
            {
                IsTransitioning = true;

                // Phát event bắt đầu → App hiển thị Transition Overlay
                ModeTransitionStarted?.Invoke(this, args);
                Serilog.Log.Information("Mode transition STARTED: {Old} → {New}", oldMode, newMode);

                // Cập nhật mode
                CurrentMode = newMode;

                // Phát event chuyển mode → App xử lý Show/Hide Window
                ModeChanged?.Invoke(this, args);

                // Đợi UI ổn định (cho phép WPF render frame đầu tiên)
                await Task.Delay(50);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "SwitchToAsync ERROR: {Old} → {New}", oldMode, newMode);
            }
            finally
            {
                sw.Stop();
                LastTransitionDurationMs = sw.ElapsedMilliseconds;
                IsTransitioning = false;

                // Phát event hoàn tất → App ẩn Transition Overlay
                ModeTransitionCompleted?.Invoke(this, args);
                Serilog.Log.Information(
                    "Mode transition COMPLETED: {Old} → {New} in {Ms}ms",
                    oldMode, newMode, LastTransitionDurationMs);
            }
        }

        /// <summary>
        /// [LOI_VID_49] Reset cưỡng bức cờ IsTransitioning (dùng cho timeout safety)
        /// </summary>
        public void ForceResetTransition()
        {
            if (IsTransitioning)
            {
                IsTransitioning = false;
                Serilog.Log.Warning("ForceResetTransition: IsTransitioning forcibly reset to false");
            }
        }

        /// <summary>
        /// Chuyển nhanh sang Smart Class
        /// </summary>
        public void GoToClass() => SwitchTo(AppMode.SmartClass);

        /// <summary>
        /// [LOI_VID_49] Chuyển nhanh sang Smart Class (async)
        /// </summary>
        public Task GoToClassAsync() => SwitchToAsync(AppMode.SmartClass);

        /// <summary>
        /// Chuyển nhanh sang Smart Touch (bảng trắng)
        /// </summary>
        public void GoToScreen() => SwitchTo(AppMode.SmartScreen);

        /// <summary>
        /// [LOI_VID_49] Chuyển nhanh sang Smart Touch (async)
        /// </summary>
        public Task GoToScreenAsync() => SwitchToAsync(AppMode.SmartScreen);

        /// <summary>
        /// Chuyển sang Desktop (ẩn app)
        /// </summary>
        public void GoToDesktop() => SwitchTo(AppMode.Desktop);

        /// <summary>
        /// [LOI_VID_49] Chuyển sang Desktop (async)
        /// </summary>
        public Task GoToDesktopAsync() => SwitchToAsync(AppMode.Desktop);

        /// <summary>
        /// Toggle giữa Class và Screen (dùng cho phím tắt)
        /// </summary>
        public void ToggleMode()
        {
            if (CurrentMode == AppMode.SmartClass)
                SwitchTo(AppMode.SmartScreen);
            else
                SwitchTo(AppMode.SmartClass);
        }

        /// <summary>
        /// [LOI_VID_49] Toggle giữa Class và Screen (async)
        /// </summary>
        public async Task ToggleModeAsync()
        {
            if (CurrentMode == AppMode.SmartClass)
                await SwitchToAsync(AppMode.SmartScreen);
            else
                await SwitchToAsync(AppMode.SmartClass);
        }

        /// <summary>
        /// [LOI_VID_49] Lấy tên hiển thị cho mode (giữ nguyên tiếng Anh theo QC_4.2_LANGUAGE_BRANDING)
        /// </summary>
        public static string GetModeDisplayName(AppMode mode) => mode switch
        {
            AppMode.SmartClass => "SMART CLASS",
            AppMode.SmartScreen => "SMART TOUCH",
            AppMode.Desktop => "DESKTOP",
            _ => mode.ToString()
        };
    }

    /// <summary>
    /// Event args khi mode thay đổi
    /// </summary>
    public class ModeChangedEventArgs : EventArgs
    {
        public AppMode OldMode { get; }
        public AppMode NewMode { get; }

        public ModeChangedEventArgs(AppMode oldMode, AppMode newMode)
        {
            OldMode = oldMode;
            NewMode = newMode;
        }
    }
}
