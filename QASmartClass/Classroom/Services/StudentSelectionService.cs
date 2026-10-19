using System.Collections.Generic;
using System.Linq;
using QASmartClass.Classroom.Helpers;
using Serilog;

namespace QASmartClass.Classroom.Services
{
    /// <summary>
    /// Bộ chọn đối tượng dùng chung cho Chiếu màn hình, Gửi file, v.v.
    /// Giai đoạn 1: gom logic quy đổi "nhóm → mã học sinh" vào một nơi duy nhất
    /// (trước đây BroadcastPage và FileTransferPage mỗi nơi viết một kiểu).
    /// </summary>
    public static class StudentSelectionService
    {
        /// <summary>
        /// Quy đổi các nhóm đã chọn (theo <c>GroupVm.Index</c>) sang danh sách StudentCode.
        /// <c>GroupVm.Members</c> chứa TÊN học sinh nên cần ánh xạ sang mã trong CSDL.
        /// Nếu trùng tên, chỉ lấy học sinh thuộc lớp đang hoạt động (ActiveRoster).
        /// Trả về danh sách rỗng (không bao giờ null) khi không có kết quả.
        /// </summary>
        public static List<string> ResolveGroupCodes(IEnumerable<int> groupIndexes)
        {
            var codes = new List<string>();
            try
            {
                var groups = ClassroomAppContext.CurrentGroups;
                if (groups == null || groups.Count == 0 || groupIndexes == null) return codes;

                var memberNames = new HashSet<string>();
                foreach (var idx in groupIndexes.Distinct())
                {
                    // Tag của checkbox là GroupVm.Index; dự phòng theo vị trí nếu Index không khớp.
                    var group = groups.FirstOrDefault(g => g.Index == idx)
                                ?? (idx >= 0 && idx < groups.Count ? groups[idx] : null);
                    if (group?.Members == null) continue;
                    foreach (var name in group.Members)
                    {
                        if (!string.IsNullOrEmpty(name)) memberNames.Add(name);
                    }
                }
                if (memberNames.Count == 0) return codes;

                var students = ClassroomAppContext.Db?.Students?.ToList();
                if (students == null || students.Count == 0) return codes;

                var activeClassName = ClassroomAppContext.ClassRoster?.ActiveRoster?.ClassName;
                var countByName = students
                    .GroupBy(s => s.FullName ?? string.Empty)
                    .ToDictionary(g => g.Key, g => g.Count());

                foreach (var s in students)
                {
                    if (string.IsNullOrEmpty(s.StudentCode)) continue;
                    if (!memberNames.Contains(s.FullName ?? string.Empty)) continue;

                    bool isDuplicateName = countByName.TryGetValue(s.FullName ?? string.Empty, out var n) && n > 1;
                    if (isDuplicateName)
                    {
                        if (!string.IsNullOrEmpty(activeClassName) && s.ClassName == activeClassName)
                            codes.Add(s.StudentCode);
                    }
                    else
                    {
                        codes.Add(s.StudentCode);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning("[StudentSelection] ResolveGroupCodes error: {Err}", ex.Message);
            }
            return codes;
        }
    }
}
