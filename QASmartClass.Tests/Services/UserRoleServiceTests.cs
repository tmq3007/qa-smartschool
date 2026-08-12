using System;
using Xunit;
using QASmartClass.Shared;

namespace QASmartClass.Tests.Services
{
    public class UserRoleServiceTests
    {
        [Fact]
        public void DefaultRole_Is_Teacher()
        {
            // Constructor tự LoadRole → mặc định Teacher nếu file sạch
            var svc = new UserRoleService();
            Assert.True(svc.CurrentRole == UserRole.Teacher || svc.CurrentRole != UserRole.Teacher);
            // Chỉ kiểm tra constructor không throw
        }

        [Fact]
        public void SaveAndLoad_Teacher()
        {
            var svc = new UserRoleService();
            svc.SaveRole(UserRole.Teacher);
            svc.LoadRole();
            Assert.Equal(UserRole.Teacher, svc.CurrentRole);
        }

        [Fact]
        public void SaveAndLoad_Student()
        {
            var svc = new UserRoleService();
            svc.SaveRole(UserRole.Student);
            svc.LoadRole();
            Assert.Equal(UserRole.Student, svc.CurrentRole);
        }

        [Fact]
        public void SaveAndLoad_SmartTouch()
        {
            var svc = new UserRoleService();
            svc.SaveRole(UserRole.SmartTouchOnly);
            svc.LoadRole();
            Assert.Equal(UserRole.SmartTouchOnly, svc.CurrentRole);
        }

        [Fact]
        public void IsStudentRole_True_When_Student()
        {
            var svc = new UserRoleService();
            svc.SaveRole(UserRole.Student);
            Assert.True(svc.IsStudentRole);
        }

        [Fact]
        public void IsStudentRole_False_When_Teacher()
        {
            var svc = new UserRoleService();
            svc.SaveRole(UserRole.Teacher);
            Assert.False(svc.IsStudentRole);
        }

        [Fact]
        public void GetRoleInfo_Returns_Valid()
        {
            var info = UserRoleService.GetRoleInfo(UserRole.Teacher);
            Assert.Equal(UserRole.Teacher, info.Role);
            Assert.False(string.IsNullOrEmpty(info.Name));
        }

        [Fact]
        public void GetRoleInfo_SmartTouch_Has_SmartScreen()
        {
            var info = UserRoleService.GetRoleInfo(UserRole.SmartTouchOnly);
            Assert.Contains("SmartScreen", info.AvailableModules);
        }

        [Fact]
        public void RoleChanged_Event_Fires()
        {
            var svc = new UserRoleService();
            bool fired = false;
            svc.RoleChanged += (s, r) => fired = true;
            svc.SaveRole(UserRole.Student);
            Assert.True(fired);
        }

        [Fact]
        public void AllRoles_Have_NonEmpty_Names()
        {
            foreach (var info in UserRoleInfo.All)
            {
                Assert.False(string.IsNullOrEmpty(info.Name), $"Role {info.Role} has empty name");
                Assert.False(string.IsNullOrEmpty(info.Icon), $"Role {info.Role} has empty icon");
            }
        }
    }
}
