using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Staff.ViewModels;
using Xunit;

namespace QASmartClass.Tests
{
    public class V16IntegrationTests
    {
        static V16IntegrationTests()
        {
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();

            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.24.0");
            QASmartClass.Staff.Services.ServiceRegistration.Initialize();
        }

        [Fact]
        public async Task TestPushNotification_SendPush_WritesAuditLog()
        {
            using (var db = new AppDbContext())
            {
                string uniqueTitle = "Push Title " + Guid.NewGuid().ToString("N").Substring(0, 6);
                var vm = new PushNotificationCenterViewModel();
                vm.Title = uniqueTitle;
                vm.Body = "Nội dung kiểm thử gửi thông báo đẩy";
                vm.SelectedTarget = "AllParents";

                await vm.SendPushCommand.ExecuteAsync(null);

                using var verifyDb = new AppDbContext();
                var audit = await verifyDb.AuditLogs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.Action == "Send_PushNotification" && l.Details.Contains(uniqueTitle));
                Assert.NotNull(audit);

                // Cleanup
                var pushLog = await verifyDb.PushMessageLogs.FirstOrDefaultAsync(p => p.Title == uniqueTitle);
                if (pushLog != null) verifyDb.PushMessageLogs.Remove(pushLog);
                verifyDb.AuditLogs.Remove(audit);
                try { await verifyDb.SaveChangesAsync(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V16IntegrationTests] DB save error: {ex.Message}"); throw; }
            }
        }

        [Fact]
        public async Task TestAssetManagement_DecommissionAsset_UpdatesStatusAndWritesAuditLog()
        {
            using (var db = new AppDbContext())
            {
                string testCode = "AST-TEST-" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
                var asset = new SchoolAsset
                {
                    AssetCode = testCode,
                    AssetType = "Smartboard - Test Asset",
                    Location = "Room 101",
                    PurchaseDate = DateTime.Now,
                    Status = "Active"
                };
                db.SchoolAssets.Add(asset);
                try { await db.SaveChangesAsync(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V16IntegrationTests] DB save error: {ex.Message}"); throw; }

                var profile = new TeacherProfile
                {
                    FullName = "Quan Ly Tai San Test",
                    TeacherCode = "TC_QLTS_TEST",
                    TeacherPassword = "123",
                    Role = "GV",
                    IsActive = true
                };
                db.TeacherProfiles.Add(profile);
                await db.SaveChangesAsync();

                try
                {
                    QASmartClass.Staff.Services.StaffSession.Login(profile);

                    var vm = new SchoolAssetManagementViewModel();
                    var assetInVm = vm.Assets.FirstOrDefault(a => a.AssetCode == testCode);
                    if (assetInVm == null)
                    {
                        await vm.LoadDataCommand.ExecuteAsync(null);
                        assetInVm = vm.Assets.FirstOrDefault(a => a.AssetCode == testCode);
                    }
                    Assert.NotNull(assetInVm);

                    await vm.DecommissionAssetCommand.ExecuteAsync(assetInVm.Id);

                    using var verifyDb = new AppDbContext();
                    var updatedAsset = await verifyDb.SchoolAssets.AsNoTracking().FirstOrDefaultAsync(a => a.AssetCode == testCode);
                    Assert.NotNull(updatedAsset);
                    Assert.Equal("Broken", updatedAsset!.Status);

                    var audit = await verifyDb.AuditLogs
                        .AsNoTracking()
                        .FirstOrDefaultAsync(l => l.Action == "Decommission_Asset" && l.ActorName == "TC_QLTS_TEST" && l.Details.Contains(testCode));
                    Assert.NotNull(audit);

                    // Cleanup
                    var aDb = await verifyDb.SchoolAssets.FindAsync(updatedAsset.Id);
                    if (aDb != null) verifyDb.SchoolAssets.Remove(aDb);
                    verifyDb.AuditLogs.Remove(audit);
                    try { await verifyDb.SaveChangesAsync(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V16IntegrationTests] DB save error: {ex.Message}"); throw; }
                }
                finally
                {
                    using var cleanupDb = new AppDbContext();
                    var prof = await cleanupDb.TeacherProfiles.FirstOrDefaultAsync(t => t.TeacherCode == "TC_QLTS_TEST");
                    if (prof != null) cleanupDb.TeacherProfiles.Remove(prof);
                    await cleanupDb.SaveChangesAsync();
                    QASmartClass.Staff.Services.StaffSession.Logout();
                }
            }
        }

        [Fact]
        public async Task TestCanteenPOS_ReprintReceipt_ExecutesWithoutException()
        {
            var vm = new CanteenPosViewModel();
            // Test default: last log is null
            vm.ReprintReceiptCommand.Execute(null);
            Assert.Equal("Không tìm thấy giao dịch gần đây để in lại.", vm.StatusMessage);
        }
    }
}
