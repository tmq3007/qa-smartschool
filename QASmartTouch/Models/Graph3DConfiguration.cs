using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Configuration for 3D graph collection and display settings
    /// </summary>
    public class Graph3DConfiguration
    {
        /// <summary>
        /// Collection of 3D functions to display
        /// </summary>
        public ObservableCollection<Graph3DFunction> Functions { get; set; }

        /// <summary>
        /// X-axis minimum value
        /// </summary>
        public double XMin { get; set; } = -5;

        /// <summary>
        /// X-axis maximum value
        /// </summary>
        public double XMax { get; set; } = 5;

        /// <summary>
        /// Y-axis minimum value
        /// </summary>
        public double YMin { get; set; } = -5;

        /// <summary>
        /// Y-axis maximum value
        /// </summary>
        public double YMax { get; set; } = 5;

        /// <summary>
        /// Mesh resolution (number of divisions per axis)
        /// Higher values = smoother surfaces but slower rendering
        /// </summary>
        public int Resolution { get; set; } = 50;

        public Graph3DConfiguration()
        {
            Functions = new ObservableCollection<Graph3DFunction>();
        }

        /// <summary>
        /// Get next available ID for new function
        /// </summary>
        public int GetNextId()
        {
            if (Functions.Count == 0)
                return 1;
            return Functions.Max(f => f.Id) + 1;
        }

        /// <summary>
        /// Add a new 3D function with specified type and color
        /// </summary>
        public Graph3DFunction AddNewGraph(Function3DType type, Color color)
        {
            var function = new Graph3DFunction
            {
                Id = GetNextId(),
                Type = type,
                Color = color,
                IsVisible = true
            };

            // Set default parameters based on type
            switch (type)
            {
                case Function3DType.Plane:
                    function.A = 0.5; // x-slope
                    function.B = 0.5; // y-slope
                    function.C = 0;   // z-intercept
                    break;

                case Function3DType.Paraboloid:
                    function.A = 0.3; // x-curvature
                    function.B = 0.3; // y-curvature
                    function.C = 0;   // z-offset
                    break;

                case Function3DType.Saddle:
                    function.A = 0.3; // x-curvature
                    function.B = 0.3; // y-curvature
                    function.C = 0;   // z-offset
                    break;

                case Function3DType.Sphere:
                    function.A = 4;   // radius
                    break;

                case Function3DType.Cone:
                    function.A = 0.5; // slope
                    break;

                case Function3DType.SineWave:
                    function.A = 2;   // amplitude
                    function.B = 1;   // x-frequency
                    function.C = 1;   // y-frequency
                    function.D = 0;   // z-offset
                    break;

                case Function3DType.Gaussian:
                    function.A = 5;   // amplitude
                    function.B = 0;   // x-center
                    function.C = 1.5; // x-spread
                    function.D = 0;   // y-center
                    function.E = 1.5; // y-spread
                    break;

                case Function3DType.Ripple:
                    function.A = 2;   // amplitude
                    function.B = 2;   // frequency
                    function.C = 0;   // z-offset
                    break;
            }

            Functions.Add(function);
            return function;
        }

        /// <summary>
        /// Remove function by ID
        /// </summary>
        public void RemoveGraph(int id)
        {
            var function = Functions.FirstOrDefault(f => f.Id == id);
            if (function != null)
            {
                Functions.Remove(function);
            }
        }

        /// <summary>
        /// Clear all functions
        /// </summary>
        public void ClearAll()
        {
            Functions.Clear();
        }
    }
}
