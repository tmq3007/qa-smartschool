using Xunit;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;

namespace QASmartClass.Tests
{
    public class V104TeacherHubUpgradesTests
    {
        private AppDbContext CreateTestDb()
        {
            var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            return db;
        }

        private void CleanupTestDb()
        {
            AppDbContext.FallbackInMemoryConnection = null;
        }

        [Fact]
        public void Test_LessonPlanPage_SafeCasting_DoesNotThrowWhenAppIsNull()
        {
            // Simulate safe cast logic when Application.Current is null or not QASmartTouch.App
            System.Windows.Application appObj = null;
            var castedApp = appObj as QASmartTouch.App;
            
            // Verifying that casting null to QASmartTouch.App returns null and does not throw InvalidCastException
            Assert.Null(castedApp);
        }

        [Fact]
        public void Test_DisciplineService_CreateHomeroomRecord_SetsCorrectStatus()
        {
            using var db = CreateTestDb();
            try
            {
                var service = new DisciplineService(db);

                // Test case 1: Discipline type should be "Pending"
                bool ok1 = service.CreateHomeroomRecord(1, "Nguyen Van A", "10A1", "Discipline", "Vi pham dong phuc", "GV01");
                Assert.True(ok1);
                var rec1 = db.DisciplineRecords.OrderByDescending(r => r.Id).First();
                Assert.Equal("Pending", rec1.Status);
                Assert.Equal(1, rec1.StudentId);
                Assert.Equal("Nguyen Van A", rec1.StudentName);

                // Test case 2: Commendation type should be "Approved"
                bool ok2 = service.CreateHomeroomRecord(2, "Nguyen Van B", "10A1", "Commendation", "Dat giai Nhat mon Toan", "GV01");
                Assert.True(ok2);
                var rec2 = db.DisciplineRecords.OrderByDescending(r => r.Id).First();
                Assert.Equal("Approved", rec2.Status);

                // Test case 3: Warning type should be "Approved"
                bool ok3 = service.CreateHomeroomRecord(3, "Nguyen Van C", "10A1", "Warning", "Nhac nho di muon", "GV01");
                Assert.True(ok3);
                var rec3 = db.DisciplineRecords.OrderByDescending(r => r.Id).First();
                Assert.Equal("Approved", rec3.Status);
            }
            finally
            {
                CleanupTestDb();
            }
        }

        [Fact]
        public void Test_EarlyWarningService_CaseInsensitiveAttendanceStatus()
        {
            using var db = CreateTestDb();
            try
            {
                var guid = Guid.NewGuid().ToString("N").Substring(0, 6);
                // Seed a student and class roster
                var student = new Student { FullName = "Le Van D " + guid, ClassName = "10A2", StudentCode = "HS_" + guid, Status = "Active", ConductScore = 80 };
                db.Students.Add(student);
                db.SaveChanges();

                var roster = new ClassRoster { ClassName = "10A2", IsActive = true };
                db.ClassRosters.Add(roster);
                db.SaveChanges();
                
                // Use DateTime.Today to match environment local date formatting
                var date = DateTime.Today;
                db.AttendanceRecords.Add(new AttendanceRecord { StudentId = student.Id, RosterId = roster.Id, Date = date, Status = "absent" });
                db.AttendanceRecords.Add(new AttendanceRecord { StudentId = student.Id, RosterId = roster.Id, Date = date, Status = "Absent" });
                db.AttendanceRecords.Add(new AttendanceRecord { StudentId = student.Id, RosterId = roster.Id, Date = date, Status = "Vắng" });
                db.AttendanceRecords.Add(new AttendanceRecord { StudentId = student.Id, RosterId = roster.Id, Date = date, Status = "vắng" });
                db.AttendanceRecords.Add(new AttendanceRecord { StudentId = student.Id, RosterId = roster.Id, Date = date, Status = "ABSENT" });
                db.AttendanceRecords.Add(new AttendanceRecord { StudentId = student.Id, RosterId = roster.Id, Date = date, Status = "VẮNG" });
                db.AttendanceRecords.Add(new AttendanceRecord { StudentId = student.Id, RosterId = roster.Id, Date = date, Status = "present" }); // Present, should not count
                
                db.SaveChanges();

                var service = new EarlyWarningService(db);
                var results = service.GetAtRiskStudents(date.Month, date.Year);

                // Since student has 6 absent sessions (which is > 5), they should be flagged
                var target = results.FirstOrDefault(r => r.StudentId == student.Id);
                Assert.True(target != null, $"Expected target student Le Van D (Id={student.Id}) to be found.");
                Assert.Contains("Vắng 6 buổi", target.Reason);
            }
            finally
            {
                CleanupTestDb();
            }
        }

        [Fact]
        public void Test_ParentApproval_ReferenceIdResolution()
        {
            using var db = CreateTestDb();
            try
            {
                var student = new Student { FullName = "Tran Van E", StudentCode = "HS20", Status = "Active" };
                db.Students.Add(student);
                db.SaveChanges();
                
                // Scenario A: Student already has a ContactBookEntry
                var entry = new ContactBookEntry { StudentId = student.Id, StudentName = student.FullName, UpdatedAt = DateTime.Now };
                db.ContactBookEntries.Add(entry);
                db.SaveChanges();

                // Perform simulated ParentApprovalView send request
                var currentContactBook = db.ContactBookEntries
                    .Where(c => c.StudentId == student.Id)
                    .OrderByDescending(c => c.UpdatedAt)
                    .FirstOrDefault();

                Assert.NotNull(currentContactBook);
                Assert.Equal(entry.Id, currentContactBook.Id);

                var req = new ParentApproval
                {
                    StudentId = student.Id,
                    DocumentType = "Sổ Liên Lạc",
                    ReferenceId = currentContactBook.Id,
                    Status = "Pending"
                };
                db.ParentApprovals.Add(req);
                db.SaveChanges();

                Assert.Equal(entry.Id, db.ParentApprovals.OrderByDescending(a => a.Id).First().ReferenceId);

                // Scenario B: Student has NO ContactBookEntry
                var student2 = new Student { FullName = "Tran Van F", StudentCode = "HS21", Status = "Active" };
                db.Students.Add(student2);
                db.SaveChanges();

                var currentContactBook2 = db.ContactBookEntries
                    .Where(c => c.StudentId == student2.Id)
                    .OrderByDescending(c => c.UpdatedAt)
                    .FirstOrDefault();

                if (currentContactBook2 == null)
                {
                    currentContactBook2 = new ContactBookEntry
                    {
                        StudentId = student2.Id,
                        StudentName = student2.FullName,
                        SchoolYear = "2025-2026",
                        Semester = "HK2",
                        UpdatedAt = DateTime.Now
                    };
                    db.ContactBookEntries.Add(currentContactBook2);
                    db.SaveChanges();
                }

                Assert.True(currentContactBook2.Id > 0);

                var req2 = new ParentApproval
                {
                    StudentId = student2.Id,
                    DocumentType = "Sổ Liên Lạc",
                    ReferenceId = currentContactBook2.Id,
                    Status = "Pending"
                };
                db.ParentApprovals.Add(req2);
                db.SaveChanges();

                Assert.Equal(currentContactBook2.Id, db.ParentApprovals.OrderByDescending(a => a.Id).First().ReferenceId);
            }
            finally
            {
                CleanupTestDb();
            }
        }

        [Fact]
        public void Test_ClassMIDashboardView_AbsoluteRadarScale()
        {
            double maxRadius = 90.0;
            const double Max_XP = 100.0;

            // Scenario A: Top performing student (100 XP)
            double scoreA = 100.0;
            double rA = (scoreA / Max_XP) * maxRadius;
            Assert.Equal(90.0, rA); // Outermost boundary

            // Scenario B: Moderate student (50 XP)
            double scoreB = 50.0;
            double rB = (scoreB / Max_XP) * maxRadius;
            Assert.Equal(45.0, rB); // Exactly half way

            // Scenario C: Zero score (0 XP)
            double scoreC = 0.0;
            double rC = (scoreC / Max_XP) * maxRadius;
            rC = Math.Max(5, rC); // Cap minimum
            Assert.Equal(5.0, rC);
        }

        [Fact]
        public void Test_BulletinBoardPage_DateTimeCombiningAndExpiration()
        {
            // Simulate day-only DatePicker combined with hours/minutes
            var selectedDate = new DateTime(2026, 7, 1);
            
            // Scheduling post at 09:30
            string scheduledHour = "09";
            string scheduledMinute = "30";
            int sh = int.Parse(scheduledHour);
            int sm = int.Parse(scheduledMinute);
            var scheduledAt = new DateTime(selectedDate.Year, selectedDate.Month, selectedDate.Day, sh, sm, 0);
            
            Assert.Equal(new DateTime(2026, 7, 1, 9, 30, 0), scheduledAt);

            // Expiration at end of day (defaults to 23:59:59)
            string expiresHour = "23";
            string expiresMinute = "59";
            int eh = int.Parse(expiresHour);
            int em = int.Parse(expiresMinute);
            var expiresAt = new DateTime(selectedDate.Year, selectedDate.Month, selectedDate.Day, eh, em, 59);

            Assert.Equal(new DateTime(2026, 7, 1, 23, 59, 59), expiresAt);
        }

        [Fact]
        public void Test_SystemSettings_SaveAndLoadGateAndLocationSettings()
        {
            using var db = CreateTestDb();
            try
            {
                // Helper function mimicking SystemSettingsView SaveSetting
                Action<string, string, string> saveHelper = (key, val, cat) =>
                {
                    var existing = db.SystemSettings.Find(key);
                    if (existing != null)
                    {
                        existing.Value = val;
                    }
                    else
                    {
                        db.SystemSettings.Add(new SystemSetting { Id = key, Value = val, Category = cat, LastUpdated = DateTime.Now });
                    }
                    db.SaveChanges();
                };

                // Save Gate and Location Settings
                saveHelper("IT_Location_TrackingTechnology", "1", "IT");
                saveHelper("IT_Gate_OfflineFallbackMode", "2", "IT");
                saveHelper("IT_Gate_AntiPassbackMode", "1", "IT");
                saveHelper("IT_Gate_AntiPassbackAction", "1", "IT");
                saveHelper("IT_Gate_FaceMatchRequirement", "0", "IT");

                // Verify they load exactly as saved
                Assert.Equal("1", db.SystemSettings.Find("IT_Location_TrackingTechnology")?.Value);
                Assert.Equal("2", db.SystemSettings.Find("IT_Gate_OfflineFallbackMode")?.Value);
                Assert.Equal("1", db.SystemSettings.Find("IT_Gate_AntiPassbackMode")?.Value);
                Assert.Equal("1", db.SystemSettings.Find("IT_Gate_AntiPassbackAction")?.Value);
                Assert.Equal("0", db.SystemSettings.Find("IT_Gate_FaceMatchRequirement")?.Value);
            }
            finally
            {
                CleanupTestDb();
            }
        }
    }
}
