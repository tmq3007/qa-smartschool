using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace QASmartClass.Utilities
{
    public static class CryptoHelper
    {
        // Khóa bí mật HMAC cho ví canteen
        public const string CanteenHmacKey = "QASmartClass_Canteen_Secure_Key_2026";

        /// <summary>
        /// Mã hóa AES-256 với IV ngẫu nhiên.
        /// Bản mã trả về chứa 16 bytes IV + bản mã thực sự, dưới dạng Base64.
        /// </summary>
        public static string Encrypt(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            byte[] keyBytes = GetKeyBytes(key);
            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.GenerateIV();
            byte[] iv = aes.IV;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            
            // Ghi IV vào đầu MemoryStream
            ms.Write(iv, 0, iv.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Giải mã AES-256. Bản mã đầu vào chứa IV (16 bytes) ở đầu.
        /// </summary>
        public static string Decrypt(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            try
            {
                byte[] fullCipher = Convert.FromBase64String(cipherText);
                if (fullCipher.Length < 16) return string.Empty;

                byte[] iv = new byte[16];
                byte[] cipherBytes = new byte[fullCipher.Length - 16];

                Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
                Buffer.BlockCopy(fullCipher, 16, cipherBytes, 0, cipherBytes.Length);

                byte[] keyBytes = GetKeyBytes(key);
                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(cipherBytes);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs, Encoding.UTF8);

                return sr.ReadToEnd();
            }
            catch
            {
                // Trả về chuỗi rỗng nếu có lỗi giải mã (ví dụ sai khóa hoặc gói tin lỗi)
                return string.Empty;
            }
        }

        /// <summary>
        /// Tính toán mã băm HMAC-SHA256
        /// </summary>
        public static string ComputeHMAC(string input, string key = CanteenHmacKey)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);

            using var hmac = new HMACSHA256(keyBytes);
            byte[] hashBytes = hmac.ComputeHash(inputBytes);

            var sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        private static readonly byte[] DpapiEntropy = Encoding.UTF8.GetBytes("SmartClass_ExitPin_Entropy_2026");

        public static string EncryptWithDpapi(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = ProtectedData.Protect(plainBytes, DpapiEntropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error("DPAPI Encryption error: {Err}", ex.Message);
                return string.Empty;
            }
        }

        public static string DecryptWithDpapi(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText)) return string.Empty;
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, DpapiEntropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("DPAPI Decryption failed (could be due to machine change): {Err}", ex.Message);
                return string.Empty;
            }
        }

        private static byte[] GetKeyBytes(string key)
        {
            using var sha256 = SHA256.Create();
            byte[] saltedBytes = Encoding.UTF8.GetBytes(key + "_QASmartClassSalt2026");
            return sha256.ComputeHash(saltedBytes);
        }
    }
}
