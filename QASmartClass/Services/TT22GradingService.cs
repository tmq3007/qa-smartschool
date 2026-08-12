using QASmartClass.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartClass.Services
{
    /// <summary>
    /// V7 P3.1: Tính điểm theo Thông tư 22/2021/TT-BGDĐT.
    /// Hệ số: Thường xuyên x1, Giữa kỳ x2, Cuối kỳ x3.
    /// Xếp loại: Tốt (>=8.0), Khá (>=6.5), Đạt (>=5.0), Chưa đạt (<5.0).
    /// </summary>
    public class TT22GradingService
    {
        private readonly AppDbContext _db;

        private static List<int>? _cachedGkTypeIds;
        private static List<int>? _cachedCkTypeIds;
        private static Dictionary<int, int>? _cachedTypeWeights;
        private static readonly object _cacheLock = new();

        public TT22GradingService(AppDbContext db) { _db = db; }

        private void EnsureCacheLoaded()
        {
            if (_cachedGkTypeIds != null && _cachedCkTypeIds != null && _cachedTypeWeights != null)
                return;

            lock (_cacheLock)
            {
                if (_cachedGkTypeIds != null && _cachedCkTypeIds != null && _cachedTypeWeights != null)
                    return;

                var masters = _db.GradeTypeMasters.ToList();
                _cachedGkTypeIds = masters
                    .Where(t => t.Id == 2 || t.ShortName == "GK" || t.Code == "GK" || t.Code == "KT1Tiet")
                    .Select(t => t.Id)
                    .ToList();
                _cachedCkTypeIds = masters
                    .Where(t => t.Id == 3 || t.ShortName == "CK" || t.Code == "CK" || t.Code == "HocKy")
                    .Select(t => t.Id)
                    .ToList();
                _cachedTypeWeights = masters.ToDictionary(t => t.Id, t => t.Weight);
            }
        }

        /// <summary>Tính DTB môn theo hệ số TT22</summary>
        public SubjectAverageResult CalculateSubjectAverage(int studentId, int rosterId)
        {
            var grades = _db.StudentGrades
                .Where(g => g.StudentId == studentId && g.RosterId == rosterId)
                .ToList();

            if (!grades.Any())
                return new SubjectAverageResult { Average = 0, Classification = "Chưa có điểm", HasData = false };

            EnsureCacheLoaded();
            var gkTypeIds = _cachedGkTypeIds!;
            var ckTypeIds = _cachedCkTypeIds!;

            bool hasGK = grades.Any(g => gkTypeIds.Contains(g.GradeTypeId));
            bool hasCK = grades.Any(g => ckTypeIds.Contains(g.GradeTypeId));

            if (!hasGK || !hasCK)
            {
                string classification = "Chưa đủ điểm";
                if (!hasGK && !hasCK) classification = "Chưa đủ điểm (Thiếu GK, CK)";
                else if (!hasGK) classification = "Chưa đủ điểm (Thiếu GK)";
                else if (!hasCK) classification = "Chưa đủ điểm (Thiếu CK)";

                return new SubjectAverageResult
                {
                    Average = 0,
                    Classification = classification,
                    HasData = true,
                    TotalGrades = grades.Count
                };
            }

            double totalWeighted = 0;
            int totalWeight = 0;

            var typeWeights = _cachedTypeWeights!;

            foreach (var g in grades)
            {
                if (g.Notes == "Miễn") continue;
                int weight = typeWeights.TryGetValue(g.GradeTypeId, out var w) ? w : 1;
                totalWeighted += g.Score * weight;
                totalWeight += weight;
            }

            double avg = totalWeight > 0 ? Math.Round(totalWeighted / totalWeight, 2) : 0;

            return new SubjectAverageResult
            {
                Average = avg,
                Classification = ClassifyAcademic(avg),
                HasData = true,
                TotalGrades = grades.Count
            };
        }

        /// <summary>
        /// Xep loai hoc luc theo TT22.
        /// Tot (>=8.0), Kha (>=6.5), Dat (>=5.0), Chua dat (<5.0)
        /// </summary>
        public static string ClassifyAcademic(double avg)
        {
            if (avg >= 8.0) return "Tốt";
            if (avg >= 6.5) return "Khá";
            if (avg >= 5.0) return "Đạt";
            return "Chưa đạt";
        }

        /// <summary>
        /// Xep loai hanh kiem theo diem (thang 100) theo TT22.
        /// Tot (>=80), Kha (>=65), Dat (>=50), Chua dat (<50)
        /// </summary>
        public static string ClassifyConduct(int conductScore)
        {
            if (conductScore >= 80) return "Tốt";
            if (conductScore >= 65) return "Khá";
            if (conductScore >= 50) return "Đạt";
            return "Chưa đạt";
        }

        /// <summary>He so theo loai diem: TX=1, GK=2, CK=3</summary>
        private static int GetWeight(int gradeTypeId)
        {
            // GradeTypeId mapping: 1=TX, 2=GK, 3=CK (theo GradeTypeMaster)
            return gradeTypeId switch
            {
                1 => 1, // Thuong xuyen
                2 => 2, // Giua ky
                3 => 3, // Cuoi ky
                _ => 1  // Default
            };
        }
    }

    public class SubjectAverageResult
    {
        public double Average { get; set; }
        public string Classification { get; set; } = string.Empty;
        public bool HasData { get; set; }
        public int TotalGrades { get; set; }
    }
}
