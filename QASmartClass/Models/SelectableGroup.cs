using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Đại diện cho một nhóm các SelectableObject
    /// Group cho phép thao tác đồng thời trên nhiều objects
    /// </summary>
    public class SelectableGroup
    {
        #region Fields

        private List<SelectableObject> _members;
        private string _groupId;

        #endregion

        #region Properties

        /// <summary>
        /// ID duy nhất của group
        /// </summary>
        public string GroupId => _groupId;

        /// <summary>
        /// Danh sách các objects trong group
        /// </summary>
        public IReadOnlyList<SelectableObject> Members => _members.AsReadOnly();

        /// <summary>
        /// Số lượng objects trong group
        /// </summary>
        public int Count => _members.Count;

        /// <summary>
        /// Bounding box chung của cả group
        /// </summary>
        public Rect Bounds
        {
            get
            {
                if (_members.Count == 0)
                    return Rect.Empty;

                double minX = double.MaxValue;
                double minY = double.MaxValue;
                double maxX = double.MinValue;
                double maxY = double.MinValue;

                foreach (var obj in _members)
                {
                    var bounds = obj.Bounds;
                    minX = Math.Min(minX, bounds.Left);
                    minY = Math.Min(minY, bounds.Top);
                    maxX = Math.Max(maxX, bounds.Right);
                    maxY = Math.Max(maxY, bounds.Bottom);
                }

                return new Rect(minX, minY, maxX - minX, maxY - minY);
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Tạo group mới với danh sách objects
        /// </summary>
        public SelectableGroup(IEnumerable<SelectableObject> objects)
        {
            _groupId = Guid.NewGuid().ToString();
            _members = new List<SelectableObject>(objects);

            // Gán GroupId cho tất cả members
            foreach (var obj in _members)
            {
                obj.GroupId = _groupId;
            }

            System.Diagnostics.Debug.WriteLine($"✅ Group created: {_groupId} with {_members.Count} objects");
        }

        /// <summary>
        /// Tạo group với ID cụ thể (dùng cho deserialization)
        /// </summary>
        public SelectableGroup(string groupId, IEnumerable<SelectableObject> objects)
        {
            _groupId = groupId;
            _members = new List<SelectableObject>(objects);

            foreach (var obj in _members)
            {
                obj.GroupId = _groupId;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Thêm object vào group
        /// </summary>
        public void AddMember(SelectableObject obj)
        {
            if (!_members.Contains(obj))
            {
                _members.Add(obj);
                obj.GroupId = _groupId;
                System.Diagnostics.Debug.WriteLine($"   ✓ Added {obj.Type} to group {_groupId}");
            }
        }

        /// <summary>
        /// Xóa object khỏi group
        /// </summary>
        public void RemoveMember(SelectableObject obj)
        {
            if (_members.Remove(obj))
            {
                obj.GroupId = null;
                System.Diagnostics.Debug.WriteLine($"   ✓ Removed {obj.Type} from group {_groupId}");
            }
        }

        /// <summary>
        /// Kiểm tra object có thuộc group không
        /// </summary>
        public bool Contains(SelectableObject obj)
        {
            return _members.Contains(obj);
        }

        /// <summary>
        /// Ungroup - xóa tất cả members khỏi group
        /// </summary>
        public void Ungroup()
        {
            foreach (var obj in _members)
            {
                obj.GroupId = null;
            }
            _members.Clear();
            System.Diagnostics.Debug.WriteLine($"❌ Group {_groupId} ungrouped");
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return $"Group({_groupId.Substring(0, 8)}...) - {_members.Count} objects";
        }

        #endregion
    }
}
