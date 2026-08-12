using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartTouch.Models;

namespace QASmartTouch.Services
{
    public class AuthenticationService
    {
        private static AuthenticationService? _instance;
        private static readonly object _lock = new();

        public static AuthenticationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new AuthenticationService();
                    }
                }
                return _instance;
            }
        }

        private AuthenticationService() { }

        public static string HashPassword(string password)
        {
            // Generate a 128-bit salt (16 bytes)
            byte[] salt = new byte[16];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Derive a 256-bit subkey (32 bytes)
            using (var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
                password, salt, 100000, System.Security.Cryptography.HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(32);
                return $"pbkdf2:100000:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
            }
        }

        public static string HashPasswordHMACSHA512(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using (var hmac = new System.Security.Cryptography.HMACSHA512(salt))
            {
                byte[] hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
            }
        }

        private static bool SafeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            uint diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
        }

        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(hash)) return false;

            // Case 1: PBKDF2 format (e.g. pbkdf2:100000:salt:hash)
            if (hash.StartsWith("pbkdf2:"))
            {
                try
                {
                    var parts = hash.Split(':');
                    if (parts.Length != 4) return false;

                    int iterations = int.Parse(parts[1]);
                    byte[] salt = Convert.FromBase64String(parts[2]);
                    byte[] expectedSubkey = Convert.FromBase64String(parts[3]);

                    using (var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
                        password, salt, iterations, System.Security.Cryptography.HashAlgorithmName.SHA256))
                    {
                        byte[] actualSubkey = pbkdf2.GetBytes(expectedSubkey.Length);
                        return SafeEquals(actualSubkey, expectedSubkey);
                    }
                }
                catch
                {
                    return false;
                }
            }

            // Case 2: Legacy HMACSHA512 format (salt:hash)
            if (hash.Contains(":"))
            {
                try
                {
                    var parts = hash.Split(':');
                    if (parts.Length != 2) return false;

                    byte[] salt = Convert.FromBase64String(parts[0]);
                    byte[] expectedHash = Convert.FromBase64String(parts[1]);

                    using (var hmac = new System.Security.Cryptography.HMACSHA512(salt))
                    {
                        byte[] actualHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                        return SafeEquals(actualHash, expectedHash);
                    }
                }
                catch
                {
                    return false;
                }
            }

            // Case 3: Simple SHA256 base64 (original fallback)
            try
            {
                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                    var hashBytes = sha.ComputeHash(bytes);
                    string shaHash = Convert.ToBase64String(hashBytes);
                    if (shaHash == hash) return true;
                }
            }
            catch { }

            // Case 4: Plaintext fallback (Vô hiệu hóa vì lý do bảo mật)
            Serilog.Log.Warning("Rejected plaintext password comparison attempt.");
            return false;
        }

        /// <summary>
        /// Xác thực thông tin đăng nhập
        /// </summary>
        public async Task<(bool success, string message)> AuthenticateAsync(string userId, string password, string licenseKey)
        {
            // Simulate authentication delay
            await Task.Delay(1000);

            // Kiểm tra kết nối CSDL (Ngăn chặn bypass khi offline)
            try
            {
                var db = QASmartClass.Services.AppServices.Database;
                if (db != null)
                {
                    // Thực hiện truy vấn kết nối và kiểm tra tính hợp lệ của bảng để chắc chắn db hoạt động
                    await db.Database.OpenConnectionAsync();
                    await db.Database.CloseConnectionAsync();
                    
                    _ = await db.TeacherProfiles.AnyAsync();
                }
                else
                {
                    return (false, "Lỗi kết nối cơ sở dữ liệu. Vui lòng kiểm tra lại kết nối!");
                }
            }
            catch (Exception)
            {
                return (false, "Lỗi kết nối cơ sở dữ liệu. Vui lòng kiểm tra lại kết nối!");
            }

            // Kiểm tra tài khoản thử nghiệm
            if (userId == "GiaoVien01" && 
                password == "PassGiaoVien01" && 
                (licenseKey == "1AAAAAAAAAAAAAA1" || licenseKey == "1AAAAAAAAAAAAA1"))
            {
                return (true, "Đăng nhập thành công với tài khoản thử nghiệm!");
            }

            // Kiểm tra thông tin không được để trống
            if (string.IsNullOrWhiteSpace(userId) || 
                string.IsNullOrWhiteSpace(password) || 
                string.IsNullOrWhiteSpace(licenseKey))
            {
                return (false, "Vui lòng nhập đầy đủ thông tin đăng nhập!");
            }

            // Trong phiên bản thử nghiệm, chấp nhận mọi thông tin hợp lệ
            // TODO: Implement real authentication với server
            bool isTestLicense = (licenseKey == "1AAAAAAAAAAAAAA1" || licenseKey == "1AAAAAAAAAAAAA1");
            int minPasswordLength = isTestLicense ? 6 : 8;
            int minLicenseLength = isTestLicense ? 15 : 16;
            if (userId.Length >= 5 && password.Length >= minPasswordLength && licenseKey.Length >= minLicenseLength)
            {
                return (true, "Đăng nhập thành công!");
            }

            return (false, "Thông tin đăng nhập không hợp lệ!");
        }

        /// <summary>
        /// Kiểm tra license
        /// </summary>
        public async Task<LicenseInfo> CheckLicenseAsync(string licenseKey, bool isOnline)
        {
            await Task.Delay(500);

            var licenseInfo = new LicenseInfo
            {
                LicenseKey = licenseKey,
                IsOnline = isOnline
            };

            // Tài khoản thử nghiệm
            if (licenseKey == "1AAAAAAAAAAAAAA1" || licenseKey == "1AAAAAAAAAAAAA1")
            {
                licenseInfo.Status = LicenseStatus.Trial;
                licenseInfo.ExpiryDate = DateTime.Now.AddMonths(3);
                return licenseInfo;
            }

            // TODO: Implement real license checking
            if (isOnline)
            {
                // Online: check with server
                licenseInfo.Status = LicenseStatus.Active;
                licenseInfo.ExpiryDate = DateTime.Now.AddYears(1);
            }
            else
            {
                // Offline: check local cache
                licenseInfo.Status = LicenseStatus.Offline;
            }

            return licenseInfo;
        }
    }
}
