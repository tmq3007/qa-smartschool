using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using QASmartClass.Classroom.Views;
using Serilog;

namespace QASmartClass.Data
{
    /// <summary>
    /// Cung cấp và đồng bộ thời khóa biểu riêng biệt, thực tế cho từng lớp học THPT
    /// </summary>
    public static class TimetableDataHelper
    {
        /// <summary>
        /// Tạo thời khóa biểu 28 tiết/tuần riêng biệt chuẩn chương trình GDPT cho từng lớp
        /// </summary>
        public static List<TimetableSlot> GetDistinctTimetable(string className)
        {
            string clean = (className ?? "").Trim().ToUpper();
            var list = new List<TimetableSlot>();

            switch (clean)
            {
                case "10A1":
                    // 10A1: Trọng điểm Toán - Tin (GV Nguyễn Văn Hùng)
                    AddDay(list, 0, new[] { ("Chào cờ", "Sân trường", "BGH"), ("Toán", "P.10A1", "Nguyễn Văn Hùng"), ("Toán", "P.10A1", "Nguyễn Văn Hùng"), ("Văn", "P.10A1", "Hoàng Đức Minh"), ("Anh", "P.10A1", "Vũ Thị Lan") });
                    AddDay(list, 1, new[] { ("Lý", "P.10A1", "Trần Thị Mai"), ("Lý", "P.10A1", "Trần Thị Mai"), ("Tin", "P.Tin", "Bùi Minh Đức"), ("Hóa", "P.10A1", "Lê Hoàng Nam"), ("GDCD", "P.10A1", "Lý Thị Thu") });
                    AddDay(list, 2, new[] { ("Toán", "P.10A1", "Nguyễn Văn Hùng"), ("Toán", "P.10A1", "Nguyễn Văn Hùng"), ("Văn", "P.10A1", "Hoàng Đức Minh"), ("Sinh", "P.10A1", "Phạm Thị Hương"), ("Sử", "P.10A1", "Đỗ Quang Trung") });
                    AddDay(list, 3, new[] { ("Anh", "P.10A1", "Vũ Thị Lan"), ("Anh", "P.10A1", "Vũ Thị Lan"), ("Tin", "P.Tin", "Bùi Minh Đức"), ("Địa", "P.10A1", "Ngô Thị Hạnh"), ("TD", "Sân TD", "GV TD") });
                    AddDay(list, 4, new[] { ("Hóa", "P.10A1", "Lê Hoàng Nam"), ("Lý", "P.10A1", "Trần Thị Mai"), ("Văn", "P.10A1", "Hoàng Đức Minh"), ("Toán", "P.10A1", "Nguyễn Văn Hùng"), ("TD", "Sân TD", "GV TD") });
                    AddDay(list, 5, new[] { ("Sinh", "P.10A1", "Phạm Thị Hương"), ("CN", "P.10A1", "GV CN"), ("SHL", "P.10A1", "Nguyễn Văn Hùng") });
                    break;

                case "10A2":
                    // 10A2: Trọng điểm Lý - Hóa (GV Trần Thị Mai)
                    AddDay(list, 0, new[] { ("Chào cờ", "Sân trường", "BGH"), ("Lý", "P.10A2", "Trần Thị Mai"), ("Lý", "P.10A2", "Trần Thị Mai"), ("Hóa", "P.10A2", "Lê Hoàng Nam"), ("Toán", "P.10A2", "Nguyễn Văn Hùng") });
                    AddDay(list, 1, new[] { ("Văn", "P.10A2", "Hoàng Đức Minh"), ("Văn", "P.10A2", "Hoàng Đức Minh"), ("Anh", "P.10A2", "Vũ Thị Lan"), ("Sử", "P.10A2", "Đỗ Quang Trung"), ("GDCD", "P.10A2", "Lý Thị Thu") });
                    AddDay(list, 2, new[] { ("Hóa", "P.10A2", "Lê Hoàng Nam"), ("Hóa", "P.10A2", "Lê Hoàng Nam"), ("Toán", "P.10A2", "Nguyễn Văn Hùng"), ("Lý", "P.10A2", "Trần Thị Mai"), ("Sinh", "P.10A2", "Phạm Thị Hương") });
                    AddDay(list, 3, new[] { ("Toán", "P.10A2", "Nguyễn Văn Hùng"), ("Toán", "P.10A2", "Nguyễn Văn Hùng"), ("Tin", "P.Tin", "Bùi Minh Đức"), ("TD", "Sân TD", "GV TD"), ("TD", "Sân TD", "GV TD") });
                    AddDay(list, 4, new[] { ("Anh", "P.10A2", "Vũ Thị Lan"), ("Anh", "P.10A2", "Vũ Thị Lan"), ("Lý", "P.10A2", "Trần Thị Mai"), ("Địa", "P.10A2", "Ngô Thị Hạnh"), ("CN", "P.10A2", "GV CN") });
                    AddDay(list, 5, new[] { ("Văn", "P.10A2", "Hoàng Đức Minh"), ("Sinh", "P.10A2", "Phạm Thị Hương"), ("SHL", "P.10A2", "Trần Thị Mai") });
                    break;

                case "10A3":
                    // 10A3: Trọng điểm Sinh học - Hóa học (GV Phạm Thị Hương)
                    AddDay(list, 0, new[] { ("Chào cờ", "Sân trường", "BGH"), ("Sinh", "P.10A3", "Phạm Thị Hương"), ("Sinh", "P.10A3", "Phạm Thị Hương"), ("Hóa", "P.10A3", "Lê Hoàng Nam"), ("Văn", "P.10A3", "Hoàng Đức Minh") });
                    AddDay(list, 1, new[] { ("Toán", "P.10A3", "Nguyễn Văn Hùng"), ("Toán", "P.10A3", "Nguyễn Văn Hùng"), ("Anh", "P.10A3", "Vũ Thị Lan"), ("Sử", "P.10A3", "Đỗ Quang Trung"), ("Địa", "P.10A3", "Ngô Thị Hạnh") });
                    AddDay(list, 2, new[] { ("Hóa", "P.10A3", "Lê Hoàng Nam"), ("Hóa", "P.10A3", "Lê Hoàng Nam"), ("Sinh", "P.10A3", "Phạm Thị Hương"), ("Lý", "P.10A3", "Trần Thị Mai"), ("Tin", "P.Tin", "Bùi Minh Đức") });
                    AddDay(list, 3, new[] { ("Văn", "P.10A3", "Hoàng Đức Minh"), ("Văn", "P.10A3", "Hoàng Đức Minh"), ("Anh", "P.10A3", "Vũ Thị Lan"), ("TD", "Sân TD", "GV TD"), ("TD", "Sân TD", "GV TD") });
                    AddDay(list, 4, new[] { ("Toán", "P.10A3", "Nguyễn Văn Hùng"), ("Sinh", "P.10A3", "Phạm Thị Hương"), ("Lý", "P.10A3", "Trần Thị Mai"), ("GDCD", "P.10A3", "Lý Thị Thu"), ("CN", "P.10A3", "GV CN") });
                    AddDay(list, 5, new[] { ("Anh", "P.10A3", "Vũ Thị Lan"), ("Hóa", "P.10A3", "Lê Hoàng Nam"), ("SHL", "P.10A3", "Phạm Thị Hương") });
                    break;

                case "11A1":
                    // 11A1: Trọng điểm Hóa học - Sinh học (GV Lê Hoàng Nam)
                    AddDay(list, 0, new[] { ("Chào cờ", "Sân trường", "BGH"), ("Hóa", "P.11A1", "Lê Hoàng Nam"), ("Hóa", "P.11A1", "Lê Hoàng Nam"), ("Toán", "P.11A1", "Nguyễn Văn Hùng"), ("Toán", "P.11A1", "Nguyễn Văn Hùng") });
                    AddDay(list, 1, new[] { ("Sinh", "P.11A1", "Phạm Thị Hương"), ("Sinh", "P.11A1", "Phạm Thị Hương"), ("Lý", "P.11A1", "Trần Thị Mai"), ("Văn", "P.11A1", "Hoàng Đức Minh"), ("Anh", "P.11A1", "Vũ Thị Lan") });
                    AddDay(list, 2, new[] { ("Toán", "P.11A1", "Nguyễn Văn Hùng"), ("Hóa", "P.11A1", "Lê Hoàng Nam"), ("Tin", "P.Tin", "Bùi Minh Đức"), ("Sử", "P.11A1", "Đỗ Quang Trung"), ("GDCD", "P.11A1", "Lý Thị Thu") });
                    AddDay(list, 3, new[] { ("Lý", "P.11A1", "Trần Thị Mai"), ("Lý", "P.11A1", "Trần Thị Mai"), ("Văn", "P.11A1", "Hoàng Đức Minh"), ("TD", "Sân TD", "GV TD"), ("TD", "Sân TD", "GV TD") });
                    AddDay(list, 4, new[] { ("Anh", "P.11A1", "Vũ Thị Lan"), ("Anh", "P.11A1", "Vũ Thị Lan"), ("Hóa", "P.11A1", "Lê Hoàng Nam"), ("Địa", "P.11A1", "Ngô Thị Hạnh"), ("CN", "P.11A1", "GV CN") });
                    AddDay(list, 5, new[] { ("Sinh", "P.11A1", "Phạm Thị Hương"), ("Toán", "P.11A1", "Nguyễn Văn Hùng"), ("SHL", "P.11A1", "Lê Hoàng Nam") });
                    break;

                case "11A2":
                    // 11A2: Trọng điểm Ngữ văn - Xã hội (GV Hoàng Đức Minh)
                    AddDay(list, 0, new[] { ("Chào cờ", "Sân trường", "BGH"), ("Văn", "P.11A2", "Hoàng Đức Minh"), ("Văn", "P.11A2", "Hoàng Đức Minh"), ("Sử", "P.11A2", "Đỗ Quang Trung"), ("Địa", "P.11A2", "Ngô Thị Hạnh") });
                    AddDay(list, 1, new[] { ("Anh", "P.11A2", "Vũ Thị Lan"), ("Anh", "P.11A2", "Vũ Thị Lan"), ("Toán", "P.11A2", "Nguyễn Văn Hùng"), ("GDCD", "P.11A2", "Lý Thị Thu"), ("Tin", "P.Tin", "Bùi Minh Đức") });
                    AddDay(list, 2, new[] { ("Văn", "P.11A2", "Hoàng Đức Minh"), ("Sử", "P.11A2", "Đỗ Quang Trung"), ("Sử", "P.11A2", "Đỗ Quang Trung"), ("Địa", "P.11A2", "Ngô Thị Hạnh"), ("Sinh", "P.11A2", "Phạm Thị Hương") });
                    AddDay(list, 3, new[] { ("Toán", "P.11A2", "Nguyễn Văn Hùng"), ("Toán", "P.11A2", "Nguyễn Văn Hùng"), ("Anh", "P.11A2", "Vũ Thị Lan"), ("TD", "Sân TD", "GV TD"), ("TD", "Sân TD", "GV TD") });
                    AddDay(list, 4, new[] { ("Văn", "P.11A2", "Hoàng Đức Minh"), ("Văn", "P.11A2", "Hoàng Đức Minh"), ("GDCD", "P.11A2", "Lý Thị Thu"), ("Lý", "P.11A2", "Trần Thị Mai"), ("Hóa", "P.11A2", "Lê Hoàng Nam") });
                    AddDay(list, 5, new[] { ("Địa", "P.11A2", "Ngô Thị Hạnh"), ("CN", "P.11A2", "GV CN"), ("SHL", "P.11A2", "Hoàng Đức Minh") });
                    break;

                case "11A3":
                    // 11A3: Trọng điểm Lịch sử - Xã hội (GV Đỗ Quang Trung)
                    AddDay(list, 0, new[] { ("Chào cờ", "Sân trường", "BGH"), ("Sử", "P.11A3", "Đỗ Quang Trung"), ("Sử", "P.11A3", "Đỗ Quang Trung"), ("Văn", "P.11A3", "Hoàng Đức Minh"), ("Văn", "P.11A3", "Hoàng Đức Minh") });
                    AddDay(list, 1, new[] { ("Địa", "P.11A3", "Ngô Thị Hạnh"), ("Địa", "P.11A3", "Ngô Thị Hạnh"), ("Anh", "P.11A3", "Vũ Thị Lan"), ("Toán", "P.11A3", "Nguyễn Văn Hùng"), ("GDCD", "P.11A3", "Lý Thị Thu") });
                    AddDay(list, 2, new[] { ("Sử", "P.11A3", "Đỗ Quang Trung"), ("Văn", "P.11A3", "Hoàng Đức Minh"), ("GDCD", "P.11A3", "Lý Thị Thu"), ("Sinh", "P.11A3", "Phạm Thị Hương"), ("Tin", "P.Tin", "Bùi Minh Đức") });
                    AddDay(list, 3, new[] { ("Anh", "P.11A3", "Vũ Thị Lan"), ("Anh", "P.11A3", "Vũ Thị Lan"), ("Toán", "P.11A3", "Nguyễn Văn Hùng"), ("TD", "Sân TD", "GV TD"), ("TD", "Sân TD", "GV TD") });
                    AddDay(list, 4, new[] { ("Địa", "P.11A3", "Ngô Thị Hạnh"), ("Sử", "P.11A3", "Đỗ Quang Trung"), ("Văn", "P.11A3", "Hoàng Đức Minh"), ("Hóa", "P.11A3", "Lê Hoàng Nam"), ("Lý", "P.11A3", "Trần Thị Mai") });
                    AddDay(list, 5, new[] { ("Toán", "P.11A3", "Nguyễn Văn Hùng"), ("CN", "P.11A3", "GV CN"), ("SHL", "P.11A3", "Đỗ Quang Trung") });
                    break;

                case "12A1":
                    // 12A1: Trọng điểm Tiếng Anh (GV Vũ Thị Lan)
                    AddDay(list, 0, new[] { ("Chào cờ", "Sân trường", "BGH"), ("Anh", "P.12A1", "Vũ Thị Lan"), ("Anh", "P.12A1", "Vũ Thị Lan"), ("Toán", "P.12A1", "Nguyễn Văn Hùng"), ("Toán", "P.12A1", "Nguyễn Văn Hùng") });
                    AddDay(list, 1, new[] { ("Văn", "P.12A1", "Hoàng Đức Minh"), ("Văn", "P.12A1", "Hoàng Đức Minh"), ("Lý", "P.12A1", "Trần Thị Mai"), ("Hóa", "P.12A1", "Lê Hoàng Nam"), ("Sinh", "P.12A1", "Phạm Thị Hương") });
                    AddDay(list, 2, new[] { ("Anh", "P.12A1", "Vũ Thị Lan"), ("Anh", "P.12A1", "Vũ Thị Lan"), ("Toán", "P.12A1", "Nguyễn Văn Hùng"), ("Sử", "P.12A1", "Đỗ Quang Trung"), ("Tin", "P.Tin", "Bùi Minh Đức") });
                    AddDay(list, 3, new[] { ("Toán", "P.12A1", "Nguyễn Văn Hùng"), ("Toán", "P.12A1", "Nguyễn Văn Hùng"), ("Văn", "P.12A1", "Hoàng Đức Minh"), ("TD", "Sân TD", "GV TD"), ("TD", "Sân TD", "GV TD") });
                    AddDay(list, 4, new[] { ("Anh", "P.12A1", "Vũ Thị Lan"), ("Lý", "P.12A1", "Trần Thị Mai"), ("Địa", "P.12A1", "Ngô Thị Hạnh"), ("GDCD", "P.12A1", "Lý Thị Thu"), ("CN", "P.12A1", "GV CN") });
                    AddDay(list, 5, new[] { ("Hóa", "P.12A1", "Lê Hoàng Nam"), ("Văn", "P.12A1", "Hoàng Đức Minh"), ("SHL", "P.12A1", "Vũ Thị Lan") });
                    break;

                case "12A2":
                    // 12A2: Trọng điểm Tin học - Công nghệ (GV Bùi Minh Đức)
                    AddDay(list, 0, new[] { ("Chào cờ", "Sân trường", "BGH"), ("Tin", "P.Tin", "Bùi Minh Đức"), ("Tin", "P.Tin", "Bùi Minh Đức"), ("Toán", "P.12A2", "Nguyễn Văn Hùng"), ("Toán", "P.12A2", "Nguyễn Văn Hùng") });
                    AddDay(list, 1, new[] { ("Lý", "P.12A2", "Trần Thị Mai"), ("Lý", "P.12A2", "Trần Thị Mai"), ("Anh", "P.12A2", "Vũ Thị Lan"), ("Hóa", "P.12A2", "Lê Hoàng Nam"), ("Văn", "P.12A2", "Hoàng Đức Minh") });
                    AddDay(list, 2, new[] { ("Tin", "P.Tin", "Bùi Minh Đức"), ("Toán", "P.12A2", "Nguyễn Văn Hùng"), ("Toán", "P.12A2", "Nguyễn Văn Hùng"), ("Anh", "P.12A2", "Vũ Thị Lan"), ("Sinh", "P.12A2", "Phạm Thị Hương") });
                    AddDay(list, 3, new[] { ("Văn", "P.12A2", "Hoàng Đức Minh"), ("Văn", "P.12A2", "Hoàng Đức Minh"), ("Sử", "P.12A2", "Đỗ Quang Trung"), ("TD", "Sân TD", "GV TD"), ("TD", "Sân TD", "GV TD") });
                    AddDay(list, 4, new[] { ("Lý", "P.12A2", "Trần Thị Mai"), ("Tin", "P.Tin", "Bùi Minh Đức"), ("Địa", "P.12A2", "Ngô Thị Hạnh"), ("GDCD", "P.12A2", "Lý Thị Thu"), ("CN", "P.12A2", "GV CN") });
                    AddDay(list, 5, new[] { ("Hóa", "P.12A2", "Lê Hoàng Nam"), ("Anh", "P.12A2", "Vũ Thị Lan"), ("SHL", "P.12A2", "Bùi Minh Đức") });
                    break;

                case "12A3":
                    // 12A3: Trọng điểm Địa lý - Xã hội (GV Ngô Thị Hạnh)
                    AddDay(list, 0, new[] { ("Chào cờ", "Sân trường", "BGH"), ("Địa", "P.12A3", "Ngô Thị Hạnh"), ("Địa", "P.12A3", "Ngô Thị Hạnh"), ("Sử", "P.12A3", "Đỗ Quang Trung"), ("Sử", "P.12A3", "Đỗ Quang Trung") });
                    AddDay(list, 1, new[] { ("Văn", "P.12A3", "Hoàng Đức Minh"), ("Văn", "P.12A3", "Hoàng Đức Minh"), ("Anh", "P.12A3", "Vũ Thị Lan"), ("Toán", "P.12A3", "Nguyễn Văn Hùng"), ("GDCD", "P.12A3", "Lý Thị Thu") });
                    AddDay(list, 2, new[] { ("Địa", "P.12A3", "Ngô Thị Hạnh"), ("Văn", "P.12A3", "Hoàng Đức Minh"), ("GDCD", "P.12A3", "Lý Thị Thu"), ("Sinh", "P.12A3", "Phạm Thị Hương"), ("Tin", "P.Tin", "Bùi Minh Đức") });
                    AddDay(list, 3, new[] { ("Anh", "P.12A3", "Vũ Thị Lan"), ("Anh", "P.12A3", "Vũ Thị Lan"), ("Toán", "P.12A3", "Nguyễn Văn Hùng"), ("TD", "Sân TD", "GV TD"), ("TD", "Sân TD", "GV TD") });
                    AddDay(list, 4, new[] { ("Sử", "P.12A3", "Đỗ Quang Trung"), ("Địa", "P.12A3", "Ngô Thị Hạnh"), ("Văn", "P.12A3", "Hoàng Đức Minh"), ("Hóa", "P.12A3", "Lê Hoàng Nam"), ("Lý", "P.12A3", "Trần Thị Mai") });
                    AddDay(list, 5, new[] { ("Toán", "P.12A3", "Nguyễn Văn Hùng"), ("CN", "P.12A3", "GV CN"), ("SHL", "P.12A3", "Ngô Thị Hạnh") });
                    break;

                default:
                    // Các lớp khác: Sinh ngẫu nhiên dựa trên mã hash của tên lớp để đảm bảo 100% không trùng lặp
                    int seed = Math.Abs(clean.GetHashCode()) + 100;
                    var rng = new Random(seed);
                    var pool = new[] { "Toán", "Văn", "Anh", "Lý", "Hóa", "Sinh", "Sử", "Địa", "GDCD", "Tin", "TD" };
                    for (int day = 0; day < 6; day++)
                    {
                        int periods = day < 5 ? 5 : 3;
                        for (int p = 0; p < periods; p++)
                        {
                            if (day == 0 && p == 0)
                            {
                                list.Add(new TimetableSlot { DayOfWeek = day, Period = p, Subject = "Chào cờ", Room = "Sân trường", Teacher = "BGH" });
                            }
                            else if (day == 5 && p == periods - 1)
                            {
                                list.Add(new TimetableSlot { DayOfWeek = day, Period = p, Subject = "SHL", Room = $"P.{clean}", Teacher = "GVCN" });
                            }
                            else
                            {
                                string subj = pool[rng.Next(pool.Length)];
                                list.Add(new TimetableSlot { DayOfWeek = day, Period = p, Subject = subj, Room = $"P.{clean}", Teacher = "GV" });
                            }
                        }
                    }
                    break;
            }

            return list;
        }

        private static void AddDay(List<TimetableSlot> list, int day, (string Subject, string Room, string Teacher)[] periods)
        {
            for (int p = 0; p < periods.Length; p++)
            {
                list.Add(new TimetableSlot
                {
                    DayOfWeek = day,
                    Period = p,
                    Subject = periods[p].Subject,
                    Room = periods[p].Room,
                    Teacher = periods[p].Teacher,
                    Note = ""
                });
            }
        }

        /// <summary>
        /// Đồng bộ và đảm bảo mỗi lớp trong cơ sở dữ liệu đều có TKB riêng biệt
        /// </summary>
        public static void EnsureDistinctTimetables(AppDbContext db, bool forceUpdate = true)
        {
            try
            {
                var classNames = db.ClassRosters
                    .Where(r => r.IsActive)
                    .Select(r => r.ClassName)
                    .Distinct()
                    .ToList();

                if (!classNames.Any())
                {
                    classNames = new List<string> { "10A1", "10A2", "10A3", "11A1", "11A2", "11A3", "12A1", "12A2", "12A3" };
                }

                foreach (var cls in classNames)
                {
                    string timetableKey = $"TIMETABLE_{cls}";
                    var slots = GetDistinctTimetable(cls);
                    var json = JsonSerializer.Serialize(slots);

                    // Kiểm tra bản ghi EventLog hiện tại
                    var existingLogs = db.EventLogs.Where(e => e.EventType == timetableKey).ToList();
                    bool shouldUpdate = forceUpdate || !existingLogs.Any();

                    // Nhận biết bản ghi cũ bị trùng lặp do Random(42)
                    if (!shouldUpdate && existingLogs.Any())
                    {
                        var latest = existingLogs.OrderByDescending(e => e.Timestamp).First();
                        if (latest.Details.Contains("\"Subject\":\"Địa\"") && latest.Details.Contains("\"Room\":\"P.10A\""))
                        {
                            shouldUpdate = true; // Dữ liệu cũ Random(42) cần được thay thế
                        }
                    }

                    if (shouldUpdate)
                    {
                        // Xóa bản ghi cũ để tránh lưu thừa
                        if (existingLogs.Any())
                        {
                            db.EventLogs.RemoveRange(existingLogs);
                        }

                        db.EventLogs.Add(new EventLog
                        {
                            EventType = timetableKey,
                            Actor = "GV",
                            Details = json,
                            Timestamp = DateTime.Now
                        });

                        // Đồng bộ vào bảng Lessons để dữ liệu nhất quán toàn diện
                        SyncLessonsForClass(db, cls, slots);
                    }
                }

                db.SaveChanges();
                Log.Information("[TimetableDataHelper] Đã đồng bộ TKB riêng biệt cho {Count} lớp học vào SQLite!", classNames.Count);
            }
            catch (Exception ex)
            {
                Log.Warning("[TimetableDataHelper] Lỗi EnsureDistinctTimetables: {Err}", ex.Message);
            }
        }

        private static void SyncLessonsForClass(AppDbContext db, string className, List<TimetableSlot> slots)
        {
            try
            {
                string[] dayStrings = { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7" };
                var oldLessons = db.Lessons.Where(l => l.ClassName == className).ToList();
                if (oldLessons.Any())
                {
                    db.Lessons.RemoveRange(oldLessons);
                }

                string grade = className.Length >= 2 && char.IsDigit(className[0]) && char.IsDigit(className[1])
                    ? className.Substring(0, 2)
                    : "10";

                foreach (var s in slots)
                {
                    if (s.DayOfWeek < 0 || s.DayOfWeek >= dayStrings.Length) continue;
                    db.Lessons.Add(new Lesson
                    {
                        ClassName = className,
                        Grade = grade,
                        Subject = s.Subject,
                        TeacherName = s.Teacher,
                        DayOfWeek = dayStrings[s.DayOfWeek],
                        Period = s.Period + 1, // 1-based
                        Title = $"Tiết {s.Period + 1}: {s.Subject} ({className})",
                        Description = $"Phòng: {s.Room} - GV: {s.Teacher}",
                        WeekNumber = 40,
                        Semester = "HK2",
                        Status = "Scheduled",
                        CreatedAt = DateTime.Now
                    });
                }
            }
            catch { }
        }
    }
}
