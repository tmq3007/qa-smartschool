using System.Windows.Media;

namespace QASmartClass.Classroom.Helpers
{
    /// <summary>
    /// Cache các Frozen SolidColorBrush dùng chung — thread-safe, không bị GC liên tục.
    /// Thay thế 1,152 lần new SolidColorBrush(...) tạo mới inline.
    /// </summary>
    internal static class BrushCache
    {
        // ── Brand colors ──────────────────────────────────────────────────────
        public static readonly SolidColorBrush Primary       = Make("#1976D2");
        public static readonly SolidColorBrush PrimaryLight  = Make("#42A5F5");
        public static readonly SolidColorBrush PrimaryDark   = Make("#0D47A1");

        // ── Semantic colors ───────────────────────────────────────────────────
        public static readonly SolidColorBrush Success       = Make("#4CAF50");
        public static readonly SolidColorBrush SuccessLight  = Make("#A5D6A7");
        public static readonly SolidColorBrush Danger        = Make("#F44336");
        public static readonly SolidColorBrush DangerLight   = Make("#FFCDD2");
        public static readonly SolidColorBrush Warning       = Make("#FF9800");
        public static readonly SolidColorBrush WarningLight  = Make("#FFE0B2");
        public static readonly SolidColorBrush Info          = Make("#29B6F6");

        // ── Neutral ───────────────────────────────────────────────────────────
        public static readonly SolidColorBrush Muted         = Make("#BDBDBD");
        public static readonly SolidColorBrush Surface       = Make("#FFFFFF");
        public static readonly SolidColorBrush Background    = Make("#F5F5F5");
        public static readonly SolidColorBrush Border        = Make("#E0E0E0");
        public static readonly SolidColorBrush TextPrimary   = Make("#212121");
        public static readonly SolidColorBrush TextSecondary = Make("#757575");

        // ── Classroom-specific ────────────────────────────────────────────────
        public static readonly SolidColorBrush SidebarBg     = Make("#0D1B2A");
        public static readonly SolidColorBrush OnlineGreen   = Make("#4CAF50");
        public static readonly SolidColorBrush OfflineGray   = Make("#BDBDBD");
        public static readonly SolidColorBrush HandRaised    = Make("#FF9800");
        public static readonly SolidColorBrush ShieldOk      = Make("#4CAF50");
        public static readonly SolidColorBrush ShieldWarning = Make("#FFC107");
        public static readonly SolidColorBrush ShieldDanger  = Make("#F44336");

        // ── Factory ───────────────────────────────────────────────────────────
        private static SolidColorBrush Make(string hex)
        {
            var brush = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(hex)!);
            brush.Freeze(); // Immutable → thread-safe, không tốn GC
            return brush;
        }
    }
}
