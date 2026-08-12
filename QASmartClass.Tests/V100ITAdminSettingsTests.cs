using Xunit;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using QASmartClass.Services;
using QASmartClass.Data;
using QASmartTouch.Services;

namespace QASmartClass.Tests
{
    [Collection("Sequential")]
    public class V100ITAdminSettingsTests
    {
        // 1. IP Validation logic test
        [Fact]
        public void Test_IPAddressValidation()
        {
            string validIp1 = "192.168.1.100";
            string validIp2 = "127.0.0.1";
            string invalidIp1 = "256.100.200.300";
            string invalidIp2 = "192.168.1";
            string invalidIp3 = "abc.def.ghi.jkl";

            Assert.True(IsValidIp(validIp1));
            Assert.True(IsValidIp(validIp2));
            Assert.False(IsValidIp(invalidIp1));
            Assert.False(IsValidIp(invalidIp2));
            Assert.False(IsValidIp(invalidIp3));
        }

        private bool IsValidIp(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return false;
            var parts = ip.Split('.');
            return parts.Length == 4 && parts.All(p => int.TryParse(p, out int val) && val >= 0 && val <= 255);
        }

        // 2. Hex color format and contrast/luminance test
        [Fact]
        public void Test_HexColorAndContrastValidation()
        {
            string validHex1 = "#1976D2"; // Blue - dark
            string validHex2 = "#9C27B0"; // Purple - dark
            string invalidHex1 = "1976D2"; // Missing '#'
            string invalidHex2 = "#XYZ123"; // Non-hex chars
            string invalidHex3 = "#FFFFFF"; // Too bright - should fail luminance contrast test (L > 0.8)
            string invalidHex4 = "#FFFF55"; // Too bright (Yellow) - should fail contrast test

            // Test regex pattern matching
            Assert.True(Regex.IsMatch(validHex1, "^#[0-9A-Fa-f]{6}$"));
            Assert.True(Regex.IsMatch(validHex2, "^#[0-9A-Fa-f]{6}$"));
            Assert.False(Regex.IsMatch(invalidHex1, "^#[0-9A-Fa-f]{6}$"));
            Assert.False(Regex.IsMatch(invalidHex2, "^#[0-9A-Fa-f]{6}$"));
            Assert.True(Regex.IsMatch(invalidHex3, "^#[0-9A-Fa-f]{6}$"));

            // Test relative luminance contrast check
            Assert.True(IsContrastAcceptable(validHex1));
            Assert.True(IsContrastAcceptable(validHex2));
            Assert.False(IsContrastAcceptable(invalidHex3)); // White (#FFFFFF)
            Assert.False(IsContrastAcceptable(invalidHex4)); // Yellow (#FFFF55)
        }

        private bool IsContrastAcceptable(string hexColor)
        {
            try
            {
                if (string.IsNullOrEmpty(hexColor) || hexColor.Length != 7 || hexColor[0] != '#')
                    return false;
                    
                int rVal = Convert.ToInt32(hexColor.Substring(1, 2), 16);
                int gVal = Convert.ToInt32(hexColor.Substring(3, 2), 16);
                int bVal = Convert.ToInt32(hexColor.Substring(5, 2), 16);
                
                double r = rVal / 255.0;
                double g = gVal / 255.0;
                double b = bVal / 255.0;
                double l = 0.2126 * r + 0.7152 * g + 0.0722 * b;
                return l <= 0.8; // Reject values > 0.8
            }
            catch
            {
                return false;
            }
        }

        // 3. Backup Time validation logic test
        [Fact]
        public void Test_BackupTimeValidation()
        {
            string validTime1 = "23:00";
            string validTime2 = "00:00";
            string validTime3 = "12:34";
            string invalidTime1 = "25:00";
            string invalidTime2 = "9:30";
            string invalidTime3 = "12:60";
            string invalidTime4 = "abc";

            Assert.True(Regex.IsMatch(validTime1, "^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$"));
            Assert.True(Regex.IsMatch(validTime2, "^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$"));
            Assert.True(Regex.IsMatch(validTime3, "^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$"));
            Assert.False(Regex.IsMatch(invalidTime1, "^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$"));
            Assert.False(Regex.IsMatch(invalidTime2, "^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$"));
            Assert.False(Regex.IsMatch(invalidTime3, "^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$"));
            Assert.False(Regex.IsMatch(invalidTime4, "^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$"));
        }

        // 4. DbSizeCheckService integration test logic
        [Fact]
        public void Test_DbSizeCheckService_DefaultsAndLimitCheck()
        {
            // Override root dir to a temp directory for safe isolated unit testing
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            AppPaths.DataDirOverride = tempDir;
            AppPaths.EnsureDirectories();
            DbEncryptionKeyManager.ClearCache();

            try
            {
                using (var db = new AppDbContext())
                {
                    db.Database.EnsureCreated();
                    // Ensure default values are in DB or service behaves properly
                    var service = new DbSizeCheckService(db);
                    var status = service.CheckDbSize();

                    // Verification
                    Assert.NotNull(status);
                    Assert.True(status.LimitMb >= 10);
                    Assert.Contains(status.Action, new[] { "WarnOnly", "AutoClean" });
                    Assert.True(status.CurrentSizeMb >= 0);
                }
            }
            finally
            {
                // Clean up temp directory
                try
                {
                    if (System.IO.Directory.Exists(tempDir))
                    {
                        System.IO.Directory.Delete(tempDir, true);
                    }
                }
                catch { }
                AppPaths.DataDirOverride = null;
                DbEncryptionKeyManager.ClearCache();
            }
        }

        // 5. AppSettings ShowInactivePackagesMode and RunningMode serialization/deserialization test
        [Fact]
        public void Test_AppSettings_ShowInactivePackagesMode_Serialization()
        {
            // Override root dir to a temp directory for safe isolated unit testing
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            AppPaths.DataDirOverride = tempDir;
            AppPaths.EnsureDirectories();

            try
            {
                // Set and save settings
                AppSettings.RunningMode = "Test";
                DbEncryptionKeyManager.ClearCache();
                AppSettings.ShowInactivePackagesMode = "LockedUpsell";
                AppSettings.ActiveUserRole = "Teacher";
                AppSettings.Save();

                // Clear memory state by resetting
                AppSettings.RunningMode = "Release";
                DbEncryptionKeyManager.ClearCache();
                AppSettings.ShowInactivePackagesMode = "Hidden";
                AppSettings.ActiveUserRole = "All";

                // Load from file
                AppSettings.Load();

                // Assertions
                Assert.Equal("Test", AppSettings.RunningMode);
                Assert.Equal("LockedUpsell", AppSettings.ShowInactivePackagesMode);
                Assert.Equal("Teacher", AppSettings.ActiveUserRole);
            }
            finally
            {
                // Clean up temp directory
                try
                {
                    if (System.IO.Directory.Exists(tempDir))
                    {
                        System.IO.Directory.Delete(tempDir, true);
                    }
                }
                catch { }
                AppPaths.DataDirOverride = null;
                DbEncryptionKeyManager.ClearCache();
            }
        }

        // 6. AppPaths DatabaseFile path isolation test based on RunningMode
        [Fact]
        public void Test_AppPaths_DatabaseIsolation_BasedOnRunningMode()
        {
            // Release mode check
            AppSettings.RunningMode = "Release";
            DbEncryptionKeyManager.ClearCache();
            string dbFileRelease = AppPaths.DatabaseFile;
            Assert.EndsWith("smartclass.db", dbFileRelease);

            // Test mode check
            AppSettings.RunningMode = "Test";
            DbEncryptionKeyManager.ClearCache();
            string dbFileTest = AppPaths.DatabaseFile;
            Assert.EndsWith("smartclass_test.db", dbFileTest);

            // Reset back
            AppSettings.RunningMode = "Release";
            DbEncryptionKeyManager.ClearCache();
        }

        // 7. Test Timetable Serialization and Deserialization
        [Fact]
        public void Test_Timetable_Config_Serialization_Deserialization_Test()
        {
            var periods = new[]
            {
                new { Period = 1, Start = "07:30", End = "08:15" },
                new { Period = 2, Start = "08:20", End = "09:05" }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(periods);
            Assert.Contains("\"Period\":1", json);
            Assert.Contains("\"Start\":\"07:30\"", json);

            using (var doc = System.Text.Json.JsonDocument.Parse(json))
            {
                var arr = doc.RootElement.EnumerateArray().ToList();
                Assert.Equal(2, arr.Count);
                Assert.Equal(1, arr[0].GetProperty("Period").GetInt32());
                Assert.Equal("07:30", arr[0].GetProperty("Start").GetString());
                Assert.Equal("08:15", arr[0].GetProperty("End").GetString());
            }
        }

        // 8. Test Timetable Time Overlap and Validation logic
        [Fact]
        public void Test_Timetable_TimeOverlapValidation_Test()
        {
            // Valid timetable
            var validPeriods = new[]
            {
                new { Period = 1, Start = "07:30", End = "08:15" },
                new { Period = 2, Start = "08:20", End = "09:05" },
                new { Period = 3, Start = "09:15", End = "10:00" }
            };
            Assert.True(ValidateTimetable(validPeriods));

            // Invalid timetable: End before Start
            var invalidPeriods1 = new[]
            {
                new { Period = 1, Start = "08:15", End = "07:30" }
            };
            Assert.False(ValidateTimetable(invalidPeriods1));

            // Invalid timetable: Overlap with previous period
            var invalidPeriods2 = new[]
            {
                new { Period = 1, Start = "07:30", End = "08:15" },
                new { Period = 2, Start = "08:10", End = "08:55" } // Starts before Tiết 1 ends
            };
            Assert.False(ValidateTimetable(invalidPeriods2));

            // Invalid format
            var invalidPeriods3 = new[]
            {
                new { Period = 1, Start = "7:30", End = "08:15" } // Missing leading zero
            };
            Assert.False(ValidateTimetable(invalidPeriods3));
        }

        private bool ValidateTimetable(dynamic[] periods)
        {
            var timeRegex = new Regex("^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$");
            TimeSpan lastEndTime = TimeSpan.Zero;

            for (int i = 0; i < periods.Length; i++)
            {
                string startStr = periods[i].Start;
                string endStr = periods[i].End;

                if (!timeRegex.IsMatch(startStr) || !timeRegex.IsMatch(endStr))
                    return false;

                var start = TimeSpan.Parse(startStr);
                var end = TimeSpan.Parse(endStr);

                if (end <= start)
                    return false;

                if (i > 0)
                {
                    if (start < lastEndTime)
                        return false;
                }

                lastEndTime = end;
            }

            return true;
        }

        // 9. Test Lesson Approval Settings Database read/write
        [Fact]
        public void Test_LessonApprovalSettings_Test()
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            AppPaths.DataDirOverride = tempDir;
            AppPaths.EnsureDirectories();
            DbEncryptionKeyManager.ClearCache();

            try
            {
                using (var db = new AppDbContext())
                {
                    db.Database.EnsureCreated();

                    // Save settings
                    var requiredSetting = new SystemSetting { Id = "LessonApproval_Required", Value = "true", Category = "LessonApproval", LastUpdated = DateTime.Now };
                    var roleSetting = new SystemSetting { Id = "LessonApproval_ApproverRole", Value = "SchoolAdminOrSubjectHead", Category = "LessonApproval", LastUpdated = DateTime.Now };
                    
                    db.SystemSettings.Add(requiredSetting);
                    db.SystemSettings.Add(roleSetting);
                    db.SaveChanges();
                }

                using (var db = new AppDbContext())
                {
                    var required = db.SystemSettings.Find("LessonApproval_Required");
                    var role = db.SystemSettings.Find("LessonApproval_ApproverRole");

                    Assert.NotNull(required);
                    Assert.Equal("true", required.Value);
                    Assert.NotNull(role);
                    Assert.Equal("SchoolAdminOrSubjectHead", role.Value);
                }
            }
            finally
            {
                try
                {
                    if (System.IO.Directory.Exists(tempDir))
                    {
                        System.IO.Directory.Delete(tempDir, true);
                    }
                }
                catch { }
                AppPaths.DataDirOverride = null;
                DbEncryptionKeyManager.ClearCache();
            }
        
                }

// 10. Test Kiosk Stream Fallback Mode Settings read/write
        [Fact]
        public void Test_KioskStreamFallbackMode_Settings()
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            AppPaths.DataDirOverride = tempDir;
            AppPaths.EnsureDirectories();
            DbEncryptionKeyManager.ClearCache();

            try
            {
                using (var db = new AppDbContext())
                {
                    db.Database.EnsureCreated();

                    // Save setting
                    var setting = new SystemSetting 
                    { 
                        Id = "Kiosk_StreamFallbackMode", 
                        Value = "HttpOnly", 
                        Category = "KIOSK", 
                        LastUpdated = DateTime.Now 
                    };

                    db.SystemSettings.Add(setting);
                    db.SaveChanges();
                }

                using (var db = new AppDbContext())
                {
                    var reloaded = db.SystemSettings.Find("Kiosk_StreamFallbackMode");
                    Assert.NotNull(reloaded);
                    Assert.Equal("HttpOnly", reloaded.Value);
                    
                    // Validate allowed values logic
                    string[] allowedValues = { "AutoFallback", "UdpOnly", "HttpOnly" };
                    Assert.Contains(reloaded.Value, allowedValues);
                }
            }
            finally
            {
                try
                {
                    if (System.IO.Directory.Exists(tempDir))
                    {
                        System.IO.Directory.Delete(tempDir, true);
                    }
                }
                catch { }
                AppPaths.DataDirOverride = null;
                DbEncryptionKeyManager.ClearCache();
            }
        }
    }
}
