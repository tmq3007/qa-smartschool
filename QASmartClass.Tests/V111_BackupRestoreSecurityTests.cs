using Xunit;
using System;
using QASmartTouch.Services;

namespace QASmartClass.Tests
{
    [Collection("Sequential")]
    public class V111_BackupRestoreSecurityTests
    {
        [Fact]
        public void Test_BackupRestore_VerifyAdminPassword_Success_Test()
        {
            // 1. Tạo password hash
            string plaintext = "AdminPass123";
            string hash = AuthenticationService.HashPassword(plaintext);
            Assert.NotEmpty(hash);

            // 2. Xác thực với mật khẩu đúng
            bool isMatch = AuthenticationService.VerifyPassword(plaintext, hash);
            Assert.True(isMatch);
        }

        [Fact]
        public void Test_BackupRestore_VerifyAdminPassword_Fail_Test()
        {
            // 1. Tạo password hash
            string plaintext = "AdminPass123";
            string hash = AuthenticationService.HashPassword(plaintext);

            // 2. Xác thực với mật khẩu sai
            bool isMatch = AuthenticationService.VerifyPassword("WrongPassword123", hash);
            Assert.False(isMatch);
        }
    }
}
