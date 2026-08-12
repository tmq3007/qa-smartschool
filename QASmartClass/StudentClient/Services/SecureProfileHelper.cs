using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Serilog;

namespace QASmartClass.StudentClient.Services
{
    public static class SecureProfileHelper
    {
        public static string ReadProfileText(string path)
        {
            if (!File.Exists(path)) return string.Empty;
            try
            {
                var encryptedBytes = File.ReadAllBytes(path);
                var plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex)
            {
                // Hỗ trợ Backward Compatibility khi nâng cấp từ bản cũ
                try
                {
                    string text = File.ReadAllText(path);
                    if (text.TrimStart().StartsWith("{"))
                    {
                        // Mã hóa lại luôn
                        WriteProfileText(path, text);
                    }
                    return text;
                }
                catch
                {
                    Log.Warning("SecureProfileHelper: Không thể giải mã tệp profile: {Err}", ex.Message);
                    return string.Empty;
                }
            }
        }

        public static void WriteProfileText(string path, string text)
        {
            try
            {
                var plainBytes = Encoding.UTF8.GetBytes(text);
                var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(path, encryptedBytes);
            }
            catch (Exception ex)
            {
                Log.Error("SecureProfileHelper: Lỗi ghi profile bảo mật: {Err}", ex.Message);
                // Fallback cuối cùng để tránh crash ứng dụng khi không dùng được DPAPI
                File.WriteAllText(path, text);
            }
        }

        public static T? ReadProfile<T>(string path) where T : class
        {
            var json = ReadProfileText(path);
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                Log.Warning("SecureProfileHelper: Lỗi deserialize: {Err}", ex.Message);
                return null;
            }
        }

        public static void WriteProfile<T>(string path, T profile) where T : class
        {
            var json = JsonSerializer.Serialize(profile);
            WriteProfileText(path, json);
        }
    }
}
