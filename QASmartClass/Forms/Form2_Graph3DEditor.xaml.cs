using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using QASmartTouch.Models;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace QASmartTouch.Forms
{
    public partial class Form2_Graph3DEditor : Window
    {
        private readonly Graph3DConfiguration _config;
        private readonly List<Color> _colorPalette = new List<Color>
        {
            Colors.Blue,
            Colors.Red,
            Colors.Green,
            Colors.Orange,
            Colors.Purple,
            Colors.Cyan,
            Colors.Magenta,
            Colors.Brown
        };
        private int _colorIndex = 0;

        public BitmapImage? ExportedGraphImage { get; private set; }

        public Form2_Graph3DEditor()
        {
            InitializeComponent();

            _config = new Graph3DConfiguration();
            
            // Apply graphics quality settings
            _config.Resolution = QASmartTouch.Services.AppSettings.GetRecommended3DResolution();
            ApplyGraphicsSettings();

            graphsList.DataContext = _config.Functions;

            // Add default surface (Paraboloid)
            AddNewSurface(Function3DType.Paraboloid, _colorPalette[_colorIndex]);
            _colorIndex = (_colorIndex + 1) % _colorPalette.Count;

            // Subscribe to parameter changes
            _config.Functions.CollectionChanged += (s, e) => Update3DScene();
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

        /// <summary>
        /// Add new 3D surface button handler
        /// </summary>
        private void BtnAddGraph_Click(object sender, RoutedEventArgs e)
        {
            var newFunction = AddNewSurface(Function3DType.Paraboloid, _colorPalette[_colorIndex]);
            _colorIndex = (_colorIndex + 1) % _colorPalette.Count;

            System.Diagnostics.Debug.WriteLine($"Added 3D surface #{newFunction.Id}");
        }

        /// <summary>
        /// Remove surface button handler
        /// </summary>
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
                System.Diagnostics.Debug.WriteLine($"Removed 3D surface #{id}");
            }
        }

        /// <summary>
        /// Add new 3D surface to configuration
        /// </summary>
        private Graph3DFunction AddNewSurface(Function3DType type, Color color)
        {
            var function = _config.AddNewGraph(type, color);

            // Subscribe to property changes for real-time updates
            function.PropertyChanged += (s, e) => Update3DScene();
            function.VisibilityChanged += (s, e) => Update3DScene();

            Update3DScene();
            return function;
        }

        /// <summary>
        /// Update all 3D surfaces in the viewport
        /// </summary>
        private void Update3DScene()
        {
            modelContainer.Children.Clear();

            foreach (var function in _config.Functions.Where(f => f.IsVisible))
            {
                try
                {
                    // Generate mesh for this function
                    var mesh = function.GenerateMesh(
                        _config.XMin, _config.XMax,
                        _config.YMin, _config.YMax,
                        _config.Resolution
                    );

                    // Create material with function's color
                    var material = new DiffuseMaterial(new SolidColorBrush(function.Color));

                    // Create geometry model
                    var geometryModel = new GeometryModel3D
                    {
                        Geometry = mesh,
                        Material = material,
                        BackMaterial = material // Show both sides
                    };

                    // Add to container
                    var modelVisual = new ModelVisual3D();
                    modelVisual.Content = geometryModel;
                    modelContainer.Children.Add(modelVisual);

                    System.Diagnostics.Debug.WriteLine($"Rendered surface #{function.Id}: {function.Type}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error rendering surface #{function.Id}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Camera preset: Top view (looking down Z axis)
        /// </summary>
        private void BtnTopView_Click(object sender, RoutedEventArgs e)
        {
            var camera = viewport3D.Camera as PerspectiveCamera;
            if (camera != null)
            {
                camera.Position = new System.Windows.Media.Media3D.Point3D(0, 0, 20);
                camera.LookDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, -20);
                camera.UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
            }
        }

        /// <summary>
        /// Camera preset: Front view (looking along Y axis)
        /// </summary>
        private void BtnFrontView_Click(object sender, RoutedEventArgs e)
        {
            var camera = viewport3D.Camera as PerspectiveCamera;
            if (camera != null)
            {
                camera.Position = new System.Windows.Media.Media3D.Point3D(0, -20, 5);
                camera.LookDirection = new System.Windows.Media.Media3D.Vector3D(0, 20, -5);
                camera.UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, 1);
            }
        }

        /// <summary>
        /// Camera preset: Side view (looking along X axis)
        /// </summary>
        private void BtnSideView_Click(object sender, RoutedEventArgs e)
        {
            var camera = viewport3D.Camera as PerspectiveCamera;
            if (camera != null)
            {
                camera.Position = new System.Windows.Media.Media3D.Point3D(20, 0, 5);
                camera.LookDirection = new System.Windows.Media.Media3D.Vector3D(-20, 0, -5);
                camera.UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, 1);
            }
        }

        /// <summary>
        /// Camera preset: Reset to default isometric view
        /// </summary>
        private void BtnResetView_Click(object sender, RoutedEventArgs e)
        {
            var camera = viewport3D.Camera as PerspectiveCamera;
            if (camera != null)
            {
                camera.Position = new System.Windows.Media.Media3D.Point3D(15, 15, 15);
                camera.LookDirection = new System.Windows.Media.Media3D.Vector3D(-15, -15, -15);
                camera.UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, 1);
            }
        }

        /// <summary>
        /// Export 3D scene to PNG and insert to canvas
        /// </summary>
        private void BtnInsert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Export viewport to bitmap
                var renderBitmap = new RenderTargetBitmap(
                    800, 600, // Width, Height
                    96, 96,   // DPI
                    PixelFormats.Pbgra32
                );

                renderBitmap.Render(viewport3D);

                // Convert to BitmapImage for easier handling
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

                DialogResult = true;
                System.Diagnostics.Debug.WriteLine("✅ 3D Graph exported successfully");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi export đồ thị 3D: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Export error: {ex}");
            }
        }
    }

    /// <summary>
    /// Value converter for Color to Brush (for XAML binding)
    /// </summary>
    public class ColorToBrushConverter : System.Windows.Data.IValueConverter
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
            throw new NotImplementedException();
        }
    }
}
