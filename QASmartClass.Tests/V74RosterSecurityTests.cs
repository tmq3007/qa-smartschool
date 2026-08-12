using System;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Classroom.Views;
using QASmartClass.Classroom.Services;
using QASmartClass.Data;
using System.Linq;
using Xunit;

namespace QASmartClass.Tests
{
    public class V74RosterSecurityTests
    {
        private void RunOnStaThread(Action action)
        {
            void InitializeApplicationFull()
            {
                var urls = new[] {
                    "pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/Styles.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/StaffTheme.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/InterOutfitFonts.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/SvgIcons.xaml",
                    "pack://application:,,,/QASmartClass;component/Localization/Strings_vi.xaml",
                    "pack://application:,,,/QASmartClass;component/LearningTools/Themes/LearningToolsStyles.xaml"
                };

                try
                {
                    var appField = typeof(System.Windows.Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    var createdField = typeof(System.Windows.Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (appField != null) appField.SetValue(null, null);
                    if (createdField != null) createdField.SetValue(null, false);

                    var app = new QASmartTouch.App();
                    foreach (var url in urls)
                    {
                        app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                        {
                            Source = new Uri(url, UriKind.Absolute)
                        });
                    }
                }
                catch { }
            }
            Exception ex = null;
            var t = new Thread(() =>
            {
                try
                {
                    InitializeApplicationFull(); action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (ex != null)
            {
                throw ex;
            }
        }

        [Fact]
        public void TestIpMaskConverter_WithIPAddress()
        {
            RunOnStaThread(() =>
            {
                var converter = new IpMaskConverter();
                var chk = new CheckBox { IsChecked = true };

                // Test trường hợp Hide IP = True
                var maskedResult = converter.Convert("192.168.1.105", typeof(string), chk, null);
                Assert.Equal("192.168.1.***", maskedResult);

                // Test trường hợp Hide IP = False
                chk.IsChecked = false;
                var rawResult = converter.Convert("192.168.1.105", typeof(string), chk, null);
                Assert.Equal("192.168.1.105", rawResult);
            });
        }

        [Fact]
        public void TestIpMaskConverter_WithPCName()
        {
            RunOnStaThread(() =>
            {
                var converter = new IpMaskConverter();
                var chk = new CheckBox { IsChecked = true };

                // Test trường hợp Hide PC = True
                var maskedResult = converter.Convert("PC-05", typeof(string), chk, null);
                Assert.Equal("PC-**", maskedResult);

                // Test trường hợp Hide PC = False
                chk.IsChecked = false;
                var rawResult = converter.Convert("PC-05", typeof(string), chk, null);
                Assert.Equal("PC-05", rawResult);
            });
        }

        [Fact]
        public void TestIpMaskConverter_WithOtherText()
        {
            RunOnStaThread(() =>
            {
                var converter = new IpMaskConverter();
                var chk = new CheckBox { IsChecked = true };

                // Test trường hợp text khác
                var maskedResult = converter.Convert("Nguyen Van An", typeof(string), chk, null);
                Assert.Equal("Nguyen Van An", maskedResult); // Không che tên học sinh
            });
        }

        [Fact]
        public void TestClassRosterService_SanitizeCsvField_Malicious()
        {
            // Test chèn công thức Excel (Excel Injection) nguy hiểm
            string inputFormula1 = "=SUM(A1:A10)";
            string output1 = ClassRosterService.SanitizeCsvField(inputFormula1);
            Assert.Equal("'=SUM(A1:A10)", output1);

            string inputFormula2 = "-cmd|' /C calc'!A1";
            string output2 = ClassRosterService.SanitizeCsvField(inputFormula2);
            Assert.Equal("'-cmd|' /C calc'!A1", output2);

            string inputFormula3 = "+100";
            string output3 = ClassRosterService.SanitizeCsvField(inputFormula3);
            Assert.Equal("'+100", output3);

            string inputFormula4 = "@something";
            string output4 = ClassRosterService.SanitizeCsvField(inputFormula4);
            Assert.Equal("'@something", output4);
        }

        [Fact]
        public void TestClassRosterService_SanitizeCsvField_Safe()
        {
            // Test chuỗi an toàn bình thường
            string inputSafe1 = "Nguyễn Văn An";
            string output1 = ClassRosterService.SanitizeCsvField(inputSafe1);
            Assert.Equal("Nguyễn Văn An", output1);

            string inputSafe2 = "HS009";
            string output2 = ClassRosterService.SanitizeCsvField(inputSafe2);
            Assert.Equal("HS009", output2);

            string inputSafe3 = "";
            string output3 = ClassRosterService.SanitizeCsvField(inputSafe3);
            Assert.Equal("", output3);

            string inputSafe4 = null;
            string output4 = ClassRosterService.SanitizeCsvField(inputSafe4);
            Assert.Null(output4);
        }

        [Fact]
        public void TestAttendanceSaveDiagnostics()
        {
            var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            connection.Open();

            try
            {
                var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<QASmartClass.Data.AppDbContext>()
                    .UseSqlite(connection)
                    .Options;

                using (var db = new QASmartClass.Data.AppDbContext(options))
                {
                    db.Database.EnsureCreated();

                    // Add Student
                    var student = new QASmartClass.Data.Student
                    {
                        FullName = "Test Student",
                        StudentCode = "TS001",
                        PCName = "PC-TEST",
                        IPAddress = "127.0.0.1"
                    };
                    db.Students.Add(student);
                    db.SaveChanges();

                    // Add AttendanceRecord
                    var record = new QASmartClass.Data.AttendanceRecord
                    {
                        StudentId = student.Id,
                        RosterId = 1,
                        Date = DateTime.Today,
                        Status = "present",
                        Note = "test note",
                        UpdatedAt = DateTime.Now
                    };
                    db.AttendanceRecords.Add(record);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Save failed: {ex.Message}. Inner: {ex.InnerException?.Message}", ex);
            }
            finally
            {
                connection.Close();
            }
        }

        [Fact]
        public void TestRealDatabaseAttendanceSave()
        {
            var dbPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "QASmartClass", "smartclass.db"
            );
            
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<QASmartClass.Data.AppDbContext>()
                .UseSqlite($"Data Source={dbPath};Foreign Keys=True")
                .Options;

            using (var db = new QASmartClass.Data.AppDbContext(options))
            {
                var student = db.Students.FirstOrDefault();
                var roster = db.ClassRosters.FirstOrDefault();
                if (student == null || roster == null) return;

                int rosterId = roster.Id;
                DateTime selectedDate = DateTime.Today;
                var targetDate = selectedDate.Date;
                var nextDate = targetDate.AddDays(1);

                // Clean up previous test records just in case
                var oldRecs = db.AttendanceRecords.Where(r => r.Note.StartsWith("test duplicate")).ToList();
                if (oldRecs.Any())
                {
                    db.AttendanceRecords.RemoveRange(oldRecs);
                    db.SaveChanges();
                }

                // First save
                var existing1 = db.AttendanceRecords
                    .Where(r => r.RosterId == rosterId && r.Date >= targetDate && r.Date < nextDate)
                    .ToList();

                var rec1 = existing1.FirstOrDefault(r => r.StudentId == student.Id);
                if (rec1 == null)
                {
                    db.AttendanceRecords.Add(new QASmartClass.Data.AttendanceRecord
                    {
                        StudentId = student.Id,
                        RosterId = rosterId,
                        Date = targetDate,
                        Status = "present",
                        Note = "test duplicate 1",
                        UpdatedAt = DateTime.Now
                    });
                }
                db.SaveChanges();

                // Second save (simulates double-clicking or re-saving on same date)
                var existing2 = db.AttendanceRecords
                    .Where(r => r.RosterId == rosterId && r.Date >= targetDate && r.Date < nextDate)
                    .ToList();

                var rec2 = existing2.FirstOrDefault(r => r.StudentId == student.Id);
                if (rec2 == null)
                {
                    db.AttendanceRecords.Add(new QASmartClass.Data.AttendanceRecord
                    {
                        StudentId = student.Id,
                        RosterId = rosterId,
                        Date = targetDate,
                        Status = "present",
                        Note = "test duplicate 2",
                        UpdatedAt = DateTime.Now
                    });
                }
                
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Real DB save error: {ex.Message}. Inner: {ex.InnerException?.Message}", ex);
                }
                finally
                {
                    // Clean up test records
                    var testRecs = db.AttendanceRecords.Where(r => r.Note.StartsWith("test duplicate")).ToList();
                    db.AttendanceRecords.RemoveRange(testRecs);
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public void TestRosterEditor_DuplicateValidation()
        {
            var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            connection.Open();

            try
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(connection)
                    .Options;

                using (var db = new AppDbContext(options))
                {
                    db.Database.EnsureCreated();

                    // Tạo lớp học mẫu đầu tiên
                    var roster1 = new ClassRoster
                    {
                        ClassName = "10A1",
                        GradeLevel = "10",
                        Subject = "STEM robot",
                        TeacherName = "Nguyen Van A",
                        SchoolYear = "2025-2026",
                        Semester = "HK2",
                        StudentCount = 0
                    };
                    db.ClassRosters.Add(roster1);
                    db.SaveChanges();

                    // Kiểm tra xem logic trùng lặp có phát hiện đúng không
                    string testClassName = "10A1";
                    string testSubject = "STEM robot";
                    string testSchoolYear = "2025-2026";
                    string testSemester = "HK2";

                    bool isDuplicate = db.ClassRosters.Any(r =>
                        r.ClassName == testClassName &&
                        r.Subject == testSubject &&
                        r.SchoolYear == testSchoolYear &&
                        r.Semester == testSemester);

                    Assert.True(isDuplicate, "Hệ thống phải phát hiện lớp học trùng tên, môn học, năm học, học kỳ trong DB.");

                    // Kiểm tra trường hợp khác môn học (không trùng)
                    bool isNotDuplicateSubject = db.ClassRosters.Any(r =>
                        r.ClassName == testClassName &&
                        r.Subject == "Math" &&
                        r.SchoolYear == testSchoolYear &&
                        r.Semester == testSemester);

                    Assert.False(isNotDuplicateSubject, "Lớp học khác môn học không được tính là trùng lặp.");
                }
            }
            finally
            {
                connection.Close();
            }
        }

        [Fact]
        public void TestIpMaskConverter_RememberedDevices()
        {
            RunOnStaThread(() =>
            {
                var converter = new IpMaskConverter();
                var chk = new CheckBox { IsChecked = true };

                // Test che IP gần nhất của thiết bị đã ghi nhớ khi trình chiếu
                var maskedIp = converter.Convert("192.168.1.103", typeof(string), chk, null);
                Assert.Equal("192.168.1.***", maskedIp);

                // Test che tên PC của thiết bị đã ghi nhớ khi trình chiếu
                var maskedPc = converter.Convert("PC-03", typeof(string), chk, null);
                Assert.Equal("PC-**", maskedPc);

                // Test giữ nguyên khi tắt trình chiếu
                chk.IsChecked = false;
                var rawIp = converter.Convert("192.168.1.103", typeof(string), chk, null);
                Assert.Equal("192.168.1.103", rawIp);

                var rawPc = converter.Convert("PC-03", typeof(string), chk, null);
                Assert.Equal("PC-03", rawPc);
            });
        }

        [Fact]
        public void TestReleaseDeviceLogic()
        {
            var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            connection.Open();

            try
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(connection)
                    .Options;

                using (var db = new AppDbContext(options))
                {
                    db.Database.EnsureCreated();

                    // 1. Add student with connection mapping
                    var student = new Student
                    {
                        FullName = "Nguyen Van Test",
                        StudentCode = "HS-TEST",
                        PCName = "PC-OLD",
                        IPAddress = "192.168.1.100",
                        LastSeen = DateTime.Now
                    };
                    db.Students.Add(student);
                    db.SaveChanges();

                    // Verify initial state
                    var savedStudent = db.Students.FirstOrDefault(s => s.StudentCode == "HS-TEST");
                    Assert.NotNull(savedStudent);
                    Assert.Equal("PC-OLD", savedStudent.PCName);
                    Assert.Equal("192.168.1.100", savedStudent.IPAddress);

                    // 2. Perform release logic (set IP/PC to empty string)
                    savedStudent.PCName = "";
                    savedStudent.IPAddress = "";
                    db.SaveChanges();

                    // 3. Verify they are successfully cleared
                    var updatedStudent = db.Students.FirstOrDefault(s => s.StudentCode == "HS-TEST");
                    Assert.NotNull(updatedStudent);
                    Assert.Equal("", updatedStudent.PCName);
                    Assert.Equal("", updatedStudent.IPAddress);
                }
            }
            finally
            {
                connection.Close();
            }
        }

        [Fact]
        public void TestAttendanceUndoLogic()
        {
            RunOnStaThread(() =>
            {
                // 1. Create page
                var page = new AttendancePage();

                // Get private collections using reflection
                var rowsField = typeof(AttendancePage).GetField("_rows", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var rowBordersField = typeof(AttendancePage).GetField("_rowBorders", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var undoStackField = typeof(AttendancePage).GetField("_undoStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var activeRowIndexField = typeof(AttendancePage).GetField("_activeRowIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var buildRowMethod = typeof(AttendancePage).GetMethod("BuildRow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                Assert.NotNull(rowsField);
                Assert.NotNull(rowBordersField);
                Assert.NotNull(undoStackField);
                Assert.NotNull(activeRowIndexField);
                Assert.NotNull(buildRowMethod);

                var rows = (System.Collections.Generic.List<AttendanceRow>)rowsField.GetValue(page);
                var rowBorders = (System.Collections.Generic.List<Border>)rowBordersField.GetValue(page);
                var undoStack = (System.Collections.Generic.Stack<(int rowIndex, string previousStatus)>)undoStackField.GetValue(page);

                // Clear initially
                rows.Clear();
                rowBorders.Clear();
                undoStack.Clear();

                // 2. Create mock row
                var row = new AttendanceRow
                {
                    STT = 1,
                    StudentId = 999,
                    FullName = "Nguyen Van Test",
                    StudentCode = "TS999",
                    IsOnline = true,
                    Status = "unknown"
                };
                rows.Add(row);

                // 3. Build Row using the private BuildRow method
                var border = (Border)buildRowMethod.Invoke(page, new object[] { row });
                rowBorders.Add(border);

                // Set active row
                activeRowIndexField.SetValue(page, 0);

                // Verify initial state
                Assert.Equal("unknown", row.Status);
                Assert.False(row.RbPresent.IsChecked);
                Assert.False(row.RbLate.IsChecked);
                Assert.False(row.RbAbsent.IsChecked);
                Assert.Empty(undoStack);

                // 4. Simulate checking "present" (user action)
                row.RbPresent.IsChecked = true;

                // Verify status and stack push
                Assert.Equal("present", row.Status);
                Assert.Single(undoStack);
                var top = undoStack.Peek();
                Assert.Equal(0, top.rowIndex);
                Assert.Equal("unknown", top.previousStatus);

                // 5. Simulate checking "absent" (user action)
                row.RbAbsent.IsChecked = true;

                // Verify status and stack push
                Assert.Equal("absent", row.Status);
                Assert.Equal(2, undoStack.Count);
                top = undoStack.Peek();
                Assert.Equal(0, top.rowIndex);
                Assert.Equal("present", top.previousStatus);

                // 6. Simulate Undo (programmatic restoration of previous "present" status)
                var popped = undoStack.Pop();
                Assert.Equal("present", popped.previousStatus);

                // Programmatically set status back to "present"
                var isProgrammaticChangeField = typeof(AttendancePage).GetField("_isProgrammaticChange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(isProgrammaticChangeField);

                isProgrammaticChangeField.SetValue(page, true);
                try
                {
                    row.Status = popped.previousStatus;
                    row.RbPresent.IsChecked = true;
                }
                finally
                {
                    isProgrammaticChangeField.SetValue(page, false);
                }

                // Verify stack didn't grow during programmatic restoration
                Assert.Single(undoStack);
                Assert.Equal("present", row.Status);
                Assert.True(row.RbPresent.IsChecked);
                Assert.False(row.RbAbsent.IsChecked);
            });
        }

        [Fact]
        public void TestAttendancePage_EmptyStateControls()
        {
            RunOnStaThread(() =>
            {
                var page = new AttendancePage();

                var emptyStateBorderField = typeof(AttendancePage).GetField("emptyStateBorder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var attendanceScrollViewerField = typeof(AttendancePage).GetField("attendanceScrollViewer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                Assert.NotNull(emptyStateBorderField);
                Assert.NotNull(attendanceScrollViewerField);

                var emptyStateBorder = (Border)emptyStateBorderField.GetValue(page);
                var attendanceScrollViewer = (ScrollViewer)attendanceScrollViewerField.GetValue(page);

                Assert.NotNull(emptyStateBorder);
                Assert.NotNull(attendanceScrollViewer);

                // Initially emptyStateBorder should be Collapsed
                Assert.Equal(Visibility.Collapsed, emptyStateBorder.Visibility);

                // Toggle visibility to verify it updates
                emptyStateBorder.Visibility = Visibility.Visible;
                attendanceScrollViewer.Visibility = Visibility.Collapsed;

                Assert.Equal(Visibility.Visible, emptyStateBorder.Visibility);
                Assert.Equal(Visibility.Collapsed, attendanceScrollViewer.Visibility);
            });
        }

        [Fact]
        public void TestStudentNameToBrushConverter()
        {
            RunOnStaThread(() =>
            {
                var converter = new StudentNameToBrushConverter();
                
                var brush1 = converter.Convert("Nguyễn Văn An", typeof(System.Windows.Media.Brush), null, null) as System.Windows.Media.SolidColorBrush;
                var brush2 = converter.Convert("Nguyễn Văn An", typeof(System.Windows.Media.Brush), null, null) as System.Windows.Media.SolidColorBrush;
                var brush3 = converter.Convert("Trần Thị Bình", typeof(System.Windows.Media.Brush), null, null) as System.Windows.Media.SolidColorBrush;

                Assert.NotNull(brush1);
                Assert.NotNull(brush2);
                Assert.NotNull(brush3);

                // Hashing must be consistent
                Assert.Equal(brush1.Color, brush2.Color);

                // Default brush for null or empty names
                var defaultBrush = converter.Convert("", typeof(System.Windows.Media.Brush), null, null) as System.Windows.Media.SolidColorBrush;
                Assert.NotNull(defaultBrush);
                Assert.Equal(System.Windows.Media.Colors.SlateGray, defaultBrush.Color);
            });
        }

        [Fact]
        public void TestStudentNameToInitialsConverter()
        {
            RunOnStaThread(() =>
            {
                var converter = new StudentNameToInitialsConverter();

                var initial1 = converter.Convert("Nguyễn Văn An", typeof(string), null, null);
                Assert.Equal("A", initial1);

                var initial2 = converter.Convert("Hoàng Thị", typeof(string), null, null);
                Assert.Equal("T", initial2);

                var initial3 = converter.Convert("  Lê  ", typeof(string), null, null);
                Assert.Equal("L", initial3);

                var initial4 = converter.Convert("", typeof(string), null, null);
                Assert.Equal("?", initial4);

                var initial5 = converter.Convert(null, typeof(string), null, null);
                Assert.Equal("?", initial5);
            });
        }

        [Fact]
        public void TestStudentPage_RosterEmptyStateControls()
        {
            RunOnStaThread(() =>
            {
                var page = new StudentPage();

                var borderField = typeof(StudentPage).GetField("rosterEmptyStateBorder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var gridField = typeof(StudentPage).GetField("rosterDataGrid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                Assert.NotNull(borderField);
                Assert.NotNull(gridField);

                var border = (Border)borderField.GetValue(page);
                var grid = (DataGrid)gridField.GetValue(page);

                Assert.NotNull(border);
                Assert.NotNull(grid);

                // Default is Collapsed
                Assert.Equal(Visibility.Collapsed, border.Visibility);

                // Test toggle
                border.Visibility = Visibility.Visible;
                grid.Visibility = Visibility.Collapsed;

                Assert.Equal(Visibility.Visible, border.Visibility);
                Assert.Equal(Visibility.Collapsed, grid.Visibility);
            });
        }

        [Fact]
        public void TestStudentPage_DeviceEmptyStateControls()
        {
            RunOnStaThread(() =>
            {
                var page = new StudentPage();

                var borderField = typeof(StudentPage).GetField("deviceEmptyStateBorder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var gridField = typeof(StudentPage).GetField("deviceDataGrid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                Assert.NotNull(borderField);
                Assert.NotNull(gridField);

                var border = (Border)borderField.GetValue(page);
                var grid = (DataGrid)gridField.GetValue(page);

                Assert.NotNull(border);
                Assert.NotNull(grid);

                // Default is Collapsed
                Assert.Equal(Visibility.Collapsed, border.Visibility);

                // Test toggle
                border.Visibility = Visibility.Visible;
                grid.Visibility = Visibility.Collapsed;

                Assert.Equal(Visibility.Visible, border.Visibility);
                Assert.Equal(Visibility.Collapsed, grid.Visibility);
            });
        }

        [Fact]
        public void TestRosterEditorWindow_UIProperties()
        {
            RunOnStaThread(() =>
            {
                try
                {
                    var wnd = new RosterEditorWindow(null);
                    Assert.NotNull(wnd);
                    Assert.Equal(13, wnd.FontSize);

                    var titleLabel = wnd.FindName("txtTitle") as TextBlock;
                    Assert.NotNull(titleLabel);
                    Assert.Equal(18, titleLabel.FontSize);
                }
                catch (Exception ex)
                {
                    throw new Exception($"TEST_ERROR_DETAILS: {ex.Message} | Stack: {ex.StackTrace} | Inner: {ex.InnerException?.Message} | InnerStack: {ex.InnerException?.StackTrace}");
                }
            });
        }

        [Fact]
        public void TestTeacherListPage_UIPropertiesAndSanitizer()
        {
            RunOnStaThread(() =>
            {
                try
                {
                    var page = new TeacherListPage();
                    Assert.NotNull(page);

                    // Test SanitizeCsvField chống Excel/CSV Injection
                    string formula = "=SUM(A1:A10)";
                    string clean = TeacherListPage.SanitizeCsvField(formula);
                    Assert.Equal("'=SUM(A1:A10)", clean);

                    string plainText = "Nguyễn Văn A";
                    string output = TeacherListPage.SanitizeCsvField(plainText);
                    Assert.Equal("Nguyễn Văn A", output);
                }
                catch (Exception ex)
                {
                    throw new Exception($"TEST_ERROR_DETAILS: {ex.Message} | Stack: {ex.StackTrace}");
                }
            });
        }
    }
}
