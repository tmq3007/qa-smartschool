using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartClass.Services
{
    public class FamilyQuizQuestion
    {
        public string Question { get; set; } = string.Empty;
        public string[] Options { get; set; } = Array.Empty<string>();
        public int CorrectIndex { get; set; }
    }

    public class FamilyLeaderboardEntry
    {
        public int ParentId { get; set; }
        public int HighestScore { get; set; }
        public DateTime LastPlayed { get; set; }
    }

    public class FamilyGameService
    {
        private readonly AppDbContext _db;

        public FamilyGameService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public List<FamilyQuizQuestion> GenerateQuizForParent(int parentId, int studentId)
        {
            try
            {
                var student = _db.Students.Find(studentId);
                if (student == null) return GetMockQuiz();

                var grades = _db.StudentGrades.Where(g => g.StudentId == studentId).ToList();
                var studentClassRosters = _db.ClassRosters.Where(r => r.ClassName == student.ClassName).ToList();
                var rosterIds = studentClassRosters.Select(r => r.Id).ToList();

                // Câu 1: Môn học con thích nhất là gì? (Tính bằng điểm trung bình cao nhất)
                var favSubject = grades.Where(g => rosterIds.Contains(g.RosterId))
                                       .Join(studentClassRosters, g => g.RosterId, r => r.Id, (g, r) => new { r.Subject, g.Score })
                                       .GroupBy(x => x.Subject)
                                       .Select(g => new { Subject = g.Key, Avg = g.Average(x => x.Score) })
                                       .OrderByDescending(x => x.Avg)
                                       .Select(x => x.Subject)
                                       .FirstOrDefault() ?? "Toán";
                var q1Opts = new List<string> { "Toán", "Ngữ Văn", "Tiếng Anh", "Thể dục" };
                if (!q1Opts.Contains(favSubject)) q1Opts[0] = favSubject;
                int c1 = q1Opts.IndexOf(favSubject);

                // Câu 2: Thứ mấy con có tiết Thể dục? (Từ thời khóa biểu)
                var gymRoster = studentClassRosters.FirstOrDefault(r => r.Subject.Contains("Thể dục") || r.Subject.ToLower().Contains("gym"));
                int dayVal = 4; // Mặc định thứ 4
                if (gymRoster != null)
                {
                    var tt = _db.TimetableEntries.FirstOrDefault(t => t.RosterId == gymRoster.Id);
                    if (tt != null) dayVal = tt.DayOfWeek;
                }
                string dayName = dayVal switch
                {
                    2 => "Thứ 2",
                    3 => "Thứ 3",
                    4 => "Thứ 4",
                    5 => "Thứ 5",
                    6 => "Thứ 6",
                    7 => "Thứ 7",
                    _ => "Thứ 4"
                };
                var q2Opts = new List<string> { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5" };
                if (!q2Opts.Contains(dayName)) q2Opts[2] = dayName;
                int c2 = q2Opts.IndexOf(dayName);

                // Câu 3: Điểm môn Tiếng Anh gần nhất của con?
                var engRoster = studentClassRosters.FirstOrDefault(r => r.Subject.Contains("Tiếng Anh") || r.Subject.ToLower().Contains("english") || r.Subject.ToLower().Contains("anh"));
                double engScore = 8.0;
                if (engRoster != null)
                {
                    var lastEng = grades.Where(g => g.RosterId == engRoster.Id).OrderByDescending(g => g.UpdatedAt).FirstOrDefault();
                    if (lastEng != null) engScore = lastEng.Score;
                }
                int roundedScore = (int)Math.Round(engScore);
                if (roundedScore < 5) roundedScore = 5;
                if (roundedScore > 10) roundedScore = 10;
                var q3Opts = new List<string> { "7", "8", "9", "10" };
                string scoreStr = roundedScore.ToString();
                if (!q3Opts.Contains(scoreStr)) q3Opts[1] = scoreStr;
                int c3 = q3Opts.IndexOf(scoreStr);

                // Câu 4: Con hay chơi môn thể thao nào? (Từ câu lạc bộ học sinh tham gia)
                var studentClubs = _db.ClubMembers.Where(m => m.StudentId == studentId).Select(m => m.ClubId).ToList();
                var sportsClub = _db.Clubs.FirstOrDefault(c => studentClubs.Contains(c.Id) && (c.Category == "Thể thao" || c.Name.ToLower().Contains("bóng") || c.Name.ToLower().Contains("cầu")));
                string sportName = sportsClub != null ? sportsClub.Name.Replace("CLB", "").Replace("Câu lạc bộ", "").Trim() : "Bóng đá";
                var q4Opts = new List<string> { "Bóng đá", "Bơi lội", "Bóng rổ", "Cầu lông" };
                if (!q4Opts.Contains(sportName)) q4Opts[0] = sportName;
                int c4 = q4Opts.IndexOf(sportName);

                // Câu 5: Giáo viên chủ nhiệm của con tên là gì?
                var shcnRoster = studentClassRosters.FirstOrDefault(r => r.Subject == "Sinh hoạt" || r.Subject.Contains("Chủ nhiệm"));
                string gvcn = shcnRoster != null ? shcnRoster.TeacherName : (studentClassRosters.FirstOrDefault()?.TeacherName ?? "Cô Lan");
                var q5Opts = new List<string> { "Cô Hoa", "Thầy Minh", "Cô Lan", "Thầy Tuấn" };
                if (!q5Opts.Contains(gvcn)) q5Opts[2] = gvcn;
                int c5 = q5Opts.IndexOf(gvcn);

                return new List<FamilyQuizQuestion>
                {
                    new FamilyQuizQuestion { Question = "Môn học con thích nhất là gì?", Options = q1Opts.ToArray(), CorrectIndex = c1 },
                    new FamilyQuizQuestion { Question = "Thứ mấy con có tiết Thể dục?", Options = q2Opts.ToArray(), CorrectIndex = c2 },
                    new FamilyQuizQuestion { Question = "Điểm môn Tiếng Anh gần nhất của con?", Options = q3Opts.ToArray(), CorrectIndex = c3 },
                    new FamilyQuizQuestion { Question = "Con hay chơi môn thể thao nào?", Options = q4Opts.ToArray(), CorrectIndex = c4 },
                    new FamilyQuizQuestion { Question = "Giáo viên chủ nhiệm của con tên là gì?", Options = q5Opts.ToArray(), CorrectIndex = c5 }
                };
            }
            catch (Exception ex)
            {
                Log.Warning("FamilyGameService: GenerateQuizForParent failed, using mock. Error: {Err}", ex.Message);
                return GetMockQuiz();
            }
        }

        private List<FamilyQuizQuestion> GetMockQuiz()
        {
            return new List<FamilyQuizQuestion>
            {
                new FamilyQuizQuestion { Question = "Môn học con thích nhất là gì?", Options = new[] { "Toán", "Ngữ Văn", "Tiếng Anh", "Thể dục" }, CorrectIndex = 0 },
                new FamilyQuizQuestion { Question = "Thứ mấy con có tiết Thể dục?", Options = new[] { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5" }, CorrectIndex = 2 },
                new FamilyQuizQuestion { Question = "Điểm môn Tiếng Anh gần nhất của con?", Options = new[] { "7", "8", "9", "10" }, CorrectIndex = 1 },
                new FamilyQuizQuestion { Question = "Con hay chơi môn thể thao nào?", Options = new[] { "Bóng đá", "Bơi lội", "Bóng rổ", "Cầu lông" }, CorrectIndex = 0 },
                new FamilyQuizQuestion { Question = "Giáo viên chủ nhiệm của con tên là gì?", Options = new[] { "Cô Hoa", "Thầy Minh", "Cô Lan", "Thầy Tuấn" }, CorrectIndex = 2 }
            };
        }

        public bool SaveGameSession(int parentId, int studentId, int score)
        {
            try
            {
                _db.FamilyGameSessions.Add(new FamilyGameSession
                {
                    ParentId = parentId,
                    StudentId = studentId,
                    Score = score,
                    CompletedAt = DateTime.Now
                });
                _db.SaveChanges();
                Log.Information("FamilyGameService: Saved session for Parent {ParentId}, Score {Score}", parentId, score);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "FamilyGameService: Save failed");
                return false;
            }
        }

        public List<FamilyLeaderboardEntry> GetLeaderboard()
        {
            try
            {
                return _db.FamilyGameSessions
                    .GroupBy(s => s.ParentId)
                    .Select(g => new FamilyLeaderboardEntry
                    {
                        ParentId = g.Key,
                        HighestScore = g.Max(x => x.Score),
                        LastPlayed = g.Max(x => x.CompletedAt)
                    })
                    .OrderByDescending(x => x.HighestScore)
                    .Take(10)
                    .ToList();
            }
            catch { return new List<FamilyLeaderboardEntry>(); }
        }
    }
}

