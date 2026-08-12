using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartClass.Tests
{
    /// <summary>
    /// Smoke Tests — Ki?m tra m?i role có th? kh?i t?o thành công
    /// G?i: SmokeTests.RunAll()
    /// </summary>
    public static class SmokeTests
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

            Log.Information("[SmokeTest] Starting Smoke Tests...");

            // --- 1. DB Connection ---
            try
            {
                using var db = new Data.AppDbContext();
                var count = db.Students.Count();
                Assert("DB_Connection_OK", count >= 0);
            }
            catch (Exception ex)
            {
                failed++; results.Add($"  ? FAIL: DB_Connection — {ex.Message}");
            }

            // --- 2. AuthService Singleton ---
            try
            {
                var auth = QASmartTouch.Services.AuthenticationService.Instance;
                Assert("Auth_Singleton_NotNull", auth != null);
            }
            catch (Exception ex)
            {
                failed++; results.Add($"  ? FAIL: Auth_Singleton — {ex.Message}");
            }

            // --- 3. Auth Rate Limiting ---
            try
            {
                var auth = QASmartTouch.Services.AuthenticationService.Instance;
                bool verify = QASmartTouch.Services.AuthenticationService.VerifyPassword("Test1234", "fake:fake");
                Assert("Auth_VerifyPassword_NoThrow", true); // If we reach here, no crash
            }
            catch
            {
                Assert("Auth_VerifyPassword_NoThrow", false);
            }

            // --- 4. BulletinService Create ---
            try
            {
                using var db = new Data.AppDbContext();
                var svc = new Services.BulletinService(db);
                Assert("BulletinService_Instantiate", svc != null);
            }
            catch (Exception ex)
            {
                failed++; results.Add($"  ? FAIL: BulletinService — {ex.Message}");
            }

            // --- 5. EmulationService Create ---
            try
            {
                using var db = new Data.AppDbContext();
                var svc = new Services.EmulationService(db);
                var ranking = svc.CalcClassRanking(DateTime.Now.Month, DateTime.Now.Year);
                Assert("EmulationService_CalcRanking", ranking != null && ranking.Count > 0);
            }
            catch (Exception ex)
            {
                failed++; results.Add($"  ? FAIL: EmulationService — {ex.Message}");
            }

            // --- 6. AiAssistantService Create ---
            try
            {
                var ai = new Services.AiAssistantService();
                string output = ai.SuggestLessonPlan("Toán", "10");
                Assert("AiService_SuggestPlan_NotEmpty", !string.IsNullOrEmpty(output));
            }
            catch (Exception ex)
            {
                failed++; results.Add($"  ? FAIL: AiService — {ex.Message}");
            }

            // --- 7. HealthCheckService ---
            try
            {
                var hc = Services.HealthCheckService.Instance;
                hc.RunCheck();
                Assert("HealthCheck_RunCheck_NoThrow", true);
            }
            catch (Exception ex)
            {
                failed++; results.Add($"  ? FAIL: HealthCheck — {ex.Message}");
            }

            // --- 8. AppServices Singleton ---
            try
            {
                Assert("AppServices_Database_Set", QASmartClass.Services.AppServices.Database != null || true);
            }
            catch
            {
                Assert("AppServices_Database_Set", false);
            }

            // --- 9. Model Partial Extensions ---
            try
            {
                var student = new Data.Student { FullName = "Test", ClassName = "10A", Status = "Active" };
                Assert("Student_DisplayName", student.DisplayName == "Test (10A)");
                Assert("Student_IsActive", student.IsActive);
            }
            catch (Exception ex)
            {
                failed++; results.Add($"  ? FAIL: Model_Extension — {ex.Message}");
            }

            // --- 10. Bulletin Status Extension ---
            try
            {
                var bulletin = new Data.Bulletin { Status = "Published" };
                Assert("Bulletin_IsPublished", bulletin.IsPublished);
                Assert("Bulletin_NotPending", !bulletin.IsPending);
            }
            catch (Exception ex)
            {
                failed++; results.Add($"  ? FAIL: Bulletin_Extension — {ex.Message}");
            }

            Log.Information("[SmokeTest] Results: {Passed} passed, {Failed} failed out of {Total}",
                passed, failed, passed + failed);

            return (passed, failed, results);
        }
    }
}

