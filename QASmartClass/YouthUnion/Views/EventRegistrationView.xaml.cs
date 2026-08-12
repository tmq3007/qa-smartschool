using OfficeOpenXml;
using Microsoft.Win32;
using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Threading.Tasks;
using QASmartClass.YouthUnion.Services;

namespace QASmartClass.YouthUnion.Views
{
    public partial class EventRegistrationView : Page
    {
        private readonly YouthUnionService _service;

        public EventRegistrationView()
        {
            InitializeComponent();
            _service = new YouthUnionService();
Unloaded += (s, e) => { _service?.Dispose(); };
            Loaded += async (_, __) => await LoadEventsAsync();
        }

        private async Task LoadEventsAsync()
        {
            try
            {
                var activities = await _service.GetAllAsync<YouthActivity>();
                CbEvent.ItemsSource = activities.OrderByDescending(a => a.Date).ToList();
                if (activities.Any()) CbEvent.SelectedIndex = 0;
            }
            catch (Exception ex) { Log.Warning("EventReg load: {Err}", ex.Message); }
        }

        private async void CbEvent_Changed(object sender, SelectionChangedEventArgs e) => await LoadRegistrationsAsync();

        private async Task LoadRegistrationsAsync()
        {
            if (CbEvent.SelectedItem is not YouthActivity act) return;
            try
            {
                var allRegs = await _service.GetAllAsync<YouthEventRegistration>();
                var regs = allRegs.Where(r => r.ActivityId == act.Id).ToList();
                var allMembers = await _service.GetAllAsync<YouthMember>();
                var display = regs.Select(r =>
                {
                    var m = allMembers.FirstOrDefault(x => x.Id == r.MemberId);
                    return new
                    {
                        RegId = r.Id,
                        StudentName = m?.StudentName ?? "?",
                        ClassName = m?.ClassName ?? "",
                        r.Role,
                        RegisteredStr = r.RegisteredAt.ToString("dd/MM HH:mm"),
                        AttendedStr = r.AttendedAt?.ToString("dd/MM HH:mm") ?? "-"
                    };
                }).ToList();
                
                string search = TxtSearch?.Text?.Trim().ToLower() ?? "";
                if (!string.IsNullOrEmpty(search))
                {
                    display = display.Where(x => 
                        x.StudentName.ToLower().Contains(search) ||
                        x.ClassName.ToLower().Contains(search)
                    ).ToList();
                }

                DgRegistrations.ItemsSource = display;
                var total = regs.Count;
                var attended = regs.Count(r => r.AttendedAt.HasValue);
                TxtRegStats.Text = $"Đăng ký: {total} | Tham gia: {attended}";
            }
            catch (Exception ex) { Log.Error(ex, "Load regs error"); }
        }

        private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadRegistrationsAsync();
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            if (CbEvent.SelectedItem is not YouthActivity act) return;
            var w = new Window { Title = "Đăng ký tham gia", Width = 380, Height = 250, Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
            var sp = new StackPanel { Margin = new Thickness(20) };
 
            sp.Children.Add(new TextBlock { Text = "Chọn Đoàn viên:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            var cbMember = new ComboBox { Height = 32, DisplayMemberPath = "DisplayName" };
            var allRegs = await _service.GetAllAsync<YouthEventRegistration>();
            var registered = allRegs.Where(r => r.ActivityId == act.Id).Select(r => r.MemberId).ToList();
            var allMembers = await _service.GetAllAsync<YouthMember>();
            var available = allMembers.Where(m => m.Status == "Active").ToList()
                .Where(m => !registered.Contains(m.Id))
                .Select(m => new { m.Id, DisplayName = $"{m.StudentName} ({m.ClassName})" }).ToList();
            cbMember.ItemsSource = available;
            if (available.Any()) cbMember.SelectedIndex = 0;
            sp.Children.Add(cbMember);

            var btnSave = new Button
            {
                Content = "Đăng ký", Margin = new Thickness(0, 15, 0, 0), Padding = new Thickness(15, 10, 15, 10),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)),
                Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand
            };
            sp.Children.Add(btnSave);

            btnSave.Click += async (s, ev) =>
            {
                dynamic? sel = cbMember.SelectedItem;
                if (sel == null) { MessageBox.Show("Vui lòng chọn đoàn viên muốn đăng ký.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                try
                {
                    await _service.AddAsync(new YouthEventRegistration { ActivityId = act.Id, MemberId = sel.Id, RegisteredAt = DateTime.Now });
                    w.Close();
                    await LoadRegistrationsAsync();
                }
                catch (Exception ex) { Log.Error(ex, "Register error"); MessageBox.Show("Đăng ký sự kiện thất bại. Vui lòng thử lại sau.\n\nChi tiết: " + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error); }
            };
            w.Content = sp;
            w.ShowDialog();
        }

        private async void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int regId)
            {
                try
                {
                    var allRegs = await _service.GetAllAsync<YouthEventRegistration>();
                    var reg = allRegs.FirstOrDefault(r => r.Id == regId);
                    if (reg != null && !reg.AttendedAt.HasValue) { reg.AttendedAt = DateTime.Now; await _service.UpdateAsync(reg); }
                    await LoadRegistrationsAsync();
                }
                catch (Exception ex) { Log.Error(ex, "Confirm error"); }
            }
        }

        private async Task LoadDataAsync() => await LoadRegistrationsAsync();

        private async void BtnEditEventReg_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var allRegs = await _service.GetAllAsync<YouthEventRegistration>();
                var item = allRegs.FirstOrDefault(r => r.Id == id);
                if (item == null) return;
                var w = new Window { Title = "Sửa đăng ký", Width = 350, Height = 250, Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
                var sp = new StackPanel { Margin = new Thickness(20) };
 
                sp.Children.Add(new TextBlock { Text = "Vai trò:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
                var cbRole = new ComboBox { Height = 32 };
                cbRole.Items.Add("Thành viên tham gia"); cbRole.Items.Add("Tình nguyện viên"); cbRole.Items.Add("Trưởng nhóm");
                cbRole.SelectedItem = item.Role == "Volunteer" ? "Tình nguyện viên" : item.Role == "Leader" ? "Trưởng nhóm" : "Thành viên tham gia";
                sp.Children.Add(cbRole);
 
                sp.Children.Add(new TextBlock { Text = "Đã tham gia:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
                var chk = new CheckBox { IsChecked = item.AttendedAt != null, Content = "Đã tham gia", Margin = new Thickness(0, 0, 0, 10) };
                sp.Children.Add(chk);
 
                var btnSave = new Button { Content = "Lưu", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(15, 10, 15, 10), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)), Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
                sp.Children.Add(btnSave);

                btnSave.Click += async (s, ev) => {
                    try {
                        var selRole = cbRole.SelectedItem?.ToString();
                        item.Role = selRole == "Tình nguyện viên" ? "Volunteer" : selRole == "Trưởng nhóm" ? "Leader" : "Participant";
                        item.AttendedAt = (chk.IsChecked ?? false) ? (item.AttendedAt ?? DateTime.Now) : null;
                        await _service.UpdateAsync(item); w.Close(); await LoadDataAsync();
                    }
                    catch (Exception ex) { Serilog.Log.Error(ex, "Edit event reg error"); MessageBox.Show("Có lỗi xảy ra khi cập nhật thông tin đăng ký.\n\nChi tiết: " + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error); }
                };
                w.Content = sp; w.ShowDialog();
            }
        }

        private async void BtnDeleteEventReg_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (MessageBox.Show("Xác nhận xóa đăng ký sự kiện?", "Xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try { await _service.DeleteAsync<YouthEventRegistration>(id); await LoadDataAsync(); }
                    catch (Exception ex) { Serilog.Log.Error(ex, "Delete event reg error"); MessageBox.Show("Có lỗi xảy ra khi cập nhật thông tin đăng ký.\n\nChi tiết: " + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
            }
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            try
            {
                if (CbEvent.SelectedItem is not YouthActivity act) { MessageBox.Show("Vui lòng chọn một sự kiện để tiếp tục.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                
                ExcelPackage.License.SetNonCommercialOrganization("QA SmartClass");
                var dlg = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = "DanhSachThamGia.xlsx" };
                if (dlg.ShowDialog() == true)
                {
                    if (btn != null) btn.IsEnabled = false;
                    using var package = new ExcelPackage();
                    QASmartClass.YouthUnion.Services.YouthExportHelper.EncryptWorkbook(package);
                    var ws = package.Workbook.Worksheets.Add("DS_ThamGia");
                    ws.Cells["A1"].Value = "STT";
                    ws.Cells["B1"].Value = "Họ và Tên";
                    ws.Cells["C1"].Value = "Lớp";
                    ws.Cells["D1"].Value = "Vai trò";
                    ws.Cells["E1"].Value = "Ngày đăng ký";
                    ws.Cells["F1"].Value = "Ngày tham gia";
                    ws.Cells["A1:F1"].Style.Font.Bold = true;
 
                    var items = DgRegistrations.ItemsSource as System.Collections.IEnumerable;
                    int row = 2;
                    if (items != null)
                    {
                        foreach (dynamic r in items)
                        {
                            ws.Cells[row, 1].Value = row - 1;
                            ws.Cells[row, 2].Value = r.StudentName;
                            ws.Cells[row, 3].Value = r.ClassName;
                            ws.Cells[row, 4].Value = r.Role;
                            ws.Cells[row, 5].Value = r.RegisteredStr;
                            ws.Cells[row, 6].Value = r.AttendedStr;
                            row++;
                        }
                    }
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                    var bytes = package.GetAsByteArray();
                    await Task.Run(() => File.WriteAllBytes(dlg.FileName, bytes));
                    MessageBox.Show("Xuất dữ liệu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { Log.Error(ex, "Export Excel error"); MessageBox.Show("Có lỗi xảy ra khi xuất dữ liệu đăng ký sự kiện ra Excel.\n\nChi tiết: " + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error); }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }
    }

    public class EventRoleConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string role)
            {
                if (role == "Participant") return "Thành viên tham gia";
                if (role == "Volunteer") return "Tình nguyện viên";
                if (role == "Leader") return "Trưởng nhóm";
            }
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }
}

