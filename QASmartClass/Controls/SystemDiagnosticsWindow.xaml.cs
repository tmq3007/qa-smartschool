using System;
using System.IO;
using System.Linq;
using System.Windows;
using QASmartTouch;

namespace QASmartTouch.Controls
{
    /// <summary>
    /// Interaction logic for SystemDiagnosticsWindow.xaml
    /// </summary>
    public partial class SystemDiagnosticsWindow : Window
    {
        public SystemDiagnosticsWindow()
        {
            InitializeComponent();
            RunDiagnostics();
        }

        private void RunDiagnostics()
        {
            // 1. Database Check
            try
            {
                var app = Application.Current as App;
                if (app?.Database == null)
                {
                    txtDbIcon.Text = "🔴";
                    txtDbStatus.Text = "Lỗi: Không khởi tạo được Database Context.";
                }
                else
                {
                    int studentCount = app.Database.Students.Count();
                    txtDbIcon.Text = "🟢";
                    txtDbStatus.Text = $"Kết nối SQLite thành công. Cơ sở dữ liệu hoạt động. Tìm thấy {studentCount} học sinh trong hệ thống.";
                }
            }
            catch (Exception ex)
            {
                txtDbIcon.Text = "🔴";
                txtDbStatus.Text = $"Lỗi truy cập SQLite: {ex.Message}";
            }

            // 2. Resources Check
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string litPath = Path.Combine(baseDir, "Resources", "LiteratureData", "Phase4_Data");
                string exPath = Path.Combine(baseDir, "Resources", "LiteratureData", "exercises_by_topic");

                int litCount = Directory.Exists(litPath) ? Directory.GetFiles(litPath, "*.json").Length : 0;
                int exCount = Directory.Exists(exPath) ? Directory.GetFiles(exPath, "*.json").Length : 0;

                txtResIcon.Text = (litCount > 0) ? "🟢" : "🟡";
                txtResStatus.Text = $"Học liệu văn bản Ngữ Văn: {litCount} tệp tin JSON | Kho bài tập luyện tập: {exCount} chủ đề.";
            }
            catch (Exception ex)
            {
                txtResIcon.Text = "🔴";
                txtResStatus.Text = $"Lỗi khi quét thư mục học liệu: {ex.Message}";
            }

            // 3. Network Check
            try
            {
                var hostName = System.Net.Dns.GetHostName();
                var hostEntry = System.Net.Dns.GetHostEntry(hostName);
                var ipv4Addresses = hostEntry.AddressList
                    .Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .Select(ip => ip.ToString())
                    .ToList();

                txtNetIcon.Text = (ipv4Addresses.Count > 0) ? "🟢" : "🟡";
                txtNetStatus.Text = ipv4Addresses.Count > 0
                    ? $"Địa chỉ IP trạm: {string.Join(", ", ipv4Addresses)} (Mạng LAN hoạt động)"
                    : "Không tìm thấy IPv4 thích hợp trên thiết bị mạng LAN này.";
            }
            catch (Exception ex)
            {
                txtNetIcon.Text = "🔴";
                txtNetStatus.Text = $"Lỗi kiểm tra mạng LAN: {ex.Message}";
            }

            // 4. Font Check
            try
            {
                bool hasInter = System.Windows.Media.Fonts.SystemFontFamilies.Any(f => f.Source.Contains("Inter", StringComparison.OrdinalIgnoreCase));
                bool hasOutfit = System.Windows.Media.Fonts.SystemFontFamilies.Any(f => f.Source.Contains("Outfit", StringComparison.OrdinalIgnoreCase));

                txtFontIcon.Text = (hasInter && hasOutfit) ? "🟢" : "🟡";
                txtFontStatus.Text = $"Inter: {(hasInter ? "Sẵn sàng" : "Chưa cài (Segoe UI fallback)")} | Outfit: {(hasOutfit ? "Sẵn sàng" : "Chưa cài (Segoe UI fallback)")}";
            }
            catch (Exception ex)
            {
                txtFontIcon.Text = "🔴";
                txtFontStatus.Text = $"Lỗi kiểm tra Font: {ex.Message}";
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RunDiagnostics();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
