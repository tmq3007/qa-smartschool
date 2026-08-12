using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;
using SmartLibrary.Desktop.Helpers;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class ReadingSignageViewModel : ObservableObject, IActiveAwareViewModel
    {
        private readonly ApiService _apiService;
        private DispatcherTimer? _timer;
        private int _dataRefreshCounter = 0;
        private readonly List<FeaturedReviewDto> _featuredReviews = new();

        [ObservableProperty]
        private int _currentSlideIndex = 0;

        [ObservableProperty]
        private string _currentQuote = "Sách là ngọn hải đăng soi sáng con đường tri thức.";

        [ObservableProperty]
        private string _currentQuoteAuthor = "Danh ngôn học đường";

        public ObservableCollection<SignageStudentItem> TopStudents { get; } = new();
        public ObservableCollection<SignageClassItem> TopClasses { get; } = new();

        private readonly string[] _quotes = new[]
        {
            "Sách là ngọn hải đăng soi sáng con đường tri thức.",
            "Việc đọc rất quan trọng. Nếu bạn biết cách đọc, cả thế giới sẽ mở ra cho bạn.",
            "Một cuốn sách hay trên giá sách là một người bạn dù quay lưng lại vẫn là bạn tốt.",
            "Học tập là hạt chọn lọc của tri thức, tri thức là khởi nguồn của hạnh phúc.",
            "Đọc sách là cách nhanh nhất để trò chuyện với những bộ óc vĩ đại nhất của các thế kỷ đã qua.",
            "Không có gì có thể thay thế văn hóa đọc, bởi nó định hình tư duy sâu sắc của con người.",
            "Một người không đọc sách chẳng hơn gì một kẻ không biết đọc.",
            "Những gì bạn đọc hôm nay sẽ quyết định con người bạn ngày mai.",
            "Sách không chỉ chứa đựng kiến thức mà còn nuôi dưỡng tâm hồn và định hướng nhân cách.",
            "Học không biết chán, dạy người không biết mệt mỏi chính là con đường của bậc hiền nhân."
        };

        private readonly string[] _quoteAuthors = new[]
        {
            "Khuyết danh",
            "Barack Obama",
            "Thế giới",
            "Ngạn ngữ",
            "René Descartes",
            "Albert Einstein",
            "Mark Twain",
            "Jim Rohn",
            "Socrates",
            "Khổng Tử"
        };

        [RelayCommand]
        private void ChangeSlide(object parameter)
        {
            if (parameter != null && int.TryParse(parameter.ToString(), out int index))
            {
                if (index >= 0 && index < 3)
                {
                    CurrentSlideIndex = index;
                    _dataRefreshCounter = 0; // Reset counter on manual slide change
                }
            }
        }

        public ReadingSignageViewModel(ApiService apiService)
        {
            _apiService = apiService;
            SetupTimer();
            _ = LoadDataAsync();
        }

        public void Activate()
        {
            Cleanup();
            SetupTimer();
            _ = LoadDataAsync();
        }

        private void SetupTimer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            CurrentSlideIndex = (CurrentSlideIndex + 1) % 3;

            if (CurrentSlideIndex == 2)
            {
                FeaturedReviewDto? selectedReview = null;
                lock (_featuredReviews)
                {
                    if (_featuredReviews.Count > 0)
                    {
                        var rand = new Random();
                        selectedReview = _featuredReviews[rand.Next(_featuredReviews.Count)];
                    }
                }

                if (selectedReview != null)
                {
                    string content = selectedReview.Content ?? "";
                    if (content.Length > 180)
                    {
                        content = content.Substring(0, 177) + "...";
                    }
                    CurrentQuote = content;
                    CurrentQuoteAuthor = $"{selectedReview.BorrowerName} — Nhận xét sách '{selectedReview.BookTitle}'";
                }
                else
                {
                    var rand = new Random();
                    int idx = rand.Next(_quotes.Length);
                    CurrentQuote = _quotes[idx];
                    CurrentQuoteAuthor = _quoteAuthors[idx];
                }
            }

            _dataRefreshCounter++;
            if (_dataRefreshCounter >= 90) // 90 * 10 seconds = 15 minutes
            {
                _dataRefreshCounter = 0;
                _ = LoadDataAsync();
            }
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var reviews = await _apiService.GetAsync<FeaturedReviewDto[]>(ApiEndpoints.FeaturedReviews);
                lock (_featuredReviews)
                {
                    _featuredReviews.Clear();
                    if (reviews != null && reviews.Length > 0)
                    {
                        foreach (var r in reviews)
                        {
                            _featuredReviews.Add(r);
                        }
                    }
                    else
                    {
                        // Server returned empty list or null, load mocks as fallback to keep slide active
                        LoadMockFeaturedReviews();
                    }
                }
            }
            catch (Exception ex)
            {
                _ = AuditLogService.WriteLogAsync("Bảng trình chiếu", $"Lỗi tải đánh giá tiêu điểm từ máy chủ: {ex.Message}. Đang nạp đánh giá tiêu điểm offline.", false);
                lock (_featuredReviews)
                {
                    _featuredReviews.Clear();
                    LoadMockFeaturedReviews();
                }
            }

            try
            {
                var response = await _apiService.GetAsync<SignageLeaderboardResponse>("/Reports/leaderboard");
                if (response != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        TopStudents.Clear();
                        if (response.TopStudents != null)
                        {
                            int rank = 1;
                            foreach (var student in response.TopStudents.Take(5))
                            {
                                student.IsRank1 = (rank == 1);
                                TopStudents.Add(student);
                                rank++;
                            }
                        }

                        TopClasses.Clear();
                        if (response.TopClasses != null)
                        {
                            foreach (var cls in response.TopClasses.Take(3))
                            {
                                TopClasses.Add(cls);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _ = AuditLogService.WriteLogAsync("Bảng trình chiếu", $"Lỗi tải dữ liệu bảng xếp hạng trình chiếu từ máy chủ: {ex.Message}. Đang nạp dữ liệu offline.", false);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (TopStudents.Count == 0)
                    {
                        TopStudents.Add(new SignageStudentItem { StudentName = "Nguyễn Văn An", ClassName = "10A1", BooksCount = 12, IsRank1 = true });
                        TopStudents.Add(new SignageStudentItem { StudentName = "Trần Thị Bình", ClassName = "11B2", BooksCount = 9 });
                        TopStudents.Add(new SignageStudentItem { StudentName = "Lê Hoàng Nam", ClassName = "12C1", BooksCount = 8 });
                        TopStudents.Add(new SignageStudentItem { StudentName = "Phạm Hồng Ngọc", ClassName = "10A3", BooksCount = 7 });
                        TopStudents.Add(new SignageStudentItem { StudentName = "Vũ Việt Anh", ClassName = "11B1", BooksCount = 5 });
                    }
                    if (TopClasses.Count == 0)
                    {
                        TopClasses.Add(new SignageClassItem { ClassName = "10A1", BooksCount = 42 });
                        TopClasses.Add(new SignageClassItem { ClassName = "11B2", BooksCount = 31 });
                        TopClasses.Add(new SignageClassItem { ClassName = "12C1", BooksCount = 28 });
                    }
                });
            }
        }

        private void LoadMockFeaturedReviews()
        {
            _featuredReviews.Add(new FeaturedReviewDto
            {
                BorrowerName = "Nguyễn Văn An",
                BorrowerSsoId = "HS001",
                BookTitle = "Đắc Nhân Tâm",
                Content = "Cuốn sách Đắc Nhân Tâm này thực sự thay đổi tư duy của mình rất nhiều! Khuyên các bạn học sinh nên đọc để cải thiện kỹ năng giao tiếp và sống tử tế hơn."
            });
            _featuredReviews.Add(new FeaturedReviewDto
            {
                BorrowerName = "Võ Minh Em",
                BorrowerSsoId = "HS002",
                BookTitle = "Thám tử lừng danh Conan - Tập 95",
                Content = "Truyện Conan đọc giải trí rất tốt, hình ảnh sắc nét, dạy cho chúng ta về tinh thần dũng cảm, tư duy logic phá án cực kỳ xuất sắc."
            });
            _featuredReviews.Add(new FeaturedReviewDto
            {
                BorrowerName = "Phạm Hồng Ngọc",
                BorrowerSsoId = "HS003",
                BookTitle = "Nhà Giả Kim",
                Content = "Một cuốn sách gối đầu giường tuyệt vời! Nhà Giả Kim truyền cảm hứng sâu sắc về khát vọng vươn lên và lắng nghe tiếng gọi từ con tim."
            });
        }

        public void Cleanup()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }
        }
    }

    public class SignageLeaderboardResponse
    {
        public ObservableCollection<SignageStudentItem>? TopStudents { get; set; }
        public ObservableCollection<SignageClassItem>? TopClasses { get; set; }
    }

    public class SignageStudentItem
    {
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public int BooksCount { get; set; }
        public bool IsRank1 { get; set; }
    }

    public class SignageClassItem
    {
        public string ClassName { get; set; } = string.Empty;
        public int BooksCount { get; set; }
    }

    public class FeaturedReviewDto
    {
        public int Id { get; set; }
        public string BorrowerName { get; set; } = string.Empty;
        public string BorrowerSsoId { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
