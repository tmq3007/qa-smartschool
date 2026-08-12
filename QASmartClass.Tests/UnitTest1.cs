// ---------------------------------------------------
//  QA SmartClass — Simple Self-Tests
//  Ch?y tr?c ti?p trong WPF project (không c?n xUnit reference)
//  S? d?ng: SelfTests.RunAll() t? Debug menu ho?c console
// ---------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using QASmartClass.Services;

namespace QASmartClass.Tests
{
    /// <summary>
    /// Self-contained test runner cho LearningAnalyticsService.
    /// Không c?n xUnit — ch?y du?c tr?c ti?p trong WPF project.
    /// </summary>
    public static class SelfTests
    {
        private static int _passed = 0;
        private static int _failed = 0;

        /// <summary>
        /// Ch?y t?t c? test cases. Tr? v? true n?u t?t c? pass.
        /// </summary>
        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;

            Log.Information("--- Running QASmartClass Self-Tests ---");

            // Test StudentIdentityService
            Test_StudentIdentityService_DefaultFallback();
            Test_StudentIdentityService_CacheClear();

            // Test LearningAnalyticsService  
            Test_GetStreak_NoData_Returns0();
            Test_GetTrend_ReturnsString();
            Test_GetWeakAreas_ReturnsListOrEmpty();

            Log.Information("--- Self-Tests Complete: {Passed} passed, {Failed} failed ---", _passed, _failed);
            return _failed == 0;
        }

        // --- StudentIdentityService Tests ---

        private static void Test_StudentIdentityService_DefaultFallback()
        {
            try
            {
                var app = System.Windows.Application.Current as QASmartTouch.App;
                if (app?.Database == null) { Skip("Test_StudentIdentityService_DefaultFallback", "No database"); return; }

                var service = new StudentClient.Services.StudentIdentityService(app.Database);
                var (id, code, name) = service.GetCurrentStudent();

                Assert(id > 0, "Test_StudentIdentityService_DefaultFallback", $"StudentId should be > 0, got {id}");
                Assert(!string.IsNullOrEmpty(code), "Test_StudentIdentityService_DefaultFallback", $"StudentCode should not be empty");
            }
            catch (Exception ex) { Fail("Test_StudentIdentityService_DefaultFallback", ex.Message); }
        }

        private static void Test_StudentIdentityService_CacheClear()
        {
            try
            {
                var app = System.Windows.Application.Current as QASmartTouch.App;
                if (app?.Database == null) { Skip("Test_StudentIdentityService_CacheClear", "No database"); return; }

                var service = new StudentClient.Services.StudentIdentityService(app.Database);
                var first = service.GetCurrentStudent();
                service.ClearCache();
                var second = service.GetCurrentStudent();

                Assert(first.Id == second.Id, "Test_StudentIdentityService_CacheClear", "Same student after cache clear");
            }
            catch (Exception ex) { Fail("Test_StudentIdentityService_CacheClear", ex.Message); }
        }

        // --- LearningAnalyticsService Tests ---

        private static void Test_GetStreak_NoData_Returns0()
        {
            try
            {
                var app = System.Windows.Application.Current as QASmartTouch.App;
                if (app?.Database == null) { Skip("Test_GetStreak_NoData_Returns0", "No database"); return; }

                var service = new LearningAnalyticsService(app.Database);
                int streak = service.GetStreak(999999); // Non-existent student
                Assert(streak == 0, "Test_GetStreak_NoData_Returns0", $"Expected 0, got {streak}");
            }
            catch (Exception ex) { Fail("Test_GetStreak_NoData_Returns0", ex.Message); }
        }

        private static void Test_GetTrend_ReturnsString()
        {
            try
            {
                var app = System.Windows.Application.Current as QASmartTouch.App;
                if (app?.Database == null) { Skip("Test_GetTrend_ReturnsString", "No database"); return; }

                var service = new LearningAnalyticsService(app.Database);
                string trend = service.GetTrend(1);
                Assert(!string.IsNullOrEmpty(trend), "Test_GetTrend_ReturnsString", "Trend should not be empty");
            }
            catch (Exception ex) { Fail("Test_GetTrend_ReturnsString", ex.Message); }
        }

        private static void Test_GetWeakAreas_ReturnsListOrEmpty()
        {
            try
            {
                var app = System.Windows.Application.Current as QASmartTouch.App;
                if (app?.Database == null) { Skip("Test_GetWeakAreas_ReturnsListOrEmpty", "No database"); return; }

                var service = new LearningAnalyticsService(app.Database);
                var weakAreas = service.GetWeakAreas(1);
                Assert(weakAreas != null, "Test_GetWeakAreas_ReturnsListOrEmpty", "Should return a list (not null)");
            }
            catch (Exception ex) { Fail("Test_GetWeakAreas_ReturnsListOrEmpty", ex.Message); }
        }

        // --- Assertion Helpers ---

        private static void Assert(bool condition, string testName, string message)
        {
            if (condition)
            {
                _passed++;
                Log.Information("  ? PASS: {Test} — {Msg}", testName, message);
            }
            else
            {
                _failed++;
                Log.Error("  ? FAIL: {Test} — {Msg}", testName, message);
            }
        }

        private static void Fail(string testName, string error)
        {
            _failed++;
            Log.Error("  ? FAIL: {Test} — Exception: {Err}", testName, error);
        }

        private static void Skip(string testName, string reason)
        {
            Log.Warning("  ? SKIP: {Test} — {Reason}", testName, reason);
        }
    }
}
