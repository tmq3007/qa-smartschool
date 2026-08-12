using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartLibrary.Desktop.Services
{
    public class RfidReaderService : IDisposable
    {
        public static bool HasHardwareError { get; set; } = false;

        // Event phát ra khi phát hiện các thẻ RFID
        public event EventHandler<List<string>>? TagsDetected;

        private bool _isScanning = false;
        private System.IO.Ports.SerialPort? _serialPort; // Kết nối mô phỏng phần cứng

        private string _comPort = "COM3";
        private int _baudRate = 9600;
        private bool _isSimulationMode = true;

        public RfidReaderService()
        {
            try
            {
                string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (System.IO.File.Exists(configPath))
                {
                    string json = System.IO.File.ReadAllText(configPath);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("RfidSettings", out var rfidSettings))
                    {
                        if (rfidSettings.TryGetProperty("ComPort", out var portVal)) _comPort = portVal.GetString() ?? "COM3";
                        if (rfidSettings.TryGetProperty("BaudRate", out var baudVal)) _baudRate = baudVal.GetInt32();
                        if (rfidSettings.TryGetProperty("SimulationMode", out var simVal)) _isSimulationMode = simVal.GetBoolean();
                    }
                }
            }
            catch { }
        }

        public void Dispose()
        {
            try
            {
                _isScanning = false;
                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();
                    _serialPort.Dispose();
                    _serialPort = null;
                }
                System.Diagnostics.Debug.WriteLine("RFID Serial COM Port has been safely disposed.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing RFID COM resources: {ex.Message}");
            }
        }

        // Bắt đầu quét liên tục (dùng cho cửa an ninh hoặc kiểm kho)
        public async Task StartScanningAsync()
        {
            _isScanning = true;

            if (!_isSimulationMode)
            {
                try
                {
                    if (_serialPort == null)
                    {
                        _serialPort = new System.IO.Ports.SerialPort(_comPort, _baudRate)
                        {
                            ReadTimeout = 500,
                            WriteTimeout = 500
                        };
                        _serialPort.DataReceived += SerialPort_DataReceived;
                    }

                    if (!_serialPort.IsOpen)
                    {
                        _serialPort.Open();
                    }
                    HasHardwareError = false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to open physical COM Port {_comPort}: {ex.Message}. Falling back to simulation mode.");
                    _ = AuditLogService.WriteLogAsync("Kết nối phần cứng RFID", $"Lỗi kết nối cổng {_comPort}: {ex.Message}. Hệ thống tự động chuyển sang chế độ giả lập.", false);
                    _isSimulationMode = true; // Tự động fallback sang chế độ giả lập nếu lỗi mở cổng
                    HasHardwareError = true;
                }
            }
            
            if (_isSimulationMode)
            {
                // Giả lập việc quét RFID liên tục
                while (_isScanning)
                {
                    await Task.Delay(2000); // Mỗi 2 giây quét 1 lần
                    
                    if (!_isScanning) break;
                    
                    // Giả lập ngẫu nhiên nhặt được 1-3 thẻ
                    var random = new Random();
                    if (random.Next(10) > 6) // 30% cơ hội phát hiện thẻ lướt qua
                    {
                        var tags = new List<string>();
                        int count = random.Next(1, 4);
                        for (int i = 0; i < count; i++)
                        {
                            tags.Add($"RFID-AUTO-{random.Next(1000, 9999)}");
                        }
                        
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            TagsDetected?.Invoke(this, tags);
                        });
                    }
                }
            }
        }

        private void SerialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;
            try
            {
                // Đọc chuỗi trọn vẹn theo dòng từ đầu đọc RFID
                string line = _serialPort.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    var rawTags = line.Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var tags = new List<string>();
                    foreach (var rawTag in rawTags)
                    {
                        var trimmed = rawTag.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && trimmed.Length >= 4 && trimmed.Length <= 50)
                        {
                            tags.Add(trimmed);
                        }
                    }

                    if (tags.Count > 0)
                    {
                        HasHardwareError = false;
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            TagsDetected?.Invoke(this, tags);
                        });
                    }
                }
            }
            catch (TimeoutException)
            {
                // Bỏ qua ngoại lệ Timeout thông thường do không có thẻ đi qua đầu đọc trong 500ms
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading serial data: {ex.Message}");
                HasHardwareError = true;
            }
        }

        public void StopScanning()
        {
            _isScanning = false;
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing serial port: {ex.Message}");
            }
        }

        // Mô phỏng việc đặt 1 xấp sách lên bàn đọc RFID (nhập kho)
        public void SimulateBatchRead(int numberOfTags)
        {
            var tags = new List<string>();
            var random = new Random();
            for (int i = 0; i < numberOfTags; i++)
            {
                tags.Add($"B{random.Next(10000, 99999)}"); // Sinh mã Barcode/RFID giả lập
            }
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                TagsDetected?.Invoke(this, tags);
            });
        }
    }
}
