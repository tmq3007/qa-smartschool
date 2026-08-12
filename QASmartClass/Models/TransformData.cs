using System.Windows;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Data class chứa thông tin transform operation
    /// </summary>
    public class TransformData
    {
        #region Properties

        /// <summary>
        /// Vị trí gốc trước khi transform
        /// </summary>
        public Point OriginalPosition { get; set; }

        /// <summary>
        /// Kích thước gốc
        /// </summary>
        public Size OriginalSize { get; set; }

        /// <summary>
        /// Góc xoay gốc
        /// </summary>
        public double OriginalRotation { get; set; }

        /// <summary>
        /// Vị trí mới sau transform
        /// </summary>
        public Point NewPosition { get; set; }

        /// <summary>
        /// Kích thước mới
        /// </summary>
        public Size NewSize { get; set; }

        /// <summary>
        /// Góc xoay mới
        /// </summary>
        public double NewRotation { get; set; }

        /// <summary>
        /// Delta X (thay đổi X)
        /// </summary>
        public double DeltaX => NewPosition.X - OriginalPosition.X;

        /// <summary>
        /// Delta Y (thay đổi Y)
        /// </summary>
        public double DeltaY => NewPosition.Y - OriginalPosition.Y;

        /// <summary>
        /// Scale factor X
        /// </summary>
        public double ScaleX => OriginalSize.Width > 0 ? NewSize.Width / OriginalSize.Width : 1.0;

        /// <summary>
        /// Scale factor Y
        /// </summary>
        public double ScaleY => OriginalSize.Height > 0 ? NewSize.Height / OriginalSize.Height : 1.0;

        /// <summary>
        /// Delta rotation
        /// </summary>
        public double DeltaRotation => NewRotation - OriginalRotation;

        #endregion

        #region Constructor

        public TransformData()
        {
            OriginalPosition = new Point(0, 0);
            OriginalSize = new Size(0, 0);
            OriginalRotation = 0;
            NewPosition = new Point(0, 0);
            NewSize = new Size(0, 0);
            NewRotation = 0;
        }

        public TransformData(SelectableObject obj)
        {
            OriginalPosition = obj.Position;
            OriginalSize = obj.Size;
            OriginalRotation = obj.RotationAngle;
            NewPosition = obj.Position;
            NewSize = obj.Size;
            NewRotation = obj.RotationAngle;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Copy dữ liệu từ object
        /// </summary>
        public void CopyFrom(SelectableObject obj)
        {
            OriginalPosition = obj.Position;
            OriginalSize = obj.Size;
            OriginalRotation = obj.RotationAngle;
        }

        /// <summary>
        /// Apply transform lên object
        /// </summary>
        public void ApplyTo(SelectableObject obj)
        {
            obj.Position = NewPosition;
            obj.Size = NewSize;
            obj.RotationAngle = NewRotation;
            obj.UpdateBounds();
            obj.ApplyTransform();
        }

        /// <summary>
        /// Reset về trạng thái gốc
        /// </summary>
        public void Reset()
        {
            NewPosition = OriginalPosition;
            NewSize = OriginalSize;
            NewRotation = OriginalRotation;
        }

        #endregion
    }
}
