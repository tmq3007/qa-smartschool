using System.Collections.Generic;

namespace SmartLibrary.Desktop.Services
{
    public static class BookLookupService
    {
        private static readonly Dictionary<int, int> _lookupCounts = new();

        static BookLookupService()
        {
            // Seed dữ liệu mẫu để sơ đồ nhiệt ban đầu phong phú
            _lookupCounts[1] = 25; // Đắc Nhân Tâm
            _lookupCounts[2] = 14; // Nhà Giả Kim
            _lookupCounts[3] = 4;  // Số Đỏ
            _lookupCounts[4] = 8;  // Toán Cao Cấp
            _lookupCounts[5] = 18; // Vật Lý Đại Cương
            _lookupCounts[6] = 35; // Harry Potter
            _lookupCounts[7] = 3;  // Tắt Đèn
            _lookupCounts[8] = 11; // Lược Sử Thời Gian
        }

        public static void RecordLookup(int bookId)
        {
            if (!_lookupCounts.ContainsKey(bookId))
                _lookupCounts[bookId] = 0;
            _lookupCounts[bookId]++;
        }

        public static int GetLookupCount(int bookId)
        {
            return _lookupCounts.TryGetValue(bookId, out int count) ? count : 0;
        }
    }
}
