using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.Tests
{
    [Collection("Sequential")]
    public class V112_Phase2UpgradeTests
    {
        private AppDbContext CreateTempDbContext(string dbPath)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            var db = new AppDbContext(options);
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            return db;
        }

        [Fact]
        public void Test_TeacherIdResolution_ByNameOrSelection()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "phase2_teachertest.db");
            try
            {
                using (var db = CreateTempDbContext(dbPath))
                {
                    // 1. Add staff profiles
                    var teacher1 = new StaffProfile { Id = 10, FullName = "Nguyễn Văn A" };
                    var teacher2 = new StaffProfile { Id = 20, FullName = "Trần Thị B" };
                    db.StaffProfiles.AddRange(teacher1, teacher2);
                    db.SaveChanges();

                    // Simulate resolving Teacher ID from auto-suggest selection (Id = 10)
                    int selectedId = 10;
                    string nameInput = "Nguyễn Văn A";
                    
                    var resolvedStaff = db.StaffProfiles.Find(selectedId);
                    Assert.NotNull(resolvedStaff);
                    Assert.Equal("Nguyễn Văn A", resolvedStaff.FullName);

                    // Simulate manual typing fallback ("trần thị b" -> case-insensitive)
                    string typedName = "trần thị b";
                    var resolvedStaff2 = db.StaffProfiles.FirstOrDefault(s => s.FullName.ToLower() == typedName.ToLower());
                    Assert.NotNull(resolvedStaff2);
                    Assert.Equal(20, resolvedStaff2.Id);
                    Assert.Equal("Trần Thị B", resolvedStaff2.FullName);

                    // Simulate thỉnh giảng (freelance teacher) name not in database
                    string freelanceName = "Lý Thỉnh Giảng";
                    var resolvedStaff3 = db.StaffProfiles.FirstOrDefault(s => s.FullName.ToLower() == freelanceName.ToLower());
                    Assert.Null(resolvedStaff3); // should fallback to teacherIdVal = 0
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [Fact]
        public void Test_CalendarExcelExport_UTF8BOMEncoding()
        {
            // Simulate the CSV export functionality
            var events = new List<SchoolEvent>
            {
                new SchoolEvent
                {
                    Title = "Lễ hội Trăng Rằm tiếng Việt",
                    StartTime = new DateTime(2026, 9, 15, 8, 0, 0),
                    EndTime = new DateTime(2026, 9, 15, 12, 0, 0),
                    Location = "Sân trường",
                    Organizer = "BGH",
                    Description = "Mô tả lễ hội tiếng Việt có dấu: á à ả ã ạ"
                }
            };

            string tempCsvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_lich.csv");
            try
            {
                using (var stream = new FileStream(tempCsvPath, FileMode.Create, FileAccess.Write))
                {
                    byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
                    stream.Write(bom, 0, bom.Length);

                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        writer.WriteLine("Tiêu đề sự kiện,Bắt đầu,Kết thúc,Người tổ chức,Địa điểm,Nội dung chi tiết");
                        foreach (var ev in events)
                        {
                            writer.WriteLine($"{ev.Title},{ev.StartTime:dd/MM/yyyy HH:mm},{ev.EndTime:dd/MM/yyyy HH:mm},{ev.Organizer},{ev.Location},{ev.Description}");
                        }
                    }
                }

                // Verify file exists
                Assert.True(File.Exists(tempCsvPath));

                // Verify BOM bytes at the start of file
                byte[] fileBytes = File.ReadAllBytes(tempCsvPath);
                Assert.True(fileBytes.Length > 3);
                Assert.Equal(0xEF, fileBytes[0]);
                Assert.Equal(0xBB, fileBytes[1]);
                Assert.Equal(0xBF, fileBytes[2]);

                // Read file contents back as UTF-8 string
                string fileContent = File.ReadAllText(tempCsvPath, Encoding.UTF8);
                Assert.Contains("Lễ hội Trăng Rằm tiếng Việt", fileContent);
                Assert.Contains("á à ả ã ạ", fileContent);
            }
            finally
            {
                if (File.Exists(tempCsvPath)) File.Delete(tempCsvPath);
            }
        }
    }
}
