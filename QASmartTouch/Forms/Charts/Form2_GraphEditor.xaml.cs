using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using QASmartTouch.Models;

namespace QASmartTouch.Forms
{
    public partial class Form2_GraphEditor : Window
    {
        private GraphConfiguration _config;
        private PlotModel _plotModel;
        private readonly Color[] _colorPalette = new[]
        {
            Color.FromRgb(33, 150, 243),   // Blue
            Color.FromRgb(239, 83, 80),    // Red
            Color.FromRgb(76, 175, 80),    // Green
            Color.FromRgb(255, 152, 0),    // Orange
            Color.FromRgb(156, 39, 176),   // Purple
            Color.FromRgb(233, 30, 99),    // Pink
            Color.FromRgb(0, 150, 136),    // Teal
            Color.FromRgb(255, 193, 7)     // Amber
        };

        public BitmapImage? ExportedGraphImage { get; private set; }

        public Form2_GraphEditor()
        {
            InitializeComponent();
            _config = new GraphConfiguration();
            InitializePlot();
            AddDefaultGraph();
        }

        #region Initialization

        private void InitializePlot()
        {
            _plotModel = new PlotModel
            {
                Title = "Đồ Thị Hàm Số",
                TitleFontSize = 18,
                TitleFontWeight = OxyPlot.FontWeights.Bold,
                Background = OxyColors.White
            };

            // X Axis
            _plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "x",
                TitleFontSize = 14,
                TitleFontWeight = OxyPlot.FontWeights.Bold,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
                MinorGridlineColor = OxyColor.FromRgb(245, 245, 245),
                Minimum = _config.XMin,
                Maximum = _config.XMax
            });

            // Y Axis
            _plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "y",
                TitleFontSize = 14,
                TitleFontWeight = OxyPlot.FontWeights.Bold,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
                MinorGridlineColor = OxyColor.FromRgb(245, 245, 245),
                Minimum = _config.YMin,
                Maximum = _config.YMax
            });

            GraphPlotView.Model = _plotModel;
        }

        private void AddDefaultGraph()
        {
            var defaultColor = _colorPalette[0];
            var graph = _config.AddNewGraph(FunctionType.Sin, defaultColor);
            graph.ParameterChanged += Graph_ParameterChanged;
            RefreshGraphsList();
            UpdatePlot();
        }

        #endregion

        #region Event Handlers

        private void BtnAddGraph_Click(object sender, RoutedEventArgs e)
        {
            var colorIndex = _config.Functions.Count % _colorPalette.Length;
            var newColor = _colorPalette[colorIndex];
            var graph = _config.AddNewGraph(FunctionType.Sin, newColor);
            graph.ParameterChanged += Graph_ParameterChanged;
            
            RefreshGraphsList();
            UpdatePlot();

            System.Diagnostics.Debug.WriteLine($"✅ Added graph #{graph.Id}, Total: {_config.Functions.Count}");
        }

        private void BtnRemoveGraph_Click(object sender, RoutedEventArgs e)
        {
            if (_config.Functions.Count <= 1)
            {
                MessageBox.Show("Phải có ít nhất 1 đồ thị!", "Thông Báo", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (sender is Button button && button.Tag is int graphId)
            {
                var graph = _config.Functions.FirstOrDefault(f => f.Id == graphId);
                if (graph != null)
                {
                    graph.ParameterChanged -= Graph_ParameterChanged;
                    _config.RemoveGraph(graphId);
                    RefreshGraphsList();
                    UpdatePlot();

                    System.Diagnostics.Debug.WriteLine($"❌ Removed graph #{graphId}, Remaining: {_config.Functions.Count}");
                }
            }
        }

        private void FunctionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Tag is int graphId)
            {
                if (comboBox.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is string typeString)
                {
                    if (Enum.TryParse<FunctionType>(typeString, out var functionType))
                    {
                        var graph = _config.Functions.FirstOrDefault(f => f.Id == graphId);
                        if (graph != null)
                        {
                            graph.Type = functionType;
                            UpdatePlot();
                            
                            System.Diagnostics.Debug.WriteLine($"🔄 Graph #{graphId} changed to {functionType}");
                        }
                    }
                }
            }
        }

        private void Parameter_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Parameters are bound via TwoWay binding, so changes trigger ParameterChanged event
            // UpdatePlot will be called via Graph_ParameterChanged
        }

        private void Graph_ParameterChanged(object? sender, EventArgs e)
        {
            UpdatePlot();
        }

        private void BtnInsert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_config.Functions.Count == 0)
                {
                    MessageBox.Show("Không có đồ thị nào để chèn!", "Thông Báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Export plot to PNG
                var pngExporter = new OxyPlot.Wpf.PngExporter
                {
                    Width = 800,
                    Height = 600
                };

                var bitmap = pngExporter.ExportToBitmap(_plotModel);

                // Convert to BitmapImage
                using (var memory = new MemoryStream())
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(memory);
                    memory.Position = 0;

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = memory;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    ExportedGraphImage = bitmapImage;
                }

                System.Diagnostics.Debug.WriteLine($"✅ Exported graph: {ExportedGraphImage.PixelWidth}x{ExportedGraphImage.PixelHeight}");

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất đồ thị: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Export error: {ex.Message}");
            }
        }

        #endregion

        #region Plot Update

        private void RefreshGraphsList()
        {
            GraphsList.ItemsSource = null;
            GraphsList.ItemsSource = _config.Functions;
        }

        private void UpdatePlot()
        {
            _plotModel.Series.Clear();

            foreach (var func in _config.Functions.Where(f => f.IsVisible))
            {
                var series = new LineSeries
                {
                    Title = $"{func.GetFunctionName()} (ID: {func.Id})",
                    Color = OxyColor.FromRgb(func.Color.R, func.Color.G, func.Color.B),
                    StrokeThickness = 2,
                    MarkerType = MarkerType.None
                };

                // Generate data points
                double xStart = _config.XMin;
                double xEnd = _config.XMax;
                double step = _config.Step;

                // Adjust range for specific functions
                switch (func.Type)
                {
                    case FunctionType.Exponential:
                        xStart = Math.Max(xStart, -5);
                        xEnd = Math.Min(xEnd, 5);
                        break;
                    case FunctionType.Logarithm:
                        xStart = Math.Max(xStart, 0.1);
                        break;
                }

                for (double x = xStart; x <= xEnd; x += step)
                {
                    double? y = func.CalculateY(x);
                    
                    if (y.HasValue && !double.IsNaN(y.Value) && !double.IsInfinity(y.Value))
                    {
                        // Clamp y values to prevent extreme outliers
                        double clampedY = Math.Max(_config.YMin, Math.Min(_config.YMax, y.Value));
                        series.Points.Add(new DataPoint(x, clampedY));
                    }
                    else
                    {
                        // Add break in line for discontinuities
                        if (series.Points.Count > 0)
                        {
                            series.Points.Add(DataPoint.Undefined);
                        }
                    }
                }

                _plotModel.Series.Add(series);
            }

            _plotModel.InvalidatePlot(true);

            System.Diagnostics.Debug.WriteLine($"🔄 Updated plot with {_plotModel.Series.Count} series");
        }

        #endregion

        #region Helper Methods

        private Color GetNextColor()
        {
            int index = _config.Functions.Count % _colorPalette.Length;
            return _colorPalette[index];
        }

        #endregion
    }
}
