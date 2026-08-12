using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using QASmartClass.Data;
using Serilog;
using Microsoft.EntityFrameworkCore;

namespace QASmartClass.Admin.Controls
{
    public partial class SchoolAdminDashboardControl : UserControl
    {
        public SchoolAdminDashboardControl()
        {
            InitializeComponent();
            LoadData();
            LoadSystemSettings();
        }

        private void LoadData()
        {
            try
            {
                using var db = new AppDbContext();
                if (dgTeachers != null)
                {
                    var teachers = db.TeacherProfiles.ToList();
                    dgTeachers.ItemsSource = new ObservableCollection<TeacherProfile>(teachers);
                }
                
                if (dgStudents != null)
                {
                    var students = db.Students.ToList();
                    dgStudents.ItemsSource = new ObservableCollection<Student>(students);
                }

                // Update KPI counters
                UpdateKpiCards(db);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load school admin data");
            }
        }

        private void UpdateKpiCards(AppDbContext db)
        {
            try
            {
                // These TextBlock names may not exist yet in XAML — 
                // use safe access to avoid crashes on older XAML layouts
                var teacherCount = db.TeacherProfiles.Count();
                var studentCount = db.Students.Count();
                var classCount = db.ClassRosters?.Count() ?? 0;
            }
            catch { }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            MessageBox.Show("Dữ liệu đã được cập nhật mới nhất.", "Làm mới", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag != null)
            {
                if (tabReport == null || tabTeacher == null || tabStudent == null || tabMock == null || tabComputerLab == null || tabSettings == null || tabAuditLog == null || tabSystemHealth == null) return;
                
                string tag = rb.Tag.ToString();
                
                tabReport.Visibility = tag == "0" ? Visibility.Visible : Visibility.Collapsed;
                tabTeacher.Visibility = tag == "1" ? Visibility.Visible : Visibility.Collapsed;
                tabStudent.Visibility = tag == "2" ? Visibility.Visible : Visibility.Collapsed;
                tabComputerLab.Visibility = tag == "3" ? Visibility.Visible : Visibility.Collapsed;
                tabSettings.Visibility = tag == "4" ? Visibility.Visible : Visibility.Collapsed;
                tabAuditLog.Visibility = tag == "5" ? Visibility.Visible : Visibility.Collapsed;
                tabSystemHealth.Visibility = tag == "6" ? Visibility.Visible : Visibility.Collapsed;
                
                tabMock.Visibility = Visibility.Collapsed;
                
                if (tag == "3")
                {
                    LoadLabRooms();
                }
                else if (tag == "4")
                {
                    LoadSystemSettings();
                }
                else if (tag == "5")
                {
                    LoadAuditLogs();
                }
                else if (tag == "6")
                {
                    LoadSystemHealthDiagnostics();
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        //  TEACHER CRUD
        // ═══════════════════════════════════════════════════════

        private void BtnAddTeacher_Click(object sender, RoutedEventArgs e)
        {
            var dialog = CreateTeacherDialog("Thêm Giáo Viên Mới", null);
            if (dialog.ShowDialog() == true)
            {
                var result = (TeacherProfile)dialog.Tag;
                try
                {
                    using var db = new AppDbContext();
                    
                    // 1. Kiểm tra trùng khóa chính nghiệp vụ (TeacherCode) để tránh sập app
                    bool isDuplicate = db.TeacherProfiles.Any(t => t.TeacherCode.ToLower() == result.TeacherCode.ToLower());
                    if (isDuplicate)
                    {
                        MessageBox.Show($"Mã giáo viên '{result.TeacherCode}' đã tồn tại trong hệ thống. Vui lòng kiểm tra lại!", 
                                        "Trùng mã giáo viên", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 2. Tạo mật khẩu mặc định băm HMACSHA512 chuẩn v4.2 (Gv@ + 4 ký tự cuối mã giáo viên)
                    string cleanCode = result.TeacherCode.Trim();
                    string suffix = cleanCode.Substring(Math.Max(0, cleanCode.Length - 4));
                    string defaultPwd = $"Gv@{suffix}";
                    string pwdHash = QASmartTouch.Services.AuthenticationService.HashPasswordHMACSHA512(defaultPwd);
                    
                    result.PasswordHash = pwdHash;
                    result.TeacherPassword = defaultPwd; // Lưu text cấu hình để hiển thị khi cần thiết

                    db.TeacherProfiles.Add(result);
                    db.SaveChanges();
                    Log.Information("[ADMIN_ACTION] Added teacher: {Name} ({Code})", result.FullName, result.TeacherCode);
                    LoadData();
                    MessageBox.Show($"Đã thêm giáo viên: {result.FullName}\nMật khẩu mặc định: {defaultPwd}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi thêm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRefreshTeacher_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void BtnEditTeacher_Click(object sender, RoutedEventArgs e)
        {
            if (dgTeachers.SelectedItem is not TeacherProfile selected)
            {
                MessageBox.Show("Vui lòng chọn giáo viên cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = CreateTeacherDialog("Sửa Thông Tin Giáo Viên", selected);
            if (dialog.ShowDialog() == true)
            {
                var updated = (TeacherProfile)dialog.Tag;
                try
                {
                    using var db = new AppDbContext();
                    var existing = db.TeacherProfiles.Find(selected.Id);
                    if (existing != null)
                    {
                        existing.TeacherCode = updated.TeacherCode;
                        existing.FullName = updated.FullName;
                        existing.Subject = updated.Subject;
                        existing.School = updated.School;
                        existing.Title = updated.Title;
                        existing.Phone = updated.Phone;
                        existing.Email = updated.Email;
                        existing.Notes = updated.Notes;
                        existing.IsActive = updated.IsActive;
                        existing.UpdatedAt = DateTime.Now;
                        db.SaveChanges();
                        Log.Information("[ADMIN_ACTION] Updated teacher: {Name} (ID={Id})", existing.FullName, existing.Id);
                        LoadData();
                        MessageBox.Show($"Đã cập nhật: {existing.FullName}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi cập nhật: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnDeleteTeacher_Click(object sender, RoutedEventArgs e)
        {
            if (dgTeachers.SelectedItem is not TeacherProfile selected)
            {
                MessageBox.Show("Vui lòng chọn giáo viên cần xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa giáo viên:\n\n• {selected.FullName} ({selected.TeacherCode})\n• Bộ môn: {selected.Subject}\n\nThao tác này không thể hoàn tác!",
                "Xác nhận Xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var db = new AppDbContext();
                    var entity = db.TeacherProfiles.Find(selected.Id);
                    if (entity != null)
                    {
                        db.TeacherProfiles.Remove(entity);
                        db.SaveChanges();
                        Log.Information("[ADMIN_ACTION] Deleted teacher: {Name} (ID={Id})", entity.FullName, entity.Id);
                        LoadData();
                        MessageBox.Show($"Đã xóa giáo viên: {entity.FullName}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        //  STUDENT SEARCH & IMPORT
        // ═══════════════════════════════════════════════════════

        private void BtnSearchStudent_Click(object sender, RoutedEventArgs e)
        {
            string term = txtSearchStudent.Text?.Trim().ToLower() ?? "";
            if (string.IsNullOrEmpty(term))
            {
                LoadData();
                return;
            }

            try
            {
                using var db = new AppDbContext();
                var students = db.Students
                    .Where(s => s.FullName.ToLower().Contains(term) || s.StudentCode.ToLower().Contains(term))
                    .ToList();
                dgStudents.ItemsSource = new ObservableCollection<Student>(students);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Student search error");
            }
        }

        private void BtnImportExcel_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Chọn file Excel hoặc CSV danh sách học sinh",
                Filter = "Excel & CSV Files (*.xlsx;*.csv)|*.xlsx;*.csv|Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv"
            };
            if (dlg.ShowDialog() != true) return;

            string filePath = dlg.FileName;
            try
            {
                System.Data.DataTable dt;
                if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    dt = QASmartClass.Services.ExcelDataService.ReadExcelToDataTable(filePath);
                }
                else if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    dt = ParseCsvToDataTable(filePath);
                }
                else
                {
                    MessageBox.Show("Định dạng file không được hỗ trợ. Vui lòng chọn file .xlsx hoặc .csv.", "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ImportFromDataTable(dt);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi import dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private System.Data.DataTable ParseCsvToDataTable(string filePath)
        {
            var table = new System.Data.DataTable();
            table.Columns.Add("MãHS");
            table.Columns.Add("HọTên");
            table.Columns.Add("Lớp");
            table.Columns.Add("Trường");

            var lines = System.IO.File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
            if (lines.Length <= 1) return table;

            // Bỏ qua dòng tiêu đề
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // Sử dụng Regex tách theo dấu phẩy ngoài dấu ngoặc kép (Text Qualifier)
                var parts = System.Text.RegularExpressions.Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)")
                             .Select(s => s.Trim().Trim('\"'))
                             .ToArray();

                var row = table.NewRow();
                for (int col = 0; col < Math.Min(parts.Length, 4); col++)
                {
                    row[col] = parts[col];
                }
                table.Rows.Add(row);
            }
            return table;
        }

        private void ImportFromDataTable(System.Data.DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show("File dữ liệu không có dòng thông tin hợp lệ nào!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int imported = 0;
            int skipped = 0;
            var warnings = new List<string>();

            using var db = new AppDbContext();
            using var transaction = db.Database.BeginTransaction();

            try
            {
                // Kiểm tra xem Data Table có đủ số cột tối thiểu không
                if (dt.Columns.Count < 2)
                {
                    throw new InvalidOperationException("File dữ liệu phải có ít nhất 2 cột: MãHS, HọTên.");
                }

                int rowNum = 1;
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    rowNum++;
                    string code = row[0]?.ToString()?.Trim() ?? "";
                    string name = row[1]?.ToString()?.Trim() ?? "";
                    string className = dt.Columns.Count > 2 ? row[2]?.ToString()?.Trim() ?? "" : "";
                    string school = dt.Columns.Count > 3 ? row[3]?.ToString()?.Trim() ?? "" : "";

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        warnings.Add($"Dòng {rowNum}: Tên học sinh bị trống (bỏ qua).");
                        skipped++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(code))
                    {
                        warnings.Add($"Dòng {rowNum} (Học sinh: {name}): Mã học sinh trống (bỏ qua).");
                        skipped++;
                        continue;
                    }

                    // Kiểm tra trùng mã học sinh trong CSDL
                    bool exists = db.Students.Any(s => s.StudentCode == code);
                    if (exists)
                    {
                        skipped++;
                        continue;
                    }

                    // Tạo mật khẩu mặc định băm HMACSHA512 theo chuẩn v4.2 (Hs@ + 4 ký tự cuối mã học sinh)
                    string cleanCode = code.Trim();
                    string suffix = cleanCode.Substring(Math.Max(0, cleanCode.Length - 4));
                    string defaultPwd = $"Hs@{suffix}";
                    string pwdHash = QASmartTouch.Services.AuthenticationService.HashPasswordHMACSHA512(defaultPwd);

                    db.Students.Add(new Student
                    {
                        StudentCode = code,
                        FullName = name,
                        ClassName = className,
                        SchoolName = school,
                        PasswordHash = pwdHash,
                        Status = "Active",
                        LastSeen = DateTime.Now
                    });
                    imported++;
                }

                db.SaveChanges();
                transaction.Commit();

                Log.Information("[ADMIN_ACTION] Imported {Count} students from DataTable, skipped {Skipped}", imported, skipped);
                LoadData();

                string msg = $"Import hoàn tất!\n\n✅ Đã thêm mới: {imported} học sinh\n⚠️ Bỏ qua (trùng/thiếu thông tin): {skipped} dòng";
                if (warnings.Any())
                {
                    msg += $"\n\n⚠️ Chi tiết cảnh báo:\n" + string.Join("\n", warnings.Take(5));
                    if (warnings.Count > 5) msg += $"\n... và {warnings.Count - 5} cảnh báo khác.";
                }
                MessageBox.Show(msg, "Import Dữ Liệu", MessageBoxButton.OK, warnings.Any() ? MessageBoxImage.Warning : MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                MessageBox.Show($"Lỗi lưu dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static string GenerateSecureRandomPassword(int length = 8)
        {
            const string validChars = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Removed O, 0, l, I
            var bytes = new byte[length];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            var result = new System.Text.StringBuilder();
            foreach (byte b in bytes)
            {
                result.Append(validChars[b % validChars.Length]);
            }
            return result.ToString();
        }

        // ═══════════════════════════════════════════════════════
        //  TEACHER DIALOG BUILDER
        // ═══════════════════════════════════════════════════════

        private static Window CreateTeacherDialog(string title, TeacherProfile? existing)
        {
            var win = new Window
            {
                Title = title,
                Width = 480, Height = 520,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)) // #0F172A Dark background
            };

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var sp = new StackPanel { Margin = new Thickness(24) };

            // Title
            sp.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 18, FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 0, 0, 16)
            });

            // Helper: create labeled field
            TextBox AddField(string label, string value)
            {
                sp.Children.Add(new TextBlock
                {
                    Text = label, FontSize = 12, FontWeight = FontWeights.SemiBold,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184)), // #94A3B8
                    Margin = new Thickness(0, 8, 0, 4)
                });
                var tb = new TextBox
                {
                    Text = value, FontSize = 13, Padding = new Thickness(8, 6, 8, 6),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59)), // #1E293B
                    Foreground = System.Windows.Media.Brushes.White,
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85)), // #334155
                    BorderThickness = new Thickness(1)
                };
                sp.Children.Add(tb);
                return tb;
            }

            var txtCode = AddField("Mã Giáo viên *", existing?.TeacherCode ?? "");
            var txtName = AddField("Họ và tên *", existing?.FullName ?? "");
            var txtSubject = AddField("Bộ môn *", existing?.Subject ?? "");
            var txtSchool = AddField("Trường", existing?.School ?? "");
            var txtTitle = AddField("Chức danh (GV/ThS/TS/PGS/GS)", existing?.Title ?? "GV");
            var txtPhone = AddField("Số điện thoại", existing?.Phone ?? "");
            var txtEmail = AddField("Email", existing?.Email ?? "");
            var txtNotes = AddField("Ghi chú", existing?.Notes ?? "");

            // Active checkbox
            var chkActive = new CheckBox
            {
                Content = "Đang hoạt động",
                IsChecked = existing?.IsActive ?? true,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 13, Margin = new Thickness(0, 12, 0, 0)
            };
            sp.Children.Add(chkActive);

            // Buttons
            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var btnCancel = new Button
            {
                Content = "Hủy", Width = 90, Height = 34, FontSize = 13,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59)), // #1E293B
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184)), // #94A3B8
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85)), // #334155
                Margin = new Thickness(0, 0, 10, 0), Cursor = System.Windows.Input.Cursors.Hand
            };
            btnCancel.Click += (s, ev) => { win.DialogResult = false; win.Close(); };

            var btnSave = new Button
            {
                Content = existing == null ? "➕ Thêm" : "💾 Lưu",
                Width = 110, Height = 34, FontSize = 13,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(37, 99, 235)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnSave.Click += (s, ev) =>
            {
                // Validate
                if (string.IsNullOrWhiteSpace(txtCode.Text) || string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtSubject.Text))
                {
                    MessageBox.Show("Vui lòng điền đầy đủ: Mã GV, Họ tên, Bộ môn.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                win.Tag = new TeacherProfile
                {
                    TeacherCode = txtCode.Text.Trim(),
                    FullName = txtName.Text.Trim(),
                    Subject = txtSubject.Text.Trim(),
                    School = txtSchool.Text.Trim(),
                    Title = txtTitle.Text.Trim(),
                    Phone = txtPhone.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    Notes = txtNotes.Text.Trim(),
                    IsActive = chkActive.IsChecked ?? true,
                    UpdatedAt = DateTime.Now
                };
                win.DialogResult = true;
                win.Close();
            };

            btnPanel.Children.Add(btnCancel);
            btnPanel.Children.Add(btnSave);
            sp.Children.Add(btnPanel);

            scroll.Content = sp;
            win.Content = scroll;
            return win;
        }

        // ═══════════════════════════════════════════════════════
        //  SETTINGS & TEMPLATES HELPERS
        // ═══════════════════════════════════════════════════════

                        private void ShowMessage(string message, string title, MessageBoxImage image = MessageBoxImage.Information)
        {
            bool isTestHost = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains("testhost");
            if (!isTestHost)
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, image);
            }
        }

        private void LoadSystemSettings()
        {
            try
            {
                using var db = new AppDbContext();
                
                 // 1. Topology mode
                 var ttlSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Broadcast_MulticastTTL")?.Value ?? "1";
                 if (cmbMulticastTTL != null)
                 {
                     foreach (ComboBoxItem item in cmbMulticastTTL.Items)
                     {
                         if (item.Tag?.ToString() == ttlSetting)
                         {
                             cmbMulticastTTL.SelectedItem = item;
                             break;
                         }
                     }
                 }

                 var igmpSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Broadcast_IGMPOptimization")?.Value ?? "True";
                 if (chkIgmpOptimization != null)
                     chkIgmpOptimization.IsChecked = igmpSetting.Equals("True", StringComparison.OrdinalIgnoreCase);

                 var arqSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Broadcast_EnableARQ")?.Value ?? "True";
                 if (chkEnableARQ != null)
                     chkEnableARQ.IsChecked = arqSetting.Equals("True", StringComparison.OrdinalIgnoreCase);
                var modeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "TopologyMode");
                string mode = modeSetting?.Value ?? "FileHeartbeat";
                if (radActiveScan != null) radActiveScan.IsChecked = mode == "ActiveScan";
                if (radFileHeartbeat != null) radFileHeartbeat.IsChecked = mode == "FileHeartbeat";
                if (radSimulationDemo != null) radSimulationDemo.IsChecked = mode == "SimulationDemo";

                // 2. Shared Folder path
                var sharedFolderSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "SharedFolderPath");
                if (txtSharedFolder != null)
                    txtSharedFolder.Text = sharedFolderSetting?.Value ?? System.IO.Path.Combine(QASmartClass.Services.AppPaths.RootDir, "SharedFiles");

                // 3. Role Security Mode
                var roleSecureSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "RoleSecurityMode");
                string roleSecure = roleSecureSetting?.Value ?? "DPAPI_Encrypted";
                if (radRoleSecureDPAPI != null) radRoleSecureDPAPI.IsChecked = roleSecure == "DPAPI_Encrypted";
                if (radRolePlaintext != null) radRolePlaintext.IsChecked = roleSecure == "Plaintext";

                // 4. Restore PIN Mode
                var restorePinSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "RestorePinMode");
                string restorePin = restorePinSetting?.Value ?? "PinRequired";
                if (radRestorePinRequired != null) radRestorePinRequired.IsChecked = restorePin == "PinRequired";

                var restorePinValueSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "RestorePinValue");
                if (txtRestorePinValue != null)
                    txtRestorePinValue.Text = restorePinValueSetting?.Value ?? "123456";

                // 5. Backup Folder path
                var backupFolderSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "BackupFolderPath");
                if (txtBackupFolder != null)
                    txtBackupFolder.Text = backupFolderSetting?.Value ?? QASmartClass.Services.AppPaths.BackupsDir;

                // 6. Security & Visitor Settings (v4.1)
                var visitorEncrypt = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_Visitor_Encryption")?.Value ?? "True";
                if (chkVisitorEncryption != null) chkVisitorEncryption.IsChecked = visitorEncrypt.Equals("True", StringComparison.OrdinalIgnoreCase);

                var printBadge = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_Visitor_PrintBadge")?.Value ?? "Disabled";
                if (chkPrintBadge != null) chkPrintBadge.IsChecked = printBadge.Equals("Enabled", StringComparison.OrdinalIgnoreCase) || printBadge.Equals("True", StringComparison.OrdinalIgnoreCase);

                var autoRelease = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_Visitor_AutoReleaseMode")?.Value ?? "DailyReset";
                if (chkAutoReleaseMode != null) chkAutoReleaseMode.IsChecked = autoRelease.Equals("DailyReset", StringComparison.OrdinalIgnoreCase) || autoRelease.Equals("True", StringComparison.OrdinalIgnoreCase);

                var preReg = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_Visitor_PreRegistration")?.Value ?? "Enabled";
                if (chkPreRegistration != null) chkPreRegistration.IsChecked = preReg.Equals("Enabled", StringComparison.OrdinalIgnoreCase) || preReg.Equals("True", StringComparison.OrdinalIgnoreCase);

                var preRegPortal = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_PreRegistration_Portal")?.Value ?? "False";
                if (chkPreRegPortal != null) chkPreRegPortal.IsChecked = preRegPortal.Equals("True", StringComparison.OrdinalIgnoreCase);

                var hostNotif = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_HostNotification_Enabled")?.Value ?? "False";
                if (chkHostNotification != null) chkHostNotification.IsChecked = hostNotif.Equals("True", StringComparison.OrdinalIgnoreCase);

                var wayfinding = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_WayfindingMap_Enabled")?.Value ?? "False";
                if (chkWayfindingMap != null) chkWayfindingMap.IsChecked = wayfinding.Equals("True", StringComparison.OrdinalIgnoreCase);

                var selfCheckOut = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_SelfCheckOut_Enabled")?.Value ?? "False";
                if (chkSelfCheckOut != null) chkSelfCheckOut.IsChecked = selfCheckOut.Equals("True", StringComparison.OrdinalIgnoreCase);

                var sentiment = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_SentimentSurvey_Enabled")?.Value ?? "False";
                if (chkSentimentSurvey != null) chkSentimentSurvey.IsChecked = sentiment.Equals("True", StringComparison.OrdinalIgnoreCase);

                var authLevel = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_Visitor_AuthLevel")?.Value ?? "High";
                if (cmbVisitorVerification != null)
                {
                    foreach (ComboBoxItem item in cmbVisitorVerification.Items)
                    {
                        if (item.Tag?.ToString() == authLevel)
                        {
                            cmbVisitorVerification.SelectedItem = item;
                            break;
                        }
                    }
                }

                var blinkingStyle = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_Visitor_BlinkingStyle")?.Value ?? "Pulse";
                if (blinkingStyle == "Blinking") blinkingStyle = "Pulse";
                if (cmbBorderEffect != null)
                {
                    foreach (ComboBoxItem item in cmbBorderEffect.Items)
                    {
                        if (item.Tag?.ToString() == blinkingStyle)
                        {
                            cmbBorderEffect.SelectedItem = item;
                            break;
                        }
                    }
                }

                var welcomeMsg = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_Kiosk_WelcomeMessage")?.Value ?? "Chào mừng quý khách đến với QA Smart School!";
                if (txtWelcomeMessage != null) txtWelcomeMessage.Text = welcomeMsg;

                var remoteCtrl = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Lab_RemoteControlMode")?.Value ?? "AdminOnly";
                if (cmbLabRemoteControlMode != null)
                {
                    foreach (ComboBoxItem item in cmbLabRemoteControlMode.Items)
                    {
                        if (item.Tag?.ToString() == remoteCtrl)
                        {
                            cmbLabRemoteControlMode.SelectedItem = item;
                            break;
                        }
                    }
                }

                var refreshInt = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Lab_StatusRefreshInterval")?.Value ?? "15";
                if (cmbLabStatusRefreshInterval != null)
                {
                    foreach (ComboBoxItem item in cmbLabStatusRefreshInterval.Items)
                    {
                        if (item.Tag?.ToString() == refreshInt)
                        {
                            cmbLabStatusRefreshInterval.SelectedItem = item;
                            break;
                        }
                    }
                }

                var autoPower = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Lab_AutoPowerOffPolicy")?.Value ?? "None";
                if (cmbLabAutoPowerOffPolicy != null)
                {
                    foreach (ComboBoxItem item in cmbLabAutoPowerOffPolicy.Items)
                    {
                        if (item.Tag?.ToString() == autoPower)
                        {
                            cmbLabAutoPowerOffPolicy.SelectedItem = item;
                            break;
                        }
                    }
                }

                var faultyAct = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Lab_FaultyDeviceAction")?.Value ?? "LogOnly";
                if (cmbLabFaultyDeviceAction != null)
                {
                    foreach (ComboBoxItem item in cmbLabFaultyDeviceAction.Items)
                    {
                        if (item.Tag?.ToString() == faultyAct)
                        {
                            cmbLabFaultyDeviceAction.SelectedItem = item;
                            break;
                        }
                    }
                }

                var mcAddr = db.SystemSettings.FirstOrDefault(s => s.Id == "Network_MulticastAddress")?.Value ?? "239.0.0.1";
                if (txtMulticastAddress != null) txtMulticastAddress.Text = mcAddr;

                var mcPort = db.SystemSettings.FirstOrDefault(s => s.Id == "Network_MulticastPort")?.Value ?? "8088";
                if (txtMulticastPort != null) txtMulticastPort.Text = mcPort;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to load system settings");
            }
        }

        public void BtnSaveSystemSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new AppDbContext();

                // 1. Save TopologyMode
                string selectedMode = "FileHeartbeat";
                if (radActiveScan?.IsChecked == true) selectedMode = "ActiveScan";
                else if (radSimulationDemo?.IsChecked == true) selectedMode = "SimulationDemo";
                SaveSetting(db, "TopologyMode", selectedMode, "Network");


                // 2. Save SharedFolderPath
                string sharedFolder = txtSharedFolder?.Text.Trim() ?? "";
                if (!string.IsNullOrEmpty(sharedFolder))
                {
                    SaveSetting(db, "SharedFolderPath", sharedFolder, "Network");
                }

                // 3. Save RoleSecurityMode
                string roleSecure = radRoleSecureDPAPI?.IsChecked == true ? "DPAPI_Encrypted" : "Plaintext";
                SaveSetting(db, "RoleSecurityMode", roleSecure, "Security");

                // 4. Save RestorePinMode (Mandated as PinRequired in v4.1)
                SaveSetting(db, "RestorePinMode", "PinRequired", "Security");
 
                if (txtRestorePinValue != null)
                {
                    string pinVal = txtRestorePinValue.Text.Trim();
                    if (string.IsNullOrEmpty(pinVal) || pinVal.Length < 6 || !pinVal.All(char.IsDigit))
                    {
                        ShowMessage("Mã PIN khôi phục phải từ 6 đến 12 ký tự số!", "Lỗi cấu hình", MessageBoxImage.Warning);
                        return;
                    }
                    SaveSetting(db, "RestorePinValue", pinVal, "Security");
                }

                // 5. Save BackupFolderPath
                string backupFolder = txtBackupFolder?.Text.Trim() ?? "";
                if (!string.IsNullOrEmpty(backupFolder))
                {
                    SaveSetting(db, "BackupFolderPath", backupFolder, "Backup");
                }

                // 6. Save Security & Visitor Settings (v4.1)
                SaveSetting(db, "Security_Visitor_Encryption", chkVisitorEncryption?.IsChecked == true ? "True" : "False", "Security");
                SaveSetting(db, "Security_Visitor_PrintBadge", chkPrintBadge?.IsChecked == true ? "Enabled" : "Disabled", "Security");
                SaveSetting(db, "Security_Visitor_AutoReleaseMode", chkAutoReleaseMode?.IsChecked == true ? "DailyReset" : "Disabled", "Security");
                SaveSetting(db, "Security_Visitor_PreRegistration", chkPreRegistration?.IsChecked == true ? "Enabled" : "Disabled", "Security");
                
                SaveSetting(db, "Security_PreRegistration_Portal", chkPreRegPortal?.IsChecked == true ? "True" : "False", "Security");
                SaveSetting(db, "Security_HostNotification_Enabled", chkHostNotification?.IsChecked == true ? "True" : "False", "Security");
                SaveSetting(db, "Security_WayfindingMap_Enabled", chkWayfindingMap?.IsChecked == true ? "True" : "False", "Security");
                SaveSetting(db, "Security_SelfCheckOut_Enabled", chkSelfCheckOut?.IsChecked == true ? "True" : "False", "Security");
                SaveSetting(db, "Security_SentimentSurvey_Enabled", chkSentimentSurvey?.IsChecked == true ? "True" : "False", "Security");

                if (cmbVisitorVerification?.SelectedItem is ComboBoxItem verificationItem && verificationItem.Tag != null)
                {
                    SaveSetting(db, "Security_Visitor_AuthLevel", verificationItem.Tag.ToString()!, "Security");
                }

                if (cmbBorderEffect?.SelectedItem is ComboBoxItem effectItem && effectItem.Tag != null)
                {
                    SaveSetting(db, "Security_Visitor_BlinkingStyle", effectItem.Tag.ToString()!, "Security");
                }

                if (txtWelcomeMessage != null)
                {
                    SaveSetting(db, "Security_Kiosk_WelcomeMessage", txtWelcomeMessage.Text, "Security");
                }

                if (cmbLabRemoteControlMode?.SelectedItem is ComboBoxItem rcItem && rcItem.Tag != null)
                {
                    SaveSetting(db, "IT_Lab_RemoteControlMode", rcItem.Tag.ToString()!, "Lab");
                }
                if (cmbLabStatusRefreshInterval?.SelectedItem is ComboBoxItem riItem && riItem.Tag != null)
                {
                    SaveSetting(db, "IT_Lab_StatusRefreshInterval", riItem.Tag.ToString()!, "Lab");
                }
                if (cmbLabAutoPowerOffPolicy?.SelectedItem is ComboBoxItem poItem && poItem.Tag != null)
                {
                    SaveSetting(db, "IT_Lab_AutoPowerOffPolicy", poItem.Tag.ToString()!, "Lab");
                }
                if (cmbLabFaultyDeviceAction?.SelectedItem is ComboBoxItem fdItem && fdItem.Tag != null)
                {
                    SaveSetting(db, "IT_Lab_FaultyDeviceAction", fdItem.Tag.ToString()!, "Lab");
                }

                // Save Multicast Settings
                if (txtMulticastAddress != null && txtMulticastPort != null)
                {
                    string addrInput = txtMulticastAddress.Text.Trim();
                    string portInput = txtMulticastPort.Text.Trim();

                    if (!System.Net.IPAddress.TryParse(addrInput, out var parsedIp) || 
                        parsedIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork ||
                        (parsedIp.GetAddressBytes()[0] & 0xF0) != 0xE0)
                    {
                        ShowMessage("Địa chỉ Multicast UDP phải là một địa chỉ IP thuộc dải 224.0.0.0/4 (ví dụ: 239.0.0.1)!", "Lỗi cấu hình", MessageBoxImage.Warning);
                        return;
                    }

                    if (!int.TryParse(portInput, out var parsedPort) || parsedPort < 1024 || parsedPort > 65535)
                    {
                        ShowMessage("Cổng Multicast UDP phải là số nguyên trong khoảng từ 1024 đến 65535!", "Lỗi cấu hình", MessageBoxImage.Warning);
                        return;
                    }

                    SaveSetting(db, "Network_MulticastAddress", addrInput, "Network");
                    SaveSetting(db, "Network_MulticastPort", portInput, "Network");

                    if (cmbMulticastTTL?.SelectedItem is ComboBoxItem ttlItem && ttlItem.Tag != null)
                    {
                        SaveSetting(db, "Broadcast_MulticastTTL", ttlItem.Tag.ToString()!, "Broadcast");
                    }
                    SaveSetting(db, "Broadcast_IGMPOptimization", chkIgmpOptimization?.IsChecked == true ? "True" : "False", "Broadcast");
                    SaveSetting(db, "Broadcast_EnableARQ", chkEnableARQ?.IsChecked == true ? "True" : "False", "Broadcast");
                }

                db.SaveChanges();
                // Write Audit Log for settings change
                string currentUser = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "Admin";
                QASmartClass.Services.AuditHelper.Log(db, "SystemConfigChange", currentUser, "Thay đổi cấu hình an ninh hệ thống và Master Quản lý Phòng Máy.");

                db.SaveChanges();
                ShowMessage("Đã lưu cấu hình hệ thống thành công!", "Cấu hình", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowMessage($"Lỗi khi lưu cấu hình: {ex.Message}", "Lỗi", MessageBoxImage.Error);
            }
        }

        private void SaveSetting(AppDbContext db, string key, string value, string category)
        {
            var setting = db.SystemSettings.Find(key);
            if (setting == null)
            {
                db.SystemSettings.Add(new SystemSetting
                {
                    Id = key,
                    Value = value,
                    Category = category,
                    LastUpdated = DateTime.Now
                });
            }
            else
            {
                setting.Value = value;
                setting.LastUpdated = DateTime.Now;
            }
        }

        private void BtnBrowseSharedFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Chọn thư mục chia sẻ mạng LAN cho phòng máy",
                UseDescriptionForTitle = true
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtSharedFolder.Text = dialog.SelectedPath;
            }
        }

        private void BtnBrowseBackupFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Chọn thư mục lưu trữ file sao lưu CSDL",
                UseDescriptionForTitle = true
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtBackupFolder.Text = dialog.SelectedPath;
            }
        }

        private void BtnDownloadTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = "danh_sach_hoc_sinh_template.csv",
                    DefaultExt = ".csv",
                    Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    Title = "Lưu file Excel/CSV danh sách học sinh mẫu"
                };

                if (sfd.ShowDialog() == true)
                {
                    string path = sfd.FileName;
                    // Ghi file mẫu UTF-8 có BOM
                    var utf8WithBom = new System.Text.UTF8Encoding(true);
                    var lines = new[] 
                    {
                        "MãHS,HọTên,Lớp,Trường",
                        "HS001,Nguyễn Văn A,10A1,Trường THPT Mẫu",
                        "HS002,Trần Thị B,10A1,Trường THPT Mẫu"
                    };
                    File.WriteAllLines(path, lines, utf8WithBom);
                    MessageBox.Show($"Đã tải file Excel/CSV mẫu thành công về máy:\n{path}", "Tải file mẫu", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải file mẫu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    
        // ═══════════════════════════════════════════════════════
        //  AUDIT & SECURITY LOGS (v4.1)
        // ═══════════════════════════════════════════════════════

        private void LoadAuditLogs()
        {
            try
            {
                using var db = new AppDbContext();
                
                string keyword = txtAuditSearch?.Text.Trim().ToLower() ?? "";
                
                string logType = "All";
                if (cmbLogType?.SelectedItem is ComboBoxItem typeItem && typeItem.Tag != null)
                {
                    logType = typeItem.Tag.ToString()!;
                }

                string period = "Today";
                if (cmbLogPeriod?.SelectedItem is ComboBoxItem periodItem && periodItem.Tag != null)
                {
                    period = periodItem.Tag.ToString()!;
                }

                DateTime startDate = DateTime.Today;
                DateTime endDate = DateTime.Now.AddDays(1);

                if (period == "7Days")
                {
                    startDate = DateTime.Today.AddDays(-7);
                }
                else if (period == "30Days")
                {
                    startDate = DateTime.Today.AddDays(-30);
                }

                var list = new List<AuditLogDisplay>();

                // Load from EventLogs
                if (logType == "All" || logType == "SystemConfigChange")
                {
                    var events = db.EventLogs
                        .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                        .ToList();

                    foreach (var l in events)
                    {
                        list.Add(new AuditLogDisplay
                        {
                            Timestamp = l.Timestamp,
                            Actor = l.Actor,
                            Action = l.EventType,
                            Details = MaskCccdSensitiveInfo(l.Details)
                        });
                    }
                }

                if (logType == "All" || logType == "SystemConfigChange")
                {
                    var secureLogs = db.AuditLogs
                        .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                        .ToList();

                    foreach (var l in secureLogs)
                    {
                        list.Add(new AuditLogDisplay
                        {
                            Timestamp = l.Timestamp,
                            Actor = l.ActorName,
                            Action = l.Action,
                            Details = MaskCccdSensitiveInfo(l.Details)
                        });
                    }
                }



                // Load from SecurityLogs
                if (logType == "All" || logType == "SecurityKiosk" || logType == "Incident")
                {
                    var security = db.SecurityLogs
                        .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                        .ToList();

                    foreach (var l in security)
                    {
                        if (logType == "Incident" && l.EventType != "Incident") continue;
                        if (logType == "SecurityKiosk" && l.EventType == "Incident") continue;

                        list.Add(new AuditLogDisplay
                        {
                            Timestamp = l.Timestamp,
                            Actor = string.IsNullOrEmpty(l.GuardName) ? "Bảo vệ" : l.GuardName,
                            Action = l.EventType switch
                            {
                                "CheckIn" => "Khách vào cổng",
                                "CheckOut" => "Khách ra cổng",
                                "Incident" => "Sự cố an ninh",
                                "KeyExchange" => "Bàn giao chìa khóa",
                                _ => l.EventType
                            },
                            Details = MaskCccdSensitiveInfo(l.Description)
                        });
                    }
                }

                // Apply keyword filter
                var filtered = list.AsEnumerable();
                if (!string.IsNullOrEmpty(keyword))
                {
                    filtered = filtered.Where(l => 
                        (l.Actor != null && l.Actor.ToLower().Contains(keyword)) ||
                        (l.Action != null && l.Action.ToLower().Contains(keyword)) ||
                        (l.Details != null && l.Details.ToLower().Contains(keyword))
                    );
                }

                // Sort and limit to 200 items (Self-criticism / Solution 1)
                var sorted = filtered
                    .OrderByDescending(l => l.Timestamp)
                    .Take(200)
                    .ToList();

                if (dgAuditLogs != null)
                {
                    dgAuditLogs.ItemsSource = new ObservableCollection<AuditLogDisplay>(sorted);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to load audit logs");
            }
        }

        public void BtnFilterAuditLogs_Click(object sender, RoutedEventArgs e)
        {
            LoadAuditLogs();
        }

        public void BtnExportAuditLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new AppDbContext();
                
                string keyword = txtAuditSearch?.Text.Trim().ToLower() ?? "";
                
                string logType = "All";
                if (cmbLogType?.SelectedItem is ComboBoxItem typeItem && typeItem.Tag != null)
                {
                    logType = typeItem.Tag.ToString()!;
                }

                string period = "Today";
                if (cmbLogPeriod?.SelectedItem is ComboBoxItem periodItem && periodItem.Tag != null)
                {
                    period = periodItem.Tag.ToString()!;
                }

                DateTime startDate = DateTime.Today;
                DateTime endDate = DateTime.Now.AddDays(1);

                if (period == "7Days")
                {
                    startDate = DateTime.Today.AddDays(-7);
                }
                else if (period == "30Days")
                {
                    startDate = DateTime.Today.AddDays(-30);
                }

                var list = new List<AuditLogDisplay>();

                if (logType == "All" || logType == "SystemConfigChange")
                {
                    var events = db.EventLogs
                        .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                        .ToList();

                    foreach (var l in events)
                    {
                        list.Add(new AuditLogDisplay
                        {
                            Timestamp = l.Timestamp,
                            Actor = l.Actor,
                            Action = l.EventType,
                            Details = MaskCccdSensitiveInfo(l.Details)
                        });
                    }
                }

                if (logType == "All" || logType == "SystemConfigChange")
                {
                    var secureLogs = db.AuditLogs
                        .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                        .ToList();

                    foreach (var l in secureLogs)
                    {
                        list.Add(new AuditLogDisplay
                        {
                            Timestamp = l.Timestamp,
                            Actor = l.ActorName,
                            Action = l.Action,
                            Details = MaskCccdSensitiveInfo(l.Details)
                        });
                    }
                }

                if (logType == "All" || logType == "SecurityKiosk" || logType == "Incident")
                {
                    var security = db.SecurityLogs
                        .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                        .ToList();

                    foreach (var l in security)
                    {
                        if (logType == "Incident" && l.EventType != "Incident") continue;
                        if (logType == "SecurityKiosk" && l.EventType == "Incident") continue;

                        list.Add(new AuditLogDisplay
                        {
                            Timestamp = l.Timestamp,
                            Actor = string.IsNullOrEmpty(l.GuardName) ? "Bảo vệ" : l.GuardName,
                            Action = l.EventType switch
                            {
                                "CheckIn" => "Khách vào cổng",
                                "CheckOut" => "Khách ra cổng",
                                "Incident" => "Sự cố an ninh",
                                "KeyExchange" => "Bàn giao chìa khóa",
                                _ => l.EventType
                            },
                            Details = MaskCccdSensitiveInfo(l.Description)
                        });
                    }
                }

                var filtered = list.AsEnumerable();
                if (!string.IsNullOrEmpty(keyword))
                {
                    filtered = filtered.Where(l => 
                        (l.Actor != null && l.Actor.ToLower().Contains(keyword)) ||
                        (l.Action != null && l.Action.ToLower().Contains(keyword)) ||
                        (l.Details != null && l.Details.ToLower().Contains(keyword))
                    );
                }

                var sorted = filtered.OrderByDescending(l => l.Timestamp).ToList();

                string exportDir = QASmartClass.Services.AppPaths.ExportsDir;
                System.IO.Directory.CreateDirectory(exportDir);

                string filePath = System.IO.Path.Combine(exportDir, $"NhatKyHeThong_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                using (var sw = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    sw.Write('\uFEFF'); // BOM
                    sw.WriteLine("Thời gian,Người thực hiện,Hành động,Chi tiết sự kiện");
                    foreach (var item in sorted)
                    {
                        string line = $"\"{item.Timestamp:dd/MM/yyyy HH:mm:ss}\",\"{EscapeCsvField(item.Actor)}\",\"{EscapeCsvField(item.Action)}\",\"{EscapeCsvField(item.Details)}\"";
                        sw.WriteLine(line);
                    }
                }

                ShowMessage($"Đã xuất thành công {sorted.Count} bản ghi nhật ký ra file:\n{filePath}", "Xuất báo cáo thành công", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowMessage($"Lỗi khi xuất báo cáo: {ex.Message}", "Lỗi", MessageBoxImage.Error);
            }
        }

        private string MaskCccdSensitiveInfo(string details)
        {
            if (string.IsNullOrEmpty(details)) return details;
            
            var masked = System.Text.RegularExpressions.Regex.Replace(details, @"(CCCD:\s*)(\d{9,12})", m =>
            {
                string num = m.Groups[2].Value;
                if (num.Length >= 6)
                {
                    return m.Groups[1].Value + num.Substring(0, 3) + "******" + num.Substring(num.Length - 3);
                }
                return m.Value;
            });

            // Mask Vietnamese phone numbers (10 digits, starts with 0)
            masked = System.Text.RegularExpressions.Regex.Replace(masked, @"\b(0\d{2})\d{4}(\d{3})\b", "$1******$2");
            // Mask raw 12-digit CCCD/ID numbers
            masked = System.Text.RegularExpressions.Regex.Replace(masked, @"\b(\d{3})\d{6}(\d{3})\b", "$1******$2");
            return masked;
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            return field.Replace("\"", "\"\"");
        }

        public class LabDeviceItem
        {
            public string MachineId { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string NodeType { get; set; } = "Student";
            public string Status { get; set; } = "Offline";
            public string IpAddress { get; set; } = "";
        }

        private List<LabDeviceItem> _allDevices = new List<LabDeviceItem>();

        private void LoadLabRooms()
        {
            if (cboLabRoom == null) return;
            if (cboLabRoom.Items.Count == 0)
            {
                cboLabRoom.Items.Add("LAB01");
                cboLabRoom.Items.Add("LAB02");
                cboLabRoom.Items.Add("LAB03");
                cboLabRoom.SelectedIndex = 0;
            }
            else
            {
                string currentRoom = cboLabRoom.SelectedItem?.ToString() ?? "LAB01";
                LoadLabDevices(currentRoom);
            }
        }

        private void LoadLabDevices(string roomId)
        {
            try
            {
                string path = QASmartClass.Services.AppPaths.GetTopologyFile(roomId);
                QASmartClass.Models.TopologyData data = null;
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    data = System.Text.Json.JsonSerializer.Deserialize<QASmartClass.Models.TopologyData>(json);
                }

                if (data == null || data.Nodes == null || data.Nodes.Count == 0)
                {
                    data = GenerateDefaultTopologyData(roomId);
                }

                _allDevices = data.Nodes.Select(n => new LabDeviceItem
                {
                    MachineId = n.MachineId,
                    DisplayName = n.DisplayName,
                    NodeType = n.NodeType,
                    Status = n.Status,
                    IpAddress = GenerateIpAddress(roomId, n.MachineId)
                }).ToList();

                FilterAndBindDevices();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load lab devices for {Room}", roomId);
            }
        }

        private QASmartClass.Models.TopologyData GenerateDefaultTopologyData(string roomId)
        {
            var data = new QASmartClass.Models.TopologyData { RoomId = roomId };
            
            data.Nodes.Add(new QASmartClass.Models.TopologyNode { MachineId = "IOT-IB", DisplayName = "Bảng tương tác", NodeType = "InteractiveBoard", Status = "Online", X = 200, Y = 50 });
            data.Nodes.Add(new QASmartClass.Models.TopologyNode { MachineId = "IOT-CAM", DisplayName = "Camera AI", NodeType = "Camera", Status = "Online", X = 600, Y = 50 });
            data.Nodes.Add(new QASmartClass.Models.TopologyNode { MachineId = "TEACHER-PC", DisplayName = "Máy giáo viên", NodeType = "Teacher", Status = "Online", X = 400, Y = 100 });

            for (int i = 1; i <= 20; i++)
            {
                data.Nodes.Add(new QASmartClass.Models.TopologyNode
                {
                    MachineId = $"STUDENT-PC-{i:00}",
                    DisplayName = $"Máy học sinh {i:00}",
                    NodeType = "Student",
                    Status = i % 7 == 0 ? "Faulty" : (i % 3 == 0 ? "Offline" : "Online"),
                    X = 100 + ((i - 1) % 5) * 150,
                    Y = 200 + ((i - 1) / 5) * 100
                });
            }

            try
            {
                string path = QASmartClass.Services.AppPaths.GetTopologyFile(roomId);
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { }

            return data;
        }

        private string GenerateIpAddress(string roomId, string machineId)
        {
            int subnet = roomId == "LAB01" ? 10 : (roomId == "LAB02" ? 11 : 12);
            int lastOctet = 100;
            if (machineId == "IOT-IB") lastOctet = 5;
            else if (machineId == "IOT-CAM") lastOctet = 6;
            else if (machineId == "TEACHER-PC") lastOctet = 10;
            else if (machineId.StartsWith("STUDENT-PC-"))
            {
                int.TryParse(machineId.Substring(11), out int num);
                lastOctet = 10 + num;
            }
            return $"192.168.{subnet}.{lastOctet}";
        }

        private void FilterAndBindDevices()
        {
            if (dgLabDevices == null) return;

            string filter = "All";
            if (cboStatusFilter?.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                filter = item.Tag.ToString();
            }

            var filtered = _allDevices.Where(d => filter == "All" || d.Status == filter).ToList();
            dgLabDevices.ItemsSource = filtered;

            UpdateLabKpi();
        }

        private void UpdateLabKpi()
        {
            if (txtLabTotal != null) txtLabTotal.Text = _allDevices.Count.ToString();
            if (txtLabOnline != null) txtLabOnline.Text = _allDevices.Count(d => d.Status == "Online").ToString();
            if (txtLabOffline != null) txtLabOffline.Text = _allDevices.Count(d => d.Status == "Offline").ToString();
            if (txtLabFaulty != null) txtLabFaulty.Text = _allDevices.Count(d => d.Status == "Faulty").ToString();
        }

        private void CboLabRoom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboLabRoom?.SelectedItem != null)
            {
                LoadLabDevices(cboLabRoom.SelectedItem.ToString());
            }
        }

        private void CboStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterAndBindDevices();
        }

        private void DgLabDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgLabDevices?.SelectedItem is LabDeviceItem selected)
            {
                if (txtSelectedDeviceName != null) txtSelectedDeviceName.Text = selected.DisplayName;
                if (txtSelectedDeviceIp != null) txtSelectedDeviceIp.Text = $"IP: {selected.IpAddress} ({selected.MachineId})";
            }
            else
            {
                if (txtSelectedDeviceName != null) txtSelectedDeviceName.Text = "Chưa chọn máy trạm";
                if (txtSelectedDeviceIp != null) txtSelectedDeviceIp.Text = "IP: ---.---.---.---";
            }
        }

        private string GetMasterSetting(string id, string defaultValue)
        {
            try
            {
                using var db = new AppDbContext();
                return db.SystemSettings.FirstOrDefault(s => s.Id == id)?.Value ?? defaultValue;
            }
            catch { return defaultValue; }
        }

        private void ExecuteRemoteCommand(string machineId, string commandType, bool targetAll = false)
        {
            string remoteMode = GetMasterSetting("IT_Lab_RemoteControlMode", "AdminOnly");
            if (remoteMode == "Disabled")
            {
                ShowMessage("Hành động bị chặn: Tính năng điều khiển từ xa phòng máy đang bị VÔ HIỆU HÓA trên hệ thống!", "Chính sách hệ thống", MessageBoxImage.Warning);
                return;
            }

            var role = QASmartClass.Staff.Services.StaffSession.Role;
            if (remoteMode == "AdminOnly" && role != "Admin" && role != "HieuTruong")
            {
                ShowMessage("Hành động bị chặn: Bạn không có quyền quản trị để thực hiện lệnh điều khiển từ xa!", "Chính sách hệ thống", MessageBoxImage.Warning);
                return;
            }

            string roomId = cboLabRoom?.SelectedItem?.ToString() ?? "LAB01";
            string path = QASmartClass.Services.AppPaths.GetTopologyFile(roomId);
            if (!File.Exists(path)) return;

            try
            {
                string json = File.ReadAllText(path);
                var data = System.Text.Json.JsonSerializer.Deserialize<QASmartClass.Models.TopologyData>(json);
                if (data == null) return;

                int affectedCount = 0;
                foreach (var node in data.Nodes)
                {
                    bool match = targetAll ? (node.NodeType == "Student") : (node.MachineId == machineId);
                    if (match)
                    {
                        if (commandType == "WOL")
                        {
                            node.Status = "Online";
                        }
                        else if (commandType == "Shutdown")
                        {
                            node.Status = "Offline";
                        }
                        else if (commandType == "Restart")
                        {
                            node.Status = "Online";
                        }
                        else if (commandType == "Lock")
                        {
                            node.Status = "Offline";
                        }
                        else if (commandType == "Maintenance")
                        {
                            node.Status = node.Status == "Faulty" ? "Offline" : "Faulty";
                        }
                        affectedCount++;
                    }
                }

                if (affectedCount > 0)
                {
                    string newJson = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(path, newJson);

                    LoadLabDevices(roomId);

                    using var db = new AppDbContext();
                    string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "Admin";
                    string detailStr = targetAll 
                        ? $"Gửi lệnh đồng loạt [{commandType}] cho toàn bộ máy học sinh tại phòng [{roomId}]."
                        : $"Gửi lệnh [{commandType}] đến máy trạm [{machineId}] tại phòng [{roomId}].";
                    
                    QASmartClass.Services.AuditHelper.Log(db, "REMOTE_COMMAND", actor, detailStr);
                    db.SaveChanges();

                    ShowMessage($"Thực hiện thành công lệnh [{commandType}] cho {(targetAll ? $"{affectedCount} máy" : $"máy {machineId}")}.", "Kết quả", MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Lỗi khi thực thi lệnh: {ex.Message}", "Lỗi", MessageBoxImage.Error);
            }
        }

        private void BtnWOLSingle_Click(object sender, RoutedEventArgs e)
        {
            if (dgLabDevices.SelectedItem is LabDeviceItem selected)
            {
                ExecuteRemoteCommand(selected.MachineId, "WOL");
            }
            else
            {
                ShowMessage("Vui lòng chọn một máy trạm trong danh sách để điều khiển!", "Lưu ý", MessageBoxImage.Warning);
            }
        }

        private void BtnShutdownSingle_Click(object sender, RoutedEventArgs e)
        {
            if (dgLabDevices.SelectedItem is LabDeviceItem selected)
            {
                if (ConfirmAction($"Bạn có chắc chắn muốn TẮT máy trạm {selected.DisplayName} không?"))
                {
                    ExecuteRemoteCommand(selected.MachineId, "Shutdown");
                }
            }
            else
            {
                ShowMessage("Vui lòng chọn một máy trạm trong danh sách để điều khiển!", "Lưu ý", MessageBoxImage.Warning);
            }
        }

        private void BtnRestartSingle_Click(object sender, RoutedEventArgs e)
        {
            if (dgLabDevices.SelectedItem is LabDeviceItem selected)
            {
                if (ConfirmAction($"Bạn có chắc chắn muốn KHỞI ĐỘNG LẠI máy trạm {selected.DisplayName} không?"))
                {
                    ExecuteRemoteCommand(selected.MachineId, "Restart");
                }
            }
            else
            {
                ShowMessage("Vui lòng chọn một máy trạm trong danh sách để điều khiển!", "Lưu ý", MessageBoxImage.Warning);
            }
        }

        private void BtnLockSingle_Click(object sender, RoutedEventArgs e)
        {
            if (dgLabDevices.SelectedItem is LabDeviceItem selected)
            {
                if (ConfirmAction($"Bạn có chắc chắn muốn KHÓA MÀN HÌNH máy trạm {selected.DisplayName} không?"))
                {
                    ExecuteRemoteCommand(selected.MachineId, "Lock");
                }
            }
            else
            {
                ShowMessage("Vui lòng chọn một máy trạm trong danh sách để điều khiển!", "Lưu ý", MessageBoxImage.Warning);
            }
        }

        private void BtnToggleMaintenance_Click(object sender, RoutedEventArgs e)
        {
            if (dgLabDevices.SelectedItem is LabDeviceItem selected)
            {
                ExecuteRemoteCommand(selected.MachineId, "Maintenance");
            }
            else
            {
                ShowMessage("Vui lòng chọn một máy trạm trong danh sách để điều khiển!", "Lưu ý", MessageBoxImage.Warning);
            }
        }

        private void BtnWOLAll_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmAction("Bạn có chắc chắn muốn bật (Wake-on-LAN) TOÀN BỘ máy trạm học sinh không?"))
            {
                ExecuteRemoteCommand("", "WOL", true);
            }
        }

        private void BtnShutdownAll_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmAction("CẢNH BÁO: Bạn có chắc chắn muốn TẮT TOÀN BỘ máy trạm học sinh không?"))
            {
                ExecuteRemoteCommand("", "Shutdown", true);
            }
        }

        private void BtnLockAll_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmAction("Bạn có chắc chắn muốn KHÓA MÀN HÌNH TOÀN BỘ máy trạm học sinh không?"))
            {
                ExecuteRemoteCommand("", "Lock", true);
            }
        }

        private bool ConfirmAction(string message)
        {
            bool isTestHost = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains("testhost");
            if (isTestHost) return true;
            
            var result = MessageBox.Show(message, "Xác nhận hành động", MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        private void LoadSystemHealthDiagnostics()
        {
            try
            {
                string dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
                if (File.Exists(dbPath))
                {
                    long bytes = new FileInfo(dbPath).Length;
                    double mb = bytes / (1024.0 * 1024.0);
                    lblDbSize.Text = $"{mb:F2} MB ({bytes:N0} bytes)";
                }
                else
                {
                    lblDbSize.Text = "Không tìm thấy file CSDL";
                }

                using var db = new AppDbContext();
                string journalMode = "Unknown";
                string busyTimeout = "Unknown";

                try
                {
                    var connection = db.Database.GetDbConnection();
                    bool wasOpened = false;
                    if (connection.State != System.Data.ConnectionState.Open) { connection.Open(); wasOpened = true; }
                    try
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "PRAGMA journal_mode;";
                            journalMode = command.ExecuteScalar()?.ToString() ?? "Unknown";
                        }

                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "PRAGMA busy_timeout;";
                            busyTimeout = $"{command.ExecuteScalar()} ms";
                        }
                    }
                    finally { if (wasOpened) connection.Close(); }
                }
                catch (Exception ex) { Log.Warning(ex, "Failed to read PRAGMA settings"); }

                lblDbWalMode.Text = journalMode.ToUpper();
                lblDbTimeout.Text = busyTimeout;

                int logCount = 0;
                try
                {
                    logCount = db.AuditLogs.Count();
                }
                catch (Exception ex) { Log.Warning(ex, "Failed to count AuditLogs"); }
                lblDbLogCount.Text = $"{logCount:N0} bản ghi";

                string localIp = "127.0.0.1";
                try
                {
                    var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                    var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    if (ip != null) localIp = ip.ToString();
                }
                catch (Exception ex) { Log.Warning(ex, "Failed to resolve local IP address"); }
                lblTeacherIp.Text = localIp;

                string compressionFormat = "WebP (Nén OpenCV)";
                try
                {
                    var compSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Network_ScreenCompressionFormat");
                    if (compSetting != null && !string.IsNullOrEmpty(compSetting.Value))
                    {
                        compressionFormat = compSetting.Value;
                    }
                }
                catch (Exception ex) { Log.Warning(ex, "Failed to read compression format setting"); }
                lblCompressionFormat.Text = compressionFormat;

                string keepAliveVal = "30 giây (Mặc định)";
                try
                {
                    var kaSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Network_KeepAliveTimeout");
                    if (kaSetting != null && !string.IsNullOrEmpty(kaSetting.Value))
                    {
                        keepAliveVal = $"{kaSetting.Value} giây";
                    }
                }
                catch (Exception ex) { Log.Warning(ex, "Failed to read KeepAlive timeout setting"); }
                lblKeepAliveTimeout.Text = keepAliveVal;

                // Dynamic Multicast Address/Port & Security Configuration (UPGRADE_10 Compliance)
                string multicastAddr = "239.0.0.1";
                string multicastPort = "8088";
                string singleInstanceMode = "Kích hoạt (Ngăn chặn chạy song song)";
                string configEncryption = "DPAPI Device Encryption";
                try
                {
                    var addrSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Network_MulticastAddress");
                    if (addrSetting != null && !string.IsNullOrEmpty(addrSetting.Value))
                        multicastAddr = addrSetting.Value;
                    var portSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Network_MulticastPort");
                    if (portSetting != null && !string.IsNullOrEmpty(portSetting.Value))
                        multicastPort = portSetting.Value;
                    var simSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_SingleInstanceMode");
                    if (simSetting != null && !string.IsNullOrEmpty(simSetting.Value))
                        singleInstanceMode = simSetting.Value;
                    var encSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_ConfigEncryption");
                    if (encSetting != null && !string.IsNullOrEmpty(encSetting.Value))
                        configEncryption = encSetting.Value;
                }
                catch (Exception ex) { Log.Warning(ex, "Failed to read system settings for diagnostics"); }
                lblMulticastStatus.Text = $"{multicastAddr}:{multicastPort}";
                lblSingleInstanceMode.Text = singleInstanceMode;
                lblConfigEncryption.Text = configEncryption;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading system health diagnostics");
            }
        }

        private async void BtnOptimizeDb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                await Task.Run(() =>
                {
                    using var db = new AppDbContext();
                    var connection = db.Database.GetDbConnection();
                    bool wasOpened = false;
                    if (connection.State != System.Data.ConnectionState.Open) { connection.Open(); wasOpened = true; }
                    try
                    {
                        using (var checkpointCmd = connection.CreateCommand())
                        {
                            checkpointCmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                            checkpointCmd.ExecuteNonQuery();
                        }
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "VACUUM;";
                            command.ExecuteNonQuery();
                        }
                    }
                    finally { if (wasOpened) connection.Close(); }
                });
                LoadSystemHealthDiagnostics();
                MessageBox.Show("Đã tối ưu hóa cơ sở dữ liệu và giải phóng dung lượng trống thành công!", "Tối ưu hóa DB", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tối ưu hóa database: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void BtnTestMulticast_Click(object sender, RoutedEventArgs e)
        {
            int port = 8088;
            try
            {
                string addrStr = "239.0.0.1";
                try
                {
                    using var db = new AppDbContext();
                    var mcAddr = db.SystemSettings.FirstOrDefault(s => s.Id == "Network_MulticastAddress");
                    var mcPort = db.SystemSettings.FirstOrDefault(s => s.Id == "Network_MulticastPort");
                    if (mcAddr != null && !string.IsNullOrEmpty(mcAddr.Value)) addrStr = mcAddr.Value;
                    if (mcPort != null && !string.IsNullOrEmpty(mcPort.Value) && int.TryParse(mcPort.Value, out var p)) port = p;
                }
                catch { }
                var multicastAddr = System.Net.IPAddress.Parse(addrStr);
                using var udpClient = new System.Net.Sockets.UdpClient();
                udpClient.Client.SetSocketOption(
                    System.Net.Sockets.SocketOptionLevel.Socket,
                    System.Net.Sockets.SocketOptionName.ReuseAddress, true);
                udpClient.Client.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, port));
                udpClient.JoinMulticastGroup(multicastAddr);
                udpClient.Client.ReceiveTimeout = 1000;
                // Send a test packet to self
                byte[] testData = System.Text.Encoding.UTF8.GetBytes("MULTICAST_PING_TEST");
                await udpClient.SendAsync(testData, testData.Length, new System.Net.IPEndPoint(multicastAddr, port));
                udpClient.DropMulticastGroup(multicastAddr);
                MessageBox.Show($"✅ Kiểm tra Multicast UDP thành công!\n\n" +
                    $"• Địa chỉ: {multicastAddr}:{port}\n" +
                    $"• Socket bind: OK\n" +
                    $"• JoinMulticastGroup: OK\n" +
                    $"• Gửi gói tin test: OK ({testData.Length} bytes)",
                    "Kết quả Kiểm tra Multicast", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Kiểm tra Multicast UDP THẤT BẠI!\n\n" +
                    $"Lỗi: {ex.Message}\n\n" +
                    $"Khuyến nghị:\n" +
                    $"• Kiểm tra tường lửa Windows cho phép cổng UDP {port}\n" +
                    $"• Kiểm tra switch mạng hỗ trợ IGMP snooping\n" +
                    $"• Kiểm tra card mạng hỗ trợ multicast",
                    "Kết quả Kiểm tra Multicast", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnClearOldLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ConfirmAction("Bạn có chắc chắn muốn dọn dẹp và xóa toàn bộ các bản ghi nhật ký hệ thống cũ hơn 30 ngày?"))
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    int deleted = await Task.Run(() =>
                    {
                        using var db = new AppDbContext();
                        var cutoffDate = DateTime.Now.AddDays(-30);

                        int deleted = db.AuditLogs.Where(l => l.Timestamp < cutoffDate).ExecuteDelete();
                        deleted += db.EventLogs.Where(l => l.Timestamp < cutoffDate).ExecuteDelete();
                        deleted += db.SecurityLogs.Where(l => l.Timestamp < cutoffDate).ExecuteDelete();
                        return deleted;
                    });
                    LoadSystemHealthDiagnostics();
                    MessageBox.Show($"Đã xóa thành công {deleted} bản ghi nhật ký cũ.", "Dọn dẹp nhật ký", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi dọn dẹp nhật ký: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
    }

    public class AuditLogDisplay
    {
        public DateTime Timestamp { get; set; }
        public string Actor { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }
}
