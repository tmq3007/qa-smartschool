using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.WhiteboardCore.Enums;

namespace QASmartClass.WhiteboardCore.Tools
{
    /// <summary>
    /// PenTool — Quản lý cấu hình và trạng thái bút vẽ tự do.
    /// Hỗ trợ chuẩn Native Inking (System.Windows.Ink.DrawingAttributes & Stroke).
    /// </summary>
    public class PenTool : IWhiteboardTool
    {
        #region IWhiteboardTool Implementation

        public string Name => "Pen";
        public ToolMode Mode => ToolMode.Pen;
        public bool IsActive { get; private set; }

        public void Activate()
        {
            IsActive = true;
            // Khôi phục cấu hình đã lưu khi kích hoạt lại
            CurrentBrushType = _savedBrushType;
            CurrentPenSize = _savedPenSize;
            CurrentPenColor = _savedPenColor;
            
            SettingsChanged?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine($"✏️ PenTool Activated — Brush={CurrentBrushType}, Size={CurrentPenSize}, Color={CurrentPenColor}");
        }

        public void Deactivate()
        {
            // Lưu cấu hình trước khi tắt
            SaveSettings();
            IsActive = false;
            System.Diagnostics.Debug.WriteLine("✏️ PenTool Deactivated");
        }

        #endregion

        #region Pen Configuration

        /// <summary>Loại bút hiện tại (Normal, Highlighter, Calligraphy, Laser, Shape...).</summary>
        public string CurrentBrushType { get; private set; } = "Normal";

        /// <summary>Kích thước nét bút (px).</summary>
        public int CurrentPenSize { get; private set; } = 5;

        /// <summary>Màu bút hiện tại.</summary>
        public Color CurrentPenColor { get; private set; } = Colors.White;

        // Cấu hình đã lưu (khôi phục khi re-activate)
        private string _savedBrushType = "Normal";
        private int _savedPenSize = 5;
        private Color _savedPenColor = Colors.White;

        /// <summary>Cờ nhận diện Bút dạ quang (Highlighter / Marker / Mask).</summary>
        public bool IsHighlighter => CurrentBrushType == "Highlighter" || CurrentBrushType == "Marker" || CurrentBrushType == "Mask" || CurrentBrushType == "MaskPen";

        /// <summary>Cờ nhận diện Bút thư pháp (Calligraphy / Hoc).</summary>
        public bool IsCalligraphy => CurrentBrushType == "Calligraphy" || CurrentBrushType == "Hoc";

        /// <summary>Cờ nhận diện Bút Laser.</summary>
        public bool IsLaser => CurrentBrushType == "Laser";

        /// <summary>Cờ nhận diện Bút ma thuật nhận dạng hình (Shape Recognition).</summary>
        public bool IsSmartShape => CurrentBrushType == "Shape";

        #endregion

        #region Events

        /// <summary>
        /// Phát ra khi cấu hình bút thay đổi.
        /// Form2 lắng nghe để đồng bộ _touchHandler.SetDrawingProperties(...).
        /// </summary>
        public event EventHandler? SettingsChanged;

        #endregion

        #region Public Methods

        /// <summary>
        /// Cập nhật cấu hình bút và tự động lưu.
        /// </summary>
        public void Configure(string brushType, int penSize, Color penColor)
        {
            CurrentBrushType = brushType;
            CurrentPenSize = penSize;
            CurrentPenColor = penColor;

            // Auto-save
            _savedBrushType = brushType;
            _savedPenSize = penSize;
            _savedPenColor = penColor;

            SettingsChanged?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine($"✏️ PenTool Configured — Brush={brushType}, Size={penSize}, Color={penColor}");
        }

        /// <summary>Lưu cấu hình hiện tại (gọi trước khi Deactivate).</summary>
        public void SaveSettings()
        {
            _savedBrushType = CurrentBrushType;
            _savedPenSize = CurrentPenSize;
            _savedPenColor = CurrentPenColor;
        }

        /// <summary>Khôi phục cấu hình đã lưu.</summary>
        public void RestoreSettings()
        {
            CurrentBrushType = _savedBrushType;
            CurrentPenSize = _savedPenSize;
            CurrentPenColor = _savedPenColor;
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Trả về cấu hình hiện tại dưới dạng tuple.
        /// Tiện cho Form2 lấy nhanh để truyền vào TouchHandler.
        /// </summary>
        public (string BrushType, int Size, Color Color) GetCurrentSettings()
        {
            return (CurrentBrushType, CurrentPenSize, CurrentPenColor);
        }

        /// <summary>
        /// Tạo DrawingAttributes chuẩn của WPF Native Inking tương ứng với cấu hình hiện tại.
        /// MilCore sẽ sử dụng cấu hình này để render phần cứng qua GPU Direct3D.
        /// </summary>
        public DrawingAttributes GetDrawingAttributes(Color? overrideColor = null)
        {
            Color strokeColor = overrideColor ?? CurrentPenColor;
            double size = Math.Max(1.0, CurrentPenSize);

            var da = new DrawingAttributes
            {
                Color = strokeColor,
                Width = size,
                Height = size,
                FitToCurve = true,
                StylusTip = StylusTip.Ellipse,
                IgnorePressure = false
            };

            if (IsHighlighter)
            {
                da.IsHighlighter = true;
                da.StylusTip = StylusTip.Rectangle;
                da.Width = Math.Max(6.0, size * 1.0);
                da.Height = Math.Max(14.0, size * 2.2);
            }
            else if (IsCalligraphy)
            {
                da.StylusTip = StylusTip.Rectangle;
                da.Width = Math.Max(2.0, size * 0.4);
                da.Height = Math.Max(6.0, size * 1.6);
                var matrix = Matrix.Identity;
                matrix.Rotate(30.0);
                da.StylusTipTransform = matrix;
            }
            else if (IsLaser)
            {
                da.Color = Color.FromArgb(220, 255, 40, 40);
                da.Width = Math.Max(4.0, size * 1.2);
                da.Height = Math.Max(4.0, size * 1.2);
            }

            return da;
        }

        /// <summary>
        /// Tạo một Stroke Native từ danh sách tọa độ điểm (Point).
        /// </summary>
        public Stroke CreateStroke(IEnumerable<Point> points, Color? overrideColor = null)
        {
            var stylusPoints = new StylusPointCollection();
            foreach (var pt in points)
            {
                stylusPoints.Add(new StylusPoint(pt.X, pt.Y));
            }
            return new Stroke(stylusPoints, GetDrawingAttributes(overrideColor));
        }

        /// <summary>
        /// Tạo một Stroke Native từ StylusPointCollection có sẵn (bảo lưu lực nhấn stylus).
        /// </summary>
        public Stroke CreateStroke(StylusPointCollection stylusPoints, Color? overrideColor = null)
        {
            return new Stroke(stylusPoints.Clone(), GetDrawingAttributes(overrideColor));
        }

        #endregion
    }
}
