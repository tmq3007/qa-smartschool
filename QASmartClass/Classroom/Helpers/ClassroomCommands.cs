namespace QASmartClass.Classroom.Helpers
{
    /// <summary>
    /// Tập trung tất cả magic command strings gửi qua mạng TCP/UDP.
    /// Thay ~50 string literals rải rác trong Views và FloatingModeBar.
    /// </summary>
    internal static class ClassroomCommands
    {
        // ── Điều khiển màn hình ────────────────────────────────────────────────
        public const string LockAll         = "CMD|LOCK|ALL";
        public const string UnlockAll       = "CMD|UNLOCK|ALL";
        public const string Silence         = "CMD|SILENCE";
        public const string ClearSilence    = "CMD|CLEAR_SILENCE";

        // ── Phát màn hình (Screen Broadcast) ──────────────────────────────────
        public const string ScreenBroadcastStart  = "CMD|SCREEN_BROADCAST_START";
        public const string ScreenBroadcastStop   = "CMD|SCREEN_BROADCAST_STOP";
        public const string ScreenBroadcastUpdate = "CMD|SCREEN_BROADCAST_UPDATE";

        // ── Phát giọng nói (Voice) ────────────────────────────────────────────
        public const string VoiceBroadcastOn  = "VOICE_BROADCAST|enabled=True";
        public const string VoiceBroadcastOff = "VOICE_BROADCAST|enabled=False";

        // ── Ghi hình ──────────────────────────────────────────────────────────
        public const string ScreenRecordOn  = "SCREEN_RECORD|enabled=True";
        public const string ScreenRecordOff = "SCREEN_RECORD|enabled=False";

        // ── Bài giảng ────────────────────────────────────────────────────────
        public static string LessonStart(int lessonId)  => $"CMD|LESSON_START|{lessonId}";
        public static string LessonStage(int stage)     => $"CMD|LESSON_STAGE|{stage}";
        public static string LessonFocus(int sort, string type) => $"CMD|LESSON_FOCUS|{sort}|{type}";
        public const string  LessonEnd                   = "CMD|LESSON_END|0";

        // ── Quiz ──────────────────────────────────────────────────────────────
        public static string QuizStart(int quizId) => $"CMD|QUIZ_START|{quizId}";
        public const string  QuizEnd               = "CMD|QUIZ_END|0";

        // ── Bảo mật ───────────────────────────────────────────────────────────
        public const string CheckIntegrity = "CMD|CHECK_INTEGRITY";

        // ── Tin nhắn ─────────────────────────────────────────────────────────
        public static string Message(string text) => $"MSG|{text}";

        // ── Form Navigation IDs ───────────────────────────────────────────────
        /// <summary>ID của các trang điều hướng trong ClassroomShell.</summary>
        internal static class Forms
        {
            public const string Dashboard    = "F1";
            public const string Lessons      = "F2";
            public const string LessonEditor = "F3";
            public const string Classroom    = "F4";
            public const string Monitor      = "F5";
            public const string Quiz         = "F6";
            public const string Report       = "F7";
            public const string Settings     = "F8";
            public const string Library      = "F9";
            public const string Groups       = "F10";
            public const string FileTransfer = "F11";
            public const string Messaging    = "F13";
            public const string QuestionBank = "F14";
            public const string Roster       = "F15";
            public const string Broadcast    = "F16";
            public const string Policy       = "F17";
            public const string Survey       = "F18";
            public const string EventLog     = "F19";
            public const string Canvas       = "F20";
            public const string Chart        = "F21";
            public const string Timer        = "F22";
            public const string RandomPicker = "F23";
            public const string SplitScreen  = "F24";
            public const string History      = "F25";
            public const string Timetable    = "F26";
            public const string Attendance   = "F27";
            public const string Homework     = "F28";
            public const string AI           = "F29";
            public const string WebPush      = "F30";
            public const string Stem         = "F31";
            public const string LearningHub  = "F32";
            public const string Teachers     = "F33";
            public const string PracticalApps = "F34";
        }
    }
}
