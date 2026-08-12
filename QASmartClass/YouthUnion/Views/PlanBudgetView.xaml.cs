using QASmartClass.Data;
using System.Globalization;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.YouthUnion.Services;
using System.Threading.Tasks;

namespace QASmartClass.YouthUnion.Views
{
    public partial class PlanBudgetView : Page
    {
        private readonly YouthUnionService _service;

        public PlanBudgetView()
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
                var allPlans = await _service.GetAllAsync<YouthPlan>();
                var query = allPlans.AsQueryable();
                if (CbFilterStatus?.SelectedItem is ComboBoxItem cbi && cbi.Tag?.ToString() != "All")
                {
                    var status = cbi.Tag.ToString();
                    query = query.Where(p => p.Status == status);
                }
                DgPlans.ItemsSource = query.OrderByDescending(p => p.StartDate).ToList();
                
                var allBudgets = await _service.GetAllAsync<YouthBudget>();
                var budgets = allBudgets.OrderByDescending(b => b.Date).ToList();
                DgBudget.ItemsSource = budgets;

                var inc = budgets.Where(b => b.Type == "Income").Sum(b => b.Amount);
                var exp = budgets.Where(b => b.Type == "Expense").Sum(b => b.Amount);
                TxtTotalIncome.Text = $"{inc:N0} đ";
                TxtTotalExpense.Text = $"{exp:N0} đ";
                TxtBalance.Text = $"{(inc - exp):N0} đ";
            }
            catch (Exception ex) { Log.Error(ex, "PlanBudget load error"); MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private async void CbFilterStatus_Changed(object sender, SelectionChangedEventArgs e)
        {
            await LoadDataAsync();
        }

        private void BtnNewPlan_Click(object sender, RoutedEventArgs e)
        {
            var w = new Window { Title = "Tạo Kế hoạch", Width = 400, Height = 350, WindowStartupLocation = WindowStartupLocation.CenterScreen, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
            var sp = new StackPanel { Margin = new Thickness(20) };
            
            sp.Children.Add(new TextBlock { Text = "Tiêu đề kế hoạch:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            var txtTitle = new TextBox { Height = 32, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
            sp.Children.Add(txtTitle);

            sp.Children.Add(new TextBlock { Text = "Kỳ hạn:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
            var cbType = new ComboBox { Height = 32 };
            cbType.Items.Add("Tháng"); cbType.Items.Add("Quý"); cbType.Items.Add("Năm");
            cbType.SelectedIndex = 0;
            sp.Children.Add(cbType);

            sp.Children.Add(new TextBlock { Text = "Ngày bắt đầu:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
            var dpStart = new DatePicker { SelectedDate = DateTime.Today, Height = 32 };
            sp.Children.Add(dpStart);

            sp.Children.Add(new TextBlock { Text = "Ngày kết thúc:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
            var dpEnd = new DatePicker { SelectedDate = DateTime.Today.AddMonths(1), Height = 32 };
            sp.Children.Add(dpEnd);

            var btnSave = new Button { Content = "Lưu Kế hoạch", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(15, 10, 15, 10), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)), Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
            sp.Children.Add(btnSave);

            btnSave.Click += async (s, ev) =>
            {
                var title = txtTitle.Text.Trim();
                if (string.IsNullOrWhiteSpace(title)) { MessageBox.Show("Vui lòng nhập tiêu đề kế hoạch.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                
                var start = dpStart.SelectedDate ?? DateTime.Today;
                var end = dpEnd.SelectedDate ?? DateTime.Today.AddMonths(1);
                if (start > end)
                {
                    MessageBox.Show("Ngày bắt đầu không được lớn hơn ngày kết thúc.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    var selPeriod = cbType.SelectedItem?.ToString();
                    await _service.AddAsync(new YouthPlan
                    {
                        Title = title,
                        PeriodType = selPeriod == "Quý" ? "Quarter" : selPeriod == "Năm" ? "Year" : "Month",
                        StartDate = start,
                        EndDate = end,
                        Status = "Draft",
                        CreatedBy = Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "System",
                        CreatedAt = DateTime.Now
                    });
                    w.Close(); await LoadDataAsync();
                }
                catch (Exception ex) { Log.Error(ex, "Plan error"); MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            };
            w.Content = sp; w.Owner = Window.GetWindow(this); w.ShowDialog();
        }

        private async void BtnNewTransaction_Click(object sender, RoutedEventArgs e)
        {
            decimal currentBalance = 0;
            try
            {
                var allBudgets = await _service.GetAllAsync<YouthBudget>();
                var inc = allBudgets.Where(b => b.Type == "Income").Sum(b => b.Amount);
                var exp = allBudgets.Where(b => b.Type == "Expense").Sum(b => b.Amount);
                currentBalance = inc - exp;
            }
            catch (Exception ex) { Log.Warning("Get budgets for balance check failed: {Err}", ex.Message); }

            var w = new Window { Title = "Thêm Giao dịch", Width = 400, Height = 380, WindowStartupLocation = WindowStartupLocation.CenterScreen, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
            var sp = new StackPanel { Margin = new Thickness(20) };

            sp.Children.Add(new TextBlock { Text = "Loại:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            var cbType = new ComboBox { Height = 32 };
            cbType.Items.Add("Thu"); cbType.Items.Add("Chi"); cbType.SelectedIndex = 0;
            sp.Children.Add(cbType);

            sp.Children.Add(new TextBlock { Text = "Tiêu đề:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
            var txtTitle = new TextBox { Height = 32, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
            sp.Children.Add(txtTitle);

            sp.Children.Add(new TextBlock { Text = "Số tiền (VND):", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
            var txtAmount = new TextBox { Height = 32, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
            sp.Children.Add(txtAmount);

            var lblWarning = new TextBlock { Text = "", Foreground = System.Windows.Media.Brushes.Red, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 5), Visibility = Visibility.Collapsed };
            sp.Children.Add(lblWarning);

            sp.Children.Add(new TextBlock { Text = "Ghi chú:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 5, 0, 5) });
            var txtNote = new TextBox { Height = 50, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Padding = new Thickness(8) };
            sp.Children.Add(txtNote);

            var btnSave = new Button { Content = "Lưu Giao dịch", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(15, 10, 15, 10), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)), Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
            sp.Children.Add(btnSave);

            var checkBalance = new Action(() =>
            {
                if (cbType.SelectedItem?.ToString() == "Chi")
                {
                    string cleanVal = new string(txtAmount.Text.Where(char.IsDigit).ToArray());
                    if (decimal.TryParse(cleanVal, out decimal amt) && amt > currentBalance)
                    {
                        lblWarning.Text = $"Cảnh báo: Số tiền chi vượt quá số dư tồn quỹ hiện có ({currentBalance:N0} đ)!";
                        lblWarning.Visibility = Visibility.Visible;
                        btnSave.IsEnabled = false;
                    }
                    else
                    {
                        lblWarning.Visibility = Visibility.Collapsed;
                        btnSave.IsEnabled = true;
                    }
                }
                else
                {
                    lblWarning.Visibility = Visibility.Collapsed;
                    btnSave.IsEnabled = true;
                }
            });

            bool isFormatting = false;
            txtAmount.TextChanged += (s, ev) =>
            {
                if (isFormatting) return;
                isFormatting = true;
                string cleanVal = new string(txtAmount.Text.Where(char.IsDigit).ToArray());
                if (decimal.TryParse(cleanVal, out decimal amt))
                {
                    txtAmount.Text = amt.ToString("N0", new System.Globalization.CultureInfo("en-US"));
                    txtAmount.SelectionStart = txtAmount.Text.Length;
                }
                else
                {
                    txtAmount.Text = "";
                }
                isFormatting = false;
                checkBalance();
            };

            cbType.SelectionChanged += (s, ev) => { checkBalance(); };

            btnSave.Click += async (s, ev) =>
            {
                var title = txtTitle.Text.Trim();
                if (string.IsNullOrWhiteSpace(title)) { MessageBox.Show("Vui lòng nhập tiêu đề giao dịch.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                string cleanAmt = new string(txtAmount.Text.Where(char.IsDigit).ToArray());
                if (!decimal.TryParse(cleanAmt, out decimal amt) || amt <= 0)
                {
                    MessageBox.Show("Số tiền phải là số lớn hơn 0.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    await _service.AddBudgetTransactionAsync(new YouthBudget
                    {
                        Title = title,
                        Type = cbType.SelectedItem?.ToString() == "Chi" ? "Expense" : "Income",
                        Amount = amt,
                        Date = DateTime.Today,
                        Category = "General",
                        Note = txtNote.Text.Trim(),
                        CreatedBy = Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "System"
                    });
                    w.Close(); await LoadDataAsync();
                }
                catch (Exception ex) { Log.Error(ex, "Budget error"); MessageBox.Show("Không thể thực hiện giao dịch.\n\nChi tiết: " + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error); }
            };
            w.Content = sp; w.Owner = Window.GetWindow(this); w.ShowDialog();
        }

        private async void BtnCompletePlan_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                try
                {
                    var allPlans = await _service.GetAllAsync<YouthPlan>();
                    var plan = allPlans.FirstOrDefault(p => p.Id == id);
                    if (plan != null && plan.Status != "Completed")
                    {
                        plan.Status = "Completed";
                        await _service.UpdateAsync(plan); await LoadDataAsync();
                        MessageBox.Show("Đã đánh dấu hoàn thành!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex) { Log.Error(ex, "Complete plan error"); MessageBox.Show($"Lỗi cập nhật kế hoạch: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private async void BtnDeletePlan_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (MessageBox.Show("Xác nhận xóa kế hoạch này?", "Xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _service.DeleteAsync<YouthPlan>(id); await LoadDataAsync();
                    }
                    catch (Exception ex) { Log.Error(ex, "Delete plan error"); MessageBox.Show("Có lỗi xảy ra trong quá trình thực hiện.\n\nChi tiết: " + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
            }
        }

        private async void BtnEditPlan_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var allPlans = await _service.GetAllAsync<YouthPlan>();
                var item = allPlans.FirstOrDefault(p => p.Id == id);
                if (item == null) return;
                var w = new Window { Title = "Sửa Kế hoạch", Width = 400, Height = 400, WindowStartupLocation = WindowStartupLocation.CenterScreen, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
                var sp = new StackPanel { Margin = new Thickness(20) };

                sp.Children.Add(new TextBlock { Text = "Tiêu đề kế hoạch:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
                var txtTitle = new TextBox { Text = item.Title, Height = 32, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
                sp.Children.Add(txtTitle);

                sp.Children.Add(new TextBlock { Text = "Kỳ hạn:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
                var cbType = new ComboBox { Height = 32 };
                cbType.Items.Add("Tháng"); cbType.Items.Add("Quý"); cbType.Items.Add("Năm");
                cbType.SelectedItem = item.PeriodType == "Quarter" ? "Quý" : item.PeriodType == "Year" ? "Năm" : "Tháng";
                sp.Children.Add(cbType);

                sp.Children.Add(new TextBlock { Text = "Ngày bắt đầu:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
                var dpStart = new DatePicker { SelectedDate = item.StartDate, Height = 32 };
                sp.Children.Add(dpStart);

                sp.Children.Add(new TextBlock { Text = "Ngày kết thúc:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
                var dpEnd = new DatePicker { SelectedDate = item.EndDate, Height = 32 };
                sp.Children.Add(dpEnd);

                sp.Children.Add(new TextBlock { Text = "Trạng thái:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
                var cbStatus = new ComboBox { Height = 32 };
                cbStatus.Items.Add("Bản nháp"); cbStatus.Items.Add("Đã duyệt"); cbStatus.Items.Add("Hoàn thành");
                cbStatus.SelectedItem = item.Status == "Approved" ? "Đã duyệt" : item.Status == "Completed" ? "Hoàn thành" : "Bản nháp";
                sp.Children.Add(cbStatus);

                var btnSave = new Button { Content = "Lưu thay đổi", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(15, 10, 15, 10), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)), Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
                sp.Children.Add(btnSave);

                btnSave.Click += async (s, ev) =>
                {
                    var title = txtTitle.Text.Trim();
                    if (string.IsNullOrWhiteSpace(title)) { MessageBox.Show("Vui lòng nhập tiêu đề kế hoạch.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                    
                    var start = dpStart.SelectedDate ?? item.StartDate;
                    var end = dpEnd.SelectedDate ?? item.EndDate;
                    if (start > end)
                    {
                        MessageBox.Show("Ngày bắt đầu không được lớn hơn ngày kết thúc.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    try
                    {
                        item.Title = title;
                        var selPeriod = cbType.SelectedItem?.ToString();
                        item.PeriodType = selPeriod == "Quý" ? "Quarter" : selPeriod == "Năm" ? "Year" : "Month";
                        item.StartDate = start;
                        item.EndDate = end;
                        var selStatus = cbStatus.SelectedItem?.ToString();
                        item.Status = selStatus == "Đã duyệt" ? "Approved" : selStatus == "Hoàn thành" ? "Completed" : "Draft";
                        await _service.UpdateAsync(item); w.Close(); await LoadDataAsync();
                    }
                    catch (Exception ex) { Log.Error(ex, "Edit plan error"); MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
                };
                w.Content = sp; w.Owner = Window.GetWindow(this); w.ShowDialog();
            }
        }

        private async void BtnEditTransaction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var allBudgets = await _service.GetAllAsync<YouthBudget>();
                var item = allBudgets.FirstOrDefault(b => b.Id == id);
                if (item == null) return;

                decimal currentBalance = 0;
                try
                {
                    var inc = allBudgets.Where(b => b.Type == "Income" && b.Id != item.Id).Sum(b => b.Amount);
                    var exp = allBudgets.Where(b => b.Type == "Expense" && b.Id != item.Id).Sum(b => b.Amount);
                    currentBalance = inc - exp;
                }
                catch (Exception ex) { Log.Warning("Get budgets for balance check failed: {Err}", ex.Message); }

                var w = new Window { Title = "Sửa Giao dịch", Width = 400, Height = 400, WindowStartupLocation = WindowStartupLocation.CenterScreen, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
                var sp = new StackPanel { Margin = new Thickness(20) };

                sp.Children.Add(new TextBlock { Text = "Loại:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
                var cbType = new ComboBox { Height = 32 };
                cbType.Items.Add("Thu"); cbType.Items.Add("Chi");
                cbType.SelectedItem = item.Type == "Expense" ? "Chi" : "Thu";
                sp.Children.Add(cbType);

                sp.Children.Add(new TextBlock { Text = "Tiêu đề:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
                var txtTitle = new TextBox { Text = item.Title, Height = 32, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
                sp.Children.Add(txtTitle);

                sp.Children.Add(new TextBlock { Text = "Số tiền (VND):", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
                var txtAmount = new TextBox { Text = item.Amount.ToString("N0", new System.Globalization.CultureInfo("en-US")), Height = 32, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
                sp.Children.Add(txtAmount);

                var lblWarning = new TextBlock { Text = "", Foreground = System.Windows.Media.Brushes.Red, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 5), Visibility = Visibility.Collapsed };
                sp.Children.Add(lblWarning);

                sp.Children.Add(new TextBlock { Text = "Ghi chú:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 5, 0, 5) });
                var txtNote = new TextBox { Text = item.Note, Height = 50, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Padding = new Thickness(8) };
                sp.Children.Add(txtNote);

                var btnSave = new Button { Content = "Lưu thay đổi", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(15, 10, 15, 10), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)), Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
                sp.Children.Add(btnSave);

                var checkBalance = new Action(() =>
                {
                    if (cbType.SelectedItem?.ToString() == "Chi")
                    {
                        string cleanVal = new string(txtAmount.Text.Where(char.IsDigit).ToArray());
                        if (decimal.TryParse(cleanVal, out decimal amt) && amt > currentBalance)
                        {
                            lblWarning.Text = $"Cảnh báo: Số tiền chi vượt quá số dư tồn quỹ hiện có ({currentBalance:N0} đ)!";
                            lblWarning.Visibility = Visibility.Visible;
                            btnSave.IsEnabled = false;
                        }
                        else
                        {
                            lblWarning.Visibility = Visibility.Collapsed;
                            btnSave.IsEnabled = true;
                        }
                    }
                    else
                    {
                        lblWarning.Visibility = Visibility.Collapsed;
                        btnSave.IsEnabled = true;
                    }
                });

                checkBalance();

                bool isFormatting = false;
                txtAmount.TextChanged += (s, ev) =>
                {
                    if (isFormatting) return;
                    isFormatting = true;
                    string cleanVal = new string(txtAmount.Text.Where(char.IsDigit).ToArray());
                    if (decimal.TryParse(cleanVal, out decimal amt))
                    {
                        txtAmount.Text = amt.ToString("N0", new System.Globalization.CultureInfo("en-US"));
                        txtAmount.SelectionStart = txtAmount.Text.Length;
                    }
                    else
                    {
                        txtAmount.Text = "";
                    }
                    isFormatting = false;
                    checkBalance();
                };

                cbType.SelectionChanged += (s, ev) => { checkBalance(); };

                btnSave.Click += async (s, ev) =>
                {
                    var title = txtTitle.Text.Trim();
                    if (string.IsNullOrWhiteSpace(title)) { MessageBox.Show("Vui lòng nhập tiêu đề giao dịch.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                    string cleanAmt = new string(txtAmount.Text.Where(char.IsDigit).ToArray());
                    if (!decimal.TryParse(cleanAmt, out decimal amt) || amt <= 0)
                    {
                        MessageBox.Show("Số tiền phải là số lớn hơn 0.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    try
                    {
                        item.Title = title;
                        item.Type = cbType.SelectedItem?.ToString() == "Chi" ? "Expense" : "Income";
                        item.Amount = amt;
                        item.Note = txtNote.Text.Trim();
                        await _service.UpdateBudgetTransactionAsync(item); w.Close(); await LoadDataAsync();
                    }
                    catch (Exception ex) { Log.Error(ex, "Edit budget error"); MessageBox.Show("Không thể cập nhật giao dịch.\n\nChi tiết: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
                };
                w.Content = sp; w.Owner = Window.GetWindow(this); w.ShowDialog();
            }
        }

        private async void BtnDeleteTransaction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (MessageBox.Show("Xác nhận xóa giao dịch này?", "Xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _service.DeleteAsync<YouthBudget>(id);
                        await LoadDataAsync();
                    }
                    catch (Exception ex) { Log.Error(ex, "Delete budget error"); MessageBox.Show("Có lỗi xảy ra trong quá trình thực hiện.\n\nChi tiết: " + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
            }
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            BtnExport.IsEnabled = false;
            try
            {
                OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization("QA SmartClass");
                var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "KeHoachNganSach.xlsx" };
                if (dlg.ShowDialog() == true)
                {
                    using var package = new OfficeOpenXml.ExcelPackage();
                    QASmartClass.YouthUnion.Services.YouthExportHelper.EncryptWorkbook(package);
                    var ws1 = package.Workbook.Worksheets.Add("KeHoach");
                    ws1.Cells["A1"].Value = "STT"; ws1.Cells["B1"].Value = "Tiêu đề"; ws1.Cells["C1"].Value = "Kỳ hạn";
                    ws1.Cells["D1"].Value = "Từ ngày"; ws1.Cells["E1"].Value = "Đến ngày"; ws1.Cells["F1"].Value = "Trạng thái";
                    ws1.Cells["A1:F1"].Style.Font.Bold = true;
                    var allPlans = await _service.GetAllAsync<YouthPlan>();
                    var plans = allPlans.OrderByDescending(p => p.StartDate).ToList();
                    int row = 2;
                    foreach (var p in plans)
                    {
                        ws1.Cells[row, 1].Value = row - 1; ws1.Cells[row, 2].Value = p.Title; ws1.Cells[row, 3].Value = p.PeriodType;
                        ws1.Cells[row, 4].Value = p.StartDate.ToString("dd/MM/yyyy"); ws1.Cells[row, 5].Value = p.EndDate.ToString("dd/MM/yyyy");
                        ws1.Cells[row, 6].Value = p.Status; row++;
                    }

                    var ws2 = package.Workbook.Worksheets.Add("NganSach");
                    ws2.Cells["A1"].Value = "STT"; ws2.Cells["B1"].Value = "Tiêu đề"; ws2.Cells["C1"].Value = "Loại";
                    ws2.Cells["D1"].Value = "Số tiền"; ws2.Cells["E1"].Value = "Ngày"; ws2.Cells["F1"].Value = "Ghi chú";
                    ws2.Cells["A1:F1"].Style.Font.Bold = true;
                    var allBudgets = await _service.GetAllAsync<YouthBudget>();
                    var budgets = allBudgets.OrderByDescending(b => b.Date).ToList();
                    row = 2;
                    foreach (var b in budgets)
                    {
                        ws2.Cells[row, 1].Value = row - 1; ws2.Cells[row, 2].Value = b.Title; ws2.Cells[row, 3].Value = b.Type;
                        ws2.Cells[row, 4].Value = (double)b.Amount; ws2.Cells[row, 5].Value = b.Date.ToString("dd/MM/yyyy");
                        ws2.Cells[row, 6].Value = b.Note; row++;
                    }

                    // Tác vụ nặng xử lý EPPlus & ghi file chạy ở Background Thread
                    byte[] fileBytes = await Task.Run(() =>
                    {
                        ws1.Cells[ws1.Dimension.Address].AutoFitColumns();
                        ws2.Cells[ws2.Dimension.Address].AutoFitColumns();
                        return package.GetAsByteArray();
                    });

                    await Task.Run(() => System.IO.File.WriteAllBytes(dlg.FileName, fileBytes));
                    
                    MessageBox.Show("Xuất dữ liệu ngân sách thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Export plan/budget error");
                MessageBox.Show("Lỗi xuất Excel: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnExport.IsEnabled = true;
            }
        }
    }

    public class PlanStatusConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string status)
            {
                if (status == "Draft") return "Bản nháp";
                if (status == "Approved") return "Đã duyệt";
                if (status == "Completed") return "Hoàn thành";
            }
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    public class PeriodTypeConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string type)
            {
                if (type == "Month") return "Tháng";
                if (type == "Quarter") return "Quý";
                if (type == "Year") return "Năm";
            }
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    public class BudgetTransactionTypeConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string type)
            {
                if (type == "Income") return "Thu";
                if (type == "Expense") return "Chi";
            }
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }
}
