using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QASmartTouch.Models;
using Serilog;

namespace QASmartTouch.Services.License
{
    /// <summary>
    /// Service quản lý license bản quyền offline cho QA SmartClass.
    /// Mô hình Challenge–Response: client tạo Request Code (hardware hash),
    /// QA team ký bằng ECDSA private key, client verify bằng public key nhúng sẵn.
    /// 
    /// Singleton pattern — truy cập qua LicenseService.Instance.
    /// </summary>
    public class LicenseService
    {
        // ═══ SINGLETON ═══
        private static LicenseService? _instance;
        private static readonly object _lock = new();

        public static LicenseService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new LicenseService();
                    }
                }
                return _instance;
            }
        }

        private LicenseService() { }

        // ═══ ECDSA PUBLIC KEY (nhúng sẵn — thay bằng key thật sau khi chạy KeyGen genkeys) ═══
        private const string PUBLIC_KEY_BASE64 = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE6fuo4tIgPrK5+Ggs/Z8lPMkjKbojSLY9dBFEc8JpKs64oDVdZ0sJU7w6nWkHw0zYXiovX6dAwsRrYaHxSeOSVA==";

        // ═══ PATHS ═══
        private static readonly string LicenseDir = QASmartClass.Services.AppPaths.RootDir;

        private static readonly string LicensePath = Path.Combine(LicenseDir, "license.lic");

        /// <summary>File đánh dấu ngày cài đặt lần đầu (ẩn, chống xóa tái tạo trial)</summary>
        private static readonly string FirstInstallMarkerPath = Path.Combine(LicenseDir, ".first_install");

        /// <summary>Thời gian dùng thử miễn phí (1 ngày = 24 giờ)</summary>
        private const int FREE_TRIAL_HOURS = 24;

        // ═══ PROPERTIES ═══

        /// <summary>License hiện tại (null nếu chưa kích hoạt)</summary>
        public LicenseInfo? CurrentLicense { get; private set; }

        /// <summary>Đang trong chế độ dùng thử miễn phí 1 ngày?</summary>
        public bool IsInFreeTrial { get; private set; }

        /// <summary>Thời gian còn lại của bản dùng thử (TimeSpan.Zero nếu không còn)</summary>
        public TimeSpan FreeTrialRemaining { get; private set; } = TimeSpan.Zero;

        // ═══ PUBLIC METHODS ═══

        /// <summary>
        /// Lấy Request Code (Hardware Fingerprint) để gửi cho QA team.
        /// Format: QASC-XXXX-XXXX-XXXX
        /// </summary>
        public string GetRequestCode() => HardwareFingerprint.Generate();

        /// <summary>
        /// Kích hoạt license bằng Activation Code.
        /// Code format: Base64(JSON).Base64(ECDSA_Signature)
        /// </summary>
        public LicenseResult Activate(string activationCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(activationCode))
                    return LicenseResult.Fail("Mã kích hoạt không được để trống.");

                // 1. Split: JSON_BASE64.SIGNATURE_BASE64
                var parts = activationCode.Trim().Split('.');
                if (parts.Length != 2)
                    return LicenseResult.Fail("Mã kích hoạt không đúng định dạng.");

                // 2. Decode
                byte[] jsonBytes;
                byte[] signature;
                try
                {
                    jsonBytes = Convert.FromBase64String(parts[0]);
                    signature = Convert.FromBase64String(parts[1]);
                }
                catch (FormatException)
                {
                    return LicenseResult.Fail("Mã kích hoạt không đúng định dạng Base64.");
                }

                // 3. Verify ECDSA signature
                if (!VerifySignature(jsonBytes, signature))
                    return LicenseResult.Fail("Chữ ký không hợp lệ — mã kích hoạt bị giả mạo hoặc đã bị sửa đổi.");

                // 4. Deserialize license JSON
                var json = Encoding.UTF8.GetString(jsonBytes);
                var license = JsonSerializer.Deserialize<LicenseInfo>(json);
                if (license == null)
                    return LicenseResult.Fail("Không đọc được thông tin license từ mã kích hoạt.");

                // 5. Verify hardware hash
                var currentHash = HardwareFingerprint.Generate();
                if (!string.Equals(license.HardwareHash, currentHash, StringComparison.OrdinalIgnoreCase))
                {
                    return LicenseResult.Fail(
                        $"Mã kích hoạt không dành cho máy này.\n" +
                        $"Mã máy trong license: {license.HardwareHash}\n" +
                        $"Mã máy hiện tại: {currentHash}");
                }

                // 6. Check expiry (bao gồm grace period)
                if (DateTime.UtcNow > license.GraceDeadline)
                {
                    return LicenseResult.Fail(
                        $"License đã hết hạn từ {license.ExpiresAt:dd/MM/yyyy}.\n" +
                        $"Ân hạn ({license.GracePeriodDays} ngày) cũng đã hết.\n" +
                        "Vui lòng liên hệ QA Smart School để gia hạn.");
                }

                // 7. Save license file
                Directory.CreateDirectory(LicenseDir);
                File.WriteAllText(LicensePath, activationCode.Trim());
                CurrentLicense = license;

                Log.Information("License activated: {Type} for {School}, expires {Expiry:dd/MM/yyyy}",
                    license.LicenseType, license.SchoolName, license.ExpiresAt);

                return LicenseResult.Ok(
                    $"Kích hoạt thành công!\n" +
                    $"Gói: {license.LicenseType.ToUpperInvariant()}\n" +
                    $"Trường: {license.SchoolName}\n" +
                    $"Hạn đến: {license.ExpiresAt:dd/MM/yyyy}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "License activation error");
                return LicenseResult.Fail($"Lỗi kích hoạt: {ex.Message}");
            }
        }

        /// <summary>
        /// Gia hạn license bằng Renewal Code.
        /// Renewal Code có cùng format như Activation Code nhưng chứa expires_at mới.
        /// </summary>
        public LicenseResult Renew(string renewalCode)
        {
            var result = Activate(renewalCode);
            if (!result.Success) return result;

            var lic = CurrentLicense!;
            return LicenseResult.Ok(
                $"Gia hạn thành công!\n" +
                $"Hạn mới: {lic.ExpiresAt:dd/MM/yyyy}\n" +
                $"Tổng thời gian sử dụng: {lic.TotalYearsUsed} năm\n" +
                $"Lần gia hạn thứ: {lic.RenewalHistory.Count}");
        }

        /// <summary>
        /// Kiểm tra trạng thái license khi khởi động app hoặc theo chu kỳ.
        /// </summary>
        public LicenseStatus CheckLicense()
        {
            // Chưa có file license → kiểm tra Free Trial 1 ngày
            if (!File.Exists(LicensePath))
            {
                var trialStatus = CheckFreeTrial();
                if (trialStatus == LicenseStatus.FreeTrial)
                    return LicenseStatus.FreeTrial;

                return LicenseStatus.NotActivated;
            }

            // Đọc file và verify lại
            string code;
            try
            {
                code = File.ReadAllText(LicensePath).Trim();
            }
            catch
            {
                return LicenseStatus.Invalid;
            }

            if (string.IsNullOrEmpty(code))
                return LicenseStatus.NotActivated;

            // Re-verify (activate internally, không ghi đè file)
            var result = ActivateInternal(code);
            if (!result.Success)
                return LicenseStatus.Invalid;

            var lic = CurrentLicense!;
            var now = DateTime.UtcNow;

            // Đã kích hoạt license thật → tắt trạng thái free trial
            IsInFreeTrial = false;

            // Hết hạn + hết grace → KHÓA TOÀN BỘ
            if (now > lic.GraceDeadline)
                return LicenseStatus.FullyExpired;

            // Hết hạn nhưng còn trong grace period
            if (lic.IsInGracePeriod)
                return LicenseStatus.GracePeriod;

            // Sắp hết hạn (≤ 30 ngày)
            if (lic.DaysRemaining <= 30)
                return LicenseStatus.ExpiringSoon;

            return LicenseStatus.Valid;
        }

        /// <summary>
        /// Kiểm tra xem license hiện tại có tính năng cụ thể không.
        /// Trả false nếu chưa kích hoạt hoặc đã hết hạn hoàn toàn.
        /// </summary>
        public bool HasFeature(string feature)
        {
            // Trong chế độ Free Trial 1 ngày → MỞ KHÓA TOÀN BỘ tính năng
            if (IsInFreeTrial)
                return true;

            if (CurrentLicense == null)
                return false;

            // Nếu hết hạn hoàn toàn → không có feature nào
            if (DateTime.UtcNow > CurrentLicense.GraceDeadline)
                return false;

            return CurrentLicense.Features.Contains(feature, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Lấy thông báo cảnh báo hết hạn để hiển thị trên UI.
        /// Trả chuỗi rỗng nếu không cần cảnh báo (> 60 ngày).
        /// </summary>
        public string GetExpiryWarning()
        {
            if (CurrentLicense == null) return "";

            var days = CurrentLicense.DaysRemaining;
            return days switch
            {
                > 60 => "",
                > 30 => $"⏳ License còn {days} ngày. Liên hệ QA Smart School để gia hạn.",
                > 7 => $"⚠️ License sắp hết hạn trong {days} ngày!",
                > 0 => $"🔴 CHỈ CÒN {days} NGÀY! Gia hạn ngay để tránh gián đoạn!",
                _ => CurrentLicense.IsInGracePeriod
                    ? $"❌ ĐÃ HẾT HẠN! Ân hạn còn {(int)(CurrentLicense.GraceDeadline - DateTime.UtcNow).TotalDays} ngày."
                    : "🔒 LICENSE HẾT HẠN — Toàn bộ tính năng đã bị khóa."
            };
        }

        /// <summary>
        /// Lấy tên hiển thị thân thiện cho loại gói license.
        /// </summary>
        public string GetLicenseTypeName()
        {
            return CurrentLicense?.LicenseType switch
            {
                "trial_7" => "Dùng thử (7 ngày)",
                "trial_30" => "Dùng thử (30 ngày)",
                "basic" => "Basic",
                "standard" => "Standard",
                "premium" => "Premium",
                "enterprise" => "Enterprise",
                _ => CurrentLicense?.LicenseType ?? "Chưa kích hoạt"
            };
        }

        // ═══ FREE TRIAL 1 NGÀY ═══

        /// <summary>
        /// Kiểm tra và quản lý chế độ dùng thử miễn phí 1 ngày.
        /// Lần đầu tiên mở app (chưa có marker file) → tạo marker + bật trial.
        /// Lần sau → đọc marker, kiểm tra còn trong 24h không.
        /// Nếu marker bị xóa → KHÔNG cấp lại trial (chống gian lận).
        /// </summary>
        private LicenseStatus CheckFreeTrial()
        {
            try
            {
                Directory.CreateDirectory(LicenseDir);

                if (!File.Exists(FirstInstallMarkerPath))
                {
                    // Lần đầu cài đặt → tạo marker file ghi timestamp
                    File.WriteAllText(FirstInstallMarkerPath, DateTime.UtcNow.ToString("o"));
                    // Đánh dấu file ẩn để người dùng không vô tình xóa
                    File.SetAttributes(FirstInstallMarkerPath, FileAttributes.Hidden | FileAttributes.ReadOnly);

                    IsInFreeTrial = true;
                    FreeTrialRemaining = TimeSpan.FromHours(FREE_TRIAL_HOURS);

                    Log.Information("Free Trial 1 ngày đã được kích hoạt cho lần cài đặt đầu tiên.");
                    return LicenseStatus.FreeTrial;
                }

                // Đọc thời điểm cài đặt từ marker file
                var markerContent = File.ReadAllText(FirstInstallMarkerPath).Trim();
                if (!DateTime.TryParse(markerContent, null, System.Globalization.DateTimeStyles.RoundtripKind, out var installTime))
                {
                    // Marker file bị lỗi → không cấp trial
                    IsInFreeTrial = false;
                    return LicenseStatus.NotActivated;
                }

                var elapsed = DateTime.UtcNow - installTime;
                var remaining = TimeSpan.FromHours(FREE_TRIAL_HOURS) - elapsed;

                if (remaining > TimeSpan.Zero)
                {
                    // Vẫn còn trong 24h → tiếp tục trial
                    IsInFreeTrial = true;
                    FreeTrialRemaining = remaining;

                    Log.Information("Free Trial: còn {Hours}h {Minutes}p",
                        (int)remaining.TotalHours, remaining.Minutes);
                    return LicenseStatus.FreeTrial;
                }
                else
                {
                    // Hết 24h → khóa
                    IsInFreeTrial = false;
                    FreeTrialRemaining = TimeSpan.Zero;

                    Log.Information("Free Trial 1 ngày đã hết hạn. Vui lòng kích hoạt bản quyền.");
                    return LicenseStatus.NotActivated;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Free Trial check error: {Error}", ex.Message);
                IsInFreeTrial = false;
                return LicenseStatus.NotActivated;
            }
        }

        /// <summary>
        /// Lấy thông báo trạng thái Free Trial để hiển thị trên UI.
        /// </summary>
        public string GetFreeTrialMessage()
        {
            if (!IsInFreeTrial)
                return "";

            var hours = (int)FreeTrialRemaining.TotalHours;
            var minutes = FreeTrialRemaining.Minutes;

            if (hours > 0)
                return $"🎁 DÙNG THỬ MIỄN PHÍ — Còn {hours} giờ {minutes} phút. Mua bản quyền để sử dụng lâu dài!";
            else
                return $"🎁 DÙNG THỬ MIỄN PHÍ — Còn {minutes} phút. Mua bản quyền ngay!";
        }

        // ═══ PRIVATE METHODS ═══

        /// <summary>
        /// Activate nội bộ — verify mà KHÔNG ghi đè file license.
        /// Dùng cho CheckLicense() để re-verify khi startup.
        /// </summary>
        private LicenseResult ActivateInternal(string activationCode)
        {
            try
            {
                var parts = activationCode.Split('.');
                if (parts.Length != 2)
                    return LicenseResult.Fail("Format sai");

                var jsonBytes = Convert.FromBase64String(parts[0]);
                var signature = Convert.FromBase64String(parts[1]);

                if (!VerifySignature(jsonBytes, signature))
                    return LicenseResult.Fail("Chữ ký không hợp lệ");

                var json = Encoding.UTF8.GetString(jsonBytes);
                var license = JsonSerializer.Deserialize<LicenseInfo>(json);
                if (license == null)
                    return LicenseResult.Fail("Không đọc được license");

                var currentHash = HardwareFingerprint.Generate();
                if (!string.Equals(license.HardwareHash, currentHash, StringComparison.OrdinalIgnoreCase))
                    return LicenseResult.Fail("Hardware mismatch");

                CurrentLicense = license;
                return LicenseResult.Ok("OK");
            }
            catch (Exception ex)
            {
                return LicenseResult.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Verify chữ ký ECDSA P-256 bằng public key nhúng sẵn.
        /// </summary>
        private static bool VerifySignature(byte[] data, byte[] signature)
        {
            try
            {
                using var ecdsa = ECDsa.Create();
                ecdsa.ImportSubjectPublicKeyInfo(
                    Convert.FromBase64String(PUBLIC_KEY_BASE64), out _);
                return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
            }
            catch (Exception ex)
            {
                Log.Warning("ECDSA verify error: {Error}", ex.Message);
                return false;
            }
        }
    }
}
