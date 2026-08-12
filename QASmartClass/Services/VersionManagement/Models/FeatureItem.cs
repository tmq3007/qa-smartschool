using System.Collections.Generic;

namespace QASmartTouch.Services.VersionManagement.Models
{
    /// <summary>
    /// Model đại diện cho một feature item trong cấu trúc phân cấp.
    /// Mỗi feature có thể chứa các children features.
    /// </summary>
    public class FeatureItem
    {
        /// <summary>
        /// ID duy nhất của feature (VD: "brush", "arrow", "cube")
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// Tên hiển thị của feature (VD: "Đường mũi tên", "Hình cầu")
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Mô tả chi tiết về feature
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Trạng thái bật/tắt của feature
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Các feature con (nếu có)
        /// Key là tên property, Value là FeatureItem
        /// </summary>
        public Dictionary<string, FeatureItem>? Children { get; set; }

        /// <summary>
        /// Kiểm tra xem feature có children không
        /// </summary>
        public bool HasChildren => Children != null && Children.Count > 0;

        /// <summary>
        /// Đường dẫn đầy đủ của feature (được set bởi FeatureManager)
        /// VD: "shapes_2d.lines_group.arrow"
        /// </summary>
        public string FullPath { get; set; } = "";

        /// <summary>
        /// Parent feature (được set bởi FeatureManager)
        /// </summary>
        public FeatureItem? Parent { get; set; }

        /// <summary>
        /// Kiểm tra xem feature có thực sự được bật không
        /// (Tính cả trạng thái của parent)
        /// </summary>
        public bool IsEffectivelyEnabled
        {
            get
            {
                if (!Enabled) return false;
                if (Parent != null) return Parent.IsEffectivelyEnabled;
                return true;
            }
        }

        /// <summary>
        /// Tạo bản sao của FeatureItem
        /// </summary>
        public FeatureItem Clone()
        {
            var clone = new FeatureItem
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Enabled = Enabled,
                FullPath = FullPath
            };

            if (Children != null)
            {
                clone.Children = new Dictionary<string, FeatureItem>();
                foreach (var kvp in Children)
                {
                    var childClone = kvp.Value.Clone();
                    childClone.Parent = clone;
                    clone.Children[kvp.Key] = childClone;
                }
            }

            return clone;
        }

        public override string ToString()
        {
            return $"{Name} ({Id}) - {(Enabled ? "Enabled" : "Disabled")}";
        }
    }
}
