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
    public partial class RecruitmentView : Page
    {
        private readonly YouthUnionService _service;
        public RecruitmentView()
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
                var statusFilter = (CbFilterStatus?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
                var allRecruits = await _service.GetAllAsync<YouthRecruitment>();
                var query = allRecruits.AsQueryable();
                if (statusFilter != "All")
                    query = query.Where(r => r.Status == statusFilter);
                var list = query.OrderByDescending(r => r.ApplyDate).ToList()
                    .Select(r => new
                    {
                        r.Id, r.StudentName, r.ClassName, r.Status, r.ApprovedBy,
                        ApplyDateStr = r.ApplyDate.ToString("dd/MM/yyyy"),
                        ApproveDateStr = r.ApproveDate?.ToString("dd/MM/yyyy") ?? ""
                    }).ToList();
                DgRecruitment.ItemsSource = list;
            }
            catch (Exception ex) { Log.Warning("Recruitment load: {Err}", ex.Message); }
        }

        private async void CbFilterStatus_Changed(object sender, SelectionChangedEventArgs e) => await LoadDataAsync();

        private void BtnNewApply_Click(object sender, RoutedEventArgs e)
        {
            var w = new Window { Title = "Nộp hồ sơ kết nạp", Width = 420, Height = 350, WindowStartupLocation = WindowStartupLocation.CenterScreen, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
            var sp = new StackPanel { Margin = new Thickness(20) };
            var txtName = AddField(sp, "Họ và tên:");
            var txtClass = AddField(sp, "Lớp:");
            sp.Children.Add(new TextBlock { Text = "Lý do xin vào Đoàn:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            var txtReason = new TextBox { Height = 70, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Padding = new Thickness(8), FontSize = 14, Margin = new Thickness(0, 0, 0, 15) };
            sp.Children.Add(txtReason);

            var btnSave = new Button { Content = "Nộp hồ sơ", Padding = new Thickness(15, 10, 15, 10), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)), Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
            sp.Children.Add(btnSave);
            btnSave.Click += async (s, ev) =>
            {
                var name = txtName.Text.Trim();
                var cls = txtClass.Text.Trim();
                if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("Vui lòng nhập họ và tên.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                
                if (!IsAgeValidForRecruitment(cls, out string err))
                {
                    MessageBox.Show(err, "Không đủ điều kiện tuổi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string digits = new string(cls.TakeWhile(char.IsDigit).ToArray());
                if (int.TryParse(digits, out int grade) && grade == 9)
                {
                    var result = MessageBox.Show($"Học sinh lớp 9 cần đảm bảo đã đủ 15 tuổi để xin vào Đoàn. Bạn đã xác minh học sinh {name} đủ 15 tuổi chưa?", 
                                                 "Xác minh độ tuổi", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }

                try
                {
                    // Check if already active member
                    var members = await _service.GetAllAsync<YouthMember>();
                    if (members.Any(m => m.StudentName == name && m.ClassName == cls && m.Status == "Active"))
                    {
                        MessageBox.Show("Học sinh này đã là đoàn viên hoạt động.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    // Check if already applied
                    var recruitments = await _service.GetAllAsync<YouthRecruitment>();
                    if (recruitments.Any(r => r.StudentName == name && r.ClassName == cls && (r.Status == "Applied" || r.Status == "Reviewing")))
                    {
                        MessageBox.Show("Hồ sơ kết nạp của học sinh này đã tồn tại và đang được xét.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    await _service.AddAsync(new YouthRecruitment { StudentName = name, ClassName = cls, Reason = txtReason.Text.Trim(), Status = "Applied", ApplyDate = DateTime.Today });
                    w.Close(); await LoadDataAsync();
                }
                catch (Exception ex) { Log.Error(ex, "Apply error"); MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            };
            w.Content = sp; w.Owner = Window.GetWindow(this); w.ShowDialog();
        }

        private async void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id) await ProcessRecruitmentAsync(id, true);
        }

        private async void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id) await ProcessRecruitmentAsync(id, false);
        }

        private async Task ProcessRecruitmentAsync(int id, bool approve)
        {
            try
            {
                var recruits = await _service.GetAllAsync<YouthRecruitment>();
                var rec = recruits.FirstOrDefault(r => r.Id == id);
                if (rec == null || rec.Status != "Applied") return;
                var user = Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "System";

                if (approve)
                {
                    if (!IsAgeValidForRecruitment(rec.ClassName, out string err))
                    {
                        MessageBox.Show(err, "Không đủ điều kiện tuổi", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    string digits = new string(rec.ClassName.TakeWhile(char.IsDigit).ToArray());
                    if (int.TryParse(digits, out int grade) && grade == 9)
                    {
                        var result = MessageBox.Show($"Học sinh lớp 9 cần đảm bảo đã đủ 15 tuổi để kết nạp. Bạn đã xác minh học sinh {rec.StudentName} đủ 15 tuổi chưa?", 
                                                     "Xác minh độ tuổi", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result != MessageBoxResult.Yes) return;
                    }

                    rec.Status = "Approved"; rec.ApproveDate = DateTime.Today; rec.ApprovedBy = user;
                    // Auto-create YouthMember
                    var members = await _service.GetAllAsync<YouthMember>();
                    if (!members.Any(m => m.StudentName == rec.StudentName && m.ClassName == rec.ClassName))
                    {
                        await _service.AddAsync(new YouthMember
                        {
                            StudentName = rec.StudentName, ClassName = rec.ClassName,
                            MemberType = YouthMapper.MapMemberTypeToDb("Đoàn viên"), Position = YouthMapper.MapPositionToDb("Thành viên"),
                            JoinDate = DateTime.Today, Status = "Active"
                        });
                    }
                }
                else
                {
                    rec.Status = "Rejected"; rec.ApproveDate = DateTime.Today; rec.ApprovedBy = user;
                }
                await _service.UpdateAsync(rec); await LoadDataAsync();
                MessageBox.Show(approve ? "Đã duyệt và kết nạp thành công!" : "Đã từ chối hồ sơ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { Log.Error(ex, "Process recruitment error"); }
        }

        private TextBox AddField(StackPanel parent, string label)
        {
            parent.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 3) });
            var tb = new TextBox { Height = 32, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 10), FontSize = 14 };
            parent.Children.Add(tb); return tb;
        }

        private async void BtnEditRecruitment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var recruits = await _service.GetAllAsync<YouthRecruitment>();
                var item = recruits.FirstOrDefault(r => r.Id == id);
                if (item == null) return;
                var w = new Window { Title = "Sửa đơn kết nạp", Width = 400, Height = 300, WindowStartupLocation = WindowStartupLocation.CenterScreen, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
                var sp = new StackPanel { Margin = new Thickness(20) };

                sp.Children.Add(new TextBlock { Text = "Trạng thái:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
                var cbStatus = new ComboBox { Height = 32 };
                cbStatus.Items.Add("Chờ duyệt"); cbStatus.Items.Add("Đang xem xét"); cbStatus.Items.Add("Đã duyệt"); cbStatus.Items.Add("Từ chối");
                cbStatus.SelectedItem = item.Status == "Applied" ? "Chờ duyệt" : item.Status == "Reviewing" ? "Đang xem xét" : item.Status == "Approved" ? "Đã duyệt" : item.Status == "Rejected" ? "Từ chối" : "Chờ duyệt";
                sp.Children.Add(cbStatus);

                sp.Children.Add(new TextBlock { Text = "Ghi chú:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
                var txtNote = new TextBox { Text = item.Note, Height = 60, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Padding = new Thickness(8) };
                sp.Children.Add(txtNote);

                var btnSave = new Button { Content = "Lưu", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(15, 10, 15, 10), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)), Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
                sp.Children.Add(btnSave);

                btnSave.Click += async (s, ev) =>
                {
                    try
                    {
                        var selStatus = cbStatus.SelectedItem?.ToString();
                        item.Status = selStatus == "Chờ duyệt" ? "Applied" : selStatus == "Đang xem xét" ? "Reviewing" : selStatus == "Đã duyệt" ? "Approved" : selStatus == "Từ chối" ? "Rejected" : item.Status;
                        item.Note = txtNote.Text.Trim();
                        if (item.Status == "Approved")
                        {
                            if (!IsAgeValidForRecruitment(item.ClassName, out string err))
                            {
                                MessageBox.Show(err, "Không đủ điều kiện tuổi", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                            string digits = new string(item.ClassName.TakeWhile(char.IsDigit).ToArray());
                            if (int.TryParse(digits, out int grade) && grade == 9)
                            {
                                var result = MessageBox.Show($"Học sinh lớp 9 cần đảm bảo đã đủ 15 tuổi để kết nạp. Bạn đã xác minh học sinh {item.StudentName} đủ 15 tuổi chưa?", 
                                                             "Xác minh độ tuổi", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                if (result != MessageBoxResult.Yes) return;
                            }

                            item.ApproveDate = DateTime.Now;
                            item.ApprovedBy = Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "System";
                            
                            // Tự động tạo hồ sơ Đoàn viên tương ứng nếu chưa tồn tại
                            var allMembers = await _service.GetAllMembersAsync();
                            var exists = allMembers.Any(m => m.StudentId == item.StudentId || (m.StudentName == item.StudentName && m.ClassName == item.ClassName));
                            if (!exists)
                            {
                                var newMember = new YouthMember
                                {
                                    StudentId = item.StudentId ?? 0,
                                    StudentName = item.StudentName,
                                    ClassName = item.ClassName,
                                    MemberType = YouthMapper.MapMemberTypeToDb("Đoàn viên"),
                                    JoinDate = DateTime.Today,
                                    Position = YouthMapper.MapPositionToDb("Thành viên"),
                                    Status = "Active",
                                    DoanCardNo = $"DOAN-{DateTime.Today.Year}-{item.Id:D4}",
                                    TotalScore = 0
                                };
                                await _service.AddMemberAsync(newMember);
                            }
                        }
                        await _service.UpdateAsync(item); w.Close(); await LoadDataAsync();
                    }
                    catch (Exception ex) { Serilog.Log.Error(ex, "Edit recruitment error"); MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
                };
                w.Content = sp; w.Owner = Window.GetWindow(this); w.ShowDialog();
            }
        }

        private async void BtnDeleteRecruitment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (MessageBox.Show("Xác nhận xóa đơn kết nạp này?", "Xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _service.DeleteAsync<YouthRecruitment>(id); await LoadDataAsync();
                    }
                    catch (Exception ex) { Serilog.Log.Error(ex, "Delete recruitment error"); MessageBox.Show($"Lỗi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
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
                var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "KetNapDoan.xlsx" };
                if (dlg.ShowDialog() == true)
                {
                    byte[] fileBytes = null;
                    using (var package = new OfficeOpenXml.ExcelPackage())
                    {
                        QASmartClass.YouthUnion.Services.YouthExportHelper.EncryptWorkbook(package);
                        var ws = package.Workbook.Worksheets.Add("KetNap");
                        ws.Cells["A1"].Value = "STT"; ws.Cells["B1"].Value = "Họ tên"; ws.Cells["C1"].Value = "Lớp";
                        ws.Cells["D1"].Value = "Ngày nộp"; ws.Cells["E1"].Value = "Trạng thái"; ws.Cells["F1"].Value = "Người duyệt";
                        ws.Cells["A1:F1"].Style.Font.Bold = true;
                        var allItems = await _service.GetAllAsync<YouthRecruitment>();
                        var items = allItems.OrderByDescending(r => r.ApplyDate).ToList();
                        int row = 2;
                        foreach (var r in items)
                        {
                            ws.Cells[row, 1].Value = row - 1; ws.Cells[row, 2].Value = r.StudentName; ws.Cells[row, 3].Value = r.ClassName;
                            ws.Cells[row, 4].Value = r.ApplyDate.ToString("dd/MM/yyyy"); ws.Cells[row, 5].Value = r.Status;
                            ws.Cells[row, 6].Value = r.ApprovedBy; row++;
                        }
                        ws.Cells[ws.Dimension.Address].AutoFitColumns();
                        fileBytes = package.GetAsByteArray();
                    }
                    await Task.Run(() => System.IO.File.WriteAllBytes(dlg.FileName, fileBytes));
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Export recruitment error");
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private bool IsAgeValidForRecruitment(string className, out string errorMsg)
        {
            errorMsg = "";
            if (string.IsNullOrWhiteSpace(className)) return true;
            
            string digits = new string(className.TakeWhile(char.IsDigit).ToArray());
            if (int.TryParse(digits, out int grade))
            {
                if (grade >= 10)
                {
                    return true;
                }
                else if (grade <= 8)
                {
                    errorMsg = $"Học sinh lớp {grade} chưa đủ điều kiện kết nạp Đoàn (phải từ đủ 15 tuổi, tương đương lớp 9 trở lên).";
                    return false;
                }
            }
            return true;
        }
    }

    public class RecruitmentStatusConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string status)
            {
                if (status == "Applied") return "Chờ duyệt";
                if (status == "Reviewing") return "Đang xem xét";
                if (status == "Approved") return "Đã duyệt";
                if (status == "Rejected") return "Từ chối";
            }
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }
}
