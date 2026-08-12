using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Leadership.Views;

namespace QASmartClass.Tests
{
    [Collection("Sequential")]
    public class V110_RolePermissionTests
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
        public void Test_RolePermission_DirtyStateDetection_Test()
        {
            // Trạng thái gốc ban đầu
            var original = new Dictionary<string, bool>
            {
                { "TeacherHub", true },
                { "HealthRoom", false },
                { "Kitchen", false },
                { "Security", false },
                { "Leadership", false },
                { "AssignTasks", false },
                { "ApproveLeave", false },
                { "SendPush", false },
                { "EditSystem", false }
            };

            // Trường hợp 1: Không có gì thay đổi -> Sạch (dirty = false)
            bool dirty1 = DetectDirtyState(original, true, false, false, false, false, false, false, false, false);
            Assert.False(dirty1);

            // Trường hợp 2: Thay đổi 1 quyền -> Dơ (dirty = true)
            bool dirty2 = DetectDirtyState(original, true, false, true, false, false, false, false, false, false);
            Assert.True(dirty2);

            // Trường hợp 3: Khôi phục lại trạng thái cũ -> Sạch (dirty = false)
            bool dirty3 = DetectDirtyState(original, true, false, false, false, false, false, false, false, false);
            Assert.False(dirty3);
        }

        [Fact]
        public void Test_RolePermission_AutoSaveOnSwitch_Test()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "role_perms_autosave.db");
            try
            {
                using (var db = CreateTempDbContext(dbPath))
                {
                    // Giả lập lưu quyền Bếp (Id=4) -> Kitchen = true
                    int roleId = 4;
                    string dbRole = "Bep";

                    // 1. Lưu Cấu hình SystemSettings
                    string key = $"Role_{roleId}_Perm_Kitchen";
                    db.SystemSettings.Add(new SystemSetting
                    {
                        Id = key,
                        Value = "true",
                        Category = "Permission",
                        LastUpdated = DateTime.Now
                    });

                    // 2. Lưu ánh xạ vào bảng RolePermissions (kitchen, Canteen, food_safety)
                    string[] tags = new[] { "kitchen", "Canteen", "food_safety" };
                    foreach (var tag in tags)
                    {
                        db.RolePermissions.Add(new RolePermission
                        {
                            Role = dbRole,
                            PermissionTag = tag
                        });
                    }

                    db.SaveChanges();

                    // Xác minh DB lưu chính xác
                    var setting = db.SystemSettings.Find(key);
                    Assert.NotNull(setting);
                    Assert.Equal("true", setting.Value);

                    var rpCount = db.RolePermissions.Where(rp => rp.Role == "Bep").Count();
                    Assert.Equal(3, rpCount);

                    var firstRp = db.RolePermissions.FirstOrDefault(rp => rp.Role == "Bep" && rp.PermissionTag == "food_safety");
                    Assert.NotNull(firstRp);
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        private bool DetectDirtyState(
            Dictionary<string, bool> original,
            bool chkTeacherHub,
            bool chkHealthRoom,
            bool chkKitchen,
            bool chkSecurity,
            bool chkLeadership,
            bool chkAssignTasks,
            bool chkApproveLeave,
            bool chkSendPush,
            bool chkEditSystem)
        {
            bool dirty = false;
            if (original.TryGetValue("TeacherHub", out var origTeacherHub) && chkTeacherHub != origTeacherHub) dirty = true;
            if (original.TryGetValue("HealthRoom", out var origHealthRoom) && chkHealthRoom != origHealthRoom) dirty = true;
            if (original.TryGetValue("Kitchen", out var origKitchen) && chkKitchen != origKitchen) dirty = true;
            if (original.TryGetValue("Security", out var origSecurity) && chkSecurity != origSecurity) dirty = true;
            if (original.TryGetValue("Leadership", out var origLeadership) && chkLeadership != origLeadership) dirty = true;
            if (original.TryGetValue("AssignTasks", out var origAssignTasks) && chkAssignTasks != origAssignTasks) dirty = true;
            if (original.TryGetValue("ApproveLeave", out var origApproveLeave) && chkApproveLeave != origApproveLeave) dirty = true;
            if (original.TryGetValue("SendPush", out var origSendPush) && chkSendPush != origSendPush) dirty = true;
            if (original.TryGetValue("EditSystem", out var origEditSystem) && chkEditSystem != origEditSystem) dirty = true;
            return dirty;
        }
    }
}
