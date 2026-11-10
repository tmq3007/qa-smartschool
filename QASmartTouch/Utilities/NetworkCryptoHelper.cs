using System;
using QASmartClass.Utilities;

namespace QASmartClass.Utilities
{
    public static class NetworkCryptoHelper
    {
        private const string StaticSharedSalt = "QA_SmartClass_Shared_Network_Salt_2026";

        /// <summary>
        /// Sinh khóa AES-256 từ SessionSalt động và muối tĩnh.
        /// </summary>
        public static string DeriveKey(string sessionSalt)
        {
            if (string.IsNullOrEmpty(sessionSalt))
            {
                return StaticSharedSalt; // Khóa fallback nếu session salt trống
            }
            return sessionSalt + "_" + StaticSharedSalt;
        }

        /// <summary>
        /// Mã hóa gói tin mạng sử dụng AES-256
        /// </summary>
        public static string EncryptCommand(string plainText, string sessionSalt)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            string key = DeriveKey(sessionSalt);
            string encrypted = CryptoHelper.Encrypt(plainText, key);
            return $"ENC_CMD|{encrypted}";
        }

        /// <summary>
        /// Giải mã gói tin mạng sử dụng AES-256
        /// </summary>
        public static string DecryptCommand(string encryptedPayload, string sessionSalt)
        {
            if (string.IsNullOrEmpty(encryptedPayload)) return string.Empty;
            
            if (encryptedPayload.StartsWith("ENC_CMD|"))
            {
                encryptedPayload = encryptedPayload.Substring("ENC_CMD|".Length);
            }
            
            string key = DeriveKey(sessionSalt);
            return CryptoHelper.Decrypt(encryptedPayload, key);
        }
    }
}
