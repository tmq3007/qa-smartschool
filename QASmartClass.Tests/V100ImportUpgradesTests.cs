using System;
using System.IO;
using System.Linq;
using QASmartClass.Classroom.Services;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartTouch.Services;
using Xunit;
using Classroom = QASmartClass.Data.Classroom;

namespace QASmartClass.Tests
{
    public class V100ImportUpgradesTests : IDisposable
    {
        private readonly string _tempDbPath;
        private readonly string _tempVersionPath;

        public V100ImportUpgradesTests()
        {
            if (System.Windows.Application.Current == null)
            {
                try
                {
                    new System.Windows.Application();
                }
                catch { }
            }

            string guid = Guid.NewGuid().ToString("N");
            _tempDbPath = Path.Combine(AppPaths.RootDir, $"smartclass_test_{guid}.db");
            _tempVersionPath = Path.Combine(AppPaths.RootDir, $"db_version_{guid}.txt");

            AppPaths.DatabaseFile = _tempDbPath;
            AppPaths.DbVersionFile = _tempVersionPath;
            AppPaths.EnsureDirectories();

            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.36.0");
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_tempDbPath)) File.Delete(_tempDbPath);
                if (File.Exists(_tempVersionPath)) File.Delete(_tempVersionPath);
            }
            catch { }
        }

        [Fact]
        public void HMACSHA512_Hashing_ShouldVerifyCorrectly()
        {
            // Arrange
            string password = "TestStudent@123";

            // Act
            string hashStr = AuthenticationService.HashPasswordHMACSHA512(password);
            
            // Assert
            Assert.Contains(":", hashStr);
            var parts = hashStr.Split(':');
            Assert.Equal(2, parts.Length);

            // Verify
            bool verified = AuthenticationService.VerifyPassword(password, hashStr);
            Assert.True(verified);

            bool wrongVerified = AuthenticationService.VerifyPassword("wrong_password", hashStr);
            Assert.False(wrongVerified);
        }

        [Theory]
        [InlineData("1,Nguyễn Văn An,HS001,Trưởng nhóm", new[] { "1", "Nguyễn Văn An", "HS001", "Trưởng nhóm" })]
        [InlineData("1,\"Nguyễn Văn, An\",HS001,\"Ghi chú, tổ 1\"", new[] { "1", "Nguyễn Văn, An", "HS001", "Ghi chú, tổ 1" })]
        [InlineData("1,\"Nguyễn \"\"Văn\"\" An\",HS001,Ghi chú", new[] { "1", "Nguyễn \"Văn\" An", "HS001", "Ghi chú" })]
        public void ParseCsvLine_RFC4180_ShouldParseCorrectly(string line, string[] expected)
        {
            // Act
            var result = ClassRosterService.ParseCsvLine(line);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ClassRosterService_AddStudent_ShouldEnforceCapacityLimit()
        {
            using (var db = new AppDbContext())
            {
                // Arrange
                // Create Classroom with capacity = 2
                var classroom = new QASmartClass.Data.Classroom
                {
                    Name = "10A1",
                    TeacherName = "Phan Văn Trị",
                    ClassCode = "10A1_CODE",
                    MaxStudents = 2,
                    CreatedAt = DateTime.Now
                };
                db.Classrooms.Add(classroom);

                // Create ClassRoster
                var service = new ClassRosterService(db);
                var roster = service.CreateRoster("10A1", "Lớp 10", "Toán", "Phan Văn Trị");

                // Create Students
                var s1 = new Student { FullName = "Học sinh 1", StudentCode = "HS101", ClassroomId = classroom.Id };
                var s2 = new Student { FullName = "Học sinh 2", StudentCode = "HS102", ClassroomId = classroom.Id };
                var s3 = new Student { FullName = "Học sinh 3", StudentCode = "HS103", ClassroomId = classroom.Id };
                db.Students.AddRange(s1, s2, s3);
                db.SaveChanges();

                // Act & Assert
                // Add student 1 and 2
                bool add1 = service.AddStudentToRoster(roster.Id, s1.Id, 1);
                bool add2 = service.AddStudentToRoster(roster.Id, s2.Id, 2);

                Assert.True(add1);
                Assert.True(add2);

                // Adding student 3 should throw InvalidOperationException due to capacity = 2
                Assert.Throws<InvalidOperationException>(() =>
                {
                    service.AddStudentToRoster(roster.Id, s3.Id, 3);
                });
            }
        }

        [Fact]
        public void TestTimetableExportImportFlow_Works()
        {
            // Simulate the exact export data format
            var sb = new System.Text.StringBuilder();
            sb.Append("\uFEFF");
            sb.AppendLine("Thu,Tiet,MonHoc,Phong,GiaoVien,GhiChu");
            sb.AppendLine("\"Thứ 2\",\"Tiết 1\",\"Toán\",\"P.10A\",\"GV\",\"\"");
            sb.AppendLine("\"Thứ 3\",\"Tiết 2\",\"Văn\",\"P.10B\",\"GV2\",\"\"");

            string tempFile = Path.Combine(AppPaths.RootDir, "temp_tkb_test.csv");
            try
            {
                File.WriteAllText(tempFile, sb.ToString(), System.Text.Encoding.UTF8);

                // Run the parser logic
                var table = new System.Data.DataTable();
                table.Columns.Add("Thứ");
                table.Columns.Add("Tiết");
                table.Columns.Add("Môn");
                table.Columns.Add("Phòng");
                table.Columns.Add("Giáo viên");
                table.Columns.Add("Ghi chú");

                var lines = File.ReadAllLines(tempFile, System.Text.Encoding.UTF8);
                Assert.True(lines.Length > 1);

                foreach (var line in lines.Skip(1))
                {
                    var cleanLine = line.Trim();
                    if (string.IsNullOrEmpty(cleanLine)) continue;

                    var cols = ClassRosterService.ParseCsvLine(cleanLine);
                    var row = table.NewRow();
                    for (int col = 0; col < Math.Min(cols.Length, 6); col++)
                    {
                        row[col] = cols[col];
                    }
                    table.Rows.Add(row);
                }

                Assert.Equal(2, table.Rows.Count);
                Assert.Equal("Thứ 2", table.Rows[0][0].ToString());
                Assert.Equal("Tiết 1", table.Rows[0][1].ToString());
                Assert.Equal("Toán", table.Rows[0][2].ToString());

                // Parse slots
                string[] DayNames = new[] { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7" };
                string[] PeriodLabels = new[] { "Tiết 1", "Tiết 2", "Tiết 3", "Tiết 4", "Tiết 5", "Tiết 6", "Tiết 7", "Tiết 8", "Tiết 9", "Tiết 10" };

                int validSlots = 0;
                foreach (System.Data.DataRow row in table.Rows)
                {
                    string thu = row[0]?.ToString()?.Trim() ?? "";
                    string tiet = row[1]?.ToString()?.Trim() ?? "";
                    string mon = row[2]?.ToString()?.Trim() ?? "";

                    int dayIdx = Array.IndexOf(DayNames, thu);
                    int periodIdx = Array.IndexOf(PeriodLabels, tiet);

                    if (dayIdx >= 0 && periodIdx >= 0 && !string.IsNullOrEmpty(mon))
                    {
                        validSlots++;
                    }
                }
                Assert.Equal(2, validSlots);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseCsvLine_WithSemicolonDelimiter_ShouldParseCorrectly()
        {
            // Arrange
            string line = "1;Nguyễn Văn An;HS001;Trưởng nhóm";
            string[] expected = new[] { "1", "Nguyễn Văn An", "HS001", "Trưởng nhóm" };

            // Act
            var result = ClassRosterService.ParseCsvLine(line, ';');

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestTimetableImport_DelimiterAutoDetection_Works()
        {
            // Case 1: Comma delimited CSV
            string csvComma = "Thu,Tiet,MonHoc,Phong,GiaoVien,GhiChu\n\"Thứ 2\",\"Tiết 1\",\"Toán\",\"P.10A\",\"GV\",\"\"";
            string tempCommaFile = Path.Combine(AppPaths.RootDir, "temp_tkb_comma.csv");

            // Case 2: Semicolon delimited CSV (Excel Vietnamese regional settings)
            string csvSemicolon = "Thu;Tiet;MonHoc;Phong;GiaoVien;GhiChu\n\"Thứ 2\";\"Tiết 1\";\"Toán\";\"P.10A\";\"GV\";\"\"";
            string tempSemicolonFile = Path.Combine(AppPaths.RootDir, "temp_tkb_semicolon.csv");

            try
            {
                File.WriteAllText(tempCommaFile, csvComma, System.Text.Encoding.UTF8);
                File.WriteAllText(tempSemicolonFile, csvSemicolon, System.Text.Encoding.UTF8);

                // Helper local function simulating TimetablePage.ParseTimetableCsvToDataTable auto-detection
                char DetectDelimiter(string filePath)
                {
                    var lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
                    if (lines.Length == 0) return ',';
                    string headerLine = lines[0];
                    int commaCount = headerLine.Split(',').Length - 1;
                    int semicolonCount = headerLine.Split(';').Length - 1;
                    return semicolonCount > commaCount ? ';' : ',';
                }

                // Assert comma detected
                Assert.Equal(',', DetectDelimiter(tempCommaFile));

                // Assert semicolon detected
                Assert.Equal(';', DetectDelimiter(tempSemicolonFile));

                // Parse using detected delimiter
                char commaDelim = DetectDelimiter(tempCommaFile);
                var commaLines = File.ReadAllLines(tempCommaFile, System.Text.Encoding.UTF8);
                var commaCols = ClassRosterService.ParseCsvLine(commaLines[1], commaDelim);
                Assert.Equal("Thứ 2", commaCols[0]);
                Assert.Equal("Toán", commaCols[2]);

                char semiDelim = DetectDelimiter(tempSemicolonFile);
                var semiLines = File.ReadAllLines(tempSemicolonFile, System.Text.Encoding.UTF8);
                var semiCols = ClassRosterService.ParseCsvLine(semiLines[1], semiDelim);
                Assert.Equal("Thứ 2", semiCols[0]);
                Assert.Equal("Toán", semiCols[2]);
            }
            finally
            {
                if (File.Exists(tempCommaFile)) File.Delete(tempCommaFile);
                if (File.Exists(tempSemicolonFile)) File.Delete(tempSemicolonFile);
            }
        }
    }
}
