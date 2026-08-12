using System;
using System.IO;
using System.Threading.Tasks;
using QASmartTouch.Models;
using QASmartTouch.Services;
using QASmartClass.Data;
using QASmartClass.Services;
using Xunit;

namespace QASmartClass.Tests
{
    public class LicenseVerificationTests : IDisposable
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection _ramConnection;
        private readonly AppDbContext _db;

        public LicenseVerificationTests()
        {
            // Setup in-memory DB fallback for test isolated context
            _ramConnection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:;Foreign Keys=True");
            _ramConnection.Open();
            AppDbContext.FallbackInMemoryConnection = _ramConnection;

            AppDbContext.IsSeedingOrMigrating = true;
            _db = new AppDbContext();
            DbMigrator.Migrate(_db, "6.04.0");
            AppServices.Database = _db;
            AppDbContext.IsSeedingOrMigrating = false;
        }

        public void Dispose()
        {
            AppServices.Database = null;
            _db.Dispose();
            AppDbContext.FallbackInMemoryConnection = null;
            _ramConnection.Close();
            _ramConnection.Dispose();
        }

        [Fact]
        public async Task TestLoginWith15CharLicenseKey_Succeeds()
        {
            // Arrange
            var auth = AuthenticationService.Instance;

            // Act
            var (success, msg) = await auth.AuthenticateAsync("GiaoVien01", "PassGiaoVien01", "1AAAAAAAAAAAAA1");

            // Assert
            Assert.True(success);
            Assert.Contains("thử nghiệm", msg);
        }

        [Fact]
        public async Task TestLoginWith16CharLicenseKey_Succeeds()
        {
            // Arrange
            var auth = AuthenticationService.Instance;

            // Act
            var (success, msg) = await auth.AuthenticateAsync("GiaoVien01", "PassGiaoVien01", "1AAAAAAAAAAAAAA1");

            // Assert
            Assert.True(success);
            Assert.Contains("thử nghiệm", msg);
        }

        [Fact]
        public async Task TestCheckLicenseAsync_ReturnsTrialForBothKeys()
        {
            // Arrange
            var auth = AuthenticationService.Instance;

            // Act
            var lic15 = await auth.CheckLicenseAsync("1AAAAAAAAAAAAA1", true);
            var lic16 = await auth.CheckLicenseAsync("1AAAAAAAAAAAAAA1", true);

            // Assert
            Assert.Equal(LicenseStatus.Trial, lic15.Status);
            Assert.Equal(LicenseStatus.Trial, lic16.Status);
        }
    }
}
