using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;

namespace QASmartClass.Tests
{
    public class V103_MobileDeviceAuthorizationTests
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
        public void Test_MobileApp_AutoApprove_Flow()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "auto_approve.db");
            try
            {
                using (var db = CreateTempDbContext(dbPath))
                {
                    // 1. Ensure AutoApprove setting is Enabled
                    var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Mobile_AutoApprove");
                    if (setting == null)
                    {
                        db.SystemSettings.Add(new SystemSetting { Id = "IT_Mobile_AutoApprove", Value = "Enabled", Category = "IT" });
                    }
                    else
                    {
                        setting.Value = "Enabled";
                    }
                    db.SaveChanges();

                    var service = new MobileApiService(db);
                    var token = service.RegisterDevice(userId: 101, role: "Parent", deviceToken: "token_auto", deviceName: "iPhone 14 Pro", platform: "iOS");

                    Assert.NotNull(token);
                    Assert.Equal("Approved", token.ApprovalStatus);
                    Assert.True(token.IsActive);
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [Fact]
        public void Test_MobileApp_PendingApproval_Flow()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "manual_approve.db");
            try
            {
                using (var db = CreateTempDbContext(dbPath))
                {
                    // 1. Disable AutoApprove setting
                    var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Mobile_AutoApprove");
                    if (setting == null)
                    {
                        db.SystemSettings.Add(new SystemSetting { Id = "IT_Mobile_AutoApprove", Value = "Disabled", Category = "IT" });
                    }
                    else
                    {
                        setting.Value = "Disabled";
                    }
                    db.SaveChanges();

                    var service = new MobileApiService(db);
                    var token = service.RegisterDevice(userId: 102, role: "Student", deviceToken: "token_manual", deviceName: "Samsung Galaxy S23", platform: "Android");

                    Assert.NotNull(token);
                    Assert.Equal("Pending", token.ApprovalStatus);
                    Assert.False(token.IsActive);
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [Fact]
        public async Task Test_MobileApp_ApproveAndReject_Actions()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "actions_approve.db");
            try
            {
                using (var db = CreateTempDbContext(dbPath))
                {
                    var service = new MobileApiService(db);
                    
                    // Create a pending token
                    var token = new MobileToken
                    {
                        UserId = 103,
                        Role = "Parent",
                        DeviceToken = "token_action",
                        DeviceName = "iPad Air",
                        Platform = "iOS",
                        ApprovalStatus = "Pending",
                        IsActive = false
                    };
                    db.MobileTokens.Add(token);
                    db.SaveChanges();

                    // Approve device
                    bool approved = await service.ApproveDeviceAsync(token.Id);
                    Assert.True(approved);
                    
                    var approvedToken = db.MobileTokens.Find(token.Id);
                    Assert.NotNull(approvedToken);
                    Assert.Equal("Approved", approvedToken.ApprovalStatus);
                    Assert.True(approvedToken.IsActive);

                    // Reject device
                    bool rejected = await service.RejectDeviceAsync(token.Id);
                    Assert.True(rejected);
                    
                    var rejectedToken = db.MobileTokens.Find(token.Id);
                    Assert.NotNull(rejectedToken);
                    Assert.Equal("Rejected", rejectedToken.ApprovalStatus);
                    Assert.False(rejectedToken.IsActive);
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
