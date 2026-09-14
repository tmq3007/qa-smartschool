using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartTouch.Services.VersionManagement;

namespace QASmartTouch.Forms
{
    public partial class Form2_3_SubMenuDrawShapes : Window
    {
        private string currentCategory = "2DGeometry";
        private string currentSubCategory = "Lines"; // For 2D shapes
        private string selectedShape = "";

        public Form2_3_SubMenuDrawShapes()
        {
            InitializeComponent();
            // QC_4.2_TOUCH_PIPELINE: Popup Window — WPF tự cô lập, KHÔNG cần ApplyTouchIsolation
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
                            this.Owner?.Activate();
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
                            this.Owner?.Activate();
                            this.Close();
                        } 
                        catch { }
                    }), System.Windows.Threading.DispatcherPriority.Normal);
                };
            }
            
            // Initialize default state after window loads
            this.Loaded += (s, e) =>
            {
                // Apply feature visibility based on VersionDetail.json
                ApplyFeatureVisibility();
                
                UpdateContentPanel("2DGeometry");
                HighlightCategory(btnCat2DGeometry);
            };
        }

        /// <summary>
        /// Highlight selected category
        /// </summary>
        private void HighlightCategory(Button selectedButton)
        {
            // Reset all category buttons
            btnCat2DGeometry.Background = new SolidColorBrush(Color.FromRgb(241, 242, 246));
            btnCat3DGeometry.Background = new SolidColorBrush(Color.FromRgb(241, 242, 246));
            btnCatChartsTable.Background = new SolidColorBrush(Color.FromRgb(241, 242, 246));
            btnCatFlashcard.Background = new SolidColorBrush(Color.FromRgb(241, 242, 246));

            // Highlight selected
            selectedButton.Background = new SolidColorBrush(Color.FromRgb(46, 134, 222)); // #2E86DE
        }

        /// <summary>
        /// Update content panel based on category
        /// </summary>
        private void UpdateContentPanel(string category)
        {
            switch (category)
            {
                case "2DGeometry":
                    txtCategoryTitle.Text = "Hình học 2D";
                    panel2DShapes.Visibility = Visibility.Visible;
                    panel3DShapes.Visibility = Visibility.Collapsed;
                    panelSubTabs.Visibility = Visibility.Visible; // Show sub-tabs for 2D
                    UpdateSubCategoryPanel("Lines"); // Default to Lines
                    break;

                case "3DGeometry":
                    txtCategoryTitle.Text = "Hình học 3D";
                    panel2DShapes.Visibility = Visibility.Collapsed;
                    panel3DShapes.Visibility = Visibility.Visible;
                    panelSubTabs.Visibility = Visibility.Collapsed; // Hide sub-tabs for 3D
                    break;

                case "ChartsTable":
                    txtCategoryTitle.Text = "Biểu đồ & Bảng";
                    panel2DShapes.Visibility = Visibility.Collapsed;
                    panel3DShapes.Visibility = Visibility.Collapsed;
                    panelSubTabs.Visibility = Visibility.Collapsed;
                    MessageBox.Show("Danh sách biểu đồ & bảng đang được cập nhật", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;

                case "Flashcard":
                    txtCategoryTitle.Text = "Flashcard & Ghi chú";
                    panel2DShapes.Visibility = Visibility.Collapsed;
                    panel3DShapes.Visibility = Visibility.Collapsed;
                    panelSubTabs.Visibility = Visibility.Collapsed;
                    MessageBox.Show("Danh sách flashcard đang được cập nhật", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
            }
        }
        
        /// <summary>
        /// Highlight selected sub-category tab
        /// </summary>
        private void HighlightSubCategory(Button selectedButton)
        {
            // Reset all sub-tab buttons
            btnSubLines.Background = new SolidColorBrush(Color.FromRgb(241, 242, 246));
            btnSubLines.Foreground = new SolidColorBrush(Color.FromRgb(47, 53, 66));
            btnSubRectangles.Background = new SolidColorBrush(Color.FromRgb(241, 242, 246));
            btnSubRectangles.Foreground = new SolidColorBrush(Color.FromRgb(47, 53, 66));
            btnSubBasicShapes.Background = new SolidColorBrush(Color.FromRgb(241, 242, 246));
            btnSubBasicShapes.Foreground = new SolidColorBrush(Color.FromRgb(47, 53, 66));

            // Highlight selected
            selectedButton.Background = new SolidColorBrush(Color.FromRgb(46, 134, 222)); // #2E86DE
            selectedButton.Foreground = new SolidColorBrush(Colors.White);
        }

        /// <summary>
        /// Update sub-category panel for 2D shapes
        /// </summary>
        private void UpdateSubCategoryPanel(string subCategory)
        {
            currentSubCategory = subCategory;
            
            switch (subCategory)
            {
                case "Lines":
                    panelLines.Visibility = Visibility.Visible;
                    panelRectangles.Visibility = Visibility.Collapsed;
                    panelBasicShapes.Visibility = Visibility.Collapsed;
                    break;

                case "Rectangles":
                    panelLines.Visibility = Visibility.Collapsed;
                    panelRectangles.Visibility = Visibility.Visible;
                    panelBasicShapes.Visibility = Visibility.Collapsed;
                    break;

                case "BasicShapes":
                    panelLines.Visibility = Visibility.Collapsed;
                    panelRectangles.Visibility = Visibility.Collapsed;
                    panelBasicShapes.Visibility = Visibility.Visible;
                    break;
            }
        }

        #region Event Handlers

        private void btnCategory_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                currentCategory = button.Tag.ToString();
                HighlightCategory(button);
                UpdateContentPanel(currentCategory);
            }
        }
        
        private void btnSubCategory_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                string subCategory = button.Tag.ToString();
                HighlightSubCategory(button);
                UpdateSubCategoryPanel(subCategory);
            }
        }

        private void btnShape_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                selectedShape = button.Tag.ToString();
                
                // Debug: Show what shape was clicked
                System.Diagnostics.Debug.WriteLine($"Shape clicked: {selectedShape}");
                
                // Open 3D editors for 3D shapes
                switch (selectedShape)
                {
                    case "Cube":
                        {
                            var cubeEditor = new Form2_6_3DCubeEditor();
                            cubeEditor.ShowDialog();
                            if (cubeEditor.IsConfirmed)
                            {
                                // TODO: Add cube to canvas
                                MessageBox.Show("Đã thêm Hình lập phương vào bảng", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        break;

                    case "Sphere":
                        {
                            var sphereEditor = new Form2_6_3DSphereEditor();
                            sphereEditor.ShowDialog();
                            if (sphereEditor.IsConfirmed)
                            {
                                // TODO: Add sphere to canvas
                                MessageBox.Show("Đã thêm Hình cầu vào bảng", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        break;

                    case "Cylinder":
                        {
                            var cylinderEditor = new Form2_6_3DCylinderEditor();
                            cylinderEditor.ShowDialog();
                            if (cylinderEditor.IsConfirmed)
                            {
                                // TODO: Add cylinder to canvas
                                MessageBox.Show("Đã thêm Hình trụ vào bảng", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        break;

                    case "Cone":
                        {
                            var coneEditor = new Form2_6_3DConeEditor();
                            coneEditor.ShowDialog();
                            if (coneEditor.IsConfirmed)
                            {
                                // TODO: Add cone to canvas
                                MessageBox.Show("Đã thêm Hình nón vào bảng", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        break;

                    case "Pyramid":
                        {
                            var pyramidEditor = new Form2_6_3DPyramidEditor();
                            pyramidEditor.ShowDialog();
                            if (pyramidEditor.IsConfirmed)
                            {
                                // TODO: Add pyramid to canvas
                                MessageBox.Show("Đã thêm Hình chóp vào bảng", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        break;

                    case "Prism":
                        {
                            var prismEditor = new Form2_6_3DPrismEditor();
                            prismEditor.ShowDialog();
                            if (prismEditor.IsConfirmed)
                            {
                                // TODO: Add prism to canvas
                                MessageBox.Show("Đã thêm Lăng trụ vào bảng", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        break;

                    // HIDDEN FOR FUTURE VERSION
                    /*
                    case "Torus":
                        {
                            var torusEditor = new Form2_6_3DTorusEditor();
                            torusEditor.ShowDialog();
                            if (torusEditor.IsConfirmed)
                            {
                                // TODO: Add torus to canvas
                                MessageBox.Show("Đã thêm Hình xuyến vào bảng", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        break;

                    case "Tetrahedron":
                        {
                            var tetrahedronEditor = new Form2_7_3DTetrahedronEditor();
                            tetrahedronEditor.ShowDialog();
                            if (tetrahedronEditor.IsConfirmed)
                            {
                                // TODO: Add tetrahedron to canvas
                                MessageBox.Show("Đã thêm Hình tứ diện vào bảng", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        break;
                    */

                    default:
                        // For 2D shapes, show selection message
                        {
                            string shapeName = selectedShape switch
                            {
                                // Lines
                                "StraightLine" => "Đường thẳng",
                                "ArrowLine" => "Đường mũi tên",
                                "DoubleArrowLine" => "Đường mũi tên hai đầu",
                                "ElbowConnector" => "Đường nối góc vuông",
                                "CurvedConnector" => "Đường nối cong",
                                
                                // Rectangles
                                "Rectangle" => "Hình chữ nhật",
                                "RoundedRectangle" => "Hình chữ nhật góc bo tròn",
                                "SnipSingleCornerRectangle" => "Hình chữ nhật cắt 1 góc",
                                "SnipDiagonalCornerRectangle" => "Hình chữ nhật cắt 2 góc chéo",
                                "RoundSingleCornerRectangle" => "Hình chữ nhật bo 1 góc",
                                
                                // Basic Shapes
                                "Circle" => "Hình tròn",
                                "Triangle" => "Tam giác",
                                "Pentagon" => "Ngũ giác",
                                "Hexagon" => "Lục giác",
                                "Star" => "Ngôi sao",
                                "Heart" => "Trái tim",
                                "Diamond" => "Hình thoi",
                                "Parallelogram" => "Hình bình hành",
                                "Cross" => "Hình chữ thập",
                                "Cloud" => "Đám mây",
                                
                                // Old shapes (keep for compatibility)
                                "Square" => "Hình vuông",
                                "ArrowRight" => "Mũi tên phải",
                                "ArrowDown" => "Mũi tên xuống",
                                "Line" => "Đường thẳng",
                                "CurvedLine" => "Đường cong",
                                "DoubleArrow" => "Mũi tên hai đầu",
                                _ => selectedShape
                            };

                            MessageBox.Show(
                                $"Đã chọn: {shapeName}\n\nKéo và thả hình này vào bảng để sử dụng.",
                                "Chọn hình",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                            // TODO: Implement drag-drop functionality for 2D shapes
                            this.Close();
                        }
                        break;
                }
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            try { this.Owner?.Activate(); } catch { }
            this.Close();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Get currently selected category
        /// </summary>
        public string SelectedCategory => currentCategory;

        /// <summary>
        /// Get currently selected shape
        /// </summary>
        public string SelectedShape => selectedShape;

        #endregion

        #region Feature Management

        /// <summary>
        /// Apply feature visibility based on VersionDetail.json configuration.
        /// This method hides/shows shape buttons based on their feature status.
        /// </summary>
        private void ApplyFeatureVisibility()
        {
            try
            {
                // =====================================================
                // CATEGORY BUTTONS - Control entire categories
                // =====================================================
                btnCat2DGeometry?.SetVisibilityByFeature("shapes_2d");
                btnCat3DGeometry?.SetVisibilityByFeature("shapes_3d");
                btnCatChartsTable?.SetVisibilityByFeature("chart_tools");
                
                // =====================================================
                // 2D SHAPES - BASIC SHAPES GROUP (10 shapes)
                // =====================================================
                
                // Shapes that are DISABLED in v1.0 (hidden for future release)
                btnPentagon?.SetVisibilityByFeature("pentagon");
                btnHexagon?.SetVisibilityByFeature("hexagon");
                btnParallelogram?.SetVisibilityByFeature("parallelogram");
                
                // Note: Other 2D shapes are enabled by default in VersionDetail.json
                // - Circle, Triangle, Star, Heart, Diamond, Cross, Cloud: enabled
                // - Lines group (5): StraightLine, ArrowLine, DoubleArrowLine, ElbowConnector, CurvedConnector
                // - Rectangles group (5): Rectangle, RoundedRectangle, SnipSingleCornerRectangle, etc.
                
                // =====================================================
                // 3D SHAPES - ADVANCED SOLIDS GROUP 
                // =====================================================
                
                // Shapes that are DISABLED in v1.0 (hidden for future release)
                btnPyramid?.SetVisibilityByFeature("pyramid");
                btnPrism?.SetVisibilityByFeature("prism");
                
                // Note: Basic 3D shapes are enabled by default
                // - Cube, Sphere, Cylinder, Cone: enabled
                
                // =====================================================
                // CALLOUTS GROUP (in JSON)
                // =====================================================
                // SpeechBubble: enabled
                // ThoughtBubble: disabled
                // CalloutBox: enabled
                
                System.Diagnostics.Debug.WriteLine("[Form2_3_SubMenuDrawShapes] Feature visibility applied successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Form2_3_SubMenuDrawShapes] Error applying feature visibility: {ex.Message}");
            }
        }

        #endregion
    }
}
