using OfficeOpenXml;
using System.Globalization;
using Microsoft.Win32;
using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using QASmartClass.YouthUnion.Services;
using System.Threading.Tasks;

namespace QASmartClass.YouthUnion.Views
{
    public partial class FeeTrackerView : Page
    {
        private readonly YouthUnionService _service;

        public FeeTrackerView()
        {
            InitializeComponent();
            _service = new YouthUnionService();
Unloaded += (s, e) => { _service?.Dispose(); };
            Loaded += async (_, __) => await LoadDataAsync();
        }

        private string GetPeriod() => (CbPeriod?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "HK1-2026";

        private async void CbFilter_Changed(object sender, SelectionChangedEventArgs e) => await LoadDataAsync();

        private async Task LoadDataAsync()
        {
            if (_service == null) return;
            try
            {
                var period = GetPeriod();
                var allMembers = await _service.GetAllAsync<YouthMember>();
                var members = allMembers.Where(m => m.Status == "Active" && (m.MemberType == "Doan vien" || m.MemberType == "Đoàn viên")).ToList();
                var allFees = await _service.GetAllAsync<YouthFee>();
                var fees = allFees.Where(f => f.Period == period).ToList();

                var display = members.Select(m =>
                {
                    var fee = fees.FirstOrDefault(f => f.MemberId == m.Id);
                    return new
                    {
                        Id = fee?.Id ?? 0,
                        m.ClassName,
                        m.StudentName,
                        FeeDisplay = fee?.Status == "Paid" ? "Đã đóng" : "Chưa đóng",
                        Amount = fee?.Amount ?? 0,
                        PaidDateStr = fee?.PaidDate?.ToString("dd/MM/yyyy") ?? ""
                    };
                }).OrderBy(x => x.ClassName).ThenBy(x => x.StudentName).ToList();

                var statusFilter = (CbStatusFilter?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
                if (statusFilter == "Paid") display = display.Where(x => x.FeeDisplay == "Đã đóng").ToList();
                else if (statusFilter == "Unpaid") display = display.Where(x => x.FeeDisplay == "Chưa đóng").ToList();

                DgFees.ItemsSource = display;

                var paid = display.Count(x => x.FeeDisplay == "Đã đóng");
                var total = display.Count;
                var sum = display.Sum(x => x.Amount);
                TxtSummary.Text = $"Đã đóng: {paid}/{total} | Tổng thu: {sum:N0} VND";
            }
            catch (Exception ex) { Log.Error(ex, "FeeTracker load error"); MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private async void BtnBulkCollect_Click(object sender, RoutedEventArgs e)
        {
            var w = new Window { Title = "Đóng phí hàng loạt", Width = 450, Height = 550, Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
            var sp = new StackPanel { Margin = new Thickness(20) };

            var allMembers = await _service.GetAllAsync<YouthMember>();
            var allFees = await _service.GetAllAsync<YouthFee>();
            var unpaidMembers = allMembers.Where(m => m.Status == "Active" && (m.MemberType == "Doan vien" || m.MemberType == "Đoàn viên")).ToList()
                .Where(m => !allFees.Any(f => f.MemberId == m.Id && f.Period == GetPeriod() && f.Status == "Paid"))
                .Select(m => new { m.Id, m.ClassName, DisplayName = $"{m.StudentName} ({m.ClassName})" }).ToList();

            sp.Children.Add(new TextBlock { Text = "Chọn lớp:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            var cbClass = new ComboBox { Height = 32, Margin = new Thickness(0, 0, 0, 10) };
            var classes = unpaidMembers.Select(m => m.ClassName).Distinct().OrderBy(c => c).ToList();
            cbClass.Items.Add("Tất cả");
            foreach (var c in classes) cbClass.Items.Add(c);
            cbClass.SelectedIndex = 0;
            sp.Children.Add(cbClass);

            sp.Children.Add(new TextBlock { Text = "Chọn các đoàn viên chưa đóng:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            var lstMembers = new ListBox { Height = 250, SelectionMode = SelectionMode.Extended, DisplayMemberPath = "DisplayName" };
            lstMembers.ItemsSource = unpaidMembers;
            sp.Children.Add(lstMembers);

            cbClass.SelectionChanged += (s, ev) =>
            {
                var selClass = cbClass.SelectedItem?.ToString();
                if (selClass == "Tất cả" || string.IsNullOrEmpty(selClass))
                    lstMembers.ItemsSource = unpaidMembers;
                else
                    lstMembers.ItemsSource = unpaidMembers.Where(m => m.ClassName == selClass).ToList();
            };

            sp.Children.Add(new TextBlock { Text = "Số tiền (VND):", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 15, 0, 5) });
            var txtAmt = new TextBox { Text = "20000", Height = 32, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center, FontSize = 14 };
            sp.Children.Add(txtAmt);

            var btnSave = new Button
            {
                Content = "Ghi nhận đóng phí", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(15, 10, 15, 10),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)),
                Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand
            };
            sp.Children.Add(btnSave);

            btnSave.Click += async (s, ev) =>
            {
                var selected = lstMembers.SelectedItems;
                if (selected.Count == 0) { MessageBox.Show("Vui lòng chọn ít nhất 1 đoàn viên.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (!decimal.TryParse(txtAmt.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal amt) || amt <= 0) { MessageBox.Show("Vui lòng nhập số tiền hợp lệ.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                try
                {
                    btnSave.IsEnabled = false;
                    foreach (dynamic sel in selected)
                    {
                        await _service.AddAsync(new YouthFee
                        {
                            MemberId = sel.Id, Amount = amt, Period = GetPeriod(),
                            PaidDate = DateTime.Today, Status = "Paid",
                            CollectedBy = Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "System"
                        });
                    }
                    w.Close();
                    await LoadDataAsync();
                    MessageBox.Show($"Đã ghi nhận đóng phí thành công cho {selected.Count} đoàn viên.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { Log.Error(ex, "Bulk collect error"); MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
                finally { btnSave.IsEnabled = true; }
            };
            w.Content = sp;
            w.ShowDialog();
        }

        private async void BtnCollect_Click(object sender, RoutedEventArgs e)
        {
            var w = new Window { Title = "Ghi nhận đóng phí", Width = 400, Height = 320, Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
            var sp = new StackPanel { Margin = new Thickness(20) };

            sp.Children.Add(new TextBlock { Text = "Chọn Đoàn viên:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            var cbMember = new ComboBox { Height = 32, DisplayMemberPath = "DisplayName" };
            var allMembers = await _service.GetAllAsync<YouthMember>();
            var allFees = await _service.GetAllAsync<YouthFee>();
            var unpaidMembers = allMembers.Where(m => m.Status == "Active" && (m.MemberType == "Doan vien" || m.MemberType == "Đoàn viên")).ToList()
                .Where(m => !allFees.Any(f => f.MemberId == m.Id && f.Period == GetPeriod() && f.Status == "Paid"))
                .Select(m => new { m.Id, DisplayName = $"{m.StudentName} ({m.ClassName})" }).ToList();
            cbMember.ItemsSource = unpaidMembers;
            if (unpaidMembers.Any()) cbMember.SelectedIndex = 0;
            sp.Children.Add(cbMember);

            sp.Children.Add(new TextBlock { Text = "Số tiền:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
            var txtAmt = new TextBox { Text = "20000", Height = 32, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center, FontSize = 14 };
            sp.Children.Add(txtAmt);

            var btnSave = new Button
            {
                Content = "Ghi nhận", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(15, 10, 15, 10),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)),
                Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand
            };
            sp.Children.Add(btnSave);

            btnSave.Click += async (s, ev) =>
            {
                dynamic? sel = cbMember.SelectedItem;
                if (sel == null) { MessageBox.Show("Vui lòng chọn đoàn viên.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (!decimal.TryParse(txtAmt.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal amt) || amt <= 0) { MessageBox.Show("Vui lòng nhập số tiền hợp lệ.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                try
                {
                    int memberId = sel.Id;
                    await _service.AddAsync(new YouthFee
                    {
                        MemberId = memberId, Amount = amt, Period = GetPeriod(),
                        PaidDate = DateTime.Today, Status = "Paid",
                        CollectedBy = Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "System"
                    });
                    w.Close();
                    await LoadDataAsync();
                }
                catch (Exception ex) { Log.Error(ex, "Fee collect error"); MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            };
            w.Content = sp;
            w.ShowDialog();
        }

        private async void BtnEditFee_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (id == 0)
                {
                    MessageBox.Show("Đoàn viên này chưa đóng phí.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var allFees = await _service.GetAllAsync<YouthFee>();
                var item = allFees.FirstOrDefault(f => f.Id == id);
                if (item == null) return;
                var w = new Window { Title = "Sửa thu phí", Width = 350, Height = 280, Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
                var sp = new StackPanel { Margin = new Thickness(20) };

                sp.Children.Add(new TextBlock { Text = "Số tiền:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
                var txtAmt = new TextBox { Text = item.Amount.ToString(), Height = 32, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
                sp.Children.Add(txtAmt);

                sp.Children.Add(new TextBlock { Text = "Trạng thái:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
                var cbStatus = new ComboBox { Height = 32 };
                cbStatus.Items.Add("Đã đóng"); cbStatus.Items.Add("Chưa đóng");
                cbStatus.SelectedItem = item.Status == "Paid" ? "Đã đóng" : "Chưa đóng";
                sp.Children.Add(cbStatus);

                var btnSave = new Button { Content = "Lưu", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(15, 10, 15, 10), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)), Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
                sp.Children.Add(btnSave);

                btnSave.Click += async (s, ev) => {
                    if (!decimal.TryParse(txtAmt.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal amt) || amt <= 0) { MessageBox.Show("Vui lòng nhập số tiền hợp lệ.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                    try {
                        item.Amount = amt;
                        item.Status = cbStatus.SelectedItem?.ToString() == "Đã đóng" ? "Paid" : "Unpaid";
                        if (item.Status == "Paid" && item.PaidDate == null) item.PaidDate = DateTime.Today;
                        if (item.Status == "Unpaid") item.PaidDate = null;
                        await _service.UpdateAsync(item); w.Close(); await LoadDataAsync();
                    }
                    catch (Exception ex) { Serilog.Log.Error(ex, "Edit fee error"); MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
                };
                w.Content = sp; w.ShowDialog();
            }
        }

        private async void BtnDeleteFee_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (id == 0)
                {
                    MessageBox.Show("Đoàn viên này chưa đóng phí.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (MessageBox.Show("Xác nhận xóa bản ghi thu phí?", "Xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try { await _service.DeleteAsync<YouthFee>(id); await LoadDataAsync(); }
                    catch (Exception ex) { Serilog.Log.Error(ex, "Delete fee error"); MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
            }
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            try
            {
                ExcelPackage.License.SetNonCommercialOrganization("QA SmartClass");
                var dlg = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = "SoQuyDoan.xlsx" };
                if (dlg.ShowDialog() == true)
                {
                    if (btn != null) btn.IsEnabled = false;
                    using var package = new ExcelPackage();
                    QASmartClass.YouthUnion.Services.YouthExportHelper.EncryptWorkbook(package);
                    var ws = package.Workbook.Worksheets.Add("SoQuy");
                    ws.Cells["A1"].Value = "STT";
                    ws.Cells["B1"].Value = "Kỳ thu";
                    ws.Cells["C1"].Value = "Lớp";
                    ws.Cells["D1"].Value = "Họ và Tên";
                    ws.Cells["E1"].Value = "Số tiền";
                    ws.Cells["F1"].Value = "Trạng thái";
                    ws.Cells["G1"].Value = "Ngày đóng";
                    ws.Cells["A1:G1"].Style.Font.Bold = true;
 
                    var items = DgFees.ItemsSource as System.Collections.IEnumerable;
                    int row = 2;
                    if (items != null)
                    {
                        foreach (dynamic f in items)
                        {
                            ws.Cells[row, 1].Value = row - 1;
                            ws.Cells[row, 2].Value = CbPeriod.SelectedItem is ComboBoxItem cbi ? cbi.Tag?.ToString() : "";
                            ws.Cells[row, 3].Value = f.ClassName;
                            ws.Cells[row, 4].Value = f.StudentName;
                            ws.Cells[row, 5].Value = f.Amount;
                            ws.Cells[row, 6].Value = f.FeeDisplay;
                            ws.Cells[row, 7].Value = f.PaidDateStr;
                            row++;
                        }
                    }
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                    var bytes = package.GetAsByteArray();
                    await Task.Run(() => File.WriteAllBytes(dlg.FileName, bytes));
                    MessageBox.Show("Xuất dữ liệu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { Log.Error(ex, "Export Excel error"); MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }
    }

    public class FeeStatusConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string status)
            {
                if (status == "Paid") return "Đã đóng";
                if (status == "Unpaid") return "Chưa đóng";
            }
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }
}

