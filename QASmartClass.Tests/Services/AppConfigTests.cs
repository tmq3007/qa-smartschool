using System;
using System.IO;
using Xunit;
using QASmartClass.Services;

namespace QASmartClass.Tests.Services
{
    public class AppConfigTests
    {
        [Fact]
        public void AppConfig_AtomicSaveAndLoad_WorksCorrectly()
        {
            // Load existing or default configuration
            var config = AppConfig.Load();
            Assert.NotNull(config);

            // Modify configuration values
            string originalSchoolName = config.SchoolName;
            string testSchoolName = "Test School THPT " + Guid.NewGuid().ToString();
            config.SchoolName = testSchoolName;

            // Save configuration
            config.Save();

            // Reload configuration
            var reloadedConfig = AppConfig.Load();
            Assert.Equal(testSchoolName, reloadedConfig.SchoolName);

            // Revert changes
            config.SchoolName = originalSchoolName;
            config.Save();
        }

        [Fact]
        public void AppConfig_AccentColor_SaveAndLoad_Works()
        {
            var config = AppConfig.Load();
            string originalAccent = config.AccentColor;
            string testAccent = "#FF9800";
            
            config.AccentColor = testAccent;
            config.Save();

            var reloaded = AppConfig.Load();
            Assert.Equal(testAccent, reloaded.AccentColor);

            // Revert
            config.AccentColor = originalAccent;
            config.Save();
        }

        [Fact]
        public void WorkstationConfig_AtomicSaveAndLoad_WorksCorrectly()
        {
            // Load existing or default configuration
            var config = WorkstationConfig.Load();
            Assert.NotNull(config);

            // Modify configuration values
            string originalMachineId = config.MachineId;
            string testMachineId = "PC-TEST-" + Guid.NewGuid().ToString();
            config.MachineId = testMachineId;

            // Save configuration
            config.Save();

            // Reload configuration
            var reloadedConfig = WorkstationConfig.Load();
            Assert.Equal(testMachineId, reloadedConfig.MachineId);

            // Revert changes
            config.MachineId = originalMachineId;
            config.Save();
        }

        [Fact]
        public void ConfigurationSecurityHelper_HashConsistency_Works()
        {
            string password = "MySecurePassword123";
            string hash1 = ConfigurationSecurityHelper.ComputeSha256Hash(password);
            string hash2 = ConfigurationSecurityHelper.ComputeSha256Hash(password);

            Assert.NotNull(hash1);
            Assert.NotEmpty(hash1);
            Assert.Equal(64, hash1.Length); // SHA-256 is 64 hex characters
            Assert.Equal(hash1, hash2);

            string hashNull = ConfigurationSecurityHelper.ComputeSha256Hash(null!);
            Assert.Empty(hashNull);
        }

        [Fact]
        public void AppConfig_NewProperties_SaveAndLoad_Works()
        {
            var config = AppConfig.Load();
            
            // Backup original values
            bool originalEnableAutoSave = config.EnableAutoSave;
            int originalAutoSaveInterval = config.AutoSaveIntervalMinutes;
            bool originalEnableAutoBackup = config.EnableAutoBackup;
            string originalBackupFreq = config.BackupFrequency;
            string originalLang = config.Language;

            // Set test values
            config.EnableAutoSave = false;
            config.AutoSaveIntervalMinutes = 99;
            config.EnableAutoBackup = false;
            config.BackupFrequency = "Monthly";
            config.Language = "ja";

            config.Save();

            // Load and assert
            var reloaded = AppConfig.Load();
            Assert.False(reloaded.EnableAutoSave);
            Assert.Equal(99, reloaded.AutoSaveIntervalMinutes);
            Assert.False(reloaded.EnableAutoBackup);
            Assert.Equal("Monthly", reloaded.BackupFrequency);
            Assert.Equal("ja", reloaded.Language);

            // Revert original values
            config.EnableAutoSave = originalEnableAutoSave;
            config.AutoSaveIntervalMinutes = originalAutoSaveInterval;
            config.EnableAutoBackup = originalEnableAutoBackup;
            config.BackupFrequency = originalBackupFreq;
            config.Language = originalLang;
            
            config.Save();
        }

        [Fact]
        public void AppConfig_GraphicsOptimizationProperties_SaveAndLoad_Works()
        {
            var config = AppConfig.Load();
            
            // Backup original values
            bool originalAutoOptimize = config.AutoOptimizeForIntegratedGraphics;
            bool originalDisableAA = config.Disable3DAntiAliasing;
            bool originalReduceMesh = config.Reduce3DMeshResolution;

            // Set test values
            config.AutoOptimizeForIntegratedGraphics = false;
            config.Disable3DAntiAliasing = true;
            config.Reduce3DMeshResolution = true;

            config.Save();

            // Load and assert
            var reloaded = AppConfig.Load();
            Assert.False(reloaded.AutoOptimizeForIntegratedGraphics);
            Assert.True(reloaded.Disable3DAntiAliasing);
            Assert.True(reloaded.Reduce3DMeshResolution);

            // Revert original values
            config.AutoOptimizeForIntegratedGraphics = originalAutoOptimize;
            config.Disable3DAntiAliasing = originalDisableAA;
            config.Reduce3DMeshResolution = originalReduceMesh;
            
            config.Save();
        }

        [Fact]
        public void AppConfig_IsIntegratedGraphicsDetected_DoesNotThrow()
        {
            try
            {
                bool isIntegrated = AppConfig.IsIntegratedGraphicsDetected;
                // Verify it doesn't throw, and check if it correctly matches our system profile (Intel UHD Graphics)
                string gpuName = "";
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        gpuName = obj["Name"]?.ToString() ?? "";
                        break;
                    }
                }
                if (gpuName.ToLower().Contains("intel") || gpuName.ToLower().Contains("uhd"))
                {
                    Assert.True(isIntegrated);
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"IsIntegratedGraphicsDetected threw an exception: {ex.Message}");
            }
        }
    }
}
