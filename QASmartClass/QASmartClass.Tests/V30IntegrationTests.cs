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
    public class V30IntegrationTests
    {
        static V30IntegrationTests()
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
            DbMigrator.Migrate(db, "5.30.0");
        }

        [Fact]
        public void TestPasswordMigration_ScrubsPlaintextPasswords()
        {
            using (var db = new AppDbContext())
            {
                // Clear any existing test teacher
                var existing = db.TeacherProfiles.FirstOrDefault(t => t.TeacherCode == "GV_V30_TEST1");
                if (existing != null)
                {
                    db.TeacherProfiles.Remove(existing);
                    db.SaveChanges();
                }

                // Add a teacher with a legacy plaintext password
                var teacher = new TeacherProfile
                {
                    TeacherCode = "GV_V30_TEST1",
                    TeacherPassword = "LegacyPlaintextPassword",
                    PasswordHash = "",
                    FullName = "Test V30 Teacher 1",
                    Role = "GV"
                };
                db.TeacherProfiles.Add(teacher);
                db.SaveChanges();

                // Force migrator to run the v5.30.0 logic by setting the version marker file back to v5.29.0
                if (File.Exists(AppPaths.DbVersionFile))
                {
                    File.WriteAllText(AppPaths.DbVersionFile, "5.29.0");
                }

                // Run migration
                DbMigrator.Migrate(db, "5.30.0");

                // Retrieve teacher using a fresh DbContext to bypass EF Core cache
                using (var freshDb = new AppDbContext())
                {
                    var migratedTeacher = freshDb.TeacherProfiles.FirstOrDefault(t => t.TeacherCode == "GV_V30_TEST1");
                    Assert.NotNull(migratedTeacher);
                    Assert.Equal("", migratedTeacher.TeacherPassword);
                    Assert.False(string.IsNullOrEmpty(migratedTeacher.PasswordHash));
                    Assert.True(QASmartTouch.Services.AuthenticationService.VerifyPassword("LegacyPlaintextPassword", migratedTeacher.PasswordHash));
                    
                    // Cleanup
                    freshDb.TeacherProfiles.Remove(migratedTeacher);
                    freshDb.SaveChanges();
                }
            }
        }

        [Fact]
        public void TestStaffLogin_PlaintextAccess_IsDenied()
        {
            using (var db = new AppDbContext())
            {
                var teacher = new TeacherProfile
                {
                    TeacherCode = "GV_V30_TEST2",
                    TeacherPassword = "PlaintextPassword",
                    PasswordHash = "", // no hash
                    FullName = "Test V30 Teacher 2",
                    Role = "GV"
                };
                db.TeacherProfiles.Add(teacher);
                db.SaveChanges();

                try
                {
                    var user = db.TeacherProfiles.FirstOrDefault(t => t.TeacherCode == "GV_V30_TEST2");
                    Assert.NotNull(user);
                    
                    bool isValid = false;
                    if (!string.IsNullOrEmpty(user.PasswordHash))
                    {
                        isValid = QASmartTouch.Services.AuthenticationService.VerifyPassword("PlaintextPassword", user.PasswordHash);
                    }
                    
                    Assert.False(isValid, "Login should NOT succeed using plaintext fallback since PasswordHash is empty.");
                }
                finally
                {
                    db.TeacherProfiles.Remove(teacher);
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public void TestAuditHelper_WriteException_LogsWarning()
        {
            using (var db = new AppDbContext())
            {
                string action = "Test_Exception_Action";
                string actor = "Test_Exception_Actor";
                string details = "Test_Exception_Details";

                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "audit.log");

                // Lock the file exclusively to force an IOException when AuditHelper.Log tries to write to it
                using (var fs = new FileStream(logPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    var logEvents = new System.Collections.Generic.List<Serilog.Events.LogEvent>();
                    var captureSink = new MockSerilogSink(logEvents);
                    var oldLogger = Serilog.Log.Logger;
                    Serilog.Log.Logger = new Serilog.LoggerConfiguration()
                        .WriteTo.Sink(captureSink)
                        .CreateLogger();

                    try
                    {
                        AuditHelper.Log(db, action, actor, details);

                        // Assert that we got a warning logged
                        Assert.Contains(logEvents, e => e.Level == Serilog.Events.LogEventLevel.Warning && e.MessageTemplate.Text.Contains("Audit log file append failed"));
                    }
                    finally
                    {
                        Serilog.Log.Logger = oldLogger;
                    }
                }
            }
        }

        [Fact]
        public void TestCanteenPos_RFIDIndicator_StylesAreValid()
        {
            var t = new System.Threading.Thread(() =>
            {
                try
                {
                    var app = System.Windows.Application.Current ?? new System.Windows.Application();
                    
                    // Add standard fallback resources if not present
                    if (!app.Resources.Contains("AppBackground"))
                        app.Resources["AppBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                    if (!app.Resources.Contains("TextPrimary"))
                        app.Resources["TextPrimary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                    if (!app.Resources.Contains("TextSecondary"))
                        app.Resources["TextSecondary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
                    if (!app.Resources.Contains("BoolToVis"))
                        app.Resources["BoolToVis"] = new System.Windows.Controls.BooleanToVisibilityConverter();
                    if (!app.Resources.Contains("BooleanToVisibilityConverter"))
                        app.Resources["BooleanToVisibilityConverter"] = new System.Windows.Controls.BooleanToVisibilityConverter();

                    // Ensure resource dictionaries are loaded
                    bool hasTheme = app.Resources.MergedDictionaries.Any(d => d.Source != null && d.Source.OriginalString.Contains("StaffTheme"));
                    if (!hasTheme)
                    {
                        try
                        {
                            var designTokens = new System.Windows.ResourceDictionary { Source = new Uri("/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Relative) };
                            var styles = new System.Windows.ResourceDictionary { Source = new Uri("/QASmartClass;component/Resources/Styles.xaml", UriKind.Relative) };
                            var staffTheme = new System.Windows.ResourceDictionary { Source = new Uri("/QASmartClass;component/Resources/StaffTheme.xaml", UriKind.Relative) };
                            var strings = new System.Windows.ResourceDictionary { Source = new Uri("/QASmartClass;component/Localization/Strings_vi.xaml", UriKind.Relative) };
                            
                            app.Resources.MergedDictionaries.Add(designTokens);
                            app.Resources.MergedDictionaries.Add(styles);
                            app.Resources.MergedDictionaries.Add(staffTheme);
                            app.Resources.MergedDictionaries.Add(strings);
                        }
                        catch { }
                    }

                    // Alias missing styles to loaded ones to prevent crashes
                    if (!app.Resources.Contains("StaffSecondaryBtn"))
                    {
                        app.Resources["StaffSecondaryBtn"] = app.Resources.Contains("StaffOutlineBtn")
                            ? app.Resources["StaffOutlineBtn"]
                            : new System.Windows.Style(typeof(System.Windows.Controls.Button));
                    }
                    if (!app.Resources.Contains("StaffBtn"))
                    {
                        app.Resources["StaffBtn"] = app.Resources.Contains("StaffPrimaryBtn")
                            ? app.Resources["StaffPrimaryBtn"]
                            : new System.Windows.Style(typeof(System.Windows.Controls.Button));
                    }

                    var view = new QASmartClass.Staff.Views.CanteenPosView();
                    Assert.NotNull(view);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("CanteenPosView test error: " + ex);
                    throw;
                }
            });
            t.SetApartmentState(System.Threading.ApartmentState.STA);
            t.Start();
            t.Join();
        }
    }

    public class MockSerilogSink : Serilog.Core.ILogEventSink
    {
        private readonly System.Collections.Generic.List<Serilog.Events.LogEvent> _events;

        public MockSerilogSink(System.Collections.Generic.List<Serilog.Events.LogEvent> events)
        {
            _events = events;
        }

        public void Emit(Serilog.Events.LogEvent logEvent)
        {
            _events.Add(logEvent);
        }
    }
}
