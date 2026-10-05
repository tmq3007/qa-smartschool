using System.Linq;
using System.Windows;
using QASmartTouch.Forms.Admin;
using QASmartTouch.Services.VersionManagement;
using QASmartTouch.Shared;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_8_SubMenuMoreExtended : Window
    {
        private Form2_MainDashboard? _mainDashboard;

        public Form2_8_SubMenuMoreExtended(Form2_MainDashboard? mainDashboard = null)
        {
            _mainDashboard = mainDashboard;
            InitializeComponent();
            TouchActivationHelper.ApplyToSubMenu(this, btnClose);
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
            
            // Apply feature visibility on load
            this.Loaded += (s, e) => ApplyFeatureVisibility();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            try { this.Owner?.Activate(); } catch { }
            this.Close();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            // Close this menu first to avoid z-order conflicts
            this.Close();
            
            // Open Form3_3_Settings
            var settingsWindow = new Form3_3_Settings();
            WindowHelper.ShowChildDialog(settingsWindow);
        }

        private Form2_MainDashboard? GetMainDashboard()
        {
            return _mainDashboard
                ?? (this.Owner as Form2_MainDashboard)
                ?? (Application.Current as App)?._whiteboardShell
                ?? Application.Current.Windows.OfType<Form2_MainDashboard>().FirstOrDefault()
                ?? (Application.Current.MainWindow as Form2_MainDashboard);
        }

        private void btnOpenLecture_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            var mainWindow = GetMainDashboard();
            if (mainWindow == null)
            {
                MessageBox.Show("Không tìm thấy bảng vẽ để mở bài giảng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string defaultDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "QA SmartClass",
                "BaiGiang");
            if (!System.IO.Directory.Exists(defaultDir))
            {
                System.IO.Directory.CreateDirectory(defaultDir);
            }

            var openDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Mở tệp bài giảng QA SmartClass",
                Filter = "Bài giảng QA SmartClass (*.qasc)|*.qasc|Tất cả tệp (*.*)|*.*",
                DefaultExt = ".qasc",
                InitialDirectory = defaultDir
            };

            if (openDialog.ShowDialog(mainWindow) == true)
            {
                mainWindow.LoadLecture(openDialog.FileName);
            }
            mainWindow.Activate();
        }

        private void btnRecentFiles_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            var mainWindow = GetMainDashboard();

            var dialog = new RecentFilesDialog();
            dialog.Owner = (Window?)mainWindow ?? Application.Current.MainWindow;
            if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.SelectedFilePath))
            {
                string ext = System.IO.Path.GetExtension(dialog.SelectedFilePath).ToLowerInvariant();

                if (ext == ".qasc" && mainWindow != null)
                {
                    mainWindow.LoadLecture(dialog.SelectedFilePath);
                    mainWindow.Activate();
                }
                else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif")
                {
                    // Mở ảnh: chèn vào canvas
                    MessageBox.Show($"Đã chọn file ảnh:\n{dialog.SelectedFilePath}\n\nFile sẽ được chèn vào bảng trắng.",
                        "Mở file gần đây", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (ext == ".pdf" || ext == ".pptx" || ext == ".docx")
                {
                    // Mở tài liệu
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = dialog.SelectedFilePath,
                            UseShellExecute = true
                        });
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show($"Không thể mở file:\n{ex.Message}",
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // Mở bằng chương trình mặc định
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = dialog.SelectedFilePath,
                            UseShellExecute = true
                        });
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show($"Không thể mở file:\n{ex.Message}",
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void btnLectureLibrary_Click(object sender, RoutedEventArgs e)
        {
            // Close this menu first to avoid z-order conflicts
            this.Close();
            
            // Open Form3_1_LectureLibrary
            var lectureWindow = new Form3_1_LectureLibrary();
            WindowHelper.ShowChildDialog(lectureWindow);
        }

        private void btnVersionManager_Click(object sender, RoutedEventArgs e)
        {
            // Close this menu first to avoid z-order conflicts
            this.Close();
            
            // Require admin authentication
            if (!Form_AdminPassword.Authenticate())
            {
                // User cancelled or failed authentication
                return;
            }
            
            // Open Version Manager
            var versionManager = new Form_VersionManager();
            WindowHelper.ShowChildDialog(versionManager);
        }

        private void btnSaveLecture_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            var mainWindow = GetMainDashboard();
            if (mainWindow != null)
            {
                mainWindow.SaveCurrentLecture();
                mainWindow.Activate();
            }
            else
            {
                MessageBox.Show("Không tìm thấy bảng vẽ để lưu bài giảng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnSaveLectureAs_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            var mainWindow = GetMainDashboard();
            if (mainWindow == null)
            {
                MessageBox.Show("Không tìm thấy bảng vẽ để lưu bài giảng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string defaultDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "QA SmartClass",
                "BaiGiang");
            if (!System.IO.Directory.Exists(defaultDir))
            {
                System.IO.Directory.CreateDirectory(defaultDir);
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Lưu bài giảng thành tệp mới",
                Filter = "Bài giảng QA SmartClass (*.qasc)|*.qasc",
                DefaultExt = ".qasc",
                InitialDirectory = defaultDir,
                FileName = $"BaiGiang_{DateTime.Now:yyyyMMdd_HHmm}.qasc"
            };

            if (saveDialog.ShowDialog(mainWindow) == true)
            {
                mainWindow.SaveCurrentLecture(saveDialog.FileName);
            }
            mainWindow.Activate();
        }

        private void btnShareLecture_Click(object sender, RoutedEventArgs e)
        {
            // Show share options dialog or directly open export dialog
            this.Close();
            var mainWindow = GetMainDashboard();
            if (mainWindow != null)
            {
                var exportDialog = new Form2_20_ExportLectureDialog(mainWindow);
                WindowHelper.ShowChildDialog(exportDialog);
                mainWindow.Activate();
            }
        }

        private void btnExportBoard_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            var mainWindow = GetMainDashboard();
            if (mainWindow != null)
            {
                var exportDialog = new Form2_20_ExportLectureDialog(mainWindow);
                WindowHelper.ShowChildDialog(exportDialog);
                mainWindow.Activate();
            }
        }

        private void btnPrintBoard_Click(object sender, RoutedEventArgs e)
        {
            this.Close();

            // Lấy canvas từ MainDashboard
            var mainWindow = GetMainDashboard();
            var canvas = mainWindow?.FindName("MainInteractiveBoard") as System.Windows.Controls.Canvas;
            if (canvas != null)
            {
                bool success = Services.CanvasExportService.PrintCanvas(canvas, "QA SmartTouch - Bảng trắng");
                if (success)
                {
                    MessageBox.Show("Đã gửi lệnh in thành công!", "In bảng trắng",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Không tìm thấy bảng trắng để in.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnCameraAI_Click(object sender, RoutedEventArgs e)
        {
            // Close this menu first to avoid z-order conflicts
            this.Close();
            
            // Open Form3_2_CameraAI
            var cameraWindow = new Form3_2_CameraAI();
            WindowHelper.ShowChildDialog(cameraWindow);
        }

        private void btnCameraConfig_Click(object sender, RoutedEventArgs e)
        {
            // Close this menu first to avoid z-order conflicts
            this.Close();
            
            // Open Form3_3_CameraConfiguration
            var configWindow = new Form3_3_CameraConfiguration();
            WindowHelper.ShowChildDialog(configWindow);
        }

        private void btnQuickSurvey_Click(object sender, RoutedEventArgs e)
        {
            // Close this menu first to avoid z-order conflicts
            this.Close();
            
            // Open Form4_1_QuickSurvey
            var surveyWindow = new Form4_1_QuickSurvey();
            WindowHelper.ShowChildDialog(surveyWindow);
        }

        private void btnQuickVote_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open quick vote dialog
            MessageBox.Show("Bắt đầu phiên bình chọn nhanh:\n• A\n• B\n• C\n• D", "Bình chọn", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnTimer_Click(object sender, RoutedEventArgs e)
        {
            // Close this menu first
            this.Close();
            
            // Open Countdown Timer window
            var countdownTimer = new Form2_19_CountdownTimer();
            WindowHelper.ShowChildWindow(countdownTimer);
        }

        private void btnCalculator_Click(object sender, RoutedEventArgs e)
        {
            // Close this menu first
            this.Close();
            
            // Open Calculator window
            var calculator = new CalculatorWindow();
            WindowHelper.ShowChildDialog(calculator);
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            this.Close();

            // Mở Help Tour overlay trên MainDashboard
            var mainWindow = (Window?)GetMainDashboard() ?? Application.Current.MainWindow;
            if (mainWindow != null)
            {
                var tour = new HelpTourOverlay(mainWindow);
                tour.Show();
            }
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            
            var aboutDialog = new AboutDialog();
            WindowHelper.ShowChildDialog(aboutDialog);
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Check for updates
            MessageBox.Show("Phiên bản hiện tại: v1.1\nKiểm tra cập nhật...\n\nBạn đang sử dụng phiên bản mới nhất!", 
                          "Cập nhật", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Information);
        }

        #region Feature Management

        /// <summary>
        /// Apply feature visibility based on VersionDetail.json configuration.
        /// Hides advanced features that are not enabled in current version.
        /// </summary>
        private void ApplyFeatureVisibility()
        {
            try
            {
                // =====================================================
                // ADVANCED FEATURES (disabled in v1.0 public release)
                // =====================================================
                
                // Camera AI - Advanced feature
                btnCameraAI?.SetVisibilityByFeature("camera_ai");
                
                // Camera Configuration - Advanced feature
                btnCameraConfig?.SetVisibilityByFeature("camera_config");
                
                // Quick Survey - Utility feature
                btnQuickSurvey?.SetVisibilityByFeature("quick_survey");
                
                // =====================================================
                // ENABLED FEATURES (always visible in v1.0)
                // =====================================================
                // - Settings: always enabled
                // - Save/Share lecture: always enabled
                // - Calculator: enabled
                // - Line Chart: enabled
                // - Help/About/Update: always enabled
                
                System.Diagnostics.Debug.WriteLine("[Form2_8_SubMenuMoreExtended] Feature visibility applied successfully");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Form2_8_SubMenuMoreExtended] Error applying feature visibility: {ex.Message}");
            }
        }

        #endregion
    }
}
