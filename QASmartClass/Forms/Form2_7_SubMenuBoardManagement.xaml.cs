using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;
using QASmartTouch.Managers;
using QASmartTouch.Services.VersionManagement;
using QASmartClass.Properties;

namespace QASmartTouch.Forms
{
    public partial class Form2_7_SubMenuBoardManagement : Window
    {
        private int _activeBoardIndex = 1;
        private int _currentBoardCount = 2;
        private const int MAX_BOARDS = 10;
        private Form2_MainDashboard? _mainDashboard;
        private List<Button> _boardButtons = new List<Button>();

        public Form2_7_SubMenuBoardManagement(Form2_MainDashboard? mainDashboard = null)
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
            _mainDashboard = mainDashboard;
            Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RenderBoardButtons();
            ApplyFeatureVisibility();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            try { this.Owner?.Activate(); } catch { }
            this.Close();
        }


        #region Board Navigation Methods

        private void RenderBoardButtons()
        {
            panelBoardButtons.Children.Clear();
            _boardButtons.Clear();

            // Sync with BoardManager if available
            if (_mainDashboard?.BoardManager != null)
            {
                _currentBoardCount = _mainDashboard.BoardManager.BoardCount;
                _activeBoardIndex = _mainDashboard.BoardManager.CurrentBoardIndex + 1; // Convert from 0-based to 1-based
            }

            // Create buttons for existing boards
            for (int i = 1; i <= _currentBoardCount; i++)
            {
                CreateBoardButton(i, i == _activeBoardIndex);
                // Note: CreateBoardButton now adds container to panelBoardButtons internally
            }

            // Add "+" button
            var addButton = CreateAddBoardButton();
            if (_currentBoardCount >= MAX_BOARDS)
            {
                addButton.IsEnabled = false;
                addButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(189, 195, 199)); // Gray
                addButton.Cursor = System.Windows.Input.Cursors.No;
            }
            panelBoardButtons.Children.Add(addButton);

            // Update counter and info
            UpdateBoardCountDisplay();
            UpdateActiveBoardInfo();
        }

        private Button CreateBoardButton(int boardIndex, bool isActive)
        {
            // Create a Grid container to hold button + delete overlay + thumbnail
            var container = new Grid
            {
                Width = 80,
                Height = 90,
                Margin = new Thickness(0, 0, 6, 0)
            };

            // Get board data
            BoardState? board = null;
            if (_mainDashboard?.BoardManager != null && boardIndex <= _mainDashboard.BoardManager.BoardCount)
            {
                board = _mainDashboard.BoardManager.Boards[boardIndex - 1];
            }

            // Create thumbnail preview (if available)
            if (board?.ThumbnailImage != null)
            {
                var thumbnailImage = new Image
                {
                    Source = board.ThumbnailImage,
                    Width = 76,
                    Height = 57,
                    Stretch = Stretch.UniformToFill,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(2, 2, 2, 0)
                };
                
                // Add border around thumbnail
                var thumbnailBorder = new Border
                {
                    Width = 80,
                    Height = 61,
                    BorderBrush = isActive ? new SolidColorBrush(Color.FromRgb(102, 126, 234)) : new SolidColorBrush(Color.FromRgb(223, 228, 234)),
                    BorderThickness = isActive ? new Thickness(2) : new Thickness(1),
                    CornerRadius = new CornerRadius(6, 6, 0, 0),
                    VerticalAlignment = VerticalAlignment.Top,
                    Child = thumbnailImage
                };
                
                container.Children.Add(thumbnailBorder);
                
                // Add object count badge (if > 0)
                if (board.ObjectCount > 0)
                {
                    var badge = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(5, 2, 5, 2),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 4, 4, 0),
                        Child = new TextBlock
                        {
                            Text = board.ObjectCount.ToString(),
                            Foreground = Brushes.White,
                            FontSize = 10,
                            FontWeight = FontWeights.Bold
                        }
                    };
                    
                    container.Children.Add(badge);
                }
                
                // Add hover tooltip with larger preview
                var tooltip = new ToolTip
                {
                    Content = new Image
                    {
                        Source = board.ThumbnailImage,
                        Width = 200,
                        Height = 150,
                        Stretch = Stretch.Uniform
                    },
                    Placement = System.Windows.Controls.Primitives.PlacementMode.Top
                };
                container.ToolTip = tooltip;
            }

            var button = new Button
            {
                Content = boardIndex.ToString(),
                Width = 80,
                Height = 29,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                Tag = boardIndex,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            ApplyBoardButtonStyle(button, isActive);

            button.Click += BoardButton_Click;

            // Add button to container
            container.Children.Add(button);

            // Add delete button overlay (only for inactive boards)
            if (!isActive)
            {
                var deleteButton = CreateDeleteButtonOverlay(boardIndex);
                deleteButton.Visibility = Visibility.Collapsed;
                container.Children.Add(deleteButton);

                // Show/hide delete button on hover
                container.MouseEnter += (s, e) =>
                {
                    deleteButton.Visibility = Visibility.Visible;
                    button.Opacity = 0.8;
                };

                container.MouseLeave += (s, e) =>
                {
                    deleteButton.Visibility = Visibility.Collapsed;
                    button.Opacity = 1.0;
                };
            }
            else
            {
                // Active board - just hover effect, no delete
                container.MouseEnter += (s, e) => button.Opacity = 0.95;
                container.MouseLeave += (s, e) => button.Opacity = 1.0;
            }

            // Store container instead of button
            panelBoardButtons.Children.Add(container);
            _boardButtons.Add(button);

            return button;
        }

        private Button CreateDeleteButtonOverlay(int boardIndex)
        {
            var deleteBtn = new Button
            {
                Width = 18,
                Height = 18,
                Background = new SolidColorBrush(Color.FromRgb(255, 107, 107)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -6, -6, 0),
                Tag = boardIndex.ToString()
            };

            // Create X icon
            var xPath = new Path
            {
                Fill = Brushes.White,
                Data = Geometry.Parse("M19,6.41L17.59,5L12,10.59L6.41,5L5,6.41L10.59,12L5,17.59L6.41,19L12,13.41L17.59,19L19,17.59L13.41,12L19,6.41Z"),
                Width = 10,
                Height = 10,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            deleteBtn.Content = xPath;

            // Template for circular delete button
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(9)); // Circular
            border.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Colors.White));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1.5));

            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(content);
            template.VisualTree = border;
            deleteBtn.Template = template;

            // Click event
            deleteBtn.Click += btnDeleteBoard_Click;

            return deleteBtn;
        }

        private void ApplyBoardButtonStyle(Button button, bool isActive)
        {
            if (isActive)
            {
                // Active style - gradient purple-blue
                var gradientBrush = new System.Windows.Media.LinearGradientBrush();
                gradientBrush.StartPoint = new Point(0, 0);
                gradientBrush.EndPoint = new Point(1, 1);
                gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(102, 126, 234), 0));
                gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(118, 75, 162), 1));

                button.Background = gradientBrush;
                button.Foreground = Brushes.White;
                button.BorderBrush = new SolidColorBrush(Color.FromRgb(102, 126, 234));
                button.BorderThickness = new Thickness(2);
                button.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(102, 126, 234),
                    BlurRadius = 12,
                    ShadowDepth = 4,
                    Opacity = 0.35
                };
                button.RenderTransform = new ScaleTransform(1.08, 1.08);
                button.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            else
            {
                // Inactive style
                button.Background = Brushes.White;
                button.Foreground = new SolidColorBrush(Color.FromRgb(47, 53, 66));
                button.BorderBrush = new SolidColorBrush(Color.FromRgb(223, 228, 234));
                button.BorderThickness = new Thickness(1);
                button.Effect = null;
                button.RenderTransform = new ScaleTransform(1, 1);
                button.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            // Common template
            button.Template = CreateBoardButtonTemplate();
        }

        private ControlTemplate CreateBoardButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));

            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(content);
            template.VisualTree = border;
            return template;
        }

        private Button CreateAddBoardButton()
        {
            var button = new Button
            {
                Content = "+",
                Width = 80,
                Height = 90,
                Margin = new Thickness(0, 0, 0, 0),
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            button.Template = CreateBoardButtonTemplate();
            button.Click += btnNewBoard_Click;

            return button;
        }

        private void UpdateBoardCountDisplay()
        {
            txtBoardCount.Text = $"Bảng ({_currentBoardCount}/{MAX_BOARDS}):";
        }

        private void UpdateActiveBoardInfo()
        {
            int objectCount = 0;
            if (_mainDashboard?.BoardManager != null)
            {
                int index = _activeBoardIndex - 1;
                if (index >= 0 && index < _mainDashboard.BoardManager.BoardCount)
                {
                    objectCount = _mainDashboard.BoardManager.Boards[index].ObjectCount;
                }
            }
            txtActiveBoardInfo.Text = $"Đang hiển thị - {objectCount} đối tượng";
        }

        private DateTime _lastBoardClickTime = DateTime.MinValue;
        private int _lastClickedBoardIndex = -1;
        private static readonly TimeSpan DoubleClickInterval = TimeSpan.FromMilliseconds(300);

        private void BoardButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            int boardIndex = (int)button.Tag;
            
            // Detect double-click
            DateTime currentTime = DateTime.Now;
            bool isDoubleClick = (currentTime - _lastBoardClickTime) <= DoubleClickInterval 
                                && _lastClickedBoardIndex == boardIndex;
            
            _lastBoardClickTime = currentTime;
            _lastClickedBoardIndex = boardIndex;

            if (boardIndex == _activeBoardIndex)
            {
                // If double-click on active board, just close menu
                if (isDoubleClick)
                {
                    this.Close();
                }
                return;
            }

            // Switch actual board via BoardManager
            if (_mainDashboard?.BoardManager != null)
            {
                bool success = _mainDashboard.BoardManager.SwitchBoard(boardIndex - 1); // Convert to 0-based index
                if (success)
                {
                    _activeBoardIndex = boardIndex;
                    RenderBoardButtons();
                    
                    // If double-click, close menu after switching
                    if (isDoubleClick)
                    {
                        this.Close();
                    }
                }
                else
                {
                    MessageBox.Show($"Không thể chuyển sang Bảng {boardIndex}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // Fallback if no BoardManager
                _activeBoardIndex = boardIndex;
                RenderBoardButtons();
                
                // If double-click, close menu
                if (isDoubleClick)
                {
                    this.Close();
                }
            }
        }


        #endregion


        private void btnDeleteBoard_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            string boardNumber = button.Tag?.ToString() ?? "?";
            int boardIndex = int.Parse(boardNumber);

            // Can't delete if only 1 board
            if (_currentBoardCount <= 1)
            {
                MessageBox.Show("Không thể xóa bảng cuối cùng!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Can't delete active board
            if (boardIndex == _activeBoardIndex)
            {
                MessageBox.Show("Không thể xóa bảng đang hiển thị!\nVui lòng chuyển sang bảng khác trước.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa Bảng {boardNumber}?\nDữ liệu sẽ không thể khôi phục.",
                                       "Xác nhận xóa",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Delete actual board via BoardManager
                if (_mainDashboard?.BoardManager != null)
                {
                    bool success = _mainDashboard.BoardManager.DeleteBoard(boardIndex - 1); // Convert to 0-based
                    if (success)
                    {
                        _currentBoardCount = _mainDashboard.BoardManager.BoardCount;
                        _activeBoardIndex = _mainDashboard.BoardManager.CurrentBoardIndex + 1;

                        RenderBoardButtons();
                        UpdateBoardCountDisplay();
                        UpdateActiveBoardInfo();
                    }
                    else
                    {
                        MessageBox.Show(this, $"Không thể xóa Bảng {boardNumber}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // Fallback if no BoardManager
                    _currentBoardCount--;
                    
                    if (boardIndex < _activeBoardIndex)
                    {
                        _activeBoardIndex--;
                    }

                    RenderBoardButtons();
                    UpdateBoardCountDisplay();
                    UpdateActiveBoardInfo();
                }
            }
        }

        private void btnNewBoard_Click(object sender, RoutedEventArgs e)
        {
            if (_currentBoardCount >= MAX_BOARDS)
            {
                MessageBox.Show($"Đã đạt giới hạn tối đa {MAX_BOARDS} bảng!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create actual board via BoardManager
            if (_mainDashboard?.BoardManager != null)
            {
                var newBoard = _mainDashboard.BoardManager.CreateBoard($"Bảng {_currentBoardCount + 1}");
                if (newBoard != null)
                {
                    _currentBoardCount++;
                    // ✅ GIAI ĐOẠN 2: Tự động chuyển ngay sang bảng mới vừa tạo
                    int newIndex = _mainDashboard.BoardManager.BoardCount - 1;
                    _mainDashboard.BoardManager.SwitchBoard(newIndex);
                    _activeBoardIndex = newIndex + 1;

                    RenderBoardButtons();
                    UpdateBoardCountDisplay();
                    UpdateActiveBoardInfo();
                }
                else
                {
                    MessageBox.Show("Không thể tạo bảng mới!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // Fallback if no BoardManager
                _currentBoardCount++;
                _activeBoardIndex = _currentBoardCount;
                RenderBoardButtons();
                UpdateBoardCountDisplay();
                UpdateActiveBoardInfo();
            }
        }

        private void btnDuplicateBoard_Click(object sender, RoutedEventArgs e)
        {
            if (_currentBoardCount >= MAX_BOARDS)
            {
                MessageBox.Show($"Đã đạt giới hạn tối đa {MAX_BOARDS} bảng!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Use BoardManager if available
            if (_mainDashboard?.BoardManager != null)
            {
                var duplicatedBoard = _mainDashboard.BoardManager.DuplicateCurrentBoard();
                
                if (duplicatedBoard != null)
                {
                    _currentBoardCount++;
                    // ✅ GIAI ĐOẠN 2: Tự động chuyển ngay sang bảng nhân bản
                    int newIndex = _mainDashboard.BoardManager.BoardCount - 1;
                    _mainDashboard.BoardManager.SwitchBoard(newIndex);
                    _activeBoardIndex = newIndex + 1;

                    RenderBoardButtons();
                    UpdateBoardCountDisplay();
                    UpdateActiveBoardInfo();
                }
                else
                {
                    MessageBox.Show("Không thể nhân bản bảng", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // Fallback if no BoardManager
                _currentBoardCount++;
                _activeBoardIndex = _currentBoardCount;
                RenderBoardButtons();
                UpdateBoardCountDisplay();
                UpdateActiveBoardInfo();
            }
        }

        private void btnBackgrounds_Click(object sender, RoutedEventArgs e)
        {
            // Show background selection dialog
            var bgSelector = new Form2_7_1_BackgroundSelector();
            
            // Position dialog centered
            bgSelector.Owner = this;
            bgSelector.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            var result = bgSelector.ShowDialog();
            
            if (result == true)
            {
                if (_mainDashboard != null)
                {
                    if (bgSelector.SelectedBackgroundImage != null)
                    {
                        // Apply custom background image
                        _mainDashboard.ApplyCanvasBackgroundImage(
                            bgSelector.SelectedBackgroundImage,
                            bgSelector.SelectedPattern
                        );
                    }
                    else
                    {
                        // Apply color and pattern directly
                        _mainDashboard.ApplyCanvasBackground(
                            bgSelector.SelectedColor,
                            bgSelector.SelectedPattern,
                            bgSelector.LineSpacing,
                            bgSelector.LineOpacity
                        );
                    }
                }
                
                // Đóng sub-menu và đưa bảng chính về tiêu điểm hoạt động (tránh nhảy ra desktop)
                this.Close();
                _mainDashboard?.Activate();
                _mainDashboard?.Focus();
            }
        }

        private void btnMultiUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mainDashboard == null)
                {
                    MessageBox.Show("Không tìm thấy bảng chính!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Open Multi-User Mode dialog
                var multiUserDialog = new Form2_7_4_MultiUserMode();
                multiUserDialog.Owner = Window.GetWindow(_mainDashboard);
                multiUserDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                multiUserDialog.Topmost = true;

                var result = multiUserDialog.ShowDialog();

                if (result == true)
                {
                    if (multiUserDialog.IsMultiUserEnabled)
                    {
                        // Apply multi-user mode to MainDashboard
                        _mainDashboard.EnableMultiUserMode(
                            multiUserDialog.SelectedSplitMode,
                            multiUserDialog.Student1,
                            multiUserDialog.Student2,
                            multiUserDialog.ZoneIsolationEnabled,
                            multiUserDialog.ShowSplitLine
                        );

                        string isolationStatus = multiUserDialog.ZoneIsolationEnabled ? "Bật" : "Tắt";
                        string splitLineStatus = multiUserDialog.ShowSplitLine ? "Hiện" : "Ẩn";

                        MessageBox.Show($"Đã bật chế độ 2 học sinh:\n\n" +
                                      $"• {multiUserDialog.Student1.Name} ({GetColorName(multiUserDialog.Student1.Color)})\n" +
                                      $"• {multiUserDialog.Student2.Name} ({GetColorName(multiUserDialog.Student2.Color)})\n\n" +
                                      $"Chia bảng: {(multiUserDialog.SelectedSplitMode == SplitMode.Vertical ? "Dọc" : "Ngang")}\n" +
                                      $"Cách ly vùng: {isolationStatus}\n" +
                                      $"Đường chia: {splitLineStatus}",
                                      "Chế độ Đa người dùng",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Information);
                    }
                    else
                    {
                        // Disable multi-user mode
                        _mainDashboard.DisableMultiUserMode();

                        MessageBox.Show("Đã tắt chế độ đa người dùng",
                                      "Thông báo",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Information);
                    }

                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}",
                              "Lỗi",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Multi-user error: {ex.Message}");
            }
        }

        private string GetColorName(System.Windows.Media.Color color)
        {
            if (color == System.Windows.Media.Colors.Blue) return "Xanh dương";
            if (color == System.Windows.Media.Colors.Red) return "Đỏ";
            if (color == System.Windows.Media.Colors.Green) return "Xanh lá";
            if (color == System.Windows.Media.Colors.Purple) return "Tím";
            if (color == System.Windows.Media.Colors.Orange) return "Cam";
            if (color == System.Windows.Media.Colors.Gold) return "Vàng";
            if (color == System.Windows.Media.Colors.Brown) return "Nâu";
            if (color == System.Windows.Media.Colors.Black) return "Đen";
            return "Khác";
        }

        private void btnSpotlight_Click(object sender, RoutedEventArgs e)
        {
            // Close board menu first
            this.Close();
            
            // Open magnifier tool starting in spotlight mode
            var magnifierTool = new Form2_7_4_MagnifierTool(false);
            magnifierTool.ShowDialog();
        }


        private void btnCurtain_Click(object sender, RoutedEventArgs e)
        {
            // ✅ Activate Screen Curtain
            var screenCurtain = new Form2_18_ScreenCurtain();
            screenCurtain.Show();
            
            // Close submenu
            this.Close();
        }

        private void btnTimer_Click(object sender, RoutedEventArgs e)
        {
            // ✅ Activate Countdown Timer
            var countdownTimer = new Form2_19_CountdownTimer();
            countdownTimer.Show();
            
            // Close submenu
            this.Close();
        }

        private void btnPointer_Click(object sender, RoutedEventArgs e)
        {
            // Close submenu
            this.Close();
            
            // ✅ Enable Pointer Tool on MainDashboard
            _mainDashboard?.EnablePointerMode();
        }

        // ✨ NEW EVENT HANDLERS

        private void btnScreenshot_Click(object sender, RoutedEventArgs e)
        {
            // Close board menu first
            this.Close();
            
            // Open screenshot region selector
            var screenshotTool = new Form2_7_2_ScreenshotRegion();
            screenshotTool.ShowDialog();
            
            // ✨ FIX: Don't check DialogResult, just check PlaceOnBoard and CapturedImage
            if (screenshotTool.PlaceOnBoard && screenshotTool.CapturedImage != null)
            {
                // Add image to canvas
                _mainDashboard?.AddImageToCanvas(screenshotTool.CapturedImage);
            }
        }

        private void btnPanCanvas_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mainDashboard == null)
                {
                    MessageBox.Show("Không tìm thấy bảng chính!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Enable pan mode on MainDashboard
                _mainDashboard.EnablePanMode();
                
                System.Diagnostics.Debug.WriteLine("✅ Pan mode enabled - drag to scroll");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}",
                              "Lỗi",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Error: {ex.Message}");
            }
            finally
            {
                // Always close the form
                this.Close();
            }
        }

        private void btnResizeCanvas_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Use the stored reference instead of Owner
                if (_mainDashboard == null)
                {
                    MessageBox.Show("Không tìm thấy bảng chính!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    return;
                }

                // Get current canvas size (use ActualWidth/ActualHeight to avoid NaN)
                double currentWidth = _mainDashboard.MainInteractiveBoard.ActualWidth;
                double currentHeight = _mainDashboard.MainInteractiveBoard.ActualHeight;

                // Double the size
                double newWidth = currentWidth * 2;
                double newHeight = currentHeight * 2;

                // Apply new size using SetCanvasSize() to trigger scrollbar auto-update
                _mainDashboard.SetCanvasSize(newWidth, newHeight);

                System.Diagnostics.Debug.WriteLine($"✅ Canvas resized: {currentWidth}x{currentHeight} → {newWidth}x{newHeight}");
                
                // ✨ Re-apply background to cover the new expanded area
                // Use Dispatcher to ensure canvas is fully resized before applying background
                _mainDashboard.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        // Get saved background settings
                        string savedColor = QASmartClass.Properties.Settings.Default.DefaultBackgroundColor ?? "";
                        string savedPattern = QASmartClass.Properties.Settings.Default.DefaultBackgroundPattern ?? "";
                        
                        // Default to Grid pattern if none saved
                        if (string.IsNullOrEmpty(savedPattern))
                        {
                            savedPattern = "Grid";
                        }
                        
                        // Default to Green color if none saved
                        if (string.IsNullOrEmpty(savedColor))
                        {
                            savedColor = "Green";
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"🔍 Applying - Color: '{savedColor}', Pattern: '{savedPattern}'");
                        
                        // Always re-apply background after resize
                        _mainDashboard.ApplyCanvasBackground(savedColor, savedPattern, 40);
                        System.Diagnostics.Debug.WriteLine($"✅ Background re-applied successfully");
                    }
                    catch (Exception bgEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Could not re-apply background: {bgEx.Message}");
                        MessageBox.Show($"Lưu ý: Nền chưa được áp dụng lại.\nVui lòng chọn lại nền từ 'Đổi nền'.",
                                      "Thông báo",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Information);
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
                
                // Show quick notification (optional)
                MessageBox.Show($"Đã mở rộng kích thước bảng gấp đôi:\n{currentWidth:F0} × {currentHeight:F0} px → {newWidth:F0} × {newHeight:F0} px",
                              "Mở rộng thành công",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở rộng bảng: {ex.Message}",
                              "Lỗi",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Resize error: {ex.Message}");
            }
            finally
            {
                // Always close the form, no matter what
                this.Close();
            }
        }

        private void btnMagnifier_Click(object sender, RoutedEventArgs e)
        {
            // Close board menu first
            this.Close();
            
            // Open magnifier tool starting in magnifier mode
            var magnifierTool = new Form2_7_4_MagnifierTool(true);
            magnifierTool.ShowDialog();
        }

        #region Feature Management

        /// <summary>
        /// Apply feature visibility based on VersionDetail.json configuration.
        /// Hides features that are not enabled in current version.
        /// </summary>
        private void ApplyFeatureVisibility()
        {
            try
            {
                // =====================================================
                // BOARD MANAGEMENT FEATURES
                // =====================================================
                
                // NOTE: These buttons don't have x:Name in XAML yet.
                // To enable feature control, add x:Name to buttons in XAML first.
                // Example: <Button x:Name="btnSpotlight" ... />
                
                // Spotlight - disabled in v1.0 (TODO: add x:Name in XAML)
                // btnSpotlight?.SetVisibilityByFeature("spotlight");
                
                // Multi-user mode - advanced feature (TODO: add x:Name in XAML)
                // btnMultiUser?.SetVisibilityByFeature("multi_user");
                
                // Screen Recording - disabled in v1.0
                // btnScreenRecording?.SetVisibilityByFeature("screen_recording");
                
                // =====================================================
                // ENABLED FEATURES (always visible in v1.0)
                // =====================================================
                // - Screen Curtain: enabled
                // - Countdown Timer: enabled
                // - Screenshot: enabled
                // - Magnifier: enabled
                // - Pointer: enabled
                // - Pan Canvas: enabled
                // - Backgrounds: enabled
                
                System.Diagnostics.Debug.WriteLine("[Form2_7_SubMenuBoardManagement] Feature visibility applied successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Form2_7_SubMenuBoardManagement] Error applying feature visibility: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ Đảm bảo khi đóng SubMenu thì MainDashboard luôn được kích hoạt lại trên cùng
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _mainDashboard?.Activate();
            _mainDashboard?.Focus();
        }

        #endregion
    }
}

