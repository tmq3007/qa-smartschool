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
    public class V31IntegrationTests
    {
        static V31IntegrationTests()
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
            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.31.0");
        }

        [Fact]
        public void TestDynamicRbac_LoadsPermissionsFromDbOnly()
        {
            // Verify that we can query RolePermissions and it has seeded rows from the migration 5.31.0
            using (var db = new AppDbContext())
            {
                var bepPermissions = db.RolePermissions
                    .Where(rp => rp.Role == "Bep")
                    .Select(rp => rp.PermissionTag)
                    .ToList();

                Assert.Contains("Canteen", bepPermissions);
                Assert.Contains("kitchen", bepPermissions);
                Assert.Contains("food_safety", bepPermissions);
                Assert.Equal(3, bepPermissions.Count);
            }
        }

        [Fact]
        public async Task TestCanteenPos_AllergyWarning_LocksPaymentUntilConfirmed()
        {
            using (var db = new AppDbContext())
            {
                // Clear any existing test student
                var existingStudent = db.Students.FirstOrDefault(s => s.StudentCode == "HS_V31_ALLERGY");
                if (existingStudent != null)
                {
                    db.Students.Remove(existingStudent);
                }
                var existingAllergy = db.FoodAllergies.FirstOrDefault(a => a.StudentCode == "HS_V31_ALLERGY");
                if (existingAllergy != null)
                {
                    db.FoodAllergies.Remove(existingAllergy);
                }
                db.SaveChanges();

                // Create a test student
                var student = new Student
                {
                    StudentCode = "HS_V31_ALLERGY",
                    FullName = "Test V31 Allergy Student",
                    ClassName = "12A1",
                    WalletBalance = 50000,
                    LowBalanceThreshold = 10000,
                    Status = "Active"
                };
                db.Students.Add(student);

                // Create a food allergy record
                var allergy = new FoodAllergy
                {
                    StudentCode = "HS_V31_ALLERGY",
                    StudentName = "Test V31 Allergy Student",
                    Allergen = "Đậu phộng",
                    Severity = "Severe",
                    ActionPlan = "Dùng EpiPen"
                };
                db.FoodAllergies.Add(allergy);
                db.SaveChanges();

                try
                {
                    var vm = new CanteenPosViewModel();
                    await vm.InitializeAsync();

                    vm.SearchCode = "HS_V31_ALLERGY";
                    await vm.SearchStudentCommand.ExecuteAsync(null);

                    // Verify student was found and allergy warning popped up
                    Assert.NotNull(vm.CurrentStudent);
                    Assert.True(vm.IsAllergyOverlayOpen, "Allergy overlay should automatically open upon searching student with severe allergy.");
                    Assert.Contains("⚠ CẢNH BÁO DỊ ỨNG", vm.AllergyWarning);

                    // Try to process payment without confirming allergy
                    vm.DeductionAmount = 25000;
                    vm.OrderDetails = "Bữa trưa";
                    await vm.ProcessPaymentCommand.ExecuteAsync(null);

                    // Verify payment was blocked (wallet balance should still be 50,000)
                    using (var checkDb = new AppDbContext())
                    {
                        var studentCheck = checkDb.Students.First(s => s.StudentCode == "HS_V31_ALLERGY");
                        Assert.Equal(50000, studentCheck.WalletBalance);
                    }
                    Assert.Contains("xác nhận đã kiểm tra dị ứng", vm.StatusMessage);

                    // Confirm the allergy
                    vm.ConfirmAllergyCommand.Execute(null);
                    Assert.False(vm.IsAllergyOverlayOpen, "Overlay should close after confirmation.");

                    // Process payment again
                    var mockUi = (MockUserInterfaceService)AppServices.UIService;
                    mockUi.ConfirmResult = true;

                    await vm.ProcessPaymentCommand.ExecuteAsync(null);

                    // Verify payment succeeded (wallet balance should be 25,000)
                    using (var checkDb = new AppDbContext())
                    {
                        var studentCheck = checkDb.Students.First(s => s.StudentCode == "HS_V31_ALLERGY");
                        Assert.Equal(25000, studentCheck.WalletBalance);
                    }
                }
                finally
                {
                    // Cleanup
                    var sDel = db.Students.FirstOrDefault(s => s.StudentCode == "HS_V31_ALLERGY");
                    if (sDel != null) db.Students.Remove(sDel);
                    var aDel = db.FoodAllergies.FirstOrDefault(a => a.StudentCode == "HS_V31_ALLERGY");
                    if (aDel != null) db.FoodAllergies.Remove(aDel);
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public void TestCanteenPos_MockReceiptPrinting()
        {
            var oldPrintService = AppServices.PrintService;
            var mockPrint = new MockReceiptPrintService();
            AppServices.PrintService = mockPrint;

            try
            {
                var vm = new CanteenPosViewModel();
                var log = new EventLog
                {
                    EventType = "WalletTransaction",
                    Actor = "HS_TEST",
                    Timestamp = DateTime.Now,
                    Details = "{\"Amount\":20000,\"BalanceAfter\":30000,\"Items\":\"Bữa sáng\",\"Cashier\":\"GV_TEST\"}"
                };

                vm.PrintReceipt(log);

                Assert.True(mockPrint.PrintReceiptCalled);
                Assert.Contains("BIÊN LAI CANTEEN", mockPrint.LastPrintedText);
                Assert.Contains("SỐ DƯ MỚI: 30,000", mockPrint.LastPrintedText);
            }
            finally
            {
                AppServices.PrintService = oldPrintService;
            }
        }
    }

    public class MockReceiptPrintService : IReceiptPrintService
    {
        public bool PrintReceiptCalled { get; private set; }
        public string LastPrintedText { get; private set; } = string.Empty;

        public void PrintReceipt(string receiptText)
        {
            PrintReceiptCalled = true;
            LastPrintedText = receiptText;
        }
    }
}
