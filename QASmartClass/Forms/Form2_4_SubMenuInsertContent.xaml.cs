using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using QASmartTouch.Helpers;
using QASmartTouch.PeriodicTable.Views;
using QASmartTouch.Services.VersionManagement;
using QASmartTouch.Shared;

namespace QASmartTouch.Forms
{
    public partial class Form2_4_SubMenuInsertContent : Window
    {
        public string SelectedInsertType { get; private set; } = "";
        private readonly Form2_MainDashboard _mainDashboard;
        
        public Form2_4_SubMenuInsertContent(Form2_MainDashboard mainDashboard = null)
        {
            InitializeComponent();
            _mainDashboard = mainDashboard;

            // QC_4.2_TOUCH_SCROLL: Kích hoạt vuốt cuộn cảm ứng và kéo thả mượt mà trên IFP
            TouchScrollHelper.AttachToAllScrollViewers(this);

            // QC_4.2_SUBMENU_CLOSE_TOUCH_FIX: Dam bao nut X mau do dong SubMenu 100% tuc thi voi chuot, ngon tay va put Stylus
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

            // QC_4.2_TOUCH_ACTIVATION: Fix "phải nhấn 2 lần" trên màn hình tương tác
            TouchActivationHelper.Apply(this);

            // QC_4.2_TOUCH_PIPELINE: Wire touch activation cho sidebar RadioButtons
            WireTouchActivationRadio(rbCategory1, Category_Changed);
            WireTouchActivationRadio(rbCategory2, Category_Changed);
            WireTouchActivationRadio(rbCategory3, Category_Changed);
            WireTouchActivationRadio(rbCategory4, Category_Changed);
            WireTouchActivationRadio(rbCategory5, Category_Changed);
            WireTouchActivationRadio(rbCategory6, Category_Changed);

            // QC_4.2_TOUCH_PIPELINE: Wire touch activation cho tất cả content Buttons sau khi XAML load xong
            this.Loaded += (s, e) => WireAllInteractiveControls(this);
        }

        /// <summary>
        /// Handle category selection change in sidebar
        /// </summary>
        private void Category_Changed(object sender, RoutedEventArgs e)
        {
            // Null check - controls might not be loaded yet
            if (pnlCategory1 == null) return;

            // Hide all panels
            pnlCategory1.Visibility = Visibility.Collapsed;
            pnlCategory2.Visibility = Visibility.Collapsed;
            pnlCategory3.Visibility = Visibility.Collapsed;
            pnlCategory4.Visibility = Visibility.Collapsed;
            pnlCategory5.Visibility = Visibility.Collapsed;
            pnlCategory6.Visibility = Visibility.Collapsed;

            // Show selected panel
            if (rbCategory1.IsChecked == true)
                pnlCategory1.Visibility = Visibility.Visible;
            else if (rbCategory2.IsChecked == true)
                pnlCategory2.Visibility = Visibility.Visible;
            else if (rbCategory3.IsChecked == true)
                pnlCategory3.Visibility = Visibility.Visible;
            else if (rbCategory4.IsChecked == true)
                pnlCategory4.Visibility = Visibility.Visible;
            else if (rbCategory5.IsChecked == true)
                pnlCategory5.Visibility = Visibility.Visible;
            else if (rbCategory6.IsChecked == true)
                pnlCategory6.Visibility = Visibility.Visible;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            try { this.Owner?.Activate(); } catch { }
            this.Close();
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "HƯỚNG DẪN SỬ DỤNG CHỨC NĂNG CHÈN NỘI DUNG\n\n" +
                "• Bước 1: Chọn một công cụ ở danh sách bên trái (Ví dụ: 4.2 Nội dung cơ bản).\n" +
                "• Bước 2: Nhấp chọn một tính năng trong lưới thẻ bên phải (Hộp văn bản, Bảng, Hình ảnh...).\n" +
                "• Bước 3: Hoàn thành thiết lập hoặc chọn file trong cửa sổ vừa mở và nhấn chèn.\n" +
                "• Bước 4: Đối tượng sẽ được chèn vào chính giữa bảng vẽ. Bạn có thể kéo thả để di chuyển hoặc chỉnh sửa trực tiếp.\n\n" +
                "Chúc bạn có một tiết giảng dạy sinh động cùng QA SmartClass!",
                "Hướng dẫn từng bước",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // TAB 1: DRAWING TOOLS
        private void btnInsertRuler_Click(object sender, RoutedEventArgs e)
        {
            if (_mainDashboard != null)
            {
                _mainDashboard.RegisterRecentTool(
                    "stem:Ruler",
                    "Thước kẻ",
                    QASmartTouch.Services.Tools.RecentToolsManager.RulerIconPath,
                    Color.FromRgb(0xFF, 0x98, 0x00),
                    () => _mainDashboard.OpenRulerTool(),
                    "Dùng lại: Thước kẻ đo chiều dài");
            }
            SelectedInsertType = "Ruler";
            this.Close();
        }

        private void OpenCircleCompassHelper()
        {
            var circleTool = new Form2_19_CircleDrawingTool();
            circleTool.Owner = this.Owner ?? _mainDashboard;
            circleTool.SetMainDashboard(_mainDashboard);
            if (_mainDashboard != null)
            {
                _mainDashboard.ActiveCircleDrawingTool = circleTool;
                circleTool.Closed += (s, args) =>
                {
                    if (_mainDashboard != null && ReferenceEquals(_mainDashboard.ActiveCircleDrawingTool, circleTool))
                    {
                        _mainDashboard.ActiveCircleDrawingTool = null;
                    }
                };
            }
            circleTool.Show();
        }

        private void btnInsertCompass_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "stem:CircleCompass",
                    "Compa",
                    QASmartTouch.Services.Tools.RecentToolsManager.CompassIconPath,
                    Color.FromRgb(0xE9, 0x1E, 0x63),
                    () => OpenCircleCompassHelper(),
                    "Dùng lại: Compa vẽ đường tròn");

                // Ẩn menu trước khi mở công cụ con
                this.Hide();
                OpenCircleCompassHelper();
                this.Close(); // Đóng menu
                try { this.Owner?.Activate(); } catch { }
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở Circle Drawing Tool: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnInsertTriangle_Click(object sender, RoutedEventArgs e)
        {
            if (_mainDashboard != null)
            {
                _mainDashboard.RegisterRecentTool(
                    "stem:SetSquare",
                    "Thước Ê-ke",
                    QASmartTouch.Services.Tools.RecentToolsManager.TriangleToolIconPath,
                    Color.FromRgb(0x4C, 0xAF, 0x50),
                    () => _mainDashboard.OpenSetSquareTool(),
                    "Dùng lại: Thước Ê-ke vuông góc");
            }
            SelectedInsertType = "Triangle";
            this.Close();
        }

        private void btnInsertProtractor_Click(object sender, RoutedEventArgs e)
        {
            if (_mainDashboard != null)
            {
                _mainDashboard.RegisterRecentTool(
                    "stem:Protractor",
                    "Đo độ",
                    QASmartTouch.Services.Tools.RecentToolsManager.ProtractorIconPath,
                    Color.FromRgb(0x9C, 0x27, 0xB0),
                    () => _mainDashboard.OpenProtractorTool(),
                    "Dùng lại: Thước đo góc (Đo độ)");
            }
            SelectedInsertType = "Protractor";
            this.Close();
        }

        private static void LaunchOnScreenKeyboard()
        {
            string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string oskPath = System.IO.Path.Combine(windir, "sysnative", "osk.exe");
            if (!System.IO.File.Exists(oskPath))
            {
                oskPath = System.IO.Path.Combine(windir, "System32", "osk.exe");
            }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = oskPath,
                UseShellExecute = true
            });
        }

        private void btnInsertKeyboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "stem:Keyboard",
                    "Bàn phím ảo",
                    QASmartTouch.Services.Tools.RecentToolsManager.KeyboardIconPath,
                    Color.FromRgb(0x60, 0x7D, 0x8B),
                    () =>
                    {
                        try { LaunchOnScreenKeyboard(); } catch { }
                    },
                    "Dùng lại: Mở bàn phím ảo Windows");

                LaunchOnScreenKeyboard();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở bàn phím ảo:\n{ex.Message}\n\nVui lòng kiểm tra quyền truy cập hệ thống.", 
                              "Lỗi", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ On-Screen Keyboard error: {ex.Message}");
            }
        }

        // TAB 2: SÁCH GIÁO KHOA
        private void btnInsertBook_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "insert:Book",
                    "Sách GK",
                    QASmartTouch.Services.Tools.RecentToolsManager.BookIconPath,
                    Color.FromRgb(0x79, 0x55, 0x48),
                    () =>
                    {
                        var bm = new Form2_20_SubMenuBooks { Owner = _mainDashboard };
                        bm.ShowDialog();
                    },
                    "Dùng lại: Sách giáo khoa điện tử");

                // Ẩn menu trước khi mở dialog con
                this.Hide();
                var booksMenu = new Form2_20_SubMenuBooks();
                booksMenu.Owner = this.Owner; // Set owner to MainDashboard
                booksMenu.ShowDialog();
                this.Close(); // Đóng menu sau khi đóng browser
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở sách giáo khoa: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Books browser error: {ex.Message}");
            }
        }

        // THÍ NGHIỆM VẬT LÝ & STEM (46 Mô phỏng Offline)
        private void btnPhysicsLab_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "insert:PhysicsLab",
                    "TN Vật lý",
                    QASmartTouch.Services.Tools.RecentToolsManager.PhysicsLabIconPath,
                    Color.FromRgb(0x09, 0x84, 0xE3),
                    () =>
                    {
                        var lab = new Form2_23_PhysicsLabWindow();
                        lab.SetMainDashboard(_mainDashboard);
                        WindowHelper.ShowChildDialog(lab, _mainDashboard);
                    },
                    "Dùng lại: Phòng Thí Nghiệm Vật Lý & STEM");

                this.Hide();
                var labWindow = new Form2_23_PhysicsLabWindow();
                if (_mainDashboard != null)
                {
                    labWindow.SetMainDashboard(_mainDashboard);
                }
                WindowHelper.ShowChildDialog(labWindow, _mainDashboard);
                this.Close();
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở Phòng Thí Nghiệm Vật Lý: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Physics lab error: {ex.Message}");
            }
        }

        // TIẾNG ANH (THẺ TỪ VỰNG & PHÁT ÂM)
        private bool _isEnglishCategoriesBuilt = false;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Windows.Media.Imaging.BitmapImage> _englishCardThumbnailCache = new(StringComparer.OrdinalIgnoreCase);

        private void btnEnglishVocab_Click(object sender, RoutedEventArgs e)
        {
            if (pnlEnglishCardsPicker == null) return;

            if (pnlEnglishCardsPicker.Visibility == Visibility.Visible)
            {
                pnlEnglishCardsPicker.Visibility = Visibility.Collapsed;
            }
            else
            {
                pnlEnglishCardsPicker.Visibility = Visibility.Visible;
                EnsureEnglishCategoriesBuilt();
            }
        }

        private void EnsureEnglishCategoriesBuilt()
        {
            if (_isEnglishCategoriesBuilt || EnglishCategoryTabBar == null) return;

            EnglishCategoryTabBar.Children.Clear();
            var subject = System.Linq.Enumerable.FirstOrDefault(
                QASmartTouch.Services.Tools.SubjectShapeGeometryService.Subjects,
                s => s.Id.Equals("eng", StringComparison.OrdinalIgnoreCase));

            if (subject == null || subject.Categories.Count == 0) return;

            Button? firstBtn = null;
            foreach (var cat in subject.Categories)
            {
                var catBtn = new Button
                {
                    Content = cat.Name,
                    Tag = cat,
                    Height = 28,
                    Margin = new Thickness(2, 1, 2, 1),
                    Padding = new Thickness(10, 0, 10, 0),
                    FontSize = 11.5,
                    FontWeight = FontWeights.Medium,
                    Cursor = Cursors.Hand,
                    Focusable = false,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F2F6")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DFE4EA")),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#57606F"))
                };
                Stylus.SetIsPressAndHoldEnabled(catBtn, false);

                var template = new ControlTemplate(typeof(Button));
                var borderFactory = new FrameworkElementFactory(typeof(Border));
                borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
                borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
                borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
                borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(13));
                borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));
                var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
                contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                borderFactory.AppendChild(contentFactory);
                template.VisualTree = borderFactory;
                catBtn.Template = template;

                catBtn.Click += EnglishCategoryButton_Click;
                TouchActivationHelper.WireButton(catBtn);
                EnglishCategoryTabBar.Children.Add(catBtn);

                if (firstBtn == null) firstBtn = catBtn;
            }

            _isEnglishCategoriesBuilt = true;
            if (firstBtn != null)
            {
                ActivateEnglishCategoryButton(firstBtn);
            }
        }

        private void EnglishCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                ActivateEnglishCategoryButton(btn);
            }
        }

        private void ActivateEnglishCategoryButton(Button activeBtn)
        {
            if (EnglishCategoryTabBar == null || EnglishCardGallery == null) return;

            foreach (var child in EnglishCategoryTabBar.Children)
            {
                if (child is Button b)
                {
                    b.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F2F6"));
                    b.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DFE4EA"));
                    b.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#57606F"));
                    b.FontWeight = FontWeights.Medium;
                }
            }

            activeBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E86DE"));
            activeBtn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E86DE"));
            activeBtn.Foreground = Brushes.White;
            activeBtn.FontWeight = FontWeights.SemiBold;

            EnglishCardGallery.Children.Clear();
            if (activeBtn.Tag is QASmartTouch.Services.Tools.CategoryDefinition cat &&
                QASmartTouch.Services.Tools.SubjectShapeGeometryService.EnglishCards.TryGetValue(cat.Id, out var cards))
            {
                foreach (var card in cards)
                {
                    var cardBtn = CreateEnglishCardButton(card);
                    EnglishCardGallery.Children.Add(cardBtn);
                }
            }
        }

        private static string? GetEnglishCardImagePath(string word)
        {
            string safeName = word.Replace(' ', '_') + ".png";
            string p1 = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "EnglishCards", safeName);
            if (System.IO.File.Exists(p1)) return p1;

            string p2 = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "QASmartClass", "Assets", "EnglishCards", safeName);
            if (System.IO.File.Exists(p2)) return p2;

            string p3 = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Assets", "EnglishCards", safeName);
            if (System.IO.File.Exists(p3)) return p3;

            return null;
        }

        private Button CreateEnglishCardButton(QASmartTouch.Services.Tools.EnglishCardDefinition card)
        {
            var button = new Button
            {
                Tag = card,
                Width = 98,
                Height = 114,
                Margin = new Thickness(4),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E1E8ED")),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Focusable = false,
                ToolTip = $"{card.Word} — {card.Meaning} (Bấm để nghe đọc và chèn lên bảng)"
            };
            Stylus.SetIsPressAndHoldEnabled(button, false);

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(4));
            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentFactory);
            template.VisualTree = borderFactory;
            button.Template = template;

            string? imgPath = GetEnglishCardImagePath(card.Word);
            if (!string.IsNullOrEmpty(imgPath))
            {
                try
                {
                    if (!_englishCardThumbnailCache.TryGetValue(imgPath, out var bitmap))
                    {
                        bitmap = new System.Windows.Media.Imaging.BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(imgPath, UriKind.Absolute);
                        bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bitmap.DecodePixelWidth = 190;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        _englishCardThumbnailCache[imgPath] = bitmap;
                    }

                    var img = new Image
                    {
                        Source = bitmap,
                        Stretch = Stretch.Uniform,
                        Width = 88,
                        Height = 104,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                    button.Content = img;
                    button.Click += EnglishCardButton_Click;
                    TouchActivationHelper.WireButton(button);
                    return button;
                }
                catch { }
            }

            var stackPanel = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Center };
            stackPanel.Children.Add(new TextBlock
            {
                Text = card.Emoji,
                FontSize = 34,
                FontFamily = new FontFamily("Segoe UI Emoji, Noto Color Emoji, Arial"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 1)
            });
            stackPanel.Children.Add(new TextBlock
            {
                Text = card.Word,
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F3A5A")),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 88
            });
            stackPanel.Children.Add(new TextBlock
            {
                Text = card.Meaning,
                FontSize = 10,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F8C8D")),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 88,
                Margin = new Thickness(0, 1, 0, 0)
            });

            button.Content = stackPanel;
            button.Click += EnglishCardButton_Click;
            TouchActivationHelper.WireButton(button);
            return button;
        }

        private void EnglishCardButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is QASmartTouch.Services.Tools.EnglishCardDefinition card)
            {
                var capturedCard = card;
                string? imgPath = GetEnglishCardImagePath(capturedCard.Word);
                _mainDashboard?.RegisterRecentTool(
                    $"eng:{capturedCard.Word}",
                    $"Từ: {capturedCard.Word}",
                    QASmartTouch.Services.Tools.RecentToolsManager.BookIconPath,
                    Color.FromRgb(0x8E, 0x24, 0xAA),
                    () =>
                    {
                        QASmartTouch.Services.Tools.SubjectShapeGeometryService.SpeakWord(capturedCard.Word);
                        _mainDashboard?.InsertEnglishWordCard(capturedCard.Emoji, capturedCard.Word, capturedCard.Meaning, imgPath);
                    },
                    $"Dùng lại thẻ từ vựng: {capturedCard.Word} ({capturedCard.Meaning})");

                QASmartTouch.Services.Tools.SubjectShapeGeometryService.SpeakWord(card.Word);
                _mainDashboard?.InsertEnglishWordCard(card.Emoji, card.Word, card.Meaning, imgPath);
                try { this.Owner?.Activate(); } catch { }
                this.Close();
            }
        }

        // TAB 3: TEXT
        private void btnInsertText_Click(object sender, RoutedEventArgs e)
        {
            btnInsertTextBox_Click(sender, e);
        }

        private const int MATRIX_ROWS = 6;
        private const int MATRIX_COLS = 8;
        private Border[,] _matrixCells = new Border[MATRIX_ROWS, MATRIX_COLS];
        private bool _isMatrixBuilt = false;
        private int _selectedMatrixRows = 3;
        private int _selectedMatrixCols = 3;

        private void btnInsertTable_Click(object sender, RoutedEventArgs e)
        {
            if (pnlTableQuickPicker.Visibility == Visibility.Visible)
            {
                pnlTableQuickPicker.Visibility = Visibility.Collapsed;
            }
            else
            {
                pnlTableQuickPicker.Visibility = Visibility.Visible;
                EnsureTableMatrixBuilt();
                HighlightMatrix(3, 3);
            }
        }

        private void EnsureTableMatrixBuilt()
        {
            if (_isMatrixBuilt) return;

            gridTableMatrix.RowDefinitions.Clear();
            gridTableMatrix.ColumnDefinitions.Clear();
            gridTableMatrix.Children.Clear();

            for (int r = 0; r < MATRIX_ROWS; r++)
            {
                gridTableMatrix.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
            }
            for (int c = 0; c < MATRIX_COLS; c++)
            {
                gridTableMatrix.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            }

            for (int r = 0; r < MATRIX_ROWS; r++)
            {
                for (int c = 0; c < MATRIX_COLS; c++)
                {
                    int row = r + 1;
                    int col = c + 1;

                    var cell = new Border
                    {
                        Width = 26,
                        Height = 24,
                        Margin = new Thickness(2),
                        Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(3),
                        Cursor = Cursors.Hand,
                        Tag = new Point(row, col)
                    };

                    cell.MouseEnter += (s, e) =>
                    {
                        HighlightMatrix(row, col);
                    };

                    cell.MouseLeftButtonUp += (s, e) =>
                    {
                        InsertTableWithSize(row, col);
                    };

                    cell.PreviewTouchDown += (s, e) =>
                    {
                        HighlightMatrix(row, col);
                    };

                    cell.PreviewTouchUp += (s, e) =>
                    {
                        InsertTableWithSize(row, col);
                    };

                    Grid.SetRow(cell, r);
                    Grid.SetColumn(cell, c);
                    gridTableMatrix.Children.Add(cell);
                    _matrixCells[r, c] = cell;
                }
            }

            _isMatrixBuilt = true;
        }

        private void HighlightMatrix(int targetRows, int targetCols)
        {
            _selectedMatrixRows = Math.Max(1, Math.Min(MATRIX_ROWS, targetRows));
            _selectedMatrixCols = Math.Max(1, Math.Min(MATRIX_COLS, targetCols));

            txtTableMatrixLabel.Text = $" ({_selectedMatrixRows} hàng x {_selectedMatrixCols} cột)";

            var activeBg = new SolidColorBrush(Color.FromRgb(59, 130, 246)); // #3B82F6 Blue
            var activeBorder = new SolidColorBrush(Color.FromRgb(29, 78, 216)); // #1D4ED8
            var inactiveBg = new SolidColorBrush(Color.FromRgb(241, 245, 249)); // #F1F5F9
            var inactiveBorder = new SolidColorBrush(Color.FromRgb(203, 213, 225)); // #CBD5E1

            for (int r = 0; r < MATRIX_ROWS; r++)
            {
                for (int c = 0; c < MATRIX_COLS; c++)
                {
                    if (_matrixCells[r, c] != null)
                    {
                        if (r < _selectedMatrixRows && c < _selectedMatrixCols)
                        {
                            _matrixCells[r, c].Background = activeBg;
                            _matrixCells[r, c].BorderBrush = activeBorder;
                        }
                        else
                        {
                            _matrixCells[r, c].Background = inactiveBg;
                            _matrixCells[r, c].BorderBrush = inactiveBorder;
                        }
                    }
                }
            }
        }

        private void InsertTableWithSize(int rows, int cols)
        {
            try
            {
                int r = rows;
                int c = cols;
                _mainDashboard?.RegisterRecentTool(
                    $"insert:Table:{r}x{c}",
                    $"Bảng {r}x{c}",
                    QASmartTouch.Services.Tools.RecentToolsManager.TableIconPath,
                    Color.FromRgb(0x9C, 0x27, 0xB0),
                    () => _mainDashboard?.InsertTableToCanvas(new QASmartTouch.Models.TableData(r, c)),
                    $"Dùng lại: Chèn bảng {r} hàng x {c} cột");

                var tableData = new QASmartTouch.Models.TableData(rows, cols);
                _mainDashboard?.InsertTableToCanvas(tableData);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo bảng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCloseTableQuickPicker_Click(object sender, RoutedEventArgs e)
        {
            pnlTableQuickPicker.Visibility = Visibility.Collapsed;
        }

        private void BtnTablePreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tagStr)
            {
                var parts = tagStr.Split(',');
                if (parts.Length == 2 && int.TryParse(parts[0], out int r) && int.TryParse(parts[1], out int c))
                {
                    InsertTableWithSize(r, c);
                }
            }
        }

        private void BtnTableDragToDraw_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            _mainDashboard?.EnableTableDrawingMode();
        }

        private void BtnTableAdvanced_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide();
                var tableDialog = new Form2_TableEditorDialog();
                if (_mainDashboard != null)
                {
                    tableDialog.Owner = _mainDashboard;
                }
                bool? result = tableDialog.ShowDialog();
                if (result == true && tableDialog.ResultTableData != null)
                {
                    _mainDashboard?.InsertTableToCanvas(tableDialog.ResultTableData);
                    this.Close();
                }
                else
                {
                    this.Show();
                }
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở tùy chỉnh bảng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnInsertTextBox_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "insert:SmartNote",
                    "Thẻ ghi chú",
                    QASmartTouch.Services.Tools.RecentToolsManager.NoteIconPath,
                    Color.FromRgb(0xF5, 0x9E, 0x0B),
                    () =>
                    {
                        var dlg = new Form2_SmartNoteDialog();
                        if (WindowHelper.ShowChildDialog(dlg, _mainDashboard) == true && dlg.IsConfirmed && dlg.Model != null)
                        {
                            _mainDashboard?.InsertSmartNoteCardToBoard(dlg.Model);
                        }
                    },
                    "Dùng lại: Thêm Thẻ ghi chú / Hộp văn bản");

                this.Hide();
                var noteDialog = new Form2_SmartNoteDialog();
                bool? result = WindowHelper.ShowChildDialog(noteDialog, _mainDashboard);
                if (result == true && noteDialog.IsConfirmed && noteDialog.Model != null)
                {
                    _mainDashboard?.InsertSmartNoteCardToBoard(noteDialog.Model);
                }
                this.Close();
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở hộp thoại văn bản: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnInsertStickyNote_Click(object sender, RoutedEventArgs e)
        {
            SelectedInsertType = "StickyNote";
            this.Close();
        }

        // TAB 4: IMAGE
        private void btnInsertImageFile_Click(object sender, RoutedEventArgs e)
        {
            SelectedInsertType = "Image";
            this.Close();
        }

        private void btnInsertImageCamera_Click(object sender, RoutedEventArgs e)
        {
            SelectedInsertType = "Camera";
            this.Close();
        }

        private void btnInsertImageLibrary_Click(object sender, RoutedEventArgs e)
        {
            SelectedInsertType = "ImageLibrary";
            this.Close();
        }

        private void btnInsert3DModel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "insert:3DModel",
                    "Mô hình 3D",
                    QASmartTouch.Services.Tools.RecentToolsManager.Cube3DIconPath,
                    Color.FromRgb(0x00, 0xBC, 0xD4),
                    () =>
                    {
                        var ms = new Form2_20_3DModelSelector { Owner = _mainDashboard };
                        if (ms.ShowDialog() == true && !string.IsNullOrEmpty(ms.SelectedModelPath) && ms.SelectedModel != null)
                        {
                            _mainDashboard?.Insert3DModelToCanvas(ms.SelectedModelPath, ms.SelectedModel);
                        }
                    },
                    "Dùng lại: Chèn Mô hình 3D (OBJ/STL)");

                // Ẩn menu trước khi mở dialog con để không bị chồng cửa sổ
                this.Hide();
                var modelSelector = new Form2_20_3DModelSelector();
                modelSelector.Owner = this.Owner; // Set owner to MainDashboard
                bool? result = modelSelector.ShowDialog();
                
                if (result == true && !string.IsNullOrEmpty(modelSelector.SelectedModelPath) && modelSelector.SelectedModel != null)
                {
                    // Insert 3D model to canvas
                    _mainDashboard?.Insert3DModelToCanvas(modelSelector.SelectedModelPath, modelSelector.SelectedModel);
                    System.Diagnostics.Debug.WriteLine($"✅ 3D Model inserted: {modelSelector.SelectedModelPath}");
                }
                
                this.Close(); // Close menu after closing selector
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở trình chọn mô hình 3D: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ 3D Model selector error: {ex.Message}");
            }
        }

        private void btnOpenYouTube_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsVisible) return;
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "insert:YouTube",
                    "YouTube",
                    QASmartTouch.Services.Tools.RecentToolsManager.VideoIconPath,
                    Color.FromRgb(0xFF, 0x00, 0x00),
                    () =>
                    {
                        var yt = new Form2_23_YouTubeBrowser { Topmost = true };
                        yt.Show();
                    },
                    "Dùng lại: Mở YouTube giáo dục");

                // Ẩn menu trước khi mở YouTube browser
                this.Hide();
                var youtubeBrowser = new Form2_23_YouTubeBrowser();
                youtubeBrowser.Topmost = true;
                youtubeBrowser.Show();
                this.Close(); // Close menu after closing YouTube browser
                
                System.Diagnostics.Debug.WriteLine("✅ YouTube browser opened");
            }
            catch (Exception ex)
            {
                try { this.Show(); } catch { }
                MessageBox.Show($"Lỗi khi mở YouTube:\n{ex.Message}", 
                              "Lỗi", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ YouTube browser error: {ex.Message}");
            }
        }

        private void btnWikipedia_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsVisible) return;
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "insert:Wikipedia",
                    "Wikipedia",
                    QASmartTouch.Services.Tools.RecentToolsManager.BookIconPath,
                    Color.FromRgb(0x2F, 0x35, 0x42),
                    () =>
                    {
                        var wk = new Form2_25_WikipediaBrowser { Topmost = true };
                        wk.Show();
                    },
                    "Dùng lại: Mở Bách khoa toàn thư Wikipedia");

                // Ẩn menu trước khi mở Wikipedia
                this.Hide();
                var wikipediaBrowser = new Form2_25_WikipediaBrowser();
                wikipediaBrowser.Topmost = true;
                wikipediaBrowser.Show();
                this.Close(); // Close menu after closing Wikipedia browser
                
                System.Diagnostics.Debug.WriteLine("✅ Wikipedia browser opened");
            }
            catch (Exception ex)
            {
                try { this.Show(); } catch { }
                MessageBox.Show($"Lỗi khi mở Wikipedia:\n{ex.Message}", 
                              "Lỗi", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Wikipedia browser error: {ex.Message}");
            }
        }

        private void btnGoogleSearch_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsVisible) return;
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "insert:GoogleSearch",
                    "Tìm Google",
                    QASmartTouch.Services.Tools.RecentToolsManager.OcrIconPath,
                    Color.FromRgb(0x42, 0x85, 0xF4),
                    () =>
                    {
                        var gs = new Form2_26_GoogleSearchBrowser { Topmost = true };
                        gs.Show();
                    },
                    "Dùng lại: Mở Tìm kiếm Google");

                // Ẩn menu trước khi mở Google Search
                this.Hide();
                var googleSearchBrowser = new Form2_26_GoogleSearchBrowser();
                googleSearchBrowser.Topmost = true;
                googleSearchBrowser.Show();
                this.Close(); // Close menu after closing Google Search browser
                
                System.Diagnostics.Debug.WriteLine("✅ Google Search browser opened");
            }
            catch (Exception ex)
            {
                try { this.Show(); } catch { }
                MessageBox.Show($"Lỗi khi mở Google Search:\n{ex.Message}", 
                              "Lỗi", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Google Search browser error: {ex.Message}");
            }
        }

        private void btnBingTranslator_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsVisible) return;
            try
            {
                // Ẩn menu trước khi mở Bing Translator
                this.Hide();
                var bingTranslator = new Form2_24_BingTranslator();
                WindowHelper.ShowChildDialog(bingTranslator, _mainDashboard);
                this.Close(); // Close menu after closing Bing Translator
                
                System.Diagnostics.Debug.WriteLine("✅ Bing Translator opened");
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi khi mở Bing Translator:\n{ex.Message}", 
                              "Lỗi", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Bing Translator error: {ex.Message}");
            }
        }

        private void btnLichAmDuong_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ẩn menu trước khi mở Lịch Âm Dương
                this.Hide();
                var lichAmDuong = new Form2_27_LichAmDuong();
                WindowHelper.ShowChildDialog(lichAmDuong, _mainDashboard);
                this.Close(); // Close menu after closing Lich Am Duong
                
                System.Diagnostics.Debug.WriteLine("✅ Lich Am Duong opened");
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi khi mở Lịch Âm Dương:\n{ex.Message}", 
                              "Lỗi", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Lich Am Duong error: {ex.Message}");
            }
        }

        private void btnWorldClock_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ẩn menu trước khi mở World Clock
                this.Hide();
                var worldClock = new Form2_28_WorldClock();
                WindowHelper.ShowChildDialog(worldClock, _mainDashboard);
                this.Close(); // Close menu after closing World Clock
                
                System.Diagnostics.Debug.WriteLine("✅ World Clock opened");
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi khi mở Thời Gian Thực:\n{ex.Message}", 
                              "Lỗi", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ World Clock error: {ex.Message}");
            }
        }

        // TAB 5: VIDEO
        private void btnInsertVideo_Click(object sender, RoutedEventArgs e)
        {
            SelectedInsertType = "Video";
            this.Close();
        }


        private void btnInsertYouTube_Click(object sender, RoutedEventArgs e)
        {
            SelectedInsertType = "YouTube";
            this.Close();
        }

        // TAB 6: CHARTS - Open chart editors directly
        private void btnChartBar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "chart:Bar",
                    "Biểu đồ cột",
                    QASmartTouch.Services.Tools.RecentToolsManager.BarChartIconPath,
                    Color.FromRgb(0x21, 0x96, 0xF3),
                    () => WindowHelper.ShowChildDialog(new Form2_8_BarChartEditor(_mainDashboard), _mainDashboard),
                    "Dùng lại: Biểu đồ cột (Bar Chart)");

                // Ẩn menu trước khi mở biểu đồ cột
                this.Hide();
                var chartEditor = new Form2_8_BarChartEditor(_mainDashboard);
                WindowHelper.ShowChildDialog(chartEditor, _mainDashboard);
                this.Close(); // Đóng menu sau khi đóng form biểu đồ
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở biểu đồ cột: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnChartLine_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "chart:Line",
                    "Biểu đồ đường",
                    QASmartTouch.Services.Tools.RecentToolsManager.GraphIconPath,
                    Color.FromRgb(0x4C, 0xAF, 0x50),
                    () => WindowHelper.ShowChildDialog(new Form2_9_LineChartEditor(_mainDashboard), _mainDashboard),
                    "Dùng lại: Biểu đồ đường (Line Chart)");

                // Ẩn menu trước khi mở biểu đồ đường
                this.Hide();
                var chartEditor = new Form2_9_LineChartEditor(_mainDashboard);
                WindowHelper.ShowChildDialog(chartEditor, _mainDashboard);
                this.Close(); // Đóng menu sau khi đóng form biểu đồ
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở biểu đồ đường: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnChartPie_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "chart:Pie",
                    "Biểu đồ tròn",
                    QASmartTouch.Services.Tools.RecentToolsManager.PieChartIconPath,
                    Color.FromRgb(0xFF, 0x98, 0x00),
                    () => WindowHelper.ShowChildDialog(new Form2_10_PieChartEditor(_mainDashboard), _mainDashboard),
                    "Dùng lại: Biểu đồ tròn (Pie Chart)");

                // Ẩn menu trước khi mở biểu đồ tròn
                this.Hide();
                var chartEditor = new Form2_10_PieChartEditor(_mainDashboard);
                WindowHelper.ShowChildDialog(chartEditor, _mainDashboard);
                this.Close(); // Đóng menu sau khi đóng form biểu đồ
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở biểu đồ tròn: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnChartArea_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "chart:Area",
                    "Biểu đồ vùng",
                    QASmartTouch.Services.Tools.RecentToolsManager.GraphIconPath,
                    Color.FromRgb(0x9C, 0x27, 0xB0),
                    () => WindowHelper.ShowChildDialog(new Form2_12_AreaChartEditor(_mainDashboard), _mainDashboard),
                    "Dùng lại: Biểu đồ vùng (Area Chart)");

                // Ẩn menu trước khi mở biểu đồ vùng
                this.Hide();
                var chartEditor = new Form2_12_AreaChartEditor(_mainDashboard);
                WindowHelper.ShowChildDialog(chartEditor, _mainDashboard);
                this.Close(); // Đóng menu sau khi đóng form biểu đồ
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở biểu đồ vùng: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnChartScatter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "chart:Scatter",
                    "Biểu đồ điểm",
                    QASmartTouch.Services.Tools.RecentToolsManager.GraphIconPath,
                    Color.FromRgb(0xFF, 0x57, 0x22),
                    () => WindowHelper.ShowChildDialog(new Form2_13_ScatterChartEditor(_mainDashboard), _mainDashboard),
                    "Dùng lại: Biểu đồ phân tán (Scatter Chart)");

                // Ẩn menu trước khi mở biểu đồ phân tán
                this.Hide();
                var chartEditor = new Form2_13_ScatterChartEditor(_mainDashboard);
                WindowHelper.ShowChildDialog(chartEditor, _mainDashboard);
                this.Close(); // Đóng menu sau khi đóng form biểu đồ
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở biểu đồ phân tán: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // TAB 7: MATH
        private void btnMathEquation_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Công thức toán:\n• Ký hiệu đặc biệt\n• Phân số, căn bậc, mũ\n• LaTeX support", 
                          "Công thức toán", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnVariationTable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "insert:VariationTable",
                    "Bảng biến thiên",
                    QASmartTouch.Services.Tools.RecentToolsManager.GraphIconPath,
                    Color.FromRgb(0x6C, 0x5C, 0xE7),
                    () =>
                    {
                        var dlg = new Form2_VariationTableDialog(_mainDashboard);
                        dlg.Owner = _mainDashboard ?? Application.Current.MainWindow;
                        dlg.ShowDialog();
                    },
                    "Dùng lại: Bảng biến thiên & Khảo sát hàm số");

                this.Hide();
                var dialog = new Form2_VariationTableDialog(_mainDashboard);
                dialog.Owner = _mainDashboard ?? Application.Current.MainWindow;
                dialog.ShowDialog();
                this.Close();
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở bảng biến thiên: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnMathGraph_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "insert:MathGraph2D",
                    "Đồ thị 2D",
                    QASmartTouch.Services.Tools.RecentToolsManager.GraphIconPath,
                    Color.FromRgb(0x00, 0xB8, 0x94),
                    () =>
                    {
                        var dlg = new Form2_VariationTableDialog(_mainDashboard, defaultToGraph: true);
                        dlg.Owner = _mainDashboard ?? Application.Current.MainWindow;
                        dlg.ShowDialog();
                    },
                    "Dùng lại: Vẽ Đồ thị hàm số 2D");

                // Ẩn menu trước khi mở đồ thị hàm số 2D (Dùng công cụ Đồ thị & BBT mới offline 100%)
                this.Hide();
                var dialog = new Form2_VariationTableDialog(_mainDashboard, defaultToGraph: true);
                dialog.Owner = _mainDashboard ?? Application.Current.MainWindow;
                dialog.ShowDialog();
                this.Close(); // Đóng menu sau khi đóng form đồ thị
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở đồ thị hàm số: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Graph editor error: {ex.Message}");
            }
        }

        private void btnGraph3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "insert:Graph3D",
                    "Desmos 3D",
                    QASmartTouch.Services.Tools.RecentToolsManager.Cube3DIconPath,
                    Color.FromRgb(0xE9, 0x1E, 0x63),
                    () =>
                    {
                        var dlg = new Form2_22_Graph3D();
                        if (WindowHelper.ShowChildDialog(dlg, _mainDashboard) == true && dlg.WasInserted && !string.IsNullOrEmpty(dlg.GraphUrl))
                        {
                            _mainDashboard?.InsertGraph3DToCanvas(dlg.GraphUrl);
                        }
                    },
                    "Dùng lại: Đồ thị 3D tương tác (Desmos 3D)");

                // Ẩn menu trước khi mở Graph 3D
                this.Hide();
                var Graph3DDialog = new Form2_22_Graph3D();
                bool? result = WindowHelper.ShowChildDialog(Graph3DDialog, _mainDashboard);
                
                if (result == true && Graph3DDialog.WasInserted && !string.IsNullOrEmpty(Graph3DDialog.GraphUrl))
                {
                    // Insert Graph 3D graph to canvas
                    _mainDashboard?.InsertGraph3DToCanvas(Graph3DDialog.GraphUrl);
                    System.Diagnostics.Debug.WriteLine($"✅ Graph 3D graph inserted: {Graph3DDialog.GraphUrl}");
                }
                
                this.Close(); // Close menu after closing dialog
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở Graph 3D Graph: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Graph 3D error: {ex.Message}");
            }
        }

        private void btnMath3DGraph_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "insert:Math3DGraph",
                    "Đồ thị 3D",
                    QASmartTouch.Services.Tools.RecentToolsManager.Cube3DIconPath,
                    Color.FromRgb(0xE9, 0x1E, 0x63),
                    () =>
                    {
                        var ed = new Form2_Graph3DEditor();
                        if (WindowHelper.ShowChildDialog(ed, _mainDashboard) == true && ed.ExportedGraphImage != null)
                        {
                            _mainDashboard?.InsertGraphToCanvas(ed.ExportedGraphImage, ed.ExportedConfig);
                        }
                    },
                    "Dùng lại: Vẽ Đồ thị 3D z = f(x,y)");

                // Ẩn menu trước khi mở đồ thị 3D
                this.Hide();
                var graph3DEditor = new Form2_Graph3DEditor();
                bool? result = WindowHelper.ShowChildDialog(graph3DEditor, _mainDashboard);
                
                if (result == true && graph3DEditor.ExportedGraphImage != null)
                {
                    // Insert 3D graph image to canvas with configuration
                    _mainDashboard?.InsertGraphToCanvas(graph3DEditor.ExportedGraphImage, graph3DEditor.ExportedConfig);
                    System.Diagnostics.Debug.WriteLine("✅ 3D Graph inserted to canvas");
                }
                
                this.Close(); // Đóng menu sau khi đóng form 3D
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở đồ thị 3D: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ 3D Graph editor error: {ex.Message}");
            }
        }

        private void btnMathGeometry_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hình học:\n• Hình 2D và 3D\n• Góc, đoạn thẳng\n• Tính toán tự động", 
                          "Hình học", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // TAB 8: PHYSICS
        private void btnPhysicsCircuit_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Sơ đồ mạch điện:\n• Điện trở, tụ, cuộn cảm\n• Nguồn điện\n• Kết nối dễ dàng", 
                          "Mạch điện", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnPhysicsVector_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Vector lực:\n• Mũi tên có hướng\n• Độ lớn\n• Phân tích vector", 
                          "Vector", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // TAB 9: CHEMISTRY
        private void btnChemFormula_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Công thức hóa học:\n• Ký hiệu nguyên tố\n• Chỉ số trên/dưới\n• Phương trình hóa học", 
                          "Công thức hóa", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnChemPeriodicTable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "insert:PeriodicTable",
                    "Bảng tuần hoàn",
                    QASmartTouch.Services.Tools.RecentToolsManager.PeriodicTableIconPath,
                    Color.FromRgb(0xE1, 0x70, 0x55),
                    () =>
                    {
                        var ptWin = new PeriodicTable.Views.MainWindow { Topmost = true };
                        ptWin.Show();
                    },
                    "Dùng lại: Bảng tuần hoàn Hóa học");

                // Ẩn menu trước khi mở Bảng tuần hoàn
                this.Hide();
                var periodicTableWindow = new PeriodicTable.Views.MainWindow();
                periodicTableWindow.Topmost = true;
                periodicTableWindow.Show();
                this.Close(); // Đóng menu sau khi đóng bảng tuần hoàn
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở Bảng tuần hoàn: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ PeriodicTable error: {ex.Message}");
            }
        }

        // TAB 10: AI
        private void btnAIGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ẩn menu trước khi mở AI Copilot
                this.Hide();
#if !SMARTTOUCH_STANDALONE
                var db = QASmartClass.Services.AppServices.Database ?? QASmartClass.Services.AppServices.CreateDb();
                var copilot = new QASmartClass.TeacherHub.Views.AiCopilotWindow(db);
                copilot.Owner = this.Owner ?? _mainDashboard;
                bool? result = copilot.ShowDialog();
                
                if (result == true)
                {
                    // Get latest generated questions from DB
                    var latestQuestions = db.QuestionBankItems.OrderByDescending(q => q.Id).Take(3).ToList();
                    if (latestQuestions.Any())
                    {
                        string formattedText = "🤖 CÂU HỎI TRẮC NGHIỆM AI GENERATED:\n\n";
                        for (int i = latestQuestions.Count - 1; i >= 0; i--)
                        {
                            var q = latestQuestions[i];
                            formattedText += $"Câu hỏi: {q.Content}\n";
                            try
                            {
                                var options = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.List<string>>(q.OptionsJson);
                                if (options != null && options.Count >= 4)
                                {
                                    formattedText += $"A. {options[0]}\nB. {options[1]}\nC. {options[2]}\nD. {options[3]}\n";
                                }
                                else if (options != null)
                                {
                                    for (int j = 0; j < options.Count; j++)
                                    {
                                        formattedText += $"{(char)('A' + j)}. {options[j]}\n";
                                    }
                                }
                            }
                            catch
                            {
                                formattedText += $"Lựa chọn: {q.OptionsJson}\n";
                            }
                            formattedText += $"👉 Đáp án đúng: {q.CorrectAnswer}\n\n";
                        }
                        
                        var tbControl = new QASmartTouch.Controls.RichTextBoxControl
                        {
                            MinWidth = 400,
                            MinHeight = 250,
                            Text = formattedText
                        };
                        
                        _mainDashboard?.InsertTextBoxToCanvas(tbControl);
                    }
                    this.Close(); // Close menu after inserting
                }
                else
                {
                    this.Show(); // Re-show menu if user cancelled
                }
#else
                MessageBox.Show("Tính năng AI Copilot tự động tạo câu hỏi yêu cầu phân hệ QA SmartClass.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Show();
#endif
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi khi mở AI Copilot:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ AI Copilot error: {ex.Message}");
            }
        }

        private void btnAIChatbot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ẩn menu trước khi mở AI Chatbot
                this.Hide();
                var aiWindow = new Form2_24_AIAssistantWindow();
                var owner = this.Owner ?? _mainDashboard;
                this.Close(); // Close menu BEFORE showing AI window
                WindowHelper.ShowChildWindow(aiWindow, owner); // Kế thừa Topmost + Owner + Focus
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi khi mở Trợ lý AI:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ AI Chatbot window error: {ex.Message}");
            }
        }

        private void btnChatGPT_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ẩn menu trước khi mở ChatGPT
                this.Hide();
                var chatGPTBrowser = new Form2_24_ChatGPTBrowser();
                chatGPTBrowser.Topmost = true;
                chatGPTBrowser.Show();
                this.Close(); // Close menu after closing ChatGPT browser
                
                System.Diagnostics.Debug.WriteLine("✅ ChatGPT browser opened");
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi khi mở ChatGPT:\n{ex.Message}", 
                              "Lỗi", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ ChatGPT browser error: {ex.Message}");
            }
        }

        // TAB 11: PHET SIMULATIONS
        private void btnPhETPhysics_Click(object sender, RoutedEventArgs e)
        {
            OpenPhETBrowser("https://phet.colorado.edu/vi/simulations/filter?subjects=physics&type=html");
        }

        private void btnPhETMath_Click(object sender, RoutedEventArgs e)
        {
            OpenPhETBrowser("https://phet.colorado.edu/vi/simulations/filter?subjects=math-and-statistics&type=html");
        }

        private void btnPhETChemistry_Click(object sender, RoutedEventArgs e)
        {
            OpenPhETBrowser("https://phet.colorado.edu/vi/simulations/filter?subjects=chemistry&type=html");
        }

        private void btnPhETBiology_Click(object sender, RoutedEventArgs e)
        {
            OpenPhETBrowser("https://phet.colorado.edu/vi/simulations/filter?subjects=biology&type=html");
        }

        private void btnPhETEarth_Click(object sender, RoutedEventArgs e)
        {
            OpenPhETBrowser("https://phet.colorado.edu/vi/simulations/filter?subjects=earth-and-space&type=html");
        }

        private void btnPhETBrowseAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainDashboard?.RegisterRecentTool(
                    "insert:PhET",
                    "Mô phỏng PhET",
                    QASmartTouch.Services.Tools.RecentToolsManager.PhysicsLabIconPath,
                    Color.FromRgb(0x4C, 0xAF, 0x50),
                    () =>
                    {
                        var pw = new Form2_24_PhETCatalogWindow();
                        pw.SetMainDashboard(_mainDashboard);
                        WindowHelper.ShowChildDialog(pw, _mainDashboard);
                    },
                    "Dùng lại: Thư viện Mô phỏng PhET (Toán, Lý, Hóa, Sinh)");

                this.Hide();
                var phetWindow = new Form2_24_PhETCatalogWindow();
                if (_mainDashboard != null)
                {
                    phetWindow.SetMainDashboard(_mainDashboard);
                }
                WindowHelper.ShowChildDialog(phetWindow, _mainDashboard);
                this.Close();
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở Thư viện Học liệu PhET: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ PhET catalog error: {ex.Message}");
            }
        }


        /// <summary>
        /// Open PhET Simulation Browser with specified URL
        /// </summary>
        private void OpenPhETBrowser(string url)
        {
            try
            {
                // Ẩn menu trước khi mở PhET Simulation
                this.Hide();
                var phetBrowser = new Form2_21_PhETSimulationBrowser(url);
                phetBrowser.SetMainDashboard(_mainDashboard);
                phetBrowser.Owner = this.Owner; // Set owner to MainDashboard
                phetBrowser.ShowDialog();
                this.Close(); // Close menu after closing PhET browser
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở PhET Simulations: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ PhET browser error: {ex.Message}");
            }
        }

        // TAB 12: GOOGLE MAPS
        // KHO HỌC LIỆU 3D YOOBOOK (yoobook.vn/explore)
        private void btnYooBook_Click(object sender, RoutedEventArgs e)
        {
            _mainDashboard?.RegisterRecentTool(
                "insert:YooBook",
                "YooBook 3D",
                QASmartTouch.Services.Tools.RecentToolsManager.Cube3DIconPath,
                Color.FromRgb(0x0F, 0x6B, 0x5D),
                () =>
                {
                    var yb = new Form2_26_YooBookBrowser("https://yoobook.vn/explore");
                    yb.SetMainDashboard(_mainDashboard);
                    yb.Owner = _mainDashboard;
                    yb.ShowDialog();
                },
                "Dùng lại: Kho học liệu 3D YooBook");
            OpenYooBookBrowser("https://yoobook.vn/explore");
        }

        /// <summary>
        /// Mở cửa sổ Kho học liệu 3D &amp; Phòng thí nghiệm ảo K12 YooBook (Explore-only + Auto-Login)
        /// </summary>
        private void OpenYooBookBrowser(string url)
        {
            try
            {
                this.Hide();
                var yooBookBrowser = new Form2_26_YooBookBrowser(url);
                yooBookBrowser.SetMainDashboard(_mainDashboard);
                yooBookBrowser.Owner = this.Owner;
                yooBookBrowser.ShowDialog();
                this.Close();
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở Kho học liệu YooBook: {ex.Message}", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ YooBook browser error: {ex.Message}");
            }
        }

        private void btnGoogleMapsHanoi_Click(object sender, RoutedEventArgs e)
        {
            InsertGoogleMapsLocation("https://www.google.com/maps/@21.0104732,105.8484669,15z");
        }

        private void btnGoogleMapsHCM_Click(object sender, RoutedEventArgs e)
        {
            InsertGoogleMapsLocation("https://www.google.com/maps/@10.8230989,106.6296638,15z");
        }

        private void btnGoogleMapsDaNang_Click(object sender, RoutedEventArgs e)
        {
            InsertGoogleMapsLocation("https://www.google.com/maps/@16.0544068,108.2021667,15z");
        }

        private void btnGoogleMapsCanTho_Click(object sender, RoutedEventArgs e)
        {
            InsertGoogleMapsLocation("https://www.google.com/maps/@10.0451618,105.7468535,15z");
        }

        private void btnGoogleMapsCustom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ẩn menu trước khi mở hộp thoại tìm kiếm để không che khuất dialog
                this.Hide();

                // Show input dialog for custom location
                var inputDialog = new Window
                {
                    Title = "Tìm kiếm địa điểm",
                    Width = 460,
                    Height = 190,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    WindowStyle = WindowStyle.ToolWindow,
                    ResizeMode = ResizeMode.NoResize,
                    Owner = this.Owner ?? _mainDashboard,
                    Topmost = true
                };

                var stackPanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) };
                
                stackPanel.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = "Nhập tên địa điểm hoặc tọa độ:",
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                var textBox = new System.Windows.Controls.TextBox
                {
                    Text = "Hà Nội, Việt Nam",
                    FontSize = 14,
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 0, 0, 15)
                };
                stackPanel.Children.Add(textBox);

                var buttonPanel = new System.Windows.Controls.StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                var okButton = new System.Windows.Controls.Button
                {
                    Content = "Tìm kiếm",
                    Width = 100,
                    Height = 36,
                    Margin = new Thickness(0, 0, 10, 0),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(66, 133, 244)),
                    Foreground = System.Windows.Media.Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold
                };

                var cancelButton = new System.Windows.Controls.Button
                {
                    Content = "Hủy",
                    Width = 80,
                    Height = 36,
                    Background = System.Windows.Media.Brushes.LightGray,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    FontSize = 13
                };

                bool searchExecuted = false;

                Action executeSearch = () =>
                {
                    string searchQuery = textBox.Text.Trim();
                    if (!string.IsNullOrEmpty(searchQuery))
                    {
                        searchExecuted = true;
                        string url = $"https://www.google.com/maps/search/{Uri.EscapeDataString(searchQuery)}";
                        inputDialog.Close();
                        InsertGoogleMapsLocation(url);
                    }
                };

                okButton.Click += (s, args) => executeSearch();
                cancelButton.Click += (s, args) => inputDialog.Close();

                // Hỗ trợ phím Enter để tìm kiếm, Esc để hủy (thuận tiện cho cả PC và màn hình tương tác)
                textBox.KeyDown += (s, args) =>
                {
                    if (args.Key == Key.Enter)
                    {
                        args.Handled = true;
                        executeSearch();
                    }
                    else if (args.Key == Key.Escape)
                    {
                        args.Handled = true;
                        inputDialog.Close();
                    }
                };

                buttonPanel.Children.Add(okButton);
                buttonPanel.Children.Add(cancelButton);
                stackPanel.Children.Add(buttonPanel);

                inputDialog.Content = stackPanel;

                // Tự động focus và bôi đen text để gõ ngay
                inputDialog.Loaded += (s, args) =>
                {
                    textBox.Focus();
                    textBox.SelectAll();
                };

                inputDialog.ShowDialog();

                // Nếu không thực hiện tìm kiếm (người dùng bấm Hủy hoặc đóng cửa sổ), đóng menu để trả lại bảng vẽ
                if (!searchExecuted)
                {
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở dialog tìm kiếm: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnGoogleMapsMapView_Click(object sender, RoutedEventArgs e)
        {
            InsertGoogleMapsLocation("https://www.google.com/maps/@21.0104732,105.8484669,15z");
        }

        private void btnGoogleMapsSatellite_Click(object sender, RoutedEventArgs e)
        {
            // Satellite view mode
            InsertGoogleMapsLocation("https://www.google.com/maps/@21.0104732,105.8484669,15z/data=!3m1!1e3");
        }

        private void btnGoogleMapsStreetView_Click(object sender, RoutedEventArgs e)
        {
            // Street View mode
            InsertGoogleMapsLocation("https://www.google.com/maps/@21.0104732,105.8484669,3a,75y,90t/data=!3m6!1e1");
        }

        /// <summary>
        /// Helper method to insert Google Maps with specified URL
        /// </summary>
        private void InsertGoogleMapsLocation(string mapsUrl)
        {
            try
            {
                if (_mainDashboard != null)
                {
                    _mainDashboard.InsertGoogleMaps(mapsUrl);
                    this.Close();
                    System.Diagnostics.Debug.WriteLine($"✅ Google Maps inserted: {mapsUrl}");
                }
                else
                {
                    MessageBox.Show("Không tìm thấy Main Dashboard", "Lỗi",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi chèn Google Maps: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Google Maps insert error: {ex.Message}");
            }
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
                // INSERT CONTENT FEATURES
                // =====================================================
                // NOTE: Button names need to match x:Name in XAML
                // To enable control, add x:Name to buttons in XAML first.
                
                // Education features (disabled in v1.0)
                // btnChemPeriodicTable?.SetVisibilityByFeature("periodic_table");
                // btnInsertBook?.SetVisibilityByFeature("book_viewer");
                
                // Advanced charts (disabled in v1.0)
                // btnChartArea?.SetVisibilityByFeature("area_chart");
                // btnChartScatter?.SetVisibilityByFeature("scatter_chart");
                // btnChartRadar?.SetVisibilityByFeature("radar_chart");
                
                // AI features (disabled in v1.0)
                // btnAIGenerate?.SetVisibilityByFeature("ai_generator");
                // btnAIChatbot?.SetVisibilityByFeature("ai_chatbot");
                
                // 3D Graph (disabled in v1.0)
                // btnMath3DGraph?.SetVisibilityByFeature("graph_3d_editor");
                
                // =====================================================
                // ENABLED FEATURES (always visible in v1.0)
                // =====================================================
                // - Text, Table, Sticky Note: enabled
                // - Image (File, Camera, Library): enabled
                // - Video, YouTube: enabled
                // - Bar Chart, Line Chart, Pie Chart: enabled
                // - Math Equation, Math Graph 2D, Geometry: enabled
                // - Physics (Circuit, Vector): enabled
                // - Chemical Formula: enabled
                // - Ruler, Compass, Triangle, Protractor: enabled
                
                System.Diagnostics.Debug.WriteLine("[Form2_4_SubMenuInsertContent] Feature visibility applied successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Form2_4_SubMenuInsertContent] Error applying feature visibility: {ex.Message}");
            }
        }

        #endregion

        #region QC_4.2_TOUCH_PIPELINE — Touch Activation cho IFP Touch Screen

        /// <summary>
        /// Tự động duyệt cây Visual Tree, gắn pipeline cảm ứng cho tất cả Button
        /// (trừ btnClose đã xử lý riêng). Đảm bảo cú chạm đầu tiên kích hoạt ngay.
        /// </summary>
        private void WireAllInteractiveControls(DependencyObject parent)
        {
            if (parent == null) return;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is Button button && button != btnClose)
                {
                    TouchActivationHelper.WireButton(button);
                }

                // Recurse vào con
                WireAllInteractiveControls(child);
            }
        }

        /// <summary>
        /// QC_4.2_TOUCH_PIPELINE: Wire touch activation cho RadioButton sidebar.
        /// Đảm bảo chạm 1 lần là chuyển tab ngay trên IFP.
        /// </summary>
        private void WireTouchActivationRadio(RadioButton radio, RoutedEventHandler checkedHandler)
        {
            if (radio == null) return;
            radio.Focusable = false;
            Stylus.SetIsPressAndHoldEnabled(radio, false);

            radio.PreviewTouchDown += (s, e) =>
            {
                e.TouchDevice.Capture(radio);
                e.Handled = true;
            };

            radio.PreviewTouchUp += (s, e) =>
            {
                if (e.TouchDevice.Captured == radio)
                {
                    radio.ReleaseTouchCapture(e.TouchDevice);
                    try
                    {
                        var pos = e.GetTouchPoint(radio).Position;
                        if (pos.X >= 0 && pos.X <= radio.ActualWidth &&
                            pos.Y >= 0 && pos.Y <= radio.ActualHeight)
                        {
                            radio.IsChecked = true;
                            checkedHandler(radio, new RoutedEventArgs(ToggleButton.CheckedEvent, radio));
                        }
                    }
                    catch
                    {
                        radio.IsChecked = true;
                        checkedHandler(radio, new RoutedEventArgs(ToggleButton.CheckedEvent, radio));
                    }
                }
                e.Handled = true;
            };

            radio.PreviewStylusDown += (s, e) =>
            {
                e.StylusDevice.Capture(radio);
                e.Handled = true;
            };

            radio.PreviewStylusUp += (s, e) =>
            {
                if (e.StylusDevice.Captured == radio)
                {
                    radio.ReleaseStylusCapture();
                    try
                    {
                        var pos = e.GetPosition(radio);
                        if (pos.X >= 0 && pos.X <= radio.ActualWidth &&
                            pos.Y >= 0 && pos.Y <= radio.ActualHeight)
                        {
                            radio.IsChecked = true;
                            checkedHandler(radio, new RoutedEventArgs(ToggleButton.CheckedEvent, radio));
                        }
                    }
                    catch
                    {
                        radio.IsChecked = true;
                        checkedHandler(radio, new RoutedEventArgs(ToggleButton.CheckedEvent, radio));
                    }
                }
                e.Handled = true;
            };
        }

        #endregion
    }
}

