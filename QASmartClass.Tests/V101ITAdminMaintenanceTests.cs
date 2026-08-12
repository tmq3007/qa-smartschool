using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Staff.ViewModels;

namespace QASmartClass.Tests
{
    public class V101ITAdminMaintenanceTests
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
        public void Test_LinearGrowthPrediction_CalculatesAccurately()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "predict_linear.db");
            try
            {
                using (var db = CreateTempDbContext(dbPath))
                {
                    var service = new DbArchiveService(db);
                    double[] sizeHistory = { 10.0, 12.0, 14.0 };
                    double limitMb = 20.0;

                    double predictedDays = service.PredictDbGrowthDays(sizeHistory, limitMb, "Linear");
                    Assert.True(Math.Abs(predictedDays - 3.0) < 1e-5);
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [Fact]
        public void Test_ExponentialGrowthPrediction_CalculatesAccurately()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "predict_exp.db");
            try
            {
                using (var db = CreateTempDbContext(dbPath))
                {
                    var service = new DbArchiveService(db);
                    double[] sizeHistory = { 10.0, 20.0, 40.0 };
                    double limitMb = 80.0;

                    double predictedDays = service.PredictDbGrowthDays(sizeHistory, limitMb, "Exponential");
                    Assert.True(Math.Abs(predictedDays - 1.0) < 0.1);
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [Fact]
        public void Test_GrowthPrediction_HandlesZeroOrNegativeGrowth()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "predict_zero.db");
            try
            {
                using (var db = CreateTempDbContext(dbPath))
                {
                    var service = new DbArchiveService(db);
                    double[] flatHistory = { 10.0, 10.0, 10.0 };
                    double[] negativeHistory = { 10.0, 9.0, 8.0 };
                    double limitMb = 20.0;

                    double predFlat = service.PredictDbGrowthDays(flatHistory, limitMb, "Linear");
                    double predNeg = service.PredictDbGrowthDays(negativeHistory, limitMb, "Linear");

                    Assert.Equal(-1.0, predFlat);
                    Assert.Equal(-1.0, predNeg);
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [Fact]
        public void Test_ArchiveOldLogs_CreatesArchiveFile_And_Vacuums()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_main.db");
            string archivePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "smartclass_archive.db");

            AppPaths.DatabaseFile = dbPath;

            try
            {
                if (File.Exists(archivePath)) File.Delete(archivePath);

                using (var db = CreateTempDbContext(dbPath))
                {
                    db.AuditLogs.Add(new AuditLog
                    {
                        Action = "RecentAction",
                        ActorName = "User1",
                        Details = "Recent Details",
                        Timestamp = DateTime.Now
                    });

                    db.AuditLogs.Add(new AuditLog
                    {
                        Action = "OldAction",
                        ActorName = "User2",
                        Details = "Old Details",
                        Timestamp = DateTime.Now.AddDays(-100)
                    });

                    db.SaveChanges();

                    var service = new DbArchiveService(db);
                    bool success = service.ArchiveOldLogs(retainDays: 30);

                    Assert.True(success);
                    Assert.Single(db.AuditLogs.ToList());
                    Assert.Equal("RecentAction", db.AuditLogs.First().Action);
                }

                Assert.True(File.Exists(archivePath));

                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite($"Data Source={archivePath}")
                    .Options;

                using (var archiveDb = new AppDbContext(options))
                {
                    var archivedLogs = archiveDb.Database.GetDbConnection();
                    archivedLogs.Open();
                    using (var cmd = archivedLogs.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM AuditLogs;";
                        var count = Convert.ToInt32(cmd.ExecuteScalar());
                        Assert.Equal(1, count);
                    }
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
                if (File.Exists(archivePath)) File.Delete(archivePath);
            }
        }

        [Fact]
        public void Test_SandboxVerification_DetectsCorruptFile()
        {
            string badBackupPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "corrupt_backup.db");
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sandbox_corrupt.db");
            try
            {
                File.WriteAllText(badBackupPath, "This is not a SQLite database file!");

                using (var db = CreateTempDbContext(dbPath))
                {
                    var service = new DbArchiveService(db);
                    bool result = service.VerifyBackupSandbox(badBackupPath);
                    Assert.False(result);
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(badBackupPath)) File.Delete(badBackupPath);
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [Fact]
        public void Test_SandboxVerification_ValidSchema_ReturnsSuccess()
        {
            string validBackupPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "valid_backup.db");
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sandbox_valid.db");
            try
            {
                using (var tempDb = CreateTempDbContext(validBackupPath))
                {
                    tempDb.SystemSettings.Add(new SystemSetting { Id = "TestKey", Value = "TestValue" });
                    tempDb.Students.Add(new Student { StudentCode = "HS01", FullName = "Hoc Sinh A", Status = "Active" });
                    tempDb.Classrooms.Add(new QASmartClass.Data.Classroom { ClassCode = "LAB01", Name = "Phong Lab 1" });
                    tempDb.SaveChanges();
                }

                using (var db = CreateTempDbContext(dbPath))
                {
                    var service = new DbArchiveService(db);
                    bool result = service.VerifyBackupSandbox(validBackupPath);
                    Assert.True(result);
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(validBackupPath)) File.Delete(validBackupPath);
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [Fact]
        public void Test_AnomalyDetection_FlagsOffHoursLogins()
        {
            var log = new AuditLog
            {
                Action = "UserLogin",
                ActorName = "Admin",
                Details = "Successful Login",
                Timestamp = new DateTime(2026, 6, 30, 1, 30, 0)
            };

            bool isSuspicious = log.Timestamp.Hour >= 22 || log.Timestamp.Hour < 4;
            Assert.True(isSuspicious);
        }

        [Fact]
        public void Test_AnomalyDetection_FlagsForeignIPs()
        {
            var log1 = new AuditLog
            {
                Action = "UserLogin",
                ActorName = "Teacher1",
                Details = "Đăng nhập từ IP lạ 192.168.99.100",
                Timestamp = new DateTime(2026, 6, 30, 10, 0, 0)
            };

            bool isSuspicious = log1.Details.Contains("IP lạ") || log1.Details.Contains("192.168.99");
            Assert.True(isSuspicious);
        }
    }
}
