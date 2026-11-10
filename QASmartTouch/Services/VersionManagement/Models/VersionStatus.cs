using System;
using System.Collections.Generic;

namespace QASmartTouch.Services.VersionManagement.Models
{
    /// <summary>
    /// Model đại diện cho trạng thái phiên bản hiện tại của ứng dụng.
    /// Được đọc từ file VersionStatus.json
    /// </summary>
    public class VersionStatus
    {
        /// <summary>
        /// Số phiên bản hiện tại (VD: "1.0.0")
        /// </summary>
        public string CurrentVersion { get; set; } = "1.0.0";

        /// <summary>
        /// Tên phiên bản (VD: "Public Release")
        /// </summary>
        public string VersionName { get; set; } = "Public Release";

        /// <summary>
        /// Ngày phát hành
        /// </summary>
        public string ReleaseDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");

        /// <summary>
        /// Loại phiên bản: stable, beta, alpha, internal
        /// </summary>
        public string ReleaseType { get; set; } = "stable";

        /// <summary>
        /// Số build
        /// </summary>
        public int BuildNumber { get; set; } = 1001;

        /// <summary>
        /// Có phải phiên bản phát hành công khai không
        /// </summary>
        public bool IsPublicRelease { get; set; } = true;

        /// <summary>
        /// Template version đang active (VD: "1.0.0-public")
        /// </summary>
        public string ActiveTemplate { get; set; } = "1.0.0-public";

        /// <summary>
        /// Mô tả phiên bản
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Danh sách các thay đổi trong phiên bản
        /// </summary>
        public List<string> Changelog { get; set; } = new List<string>();

        /// <summary>
        /// Metadata bổ sung
        /// </summary>
        public VersionMetadata? Metadata { get; set; }
    }

    /// <summary>
    /// Metadata bổ sung cho phiên bản
    /// </summary>
    public class VersionMetadata
    {
        public string CreatedAt { get; set; } = "";
        public string LastModified { get; set; } = "";
        public string Author { get; set; } = "";
    }
}
