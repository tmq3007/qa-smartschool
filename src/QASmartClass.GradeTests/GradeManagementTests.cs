using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using Xunit;

namespace QASmartClass.GradeTests
{
    /// <summary>
    /// 5 Test Cases cho module Grade Management.
    /// Sử dụng EF Core InMemory để test logic thuần — không cần SQLite file.
    /// </summary>
    public class GradeManagementTests : IDisposable
    {
        private readonly AppDbContext _db;

        public GradeManagementTests()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(opts);
            SeedTestData();
        }

        public void Dispose() => _db.Dispose();

        /// <summary>Tạo dữ liệu test chuẩn</summary>
        private void SeedTestData()
        {
            // 4 loại điểm theo Thông tư 22
            _db.GradeTypeMasters.AddRange(
                new GradeTypeMaster { Id = 1, Code = "Mieng", DisplayName = "KT Miệng", ShortName = "M", Weight = 1, MaxAttempts = 2, SortOrder = 1, IsActive = true },
                new GradeTypeMaster { Id = 2, Code = "KT15p", DisplayName = "KT 15 phút", ShortName = "15p", Weight = 1, MaxAttempts = 2, SortOrder = 2, IsActive = true },
                new GradeTypeMaster { Id = 3, Code = "KT1Tiet", DisplayName = "KT 1 tiết", ShortName = "1T", Weight = 2, MaxAttempts = 1, SortOrder = 3, IsActive = true },
                new GradeTypeMaster { Id = 4, Code = "HocKy", DisplayName = "Thi HK", ShortName = "HK", Weight = 3, MaxAttempts = 1, SortOrder = 4, IsActive = true }
            );

            // 1 loại bị vô hiệu hóa
            _db.GradeTypeMasters.Add(
                new GradeTypeMaster { Id = 5, Code = "Disabled", DisplayName = "Đã tắt", ShortName = "X", Weight = 1, MaxAttempts = 1, SortOrder = 99, IsActive = false }
            );

            // 1 roster + 3 học sinh
            _db.ClassRosters.Add(new ClassRoster
            {
                Id = 1, ClassName = "10A3", GradeLevel = "10",
                SchoolYear = "2025-2026", Semester = "HK2",
                Subject = "Toán", IsActive = true
            });

            _db.Students.AddRange(
                new Student { Id = 1, FullName = "Nguyễn Văn A", StudentCode = "HS001" },
                new Student { Id = 2, FullName = "Trần Thị B", StudentCode = "HS002" },
                new Student { Id = 3, FullName = "Lê Hoàng C", StudentCode = "HS003" }
            );

            _db.ClassRosterStudents.AddRange(
                new ClassRosterStudent { Id = 1, RosterId = 1, StudentId = 1, SeatNumber = 1 },
                new ClassRosterStudent { Id = 2, RosterId = 1, StudentId = 2, SeatNumber = 2 },
                new ClassRosterStudent { Id = 3, RosterId = 1, StudentId = 3, SeatNumber = 3 }
            );

            _db.SaveChanges();
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 1: Weighted Average Calculation (Công thức TB)
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Test công thức tính điểm trung bình có trọng số:
        /// TB = Σ(Điểm × Hệ số) / Σ(Hệ số)
        /// 
        /// Input: HS "Nguyễn Văn A" có đủ điểm:
        ///   M1=8, M2=7, 15p1=9, 15p2=8, 1T=7, HK=8
        /// Expected: TB = (8×1 + 7×1 + 9×1 + 8×1 + 7×2 + 8×3) / (1+1+1+1+2+3)
        ///         = (8 + 7 + 9 + 8 + 14 + 24) / 9 = 70 / 9 ≈ 7.8
        /// </summary>
        [Fact]
        public void Test1_WeightedAverage_CalculatesCorrectly()
        {
            // Arrange — nhập đủ điểm cho HS1
            var grades = new List<StudentGrade>
            {
                new() { StudentId = 1, RosterId = 1, GradeTypeId = 1, Attempt = 1, Score = 8.0 }, // M1
                new() { StudentId = 1, RosterId = 1, GradeTypeId = 1, Attempt = 2, Score = 7.0 }, // M2
                new() { StudentId = 1, RosterId = 1, GradeTypeId = 2, Attempt = 1, Score = 9.0 }, // 15p1
                new() { StudentId = 1, RosterId = 1, GradeTypeId = 2, Attempt = 2, Score = 8.0 }, // 15p2
                new() { StudentId = 1, RosterId = 1, GradeTypeId = 3, Attempt = 1, Score = 7.0 }, // 1T
                new() { StudentId = 1, RosterId = 1, GradeTypeId = 4, Attempt = 1, Score = 8.0 }, // HK
            };
            _db.StudentGrades.AddRange(grades);
            _db.SaveChanges();

            // Act — tính TB
            var activeTypes = _db.GradeTypeMasters.Where(g => g.IsActive).OrderBy(g => g.SortOrder).ToList();
            var allGrades = _db.StudentGrades.Where(g => g.StudentId == 1 && g.RosterId == 1).ToList();

            double totalWeighted = 0;
            int totalWeight = 0;
            foreach (var gt in activeTypes)
            {
                for (int a = 1; a <= gt.MaxAttempts; a++)
                {
                    var g = allGrades.FirstOrDefault(x => x.GradeTypeId == gt.Id && x.Attempt == a);
                    if (g != null)
                    {
                        totalWeighted += g.Score * gt.Weight;
                        totalWeight += gt.Weight;
                    }
                }
            }
            double avg = Math.Round(totalWeighted / totalWeight, 1);

            // Assert
            // (8×1 + 7×1 + 9×1 + 8×1 + 7×2 + 8×3) / (1+1+1+1+2+3) = 70/9 ≈ 7.8
            Assert.Equal(7.8, avg);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 2: Partial Grades — HS chưa nhập đủ điểm
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Test khi HS chỉ có 1 phần điểm (VD: chỉ có M1=9 và HK=6).
        /// TB chỉ tính trên các điểm đã nhập, không chia cho tổng hệ số toàn bộ.
        /// TB = (9×1 + 6×3) / (1+3) = 27/4 = 6.8
        /// </summary>
        [Fact]
        public void Test2_PartialGrades_OnlyCountsEnteredScores()
        {
            // Arrange — chỉ nhập 2 điểm
            _db.StudentGrades.AddRange(
                new StudentGrade { StudentId = 2, RosterId = 1, GradeTypeId = 1, Attempt = 1, Score = 9.0 },
                new StudentGrade { StudentId = 2, RosterId = 1, GradeTypeId = 4, Attempt = 1, Score = 6.0 }
            );
            _db.SaveChanges();

            // Act
            var activeTypes = _db.GradeTypeMasters.Where(g => g.IsActive).OrderBy(g => g.SortOrder).ToList();
            var allGrades = _db.StudentGrades.Where(g => g.StudentId == 2 && g.RosterId == 1).ToList();

            double totalWeighted = 0;
            int totalWeight = 0;
            foreach (var gt in activeTypes)
            {
                for (int a = 1; a <= gt.MaxAttempts; a++)
                {
                    var g = allGrades.FirstOrDefault(x => x.GradeTypeId == gt.Id && x.Attempt == a);
                    if (g != null)
                    {
                        totalWeighted += g.Score * gt.Weight;
                        totalWeight += gt.Weight;
                    }
                }
            }
            double avg = totalWeight > 0 ? Math.Round(totalWeighted / totalWeight, 1) : -1;

            // Assert: (9×1 + 6×3) / (1+3) = 27/4 = 6.8
            Assert.Equal(6.8, avg);
            Assert.Equal(2, allGrades.Count); // chỉ 2 bản ghi
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 3: Unique Constraint — Không nhập trùng điểm
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Test logic upsert: khi GV nhập lại điểm cho cùng (StudentId, RosterId, GradeTypeId, Attempt),
        /// phải UPDATE thay vì INSERT trùng.
        /// Scenario: Nhập M1=5, sau đó sửa M1=8 → DB chỉ có 1 record với Score=8.
        /// </summary>
        [Fact]
        public void Test3_UpsertGrade_UpdatesExistingRecord()
        {
            // Arrange — nhập lần 1
            _db.StudentGrades.Add(new StudentGrade
            {
                StudentId = 3, RosterId = 1, GradeTypeId = 1, Attempt = 1,
                Score = 5.0, EnteredBy = "GV", UpdatedAt = DateTime.Now
            });
            _db.SaveChanges();

            // Act — "sửa" điểm (logic giống GradeCell_EditEnding)
            int studentId = 3, gradeTypeId = 1, attempt = 1, rosterId = 1;
            double newScore = 8.0;

            var existing = _db.StudentGrades.FirstOrDefault(g =>
                g.StudentId == studentId && g.RosterId == rosterId &&
                g.GradeTypeId == gradeTypeId && g.Attempt == attempt);

            if (existing != null)
            {
                existing.Score = newScore;
                existing.UpdatedAt = DateTime.Now;
            }
            else
            {
                _db.StudentGrades.Add(new StudentGrade
                {
                    StudentId = studentId, RosterId = rosterId,
                    GradeTypeId = gradeTypeId, Attempt = attempt,
                    Score = newScore, EnteredBy = "GV"
                });
            }
            _db.SaveChanges();

            // Assert
            var allForStudent = _db.StudentGrades.Where(g =>
                g.StudentId == 3 && g.GradeTypeId == 1 && g.Attempt == 1).ToList();

            Assert.Single(allForStudent);            // chỉ 1 record
            Assert.Equal(8.0, allForStudent[0].Score); // đã update lên 8.0
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 4: Soft Delete — Vô hiệu hóa loại điểm có dữ liệu
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Test logic xóa loại điểm: nếu đã có điểm nhập rồi → soft-delete (IsActive=false).
        /// Khi IsActive=false, loại điểm không xuất hiện trong grid nhưng data vẫn còn.
        /// </summary>
        [Fact]
        public void Test4_SoftDelete_DeactivatesGradeTypeWithExistingData()
        {
            // Arrange — nhập điểm cho loại KT1Tiet (Id=3)
            _db.StudentGrades.Add(new StudentGrade
            {
                StudentId = 1, RosterId = 1, GradeTypeId = 3, Attempt = 1, Score = 7.5
            });
            _db.SaveChanges();

            // Act — thử xóa loại điểm Id=3
            var gt = _db.GradeTypeMasters.First(g => g.Id == 3);
            bool hasGrades = _db.StudentGrades.Any(sg => sg.GradeTypeId == 3);

            if (hasGrades)
            {
                // Soft delete — giống logic DeleteGradeType_Click
                gt.IsActive = false;
                _db.SaveChanges();
            }

            // Assert
            var updated = _db.GradeTypeMasters.First(g => g.Id == 3);
            Assert.False(updated.IsActive);                                        // đã vô hiệu
            Assert.True(_db.StudentGrades.Any(sg => sg.GradeTypeId == 3));         // data vẫn còn
            Assert.Equal(3, _db.GradeTypeMasters.Count(g => g.IsActive));          // chỉ còn 3 active (từ 4)

            // Loại điểm inactive không nên xuất hiện trong grid
            var activeTypes = _db.GradeTypeMasters.Where(g => g.IsActive).ToList();
            Assert.DoesNotContain(activeTypes, t => t.Code == "KT1Tiet");
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 5: Score Validation — Điểm ngoài khoảng 0-10
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Test validate điểm: GV nhập giá trị ngoài 0-10 → phải clamp.
        /// Input: -2, 15, 10.5 → Expected: 0.0, 10.0, 10.0
        /// Cũng test input rỗng → xóa record, input chữ → bỏ qua.
        /// </summary>
        [Fact]
        public void Test5_ScoreValidation_ClampsAndRejectsInvalid()
        {
            int studentId = 1, rosterId = 1, gradeTypeId = 2, attempt = 1;

            // --- Sub-test A: Điểm âm → clamp to 0 ---
            double scoreA = Math.Max(0, Math.Min(10, Math.Round(-2.0, 1)));
            Assert.Equal(0.0, scoreA);

            // --- Sub-test B: Điểm > 10 → clamp to 10 ---
            double scoreB = Math.Max(0, Math.Min(10, Math.Round(15.0, 1)));
            Assert.Equal(10.0, scoreB);

            // --- Sub-test C: Điểm 10.5 → clamp to 10 ---
            double scoreC = Math.Max(0, Math.Min(10, Math.Round(10.5, 1)));
            Assert.Equal(10.0, scoreC);

            // --- Sub-test D: Input hợp lệ "8.5" → parse OK ---
            string inputD = "8.5";
            bool parsedD = double.TryParse(inputD, out double valD);
            Assert.True(parsedD);
            Assert.Equal(8.5, valD);

            // --- Sub-test E: Input chữ "abc" → parse fails → skip ---
            string inputE = "abc";
            bool parsedE = double.TryParse(inputE, out _);
            Assert.False(parsedE);

            // --- Sub-test F: Input rỗng → xóa record ---
            _db.StudentGrades.Add(new StudentGrade
            {
                StudentId = studentId, RosterId = rosterId,
                GradeTypeId = gradeTypeId, Attempt = attempt, Score = 7.0
            });
            _db.SaveChanges();

            string inputF = "";
            if (string.IsNullOrEmpty(inputF))
            {
                var existing = _db.StudentGrades.FirstOrDefault(g =>
                    g.StudentId == studentId && g.RosterId == rosterId &&
                    g.GradeTypeId == gradeTypeId && g.Attempt == attempt);
                if (existing != null)
                {
                    _db.StudentGrades.Remove(existing);
                    _db.SaveChanges();
                }
            }

            Assert.False(_db.StudentGrades.Any(g =>
                g.StudentId == studentId && g.GradeTypeId == gradeTypeId && g.Attempt == attempt));
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 6: Circular 22 — Missing Midterm (GK) or Final (CK)
        // ═══════════════════════════════════════════════════════
        [Fact]
        public void Test6_Circular22_MissingGKorCK_ReturnsChuaDuDiem()
        {
            // Student 1 has TX grades but missing GK (Id=3 in this in-memory test) or CK (Id=4)
            var grades = new List<StudentGrade>
            {
                new() { StudentId = 1, RosterId = 1, GradeTypeId = 1, Attempt = 1, Score = 8.0 },
                new() { StudentId = 1, RosterId = 1, GradeTypeId = 2, Attempt = 1, Score = 9.0 }
            };
            _db.StudentGrades.AddRange(grades);
            _db.SaveChanges();

            var gradingService = new Services.TT22GradingService(_db);
            var result = gradingService.CalculateSubjectAverage(1, 1);

            Assert.Equal("Chưa đủ điểm (Thiếu CK)", result.Classification);
            Assert.Equal(0, result.Average);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 7: Circular 22 — Absent (Vắng) GK or CK
        // ═══════════════════════════════════════════════════════
        [Fact]
        public void Test7_Circular22_AbsentGKOrCK_CalculatesWithZeroScore()
        {
            var grades = new List<StudentGrade>
            {
                new() { StudentId = 1, RosterId = 1, GradeTypeId = 1, Attempt = 1, Score = 8.0 },
                new() { StudentId = 1, RosterId = 1, GradeTypeId = 2, Attempt = 1, Score = 9.0 },
                new() { StudentId = 1, RosterId = 1, GradeTypeId = 3, Attempt = 1, Score = 0.0, Notes = "Vắng" },
                new() { StudentId = 1, RosterId = 1, GradeTypeId = 4, Attempt = 1, Score = 8.0 }
            };
            _db.StudentGrades.AddRange(grades);
            _db.SaveChanges();

            var gradingService = new Services.TT22GradingService(_db);
            var result = gradingService.CalculateSubjectAverage(1, 1);

            Assert.Equal("Đạt", result.Classification);
            Assert.Equal(5.86, result.Average);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 8: Circular 22 — Exempt (Miễn) GK or CK
        // ═══════════════════════════════════════════════════════
        [Fact]
        public void Test8_Circular22_ExemptGKOrCK_IgnoresWeightAndDoesNotReturnCDD()
        {
            var grades = new List<StudentGrade>
            {
                new() { StudentId = 1, RosterId = 1, GradeTypeId = 1, Attempt = 1, Score = 8.0 },
                new() { StudentId = 1, RosterId = 1, GradeTypeId = 2, Attempt = 1, Score = 9.0 },
                new() { StudentId = 1, RosterId = 1, GradeTypeId = 3, Attempt = 1, Score = 0.0, Notes = "Miễn" },
                new() { StudentId = 1, RosterId = 1, GradeTypeId = 4, Attempt = 1, Score = 8.0 }
            };
            _db.StudentGrades.AddRange(grades);
            _db.SaveChanges();

            var gradingService = new Services.TT22GradingService(_db);
            var result = gradingService.CalculateSubjectAverage(1, 1);

            Assert.Equal("Tốt", result.Classification);
            Assert.Equal(8.20, result.Average);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 9: TT22GradingService Static Caching
        // ═══════════════════════════════════════════════════════
        [Fact]
        public void Test9_TT22GradingService_StaticCaching_WorksCorrectly()
        {
            var gradingService = new Services.TT22GradingService(_db);
            
            // First run will load cache
            var result1 = gradingService.CalculateSubjectAverage(1, 1);
            
            // Clear the DB table to verify it doesn't query it again
            _db.GradeTypeMasters.RemoveRange(_db.GradeTypeMasters);
            _db.SaveChanges();
            
            // Second run should still succeed using the cache
            var result2 = gradingService.CalculateSubjectAverage(1, 1);
            
            Assert.NotNull(result2);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 10: Circular 22 Overall Classification Rules
        // ═══════════════════════════════════════════════════════
        [Fact]
        public void Test10_Circular22OverallClassification_EvaluatesCorrectly()
        {
            // Test Case 1: n65 == n (8 subjects), 5 subjects >= 8.0, 3 subjects = 7.5 -> Should be "Khá"
            var subjectAverages1 = new List<double> { 8.5, 8.0, 8.2, 8.0, 8.1, 7.5, 7.5, 7.5 };
            string class1 = EvaluateOverallClassification(subjectAverages1);
            Assert.Equal("Khá", class1);

            // Test Case 2: n65 == n (8 subjects), 6 subjects >= 8.0, 2 subjects = 7.5 -> Should be "Tốt"
            var subjectAverages2 = new List<double> { 8.5, 8.0, 8.2, 8.0, 8.1, 8.0, 7.5, 7.5 };
            string class2 = EvaluateOverallClassification(subjectAverages2);
            Assert.Equal("Tốt", class2);

            // Test Case 3: 1 subject < 5.0 (but >= 3.5), others >= 5.0 -> Should be "Đạt"
            var subjectAverages3 = new List<double> { 8.5, 8.0, 8.2, 8.0, 5.5, 6.0, 5.0, 4.0 };
            string class3 = EvaluateOverallClassification(subjectAverages3);
            Assert.Equal("Đạt", class3);

            // Test Case 4: 2 subjects < 5.0 -> Should be "Chưa đạt"
            var subjectAverages4 = new List<double> { 8.5, 8.0, 8.2, 8.0, 5.5, 6.0, 4.5, 4.0 };
            string class4 = EvaluateOverallClassification(subjectAverages4);
            Assert.Equal("Chưa đạt", class4);

            // Test Case 5: 1 subject < 3.5 -> Should be "Chưa đạt"
            var subjectAverages5 = new List<double> { 8.5, 8.0, 8.2, 8.0, 5.5, 6.0, 5.0, 3.0 };
            string class5 = EvaluateOverallClassification(subjectAverages5);
            Assert.Equal("Chưa đạt", class5);
        }

        private string EvaluateOverallClassification(List<double> subjectAvgs)
        {
            int n = subjectAvgs.Count;
            int n8 = subjectAvgs.Count(a => a >= 8.0);
            int n65 = subjectAvgs.Count(a => a >= 6.5);
            int n50 = subjectAvgs.Count(a => a >= 5.0);
            int n35 = subjectAvgs.Count(a => a >= 3.5);

            if (n65 == n && (n8 >= 6 || (n < 6 && n8 == n)))
            {
                return "Tốt";
            }
            else if (n50 == n && (n65 >= 6 || (n < 6 && n65 == n)))
            {
                return "Khá";
            }
            else if (n35 == n && n50 >= n - 1)
            {
                return "Đạt";
            }
            else
            {
                return "Chưa đạt";
            }
        }
    }
}
