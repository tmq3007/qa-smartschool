using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using HelixToolkit.Wpf;
using QASmartTouch.Models;

namespace QASmartTouch.Forms
{
    public partial class Form2_GraphEditorUnified : Window
    {
        private readonly GraphConfiguration _config;
        private PlotModel _plotModel;
        private string _currentMode = "2D"; // "2D" or "3D"
        
        private readonly List<Color> _colorPalette = new List<Color>
        {
            Colors.Blue,      // #3B82F6
            Colors.Red,       // #EF4444
            Colors.Green,     // #10B981
            Colors.Orange,    // #F59E0B
            Colors.Purple,    // #8B5CF6
            Colors.DeepPink,  // #EC4899
            Colors.Teal,      // #14B8A6
            Colors.OrangeRed  // #F97316
        };
        private int _colorIndex = 0;

        public BitmapImage? ExportedGraphImage { get; private set; }
        public GraphConfiguration? ExportedConfig { get; private set; }

        public Form2_GraphEditorUnified()
        {
            InitializeComponent();

            _config = new GraphConfiguration();
            graphsList.ItemsSource = _config.Functions;

            // Initialize 2D plot
            Initialize2DPlot();

            // Add default graph
            AddNewGraph(FunctionType.Sin, _colorPalette[_colorIndex]);
            _colorIndex = (_colorIndex + 1) % _colorPalette.Count;

            // Subscribe to changes
            _config.Functions.CollectionChanged += (s, e) => UpdateCurrentView();
            
            // Set initial mode button states
            UpdateModeButtons();
            
            // Configure PlotView to maintain equal aspect ratio for X and Y axes
            ConfigurePlotViewAspectRatio();
            
            // Add mouse event handlers for 3D viewport
            viewport3D.MouseDown += Viewport3D_MouseDown;
            viewport3D.MouseMove += Viewport3D_MouseMove;
            viewport3D.MouseUp += Viewport3D_MouseUp;
            viewport3D.MouseWheel += Viewport3D_MouseWheel;

            ApplyGraphicsSettings();
            
            // Handle window closing
            this.Closing += (s, e) => 
            {
                _autoRotateTimer?.Stop();
                _autoRotateTimer = null;
            };
        }
        
        // Constructor for editing existing graph
        public Form2_GraphEditorUnified(object existingConfig)
        {
            InitializeComponent();

            if (existingConfig is GraphConfiguration config)
            {
                _config = config;
            }
            else
            {
                _config = new GraphConfiguration();
            }
            
            graphsList.ItemsSource = _config.Functions;

            // Initialize 2D plot
            Initialize2DPlot();

            // Restore graphs from config
            if (_config.Functions.Count == 0)
            {
                // Add default if empty
                AddNewGraph(FunctionType.Sin, _colorPalette[_colorIndex]);
                _colorIndex = (_colorIndex + 1) % _colorPalette.Count;
            }
            else
            {
                // Update color index based on existing functions
                _colorIndex = _config.Functions.Count % _colorPalette.Count;
                
                // Restore 3D camera angles from saved configuration (before switching mode)
                _camera3DDistance = _config.Camera3DDistance;
                _camera3DRotationX = _config.Camera3DRotationX;
                _camera3DRotationY = _config.Camera3DRotationY;
                _camera3DRotationZ = _config.Camera3DRotationZ;
                
                // Restore graph type and switch to correct tab
                if (!string.IsNullOrEmpty(_config.GraphType))
                {
                    // Use SwitchMode to properly update UI and show correct tab
                    SwitchMode(_config.GraphType);
                    
                    // For 3D mode, ensure camera is updated after switching
                    if (_config.GraphType == "3D")
                    {
                        Update3DCamera();
                    }
                }
                else
                {
                    // Default to 2D if no type specified
                    UpdateCurrentView();
                }
            }

            // Subscribe to changes
            _config.Functions.CollectionChanged += (s, e) => UpdateCurrentView();
            
            // Configure PlotView to maintain equal aspect ratio for X and Y axes
            ConfigurePlotViewAspectRatio();
            
            // Add mouse event handlers for 3D viewport
            viewport3D.MouseDown += Viewport3D_MouseDown;
            viewport3D.MouseMove += Viewport3D_MouseMove;
            viewport3D.MouseUp += Viewport3D_MouseUp;
            viewport3D.MouseWheel += Viewport3D_MouseWheel;

            ApplyGraphicsSettings();
            
            // Handle window closing
            this.Closing += (s, e) => 
            {
                _autoRotateTimer?.Stop();
                _autoRotateTimer = null;
            };
        }

        private void ApplyGraphicsSettings()
        {
            int resolutionSetting = QASmartTouch.Services.AppSettings.GetRecommended3DResolution();
            var config = QASmartClass.Services.AppConfig.Load();
            if (resolutionSetting <= 20 || config.Disable3DAntiAliasing || (config.AutoOptimizeForIntegratedGraphics && QASmartClass.Services.AppConfig.IsIntegratedGraphicsDetected))
            {
                System.Windows.Media.RenderOptions.SetEdgeMode(viewport3D, System.Windows.Media.EdgeMode.Aliased);
            }
            else
            {
                System.Windows.Media.RenderOptions.SetEdgeMode(viewport3D, System.Windows.Media.EdgeMode.Unspecified);
            }
        }

        #region Mode Switching

        private void Btn2DMode_Click(object sender, RoutedEventArgs e)
        {
            SwitchMode("2D");
        }

        private void Btn3DMode_Click(object sender, RoutedEventArgs e)
        {
            SwitchMode("3D");
        }

        private void SwitchMode(string mode)
        {
            _currentMode = mode;
            
            if (mode == "2D")
            {
                plot2DContainer.Visibility = Visibility.Visible;
                plot3DContainer.Visibility = Visibility.Collapsed;
                graphTitle.Text = "📈 Đồ Thị 2D";
                
                btn2DMode.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)); // Blue
                btn3DMode.Background = new SolidColorBrush(Color.FromArgb(51, 255, 255, 255)); // Transparent white
            }
            else
            {
                plot2DContainer.Visibility = Visibility.Collapsed;
                plot3DContainer.Visibility = Visibility.Visible;
                graphTitle.Text = "🎨 Đồ Thị 3D";
                
                btn2DMode.Background = new SolidColorBrush(Color.FromArgb(51, 255, 255, 255));
                btn3DMode.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246)); // Purple
                
                // Hook up camera changed event to sync user interactions
                if (viewport3D != null && viewport3D.Camera != null)
                {
                    viewport3D.CameraChanged -= Viewport3D_CameraChanged; // Remove first to avoid duplicates
                    viewport3D.CameraChanged += Viewport3D_CameraChanged;
                }
            }
            
            UpdateCurrentView();
        }

        private void UpdateModeButtons()
        {
            if (_currentMode == "2D")
            {
                btn2DMode.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                btn3DMode.Background = new SolidColorBrush(Color.FromArgb(51, 255, 255, 255));
            }
            else
            {
                btn2DMode.Background = new SolidColorBrush(Color.FromArgb(51, 255, 255, 255));
                btn3DMode.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246));
            }
        }

        private void UpdateCurrentView()
        {
            if (_currentMode == "2D")
            {
                Update2DPlot();
            }
            else
            {
                Update3DScene();
            }
        }

        private void BtnResetView_Click(object sender, RoutedEventArgs e)
        {
            // Reset 2D plot view to default zoom level
            if (_plotModel != null)
            {
                _plotModel.ResetAllAxes();
                _plotModel.InvalidatePlot(true);
            }
        }
        
        private void ConfigurePlotViewAspectRatio()
        {
            // Ensure X and Y axes maintain 1:1 aspect ratio
            // This prevents distortion where 1 unit on X axis ≠ 1 unit on Y axis visually
            GraphPlotView.SizeChanged += (s, e) =>
            {
                if (_plotModel == null || _plotModel.Axes.Count < 2)
                    return;

                var xAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
                var yAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Left);

                if (xAxis == null || yAxis == null)
                    return;

                // Calculate plot area dimensions
                var plotArea = _plotModel.PlotArea;
                double plotWidth = plotArea.Width;
                double plotHeight = plotArea.Height;

                if (plotWidth <= 0 || plotHeight <= 0)
                    return;

                // Get current axis ranges
                double xRange = Math.Abs(xAxis.ActualMaximum - xAxis.ActualMinimum);
                double yRange = Math.Abs(yAxis.ActualMaximum - yAxis.ActualMinimum);

                if (xRange <= 0 || yRange <= 0)
                    return;

                // Calculate pixels per unit for each axis
                double xPixelsPerUnit = plotWidth / xRange;
                double yPixelsPerUnit = plotHeight / yRange;

                // Adjust zoom to maintain equal scale
                // We want: xPixelsPerUnit == yPixelsPerUnit
                if (Math.Abs(xPixelsPerUnit - yPixelsPerUnit) > 0.1)
                {
                    // Use the smaller scale to ensure both axes fit
                    double targetPixelsPerUnit = Math.Min(xPixelsPerUnit, yPixelsPerUnit);

                    // Calculate new ranges to achieve equal scale
                    double newXRange = plotWidth / targetPixelsPerUnit;
                    double newYRange = plotHeight / targetPixelsPerUnit;

                    // Center the new range around current center
                    double xCenter = (xAxis.ActualMaximum + xAxis.ActualMinimum) / 2;
                    double yCenter = (yAxis.ActualMaximum + yAxis.ActualMinimum) / 2;

                    // Apply new zoom maintaining aspect ratio
                    xAxis.Zoom(xCenter - newXRange / 2, xCenter + newXRange / 2);
                    yAxis.Zoom(yCenter - newYRange / 2, yCenter + newYRange / 2);

                    _plotModel.InvalidatePlot(false);
                }
            };
        }

        #endregion

        #region 2D Graph (OxyPlot)

        private void Initialize2DPlot()
        {
            _plotModel = new PlotModel
            {
                Title = "",
                Background = OxyColors.White,
                PlotAreaBorderColor = OxyColor.FromRgb(200, 200, 200),
                PlotAreaBorderThickness = new OxyThickness(1),
                TitleFontSize = 16,
                TitleFontWeight = 600,
                Padding = new OxyThickness(60, 10, 20, 60), // More space for labels
                
                // Force aspect ratio 1:1 to ensure X and Y axes have equal scale
                PlotAreaBackground = OxyColors.White
            };

            // Enable Legend with cleaner style
            _plotModel.Legends.Add(new OxyPlot.Legends.Legend
            {
                LegendPosition = OxyPlot.Legends.LegendPosition.TopRight,
                LegendPlacement = OxyPlot.Legends.LegendPlacement.Inside,
                LegendBackground = OxyColor.FromAColor(250, OxyColors.White),
                LegendBorder = OxyColor.FromRgb(220, 220, 220),
                LegendBorderThickness = 1,
                LegendPadding = 8,
                LegendMargin = 10,
                LegendFontSize = 11
            });

            // X Axis - Clean mathematical style with centered origin and arrows
            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                // Title removed - using custom TextAnnotation instead
                Title = "",
                TitleFontSize = 28,
                TitleFontWeight = 700,
                TitleColor = OxyColors.Black,
                TitlePosition = 0.98,
                
                // Grid styling - professional hierarchy
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColor.FromRgb(200, 200, 200),  // Darker for major
                MinorGridlineColor = OxyColor.FromRgb(230, 230, 230),  // Lighter for minor
                MajorGridlineThickness = 1.0,
                MinorGridlineThickness = 0.5,
                
                // Axis line with arrow - will add arrow annotation separately
                AxislineColor = OxyColors.Black,
                AxislineThickness = 2.0,  // Thicker for better visibility
                AxislineStyle = LineStyle.Solid,
                
                // Tick styling
                TicklineColor = OxyColors.Black,
                MajorTickSize = 5,
                MinorTickSize = 3,
                
                // Auto-range with zoom support
                MajorStep = 1.0,  // Major gridlines every 1 unit
                MinorStep = 0.5,  // Minor gridlines every 0.5 unit
                
                FontSize = 12,
                FontWeight = 500,
                
                // Position at zero crossing for centered origin
                PositionAtZeroCrossing = true,
                AxisDistance = 0,
                
                // Start and end arrows
                StartPosition = 0,
                EndPosition = 1
            };

            // Y Axis - Clean mathematical style with centered origin and arrows
            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                // Title removed - using custom TextAnnotation instead
                Title = "",
                TitleFontSize = 28,
                TitleFontWeight = 700,
                TitleColor = OxyColors.Black,
                TitlePosition = 0.98,
                
                // Grid styling - professional hierarchy
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColor.FromRgb(200, 200, 200),  // Darker for major
                MinorGridlineColor = OxyColor.FromRgb(230, 230, 230),  // Lighter for minor
                MajorGridlineThickness = 1.0,
                MinorGridlineThickness = 0.5,
                
                // Axis line with arrow - will add arrow annotation separately
                AxislineColor = OxyColors.Black,
                AxislineThickness = 2.0,  // Thicker for better visibility
                AxislineStyle = LineStyle.Solid,
                
                // Tick styling
                TicklineColor = OxyColors.Black,
                MajorTickSize = 5,
                MinorTickSize = 3,
                
                // Auto-range with zoom support
                MajorStep = 1.0,
                MinorStep = 0.5,
                
                FontSize = 12,
                FontWeight = 500,
                
                // Position at zero crossing for centered origin
                PositionAtZeroCrossing = true,
                AxisDistance = 0,
                
                // Start and end arrows
                StartPosition = 0,
                EndPosition = 1
            };

            _plotModel.Axes.Add(xAxis);
            _plotModel.Axes.Add(yAxis);
            
            // Set initial range for default view
            xAxis.Zoom(-5, 5);
            yAxis.Zoom(-5, 5);

            // Add arrow annotations for axis arrows
            AddAxisArrows();
            
            // Update arrows and redraw graph when axis range changes (zoom/pan)
            xAxis.AxisChanged += (s, e) => 
            {
                UpdateAxisArrows();
                Update2DPlot();  // Redraw graph with new range
            };
            yAxis.AxisChanged += (s, e) => 
            {
                UpdateAxisArrows();
                Update2DPlot();  // Redraw graph with new range
            };

            GraphPlotView.Model = _plotModel;
        }
        
        private void UpdateAxisArrows()
        {
            // Remove old arrow annotations and text labels
            var oldArrowsAndLabels = _plotModel.Annotations
                .Where(a => a is OxyPlot.Annotations.ArrowAnnotation || 
                           (a is OxyPlot.Annotations.TextAnnotation text && (text.Text == "x" || text.Text == "y")))
                .ToList();
            foreach (var item in oldArrowsAndLabels)
            {
                _plotModel.Annotations.Remove(item);
            }
            
            // Add new arrows and labels at correct positions
            AddAxisArrows();
            
            // Refresh plot
            _plotModel.InvalidatePlot(false);
        }

        private void AddAxisArrows()
        {
            // Get current axis ranges
            var xAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
            var yAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Left);
            
            if (xAxis == null || yAxis == null) return;
            
            double xMax = xAxis.ActualMaximum != 0 ? xAxis.ActualMaximum : xAxis.Maximum;
            double yMax = yAxis.ActualMaximum != 0 ? yAxis.ActualMaximum : yAxis.Maximum;
            double xMin = xAxis.ActualMinimum != 0 ? xAxis.ActualMinimum : xAxis.Minimum;
            double yMin = yAxis.ActualMinimum != 0 ? yAxis.ActualMinimum : yAxis.Minimum;
            
            // Calculate arrow positions based on current zoom level
            // Arrow occupies ~4% of the visible range
            double xRange = xMax - xMin;
            double yRange = yMax - yMin;
            double arrowLength = Math.Max(xRange * 0.04, 0.1);
            
            // X-axis arrow (pointing right at positive end)
            var xArrow = new OxyPlot.Annotations.ArrowAnnotation
            {
                StartPoint = new DataPoint(xMax - arrowLength, 0),
                EndPoint = new DataPoint(xMax, 0),
                Color = OxyColors.Black,
                StrokeThickness = 2.0,
                HeadLength = 8,
                HeadWidth = 6,
                LineStyle = LineStyle.Solid
            };
            _plotModel.Annotations.Add(xArrow);

            // Y-axis arrow (pointing up at positive end)
            var yArrow = new OxyPlot.Annotations.ArrowAnnotation
            {
                StartPoint = new DataPoint(0, yMax - arrowLength),
                EndPoint = new DataPoint(0, yMax),
                Color = OxyColors.Black,
                StrokeThickness = 2.0,
                HeadLength = 8,
                HeadWidth = 6,
                LineStyle = LineStyle.Solid
            };
            _plotModel.Annotations.Add(yArrow);
            
            // Add x label annotation near arrow (right side, slightly above axis)
            // Position: near the end of arrow, always in positive y region
            var xLabel = new OxyPlot.Annotations.TextAnnotation
            {
                Text = "x",
                TextPosition = new DataPoint(xMax - arrowLength * 0.3, Math.Max(0.15, yRange * 0.05)),
                Font = "Arial",
                FontSize = 24,
                FontWeight = OxyPlot.FontWeights.Bold,
                TextColor = OxyColors.Black,
                Stroke = OxyColors.Transparent,
                StrokeThickness = 0
            };
            _plotModel.Annotations.Add(xLabel);
            
            // Add y label annotation below arrow to avoid being obscured
            // Position: below the arrow end, in positive x region
            var yLabel = new OxyPlot.Annotations.TextAnnotation
            {
                Text = "y",
                TextPosition = new DataPoint(Math.Max(0.15, xRange * 0.04), yMax - arrowLength * 1.5),
                Font = "Arial",
                FontSize = 24,
                FontWeight = OxyPlot.FontWeights.Bold,
                TextColor = OxyColors.Black,
                Stroke = OxyColors.Transparent,
                StrokeThickness = 0
            };
            _plotModel.Annotations.Add(yLabel);
        }

        private void Update2DPlot()
        {
            _plotModel.Series.Clear();
            
            // Clear all annotations except axis arrows and labels
            var nonArrowAnnotations = _plotModel.Annotations
                .Where(a => !(a is OxyPlot.Annotations.ArrowAnnotation arrow && arrow.HeadLength == 8) &&
                           !(a is OxyPlot.Annotations.TextAnnotation text && (text.Text == "x" || text.Text == "y")))
                .ToList();
            
            foreach (var ann in nonArrowAnnotations)
            {
                _plotModel.Annotations.Remove(ann);
            }
            
            // Ensure axis arrows and labels are present
            UpdateAxisArrows();

            foreach (var function in _config.Functions.Where(f => f.IsVisible))
            {
                var lineColor = OxyColor.FromRgb(function.Color.R, function.Color.G, function.Color.B);
                
                var series = new LineSeries
                {
                    Title = GetShortFormula(function),
                    Color = lineColor,
                    StrokeThickness = 2.0,  // Professional thickness like reference image
                    LineStyle = LineStyle.Solid,
                    // Enable tracker (tooltip)
                    TrackerFormatString = $"{GetShortFormula(function)}\n{{0}}: {{2:F2}}\n{{1}}: {{4:F2}}",
                    CanTrackerInterpolatePoints = true,
                    MarkerType = MarkerType.None  // No markers on line by default
                };

                // Use axis range for dynamic zoom support
                var xAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom) as LinearAxis;
                var yAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Left) as LinearAxis;
                
                // Get effective range (use ActualMinimum/Maximum which reflects current zoom state)
                double xStart = xAxis?.ActualMinimum ?? -5.0;
                double xEnd = xAxis?.ActualMaximum ?? 5.0;
                double yMin = yAxis?.ActualMinimum ?? -5.0;
                double yMax = yAxis?.ActualMaximum ?? 5.0;
                
                // If ActualMinimum/Maximum are not set yet, use default range
                if (double.IsNaN(xStart) || double.IsInfinity(xStart)) xStart = -5.0;
                if (double.IsNaN(xEnd) || double.IsInfinity(xEnd)) xEnd = 5.0;
                if (double.IsNaN(yMin) || double.IsInfinity(yMin)) yMin = -5.0;
                if (double.IsNaN(yMax) || double.IsInfinity(yMax)) yMax = 5.0;
                
                double step = (xEnd - xStart) / 200.0;  // 200 points for smooth curve

                // Adjust range for special functions
                if (function.Type == FunctionType.Exponential)
                {
                    // Keep calculated range
                }
                else if (function.Type == FunctionType.Logarithm)
                {
                    xStart = Math.Max(0.1, xStart);  // Logarithm requires positive x
                }

                List<DataPoint> intersectionPoints = new List<DataPoint>();

                for (double x = xStart; x <= xEnd; x += step)
                {
                    double? y = function.CalculateY(x);
                    
                    if (y.HasValue && !double.IsNaN(y.Value) && !double.IsInfinity(y.Value))
                    {
                        // No clamping - let OxyPlot handle range
                        series.Points.Add(new DataPoint(x, y.Value));
                        
                        // Mark axis intersections (within small threshold)
                        if (Math.Abs(y.Value) < 0.05) // Y-axis intersection (x-intercept)
                        {
                            intersectionPoints.Add(new DataPoint(x, 0));
                        }
                        if (Math.Abs(x) < 0.05) // X-axis intersection (y-intercept)
                        {
                            intersectionPoints.Add(new DataPoint(0, y.Value));
                        }
                    }
                    else if (series.Points.Count > 0)
                    {
                        series.Points.Add(DataPoint.Undefined);
                    }
                }

                _plotModel.Series.Add(series);

                // Add markers for intersection points
                if (intersectionPoints.Count > 0)
                {
                    var pointSeries = new ScatterSeries
                    {
                        MarkerType = MarkerType.Circle,
                        MarkerSize = 5,
                        MarkerFill = lineColor,
                        MarkerStroke = OxyColors.Black,
                        MarkerStrokeThickness = 1.0
                    };
                    
                    foreach (var point in intersectionPoints.Take(2)) // Limit to 2 main intersections
                    {
                        pointSeries.Points.Add(new ScatterPoint(point.X, point.Y));
                    }
                    
                    _plotModel.Series.Add(pointSeries);
                }

                // Add equation annotation on the line (like reference image)
                if (series.Points.Count > series.Points.Count / 2)
                {
                    var midPoint = series.Points[series.Points.Count / 2];
                    if (midPoint.X >= xStart && midPoint.X <= xEnd)
                    {
                        var annotation = new OxyPlot.Annotations.TextAnnotation
                        {
                            Text = GetShortFormula(function),
                            TextPosition = new DataPoint(midPoint.X, midPoint.Y),
                            TextColor = lineColor,
                            FontSize = 11,
                            FontWeight = 600,
                            Background = OxyColor.FromAColor(220, OxyColors.White),
                            Padding = new OxyThickness(4, 2, 4, 2),
                            TextRotation = -30,  // Slanted like the line in reference image
                            Offset = new OxyPlot.ScreenVector(0, -15),
                            StrokeThickness = 0
                        };
                        _plotModel.Annotations.Add(annotation);
                    }
                }
            }

            _plotModel.InvalidatePlot(true);
            GraphPlotView.InvalidatePlot(true);
            
            System.Diagnostics.Debug.WriteLine($"Updated 2D plot with {_config.Functions.Count(f => f.IsVisible)} visible graphs");
        }

        #endregion

        #region 3D Graph (HelixToolkit)

        private double _camera3DDistance = 25.0;
        private double _camera3DRotationX = 0.0;
        private double _camera3DRotationY = -30.0;
        private double _camera3DRotationZ = 0.0;
        
        // Mouse interaction
        private bool _isMouseDragging = false;
        private bool _isUpdatingFromCamera = false; // Prevent infinite loop between camera and slider updates
        
        // Auto-rotate functionality
        private System.Windows.Threading.DispatcherTimer? _autoRotateTimer;
        private bool _isControlPanelVisible = true; // Track control panel visibility state
        private bool _isAutoRotating = false;
        private double _autoRotateSpeedX = 1.0; // degrees per tick
        private double _autoRotateSpeedY = 0.0;
        private double _autoRotateSpeedZ = 0.0;
        private System.Windows.Point _lastMousePosition;

        private void Update3DScene()
        {
            modelContainer.Children.Clear();

            // Add grid plane for reference
            CreateGridPlane();
            
            // Add bounding box
            CreateBoundingBox();
            
            // Add 3D coordinate axes
            Create3DCoordinateAxes();

            int visibleIndex = 0;
            foreach (var function in _config.Functions.Where(f => f.IsVisible))
            {
                try
                {
                    Create3DSurface(function, visibleIndex * 0.5);
                    visibleIndex++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating 3D surface for graph #{function.Id}: {ex.Message}");
                }
            }

            Update3DCamera();
            System.Diagnostics.Debug.WriteLine($"Updated 3D scene with {visibleIndex} visible surfaces");
        }

        private void Update3DCamera()
        {
            if (viewport3D.Camera is PerspectiveCamera camera)
            {
                // Calculate camera position based on rotation angles
                double angleXRad = _camera3DRotationX * Math.PI / 180.0;
                double angleYRad = _camera3DRotationY * Math.PI / 180.0;
                double angleZRad = _camera3DRotationZ * Math.PI / 180.0;

                // Spherical coordinates to Cartesian
                double x = _camera3DDistance * Math.Cos(angleYRad) * Math.Sin(angleXRad);
                double y = _camera3DDistance * Math.Sin(angleYRad);
                double z = _camera3DDistance * Math.Cos(angleYRad) * Math.Cos(angleXRad);

                camera.Position = new System.Windows.Media.Media3D.Point3D(x, y, z);
                camera.LookDirection = new System.Windows.Media.Media3D.Vector3D(-x, -y, -z);
                
                // Apply Z rotation to up direction
                camera.UpDirection = new System.Windows.Media.Media3D.Vector3D(
                    Math.Sin(angleZRad),
                    Math.Cos(angleZRad),
                    0
                );
            }
            
            // Save current camera state to configuration (if config exists)
            if (_config != null)
            {
                _config.Camera3DDistance = _camera3DDistance;
                _config.Camera3DRotationX = _camera3DRotationX;
                _config.Camera3DRotationY = _camera3DRotationY;
                _config.Camera3DRotationZ = _camera3DRotationZ;
            }
        }

        private void CreateGridPlane()
        {
            double gridSize = 10.0;
            int gridLines = 20; // 20 lines = 21 divisions
            double step = (gridSize * 2) / gridLines;

            // Grid lines parallel to X axis (with layered appearance)
            for (int i = 0; i <= gridLines; i++)
            {
                double yPos = -gridSize + (i * step);
                bool isMajorLine = (i % 5 == 0 && i != 10); // Every 5th line (except center)
                
                var lineX = new LinesVisual3D
                {
                    Color = isMajorLine 
                        ? Color.FromArgb(50, 180, 180, 180)  // Major lines slightly darker
                        : Color.FromArgb(35, 200, 200, 200), // Minor lines lighter
                    Thickness = isMajorLine ? 0.6 : 0.25  // Major: 0.6, Minor: 0.25 (professional hierarchy)
                };
                lineX.Points.Add(new System.Windows.Media.Media3D.Point3D(-gridSize, yPos, 0));
                lineX.Points.Add(new System.Windows.Media.Media3D.Point3D(gridSize, yPos, 0));
                modelContainer.Children.Add(lineX);
            }

            // Grid lines parallel to Y axis (with layered appearance)
            for (int i = 0; i <= gridLines; i++)
            {
                double xPos = -gridSize + (i * step);
                bool isMajorLine = (i % 5 == 0 && i != 10); // Every 5th line (except center)
                
                var lineY = new LinesVisual3D
                {
                    Color = isMajorLine 
                        ? Color.FromArgb(50, 180, 180, 180)  // Major lines slightly darker
                        : Color.FromArgb(35, 200, 200, 200), // Minor lines lighter
                    Thickness = isMajorLine ? 0.6 : 0.25  // Major: 0.6, Minor: 0.25 (professional hierarchy)
                };
                lineY.Points.Add(new System.Windows.Media.Media3D.Point3D(xPos, -gridSize, 0));
                lineY.Points.Add(new System.Windows.Media.Media3D.Point3D(xPos, gridSize, 0));
                modelContainer.Children.Add(lineY);
            }

            // Make center lines slightly darker for emphasis (axes intersect here)
            var centerLineX = new LinesVisual3D
            {
                Color = Color.FromArgb(70, 130, 130, 130),  // Slightly darker for emphasis
                Thickness = 0.8  // Thicker to emphasize coordinate plane
            };
            centerLineX.Points.Add(new System.Windows.Media.Media3D.Point3D(-gridSize, 0, 0));
            centerLineX.Points.Add(new System.Windows.Media.Media3D.Point3D(gridSize, 0, 0));
            modelContainer.Children.Add(centerLineX);

            var centerLineY = new LinesVisual3D
            {
                Color = Color.FromArgb(70, 130, 130, 130),  // Slightly darker for emphasis
                Thickness = 0.8  // Thicker to emphasize coordinate plane
            };
            centerLineY.Points.Add(new System.Windows.Media.Media3D.Point3D(0, -gridSize, 0));
            centerLineY.Points.Add(new System.Windows.Media.Media3D.Point3D(0, gridSize, 0));
            modelContainer.Children.Add(centerLineY);
        }

        private void CreateBoundingBox()
        {
            // Create perfect cube with equal dimensions on all axes
            double size = 10.0;
            double xMin = -size, xMax = size;
            double yMin = -size, yMax = size;
            double zMin = -size, zMax = size;

            var boxColor = Color.FromArgb(40, 190, 190, 190); // Light gray, subtle outline
            double thickness = 0.3;  // Very thin for professional appearance

            // Bottom rectangle (z = zMin)
            var bottomLines = new LinesVisual3D { Color = boxColor, Thickness = thickness };
            bottomLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMin, yMin, zMin));
            bottomLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMax, yMin, zMin));
            bottomLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMax, yMin, zMin));
            bottomLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMax, yMax, zMin));
            bottomLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMax, yMax, zMin));
            bottomLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMin, yMax, zMin));
            bottomLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMin, yMax, zMin));
            bottomLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMin, yMin, zMin));
            modelContainer.Children.Add(bottomLines);

            // Top rectangle (z = zMax)
            var topLines = new LinesVisual3D { Color = boxColor, Thickness = thickness };
            topLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMin, yMin, zMax));
            topLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMax, yMin, zMax));
            topLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMax, yMin, zMax));
            topLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMax, yMax, zMax));
            topLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMax, yMax, zMax));
            topLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMin, yMax, zMax));
            topLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMin, yMax, zMax));
            topLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMin, yMin, zMax));
            modelContainer.Children.Add(topLines);

            // Vertical edges connecting bottom to top
            var verticalLines = new LinesVisual3D { Color = boxColor, Thickness = thickness };
            verticalLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMin, yMin, zMin));
            verticalLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMin, yMin, zMax));
            verticalLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMax, yMin, zMin));
            verticalLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMax, yMin, zMax));
            verticalLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMax, yMax, zMin));
            verticalLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMax, yMax, zMax));
            verticalLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMin, yMax, zMin));
            verticalLines.Points.Add(new System.Windows.Media.Media3D.Point3D(xMin, yMax, zMax));
            modelContainer.Children.Add(verticalLines);
        }

        private void Create3DCoordinateAxes()
        {
            double axisLength = 12.0;  // Reduced from 15.0 (20% smaller)
            double arrowDiameter = 0.12;  // Thicker axes for better visibility and hierarchy
            double arrowLength = 0.6;  // Reduced from 1.0 (40% smaller)

            // X Axis (Red)
            var xAxis = new ArrowVisual3D
            {
                Point1 = new System.Windows.Media.Media3D.Point3D(0, 0, 0),
                Point2 = new System.Windows.Media.Media3D.Point3D(axisLength, 0, 0),
                Diameter = arrowDiameter,
                Fill = new SolidColorBrush(Colors.Red),
                HeadLength = arrowLength,
                ThetaDiv = 12
            };
            modelContainer.Children.Add(xAxis);

            // X Axis Label
            var xLabel = new TextVisual3D
            {
                Text = "X",
                Position = new System.Windows.Media.Media3D.Point3D(axisLength + 1, 0, 0),
                Height = 1.0,  // Reduced from 1.5 (33% smaller)
                Foreground = new SolidColorBrush(Colors.Red),
                FontWeight = System.Windows.FontWeights.Bold
            };
            modelContainer.Children.Add(xLabel);

            // Y Axis (Green)
            var yAxis = new ArrowVisual3D
            {
                Point1 = new System.Windows.Media.Media3D.Point3D(0, 0, 0),
                Point2 = new System.Windows.Media.Media3D.Point3D(0, axisLength, 0),
                Diameter = arrowDiameter,
                Fill = new SolidColorBrush(Colors.Green),
                HeadLength = arrowLength,
                ThetaDiv = 12
            };
            modelContainer.Children.Add(yAxis);

            // Y Axis Label
            var yLabel = new TextVisual3D
            {
                Text = "Y",
                Position = new System.Windows.Media.Media3D.Point3D(0, axisLength + 1, 0),
                Height = 1.0,  // Reduced from 1.5
                Foreground = new SolidColorBrush(Colors.Green),
                FontWeight = System.Windows.FontWeights.Bold
            };
            modelContainer.Children.Add(yLabel);

            // Z Axis (Blue)
            var zAxis = new ArrowVisual3D
            {
                Point1 = new System.Windows.Media.Media3D.Point3D(0, 0, 0),
                Point2 = new System.Windows.Media.Media3D.Point3D(0, 0, axisLength),
                Diameter = arrowDiameter,
                Fill = new SolidColorBrush(Colors.Blue),
                HeadLength = arrowLength,
                ThetaDiv = 12
            };
            modelContainer.Children.Add(zAxis);

            // Z Axis Label
            var zLabel = new TextVisual3D
            {
                Text = "Z",
                Position = new System.Windows.Media.Media3D.Point3D(0, 0, axisLength + 1),
                Height = 1.0,  // Reduced from 1.5
                Foreground = new SolidColorBrush(Colors.Blue),
                FontWeight = System.Windows.FontWeights.Bold
            };
            modelContainer.Children.Add(zLabel);

            // Add axis lines in negative directions (thinner, no arrows, more transparent)
            var xNegativeLine = new PipeVisual3D
            {
                Point1 = new System.Windows.Media.Media3D.Point3D(0, 0, 0),
                Point2 = new System.Windows.Media.Media3D.Point3D(-axisLength * 0.5, 0, 0),
                Diameter = arrowDiameter * 0.5,
                Fill = new SolidColorBrush(Color.FromArgb(76, 255, 0, 0))  // 30% opacity (was 128=50%)
            };
            modelContainer.Children.Add(xNegativeLine);

            var yNegativeLine = new PipeVisual3D
            {
                Point1 = new System.Windows.Media.Media3D.Point3D(0, 0, 0),
                Point2 = new System.Windows.Media.Media3D.Point3D(0, -axisLength * 0.5, 0),
                Diameter = arrowDiameter * 0.5,
                Fill = new SolidColorBrush(Color.FromArgb(76, 0, 255, 0))  // 30% opacity
            };
            modelContainer.Children.Add(yNegativeLine);

            var zNegativeLine = new PipeVisual3D
            {
                Point1 = new System.Windows.Media.Media3D.Point3D(0, 0, 0),
                Point2 = new System.Windows.Media.Media3D.Point3D(0, 0, -axisLength * 0.5),
                Diameter = arrowDiameter * 0.5,
                Fill = new SolidColorBrush(Color.FromArgb(76, 0, 0, 255))  // 30% opacity
            };
            modelContainer.Children.Add(zNegativeLine);

            // Add origin marker (small sphere, subtle gray)
            var origin = new SphereVisual3D
            {
                Center = new System.Windows.Media.Media3D.Point3D(0, 0, 0),
                Radius = 0.15,  // Reduced from 0.3 (50% smaller)
                Fill = new SolidColorBrush(Color.FromRgb(80, 80, 80))  // Dark gray instead of black
            };
            modelContainer.Children.Add(origin);

            // Add axis tick marks and numeric labels
            AddAxisTicksAndLabels(axisLength, arrowDiameter);
        }

        private void AddAxisTicksAndLabels(double axisLength, double arrowDiameter)
        {
            // Tick positions: -10, -5, 0, 5, 10
            double[] tickPositions = { -10, -5, 0, 5, 10 };
            double minorTickLength = 0.3;  // Small ticks
            double majorTickLength = 0.5;  // Larger ticks for multiples of 5

            foreach (double pos in tickPositions)
            {
                if (pos == 0) continue; // Skip origin (already marked)
                
                bool isMajor = Math.Abs(pos % 5) < 0.01; // Major tick at multiples of 5
                double tickLen = isMajor ? majorTickLength : minorTickLength;
                double tickThickness = isMajor ? 0.08 : 0.05;

                // X axis ticks (perpendicular to X, in Y direction)
                var xTick = new LinesVisual3D
                {
                    Color = Color.FromArgb(150, 255, 0, 0), // Semi-transparent red
                    Thickness = tickThickness
                };
                xTick.Points.Add(new System.Windows.Media.Media3D.Point3D(pos, -tickLen, 0));
                xTick.Points.Add(new System.Windows.Media.Media3D.Point3D(pos, tickLen, 0));
                modelContainer.Children.Add(xTick);

                // X axis label
                var xLabel = new TextVisual3D
                {
                    Text = pos.ToString("0"),
                    Position = new System.Windows.Media.Media3D.Point3D(pos, -1.5, 0),
                    Height = 0.6,
                    Foreground = new SolidColorBrush(Color.FromArgb(150, 255, 0, 0)),
                    FontWeight = System.Windows.FontWeights.Normal
                };
                modelContainer.Children.Add(xLabel);

                // Y axis ticks (perpendicular to Y, in X direction)
                var yTick = new LinesVisual3D
                {
                    Color = Color.FromArgb(150, 0, 255, 0), // Semi-transparent green
                    Thickness = tickThickness
                };
                yTick.Points.Add(new System.Windows.Media.Media3D.Point3D(-tickLen, pos, 0));
                yTick.Points.Add(new System.Windows.Media.Media3D.Point3D(tickLen, pos, 0));
                modelContainer.Children.Add(yTick);

                // Y axis label
                var yLabel = new TextVisual3D
                {
                    Text = pos.ToString("0"),
                    Position = new System.Windows.Media.Media3D.Point3D(-1.5, pos, 0),
                    Height = 0.6,
                    Foreground = new SolidColorBrush(Color.FromArgb(150, 0, 255, 0)),
                    FontWeight = System.Windows.FontWeights.Normal
                };
                modelContainer.Children.Add(yLabel);

                // Z axis ticks (perpendicular to Z, in X direction)
                // Only for positions within Z range (-5 to 5)
                if (pos >= -5 && pos <= 5)
                {
                    var zTick = new LinesVisual3D
                    {
                        Color = Color.FromArgb(150, 0, 0, 255), // Semi-transparent blue
                        Thickness = tickThickness
                    };
                    zTick.Points.Add(new System.Windows.Media.Media3D.Point3D(-tickLen, 0, pos));
                    zTick.Points.Add(new System.Windows.Media.Media3D.Point3D(tickLen, 0, pos));
                    modelContainer.Children.Add(zTick);

                    // Z axis label
                    var zLabel = new TextVisual3D
                    {
                        Text = pos.ToString("0"),
                        Position = new System.Windows.Media.Media3D.Point3D(-1.5, 0, pos),
                        Height = 0.6,
                        Foreground = new SolidColorBrush(Color.FromArgb(150, 0, 0, 255)),
                        FontWeight = System.Windows.FontWeights.Normal
                    };
                    modelContainer.Children.Add(zLabel);
                }
            }

            // Add "0" label at origin
            var originLabel = new TextVisual3D
            {
                Text = "0",
                Position = new System.Windows.Media.Media3D.Point3D(-1.2, -1.2, 0),
                Height = 0.6,
                Foreground = new SolidColorBrush(Color.FromArgb(120, 100, 100, 100)),
                FontWeight = System.Windows.FontWeights.Normal
            };
            modelContainer.Children.Add(originLabel);
        }

        private void Create3DSurface(GraphFunction function, double zOffset)
        {
            var mesh = new MeshGeometry3D();

            double xMin = -10, xMax = 10;
            double yMin = -10, yMax = 10;
            double xStep = 0.01;  // Fixed step size for smooth curve
            
            int resolutionSetting = QASmartTouch.Services.AppSettings.GetRecommended3DResolution();
            if (resolutionSetting <= 20)
            {
                xStep = 0.1; // 200 points
            }
            else if (resolutionSetting <= 35)
            {
                xStep = 0.05; // 400 points
            }
            else
            {
                xStep = 0.01; // 2000 points
            }

            var appConfig = QASmartClass.Services.AppConfig.Load();
            if (appConfig.Reduce3DMeshResolution || (appConfig.AutoOptimizeForIntegratedGraphics && QASmartClass.Services.AppConfig.IsIntegratedGraphicsDetected))
            {
                xStep = Math.Max(xStep, 0.05);
            }
            
            int resolution = (int)((xMax - xMin) / xStep);  // Calculate number of steps

            // Generate vertices - RIBBON/CURTAIN METHOD (correct for 1-variable function)
            // Creates a "curtain" surface from the curve down to the XY plane
            for (int i = 0; i <= resolution; i++)
            {
                double x = xMin + i * xStep;
                
                // Calculate Z value for this X position
                double? z = function.CalculateY(x);
                double zValue = z.HasValue && !double.IsNaN(z.Value) && !double.IsInfinity(z.Value) 
                    ? Math.Max(-10, Math.Min(10, z.Value)) 
                    : 0;
                
                // Create two vertices: one at the curve, one at the bottom
                // Top vertex (on the curve)
                mesh.Positions.Add(new System.Windows.Media.Media3D.Point3D(x, yMin, zValue + zOffset));
                mesh.TextureCoordinates.Add(new System.Windows.Point((double)i / resolution, 0));
                
                // Bottom vertex (on the curve)
                mesh.Positions.Add(new System.Windows.Media.Media3D.Point3D(x, yMax, zValue + zOffset));
                mesh.TextureCoordinates.Add(new System.Windows.Point((double)i / resolution, 1));
            }

            // Generate triangle indices for ribbon surface
            for (int i = 0; i < resolution; i++)
            {
                int topLeft = i * 2;
                int bottomLeft = i * 2 + 1;
                int topRight = (i + 1) * 2;
                int bottomRight = (i + 1) * 2 + 1;

                // First triangle
                mesh.TriangleIndices.Add(topLeft);
                mesh.TriangleIndices.Add(bottomLeft);
                mesh.TriangleIndices.Add(topRight);

                // Second triangle
                mesh.TriangleIndices.Add(topRight);
                mesh.TriangleIndices.Add(bottomLeft);
                mesh.TriangleIndices.Add(bottomRight);
            }

            // Create material group with diffuse and specular for better appearance
            var materialGroup = new MaterialGroup();
            
            // Diffuse material - main color
            var diffuseBrush = new SolidColorBrush(function.Color) { Opacity = 0.75 };
            materialGroup.Children.Add(new DiffuseMaterial(diffuseBrush));
            
            // Specular material - shiny highlights
            var specularBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
            materialGroup.Children.Add(new SpecularMaterial(specularBrush, 40));
            
            // Emissive material - slight glow for better visibility
            var emissiveBrush = new SolidColorBrush(Color.FromArgb(20, function.Color.R, function.Color.G, function.Color.B));
            materialGroup.Children.Add(new EmissiveMaterial(emissiveBrush));

            var geometryModel = new GeometryModel3D
            {
                Geometry = mesh,
                Material = materialGroup,
                BackMaterial = materialGroup
            };

            var modelVisual = new ModelVisual3D();
            modelVisual.Content = geometryModel;
            modelContainer.Children.Add(modelVisual);
        }

        #endregion

        #region 3D Controls

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (zoomSlider.Value < zoomSlider.Maximum)
            {
                zoomSlider.Value = Math.Min(zoomSlider.Maximum, zoomSlider.Value + 0.2);
            }
        }

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (zoomSlider.Value > zoomSlider.Minimum)
            {
                zoomSlider.Value = Math.Max(zoomSlider.Minimum, zoomSlider.Value - 0.2);
            }
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (viewport3D != null && !_isUpdatingFromCamera)
            {
                _camera3DDistance = 25.0 / e.NewValue; // Inverse: higher value = closer camera
                Update3DCamera();
            }
        }

        private void BtnRotateLeft_Click(object sender, RoutedEventArgs e)
        {
            rotationXSlider.Value = Math.Max(rotationXSlider.Minimum, rotationXSlider.Value - 15);
        }

        private void BtnRotateRight_Click(object sender, RoutedEventArgs e)
        {
            rotationXSlider.Value = Math.Min(rotationXSlider.Maximum, rotationXSlider.Value + 15);
        }

        private void RotationXSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (viewport3D != null && !_isUpdatingFromCamera)
            {
                _camera3DRotationX = e.NewValue;
                Update3DCamera();
            }
        }

        private void BtnRotateUp_Click(object sender, RoutedEventArgs e)
        {
            rotationYSlider.Value = Math.Min(rotationYSlider.Maximum, rotationYSlider.Value + 15);
        }

        private void BtnRotateDown_Click(object sender, RoutedEventArgs e)
        {
            rotationYSlider.Value = Math.Max(rotationYSlider.Minimum, rotationYSlider.Value - 15);
        }

        private void RotationYSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (viewport3D != null && !_isUpdatingFromCamera)
            {
                _camera3DRotationY = e.NewValue;
                Update3DCamera();
            }
        }

        private void BtnRotateClockwise_Click(object sender, RoutedEventArgs e)
        {
            rotationZSlider.Value = Math.Min(rotationZSlider.Maximum, rotationZSlider.Value + 15);
        }

        private void BtnRotateCounterClockwise_Click(object sender, RoutedEventArgs e)
        {
            rotationZSlider.Value = Math.Max(rotationZSlider.Minimum, rotationZSlider.Value - 15);
        }

        private void RotationZSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (viewport3D != null && !_isUpdatingFromCamera)
            {
                _camera3DRotationZ = e.NewValue;
                Update3DCamera();
            }
        }

        private void BtnReset3DView_Click(object sender, RoutedEventArgs e)
        {
            // Stop auto-rotate if running
            if (_isAutoRotating)
            {
                StopAutoRotate();
                // Update button appearance
                btnToggleAutoRotate.Content = "▶️ Tự Động Xoay";
                btnToggleAutoRotate.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            }
            
            // Reset all controls to default values
            zoomSlider.Value = 1.0;
            rotationXSlider.Value = 0.0;
            rotationYSlider.Value = -30.0;
            rotationZSlider.Value = 0.0;
            
            _camera3DDistance = 25.0;
            _camera3DRotationX = 0.0;
            _camera3DRotationY = -30.0;
            _camera3DRotationZ = 0.0;
            
            Update3DCamera();
            
            System.Diagnostics.Debug.WriteLine("Reset 3D view to default");
        }

        #endregion
        
        #region Auto-Rotate Controls
        
        private void BtnToggleAutoRotate_Click(object sender, RoutedEventArgs e)
        {
            ToggleAutoRotate(sender as Button);
        }
        
        private void ToggleAutoRotate(Button? button)
        {
            _isAutoRotating = !_isAutoRotating;
            
            if (_isAutoRotating)
            {
                // Start timer
                if (_autoRotateTimer == null)
                {
                    _autoRotateTimer = new System.Windows.Threading.DispatcherTimer();
                    _autoRotateTimer.Interval = TimeSpan.FromMilliseconds(90); // Reduced from 30ms to 90ms (1/3 speed)
                    _autoRotateTimer.Tick += AutoRotateTimer_Tick;
                }
                _autoRotateTimer.Start();
                
                // Update button appearance
                if (button != null)
                {
                    button.Content = "⏸️ Dừng Xoay";
                    button.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                }
                
                System.Diagnostics.Debug.WriteLine("Auto-rotate started");
            }
            else
            {
                StopAutoRotate();
                
                // Update button appearance
                if (button != null)
                {
                    button.Content = "▶️ Tự Động Xoay";
                    button.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green
                }
                
                System.Diagnostics.Debug.WriteLine("Auto-rotate stopped");
            }
        }
        
        private void StopAutoRotate()
        {
            _autoRotateTimer?.Stop();
            _isAutoRotating = false;
        }
        
        private void AutoRotateTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isAutoRotating) return;
            
            // Apply rotation increments
            _camera3DRotationX += _autoRotateSpeedX;
            _camera3DRotationY += _autoRotateSpeedY;
            _camera3DRotationZ += _autoRotateSpeedZ;
            
            // Wrap angles to -180...180
            _camera3DRotationX = NormalizeAngle(_camera3DRotationX);
            _camera3DRotationY = NormalizeAngle(_camera3DRotationY);
            _camera3DRotationZ = NormalizeAngle(_camera3DRotationZ);
            
            // Update sliders (will trigger camera update)
            rotationXSlider.Value = _camera3DRotationX;
            rotationYSlider.Value = _camera3DRotationY;
            rotationZSlider.Value = _camera3DRotationZ;
        }
        
        private double NormalizeAngle(double angle)
        {
            while (angle > 180) angle -= 360;
            while (angle < -180) angle += 360;
            return angle;
        }
        
        private void AutoRotateSpeedXSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _autoRotateSpeedX = e.NewValue;
            System.Diagnostics.Debug.WriteLine($"Auto-rotate speed X: {_autoRotateSpeedX}°/frame");
        }
        
        private void AutoRotateSpeedYSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _autoRotateSpeedY = e.NewValue;
            System.Diagnostics.Debug.WriteLine($"Auto-rotate speed Y: {_autoRotateSpeedY}°/frame");
        }
        
        private void AutoRotateSpeedZSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _autoRotateSpeedZ = e.NewValue;
            System.Diagnostics.Debug.WriteLine($"Auto-rotate speed Z: {_autoRotateSpeedZ}°/frame");
        }
        
        private void BtnToggleControlPanel_Click(object sender, RoutedEventArgs e)
        {
            _isControlPanelVisible = !_isControlPanelVisible;
            
            var panel = this.FindName("controlPanel3D") as Border;
            var toggleButton = sender as Button;
            
            if (panel != null)
            {
                panel.Visibility = _isControlPanelVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            
            if (toggleButton != null)
            {
                toggleButton.Content = _isControlPanelVisible ? "◀ Ẩn" : "▶ Hiện";
                toggleButton.ToolTip = _isControlPanelVisible ? "Ẩn bảng điều khiển" : "Hiện bảng điều khiển";
            }
        }
        
        // Sync camera changes from user interactions (mouse drag, scroll) back to sliders
        private void Viewport3D_CameraChanged(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingFromCamera) return; // Prevent re-entry
            
            if (viewport3D?.Camera is PerspectiveCamera camera)
            {
                _isUpdatingFromCamera = true;
                
                try
                {
                    // Calculate distance from camera position to origin
                    var position = camera.Position;
                    double distance = Math.Sqrt(position.X * position.X + position.Y * position.Y + position.Z * position.Z);
                    
                    // Update distance and zoom slider
                    _camera3DDistance = distance;
                    if (zoomSlider != null && distance > 0)
                    {
                        double zoomValue = 25.0 / distance;
                        if (zoomValue >= zoomSlider.Minimum && zoomValue <= zoomSlider.Maximum)
                        {
                            zoomSlider.Value = zoomValue;
                        }
                    }
                    
                    // Calculate rotation angles from camera position
                    if (distance > 0.001)
                    {
                        double angleY = Math.Asin(Math.Max(-1, Math.Min(1, position.Y / distance))) * 180.0 / Math.PI;
                        double angleX = Math.Atan2(position.X, position.Z) * 180.0 / Math.PI;
                        
                        _camera3DRotationX = angleX;
                        _camera3DRotationY = angleY;
                        
                        // Update rotation sliders
                        if (rotationXSlider != null)
                        {
                            rotationXSlider.Value = Math.Max(rotationXSlider.Minimum, 
                                                            Math.Min(rotationXSlider.Maximum, angleX));
                        }
                        if (rotationYSlider != null)
                        {
                            rotationYSlider.Value = Math.Max(rotationYSlider.Minimum, 
                                                            Math.Min(rotationYSlider.Maximum, angleY));
                        }
                    }
                }
                finally
                {
                    _isUpdatingFromCamera = false;
                }
            }
        }
        
        #endregion

        #region Graph Management

        private void BtnAddGraph_Click(object sender, RoutedEventArgs e)
        {
            var newFunction = AddNewGraph(FunctionType.Sin, _colorPalette[_colorIndex]);
            _colorIndex = (_colorIndex + 1) % _colorPalette.Count;

            System.Diagnostics.Debug.WriteLine($"Added graph #{newFunction.Id}");
        }

        private void BtnRemoveGraph_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int id)
            {
                if (_config.Functions.Count <= 1)
                {
                    MessageBox.Show("Phải có ít nhất 1 đồ thị!", "Thông báo", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _config.RemoveGraph(id);
                System.Diagnostics.Debug.WriteLine($"Removed graph #{id}");
            }
        }

        private void BtnShowMathInfo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is GraphFunction function)
            {
                UpdateMathInfo(function);
                
                // Toggle visibility
                if (mathInfoPanel.Visibility == Visibility.Collapsed)
                {
                    mathInfoPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    // If clicking same function, toggle off; if different function, update
                    if (mathInfoPanel.Tag == function)
                    {
                        mathInfoPanel.Visibility = Visibility.Collapsed;
                    }
                }
                
                mathInfoPanel.Tag = function;
            }
        }

        private void UpdateMathInfo(GraphFunction function)
        {
            if (function != null)
            {
                mathInfoText.Text = function.GetMathematicalInfo();
            }
        }

        private GraphFunction AddNewGraph(FunctionType type, Color color)
        {
            var function = _config.AddNewGraph(type, color);

            function.PropertyChanged += (s, e) => 
            {
                System.Diagnostics.Debug.WriteLine($"Property changed: {e.PropertyName} for function #{function.Id}");
                UpdateCurrentView();
                
                // Auto-update math info if panel is visible and this is the tracked function
                if (mathInfoPanel.Visibility == Visibility.Visible && 
                    mathInfoPanel.Tag == function)
                {
                    UpdateMathInfo(function);
                }
            };

            UpdateCurrentView();
            return function;
        }

        #endregion

        #region Export

        private void BtnInsert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentMode == "2D")
                {
                    Export2DGraph();
                }
                else
                {
                    Export3DGraph();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi export đồ thị: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Export2DGraph()
        {
            // Save graph type to config
            if (_config != null)
            {
                _config.GraphType = "2D";
            }

            var pngExporter = new OxyPlot.Wpf.PngExporter { Width = 800, Height = 600 };
            var bitmap = pngExporter.ExportToBitmap(_plotModel);

            using (var memoryStream = new System.IO.MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(memoryStream);
                memoryStream.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                ExportedGraphImage = bitmapImage;
            }
            
            // Save config with graph type for editing later
            ExportedConfig = _config;

            DialogResult = true;
            System.Diagnostics.Debug.WriteLine("✅ 2D Graph exported");
            this.Close();
        }

        private void Export3DGraph()
        {
            // Save current camera state and graph type to config BEFORE export
            // This preserves user's custom view angle, zoom, and graph type
            if (_config != null)
            {
                _config.GraphType = "3D";
                _config.Camera3DDistance = _camera3DDistance;
                _config.Camera3DRotationX = _camera3DRotationX;
                _config.Camera3DRotationY = _camera3DRotationY;
                _config.Camera3DRotationZ = _camera3DRotationZ;
            }

            try
            {
                // Force viewport to update and measure
                viewport3D.UpdateLayout();
                viewport3D.Measure(new System.Windows.Size(800, 600));
                viewport3D.Arrange(new System.Windows.Rect(0, 0, 800, 600));
                viewport3D.UpdateLayout();

                // Render at higher resolution for better quality
                var renderBitmap = new RenderTargetBitmap(800, 600, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(viewport3D);

                var pngEncoder = new PngBitmapEncoder();
                pngEncoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                using (var memoryStream = new System.IO.MemoryStream())
                {
                    pngEncoder.Save(memoryStream);
                    memoryStream.Position = 0;

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = memoryStream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    ExportedGraphImage = bitmapImage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Export 3D graph error: {ex.Message}");
            }
            
            // Save config with camera state for editing later
            ExportedConfig = _config;

            DialogResult = true;
            System.Diagnostics.Debug.WriteLine("✅ 3D Graph exported with user's camera view");
            this.Close();
        }

        /// <summary>
        /// Get short formula for Legend display
        /// </summary>
        private string GetShortFormula(GraphFunction function)
        {
            return function.Type switch
            {
                FunctionType.Absolute => "|x|",
                FunctionType.Sin => "sin(x)",
                FunctionType.Cos => "cos(x)",
                FunctionType.Tan => "tan(x)",
                FunctionType.Exponential => "aˣ",
                FunctionType.Logarithm => "log(x)",
                FunctionType.Cubic => "x³",
                FunctionType.Rational => "x/(x)",
                FunctionType.Composite => "sin(ax)",
                _ => "f(x)"
            };
        }

        #endregion

        #region Mouse Interaction for 3D

        private void Viewport3D_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                _isMouseDragging = true;
                _lastMousePosition = e.GetPosition(viewport3D);
                viewport3D.CaptureMouse();
                viewport3D.Cursor = System.Windows.Input.Cursors.Hand;
                e.Handled = true;
            }
        }

        private void Viewport3D_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isMouseDragging && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(viewport3D);
                var deltaX = currentPosition.X - _lastMousePosition.X;
                var deltaY = currentPosition.Y - _lastMousePosition.Y;

                // Update rotation based on mouse movement
                // Horizontal movement rotates around Y axis (rotation X)
                _camera3DRotationX += deltaX * 0.5; // Sensitivity factor
                
                // Vertical movement rotates around X axis (rotation Y)
                _camera3DRotationY += deltaY * 0.5; // Sensitivity factor

                // Clamp rotation values
                if (_camera3DRotationX > 180) _camera3DRotationX -= 360;
                if (_camera3DRotationX < -180) _camera3DRotationX += 360;
                
                _camera3DRotationY = Math.Max(-90, Math.Min(90, _camera3DRotationY));

                // Update sliders to reflect changes
                rotationXSlider.Value = _camera3DRotationX;
                rotationYSlider.Value = _camera3DRotationY;

                Update3DCamera();
                _lastMousePosition = currentPosition;
                
                e.Handled = true;
            }
        }

        private void Viewport3D_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isMouseDragging)
            {
                _isMouseDragging = false;
                viewport3D.ReleaseMouseCapture();
                viewport3D.Cursor = System.Windows.Input.Cursors.Arrow;
                e.Handled = true;
            }
        }

        private void Viewport3D_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            // Scroll up = zoom in, scroll down = zoom out
            double zoomDelta = e.Delta > 0 ? 0.1 : -0.1;
            double newZoom = zoomSlider.Value + zoomDelta;
            
            // Clamp zoom value
            newZoom = Math.Max(zoomSlider.Minimum, Math.Min(zoomSlider.Maximum, newZoom));
            
            zoomSlider.Value = newZoom;
            e.Handled = true;
        }

        #endregion
    }

    /// <summary>
    /// Converter for Color to SolidColorBrush (GraphEditor specific)
    /// </summary>
    public class GraphEditorColorToBrushConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Color color)
            {
                return new SolidColorBrush(color);
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color;
            }
            return Colors.Black;
        }
    }

    /// <summary>
    /// Converter to display function type as mathematical formula
    /// </summary>
    public class FunctionTypeToFormulaConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is FunctionType functionType)
            {
                return functionType switch
                {
                    FunctionType.Absolute => "y = |ax + b|",
                    FunctionType.Sin => "y = sin(x)",
                    FunctionType.Cos => "y = cos(x)",
                    FunctionType.Tan => "y = tan(x)",
                    FunctionType.Exponential => "y = aˣ",
                    FunctionType.Logarithm => "y = logₐ(x)",
                    FunctionType.Cubic => "y = ax³ + bx² + cx + d",
                    FunctionType.Rational => "y = (ax + b) / (cx + d)",
                    FunctionType.Composite => "y = sin(ax + b)",
                    _ => value.ToString()
                };
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
