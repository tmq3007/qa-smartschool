using System;
using Xunit;
using QASmartClass.Classroom.Views;

namespace QASmartClass.Tests
{
    public class V85MessagingUpgradesTests
    {
        [Theory]
        [InlineData("dm bài khó quá", "** bài khó quá")]
        [InlineData("bài vcl thế", "bài *** thế")]
        [InlineData("Học sinh lớp c1 chói mắt", "Học sinh lớp c1 chói mắt")] // Không bị lọc nhầm subwords
        [InlineData("ĐM BÀI NÀY DỄ", "** BÀI NÀY DỄ")] // Không phân biệt hoa thường
        [InlineData("đáp án chói sáng", "đáp án chói sáng")]
        [InlineData("bài này fuck thật", "bài này **** thật")]
        [InlineData("con chó này ngoan", "con *** này ngoan")]
        [InlineData("đéo tin được", "*** tin được")]
        [InlineData("cứt gà sáp", "*** gà sáp")]
        public void SanitizeBadWords_ShouldFilterProfanityCorrectly(string input, string expected)
        {
            string actual = MessagingPage.SanitizeBadWords(input);
            Assert.Equal(expected, actual);
        }
    }
}
