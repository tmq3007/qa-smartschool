using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using QASmartClass.YouthUnion.Services;

namespace QASmartClass.YouthUnion.Views
{
    public partial class AwardProposalView : Page
    {
        private readonly YouthUnionService _service;

        public AwardProposalView()
        {
            InitializeComponent();
            _service = new YouthUnionService();
            Loaded += async (_, __) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (_service == null) return;
            try
            {
                var allProposals = await _service.GetAllAsync<YouthAwardProposal>();
                var query = allProposals.AsQueryable();
                if (CbFilterStatus?.SelectedItem is ComboBoxItem cbi && cbi.Tag?.ToString() != "All")
                {
                    var status = cbi.Tag.ToString();
                    query = query.Where(p => p.Status == status);
                }
                var allMembers = await _service.GetAllAsync<YouthMember>();
                var proposals = query.OrderByDescending(p => p.CreatedAt).ToList();
                var displayProps = proposals.Select(p =>
                {
                    var m = allMembers.FirstOrDefault(x => x.Id == p.MemberId);
                    return new { p.Id, Name = p.ProposalType == "Tap the" ? "Tập thể Chi đoàn" : (m?.StudentName ?? "Chưa rõ"), p.ProposalType, p.Reason, p.Status, p.ProposedBy };
                }).ToList();
                DgProposals.ItemsSource = displayProps;

                var allRecords = await _service.GetAllAsync<YouthAwardRecord>();
                var records = allRecords.OrderByDescending(r => r.AwardDate).ToList();
                var displayRecs = records.Select(r =>
                {
                    var m = allMembers.FirstOrDefault(x => x.Id == r.MemberId);
                    return new { StudentName = r.MemberId == 0 ? "Tập thể Chi đoàn" : (m?.StudentName ?? "Chưa rõ"), r.AwardTitle, r.DecisionNo, r.AwardDate };
                }).ToList();
                DgRecords.ItemsSource = displayRecs;
            }
            catch (Exception ex) 
            { 
                Log.Error(ex, "Award load error"); 
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); 
            }
        }

        private async void CbFilterStatus_Changed(object sender, SelectionChangedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async void BtnNewProposal_Click(object sender, RoutedEventArgs e)
        {
            var w = new Window { Title = "Tạo đề xuất", Width = 400, Height = 350, WindowStartupLocation = WindowStartupLocation.CenterScreen, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
            var sp = new StackPanel { Margin = new Thickness(20) };
            
            sp.Children.Add(new TextBlock { Text = "Loại:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            var cbType = new ComboBox { Height = 32, Margin = new Thickness(0, 0, 0, 10) };
            cbType.Items.Add("Cá nhân"); cbType.Items.Add("Tập thể"); cbType.SelectedIndex = 0;
            sp.Children.Add(cbType);

            sp.Children.Add(new TextBlock { Text = "Thành viên (nếu cá nhân):", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            var cbMember = new ComboBox { Height = 32, DisplayMemberPath = "DisplayName", Margin = new Thickness(0, 0, 0, 10) };
            var allMembers = await _service.GetAllAsync<YouthMember>();
            var members = allMembers.Where(m => m.Status == "Active").Select(m => new { m.Id, DisplayName = $"{m.StudentName} ({m.ClassName})" }).ToList();
            cbMember.ItemsSource = members; if (members.Any()) cbMember.SelectedIndex = 0;
            sp.Children.Add(cbMember);

            sp.Children.Add(new TextBlock { Text = "Lý do / Thành tích:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            var txtReason = new TextBox { Height = 60, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Padding = new Thickness(8) };
            sp.Children.Add(txtReason);

            var btnSave = new Button { Content = "Gửi Đề xuất", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(15, 10, 15, 10), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)), Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
            sp.Children.Add(btnSave);

            btnSave.Click += async (s, ev) =>
            {
                dynamic? sel = cbMember.SelectedItem;
                try
                {
                    string pType = cbType.SelectedItem?.ToString() == "Tập thể" ? "Tap the" : "Ca nhan";
                    await _service.AddAsync(new YouthAwardProposal
                    {
                        MemberId = pType == "Tap the" ? 0 : (sel?.Id ?? 0),
                        ProposalType = pType,
                        Reason = txtReason.Text.Trim(),
                        Status = "Pending",
                        ProposedBy = Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "System",
                        CreatedAt = DateTime.Now
                    });
                    w.Close(); await LoadDataAsync();
                }
                catch (Exception ex) { Log.Error(ex, "Proposal error"); MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            };
            w.Content = sp; w.Owner = Window.GetWindow(this); w.ShowDialog();
        }

        private async void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id) await ProcessProposalAsync(id, true);
        }

        private async void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id) await ProcessProposalAsync(id, false);
        }

        private async Task ProcessProposalAsync(int id, bool approve)
        {
            try
            {
                var allProps = await _service.GetAllAsync<YouthAwardProposal>();
                var prop = allProps.FirstOrDefault(p => p.Id == id);
                if (prop == null || prop.Status != "Pending") return;

                if (approve)
                {
                    prop.Status = "Approved";
                    prop.ApprovedBy = Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "System";
                    // Create record
                    var decisionNo = $"QD{DateTime.Today:yyMMdd}-{prop.MemberId}";
                    await _service.AddAsync(new YouthAwardRecord
                    {
                        MemberId = prop.MemberId,
                        AwardTitle = prop.Reason,
                        DecisionNo = decisionNo,
                        AwardDate = DateTime.Today
                    });
                }
                else
                {
                    prop.Status = "Rejected";
                    prop.ApprovedBy = Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "System";
                }
                await _service.UpdateAsync(prop); await LoadDataAsync();
                if (approve) MessageBox.Show("Đã duyệt đề xuất và tạo Hồ sơ khen thưởng!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { Log.Error(ex, "Approve error"); MessageBox.Show($"Lỗi xử lý đề xuất: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
        private async void BtnEditProposal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var allProps = await _service.GetAllAsync<YouthAwardProposal>();
                var item = allProps.FirstOrDefault(p => p.Id == id);
                if (item == null) return;
                var w = new Window { Title = "Sửa đề xuất", Width = 400, Height = 380, WindowStartupLocation = WindowStartupLocation.CenterScreen, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
                var sp = new StackPanel { Margin = new Thickness(20) };

                sp.Children.Add(new TextBlock { Text = "Loại:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
                var cbType = new ComboBox { Height = 32, Margin = new Thickness(0, 0, 0, 10) };
                cbType.Items.Add("Cá nhân"); cbType.Items.Add("Tập thể");
                cbType.SelectedItem = item.ProposalType == "Tap the" ? "Tập thể" : "Cá nhân";
                sp.Children.Add(cbType);

                sp.Children.Add(new TextBlock { Text = "Thành viên (nếu cá nhân):", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
                var cbMember = new ComboBox { Height = 32, DisplayMemberPath = "DisplayName", Margin = new Thickness(0, 0, 0, 10) };
                var allMembers = await _service.GetAllAsync<YouthMember>();
                var members = allMembers.Where(m => m.Status == "Active").Select(m => new { m.Id, DisplayName = $"{m.StudentName} ({m.ClassName})" }).ToList();
                cbMember.ItemsSource = members;
                if (item.ProposalType == "Ca nhan")
                {
                    var selectedMem = members.FirstOrDefault(m => m.Id == item.MemberId);
                    if (selectedMem != null) cbMember.SelectedItem = selectedMem;
                }
                sp.Children.Add(cbMember);

                sp.Children.Add(new TextBlock { Text = "Lý do / Thành tích:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
                var txtReason = new TextBox { Text = item.Reason, Height = 60, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Padding = new Thickness(8) };
                sp.Children.Add(txtReason);

                var btnSave = new Button { Content = "Lưu thay đổi", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(15, 10, 15, 10), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)), Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
                sp.Children.Add(btnSave);

                btnSave.Click += async (s, ev) =>
                {
                    dynamic? sel = cbMember.SelectedItem;
                    try
                    {
                        string pType = cbType.SelectedItem?.ToString() == "Tập thể" ? "Tap the" : "Ca nhan";
                        item.ProposalType = pType;
                        item.MemberId = pType == "Tap the" ? 0 : (sel?.Id ?? 0);
                        item.Reason = txtReason.Text.Trim();
                        await _service.UpdateAsync(item); w.Close(); await LoadDataAsync();
                    }
                    catch (Exception ex) { Log.Error(ex, "Edit proposal error"); MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
                };
                w.Content = sp; w.Owner = Window.GetWindow(this); w.ShowDialog();
            }
        }

        private async void BtnDeleteProposal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (MessageBox.Show("Xác nhận xóa đề xuất này?", "Xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _service.DeleteAsync<YouthAwardProposal>(id); await LoadDataAsync();
                    }
                    catch (Exception ex) { Log.Error(ex, "Delete proposal error"); MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
            }
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) btn.IsEnabled = false;
            try
            {
                OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization("QA SmartClass");
                var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "KhenThuong.xlsx" };
                if (dlg.ShowDialog() == true)
                {
                    using var package = new OfficeOpenXml.ExcelPackage();
                    QASmartClass.YouthUnion.Services.YouthExportHelper.EncryptWorkbook(package);
                    var ws = package.Workbook.Worksheets.Add("KhenThuong");
                    ws.Cells["A1"].Value = "STT"; 
                    ws.Cells["B1"].Value = "Họ tên"; 
                    ws.Cells["C1"].Value = "Loại";
                    ws.Cells["D1"].Value = "Lý do"; 
                    ws.Cells["E1"].Value = "Trạng thái"; 
                    ws.Cells["F1"].Value = "Người đề xuất";
                    ws.Cells["A1:F1"].Style.Font.Bold = true;

                    var allProps = await _service.GetAllAsync<YouthAwardProposal>();
                    var proposals = allProps.OrderByDescending(p => p.CreatedAt).ToList();
                    var allMembers = await _service.GetAllAsync<YouthMember>();
                    int row = 2;
                    foreach (var p in proposals)
                    {
                        var m = allMembers.FirstOrDefault(x => x.Id == p.MemberId);
                        ws.Cells[row, 1].Value = row - 1; 
                        ws.Cells[row, 2].Value = p.ProposalType == "Tap the" ? "Tập thể Chi đoàn" : (m?.StudentName ?? "Chưa rõ");
                        ws.Cells[row, 3].Value = p.ProposalType == "Tap the" ? "Tập thể" : "Cá nhân"; 
                        ws.Cells[row, 4].Value = p.Reason;
                        ws.Cells[row, 5].Value = p.Status; 
                        ws.Cells[row, 6].Value = p.ProposedBy; 
                        row++;
                    }
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                    
                    byte[] fileBytes = package.GetAsByteArray();
                    await Task.Run(() => System.IO.File.WriteAllBytes(dlg.FileName, fileBytes));
                    
                    MessageBox.Show("Xuất dữ liệu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { Log.Error(ex, "Export award error"); MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            finally
            {
                if (sender is Button btnLock) btnLock.IsEnabled = true;
            }
        }
    }

    public class ProposalTypeConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string type)
            {
                if (type == "Ca nhan" || type == "CaNhan") return "Cá nhân";
                if (type == "Tap the" || type == "TapThe") return "Tập thể";
            }
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    public class ProposalStatusConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string status)
            {
                if (status == "Pending") return "Chờ duyệt";
                if (status == "Approved") return "Đã duyệt";
                if (status == "Rejected") return "Từ chối";
            }
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }
}
