using QASmartClass.Data;
using QASmartClass.YouthUnion.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.YouthUnion.Views
{
    public class AttendanceRow : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public string StudentName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string Position { get; set; } = "";
        private bool _isPresent;
        public bool IsPresent { get => _isPresent; set { _isPresent = value; OnPropertyChanged(nameof(IsPresent)); } }
        private string _note = "";
        public string Note { get => _note; set { _note = value; OnPropertyChanged(nameof(Note)); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

    public class SessionItem
    {
        public string SessionDate { get; set; } = "";
        public string SessionTitle { get; set; } = "";
        public string Display => $"{SessionDate}  {SessionTitle}";
    }

    public partial class AttendanceView : Page
    {
        private readonly YouthUnionService _service;
        private List<AttendanceRow> _rows = new();
        private System.IO.Ports.SerialPort? _serialPort;

        public AttendanceView()
        {
            InitializeComponent();
            _service = new YouthUnionService();
            InitializeSerialPort();
            Unloaded += (s, e) => {
                CloseSerialPort();
                _service?.Dispose();
            };
            Loaded += async (_, __) => await LoadSessionsAsync();
        }

        private void InitializeSerialPort()
        {
            try
            {
                var config = QASmartClass.Services.AppConfig.Load();
                string comPort = config.YouthUnionComPort?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(comPort))
                {
                    var ports = System.IO.Ports.SerialPort.GetPortNames();
                    if (ports.Length > 0) comPort = ports[0];
                }

                if (!string.IsNullOrWhiteSpace(comPort))
                {
                    _serialPort = new System.IO.Ports.SerialPort(comPort, 9600);
                    _serialPort.DataReceived += SerialPort_DataReceived;
                    _serialPort.Open();
                    Log.Information("[YouthUnion] SerialPort {Port} opened for RFID scan", comPort);
                    
                    TxtRfidStatus.Text = $"Đầu đọc RFID hoạt động (Cổng: {comPort}). Đang chờ quét thẻ...";
                    TxtRfidStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105));
                    BdrRfidStatus.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252));
                    BdrRfidStatus.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(222, 226, 230));
                }
                else
                {
                    ShowHardwareError("Không tìm thấy thiết bị đầu đọc RFID kết nối với máy tính!");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[YouthUnion] InitializeSerialPort failed: {Err}", ex.Message);
                ShowHardwareError("Lỗi phần cứng: Cổng kết nối RFID đang bị chiếm dụng hoặc cáp bị rút!");
            }
        }

        private void ShowHardwareError(string message)
        {
            TxtRfidStatus.Text = message;
            TxtRfidStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38));
            BdrRfidStatus.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 226, 226));
            BdrRfidStatus.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(252, 165, 165));
        }

        private void CloseSerialPort()
        {
            try
            {
                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen) _serialPort.Close();
                    _serialPort.Dispose();
                    _serialPort = null;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[YouthUnion] CloseSerialPort failed: {Err}", ex.Message);
            }
        }

        private void SerialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;
            try
            {
                string rawData = _serialPort.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(rawData)) return;

                Dispatcher.Invoke(async () =>
                {
                    await ProcessCardScanAsync(rawData);
                });
            }
            catch (Exception ex)
            {
                Log.Warning("[YouthUnion] SerialPort_DataReceived error: {Err}", ex.Message);
            }
        }

        public async Task ProcessCardScanAsync(string cardUid)
        {
            if (string.IsNullOrWhiteSpace(cardUid)) return;
            try
            {
                var config = QASmartClass.Services.AppConfig.Load();
                int cardStandard = config.YouthUnionCardStandard;
                
                var members = await _service.GetAllMembersAsync();
                YouthMember? matchedMember = null;

                if (cardStandard == 0) // Shared Canteen Card
                {
                    using (var db = new AppDbContext())
                    {
                        var student = db.Students.FirstOrDefault(s => s.StudentCode == cardUid);
                        if (student != null)
                        {
                            matchedMember = members.FirstOrDefault(m => m.StudentId == student.Id);
                        }
                    }
                }
                
                if (matchedMember == null)
                {
                    matchedMember = members.FirstOrDefault(m => m.DoanCardNo == cardUid);
                }

                if (matchedMember == null)
                {
                    TxtRfidStatus.Text = $"Quét thất bại: Thẻ '{cardUid}' không khớp với bất kỳ Đoàn viên/Đội viên nào!";
                    TxtRfidStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38));
                    BdrRfidStatus.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 226, 226));
                    BdrRfidStatus.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(252, 165, 165));
                    System.Media.SystemSounds.Hand.Play();
                    return;
                }

                var row = _rows.FirstOrDefault(r => r.MemberId == matchedMember.Id);
                if (row != null)
                {
                    if (!row.IsPresent)
                    {
                        row.IsPresent = true;
                        row.Note = "Quét thẻ RFID";
                        
                        if (CbSessions.SelectedItem is SessionItem si)
                        {
                            var date = DateTime.ParseExact(si.SessionDate, "dd/MM/yyyy", null);
                            var allAtts = await _service.GetAllAsync<YouthAttendance>();
                            var rec = allAtts.FirstOrDefault(a => a.MemberId == row.MemberId && a.SessionDate == date && a.SessionTitle == si.SessionTitle);
                            if (rec != null)
                            {
                                rec.IsPresent = true;
                                rec.Note = row.Note;
                                await _service.UpdateAsync(rec);
                            }
                        }
                        
                        UpdateStats();
                        
                        TxtRfidStatus.Text = $"Quét thành công: {matchedMember.StudentName} ({matchedMember.ClassName})";
                        TxtRfidStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 163, 74));
                        BdrRfidStatus.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 252, 231));
                        BdrRfidStatus.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(187, 247, 208));
                        System.Media.SystemSounds.Beep.Play();
                    }
                    else
                    {
                        TxtRfidStatus.Text = $"Thông tin: {matchedMember.StudentName} đã được điểm danh trước đó.";
                        TxtRfidStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(217, 119, 6));
                        BdrRfidStatus.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 243, 199));
                        BdrRfidStatus.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(253, 230, 138));
                        System.Media.SystemSounds.Asterisk.Play();
                    }
                }
                else
                {
                    TxtRfidStatus.Text = $"Cảnh báo: Đoàn viên '{matchedMember.StudentName}' không có trong danh sách buổi sinh hoạt này!";
                    TxtRfidStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38));
                    BdrRfidStatus.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 226, 226));
                    BdrRfidStatus.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(252, 165, 165));
                    System.Media.SystemSounds.Hand.Play();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ProcessCardScanAsync error");
            }
        }

        private async Task LoadSessionsAsync()
        {
            try
            {
                var allAtts = await _service.GetAllAsync<YouthAttendance>();
                var sessions = allAtts
                    .Select(a => new { a.SessionDate, a.SessionTitle })
                    .Distinct().OrderByDescending(a => a.SessionDate).ToList()
                    .Select(a => new SessionItem { SessionDate = a.SessionDate.ToString("dd/MM/yyyy"), SessionTitle = a.SessionTitle })
                    .ToList();
                CbSessions.ItemsSource = sessions;
                if (sessions.Any()) CbSessions.SelectedIndex = 0;
            }
            catch (Exception ex) { Log.Warning("AttendanceView sessions: {Err}", ex.Message); }
        }

        private async void CbSessions_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (CbSessions.SelectedItem is SessionItem si)
            {
                var date = DateTime.ParseExact(si.SessionDate, "dd/MM/yyyy", null);
                var allAtts = await _service.GetAllAsync<YouthAttendance>();
                var members = await _service.GetAllAsync<YouthMember>();
                var records = allAtts.Where(a => a.SessionDate == date && a.SessionTitle == si.SessionTitle).ToList();
                _rows = records.Select(r =>
                {
                    var m = members.FirstOrDefault(x => x.Id == r.MemberId);
                    return new AttendanceRow
                    {
                        Id = r.Id,
                        MemberId = r.MemberId,
                        StudentName = m?.StudentName ?? "?",
                        ClassName = m?.ClassName ?? "",
                        Position = m?.Position ?? "",
                        IsPresent = r.IsPresent,
                        Note = r.Note
                    };
                }).ToList();
                DgAttendance.ItemsSource = _rows;
                UpdateStats();
            }
        }

        private void BtnNewSession_Click(object sender, RoutedEventArgs e)
        {
            var w = new Window { Title = "Tạo buổi sinh hoạt mới", Width = 400, Height = 250, Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
            var sp = new StackPanel { Margin = new Thickness(20) };
            sp.Children.Add(new TextBlock { Text = "Tiêu đề buổi sinh hoạt:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            var txtTitle = new TextBox { Height = 32, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center, FontSize = 14 };
            sp.Children.Add(txtTitle);

            sp.Children.Add(new TextBlock { Text = "Loại:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
            var cbType = new ComboBox { Height = 32 };
            cbType.Items.Add("Chi Đoàn"); cbType.Items.Add("Lớp"); cbType.Items.Add("Khối");
            cbType.SelectedIndex = 0;
            sp.Children.Add(cbType);

            var btnCreate = new Button { Content = "Tạo & Điểm danh", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(15, 10, 15, 10), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)), Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
            sp.Children.Add(btnCreate);

            btnCreate.Click += async (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtTitle.Text)) { MessageBox.Show("Vui lòng nhập tiêu đề buổi sinh hoạt.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                try
                {
                    var allMembers = await _service.GetAllAsync<YouthMember>();
                    var members = allMembers.Where(m => m.Status == "Active").ToList();
                    var today = DateTime.Today;
                    foreach (var m in members)
                    {
                        await _service.AddAsync(new YouthAttendance
                        {
                            MemberId = m.Id,
                            SessionDate = today,
                            SessionTitle = txtTitle.Text.Trim(),
                            SessionType = cbType.SelectedItem?.ToString() ?? "Chi Đoàn",
                            IsPresent = false
                        });
                    }
                    w.Close();
                    await LoadSessionsAsync();
                    if (CbSessions.Items.Count > 0) CbSessions.SelectedIndex = 0;
                }
                catch (Exception ex) { Log.Error(ex, "Tao buoi sinh hoat loi"); MessageBox.Show($"Có lỗi xảy ra khi tạo buổi sinh hoạt: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            };
            w.Content = sp;
            w.ShowDialog();
        }

        private async void BtnSaveAttendance_Click(object sender, RoutedEventArgs e)
        {
            if (CbSessions.SelectedItem is not SessionItem si) return;
            try
            {
                var date = DateTime.ParseExact(si.SessionDate, "dd/MM/yyyy", null);
                var allAtts = await _service.GetAllAsync<YouthAttendance>();
                foreach (var row in _rows)
                {
                    var rec = allAtts.FirstOrDefault(a => a.MemberId == row.MemberId && a.SessionDate == date && a.SessionTitle == si.SessionTitle);
                    if (rec != null) { rec.IsPresent = row.IsPresent; rec.Note = row.Note; await _service.UpdateAsync(rec); }
                }
                UpdateStats();
                MessageBox.Show("Đã lưu điểm danh thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { Log.Error(ex, "Save attendance error"); MessageBox.Show($"Có lỗi xảy ra khi lưu điểm danh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void UpdateStats()
        {
            var total = _rows.Count;
            var present = _rows.Count(r => r.IsPresent);
            var pct = total > 0 ? (present * 100 / total) : 0;
            TxtStats.Text = $"Có mặt: {present}/{total} ({pct}%)";
        }
        private void LoadData()
        {
            if (CbSessions.SelectedItem is SessionItem si)
                CbSessions_Changed(CbSessions, null!);
        }

        private async void BtnEditAttendance_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var allAtts = await _service.GetAllAsync<YouthAttendance>();
                var item = allAtts.FirstOrDefault(a => a.Id == id);
                if (item == null) return;
                var w = new Window { Title = "Sửa điểm danh", Width = 350, Height = 250, Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
                var sp = new StackPanel { Margin = new Thickness(20) };

                sp.Children.Add(new TextBlock { Text = "Có mặt:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
                var chk = new CheckBox { IsChecked = item.IsPresent, Content = "Có mặt", Margin = new Thickness(0, 0, 0, 10) };
                sp.Children.Add(chk);

                sp.Children.Add(new TextBlock { Text = "Ghi chú:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
                var txtNote = new TextBox { Text = item.Note, Height = 50, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Padding = new Thickness(8) };
                sp.Children.Add(txtNote);

                var btnSave = new Button { Content = "Lưu", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(15, 10, 15, 10), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)), Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
                sp.Children.Add(btnSave);

                btnSave.Click += async (s, ev) => {
                    try { item.IsPresent = chk.IsChecked ?? false; item.Note = txtNote.Text.Trim(); await _service.UpdateAsync(item); w.Close(); LoadData(); }
                    catch (Exception ex) { Serilog.Log.Error(ex, "Edit attendance error"); MessageBox.Show($"Có lỗi xảy ra khi sửa điểm danh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
                };
                w.Content = sp; w.ShowDialog();
            }
        }

        private async void BtnDeleteAttendance_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (MessageBox.Show("Xác nhận xóa bản ghi điểm danh này?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try { await _service.DeleteAsync<YouthAttendance>(id); LoadData(); }
                    catch (Exception ex) { Serilog.Log.Error(ex, "Delete attendance error"); MessageBox.Show($"Có lỗi xảy ra khi xóa điểm danh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
            }
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            try
            {
                OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization("QA SmartClass");
                var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "DiemDanh.xlsx" };
                if (dlg.ShowDialog() == true)
                {
                    if (btn != null) btn.IsEnabled = false;
                    using var package = new OfficeOpenXml.ExcelPackage();
                    QASmartClass.YouthUnion.Services.YouthExportHelper.EncryptWorkbook(package);
                    var ws = package.Workbook.Worksheets.Add("DiemDanh");
                    ws.Cells["A1"].Value = "STT"; ws.Cells["B1"].Value = "Họ tên"; ws.Cells["C1"].Value = "Lớp";
                    ws.Cells["D1"].Value = "Chức vụ"; ws.Cells["E1"].Value = "Có mặt"; ws.Cells["F1"].Value = "Ghi chú";
                    ws.Cells["A1:F1"].Style.Font.Bold = true;
                    int row = 2;
                    foreach (var r in _rows)
                    {
                        ws.Cells[row, 1].Value = row - 1; ws.Cells[row, 2].Value = r.StudentName; ws.Cells[row, 3].Value = r.ClassName;
                        ws.Cells[row, 4].Value = r.Position; ws.Cells[row, 5].Value = r.IsPresent ? "Có" : "Vắng";
                        ws.Cells[row, 6].Value = r.Note; row++;
                    }
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                    var bytes = package.GetAsByteArray();
                    await Task.Run(() => System.IO.File.WriteAllBytes(dlg.FileName, bytes));
                    MessageBox.Show("Xuất dữ liệu Excel thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { Log.Error(ex, "Export attendance error"); MessageBox.Show($"Có lỗi xảy ra khi xuất dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }
    }
}

