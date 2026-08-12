using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace QASmartClass.Tests
{
    /// <summary>
    /// 🧪 BỘ TEST CASE TỰ ĐỘNG — Nâng cấp Menu Bar Học Sinh
    /// 30 Test Cases: T1 (Ngôn ngữ), T2 (Font/Layout), T3 (Logic/UX), T4 (Regression)
    /// Áp dụng bộ quy chuẩn QA SmartClass v4.1
    /// </summary>
    public class StudentMenuBarUpgradeTests
    {
        private readonly ITestOutputHelper _output;
        private readonly string _xamlContent;
        private readonly string _csContent;

        public StudentMenuBarUpgradeTests(ITestOutputHelper output)
        {
            _output = output;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectRoot = baseDir;
            while (!string.IsNullOrEmpty(projectRoot) && !Directory.Exists(Path.Combine(projectRoot, "QASmartClass")))
            {
                projectRoot = Directory.GetParent(projectRoot)?.FullName ?? "";
            }

            var xamlPath = Path.Combine(projectRoot, "QASmartClass", "StudentClient", "Views", "StudentShell.xaml");
            var csPath = Path.Combine(projectRoot, "QASmartClass", "StudentClient", "Views", "StudentShell.xaml.cs");
            _xamlContent = File.Exists(xamlPath) ? File.ReadAllText(xamlPath) : "";
            _csContent = File.Exists(csPath) ? File.ReadAllText(csPath) : "";
        }

        // ═══ MODULE T1: NGÔN NGỮ TIẾNG VIỆT ═══

        [Fact]
        public void T1_01_SmartTouch_ReplacedWith_BangTuongTac()
        {
            _output.WriteLine("[P0][R1] Nút Smart Touch phải đổi thành 'Bảng tương tác'");
            var contentMatches = Regex.Matches(_xamlContent, @"Content=""[^""]*Smart Touch[^""]*""");
            Assert.Empty(contentMatches);
            Assert.Contains("Bảng tương tác", _xamlContent);
        }

        [Fact]
        public void T1_02_ToolTip_BangTuongTac_TiengViet()
        {
            _output.WriteLine("[P0][R1] ToolTip nút Bảng tương tác phải tiếng Việt");
            var tooltipMatches = Regex.Matches(_xamlContent, @"ToolTip=""[^""]*Smart Touch[^""]*""");
            Assert.Empty(tooltipMatches);
            Assert.Contains("Chuyển sang Bảng tương tác", _xamlContent);
        }

        [Fact]
        public void T1_03_ToolTip_Avatar_NoClick()
        {
            _output.WriteLine("[P1][R1] ToolTip avatar không được chứa 'Click'");
            var clickTooltips = Regex.Matches(_xamlContent, @"ToolTip=""Click[^""]*""");
            Assert.Empty(clickTooltips);
            Assert.Contains("Nhấp để đổi ảnh đại diện", _xamlContent);
        }

        [Fact]
        public void T1_04_XP_Label_Diem_InXAML()
        {
            _output.WriteLine("[P1][R1] Label XP phải đổi thành 'điểm' trong XAML");
            var xpTextMatches = Regex.Matches(_xamlContent, @"Text=""[^""]*\bXP\b[^""]*""");
            Assert.Empty(xpTextMatches);
            Assert.Contains("0/100 điểm", _xamlContent);
        }

        [Fact]
        public void T1_05_XP_FormatString_Diem_InCodeBehind()
        {
            _output.WriteLine("[P1][R1] Code-behind format XP phải dùng 'điểm'");
            var displayXpMatches = Regex.Matches(_csContent, @"\.Text\s*=\s*\$""[^""]*\bXP\b[^""]*""");
            Assert.Empty(displayXpMatches);
            Assert.Contains("điểm\"", _csContent);
        }

        [Fact]
        public void T1_06_InternalVariableNames_Preserved()
        {
            _output.WriteLine("[P0] Tên biến nội bộ phải giữ nguyên");
            Assert.Contains("lblXpValue", _xamlContent);
            Assert.Contains("xpPanel", _xamlContent);
            Assert.Contains("pbXpProgress", _xamlContent);
        }

        [Fact]
        public void T1_07_AllMenuItems_Vietnamese()
        {
            _output.WriteLine("[P0][R1] Tất cả menu item text phải tiếng Việt");
            string[] items = { "Trang chủ", "Bài giảng hôm nay", "Bảng trắng học sinh",
                "Bài kiểm tra", "Bài tập học tập", "Khảo sát", "Tin nhắn",
                "Hỏi bài Giáo viên", "Kết quả học tập", "Tiến trình học tập",
                "Cài đặt", "Hướng dẫn sử dụng", "Thoát chương trình" };
            foreach (var item in items)
                Assert.Contains(item, _xamlContent);
        }

        [Fact]
        public void T1_08_AllToolTips_Vietnamese()
        {
            _output.WriteLine("[P1][R1] Tất cả ToolTip phải tiếng Việt");
            var tooltips = Regex.Matches(_xamlContent, @"ToolTip=""([^""]+)""");
            foreach (Match m in tooltips)
            {
                string tip = m.Groups[1].Value;
                bool hasVN = Regex.IsMatch(tip, @"[àáảãạăắằẳẵặâấầẩẫậèéẻẽẹêếềểễệìíỉĩịòóỏõọôốồổỗộơớờởỡợùúủũụưứừửữựỳýỷỹỵđ]", RegexOptions.IgnoreCase);
                bool isProduct = tip.Contains("QA Smart Class");
                bool isBinding = tip.StartsWith("{") && tip.EndsWith("}");
                Assert.True(hasVN || isProduct || isBinding, $"ToolTip '{tip}' thiếu tiếng Việt");
            }
        }

        // ═══ MODULE T2: FONT & LAYOUT ═══

        [Theory]
        [InlineData("CHÍNH")]
        [InlineData("HỌC TẬP")]
        [InlineData("TƯƠNG TÁC")]
        [InlineData("CÁ NHÂN")]
        public void T2_01to04_SectionHeader_FontSize_AtLeast12(string headerText)
        {
            _output.WriteLine($"[P0][R2] Section header '{headerText}' FontSize >= 12");
            var pattern = $@"Text=""{Regex.Escape(headerText)}""\s+FontSize=""(\d+)""";
            var match = Regex.Match(_xamlContent, pattern);
            Assert.True(match.Success, $"Không tìm thấy header '{headerText}'");
            int fontSize = int.Parse(match.Groups[1].Value);
            Assert.True(fontSize >= 12, $"FontSize={fontSize} < 12");
        }

        [Fact]
        public void T2_05_FontFallback_ContainsArial()
        {
            _output.WriteLine("[P1][R8] Font fallback chain phải chứa Arial");
            Assert.Contains("Segoe UI, Arial", _xamlContent);
        }

        [Fact]
        public void T2_06_S8_IconColor_DifferentFrom_S12()
        {
            _output.WriteLine("[P1][R5] S8 icon color phải khác S12 icon color");
            Assert.Contains("#818CF8", _xamlContent);
            Assert.Contains("#2DD4BF", _xamlContent);
        }

        [Fact]
        public void T2_07_AllIconColors_Unique()
        {
            _output.WriteLine("[P1][R5] Tất cả nav icon phải có màu khác nhau");
            var navIconColors = Regex.Matches(_xamlContent,
                @"FontFamily=""Segoe MDL2 Assets""\s+FontSize=""16""\s+VerticalAlignment=""Center""\s+Margin=""0,0,12,0""\s+Foreground=""(#[0-9A-Fa-f]{6})""")
                .Cast<Match>().Select(m => m.Groups[1].Value.ToUpper()).ToList();
            var duplicates = navIconColors.GroupBy(c => c).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            Assert.Empty(duplicates);
        }

        [Fact]
        public void T2_08_Badge_Sizes_Uniform()
        {
            _output.WriteLine("[P1][R6] Tất cả badge phải cùng kích thước 20x20");
            string[] badges = { "quizBadge", "assignmentBadge", "stuMsgBadge" };
            foreach (var badge in badges)
            {
                var pattern = $@"x:Name=""{badge}""[^>]*Width=""(\d+)""[^>]*Height=""(\d+)""[^>]*CornerRadius=""(\d+)""";
                var match = Regex.Match(_xamlContent, pattern);
                Assert.True(match.Success, $"Không tìm thấy badge '{badge}'");
                Assert.Equal("20", match.Groups[1].Value);
                Assert.Equal("20", match.Groups[2].Value);
                Assert.Equal("10", match.Groups[3].Value);
            }
        }

        // ═══ MODULE T3: LOGIC & UX ═══

        [Fact]
        public void T3_01_CloseApp_ContainsMessageBox()
        {
            _output.WriteLine("[P0][R4] CloseApp_Click phải chứa MessageBox.Show");
            var match = Regex.Match(_csContent, @"CloseApp_Click[^{]*\{([\s\S]*?)\n\s{4}\t\}");
            Assert.True(match.Success, "Không tìm thấy CloseApp_Click");
            Assert.Contains("MessageBox.Show", match.Groups[1].Value);
        }

        [Fact]
        public void T3_02_MessageBox_Title_Vietnamese()
        {
            _output.WriteLine("[P0][R1+R4] MessageBox title phải tiếng Việt");
            Assert.Contains("Xác nhận thoát", _csContent);
        }

        [Fact]
        public void T3_03_MessageBox_Body_Vietnamese()
        {
            _output.WriteLine("[P0][R1+R4] MessageBox body phải tiếng Việt");
            Assert.Contains("chắc chắn muốn thoát", _csContent);
        }

        [Fact]
        public void T3_04_MessageBox_Default_No()
        {
            _output.WriteLine("[P0][R4] MessageBox default = No (fail-safe)");
            // Tìm trong context CloseApp_Click
            int idx = _csContent.IndexOf("CloseApp_Click");
            Assert.True(idx > 0);
            string afterMethod = _csContent.Substring(idx, Math.Min(500, _csContent.Length - idx));
            Assert.Contains("MessageBoxResult.No", afterMethod);
        }

        [Fact]
        public void T3_05_BtnQuickClose_RemovedFromXAML()
        {
            _output.WriteLine("[P0][R3] btnQuickClose phải bị xóa khỏi XAML");
            Assert.DoesNotContain("btnQuickClose", _xamlContent);
        }

        [Fact]
        public void T3_06_BtnQuickClose_NoReference_InCodeBehind()
        {
            _output.WriteLine("[P0][R3] Code-behind không reference btnQuickClose");
            var references = Regex.Matches(_csContent, @"(?<!//.*)btnQuickClose");
            Assert.Empty(references);
        }

        [Fact]
        public void T3_07_TopBar_ReducedElements()
        {
            _output.WriteLine("[P1] Top bar đã giảm phần tử (xóa btnQuickClose)");
            Assert.DoesNotContain("btnQuickClose", _xamlContent);
        }

        [Fact]
        public void T3_08_LogoTooltip_NoTechnicalTerms()
        {
            _output.WriteLine("[P1][R7] Logo ToolTip không chứa thuật ngữ kỹ thuật");
            Assert.DoesNotContain("chẩn đoán", _xamlContent);
            Assert.Contains("Phiên bản", _xamlContent);
        }

        [Fact]
        public void T3_09_LogoLongPress_StillRegistered()
        {
            _output.WriteLine("[P0] Logo long-press event handler vẫn registered");
            Assert.Contains("Logo_MouseLeftButtonDown", _xamlContent);
            Assert.Contains("Logo_MouseLeftButtonUp", _xamlContent);
        }

        [Fact]
        public void T3_10_FocusLock_QuizMode_CodeExists()
        {
            _output.WriteLine("[P1] Focus Lock: Quiz mode logic phải tồn tại");
            Assert.Contains("_isQuizFocusActive", _csContent);
            Assert.Contains("Tính năng bị khóa", _csContent);
        }

        // ═══ MODULE T4: REGRESSION ═══

        [Fact]
        public void T4_01_XpProgressBar_Height6()
        {
            _output.WriteLine("[P2] XP progress bar height = 6");
            var match = Regex.Match(_xamlContent, @"pbXpProgress[^>]*Height=""(\d+)""");
            Assert.True(match.Success);
            Assert.Equal("6", match.Groups[1].Value);
        }

        [Fact]
        public void T4_02_Navigation_12MenuItems_AllExist()
        {
            _output.WriteLine("[P0] 12 menu items navigation targets đều tồn tại");
            string[] tags = { "S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8", "S9", "S10", "S11", "S12" };
            foreach (var tag in tags)
                Assert.Contains($"Tag=\"{tag}\"", _xamlContent);
        }

        [Fact]
        public void T4_03_SidebarToggle_CodeExists()
        {
            _output.WriteLine("[P1] Sidebar toggle handler phải tồn tại");
            Assert.Contains("ToggleSidebar_Click", _csContent);
            Assert.Contains("btnToggleSidebar", _xamlContent);
        }

        [Fact]
        public void T4_04_ThemeToggle_CodeExists()
        {
            _output.WriteLine("[P1] Theme toggle handler phải tồn tại");
            Assert.Contains("ThemeToggle_Click", _csContent);
            Assert.Contains("btnThemeToggle", _xamlContent);
        }
    }
}
