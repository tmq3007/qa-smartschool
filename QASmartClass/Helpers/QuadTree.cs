using System;
using System.Collections.Generic;
using System.Windows;
using QASmartTouch.Models;

namespace QASmartTouch.Helpers
{
    /// <summary>
    /// Cấu trúc dữ liệu QuadTree để tối ưu hóa việc phân hoạch không gian các đối tượng vẽ trên Canvas.
    /// Giúp tăng tốc độ truy vấn quét chọn vùng (Rectangle/Lasso Selection) và kiểm tra va chạm (Hit Testing).
    /// </summary>
    public class QuadTree
    {
        private const int MaxObjects = 10;
        private const int MaxLevels = 5;

        private readonly int _level;
        private readonly List<SelectableObject> _objects;
        private readonly Rect _bounds;
        private QuadTree[]? _nodes;

        public QuadTree(int level, Rect bounds)
        {
            _level = level;
            _bounds = bounds;
            _objects = new List<SelectableObject>();
        }

        /// <summary>
        /// Xóa sạch QuadTree và giải phóng các nút con
        /// </summary>
        public void Clear()
        {
            _objects.Clear();

            if (_nodes != null)
            {
                for (int i = 0; i < _nodes.Length; i++)
                {
                    _nodes[i].Clear();
                }
                _nodes = null;
            }
        }

        /// <summary>
        /// Phân chia nút hiện tại thành 4 nút con (Sub-nodes)
        /// </summary>
        private void Split()
        {
            double subWidth = _bounds.Width / 2;
            double subHeight = _bounds.Height / 2;
            double x = _bounds.X;
            double y = _bounds.Y;

            _nodes = new QuadTree[4]
            {
                new QuadTree(_level + 1, new Rect(x + subWidth, y, subWidth, subHeight)),           // Top-Right (0)
                new QuadTree(_level + 1, new Rect(x, y, subWidth, subHeight)),                      // Top-Left (1)
                new QuadTree(_level + 1, new Rect(x, y + subHeight, subWidth, subHeight)),           // Bottom-Left (2)
                new QuadTree(_level + 1, new Rect(x + subWidth, y + subHeight, subWidth, subHeight))  // Bottom-Right (3)
            };
        }

        /// <summary>
        /// Xác định chỉ số nút con chứa trọn vẹn đối tượng.
        /// Trả về -1 nếu đối tượng nằm cắt ngang biên giữa các nút con.
        /// </summary>
        private int GetIndex(Rect rect)
        {
            int index = -1;
            double verticalMidpoint = _bounds.X + (_bounds.Width / 2);
            double horizontalMidpoint = _bounds.Y + (_bounds.Height / 2);

            // Kiểm tra đối tượng có nằm trọn ở nửa trên không
            bool topQuadrant = (rect.Y < horizontalMidpoint && rect.Y + rect.Height < horizontalMidpoint);
            // Kiểm tra đối tượng có nằm trọn ở nửa dưới không
            bool bottomQuadrant = (rect.Y > horizontalMidpoint);

            // Kiểm tra đối tượng có nằm trọn ở nửa trái không
            if (rect.X < verticalMidpoint && rect.X + rect.Width < verticalMidpoint)
            {
                if (topQuadrant)
                {
                    index = 1; // Top-Left
                }
                else if (bottomQuadrant)
                {
                    index = 2; // Bottom-Left
                }
            }
            // Kiểm tra đối tượng có nằm trọn ở nửa phải không
            else if (rect.X > verticalMidpoint)
            {
                if (topQuadrant)
                {
                    index = 0; // Top-Right
                }
                else if (bottomQuadrant)
                {
                    index = 3; // Bottom-Right
                }
            }

            return index;
        }

        /// <summary>
        /// Chèn một đối tượng vào QuadTree.
        /// Nếu vượt quá giới hạn dung lượng nút, sẽ phân hoạch thêm nút con.
        /// </summary>
        public void Insert(SelectableObject obj)
        {
            if (obj == null) return;

            // Nếu nút hiện tại đã có nút con, chèn sâu xuống dưới
            if (_nodes != null)
            {
                int index = GetIndex(obj.Bounds);

                if (index != -1)
                {
                    _nodes[index].Insert(obj);
                    return;
                }
            }

            _objects.Add(obj);

            // Nếu số lượng vượt quá và chưa đạt độ sâu tối đa -> Split
            if (_objects.Count > MaxObjects && _level < MaxLevels)
            {
                if (_nodes == null)
                {
                    Split();
                }

                int i = 0;
                while (i < _objects.Count)
                {
                    int index = GetIndex(_objects[i].Bounds);
                    if (index != -1)
                    {
                        var target = _objects[i];
                        _objects.RemoveAt(i);
                        _nodes![index].Insert(target);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        /// <summary>
        /// Xóa một đối tượng khỏi QuadTree.
        /// </summary>
        public bool Remove(SelectableObject obj)
        {
            if (obj == null) return false;

            // Tìm trong danh sách đối tượng của nút hiện tại
            if (_objects.Remove(obj))
            {
                return true;
            }

            // Nếu có các nút con, chuyển hướng tìm kiếm xuống dưới
            if (_nodes != null)
            {
                int index = GetIndex(obj.Bounds);
                if (index != -1)
                {
                    return _nodes[index].Remove(obj);
                }
                else
                {
                    // Nếu vật thể nằm đè biên, có thể nằm ở bất kỳ nút con nào
                    bool removed = false;
                    for (int i = 0; i < 4; i++)
                    {
                        if (_nodes[i].Remove(obj))
                        {
                            removed = true;
                        }
                    }
                    return removed;
                }
            }

            return false;
        }

        /// <summary>
        /// Truy vấn tìm tất cả đối tượng giao cắt hoặc nằm trong Rect chỉ định
        /// </summary>
        public List<SelectableObject> Retrieve(Rect rect)
        {
            var returnObjects = new List<SelectableObject>();
            RetrieveInternal(returnObjects, rect);
            return returnObjects;
        }

        private void RetrieveInternal(List<SelectableObject> returnObjects, Rect rect)
        {
            // Lấy các đối tượng ở nút hiện tại mà giao với rect truy vấn
            foreach (var obj in _objects)
            {
                if (rect.IntersectsWith(obj.Bounds))
                {
                    returnObjects.Add(obj);
                }
            }

            // Tiếp tục tìm dưới các nút con
            if (_nodes != null)
            {
                int index = GetIndex(rect);
                if (index != -1)
                {
                    _nodes[index].RetrieveInternal(returnObjects, rect);
                }
                else
                {
                    // Nếu vùng truy vấn giao cắt biên, duyệt tất cả nút con có va chạm biên
                    for (int i = 0; i < 4; i++)
                    {
                        if (rect.IntersectsWith(_nodes[i]._bounds))
                        {
                            _nodes[i].RetrieveInternal(returnObjects, rect);
                        }
                    }
                }
            }
        }
    }
}
