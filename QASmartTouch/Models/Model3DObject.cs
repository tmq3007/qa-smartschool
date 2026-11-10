using System;
using System.Windows;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Represents a 3D model object that can be placed on the canvas
    /// </summary>
    public class Model3DObject
    {
        /// <summary>
        /// Full path to the 3D model file
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Name of the file (without path)
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        
        /// <summary>
        /// Position on the canvas
        /// </summary>
        public Point Position { get; set; }
        
        /// <summary>
        /// Size of the 3D viewport control
        /// </summary>
        public Size Size { get; set; }
        
        /// <summary>
        /// Rotation around X axis (degrees)
        /// </summary>
        public double RotationX { get; set; }
        
        /// <summary>
        /// Rotation around Y axis (degrees)
        /// </summary>
        public double RotationY { get; set; }
        
        /// <summary>
        /// Rotation around Z axis (degrees)
        /// </summary>
        public double RotationZ { get; set; }
        
        /// <summary>
        /// Scale factor (1.0 = original size)
        /// </summary>
        public double Scale { get; set; } = 1.0;
        
        /// <summary>
        /// When the model was imported
        /// </summary>
        public DateTime ImportedDate { get; set; }
        
        /// <summary>
        /// Camera distance for the 3D view
        /// </summary>
        public double CameraDistance { get; set; } = 10.0;
        
        /// <summary>
        /// Unique ID for this model instance
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
