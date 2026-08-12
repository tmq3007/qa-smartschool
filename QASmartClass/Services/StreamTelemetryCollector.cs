using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Serilog;

namespace QASmartClass.Services
{
    public class StreamTelemetryCollector : IDisposable
    {
        private static readonly Lazy<StreamTelemetryCollector> _instance = new(() => new StreamTelemetryCollector());
        public static StreamTelemetryCollector Instance => _instance.Value;

        private readonly ConcurrentQueue<string> _logQueue = new();
        private readonly string _logFilePath;
        private Thread? _writerThread;
        private bool _isLoggingActive = false;

        // Thread-safe metrics lock
        private readonly object _metricsLock = new();

        // Sliding window for FPS (last 1 second)
        private readonly Queue<double> _frameTimes = new();

        // Metrics counters
        private int _frameReceivedCount = 0;
        private int _frameDroppedCount = 0;
        private int _packetReceivedCount = 0;
        private int _packetLostCount = 0;
        private double _lastLatencyMs = 0;
        private double _lastFrameTimestamp = 0;
        private double _jitterSum = 0;
        private int _jitterCount = 0;

        private StreamTelemetryCollector()
        {
            try
            {
                Directory.CreateDirectory(AppPaths.LogsDir);
                _logFilePath = Path.Combine(AppPaths.LogsDir, "stream_telemetry.csv");
                StartLogging();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Telemetry] Failed to initialize");
                _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stream_telemetry.csv");
            }
        }

        public void RecordFrameReceived(int frameId, int totalSlices, int slicesReceived, double latencyMs)
        {
            double nowMs = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;

            lock (_metricsLock)
            {
                _frameReceivedCount++;
                
                // Add timestamp to sliding window for FPS
                _frameTimes.Enqueue(nowMs);
                CleanOldFrameTimes(nowMs);

                // Latency
                _lastLatencyMs = latencyMs;

                // Jitter
                if (_lastFrameTimestamp > 0)
                {
                    double interval = nowMs - _lastFrameTimestamp;
                    double diff = Math.Abs(interval - 33.3); // Diff from target 30 FPS interval
                    _jitterSum += diff;
                    _jitterCount++;
                }
                _lastFrameTimestamp = nowMs;

                // Packets
                _packetReceivedCount += slicesReceived;
                _packetLostCount += (totalSlices - slicesReceived);

                // In log gộp định kỳ sau mỗi 100 frames nhận được (V2.2.3)
                if (_frameReceivedCount % 100 == 0)
                {
                    double fps = GetCurrentFps();
                    double jitter = GetCurrentJitter();
                    double dropRate = GetFrameDropRate();
                    double lossRate = GetPacketLossRate();
                    Log.Information("[UdpBroadcast-Telemetry] Báo cáo gộp sau 100 frames: Frame ID #{FrameId}, FPS={Fps:F1}, Độ trễ={Latency:F1}ms, Jitter={Jitter:F1}ms, Tỉ lệ rớt frame={DropRate:P2}, Tỉ lệ mất gói={LossRate:P2}", 
                        frameId, fps, latencyMs, jitter, dropRate, lossRate);
                }
            }

            LogMetrics(frameId, latencyMs, slicesReceived, totalSlices);
        }

        public void RecordFrameDropped(int frameId, int totalSlices, int slicesReceived)
        {
            lock (_metricsLock)
            {
                _frameDroppedCount++;
                _packetReceivedCount += slicesReceived;
                _packetLostCount += (totalSlices - slicesReceived);

                Log.Warning("[UdpBroadcast-Telemetry] CẢNH BÁO: Rớt Frame #{FrameId}! Số lát cắt nhận được: {Received}/{Total}", 
                    frameId, slicesReceived, totalSlices);
            }
        }

        private void CleanOldFrameTimes(double nowMs)
        {
            while (_frameTimes.Count > 0 && nowMs - _frameTimes.Peek() > 1000.0)
            {
                _frameTimes.Dequeue();
            }
        }

        private void LogMetrics(int frameId, double latencyMs, int slicesReceived, int totalSlices)
        {
            double fps = GetCurrentFps();
            double jitter = GetCurrentJitter();
            double dropRate = GetFrameDropRate();
            double lossRate = GetPacketLossRate();

            string csvLine = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff},{frameId},{latencyMs:F1},{fps:F1},{jitter:F1},{dropRate:F4},{lossRate:F4}";
            _logQueue.Enqueue(csvLine);
        }

        public double GetCurrentFps()
        {
            lock (_metricsLock)
            {
                double nowMs = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
                CleanOldFrameTimes(nowMs);
                return _frameTimes.Count;
            }
        }

        public double GetCurrentJitter()
        {
            lock (_metricsLock)
            {
                if (_jitterCount == 0) return 0;
                double j = _jitterSum / _jitterCount;
                // Reset sum for next window to capture temporal jitter
                _jitterSum = 0;
                _jitterCount = 0;
                return j;
            }
        }

        public double GetFrameDropRate()
        {
            lock (_metricsLock)
            {
                int rec = _frameReceivedCount;
                int drop = _frameDroppedCount;
                if (rec + drop == 0) return 0;
                return (double)drop / (rec + drop);
            }
        }

        public double GetPacketLossRate()
        {
            lock (_metricsLock)
            {
                int rec = _packetReceivedCount;
                int lost = _packetLostCount;
                if (rec + lost == 0) return 0;
                return (double)lost / (rec + lost);
            }
        }

        public double LastLatencyMs
        {
            get
            {
                lock (_metricsLock)
                {
                    return _lastLatencyMs;
                }
            }
        }

        private void StartLogging()
        {
            _isLoggingActive = true;
            _writerThread = new Thread(WriteLoop)
            {
                IsBackground = true,
                Name = "TelemetryLogWriter"
            };
            _writerThread.Start();
        }

        private void WriteLoop()
        {
            try
            {
                while (_isLoggingActive)
                {
                    // Create/Append to CSV
                    if (!_logQueue.IsEmpty)
                    {
                        using (var fs = new FileStream(_logFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                        using (var writer = new StreamWriter(fs, Encoding.UTF8))
                        {
                            if (fs.Length == 0)
                            {
                                writer.WriteLine("Timestamp,FrameId,LatencyMs,FPS,JitterMs,FrameDropRate,PacketLossRate");
                            }
                            fs.Seek(0, SeekOrigin.End);

                            while (_logQueue.TryDequeue(out string? line))
                            {
                                writer.WriteLine(line);
                            }
                        }
                    }
                    Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Telemetry] Log writer thread encountered an error: {Msg}", ex.Message);
            }
        }

        public void Dispose()
        {
            _isLoggingActive = false;
            _writerThread?.Join(1000);
        }
    }
}
