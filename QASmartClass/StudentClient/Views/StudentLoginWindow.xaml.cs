using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using QASmartClass.LearningTools.Helpers;
using QASmartClass.StudentClient.Models;

namespace QASmartClass.StudentClient.Views
{
    public partial class StudentLoginWindow : Window
    {

        private string _profilePath => QASmartClass.Services.AppPaths.StudentProfileFile;

        public string LoggedInStudentCode { get; private set; } = "";
        public string LoggedInStudentName { get; private set; } = "";
        public string ManualTeacherIP { get; private set; } = "";

        public StudentLoginWindow()
        {
            InitializeComponent();
            
            // Handle dragging the borderless window only from the background border
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.OriginalSource == windowBorder)
                {
                    this.DragMove();
                }
            };

            var folder = QASmartClass.Services.AppPaths.SettingsDir;
            Directory.CreateDirectory(folder);
 
            Loaded += (_, _) => {
                LoadProfile();
                LoadTestAccounts();
            };

            // Nạp hình nền học sinh nếu có
            var brush = LoadFileSafeImageBrush("login_student_bg.png");
            if (brush != null)
            {
                MainGrid.Background = brush;
            }
        }

        private System.Windows.Media.ImageBrush? LoadFileSafeImageBrush(string fileName)
        {
            try
            {
                string dir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images");
                string filePath = System.IO.Path.Combine(dir, fileName);
                
                if (System.IO.File.Exists(filePath) && new System.IO.FileInfo(filePath).Length > 0)
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath);
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    
                    return new System.Windows.Media.ImageBrush(bitmap)
                    {
                        Stretch = System.Windows.Media.Stretch.UniformToFill,
                        AlignmentX = System.Windows.Media.AlignmentX.Center,
                        AlignmentY = System.Windows.Media.AlignmentY.Center
                    };
                }
            }
            catch (Exception ex)
            {
                // Sử dụng System.Diagnostics.Debug để ghi log trong StudentClient
                System.Diagnostics.Debug.WriteLine($"Không thể tải hình nền {fileName}: {ex.Message}");
            }
            return null;
        }

        private void LoadProfile()
        {
            try
            {
                if (File.Exists(_profilePath))
                {
                    var protectedBytes = File.ReadAllBytes(_profilePath);
                    var rawBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
                    var json = Encoding.UTF8.GetString(rawBytes);
                    var profile = JsonSerializer.Deserialize<StudentProfileCache>(json);
                    
                    if (profile != null)
                    {
                        txtStudentCode.Text = profile.StudentCode;
                        txtStudentName.Text = profile.StudentName;
                        txtTeacherIP.Text = profile.TeacherIP;
                        chkRemember.IsChecked = profile.RememberMe;
                    }
                }
            }
            catch { /* ignore load error */ }
        }

        private void SaveProfile()
        {
            if (chkRemember.IsChecked == true)
            {
                try
                {
                    bool isExitPinRequired = false;
                    string exitPinCode = "";
                    int networkPort = 29877;
                    if (File.Exists(_profilePath))
                    {
                        try
                        {
                            var oldProtectedBytes = File.ReadAllBytes(_profilePath);
                            var oldRawBytes = ProtectedData.Unprotect(oldProtectedBytes, null, DataProtectionScope.CurrentUser);
                            var oldJson = Encoding.UTF8.GetString(oldRawBytes);
                            var oldProfile = JsonSerializer.Deserialize<StudentProfileCache>(oldJson);
                            if (oldProfile != null)
                            {
                                isExitPinRequired = oldProfile.IsExitPinRequired;
                                exitPinCode = oldProfile.ExitPinCode;
                                networkPort = oldProfile.NetworkPort;
                            }
                        }
                        catch { /* ignore */ }
                    }

                    var profile = new StudentProfileCache
                    {
                        StudentCode = PathHelper.SanitizeFileNameInput(txtStudentCode.Text, 20),
                        StudentName = PathHelper.SanitizeInput(txtStudentName.Text, 50),
                        TeacherIP = txtTeacherIP.Text.Trim(),
                        RememberMe = true,
                        IsExitPinRequired = isExitPinRequired,
                        ExitPinCode = exitPinCode,
                        NetworkPort = networkPort
                    };
                    var json = JsonSerializer.Serialize(profile);
                    var rawBytes = Encoding.UTF8.GetBytes(json);
                    var protectedBytes = ProtectedData.Protect(rawBytes, null, DataProtectionScope.CurrentUser);
                    // Ghi tệp profile cá nhân
                    QASmartClass.Services.AppPaths.CurrentStudentCode = profile.StudentCode;
                    string personalPath = QASmartClass.Services.AppPaths.StudentProfileFile;
                    File.WriteAllBytes(personalPath, protectedBytes);

                    // Ghi tệp profile chung của thiết bị
                    QASmartClass.Services.AppPaths.CurrentStudentCode = "";
                    string devicePath = QASmartClass.Services.AppPaths.StudentProfileFile;
                    File.WriteAllBytes(devicePath, protectedBytes);

                    // Khôi phục lại mã học sinh hiện tại của phiên
                    QASmartClass.Services.AppPaths.CurrentStudentCode = profile.StudentCode;
                }
                catch { /* ignore save error */ }
            }
            else
            {
                try
                {
                    // Xóa tệp profile cá nhân
                    var code = PathHelper.SanitizeFileNameInput(txtStudentCode.Text, 20);
                    if (!string.IsNullOrEmpty(code))
                    {
                        QASmartClass.Services.AppPaths.CurrentStudentCode = code;
                        string personalPath = QASmartClass.Services.AppPaths.StudentProfileFile;
                        if (File.Exists(personalPath))
                            File.Delete(personalPath);
                    }
                }
                catch { /* ignore delete error */ }
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            txtError.Visibility = Visibility.Collapsed;

            var code = PathHelper.SanitizeFileNameInput(txtStudentCode.Text, 20);
            var name = PathHelper.SanitizeInput(txtStudentName.Text, 50);

            if (string.IsNullOrEmpty(code))
            {
                txtError.Text = "Mã học sinh không được để trống!";
                txtError.Visibility = Visibility.Visible;
                txtStudentCode.Focus();
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                name = "Học sinh " + code;
            }

            LoggedInStudentCode = code;
            LoggedInStudentName = name;
            ManualTeacherIP = txtTeacherIP.Text.Trim();

            // Sync sanitized values back to UI so they are saved correctly in profile cache
            txtStudentCode.Text = code;
            txtStudentName.Text = name;

            QASmartClass.Services.AppPaths.CurrentStudentCode = code;
            SaveProfile();

            // Save last selected test user if in test mode
            if (QASmartTouch.Services.AppSettings.RunningMode == "Test" && cboTestAccounts.SelectedItem is QASmartClass.Data.Student selectedStudent)
            {
                QASmartTouch.Services.AppSettings.LastTestUserStudent = selectedStudent.StudentCode;
                QASmartTouch.Services.AppSettings.Save();
            }

            this.DialogResult = true;
            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void LoadTestAccounts()
        {
            if (QASmartTouch.Services.AppSettings.RunningMode != "Test")
            {
                pnlTestAccounts.Visibility = Visibility.Collapsed;
                return;
            }

            pnlTestAccounts.Visibility = Visibility.Visible;
            List<QASmartClass.Data.Student> students = new List<QASmartClass.Data.Student>();

            try
            {
                using (var db = new QASmartClass.Data.AppDbContext())
                {
                    students = db.Students.Take(40).ToList();
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading test students: {Err}", ex.Message);
            }

            // Fallback
            if (students == null || students.Count == 0)
            {
                students = new List<QASmartClass.Data.Student>
                {
                    new QASmartClass.Data.Student { StudentCode = "HS001", FullName = "Nguyễn Văn An" },
                    new QASmartClass.Data.Student { StudentCode = "HS002", FullName = "Trần Thị Bích" }
                };
            }

            cboTestAccounts.ItemsSource = students;
            cboTestAccounts.DisplayMemberPath = "FullName";

            // Select last test user
            string lastUser = QASmartTouch.Services.AppSettings.LastTestUserStudent;
            if (!string.IsNullOrEmpty(lastUser))
            {
                var match = students.Find(s => s.StudentCode == lastUser);
                if (match != null)
                {
                    cboTestAccounts.SelectedItem = match;
                }
                else
                {
                    cboTestAccounts.SelectedIndex = 0;
                }
            }
            else
            {
                cboTestAccounts.SelectedIndex = 0;
            }
        }

        private void cboTestAccounts_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cboTestAccounts.SelectedItem is QASmartClass.Data.Student selected)
            {
                txtStudentCode.Text = selected.StudentCode;
                txtStudentName.Text = selected.FullName;
            }
        }

        private void txtStudentCode_LostFocus(object sender, RoutedEventArgs e)
        {
            var code = PathHelper.SanitizeFileNameInput(txtStudentCode.Text);
            if (string.IsNullOrEmpty(code)) return;

            try
            {
                // Tạm thời gán CurrentStudentCode để lấy đường dẫn profile riêng
                QASmartClass.Services.AppPaths.CurrentStudentCode = code;
                var personalPath = QASmartClass.Services.AppPaths.StudentProfileFile;
                if (File.Exists(personalPath))
                {
                    var protectedBytes = File.ReadAllBytes(personalPath);
                    var rawBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
                    var json = Encoding.UTF8.GetString(rawBytes);
                    var profile = JsonSerializer.Deserialize<StudentProfileCache>(json);
                    if (profile != null)
                    {
                        txtStudentName.Text = profile.StudentName;
                        txtTeacherIP.Text = profile.TeacherIP;
                        chkRemember.IsChecked = profile.RememberMe;
                        return;
                    }
                }

                // Nếu không có profile cá nhân, fallback về profile chung (bằng cách xóa CurrentStudentCode)
                QASmartClass.Services.AppPaths.CurrentStudentCode = "";
                var devicePath = QASmartClass.Services.AppPaths.StudentProfileFile;
                if (File.Exists(devicePath))
                {
                    var protectedBytes = File.ReadAllBytes(devicePath);
                    var rawBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
                    var json = Encoding.UTF8.GetString(rawBytes);
                    var profile = JsonSerializer.Deserialize<StudentProfileCache>(json);
                    if (profile != null)
                    {
                        txtTeacherIP.Text = profile.TeacherIP;
                        chkRemember.IsChecked = profile.RememberMe;
                    }
                }
            }
            catch { /* ignore */ }
            finally
            {
                // Khôi phục gán code cho đúng phiên làm việc
                QASmartClass.Services.AppPaths.CurrentStudentCode = code;
            }
        }
    }
}
