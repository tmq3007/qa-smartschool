using System.Collections.Generic;

namespace QASmartTouch.Services.VersionManagement.Models
{
    /// <summary>
    /// Model đại diện cho cấu hình chi tiết các features.
    /// Được đọc từ file VersionDetail.json
    /// </summary>
    public class VersionDetail
    {
        /// <summary>
        /// Phiên bản của cấu hình
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Thời gian cập nhật cuối
        /// </summary>
        public string LastModified { get; set; } = "";

        /// <summary>
        /// Phiên bản schema
        /// </summary>
        public string SchemaVersion { get; set; } = "1.0";

        /// <summary>
        /// Dictionary chứa tất cả các features
        /// Key là tên category (VD: "DrawingTools", "ShapeTools2D")
        /// </summary>
        public Dictionary<string, FeatureItem> Features { get; set; } = new Dictionary<string, FeatureItem>();

        /// <summary>
        /// Các template phiên bản định sẵn
        /// Key là tên template (VD: "1.0.0-public", "1.1.0-beta")
        /// </summary>
        public Dictionary<string, VersionTemplate> VersionTemplates { get; set; } = new Dictionary<string, VersionTemplate>();
    }

    /// <summary>
    /// Model đại diện cho một template phiên bản
    /// </summary>
    public class VersionTemplate
    {
        /// <summary>
        /// Tên template
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Mô tả template
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Loại phiên bản: stable, beta, alpha, internal
        /// </summary>
        public string ReleaseType { get; set; } = "stable";

        /// <summary>
        /// Các override cho features
        /// Key là đường dẫn feature (VD: "shapes_2d.lines_group.arrow")
        /// Value là trạng thái enabled mới
        /// </summary>
        public Dictionary<string, bool> Overrides { get; set; } = new Dictionary<string, bool>();
    }
}
