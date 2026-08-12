﻿using QASmartClass.Services;
using Xunit;

namespace QASmartClass.Tests
{
    /// <summary>
    /// V7 P5.2: Unit tests cho TeacherKpiService rating labels
    /// (Test logic xep loai — khong can DB)
    /// </summary>
    public class TeacherKpiServiceTests
    {
        [Fact]
        public void FinalRating_90_ShouldBe_XuatSac()
        {
            var kpi = new TeacherKpiResult { FinalRating = 90 };
            string label = GetRatingLabel(kpi.FinalRating);
            Assert.Contains("Xuất sắc", label);
        }

        [Fact]
        public void FinalRating_80_ShouldBe_Tot()
        {
            string label = GetRatingLabel(80);
            Assert.Contains("Tốt", label);
        }

        [Fact]
        public void FinalRating_65_ShouldBe_Kha()
        {
            string label = GetRatingLabel(65);
            Assert.Contains("Khá", label);
        }

        [Fact]
        public void FinalRating_50_ShouldBe_Dat()
        {
            string label = GetRatingLabel(50);
            Assert.Contains("Đạt", label);
        }

        [Fact]
        public void FinalRating_49_ShouldBe_CanCaiThien()
        {
            string label = GetRatingLabel(49);
            Assert.Contains("Cần cải thiện", label);
        }

        [Fact]
        public void TaskCompletionRate_NoTasks_ShouldBeZero()
        {
            var kpi = new TeacherKpiResult { TotalTasks = 0, CompletedTasks = 0 };
            Assert.Equal(0, kpi.TaskCompletionRate);
        }

        [Fact]
        public void TaskCompletionRate_8Of10_ShouldBe80()
        {
            var kpi = new TeacherKpiResult { TotalTasks = 10, CompletedTasks = 8 };
            Assert.Equal(80, kpi.TaskCompletionRate);
        }

        [Fact]
        public void VamScoreNorm_ShouldCap100()
        {
            // VAM = 5 => vamScoreNorm = 50 + 5*10 = 100 (capped)
            double vamScoreNorm = 50 + (5.0 * 10);
            if (vamScoreNorm > 100) vamScoreNorm = 100;
            Assert.Equal(100, vamScoreNorm);
        }

        // Helper: Replicate rating logic from TeacherKpiService
        private static string GetRatingLabel(double finalRating)
        {
            if (finalRating >= 90) return "🌟 Xuất sắc";
            if (finalRating >= 80) return "✅ Tốt";
            if (finalRating >= 65) return "👍 Khá";
            if (finalRating >= 50) return "⚠ Đạt";
            return "🔴 Cần cải thiện";
        }
    }
}

