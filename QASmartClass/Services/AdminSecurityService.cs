using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Serilog;

namespace QASmartClass.Services
{
    public class AdminSecurityData
    {
        public string Salt { get; set; } = "";
        public string PinHash { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int FailedAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }
    }

    public class AdminSecurityService
    {
        private AdminSecurityData _data;
        private const int MAX_ATTEMPTS = 5;
        private const int LOCKOUT_MINUTES = 5;

        public AdminSecurityService()
        {
            _data = LoadData();
        }

        public bool IsFirstSetup => string.IsNullOrEmpty(_data.PinHash) || string.IsNullOrEmpty(_data.Salt);

        public bool IsLockedOut
        {
            get
            {
                if (_data.LockoutEnd.HasValue)
                {
                    if (DateTime.Now >= _data.LockoutEnd.Value)
                    {
                        // Lockout expired
                        _data.LockoutEnd = null;
                        _data.FailedAttempts = 0;
                        SaveData();
                        return false;
                    }
                    return true;
                }
                return false;
            }
        }

        public TimeSpan LockoutTimeRemaining
        {
            get
            {
                if (_data.LockoutEnd.HasValue && DateTime.Now < _data.LockoutEnd.Value)
                {
                    return _data.LockoutEnd.Value - DateTime.Now;
                }
                return TimeSpan.Zero;
            }
        }

        public int RemainingAttempts => Math.Max(0, MAX_ATTEMPTS - _data.FailedAttempts);

        public void ChangePin(string newPin)
        {
            if (string.IsNullOrWhiteSpace(newPin))
                throw new ArgumentException("PIN cannot be empty.");

            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            string salt = Convert.ToBase64String(saltBytes);
            string hash = HashPin(newPin, salt);

            _data.Salt = salt;
            _data.PinHash = hash;
            _data.CreatedAt = DateTime.Now;
            _data.FailedAttempts = 0;
            _data.LockoutEnd = null;

            SaveData();
            Log.Information("Admin PIN has been changed/setup.");
        }

        public bool ValidatePin(string pin)
        {
            if (IsLockedOut)
            {
                Log.Warning("[SECURITY] Attempted to validate PIN while locked out.");
                return false;
            }

            if (IsFirstSetup) return false;

            string expectedHash = HashPin(pin, _data.Salt);
            
            if (expectedHash == _data.PinHash)
            {
                // Success
                if (_data.FailedAttempts > 0)
                {
                    _data.FailedAttempts = 0;
                    SaveData();
                }
                return true;
            }
            else
            {
                // Fail
                RecordFailedAttempt();
                return false;
            }
        }

        private void RecordFailedAttempt()
        {
            _data.FailedAttempts++;
            if (_data.FailedAttempts >= MAX_ATTEMPTS)
            {
                _data.LockoutEnd = DateTime.Now.AddMinutes(LOCKOUT_MINUTES);
                Log.Warning("[SECURITY] PIN brute-force detected. Locked out until {LockoutEnd}", _data.LockoutEnd);
            }
            else
            {
                Log.Warning("[SECURITY] Failed PIN attempt ({Count}/{Max})", _data.FailedAttempts, MAX_ATTEMPTS);
            }
            SaveData();
        }

        private string HashPin(string pin, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(pin + salt);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private static readonly byte[] DpapiEntropy = Encoding.UTF8.GetBytes("QASmartClass_Admin_v4");

        private AdminSecurityData LoadData()
        {
            try
            {
                if (File.Exists(AppPaths.AdminSecurityFile))
                {
                    var fileBytes = File.ReadAllBytes(AppPaths.AdminSecurityFile);

                    // Thử giải mã DPAPI trước (format mới)
                    try
                    {
                        var decrypted = System.Security.Cryptography.ProtectedData.Unprotect(
                            fileBytes, DpapiEntropy,
                            System.Security.Cryptography.DataProtectionScope.LocalMachine);
                        string json = Encoding.UTF8.GetString(decrypted);
                        var data = JsonSerializer.Deserialize<AdminSecurityData>(json);
                        if (data != null) return data;
                    }
                    catch
                    {
                        // Fallback: thử đọc plaintext JSON (format cũ) → auto-migrate
                        try
                        {
                            string json = Encoding.UTF8.GetString(fileBytes);
                            var data = JsonSerializer.Deserialize<AdminSecurityData>(json);
                            if (data != null)
                            {
                                Log.Information("[Security] Migrating plaintext PIN to DPAPI encrypted format");
                                _data = data;
                                SaveData(); // Re-save encrypted
                                return data;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed to load AdminSecurityFile: {Err}", ex.Message);
            }
            return new AdminSecurityData();
        }

        private void SaveData()
        {
            try
            {
                var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
                var plainBytes = Encoding.UTF8.GetBytes(json);

                // Mã hóa bằng DPAPI — chỉ machine hiện tại mới giải mã được
                var encrypted = System.Security.Cryptography.ProtectedData.Protect(
                    plainBytes, DpapiEntropy,
                    System.Security.Cryptography.DataProtectionScope.LocalMachine);

                Directory.CreateDirectory(Path.GetDirectoryName(AppPaths.AdminSecurityFile)!);
                File.WriteAllBytes(AppPaths.AdminSecurityFile, encrypted);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to save AdminSecurityFile: {Err}", ex.Message);
            }
        }
    }
}
