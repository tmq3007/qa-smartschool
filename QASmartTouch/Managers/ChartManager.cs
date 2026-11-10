using System.Windows;
using System.Windows.Controls;
using QASmartTouch.Forms;
using QASmartTouch.Services.Charts;
using CanvasControl = System.Windows.Controls.Canvas;

namespace QASmartTouch.Managers
{
    /// <summary>
    /// Manages all chart operations
    /// Responsibilities:
    /// - Orchestrate chart creation and rendering
    /// - Delegate to specific chart services (Line, Pie, Area, Scatter, Radar)
    /// - Handle chart placement on canvas
    /// - Support DisplayStats mode with stats panel
    /// - Coordinate with undo/redo system
    /// - Performance monitoring (Sprint 2 Day 10)
    /// </summary>
    public class ChartManager
    {
        #region Fields

        private readonly CanvasControl _canvas;
        private readonly ILineChartService _lineChartService;
        private readonly IPieChartService _pieChartService;
        private readonly IAreaChartService _areaChartService;
        private readonly IScatterChartService _scatterChartService;
        private readonly IRadarChartService _radarChartService;
        private readonly System.Diagnostics.Stopwatch _performanceTimer;
        private bool _enablePerformanceLogging = true;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a chart is created
        /// </summary>
        public event System.EventHandler<CanvasControl> OnChartCreated;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize ChartManager with required dependencies
        /// </summary>
        public ChartManager(
            CanvasControl canvas,
            ILineChartService lineChartService = null,
            IPieChartService pieChartService = null,
            IAreaChartService areaChartService = null,
            IScatterChartService scatterChartService = null,
            IRadarChartService radarChartService = null)
        {
            _canvas = canvas ?? throw new System.ArgumentNullException(nameof(canvas));
            
            // Auto-create services if not provided (backward compatibility)
            _lineChartService = lineChartService ?? new LineChartService();
            _pieChartService = pieChartService ?? new PieChartService();
            _areaChartService = areaChartService ?? new AreaChartService();
            _scatterChartService = scatterChartService ?? new ScatterChartService();
            _radarChartService = radarChartService ?? new RadarChartService();

            // Initialize performance timer (Sprint 2 Day 10)
            _performanceTimer = new System.Diagnostics.Stopwatch();

            System.Diagnostics.Debug.WriteLine("✅ ChartManager initialized");
        }

        #endregion

        #region Public Methods - Line Chart

        /// <summary>
        /// Create and add line chart to canvas
        /// </summary>
        public CanvasControl CreateLineChart(Form2_9_LineChartEditor editor, double width = 800, double height = 600)
        {
            _performanceTimer.Restart();
            
            var chartContainer = _lineChartService.CreateChart(editor, width, height);
            
            AddChartToCanvas(chartContainer, 100, 100);
            OnChartCreated?.Invoke(this, chartContainer);
            
            _performanceTimer.Stop();
            LogPerformance("Line Chart", _performanceTimer.ElapsedMilliseconds);
            
            return chartContainer;
        }

        /// <summary>
        /// Create line chart with statistics panel
        /// </summary>
        public (CanvasControl chart, StackPanel stats) CreateLineChartWithStats(Form2_9_LineChartEditor editor, 
            double width = 800, double height = 600)
        {
            _performanceTimer.Restart();
            
            var (chartContainer, statsPanel) = _lineChartService.CreateChartWithStats(editor, width, height);
            
            AddChartToCanvas(chartContainer, 50, 50);
            AddStatsPanelToCanvas(statsPanel, 900, 50);
            OnChartCreated?.Invoke(this, chartContainer);
            
            _performanceTimer.Stop();
            LogPerformance("Line Chart with Stats", _performanceTimer.ElapsedMilliseconds);
            
            return (chartContainer, statsPanel);
        }

        #endregion

        #region Public Methods - Pie Chart

        /// <summary>
        /// Create and add pie chart to canvas
        /// </summary>
        public CanvasControl CreatePieChart(Form2_10_PieChartEditor editor, double width = 900, double height = 700)
        {
            _performanceTimer.Restart();
            
            var chartContainer = _pieChartService.CreateChart(editor, width, height);
            
            AddChartToCanvas(chartContainer, 100, 100);
            OnChartCreated?.Invoke(this, chartContainer);
            
            _performanceTimer.Stop();
            LogPerformance("Pie Chart", _performanceTimer.ElapsedMilliseconds);
            
            return chartContainer;
        }

        /// <summary>
        /// Create pie chart with statistics panel
        /// </summary>
        public (CanvasControl chart, StackPanel stats) CreatePieChartWithStats(Form2_10_PieChartEditor editor, 
            double width = 900, double height = 700)
        {
            _performanceTimer.Restart();
            
            var (chartContainer, statsPanel) = _pieChartService.CreateChartWithStats(editor, width, height);
            
            AddChartToCanvas(chartContainer, 50, 50);
            AddStatsPanelToCanvas(statsPanel, 1000, 50);
            OnChartCreated?.Invoke(this, chartContainer);
            
            _performanceTimer.Stop();
            LogPerformance("Pie Chart with Stats", _performanceTimer.ElapsedMilliseconds);
            
            return (chartContainer, statsPanel);
        }

        #endregion

        #region Public Methods - Area Chart

        /// <summary>
        /// Create and add area chart to canvas
        /// </summary>
        public CanvasControl CreateAreaChart(Form2_12_AreaChartEditor editor, double width = 800, double height = 600)
        {
            var chartContainer = _areaChartService.CreateChart(editor, width, height);
            
            AddChartToCanvas(chartContainer, 100, 100);
            OnChartCreated?.Invoke(this, chartContainer);
            
            System.Diagnostics.Debug.WriteLine($"📊 Area chart created ({width}x{height})");
            return chartContainer;
        }

        #endregion

        #region Public Methods - Scatter Chart

        /// <summary>
        /// Create and add scatter chart to canvas
        /// </summary>
        public CanvasControl CreateScatterChart(Form2_13_ScatterChartEditor editor, double width = 900, double height = 600)
        {
            var chartContainer = _scatterChartService.CreateChart(editor, width, height);
            
            AddChartToCanvas(chartContainer, 100, 100);
            OnChartCreated?.Invoke(this, chartContainer);
            
            System.Diagnostics.Debug.WriteLine($"📊 Scatter chart created ({width}x{height})");
            return chartContainer;
        }

        #endregion

        #region Public Methods - Radar Chart

        /// <summary>
        /// Create and add radar chart to canvas
        /// </summary>
        public CanvasControl CreateRadarChart(Form2_14_RadarChartEditor editor, double width = 700, double height = 700)
        {
            var chartContainer = _radarChartService.CreateChart(editor, width, height);
            
            AddChartToCanvas(chartContainer, 100, 100);
            OnChartCreated?.Invoke(this, chartContainer);
            
            System.Diagnostics.Debug.WriteLine($"📊 Radar chart created ({width}x{height})");
            return chartContainer;
        }

        #endregion

        #region Private Methods - Canvas Operations

        /// <summary>
        /// Add chart container to main canvas
        /// </summary>
        private void AddChartToCanvas(CanvasControl chartContainer, double left, double top)
        {
            CanvasControl.SetLeft(chartContainer, left);
            CanvasControl.SetTop(chartContainer, top);
            _canvas.Children.Add(chartContainer);
        }

        /// <summary>
        /// Add stats panel to main canvas
        /// </summary>
        private void AddStatsPanelToCanvas(StackPanel statsPanel, double left, double top)
        {
            // Wrap stats panel in a border for dragging support
            var statsBorder = new System.Windows.Controls.Border
            {
                Child = statsPanel,
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(2),
                CornerRadius = new System.Windows.CornerRadius(5),
                Padding = new Thickness(10)
            };

            CanvasControl.SetLeft(statsBorder, left);
            CanvasControl.SetTop(statsBorder, top);
            _canvas.Children.Add(statsBorder);
        }

        #endregion

        #region Public Properties - Service Access

        /// <summary>
        /// Access to LineChartService for advanced operations
        /// </summary>
        public ILineChartService LineChartService => _lineChartService;

        /// <summary>
        /// Access to PieChartService for advanced operations
        /// </summary>
        public IPieChartService PieChartService => _pieChartService;

        /// <summary>
        /// Access to AreaChartService for advanced operations
        /// </summary>
        public IAreaChartService AreaChartService => _areaChartService;

        /// <summary>
        /// Access to ScatterChartService for advanced operations
        /// </summary>
        public IScatterChartService ScatterChartService => _scatterChartService;

        /// <summary>
        /// Access to RadarChartService for advanced operations
        /// </summary>
        public IRadarChartService RadarChartService => _radarChartService;

        /// <summary>
        /// Enable/disable performance logging (Sprint 2 Day 10)
        /// </summary>
        public bool EnablePerformanceLogging
        {
            get => _enablePerformanceLogging;
            set => _enablePerformanceLogging = value;
        }

        #endregion

        #region Performance Monitoring (Sprint 2 Day 10)

        /// <summary>
        /// Log performance metrics for chart creation
        /// </summary>
        private void LogPerformance(string chartType, long elapsedMs)
        {
            if (!_enablePerformanceLogging) return;

            var performanceLevel = elapsedMs switch
            {
                < 100 => "⚡ EXCELLENT",
                < 300 => "✅ GOOD",
                < 500 => "⚠️ ACCEPTABLE",
                _ => "❌ SLOW"
            };

            var logMessage = $"📊 Performance: {chartType} - {elapsedMs}ms {performanceLevel}";
            
            // Log to Debug Console
            System.Diagnostics.Debug.WriteLine(logMessage);
            
            // Also log to file for standalone exe verification
            LogToFile(logMessage);

            // Log warning if exceeds target (500ms)
            if (elapsedMs > 500)
            {
                var warningMessage = $"⚠️ WARNING: {chartType} creation exceeded 500ms target!";
                System.Diagnostics.Debug.WriteLine(warningMessage);
                LogToFile(warningMessage);
            }
        }

        /// <summary>
        /// Log performance data to file for standalone verification
        /// </summary>
        private void LogToFile(string message)
        {
            try
            {
                var timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] {message}";
                var logPath = System.IO.Path.Combine(
                    System.AppDomain.CurrentDomain.BaseDirectory,
                    "performance_log.txt"
                );
                System.IO.File.AppendAllText(logPath, logEntry + System.Environment.NewLine);
            }
            catch
            {
                // Silently fail if file logging doesn't work
            }
        }

        /// <summary>
        /// Get performance recommendations based on current metrics
        /// </summary>
        public string GetPerformanceRecommendations()
        {
            return @"Performance Optimization Tips:
1. Use BitmapCache for complex charts (set RenderOptions.CachingHint)
2. Reduce chart resolution for preview mode
3. Lazy load chart editors (delay initialization)
4. Use virtualization for large datasets
5. Minimize property changes during rendering
6. Consider background rendering for complex charts";
        }

        #endregion
    }
}
