using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace QASmartTouch.Services.License
{
    /// <summary>
    /// Thu thập thông tin phần cứng máy tính để tạo Hardware Fingerprint.
    /// Kết hợp CPU ID + BIOS Serial + Disk Serial → SHA-256 → Base32 → format QASC-XXXX-XXXX-XXXX.
    /// Dùng để bind license vào một máy cụ thể (không copy sang máy khác được).
    /// </summary>
    public static class HardwareFingerprint
    {
        // Base32 alphabet loại bỏ ký tự dễ nhầm lẫn: I, O, 0, 1
        private const string BASE32_ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        /// <summary>
        /// Tạo Hardware Fingerprint dạng QASC-XXXX-XXXX-XXXX.
        /// Deterministic: cùng máy luôn cho cùng kết quả.
        /// </summary>
        public static string Generate()
        {
            var cpuId = GetWmiProperty("SELECT ProcessorId FROM Win32_Processor", "ProcessorId");
            var biosSerial = GetWmiProperty("SELECT SerialNumber FROM Win32_BIOS", "SerialNumber");
            var diskSerial = GetWmiProperty("SELECT SerialNumber FROM Win32_DiskDrive WHERE Index=0", "SerialNumber");

            var raw = $"{cpuId}|{biosSerial}|{diskSerial}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));

            // Lấy 10 bytes đầu → encode Base32 → 16 ký tự
            var b32 = ToBase32(hash.AsSpan(0, 10));

            // Format: QASC-XXXX-XXXX-XXXX (lấy 12 ký tự)
            return $"QASC-{b32[..4]}-{b32[4..8]}-{b32[8..12]}";
        }

        /// <summary>
        /// Truy vấn WMI để lấy thông tin phần cứng.
        /// Fallback trả về "UNKNOWN_[property]" nếu không đọc được.
        /// </summary>
        private static string GetWmiProperty(string query, string propertyName)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(query);
                foreach (var obj in searcher.Get())
                {
                    var value = obj[propertyName]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(value))
                        return value;
                }
            }
            catch
            {
                // WMI có thể fail trên một số máy đặc biệt
            }

            return $"UNKNOWN_{propertyName.ToUpperInvariant()}";
        }

        /// <summary>
        /// Encode bytes thành chuỗi Base32 (RFC 4648 variant, bỏ ký tự dễ nhầm).
        /// </summary>
        private static string ToBase32(ReadOnlySpan<byte> data)
        {
            var sb = new StringBuilder(data.Length * 2);
            int bits = 0, value = 0;

            foreach (var b in data)
            {
                value = (value << 8) | b;
                bits += 8;

                while (bits >= 5)
                {
                    sb.Append(BASE32_ALPHABET[(value >> (bits - 5)) & 31]);
                    bits -= 5;
                }
            }

            if (bits > 0)
            {
                sb.Append(BASE32_ALPHABET[(value << (5 - bits)) & 31]);
            }

            return sb.ToString();
        }
    }
}
