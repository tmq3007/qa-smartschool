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
    public class V27IntegrationTests
    {
        static V27IntegrationTests()
        {
            QASmartClass.Staff.Services.ServiceRegistration.Initialize();
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();
            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.27.0");
        }

        [Fact]
        public async Task TestAssetBookingConflict_ShouldWarn()
        {
            using (var db = new AppDbContext())
            {
                // Create an asset
                var asset = new SchoolAsset
                {
                    AssetCode = "AST-TEST-V27",
                    AssetType = "Projector - Máy chiếu 101",
                    PurchaseDate = DateTime.Now,
                    Status = "Active",
                    Location = "Phòng Hóa Học"
                };
                db.SchoolAssets.Add(asset);
                await db.SaveChangesAsync();

                try
                {
                    var mockUI = QASmartClass.Services.AppServices.UIService as MockUserInterfaceService;
                    Assert.NotNull(mockUI);

                    var vm = new SchoolAssetManagementViewModel();
                    await vm.InitializeAsync();

                    // Step 1: Book the asset
                    vm.BookingAssetId = asset.Id;
                    vm.BookingDate = new DateTime(2026, 6, 15);
                    vm.BookingTimeSlot = 3;
                    vm.BookedBy = "GV_TEST_V27_A";

                    mockUI.ConfirmResult = true; 
                    await vm.SaveBookingCommand.ExecuteAsync(null);

                    // Verify first booking saved
                    using (var verifyDb = new AppDbContext())
                    {
                        var booking = await verifyDb.AssetBookings.FirstOrDefaultAsync(b => b.AssetId == asset.Id && b.BookedBy == "GV_TEST_V27_A");
                        Assert.NotNull(booking);
                        Assert.Equal(3, booking.TimeSlot);
                    }

                    // Step 2: Try to book the same asset at the same slot and date with ConfirmResult = false (cancel)
                    vm.BookingAssetId = asset.Id;
                    vm.BookingDate = new DateTime(2026, 6, 15);
                    vm.BookingTimeSlot = 3;
                    vm.BookedBy = "GV_TEST_V27_B";

                    mockUI.ConfirmResult = false; // User cancels warning
                    await vm.SaveBookingCommand.ExecuteAsync(null);

                    // Verify second booking is NOT saved
                    using (var verifyDb = new AppDbContext())
                    {
                        var bookingB = await verifyDb.AssetBookings.FirstOrDefaultAsync(b => b.AssetId == asset.Id && b.BookedBy == "GV_TEST_V27_B");
                        Assert.Null(bookingB);
                    }

                    // Step 3: Try to book with ConfirmResult = true (allow override)
                    mockUI.ConfirmResult = true; // User accepts warning
                    await vm.SaveBookingCommand.ExecuteAsync(null);

                    // Verify second booking IS saved
                    using (var verifyDb = new AppDbContext())
                    {
                        var bookingB = await verifyDb.AssetBookings.FirstOrDefaultAsync(b => b.AssetId == asset.Id && b.BookedBy == "GV_TEST_V27_B");
                        Assert.NotNull(bookingB);
                        Assert.Equal(3, bookingB.TimeSlot);

                        // Cleanup bookings
                        var bookings = await verifyDb.AssetBookings.Where(b => b.AssetId == asset.Id).ToListAsync();
                        verifyDb.AssetBookings.RemoveRange(bookings);
                        await verifyDb.SaveChangesAsync();
                    }
                }
                finally
                {
                    db.SchoolAssets.Remove(asset);
                    await db.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task TestRolePermissions_DynamicLoading()
        {
            using (var db = new AppDbContext())
            {
                // Insert a test role permission
                var rp = new RolePermission
                {
                    Role = "RoleV27Test",
                    PermissionTag = "FeatureV27Test"
                };
                db.RolePermissions.Add(rp);
                await db.SaveChangesAsync();

                try
                {
                    // Let's verify it is loaded correctly
                    using (var verifyDb = new AppDbContext())
                    {
                        var loaded = await verifyDb.RolePermissions.FirstOrDefaultAsync(x => x.Role == "RoleV27Test" && x.PermissionTag == "FeatureV27Test");
                        Assert.NotNull(loaded);
                    }
                }
                finally
                {
                    db.RolePermissions.Remove(rp);
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
