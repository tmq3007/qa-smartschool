using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;
using QASmartTouch.Services.VersionManagement;

namespace QASmartTouch.Forms
{
    public partial class Form2_5_SubMenuDrawShapes : Window
    {
        public string? SelectedShape { get; private set; }
        public string? SelectedCategory { get; private set; }
        public bool UserSelected { get; private set; } = false;  // ✅ Flag to check if user selected a shape
        private Form2_MainDashboard? _mainDashboard;
        private string _currentCategory = "2D";
        
        public Form2_5_SubMenuDrawShapes(Form2_MainDashboard? mainDashboard = null)
        {
            InitializeComponent();
            if (btnClose != null)
            {
                System.Windows.Input.Stylus.SetIsPressAndHoldEnabled(btnClose, false);

                btnClose.PreviewTouchDown += (s, e) =>
                {
                    e.TouchDevice.Capture(btnClose);
                    e.Handled = true;
                };

                btnClose.PreviewTouchUp += (s, e) =>
                {
                    if (e.TouchDevice.Captured == btnClose)
                    {
                        btnClose.ReleaseTouchCapture(e.TouchDevice);
                    }
                    e.Handled = true;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try 
                        {
                            _mainDashboard?.Activate();
                            this.Close();
                        } 
                        catch { }
                    }), System.Windows.Threading.DispatcherPriority.Normal);
                };

                btnClose.PreviewStylusDown += (s, e) =>
                {
                    e.StylusDevice.Capture(btnClose);
                    e.Handled = true;
                };

                btnClose.PreviewStylusUp += (s, e) =>
                {
                    if (e.StylusDevice.Captured == btnClose)
                    {
                        btnClose.ReleaseStylusCapture();
                    }
                    e.Handled = true;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try 
                        {
                            _mainDashboard?.Activate();
                            this.Close();
                        } 
                        catch { }
                    }), System.Windows.Threading.DispatcherPriority.Normal);
                };
            }
            _mainDashboard = mainDashboard;
            
            // Subscribe to feature changes
            FeatureManager.Instance.FeaturesChanged += OnFeaturesChanged;
            
            if (btn2D != null) WireTouchActivation(btn2D, btnCategory_Click);
            if (btn3D != null) WireTouchActivation(btn3D, btnCategory_Click);

            // Load default category (2D)
            UpdateShapeGallery("2D");
        }

        private void OnFeaturesChanged(object? sender, EventArgs e)
        {
            // Refresh gallery when features change
            System.Diagnostics.Debug.WriteLine($"[Form2_5] Features changed, refreshing gallery for category: {_currentCategory}");
            Dispatcher.Invoke(() => UpdateShapeGallery(_currentCategory));
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            // Unsubscribe from event
            FeatureManager.Instance.FeaturesChanged -= OnFeaturesChanged;
            try { _mainDashboard?.Activate(); } catch { }
            this.Close();
        }

        private void btnCategory_Click(object sender, RoutedEventArgs e)
        {
        if (sender is Button button && button.Tag is string category)
        {
            SelectedCategory = category;
            _currentCategory = category; // Track current category
            UpdateShapeGallery(category);
            
            // Highlight selected category
            btn2D.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3F2FD"));
            btn3D.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3E5F5"));
            
            button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64B5F6"));
        }
    }        private void UpdateShapeGallery(string category)
        {
            ShapeGallery.Children.Clear();
            
            string[] shapes = category switch
            {
                // PHASE 1: 19 shapes (5 Lines + 5 Rectangles + 9 Basic Shapes)
                // PHASE 2: +95 shapes (Block Arrows, Stars, Flowchart, Callouts, Equations)
                "2D" => new[] {
                    // 7 basic 2D shapes
                    "StraightLine",    // Đường thẳng
                    "ArrowLine",       // Đường mũi tên
                    "DashedLine",      // Nét đứt
                    "Circle",          // Hình tròn
                    "Triangle",        // Hình tam giác
                    "Square",          // Hình vuông
                    "Rectangle",       // Hình chữ nhật
                },
                "3D" => new[] { "Cube", "Sphere", "Pyramid", "Cylinder", "Cone", "Prism" },
                _ => new[] { "Rectangle", "Circle", "Triangle" }
            };
            
            // Filter shapes based on FeatureManager configuration
            var featureManager = FeatureManager.Instance;
            var enabledShapes = shapes.Where(shapeName => 
            {
                var featureId = GetFeatureId(shapeName);
                var isEnabled = featureManager.IsEnabled(featureId);
                
                // Debug log cho các ký hiệu toán học
                if (shapeName == "Approximately" || shapeName == "Infinity" || shapeName == "Radical" || 
                    shapeName == "Summation" || shapeName == "Integral")
                {
                    System.Diagnostics.Debug.WriteLine($"[Form2_5] Math symbol: {shapeName} -> featureId: {featureId} -> enabled: {isEnabled}");
                }
                
                return isEnabled;
            }).ToArray();
            
            System.Diagnostics.Debug.WriteLine($"[Form2_5] Showing {enabledShapes.Length}/{shapes.Length} shapes for category {category}");
            
            foreach (var shapeName in enabledShapes)
            {
                var btn = CreateShapeButton(shapeName, category);
                ShapeGallery.Children.Add(btn);
            }
        }
        
        /// <summary>
        /// Map shape name to feature ID in VersionDetail.json
        /// </summary>
        private string GetFeatureId(string shapeName)
        {
            return shapeName switch
            {
                // Lines
                "StraightLine" => "straight_line",
                "ArrowLine" => "arrow_line",
                "DashedLine" => "dashed_line",
                "DoubleArrowLine" => "double_arrow_line",
                "ElbowConnector" => "elbow_connector",
                "CurvedConnector" => "curved_connector",
                
                // Rectangles / Basic
                "Square" => "square",
                "Rectangle" => "rectangle",
                "RoundedRectangle" => "rounded_rectangle",
                "SnipSingleCornerRectangle" => "snip_single_corner",
                "SnipDiagonalCornerRectangle" => "snip_diagonal_corner",
                "RoundSingleCornerRectangle" => "round_single_corner",
                
                // Basic Shapes
                "Circle" => "circle",
                "Triangle" => "triangle",
                "Pentagon" => "pentagon",
                "Hexagon" => "hexagon",
                "Star" => "star",
                "Heart" => "heart",
                "Diamond" => "diamond",
                "Parallelogram" => "parallelogram",
                "Cross" => "cross",
                "Cloud" => "cloud",
                
                // Block Arrows
                "RightArrow" => "right_arrow",
                "LeftArrow" => "left_arrow",
                "UpArrow" => "up_arrow",
                "DownArrow" => "down_arrow",
                "LeftRightArrow" => "left_right_arrow",
                "UpDownArrow" => "up_down_arrow",
                "QuadArrow" => "quad_arrow",
                "NotchedRightArrow" => "notched_arrow",
                "PentagonArrow" => "pentagon_arrow",
                "ChevronArrow" => "chevron_arrow",
                "StripedRightArrow" => "striped_arrow",
                "CurvedRightArrow" => "curved_right_arrow",
                "CurvedLeftArrow" => "curved_left_arrow",
                "CurvedUpArrow" => "curved_up_arrow",
                "CurvedDownArrow" => "curved_down_arrow",
                
                // Stars & Banners
                "Star4" => "star_4",
                "Star6" => "star_6",
                "Star7" => "star_7",
                "Star8" => "star_8",
                "Star10" => "star_10",
                "Star12" => "star_12",
                "Star16" => "star_16",
                "Star24" => "star_24",
                "Star32" => "star_32",
                "Explosion1" => "explosion_1",
                "Explosion2" => "explosion_2",
                "Wave" => "wave",
                "DoubleWave" => "double_wave",
                "Ribbon" => "ribbon",
                "Scroll" => "scroll",
                
                // Flowchart
                "FlowProcess" => "flow_process",
                "FlowDecision" => "flow_decision",
                "FlowDocument" => "flow_document",
                "FlowData" => "flow_data",
                "FlowPredefinedProcess" => "flow_predefined",
                "FlowInternalStorage" => "flow_storage",
                "FlowSequentialData" => "flow_seq_data",
                "FlowDirectData" => "flow_direct_data",
                "FlowManualInput" => "flow_manual_input",
                "FlowManualOperation" => "flow_manual_op",
                "FlowConnector" => "flow_connector",
                "FlowOffPageConnector" => "flow_offpage",
                "FlowSummingJunction" => "flow_summing",
                "FlowOr" => "flow_or",
                "FlowSort" => "flow_sort",
                "FlowExtract" => "flow_extract",
                "FlowMerge" => "flow_merge",
                "FlowStoredData" => "flow_stored",
                "FlowDelay" => "flow_delay",
                "FlowPreparation" => "flow_preparation",
                
                // Equation Shapes
                "Plus" => "plus",
                "Minus" => "minus",
                "Multiply" => "multiply",
                "Divide" => "divide",
                "Equal" => "equal",
                "NotEqual" => "not_equal",
                "LessThan" => "less_than",
                "GreaterThan" => "greater_than",
                "LessOrEqual" => "less_or_equal",
                "GreaterOrEqual" => "greater_or_equal",
                "Approximately" => "approximately",
                "Infinity" => "infinity",
                "Radical" => "radical",
                "Summation" => "summation",
                "Integral" => "integral",
                
                // 3D Shapes
                "Cube" => "cube",
                "Sphere" => "sphere",
                "Pyramid" => "pyramid",
                "Cylinder" => "cylinder",
                "Cone" => "cone",
                "Prism" => "prism",
                
                // Callouts - map to callout group for now
                "RectangularCallout" or "RoundedRectangularCallout" or "OvalCallout" => "speech_bubble",
                "LineCallout1" or "LineCallout2" or "LineCallout3" or "LineCallout4" => "callout_box",
                "BorderlessLineCallout1" or "BorderlessLineCallout2" or "BorderlessLineCallout3" or "BorderlessLineCallout4" => "callout_box",
                "AccentCallout1" or "AccentCallout2" or "AccentCallout3" => "callout_box",
                
                // Default - use lowercase with underscore
                _ => shapeName.ToLowerInvariant()
            };
        }


        private Button CreateShapeButton(string shapeName, string category)
        {
            var button = new Button
            {
                Width = 80,
                Height = 80,
                Margin = new Thickness(5),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F6FA")),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DFE4EA")),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = shapeName
            };

            // Create visual preview of shape
            var canvas = new Canvas
            {
                Width = 50,
                Height = 50,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            UIElement? shapePreview = null;
            
            if (shapeName == "ScatterChart")
            {
                shapePreview = CreateScatterChart();
            }
            else
            {
                shapePreview = shapeName switch
                {
                    // Lines
                    "StraightLine" => new Line { X1 = 5, Y1 = 25, X2 = 45, Y2 = 25, Stroke = Brushes.DarkGray, StrokeThickness = 3 },
                    "ArrowLine" => CreateArrow(),
                    "DashedLine" => new Line { X1 = 5, Y1 = 25, X2 = 45, Y2 = 25, Stroke = Brushes.DarkGray, StrokeThickness = 3,
                                               StrokeDashArray = new DoubleCollection { 5, 3 } },
                    "DoubleArrowLine" => CreateDoubleArrow(),
                    "ElbowConnector" => CreateElbowConnector(),
                    "CurvedConnector" => CreateCurvedConnector(),
                    
                    // Basic shapes (user-selected 7)
                    "Square" => new Rectangle { Width = 35, Height = 35, Fill = Brushes.CornflowerBlue, Stroke = Brushes.DarkBlue, StrokeThickness = 2 },
                    "Rectangle" => new Rectangle { Width = 40, Height = 28, Fill = Brushes.MediumPurple, Stroke = Brushes.DarkMagenta, StrokeThickness = 2 },
                    "RoundedRectangle" => new Rectangle { Width = 40, Height = 30, Fill = Brushes.LightGreen, Stroke = Brushes.DarkGreen, StrokeThickness = 2, RadiusX = 5, RadiusY = 5 },
                    "SnipSingleCornerRectangle" => CreateSnipSingleCorner(),
                    "SnipDiagonalCornerRectangle" => CreateSnipDiagonalCorner(),
                    "RoundSingleCornerRectangle" => CreateRoundSingleCorner(),
                    
                    // PHASE 1: Basic Shapes (9) - Removed Cloud
                    "Circle" => new Ellipse { Width = 35, Height = 35, Fill = Brushes.DeepSkyBlue, Stroke = Brushes.DarkBlue, StrokeThickness = 2 },
                    "Triangle" => CreateTriangle(),
                    "Pentagon" => CreatePentagon(),
                    "Hexagon" => CreateHexagon(),
                    "Star" => CreateStar(),
                    "Heart" => CreateHeart(),
                    "Diamond" => CreateDiamond(),
                    "Parallelogram" => CreateParallelogram(),
                    "Cross" => CreateCross(),
                    
                    // PHASE 2A: Block Arrows (15) - Simple previews
                    "RightArrow" or "LeftArrow" or "UpArrow" or "DownArrow" => CreateArrow(),
                    "LeftRightArrow" or "UpDownArrow" => CreateDoubleArrow(),
                    "QuadArrow" or "NotchedRightArrow" or "PentagonArrow" or "ChevronArrow" or "StripedRightArrow" => CreateArrow(),
                    "CurvedRightArrow" or "CurvedLeftArrow" or "CurvedUpArrow" or "CurvedDownArrow" => CreateCurvedConnector(),
                    
                    // PHASE 2B: Stars & Banners (15) - Reuse existing star
                    "Star4" or "Star6" or "Star7" or "Star8" or "Star10" or "Star12" or "Star16" or "Star24" or "Star32" => CreateStar(),
                    "Explosion1" or "Explosion2" or "Wave" or "DoubleWave" or "Ribbon" or "Scroll" => CreateStar(),
                    
                    // PHASE 2C: Flowchart (20) - Basic shapes for preview
                    "FlowProcess" => new Rectangle { Width = 40, Height = 25, Fill = Brushes.LightBlue, Stroke = Brushes.DarkBlue, StrokeThickness = 2 },
                    "FlowDecision" or "FlowData" or "FlowSort" => CreateDiamond(),
                    "FlowDocument" or "FlowManualInput" => CreateTriangle(),
                    "FlowPredefinedProcess" or "FlowInternalStorage" => new Rectangle { Width = 40, Height = 25, Fill = Brushes.LightGreen, Stroke = Brushes.DarkGreen, StrokeThickness = 2 },
                    "FlowSequentialData" or "FlowDirectData" or "FlowStoredData" or "FlowDelay" => new Ellipse { Width = 35, Height = 25, Fill = Brushes.LightYellow, Stroke = Brushes.Orange, StrokeThickness = 2 },
                    "FlowManualOperation" or "FlowPreparation" => CreateHexagon(),
                    "FlowConnector" or "FlowSummingJunction" or "FlowOr" => new Ellipse { Width = 30, Height = 30, Fill = Brushes.LightGreen, Stroke = Brushes.DarkGreen, StrokeThickness = 2 },
                    "FlowOffPageConnector" or "FlowExtract" or "FlowMerge" => CreatePentagon(),
                    
                    // PHASE 2D: Callouts (14) - Specific preview icons
                    "RectangularCallout" => CreateRectangularCalloutIcon(),
                    "RoundedRectangularCallout" => CreateRoundedCalloutIcon(),
                    "OvalCallout" => CreateOvalCalloutIcon(),
                    "CloudCallout" => CreateCloudCalloutIcon(),
                    "LineCallout1" or "BorderCallout1" or "AccentCallout1" => CreateLineCallout1Icon(),
                    "LineCallout2" or "BorderCallout2" or "AccentCallout2" => CreateLineCallout2Icon(),
                    "LineCallout3" or "BorderCallout3" or "AccentCallout3" => CreateLineCallout3Icon(),
                    "LineCallout4" => CreateLineCallout4Icon(),
                    
                    // PHASE 2E: Equation Shapes (15) - Proper icons
                    "Plus" => CreatePlusIcon(),
                    "Minus" => CreateMinusIcon(),
                    "Multiply" => CreateMultiplyIcon(),
                    "Divide" => CreateDivideIcon(),
                    "Equal" => CreateEqualIcon(),
                    "NotEqual" => CreateNotEqualIcon(),
                    "Approximately" => CreateApproximatelyIcon(),
                    "LessThan" => CreateLessThanIcon(),
                    "GreaterThan" => CreateGreaterThanIcon(),
                    "LessOrEqual" => CreateLessOrEqualIcon(),
                    "GreaterOrEqual" => CreateGreaterOrEqualIcon(),
                    "Infinity" => CreateInfinityIcon(),
                    "Radical" => CreateRadicalIcon(),
                    "Summation" => CreateSummationIcon(),
                    "Integral" => CreateIntegralIcon(),
                    
                    // Old shapes (keeping for compatibility)
                    "Arrow" => CreateArrow(),
                    "Line" => new Line { X1 = 5, Y1 = 40, X2 = 45, Y2 = 10, Stroke = Brushes.DarkGray, StrokeThickness = 3 },
                    "Polygon" => CreatePolygon(),
                    
                    // 3D Shapes
                    "Cube" => CreateCube(),
                    "Sphere" => new Ellipse { Width = 35, Height = 35, Fill = Brushes.LightSteelBlue, Stroke = Brushes.SteelBlue, StrokeThickness = 2 },
                    "Pyramid" => CreatePyramid(),
                    "Cylinder" => CreateCylinder(),
                    "Cone" => CreateCone(),
                    "Prism" => CreatePrism(),
                    "Torus" => new Ellipse { Width = 35, Height = 35, Fill = Brushes.Plum, Stroke = Brushes.Purple, StrokeThickness = 2 },
                    "Tetrahedron" => CreateTetrahedron(),
                    _ => new Rectangle { Width = 30, Height = 30, Fill = Brushes.LightGray, Stroke = Brushes.Gray, StrokeThickness = 1 }
                };
            }            if (shapePreview != null)
            {
                if (shapePreview is Shape shape)
                {
                    Canvas.SetLeft(shape, (50 - (shape.Width == 0 ? 40 : shape.Width)) / 2);
                    Canvas.SetTop(shape, (50 - (shape.Height == 0 ? 30 : shape.Height)) / 2);
                }
                else if (shapePreview is Canvas scatterCanvas)
                {
                    Canvas.SetLeft(scatterCanvas, 5);
                    Canvas.SetTop(scatterCanvas, 5);
                }
                canvas.Children.Add(shapePreview);
            }

            var stackPanel = new StackPanel { Orientation = Orientation.Vertical };
            stackPanel.Children.Add(canvas);
            stackPanel.Children.Add(new TextBlock
            {
                Text = GetVietnameseName(shapeName),
                FontSize = 10,
                Foreground = Brushes.DarkGray,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            button.Content = stackPanel;
            button.Click += ShapeButton_Click;
            WireTouchActivation(button, ShapeButton_Click);

            return button;
        }

        private Polygon CreateTriangle()
        {
            return new Polygon
            {
                Points = new PointCollection { new Point(20, 5), new Point(5, 35), new Point(35, 35) },
                Fill = Brushes.LightGreen,
                Stroke = Brushes.DarkGreen,
                StrokeThickness = 2
            };
        }

        private Polygon CreateStar()
        {
            return new Polygon
            {
                Points = new PointCollection
                {
                    new Point(20, 2), new Point(24, 15), new Point(38, 15),
                    new Point(26, 24), new Point(30, 38), new Point(20, 28),
                    new Point(10, 38), new Point(14, 24), new Point(2, 15),
                    new Point(16, 15)
                },
                Fill = Brushes.Gold,
                Stroke = Brushes.DarkGoldenrod,
                StrokeThickness = 1
            };
        }

        private Polygon CreateArrow()
        {
            return new Polygon
            {
                Points = new PointCollection
                {
                    new Point(5, 18), new Point(25, 18), new Point(25, 8),
                    new Point(40, 20), new Point(25, 32), new Point(25, 22),
                    new Point(5, 22)
                },
                Fill = Brushes.LightSalmon,
                Stroke = Brushes.DarkSalmon,
                StrokeThickness = 2
            };
        }

        private Path CreateHeart()
        {
            return new Path
            {
                Data = Geometry.Parse("M12,21.35L10.55,20.03C5.4,15.36 2,12.27 2,8.5C2,5.41 4.42,3 7.5,3C9.24,3 10.91,3.81 12,5.08C13.09,3.81 14.76,3 16.5,3C19.58,3 22,5.41 22,8.5C22,12.27 18.6,15.36 13.45,20.03L12,21.35Z"),
                Fill = Brushes.HotPink,
                Stroke = Brushes.DeepPink,
                StrokeThickness = 1,
                Stretch = Stretch.Uniform,
                Width = 30,
                Height = 30
            };
        }

        private Polygon CreatePentagon()
        {
            var points = new PointCollection();
            double centerX = 20;
            double centerY = 20;
            double radius = 15;
            
            for (int i = 0; i < 5; i++)
            {
                double angle = (i * 72 - 90) * Math.PI / 180;
                points.Add(new Point(
                    centerX + radius * Math.Cos(angle),
                    centerY + radius * Math.Sin(angle)
                ));
            }
            
            return new Polygon
            {
                Points = points,
                Fill = Brushes.Lavender,
                Stroke = Brushes.DarkViolet,
                StrokeThickness = 2
            };
        }

        private Polygon CreateHexagon()
        {
            var points = new PointCollection();
            double centerX = 20;
            double centerY = 20;
            double radius = 15;
            
            for (int i = 0; i < 6; i++)
            {
                double angle = (i * 60 - 90) * Math.PI / 180;
                points.Add(new Point(
                    centerX + radius * Math.Cos(angle),
                    centerY + radius * Math.Sin(angle)
                ));
            }
            
            return new Polygon
            {
                Points = points,
                Fill = Brushes.LightYellow,
                Stroke = Brushes.Orange,
                StrokeThickness = 2
            };
        }

        private Polygon CreatePolygon()
        {
            return new Polygon
            {
                Points = new PointCollection { new Point(10, 5), new Point(30, 10), new Point(35, 30), new Point(15, 35), new Point(5, 20) },
                Fill = Brushes.LightGray,
                Stroke = Brushes.DarkGray,
                StrokeThickness = 2
            };
        }

        private Polygon CreateCube()
        {
            return new Polygon
            {
                Points = new PointCollection 
                { 
                    new Point(8, 15), new Point(22, 15), new Point(22, 29), new Point(8, 29),
                    new Point(8, 15), new Point(14, 9), new Point(28, 9), new Point(28, 23),
                    new Point(22, 29), new Point(28, 23), new Point(28, 9), new Point(22, 15)
                },
                Fill = Brushes.LightBlue,
                Stroke = Brushes.Navy,
                StrokeThickness = 1.5
            };
        }

        private Polygon CreatePyramid()
        {
            return new Polygon
            {
                Points = new PointCollection { new Point(20, 5), new Point(5, 28), new Point(15, 35), new Point(35, 20) },
                Fill = Brushes.SandyBrown,
                Stroke = Brushes.Sienna,
                StrokeThickness = 2
            };
        }

        private Path CreateCylinder()
        {
            return new Path
            {
                Data = Geometry.Parse("M5,10 Q5,5 20,5 Q35,5 35,10 L35,30 Q35,35 20,35 Q5,35 5,30 Z M5,10 Q5,15 20,15 Q35,15 35,10"),
                Fill = Brushes.LightCyan,
                Stroke = Brushes.DarkCyan,
                StrokeThickness = 1.5
            };
        }

        private Polygon CreateCone()
        {
            return new Polygon
            {
                Points = new PointCollection { new Point(20, 5), new Point(5, 35), new Point(35, 35) },
                Fill = Brushes.Coral,
                Stroke = Brushes.OrangeRed,
                StrokeThickness = 2
            };
        }

        private Polygon CreatePrism()
        {
            return new Polygon
            {
                Points = new PointCollection 
                { 
                    new Point(15, 8), new Point(8, 32), new Point(25, 32),
                    new Point(25, 32), new Point(32, 8), new Point(15, 8),
                    new Point(32, 8), new Point(25, 32)
                },
                Fill = Brushes.LightGreen,
                Stroke = Brushes.DarkGreen,
                StrokeThickness = 1.5
            };
        }

        private Polygon CreateTetrahedron()
        {
            return new Polygon
            {
                Points = new PointCollection { new Point(20, 8), new Point(8, 32), new Point(32, 25) },
                Fill = Brushes.Thistle,
                Stroke = Brushes.Purple,
                StrokeThickness = 2
            };
        }

        private Path CreateBarChart()
        {
            return new Path
            {
                Data = Geometry.Parse("M5,20 L5,35 L12,35 L12,20 Z M15,12 L15,35 L22,35 L22,12 Z M25,25 L25,35 L32,35 L32,25 Z"),
                Fill = Brushes.LightSkyBlue,
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 1.5
            };
        }

        private Path CreatePieChart()
        {
            return new Path
            {
                Data = Geometry.Parse("M20,20 L20,5 A15,15 0 0,1 35,20 Z M20,20 L35,20 A15,15 0 0,1 20,35 Z M20,20 L20,35 A15,15 0 0,1 5,20 Z M20,20 L5,20 A15,15 0 0,1 20,5"),
                Fill = Brushes.LightPink,
                Stroke = Brushes.DeepPink,
                StrokeThickness = 1.5
            };
        }

        private Polyline CreateLineChart()
        {
            return new Polyline
            {
                Points = new PointCollection { new Point(5, 30), new Point(12, 15), new Point(20, 25), new Point(28, 10), new Point(35, 20) },
                Stroke = Brushes.Green,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };
        }

        private Polygon CreateAreaChart()
        {
            return new Polygon
            {
                Points = new PointCollection { new Point(5, 35), new Point(5, 30), new Point(12, 15), new Point(20, 25), new Point(28, 10), new Point(35, 20), new Point(35, 35) },
                Fill = Brushes.LightGreen,
                Stroke = Brushes.ForestGreen,
                StrokeThickness = 1.5
            };
        }

        private Canvas CreateScatterChart()
        {
            var canvas = new Canvas { Width = 40, Height = 40 };
            double[][] points = new double[][]
            {
                new double[] { 8, 28 }, new double[] { 12, 15 }, new double[] { 18, 22 },
                new double[] { 22, 10 }, new double[] { 26, 18 }, new double[] { 30, 25 }
            };
            
            foreach (var pt in points)
            {
                canvas.Children.Add(new Ellipse
                {
                    Width = 4,
                    Height = 4,
                    Fill = Brushes.Blue,
                    Margin = new Thickness(pt[0], pt[1], 0, 0)
                });
            }
            
            return canvas;
        }

        private Polygon CreateRadarChart()
        {
            var points = new PointCollection();
            double centerX = 20;
            double centerY = 20;
            double radius = 12;
            
            for (int i = 0; i < 5; i++)
            {
                double angle = (i * 72 - 90) * Math.PI / 180;
                points.Add(new Point(
                    centerX + radius * Math.Cos(angle),
                    centerY + radius * Math.Sin(angle)
                ));
            }
            
            return new Polygon
            {
                Points = points,
                Fill = Brushes.LightSeaGreen,
                Stroke = Brushes.Teal,
                StrokeThickness = 2
            };
        }

    private string GetVietnameseName(string shapeName)
    {
        return shapeName switch
        {
            // PHASE 1: Lines (5)
            "StraightLine" => "Đường thẳng",
            "ArrowLine" => "Đường mũi tên",
            "DashedLine" => "Nét đứt",
            "DoubleArrowLine" => "Mũi tên 2 đầu",
            "ElbowConnector" => "Đường góc vuông",
            "CurvedConnector" => "Đường cong",
            
            // Rectangles / Basic
            "Square" => "Hình vuông",
            "Rectangle" => "Hình chữ nhật",
            "RoundedRectangle" => "HCN bo góc",
            "SnipSingleCornerRectangle" => "HCN cắt 1 góc",
            "SnipDiagonalCornerRectangle" => "HCN cắt 2 góc",
            "RoundSingleCornerRectangle" => "HCN bo 1 góc",
            
            // PHASE 1: Basic Shapes (10)
            "Circle" => "Hình tròn",
            "Triangle" => "Tam giác",
            "Pentagon" => "Ngũ giác",
            "Hexagon" => "Lục giác",
            "Star" => "Ngôi sao",
            "Heart" => "Trái tim",
            "Diamond" => "Hình thoi",
            "Parallelogram" => "Hình bình hành",
            "Cross" => "Chữ thập",
            
            // PHASE 2A: Block Arrows (15)
            "RightArrow" => "Mũi tên phải",
            "LeftArrow" => "Mũi tên trái",
            "UpArrow" => "Mũi tên lên",
            "DownArrow" => "Mũi tên xuống",
            "LeftRightArrow" => "Mũi tên 2 chiều ngang",
            "UpDownArrow" => "Mũi tên 2 chiều dọc",
            "QuadArrow" => "Mũi tên 4 hướng",
            "NotchedRightArrow" => "Mũi tên phải có rãnh",
            "PentagonArrow" => "Mũi tên ngũ giác",
            "ChevronArrow" => "Mũi tên chữ V",
            "StripedRightArrow" => "Mũi tên sọc",
            "CurvedRightArrow" => "Mũi tên cong phải",
            "CurvedLeftArrow" => "Mũi tên cong trái",
            "CurvedUpArrow" => "Mũi tên cong lên",
            "CurvedDownArrow" => "Mũi tên cong xuống",
            
            // PHASE 2B: Stars & Banners (15)
            "Star4" => "Sao 4 cánh",
            "Star6" => "Sao 6 cánh",
            "Star7" => "Sao 7 cánh",
            "Star8" => "Sao 8 cánh",
            "Star10" => "Sao 10 cánh",
            "Star12" => "Sao 12 cánh",
            "Star16" => "Sao 16 cánh",
            "Star24" => "Sao 24 cánh",
            "Star32" => "Sao 32 cánh",
            "Explosion1" => "Vụ nổ 1",
            "Explosion2" => "Vụ nổ 2",
            "Wave" => "Sóng",
            "DoubleWave" => "Sóng kép",
            "Ribbon" => "Băng rôn",
            "Scroll" => "Cuộn giấy",
            
            // PHASE 2C: Flowchart (20)
            "FlowProcess" => "Quy trình",
            "FlowDecision" => "Quyết định",
            "FlowDocument" => "Tài liệu",
            "FlowData" => "Dữ liệu",
            "FlowPredefinedProcess" => "QT định sẵn",
            "FlowInternalStorage" => "Bộ nhớ trong",
            "FlowSequentialData" => "DL tuần tự",
            "FlowDirectData" => "DL trực tiếp",
            "FlowManualInput" => "Nhập tay",
            "FlowManualOperation" => "Thao tác tay",
            "FlowConnector" => "Kết nối",
            "FlowOffPageConnector" => "Kết nối ngoài",
            "FlowSummingJunction" => "Điểm tổng",
            "FlowOr" => "Hoặc",
            "FlowSort" => "Sắp xếp",
            "FlowExtract" => "Trích xuất",
            "FlowMerge" => "Gộp",
            "FlowStoredData" => "DL lưu trữ",
            "FlowDelay" => "Trễ",
            "FlowPreparation" => "Chuẩn bị",
            
            // PHASE 2D: Callouts (14)
            "RectangularCallout" => "Chú thích HCN",
            "RoundedRectangularCallout" => "Chú thích bo góc",
            "OvalCallout" => "Chú thích oval",
            "LineCallout1" => "Chú thích 1 dòng",
            "LineCallout2" => "Chú thích 2 dòng",
            "LineCallout3" => "Chú thích 3 dòng",
            "LineCallout4" => "Chú thích 4 dòng",
            "BorderlessLineCallout1" => "CT không viền 1",
            "BorderlessLineCallout2" => "CT không viền 2",
            "BorderlessLineCallout3" => "CT không viền 3",
            "BorderlessLineCallout4" => "CT không viền 4",
            "AccentCallout1" => "CT nhấn mạnh 1",
            "AccentCallout2" => "CT nhấn mạnh 2",
            "AccentCallout3" => "CT nhấn mạnh 3",
            
            // PHASE 2E: Equation Shapes (15)
            "Plus" => "Cộng",
            "Minus" => "Trừ",
            "Multiply" => "Nhân",
            "Divide" => "Chia",
            "Equal" => "Bằng",
            "NotEqual" => "Không bằng",
            "LessThan" => "Nhỏ hơn",
            "GreaterThan" => "Lớn hơn",
            "LessOrEqual" => "Nhỏ hơn bằng",
            "GreaterOrEqual" => "Lớn hơn bằng",
            "Approximately" => "Xấp xỉ",
            "Infinity" => "Vô cực",
            "Radical" => "Căn bậc hai",
            "Summation" => "Tổng",
            "Integral" => "Tích phân",
            
            // Old shapes
            "Arrow" => "Mũi tên",
            "Line" => "Đường thẳng",
            "Polygon" => "Đa giác",
            "Cube" => "Khối lập phương",
            "Sphere" => "Hình cầu",
            "Pyramid" => "Hình chóp",
            "Cylinder" => "Hình trụ",
            "Cone" => "Hình nón",
            "Prism" => "Lăng trụ",
            "Torus" => "Hình xuyến",
            "Tetrahedron" => "Tứ diện",
            _ => shapeName
        };
    }

        // PHASE 1 NEW SHAPE CREATORS
        
        private Canvas CreateDoubleArrow()
        {
            var canvas = new Canvas { Width = 40, Height = 40 };
            canvas.Children.Add(new Line { X1 = 8, Y1 = 20, X2 = 32, Y2 = 20, Stroke = Brushes.DarkGray, StrokeThickness = 2 });
            canvas.Children.Add(new Polygon { Points = new PointCollection { new Point(8, 15), new Point(3, 20), new Point(8, 25) }, Fill = Brushes.DarkGray });
            canvas.Children.Add(new Polygon { Points = new PointCollection { new Point(32, 15), new Point(37, 20), new Point(32, 25) }, Fill = Brushes.DarkGray });
            return canvas;
        }

        private Path CreateElbowConnector()
        {
            return new Path
            {
                Data = Geometry.Parse("M5,10 L20,10 L20,30 L35,30"),
                Stroke = Brushes.DarkGray,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };
        }

        private Path CreateCurvedConnector()
        {
            return new Path
            {
                Data = Geometry.Parse("M5,20 Q20,5 35,20"),
                Stroke = Brushes.DarkGray,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };
        }

        private Path CreateSnipSingleCorner()
        {
            return new Path
            {
                Data = Geometry.Parse("M5,5 L35,5 L35,10 L30,5 L30,25 L5,25 Z"),
                Fill = Brushes.Orange,
                Stroke = Brushes.DarkOrange,
                StrokeThickness = 1.5
            };
        }

        private Path CreateSnipDiagonalCorner()
        {
            return new Path
            {
                Data = Geometry.Parse("M5,5 L35,5 L35,10 L30,5 L30,25 L10,30 L5,30 L5,25 L10,25 Z"),
                Fill = Brushes.DeepSkyBlue,
                Stroke = Brushes.DarkBlue,
                StrokeThickness = 1.5
            };
        }

        private Path CreateRoundSingleCorner()
        {
            return new Path
            {
                Data = Geometry.Parse("M5,5 L30,5 Q35,5 35,10 L35,25 L5,25 Z"),
                Fill = Brushes.Cyan,
                Stroke = Brushes.DarkCyan,
                StrokeThickness = 1.5
            };
        }

        private Polygon CreateDiamond()
        {
            return new Polygon
            {
                Points = new PointCollection { new Point(20, 5), new Point(35, 20), new Point(20, 35), new Point(5, 20) },
                Fill = Brushes.MediumPurple,
                Stroke = Brushes.Purple,
                StrokeThickness = 2
            };
        }

        private Polygon CreateParallelogram()
        {
            return new Polygon
            {
                Points = new PointCollection { new Point(10, 10), new Point(35, 10), new Point(30, 30), new Point(5, 30) },
                Fill = Brushes.RoyalBlue,
                Stroke = Brushes.DarkBlue,
                StrokeThickness = 2
            };
        }

        private Path CreateCross()
        {
            return new Path
            {
                Data = Geometry.Parse("M15,5 L25,5 L25,15 L35,15 L35,25 L25,25 L25,35 L15,35 L15,25 L5,25 L5,15 L15,15 Z"),
                Fill = Brushes.Red,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 1.5
            };
        }

        // CALLOUT PREVIEW ICONS
        private Polygon CreateRectangularCalloutIcon()
        {
            var polygon = new Polygon();
            polygon.Points.Add(new Point(5, 5));
            polygon.Points.Add(new Point(35, 5));
            polygon.Points.Add(new Point(35, 20));
            polygon.Points.Add(new Point(25, 20));
            polygon.Points.Add(new Point(20, 30));  // Pointer
            polygon.Points.Add(new Point(15, 20));
            polygon.Points.Add(new Point(5, 20));
            polygon.Fill = Brushes.LightYellow;
            polygon.Stroke = Brushes.Orange;
            polygon.StrokeThickness = 1.5;
            return polygon;
        }

        private Polygon CreateRoundedCalloutIcon()
        {
            var polygon = new Polygon();
            polygon.Points.Add(new Point(8, 5));
            polygon.Points.Add(new Point(32, 5));
            polygon.Points.Add(new Point(35, 8));
            polygon.Points.Add(new Point(35, 17));
            polygon.Points.Add(new Point(32, 20));
            polygon.Points.Add(new Point(23, 20));
            polygon.Points.Add(new Point(20, 30));  // Pointer
            polygon.Points.Add(new Point(17, 20));
            polygon.Points.Add(new Point(8, 20));
            polygon.Points.Add(new Point(5, 17));
            polygon.Points.Add(new Point(5, 8));
            polygon.Fill = Brushes.LightYellow;
            polygon.Stroke = Brushes.Orange;
            polygon.StrokeThickness = 1.5;
            return polygon;
        }

        private Polygon CreateOvalCalloutIcon()
        {
            var polygon = new Polygon();
            // Octagon approximation of oval
            polygon.Points.Add(new Point(15, 5));
            polygon.Points.Add(new Point(25, 5));
            polygon.Points.Add(new Point(35, 10));
            polygon.Points.Add(new Point(35, 15));
            polygon.Points.Add(new Point(25, 20));
            polygon.Points.Add(new Point(23, 20));
            polygon.Points.Add(new Point(20, 30));  // Pointer
            polygon.Points.Add(new Point(17, 20));
            polygon.Points.Add(new Point(15, 20));
            polygon.Points.Add(new Point(5, 15));
            polygon.Points.Add(new Point(5, 10));
            polygon.Fill = Brushes.LightYellow;
            polygon.Stroke = Brushes.Orange;
            polygon.StrokeThickness = 1.5;
            return polygon;
        }

        private Polygon CreateCloudCalloutIcon()
        {
            var polygon = new Polygon();
            // Wavy cloud shape
            polygon.Points.Add(new Point(10, 5));
            polygon.Points.Add(new Point(18, 7));
            polygon.Points.Add(new Point(25, 5));
            polygon.Points.Add(new Point(32, 7));
            polygon.Points.Add(new Point(35, 12));
            polygon.Points.Add(new Point(33, 17));
            polygon.Points.Add(new Point(25, 20));
            polygon.Points.Add(new Point(23, 20));
            polygon.Points.Add(new Point(20, 30));  // Pointer
            polygon.Points.Add(new Point(17, 20));
            polygon.Points.Add(new Point(10, 20));
            polygon.Points.Add(new Point(7, 17));
            polygon.Points.Add(new Point(5, 12));
            polygon.Fill = Brushes.LightYellow;
            polygon.Stroke = Brushes.Orange;
            polygon.StrokeThickness = 1.5;
            return polygon;
        }

        private Polygon CreateLineCallout1Icon()
        {
            var polygon = new Polygon();
            polygon.Points.Add(new Point(5, 5));
            polygon.Points.Add(new Point(30, 5));
            polygon.Points.Add(new Point(30, 18));
            polygon.Points.Add(new Point(5, 18));
            polygon.Fill = Brushes.LightYellow;
            polygon.Stroke = Brushes.Orange;
            polygon.StrokeThickness = 1.5;
            return polygon;
        }

        private Polygon CreateLineCallout2Icon()
        {
            var polygon = new Polygon();
            polygon.Points.Add(new Point(5, 5));
            polygon.Points.Add(new Point(28, 5));
            polygon.Points.Add(new Point(28, 18));
            polygon.Points.Add(new Point(5, 18));
            polygon.Fill = Brushes.Wheat;
            polygon.Stroke = Brushes.DarkOrange;
            polygon.StrokeThickness = 1.5;
            return polygon;
        }

        private Polygon CreateLineCallout3Icon()
        {
            var polygon = new Polygon();
            polygon.Points.Add(new Point(5, 5));
            polygon.Points.Add(new Point(26, 5));
            polygon.Points.Add(new Point(26, 15));
            polygon.Points.Add(new Point(5, 15));
            polygon.Fill = Brushes.LightGoldenrodYellow;
            polygon.Stroke = Brushes.DarkOrange;
            polygon.StrokeThickness = 1.5;
            return polygon;
        }

        private Polygon CreateLineCallout4Icon()
        {
            var polygon = new Polygon();
            polygon.Points.Add(new Point(7, 7));
            polygon.Points.Add(new Point(28, 7));
            polygon.Points.Add(new Point(28, 16));
            polygon.Points.Add(new Point(7, 16));
            polygon.Fill = Brushes.LightYellow;
            polygon.Stroke = Brushes.Orange;
            polygon.StrokeThickness = 2;
            return polygon;
        }

        // EQUATION SHAPE PREVIEW ICONS
        private Path CreatePlusIcon()
        {
            var path = new Path
            {
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Fill = Brushes.Black
            };
            
            // Create geometry with EvenOdd fill rule for transparent intersection
            var geometry = new PathGeometry();
            geometry.FillRule = FillRule.EvenOdd;
            
            // Outer rectangle (vertical bar)
            var verticalFigure = new PathFigure { StartPoint = new Point(18, 8) };
            verticalFigure.Segments.Add(new LineSegment(new Point(22, 8), true));
            verticalFigure.Segments.Add(new LineSegment(new Point(22, 32), true));
            verticalFigure.Segments.Add(new LineSegment(new Point(18, 32), true));
            verticalFigure.IsClosed = true;
            geometry.Figures.Add(verticalFigure);
            
            // Horizontal bar
            var horizontalFigure = new PathFigure { StartPoint = new Point(8, 18) };
            horizontalFigure.Segments.Add(new LineSegment(new Point(32, 18), true));
            horizontalFigure.Segments.Add(new LineSegment(new Point(32, 22), true));
            horizontalFigure.Segments.Add(new LineSegment(new Point(8, 22), true));
            horizontalFigure.IsClosed = true;
            geometry.Figures.Add(horizontalFigure);
            
            path.Data = geometry;
            return path;
        }

        private Path CreateMinusIcon()
        {
            return new Path
            {
                Data = Geometry.Parse("M8,18 L32,18 L32,22 L8,22 Z"),
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Fill = Brushes.Black
            };
        }

        private Path CreateMultiplyIcon()
        {
            var path = new Path
            {
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Fill = Brushes.Black
            };
            
            // Create geometry with EvenOdd fill rule for transparent intersection
            var geometry = new PathGeometry();
            geometry.FillRule = FillRule.EvenOdd;
            
            // First diagonal bar (top-left to bottom-right)
            var diagonal1 = new PathFigure { StartPoint = new Point(10, 10) };
            diagonal1.Segments.Add(new LineSegment(new Point(14, 10), true));
            diagonal1.Segments.Add(new LineSegment(new Point(30, 26), true));
            diagonal1.Segments.Add(new LineSegment(new Point(30, 30), true));
            diagonal1.Segments.Add(new LineSegment(new Point(26, 30), true));
            diagonal1.Segments.Add(new LineSegment(new Point(10, 14), true));
            diagonal1.IsClosed = true;
            geometry.Figures.Add(diagonal1);
            
            // Second diagonal bar (top-right to bottom-left)
            var diagonal2 = new PathFigure { StartPoint = new Point(26, 10) };
            diagonal2.Segments.Add(new LineSegment(new Point(30, 10), true));
            diagonal2.Segments.Add(new LineSegment(new Point(30, 14), true));
            diagonal2.Segments.Add(new LineSegment(new Point(14, 30), true));
            diagonal2.Segments.Add(new LineSegment(new Point(10, 30), true));
            diagonal2.Segments.Add(new LineSegment(new Point(10, 26), true));
            diagonal2.IsClosed = true;
            geometry.Figures.Add(diagonal2);
            
            path.Data = geometry;
            return path;
        }

        private Path CreateDivideIcon()
        {
            return new Path
            {
                Data = Geometry.Parse("M5,20 L35,20 M20,10 A2,2 0 1,1 20,14 A2,2 0 1,1 20,10 M20,26 A2,2 0 1,1 20,30 A2,2 0 1,1 20,26"),
                Fill = Brushes.Black,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
        }

        private Path CreateEqualIcon()
        {
            return new Path
            {
                Data = Geometry.Parse("M5,14 L35,14 L35,17 L5,17 Z M5,23 L35,23 L35,26 L5,26 Z"),
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Fill = Brushes.Black  // BUGFIX: Fill for better visibility
            };
        }

        private Path CreateNotEqualIcon()
        {
            var path = new Path
            {
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Fill = Brushes.Black
            };
            
            // Create geometry with EvenOdd fill rule for transparent intersection
            var geometry = new PathGeometry();
            geometry.FillRule = FillRule.EvenOdd;
            
            // First horizontal bar (top)
            var bar1 = new PathFigure { StartPoint = new Point(5, 14) };
            bar1.Segments.Add(new LineSegment(new Point(35, 14), true));
            bar1.Segments.Add(new LineSegment(new Point(35, 17), true));
            bar1.Segments.Add(new LineSegment(new Point(5, 17), true));
            bar1.IsClosed = true;
            geometry.Figures.Add(bar1);
            
            // Second horizontal bar (bottom)
            var bar2 = new PathFigure { StartPoint = new Point(5, 23) };
            bar2.Segments.Add(new LineSegment(new Point(35, 23), true));
            bar2.Segments.Add(new LineSegment(new Point(35, 26), true));
            bar2.Segments.Add(new LineSegment(new Point(5, 26), true));
            bar2.IsClosed = true;
            geometry.Figures.Add(bar2);
            
            // Diagonal slash
            var slash = new PathFigure { StartPoint = new Point(26, 5) };
            slash.Segments.Add(new LineSegment(new Point(30, 5), true));
            slash.Segments.Add(new LineSegment(new Point(14, 35), true));
            slash.Segments.Add(new LineSegment(new Point(10, 35), true));
            slash.IsClosed = true;
            geometry.Figures.Add(slash);
            
            path.Data = geometry;
            return path;
        }

        private Path CreateApproximatelyIcon()
        {
            return new Path
            {
                Data = Geometry.Parse("M5,15 Q12,10 18,15 Q24,20 30,15 M5,25 Q12,20 18,25 Q24,30 30,25"),
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };
        }

        private Path CreateLessThanIcon()
        {
            return new Path
            {
                Data = Geometry.Parse("M30,8 L10,20 L30,32 L34,28 L18,20 L34,12 Z"),
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Fill = Brushes.Black  // BUGFIX: Fill for better visibility
            };
        }

        private Path CreateGreaterThanIcon()
        {
            return new Path
            {
                Data = Geometry.Parse("M10,8 L30,20 L10,32 L6,28 L22,20 L6,12 Z"),
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Fill = Brushes.Black  // BUGFIX: Fill for better visibility
            };
        }

        private Path CreateLessOrEqualIcon()
        {
            return new Path
            {
                Data = Geometry.Parse("M30,5 L10,17 L30,29 Z M8,33 L32,33 L32,36 L8,36 Z"),
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Fill = Brushes.Black  // BUGFIX: Fill with black for consistency
            };
        }

        private Path CreateGreaterOrEqualIcon()
        {
            return new Path
            {
                Data = Geometry.Parse("M10,5 L30,17 L10,29 Z M8,33 L32,33 L32,36 L8,36 Z"),
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Fill = Brushes.Black  // BUGFIX: Fill with black for better visibility
            };
        }

        private Path CreateInfinityIcon()
        {
            return new Path
            {
                Data = Geometry.Parse("M12,20 A8,8 0 1,0 20,20 A8,8 0 1,0 28,20 A8,8 0 1,0 20,20 A8,8 0 1,0 12,20"),
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };
        }

        private Path CreateRadicalIcon()
        {
            return new Path
            {
                Data = Geometry.Parse("M5,20 L12,28 L18,5 L38,5"),
                Stroke = Brushes.Black,
                StrokeThickness = 2.5,
                Fill = Brushes.Transparent
            };
        }

        private Path CreateSummationIcon()
        {
            return new Path
            {
                Data = Geometry.Parse("M30,5 L10,5 L20,20 L10,35 L30,35"),
                Stroke = Brushes.Black,
                StrokeThickness = 2.5,
                Fill = Brushes.Transparent
            };
        }

        private Path CreateIntegralIcon()
        {
            return new Path
            {
                Data = Geometry.Parse("M25,5 Q20,5 20,10 L20,30 Q20,35 15,35"),
                Stroke = Brushes.Black,
                StrokeThickness = 2.5,
                Fill = Brushes.Transparent
            };
        }

    private void ShapeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string shapeName)
        {
            SelectedShape = shapeName;
            UserSelected = true;  // ✅ User selected a shape
            
            // ✅ Safe DialogResult setter
            try
            {
                if (this.IsLoaded)
                {
                    DialogResult = true;
                }
            }
            catch { }
            
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try 
                { 
                    _mainDashboard?.Activate();
                    _mainDashboard?.Focus();
                } 
                catch { }
                this.Close();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    /// <summary>
    /// QC_4.2_TOUCH_PIPELINE: Trực tiếp kích hoạt cảm ứng cho nút bấm trong SubMenu.
    /// Bắt trực tiếp PreviewTouchDown/Up và PreviewStylusDown/Up để đảm bảo 100% cú chạm đầu tiên
    /// kích hoạt hành động ngay lập tức (Zero 2nd tap) trên màn hình tương tác.
    /// </summary>
    private void WireTouchActivation(Button button, RoutedEventHandler clickHandler)
    {
        if (button == null) return;
        button.Focusable = false;
        System.Windows.Input.Stylus.SetIsPressAndHoldEnabled(button, false);

        button.PreviewTouchDown += (s, e) =>
        {
            e.TouchDevice.Capture(button);
            e.Handled = true;
        };

        button.PreviewTouchUp += (s, e) =>
        {
            if (e.TouchDevice.Captured == button)
            {
                button.ReleaseTouchCapture(e.TouchDevice);
                try
                {
                    var pos = e.GetTouchPoint(button).Position;
                    if (pos.X >= 0 && pos.X <= button.ActualWidth &&
                        pos.Y >= 0 && pos.Y <= button.ActualHeight)
                    {
                        clickHandler(button, new RoutedEventArgs(Button.ClickEvent, button));
                    }
                }
                catch
                {
                    clickHandler(button, new RoutedEventArgs(Button.ClickEvent, button));
                }
            }
            e.Handled = true;
        };

        button.PreviewStylusDown += (s, e) =>
        {
            e.StylusDevice.Capture(button);
            e.Handled = true;
        };

        button.PreviewStylusUp += (s, e) =>
        {
            if (e.StylusDevice.Captured == button)
            {
                button.ReleaseStylusCapture();
                try
                {
                    var pos = e.GetPosition(button);
                    if (pos.X >= 0 && pos.X <= button.ActualWidth &&
                        pos.Y >= 0 && pos.Y <= button.ActualHeight)
                    {
                        clickHandler(button, new RoutedEventArgs(Button.ClickEvent, button));
                    }
                }
                catch
                {
                    clickHandler(button, new RoutedEventArgs(Button.ClickEvent, button));
                }
            }
            e.Handled = true;
        };
    }
}
}