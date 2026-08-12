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
        [InlineData(10.0, "Giỏi")]
        [InlineData(8.0, "Giỏi")]
        [InlineData(7.9, "Khá")]
        [InlineData(6.5, "Khá")]
        [InlineData(6.4, "Trung bình")]
        [InlineData(5.0, "Trung bình")]
        [InlineData(4.9, "Yếu")]
        [InlineData(3.5, "Yếu")]
        [InlineData(3.4, "Kém")]
        [InlineData(0, "Kém")]
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
        [InlineData(64, "Trung bình")]
        [InlineData(50, "Trung bình")]
        [InlineData(49, "Yếu")]
        [InlineData(0, "Yếu")]
        public void ClassifyConduct_ShouldReturnCorrectLabel(int score, string expected)
        {
            var result = TT22GradingService.ClassifyConduct(score);
            Assert.Equal(expected, result);
        }
    }
}
