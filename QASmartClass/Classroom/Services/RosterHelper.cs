using System.Collections.Generic;
using System.Linq;
using System.Windows;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Classroom.Services
{
    /// <summary>
    /// Helper tập trung: lấy danh sách HS theo Active Roster.
    /// Tất cả các page (Random, Group, Monitor...) nên gọi helper này
    /// thay vì query Database.Students trực tiếp.
    /// </summary>
    public static class RosterHelper
    {
        /// <summary>
        /// Lấy DS học sinh theo Active Roster.
        /// Nếu chưa chọn roster → trả về toàn bộ HS trong DB.
        /// </summary>
        public static List<Student> GetStudents()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var roster = app.ClassRoster.ActiveRoster;

                if (roster != null)
                {
                    var students = app.ClassRoster.GetActiveStudents();
                    if (students.Any())
                    {
                        Log.Debug("RosterHelper: {Count} HS from roster '{Name}'",
                            students.Count, roster.ClassName);
                        return students;
                    }
                }

                // Tự động gán lớp đầu tiên thay vì trả về toàn trường 292 học sinh
                var firstRoster = app.ClassRoster.GetAllRosters().FirstOrDefault();
                if (firstRoster != null)
                {
                    app.ClassRoster.SetActiveRoster(firstRoster);
                    var students = app.ClassRoster.GetActiveStudents();
                    if (students.Any()) return students;
                }

                // Fallback tối đa 35 HS nếu chưa có lớp nào được tạo
                return app.Database.Students.Take(35).ToList();
            }
            catch
            {
                return new List<Student>();
            }
        }

        /// <summary>Lấy DS tên HS (cho RandomPicker, Group...)</summary>
        public static List<string> GetStudentNames()
        {
            return GetStudents().Select(s => s.FullName).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
        }

        /// <summary>Tên roster đang active (hiển thị UI)</summary>
        public static string GetActiveRosterName()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var active = app.ClassRoster.ActiveRoster;
                if (active != null) return active.ClassName;

                // Tự động gán lớp đầu tiên thay vì "Tất cả HS"
                var firstRoster = app.ClassRoster.GetAllRosters().FirstOrDefault();
                if (firstRoster != null)
                {
                    app.ClassRoster.SetActiveRoster(firstRoster);
                    return firstRoster.ClassName;
                }
                return "Chưa chọn lớp";
            }
            catch { return "Chưa chọn lớp"; }
        }
    }
}
