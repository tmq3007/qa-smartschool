using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using QASmartTouch.Interfaces;
using Serilog;

namespace QASmartTouch.Managers
{
    /// <summary>
    /// [CAI_TIEN_VID_10] Object Pool cho STEM tools (Ruler, Protractor, SetSquare, Compass).
    /// 
    /// Thay vì tạo mới Window mỗi lần mở tool → tái sử dụng Window đã tạo:
    /// - GetOrCreate<T>(): Lấy từ pool hoặc tạo mới nếu chưa có
    /// - Return<T>(): Ẩn Window, gọi OnDeactivated(), giữ trong pool
    /// - Auto-release sau 5 phút idle (phản biện ThS. Ngô Thanh Tùng)
    /// - DisposeAll(): Đóng tất cả khi app shutdown
    /// 
    /// Target: RAM khi bật 4 STEM tools ≤ 150MB, FPS ≥ 55
    /// </summary>
    public class StemToolPool
    {
        #region Singleton

        private static readonly Lazy<StemToolPool> _instance = new(() => new StemToolPool());
        public static StemToolPool Instance => _instance.Value;

        #endregion

        #region Constants

        /// <summary>
        /// Thời gian idle tối đa trước khi auto-release (5 phút).
        /// Theo phản biện ThS. Ngô Thanh Tùng: "Singleton pool giữ 4 Window liên tục có thể lãng phí"
        /// </summary>
        private static readonly TimeSpan IDLE_TIMEOUT = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Chu kỳ kiểm tra idle (mỗi 60 giây)
        /// </summary>
        private static readonly TimeSpan CHECK_INTERVAL = TimeSpan.FromSeconds(60);

        #endregion

        #region Fields

        private readonly Dictionary<Type, Window> _pool = new();
        private readonly Dictionary<Type, DateTime> _lastReturnedTime = new();
        private readonly DispatcherTimer _idleTimer;
        private bool _isDisposed;

        #endregion

        #region Constructor

        private StemToolPool()
        {
            _idleTimer = new DispatcherTimer
            {
                Interval = CHECK_INTERVAL
            };
            _idleTimer.Tick += IdleTimer_Tick;
            _idleTimer.Start();

            Log.Information("[CAI_TIEN_VID_10] StemToolPool initialized (idle timeout: {Timeout}min)",
                IDLE_TIMEOUT.TotalMinutes);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Lấy STEM tool từ pool, hoặc tạo mới nếu chưa có.
        /// Gọi OnActivated() nếu tool implement IStemTool.
        /// </summary>
        /// <typeparam name="T">Loại Window (Form2_15_RulerTool, Form2_17_SetSquareTool, ...)</typeparam>
        /// <param name="factory">Factory function để tạo tool mới (vì constructor có tham số)</param>
        /// <returns>Instance của tool</returns>
        public T GetOrCreate<T>(Func<T> factory) where T : Window
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(StemToolPool));

            if (_pool.TryGetValue(typeof(T), out var existing))
            {
                existing.Visibility = Visibility.Visible;

                // Gọi OnActivated nếu implement IStemTool
                if (existing is IStemTool stemTool)
                {
                    stemTool.OnActivated();
                    Log.Debug("[CAI_TIEN_VID_10] Reused {Tool} from pool (memory: {Mem:F1}MB)",
                        stemTool.ToolDisplayName, stemTool.GetMemoryUsageMB());
                }

                _lastReturnedTime.Remove(typeof(T)); // Đang active → xóa idle timestamp
                return (T)existing;
            }

            // Tạo mới
            var tool = factory();
            _pool[typeof(T)] = tool;

            if (tool is IStemTool newStemTool)
            {
                newStemTool.OnActivated();
                Log.Information("[CAI_TIEN_VID_10] Created new {Tool} in pool (memory: {Mem:F1}MB)",
                    newStemTool.ToolDisplayName, newStemTool.GetMemoryUsageMB());
            }

            return tool;
        }

        /// <summary>
        /// Trả tool về pool (ẩn, không đóng).
        /// Gọi OnDeactivated() để giải phóng resources tạm.
        /// </summary>
        public void Return<T>(T tool) where T : Window
        {
            if (_isDisposed || tool == null) return;

            tool.Visibility = Visibility.Hidden;

            if (tool is IStemTool stemTool)
            {
                stemTool.OnDeactivated();
                Log.Debug("[CAI_TIEN_VID_10] Returned {Tool} to pool", stemTool.ToolDisplayName);
            }

            _lastReturnedTime[typeof(T)] = DateTime.UtcNow;
        }

        /// <summary>
        /// Kiểm tra xem tool có đang ở trong pool (đã tạo) hay không.
        /// </summary>
        public bool HasTool<T>() where T : Window
        {
            return _pool.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Lấy tổng bộ nhớ đang sử dụng bởi tất cả tools trong pool (MB).
        /// </summary>
        public double GetTotalMemoryUsageMB()
        {
            double total = 0;
            foreach (var tool in _pool.Values)
            {
                if (tool is IStemTool stemTool)
                {
                    total += stemTool.GetMemoryUsageMB();
                }
            }
            return total;
        }

        /// <summary>
        /// Đóng và giải phóng tất cả tools trong pool.
        /// Gọi khi app shutdown.
        /// </summary>
        public void DisposeAll()
        {
            _isDisposed = true;
            _idleTimer.Stop();

            foreach (var kvp in _pool)
            {
                try
                {
                    if (kvp.Value is IStemTool stemTool)
                    {
                        stemTool.OnDeactivated();
                    }
                    kvp.Value.Close();
                }
                catch
                {
                    // Ignore errors during shutdown
                }
            }

            var count = _pool.Count;
            _pool.Clear();
            _lastReturnedTime.Clear();

            Log.Information("[CAI_TIEN_VID_10] StemToolPool disposed: {Count} tools released", count);
        }

        /// <summary>
        /// Lấy số lượng tools trong pool (để monitoring).
        /// </summary>
        public int PoolSize => _pool.Count;

        #endregion

        #region Private Methods

        /// <summary>
        /// [CAI_TIEN_VID_10] Timer kiểm tra idle tools.
        /// Auto-release Window sau 5 phút không dùng.
        /// </summary>
        private void IdleTimer_Tick(object? sender, EventArgs e)
        {
            if (_isDisposed) return;

            var now = DateTime.UtcNow;
            var toRelease = new List<Type>();

            foreach (var kvp in _lastReturnedTime)
            {
                if (now - kvp.Value > IDLE_TIMEOUT)
                {
                    toRelease.Add(kvp.Key);
                }
            }

            foreach (var type in toRelease)
            {
                if (_pool.TryGetValue(type, out var window))
                {
                    try
                    {
                        if (window is IStemTool stemTool)
                        {
                            Log.Information(
                                "[CAI_TIEN_VID_10] Auto-releasing idle tool: {Tool} (idle {Idle:F0}s, memory: {Mem:F1}MB)",
                                stemTool.ToolDisplayName,
                                (now - _lastReturnedTime[type]).TotalSeconds,
                                stemTool.GetMemoryUsageMB());
                        }

                        window.Close();
                    }
                    catch { /* Ignore */ }

                    _pool.Remove(type);
                    _lastReturnedTime.Remove(type);
                }
            }
        }

        #endregion
    }
}
