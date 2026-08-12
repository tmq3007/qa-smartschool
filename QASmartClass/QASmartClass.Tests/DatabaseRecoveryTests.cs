using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using Xunit;

namespace QASmartClass.Tests
{
    public class DatabaseRecoveryTests
    {
        [Fact]
        public async Task TestInMemoryToRealDbSync_Succeeds()
        {
            string? originalOverride = AppPaths.DataDirOverride;
            string tempPath = Path.Combine(Path.GetTempPath(), "QASmartClassDbRec_" + Guid.NewGuid().ToString());
            
            var ramConnection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:;Foreign Keys=True");
            ramConnection.Open();

            try
            {
                AppPaths.DataDirOverride = tempPath;
                AppPaths.EnsureDirectories();

                // 1. Initialize RAM DB
                AppDbContext.FallbackInMemoryConnection = ramConnection;
                AppDbContext.IsSeedingOrMigrating = true;
                using (var ramDb = new AppDbContext())
                {
                    DbMigrator.Migrate(ramDb, "6.04.0");
                }
                AppDbContext.IsSeedingOrMigrating = false;

                // 2. Add test data in RAM DB
                using (var ramDb = new AppDbContext())
                {
                    ramDb.EventLogs.Add(new EventLog
                    {
                        EventType = "TEST_RAM_LOG",
                        Actor = "Student01",
                        Details = "Saved to RAM DB",
                        Timestamp = DateTime.Now
                    });

                    ramDb.SurveyResponses.Add(new SurveyResponse
                    {
                        Id = Guid.NewGuid().ToString(),
                        SurveyId = "SURVEY_1",
                        StudentCode = "HS01",
                        StudentName = "Student One",
                        SelectedOptionIndex = 1,
                        SelectedOptionText = "Option A",
                        ResponseTimeSeconds = 5,
                        SubmittedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        IsSynced = 0
                    });

                    await ramDb.SaveChangesAsync();
                }

                // Retrieve data from RAM DB
                var ramLogs = new System.Collections.Generic.List<EventLog>();
                var ramResponses = new System.Collections.Generic.List<SurveyResponse>();
                using (var ramDb = new AppDbContext())
                {
                    ramLogs = await ramDb.EventLogs.AsNoTracking().ToListAsync();
                    ramResponses = await ramDb.SurveyResponses.AsNoTracking().ToListAsync();
                }

                Assert.Single(ramLogs);
                Assert.Single(ramResponses);

                // 3. Clear Fallback to simulate recovery to file DB
                AppDbContext.FallbackInMemoryConnection = null;

                // 4. Initialize and migrate the real File DB
                using (var realDb = new AppDbContext())
                {
                    DbMigrator.Migrate(realDb, "6.04.0");
                }

                // 5. Run sync logic
                using (var realDb = new AppDbContext())
                {
                    using (var transaction = await realDb.Database.BeginTransactionAsync())
                    {
                        foreach (var log in ramLogs)
                        {
                            log.Id = 0;
                            realDb.EventLogs.Add(log);
                        }
                        foreach (var resp in ramResponses)
                        {
                            realDb.SurveyResponses.Add(resp);
                        }
                        await realDb.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                }

                // 6. Verify data was successfully written to the File DB
                using (var realDb = new AppDbContext())
                {
                    var fileLogs = await realDb.EventLogs.ToListAsync();
                    var fileResponses = await realDb.SurveyResponses.ToListAsync();

                    Assert.Contains(fileLogs, l => l.EventType == "TEST_RAM_LOG");
                    Assert.Contains(fileResponses, r => r.SurveyId == "SURVEY_1");
                }
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
                ramConnection.Close();
                ramConnection.Dispose();

                AppPaths.DataDirOverride = originalOverride;
                try
                {
                    if (Directory.Exists(tempPath))
                    {
                        Directory.Delete(tempPath, true);
                    }
                }
                catch { }
            }
        }
    }
}
