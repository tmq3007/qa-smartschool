using System;
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
    public class V28IntegrationTests
    {
        static V28IntegrationTests()
        {
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();
            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.28.0");
        }

        [Fact]
        public void TestCccdQrParsing_AutoFillsVisitorForm()
        {
            var vm = new SecurityKioskViewModel();
            
            // Raw string from PCA chip-based Citizen ID Card QR format:
            // Số CCCD|Số CMND cũ|Họ và tên|Ngày sinh|Giới tính|Địa chỉ thường trú|Ngày cấp
            string sampleQr = "038096000123|012345678|Nguyễn Văn A|15061990|Nam|Số 123 Đường Láng, Hà Nội|25062021";
            
            vm.QrInputBuffer = sampleQr;
            
            Assert.Equal("Nguyễn Văn A", vm.Person);
            Assert.Contains("Nguyễn Văn A", vm.Description);
            Assert.Contains("038096000123", vm.Description);
            Assert.Contains("Nam", vm.Description);
            Assert.Contains("Số 123 Đường Láng, Hà Nội", vm.Description);
            Assert.Empty(vm.QrInputBuffer); // Check it gets reset
        }

        [Fact]
        public async Task TestCanteenPos_LowBalanceWarning_TriggersCorrectly()
        {
            using (var db = new AppDbContext())
            {
                string hsCode = "HS_V28_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                var student = new Student
                {
                    FullName = "Học sinh V28 Canteen",
                    StudentCode = hsCode,
                    ClassName = "11A1",
                    WalletBalance = 25000,
                    LowBalanceThreshold = 20000, // personalized threshold is 20,000
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
                    mockUI.LastSoundPlayed = string.Empty;
                    mockUI.PlayedSounds.Clear();

                    var vm = new CanteenPosViewModel();
                    
                    // Swipe student
                    vm.SearchCode = hsCode;
                    await vm.SearchStudentCommand.ExecuteAsync(null);
                    Assert.False(vm.IsLowBalanceWarningVisible);

                    // Pay 10,000 -> balance becomes 15,000 (which is < 20,000)
                    vm.DeductionAmount = 10000;
                    vm.OrderDetails = "Suất ăn trưa";
                    await vm.ProcessPaymentCommand.ExecuteAsync(null);

                    Assert.True(vm.IsLowBalanceWarningVisible);
                    Assert.Contains(15000.ToString("N0"), vm.LowBalanceWarningMessage);
                    Assert.Contains("Warning", mockUI.PlayedSounds);
                }
                finally
                {
                    db.Students.Remove(student);
                    await db.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task TestAssetBookingConflict_DisplaysOriginalBookerName()
        {
            using (var db = new AppDbContext())
            {
                var asset = new SchoolAsset
                {
                    AssetCode = "AST-V28-CONF",
                    AssetType = "Projector - Máy chiếu V28",
                    PurchaseDate = DateTime.Now,
                    Status = "Active",
                    Location = "Phòng Sinh Học"
                };
                db.SchoolAssets.Add(asset);
                await db.SaveChangesAsync();

                try
                {
                    var mockUI = QASmartClass.Services.AppServices.UIService as MockUserInterfaceService;
                    Assert.NotNull(mockUI);

                    var vm = new SchoolAssetManagementViewModel();
                    await vm.InitializeAsync();

                    // Step 1: Book the asset first
                    vm.BookingAssetId = asset.Id;
                    vm.BookingDate = new DateTime(2026, 6, 22); // Monday (weekday)
                    vm.BookingTimeSlot = 4;
                    vm.BookedBy = "GV_ORIGINAL_V28";
                    mockUI.ConfirmResult = true;
                    await vm.SaveBookingCommand.ExecuteAsync(null);

                    // Step 2: Try to book again by another teacher, set ConfirmResult = false (cancel) so it exits early
                    vm.BookingAssetId = asset.Id;
                    vm.BookingDate = new DateTime(2026, 6, 22); // Monday (weekday)
                    vm.BookingTimeSlot = 4;
                    vm.BookedBy = "GV_CONFL_V28";
                    
                    mockUI.ConfirmResult = false;
                    mockUI.LastMessage = string.Empty;
                    await vm.SaveBookingCommand.ExecuteAsync(null);

                    // Verify warning message contains the name of the teacher who booked it first
                    Assert.NotNull(mockUI.LastMessage);
                    Assert.Contains("GV_ORIGINAL_V28", mockUI.LastMessage);

                    // Cleanup
                    var bookings = await db.AssetBookings.Where(b => b.AssetId == asset.Id).ToListAsync();
                    db.AssetBookings.RemoveRange(bookings);
                    await db.SaveChangesAsync();
                }
                finally
                {
                    db.SchoolAssets.Remove(asset);
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
