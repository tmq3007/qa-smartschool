using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.YouthUnion.Services;
using System.Threading.Tasks;

namespace QASmartClass.YouthUnion.Views
{
    public partial class VotingView : Page
    {
        private readonly YouthUnionService _service;

        public VotingView()
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
                var allVotings = await _service.GetAllAsync<YouthVoting>();
                var query = allVotings.AsQueryable();
                if (CbFilterStatus?.SelectedItem is ComboBoxItem cbi && cbi.Tag?.ToString() != "All")
                {
                    var status = cbi.Tag.ToString();
                    query = query.Where(v => v.Status == status);
                }
                var list = query.OrderByDescending(v => v.StartDate).ToList()
                    .Select(v => new
                    {
                        v.Id, v.Title, v.StartDate, v.EndDate,
                        Status = v.Status == "Open" ? "Đang diễn ra" : "Đã kết thúc",
                        v.Candidates,
                        CanClose = v.Status == "Open"
                    }).ToList();
                DgVotings.ItemsSource = list;
            }
            catch (Exception ex) { Log.Error(ex, "Voting load error"); MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private async void CbFilterStatus_Changed(object sender, SelectionChangedEventArgs e)
        {
            await LoadDataAsync();
        }

        private void BtnNewVoting_Click(object sender, RoutedEventArgs e)
        {
            var w = new Window { Title = "Tạo Phiên Bình chọn", Width = 400, Height = 400, WindowStartupLocation = WindowStartupLocation.CenterScreen, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
            var sp = new StackPanel { Margin = new Thickness(20) };

            sp.Children.Add(new TextBlock { Text = "Tiêu đề:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            var txtTitle = new TextBox { Height = 32, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
            sp.Children.Add(txtTitle);

            sp.Children.Add(new TextBlock { Text = "Mô tả:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
            var txtDesc = new TextBox { Height = 50, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Padding = new Thickness(8) };
            sp.Children.Add(txtDesc);

            sp.Children.Add(new TextBlock { Text = "Ứng cử viên (cách nhau bằng dấu phẩy):", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
            var txtCandidates = new TextBox { Height = 50, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Padding = new Thickness(8) };
            sp.Children.Add(txtCandidates);

            sp.Children.Add(new TextBlock { Text = "Ngày kết thúc:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
            var dpEnd = new DatePicker { SelectedDate = DateTime.Today.AddDays(7), Height = 32 };
            sp.Children.Add(dpEnd);

            var btnSave = new Button { Content = "Tạo", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(15, 10, 15, 10), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)), Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
            sp.Children.Add(btnSave);

            btnSave.Click += async (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtTitle.Text) || string.IsNullOrWhiteSpace(txtCandidates.Text))
                {
                    MessageBox.Show("Vui lòng nhập tiêu đề và ứng cử viên.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning); return;
                }
                try
                {
                    await _service.AddAsync(new YouthVoting
                    {
                        Title = txtTitle.Text.Trim(),
                        Description = txtDesc.Text.Trim(),
                        Candidates = txtCandidates.Text.Trim(),
                        StartDate = DateTime.Today,
                        EndDate = dpEnd.SelectedDate ?? DateTime.Today.AddDays(7),
                        Status = "Open",
                        CreatedBy = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "Bí thư"
                    });
                    w.Close(); await LoadDataAsync();
                }
                catch (Exception ex) { Log.Error(ex, "Create voting error"); MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            };
            w.Content = sp; w.Owner = Window.GetWindow(this); w.ShowDialog();
        }

        private async void BtnCloseVoting_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                try
                {
                    var allVotings = await _service.GetAllAsync<YouthVoting>();
                    var voting = allVotings.FirstOrDefault(v => v.Id == id);
                    if (voting != null) { voting.Status = "Closed"; await _service.UpdateAsync(voting); await LoadDataAsync(); }
                }
                catch (Exception ex) { Log.Error(ex, "Close voting error"); MessageBox.Show($"Lỗi đóng phiên bầu cử: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }
        private async void BtnEditVoting_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var allVotings = await _service.GetAllAsync<YouthVoting>();
                var item = allVotings.FirstOrDefault(v => v.Id == id);
                if (item == null) return;
                var w = new Window { Title = "Sửa Phiên Bình chọn", Width = 400, Height = 450, WindowStartupLocation = WindowStartupLocation.CenterScreen, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
                var sp = new StackPanel { Margin = new Thickness(20) };

                sp.Children.Add(new TextBlock { Text = "Tiêu đề:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
                var txtTitle = new TextBox { Text = item.Title, Height = 32, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
                sp.Children.Add(txtTitle);

                sp.Children.Add(new TextBlock { Text = "Mô tả:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
                var txtDesc = new TextBox { Text = item.Description, Height = 50, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Padding = new Thickness(8) };
                sp.Children.Add(txtDesc);

                sp.Children.Add(new TextBlock { Text = "Ứng cử viên (cách nhau bằng dấu phẩy):", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
                var txtCandidates = new TextBox { Text = item.Candidates, Height = 50, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Padding = new Thickness(8) };
                sp.Children.Add(txtCandidates);

                sp.Children.Add(new TextBlock { Text = "Ngày kết thúc:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
                var dpEnd = new DatePicker { SelectedDate = item.EndDate, Height = 32 };
                sp.Children.Add(dpEnd);

                sp.Children.Add(new TextBlock { Text = "Trạng thái:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
                var cbStatus = new ComboBox { Height = 32 };
                cbStatus.Items.Add(new ComboBoxItem { Content = "Đang diễn ra", Tag = "Open" });
                cbStatus.Items.Add(new ComboBoxItem { Content = "Đã kết thúc", Tag = "Closed" });
                cbStatus.SelectedIndex = item.Status == "Open" ? 0 : 1;
                sp.Children.Add(cbStatus);

                var btnSave = new Button { Content = "Lưu thay đổi", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(15, 10, 15, 10), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)), Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
                sp.Children.Add(btnSave);

                btnSave.Click += async (s, ev) =>
                {
                    if (string.IsNullOrWhiteSpace(txtTitle.Text) || string.IsNullOrWhiteSpace(txtCandidates.Text))
                    {
                        MessageBox.Show("Vui lòng nhập tiêu đề và ứng cử viên.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning); return;
                    }
                    try
                    {
                        item.Title = txtTitle.Text.Trim();
                        item.Description = txtDesc.Text.Trim();
                        item.Candidates = txtCandidates.Text.Trim();
                        item.EndDate = dpEnd.SelectedDate ?? item.EndDate;
                        item.Status = (cbStatus.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? item.Status;
                        await _service.UpdateAsync(item); w.Close(); await LoadDataAsync();
                    }
                    catch (Exception ex) { Log.Error(ex, "Edit voting error"); MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
                };
                w.Content = sp; w.Owner = Window.GetWindow(this); w.ShowDialog();
            }
        }

        private async void BtnDeleteVoting_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (MessageBox.Show("Xác nhận xóa phiên bầu cử này?", "Xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _service.DeleteAsync<YouthVoting>(id); await LoadDataAsync();
                    }
                    catch (Exception ex) { Log.Error(ex, "Delete voting error"); MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
            }
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;
            try
            {
                OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization("QA SmartClass");
                var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "DanhSachBauCu.xlsx" };
                if (dlg.ShowDialog() == true)
                {
                    byte[] fileBytes = null;
                    using (var package = new OfficeOpenXml.ExcelPackage())
                    {
                        QASmartClass.YouthUnion.Services.YouthExportHelper.EncryptWorkbook(package);
                        var ws = package.Workbook.Worksheets.Add("BauCu");
                        ws.Cells["A1"].Value = "STT"; ws.Cells["B1"].Value = "Tiêu đề"; ws.Cells["C1"].Value = "Từ ngày";
                        ws.Cells["D1"].Value = "Đến ngày"; ws.Cells["E1"].Value = "Trạng thái"; ws.Cells["F1"].Value = "Ứng cử viên";
                        ws.Cells["A1:F1"].Style.Font.Bold = true;
                        var allVotings = await _service.GetAllAsync<YouthVoting>();
                        var items = allVotings.OrderByDescending(v => v.StartDate).ToList();
                        int row = 2;
                        foreach (var v in items)
                        {
                            ws.Cells[row, 1].Value = row - 1; ws.Cells[row, 2].Value = v.Title;
                            ws.Cells[row, 3].Value = v.StartDate.ToString("dd/MM/yyyy"); ws.Cells[row, 4].Value = v.EndDate.ToString("dd/MM/yyyy");
                             ws.Cells[row, 5].Value = v.Status == "Closed" ? "Đã kết thúc" : "Đang diễn ra"; ws.Cells[row, 6].Value = v.Candidates; row++;
                        }
                        ws.Cells[ws.Dimension.Address].AutoFitColumns();
                        fileBytes = package.GetAsByteArray();
                    }
                    await Task.Run(() => System.IO.File.WriteAllBytes(dlg.FileName, fileBytes));
                    MessageBox.Show("Xuất dữ liệu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { Log.Error(ex, "Export voting error"); MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }
    }

    public class VotingStatusConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "Open" => "Đang diễn ra",
                    "Closed" => "Đã kết thúc",
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
