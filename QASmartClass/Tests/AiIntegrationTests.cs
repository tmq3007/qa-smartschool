﻿using QASmartClass.Services;
using Serilog;
using System;
using System.Collections.Generic;

namespace QASmartClass.Tests
{
    /// <summary>
    /// Integration Tests cho AI engine — xác nhận output format + randomness
    /// </summary>
    public static class AiIntegrationTests
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

            Log.Information("[AiTest] Starting AI Integration Tests...");

            var ai = new AiAssistantService();

            // --- Test 1: LessonPlan returns non-empty ---
            string plan1 = ai.SuggestLessonPlan("Toán", "10");
            Assert("LessonPlan_ReturnsNonEmpty", !string.IsNullOrEmpty(plan1) && plan1.Length > 50);

            // --- Test 2: LessonPlan contains subject ---
            Assert("LessonPlan_ContainsSubject", plan1.Contains("Toán"));

            // --- Test 3: LessonPlan contains grade ---
            Assert("LessonPlan_ContainsGrade", plan1.Contains("10"));

            // --- Test 4: LessonPlan has template marker ---
            bool hasTemplate = plan1.Contains("5E") || plan1.Contains("Tích hợp") ||
                               plan1.Contains("Truyền thống") || plan1.Contains("STEM") ||
                               plan1.Contains("Flipped");
            Assert("LessonPlan_HasTemplateMarker", hasTemplate);

            // --- Test 5: Randomness — 10 calls should yield =2 unique results ---
            var uniquePlans = new HashSet<string>();
            for (int i = 0; i < 10; i++)
                uniquePlans.Add(ai.SuggestLessonPlan("Van", "11"));
            Assert("LessonPlan_Randomness_GE2Unique", uniquePlans.Count >= 2);

            // --- Test 6: StudentProgress non-empty ---
            string progress = ai.AnalyzeStudentProgress(1);
            Assert("StudentProgress_ReturnsNonEmpty", !string.IsNullOrEmpty(progress) && progress.Length > 20);

            // --- Test 7: Progress has emoji ---
            bool hasEmoji = progress.Contains("📈") || progress.Contains("🌱") ||
                            progress.Contains("🏆") || progress.Contains("⚠️") ||
                            progress.Contains("📊");
            Assert("StudentProgress_HasEmoji", hasEmoji);

            // --- Test 8: CareerGuidance non-empty ---
            string career = ai.CareerGuidance(null);
            Assert("CareerGuidance_ReturnsNonEmpty", !string.IsNullOrEmpty(career) && career.Length > 20);

            // --- Test 9: CareerGuidance contains Holland ---
            Assert("CareerGuidance_ContainsHolland", career.Contains("Holland"));

            // --- Test 10: GetLessonPlanSuggestion with topic ---
            string suggestion = ai.GetLessonPlanSuggestion("Hóa", "12", "Liên kết hóa học");
            Assert("LessonPlanSuggestion_ContainsTopic", suggestion.Contains("Liên kết hóa học"));

            // --- Test 11: GetCareerGuidance overload ---
            string career2 = ai.GetCareerGuidance("Toán", 9.0, new Dictionary<string, double> { { "Toán", 9 } });
            Assert("CareerGuidance_Overload_NotNull", !string.IsNullOrEmpty(career2));

            // --- Test 12: Progress randomness ---
            var uniqueProgress = new HashSet<string>();
            for (int i = 0; i < 10; i++)
                uniqueProgress.Add(ai.AnalyzeStudentProgress(i));
            Assert("StudentProgress_Randomness_GE2Unique", uniqueProgress.Count >= 2);

            Log.Information("[AiTest] Results: {Passed} passed, {Failed} failed out of {Total}",
                passed, failed, passed + failed);

            return (passed, failed, results);
        }
    }
}

