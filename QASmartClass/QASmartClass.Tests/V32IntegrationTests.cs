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
    public class V32IntegrationTests : IDisposable
    {
        private readonly string _tempDbPath;
        private readonly string _tempVersionPath;

        public V32IntegrationTests()
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
            DbMigrator.Migrate(db, "5.31.0");
        }

        [Fact]
        public async Task TestAssetBooking_Conflict_DisplaysConflictingTeacherName()
        {
            using (var db = new AppDbContext())
            {
                // Create a test teacher profile
                var teacher = new TeacherProfile
                {
                    TeacherCode = "GV_V32_CONFLICT",
                    FullName = "Nguyễn Văn Chiến Thắng",
                    Role = "GV"
                };
                db.TeacherProfiles.Add(teacher);

                // Create a school asset
                var asset = new SchoolAsset
                {
                    AssetCode = "AST-V32-TEST",
                    AssetType = "Projector - Test Projector",
                    Status = "Active",
                    Location = "Phòng 101"
                };
                db.SchoolAssets.Add(asset);
                db.SaveChanges();

                // Create an existing booking (ensure it's a weekday to avoid weekend warning)
                var bookingDate = DateTime.Today.AddDays(1);
                while (bookingDate.DayOfWeek == DayOfWeek.Saturday || bookingDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    bookingDate = bookingDate.AddDays(1);
                }
                var booking = new SchoolAssetBooking
                {
                    AssetId = asset.Id,
                    BookingDate = bookingDate.Date,
                    TimeSlot = 3,
                    BookedBy = "GV_V32_CONFLICT"
                };
                db.AssetBookings.Add(booking);
                db.SaveChanges();

                // Instantiate VM
                var vm = new SchoolAssetManagementViewModel();
                await vm.InitializeAsync();

                // Set properties to attempt booking same asset at same slot
                vm.BookingAssetId = asset.Id;
                vm.BookingDate = bookingDate;
                vm.BookingTimeSlot = 3;
                vm.BookedBy = "GV_V32_NEW";

                // We want to verify conflict warning displays full name
                var mockUi = (MockUserInterfaceService)AppServices.UIService;
                mockUi.ConfirmResult = false; // Cancel save for now to test warning text

                await vm.SaveBookingAsync();

                // Assertions
                Assert.Contains("Nguyễn Văn Chiến Thắng", mockUi.LastMessage);
                Assert.Contains("GV_V32_CONFLICT", mockUi.LastMessage);
                Assert.Contains("đăng ký mượn trong cùng tiết và ngày học", mockUi.LastMessage);

                // Verify no double booking occurred because we selected cancel (ConfirmResult = false)
                using (var checkDb = new AppDbContext())
                {
                    var count = checkDb.AssetBookings.Count(b => b.AssetId == asset.Id && b.BookingDate.Date == bookingDate.Date && b.TimeSlot == 3);
                    Assert.Equal(1, count);
                }

                // Now allow booking override (ConfirmResult = true)
                mockUi.ConfirmResult = true;
                await vm.SaveBookingAsync();

                // Verify overriding booking was saved successfully
                using (var checkDb = new AppDbContext())
                {
                    var bookingsForAsset = checkDb.AssetBookings.Where(b => b.AssetId == asset.Id && b.BookingDate.Date == bookingDate.Date && b.TimeSlot == 3).ToList();
                    Assert.Equal(2, bookingsForAsset.Count);
                    Assert.Contains(bookingsForAsset, b => b.BookedBy == "GV_V32_NEW");
                }
            }
        }

        [Fact]
        public void TestAuditHelper_IdentityAudit_LogsUserAndMachine()
        {
            using (var db = new AppDbContext())
            {
                // Verify when logged in, actor includes TeacherCode
                var testUser = new TeacherProfile
                {
                    TeacherCode = "GV_V32_AUDIT",
                    FullName = "Kiểm toán viên V32",
                    Role = "Admin"
                };
                StaffSession.Login(testUser);

                try
                {
                    string details = "Action details " + Guid.NewGuid();
                    AuditHelper.Log(db, "Test_V32_Action", details);

                    // Check Database
                    var logEntry = db.AuditLogs.OrderByDescending(l => l.Timestamp).FirstOrDefault(l => l.Details == details);
                    Assert.NotNull(logEntry);
                    Assert.Contains(Environment.MachineName, logEntry.ActorName);
                    Assert.Contains("User: GV_V32_AUDIT", logEntry.ActorName);
                }
                finally
                {
                    StaffSession.Logout();
                }

                // Verify when not logged in, actor defaults to MachineName only
                string detailsAnonymous = "Action details anonymous " + Guid.NewGuid();
                AuditHelper.Log(db, "Test_V32_Action_Anon", detailsAnonymous);

                var logEntryAnon = db.AuditLogs.OrderByDescending(l => l.Timestamp).FirstOrDefault(l => l.Details == detailsAnonymous);
                Assert.NotNull(logEntryAnon);
                Assert.Equal(Environment.MachineName, logEntryAnon.ActorName);
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
