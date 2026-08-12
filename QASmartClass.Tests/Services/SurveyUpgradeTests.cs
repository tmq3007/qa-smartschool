using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using QASmartClass.Data;
using QASmartClass.Services;
using Microsoft.EntityFrameworkCore;

namespace QASmartClass.Tests.Services
{
    public class SurveyUpgradeTests
    {
        [Fact]
        public void SurveyModel_CanBe_Initialized()
        {
            var survey = new Survey
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Khảo sát ý kiến",
                Description = "Đánh giá bài giảng",
                SurveyType = "quick",
                QuestionText = "Em hiểu bài không?",
                OptionsJson = "[\"Hiểu\", \"Không hiểu\"]",
                CreatedByTeacher = "GV01",
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                TargetClasses = "ALL"
            };

            Assert.NotNull(survey.Id);
            Assert.Equal("Khảo sát ý kiến", survey.Title);
            Assert.Equal("quick", survey.SurveyType);
        }

        [Fact]
        public void SurveyResponseModel_CanBe_Initialized()
        {
            var response = new SurveyResponse
            {
                Id = Guid.NewGuid().ToString(),
                SurveyId = "survey-id",
                StudentCode = "HS01",
                StudentName = "Nguyen Van A",
                SelectedOptionIndex = 0,
                SelectedOptionText = "Hiểu",
                ResponseTimeSeconds = 12,
                SubmittedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            Assert.NotNull(response.Id);
            Assert.Equal("survey-id", response.SurveyId);
            Assert.Equal("HS01", response.StudentCode);
            Assert.Equal(0, response.SelectedOptionIndex);
        }

        [Fact]
        public void JSON_Serialization_SurveyStart_IsValid()
        {
            var startPayload = new
            {
                SurveyId = "survey-123",
                Title = "Khảo sát nhanh",
                SurveyType = "custom",
                QuestionText = "Câu hỏi 1?",
                Options = new List<string> { "Option A", "Option B" },
                TimeLimitSeconds = 30,
                IsAnonymous = true,
                TargetClasses = "ALL"
            };

            var startMsg = new
            {
                Action = "SURVEY_START",
                Payload = startPayload
            };

            string json = JsonSerializer.Serialize(startMsg);
            Assert.Contains("SURVEY_START", json);
            Assert.Contains("survey-123", json);
            Assert.Contains("Option A", json);
        }

        [Fact]
        public void ComputeSha256Hash_ProducesDeterministicResult()
        {
            string rawData = "HS01" + "survey-123";
            string hash;
            using (var sha256Hash = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
                var builder = new System.Text.StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                hash = builder.ToString();
            }

            Assert.Equal(64, hash.Length);
            Assert.Matches("^[a-f0-9]{64}$", hash);

            string secondHash;
            using (var sha256Hash = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
                var builder = new System.Text.StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                secondHash = builder.ToString();
            }
            Assert.Equal(hash, secondHash);
        }

        [Fact]
        public async System.Threading.Tasks.Task SQLite_Concurrent_Writes_DoNotLock()
        {
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=test_survey_concurrent.db;Foreign Keys=True;")
                .Options;

            using (var context = new AppDbContext(options))
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
                
                using (var conn = context.Database.GetDbConnection())
                {
                    conn.Open();
                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
                        command.ExecuteNonQuery();
                    }
                }
            }

            var tasks = new List<System.Threading.Tasks.Task>();
            for (int i = 0; i < 20; i++)
            {
                int index = i;
                tasks.Add(System.Threading.Tasks.Task.Run(() =>
                {
                    using (var context = new AppDbContext(options))
                    {
                        var response = new SurveyResponse
                        {
                            Id = Guid.NewGuid().ToString(),
                            SurveyId = "survey-123",
                            StudentCode = $"HS_{index}",
                            StudentName = "Ẩn danh",
                            SelectedOptionIndex = index % 2,
                            SelectedOptionText = index % 2 == 0 ? "Hiểu" : "Không hiểu",
                            ResponseTimeSeconds = index,
                            SubmittedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        context.SurveyResponses.Add(response);
                        context.SaveChanges();
                    }
                }));
            }

            await System.Threading.Tasks.Task.WhenAll(tasks);

            using (var context = new AppDbContext(options))
            {
                var count = context.SurveyResponses.Count();
                Assert.Equal(20, count);
            }

            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

            using (var context = new AppDbContext(options))
            {
                context.Database.EnsureDeleted();
            }
        }

        [Fact]
        public void Test_SurveyTemplates_Database_CRUD()
        {
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=test_survey_templates_crud.db;Foreign Keys=True;")
                .Options;

            using (var context = new AppDbContext(options))
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
                DbMigrator.Migrate(context, "5.39.0");
            }

            using (var db = new AppDbContext(options))
            {
                var templateId = Guid.NewGuid().ToString();
                var originalOptions = new List<string> { "Đồng ý", "Không đồng ý" };
                var optionsJson = System.Text.Json.JsonSerializer.Serialize(originalOptions);

                // Create
                var template = new SurveyTemplate
                {
                    Id = templateId,
                    Title = "Khảo sát ý kiến mẫu test",
                    Description = "Mô tả mẫu",
                    SurveyType = "custom",
                    QuestionText = "Bạn có đồng ý không?",
                    OptionsJson = optionsJson,
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                db.SurveyTemplates.Add(template);
                db.SaveChanges();

                // Read
                var readTemplate = db.SurveyTemplates.FirstOrDefault(t => t.Id == templateId);
                Assert.NotNull(readTemplate);
                Assert.Equal("Khảo sát ý kiến mẫu test", readTemplate.Title);

                var opts = System.Text.Json.JsonSerializer.Deserialize<List<string>>(readTemplate.OptionsJson);
                Assert.NotNull(opts);
                Assert.Equal(2, opts.Count);

                // Update
                readTemplate.Title = "Updated Title";
                db.SaveChanges();

                var updated = db.SurveyTemplates.FirstOrDefault(t => t.Id == templateId);
                Assert.NotNull(updated);
                Assert.Equal("Updated Title", updated.Title);

                // Delete
                db.SurveyTemplates.Remove(updated);
                db.SaveChanges();

                Assert.Null(db.SurveyTemplates.FirstOrDefault(t => t.Id == templateId));
            }

            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            using (var context = new AppDbContext(options))
            {
                context.Database.EnsureDeleted();
            }
        }

        [Fact]
        public void Test_Database_Retention_Cleanup()
        {
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=test_survey_retention.db;Foreign Keys=True;")
                .Options;

            using (var context = new AppDbContext(options))
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
                DbMigrator.Migrate(context, "5.39.0");
            }

            using (var db = new AppDbContext(options))
            {
                var now = DateTime.Now;
                var oldDate = now.AddDays(-35);
                var recentDate = now.AddDays(-10);

                var oldSurveyId = "OLD_SURVEY_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                var recentSurveyId = "RECENT_SURVEY_" + Guid.NewGuid().ToString("N").Substring(0, 8);

                // Insert old and recent surveys
                db.Surveys.Add(new Survey
                {
                    Id = oldSurveyId,
                    Title = "Old Survey",
                    Description = "Old",
                    SurveyType = "custom",
                    QuestionText = "Question",
                    OptionsJson = "[\"A\",\"B\"]",
                    TimeLimitSeconds = 0,
                    IsAnonymous = 1,
                    TargetClasses = "ALL",
                    CreatedByTeacher = "GV",
                    CreatedAt = oldDate.ToString("yyyy-MM-dd HH:mm:ss")
                });

                db.Surveys.Add(new Survey
                {
                    Id = recentSurveyId,
                    Title = "Recent Survey",
                    Description = "Recent",
                    SurveyType = "custom",
                    QuestionText = "Question",
                    OptionsJson = "[\"A\",\"B\"]",
                    TimeLimitSeconds = 0,
                    IsAnonymous = 1,
                    TargetClasses = "ALL",
                    CreatedByTeacher = "GV",
                    CreatedAt = recentDate.ToString("yyyy-MM-dd HH:mm:ss")
                });

                // Insert responses
                db.SurveyResponses.Add(new SurveyResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    SurveyId = oldSurveyId,
                    StudentCode = "HS1",
                    StudentName = "Student 1",
                    SelectedOptionIndex = 0,
                    SelectedOptionText = "A",
                    ResponseTimeSeconds = 5,
                    SubmittedAt = oldDate.ToString("yyyy-MM-dd HH:mm:ss")
                });

                db.SurveyResponses.Add(new SurveyResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    SurveyId = recentSurveyId,
                    StudentCode = "HS2",
                    StudentName = "Student 2",
                    SelectedOptionIndex = 1,
                    SelectedOptionText = "B",
                    ResponseTimeSeconds = 6,
                    SubmittedAt = recentDate.ToString("yyyy-MM-dd HH:mm:ss")
                });

                // Insert old and recent event logs
                db.EventLogs.Add(new EventLog
                {
                    EventType = "SURVEY",
                    Actor = "GV",
                    Details = "Old Event",
                    Timestamp = oldDate
                });

                db.EventLogs.Add(new EventLog
                {
                    EventType = "SURVEY",
                    Actor = "GV",
                    Details = "Recent Event",
                    Timestamp = recentDate
                });

                db.SaveChanges();

                // Run cleanup
                var cutoff = DateTime.Now.AddDays(-30);
                string cutoffStr = cutoff.ToString("yyyy-MM-dd HH:mm:ss");

                var oldLogs = db.EventLogs.Where(l => l.Timestamp < cutoff).ToList();
                if (oldLogs.Any()) db.EventLogs.RemoveRange(oldLogs);

                var oldSurveysList = db.Surveys.AsEnumerable().Where(s => {
                    if (DateTime.TryParse(s.CreatedAt, out var date))
                    {
                        return date < cutoff;
                    }
                    return string.Compare(s.CreatedAt, cutoffStr) < 0;
                }).ToList();

                if (oldSurveysList.Any())
                {
                    db.Surveys.RemoveRange(oldSurveysList);
                    db.SaveChanges();
                }

                var surveyIds = db.Surveys.Select(s => s.Id).ToHashSet();
                var orphanedResponses = db.SurveyResponses.AsEnumerable()
                    .Where(r => !surveyIds.Contains(r.SurveyId))
                    .ToList();
                if (orphanedResponses.Any()) db.SurveyResponses.RemoveRange(orphanedResponses);

                db.SaveChanges();

                // Assertions
                Assert.Null(db.Surveys.FirstOrDefault(s => s.Id == oldSurveyId));
                Assert.NotNull(db.Surveys.FirstOrDefault(s => s.Id == recentSurveyId));

                Assert.Null(db.SurveyResponses.FirstOrDefault(r => r.SurveyId == oldSurveyId));
                Assert.NotNull(db.SurveyResponses.FirstOrDefault(r => r.SurveyId == recentSurveyId));

                Assert.Empty(db.EventLogs.Where(e => e.Details == "Old Event").ToList());
                Assert.NotEmpty(db.EventLogs.Where(e => e.Details == "Recent Event").ToList());
            }

            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            using (var context = new AppDbContext(options))
            {
                context.Database.EnsureDeleted();
            }
        }
    }
}
