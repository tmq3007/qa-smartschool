using System;
using Xunit;
using QASmartClass.Utilities;
using static QASmartClass.Utilities.MessageGuard;

namespace QASmartClass.Tests
{
    /// <summary>
    /// Unit tests cho MessageGuard — LOI_VID_30 FIX.
    /// Đạt mục tiêu: 100% code coverage cho MessageGuard.cs
    /// Tổng: 25 test cases covering whitelist, blacklist, edge cases, và performance.
    /// </summary>
    public class MessageGuardTests
    {
        // ═══════════════════════════════════════════════════
        //  EDGE CASES (T01-T03)
        // ═══════════════════════════════════════════════════

        [Fact]
        public void T01_Evaluate_Null_ReturnsFalse()
        {
            var result = MessageGuard.Evaluate(null);
            Assert.False(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.Unknown, result.Category);
            Assert.Equal(string.Empty, result.RawMessage);
        }

        [Fact]
        public void T02_Evaluate_Empty_ReturnsFalse()
        {
            var result = MessageGuard.Evaluate("");
            Assert.False(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.Unknown, result.Category);
        }

        [Fact]
        public void T03_Evaluate_Whitespace_ReturnsFalse()
        {
            var result = MessageGuard.Evaluate("   ");
            Assert.False(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.Unknown, result.Category);
        }

        // ═══════════════════════════════════════════════════
        //  WHITELIST — Allowed in Chat (T04-T11)
        // ═══════════════════════════════════════════════════

        [Fact]
        public void T04_Evaluate_ChatAll_ReturnsTrue()
        {
            var result = MessageGuard.Evaluate("CHAT|HS001|ALL|Xin chào thầy");
            Assert.True(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.Chat, result.Category);
        }

        [Fact]
        public void T05_Evaluate_ChatPrivate_ReturnsTrue()
        {
            var result = MessageGuard.Evaluate("CHAT|HS001|TEACHER|Em có câu hỏi");
            Assert.True(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.Chat, result.Category);
        }

        [Fact]
        public void T06_Evaluate_ChatGroup_ReturnsTrue()
        {
            var result = MessageGuard.Evaluate("CHAT|HS001|GROUP_1|Nhóm mình thảo luận");
            Assert.True(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.Chat, result.Category);
        }

        [Fact]
        public void T07_Evaluate_StudentQuestion_ReturnsTrue()
        {
            var result = MessageGuard.Evaluate("STUDENT_QUESTION|text=Thầy ơi giải thích bài 5");
            Assert.True(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.StudentQuestion, result.Category);
        }

        [Fact]
        public void T08_Evaluate_StudentQuestionEnc_ReturnsTrue()
        {
            var result = MessageGuard.Evaluate("STUDENT_QUESTION_ENC|abc123encryptedpayload==");
            Assert.True(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.StudentQuestionEnc, result.Category);
        }

        [Fact]
        public void T09_Evaluate_HandRaise_ReturnsTrue()
        {
            var result = MessageGuard.Evaluate("HAND_RAISE|raised=True|reason=Phát biểu");
            Assert.True(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.HandRaise, result.Category);
        }

        [Fact]
        public void T10_Evaluate_HandRaiseEnc_ReturnsTrue()
        {
            var result = MessageGuard.Evaluate("HAND_RAISE_ENC|payload=xyz123encrypted==");
            Assert.True(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.HandRaiseEnc, result.Category);
        }

        [Fact]
        public void T11_Evaluate_Feedback_ReturnsTrue()
        {
            var result = MessageGuard.Evaluate("STUDENT_FEEDBACK|HS001|\ud83d\udc4d");
            Assert.True(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.StudentFeedback, result.Category);
        }

        // ═══════════════════════════════════════════════════
        //  BLACKLIST — Blocked from Chat (T12-T21)
        //  These are the CRITICAL tests for LOI_VID_30 fix
        // ═══════════════════════════════════════════════════

        [Fact]
        public void T12_Evaluate_HbUpdate_ReturnsFalse()
        {
            // 🔴 PRIMARY LOI_VID_30 scenario: HB_UPDATE leaks into chat
            var result = MessageGuard.Evaluate("HB_UPDATE|Chrome|5");
            Assert.False(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.Heartbeat, result.Category);
        }

        [Fact]
        public void T13_Evaluate_HbUpdateStudentCode_ReturnsFalse()
        {
            // 🔴 Exact string from video: HB_UPDATE|HS001|0
            var result = MessageGuard.Evaluate("HB_UPDATE|HS001|0");
            Assert.False(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.Heartbeat, result.Category);
        }

        [Fact]
        public void T14_Evaluate_HbAck_ReturnsFalse()
        {
            var result = MessageGuard.Evaluate("HB_ACK");
            Assert.False(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.Heartbeat, result.Category);
        }

        [Fact]
        public void T15_Evaluate_Screenshot_ReturnsFalse()
        {
            var result = MessageGuard.Evaluate("SCREENSHOT|HS001|/9j/4AAQSkZJRg...");
            Assert.False(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.Screenshot, result.Category);
        }

        [Fact]
        public void T16_Evaluate_Ack_ReturnsFalse()
        {
            var result = MessageGuard.Evaluate("ACK|42|PC-02|SUCCESS");
            Assert.False(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.CommandAck, result.Category);
        }

        [Fact]
        public void T17_Evaluate_ErrReport_ReturnsFalse()
        {
            var result = MessageGuard.Evaluate("ERR_REPORT|HS001|ERROR|NullReferenceException");
            Assert.False(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.ErrorReport, result.Category);
        }

        [Fact]
        public void T18_Evaluate_EncCmd_ReturnsFalse()
        {
            // 🔴 CRITICAL LOI_VID_30: ENC_CMD leaked into chat (seen in video 2 & 3)
            var result = MessageGuard.Evaluate("ENC_CMD|base64payloadVeryLongEncryptedString==");
            Assert.False(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.EncryptedCommand, result.Category);
        }

        [Fact]
        public void T19_Evaluate_CmdLock_ReturnsFalse()
        {
            var result = MessageGuard.Evaluate("CMD|LOCK|ALL");
            Assert.False(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.SystemInternal, result.Category);
        }

        [Fact]
        public void T20_Evaluate_PolicyMsg_ReturnsFalse()
        {
            var result = MessageGuard.Evaluate("POLICY|block_web_on");
            Assert.False(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.SystemInternal, result.Category);
        }

        [Fact]
        public void T21_Evaluate_MsgBroadcast_ReturnsFalse()
        {
            var result = MessageGuard.Evaluate("MSG|Thông báo từ hệ thống");
            Assert.False(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.SystemInternal, result.Category);
        }

        // ═══════════════════════════════════════════════════
        //  DEFAULT BLOCK & TRICKY EDGE CASES (T22-T25)
        // ═══════════════════════════════════════════════════

        [Fact]
        public void T22_Evaluate_Unknown_ReturnsFalse()
        {
            var result = MessageGuard.Evaluate("RANDOM_DATA_12345_UNKNOWN");
            Assert.False(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.Unknown, result.Category);
        }

        [Fact]
        public void T23_Evaluate_PrefixSimilar_ChatbotIsBlocked()
        {
            // ⚠️ TRICKY: "CHATBOT|" starts with "CHAT" but NOT "CHAT|"
            // Must be blocked because whitelist uses exact prefix "CHAT|"
            var result = MessageGuard.Evaluate("CHATBOT|some_data");
            Assert.False(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.Unknown, result.Category);
        }

        [Fact]
        public void T24_Evaluate_CaseSensitive_LowercaseBlocked()
        {
            // Protocol uses uppercase prefixes — lowercase should NOT match
            var result = MessageGuard.Evaluate("chat|HS001|ALL|Hello");
            Assert.False(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.Unknown, result.Category);
        }

        [Fact]
        public void T25_Evaluate_VeryLongMessage_Blocked()
        {
            // Large ENC_CMD payload (10KB) should be blocked efficiently
            var longPayload = "ENC_CMD|" + new string('A', 10240);
            var result = MessageGuard.Evaluate(longPayload);
            Assert.False(result.IsAllowedInChat);
            Assert.Equal(MessageCategory.EncryptedCommand, result.Category);
        }
    }
}
