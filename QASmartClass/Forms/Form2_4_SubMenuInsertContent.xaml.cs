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
            SelectedInsertType = "Ruler";
            this.Close();
        }

        private void btnInsertCompass_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ẩn menu trước khi mở công cụ con
                this.Hide();
                var circleTool = new Form2_19_CircleDrawingTool();
                circleTool.Owner = this.Owner ?? _mainDashboard; // Set owner để tránh rò rỉ cửa sổ con
                circleTool.SetMainDashboard(_mainDashboard);
                circleTool.Show(); // Show (not ShowDialog) để không block
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
            SelectedInsertType = "Triangle";
            this.Close();
        }

        private void btnInsertProtractor_Click(object sender, RoutedEventArgs e)
        {
            SelectedInsertType = "Protractor";
            this.Close();
        }

        private void btnInsertKeyboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Xử lý bypass WOW64 File Redirection cho OSK trên hệ thống 64-bit khi chạy app 32-bit
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
                
                System.Diagnostics.Debug.WriteLine($"✅ On-Screen Keyboard launched via: {oskPath}");
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

        // TAB 3: TEXT
        private void btnInsertText_Click(object sender, RoutedEventArgs e)
        {
            SelectedInsertType = "Text";
            this.Close();
        }

        private void btnInsertTable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ẩn menu trước khi mở dialog con để không bị chồng cửa sổ
                this.Hide();
                var tableDialog = new Form2_TableEditorDialog();
                if (_mainDashboard != null)
                {
                    tableDialog.Owner = _mainDashboard;
                }
                bool? result = tableDialog.ShowDialog();
                
                if (result == true && tableDialog.ResultTableData != null)
                {
                    // Insert table to canvas
                    _mainDashboard?.InsertTableToCanvas(tableDialog.ResultTableData);
                    System.Diagnostics.Debug.WriteLine("✅ Table inserted to canvas");
                    this.Close(); // Đóng menu sau khi chèn thành công
                }
                else
                {
                    this.Show(); // Hiện lại menu nếu người dùng hủy
                }
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở trình chỉnh sửa bảng: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Table editor error: {ex.Message}");
            }
        }

        private void btnInsertTextBox_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ẩn menu trước khi mở dialog con để không bị chồng cửa sổ
                this.Hide();
                var textBoxEditor = new Form2_TextBoxEditor();
                bool? result = textBoxEditor.ShowDialog();
                
                if (result == true && textBoxEditor.IsConfirmed && textBoxEditor.TextBoxControl != null)
                {
                    // Insert text box to canvas
                    _mainDashboard?.InsertTextBoxToCanvas(textBoxEditor.TextBoxControl);
                    System.Diagnostics.Debug.WriteLine("✅ Text box inserted to canvas");
                    this.Close(); // Đóng menu sau khi chèn thành công
                }
                else
                {
                    this.Show(); // Hiện lại menu nếu người dùng hủy
                }
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Lỗi mở trình chỉnh sửa hộp văn bản: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Text box editor error: {ex.Message}");
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
            try
            {
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
            try
            {
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
            try
            {
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

        private void btnMathGraph_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ẩn menu trước khi mở đồ thị hàm số
                this.Hide();
                var graphEditor = new Form2_GraphEditorUnified(); // 🆕 Unified 2D+3D editor
                bool? result = WindowHelper.ShowChildDialog(graphEditor, _mainDashboard);
                
                if (result == true && graphEditor.ExportedGraphImage != null)
                {
                    // Insert graph image to canvas with config
                    _mainDashboard?.InsertGraphToCanvas(graphEditor.ExportedGraphImage, graphEditor.ExportedConfig);
                    System.Diagnostics.Debug.WriteLine("✅ Graph (2D/3D) inserted to canvas");
                }
                
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
                // Ẩn menu trước khi mở đồ thị 3D
                this.Hide();
                var graph3DEditor = new Form2_Graph3DEditor();
                bool? result = WindowHelper.ShowChildDialog(graph3DEditor, _mainDashboard);
                
                if (result == true && graph3DEditor.ExportedGraphImage != null)
                {
                    // Insert 3D graph image to canvas (config not supported yet for old 3D editor)
                    _mainDashboard?.InsertGraphToCanvas(graph3DEditor.ExportedGraphImage, null);
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
            OpenPhETBrowser("https://phet.colorado.edu/vi/simulations/browse?type=html");
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
                    WireTouchActivationButton(button);
                }

                // Recurse vào con
                WireAllInteractiveControls(child);
            }
        }

        /// <summary>
        /// QC_4.2_TOUCH_PIPELINE: Wire touch activation cho Button.
        /// Bắt trực tiếp PreviewTouchDown/Up để đảm bảo Zero 2nd tap.
        /// Phân biệt tap (< 15px) vs drag scroll (> 15px) để giữ ScrollViewer hoạt động.
        /// </summary>
        private void WireTouchActivationButton(Button button)
        {
            if (button == null) return;
            button.Focusable = false;
            Stylus.SetIsPressAndHoldEnabled(button, false);

            Point? touchStart = null;

            button.PreviewTouchDown += (s, e) =>
            {
                touchStart = e.GetTouchPoint(button).Position;
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
                        double dist = touchStart.HasValue
                            ? Math.Sqrt(Math.Pow(pos.X - touchStart.Value.X, 2) + Math.Pow(pos.Y - touchStart.Value.Y, 2))
                            : 0;

                        // Tap (< 15px) → kích hoạt Click; Drag (>= 15px) → nhường cho ScrollViewer
                        if (dist < 15 &&
                            pos.X >= 0 && pos.X <= button.ActualWidth &&
                            pos.Y >= 0 && pos.Y <= button.ActualHeight)
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
                            }), System.Windows.Threading.DispatcherPriority.Normal);
                        }
                    }
                    catch
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
                        }), System.Windows.Threading.DispatcherPriority.Normal);
                    }
                }
                touchStart = null;
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
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
                            }), System.Windows.Threading.DispatcherPriority.Normal);
                        }
                    }
                    catch
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
                        }), System.Windows.Threading.DispatcherPriority.Normal);
                    }
                }
                e.Handled = true;
            };
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

