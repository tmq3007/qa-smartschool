using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using QASmartClass.Models;

namespace QASmartClass.Services
{
    public class StatusReportService
    {
        private static StatusReportService? _instance;
        private static readonly object _lock = new object();
        private Timer? _timer;
        private readonly HttpClient _httpClient;
        private readonly string _machineId;
        private DateTime _startTime;

        private StatusReportService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _machineId = Environment.MachineName;
            _startTime = DateTime.Now;
        }

        public static StatusReportService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new StatusReportService();
                        }
                    }
                }
                return _instance;
            }
        }

        public void Start()
        {
            // Send heartbeat every 5 minutes (300,000 ms)
            _timer = new Timer(async _ => await SendHeartbeatAsync(), null, 0, 300000);
        }

        public void Stop()
        {
            _timer?.Change(Timeout.Infinite, 0);
        }

        private async Task SendHeartbeatAsync()
        {
            try
            {
                var app = System.Windows.Application.Current as QASmartTouch.App;
                string currentMode = app?.ModeService?.CurrentMode.ToString() ?? "Unknown";

                var payload = new
                {
                    machineId = _machineId,
                    status = "Online",
                    currentMode = currentMode,
                    uptimeSeconds = (DateTime.Now - _startTime).TotalSeconds,
                    timestamp = DateTime.UtcNow
                };

                string json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Mock endpoint. In production: read from SettingsManager.Instance.VendorApiUrl
                // For now, write to a local JSON file to simulate WebAdmin DB
                SaveLocalMock(json);

                // string apiUrl = "http://localhost:5000/api/heartbeat";
                // await _httpClient.PostAsync(apiUrl, content);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Failed to send heartbeat: {Error}", ex.Message);
            }
        }

        private void SaveLocalMock(string json)
        {
            try
            {
                // Save to local AppData for the mock Web Dashboard to read
                string dir = AppPaths.SharedFilesDir;
                string filePath = System.IO.Path.Combine(dir, $"heartbeat_{_machineId}.json");
                System.IO.File.WriteAllText(filePath, json);
            }
            catch { }
        }
    }
}
