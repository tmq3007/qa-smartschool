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
    public class V29IntegrationTests
    {
        static V29IntegrationTests()
        {
            if (System.Windows.Application.Current == null)
            {
                try
                {
                    new System.Windows.Application();
                }
                catch { }
            }
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();
            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.29.0");
        }

        [Fact]
        public void TestAuditHelper_WritesToFile()
        {
            using (var db = new AppDbContext())
            {
                string action = "Test_Action_V29";
                string actor = "Test_Actor_V29";
                string details = "Test_Details_V29";

                // Arrange - Clear existing file if any
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                string logPath = Path.Combine(logDir, "audit.log");
                if (File.Exists(logPath))
                {
                    try { File.Delete(logPath); } catch { }
                }

                // Act
                AuditHelper.Log(db, action, actor, details);

                // Assert Database
                var logInDb = db.AuditLogs.FirstOrDefault(l => l.Action == action && l.ActorName == actor);
                Assert.NotNull(logInDb);
                Assert.Equal(details, logInDb.Details);

                // Assert File
                Assert.True(File.Exists(logPath));
                string fileContent = File.ReadAllText(logPath);
                Assert.Contains(action, fileContent);
                Assert.Contains(actor, fileContent);
                Assert.Contains(details, fileContent);

                // Clean up database
                db.AuditLogs.Remove(logInDb);
                db.SaveChanges();
            }
        }

        [Fact]
        public async Task TestAssetBooking_HolidayWarning_ShowsConfirm()
        {
            using (var db = new AppDbContext())
            {
                var asset = new SchoolAsset
                {
                    AssetCode = "AST-V29-TEST",
                    AssetType = "Tablet - Máy tính bảng V29",
                    PurchaseDate = DateTime.Now,
                    Status = "Active",
                    Location = "Thư viện"
                };
                db.SchoolAssets.Add(asset);
                await db.SaveChangesAsync();

                try
                {
                    var mockUI = QASmartClass.Services.AppServices.UIService as MockUserInterfaceService;
                    Assert.NotNull(mockUI);

                    var vm = new SchoolAssetManagementViewModel();
                    await vm.InitializeAsync();

                    // Saturday booking (Weekend)
                    vm.BookingAssetId = asset.Id;
                    vm.BookingDate = new DateTime(2026, 6, 27); // Saturday
                    vm.BookingTimeSlot = 2;
                    vm.BookedBy = "GV_WEEKEND_V29";
                    
                    // Cancel booking
                    mockUI.ConfirmResult = false;
                    await vm.SaveBookingCommand.ExecuteAsync(null);

                    // Verify it didn't save because we canceled the holiday/weekend warning
                    var bookingsCount = await db.AssetBookings.CountAsync(b => b.AssetId == asset.Id);
                    Assert.Equal(0, bookingsCount);

                    // Confirm warning booking
                    mockUI.ConfirmResult = true;
                    await vm.SaveBookingCommand.ExecuteAsync(null);

                    // Verify it saved now
                    var weekendBooking = await db.AssetBookings.FirstOrDefaultAsync(b => b.AssetId == asset.Id && b.BookedBy == "GV_WEEKEND_V29");
                    Assert.NotNull(weekendBooking);

                    // Cleanup booking
                    db.AssetBookings.Remove(weekendBooking);
                    await db.SaveChangesAsync();
                }
                finally
                {
                    db.SchoolAssets.Remove(asset);
                    await db.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task TestCanteenPos_FcmException_TransactionSucceeds()
        {
            using (var db = new AppDbContext())
            {
                string hsCode = "HS_V29_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                var student = new Student
                {
                    FullName = "Học sinh V29 Canteen FCM Fail",
                    StudentCode = hsCode,
                    ClassName = "12A2",
                    WalletBalance = 80000,
                    LowBalanceThreshold = 10000,
                    Status = "Active",
                    IsOnline = false,
                    LastSeen = DateTime.Now
                };
                db.Students.Add(student);
                await db.SaveChangesAsync();

                try
                {
                    var mockUI = QASmartClass.Services.AppServices.UIService as MockUserInterfaceService;
                    Assert.NotNull(mockUI);
                    mockUI.ConfirmResult = true; // confirm payment

                    var vm = new CanteenPosViewModel();
                    vm.SearchCode = hsCode;
                    await vm.SearchStudentCommand.ExecuteAsync(null);

                    // Assert student is found
                    Assert.NotNull(vm.CurrentStudent);
                    Assert.Equal("Học sinh V29 Canteen FCM Fail", vm.CurrentStudent.FullName);

                    // Enable FCM Exception
                    MobileApiService.SimulateNetworkError = true;

                    vm.DeductionAmount = 30000;
                    vm.OrderDetails = "Bữa trưa V29";
                    
                    // Act
                    await vm.ProcessPaymentCommand.ExecuteAsync(null);

                    // Assert success message
                    Assert.Equal("Đã thanh toán thành công 30,000 đ", vm.StatusMessage);

                    // Assert: Wallet balance must be updated successfully despite FCM failure
                    // Use a fresh DbContext to bypass the cache tracking in EF Core
                    using (var dbFresh = new AppDbContext())
                    {
                        var studentAfter = await dbFresh.Students.FirstOrDefaultAsync(s => s.StudentCode == hsCode);
                        Assert.NotNull(studentAfter);
                        Assert.Equal(50000, studentAfter.WalletBalance); // 80k - 30k = 50k
                    }

                    // Cleanup transaction logs
                    var logs = await db.EventLogs.Where(l => l.Actor == hsCode).ToListAsync();
                    db.EventLogs.RemoveRange(logs);
                    await db.SaveChangesAsync();
                }
                finally
                {
                    MobileApiService.SimulateNetworkError = false;
                    db.Students.Remove(student);
                    await db.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public void TestCleaningSchedule_AutoRefreshTimer_Exists()
        {
            var vm = new CleaningScheduleViewModel();
            Assert.NotNull(vm.AutoUpdateTimer);
            Assert.Equal(TimeSpan.FromSeconds(60), vm.AutoUpdateTimer.Interval);
        }
    }
}
