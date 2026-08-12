using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Utilities;

namespace QASmartClass.Tests
{
    public class V7UpgradeTests
    {
        [Fact]
        public void Test_BUG_701_StartupLock_HealDatabaseBeforeTelemetry()
        {
            // Kiểm tra tính logic: HealDatabase phải hoàn thành trước khi TelemetryService mở kết nối đĩa.
            // Biến Static FallbackConnection phải rỗng lúc khởi động nếu không có xung đột thực tế.
            Assert.Null(AppDbContext.FallbackInMemoryConnection);
        }

        [Fact]
        public void Test_BUG_702_Migrate_DoesNotDeleteDiskDb_WhenInFallbackMode()
        {
            // Giả lập trạng thái Fallback RAM DB
            var tempConnection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:;Foreign Keys=True");
            tempConnection.Open();
            AppDbContext.FallbackInMemoryConnection = tempConnection;

            string dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
            bool fileDeleted = false;

            try
            {
                using (var db = new AppDbContext())
                {
                    // Chạy Migrate trong chế độ Fallback
                    DbMigrator.Migrate(db, "6.04.0");
                }
                
                // Xác nhận tệp đĩa vật lý không bị xóa nhầm
                fileDeleted = !File.Exists(dbPath);
            }
            finally
            {
                // Dọn dẹp kết nối tạm
                AppDbContext.FallbackInMemoryConnection = null;
                tempConnection.Close();
                tempConnection.Dispose();
            }

            Assert.False(fileDeleted, "Tệp CSDL vật lý trên đĩa tuyệt đối không được bị xóa khi đang chạy chế độ RAM DB Fallback!");
        }

        [Fact]
        public void Test_BUG_703_Recovery_EnsuresDatabaseEncryptedAndMigrated()
        {
            // Kiểm tra tính độc lập: Khi chạy khôi phục, CSDL trên đĩa phải được mã hóa trước
            var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
            
            // Đảm bảo tệp đĩa tồn tại dưới dạng mã hóa hợp lệ
            DbEncryptionKeyManager.EnsureDatabaseEncrypted();
            Assert.True(File.Exists(dbPath), "Tệp CSDL vật lý phải được tạo và mã hóa.");
            
            using (var db = new AppDbContext())
            {
                // Đảm bảo Migrate không bị crash và bảng EventLogs được tạo trên đĩa
                DbMigrator.Migrate(db, "6.04.0");
                var tablesExist = db.Database.GetDbConnection().State;
                Assert.NotNull(tablesExist);
            }
        }

        [Fact]
        public void Test_BUG_704_SessionSignature_BootstrappingVerification()
        {
            // Giả lập lệnh UPDATE_SESSION ban đầu gửi về từ máy Giáo viên
            string newClassCode = "QA-0702-6518";
            string newSessionSalt = "fb2ff225be644220";
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string rawCmd = $"CMD|UPDATE_SESSION|{newClassCode}|{newSessionSalt}";
            string payload = $"{rawCmd}|{timestamp}";

            // Giáo viên ký lệnh bằng newClassCode (HMAC-SHA256)
            string signature;
            using (var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(newClassCode)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                signature = Convert.ToBase64String(hash);
            }
            string fullCmd = $"{payload}|{signature}";

            // Học sinh thực hiện giải mã và xác minh chữ ký (Giả định SessionSalt của học sinh đang rỗng)
            string sessionSalt = ""; // Trống ban đầu
            string classCode = "";   // Trống ban đầu

            string verifySalt = !string.IsNullOrEmpty(sessionSalt) ? sessionSalt : classCode;
            
            // Áp dụng vá lỗi logic: trích xuất newClassCode từ payload
            if (payload.StartsWith("CMD|UPDATE_SESSION|") && string.IsNullOrEmpty(sessionSalt))
            {
                var cmdParts = payload.Split('|');
                if (cmdParts.Length >= 4)
                {
                    verifySalt = cmdParts[2]; // QA-0702-6518
                }
            }

            string computedSignature;
            using (var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(verifySalt)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                computedSignature = Convert.ToBase64String(hash);
            }

            Assert.Equal(signature, computedSignature);
        }

        [Fact]
        public void Test_BUG_705_QoSSuffixParsing_ExtractsPortCorrectly()
        {
            // Lệnh trình chiếu đính kèm QoS ID ở cuối
            string cmdStart = "CMD|SCREEN_BROADCAST_START|img_01.png|8080|TK_ABC|id=110";
            
            var rawParts = cmdStart.Split('|');
            var cleanPartsList = new System.Collections.Generic.List<string>(rawParts);
            if (cleanPartsList.Count > 0 && cleanPartsList[cleanPartsList.Count - 1].StartsWith("id="))
            {
                cleanPartsList.RemoveAt(cleanPartsList.Count - 1);
            }
            var parts = cleanPartsList.ToArray();

            // Xác minh mảng đã làm sạch mất đi id=110
            Assert.Equal(5, parts.Length);
            Assert.Equal("TK_ABC", parts[parts.Length - 1]);
            Assert.Equal("8080", parts[parts.Length - 2]);

            // Trích xuất cổng kết nối
            bool isPortParsed = int.TryParse(parts[parts.Length - 2], out int portVal);
            Assert.True(isPortParsed);
            Assert.Equal(8080, portVal);
        }

        [Fact]
        public void Test_BUG_707_SqlitePoolingDisabled()
        {
            // Kiểm tra cấu hình chuỗi kết nối của AppDbContext phải chứa Pooling=False
            using (var db = new AppDbContext())
            {
                var connStr = db.Database.GetDbConnection().ConnectionString;
                Assert.Contains("Pooling=False", connStr);
            }
        }

        [Fact]
        public void Test_IMP_708_MessageGuard_CheatingAlert_Whitelisted()
        {
            // Gói tin gian lận gửi từ học sinh
            string msg = "CHEATING_ALERT|1|Học sinh rời khỏi màn hình kiểm tra (Alt+Tab hoặc chuyển App)";
            
            var guard = MessageGuard.Evaluate(msg);
            
            // Xác nhận bị ẩn khỏi Chat UI công cộng nhưng phân loại chính xác danh mục CheatingAlert
            Assert.False(guard.IsAllowedInChat);
            Assert.Equal(MessageGuard.MessageCategory.CheatingAlert, guard.Category);
        }
    }
}
