using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Controls
{
    public partial class PracticalAppViewer : UserControl
    {
        public PracticalAppViewer()
        {
            InitializeComponent();
        }

        // Sets the practical application items and auto-selects the first item
        public void SetItemsSource(List<PracticalAppItem> items)
        {
            lstPracticalItems.ItemsSource = items;
            if (items != null && items.Count > 0)
            {
                lstPracticalItems.SelectedIndex = 0;
            }
        }

        private void LstPracticalItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstPracticalItems.SelectedItem is PracticalAppItem selectedItem)
            {
                txtDetailTitle.Text = selectedItem.Title;
                txtDetailDesc.Text = selectedItem.Description;

                // Cập nhật Khái niệm sư phạm cốt lõi
                if (panelMathFormula != null && txtMathFormula != null)
                {
                    var formula = GetPropertyValue(selectedItem, "Formula");
                    if (!string.IsNullOrEmpty(formula))
                    {
                        txtMathFormula.Text = formula;
                        panelMathFormula.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        panelMathFormula.Visibility = Visibility.Collapsed;
                    }
                }

                // Cập nhật Phân tích toán học
                if (panelMathAnalysis != null && txtMathAnalysis != null)
                {
                    var analysis = GetPropertyValue(selectedItem, "MathAnalysis");
                    if (!string.IsNullOrEmpty(analysis))
                    {
                        txtMathAnalysis.Text = analysis;
                        panelMathAnalysis.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        panelMathAnalysis.Visibility = Visibility.Collapsed;
                    }
                }

                // Cập nhật Câu hỏi gợi mở
                if (panelDiscussionQuestion != null && txtDiscussionQuestion != null)
                {
                    var question = GetPropertyValue(selectedItem, "DiscussionQuestion");
                    if (!string.IsNullOrEmpty(question))
                    {
                        txtDiscussionQuestion.Text = question;
                        panelDiscussionQuestion.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        panelDiscussionQuestion.Visibility = Visibility.Collapsed;
                    }
                }

                // Cập nhật Nhiệm vụ thực hành
                if (panelPracticeTask != null && txtPracticeTask != null)
                {
                    var task = GetPropertyValue(selectedItem, "InteractiveTask");
                    if (!string.IsNullOrEmpty(task))
                    {
                        txtPracticeTask.Text = task;
                        panelPracticeTask.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        panelPracticeTask.Visibility = Visibility.Collapsed;
                    }
                }

                // Load image asynchronously to prevent UI freezing
                try
                {
                    txtLoadingPlaceholder.Visibility = Visibility.Visible;
                    imgLargePreview.Source = null;
                    borderFallbackCard.Visibility = Visibility.Collapsed;
                    imgLargePreview.Visibility = Visibility.Visible;
                    btnZoomImage.Visibility = Visibility.Collapsed;
                    btnFullscreenImage.Visibility = Visibility.Collapsed;

                    string imagePath = selectedItem.ImagePath;
                    bool existsInResources = ResourceExists(imagePath);
                    bool existsLocally = false;

                    // 1. Support local file fallback if the image is not embedded in resources yet
                    if (!existsInResources &&
                        !string.IsNullOrEmpty(imagePath) && 
                        imagePath.StartsWith("pack://application:,,,", StringComparison.OrdinalIgnoreCase) && 
                        imagePath.Contains(";component/"))
                    {
                        int index = imagePath.IndexOf(";component/");
                        string relativePath = imagePath.Substring(index + ";component/".Length);
                        relativePath = relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar);

                        // Check output directory bin\Debug\...\Assets\Images
                        string localBinPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
                        if (System.IO.File.Exists(localBinPath))
                        {
                            imagePath = localBinPath;
                            existsLocally = true;
                        }
                        else
                        {
                            // Check project source directory Assets\Images
                            string current = AppDomain.CurrentDomain.BaseDirectory;
                            for (int i = 0; i < 5; i++)
                            {
                                string possiblePath = System.IO.Path.Combine(current, relativePath);
                                if (System.IO.File.Exists(possiblePath))
                                {
                                    imagePath = possiblePath;
                                    existsLocally = true;
                                    break;
                                }
                                var parent = System.IO.Directory.GetParent(current);
                                if (parent == null) break;
                                current = parent.FullName;
                            }
                        }
                    }

                    // 2. Gemini folder check & auto-copy (for newly generated/copied assets)
                    if (!existsInResources && !existsLocally && !string.IsNullOrEmpty(imagePath))
                    {
                        string geminiDir = @"C:\Users\DELL\.gemini\antigravity\brain\0bfb4228-f2f8-4de1-8b64-3dd2e404647b";
                        string fileName = System.IO.Path.GetFileName(imagePath) ?? "";
                        Serilog.Log.Information("Gemini check: imagePath={Path}, fileName={File}", imagePath, fileName);
                        
                        string mappedFile = "";
                        if (fileName.Contains("app_limit_4_", StringComparison.OrdinalIgnoreCase))
                            mappedFile = "app_limit_4_carrying_capacity_1782050007301.png";
                        else if (fileName.Contains("app_limit_5_", StringComparison.OrdinalIgnoreCase))
                            mappedFile = "app_limit_5_compound_interest_1782050020817.png";
                        else if (fileName.Contains("app_limit_6_", StringComparison.OrdinalIgnoreCase))
                            mappedFile = "app_limit_6_reactant_limit_1782050036483.png";
                        else if (fileName.Contains("app_vector_7_", StringComparison.OrdinalIgnoreCase))
                            mappedFile = "app_vector_7_1782052579413.png";
                        else if (fileName.Contains("app_vector_8_", StringComparison.OrdinalIgnoreCase))
                            mappedFile = "app_vector_8_1782052593315.png";

                        if (!string.IsNullOrEmpty(mappedFile))
                        {
                            string fullGeminiPath = System.IO.Path.Combine(geminiDir, mappedFile);
                            bool fileExists = System.IO.File.Exists(fullGeminiPath);
                            Serilog.Log.Information("Gemini check: mappedFile={Mapped}, fullPath={FullPath}, exists={Exists}", mappedFile, fullGeminiPath, fileExists);
                            if (fileExists)
                            {
                                imagePath = fullGeminiPath;
                                existsLocally = true; // Mark as resolved locally
                                
                                // Auto-copy to local assets to make it permanent
                                try
                                {
                                    int compIdx = selectedItem.ImagePath.IndexOf(";component/");
                                    if (compIdx >= 0)
                                    {
                                        string relPath = selectedItem.ImagePath.Substring(compIdx + ";component/".Length);
                                        relPath = relPath.Replace('/', System.IO.Path.DirectorySeparatorChar);
                                        
                                        // 1. Copy to bin output directory to make it display immediately
                                        string binDest = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relPath);
                                        string binDestDir = System.IO.Path.GetDirectoryName(binDest);
                                        if (!System.IO.Directory.Exists(binDestDir)) System.IO.Directory.CreateDirectory(binDestDir);
                                        System.IO.File.Copy(fullGeminiPath, binDest, true);
                                        Serilog.Log.Information("Gemini check: copied to binDest={BinDest}", binDest);
                                        
                                        // 2. Copy to project source directory so it persists in next build
                                        string currentSrc = AppDomain.CurrentDomain.BaseDirectory;
                                        for (int i = 0; i < 5; i++)
                                        {
                                            string possibleSrcDir = System.IO.Path.Combine(currentSrc, System.IO.Path.GetDirectoryName(relPath));
                                            if (System.IO.Directory.Exists(possibleSrcDir) && possibleSrcDir.Contains("QASmartClass"))
                                            {
                                                string srcDest = System.IO.Path.Combine(possibleSrcDir, System.IO.Path.GetFileName(relPath));
                                                System.IO.File.Copy(fullGeminiPath, srcDest, true);
                                                Serilog.Log.Information("Gemini check: copied to srcDest={SrcDest}", srcDest);
                                                break;
                                            }
                                            var parent = System.IO.Directory.GetParent(currentSrc);
                                            if (parent == null) break;
                                            currentSrc = parent.FullName;
                                        }
                                    }
                                }
                                catch (Exception copyEx)
                                {
                                    Serilog.Log.Warning("Auto-copy of fallback image failed: {Err}", copyEx.Message);
                                }
                            }
                        }
                    }

                    // 3. Smart Fallback if the image doesn't exist in Resources, local disk, or Gemini
                    if (!existsInResources && !existsLocally)
                    {
                        // Check if it is a missing 7 or 8 image
                        string fileName = System.IO.Path.GetFileName(imagePath);
                        bool isSeven = fileName.Contains("_7_VN") || fileName.Contains("_7_EN");
                        bool isEight = fileName.Contains("_8_VN") || fileName.Contains("_8_EN");

                        if (isSeven || isEight)
                        {
                            string suffix = fileName.Contains("_VN") ? "VN" : "EN";
                            
                            // Check if it belongs to Science category
                            bool isScience = fileName.Contains("constants") || 
                                             fileName.Contains("density") || 
                                             fileName.Contains("boiling") || 
                                             fileName.Contains("circuit") || 
                                             fileName.Contains("electron") || 
                                             fileName.Contains("genetics") || 
                                             fileName.Contains("molecular") || 
                                             fileName.Contains("phscale") || 
                                             fileName.Contains("unitconverter") || 
                                             fileName.Contains("wavespeed");

                            if (isScience)
                            {
                                imagePath = isSeven 
                                    ? $"pack://application:,,,/QASmartClass;component/Assets/Images/app_unitconverter_7_{suffix}.png"
                                    : $"pack://application:,,,/QASmartClass;component/Assets/Images/app_unitconverter_8_{suffix}.png";
                            }
                            else
                            {
                                // Math and others fallback
                                imagePath = isSeven 
                                    ? $"pack://application:,,,/QASmartClass;component/Assets/Images/app_complex_7_{suffix}.png"
                                    : $"pack://application:,,,/QASmartClass;component/Assets/Images/app_complex_8_{suffix}.png";
                            }
                        }
                    }

                    // Check if resolved image is a placeholder or doesn't exist
                    bool isPlaceholder = IsBlacklistedPlaceholder(imagePath);
                    if (isPlaceholder || (!existsInResources && !existsLocally && !System.IO.File.Exists(imagePath)))
                    {
                        txtLoadingPlaceholder.Visibility = Visibility.Collapsed;
                        imgLargePreview.Visibility = Visibility.Collapsed;
                        borderFallbackCard.Visibility = Visibility.Visible;
                        btnZoomImage.Visibility = Visibility.Collapsed;
                        btnFullscreenImage.Visibility = Visibility.Collapsed;

                        txtFallbackIcon.Text = selectedItem.Icon;
                        txtFallbackTitle.Text = selectedItem.Title;

                        string category = GetCategoryFromPath(selectedItem.ImagePath);
                        ApplyCategoryTheme(category);
                        return;
                    }

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    if (!bitmap.IsDownloading)
                    {
                        txtLoadingPlaceholder.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        bitmap.DownloadCompleted += (s, ev) => txtLoadingPlaceholder.Visibility = Visibility.Collapsed;
                    }

                    bitmap.DecodeFailed += (s, ev) => {
                        txtLoadingPlaceholder.Visibility = Visibility.Collapsed;
                        Serilog.Log.Error("Decode image failed: {Path}", selectedItem.ImagePath);
                    };

                    imgLargePreview.Source = bitmap;
                    btnZoomImage.Visibility = Visibility.Visible;
                    btnFullscreenImage.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    txtLoadingPlaceholder.Visibility = Visibility.Collapsed;
                    Serilog.Log.Warning("Error loading image in viewer: {Err}", ex.Message);
                }
            }
        }

        private bool ResourceExists(string resourcePath)
        {
            try
            {
                if (string.IsNullOrEmpty(resourcePath)) return false;
                var uri = new Uri(resourcePath, UriKind.RelativeOrAbsolute);
                var stream = Application.GetResourceStream(uri);
                return stream != null;
            }
            catch
            {
                return false;
            }
        }

        private bool IsBlacklistedPlaceholder(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return true;

            // 1. Check Resource stream length (for embedded resources)
            try
            {
                if (imagePath.StartsWith("pack://application:,,,", StringComparison.OrdinalIgnoreCase))
                {
                    var uri = new Uri(imagePath, UriKind.RelativeOrAbsolute);
                    var streamInfo = Application.GetResourceStream(uri);
                    if (streamInfo != null && streamInfo.Stream != null)
                    {
                        long size = streamInfo.Stream.Length;
                        if (size == 792630 || size == 1020514 || size == 1043166)
                        {
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Fallback to local file checks
            }

            // 2. Check Local File length on disk
            try
            {
                string resolvedPath = imagePath;
                if (imagePath.StartsWith("pack://application:,,,", StringComparison.OrdinalIgnoreCase))
                {
                    int index = imagePath.IndexOf(";component/");
                    if (index >= 0)
                    {
                        string relativePath = imagePath.Substring(index + ";component/".Length);
                        relativePath = relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar);
                        resolvedPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
                    }
                }

                if (System.IO.File.Exists(resolvedPath))
                {
                    var fileInfo = new System.IO.FileInfo(resolvedPath);
                    long size = fileInfo.Length;
                    if (size == 792630 || size == 1020514 || size == 1043166)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Ignore
            }

            return false;
        }

        private string GetCategoryFromPath(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return "Math";
            string fileName = System.IO.Path.GetFileName(imagePath).ToLowerInvariant();

            if (fileName.Contains("boiling") ||
                fileName.Contains("circuit") ||
                fileName.Contains("constants") ||
                fileName.Contains("density") ||
                fileName.Contains("electron") ||
                fileName.Contains("genetics") ||
                fileName.Contains("lens") ||
                fileName.Contains("molecular") ||
                fileName.Contains("phscale") ||
                fileName.Contains("unitconverter") ||
                fileName.Contains("wavespeed"))
            {
                return "Science";
            }

            if (fileName.Contains("grammar") ||
                fileName.Contains("ipa") ||
                fileName.Contains("irregular") ||
                fileName.Contains("vocabulary"))
            {
                return "Language";
            }

            if (fileName.Contains("eisenhower") ||
                fileName.Contains("kanban") ||
                fileName.Contains("fishbone") ||
                fileName.Contains("five_s") ||
                fileName.Contains("fivewhy") ||
                fileName.Contains("five_why") ||
                fileName.Contains("focustimer") ||
                fileName.Contains("formulas") ||
                fileName.Contains("noise") ||
                fileName.Contains("notebook") ||
                fileName.Contains("pareto") ||
                fileName.Contains("pdca") ||
                fileName.Contains("swot") ||
                fileName.Contains("textbook"))
            {
                return "Workplace";
            }

            if (fileName.Contains("literature"))
            {
                return "Literature";
            }

            return "Math"; // Default
        }

        private void ApplyCategoryTheme(string category)
        {
            var brush = new System.Windows.Media.LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(1, 1);

            if (category == "Science")
            {
                brush.GradientStops.Add(new System.Windows.Media.GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#11998e"), 0));
                brush.GradientStops.Add(new System.Windows.Media.GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#38ef7d"), 1));
            }
            else if (category == "Language")
            {
                brush.GradientStops.Add(new System.Windows.Media.GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#833ab4"), 0));
                brush.GradientStops.Add(new System.Windows.Media.GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#fd1d1d"), 0.5));
                brush.GradientStops.Add(new System.Windows.Media.GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#fcb045"), 1));
            }
            else if (category == "Workplace")
            {
                brush.GradientStops.Add(new System.Windows.Media.GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f12711"), 0));
                brush.GradientStops.Add(new System.Windows.Media.GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f5af19"), 1));
            }
            else if (category == "Literature")
            {
                brush.GradientStops.Add(new System.Windows.Media.GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#cb2d3e"), 0));
                brush.GradientStops.Add(new System.Windows.Media.GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ef473a"), 1));
            }
            else
            {
                brush.GradientStops.Add(new System.Windows.Media.GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1A2980"), 0));
                brush.GradientStops.Add(new System.Windows.Media.GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#26D0CE"), 1));
            }

            borderFallbackCard.Background = brush;
        }

        private string GetPropertyValue(object obj, string propName)
        {
            if (obj == null) return "";
            var prop = obj.GetType().GetProperty(propName);
            return prop != null ? (prop.GetValue(obj) as string ?? "") : "";
        }

        private void BtnZoomImage_Click(object sender, RoutedEventArgs e)
        {
            if (imgLargePreview.Source != null && gridFullscreenOverlay != null && imgFullscreen != null)
            {
                imgFullscreen.Source = imgLargePreview.Source;
                txtFullscreenTitle.Text = txtDetailTitle.Text;
                ResetZoom();
                gridFullscreenOverlay.Visibility = Visibility.Visible;
            }
        }

        private void BtnCloseFullscreen_Click(object sender, RoutedEventArgs e)
        {
            if (gridFullscreenOverlay != null)
            {
                gridFullscreenOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void OverlayGrid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (gridFullscreenOverlay != null)
            {
                gridFullscreenOverlay.Visibility = Visibility.Collapsed;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  ZOOM & PAN LOGIC (Widescreen LightBox)
        // ═══════════════════════════════════════════════════════════
        private Point _panStartPoint;
        private double _panOriginX;
        private double _panOriginY;
        private bool _isPanning = false;

        private void ImgContainer_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (imgFullscreen.Source != null && imgContainer != null && fullscreenTranslate != null)
            {
                _panStartPoint = e.GetPosition(imgContainer);
                _panOriginX = fullscreenTranslate.X;
                _panOriginY = fullscreenTranslate.Y;
                imgContainer.CaptureMouse();
                _isPanning = true;
                e.Handled = true; // Prevent closing overlay
            }
        }

        private void ImgContainer_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isPanning && imgContainer != null && imgContainer.IsMouseCaptured && fullscreenTranslate != null)
            {
                Point currentPoint = e.GetPosition(imgContainer);
                double deltaX = currentPoint.X - _panStartPoint.X;
                double deltaY = currentPoint.Y - _panStartPoint.Y;
                
                fullscreenTranslate.X = _panOriginX + deltaX;
                fullscreenTranslate.Y = _panOriginY + deltaY;
            }
        }

        private void ImgContainer_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isPanning && imgContainer != null)
            {
                imgContainer.ReleaseMouseCapture();
                _isPanning = false;
            }
        }

        private void ImgContainer_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (imgFullscreen.Source != null)
            {
                double zoomFactor = e.Delta > 0 ? 1.15 : 0.85;
                ZoomImage(zoomFactor);
                e.Handled = true;
            }
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ZoomImage(1.25);
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ZoomImage(0.8);
        }

        private void ZoomReset_Click(object sender, RoutedEventArgs e)
        {
            ResetZoom();
        }

        private void ZoomImage(double factor)
        {
            if (fullscreenScale != null)
            {
                double newScaleX = fullscreenScale.ScaleX * factor;
                double newScaleY = fullscreenScale.ScaleY * factor;
                
                // Limit scale between 1x and 8x
                if (newScaleX >= 1.0 && newScaleX <= 8.0)
                {
                    fullscreenScale.ScaleX = newScaleX;
                    fullscreenScale.ScaleY = newScaleY;
                }
                else if (newScaleX < 1.0)
                {
                    ResetZoom();
                }
            }
        }

        private void ResetZoom()
        {
            if (fullscreenScale != null && fullscreenTranslate != null)
            {
                fullscreenScale.ScaleX = 1.0;
                fullscreenScale.ScaleY = 1.0;
                fullscreenTranslate.X = 0.0;
                fullscreenTranslate.Y = 0.0;
            }
        }

        private void BtnFullscreenImage_Click(object sender, RoutedEventArgs e)
        {
            if (imgLargePreview.Source != null)
            {
                var fsWin = new FullscreenImageWindow(imgLargePreview.Source, txtDetailTitle.Text);
                fsWin.Owner = Window.GetWindow(this);
                fsWin.ShowDialog();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  TRUE FULLSCREEN BORDERLESS WINDOW (Màn hình rộng trình chiếu)
    // ═══════════════════════════════════════════════════════════
    public class FullscreenImageWindow : Window
    {
        private Image _imgFullscreen;
        private ScaleTransform _scaleTransform;
        private TranslateTransform _translateTransform;
        private Grid _imgContainer;
        private Point _panStartPoint;
        private double _panOriginX;
        private double _panOriginY;
        private bool _isPanning = false;

        public FullscreenImageWindow(System.Windows.Media.ImageSource source, string title)
        {
            this.Title = title;
            this.WindowStyle = WindowStyle.None;
            this.WindowState = WindowState.Maximized;
            this.ResizeMode = ResizeMode.NoResize;
            this.Background = System.Windows.Media.Brushes.Black;
            this.Topmost = true;
            this.ShowInTaskbar = false;

            // Root Grid
            var mainGrid = new Grid();
            
            // Image Container
            _imgContainer = new Grid
            {
                Background = System.Windows.Media.Brushes.Transparent,
                ClipToBounds = true,
                Cursor = System.Windows.Input.Cursors.SizeAll
            };

            // Image
            _imgFullscreen = new Image
            {
                Source = source,
                Stretch = System.Windows.Media.Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };

            // Transforms
            var transformGroup = new TransformGroup();
            _scaleTransform = new ScaleTransform(1, 1);
            _translateTransform = new TranslateTransform(0, 0);
            transformGroup.Children.Add(_scaleTransform);
            transformGroup.Children.Add(_translateTransform);
            _imgFullscreen.RenderTransform = transformGroup;

            _imgContainer.Children.Add(_imgFullscreen);
            mainGrid.Children.Add(_imgContainer);

            // Hook mouse events for pan & zoom
            _imgContainer.MouseDown += ImgContainer_MouseDown;
            _imgContainer.MouseMove += ImgContainer_MouseMove;
            _imgContainer.MouseUp += ImgContainer_MouseUp;
            _imgContainer.MouseWheel += ImgContainer_MouseWheel;

            // Title and Close Button Bar (Top)
            var topBar = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 0, 0, 0)),
                Padding = new Thickness(24, 18, 24, 18),
                VerticalAlignment = VerticalAlignment.Top
            };
            var topDock = new DockPanel();
            
            var closeBtn = new Button
            {
                Content = "✕ Đóng [ESC]",
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(64, 255, 255, 255)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(20, 10, 20, 10),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            closeBtn.Click += (s, e) => this.Close();
            
            var btnStyle = new Style(typeof(Border));
            btnStyle.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(6)));
            closeBtn.Resources.Add(typeof(Border), btnStyle);

            DockPanel.SetDock(closeBtn, Dock.Right);
            topDock.Children.Add(closeBtn);

            var titleTxt = new TextBlock
            {
                Text = title,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            topDock.Children.Add(titleTxt);
            topBar.Child = topDock;
            mainGrid.Children.Add(topBar);

            // Zoom Toolbar (Bottom)
            var bottomBar = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 36),
                CornerRadius = new CornerRadius(20),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 0, 0, 0)),
                Padding = new Thickness(16, 8, 16, 8)
            };
            var shadow = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 8,
                ShadowDepth = 2,
                Opacity = 0.3
            };
            bottomBar.Effect = shadow;

            var toolbarStack = new StackPanel { Orientation = Orientation.Horizontal };
            
            var zoomInBtn = CreateToolbarButton("➕ Phóng to", (s, e) => ZoomImage(1.25));
            var zoomOutBtn = CreateToolbarButton("➖ Thu nhỏ", (s, e) => ZoomImage(0.8));
            var resetBtn = CreateToolbarButton("⟲ Reset", (s, e) => ResetZoom());

            toolbarStack.Children.Add(zoomInBtn);
            toolbarStack.Children.Add(CreateSeparator());
            toolbarStack.Children.Add(zoomOutBtn);
            toolbarStack.Children.Add(CreateSeparator());
            toolbarStack.Children.Add(resetBtn);

            bottomBar.Child = toolbarStack;
            mainGrid.Children.Add(bottomBar);

            this.Content = mainGrid;

            // Handle Esc key to close
            this.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    this.Close();
                }
            };
        }

        private Button CreateToolbarButton(string text, RoutedEventHandler clickHandler)
        {
            var btn = new Button
            {
                Content = text,
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(12, 6, 12, 6),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 13,
                FontWeight = FontWeights.Bold
            };
            btn.Click += clickHandler;
            return btn;
        }

        private Border CreateSeparator()
        {
            return new Border
            {
                Width = 1,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(64, 255, 255, 255)),
                Margin = new Thickness(8, 4, 8, 4)
            };
        }

        // Panning and Zooming handlers
        private void ImgContainer_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _panStartPoint = e.GetPosition(_imgContainer);
            _panOriginX = _translateTransform.X;
            _panOriginY = _translateTransform.Y;
            _imgContainer.CaptureMouse();
            _isPanning = true;
        }

        private void ImgContainer_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isPanning && _imgContainer.IsMouseCaptured)
            {
                Point currentPoint = e.GetPosition(_imgContainer);
                double deltaX = currentPoint.X - _panStartPoint.X;
                double deltaY = currentPoint.Y - _panStartPoint.Y;
                _translateTransform.X = _panOriginX + deltaX;
                _translateTransform.Y = _panOriginY + deltaY;
            }
        }

        private void ImgContainer_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                _imgContainer.ReleaseMouseCapture();
                _isPanning = false;
            }
        }

        private void ImgContainer_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            double zoomFactor = e.Delta > 0 ? 1.15 : 0.85;
            ZoomImage(zoomFactor);
        }

        private void ZoomImage(double factor)
        {
            double newScaleX = _scaleTransform.ScaleX * factor;
            double newScaleY = _scaleTransform.ScaleY * factor;
            if (newScaleX >= 1.0 && newScaleX <= 8.0)
            {
                _scaleTransform.ScaleX = newScaleX;
                _scaleTransform.ScaleY = newScaleY;
            }
            else if (newScaleX < 1.0)
            {
                ResetZoom();
            }
        }

        private void ResetZoom()
        {
            _scaleTransform.ScaleX = 1.0;
            _scaleTransform.ScaleY = 1.0;
            _translateTransform.X = 0.0;
            _translateTransform.Y = 0.0;
        }
    }
}
