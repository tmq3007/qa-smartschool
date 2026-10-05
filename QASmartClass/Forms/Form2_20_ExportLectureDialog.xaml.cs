using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using QASmartTouch.Services;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_20_ExportLectureDialog : Window
    {
        private readonly Form2_MainDashboard? _mainDashboard;
        private string? _exportedFilePath;

        public Form2_20_ExportLectureDialog(Form2_MainDashboard? mainDashboard = null)
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
            _mainDashboard = mainDashboard ?? Application.Current.MainWindow as Form2_MainDashboard;
            Owner = _mainDashboard;

            SetupTouchSupport();
            InitializeDialogValues();
        }

        private void InitializeDialogValues()
        {
            string defaultName = $"BaiGiang_{DateTime.Now:yyyyMMdd_HHmm}";
            txtLectureTitle.Text = defaultName;

            if (_mainDashboard?.BoardManager != null)
            {
                int count = _mainDashboard.BoardManager.BoardCount;
                var currentBoard = _mainDashboard.BoardManager.CurrentBoard;

                txtScopeAllTitle.Text = $"Toàn bộ bài giảng ({count} trang)";
                txtScopeAllDesc.Text = $"Ghép trọn vẹn cả {count} trang bảng thành một tài liệu thống nhất";

                txtScopeCurrentTitle.Text = $"Chỉ trang hiện tại ({currentBoard?.Name ?? "Trang 1"})";
            }
        }

        private void SetupTouchSupport()
        {
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
                    CloseDialog();
                };
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog();
        }

        private void CloseDialog()
        {
            try
            {
                this.Owner?.Activate();
            }
            catch { }
            this.Close();
        }

        private void btnStartExport_Click(object sender, RoutedEventArgs e)
        {
            if (_mainDashboard == null)
            {
                MessageBox.Show("Không tìm thấy cửa sổ bảng trắng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string title = !string.IsNullOrWhiteSpace(txtLectureTitle.Text) 
                ? txtLectureTitle.Text.Trim() 
                : $"BaiGiang_{DateTime.Now:yyyyMMdd_HHmm}";

            bool isAllBoards = rbScopeAll.IsChecked == true;
            bool isPdf = rbFormatPdf.IsChecked == true;
            bool isPng = rbFormatPng.IsChecked == true;

            string filter;
            string defaultExt;
            string initialFileName;

            if (isPdf)
            {
                filter = "Tài liệu PDF (*.pdf)|*.pdf";
                defaultExt = ".pdf";
                initialFileName = $"{title}.pdf";
            }
            else if (isPng)
            {
                filter = "Hình ảnh PNG (*.png)|*.png";
                defaultExt = ".png";
                initialFileName = isAllBoards ? $"{title}_Trang_1.png" : $"{title}.png";
            }
            else
            {
                filter = "Hình ảnh JPEG (*.jpg)|*.jpg";
                defaultExt = ".jpg";
                initialFileName = isAllBoards ? $"{title}_Trang_1.jpg" : $"{title}.jpg";
            }

            string defaultDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "QA SmartClass",
                "XuatBaiGiang");
            if (!System.IO.Directory.Exists(defaultDir))
            {
                System.IO.Directory.CreateDirectory(defaultDir);
            }

            var saveDialog = new SaveFileDialog
            {
                Title = isAllBoards ? "Chọn nơi lưu bài giảng" : "Chọn nơi lưu ảnh trang bảng",
                Filter = filter,
                DefaultExt = defaultExt,
                InitialDirectory = defaultDir,
                FileName = initialFileName
            };

            if (saveDialog.ShowDialog(this) != true)
            {
                return;
            }

            string targetPath = saveDialog.FileName;

            // Chuyển sang màn hình Progress
            panelFormOptions.Visibility = Visibility.Collapsed;
            panelProgress.Visibility = Visibility.Visible;
            panelFooterActions.Visibility = Visibility.Collapsed;

            // Cập nhật giá trị thanh tiến trình
            int totalBoards = _mainDashboard.BoardManager?.BoardCount ?? 1;
            pbExport.Minimum = 0;
            pbExport.Maximum = isAllBoards ? totalBoards : 1;
            pbExport.Value = 0;

            // Chạy xuất dữ liệu
            Dispatcher.BeginInvoke(new Action(() =>
            {
                bool success = false;

                try
                {
                    if (isPdf)
                    {
                        if (isAllBoards && _mainDashboard.BoardManager != null)
                        {
                            success = CanvasExportService.ExportBoardsToPdf(
                                _mainDashboard.BoardManager,
                                _mainDashboard.MainInteractiveBoard,
                                targetPath,
                                title,
                                192,
                                (current, total) =>
                                {
                                    pbExport.Value = current;
                                    txtProgressStatus.Text = $"Đang xuất bài giảng... (Trang {current}/{total})";
                                });
                        }
                        else
                        {
                            pbExport.Value = 1;
                            txtProgressStatus.Text = "Đang kết xuất PDF trang hiện tại...";
                            success = CanvasExportService.ExportToPdf(
                                _mainDashboard.MainInteractiveBoard,
                                targetPath,
                                title,
                                192,
                                _mainDashboard.BoardManager != null ? _mainDashboard.BoardManager.IsSystemElement : null);
                        }
                    }
                    else
                    {
                        string format = isPng ? "png" : "jpg";

                        if (isAllBoards && _mainDashboard.BoardManager != null)
                        {
                            string outDir = Path.GetDirectoryName(targetPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                            string baseName = Path.GetFileNameWithoutExtension(targetPath);
                            // Nếu tên có đuôi _Trang_1 thì bỏ đuôi để đặt đồng nhất
                            if (baseName.EndsWith("_Trang_1", StringComparison.OrdinalIgnoreCase))
                            {
                                baseName = baseName.Substring(0, baseName.Length - "_Trang_1".Length);
                            }

                            success = CanvasExportService.ExportBoardsToImages(
                                _mainDashboard.BoardManager,
                                _mainDashboard.MainInteractiveBoard,
                                outDir,
                                baseName,
                                format,
                                192,
                                (current, total) =>
                                {
                                    pbExport.Value = current;
                                    txtProgressStatus.Text = $"Đang lưu hình ảnh... (Trang {current}/{total})";
                                });

                            targetPath = Path.Combine(outDir, $"{baseName}_Trang_1.{format}");
                        }
                        else
                        {
                            pbExport.Value = 1;
                            txtProgressStatus.Text = "Đang lưu hình ảnh trang hiện tại...";
                            success = CanvasExportService.ExportSingleBoardToImage(
                                _mainDashboard.MainInteractiveBoard,
                                targetPath,
                                format,
                                192,
                                _mainDashboard.BoardManager != null ? _mainDashboard.BoardManager.IsSystemElement : null);
                        }
                    }

                    if (success)
                    {
                        _exportedFilePath = targetPath;
                        ShowSuccessState(targetPath);
                    }
                    else
                    {
                        MessageBox.Show("Có lỗi xảy ra trong quá trình xuất bài giảng. Vui lòng thử lại.", "Lỗi xuất", MessageBoxButton.OK, MessageBoxImage.Error);
                        panelProgress.Visibility = Visibility.Collapsed;
                        panelFormOptions.Visibility = Visibility.Visible;
                        panelFooterActions.Visibility = Visibility.Visible;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi không xác định: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    panelProgress.Visibility = Visibility.Collapsed;
                    panelFormOptions.Visibility = Visibility.Visible;
                    panelFooterActions.Visibility = Visibility.Visible;
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void ShowSuccessState(string filePath)
        {
            panelProgress.Visibility = Visibility.Collapsed;
            panelSuccess.Visibility = Visibility.Visible;

            txtSuccessPath.Text = filePath;

            // Cập nhật lại thanh nút hành động
            panelFooterActions.Visibility = Visibility.Visible;
            btnStartExport.Visibility = Visibility.Collapsed;
            btnCancel.Content = "Đóng";
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_exportedFilePath) && File.Exists(_exportedFilePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _exportedFilePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể mở tệp: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_exportedFilePath))
            {
                try
                {
                    if (File.Exists(_exportedFilePath))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"/select,\"{_exportedFilePath}\"",
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        string? dir = Path.GetDirectoryName(_exportedFilePath);
                        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = dir,
                                UseShellExecute = true
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể mở thư mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
