using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Represents a 3D mathematical function surface z = f(x, y)
    /// Implements INotifyPropertyChanged for real-time UI updates
    /// </summary>
    public class Graph3DFunction : INotifyPropertyChanged
    {
        private int _id;
        private Function3DType _type;
        private double _a = 1;
        private double _b = 0;
        private double _c = 1;
        private double _d = 0;
        private double _e = 1;
        private double _f = 0;
        private Color _color;
        private bool _isVisible = true;

        #region Properties

        /// <summary>
        /// Unique identifier for the 3D graph
        /// </summary>
        public int Id
        {
            get => _id;
            set => SetField(ref _id, value);
        }

        /// <summary>
        /// Type of 3D mathematical function
        /// </summary>
        public Function3DType Type
        {
            get => _type;
            set
            {
                if (SetField(ref _type, value))
                {
                    OnParameterChanged();
                }
            }
        }

        /// <summary>
        /// Parameter A - typically amplitude or x-scaling
        /// </summary>
        public double A
        {
            get => _a;
            set
            {
                if (SetField(ref _a, value))
                {
                    OnParameterChanged();
                }
            }
        }

        /// <summary>
        /// Parameter B - typically y-scaling or x-center
        /// </summary>
        public double B
        {
            get => _b;
            set
            {
                if (SetField(ref _b, value))
                {
                    OnParameterChanged();
                }
            }
        }

        /// <summary>
        /// Parameter C - typically frequency or spread
        /// </summary>
        public double C
        {
            get => _c;
            set
            {
                if (SetField(ref _c, value))
                {
                    OnParameterChanged();
                }
            }
        }

        /// <summary>
        /// Parameter D - typically offset or y-center
        /// </summary>
        public double D
        {
            get => _d;
            set
            {
                if (SetField(ref _d, value))
                {
                    OnParameterChanged();
                }
            }
        }

        /// <summary>
        /// Parameter E - additional parameter (used in Gaussian)
        /// </summary>
        public double E
        {
            get => _e;
            set
            {
                if (SetField(ref _e, value))
                {
                    OnParameterChanged();
                }
            }
        }

        /// <summary>
        /// Parameter F - additional parameter
        /// </summary>
        public double F
        {
            get => _f;
            set
            {
                if (SetField(ref _f, value))
                {
                    OnParameterChanged();
                }
            }
        }

        /// <summary>
        /// Color for the surface
        /// </summary>
        public Color Color
        {
            get => _color;
            set => SetField(ref _color, value);
        }

        /// <summary>
        /// Whether the surface is visible
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (SetField(ref _isVisible, value))
                {
                    VisibilityChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Computed property for formula display string (bindable)
        /// </summary>
        public string FormulaString => GetFormulaString();

        #endregion

        #region Events

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? ParameterChanged;
        public event EventHandler? VisibilityChanged;

        #endregion

        #region Methods

        /// <summary>
        /// Calculate z value for given (x, y) coordinates
        /// Returns null if point is outside function domain
        /// </summary>
        public double? CalculateZ(double x, double y)
        {
            try
            {
                switch (Type)
                {
                    case Function3DType.Plane:
                        // z = ax + by + c
                        return A * x + B * y + C;

                    case Function3DType.Paraboloid:
                        // z = a*x² + b*y² + c
                        return A * x * x + B * y * y + C;

                    case Function3DType.Saddle:
                        // z = a*x² - b*y² + c
                        return A * x * x - B * y * y + C;

                    case Function3DType.Sphere:
                        // z = sqrt(r² - x² - y²) where r = a
                        double radiusSq = A * A;
                        double distSq = x * x + y * y;
                        if (distSq > radiusSq)
                            return null; // Outside domain
                        double zValue = Math.Sqrt(radiusSq - distSq);
                        return zValue;

                    case Function3DType.Cone:
                        // z = a * sqrt(x² + y²)
                        return A * Math.Sqrt(x * x + y * y);

                    case Function3DType.SineWave:
                        // z = a * sin(b*x) * sin(c*y) + d
                        return A * Math.Sin(B * x) * Math.Sin(C * y) + D;

                    case Function3DType.Gaussian:
                        // z = a * exp(-((x-b)²/(2*c²) + (y-d)²/(2*e²)))
                        if (Math.Abs(C) < 0.01 || Math.Abs(E) < 0.01)
                            return null; // Avoid division by zero
                        double xTerm = (x - B) * (x - B) / (2 * C * C);
                        double yTerm = (y - D) * (y - D) / (2 * E * E);
                        double expValue = Math.Exp(-(xTerm + yTerm));
                        return A * expValue;

                    case Function3DType.Ripple:
                        // z = a * sin(b * sqrt(x² + y²)) + c
                        double r = Math.Sqrt(x * x + y * y);
                        return A * Math.Sin(B * r) + C;

                    default:
                        return 0;
                }
            }
            catch
            {
                return null; // Return null for calculation errors
            }
        }

        /// <summary>
        /// Get human-readable formula string for display
        /// </summary>
        public string GetFormulaString()
        {
            switch (Type)
            {
                case Function3DType.Plane:
                    return $"z = {A:F2}x + {B:F2}y + {C:F2}";

                case Function3DType.Paraboloid:
                    return $"z = {A:F2}x² + {B:F2}y² + {C:F2}";

                case Function3DType.Saddle:
                    return $"z = {A:F2}x² - {B:F2}y² + {C:F2}";

                case Function3DType.Sphere:
                    return $"z = √({A:F2}² - x² - y²)";

                case Function3DType.Cone:
                    return $"z = {A:F2}√(x² + y²)";

                case Function3DType.SineWave:
                    return $"z = {A:F2}·sin({B:F2}x)·sin({C:F2}y) + {D:F2}";

                case Function3DType.Gaussian:
                    return $"z = {A:F2}·exp(-((x-{B:F2})²/(2·{C:F2}²) + (y-{D:F2})²/(2·{E:F2}²)))";

                case Function3DType.Ripple:
                    return $"z = {A:F2}·sin({B:F2}·√(x² + y²)) + {C:F2}";

                default:
                    return "z = f(x, y)";
            }
        }

        /// <summary>
        /// Generate MeshGeometry3D for this function surface
        /// </summary>
        /// <param name="xMin">Minimum x value</param>
        /// <param name="xMax">Maximum x value</param>
        /// <param name="yMin">Minimum y value</param>
        /// <param name="yMax">Maximum y value</param>
        /// <param name="resolution">Number of divisions per axis (higher = smoother)</param>
        public MeshGeometry3D GenerateMesh(double xMin, double xMax, double yMin, double yMax, int resolution = 50)
        {
            int recommendedResolution = QASmartTouch.Services.AppSettings.GetRecommended3DResolution();
            resolution = Math.Min(resolution, recommendedResolution);

            var appConfig = QASmartClass.Services.AppConfig.Load();
            if (appConfig.Reduce3DMeshResolution || (appConfig.AutoOptimizeForIntegratedGraphics && QASmartClass.Services.AppConfig.IsIntegratedGraphicsDetected))
            {
                resolution = Math.Min(resolution, 20);
            }

            var mesh = new MeshGeometry3D();
            
            double xStep = (xMax - xMin) / resolution;
            double yStep = (yMax - yMin) / resolution;

            // Generate vertices
            for (int i = 0; i <= resolution; i++)
            {
                for (int j = 0; j <= resolution; j++)
                {
                    double x = xMin + i * xStep;
                    double y = yMin + j * yStep;
                    double? z = CalculateZ(x, y);

                    // Use 0 if outside domain (for visualization continuity)
                    mesh.Positions.Add(new Point3D(x, y, z ?? 0));

                    // Simple texture coordinates (0 to 1)
                    mesh.TextureCoordinates.Add(new System.Windows.Point(
                        (double)i / resolution,
                        (double)j / resolution
                    ));
                }
            }

            // Generate triangle indices
            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    int topLeft = i * (resolution + 1) + j;
                    int topRight = topLeft + 1;
                    int bottomLeft = (i + 1) * (resolution + 1) + j;
                    int bottomRight = bottomLeft + 1;

                    // First triangle (top-left, bottom-left, top-right)
                    mesh.TriangleIndices.Add(topLeft);
                    mesh.TriangleIndices.Add(bottomLeft);
                    mesh.TriangleIndices.Add(topRight);

                    // Second triangle (top-right, bottom-left, bottom-right)
                    mesh.TriangleIndices.Add(topRight);
                    mesh.TriangleIndices.Add(bottomLeft);
                    mesh.TriangleIndices.Add(bottomRight);
                }
            }

            return mesh;
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnParameterChanged()
        {
            OnPropertyChanged(nameof(FormulaString)); // Update formula display
            ParameterChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
