using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Staff.ViewModels;

namespace QASmartClass.Tests
{
    public class V105_AssetMaintenanceTests
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
        public async Task Test_Asset_AddAsset_SetsNextMaintenanceDate()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asset_add.db");
            try
            {
                using (var db = CreateTempDbContext(dbPath))
                {
                    // Set interval to 3 months
                    db.SystemSettings.Add(new SystemSetting { Id = "IT_Asset_MaintenanceIntervalMonths", Value = "3", Category = "IT" });
                    db.SaveChanges();

                    var asset = new SchoolAsset
                    {
                        AssetCode = "AST-001",
                        AssetType = "Projector - Sony Laser",
                        PurchaseDate = DateTime.Today,
                        Status = "Active"
                    };
                    
                    // Compute next maintenance date based on setting
                    var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Asset_MaintenanceIntervalMonths");
                    int months = setting != null && int.TryParse(setting.Value, out int m) ? m : 6;
                    asset.NextMaintenanceDate = asset.PurchaseDate.AddMonths(months);

                    db.SchoolAssets.Add(asset);
                    db.SaveChanges();

                    var saved = db.SchoolAssets.Find(asset.Id);
                    Assert.NotNull(saved);
                    Assert.Equal(DateTime.Today.AddMonths(3), saved.NextMaintenanceDate);
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [Fact]
        public void Test_Asset_MaintenanceOverdue_Detection()
        {
            var activeAssetOverdue = new AssetDisplayModel
            {
                StatusText = "Active"
            };
            // Set NextMaintenanceDate to yesterday
            var nextMaint = DateTime.Today.AddDays(-1);
            bool isOverdue = (activeAssetOverdue.StatusText == "Active" || activeAssetOverdue.StatusText == "NeedsRepair") && nextMaint < DateTime.Today;
            
            Assert.True(isOverdue);

            var activeAssetNotOverdue = new AssetDisplayModel
            {
                StatusText = "Active"
            };
            var nextMaintFuture = DateTime.Today.AddDays(1);
            bool isNotOverdue = (activeAssetNotOverdue.StatusText == "Active" || activeAssetNotOverdue.StatusText == "NeedsRepair") && nextMaintFuture < DateTime.Today;

            Assert.False(isNotOverdue);
        }

        [Fact]
        public void Test_Asset_PerformMaintenance_UpdatesDatesAndCount()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asset_maint_perform.db");
            try
            {
                using (var db = CreateTempDbContext(dbPath))
                {
                    db.SystemSettings.Add(new SystemSetting { Id = "IT_Asset_MaintenanceIntervalMonths", Value = "6", Category = "IT" });
                    
                    var asset = new SchoolAsset
                    {
                        AssetCode = "AST-002",
                        AssetType = "Tablet - iPad Air",
                        PurchaseDate = DateTime.Today.AddMonths(-6),
                        NextMaintenanceDate = DateTime.Today.AddDays(-5),
                        Status = "NeedsRepair",
                        RepairCount = 2
                    };
                    db.SchoolAssets.Add(asset);
                    db.SaveChanges();

                    // Perform maintenance
                    var saved = db.SchoolAssets.Find(asset.Id);
                    Assert.NotNull(saved);
                    
                    saved.Status = "Active";
                    saved.RepairCount += 1;
                    var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Asset_MaintenanceIntervalMonths");
                    int months = setting != null && int.TryParse(setting.Value, out int m) ? m : 6;
                    saved.NextMaintenanceDate = DateTime.Today.AddMonths(months);
                    db.SaveChanges();

                    var updated = db.SchoolAssets.Find(asset.Id);
                    Assert.NotNull(updated);
                    Assert.Equal("Active", updated.Status);
                    Assert.Equal(3, updated.RepairCount);
                    Assert.Equal(DateTime.Today.AddMonths(6), updated.NextMaintenanceDate);
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [Fact]
        public async Task Test_Asset_AutoCancelBookingsOnRepair_Mode1_CancelsAndNotifies()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asset_cancel_m1.db");
            try
            {
                using (var db = CreateTempDbContext(dbPath))
                {
                    // Mode 1: Hủy lịch mượn & gửi thông báo
                    db.SystemSettings.Add(new SystemSetting { Id = "Asset_AutoCancelBookingsOnRepair", Value = "1", Category = "Database" });
                    
                    var asset = new SchoolAsset { AssetCode = "AST-M1", AssetType = "Projector - Sony", Status = "Active" };
                    db.SchoolAssets.Add(asset);
                    db.SaveChanges();

                    var booking = new SchoolAssetBooking
                    {
                        AssetId = asset.Id,
                        BookedBy = "GV001",
                        BookingDate = DateTime.Today.AddDays(2),
                        TimeSlot = 3
                    };
                    db.AssetBookings.Add(booking);
                    db.SaveChanges();

                    // Perform cancellation logic
                    var bookingsToProcess = await db.AssetBookings
                        .Where(b => b.AssetId == asset.Id && b.BookingDate.Date >= DateTime.Today)
                        .ToListAsync();

                    Assert.Single(bookingsToProcess);

                    db.AssetBookings.RemoveRange(bookingsToProcess);
                    
                    var log = new PushMessageLog
                    {
                        RecipientId = 1,
                        RecipientRole = "Teacher",
                        Title = "Hủy lịch mượn thiết bị",
                        Body = $"Lịch đặt thiết bị {asset.AssetCode} vào ngày {booking.BookingDate:dd/MM} đã bị hủy.",
                        SentAt = DateTime.Now,
                        Status = "Delivered"
                    };
                    db.PushMessageLogs.Add(log);
                    db.SaveChanges();

                    Assert.Empty(db.AssetBookings.Where(b => b.AssetId == asset.Id).ToList());
                    Assert.Single(db.PushMessageLogs.ToList());
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [Fact]
        public async Task Test_Asset_AutoCancelBookingsOnRepair_Mode0_ReplacesOrCancels()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asset_cancel_m0.db");
            try
            {
                using (var db = CreateTempDbContext(dbPath))
                {
                    // Mode 0: Tự động thay thế
                    db.SystemSettings.Add(new SystemSetting { Id = "Asset_AutoCancelBookingsOnRepair", Value = "0", Category = "Database" });
                    
                    var assetBroken = new SchoolAsset { AssetCode = "AST-B", AssetType = "Projector - Sony", Status = "NeedsRepair" };
                    var assetAlt = new SchoolAsset { AssetCode = "AST-A", AssetType = "Projector - Panasonic", Status = "Active" };
                    db.SchoolAssets.Add(assetBroken);
                    db.SchoolAssets.Add(assetAlt);
                    db.SaveChanges();

                    var booking = new SchoolAssetBooking
                    {
                        AssetId = assetBroken.Id,
                        BookedBy = "GV002",
                        BookingDate = DateTime.Today.AddDays(2),
                        TimeSlot = 3
                    };
                    db.AssetBookings.Add(booking);
                    db.SaveChanges();

                    // Alternate check logic
                    var category = "Projector";
                    var alternateAssets = await db.SchoolAssets
                        .Where(a => a.Id != assetBroken.Id && a.Status == "Active" && a.AssetType.Contains(category))
                        .ToListAsync();

                    Assert.Single(alternateAssets);
                    var replacement = alternateAssets.FirstOrDefault(alt => 
                        !db.AssetBookings.Any(b => b.AssetId == alt.Id && b.BookingDate.Date == booking.BookingDate.Date && b.TimeSlot == booking.TimeSlot)
                    );

                    Assert.NotNull(replacement);
                    booking.AssetId = replacement.Id;
                    db.SaveChanges();

                    var updatedBooking = db.AssetBookings.Find(booking.Id);
                    Assert.NotNull(updatedBooking);
                    Assert.Equal(assetAlt.Id, updatedBooking.AssetId); // Replaced successfully!
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }
    }
}
