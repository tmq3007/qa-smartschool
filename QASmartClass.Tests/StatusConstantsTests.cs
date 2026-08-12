using QASmartClass.Data;
using QASmartClass.Services;
using Xunit;

namespace QASmartClass.Tests
{
    /// <summary>
    /// V7 P5.1: Unit tests cho StatusConstants va TT22GradingService
    /// </summary>
    public class StatusConstantsTests
    {
        // --- StatusConstants Sanity --------------------------
        [Fact]
        public void ApprovalPending_ShouldBe_Pending()
        {
            Assert.Equal("Pending", StatusConstants.Approval.Pending);
        }

        [Fact]
        public void ApprovalApproved_ShouldBe_Approved()
        {
            Assert.Equal("Approved", StatusConstants.Approval.Approved);
        }

        [Fact]
        public void RiskHigh_ShouldBe_High()
        {
            Assert.Equal("High", StatusConstants.Risk.High);
        }

        [Fact]
        public void TeacherRole_GiaoVien_ShouldBe_GV()
        {
            Assert.Equal("GV", StatusConstants.TeacherRole.GiaoVien);
        }

        // --- TT22 ClassifyAcademic --------------------------
        [Theory]
        [InlineData(10.0, "Tốt")]
        [InlineData(8.0, "Tốt")]
        [InlineData(7.9, "Khá")]
        [InlineData(6.5, "Khá")]
        [InlineData(6.4, "Đạt")]
        [InlineData(5.0, "Đạt")]
        [InlineData(4.9, "Chưa đạt")]
        [InlineData(3.5, "Chưa đạt")]
        [InlineData(3.4, "Chưa đạt")]
        [InlineData(0, "Chưa đạt")]
        public void ClassifyAcademic_ShouldReturnCorrectLabel(double avg, string expected)
        {
            var result = TT22GradingService.ClassifyAcademic(avg);
            Assert.Equal(expected, result);
        }

        // --- TT22 ClassifyConduct ---------------------------
        [Theory]
        [InlineData(100, "Tốt")]
        [InlineData(80, "Tốt")]
        [InlineData(79, "Khá")]
        [InlineData(65, "Khá")]
        [InlineData(64, "Đạt")]
        [InlineData(50, "Đạt")]
        [InlineData(49, "Chưa đạt")]
        [InlineData(0, "Chưa đạt")]
        public void ClassifyConduct_ShouldReturnCorrectLabel(int score, string expected)
        {
            var result = TT22GradingService.ClassifyConduct(score);
            Assert.Equal(expected, result);
        }
    }
}
