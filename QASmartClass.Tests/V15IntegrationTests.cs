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
    public class V15IntegrationTests
    {
        static V15IntegrationTests()
        {
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();

            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.24.0");
            QASmartClass.Staff.Services.ServiceRegistration.Initialize();
        }

        [Fact]
        public async Task TestKitchenDashboard_SaveMenu_WritesAuditLogAndExecutesCleanly()
        {
            var prevUser = QASmartClass.Staff.Services.StaffSession.CurrentUser;
            QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { FullName = "Teacher Log", Role = "GV", IsActive = true });

            using (var db = new AppDbContext())
            {
                string uniqueItems = "Test Menu Items " + Guid.NewGuid().ToString("N").Substring(0, 6);
                var vm = new KitchenDashboardViewModel();
                vm.MenuItems = uniqueItems;
                vm.MenuDate = DateTime.Today;
                vm.SelectedMealType = "Lunch";
                vm.NutritionInfo = "Test Nutrition";
                vm.Allergens = "Test Allergen";

                try
                {
                    // Execute SaveCommand (SaveMenuAsync) headlessly
                    await vm.SaveMenuCommand.ExecuteAsync(null);

                    using var verifyDb = new AppDbContext();
                    var audit = await verifyDb.AuditLogs
                        .AsNoTracking()
                        .FirstOrDefaultAsync(l => l.Action == "Save_SchoolMenu" && l.Details.Contains(uniqueItems));
                    Assert.NotNull(audit);

                    // Cleanup
                    var menu = await verifyDb.SchoolMenus.FirstOrDefaultAsync(m => m.Items == uniqueItems);
                    if (menu != null) verifyDb.SchoolMenus.Remove(menu);
                    verifyDb.AuditLogs.Remove(audit);
                    try { await verifyDb.SaveChangesAsync(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V15IntegrationTests] DB save error: {ex.Message}"); throw; }
                }
                finally
                {
                    if (prevUser != null)
                        QASmartClass.Staff.Services.StaffSession.Login(prevUser);
                    else
                        QASmartClass.Staff.Services.StaffSession.Logout();
                }
            }
        }

        [Fact]
        public async Task TestDocumentManager_SaveDocument_LogsWithTeacherCode()
        {
            using (var db = new AppDbContext())
            {
                string uniqueTitle = "Doc Title " + Guid.NewGuid().ToString("N").Substring(0, 6);
                string uniqueNum = "NUM-" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();

                var profile = new TeacherProfile
                {
                    FullName = "Van Thu Test",
                    TeacherCode = "TC_VT_TEST",
                    TeacherPassword = "123",
                    Role = "GV",
                    IsActive = true
                };
                db.TeacherProfiles.Add(profile);
                try { await db.SaveChangesAsync(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V15IntegrationTests] DB save error: {ex.Message}"); throw; }

                try
                {
                    QASmartClass.Staff.Services.StaffSession.Login(profile);
                    
                    var vm = new DocumentManagerViewModel();
                    vm.DocNumber = uniqueNum;
                    vm.Title = uniqueTitle;
                    vm.SelectedType = "Incoming";
                    vm.Recipient = "BGH";

                    await vm.SaveCommand.ExecuteAsync(null);

                    using var verifyDb = new AppDbContext();
                    var audit = await verifyDb.AuditLogs
                        .AsNoTracking()
                        .FirstOrDefaultAsync(l => l.Action == "Upload_Document" && l.ActorName == "TC_VT_TEST" && l.Details.Contains(uniqueTitle));
                    Assert.NotNull(audit);

                    // Cleanup
                    var doc = await verifyDb.OfficialDocuments.FirstOrDefaultAsync(d => d.DocumentNumber == uniqueNum);
                    if (doc != null) verifyDb.OfficialDocuments.Remove(doc);
                    verifyDb.AuditLogs.Remove(audit);
                    try { await verifyDb.SaveChangesAsync(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V15IntegrationTests] DB save error: {ex.Message}"); throw; }
                }
                finally
                {
                    using var cleanupDb = new AppDbContext();
                    var prof = await cleanupDb.TeacherProfiles.FirstOrDefaultAsync(t => t.TeacherCode == "TC_VT_TEST");
                    if (prof != null) cleanupDb.TeacherProfiles.Remove(prof);
                    await cleanupDb.SaveChangesAsync();
                    QASmartClass.Staff.Services.StaffSession.Logout();
                }
            }
        }
    }
}
