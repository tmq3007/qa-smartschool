using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.Forms
{
    public partial class SelfDiagnosticsWindow : Window
    {
        private string _targetIp = "127.0.0.1";
        private DiagnosticsResult _result = new();

        public SelfDiagnosticsWindow()
        {
            InitializeComponent();
            LoadTargetIp();
        }

        private void LoadTargetIp()
        {
            try
            {
                using var db = new AppDbContext();
                var ipSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "ServerIP" || s.Id == "TeacherIP" || s.Id == "Server_IP");
                if (ipSetting != null && !string.IsNullOrEmpty(ipSetting.Value))
                {
                    _targetIp = ipSetting.Value;
                }
                lblNetworkDetails.Text = $"Ping tới máy chủ: {_targetIp}";
            }
            catch
            {
                lblNetworkDetails.Text = "Ping tới máy chủ: 127.0.0.1 (mặc định)";
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void Run_Click(object sender, RoutedEventArgs e)
        {
            btnRun.IsEnabled = false;
            btnCopy.IsEnabled = false;
            txtConsoleOutput.Text = "⌛ Đang bắt đầu chẩn đoán sức khỏe hệ thống...\n";

            ResetStatuses();

            _result = new DiagnosticsResult();

            // Run DB Test
            await RunDbTestAsync();

            // Run Ping Test
            await RunPingTestAsync();

            // Run Port Test
            await RunPortTestAsync();

            // Run WebView2 Test
            await RunWebviewTestAsync();

            txtConsoleOutput.Text += "\n✓ Quá trình chẩn đoán hoàn tất!\n";
            btnRun.IsEnabled = true;
            btnCopy.IsEnabled = true;
        }

        private void ResetStatuses()
        {
            lblDbStatus.Text = "Đang chạy...";
            lblDbStatus.Foreground = Brushes.Orange;

            lblNetworkStatus.Text = "Đang chạy...";
            lblNetworkStatus.Foreground = Brushes.Orange;

            lblPortStatus.Text = "Đang chạy...";
            lblPortStatus.Foreground = Brushes.Orange;

            lblWebviewStatus.Text = "Đang chạy...";
            lblWebviewStatus.Foreground = Brushes.Orange;
        }

        private async Task RunDbTestAsync()
        {
            txtConsoleOutput.Text += "-> Đang kiểm tra cơ sở dữ liệu SQLite...\n";
            var start = DateTime.Now;
            try
            {
                using var db = new AppDbContext();
                var tempId = "SelfDiag_TempTestKey_" + Guid.NewGuid().ToString().Substring(0, 8);
                
                // Write test
                var setting = new SystemSetting
                {
                    Id = tempId,
                    Value = "TestDiagnosticValue",
                    Category = "Diagnostic",
                    LastUpdated = DateTime.Now
                };
                db.SystemSettings.Add(setting);
                await db.SaveChangesAsync();

                // Read test
                var fetched = await db.SystemSettings.FirstOrDefaultAsync(x => x.Id == tempId);
                if (fetched == null || fetched.Value != "TestDiagnosticValue")
                {
                    throw new InvalidOperationException("Đọc dữ liệu vừa ghi thất bại.");
                }

                // Delete test
                db.SystemSettings.Remove(fetched);
                await db.SaveChangesAsync();

                var latency = (DateTime.Now - start).TotalMilliseconds;
                lblDbStatus.Text = "PASS";
                lblDbStatus.Foreground = Brushes.Green;
                lblDbDetails.Text = $"Đọc/Ghi thành công ({latency:F1} ms)";
                txtConsoleOutput.Text += $"   [OK] SQLite DB phản hồi trong {latency:F1} ms.\n";

                _result.Database = new CheckItem { Status = "PASS", Message = $"OK ({latency:F1} ms)" };
            }
            catch (Exception ex)
            {
                lblDbStatus.Text = "FAIL";
                lblDbStatus.Foreground = Brushes.Red;
                lblDbDetails.Text = "Lỗi truy cập cơ sở dữ liệu!";
                txtConsoleOutput.Text += $"   [FAIL] SQLite DB lỗi: {ex.Message}\n";

                _result.Database = new CheckItem { Status = "FAIL", Message = ex.Message };
            }
        }

        private async Task RunPingTestAsync()
        {
            txtConsoleOutput.Text += $"-> Đang Ping tới máy chủ {_targetIp}...\n";
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(_targetIp, 2000); // 2 seconds timeout
                if (reply.Status == IPStatus.Success)
                {
                    lblNetworkStatus.Text = "PASS";
                    lblNetworkStatus.Foreground = Brushes.Green;
                    lblNetworkDetails.Text = $"Ping thành công ({reply.RoundtripTime} ms)";
                    txtConsoleOutput.Text += $"   [OK] Phản hồi Ping từ {_targetIp} trong {reply.RoundtripTime} ms.\n";

                    _result.Network = new CheckItem { Status = "PASS", Message = $"OK ({reply.RoundtripTime} ms)" };
                }
                else
                {
                    throw new PingException($"Trạng thái Ping: {reply.Status}");
                }
            }
            catch (Exception ex)
            {
                lblNetworkStatus.Text = "FAIL";
                lblNetworkStatus.Foreground = Brushes.Red;
                lblNetworkDetails.Text = "Không thể kết nối vật lý!";
                txtConsoleOutput.Text += $"   [FAIL] Ping tới {_targetIp} thất bại: {ex.Message}\n";

                _result.Network = new CheckItem { Status = "FAIL", Message = ex.Message };
            }
        }

        private async Task RunPortTestAsync()
        {
            int[] ports = { 8080, 9000, 9001 };
            txtConsoleOutput.Text += "-> Đang quét các cổng mạng truyền tin...\n";
            var sbDetails = new StringBuilder();
            bool allPortsOpen = true;

            foreach (var port in ports)
            {
                bool isOpen = await CheckPortAsync(_targetIp, port, 1000); // 1s timeout
                string portName = port switch
                {
                    8080 => "WebRTC/PhET",
                    9000 => "TCP Control",
                    9001 => "File Transfer",
                    _ => "Unknown"
                };

                if (isOpen)
                {
                    sbDetails.Append($"{port} [Mở], ");
                    txtConsoleOutput.Text += $"   [OK] Cổng {port} ({portName}) khả dụng.\n";
                }
                else
                {
                    sbDetails.Append($"{port} [Chặn], ");
                    txtConsoleOutput.Text += $"   [WARN] Cổng {port} ({portName}) không phản hồi.\n";
                    allPortsOpen = false;
                }

                _result.Ports.Add(port.ToString(), isOpen ? "OPEN" : "BLOCKED");
            }

            string detailText = sbDetails.ToString().TrimEnd(' ', ',');
            lblPortDetails.Text = detailText;

            if (allPortsOpen)
            {
                lblPortStatus.Text = "PASS";
                lblPortStatus.Foreground = Brushes.Green;
            }
            else
            {
                lblPortStatus.Text = "WARN";
                lblPortStatus.Foreground = Brushes.Orange;
            }
        }

        private async Task<bool> CheckPortAsync(string ip, int port, int timeoutMs)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(ip, port);
                var delayTask = Task.Delay(timeoutMs);

                var completedTask = await Task.WhenAny(connectTask, delayTask);
                if (completedTask == connectTask)
                {
                    await connectTask; // Wait to catch any socket exception
                    return true;
                }
                return false; // Timed out
            }
            catch
            {
                return false;
            }
        }

        private async Task RunWebviewTestAsync()
        {
            txtConsoleOutput.Text += "-> Đang kiểm tra WebView2 Runtime...\n";
            await Task.Delay(100); // UI breathing room
            try
            {
                string version = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
                lblWebviewStatus.Text = "PASS";
                lblWebviewStatus.Foreground = Brushes.Green;
                lblWebviewDetails.Text = $"WebView2 khả dụng (v{version})";
                txtConsoleOutput.Text += $"   [OK] Phát hiện WebView2 Runtime v{version} đã được cài đặt.\n";

                _result.WebView2 = new CheckItem { Status = "PASS", Message = version };
            }
            catch (Exception ex)
            {
                lblWebviewStatus.Text = "FAIL";
                lblWebviewStatus.Foreground = Brushes.Red;
                lblWebviewDetails.Text = "Thư viện WebView2 bị thiếu/lỗi!";
                txtConsoleOutput.Text += $"   [FAIL] WebView2 kiểm tra thất bại: {ex.Message}\n";

                _result.WebView2 = new CheckItem { Status = "FAIL", Message = ex.Message };
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_result, options);
                Clipboard.SetText(json);
                MessageBox.Show("Đã sao chép báo cáo JSON vào Clipboard!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi sao chép: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class CheckItem
        {
            public string Status { get; set; } = "UNKNOWN";
            public string Message { get; set; } = string.Empty;
        }

        public class DiagnosticsResult
        {
            public string Timestamp { get; set; } = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            public CheckItem Database { get; set; } = new();
            public CheckItem Network { get; set; } = new();
            public System.Collections.Generic.Dictionary<string, string> Ports { get; set; } = new();
            public CheckItem WebView2 { get; set; } = new();
        }
}
}
