using System;

namespace QASmartClass.Utilities
{
    /// <summary>
    /// Bộ lọc tin nhắn tập trung — áp dụng ràng buộc NET-001, SEC-001, SP-002.
    /// Sử dụng WHITELIST (chỉ cho phép message type đã khai báo) kết hợp BLACKLIST.
    /// 
    /// LOI_VID_30 FIX: Ngăn chặn rò rỉ chuỗi Heartbeat (HB_UPDATE|...) và 
    /// Encrypted Command (ENC_CMD|...) vào giao diện Chat.
    /// 
    /// Root Cause: MessagingPage.xaml.cs (L608-860) không có guard clause filter,
    /// dẫn đến tất cả message types từ NetworkDiscoveryService.MessageReceived event
    /// bị fallthrough trực tiếp vào ChatMessage collection.
    /// 
    /// Thiết kế: Pure static function, thread-safe, no allocation, < 0.1ms per call.
    /// </summary>
    public static class MessageGuard
    {
        /// <summary>
        /// Phân loại tin nhắn trong hệ thống QA SmartClass.
        /// </summary>
        public enum MessageCategory
        {
            // ─── ALLOWED in Chat UI ───
            Chat,               // CHAT|code|channel|text
            StudentQuestion,    // STUDENT_QUESTION|text=...
            StudentQuestionEnc, // STUDENT_QUESTION_ENC|text=... (encrypted fallback)
            HandRaise,          // HAND_RAISE|raised=...|reason=...
            HandRaiseEnc,       // HAND_RAISE_ENC|payload=... (encrypted fallback)
            StudentFeedback,    // STUDENT_FEEDBACK|code|emoji

            // ─── BLOCKED from Chat UI ───
            Heartbeat,          // HB_UPDATE|..., HB_ACK, HB|...
            Screenshot,         // SCREENSHOT|code|base64...
            CommandAck,         // ACK|cmdId|pcName|status
            ErrorReport,        // ERR_REPORT|code|severity|message
            EncryptedCommand,   // ENC_CMD|base64payload
            SystemInternal,     // CMD|, POLICY|, LOCK_KEYBOARD|, SILENCE|, MSG|, QUIET_MODE|
            CheatingAlert,      // CHEATING_ALERT|quizId|reason

            // ─── DEFAULT ───
            Unknown             // Unrecognized — BLOCKED by default (fail-safe)
        }

        /// <summary>
        /// Kết quả đánh giá tin nhắn.
        /// </summary>
        public readonly struct GuardResult
        {
            /// <summary>True nếu tin nhắn được phép hiển thị trong Chat UI.</summary>
            public bool IsAllowedInChat { get; init; }

            /// <summary>Phân loại tin nhắn.</summary>
            public MessageCategory Category { get; init; }

            /// <summary>Nội dung tin nhắn gốc (không thay đổi).</summary>
            public string RawMessage { get; init; }
        }

        // ═══════════════════════════════════════════════════════════════
        //  WHITELIST — Chỉ các prefix này được phép hiển thị trong Chat
        //  Khi thêm message type mới cho Chat, PHẢI thêm prefix vào đây.
        // ═══════════════════════════════════════════════════════════════
        private static readonly string[] ChatAllowedPrefixes = new[]
        {
            "CHAT|",
            "STUDENT_QUESTION|",
            "STUDENT_QUESTION_ENC|",
            "HAND_RAISE|",
            "HAND_RAISE_ENC|",
            "STUDENT_FEEDBACK|",
            "MSG_HISTORY|",
            "\ud83d\udce2", // 📢
            "\ud83d\udce8", // 📨
            "\ud83d\udd12", // 🔒
            "\ud83d\udcda", // 📚
            "\ud83d\udcac"  // 💬
        };

        // ═══════════════════════════════════════════════════════════════
        //  BLACKLIST — Các prefix hệ thống TUYỆT ĐỐI không hiển thị.
        //  Dùng để phân loại chính xác cho logging.
        // ═══════════════════════════════════════════════════════════════
        private static readonly string[] SystemBlockedPrefixes = new[]
        {
            "HB_UPDATE|", "HB_ACK", "HB|",
            "SCREENSHOT|",
            "ACK|",
            "ERR_REPORT|",
            "ENC_CMD|",
            "CMD|",
            "POLICY|",
            "LOCK_KEYBOARD|",
            "SILENCE|",
            "QUIET_MODE|",
            "MSG|",   // Server broadcast message — khác với CHAT
            "CHEATING_ALERT|"
        };

        /// <summary>
        /// Kiểm tra và phân loại tin nhắn.
        /// Áp dụng: WHITELIST FIRST → BLACKLIST SECOND → DEFAULT BLOCK.
        /// 
        /// Hiệu năng: O(n) với n = số prefix (< 20), không allocate memory.
        /// Thread-safe: Pure function, không có shared mutable state.
        /// </summary>
        /// <param name="message">Chuỗi tin nhắn thô từ StudentMessageEventArgs.Message</param>
        /// <returns>GuardResult chứa IsAllowedInChat, Category, và RawMessage</returns>
        public static GuardResult Evaluate(string? message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return new GuardResult
                {
                    IsAllowedInChat = false,
                    Category = MessageCategory.Unknown,
                    RawMessage = message ?? string.Empty
                };
            }

            // ── Step 1: Whitelist check (ALLOW) ──
            for (int i = 0; i < ChatAllowedPrefixes.Length; i++)
            {
                if (message.StartsWith(ChatAllowedPrefixes[i], StringComparison.Ordinal))
                {
                    return new GuardResult
                    {
                        IsAllowedInChat = true,
                        Category = CategorizeAllowed(ChatAllowedPrefixes[i]),
                        RawMessage = message
                    };
                }
            }

            // ── Step 2: Blacklist check (BLOCK with specific category for logging) ──
            for (int i = 0; i < SystemBlockedPrefixes.Length; i++)
            {
                if (message.StartsWith(SystemBlockedPrefixes[i], StringComparison.Ordinal))
                {
                    return new GuardResult
                    {
                        IsAllowedInChat = false,
                        Category = CategorizeBlocked(SystemBlockedPrefixes[i]),
                        RawMessage = message
                    };
                }
            }

            // ── Step 3: DEFAULT = BLOCK + Unknown (fail-safe) ──
            return new GuardResult
            {
                IsAllowedInChat = false,
                Category = MessageCategory.Unknown,
                RawMessage = message
            };
        }

        /// <summary>Phân loại message type trong whitelist.</summary>
        private static MessageCategory CategorizeAllowed(string prefix) => prefix switch
        {
            "CHAT|" => MessageCategory.Chat,
            "STUDENT_QUESTION|" => MessageCategory.StudentQuestion,
            "STUDENT_QUESTION_ENC|" => MessageCategory.StudentQuestionEnc,
            "HAND_RAISE|" => MessageCategory.HandRaise,
            "HAND_RAISE_ENC|" => MessageCategory.HandRaiseEnc,
            "STUDENT_FEEDBACK|" => MessageCategory.StudentFeedback,
            "MSG_HISTORY|" => MessageCategory.Chat,
            "\ud83d\udce2" or "\ud83d\udce8" or "\ud83d\udd12" or "\ud83d\udcda" or "\ud83d\udcac" => MessageCategory.Chat,
            _ => MessageCategory.Unknown
        };

        /// <summary>Phân loại message type trong blacklist (cho logging).</summary>
        private static MessageCategory CategorizeBlocked(string prefix) => prefix switch
        {
            "HB_UPDATE|" or "HB_ACK" or "HB|" => MessageCategory.Heartbeat,
            "SCREENSHOT|" => MessageCategory.Screenshot,
            "ACK|" => MessageCategory.CommandAck,
            "ERR_REPORT|" => MessageCategory.ErrorReport,
            "ENC_CMD|" => MessageCategory.EncryptedCommand,
            "CHEATING_ALERT|" => MessageCategory.CheatingAlert,
            _ => MessageCategory.SystemInternal
        };
    }
}
