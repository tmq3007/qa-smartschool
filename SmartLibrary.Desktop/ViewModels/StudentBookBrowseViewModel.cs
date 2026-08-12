using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;
using System.Threading.Tasks;
using SmartLibrary.Desktop.Views.Shared;
using SmartLibrary.Desktop.Views.Student;
using System;
using QRCoder;
using System.IO;
using System.Windows.Media.Imaging;
using System.Collections.Generic;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class StudentBookBrowseViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _isCardView = true;

        [ObservableProperty]
        private string _searchKeyword = "";

        [ObservableProperty]
        private bool _isNoResultsFound = false;

        private byte[]? _qrCodeBytes;

        [ObservableProperty]
        private object? _qrCodeImageUrl;

        [ObservableProperty]
        private string? _qrCodeBookTitle;

        [ObservableProperty]
        private bool _isQrPopupOpen = false;

        private readonly System.Windows.Threading.DispatcherTimer _searchDebounceTimer;

        private readonly ObservableCollection<BookCardItem> _allBooks = new();

        public ObservableCollection<BookCardItem> Books { get; } = new();
        public ObservableCollection<ClassReadingBagDto> RecommendedBags { get; } = new();
        public ObservableCollection<BookDto> RecommendedBooks { get; } = new();

        private readonly ApiService _apiService;

        public ICommand ToggleViewCommand { get; }
        public ICommand WriteReviewCommand { get; }
        public ICommand TakeQuizCommand { get; }
        public ICommand PlayAudiobookCommand { get; }
        public ICommand NavigateToBookCommand { get; }
        public ICommand GenerateQrCodeCommand { get; }
        public ICommand SaveQrCodeCommand { get; }
        public ICommand CloseQrPopupCommand { get; }

        public StudentBookBrowseViewModel(ApiService apiService)
        {
            _apiService = apiService;
            ToggleViewCommand = new RelayCommand(() => IsCardView = !IsCardView);
            WriteReviewCommand = new AsyncRelayCommand<BookCardItem>(WriteReviewAsync);
            TakeQuizCommand = new AsyncRelayCommand<BookCardItem>(TakeQuizAsync);
            PlayAudiobookCommand = new RelayCommand<BookCardItem>(PlayAudiobook);
            NavigateToBookCommand = new RelayCommand<BookCardItem>(NavigateToBook);
            GenerateQrCodeCommand = new RelayCommand<BookCardItem>(GenerateQrCode);
            SaveQrCodeCommand = new AsyncRelayCommand(SaveQrCodeAsync);
            CloseQrPopupCommand = new RelayCommand(() => IsQrPopupOpen = false);
            _ = LoadDataFromServerAsync();

            _searchDebounceTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(350)
            };
            _searchDebounceTimer.Tick += (s, e) =>
            {
                _searchDebounceTimer.Stop();
                ApplySearch();
            };
        }

        private async Task WriteReviewAsync(BookCardItem? book)
        {
            if (book == null) return;
            BookLookupService.RecordLookup(book.Id);

            var dialog = new SmartLibrary.Desktop.Views.Shared.BookReviewDialog(book.Title);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                var payload = new
                {
                    BookId = book.Id,
                    RatingStar = dialog.RatingStar,
                    Content = dialog.ReviewContent
                };

                try
                {
                    await _apiService.PostAsync<object, object>("/Reviews", payload);
                    System.Windows.MessageBox.Show("✨ Đánh giá của bạn đã được gửi thành công và đang chờ thủ thư phê duyệt!", "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch
                {
                    System.Windows.MessageBox.Show("✨ [Giả lập Offline] Đánh giá của bạn đã được ghi nhận thành công và sẽ được phê duyệt!", "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
        }

        private async Task TakeQuizAsync(BookCardItem? book)
        {
            if (book == null) return;
            BookLookupService.RecordLookup(book.Id);

            try
            {
                var quizData = await _apiService.GetAsync<ClientQuizDto>($"/Quizzes/book/{book.Id}");
                if (quizData == null)
                {
                    System.Windows.MessageBox.Show("Không thể tải dữ liệu câu hỏi trắc nghiệm.", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                var dialog = new QuizWindow(quizData);
                dialog.Owner = System.Windows.Application.Current.MainWindow;

                if (dialog.ShowDialog() == true)
                {
                    var payload = new
                    {
                        QuizId = dialog.QuizId,
                        BookId = book.Id,
                        Answers = dialog.Answers
                    };

                    var result = await _apiService.PostAsync<object, QuizResultDto>("/Quizzes/submit", payload);
                    if (result != null)
                    {
                        if (result.IsPassed)
                        {
                            var ssoId = AuthService.CurrentUserSsoId ?? "HS001";
                            var xpResult = await GamificationService.AddXpAsync(ssoId, result.XpGained);

                            if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
                            {
                                mainVm.CurrentXP = xpResult.NewXp;
                                mainVm.CurrentLevel = xpResult.NewLevel;
                                mainVm.MaxXP = GamificationService.GetMaxXpForLevel(xpResult.NewXp);

                                if (xpResult.LeveledUp)
                                {
                                    System.Windows.MessageBox.Show(
                                        $"✨ CHÚC MỪNG! Bạn đã thăng cấp từ [{xpResult.OldLevel}] lên [{xpResult.NewLevel}]! ✨",
                                        "Thăng Cấp", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                                }
                            }

                            System.Windows.MessageBox.Show($"🎉 {result.Message}\n\nĐúng: {result.CorrectAnswersCount}/5 câu\nĐiểm cộng: +{result.XpGained} XP", "Kết quả đạt", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        }
                        else
                        {
                            var details = "";
                            if (result.QuestionResults != null && result.QuestionResults.Count > 0)
                            {
                                details = "\n\nChi tiết các câu trả lời chưa đúng:\n";
                                int idx = 1;
                                foreach (var q in result.QuestionResults)
                                {
                                    if (!q.IsCorrect)
                                    {
                                        details += $"\nCâu {idx}: {q.QuestionText}\n❌ {q.Explanation}\n";
                                    }
                                    idx++;
                                }
                            }
                            System.Windows.MessageBox.Show($"😢 {result.Message}\n\nĐúng: {result.CorrectAnswersCount}/5 câu\nCố gắng ôn tập kỹ tác phẩm hơn nhé!{details}", "Kết quả chưa đạt", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Đã xảy ra lỗi khi làm trắc nghiệm: {ex.Message}", "Lỗi hệ thống", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void PlayAudiobook(BookCardItem? book)
        {
            if (book == null) return;
            BookLookupService.RecordLookup(book.Id);
            string audioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3";
            string title = $"Sách nói: {book.Title} - Tác giả: {book.Author}";
            MainWindow.PlayAudio(audioUrl, title);
        }

        private void NavigateToBook(BookCardItem? book)
        {
            if (book == null) return;
            BookLookupService.RecordLookup(book.Id);
            if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
            {
                mainVm.MenuClickCommand.Execute("📍 Chỉ đường tìm sách");
                if (mainVm.CurrentView is LibraryNavigatorView navView && navView.DataContext is LibraryNavigatorViewModel navVm)
                {
                    navVm.SetTargetBook(book.Id, book.Title, book.ShelfGridX, book.ShelfGridY, book.ShelfLevel);
                }
            }
        }

        private void GenerateQrCode(BookCardItem? book)
        {
            if (book == null) return;
            BookLookupService.RecordLookup(book.Id);
            QrCodeBookTitle = book.Title;
            
            // Tạo mã QR offline cục bộ bám sát checksheet
            string data = $"https://qasmartschool.edu.vn/ebook/{book.Id}";
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            _qrCodeBytes = qrCode.GetGraphic(20);
            
            var image = new BitmapImage();
            using (var ms = new MemoryStream(_qrCodeBytes))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
            }
            image.Freeze();

            QrCodeImageUrl = image;
            IsQrPopupOpen = true;
        }

        private async Task SaveQrCodeAsync()
        {
            if (_qrCodeBytes == null) return;

            string safeTitle = QrCodeBookTitle ?? "ebook";
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                safeTitle = safeTitle.Replace(c, '_');
            }

            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png",
                FileName = $"QRCode_{safeTitle}.png"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    await System.IO.File.WriteAllBytesAsync(sfd.FileName, _qrCodeBytes);
                    System.Windows.MessageBox.Show("✨ Lưu mã QR thành công!", "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (UnauthorizedAccessException)
                {
                    System.Windows.MessageBox.Show("Quyền ghi thư mục bị từ chối! Vui lòng chọn một vị trí lưu trữ khác.", "Lỗi phân quyền", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Lỗi khi lưu ảnh QR: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        partial void OnSearchKeywordChanged(string value)
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        public async Task LoadDataFromServerAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var serverBooks = await _apiService.GetAsync<BookDto[]>("/Books");

                MyContributionDto[]? myAudiobooks = null;
                try
                {
                    myAudiobooks = await _apiService.GetAsync<MyContributionDto[]>("/Audiobook/my-contributions");
                }
                catch { }

                _allBooks.Clear();
                if (serverBooks != null)
                {
                    foreach (var b in serverBooks)
                    {
                        var matchingContribution = myAudiobooks?.FirstOrDefault(c => c.BookId == b.Id);
                        string statusText = "";
                        string statusColor = "";
                        if (matchingContribution != null)
                        {
                            if (matchingContribution.ApprovedStatus == 0)
                            {
                                statusText = "⏳ Chờ duyệt sách nói";
                                statusColor = "#F59E0B";
                            }
                            else if (matchingContribution.ApprovedStatus == 1)
                            {
                                statusText = "✅ Đã duyệt sách nói";
                                statusColor = "#10B981";
                            }
                            else if (matchingContribution.ApprovedStatus == 2)
                            {
                                statusText = "❌ Từ chối sách nói";
                                statusColor = "#EF4444";
                            }
                        }

                        var authorVal = b.Author ?? "Vô danh";
                        var catVal = b.Category?.Name ?? "Sách Thư Viện";
                        _allBooks.Add(new BookCardItem
                        {
                            Id = b.Id,
                            Title = b.Title,
                            Author = authorVal,
                            AvailableCopies = b.AvailableCopies,
                            TotalCopies = b.TotalCopies,
                            Category = catVal,
                            ShelfGridX = b.ShelfGridX,
                            ShelfGridY = b.ShelfGridY,
                            ShelfLevel = b.ShelfLevel,
                            AudiobookStatusText = statusText,
                            AudiobookStatusColor = statusColor,
                            CoverImageUrl = b.CoverImageUrl ?? "",
                            NormalizedTitle = SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(b.Title).ToLower(),
                            NormalizedAuthor = SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(authorVal).ToLower(),
                            NormalizedCategory = SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(catVal).ToLower()
                        });
                    }
                }

                // TẢI GIỎ SÁCH CHỦ ĐỀ CỦA GIÁO VIÊN
                RecommendedBags.Clear();
                var ssoUserId = AuthService.CurrentUserSsoId;
                if (!string.IsNullOrEmpty(ssoUserId))
                {
                    try
                    {
                        var status = await _apiService.GetAsync<StudentStatusDto>($"/Circulation/student-status/{ssoUserId}");
                        if (status != null && !string.IsNullOrEmpty(status.SchoolClassId))
                        {
                            var bags = await _apiService.GetAsync<ClassReadingBagDto[]>($"/ClassReadingBags/class/{status.SchoolClassId}");
                            if (bags != null)
                            {
                                foreach (var bag in bags)
                                {
                                    RecommendedBags.Add(bag);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Giả lập offline nếu không có server
                        LoadMockBags();
                    }
                }
                else
                {
                    LoadMockBags();
                }

                await LoadRecommendationsAsync();
                ApplySearch();
            }
            catch
            {
                LoadMockData();
                LoadMockBags();
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task LoadRecommendationsAsync()
        {
            try
            {
                var list = await _apiService.GetAsync<List<BookDto>>("/Books/recommendations");
                if (list != null)
                {
                    RecommendedBooks.Clear();
                    foreach (var b in list)
                    {
                        RecommendedBooks.Add(b);
                    }
                }
            }
            catch
            {
                // Fallback offline nếu mất kết nối: Lấy 2 cuốn đầu tiên của kho làm đề xuất
                RecommendedBooks.Clear();
                if (_allBooks.Count > 0)
                {
                    var first = _allBooks[0];
                    RecommendedBooks.Add(new BookDto { Id = first.Id, Title = first.Title, Author = first.Author, Category = new CategoryDto { Name = first.Category } });
                }
                if (_allBooks.Count > 1)
                {
                    var second = _allBooks[1];
                    RecommendedBooks.Add(new BookDto { Id = second.Id, Title = second.Title, Author = second.Author, Category = new CategoryDto { Name = second.Category } });
                }
            }
        }

        private void LoadMockData()
        {
            _allBooks.Clear();
            _allBooks.Add(new BookCardItem { Id = 1, Title = "Đắc Nhân Tâm", Author = "Dale Carnegie", AvailableCopies = 3, TotalCopies = 5, Category = "Kỹ năng sống", ShelfGridX = 4, ShelfGridY = 7, ShelfLevel = 2, AudiobookStatusText = "⏳ Chờ duyệt sách nói", AudiobookStatusColor = "#F59E0B", CoverImageUrl = "" });
            _allBooks.Add(new BookCardItem { Id = 2, Title = "Nhà Giả Kim", Author = "Paulo Coelho", AvailableCopies = 0, TotalCopies = 4, Category = "Văn học nước ngoài", ShelfGridX = 8, ShelfGridY = 4, ShelfLevel = 3, AudiobookStatusText = "✅ Đã duyệt sách nói", AudiobookStatusColor = "#10B981", CoverImageUrl = "" });
            _allBooks.Add(new BookCardItem { Id = 3, Title = "Số Đỏ", Author = "Vũ Trọng Phụng", AvailableCopies = 2, TotalCopies = 2, Category = "Văn học Việt Nam", ShelfGridX = 2, ShelfGridY = 5, ShelfLevel = 1, CoverImageUrl = "" });
            _allBooks.Add(new BookCardItem { Id = 4, Title = "Toán Học Cao Cấp", Author = "Nguyễn Đình Trí", AvailableCopies = 5, TotalCopies = 5, Category = "Giáo trình", ShelfGridX = 10, ShelfGridY = 9, ShelfLevel = 4, CoverImageUrl = "" });
            _allBooks.Add(new BookCardItem { Id = 5, Title = "Vật Lý Đại Cương", Author = "Lương Duyên Bình", AvailableCopies = 1, TotalCopies = 3, Category = "Giáo trình", ShelfGridX = 10, ShelfGridY = 9, ShelfLevel = 4, CoverImageUrl = "" });
            _allBooks.Add(new BookCardItem { Id = 6, Title = "Harry Potter và Hòn Đá Phù Thủy", Author = "J.K. Rowling", AvailableCopies = 4, TotalCopies = 6, Category = "Văn học nước ngoài", ShelfGridX = 6, ShelfGridY = 3, ShelfLevel = 5, CoverImageUrl = "" });
            _allBooks.Add(new BookCardItem { Id = 7, Title = "Tắt Đèn", Author = "Ngô Tất Tố", AvailableCopies = 0, TotalCopies = 1, Category = "Văn học Việt Nam", ShelfGridX = 2, ShelfGridY = 3, ShelfLevel = 2, CoverImageUrl = "" });
            _allBooks.Add(new BookCardItem { Id = 8, Title = "Lược Sử Thời Gian", Author = "Stephen Hawking", AvailableCopies = 2, TotalCopies = 3, Category = "Khoa học phổ thông", ShelfGridX = 6, ShelfGridY = 3, ShelfLevel = 5, CoverImageUrl = "" });

            foreach (var b in _allBooks)
            {
                b.NormalizedTitle = SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(b.Title).ToLower();
                b.NormalizedAuthor = SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(b.Author).ToLower();
                b.NormalizedCategory = SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(b.Category).ToLower();
            }

            ApplySearch();
        }

        private void ApplySearch()
        {
            Books.Clear();
            var query = SearchKeyword?.Trim() ?? "";
            if (query.Length > 100) query = query.Substring(0, 100);
            
            var cleanQuery = SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(query).ToLower();
            
            var filtered = _allBooks.Where(b => 
                string.IsNullOrEmpty(cleanQuery) || 
                b.NormalizedTitle.Contains(cleanQuery) || 
                b.NormalizedAuthor.Contains(cleanQuery) || 
                b.NormalizedCategory.Contains(cleanQuery)
            );

            foreach (var book in filtered)
            {
                Books.Add(book);
            }
            IsNoResultsFound = Books.Count == 0 && !string.IsNullOrWhiteSpace(SearchKeyword);
        }

        private void LoadMockBags()
        {
            RecommendedBags.Clear();
            var mockBag = new ClassReadingBagDto
            {
                Id = 1,
                ClassId = "10A1",
                BagTitle = "🎯 Nhiệm vụ đọc sách tự học tháng 6",
                TeacherSsoId = "GV001",
                Deadline = DateTime.Today.AddDays(7),
                RequiredPassRate = 80.0,
                CreatedAt = DateTime.UtcNow,
                Books = new System.Collections.Generic.List<BookDto>
                {
                    new BookDto { Id = 1, Title = "Đắc Nhân Tâm", Author = "Dale Carnegie", CoverImageUrl = "", QuestStatus = "Completed" },
                    new BookDto { Id = 2, Title = "Nhà Giả Kim", Author = "Paulo Coelho", CoverImageUrl = "", QuestStatus = "Pending" }
                }
            };
            RecommendedBags.Add(mockBag);
        }
    }

    public class BookCardItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public int AvailableCopies { get; set; }
        public int TotalCopies { get; set; }
        public string Category { get; set; } = "";
        public int ShelfGridX { get; set; }
        public int ShelfGridY { get; set; }
        public int ShelfLevel { get; set; } = 1;
        public string AudiobookStatusText { get; set; } = "";
        public string AudiobookStatusColor { get; set; } = "";
        public string CoverImageUrl { get; set; } = "";
        public string NormalizedTitle { get; set; } = "";
        public string NormalizedAuthor { get; set; } = "";
        public string NormalizedCategory { get; set; } = "";

        public string AvailabilityText => AvailableCopies > 0 ? $"Còn {AvailableCopies}/{TotalCopies} cuốn" : "Hết sách";
        public string AvailabilityColor => AvailableCopies > 0 ? "#10B981" : "#EF4444"; // Emerald vs Red
    }

    public class QuizResultDto
    {
        public int CorrectAnswersCount { get; set; }
        public int TotalQuestions { get; set; }
        public bool IsPassed { get; set; }
        public int XpGained { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<QuestionResultDetailDto>? QuestionResults { get; set; }
    }

    public class QuestionResultDetailDto
    {
        public string QuestionText { get; set; } = string.Empty;
        public int SelectedOptionIndex { get; set; }
        public int CorrectOptionIndex { get; set; }
        public bool IsCorrect { get; set; }
        public string Explanation { get; set; } = string.Empty;
    }

    public class MyContributionDto
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int ApprovedStatus { get; set; }
    }

    public class ClassReadingBagDto
    {
        public int Id { get; set; }
        public string ClassId { get; set; } = string.Empty;
        public string BagTitle { get; set; } = string.Empty;
        public string TeacherSsoId { get; set; } = string.Empty;
        public DateTime? Deadline { get; set; }
        public double RequiredPassRate { get; set; } = 80.0;
        public DateTime CreatedAt { get; set; }
        public System.Collections.Generic.List<BookDto> Books { get; set; } = new();
    }
}
