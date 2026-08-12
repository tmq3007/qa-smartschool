using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace QASmartClass.Utilities
{
    public static class SecurityKeyProvider
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "QASmartClass", "secure_config.bin");
        
        private static readonly byte[] Entropy = new byte[] { 0x5a, 0x22, 0x1f, 0x7c, 0x88, 0x9a, 0x11, 0xef }; // Muối bảo mật bổ sung

        public static string GetHmacKey()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    // Fallback mặc định khi chưa đồng bộ khóa từ giáo viên
                    string fallbackKey = "QASmartClass_Internal_Backup_Secret_Key_2026";
                    SaveKey(fallbackKey);
                    return fallbackKey;
                }

                byte[] encryptedBytes = File.ReadAllBytes(ConfigPath);
                byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.LocalMachine);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                return "QASmartClass_Internal_Backup_Secret_Key_2026";
            }
        }

        public static void UpdateHmacKey(string serverKey)
        {
            if (!string.IsNullOrEmpty(serverKey) && serverKey != GetHmacKey())
            {
                SaveKey(serverKey);
            }
        }

        public static void SaveKey(string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] encryptedBytes = ProtectedData.Protect(keyBytes, Entropy, DataProtectionScope.LocalMachine);
            string dir = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(ConfigPath, encryptedBytes);
        }
    }
}
