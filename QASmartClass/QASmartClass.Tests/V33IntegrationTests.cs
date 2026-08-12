using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Staff.Services;
using QASmartClass.Staff.ViewModels;
using Xunit;

namespace QASmartClass.Tests
{
    public class V33IntegrationTests : IDisposable
    {
        private readonly string _tempDbPath;
        private readonly string _tempVersionPath;

        public V33IntegrationTests()
        {
            if (System.Windows.Application.Current == null)
            {
                try
                {
                    new System.Windows.Application();
                }
                catch { }
            }
            AppServices.UIService = new MockUserInterfaceService();

            string guid = Guid.NewGuid().ToString("N");
            _tempDbPath = Path.Combine(AppPaths.RootDir, $"smartclass_test_{guid}.db");
            _tempVersionPath = Path.Combine(AppPaths.RootDir, $"db_version_{guid}.txt");

            AppPaths.DatabaseFile = _tempDbPath;
            AppPaths.DbVersionFile = _tempVersionPath;
            AppPaths.EnsureDirectories();

            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.33.0");
        }

        [Fact]
        public async Task TestCanteenPos_LowBalance_ChangesBalanceColor()
        {
            using (var db = new AppDbContext())
            {
                // Create a student with low balance
                var student = new Student
                {
                    StudentCode = "STUDENT_V33_LOW",
                    FullName = "Học sinh số dư thấp",
                    WalletBalance = 20000m,
                    LowBalanceThreshold = 30000m,
                    ClassroomId = 1
                };
                db.Students.Add(student);
                db.SaveChanges();

                var vm = new CanteenPosViewModel();
                // Set RFID scan string or student code to load student
                vm.SearchCode = "STUDENT_V33_LOW";
                await vm.SearchStudentCommand.ExecuteAsync(null);

                // Assertions
                Assert.NotNull(vm.CurrentStudent);
                Assert.Equal("STUDENT_V33_LOW", vm.CurrentStudent.StudentCode);
                Assert.True(vm.IsLowBalanceWarningVisible);
            }
        }

        [Fact]
        public async Task TestAssetBooking_DynamicHolidayConflict_WarnsUser()
        {
            using (var db = new AppDbContext())
            {
                // Create a school asset
                var asset = new SchoolAsset
                {
                    AssetCode = "AST-V33-TEST",
                    AssetType = "Projector - Test Projector V33",
                    Status = "Active",
                    Location = "Phòng 202"
                };
                db.SchoolAssets.Add(asset);

                // Create a dynamic holiday event in database
                var bookingDate = DateTime.Today.AddDays(5);
                var holidayEvent = new SchoolEvent
                {
                    Title = "Nghỉ Tết Đoan Ngọ V33",
                    StartTime = bookingDate.Date.AddHours(8),
                    EndTime = bookingDate.Date.AddHours(17),
                    Description = "Sự kiện nghỉ lễ động"
                };
                db.SchoolEvents.Add(holidayEvent);
                db.SaveChanges();

                // Instantiate VM
                var vm = new SchoolAssetManagementViewModel();
                await vm.InitializeAsync();

                vm.BookingAssetId = asset.Id;
                vm.BookingDate = bookingDate;
                vm.BookingTimeSlot = 2;
                vm.BookedBy = "GV_V33_TEST";

                // UIService mock configuration
                var mockUi = (MockUserInterfaceService)AppServices.UIService;
                mockUi.ConfirmResult = false; // Cancel save to just check the warning popup text

                await vm.SaveBookingAsync();

                // Verify warning is displayed with correct dynamic holiday title
                Assert.NotNull(mockUi.LastMessage);
                Assert.Contains("Nghỉ Tết Đoan Ngọ V33", mockUi.LastMessage);
                Assert.Contains("ngày lễ", mockUi.LastMessage);
            }
        }

        [Fact]
        public void TestDbIndexes_AreSuccessfullyCreated()
        {
            using (var db = new AppDbContext())
            {
                var conn = db.Database.GetDbConnection();
                bool hasConnectionOpened = false;
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    conn.Open();
                    hasConnectionOpened = true;
                }

                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='index';";
                        using (var reader = cmd.ExecuteReader())
                        {
                            var indexNames = new System.Collections.Generic.List<string>();
                            while (reader.Read())
                            {
                                indexNames.Add(reader.GetString(0));
                            }

                            // Verify indexes exist
                            Assert.Contains("IX_AssetBookings_AssetId", indexNames);
                            Assert.Contains("IX_StudentGrades_StudentId", indexNames);
                            Assert.Contains("IX_AttendanceRecords_StudentId", indexNames);
                        }
                    }
                }
                finally
                {
                    if (hasConnectionOpened)
                    {
                        conn.Close();
                    }
                }
            }
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
    }
}
