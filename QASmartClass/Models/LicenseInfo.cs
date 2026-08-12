using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Thông tin license bản quyền phần mềm QA SmartClass.
    /// Schema version 2: hỗ trợ quản lý theo năm, gia hạn, grace period.
    /// </summary>
    public class LicenseInfo
    {
        /// <summary>Schema version (hiện tại: 2)</summary>
        [JsonPropertyName("version")]
        public int Version { get; set; } = 2;

        /// <summary>Hardware hash của máy được cấp phép (format: QASC-XXXX-XXXX-XXXX)</summary>
        [JsonPropertyName("hardware_hash")]
        public string HardwareHash { get; set; } = "";

        /// <summary>Tên trường / tổ chức</summary>
        [JsonPropertyName("school_name")]
        public string SchoolName { get; set; } = "";

        /// <summary>Mã trường</summary>
        [JsonPropertyName("school_id")]
        public string SchoolId { get; set; } = "";

        /// <summary>Mã khách hàng</summary>
        [JsonPropertyName("customer_id")]
        public string CustomerId { get; set; } = "";

        /// <summary>Loại gói: trial_7, trial_30, basic, standard, premium, enterprise</summary>
        [JsonPropertyName("license_type")]
        public string LicenseType { get; set; } = "trial_7";

        /// <summary>Danh sách tính năng được phép: smartscreen, smartclass, learning_tools, quiz, screen_broadcast, file_transfer, multi_room, api</summary>
        [JsonPropertyName("features")]
        public List<string> Features { get; set; } = new();

        /// <summary>Số học sinh tối đa kết nối đồng thời</summary>
        [JsonPropertyName("max_students")]
        public int MaxStudents { get; set; } = 10;

        // ═══ QUẢN LÝ THEO NĂM ═══

        /// <summary>Ngày bắt đầu hợp đồng (lần kích hoạt đầu tiên)</summary>
        [JsonPropertyName("contract_start")]
        public DateTime ContractStart { get; set; }

        /// <summary>Thời hạn ban đầu (năm): 0=trial, 1=basic, 3=standard/premium, 5=enterprise</summary>
        [JsonPropertyName("initial_years")]
        public int InitialYears { get; set; }

        /// <summary>Ngày cấp key hiện tại (lần activate/renew gần nhất)</summary>
        [JsonPropertyName("issued_at")]
        public DateTime IssuedAt { get; set; }

        /// <summary>Ngày hết hạn hiện tại</summary>
        [JsonPropertyName("expires_at")]
        public DateTime ExpiresAt { get; set; }

        /// <summary>Compatibility property for ExpiryDate</summary>
        [JsonIgnore]
        public DateTime? ExpiryDate { get => ExpiresAt; set => ExpiresAt = value ?? DateTime.MinValue; }

        /// <summary>Mỗi lần gia hạn bao nhiêu năm (mặc định: 1)</summary>
        [JsonPropertyName("renewal_years")]
        public int RenewalYears { get; set; } = 1;

        /// <summary>Số ngày ân hạn sau khi hết hạn (0=trial, 7/15/30/60 theo gói)</summary>
        [JsonPropertyName("grace_period_days")]
        public int GracePeriodDays { get; set; }

        /// <summary>Lịch sử gia hạn</summary>
        [JsonPropertyName("renewal_history")]
        public List<RenewalRecord> RenewalHistory { get; set; } = new();

        // ═══ RE-ACTIVATION ═══

        /// <summary>Số lần đã re-activate (khi đổi phần cứng)</summary>
        [JsonPropertyName("reactivation_count")]
        public int ReactivationCount { get; set; }

        /// <summary>Số lần re-activate tối đa cho phép</summary>
        [JsonPropertyName("max_reactivations")]
        public int MaxReactivations { get; set; } = 3;

        // ═══ COMPUTED PROPERTIES ═══

        /// <summary>Deadline cuối cùng (hết hạn + grace period)</summary>
        [JsonIgnore]
        public DateTime GraceDeadline => ExpiresAt.AddDays(GracePeriodDays);

        /// <summary>Số ngày còn lại trước khi hết hạn (âm = đã quá hạn)</summary>
        [JsonIgnore]
        public int DaysRemaining => (int)(ExpiresAt - DateTime.UtcNow).TotalDays;

        /// <summary>Tổng số năm đã sử dụng từ ngày ký hợp đồng</summary>
        [JsonIgnore]
        public int TotalYearsUsed => (int)((DateTime.UtcNow - ContractStart).TotalDays / 365);

        /// <summary>Đang trong giai đoạn ân hạn (hết hạn nhưng chưa hết grace)?</summary>
        [JsonIgnore]
        public bool IsInGracePeriod => DateTime.UtcNow > ExpiresAt && DateTime.UtcNow <= GraceDeadline;

        /// <summary>Có phải gói trial không?</summary>
        [JsonIgnore]
        public bool IsTrial => LicenseType.StartsWith("trial", StringComparison.OrdinalIgnoreCase);

        /// <summary>Compatibility property for LicenseKey</summary>
        [JsonIgnore]
        public string LicenseKey { get; set; } = string.Empty;

        /// <summary>Compatibility property for IsOnline</summary>
        [JsonIgnore]
        public bool IsOnline { get; set; }

        /// <summary>Compatibility property for Status</summary>
        [JsonIgnore]
        public LicenseStatus Status { get; set; } = LicenseStatus.Valid;
    }

    /// <summary>
    /// Bản ghi lịch sử gia hạn license
    /// </summary>
    public class RenewalRecord
    {
        [JsonPropertyName("renewed_at")]
        public string RenewedAt { get; set; } = "";

        [JsonPropertyName("extended_to")]
        public string ExtendedTo { get; set; } = "";

        [JsonPropertyName("by")]
        public string By { get; set; } = "";
    }

    /// <summary>
    /// Trạng thái license hiện tại
    /// </summary>
    public enum LicenseStatus
    {
        /// <summary>License hợp lệ, hoạt động bình thường</summary>
        Valid,

        /// <summary>Chưa kích hoạt — chưa có file license.lic</summary>
        NotActivated,

        /// <summary>Đang trong 1 ngày dùng thử miễn phí (lần đầu cài đặt)</summary>
        FreeTrial,

        /// <summary>License không hợp lệ — chữ ký sai, hardware mismatch, file lỗi</summary>
        Invalid,

        /// <summary>Đã hết hạn</summary>
        Expired,

        /// <summary>Sắp hết hạn (≤ 30 ngày)</summary>
        ExpiringSoon,

        /// <summary>Đã hết hạn nhưng còn trong grace period — app vẫn chạy, hiện cảnh báo</summary>
        GracePeriod,

        /// <summary>Hết hạn hoàn toàn (hết cả grace) — KHÓA TOÀN BỘ tính năng</summary>
        FullyExpired,

        // --- Compatibility values ---
        Active,
        Offline,
        Trial
    }

    /// <summary>
    /// Kết quả xác thực / kích hoạt license
    /// </summary>
    public record LicenseResult(bool Success, string Message)
    {
        public static LicenseResult Ok(string message) => new(true, message);
        public static LicenseResult Fail(string message) => new(false, message);
    }
}
