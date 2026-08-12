using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Serilog;

namespace QASmartClass.StudentClient.Views
{
    public partial class StudentResultsPage : Page
    {
        public StudentResultsPage()
        {
            InitializeComponent();
            Loaded += (_, _) => LoadResults();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadResults();
        }

        private void LoadResults()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.Database == null) return;

                var db = app.Database;

                // Resolve student identity via StudentIdentityService
                var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(db);
                var (currentId, studentCode, className) = identityService.GetCurrentStudent();

                // Load student to get rank
                var student = db.Students.Find(currentId);
                int classRank = 1;
                if (student != null && !string.IsNullOrEmpty(className))
                {
                    classRank = db.Students.Count(s => s.ClassName == className && s.TotalXp > student.TotalXp) + 1;
                }

                var allResults = db.QuizResults
                    .Where(r => r.StudentId == currentId)
                    .ToList();

                if (allResults.Count == 0)
                {
                    txtAvgScore.Text = "—";
                    txtClassRank.Text = !string.IsNullOrEmpty(className) ? $"#{classRank}" : "—";
                    txtTotalDone.Text = "0";
                    txtCorrectRate.Text = "0%";
                    txtNoResults.Visibility = Visibility.Visible;
                    lstRecentResults.Visibility = Visibility.Collapsed;
                    txtPedagogicalAdvice.Text = "Chưa có đủ dữ liệu học tập. Hãy làm bài kiểm tra để nhận lời khuyên học tập nhé!";
                    return;
                }

                txtNoResults.Visibility = Visibility.Collapsed;
                lstRecentResults.Visibility = Visibility.Visible;

                // Calculate stats based on all results
                double avgScore = allResults.Average(r => r.Score / 10.0);
                int totalDone = allResults.Count;
                int correctTotal = allResults.Sum(r => r.CorrectCount);
                int questionsTotal = allResults.Sum(r => r.TotalQuestions);
                double correctRate = questionsTotal > 0 ? (double)correctTotal / questionsTotal * 100.0 : 0.0;

                txtAvgScore.Text = avgScore.ToString("F1");
                txtClassRank.Text = $"#{classRank}";
                txtTotalDone.Text = totalDone.ToString();
                txtCorrectRate.Text = $"{correctRate:F0}%";
                txtPedagogicalAdvice.Text = GetFriendlyAdvice(avgScore, currentId);

                // Get top 10 recent results for display
                var results = allResults
                    .OrderByDescending(r => r.SubmittedAt)
                    .Take(10)
                    .ToList();

                // Bind list
                var quizIds = results.Select(r => r.QuizId).Distinct().ToList();
                var quizzes = db.Quizzes.Where(q => quizIds.Contains(q.Id)).ToDictionary(q => q.Id);

                lstRecentResults.ItemsSource = results.Select(r =>
                {
                    var quiz = quizzes.ContainsKey(r.QuizId) ? quizzes[r.QuizId] : null;
                    double scaledScore = r.Score / 10.0;

                    string colorStart, colorEnd;
                    string ratingText, ratingBgColor, ratingFgColor;

                    if (scaledScore >= 8.0)
                    {
                        colorStart = "#10B981"; // Emerald Green
                        colorEnd = "#34D399";
                        ratingText = "Giỏi";
                        ratingBgColor = "#D1FAE5"; // Emerald Green nhạt
                        ratingFgColor = "#065F46";
                    }
                    else if (scaledScore >= 6.5)
                    {
                        colorStart = "#3B82F6"; // Blue
                        colorEnd = "#60A5FA";
                        ratingText = "Khá";
                        ratingBgColor = "#DBEAFE"; // Blue nhạt
                        ratingFgColor = "#1E40AF";
                    }
                    else if (scaledScore >= 5.0)
                    {
                        colorStart = "#D97706"; // Amber/Orange
                        colorEnd = "#FBBF24";
                        ratingText = "Trung bình";
                        ratingBgColor = "#FEF3C7"; // Amber nhạt
                        ratingFgColor = "#92400E"; // Amber đậm
                    }
                    else
                    {
                        colorStart = "#EF4444"; // Red
                        colorEnd = "#F87171";
                        ratingText = "Cần cải thiện";
                        ratingBgColor = "#FEE2E2"; // Red nhạt
                        ratingFgColor = "#991B1B";
                    }

                    // Format date an toàn chống sai lệch múi giờ
                    string submittedText;
                    var diff = DateTime.Now.Date - r.SubmittedAt.Date;
                    if (diff.Days == 0)
                        submittedText = $"Hôm nay, {r.SubmittedAt:HH:mm}";
                    else if (diff.Days == 1)
                        submittedText = $"Hôm qua, {r.SubmittedAt:HH:mm}";
                    else
                        submittedText = r.SubmittedAt.ToString("dd/MM/yyyy, HH:mm");

                    return new
                    {
                        DisplayScore = scaledScore.ToString("0.#"),
                        QuizTitle = quiz?.Title ?? "Bài kiểm tra",
                        RatingText = ratingText,
                        RatingBgColor = ratingBgColor,
                        RatingFgColor = ratingFgColor,
                        SubmittedAtText = submittedText,
                        ScoreFractionText = $"{r.CorrectCount}/{r.TotalQuestions} câu",
                        ScoreColorStart = colorStart,
                        ScoreColorEnd = colorEnd
                    };
                }).ToList();

                Log.Information("Results loaded: {Count} results, avg={Avg:F1}, rank={Rank}",
                    results.Count, avgScore, classRank);
            }
            catch (Exception ex) { Log.Warning("LoadResults error: {Err}", ex.Message); }
        }

        private string GetFriendlyAdvice(double score, int studentId)
        {
            string[] excellentAdvice = new[]
            {
                "Thật tuyệt vời! Kết quả học tập của em rất xuất sắc. Hãy tiếp tục phát huy và giúp đỡ các bạn cùng tiến bộ nhé!",
                "Thành tích xuất sắc! Em đang làm rất tốt, hãy giữ vững ngọn lửa đam mê và phong độ này nhé!",
                "Rất xuất sắc! Em có tư duy học tập rất tốt, hãy tiếp tục chinh phục những bài tập khó tiếp theo nhé!"
            };

            string[] goodAdvice = new[]
            {
                "Rất tốt! Em đã đạt kết quả Giỏi. Cố gắng trau dồi thêm các phần còn thiếu để vươn tới mức Xuất sắc nhé!",
                "Kết quả rất tốt! Em chỉ cần cẩn thiện hơn một chút ở các câu hỏi logic khó để đạt điểm tối đa nhé!",
                "Phong độ học tập rất cao! Hãy đặt mục tiêu cao hơn nữa ở kỳ thi sắp tới nhé!"
            };

            string[] fairAdvice = new[]
            {
                "Khá tốt! Em có nền tảng học tập vững vàng. Chỉ cần tập trung rèn luyện thêm, em chắc chắn sẽ đạt điểm Giỏi!",
                "Kết quả Khá! Em đã làm tốt phần cơ bản, hãy đọc kỹ đề ở phần nâng cao để bứt phá điểm số nhé!",
                "Tiến bộ rõ rệt! Hãy tiếp tục duy trì đà học tập này để đạt được kết quả cao hơn nữa."
            };

            string[] averageAdvice = new[]
            {
                "Đạt yêu cầu! Em đã vượt qua ngưỡng an toàn. Hãy nỗ lực ôn tập thêm các kiến thức trọng tâm để bứt phá lên mức Khá nhé!",
                "Kết quả Trung bình. Em cần chủ động hỏi bài giáo viên và các bạn khi gặp bài khó để nhanh chóng tiến bộ nhé!",
                "Em đã vượt qua mức trung bình, cố gắng tập trung thêm một chút nữa để đạt điểm Khá nhé!"
            };

            string[] poorAdvice = new[]
            {
                "Cố gắng lên em! Kết quả này chưa phản ánh đúng thực lực của em. Thầy cô và các bạn luôn sẵn sàng đồng hành cùng em ôn tập lại.",
                "Đừng nản chí! Hãy xem lại các lỗi sai trong bài làm để rút kinh nghiệm. Chăm chỉ luyện tập sẽ giúp em cải thiện nhanh chóng!",
                "Hãy tích cực tương tác và làm thêm bài tập để củng cố lại phần kiến thức bị hổng nhé. Em làm được mà!"
            };

            int dayOfYear = DateTime.Now.DayOfYear;
            int seed = studentId + dayOfYear;
            Random rand = new Random(seed);

            if (score >= 9.0)
            {
                return excellentAdvice[rand.Next(excellentAdvice.Length)];
            }
            else if (score >= 8.0)
            {
                return goodAdvice[rand.Next(goodAdvice.Length)];
            }
            else if (score >= 6.5)
            {
                return fairAdvice[rand.Next(fairAdvice.Length)];
            }
            else if (score >= 5.0)
            {
                return averageAdvice[rand.Next(averageAdvice.Length)];
            }
            else
            {
                return poorAdvice[rand.Next(poorAdvice.Length)];
            }
        }
    }
}