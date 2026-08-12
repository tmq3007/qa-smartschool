using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using QASmartClass.Data;
using QASmartClass.Helpers;
using QASmartTouch.Services;

namespace QASmartClass.Forms
{
    public partial class InAppSupportTicketWindow : Window
    {
        private byte[]? _screenshotBytes;
        private static readonly HttpClient _httpClient = new HttpClient();

        public InAppSupportTicketWindow()
        {
            InitializeComponent();
            CaptureAndShowScreenshot();
        }

        private void CaptureAndShowScreenshot()
        {
            try
            {
                int width = (int)SystemParameters.PrimaryScreenWidth;
                int height = (int)SystemParameters.PrimaryScreenHeight;
                using var bmp = new System.Drawing.Bitmap(width, height);
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                }

                using var ms = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                _screenshotBytes = ms.ToArray();

                ms.Position = 0;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                imgPreview.Source = bitmapImage;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[SupportTicket] Failed to capture or preview screenshot");
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

        private void AttachScreenshot_Checked(object sender, RoutedEventArgs e)
        {
            if (_screenshotBytes != null && imgPreview != null)
            {
                try
                {
                    using var ms = new MemoryStream(_screenshotBytes);
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = ms;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                    imgPreview.Source = bitmapImage;
                }
                catch { }
            }
        }

        private void AttachScreenshot_Unchecked(object sender, RoutedEventArgs e)
        {
            if (imgPreview != null)
            {
                imgPreview.Source = null;
            }
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            string category = (cboCategory.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Khác";
            string description = txtDescription.Text?.Trim() ?? string.Empty;
            bool attachScreenshot = chkAttachScreenshot.IsChecked == true;

            if (string.IsNullOrEmpty(description))
            {
                MessageBox.Show("Vui lòng nhập mô tả chi tiết lỗi gặp phải.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnSend.IsEnabled = false;

            try
            {
                // 1. Kiểm tra mạng xem server online không
                bool isOnline = await IsServerOnlineAsync();

                // 2. Ghi nhận sự cố vào EventLogs cục bộ
                string serverIp = "127.0.0.1";
                using (var db = new AppDbContext())
                {
                    var ipSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "ServerIP" || s.Id == "TeacherIP" || s.Id == "Server_IP");
                    if (ipSetting != null && !string.IsNullOrEmpty(ipSetting.Value))
                    {
                        serverIp = ipSetting.Value;
                    }

                    var log = new EventLog
                    {
                        Timestamp = DateTime.Now,
                        EventType = "SystemIncident",
                        Actor = AppSettings.ActiveUserRole ?? "GV/HS",
                        Details = $"[Category: {category}] {description}" + (attachScreenshot ? " (Đính kèm ảnh chụp)" : ""),
                        ClientIP = serverIp
                    };
                    db.EventLogs.Add(log);
                    await db.SaveChangesAsync();
                }

                // 3. Tiến hành gửi hoặc xếp hàng offline
                if (isOnline)
                {
                    bool apiSuccess = false;
                    try
                    {
                        var payload = new
                        {
                            Category = category,
                            Description = description,
                            Timestamp = DateTime.Now,
                            ScreenshotBase64 = (attachScreenshot && _screenshotBytes != null) ? Convert.ToBase64String(_screenshotBytes) : null,
                            Actor = AppSettings.ActiveUserRole ?? "GV/HS"
                        };
                        var json = JsonSerializer.Serialize(payload);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        using var cts = new CancellationTokenSource(2000);
                        var response = await _httpClient.PostAsync($"http://{serverIp}:9000/api/incident", content, cts.Token);
                        apiSuccess = response.IsSuccessStatusCode;
                    }
                    catch
                    {
                        apiSuccess = false;
                    }

                    if (apiSuccess)
                    {
                        MessageBox.Show("Đã gửi yêu cầu hỗ trợ tới bộ phận IT thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Gặp lỗi khi gửi API -> Đưa vào Offline Queue để đồng bộ tự động sau
                        await OfflineSyncManager.QueueItemAsync("TICKET", new
                        {
                            Category = category,
                            Description = description,
                            Timestamp = DateTime.Now,
                            ScreenshotBase64 = (attachScreenshot && _screenshotBytes != null) ? Convert.ToBase64String(_screenshotBytes) : null,
                            Actor = AppSettings.ActiveUserRole ?? "GV/HS"
                        });
                        MessageBox.Show("Không kết nối được dịch vụ. Yêu cầu hỗ trợ đã được xếp hàng và sẽ tự động gửi đi khi có mạng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    // Thiết bị mất kết nối -> Xếp hàng offline
                    await OfflineSyncManager.QueueItemAsync("TICKET", new
                    {
                        Category = category,
                        Description = description,
                        Timestamp = DateTime.Now,
                        ScreenshotBase64 = (attachScreenshot && _screenshotBytes != null) ? Convert.ToBase64String(_screenshotBytes) : null,
                        Actor = AppSettings.ActiveUserRole ?? "GV/HS"
                    });
                    MessageBox.Show("Thiết bị đang ngoại tuyến. Yêu cầu hỗ trợ đã được xếp hàng và sẽ tự động gửi đi khi có mạng.", "Thông báo ngoại tuyến", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi khi gửi yêu cầu hỗ trợ: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                btnSend.IsEnabled = true;
            }
        }

        private async Task<bool> IsServerOnlineAsync()
        {
            string serverIp = "127.0.0.1";
            try
            {
                using var db = new AppDbContext();
                var ipSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "ServerIP" || s.Id == "TeacherIP" || s.Id == "Server_IP");
                if (ipSetting != null && !string.IsNullOrEmpty(ipSetting.Value))
                {
                    serverIp = ipSetting.Value;
                }
            }
            catch { }

            try
            {
                using var ping = new System.Net.NetworkInformation.Ping();
                var reply = await ping.SendPingAsync(serverIp, 1000);
                return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }
    }
}
