using System;
using System.Security.Cryptography;
using System.Text;

namespace QASmartClass.Services
{
    public static class ConfigurationSecurityHelper
    {
        private const string SECURE_SALT = "QASmartTouch_SecureSalt_2026_@AdminLock";

        public static string ComputeSha256Hash(string password)
        {
            if (password == null) return string.Empty;
            using (var sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password + SECURE_SALT));
                var builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
