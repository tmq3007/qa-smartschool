using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Staff.Services;
using QASmartClass.Staff.ViewModels;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace QASmartClass.Tests
{
    public class V34IntegrationTests : IDisposable
    {
        private readonly string _tempDbPath;
        private readonly string _tempVersionPath;

        public V34IntegrationTests()
        {
            QASmartClass.Staff.Services.ServiceRegistration.Initialize();
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
        public async Task TestRecurringBooking_SavesMultipleWeeks_SkipsHolidaysAndConflicts()
        {
            using (var db = new AppDbContext())
            {
                // 1. Create a school asset
                var asset = new SchoolAsset
                {
                    AssetCode = "AST-V34-RECURRING",
                    AssetType = "Projector - Test Projector V34",
                    Status = "Active",
                    Location = "Phòng 303"
                };
                db.SchoolAssets.Add(asset);

                // 2. Set booking date to a Monday: 2026-06-08
                var bookingDate = new DateTime(2026, 6, 8); // Monday

                // 3. Create a holiday on the second week Monday (2026-06-15)
                var holiday = new SchoolEvent
                {
                    Title = "Nghỉ Tết Đoan Ngọ V34",
                    StartTime = new DateTime(2026, 6, 15, 8, 0, 0),
                    EndTime = new DateTime(2026, 6, 15, 17, 0, 0),
                    Description = "Sự kiện nghỉ lễ"
                };
                db.SchoolEvents.Add(holiday);

                // 4. Create an existing booking on the third week Monday (2026-06-22) for the same time slot
                var existingBooking = new SchoolAssetBooking
                {
                    AssetId = 1,
                    BookingDate = new DateTime(2026, 6, 22),
                    TimeSlot = 2,
                    BookedBy = "GV_OTHER"
                };
                db.SaveChanges(); // Save asset to get ID

                existingBooking.AssetId = asset.Id;
                db.AssetBookings.Add(existingBooking);
                db.SaveChanges();

                // 5. Instantiate VM
                var vm = new SchoolAssetManagementViewModel();
                await vm.InitializeAsync();

                vm.BookingAssetId = asset.Id;
                vm.BookingDate = bookingDate;
                vm.BookingTimeSlot = 2;
                vm.BookedBy = "GV_V34_TEST";
                vm.IsRecurring = true;
                vm.RecurringWeeks = 3;

                // UIService mock configuration
                var mockUi = (MockUserInterfaceService)AppServices.UIService;

                await vm.SaveBookingAsync();

                // Verify that only the first week booking (2026-06-08) was saved.
                // Week 2 (2026-06-15) is skipped because of holiday.
                // Week 3 (2026-06-22) is skipped because of conflict.
                var bookings = db.AssetBookings.Where(b => b.AssetId == asset.Id && b.BookedBy == "GV_V34_TEST").ToList();
                Assert.Single(bookings);
                Assert.Equal(new DateTime(2026, 6, 8), bookings[0].BookingDate.Date);

                // Verify that the UI warning messages contain skipped details
                Assert.NotNull(mockUi.LastMessage);
                Assert.Contains("Đăng ký mượn thành công 1 ngày học", mockUi.LastMessage);
                Assert.Contains("Nghỉ Tết Đoan Ngọ V34", mockUi.LastMessage);
                Assert.Contains("Trùng lịch mượn", mockUi.LastMessage);
            }
        }

        [Fact]
        public void TestSensitiveDataMaskingFormatter_MasksDataSuccessfully()
        {
            var formatter = new SensitiveDataMaskingFormatter("{Message}");
            
            // Format a LogEvent containing sensitive info
            var template = new MessageTemplateParser().Parse("Logging sensitive data: CCCD = 123456789012, RFID = 7E4A8F9C, số dư ví = 1,500,000");
            var properties = new System.Collections.Generic.List<LogEventProperty>();
            var logEvent = new LogEvent(
                DateTimeOffset.Now, 
                LogEventLevel.Information, 
                null, 
                template, 
                properties
            );

            using (var writer = new StringWriter())
            {
                formatter.Format(logEvent, writer);
                string result = writer.ToString();

                // Assertions
                Assert.Contains("XXXXXXXX9012", result); // CCCD masked
                Assert.Contains("7E4AXXXX", result);     // RFID masked
                Assert.Contains("số dư ví = X,XXX,XXX", result); // Balance masked
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
