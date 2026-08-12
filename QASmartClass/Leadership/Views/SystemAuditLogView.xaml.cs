using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.IO;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Converters;

namespace QASmartClass.Leadership.Views
{
    public partial class SystemAuditLogView : Page
    {
        private int _currentPage = 1;
        private readonly int _pageSize = 20;
        private int _totalPages = 1;

        public SystemAuditLogView()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
            dpFromDate.SelectedDateChanged += DpFromDate_SelectedDateChanged;
            dpToDate.SelectedDateChanged += DpToDate_SelectedDateChanged;
        }

        private void DpFromDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateDateRange();
        }

        private void DpToDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateDateRange();
        }

        private void ValidateDateRange()
        {
            if (dpFromDate.SelectedDate.HasValue && dpToDate.SelectedDate.HasValue)
            {
                if (dpFromDate.SelectedDate.Value > dpToDate.SelectedDate.Value)
                {
                    dpToDate.SelectedDate = dpFromDate.SelectedDate.Value;
                    
                    var toolTip = new ToolTip
                    {
                        Content = "Ngày kết thúc đã được tự động điều chỉnh bằng ngày bắt đầu.",
                        Placement = System.Windows.Controls.Primitives.PlacementMode.Top,
                        PlacementTarget = dpToDate,
                        IsOpen = true
                    };
                    
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(3)
                    };
                    timer.Tick += (s, args) =>
                    {
                        toolTip.IsOpen = false;
                        timer.Stop();
                    };
                    timer.Start();
                }
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            LoadLogs();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // Giải phóng tài nguyên nếu cần
        }

        private void LoadLogs()
        {
            // Trích xuất các bộ lọc từ giao diện trước khi chuyển sang luồng chạy nền
            string actor = txtSearchActor.Text.Trim();
            string action = txtSearchAction.Text.Trim();
            DateTime? fromDate = dpFromDate.SelectedDate;
            DateTime? toDate = dpToDate.SelectedDate;
            int page = _currentPage;

            string selectedSeverity = "Tất cả";
            if (cbSeverity.SelectedItem is ComboBoxItem selectedItem)
            {
                selectedSeverity = selectedItem.Content.ToString() ?? "Tất cả";
            }

            // Tải dữ liệu bất đồng bộ để tránh treo luồng giao diện chính (UI Thread)
            Task.Run(() =>
            {
                try
                {
                    using (var db = new AppDbContext())
                    {
                        var query = db.AuditLogs.AsQueryable();

                        // Áp dụng bộ lọc người dùng
                        if (!string.IsNullOrEmpty(actor))
                        {
                            query = query.Where(a => a.ActorName.Contains(actor));
                        }
                        if (!string.IsNullOrEmpty(action))
                        {
                            query = query.Where(a => a.Action.Contains(action));
                        }
                        if (fromDate.HasValue)
                        {
                            var start = fromDate.Value.Date;
                            query = query.Where(a => a.Timestamp >= start);
                        }
                        if (toDate.HasValue)
                        {
                            var end = toDate.Value.Date.AddDays(1).AddTicks(-1);
                            query = query.Where(a => a.Timestamp <= end);
                        }

                        // Áp dụng bộ lọc mức độ nghiêm trọng (bảo vệ SQL-compatible cho SQLite)
                        if (selectedSeverity.Contains("Nguy cấp"))
                        {
                            query = query.Where(a => 
                                a.Action.ToLower().Contains("tampered") || a.Action.ToLower().Contains("intrusion") || 
                                a.Action.ToLower().Contains("failure") || a.Action.ToLower().Contains("sql_injection") || 
                                a.Action.ToLower().Contains("bypass") || a.Action.ToLower().Contains("integrityerror") ||
                                (a.Details != null && a.Details.ToLower().Contains("tampered")) || 
                                (a.Details != null && a.Details.ToLower().Contains("intrusion")) || 
                                (a.Details != null && a.Details.ToLower().Contains("vi phạm"))
                            );
                        }
                        else if (selectedSeverity.Contains("Cảnh báo"))
                        {
                            query = query.Where(a => 
                                (a.Action.ToLower().Contains("rejected") || a.Action.ToLower().Contains("emergency") || 
                                 a.Action.ToLower().Contains("blocked") || a.Action.ToLower().Contains("reset_password") || 
                                 a.Action.ToLower().Contains("limit_exceeded") || a.Action.ToLower().Contains("unauthorized") ||
                                 (a.Details != null && a.Details.ToLower().Contains("từ chối")) || 
                                 (a.Details != null && a.Details.ToLower().Contains("khẩn cấp")))
                                &&
                                !(a.Action.ToLower().Contains("tampered") || a.Action.ToLower().Contains("intrusion") || 
                                  a.Action.ToLower().Contains("failure") || a.Action.ToLower().Contains("sql_injection") || 
                                  a.Action.ToLower().Contains("bypass") || a.Action.ToLower().Contains("integrityerror") ||
                                  (a.Details != null && a.Details.ToLower().Contains("tampered")) || 
                                  (a.Details != null && a.Details.ToLower().Contains("intrusion")) || 
                                  (a.Details != null && a.Details.ToLower().Contains("vi phạm")))
                            );
                        }
                        else if (selectedSeverity.Contains("Thông tin"))
                        {
                            query = query.Where(a => 
                                !(a.Action.ToLower().Contains("tampered") || a.Action.ToLower().Contains("intrusion") || 
                                  a.Action.ToLower().Contains("failure") || a.Action.ToLower().Contains("sql_injection") || 
                                  a.Action.ToLower().Contains("bypass") || a.Action.ToLower().Contains("integrityerror") ||
                                  (a.Details != null && a.Details.ToLower().Contains("tampered")) || 
                                  (a.Details != null && a.Details.ToLower().Contains("intrusion")) || 
                                  (a.Details != null && a.Details.ToLower().Contains("vi phạm")))
                                &&
                                !(a.Action.ToLower().Contains("rejected") || a.Action.ToLower().Contains("emergency") || 
                                  a.Action.ToLower().Contains("blocked") || a.Action.ToLower().Contains("reset_password") || 
                                  a.Action.ToLower().Contains("limit_exceeded") || a.Action.ToLower().Contains("unauthorized") ||
                                  (a.Details != null && a.Details.ToLower().Contains("từ chối")) || 
                                  (a.Details != null && a.Details.ToLower().Contains("khẩn cấp")))
                            );
                        }

                        // Tính tổng số dòng phù hợp bộ lọc
                        int totalCount = query.Count();

                        // Tính số trang tối đa
                        int totalPages = (int)Math.Ceiling((double)totalCount / _pageSize);
                        if (totalPages < 1) totalPages = 1;

                        // Truy vấn phân trang nâng cao
                        var logs = query.OrderByDescending(a => a.Timestamp)
                                        .Skip((page - 1) * _pageSize)
                                        .Take(_pageSize)
                                        .ToList();

                        // Đưa dữ liệu kết quả về hiển thị trên giao diện thông qua Dispatcher
                        Dispatcher.Invoke(() =>
                        {
                            _totalPages = totalPages;
                            if (_currentPage > _totalPages) _currentPage = _totalPages;

                            dgAuditLogs.ItemsSource = logs;
                            txtPageInfo.Text = $"Trang {_currentPage} / {_totalPages}";
                            txtTotalCount.Text = $"Tổng số bản ghi: {totalCount}";

                            // Bật/tắt nút điều hướng phân trang
                            btnPrevPage.IsEnabled = _currentPage > 1;
                            btnNextPage.IsEnabled = _currentPage < _totalPages;
                        });
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Lỗi tải dữ liệu nhật ký hoạt động: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }

        private void TxtSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                e.Handled = true;
                _currentPage = 1;
                LoadLogs();
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            LoadLogs();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            txtSearchActor.Text = string.Empty;
            txtSearchAction.Text = string.Empty;
            dpFromDate.SelectedDate = null;
            dpToDate.SelectedDate = null;
            cbSeverity.SelectedIndex = 0;
            _currentPage = 1;
            LoadLogs();
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadLogs();
            }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadLogs();
            }
        }

        private void BtnVerify_Click(object sender, RoutedEventArgs e)
        {
            btnVerify.IsEnabled = false;
            Task.Run(() =>
            {
                bool success = AuditHelper.VerifyIntegrity(out string errorDetails);
                
                Dispatcher.Invoke(() =>
                {
                    btnVerify.IsEnabled = true;
                    if (success)
                    {
                        MessageBox.Show("🛡 Xác thực hoàn tất! Cơ sở dữ liệu nhật ký hoạt động hoàn toàn toàn vẹn và không bị can thiệp trái phép.", "Xác thực bảo mật", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"⚠️ CẢNH BÁO BẢO MẬT: Chuỗi dữ liệu nhật ký hoạt động bị đứt gãy hoặc đã bị can thiệp sửa đổi bên ngoài!\n\nChi tiết lỗi: {errorDetails}", "Vi phạm toàn vẹn dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                });
            });
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"NhatKyHoatDong_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FileName;
                btnExport.IsEnabled = false;

                // Xuất toàn bộ dữ liệu khớp bộ lọc ra file Excel chạy luồng nền
                Task.Run(() =>
                {
                    try
                    {
                        using (var db = new AppDbContext())
                        {
                            var query = db.AuditLogs.AsQueryable();

                            // Trích xuất bộ lọc từ UI qua Dispatcher
                            string actor = string.Empty;
                            string action = string.Empty;
                            DateTime? fromDate = null;
                            DateTime? toDate = null;
                            string selectedSeverity = "Tất cả";

                            Dispatcher.Invoke(() =>
                            {
                                actor = txtSearchActor.Text.Trim();
                                action = txtSearchAction.Text.Trim();
                                fromDate = dpFromDate.SelectedDate;
                                toDate = dpToDate.SelectedDate;
                                if (cbSeverity.SelectedItem is ComboBoxItem selectedItem)
                                {
                                    selectedSeverity = selectedItem.Content.ToString() ?? "Tất cả";
                                }
                            });

                            if (!string.IsNullOrEmpty(actor))
                            {
                                query = query.Where(a => a.ActorName.Contains(actor));
                            }
                            if (!string.IsNullOrEmpty(action))
                            {
                                query = query.Where(a => a.Action.Contains(action));
                            }
                            if (fromDate.HasValue)
                            {
                                var start = fromDate.Value.Date;
                                query = query.Where(a => a.Timestamp >= start);
                            }
                            if (toDate.HasValue)
                            {
                                var end = toDate.Value.Date.AddDays(1).AddTicks(-1);
                                query = query.Where(a => a.Timestamp <= end);
                            }

                            // Áp dụng bộ lọc mức độ nghiêm trọng (bảo vệ SQL-compatible cho SQLite)
                            if (selectedSeverity.Contains("Nguy cấp"))
                            {
                                query = query.Where(a => 
                                    a.Action.ToLower().Contains("tampered") || a.Action.ToLower().Contains("intrusion") || 
                                    a.Action.ToLower().Contains("failure") || a.Action.ToLower().Contains("sql_injection") || 
                                    a.Action.ToLower().Contains("bypass") || a.Action.ToLower().Contains("integrityerror") ||
                                    (a.Details != null && a.Details.ToLower().Contains("tampered")) || 
                                    (a.Details != null && a.Details.ToLower().Contains("intrusion")) || 
                                    (a.Details != null && a.Details.ToLower().Contains("vi phạm"))
                                );
                            }
                            else if (selectedSeverity.Contains("Cảnh báo"))
                            {
                                query = query.Where(a => 
                                    (a.Action.ToLower().Contains("rejected") || a.Action.ToLower().Contains("emergency") || 
                                     a.Action.ToLower().Contains("blocked") || a.Action.ToLower().Contains("reset_password") || 
                                     a.Action.ToLower().Contains("limit_exceeded") || a.Action.ToLower().Contains("unauthorized") ||
                                     (a.Details != null && a.Details.ToLower().Contains("từ chối")) || 
                                     (a.Details != null && a.Details.ToLower().Contains("khẩn cấp")))
                                    &&
                                    !(a.Action.ToLower().Contains("tampered") || a.Action.ToLower().Contains("intrusion") || 
                                      a.Action.ToLower().Contains("failure") || a.Action.ToLower().Contains("sql_injection") || 
                                      a.Action.ToLower().Contains("bypass") || a.Action.ToLower().Contains("integrityerror") ||
                                      (a.Details != null && a.Details.ToLower().Contains("tampered")) || 
                                      (a.Details != null && a.Details.ToLower().Contains("intrusion")) || 
                                      (a.Details != null && a.Details.ToLower().Contains("vi phạm")))
                                );
                            }
                            else if (selectedSeverity.Contains("Thông tin"))
                            {
                                query = query.Where(a => 
                                    !(a.Action.ToLower().Contains("tampered") || a.Action.ToLower().Contains("intrusion") || 
                                      a.Action.ToLower().Contains("failure") || a.Action.ToLower().Contains("sql_injection") || 
                                      a.Action.ToLower().Contains("bypass") || a.Action.ToLower().Contains("integrityerror") ||
                                      (a.Details != null && a.Details.ToLower().Contains("tampered")) || 
                                      (a.Details != null && a.Details.ToLower().Contains("intrusion")) || 
                                      (a.Details != null && a.Details.ToLower().Contains("vi phạm")))
                                    &&
                                    !(a.Action.ToLower().Contains("rejected") || a.Action.ToLower().Contains("emergency") || 
                                      a.Action.ToLower().Contains("blocked") || a.Action.ToLower().Contains("reset_password") || 
                                      a.Action.ToLower().Contains("limit_exceeded") || a.Action.ToLower().Contains("unauthorized") ||
                                      (a.Details != null && a.Details.ToLower().Contains("từ chối")) || 
                                      (a.Details != null && a.Details.ToLower().Contains("khẩn cấp")))
                                );
                            }

                            var logs = query.OrderByDescending(a => a.Timestamp).ToList();

                            // Sử dụng thư viện OfficeOpenXml (EPPlus) có sẵn trong dự án
                            OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization("QA SmartClass");
                            using (var package = new OfficeOpenXml.ExcelPackage())
                            {
                                var ws = package.Workbook.Worksheets.Add("Nhật ký hoạt động");

                                // 1. Thêm Header định danh trường học
                                var config = AppConfig.Load();
                                string schoolName = config.SchoolName ?? "Trường THPT QA";
                                string schoolCode = config.SchoolCode ?? "";

                                ws.Cells[1, 1].Value = "Trường học: " + schoolName;
                                ws.Cells[1, 1].Style.Font.Bold = true;
                                ws.Cells[1, 1].Style.Font.Size = 12;

                                if (!string.IsNullOrEmpty(schoolCode))
                                {
                                    ws.Cells[2, 1].Value = "Mã trường (Bộ GD&ĐT): " + schoolCode;
                                    ws.Cells[2, 1].Style.Font.Bold = true;
                                    ws.Cells[2, 1].Style.Font.Size = 11;
                                }

                                ws.Cells[3, 1].Value = "BÁO CÁO LỊCH SỬ HOẠT ĐỘNG HỆ THỐNG";
                                ws.Cells[3, 1].Style.Font.Bold = true;
                                ws.Cells[3, 1].Style.Font.Size = 14;
                                ws.Cells[3, 1].Style.Font.Color.SetColor(System.Drawing.Color.DarkBlue);

                                // 2. Tiêu đề cột chính (Dòng 5)
                                ws.Cells[5, 1].Value = "Mã";
                                ws.Cells[5, 2].Value = "Thời gian thực hiện";
                                ws.Cells[5, 3].Value = "Hành động / Tác vụ";
                                ws.Cells[5, 4].Value = "Người dùng thực hiện";
                                ws.Cells[5, 5].Value = "Chi tiết thay đổi";
                                ws.Cells[5, 6].Value = "Mã bảo mật (SHA256)";
                                ws.Cells[5, 7].Value = "Mức độ";

                                // Trang trí Header bảng chính
                                for (int i = 1; i <= 7; i++)
                                {
                                    ws.Cells[5, i].Style.Font.Bold = true;
                                    ws.Cells[5, i].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                    ws.Cells[5, i].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                                    ws.Cells[5, i].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                                }

                                // 3. Điền dữ liệu (Bắt đầu từ dòng 6)
                                int row = 6;
                                var translator = new ActionTranslationConverter();
                                foreach (var log in logs)
                                {
                                    ws.Cells[row, 1].Value = log.Id;
                                    ws.Cells[row, 2].Value = log.Timestamp.ToString("dd/MM/yyyy HH:mm:ss");
                                    
                                    // Dịch nghĩa hành động kỹ thuật sang tiếng Việt trong file Excel báo cáo
                                    var translatedAction = translator.Convert(log.Action, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
                                    ws.Cells[row, 3].Value = translatedAction;
                                    
                                    ws.Cells[row, 4].Value = log.ActorName;
                                    ws.Cells[row, 5].Value = log.Details;
                                    ws.Cells[row, 6].Value = log.RowHash;

                                    string sev = log.Severity;
                                    string translatedSev = "Thông tin";
                                    if (sev == "Warning") translatedSev = "Cảnh báo";
                                    else if (sev == "Critical") translatedSev = "Nguy cấp";
                                    ws.Cells[row, 7].Value = translatedSev;

                                    // Tô màu nền theo độ nghiêm trọng
                                    if (sev == "Critical")
                                    {
                                        for (int col = 1; col <= 7; col++)
                                        {
                                            ws.Cells[row, col].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                            ws.Cells[row, col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(252, 232, 230)); // #FCE8E6 (Light Red)
                                        }
                                    }
                                    else if (sev == "Warning")
                                    {
                                        for (int col = 1; col <= 7; col++)
                                        {
                                            ws.Cells[row, col].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                            ws.Cells[row, col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(254, 247, 224)); // #FEF7E0 (Light Orange)
                                        }
                                    }

                                    for (int col = 1; col <= 7; col++)
                                    {
                                        ws.Cells[row, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                                    }
                                    row++;
                                }

                                ws.Cells.AutoFitColumns();
                                package.SaveAs(new FileInfo(path));
                            }

                            Dispatcher.Invoke(() =>
                            {
                                btnExport.IsEnabled = true;
                                MessageBox.Show("Xuất báo cáo Excel thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            btnExport.IsEnabled = true;
                            MessageBox.Show($"Lỗi xuất Excel: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                });
            }
        }
    }
}
