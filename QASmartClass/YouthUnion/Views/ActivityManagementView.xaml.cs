using QASmartClass.Data;
using System.Globalization;
using QASmartClass.Services;
using QASmartClass.YouthUnion.Services;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace QASmartClass.YouthUnion.Views
{
    public partial class ActivityManagementView : Page
    {
        private readonly YouthUnionService _service;

        public ActivityManagementView()
        {
            InitializeComponent();
            _service = new YouthUnionService();
Unloaded += (s, e) => { _service?.Dispose(); };
            Loaded += async (_, __) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (_service == null) return;
            try
            {
                var allActs = await _service.GetAllAsync<YouthActivity>();
                var query = allActs.AsQueryable();
                if (CbFilterStatus?.SelectedItem is ComboBoxItem cbi && cbi.Tag?.ToString() != "All")
                {
                    var status = cbi.Tag.ToString();
                    query = query.Where(a => a.Status == status);
                }
                DgActivities.ItemsSource = query.OrderByDescending(a => a.Date).ToList();
            }
            catch (Exception ex) { Log.Warning("YouthActivity Load error: {Err}", ex.Message); }
        }

        private async void CbFilterStatus_Changed(object sender, SelectionChangedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadDataAsync();

        private void BtnAdd_Click(object sender, RoutedEventArgs e) => ShowActivityForm(null);

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var allActs = await _service.GetAllAsync<YouthActivity>();
                var act = allActs.FirstOrDefault(a => a.Id == id);
                if (act != null) ShowActivityForm(act);
            }
        }

        private async void DgActivities_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DgActivities.SelectedItem is YouthActivity a)
            {
                var allActs = await _service.GetAllAsync<YouthActivity>();
                var act = allActs.FirstOrDefault(x => x.Id == a.Id);
                if (act != null) ShowActivityForm(act);
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (MessageBox.Show("Xác nhận xóa hoạt động này?", "Xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _service.DeleteAsync<YouthActivity>(id);
                        await LoadDataAsync();
                    }
                    catch (Exception ex) { Log.Error(ex, "Loi xoa YouthActivity {Id}", id); }
                }
            }
        }

        private void ShowActivityForm(YouthActivity? existing)
        {
            var isEdit = existing != null;
            var w = new Window
            {
                Title = isEdit ? "Sửa Hoạt động" : "Thêm Hoạt động mới",
                Width = 520, Height = 620,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            var sp = new StackPanel { Margin = new Thickness(25) };
            var txtTitle = AddField(sp, "Tên hoạt động:", existing?.Title ?? "");
            var txtLocation = AddField(sp, "Địa điểm:", existing?.Location ?? "");
            var txtParticipants = AddField(sp, "Thành phần tham gia:", existing?.Participants ?? "");
            var txtDesc = AddField(sp, "Mô tả:", existing?.Description ?? "");
            var txtPoints = AddField(sp, "Điểm thi đua:", existing?.Points.ToString() ?? "0");
            var txtBudget = AddField(sp, "Ngân sách (VND):", existing?.Budget.ToString("0") ?? "0");

            var dpDate = new DatePicker { SelectedDate = existing?.Date ?? DateTime.Today, Height = 32, Margin = new Thickness(0, 0, 0, 10) };
            sp.Children.Insert(2, new TextBlock { Text = "Ngày tổ chức:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 3) });
            sp.Children.Insert(3, dpDate);

            var cbStatus = new ComboBox { Height = 32, Margin = new Thickness(0, 0, 0, 10) };
            cbStatus.Items.Add("Bản nháp"); cbStatus.Items.Add("Đã duyệt"); cbStatus.Items.Add("Hoàn thành");
            cbStatus.SelectedItem = existing?.Status == "Approved" ? "Đã duyệt" : existing?.Status == "Completed" ? "Hoàn thành" : "Bản nháp";
            sp.Children.Add(new TextBlock { Text = "Trạng thái:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 3) });
            sp.Children.Add(cbStatus);

            // Upload evidence
            string evidencePath = existing?.Evidence ?? "";
            var btnUpload = new Button
            {
                Content = string.IsNullOrEmpty(evidencePath) ? "📎 Tải lên minh chứng" : "📎 " + System.IO.Path.GetFileName(evidencePath),
                Padding = new Thickness(15, 8, 15, 8),
                Margin = new Thickness(0, 0, 0, 15),
                Cursor = Cursors.Hand
            };
            btnUpload.Click += (s, ev) =>
            {
                var dlg = new OpenFileDialog { Filter = "Image/PDF|*.jpg;*.jpeg;*.png;*.pdf|All|*.*" };
                if (dlg.ShowDialog() == true)
                {
                    try
                    {
                        var destDir = System.IO.Path.Combine(AppPaths.RootDir, "YouthEvidence");
                        System.IO.Directory.CreateDirectory(destDir);
                        var destFile = System.IO.Path.Combine(destDir, System.IO.Path.GetFileName(dlg.FileName));
                        System.IO.File.Copy(dlg.FileName, destFile, true);
                        evidencePath = destFile;
                        btnUpload.Content = "📎 " + System.IO.Path.GetFileName(destFile);
                    }
                    catch (Exception ex) { Log.Error(ex, "Upload evidence error"); }
                }
            };
            sp.Children.Add(btnUpload);

            var btnSave = new Button
            {
                Content = isEdit ? "💾 Lưu thay đổi" : "➕ Thêm mới",
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)),
                Foreground = System.Windows.Media.Brushes.White,
                Padding = new Thickness(20, 10, 20, 10),
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.Bold, FontSize = 15, Cursor = Cursors.Hand
            };
            sp.Children.Add(btnSave);

            btnSave.Click += async (s, ev) =>
            {
                var title = txtTitle.Text.Trim();
                if (string.IsNullOrWhiteSpace(title)) { MessageBox.Show("Vui lòng nhập tên hoạt động.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (!int.TryParse(txtPoints.Text, out int pts) || pts < 0)
                {
                    MessageBox.Show("Điểm thi đua phải là số nguyên không âm.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!decimal.TryParse(txtBudget.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal budget) || budget < 0)
                {
                    MessageBox.Show("Ngân sách phải là số không âm.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {

                    if (isEdit)
                    {
                        existing!.Title = txtTitle.Text.Trim();
                        existing.Location = txtLocation.Text.Trim();
                        existing.Participants = txtParticipants.Text.Trim();
                        existing.Description = txtDesc.Text.Trim();
                        existing.Points = pts;
                        existing.Budget = budget;
                        existing.Date = dpDate.SelectedDate ?? DateTime.Today;
                        existing.Status = cbStatus.SelectedItem?.ToString() == "Đã duyệt" ? "Approved" : cbStatus.SelectedItem?.ToString() == "Hoàn thành" ? "Completed" : "Draft";
                        existing.Evidence = evidencePath;
                        await _service.UpdateAsync(existing);
                    }
                    else
                    {
                        await _service.AddAsync(new YouthActivity
                        {
                            Title = txtTitle.Text.Trim(),
                            Location = txtLocation.Text.Trim(),
                            Participants = txtParticipants.Text.Trim(),
                            Description = txtDesc.Text.Trim(),
                            Points = pts, Budget = budget,
                            Date = dpDate.SelectedDate ?? DateTime.Today,
                            Status = cbStatus.SelectedItem?.ToString() == "Đã duyệt" ? "Approved" : cbStatus.SelectedItem?.ToString() == "Hoàn thành" ? "Completed" : "Draft",
                            Evidence = evidencePath,
                            CreatedBy = Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "System"
                        });
                    }
                    w.Close();
                    await LoadDataAsync();
                }
                catch (Exception ex) { Log.Error(ex, "Loi luu YouthActivity"); MessageBox.Show("Đã xảy ra lỗi khi lưu hoạt động. Vui lòng thử lại sau.\n\nChi tiết: " + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error); }
            };

            w.Content = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            w.Owner = Window.GetWindow(this);
            w.ShowDialog();
        }

        private TextBox AddField(StackPanel parent, string label, string value)
        {
            parent.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 3) });
            var tb = new TextBox { Text = value, Height = 32, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 10), FontSize = 14 };
            parent.Children.Add(tb);
            return tb;
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;
            try
            {
                OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization("QA SmartClass");
                var dlg = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = "DanhSachHoatDongDoanDoi.xlsx" };
                if (dlg.ShowDialog() == true)
                {
                    byte[] fileBytes = null;
                    using (var package = new OfficeOpenXml.ExcelPackage())
                    {
                        QASmartClass.YouthUnion.Services.YouthExportHelper.EncryptWorkbook(package);
                        var ws = package.Workbook.Worksheets.Add("HoatDong");
                        ws.Cells["A1"].Value = "STT";
                        ws.Cells["B1"].Value = "Ngày";
                        ws.Cells["C1"].Value = "Tên Hoạt động";
                        ws.Cells["D1"].Value = "Địa điểm";
                        ws.Cells["E1"].Value = "Thành phần";
                        ws.Cells["F1"].Value = "Trạng thái";
                        ws.Cells["G1"].Value = "Điểm thi đua";
                        ws.Cells["H1"].Value = "Ngân sách";
                        ws.Cells["A1:H1"].Style.Font.Bold = true;

                        var items = DgActivities.ItemsSource as System.Collections.IEnumerable;
                        int row = 2;
                        if (items != null)
                        {
                            foreach (dynamic act in items)
                            {
                                ws.Cells[row, 1].Value = row - 1;
                                ws.Cells[row, 2].Value = act.Date.ToString("dd/MM/yyyy");
                                ws.Cells[row, 3].Value = act.Title;
                                ws.Cells[row, 4].Value = act.Location;
                                ws.Cells[row, 5].Value = act.Participants;
                                ws.Cells[row, 6].Value = act.Status;
                                ws.Cells[row, 7].Value = act.Points;
                                ws.Cells[row, 8].Value = act.Budget;
                                row++;
                            }
                        }
                        ws.Cells[ws.Dimension.Address].AutoFitColumns();
                        fileBytes = package.GetAsByteArray();
                    }
                    await Task.Run(() => System.IO.File.WriteAllBytes(dlg.FileName, fileBytes));
                    MessageBox.Show("Xuất dữ liệu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) 
            { 
                Log.Error(ex, "Export Excel error"); 
                MessageBox.Show("Lỗi xuất Excel: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); 
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private async void BtnNotify_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int activityId)
            {
                var result = MessageBox.Show("Gửi thông báo nhắc nhở đến tất cả đoàn viên tham gia?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var act = await _service.GetByIdAsync<QASmartClass.Data.YouthActivity>(activityId);
                        if (act != null)
                        {
                            await _service.SendBulkNotificationAsync(activityId, $"Nhắc nhở: Sắp tới sẽ diễn ra hoạt động '{act.Title}' vào ngày {act.Date:dd/MM/yyyy}. Đề nghị các đồng chí tham gia đầy đủ.", "Ban Chấp Hành");
                            MessageBox.Show("Đã gửi thông báo thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error sending bulk notification for activity {ActivityId}", activityId);
                        MessageBox.Show($"Có lỗi xảy ra: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }

    public class YouthActivityStatusConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "Draft" => "Bản nháp",
                    "Approved" => "Đã duyệt",
                    "Completed" => "Hoàn thành",
                    _ => status
                };
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

