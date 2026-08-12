using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;
using SmartLibrary.Desktop.Helpers;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class BookManagementViewModel : ObservableObject, IActiveAwareViewModel
    {
        private readonly ApiService _apiService;
        private readonly RfidReaderService _rfidService;
        private readonly System.Windows.Threading.DispatcherTimer _searchDebounceTimer;

        public ObservableCollection<BookDto> Books { get; } = new();

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _statusMessage = "";

        [ObservableProperty]
        private string _searchKeyword = "";

        [ObservableProperty]
        private string _isbnInput = "";

        [ObservableProperty]
        private string _selectedAgeGroup = "Tất cả";

        [ObservableProperty]
        private string _selectedCategory = "Tất cả";

        public ObservableCollection<string> AgeGroups { get; } = new() { "Tất cả", "Tiểu học", "Trung học" };
        public ObservableCollection<string> Categories { get; } = new() { "Tất cả" };

        partial void OnSelectedAgeGroupChanged(string value)
        {
            System.Windows.Data.CollectionViewSource.GetDefaultView(Books)?.Refresh();
        }

        partial void OnSelectedCategoryChanged(string value)
        {
            System.Windows.Data.CollectionViewSource.GetDefaultView(Books)?.Refresh();
        }

        partial void OnIsbnInputChanged(string value)
        {
            (LookupIsbnCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        }

        partial void OnSearchKeywordChanged(string value)
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        public ICommand LoadBooksCommand { get; }
        public ICommand SimulateRfidScanCommand { get; }
        public ICommand ImportCsvCommand { get; }
        public ICommand PrintBarcodeCommand { get; }
        public ICommand AddBookCommand { get; }
        public ICommand PlayTeaserCommand { get; }
        public ICommand LookupIsbnCommand { get; }

        public BookManagementViewModel(ApiService apiService, RfidReaderService rfidService)
        {
            _apiService = apiService;
            _rfidService = rfidService;
            
            LoadBooksCommand = new AsyncRelayCommand(LoadBooksAsync);
            SimulateRfidScanCommand = new RelayCommand(SimulateRfidScan);
            ImportCsvCommand = new RelayCommand(ImportCsv);
            PrintBarcodeCommand = new RelayCommand(PrintBarcode, CanPrintBarcode);
            AddBookCommand = new AsyncRelayCommand(AddBookAsync);
            PlayTeaserCommand = new RelayCommand<BookDto>(PlayTeaser);
            LookupIsbnCommand = new AsyncRelayCommand(LookupIsbnAsync, CanLookupIsbn);

            _searchDebounceTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(350)
            };
            _searchDebounceTimer.Tick += (s, e) =>
            {
                _searchDebounceTimer.Stop();
                System.Windows.Data.CollectionViewSource.GetDefaultView(Books)?.Refresh();
            };

            // Đăng ký nhận sự kiện RFID
            _rfidService.TagsDetected += RfidService_TagsDetected;
            
            // Lắng nghe sự thay đổi của danh sách sách
            Books.CollectionChanged += (s, e) => 
            {
                if (e.NewItems != null)
                {
                    foreach (BookDto item in e.NewItems)
                    {
                        item.PropertyChanged += (sender, args) => 
                        {
                            if (args.PropertyName == nameof(BookDto.IsSelected))
                            {
                                (PrintBarcodeCommand as RelayCommand)?.NotifyCanExecuteChanged();
                            }
                        };
                    }
                }
                (PrintBarcodeCommand as RelayCommand)?.NotifyCanExecuteChanged();
            };

            // Thiết lập bộ lọc tìm kiếm
            var collectionView = System.Windows.Data.CollectionViewSource.GetDefaultView(Books);
            if (collectionView != null)
            {
                collectionView.Filter = FilterBooks;
            }
        }

        private bool FilterBooks(object obj)
        {
            if (obj is not BookDto book) return false;
            
            var keyword = SearchKeyword;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var cleanKeyword = SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(keyword.Trim()).ToLower();
                var titleClean = book.Title != null ? SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(book.Title).ToLower() : "";
                var authorClean = book.Author != null ? SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(book.Author).ToLower() : "";
                var isbnClean = book.Isbn != null ? SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(book.Isbn).ToLower() : "";
                
                bool matchesKeyword = titleClean.Contains(cleanKeyword) ||
                                      authorClean.Contains(cleanKeyword) ||
                                      isbnClean.Contains(cleanKeyword);
                if (!matchesKeyword) return false;
            }

            // Lọc theo Lứa tuổi (AgeGroup)
            if (SelectedAgeGroup != "Tất cả")
            {
                if (book.AgeGroup != null && book.AgeGroup != SelectedAgeGroup)
                {
                    return false;
                }
            }

            // Lọc theo Thể loại (Category)
            if (SelectedCategory != "Tất cả")
            {
                if (book.Category == null || book.Category.Name != SelectedCategory)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanPrintBarcode()
        {
            return Books.Any(b => b.IsSelected);
        }

        private void ImportCsv()
        {
            if (IsBusy) return;

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Chọn danh sách sách (CSV)"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                IsBusy = true;
                StatusMessage = "Đang đọc file CSV: " + System.IO.Path.GetFileName(openFileDialog.FileName);
                System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => 
                {
                    string fileName = System.IO.Path.GetFileName(openFileDialog.FileName);
                    try
                    {
                        await Task.Delay(1500); // Giả lập I/O đọc chunk
                        int validCount = 0;
                        int invalidCount = 0;
                        var random = new Random();
                        for (int i = 1; i <= 50; i++)
                        {
                            // Giả lập thỉnh thoảng có dòng sai định dạng
                            string testIsbn = i == 5 ? "123-abc-xyz" : $"978-604-0-{random.Next(100000, 999999)}";
                            
                            if (!SmartLibrary.Desktop.Helpers.InputHelper.ValidateIsbn(testIsbn))
                            {
                                invalidCount++;
                                continue;
                            }
                            
                            Books.Insert(0, new BookDto 
                            {
                                Id = 1000 + i,
                                Title = $"Sách nhập từ Excel {i}",
                                Author = "Nhiều tác giả",
                                Isbn = testIsbn,
                                TotalCopies = 5,
                                AvailableCopies = 5
                            });
                            validCount++;
                        }
                        
                        await AuditLogService.WriteLogAsync("Nhập kho sách", $"Nhập thành công {validCount} sách từ file CSV: '{fileName}' ({invalidCount} dòng lỗi bị loại bỏ)", true);
                        if (invalidCount > 0)
                        {
                            StatusMessage = $"Đã nhập {validCount}/50 sách. {invalidCount} dòng lỗi bị loại bỏ.";
                            System.Media.SystemSounds.Hand.Play();
                        }
                        else
                        {
                            StatusMessage = $"✅ Đã import thành công {validCount} cuốn sách từ file CSV!";
                            System.Media.SystemSounds.Exclamation.Play();
                        }
                    }
                    catch (Exception ex)
                    {
                        await AuditLogService.WriteLogAsync("Nhập kho sách", $"Lỗi khi nhập file CSV '{fileName}': {ex.Message}", false);
                        StatusMessage = "❌ Có lỗi xảy ra khi nhập file CSV.";
                        System.Media.SystemSounds.Hand.Play();
                    }
                    finally
                    {
                        IsBusy = false;
                    }
                });
            }
        }

        private async Task AddBookAsync()
        {
            StatusMessage = "Đang thêm sách mới...";
            await Task.Delay(300); // Tạo hiệu ứng mượt mà
 
            try
            {
                var random = new Random();
                int newId = Books.Count > 0 ? Books.Max(b => b.Id) + 1 : 1001;
                var newBook = new BookDto
                {
                    Id = newId,
                    Title = $"Sách tham khảo mới {newId}",
                    Author = "Tác giả Giáo dục",
                    Isbn = $"ISBN-978604{random.Next(100000, 999999)}",
                    TotalCopies = 3,
                    AvailableCopies = 3
                };
                Books.Insert(0, newBook);
                await AuditLogService.WriteLogAsync("Biến động kho sách", $"Thêm mới sách thành công: '{newBook.Title}' (ISBN: {newBook.Isbn})", true);
                StatusMessage = $"✅ Đã thêm mới sách '{newBook.Title}' thành công!";
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch (Exception ex)
            {
                await AuditLogService.WriteLogAsync("Biến động kho sách", $"Lỗi khi thêm sách mới: {ex.Message}", false);
                StatusMessage = "❌ Không thể thêm sách mới.";
            }
        }

        private async Task LookupIsbnAsync()
        {
            if (string.IsNullOrWhiteSpace(IsbnInput))
            {
                System.Windows.MessageBox.Show("Vui lòng nhập hoặc quét mã ISBN.", "Cảnh báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (!SmartLibrary.Desktop.Helpers.InputHelper.ValidateIsbn(IsbnInput))
            {
                System.Windows.MessageBox.Show("Mã ISBN không đúng cấu trúc hợp lệ (Phải dài 10 hoặc 13 chữ số)!", "Lỗi định dạng", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                System.Media.SystemSounds.Hand.Play();
                StatusMessage = "❌ Mã ISBN nhập vào không đúng định dạng.";
                return;
            }

            IsBusy = true;
            StatusMessage = $"Đang tra cứu ISBN '{IsbnInput}'...";
            try
            {
                var cleanIsbn = Uri.EscapeDataString(IsbnInput.Trim().Replace("-", ""));
                var bookDto = await _apiService.GetAsync<BookDto>($"{ApiEndpoints.BookLookup}{cleanIsbn}");
                if (bookDto != null)
                {
                    int newId = Books.Count > 0 ? Books.Max(b => b.Id) + 1 : 1001;
                    bookDto.Id = newId;
                    bookDto.TotalCopies = 3;
                    bookDto.AvailableCopies = 3;
                    bookDto.Publisher = SmartLibrary.Desktop.Helpers.InputHelper.NormalizePublisherName(bookDto.Publisher);

                    Books.Insert(0, bookDto);
                    await AuditLogService.WriteLogAsync("Biến động kho sách", $"Tự động điền sách thành công qua ISBN {IsbnInput}: '{bookDto.Title}' (Tác giả: {bookDto.Author})", true);
                    StatusMessage = $"✅ Tự động điền thành công: '{bookDto.Title}'!";
                    IsbnInput = "";
                    System.Media.SystemSounds.Asterisk.Play();
                }
                else
                {
                    StatusMessage = "❌ Không tìm thấy thông tin sách.";
                    System.Media.SystemSounds.Hand.Play();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi tra cứu ISBN: " + ex.Message;
                System.Media.SystemSounds.Hand.Play();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanLookupIsbn()
        {
            return !string.IsNullOrWhiteSpace(IsbnInput);
        }

        private void PrintBarcode()
        {
            var selectedBooks = Books.Where(b => b.IsSelected).ToList();
            if (selectedBooks.Count == 0)
            {
                System.Windows.MessageBox.Show("Vui lòng tick chọn ít nhất 1 cuốn sách để in tem!", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Gửi lệnh in (Giả lập)
            System.Windows.MessageBox.Show(
                $"🖨️ Đã gửi lệnh in {selectedBooks.Count} tem mã vạch tới máy in Zebra/Xprinter!\n\n(Các sách này sẽ tự động bỏ tick)", 
                "In Tem Nhãn", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Information);

            // Bỏ tick
            foreach (var book in selectedBooks)
            {
                book.IsSelected = false;
            }
            StatusMessage = $"Đã in {selectedBooks.Count} tem mã vạch.";
        }

        public async Task LoadBooksAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Đang tải dữ liệu sách...";

            try
            {
                var books = await _apiService.GetAsync<BookDto[]>(ApiEndpoints.Books);
                Books.Clear();
                if (books != null)
                {
                    foreach (var book in books)
                    {
                        Books.Add(book);
                    }

                    // Cập nhật danh sách thể loại động
                    Categories.Clear();
                    Categories.Add("Tất cả");
                    var uniqueCats = books
                        .Where(b => b.Category != null && !string.IsNullOrEmpty(b.Category.Name))
                        .Select(b => b.Category.Name)
                        .Distinct()
                        .OrderBy(n => n);
                    foreach (var cat in uniqueCats)
                    {
                        Categories.Add(cat);
                    }
                }
                SelectedAgeGroup = "Tất cả";
                SelectedCategory = "Tất cả";
                StatusMessage = $"Đã tải {Books.Count} đầu sách.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi tải dữ liệu: " + ex.Message;
                await AuditLogService.WriteLogAsync("Tải danh mục sách", $"Lỗi tải dữ liệu sách từ API: {ex.Message}", false);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void SimulateRfidScan()
        {
            StatusMessage = "Đang mô phỏng quét xấp 5 cuốn sách bằng RFID...";
            _rfidService.SimulateBatchRead(5);
        }

        private void RfidService_TagsDetected(object? sender, List<string> tags)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = $"Phát hiện {tags.Count} thẻ mới. Đang map vào Database...";

                if (Books.Count > 0)
                {
                    Random rng = new Random();
                    foreach(var tag in tags)
                    {
                        // Giả lập: Map mỗi thẻ RFID vào 1 sách có sẵn trong danh sách
                        int index = rng.Next(Books.Count);
                        var book = Books[index];
                        book.AvailableCopies += 1;
                        book.TotalCopies += 1;

                        // Tự động đẩy sách vừa được quét lên đầu Grid (Vá lỗi Mù mờ UX)
                        Books.RemoveAt(index);
                        Books.Insert(0, book);
                    }
                }
                
                SmartLibrary.Desktop.Views.Shared.TopBarControl.TriggerRfidFlash();
                System.Media.SystemSounds.Beep.Play();
            });

            _ = AuditLogService.WriteLogAsync("Nhận diện RFID hàng loạt", $"Đã map và cập nhật số lượng cho {tags.Count} cuốn sách quét qua RFID", true);
        }

        private void PlayTeaser(BookDto? book)
        {
            if (book == null) return;
            string audioUrl = $"https://www.soundhelix.com/examples/mp3/SoundHelix-Song-{(book.Id % 8) + 1}.mp3";
            MainWindow.PlayAudio(audioUrl, $"Giới thiệu sách: {book.Title}");
        }

        public void Cleanup()
        {
            _rfidService.TagsDetected -= RfidService_TagsDetected;
        }

        public void Activate()
        {
            _rfidService.TagsDetected -= RfidService_TagsDetected;
            _rfidService.TagsDetected += RfidService_TagsDetected;
            _ = LoadBooksAsync();
        }
    }

    public class BookDto : ObservableObject
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Isbn { get; set; }
        public string Publisher { get; set; }
        public int PublishYear { get; set; }
        public int ShelfGridX { get; set; }
        public int ShelfGridY { get; set; }
        public int ShelfLevel { get; set; } = 1;

        private int _totalCopies;
        public int TotalCopies
        {
            get => Copies != null && Copies.Count > 0 ? Copies.Count : _totalCopies;
            set => SetProperty(ref _totalCopies, value);
        }

        private int _availableCopies;
        public int AvailableCopies
        {
            get => Copies != null && Copies.Count > 0 ? Copies.Count(c => c.Status == 0) : _availableCopies;
            set => SetProperty(ref _availableCopies, value);
        }

        public string CoverImageUrl { get; set; }
        public string QuestStatus { get; set; } = "Pending";
        public decimal Price { get; set; }
        public CategoryDto Category { get; set; }
        public System.Collections.Generic.List<BookCopyDto> Copies { get; set; }
        public string? AgeGroup { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class BookCopyDto
    {
        public int Id { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public int Status { get; set; }
    }
}
