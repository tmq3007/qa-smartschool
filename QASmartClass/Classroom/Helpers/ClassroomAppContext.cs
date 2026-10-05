using System.Threading.Tasks;
using QASmartClass.Classroom.Services;
using QASmartClass.Data;

namespace QASmartClass.Classroom.Helpers
{
    /// <summary>
    /// Facade duy nhất cho các Views trong Classroom truy cập App services.
    /// Thay thế pattern: var app = (QASmartTouch.App)Application.Current;
    /// </summary>
    internal static class ClassroomAppContext
    {
        private static QASmartTouch.App App =>
            (QASmartTouch.App)System.Windows.Application.Current;

        // ── Services ──────────────────────────────────────────────────────────
        public static NetworkDiscoveryService?   Network       => App.NetworkService;
        public static ClassroomSessionService    Session       => App.ClassroomSession;
        public static ClassRosterService         ClassRoster   => App.ClassRoster;
        public static AppDbContext               Db            => App.Database;
        public static QASmartClass.Shared.UserRoleService Role => App.UserRoleService;

        // ── Static State Services ─────────────────────────────────────────────
        public static QASmartClass.Services.BroadcastStateService BroadcastState =>
            QASmartTouch.App.BroadcastState;
        public static QASmartClass.Services.LessonStateService LessonState =>
            QASmartTouch.App.LessonState;

        // ── App-level State ───────────────────────────────────────────────────
        /// <summary>Danh sách nhóm hiện tại — thấy được từ BroadcastPage, LessonEditorPage...</summary>
        public static System.Collections.Generic.List<QASmartClass.Classroom.Views.GroupVm> CurrentGroups
        {
            get => App.CurrentGroups;
            set => App.CurrentGroups = value;
        }

        /// <summary>Quản lý danh sách thiết bị học sinh — dùng bởi StudentPage.</summary>
        public static DeviceMemoryService? DeviceMemory => App.DeviceMemory;

        /// <summary>Service truyền file lên/xuống máy học sinh — dùng bởi FileTransferPage.</summary>
        public static QASmartClass.Classroom.Services.FileTransferService? FileTransfer => App.FileTransfer;

        // ── Convenience Helpers ───────────────────────────────────────────────

        /// <summary>
        /// Gửi lệnh mạng an toàn. Không throw nếu NetworkService chưa broadcast.
        /// </summary>
        public static Task SendCommandSafeAsync(string command)
        {
            if (Network?.IsBroadcasting == true)
                return Network.SendCommandAsync(command);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gửi lệnh tới một học sinh cụ thể hoặc broadcast toàn lớp.
        /// Nếu không kết nối mạng thì raise LocalCommand để test offline.
        /// </summary>
        public static void DispatchCommand(string command)
        {
            if (Network?.IsBroadcasting == true)
                _ = Network.SendCommandAsync(command);
            else
                App.RaiseLocalCommand(command);
        }
    }
}
