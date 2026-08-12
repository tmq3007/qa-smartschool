using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;
using Serilog;

namespace QASmartClass.Services
{
    public class VncBroadcastService
    {
        public static VncBroadcastService Instance { get; } = new();

        private Process? _vncProcess;
        private DispatcherTimer? _elapsedTimer;
        private DispatcherTimer? _heartbeatTimer;
        private int _elapsedSeconds;
        private bool _isVncBroadcasting;

        public bool IsVncBroadcasting
        {
            get => _isVncBroadcasting;
            private set
            {
                if (_isVncBroadcasting != value)
                {
                    _isVncBroadcasting = value;
                    StatusChanged?.Invoke(this, _isVncBroadcasting ? "STARTED" : "STOPPED");
                }
            }
        }

        public int VncPort { get; private set; } = 5901;
        public string VncSessionCode { get; private set; } = "";
        public string ElapsedTimeDisplay => $"{_elapsedSeconds / 60:D2}:{_elapsedSeconds % 60:D2}";
        public List<string>? TargetStudentCodes { get; private set; }

        public event EventHandler<string>? StatusChanged;
        public event EventHandler<string>? TimerTick;

        private VncBroadcastService()
        {
            _elapsedTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _elapsedTimer.Tick += OnTimerTick;

            _heartbeatTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _heartbeatTimer.Tick += OnHeartbeatTick;
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            _elapsedSeconds++;
            TimerTick?.Invoke(this, ElapsedTimeDisplay);
        }

        private async void OnHeartbeatTick(object? sender, EventArgs e)
        {
            if (!IsVncBroadcasting) return;
            try
            {
                await SendCommandToTargetsAsync("CMD|VNC_HEARTBEAT|1");
            }
            catch (Exception ex)
            {
                Log.Debug("[VncBroadcast] Heartbeat error: {Err}", ex.Message);
            }
        }

        private async Task SendCommandToTargetsAsync(string cmd)
        {
            var app = (QASmartTouch.App)System.Windows.Application.Current;
            if (app?.NetworkService == null) return;

            if (TargetStudentCodes == null || TargetStudentCodes.Count == 0)
            {
                if (app.NetworkService.IsBroadcasting)
                {
                    await app.NetworkService.SendCommandAsync(cmd);
                }
            }
            else
            {
                if (app.NetworkService.IsBroadcasting)
                {
                    await app.NetworkService.SendToStudentsAsync(TargetStudentCodes, cmd);
                }
            }
        }

        public async Task<bool> StartBroadcastAsync(bool forceWatch, List<string>? targetStudentCodes)
        {
            if (IsVncBroadcasting) return true;

            try
            {
                KillExistingVncProcesses();

                VncPort = GetAvailablePort(5901);
                VncSessionCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                TargetStudentCodes = targetStudentCodes;

                WriteServerIni(VncPort, VncSessionCode);

                string localIp = GetLocalIPAddress();
                string vncDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vnc");
                string exePath = Path.Combine(vncDir, "vnctool-server.exe");

                if (!File.Exists(exePath))
                {
                    Log.Error("[VncBroadcast] vnctool-server.exe not found at: {Path}", exePath);
                    return false;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = vncDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                _vncProcess = new Process { StartInfo = startInfo };
                _vncProcess.EnableRaisingEvents = true;
                _vncProcess.Exited += OnVncProcessExited;

                Log.Information("[VncBroadcast] Starting vnctool-server.exe on port {Port}...", VncPort);
                _vncProcess.Start();

                // Setup timer
                _elapsedSeconds = 0;
                _elapsedTimer?.Start();
                _heartbeatTimer?.Start();
                IsVncBroadcasting = true;

                // Send network command to student clients
                string vncCmd = $"CMD|VNC_BROADCAST_START|{localIp}|{VncPort}|{VncSessionCode}|{(forceWatch ? "FORCE_WATCH" : "0")}";
                await SendCommandToTargetsAsync(vncCmd);
                Log.Information("[VncBroadcast] Broadcasted start command: {Cmd}", vncCmd);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error("[VncBroadcast] Failed to start VNC broadcast: {Err}", ex.Message);
                StopBroadcast();
                return false;
            }
        }

        public void StopBroadcast()
        {
            if (!IsVncBroadcasting) return;

            Log.Information("[VncBroadcast] Stopping VNC broadcast...");
            IsVncBroadcasting = false;
            _elapsedTimer?.Stop();
            _heartbeatTimer?.Stop();

            if (_vncProcess != null)
            {
                _vncProcess.Exited -= OnVncProcessExited;
                try
                {
                    if (!_vncProcess.HasExited)
                    {
                        _vncProcess.Kill();
                    }
                }
                catch { }
                _vncProcess.Dispose();
                _vncProcess = null;
            }

            KillExistingVncProcesses();

            // Send stop network command
            try
            {
                string stopCmd = "CMD|VNC_BROADCAST_STOP|0";
                _ = SendCommandToTargetsAsync(stopCmd);
            }
            catch (Exception ex)
            {
                Log.Warning("[VncBroadcast] Error broadcasting stop command: {Err}", ex.Message);
            }
        }

        public void PauseBroadcast()
        {
            if (!IsVncBroadcasting || _vncProcess == null || _vncProcess.HasExited) return;
            try
            {
                _vncProcess.StandardInput.WriteLine("pause");
                _vncProcess.StandardInput.Flush();
                Log.Information("[VncBroadcast] Sent pause command to process stdin.");
            }
            catch (Exception ex)
            {
                Log.Warning("[VncBroadcast] Failed to send pause command: {Err}", ex.Message);
            }
        }

        public void ResumeBroadcast()
        {
            if (!IsVncBroadcasting || _vncProcess == null || _vncProcess.HasExited) return;
            try
            {
                _vncProcess.StandardInput.WriteLine("resume");
                _vncProcess.StandardInput.Flush();
                Log.Information("[VncBroadcast] Sent resume command to process stdin.");
            }
            catch (Exception ex)
            {
                Log.Warning("[VncBroadcast] Failed to send resume command: {Err}", ex.Message);
            }
        }

        private int _currentFps = 30;
        private int _currentZlib = 4;
        private DateTime _lastAdjustmentTime = DateTime.MinValue;
        private bool _isDegraded = false;

        public void AdjustStreamingQuality(double avgLatency, double avgPacketLoss)
        {
            if (!IsVncBroadcasting) return;

            bool shouldDegrade = avgPacketLoss > 5.0 || avgLatency > 150.0;

            if (shouldDegrade && !_isDegraded)
            {
                Log.Warning("[VncBroadcast] Network degradation detected! Latency={Lat}ms, Loss={Loss}%. Adjusting stream: FPS=15, Zlib=7", avgLatency, avgPacketLoss);
                _isDegraded = true;
                _currentFps = 15;
                _currentZlib = 7;
                _lastAdjustmentTime = DateTime.Now;

                ApplyNewStreamParameters();
            }
            else if (!shouldDegrade && _isDegraded)
            {
                if ((DateTime.Now - _lastAdjustmentTime).TotalSeconds >= 30)
                {
                    Log.Information("[VncBroadcast] Network recovered. Restoring default stream quality: FPS=30, Zlib=4");
                    _isDegraded = false;
                    _currentFps = 30;
                    _currentZlib = 4;
                    
                    ApplyNewStreamParameters();
                }
            }
        }

        private void ApplyNewStreamParameters()
        {
            try
            {
                WriteServerIniWithParameters(VncPort, VncSessionCode, _currentFps, 30, _currentZlib);

                if (_vncProcess != null && !_vncProcess.HasExited)
                {
                    _vncProcess.Exited -= OnVncProcessExited;
                    try { _vncProcess.Kill(); } catch { }
                    _vncProcess.Dispose();
                    _vncProcess = null;
                }

                string vncDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vnc");
                string exePath = Path.Combine(vncDir, "vnctool-server.exe");

                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = vncDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                _vncProcess = new Process { StartInfo = startInfo };
                _vncProcess.EnableRaisingEvents = true;
                _vncProcess.Exited += OnVncProcessExited;
                _vncProcess.Start();

                Log.Information("[VncBroadcast] Restarted VNC Server process to apply adaptive quality parameters.");
            }
            catch (Exception ex)
            {
                Log.Error("[VncBroadcast] Error applying new stream parameters: {Err}", ex.Message);
            }
        }

        private void OnVncProcessExited(object? sender, EventArgs e)
        {
            int exitCode = -1;
            try { exitCode = _vncProcess?.ExitCode ?? -1; } catch { }

            Log.Warning("[VncBroadcast] VNC process exited unexpectedly! ExitCode={Code}", exitCode);

            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                if (IsVncBroadcasting)
                {
                    IsVncBroadcasting = false;
                    _elapsedTimer?.Stop();
                    _heartbeatTimer?.Stop();
                    StatusChanged?.Invoke(this, "STOPPED_UNEXPECTED");
                }
            });
        }

        private void WriteServerIni(int port, string sessionCode)
        {
            WriteServerIniWithParameters(port, sessionCode, fps: 30, keyframeInterval: 30, zlibLevel: 4);
        }

        public void WriteServerIniWithParameters(int port, string sessionCode, int fps, int keyframeInterval, int zlibLevel)
        {
            try
            {
                string vncDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vnc");
                if (!Directory.Exists(vncDir))
                {
                    Directory.CreateDirectory(vncDir);
                }

                string iniPath = Path.Combine(vncDir, "server.ini");
                string iniContent = $"""
[server]
port={port}
session_code={sessionCode}
fps={fps}
keyframe_interval={keyframeInterval}
zlib_compression_level={zlibLevel}
""";
                File.WriteAllText(iniPath, iniContent.TrimStart());
                Log.Debug("[VncBroadcast] Wrote server.ini: fps={Fps}, keyframe={KF}, zlib={Z}", fps, keyframeInterval, zlibLevel);
            }
            catch (Exception ex)
            {
                Log.Error("[VncBroadcast] Error writing server.ini: {Err}", ex.Message);
            }
        }

        private void KillExistingVncProcesses()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("vnctool-server"))
                {
                    try
                    {
                        p.Kill();
                        p.WaitForExit(1000);
                        Log.Information("[VncBroadcast] Terminated zombie vnctool-server process.");
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[VncBroadcast] Error cleaning up VNC processes: {Err}", ex.Message);
            }
        }

        private int GetAvailablePort(int startingPort)
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnections = properties.GetActiveTcpConnections();
            var tcpListeners = properties.GetActiveTcpListeners();

            var busyPorts = tcpConnections.Select(c => c.LocalEndPoint.Port)
                .Concat(tcpListeners.Select(l => l.Port))
                .ToHashSet();

            int port = startingPort;
            while (busyPorts.Contains(port))
            {
                port++;
            }
            return port;
        }

        private string GetLocalIPAddress()
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    if (socket.LocalEndPoint is IPEndPoint endPoint)
                    {
                        return endPoint.Address.ToString();
                    }
                }
            }
            catch { }

            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }

            return "127.0.0.1";
        }
    }
}
