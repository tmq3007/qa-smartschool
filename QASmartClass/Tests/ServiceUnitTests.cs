using QASmartClass.Data;
using QASmartClass.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace QASmartClass.Tests
{
    /// <summary>
    /// Unit Tests cho các Service chính — 12 test cases
    /// Ch?y: TestRunner.RunServiceTests()
    /// </summary>
    public static class ServiceUnitTests
    {
        public static (int passed, int failed, List<string> results) RunAll()
        {
            var results = new List<string>();
            int passed = 0, failed = 0;

            void Assert(string testName, bool condition)
            {
                if (condition) { passed++; results.Add($"  ? PASS: {testName}"); }
                else { failed++; results.Add($"  ? FAIL: {testName}"); }
            }

            Log.Information("[UnitTest] Starting Service Unit Tests...");

            // --- BulletinService Tests ---
            try
            {
                using var db = TestDbFactory.CreateWithSeed();
                var svc = new BulletinService(db);

                // Test 1: CreateBulletin_Valid
                bool result1 = svc.CreateBulletin("Test Title", "Test Content", "All", "Normal", "UnitTest");
                Assert("CreateBulletin_Valid", result1);

                // Test 2: CreateBulletin_EmptyTitle
                bool result2 = svc.CreateBulletin("", "Content", "All", "Normal", "UnitTest");
                Assert("CreateBulletin_EmptyTitle_RejectsFalse", !result2);

                // Test 3: CreateBulletin_InvalidAudience
                bool result3 = svc.CreateBulletin("Title", "Content", "InvalidTarget", "Normal", "UnitTest");
                Assert("CreateBulletin_InvalidAudience_RejectsFalse", !result3);

                // Test 4: GetBulletins returns list
                var bulletins = svc.GetBulletins("All");
                Assert("GetBulletins_ReturnsList", bulletins != null && bulletins.Count >= 0);

                // Test 5: High priority ? status Pending
                svc.CreateBulletin("Urgent Test", "Content", "GV", "High", "UnitTest");
                var latest = db.Bulletins.OrderByDescending(b => b.Id).First();
                Assert("CreateBulletin_HighPriority_StatusPending", latest.Status == "Pending");
            }
            catch (Exception ex)
            {
                failed++;
                results.Add($"  ? FAIL: BulletinService Tests — {ex.Message}");
            }

            // --- EmulationService Tests ---
            try
            {
                using var db = TestDbFactory.CreateWithSeed();
                var svc = new EmulationService(db);

                // Test 6: CalcClassRanking returns data
                var ranking = svc.CalcClassRanking(DateTime.Now.Month, DateTime.Now.Year);
                Assert("CalcClassRanking_ReturnsData", ranking != null && ranking.Count > 0);

                // Test 7: Rankings are ordered by TotalScore descending
                bool isOrdered = true;
                for (int i = 1; i < ranking.Count; i++)
                    if (ranking[i].TotalScore > ranking[i - 1].TotalScore) isOrdered = false;
                Assert("CalcClassRanking_OrderedDescending", isOrdered);

                // Test 8: CalcTeacherEmulation returns data
                var teachers = svc.CalcTeacherEmulation(DateTime.Now.Month, DateTime.Now.Year);
                Assert("CalcTeacherEmulation_ReturnsData", teachers != null && teachers.Count > 0);

                // Test 9: Invalid month returns fallback
                var fallback = svc.CalcClassRanking(0, 1900);
                Assert("CalcClassRanking_InvalidMonth_Fallback", fallback != null && fallback.Count > 0);
            }
            catch (Exception ex)
            {
                failed++;
                results.Add($"  ? FAIL: EmulationService Tests — {ex.Message}");
            }

            // --- AiAssistantService Tests ---
            try
            {
                using var db = TestDbFactory.CreateWithSeed();
                var svc = new AiAssistantService(db);

                // Test 10: CareerGuidance not null
                string career = svc.GetCareerGuidance("Toán", 9.0, new Dictionary<string, double> { { "Toán", 9 } });
                Assert("CareerGuidance_NotNull", !string.IsNullOrEmpty(career));

                // Test 11: LessonPlanSuggestion not null
                string suggestion = svc.GetLessonPlanSuggestion("Toán", "10", "Phuong tŕnh b?c hai");
                Assert("LessonPlanSuggestion_NotNull", !string.IsNullOrEmpty(suggestion));

                // Test 12: CareerGuidance with null input
                string nullResult = svc.GetCareerGuidance(null, 0, null);
                Assert("CareerGuidance_NullInput_NoThrow", true); // If we reach here, no exception
            }
            catch (Exception ex)
            {
                failed++;
                results.Add($"  ? FAIL: AiAssistantService Tests — {ex.Message}");
            }

            Log.Information("[UnitTest] Results: {Passed} passed, {Failed} failed out of {Total}",
                passed, failed, passed + failed);

            return (passed, failed, results);
        }
    }
}

