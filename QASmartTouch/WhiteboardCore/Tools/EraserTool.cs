using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Ink;
using QASmartClass.WhiteboardCore.Enums;

namespace QASmartClass.WhiteboardCore.Tools
{
    /// <summary>
    /// EraserTool — Quản lý cấu hình và trạng thái công cụ tẩy.
    /// 
    /// Thiết kế: Class thuần logic, KHÔNG tham chiếu UI controls.
    /// - Lưu trữ cấu hình tẩy (Size, Mode).
    /// - Phát sự kiện khi cấu hình thay đổi → Form2 lắng nghe để đồng bộ UI.
    /// - Hỗ trợ lưu/khôi phục cấu hình qua SaveSettings() / RestoreSettings().
    /// 
    /// Logic tẩy thực tế (EraseStrokeAt, EraseElementsInRect) vẫn ở Form2 partial class
    /// vì phụ thuộc trực tiếp vào Canvas.Children — sẽ chuyển sang Core ở giai đoạn sau.
    /// </summary>
    public class EraserTool : IWhiteboardTool
    {
        #region IWhiteboardTool Implementation

        public string Name => "Eraser";
        public ToolMode Mode => ToolMode.Eraser;
        public bool IsActive { get; private set; }

        public void Activate()
        {
            IsActive = true;
            // Khôi phục cấu hình đã lưu khi kích hoạt lại
            CurrentEraserSize = _savedEraserSize;
            CurrentEraserMode = _savedEraserMode;

            SettingsChanged?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine($"🧹 EraserTool Activated — Size={CurrentEraserSize}, Mode={CurrentEraserMode}");
        }

        public void Deactivate()
        {
            // Lưu cấu hình trước khi tắt
            SaveSettings();
            IsActive = false;
            Deactivated?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine("🧹 EraserTool Deactivated");
        }

        #endregion

        #region Eraser Configuration

        /// <summary>Kích thước vùng tẩy (bán kính, px).</summary>
        public int CurrentEraserSize { get; private set; } = 20;

        /// <summary>Chế độ tẩy hiện tại.</summary>
        public EraserMode CurrentEraserMode { get; private set; } = Enums.EraserMode.Stroke;

        // Cấu hình đã lưu (khôi phục khi re-activate)
        private int _savedEraserSize = 20;
        private EraserMode _savedEraserMode = Enums.EraserMode.Stroke;

        #endregion

        #region Events

        /// <summary>
        /// Phát ra khi cấu hình tẩy thay đổi.
        /// Form2 lắng nghe để đồng bộ _touchHandler.SetEraserProperties(...).
        /// </summary>
        public event EventHandler? SettingsChanged;

        /// <summary>
        /// Phát ra khi tool bị tắt.
        /// Form2 lắng nghe để dọn dẹp UI (eraser preview, cursor...).
        /// </summary>
        public event EventHandler? Deactivated;

        /// <summary>
        /// Phát ra khi chế độ ClearAll được chọn.
        /// Form2 xử lý logic xóa tất cả (cần truy cập Canvas.Children).
        /// </summary>
        public event EventHandler? ClearAllRequested;

        #endregion

        #region Public Methods

        /// <summary>
        /// Cập nhật cấu hình tẩy (từ string mode, tương thích legacy code).
        /// </summary>
        public void Configure(int eraserSize, string eraserMode)
        {
            CurrentEraserSize = eraserSize;
            CurrentEraserMode = ParseEraserMode(eraserMode);

            // Auto-save
            _savedEraserSize = eraserSize;
            _savedEraserMode = CurrentEraserMode;

            if (CurrentEraserMode == Enums.EraserMode.ClearAll)
            {
                ClearAllRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }

            System.Diagnostics.Debug.WriteLine($"🧹 EraserTool Configured — Size={eraserSize}, Mode={eraserMode}");
        }

        /// <summary>
        /// Cập nhật cấu hình tẩy (từ enum mode).
        /// </summary>
        public void Configure(int eraserSize, EraserMode eraserMode)
        {
            CurrentEraserSize = eraserSize;
            CurrentEraserMode = eraserMode;

            _savedEraserSize = eraserSize;
            _savedEraserMode = eraserMode;

            if (eraserMode == Enums.EraserMode.ClearAll)
            {
                ClearAllRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Lưu cấu hình hiện tại.</summary>
        public void SaveSettings()
        {
            _savedEraserSize = CurrentEraserSize;
            _savedEraserMode = CurrentEraserMode;
        }

        /// <summary>Khôi phục cấu hình đã lưu.</summary>
        public void RestoreSettings()
        {
            CurrentEraserSize = _savedEraserSize;
            CurrentEraserMode = _savedEraserMode;
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Trả về cấu hình hiện tại dưới dạng tuple.
        /// </summary>
        public (int Size, EraserMode Mode) GetCurrentSettings()
        {
            return (CurrentEraserSize, CurrentEraserMode);
        }

        /// <summary>
        /// Trả về chế độ tẩy dưới dạng string (tương thích legacy code).
        /// </summary>
        public string GetCurrentModeString()
        {
            return CurrentEraserMode switch
            {
                Enums.EraserMode.Stroke => "Stroke",
                Enums.EraserMode.Point => "Point",
                Enums.EraserMode.Drag => "Point", // Map legacy drag to point
                Enums.EraserMode.ClearAll => "ClearAll",
                _ => "Stroke"
            };
        }

        #endregion

        #region Session Management

        private readonly Dictionary<string, NativeEraseSession> _sessions = new Dictionary<string, NativeEraseSession>();

        /// <summary>
        /// Bắt đầu một phiên tẩy Native Inking (theo dõi stroke bị xóa và stroke con mới sinh ra).
        /// </summary>
        public NativeEraseSession StartSession(string sessionId = "default")
        {
            var session = new NativeEraseSession();
            _sessions[sessionId] = session;
            return session;
        }

        /// <summary>
        /// Lấy phiên tẩy Native Inking đang hoạt động.
        /// </summary>
        public NativeEraseSession? GetSession(string sessionId = "default")
        {
            return _sessions.TryGetValue(sessionId, out var s) ? s : null;
        }

        /// <summary>
        /// Kết thúc phiên tẩy Native Inking và trả về kết quả để tạo Undo action.
        /// </summary>
        public NativeEraseSession? EndSession(string sessionId = "default")
        {
            if (_sessions.TryGetValue(sessionId, out var s))
            {
                _sessions.Remove(sessionId);
                return s;
            }
            return null;
        }

        #endregion

        #region Erase Operations

        /// <summary>
        /// XÓA THEO NÉT (Stroke Erase): Xóa hoàn toàn toàn bộ nét vẽ khi đầu tẩy chạm vào bất kỳ điểm nào của nét.
        /// </summary>
        public List<Stroke> EraseByStrokeAt(
            StrokeCollection? strokes,
            Point point,
            double? overrideRadius = null,
            NativeEraseSession? session = null,
            Action<Stroke>? onStrokeRemoved = null)
        {
            var erasedStrokes = new List<Stroke>();
            if (strokes == null || strokes.Count == 0) return erasedStrokes;

            double radius = overrideRadius ?? CurrentEraserSize;
            double diameter = radius * 2.0;

            var hitStrokes = strokes.HitTest(point, diameter);
            if (hitStrokes != null && hitStrokes.Count > 0)
            {
                foreach (Stroke s in hitStrokes)
                {
                    if (strokes.Contains(s))
                    {
                        strokes.Remove(s);
                        erasedStrokes.Add(s);
                        session?.RegisterRemovedStroke(s);
                        onStrokeRemoved?.Invoke(s);
                    }
                }
            }

            return erasedStrokes;
        }

        /// <summary>
        /// XÓA THEO ĐIỂM (Point Erase): Cắt xén nét vẽ bằng thuật toán GetClipResult của Windows MilCore.
        /// Chỉ xóa đoạn nét nằm dưới đầu tẩy; các phần còn lại của nét vẽ được giữ nguyên vẹn trên bảng.
        /// </summary>
        public List<Stroke> EraseByPointAt(
            StrokeCollection? strokes,
            Point currentPoint,
            Point? lastPoint = null,
            double? overrideRadius = null,
            NativeEraseSession? session = null,
            Action<Stroke>? onStrokeAdded = null,
            Action<Stroke>? onStrokeRemoved = null)
        {
            var removedStrokes = new List<Stroke>();
            if (strokes == null || strokes.Count == 0) return removedStrokes;

            double radius = overrideRadius ?? CurrentEraserSize;
            double diameter = radius * 2.0;
            var shape = new EllipseStylusShape(diameter, diameter);

            // Tạo đoạn quét liên tục giữa điểm trước và điểm hiện tại để không bao giờ bị đứt nét khi di chuyển nhanh
            Point[] path;
            if (lastPoint.HasValue && (lastPoint.Value - currentPoint).Length > 0.05)
            {
                path = new Point[] { lastPoint.Value, currentPoint };
            }
            else
            {
                path = new Point[] { currentPoint, new Point(currentPoint.X + 0.01, currentPoint.Y) };
            }

            var hitStrokes = strokes.HitTest(path, shape);
            if (hitStrokes == null || hitStrokes.Count == 0) return removedStrokes;

            var hitList = hitStrokes.ToList();
            foreach (var s in hitList)
            {
                if (!strokes.Contains(s)) continue;

                // 🚀 MilCore Slicing: Cắt xén nét vẽ, trả về các đoạn còn lại
                var clipResults = s.GetEraseResult(path, shape);

                // Gỡ stroke gốc bị cắt xén
                strokes.Remove(s);
                removedStrokes.Add(s);
                session?.RegisterRemovedStroke(s);
                onStrokeRemoved?.Invoke(s);

                // Thêm các đoạn nét con còn lại vào bảng
                if (clipResults != null && clipResults.Count > 0)
                {
                    foreach (var piece in clipResults)
                    {
                        strokes.Add(piece);
                        session?.RegisterAddedStroke(piece);
                        onStrokeAdded?.Invoke(piece);
                    }
                }
            }

            return removedStrokes;
        }

        /// <summary>
        /// Phương thức điều phối chung (Dispatch) hỗ trợ tự động nhận diện chế độ Tẩy (Point hoặc Stroke).
        /// </summary>
        public List<Stroke> EraseStrokesAt(
            StrokeCollection? strokes,
            Point point,
            double? overrideRadius = null,
            string? mode = null,
            Point? lastPoint = null,
            NativeEraseSession? session = null,
            Action<Stroke>? onStrokeAdded = null,
            Action<Stroke>? onStrokeRemoved = null)
        {
            var effectiveMode = !string.IsNullOrEmpty(mode) ? ParseEraserMode(mode) : CurrentEraserMode;

            if (effectiveMode == EraserMode.Stroke)
            {
                return EraseByStrokeAt(strokes, point, overrideRadius, session, onStrokeRemoved);
            }
            else
            {
                return EraseByPointAt(strokes, point, lastPoint, overrideRadius, session, onStrokeAdded, onStrokeRemoved);
            }
        }

        /// <summary>
        /// Xóa tất cả nét vẽ Native Inking nằm trong vùng chữ nhật (Marquee Erase / Drag Erase).
        /// </summary>
        public List<Stroke> EraseStrokesInRect(StrokeCollection? strokes, Rect bounds)
        {
            var erasedStrokes = new List<Stroke>();
            if (strokes == null || strokes.Count == 0) return erasedStrokes;

            var hitStrokes = strokes.HitTest(bounds, 10);
            if (hitStrokes != null && hitStrokes.Count > 0)
            {
                foreach (Stroke s in hitStrokes)
                {
                    strokes.Remove(s);
                    erasedStrokes.Add(s);
                }
            }

            return erasedStrokes;
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Chuyển đổi string → EraserMode enum (tương thích legacy code).
        /// </summary>
        public static EraserMode ParseEraserMode(string mode)
        {
            return mode?.ToLowerInvariant() switch
            {
                "stroke" => Enums.EraserMode.Stroke,
                "point" => Enums.EraserMode.Point,
                "drag" => Enums.EraserMode.Point, // Map legacy drag to point
                "clearall" => Enums.EraserMode.ClearAll,
                _ => Enums.EraserMode.Stroke
            };
        }

        #endregion
    }

    /// <summary>
    /// Phiên tẩy xóa Native Inking để gom các stroke bị xóa và stroke con mới sinh ra vào một bước Undo duy nhất.
    /// </summary>
    public class NativeEraseSession
    {
        private readonly HashSet<Stroke> _originalRemovedStrokes = new HashSet<Stroke>();
        private readonly HashSet<Stroke> _activeGeneratedStrokes = new HashSet<Stroke>();

        public void RegisterRemovedStroke(Stroke s)
        {
            if (s == null) return;
            // Nếu stroke này là một stroke con vừa được sinh ra trong chính phiên tẩy hiện tại,
            // việc xóa nó có nghĩa là nó không còn tồn tại nữa -> chỉ cần gỡ khỏi active generated.
            if (_activeGeneratedStrokes.Contains(s))
            {
                _activeGeneratedStrokes.Remove(s);
            }
            else
            {
                _originalRemovedStrokes.Add(s);
            }
        }

        public void RegisterAddedStroke(Stroke s)
        {
            if (s == null) return;
            _activeGeneratedStrokes.Add(s);
        }

        public List<Stroke> GetFinalRemovedStrokes() => _originalRemovedStrokes.ToList();
        public List<Stroke> GetFinalAddedStrokes() => _activeGeneratedStrokes.ToList();
        public bool HasChanges => _originalRemovedStrokes.Count > 0 || _activeGeneratedStrokes.Count > 0;
    }
}
