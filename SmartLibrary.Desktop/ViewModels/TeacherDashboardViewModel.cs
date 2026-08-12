using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;

namespace SmartLibrary.Desktop.ViewModels
{
    public class QuestTemplate
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public List<int> BookIds { get; set; } = new();
        public double RequiredPassRate { get; set; } = 80.0;
        public int DurationDays { get; set; } = 7;
    }

    public partial class TeacherDashboardViewModel : ObservableObject
    {
        [ObservableProperty] private string _className = "10A1";
        [ObservableProperty] private string _totalBorrowed = "125";
        [ObservableProperty] private string _notBorrowedCount = "5";
        [ObservableProperty] private string _topReader = "Nguyễn Văn An (15 cuốn)";
        [ObservableProperty] private string _alertMessage = "";
        [ObservableProperty] private bool _isBusy = false;
        [ObservableProperty] private bool _isChartEmpty = false;
        [ObservableProperty] private string _bookSearchKeyword = "";
        [ObservableProperty] private int _selectedTabIndex = 0;
        private readonly System.Collections.Generic.List<BookSelectableItem> _allAvailableBooks = new();

        [ObservableProperty] private string _studentSearchKeyword = "";
        private readonly System.Collections.Generic.List<ClassRecordDto> _allClassProgressRecords = new();

        partial void OnStudentSearchKeywordChanged(string value)
        {
            ApplyStudentProgressFilter();
        }

        private void ApplyStudentProgressFilter()
        {
            ClassProgressRecords.Clear();
            var query = StudentSearchKeyword?.Trim().ToLower() ?? "";
            var filtered = _allClassProgressRecords.Where(r =>
                string.IsNullOrEmpty(query) ||
                r.StudentName.ToLower().Contains(query) ||
                r.StudentSso.ToLower().Contains(query)
            );
            foreach (var r in filtered)
            {
                ClassProgressRecords.Add(r);
            }
        }

        partial void OnBookSearchKeywordChanged(string value)
        {
            ApplyBookFilter();
        }

        private void ApplyBookFilter()
        {
            AvailableBooks.Clear();
            var query = BookSearchKeyword?.Trim().ToLower() ?? "";
            var filtered = _allAvailableBooks.Where(b =>
                string.IsNullOrEmpty(query) ||
                b.Title.ToLower().Contains(query) ||
                b.Author.ToLower().Contains(query) ||
                b.BookId.ToString().Contains(query)
            );

            foreach (var item in filtered)
            {
                AvailableBooks.Add(item);
            }
        }

        [ObservableProperty] private string _newBagTitle = "";
        [ObservableProperty] private string _newBagBookIds = "";
        [ObservableProperty] private DateTime _newBagDeadline;
        [ObservableProperty] private double _newBagRequiredPassRate;

        [ObservableProperty] private QuestTemplate? _selectedQuestTemplate;
        public ObservableCollection<QuestTemplate> QuestTemplates { get; } = new();

        [ObservableProperty]
        private bool _showOnlyUnreadStudents = false;

        [ObservableProperty]
        private bool _hasUnreadStudents = false;

        partial void OnShowOnlyUnreadStudentsChanged(bool value)
        {
            _ = LoadDataAsync();
        }

        public ObservableCollection<StudentReadingStat> StudentStats { get; } = new();
        public ObservableCollection<StudentReadingStat> UnreadStudentStats { get; } = new();
        public ObservableCollection<string> AvailableClasses { get; } = new() { "10A1", "11B2", "10A2", "11A1", "12C1" };
        public ObservableCollection<BookSelectableItem> AvailableBooks { get; } = new();
        public ObservableCollection<ClassRecordDto> ClassProgressRecords { get; } = new();

        partial void OnClassNameChanged(string value)
        {
            _ = LoadDataAsync();
        }

        public ICommand ExportAlertListCommand { get; }
        public ICommand CopyAlertTextCommand { get; }
        public ICommand CopyIndividualAlertTextCommand { get; }
        public ICommand CreateClassReadingBagCommand { get; }

        private readonly ApiService _apiService;

        public TeacherDashboardViewModel(ApiService apiService)
        {
            _apiService = apiService;
            ExportAlertListCommand = new AsyncRelayCommand(ExportAlertListAsync);
            CopyAlertTextCommand = new RelayCommand(CopyAlertText);
            CopyIndividualAlertTextCommand = new RelayCommand<string>(CopyIndividualAlertText);
            CreateClassReadingBagCommand = new AsyncRelayCommand(CreateClassReadingBagAsync);

            // Tự động tải mặc định lớp chủ nhiệm của Giáo viên đăng nhập
            var ssoId = AuthService.CurrentUserSsoId ?? "GV001";
            AvailableClasses.Clear();
            
            if (ssoId == "GV001")
            {
                _className = "10A1";
                AvailableClasses.Add("10A1");
                AvailableClasses.Add("10A2");
            }
            else if (ssoId == "GV002")
            {
                _className = "11B2";
                AvailableClasses.Add("11B2");
                AvailableClasses.Add("11A1");
            }
            else if (ssoId == "GV003")
            {
                _className = "10A2";
                AvailableClasses.Add("10A2");
            }
            else if (ssoId == "GV004")
            {
                _className = "11A1";
                AvailableClasses.Add("11A1");
            }
            else if (ssoId == "GV005")
            {
                _className = "12C1";
                AvailableClasses.Add("12C1");
            }
            else
            {
                _className = "10A1";
                AvailableClasses.Add("10A1");
                AvailableClasses.Add("11B2");
                AvailableClasses.Add("10A2");
                AvailableClasses.Add("11A1");
                AvailableClasses.Add("12C1");
            }

            _newBagDeadline = DateTime.Today.AddDays(7);
            _newBagRequiredPassRate = 80.0;

            // Setup Quest Templates
            QuestTemplates.Add(new QuestTemplate 
            { 
                DisplayName = "📖 [Ngữ văn 10] Rèn luyện kỹ năng sống và Đắc Nhân Tâm", 
                Title = "Nhiệm vụ tự đọc hiểu Kỹ năng sống Học kỳ 2", 
                Subject = "Ngữ văn", 
                Grade = "Khối 10", 
                BookIds = new() { 1 },
                RequiredPassRate = 80.0,
                DurationDays = 7
            });
            QuestTemplates.Add(new QuestTemplate 
            { 
                DisplayName = "🔮 [Triết học 11] Tư duy cuộc sống và Nhà Giả Kim", 
                Title = "Nhiệm vụ tìm hiểu tư duy triết học qua tác phẩm Nhà Giả Kim", 
                Subject = "Ngữ văn/Triết học", 
                Grade = "Khối 11", 
                BookIds = new() { 2 },
                RequiredPassRate = 80.0,
                DurationDays = 10
            });
            QuestTemplates.Add(new QuestTemplate 
            { 
                DisplayName = "🌱 [Kỹ năng] Phát triển tâm hồn và nghị lực", 
                Title = "Nhiệm vụ đọc nuôi dưỡng tâm hồn thiếu niên", 
                Subject = "Giáo dục công dân", 
                Grade = "Khối 10", 
                BookIds = new() { 3 },
                RequiredPassRate = 70.0,
                DurationDays = 14
            });
            QuestTemplates.Add(new QuestTemplate 
            { 
                DisplayName = "📚 [Toàn diện] Giỏ sách tự học tổng hợp", 
                Title = "Tuần lễ đọc sách tích lũy tri thức trường QA SmartSchool", 
                Subject = "Tất cả", 
                Grade = "Mọi khối lớp", 
                BookIds = new() { 1, 2, 3 },
                RequiredPassRate = 80.0,
                DurationDays = 7
            });

            _ = LoadDataAsync();
            _ = LoadAvailableBooksAsync();
        }

        partial void OnSelectedQuestTemplateChanged(QuestTemplate? value)
        {
            if (value == null) return;
            NewBagTitle = value.Title;
            NewBagRequiredPassRate = value.RequiredPassRate;
            NewBagDeadline = DateTime.Today.AddDays(value.DurationDays);

            foreach (var book in AvailableBooks)
            {
                book.IsSelected = value.BookIds.Contains(book.BookId);
            }
        }

        public async Task LoadDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                try
                {
                    var stats = await _apiService.GetAsync<StudentReadingStatDto[]>($"/Reports/class-stats/{ClassName}");
                    StudentStats.Clear();
                    if (stats != null && stats.Length > 0)
                    {
                        var filteredStats = ShowOnlyUnreadStudents 
                            ? stats.Where(s => s.BooksRead == 0).ToArray() 
                            : stats;

                        if (filteredStats.Length > 0)
                        {
                            int maxRead = filteredStats.Max(s => s.BooksRead);
                            int total = 0;
                            foreach (var stat in filteredStats)
                            {
                                double height = maxRead > 0 ? ((double)stat.BooksRead / maxRead) * 220 : 0;
                                StudentStats.Add(new StudentReadingStat
                                {
                                    StudentName = stat.StudentName,
                                    BooksRead = stat.BooksRead,
                                    HeightPct = height
                                });
                                total += stat.BooksRead;
                            }
                        }

                        // Thống kê chung của cả lớp vẫn giữ nguyên để đảm bảo độ chính xác của các thẻ chỉ số
                        int grandTotal = stats.Sum(s => s.BooksRead);
                        TotalBorrowed = grandTotal.ToString();

                        var top = stats.OrderByDescending(s => s.BooksRead).FirstOrDefault();
                        TopReader = top != null ? $"{top.StudentName} ({top.BooksRead} cuốn)" : "Chưa có";
                    }
                    else
                    {
                        TotalBorrowed = "0";
                        TopReader = "Chưa có dữ liệu";
                    }
                }
                catch
                {
                    // Fallback load mock cũ nếu offline sập
                    var mockList = new System.Collections.Generic.List<StudentReadingStat>
                    {
                        new StudentReadingStat { StudentName = "Nguyễn Văn An", BooksRead = 15, HeightPct = 220 },
                        new StudentReadingStat { StudentName = "Trần Thị Bình", BooksRead = 12, HeightPct = 176 },
                        new StudentReadingStat { StudentName = "Lê Hoàng Cường", BooksRead = 8, HeightPct = 117 },
                        new StudentReadingStat { StudentName = "Phạm Minh Đức", BooksRead = 5, HeightPct = 73 },
                        new StudentReadingStat { StudentName = "Võ Thị Em", BooksRead = 0, HeightPct = 0 }
                    };

                    StudentStats.Clear();
                    var filteredMock = ShowOnlyUnreadStudents 
                        ? mockList.Where(s => s.BooksRead == 0) 
                        : mockList;

                    foreach (var item in filteredMock)
                    {
                        StudentStats.Add(item);
                    }
                    TotalBorrowed = "40";
                    TopReader = "Nguyễn Văn An (15 cuốn)";
                }

                IsChartEmpty = StudentStats.Count == 0;
                int notBorrowed = StudentStats.Count(s => s.BooksRead == 0);
                NotBorrowedCount = notBorrowed.ToString();
                AlertMessage = notBorrowed > 0
                    ? $"⚠️ {notBorrowed} học sinh chưa mượn sách nào trong tháng này!"
                    : "✅ Tất cả học sinh đều đã đọc sách!";

                UnreadStudentStats.Clear();
                foreach (var stat in StudentStats)
                {
                    if (stat.BooksRead == 0)
                    {
                        UnreadStudentStats.Add(stat);
                    }
                }
                HasUnreadStudents = UnreadStudentStats.Count > 0;

                await LoadClassProgressAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExportAlertListAsync()
        {
            var lazyStudents = StudentStats.Where(s => s.BooksRead == 0).ToList();
            if (!lazyStudents.Any())
            {
                System.Windows.MessageBox.Show("Tuyệt vời! Cả lớp đều đã đọc sách trong tháng này.", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "Danh_sach_nhac_nho_doc_sach.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    string[] headers = new[] { "Tên Học Sinh", "Số Cuốn Đã Mượn", "Trạng thái nhắc nhở" };
                    Func<StudentReadingStat, object[]> mapper = (item) => new object[] { item.StudentName, item.BooksRead, "Chưa mượn cuốn nào trong tháng" };
                    
                    await ExcelExportService.ExportToXlsxAsync(lazyStudents, headers, mapper, sfd.FileName, "Nhắc nhở đọc sách");
                    System.Windows.MessageBox.Show("Xuất danh sách Excel thành công!", "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Lỗi xuất Excel: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void CopyAlertText()
        {
            var lazyStudents = StudentStats.Where(s => s.BooksRead == 0).Select(s => s.StudentName).ToList();
            if (!lazyStudents.Any())
            {
                System.Windows.MessageBox.Show("Tuyệt vời! Cả lớp đều đã đọc sách trong tháng này.", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            string names = string.Join("\n", lazyStudents.Select(s => $"- {s}"));
            string message = $"[Nhắc nhở lớp {ClassName}]\nDanh sách học sinh chưa mượn sách thư viện trong tháng này:\n{names}\nCác em nhớ chủ động mượn sách để hoàn thành chỉ tiêu thi đua nhé!";
            System.Windows.Clipboard.SetText(message);
            System.Windows.MessageBox.Show("Đã sao chép văn bản nhắc nhở vào Clipboard thành công! Bạn có thể dán (Ctrl+V) ngay lên nhóm Zalo/Viber lớp.", "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void CopyIndividualAlertText(string? studentName)
        {
            if (string.IsNullOrEmpty(studentName)) return;
            string message = $"[QA SmartSchool] Kính gửi phụ huynh em {studentName}, tháng này con chưa mượn sách tại thư viện. Kính mong phụ huynh nhắc con mượn sách để hoàn thành thi đua của lớp {ClassName}. Trân trọng!";
            System.Windows.Clipboard.SetText(message);
            System.Windows.MessageBox.Show($"Đã sao chép tin nhắn nhắc nhở riêng cho phụ huynh em {studentName} vào Clipboard!", "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private async Task CreateClassReadingBagAsync()
        {
            if (string.IsNullOrWhiteSpace(NewBagTitle))
            {
                System.Windows.MessageBox.Show("Vui lòng nhập tiêu đề giỏ sách.", "Cảnh báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var idList = AvailableBooks.Where(b => b.IsSelected).Select(b => b.BookId).ToList();

            if (!idList.Any())
            {
                System.Windows.MessageBox.Show("Vui lòng tick chọn ít nhất 1 cuốn sách từ danh sách để tạo giỏ sách.", "Cảnh báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                var payload = new
                {
                    ClassId = ClassName,
                    BagTitle = NewBagTitle,
                    BookIds = idList,
                    TeacherSsoId = AuthService.CurrentUserSsoId ?? "GV001",
                    Deadline = NewBagDeadline,
                    RequiredPassRate = NewBagRequiredPassRate
                };

                var response = await _apiService.PostAsync<object, CreateBagResponseDto>("/ClassReadingBags", payload);
                
                string successMessage = $"Đã tạo giỏ sách chủ đề '{NewBagTitle}' thành công cho lớp {ClassName}!";
                
                if (response?.Warnings != null && response.Warnings.Any())
                {
                    string warningsText = string.Join("\n", response.Warnings);
                    successMessage += $"\n\nCảnh báo từ hệ thống:\n{warningsText}";
                }
                
                System.Windows.MessageBox.Show(successMessage, "Thành công", System.Windows.MessageBoxButton.OK, 
                    (response?.Warnings != null && response.Warnings.Any()) ? System.Windows.MessageBoxImage.Warning : System.Windows.MessageBoxImage.Information);

                NewBagTitle = "";
                NewBagDeadline = DateTime.Today.AddDays(7);
                NewBagRequiredPassRate = 80.0;
                // Bỏ chọn tất cả sách sau khi tạo thành công
                foreach (var book in AvailableBooks)
                {
                    book.IsSelected = false;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Không thể tạo giỏ sách chủ đề: {ex.Message}.", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task LoadAvailableBooksAsync()
        {
            try
            {
                var books = await _apiService.GetAsync<BookDto[]>("/Books");
                _allAvailableBooks.Clear();
                if (books != null)
                {
                    foreach (var book in books)
                    {
                        _allAvailableBooks.Add(new BookSelectableItem
                        {
                            BookId = book.Id,
                            Title = book.Title ?? string.Empty,
                            Author = book.Author ?? string.Empty,
                            IsSelected = false
                        });
                    }
                }
                ApplyBookFilter();
            }
            catch
            {
                // Fallback mock book selection if offline
                _allAvailableBooks.Clear();
                _allAvailableBooks.Add(new BookSelectableItem { BookId = 1, Title = "Đắc Nhân Tâm", Author = "Dale Carnegie", IsSelected = false });
                _allAvailableBooks.Add(new BookSelectableItem { BookId = 2, Title = "Nhà Giả Kim", Author = "Paulo Coelho", IsSelected = false });
                _allAvailableBooks.Add(new BookSelectableItem { BookId = 3, Title = "Hạt Giống Tâm Hồn", Author = "Nhiều tác giả", IsSelected = false });
                ApplyBookFilter();
            }
        }

        public async Task LoadClassProgressAsync()
        {
            try
            {
                var records = await _apiService.GetAsync<List<ClassRecordDto>>($"/StudyAnalytics/class-progress/{ClassName}");
                _allClassProgressRecords.Clear();
                if (records != null)
                {
                    foreach (var r in records)
                    {
                        _allClassProgressRecords.Add(r);
                    }
                }
                ApplyStudentProgressFilter();
            }
            catch (Exception)
            {
                // Fallback offline dữ liệu mô phỏng
                _allClassProgressRecords.Clear();
                _allClassProgressRecords.Add(new ClassRecordDto { StudentName = "Nguyễn Văn An", StudentSso = "HS001", TotalLoans = 6, AverageQuizScore = 85.0, QuizzesPassed = 4, AudiobooksListened = 2, XpPoints = 320, ReadingLevel = "Cấp 3: Vàng" });
                _allClassProgressRecords.Add(new ClassRecordDto { StudentName = "Trần Thị Bình", StudentSso = "HS002", TotalLoans = 3, AverageQuizScore = 72.5, QuizzesPassed = 2, AudiobooksListened = 0, XpPoints = 180, ReadingLevel = "Cấp 2: Bạc" });
                ApplyStudentProgressFilter();
            }
        }
    }

    public class StudentReadingStat
    {
        public string StudentName { get; set; } = "";
        public int BooksRead { get; set; }
        public double HeightPct { get; set; }
    }

    public class StudentReadingStatDto
    {
        public string StudentName { get; set; } = "";
        public int BooksRead { get; set; }
    }

    public partial class BookSelectableItem : ObservableObject
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;

        [ObservableProperty]
        private bool _isSelected;
    }

    public class CreateBagResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public int BagId { get; set; }
        public string ClassId { get; set; } = string.Empty;
        public System.Collections.Generic.List<int> BookIds { get; set; } = new();
        public System.Collections.Generic.List<string> Warnings { get; set; } = new();
    }

    public class ClassRecordDto
    {
        public string StudentName { get; set; } = string.Empty;
        public string StudentSso { get; set; } = string.Empty;
        public int TotalLoans { get; set; }
        public double AverageQuizScore { get; set; }
        public int QuizzesPassed { get; set; }
        public int AudiobooksListened { get; set; }
        public int XpPoints { get; set; }
        public string ReadingLevel { get; set; } = string.Empty;
    }
}
