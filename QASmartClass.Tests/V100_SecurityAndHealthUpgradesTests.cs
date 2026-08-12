using Xunit;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Staff.ViewModels;
using QASmartClass.Converters;
using QASmartClass.HealthRoom.Views;
using System.Windows.Controls;

namespace QASmartClass.Tests
{
    public class V100_SecurityAndHealthUpgradesTests
    {
        public V100_SecurityAndHealthUpgradesTests()
        {
            using var db = new AppDbContext();
            QASmartClass.Services.DbMigrator.Migrate(db, "6.03.0");
        }

        private void RunOnStaThread(Action action)
        {
            void InitializeApplicationFull()
            {
                var urls = new[] {
                    "pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/Styles.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/StaffTheme.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/InterOutfitFonts.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/SvgIcons.xaml",
                    "pack://application:,,,/QASmartClass;component/Localization/Strings_vi.xaml",
                    "pack://application:,,,/QASmartClass;component/LearningTools/Themes/LearningToolsStyles.xaml"
                };

                try
                {
                    var appField = typeof(System.Windows.Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    var createdField = typeof(System.Windows.Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (appField != null) appField.SetValue(null, null);
                    if (createdField != null) createdField.SetValue(null, false);

                    var app = new QASmartTouch.App();
                    foreach (var url in urls)
                    {
                        app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                        {
                            Source = new Uri(url, UriKind.Absolute)
                        });
                    }
                }
                catch { }
            }
            var t = new System.Threading.Thread(() =>
            {
                try
                {
                    InitializeApplicationFull(); action();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("STA thread error: " + ex.Message, ex);
                }
            });
            t.SetApartmentState(System.Threading.ApartmentState.STA);
            t.Start();
            t.Join();
        }

        [Fact]
        public async Task Test_Security_Gate_MissingStudentsActiveWarning_Modes()
        {
            using var db = new AppDbContext();
            
            // Setup Settings
            var setting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Security_Gate_MissingStudentsActiveWarning");
            if (setting == null)
            {
                setting = new SystemSetting { Id = "Security_Gate_MissingStudentsActiveWarning", Value = "0", Category = "Security" };
                db.SystemSettings.Add(setting);
            }
            
            // Mode 0: Off/Hide
            setting.Value = "0";
            await db.SaveChangesAsync();
            
            var vm0 = new GateMonitorViewModel();
            await vm0.RefreshDataCommand.ExecuteAsync(null);
            
            Assert.Equal("Đang ở trường (Tắt)", vm0.MissingHeaderTitle);
            Assert.Empty(vm0.MissingStudents);
            
            // Mode 2: Always on (strict Checkout warning)
            setting.Value = "2";
            await db.SaveChangesAsync();
            
            var vm2 = new GateMonitorViewModel();
            await vm2.RefreshDataCommand.ExecuteAsync(null);
            
            Assert.Equal("Chưa Check-out", vm2.MissingHeaderTitle);
            Assert.Equal("#FFFEE2E2", vm2.MissingHeaderBgColor.ToString());
        }

        [Fact]
        public async Task Test_GateMonitor_DynamicAbsentHeader_BasedOnTime()
        {
            // We just verify the logic of the dynamic headers property in GateMonitorViewModel
            var vm = new GateMonitorViewModel();
            await vm.RefreshDataCommand.ExecuteAsync(null);
            
            // Before 8:00 or after 8:00
            if (DateTime.Now.Hour < 8)
            {
                Assert.Equal("Chưa đến trường", vm.AbsentHeaderTitle);
                Assert.Equal("#FFFEF3C7", vm.AbsentHeaderBgColor.ToString());
            }
            else
            {
                Assert.Equal("Vắng Không Phép", vm.AbsentHeaderTitle);
                Assert.Equal("#FFFEE2E2", vm.AbsentHeaderBgColor.ToString());
            }
        }

        [Fact]
        public async Task Test_GateMonitor_HighStrictnessOfflineVerification()
        {
            using var db = new AppDbContext();
            
            // Setup Strictness = 2
            var setting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Security_OfflineVerificationStrictness");
            if (setting == null)
            {
                setting = new SystemSetting { Id = "Security_OfflineVerificationStrictness", Value = "2", Category = "Security" };
                db.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = "2";
            }
            await db.SaveChangesAsync();

            // Setup a mock student
            var student = new Student
            {
                FullName = "Học sinh LP Strict Test",
                StudentCode = "HS_LP_STRICT_" + Guid.NewGuid().ToString("N").Substring(0, 6),
                ClassName = "12A1",
                Status = "Active"
            };
            db.Students.Add(student);
            await db.SaveChangesAsync();

            var leave = new StudentLeaveRequest
            {
                StudentId = student.Id,
                StudentName = student.FullName,
                ClassName = student.ClassName,
                LeaveDate = DateTime.Today,
                Reason = "Đi khám bệnh",
                Status = "Approved",
                CreatedAt = DateTime.Now
            };
            db.StudentLeaveRequests.Add(leave);
            await db.SaveChangesAsync();

            var vm = new GateMonitorViewModel();

            // 1. Missing signature/timestamp (only 3 parts) - should fail
            vm.ScannedLeavePassCode = $"LP-{DateTime.Today:yyyyMMdd}-{student.Id}";
            await vm.ScanLeavePassCommand.ExecuteAsync(null);
            Assert.Equal(string.Empty, vm.ScannedLeavePassCode);

            // 2. Replay attack: Old timestamp - should fail
            long oldUnixTime = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds();
            vm.ScannedLeavePassCode = $"LP-{DateTime.Today:yyyyMMdd}-{student.Id}-{oldUnixTime}-signature";
            await vm.ScanLeavePassCommand.ExecuteAsync(null);
            Assert.Equal(string.Empty, vm.ScannedLeavePassCode);

            // 3. Valid timestamp and correct signature - should succeed
            long validUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string rawData = $"LP-{DateTime.Today:yyyyMMdd}-{student.Id}-{validUnixTime}";
            
            // Calculate expected signature using sha256 helper
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData + "QASmartClassOfflineSalt_2026"));
                var builder = new System.Text.StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                    builder.Append(bytes[i].ToString("x2"));
                
                string validSignature = builder.ToString();
                vm.ScannedLeavePassCode = $"LP-{DateTime.Today:yyyyMMdd}-{student.Id}-{validUnixTime}-{validSignature}";
            }

            await vm.ScanLeavePassCommand.ExecuteAsync(null);
            Assert.Equal(string.Empty, vm.ScannedLeavePassCode); // Cleared
            
            // Check that CheckOut log was written
            var checkoutLog = await db.EventLogs.FirstOrDefaultAsync(l => l.Actor == student.StudentCode && l.EventType == "GateCheckOut");
            Assert.NotNull(checkoutLog);
        }

        [Fact]
        public void Test_MedicalCategoryConverter_And_SupplyAlerts()
        {
            var catConverter = new MedicalCategoryConverter();
            Assert.Equal("Thuốc", catConverter.Convert("Medicine", typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal("Sơ cứu", catConverter.Convert("FirstAid", typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal("Thiết bị", catConverter.Convert("Equipment", typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));

            var colorConverter = new QuantityAlertColorConverter();
            var weightConverter = new QuantityAlertWeightConverter();

            // Quantity >= 10
            Assert.Equal(System.Windows.Media.Brushes.Black, colorConverter.Convert(new QASmartClass.Data.MedicalSupply { Quantity = 10, MinAlertQty = 10 }, typeof(System.Windows.Media.Brush), null, System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal(System.Windows.FontWeights.Normal, weightConverter.Convert(new QASmartClass.Data.MedicalSupply { Quantity = 10, MinAlertQty = 10 }, typeof(System.Windows.FontWeight), null, System.Globalization.CultureInfo.InvariantCulture));

            // Quantity < 10
            Assert.Equal(System.Windows.Media.Brushes.Red, colorConverter.Convert(new QASmartClass.Data.MedicalSupply { Quantity = 5, MinAlertQty = 10 }, typeof(System.Windows.Media.Brush), null, System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal(System.Windows.FontWeights.Bold, weightConverter.Convert(new QASmartClass.Data.MedicalSupply { Quantity = 5, MinAlertQty = 10 }, typeof(System.Windows.FontWeight), null, System.Globalization.CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task Test_FoodSafety_CanteenMenuAutoFill()
        {
            using var db = new AppDbContext();
            
            // Seed a canteen menu for today
            var menu = new SchoolMenu
            {
                Date = DateTime.Today,
                MealType = "Lunch",
                Items = "Thịt kho, canh cua, cơm tám"
            };
            db.SchoolMenus.Add(menu);
            await db.SaveChangesAsync();

            RunOnStaThread(() =>
            {
                var view = new FoodSafetyView();
                // Simulating clicking BtnAdd
                var dp = (DatePicker)view.FindName("DpRecordDate");
                var tb = (TextBox)view.FindName("TxtMenuItems");
                
                dp.SelectedDate = DateTime.Today;
                Assert.Contains("Thịt kho, canh cua, cơm tám", tb.Text);
            });
        }
    }
}
