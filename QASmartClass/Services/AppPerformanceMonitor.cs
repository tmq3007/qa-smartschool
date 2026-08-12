using System;
using System.Diagnostics;
using System.Threading;
using Serilog;

namespace QASmartClass.Services
{
    public class AppPerformanceMonitor : IDisposable
    {
        private static readonly Lazy<AppPerformanceMonitor> _instance = new(() => new AppPerformanceMonitor());
        public static AppPerformanceMonitor Instance => _instance.Value;

        private Timer? _monitorTimer;
        private long _bytesTransmitted = 0;
        private TimeSpan _lastCpuTime = TimeSpan.Zero;
        private DateTime _lastSnapshotTime = DateTime.MinValue;
        private readonly object _lock = new();
        private bool _isStarted = false;

        private AppPerformanceMonitor()
        {
        }

        public void Start()
        {
            lock (_lock)
            {
                if (_isStarted) return;
                _isStarted = true;

                _bytesTransmitted = 0;
                try
                {
                    var process = Process.GetCurrentProcess();
                    _lastCpuTime = process.TotalProcessorTime;
                }
                catch (Exception ex)
                {
                    _lastCpuTime = TimeSpan.Zero;
                    Log.Warning("[PerfMonitor] Không thể lấy TotalProcessorTime lúc khởi động: {Msg}", ex.Message);
                }
                _lastSnapshotTime = DateTime.UtcNow;

                _monitorTimer = new Timer(MonitorTick, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
                Log.Information("[PerfMonitor] Bộ giám sát tài nguyên ứng dụng đã khởi động (chu kỳ 5 giây).");
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!_isStarted) return;
                _isStarted = false;

                _monitorTimer?.Dispose();
                _monitorTimer = null;
                Log.Information("[PerfMonitor] Bộ giám sát tài nguyên ứng dụng đã dừng.");
            }
        }

        public void RecordBytesTransmitted(long bytes)
        {
            Interlocked.Add(ref _bytesTransmitted, bytes);
        }

        private void MonitorTick(object? state)
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var now = DateTime.UtcNow;
                TimeSpan cpuTime = TimeSpan.Zero;
                try
                {
                    cpuTime = process.TotalProcessorTime;
                }
                catch (Exception exCpu)
                {
                    Log.Warning("[PerfMonitor] Lỗi đọc TotalProcessorTime: {Msg}", exCpu.Message);
                    return;
                }

                double elapsedSeconds = (now - _lastSnapshotTime).TotalSeconds;
                if (elapsedSeconds <= 0) elapsedSeconds = 5.0;

                // CPU percentage for current process
                double cpuUsedMs = (cpuTime - _lastCpuTime).TotalMilliseconds;
                double systemMs = elapsedSeconds * 1000.0 * Environment.ProcessorCount;
                double cpuUsage = (cpuUsedMs / systemMs) * 100.0;
                if (cpuUsage < 0) cpuUsage = 0;
                if (cpuUsage > 100.0) cpuUsage = 100.0;

                // RAM (Working Set) in MB
                double workingSetMb = process.WorkingSet64 / (1024.0 * 1024.0);

                // Network throughput in Kbps
                long transmittedBytes = Interlocked.Exchange(ref _bytesTransmitted, 0);
                double throughputKbps = (transmittedBytes * 8.0) / (1024.0 * elapsedSeconds);

                _lastCpuTime = cpuTime;
                _lastSnapshotTime = now;

                Log.Information("[PerfMonitor] Hiệu năng: CPU Tiến trình={CpuUsage:F1}%, RAM chiếm dụng={RamMB:F1}MB, Băng thông mạng={NetKbps:F1}Kbps (Tổng truyền tải={Bytes} bytes trong {Elapsed:F1}s)", 
                    cpuUsage, workingSetMb, throughputKbps, transmittedBytes, elapsedSeconds);

                // Warning alerts
                if (cpuUsage > 85.0)
                {
                    Log.Warning("[PerfMonitor] ⚠️ CẢNH BÁO: Tải CPU của ứng dụng vượt quá giới hạn an toàn (>85%): {CpuUsage:F1}%", cpuUsage);
                }
                if (workingSetMb > 800.0)
                {
                    Log.Warning("[PerfMonitor] ⚠️ CẢNH BÁO: Bộ nhớ RAM ứng dụng vượt quá giới hạn an toàn (>800MB): {RamMB:F1}MB", workingSetMb);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[PerfMonitor] Lỗi khi đo tài nguyên hiệu năng: {Msg}", ex.Message);
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
