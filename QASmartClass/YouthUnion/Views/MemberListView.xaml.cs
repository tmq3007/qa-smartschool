using OfficeOpenXml;
using Microsoft.Win32;
using QASmartClass.Data;
using QASmartClass.Staff.Services;
using QASmartClass.YouthUnion.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Threading.Tasks;

namespace QASmartClass.YouthUnion.Views
{
    public partial class MemberListView : Page
    {
        private YouthUnionService _service;
        private List<YouthMember> _allMembers = new();

        public MemberListView()
        {
            InitializeComponent();
            
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_service == null)
            {
                _service = new YouthUnionService();
            }
            await LoadDataAsync();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _service?.Dispose();
            _service = null;
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _allMembers = await _service.GetAllMembersAsync();

                // Populate class filter dynamically
                var classes = _allMembers.Select(m => m.ClassName).Distinct().OrderBy(c => c).ToList();
                CbFilterClass.Items.Clear();
                CbFilterClass.Items.Add(new ComboBoxItem { Content = "Tất cả lớp", Tag = "All" });
                foreach (var c in classes)
                    CbFilterClass.Items.Add(new ComboBoxItem { Content = c, Tag = c });
                CbFilterClass.SelectedIndex = 0;

                ApplyFilters();
            }
            catch (Exception ex) { Log.Warning("YouthMember Load error: {Err}", ex.Message); }
        }

        private void ApplyFilters()
        {
            var filtered = _allMembers.AsEnumerable();

            // Search
            var search = TxtSearch?.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(search))
                filtered = filtered.Where(m => m.StudentName.Contains(search, StringComparison.OrdinalIgnoreCase));

            // Class filter
            var classTag = (CbFilterClass?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
            if (classTag != "All")
                filtered = filtered.Where(m => m.ClassName == classTag);

            // Type filter
            var typeTag = (CbFilterType?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
            if (typeTag != "All")
                filtered = filtered.Where(m => m.MemberType == typeTag);

            // Status filter
            var statusTag = (CbFilterStatus?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
            if (statusTag != "All")
                filtered = filtered.Where(m => m.Status == statusTag);

            var result = filtered.ToList();
            if (DgMembers != null) DgMembers.ItemsSource = result;
            if (RunTotal != null) RunTotal.Text = result.Count.ToString();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadDataAsync();

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            ShowMemberForm(null);
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExcelPackage.License.SetNonCommercialOrganization("QA SmartClass");
                var dlg = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = "DanhSachDoanVien.xlsx" };
                if (dlg.ShowDialog() == true)
                {
                    BtnExport.IsEnabled = false;
                    using var package = new ExcelPackage();
                    QASmartClass.YouthUnion.Services.YouthExportHelper.EncryptWorkbook(package);
                    var ws = package.Workbook.Worksheets.Add("DanhSach");
                    ws.Cells["A1"].Value = "STT";
                    ws.Cells["B1"].Value = "Họ và Tên";
                    ws.Cells["C1"].Value = "Lớp";
                    ws.Cells["D1"].Value = "Loại";
                    ws.Cells["E1"].Value = "Chức vụ";
                    ws.Cells["F1"].Value = "Trạng thái";
                    ws.Cells["G1"].Value = "Số sổ Đoàn";
                    ws.Cells["A1:G1"].Style.Font.Bold = true;

                    var items = DgMembers.ItemsSource as IEnumerable<dynamic> ?? _allMembers;
                    int row = 2;
                    foreach (YouthMember m in items)
                    {
                        ws.Cells[row, 1].Value = row - 1;
                        ws.Cells[row, 2].Value = m.StudentName;
                        ws.Cells[row, 3].Value = m.ClassName;
                        ws.Cells[row, 4].Value = m.MemberType;
                        ws.Cells[row, 5].Value = m.Position;
                        ws.Cells[row, 6].Value = m.Status;
                        ws.Cells[row, 7].Value = m.DoanCardNo;
                        row++;
                    }
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                    
                    var fileBytes = package.GetAsByteArray();
                    var fileName = dlg.FileName;
                    await Task.Run(() => File.WriteAllBytes(fileName, fileBytes));
                    MessageBox.Show("Xuất dữ liệu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) 
            { 
                Log.Error(ex, "Export Excel error"); 
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); 
            }
            finally
            {
                BtnExport.IsEnabled = true;
            }
        }

        private async void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel Files|*.xlsx;*.xls",
                    Title = "Chọn tệp Excel danh sách Đoàn viên"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    ExcelPackage.License.SetNonCommercialOrganization("QA SmartClass");
                    
                    using var package = new ExcelPackage(new FileInfo(filePath));
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        MessageBox.Show("Không tìm thấy Worksheet nào trong tệp Excel.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    int rowCount = worksheet.Dimension?.Rows ?? 0;
                    if (rowCount < 2)
                    {
                        MessageBox.Show("Tệp Excel không chứa dữ liệu hoặc chỉ có Header.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int successCount = 0;
                    int errorCount = 0;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        string name = worksheet.Cells[row, 2].Value?.ToString()?.Trim() ?? "";
                        string className = worksheet.Cells[row, 3].Value?.ToString()?.Trim() ?? "";
                        string memberType = worksheet.Cells[row, 4].Value?.ToString()?.Trim() ?? "Đoàn viên";
                        string position = worksheet.Cells[row, 5].Value?.ToString()?.Trim() ?? "Thành viên";
                        string status = worksheet.Cells[row, 6].Value?.ToString()?.Trim() ?? "Active";
                        string cardNo = worksheet.Cells[row, 7].Value?.ToString()?.Trim() ?? "";

                        if (string.IsNullOrEmpty(name))
                        {
                            errorCount++;
                            continue;
                        }

                        var member = new YouthMember
                        {
                            StudentName = name,
                            ClassName = className,
                            MemberType = memberType,
                            Position = position,
                            Status = status,
                            DoanCardNo = cardNo,
                            JoinDate = DateTime.Today
                        };

                        await _service.AddMemberAsync(member);
                        successCount++;
                    }

                    MessageBox.Show($"Nhập dữ liệu thành công!\n\nĐã thêm: {successCount} Đoàn viên\nBị lỗi/Bỏ qua: {errorCount}", "Kết quả", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error importing Excel members");
                MessageBox.Show("Lỗi khi nhập dữ liệu: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var member = await _service.GetMemberByIdAsync(id);
                if (member != null) ShowMemberForm(member);
            }
        }

        private async void DgMembers_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DgMembers.SelectedItem is YouthMember m)
            {
                var member = await _service.GetMemberByIdAsync(m.Id);
                if (member != null) ShowMemberForm(member);
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var result = MessageBox.Show("Bạn có chắc chắn muốn xóa Đoàn viên này?\nThao tác này không thể hoàn tác.",
                    "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _service.DeleteMemberAsync(id);
                        await LoadDataAsync();
                    }
                    catch (Exception ex) { Log.Error(ex, "Lỗi xóa YouthMember {Id}", id); }
                }
            }
        }

        private void ShowMemberForm(YouthMember? existing)
        {
            var isEdit = existing != null;
            var w = new Window
            {
                Title = isEdit ? "Sửa Đoàn viên" : "Thêm Đoàn viên mới",
                Width = 500, Height = 580,
                Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            var sp = new StackPanel { Margin = new Thickness(25) };

            var txtName = AddField(sp, "Họ và Tên:", existing?.StudentName ?? "");
            var txtClass = AddField(sp, "Lớp:", existing?.ClassName ?? "");
            var txtCard = AddField(sp, "Số sổ Đoàn:", existing?.DoanCardNo ?? "");
            var txtPhone = AddField(sp, "Số điện thoại:", existing?.PhoneNumber ?? "");
            var txtEmail = AddField(sp, "Email:", existing?.Email ?? "");

            var cbType = new ComboBox { Height = 32, Margin = new Thickness(0, 0, 0, 10) };
            cbType.Items.Add("Đoàn viên"); cbType.Items.Add("Đội viên");
            cbType.SelectedItem = existing != null ? YouthMapper.MapMemberTypeToUI(existing.MemberType) : "Đoàn viên";
            sp.Children.Add(new TextBlock { Text = "Loại:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 3) });
            sp.Children.Add(cbType);

            var cbPos = new ComboBox { Height = 32, Margin = new Thickness(0, 0, 0, 10) };
            cbPos.Items.Add("Thành viên"); cbPos.Items.Add("Bí thư"); cbPos.Items.Add("Phó Bí thư"); cbPos.Items.Add("UV BCH");
            cbPos.SelectedItem = existing != null ? YouthMapper.MapPositionToUI(existing.Position) : "Thành viên";
            sp.Children.Add(new TextBlock { Text = "Chức vụ:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 3) });
            sp.Children.Add(cbPos);

            var cbStatus = new ComboBox { Height = 32, Margin = new Thickness(0, 0, 0, 15) };
            cbStatus.Items.Add("Đang hoạt động"); cbStatus.Items.Add("Tạm dừng"); cbStatus.Items.Add("Đã chuyển đi");
            cbStatus.SelectedItem = existing != null ? YouthMapper.MapStatusToUI(existing.Status) : "Đang hoạt động";
            sp.Children.Add(new TextBlock { Text = "Trạng thái:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 3) });
            sp.Children.Add(cbStatus);

            var btnSave = new Button
            {
                Content = isEdit ? "💾 Lưu thay đổi" : "➕ Thêm mới",
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)),
                Foreground = System.Windows.Media.Brushes.White,
                Padding = new Thickness(20, 10, 20, 10),
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.Bold,
                FontSize = 15,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            sp.Children.Add(btnSave);

            btnSave.Click += async (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Vui lòng nhập Họ tên.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var phoneStr = txtPhone.Text.Trim();
                var emailStr = txtEmail.Text.Trim();

                // 1. Kiểm tra định dạng Số điện thoại (Chỉ kiểm tra khi người dùng nhập dữ liệu)
                if (!string.IsNullOrEmpty(phoneStr))
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(phoneStr, @"^\d{10}$"))
                    {
                        MessageBox.Show("Số điện thoại phải gồm đúng 10 chữ số.", "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtPhone.Focus();
                        return;
                    }
                }

                // 2. Kiểm tra định dạng Email (Chỉ kiểm tra khi người dùng nhập dữ liệu)
                if (!string.IsNullOrEmpty(emailStr))
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(emailStr, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
                    {
                        MessageBox.Show("Địa chỉ Email không hợp lệ (Ví dụ hợp lệ: doanvien@school.edu.vn).", "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtEmail.Focus();
                        return;
                    }
                }

                try
                {
                    if (isEdit)
                    {
                        existing!.StudentName = txtName.Text.Trim();
                        existing.ClassName = txtClass.Text.Trim();
                        existing.DoanCardNo = txtCard.Text.Trim();
                        existing.PhoneNumber = txtPhone.Text.Trim();
                        existing.Email = txtEmail.Text.Trim();
                        existing.MemberType = YouthMapper.MapMemberTypeToDb(cbType.SelectedItem?.ToString() ?? "Đoàn viên");
                        existing.Position = YouthMapper.MapPositionToDb(cbPos.SelectedItem?.ToString() ?? "Thành viên");
                        existing.Status = YouthMapper.MapStatusToDb(cbStatus.SelectedItem?.ToString() ?? "Đang hoạt động");
                        
                        await _service.UpdateMemberAsync(existing);
                    }
                    else
                    {
                        var newMember = new YouthMember
                        {
                            StudentName = txtName.Text.Trim(),
                            ClassName = txtClass.Text.Trim(),
                            DoanCardNo = txtCard.Text.Trim(),
                            PhoneNumber = txtPhone.Text.Trim(),
                            Email = txtEmail.Text.Trim(),
                            MemberType = YouthMapper.MapMemberTypeToDb(cbType.SelectedItem?.ToString() ?? "Đoàn viên"),
                            Position = YouthMapper.MapPositionToDb(cbPos.SelectedItem?.ToString() ?? "Thành viên"),
                            Status = YouthMapper.MapStatusToDb(cbStatus.SelectedItem?.ToString() ?? "Đang hoạt động"),
                            JoinDate = DateTime.Today
                        };
                        await _service.AddMemberAsync(newMember);
                    }
                    
                    w.Close();
                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Lỗi lưu YouthMember");
                    MessageBox.Show("Lỗi khi lưu: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            w.Content = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            w.ShowDialog();
        }

        private TextBox AddField(StackPanel parent, string label, string value)
        {
            parent.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 3) });
            var tb = new TextBox { Text = value, Height = 32, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 10), FontSize = 14 };
            parent.Children.Add(tb);
            return tb;
        }

    }

    public class MemberStatusConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string status)
            {
                if (status == "Active") return "Đang hoạt động";
                if (status == "Inactive") return "Tạm dừng";
                if (status == "Transferred") return "Đã chuyển đi";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MemberTypeConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string t) return YouthMapper.MapMemberTypeToUI(t);
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    public class PositionConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string p) return YouthMapper.MapPositionToUI(p);
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }
}

