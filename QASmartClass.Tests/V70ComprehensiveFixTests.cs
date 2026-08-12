using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartTouch.Services;

namespace QASmartClass.Tests
{
    public class V70ComprehensiveFixTests
    {
        [Fact]
        public void TestAuthenticationService_HashAndVerify_ShouldSucceed()
        {
            string password = "mySecurePassword123";
            string hash = AuthenticationService.HashPassword(password);
            
            Assert.StartsWith("pbkdf2:", hash);
            
            bool isCorrect = AuthenticationService.VerifyPassword(password, hash);
            Assert.True(isCorrect);
            
            bool isIncorrect = AuthenticationService.VerifyPassword("wrongPassword", hash);
            Assert.False(isIncorrect);
        }

        [Fact]
        public void TestAuthenticationService_PlaintextFallback_ShouldBeRejected()
        {
            // The hash parameter contains a plaintext password "admin123"
            // With plaintext fallback disabled, VerifyPassword should return false
            string plaintextPassword = "admin123";
            string storedHash = "admin123"; // simulating legacy plaintext storage
            
            bool isVerified = AuthenticationService.VerifyPassword(plaintextPassword, storedHash);
            Assert.False(isVerified);
        }

        [Fact]
        public void TestAppDbContext_ConnectionString_ShouldContainDefaultTimeout()
        {
            using (var db = new AppDbContext())
            {
                var connStr = db.Database.GetDbConnection().ConnectionString;
                Assert.Contains("Default Timeout=5", connStr);
            }
        }
    }
}
