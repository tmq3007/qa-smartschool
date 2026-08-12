using Xunit;
using System;
using System.Threading.Tasks;
using QASmartClass.Services;
using QASmartClass.Data;
using QASmartTouch.Services;
using QASmartTouch.Models;

namespace QASmartClass.Tests
{
    [Collection("Sequential")]
    public class AuthenticationServiceTests : IDisposable
    {
        private readonly string _tempDir;

        public AuthenticationServiceTests()
        {
            // Set up a temporary directory for database isolation in tests
            _tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            AppPaths.DataDirOverride = _tempDir;
            AppPaths.EnsureDirectories();
            DbEncryptionKeyManager.ClearCache();
        }

        public void Dispose()
        {
            // Clear database reference
            AppServices.Database = null;
            AppPaths.DataDirOverride = null;
            DbEncryptionKeyManager.ClearCache();

            // Clean up temp directory
            try
            {
                if (System.IO.Directory.Exists(_tempDir))
                {
                    System.IO.Directory.Delete(_tempDir, true);
                }
            }
            catch { }
        }

        // --- PASSWORD VERIFICATION TESTS ---

        [Fact]
        public void VerifyPassword_Pbkdf2_CorrectPassword_ReturnsTrue()
        {
            string password = "StrongPassword2026!";
            string hash = AuthenticationService.HashPassword(password);

            Assert.StartsWith("pbkdf2:", hash);
            Assert.True(AuthenticationService.VerifyPassword(password, hash));
        }

        [Fact]
        public void VerifyPassword_Pbkdf2_WrongPassword_ReturnsFalse()
        {
            string password = "StrongPassword2026!";
            string hash = AuthenticationService.HashPassword(password);

            Assert.False(AuthenticationService.VerifyPassword("WrongPassword!", hash));
        }

        [Fact]
        public void VerifyPassword_LegacyHMAC_CorrectPassword_ReturnsTrue()
        {
            string password = "LegacyPassword123";
            string hash = AuthenticationService.HashPasswordHMACSHA512(password);

            Assert.Contains(":", hash);
            Assert.False(hash.StartsWith("pbkdf2:"));
            Assert.True(AuthenticationService.VerifyPassword(password, hash));
        }

        [Fact]
        public void VerifyPassword_LegacyHMAC_WrongPassword_ReturnsFalse()
        {
            string password = "LegacyPassword123";
            string hash = AuthenticationService.HashPasswordHMACSHA512(password);

            Assert.False(AuthenticationService.VerifyPassword("WrongLegacyPassword", hash));
        }

        [Fact]
        public void VerifyPassword_FallbackSha256_CorrectPassword_ReturnsTrue()
        {
            string password = "Sha256Password";
            string shaHash;

            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                var hashBytes = sha.ComputeHash(bytes);
                shaHash = Convert.ToBase64String(hashBytes);
            }

            Assert.True(AuthenticationService.VerifyPassword(password, shaHash));
        }

        [Fact]
        public void VerifyPassword_FallbackSha256_WrongPassword_ReturnsFalse()
        {
            string password = "Sha256Password";
            string shaHash;

            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                var hashBytes = sha.ComputeHash(bytes);
                shaHash = Convert.ToBase64String(hashBytes);
            }

            Assert.False(AuthenticationService.VerifyPassword("IncorrectShaPassword", shaHash));
        }

        [Fact]
        public void VerifyPassword_PlaintextPassword_ReturnsFalse()
        {
            // Plaintext comparison should be rejected for security, yielding false
            Assert.False(AuthenticationService.VerifyPassword("PlainPassword", "PlainPassword"));
        }

        [Fact]
        public void VerifyPassword_NullOrEmptyHash_ReturnsFalse()
        {
            Assert.False(AuthenticationService.VerifyPassword("Password", null));
            Assert.False(AuthenticationService.VerifyPassword("Password", ""));
        }

        // --- AUTHENTICATE ASYNC TESTS ---

        [Fact]
        public async Task AuthenticateAsync_TestAccount_ReturnsSuccess()
        {
            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();
                AppServices.Database = db;

                var result = await AuthenticationService.Instance.AuthenticateAsync("GiaoVien01", "PassGiaoVien01", "1AAAAAAAAAAAAAA1");
                
                Assert.True(result.success);
                Assert.Contains("thành công", result.message);
            }
        }

        [Fact]
        public async Task AuthenticateAsync_EmptyInputs_ReturnsFailure()
        {
            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();
                AppServices.Database = db;

                var result1 = await AuthenticationService.Instance.AuthenticateAsync("", "somepass", "1AAAAAAAAAAAAAA1");
                var result2 = await AuthenticationService.Instance.AuthenticateAsync("user", null, "1AAAAAAAAAAAAAA1");
                var result3 = await AuthenticationService.Instance.AuthenticateAsync("user", "somepass", "   ");

                Assert.False(result1.success);
                Assert.False(result2.success);
                Assert.False(result3.success);
                Assert.Equal("Vui lòng nhập đầy đủ thông tin đăng nhập!", result1.message);
            }
        }

        [Fact]
        public async Task AuthenticateAsync_ValidFormatMock_ReturnsSuccess()
        {
            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();
                AppServices.Database = db;

                // Username >= 5, Password >= 8, LicenseKey >= 16
                var result = await AuthenticationService.Instance.AuthenticateAsync("TeacherUser", "ValidPassword123", "LICENSE-KEY-12345");
                
                Assert.True(result.success);
                Assert.Equal("Đăng nhập thành công!", result.message);
            }
        }

        [Fact]
        public async Task AuthenticateAsync_InvalidFormatMock_ReturnsFailure()
        {
            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();
                AppServices.Database = db;

                // Too short username
                var result1 = await AuthenticationService.Instance.AuthenticateAsync("user", "ValidPassword123", "LICENSE-KEY-12345");
                // Too short password
                var result2 = await AuthenticationService.Instance.AuthenticateAsync("TeacherUser", "short", "LICENSE-KEY-12345");
                // Too short license
                var result3 = await AuthenticationService.Instance.AuthenticateAsync("TeacherUser", "ValidPassword123", "lic");

                Assert.False(result1.success);
                Assert.False(result2.success);
                Assert.False(result3.success);
                Assert.Equal("Thông tin đăng nhập không hợp lệ!", result1.message);
            }
        }

        [Fact]
        public async Task AuthenticateAsync_DatabaseOffline_ReturnsDatabaseError()
        {
            // Null database service reference
            AppServices.Database = null;

            var result = await AuthenticationService.Instance.AuthenticateAsync("GiaoVien01", "PassGiaoVien01", "1AAAAAAAAAAAAAA1");
            
            Assert.False(result.success);
            Assert.Contains("kết nối cơ sở dữ liệu", result.message);
        }

        // --- LICENSE STATUS CHECKING TESTS ---

        [Fact]
        public async Task CheckLicenseAsync_TestLicenseKey_ReturnsTrial()
        {
            var license = await AuthenticationService.Instance.CheckLicenseAsync("1AAAAAAAAAAAAAA1", isOnline: false);
            
            Assert.Equal("1AAAAAAAAAAAAAA1", license.LicenseKey);
            Assert.Equal(LicenseStatus.Trial, license.Status);
            Assert.True(license.ExpiryDate > DateTime.Now);
        }

        [Fact]
        public async Task CheckLicenseAsync_OnlineKey_ReturnsActive()
        {
            var license = await AuthenticationService.Instance.CheckLicenseAsync("ANY-OTHER-LICENSE-KEY", isOnline: true);
            
            Assert.Equal("ANY-OTHER-LICENSE-KEY", license.LicenseKey);
            Assert.Equal(LicenseStatus.Active, license.Status);
            Assert.True(license.ExpiryDate > DateTime.Now);
        }

        [Fact]
        public async Task CheckLicenseAsync_OfflineKey_ReturnsOffline()
        {
            var license = await AuthenticationService.Instance.CheckLicenseAsync("ANY-OTHER-LICENSE-KEY", isOnline: false);
            
            Assert.Equal("ANY-OTHER-LICENSE-KEY", license.LicenseKey);
            Assert.Equal(LicenseStatus.Offline, license.Status);
        }
    }
}
