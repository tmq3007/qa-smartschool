using System.Collections.ObjectModel;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Manages the configuration and state of multiple graph functions
    /// </summary>
    public class GraphConfiguration
    {
        /// <summary>
        /// Collection of graph functions to be plotted
        /// </summary>
        public ObservableCollection<GraphFunction> Functions { get; set; } = new();

        /// <summary>
        /// Minimum x-axis value
        /// </summary>
        public double XMin { get; set; } = -10;

        /// <summary>
        /// Maximum x-axis value
        /// </summary>
        public double XMax { get; set; } = 10;

        /// <summary>
        /// Minimum y-axis value
        /// </summary>
        public double YMin { get; set; } = -10;

        /// <summary>
        /// Maximum y-axis value
        /// </summary>
        public double YMax { get; set; } = 10;

        /// <summary>
        /// Step size for generating data points
        /// </summary>
        public double Step { get; set; } = 0.1;

        /// <summary>
        /// 3D Camera distance from origin
        /// </summary>
        public double Camera3DDistance { get; set; } = 25.0;

        /// <summary>
        /// 3D Camera rotation around X axis (degrees)
        /// </summary>
        public double Camera3DRotationX { get; set; } = 0.0;

        /// <summary>
        /// 3D Camera rotation around Y axis (degrees)
        /// </summary>
        public double Camera3DRotationY { get; set; } = -30.0;

        /// <summary>
        /// 3D Camera rotation around Z axis (degrees)
        /// </summary>
        public double Camera3DRotationZ { get; set; } = 0.0;

        /// <summary>
        /// Graph type: "2D" or "3D"
        /// </summary>
        public string GraphType { get; set; } = "2D";

        /// <summary>
        /// Constructor with default configuration
        /// </summary>
        public GraphConfiguration()
        {
            // Empty - uses property initializers
        }

        /// <summary>
        /// Gets the next available graph ID
        /// </summary>
        public int GetNextId()
        {
            if (Functions.Count == 0) return 1;
            int maxId = 0;
            foreach (var func in Functions)
            {
                if (func.Id > maxId) maxId = func.Id;
            }
            return maxId + 1;
        }

        /// <summary>
        /// Adds a new graph function with default parameters
        /// </summary>
        public GraphFunction AddNewGraph(FunctionType type, System.Windows.Media.Color color)
        {
            var newGraph = new GraphFunction
            {
                Id = GetNextId(),
                Type = type,
                Color = color,
                A = 1,
                B = 0,
                C = 1,
                D = 0,
                IsVisible = true
            };
            Functions.Add(newGraph);
            return newGraph;
        }

        /// <summary>
        /// Removes a graph function by ID
        /// </summary>
        public bool RemoveGraph(int id)
        {
            var graph = Functions.FirstOrDefault(f => f.Id == id);
            if (graph != null)
            {
                Functions.Remove(graph);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clears all graphs
        /// </summary>
        public void ClearAll()
        {
            Functions.Clear();
        }
    }
}
