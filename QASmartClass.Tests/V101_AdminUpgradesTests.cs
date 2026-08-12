using Xunit;
using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Admin.Views;

namespace QASmartClass.Tests
{
    public class V101_AdminUpgradesTests
    {
        [Fact]
        public void Test_DynamicStudentProfilePath_ContainsHardwareId()
        {
            string profilePath = AppPaths.StudentProfileFile;
            string hwId = AppPaths.GetHardwareId();
            
            Assert.Contains(hwId, profilePath);
            Assert.EndsWith(".json", profilePath);
        }

        [Fact]
        public void Test_ParseRole_ValidatesCorrectly()
        {
            using var db = new AppDbContext();
            
            // Set Plaintext Mode
            var setting = db.SystemSettings.Find("RoleSecurityMode");
            if (setting == null)
            {
                setting = new SystemSetting { Id = "RoleSecurityMode", Value = "Plaintext", Category = "Security", LastUpdated = DateTime.Now };
                db.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = "Plaintext";
                setting.LastUpdated = DateTime.Now;
            }
            db.SaveChanges();

            // Save Plaintext Role
            AdminConsoleWindow.SaveRoleSecurely("L1");
            var roleFile = Path.Combine(AppPaths.RootDir, "admin_role.txt");
            Assert.True(File.Exists(roleFile));
            string roleContent = File.ReadAllText(roleFile);
            Assert.Equal("L1", roleContent);

            // Test DPAPI Mode
            setting.Value = "DPAPI_Encrypted";
            db.SaveChanges();

            AdminConsoleWindow.SaveRoleSecurely("L2");
            Assert.True(File.Exists(roleFile));
            byte[] encryptedBytes = File.ReadAllBytes(roleFile);
            
            // Should be encrypted, not plaintext "L2"
            Assert.NotEqual("L2", System.Text.Encoding.UTF8.GetString(encryptedBytes));

            // Decrypt it to verify integrity
            byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.LocalMachine);
            string roleDecrypted = System.Text.Encoding.UTF8.GetString(decryptedBytes);
            Assert.Equal("L2", roleDecrypted);

            // Cleanup
            if (File.Exists(roleFile)) File.Delete(roleFile);
        }

        [Fact]
        public void Test_SharedFolderPath_ResolvesDynamicSetting()
        {
            using var db = new AppDbContext();
            
            // Set custom path in DB
            string customPath = @"C:\TestSharedFolder_2026";
            var setting = db.SystemSettings.Find("SharedFolderPath");
            if (setting == null)
            {
                setting = new SystemSetting { Id = "SharedFolderPath", Value = customPath, Category = "Network", LastUpdated = DateTime.Now };
                db.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = customPath;
                setting.LastUpdated = DateTime.Now;
            }
            db.SaveChanges();

            // Verify setting in DB
            var resolved = db.SystemSettings.Find("SharedFolderPath");
            Assert.NotNull(resolved);
            Assert.Equal(customPath, resolved.Value);

            // Clean setting
            db.SystemSettings.Remove(resolved);
            db.SaveChanges();
        }
    }
}
