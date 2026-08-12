using System;
using System.Security.Cryptography;
using System.Text;

namespace SmartLibrary.Desktop.Helpers
{
    public static class SecurityHelper
    {
        public static string ComputeSha256(string input)
        {
            if (input == null) return string.Empty;
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToHexString(hash).ToLower();
            }
        }

        public static string GetConfigValue(string sectionName, string keyName, string defaultValue)
        {
            try
            {
                string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (System.IO.File.Exists(configPath))
                {
                    string jsonContent = System.IO.File.ReadAllText(configPath);
                    using var doc = System.Text.Json.JsonDocument.Parse(jsonContent);
                    if (doc.RootElement.TryGetProperty(sectionName, out var sectionProp) &&
                        sectionProp.TryGetProperty(keyName, out var keyProp))
                    {
                        return keyProp.GetString() ?? defaultValue;
                    }
                }
            }
            catch { }
            return defaultValue;
        }
    }
}
