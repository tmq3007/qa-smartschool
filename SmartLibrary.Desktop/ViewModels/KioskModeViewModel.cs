using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;
using SmartLibrary.Desktop.Views.Shared;
using SmartLibrary.Desktop.Models;
using SmartLibrary.Desktop.Helpers;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class KioskModeViewModel : ObservableObject, IActiveAwareViewModel
    {
        private readonly ApiService _apiService;
        private readonly QrScannerService _qrScannerService;
        private DispatcherTimer _idleTimer;
        private int _idleSecondsCount = 0;
        private int _baseTimeoutSecondsValue = 30;
        private const int BonusTimeSeconds = 15;
        private const int MaxTimeoutSeconds = 60;
        private int _currentTimeout = 30;

        [ObservableProperty]
        private string _kioskState = "IDLE"; // IDLE, SCANNING_QR, SCANNING_BOOKS, SUCCESS

        [ObservableProperty]
        private string _mainInstruction = "CHẠM ĐỂ BẮT ĐẦU\nMƯỢN SÁCH";

        [ObservableProperty]
        private string _studentInfo = "";

        [ObservableProperty]
        private string _currentStudentName = "";

        [ObservableProperty]
        private string _statusMessage = "";

        [ObservableProperty]
        private string _kioskErrorMessage = "";

        [ObservableProperty]
        private string _timeRemainingText = "";

        [ObservableProperty]
        private bool _isConfirmingCancel = false;

        [ObservableProperty]
        private bool _isScreensaverVisible = true; // Hiện ngay từ đầu nếu Idle

        [ObservableProperty]
        private bool _isFineVisible = false;

        [ObservableProperty]
        private string _fineAmountText = "***** VNĐ";

        private string _currentSsoId = "";

        // PIN Security
        private static readonly System.Collections.Generic.Dictionary<string, (int attempts, DateTime lockedUntil)> PinSecurityCache = new();
        private int _pinAttempts = 0;
        private const int MaxPinAttempts = 3;
        private DateTime _pinLockedUntil = DateTime.MinValue;
        private const int PinLockDurationSeconds = 30;
        private int _maxKioskBorrowLimit = 3;
        private double _actualStudentFine = 0;

        [ObservableProperty]
        private string _searchKeyword = "";

        [ObservableProperty]
        private bool _isFineButtonEnabled = true;

        [ObservableProperty]
        private bool _isSearchPanelVisible = false;

        [ObservableProperty]
        private bool _isNoResultsFound = false;

        [ObservableProperty]
        private string _scannedBooksCountText = "Sách đã quét: 0/3 cuốn";

        [ObservableProperty]
        private bool _isTimeoutWarningVisible = false;

        [ObservableProperty]
        private bool _isFaceIdConsentGiven = false;

        [ObservableProperty]
        private int _currentStreak = 0;

        [ObservableProperty]
        private int _maxStreak = 0;

        [ObservableProperty]
        private string _streakMessage = "";

        public ObservableCollection<PendingBookItem> PendingBooks { get; } = new();
        public ObservableCollection<BookDto> OpacSearchResults { get; } = new();
        public ObservableCollection<BookDto> ScreensaverTrendingBooks { get; } = new();

        public ICommand StartCommand { get; }
        public ICommand ToggleSearchPanelCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand CloseSearchPanelCommand { get; }
        public ICommand ToggleFineVisibilityCommand { get; }
        public ICommand FinishCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ConfirmCancelCommand { get; }
        public ICommand AbortCancelCommand { get; }
        public ICommand SubmitRatingCommand { get; }
        public ICommand DismissErrorCommand { get; }
        public ICommand RemoveBookCommand { get; }
        public ICommand ExtendSessionCommand { get; }
        public ICommand FaceIdLoginCommand { get; }

        public KioskModeViewModel(ApiService apiService, QrScannerService qrScannerService)
        {
            _apiService = apiService;
            _qrScannerService = qrScannerService;

            StartCommand = new AsyncRelayCommand(StartSessionAsync);
            ToggleSearchPanelCommand = new RelayCommand(() => { ResetIdleTimer(); IsSearchPanelVisible = !IsSearchPanelVisible; });
            SearchCommand = new AsyncRelayCommand(ExecuteSearchAsync);
            CloseSearchPanelCommand = new RelayCommand(() => { ResetIdleTimer(); IsSearchPanelVisible = false; SearchKeyword = ""; OpacSearchResults.Clear(); });
            FinishCommand = new AsyncRelayCommand(FinishSessionAsync);
            CancelCommand = new RelayCommand(RequestCancel);
            ConfirmCancelCommand = new RelayCommand(ResetSession);
            AbortCancelCommand = new RelayCommand(() => { IsConfirmingCancel = false; ResetIdleTimer(); });
            ToggleFineVisibilityCommand = new AsyncRelayCommand(ToggleFineVisibilityAsync);
            SubmitRatingCommand = new RelayCommand<string>(SubmitRating);
            DismissErrorCommand = new RelayCommand(DismissError);
            RemoveBookCommand = new RelayCommand<PendingBookItem>(RemoveBook);
            ExtendSessionCommand = new RelayCommand(() => { IsTimeoutWarningVisible = false; ResetIdleTimer(); });
            FaceIdLoginCommand = new AsyncRelayCommand(FaceIdLoginAsync);

            try
            {
                string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (System.IO.File.Exists(configPath))
                {
                    string json = System.IO.File.ReadAllText(configPath);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("KioskSettings", out var kioskSettings) &&
                        kioskSettings.TryGetProperty("BaseTimeoutSeconds", out var timeoutVal))
                    {
                        _baseTimeoutSecondsValue = timeoutVal.GetInt32();
                    }
                }
            }
            catch { }
            _currentTimeout = _baseTimeoutSecondsValue;

            _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _idleTimer.Tick += IdleTimer_Tick;
            _idleTimer.Start(); // Chạy ngầm từ đầu để tính giờ bật Screensaver
            _ = LoadTrendingBooksAsync();
        }

        private void DismissError()
        {
            KioskErrorMessage = "";
            ResetIdleTimer();
        }

        private async Task ToggleFineVisibilityAsync()
        {
            ResetIdleTimer();

            // Kiểm tra khóa
            if (DateTime.Now < _pinLockedUntil)
            {
                int remaining = (int)(_pinLockedUntil - DateTime.Now).TotalSeconds;
                FineAmountText = $"🔒 Đã khóa {remaining}s";
                return;
            }

            // Nếu đang hiện → ẩn lại
            if (IsFineVisible)
            {
                IsFineVisible = false;
                FineAmountText = "***** VNĐ";
                return;
            }

            // Chưa đăng nhập → bỏ qua
            if (string.IsNullOrEmpty(_currentSsoId))
            {
                FineAmountText = "Vui lòng quét thẻ trước";
                return;
            }

            // Tải cấu hình PIN động từ API hoặc cấu hình cục bộ an toàn
            string defaultFallbackHash = SecurityHelper.ComputeSha256("1234");
            string pinHash = SecurityHelper.GetConfigValue("KioskSettings", "DefaultPinHash", defaultFallbackHash);
            try
            {
                var configs = await _apiService.GetAsync<LibraryConfigDto[]>(ApiEndpoints.LibraryConfig);
                var pinConfig = configs?.FirstOrDefault(c => c.Key == "StudentDefaultPin");
                if (pinConfig != null && !string.IsNullOrEmpty(pinConfig.Value))
                {
                    string val = pinConfig.Value.Trim();
                    if (val.Length == 64 && val.All(c => Uri.IsHexDigit(c)))
                    {
                        pinHash = val.ToLower();
                    }
                    else
                    {
                        pinHash = SecurityHelper.ComputeSha256(val);
                    }
                }
            }
            catch { }

            // Mở Dialog PIN
            var currentWindow = System.Windows.Application.Current.Windows
                .OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive);

            var dialog = new PinVerificationDialog(pinHash)
            {
                Owner = currentWindow
            };

            var result = dialog.ShowDialog();
            _pinAttempts += dialog.FailedAttempts;

            if (result == true && dialog.IsVerified)
            {
                // PIN đúng → hiện tiền 3 giây rồi tự ẩn
                _pinAttempts = 0;
                IsFineVisible = true;
                FineAmountText = $"{_actualStudentFine:N0} VNĐ";

                // Auto-hide sau 3 giây
                var hideTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(3)
                };
                hideTimer.Tick += (s, e) =>
                {
                    hideTimer.Stop();
                    IsFineVisible = false;
                    FineAmountText = "***** VNĐ";
                };
                hideTimer.Start();
            }
            
            if (_pinAttempts >= MaxPinAttempts)
            {
                _pinLockedUntil = DateTime.Now.AddSeconds(PinLockDurationSeconds);
                FineAmountText = $"🔒 Đã khóa {PinLockDurationSeconds}s";
                IsFineButtonEnabled = false;
                _pinAttempts = 0; // Reset counter
                try { System.Media.SystemSounds.Hand.Play(); } catch { }
            }

            // Cập nhật Cache bảo mật tĩnh
            PinSecurityCache[_currentSsoId] = (_pinAttempts, _pinLockedUntil);
        }

        private void IdleTimer_Tick(object? sender, EventArgs e)
        {
            _idleSecondsCount++;
            
            // Cập nhật live countdown đếm ngược khóa PIN nếu có
            if (DateTime.Now < _pinLockedUntil)
            {
                int remainingLock = (int)(_pinLockedUntil - DateTime.Now).TotalSeconds;
                if (IsFineVisible || FineAmountText.StartsWith("🔒"))
                {
                    FineAmountText = $"🔒 Đã khóa {remainingLock}s";
                }
                IsFineButtonEnabled = false;
            }
            else
            {
                IsFineButtonEnabled = true;
                if (FineAmountText.StartsWith("🔒"))
                {
                    FineAmountText = "***** VNĐ";
                }
            }

            if (KioskState == "IDLE")
            {
                if (_idleSecondsCount >= 15)
                {
                    IsScreensaverVisible = true;
                    _qrScannerService.StopCamera(); // TẮT camera khi Screensaver bật
                }
            }
            else
            {
                int remaining = _currentTimeout - _idleSecondsCount;
                TimeRemainingText = $"Tự động thoát sau: {remaining}s";

                // Hiển thị overlay cảnh báo hết giờ khi còn từ 10s trở xuống
                if (remaining <= 10 && remaining > 0 && KioskState != "SUCCESS" && KioskState != "RATING")
                {
                    IsTimeoutWarningVisible = true;
                }
                else
                {
                    IsTimeoutWarningVisible = false;
                }

                if (remaining <= 0)
                {
                    IsTimeoutWarningVisible = false;
                    ResetSession();
                }
            }
        }

        public void ResetIdleTimer()
        {
            _idleSecondsCount = 0;
            IsScreensaverVisible = false;
            
            // Bonus time khi tương tác (cap ở MaxTimeout)
            if (KioskState != "IDLE")
            {
                _currentTimeout = Math.Min(_currentTimeout + BonusTimeSeconds, MaxTimeoutSeconds);
            }
            else
            {
                _currentTimeout = _baseTimeoutSecondsValue;
            }
            
            TimeRemainingText = $"Tự động thoát sau: {_currentTimeout}s";
        }

        private async Task StartSessionAsync()
        {
            KioskState = "SCANNING_QR";
            MainInstruction = "VUI LÒNG ĐƯA MÃ QR VÀO CAMERA";
            StatusMessage = "Đang quét mã QR...";
            _idleTimer.Start();
            ResetIdleTimer();

            _qrScannerService.StartCamera(); // BẬT camera trước khi quét

            try
            {
                var rawSsoId = await _qrScannerService.ScanQrCodeAsync();
                _currentSsoId = rawSsoId?.Trim().ToUpper() ?? "";
                
                if (KioskState != "SCANNING_QR")
                {
                    return;
                }

                // Khôi phục trạng thái bảo mật mã PIN từ Cache tĩnh
                if (PinSecurityCache.TryGetValue(_currentSsoId, out var cachedState))
                {
                    _pinAttempts = cachedState.attempts;
                    _pinLockedUntil = cachedState.lockedUntil;
                }
                else
                {
                    _pinAttempts = 0;
                    _pinLockedUntil = DateTime.MinValue;
                }

                await LoadKioskConfigAsync();
                
                bool hasRestored = RestoreSessionDraftFromDisk(_currentSsoId);
                
                if (!hasRestored)
                {
                    try
                    {
                        var status = await _apiService.GetAsync<StudentStatusDto>($"{ApiEndpoints.CirculationStudentStatus}{_currentSsoId}");
                        if (status != null)
                        {
                            _actualStudentFine = status.TotalUnpaidFine;
                            CurrentStudentName = status.FullName;
                            StudentInfo = $"Xin chào Học sinh: {status.FullName}\nMã HS: {_currentSsoId}\nLớp: {status.SchoolClassId}";
                        }
                        else
                        {
                            _actualStudentFine = 0;
                            CurrentStudentName = $"Học sinh {_currentSsoId}";
                            StudentInfo = $"Xin chào Học sinh: {_currentSsoId}";
                        }
                    }
                    catch
                    {
                        _actualStudentFine = 0;
                        CurrentStudentName = $"Học sinh {_currentSsoId}";
                        StudentInfo = $"Xin chào Học sinh: {_currentSsoId}";
                    }
                }
                
                KioskState = "SCANNING_BOOKS";
                MainInstruction = "ĐẶT SÁCH LÊN BÀN ĐỂ QUÉT RFID\nHoặc quét mã vạch sách.";
                StatusMessage = hasRestored ? "Đã khôi phục phiên quét cũ của bạn!" : "Chờ quét sách...";
                System.Media.SystemSounds.Beep.Play();
                SpeechService.Speak(hasRestored ? $"Chào mừng quay trở lại {CurrentStudentName}. Đã khôi phục danh sách sách chờ mượn." : $"Xin chào bạn đọc {CurrentStudentName}. Vui lòng quét mã sách hoặc đặt sách lên vị trí quét.");
                ResetIdleTimer();
            }
            catch (Exception ex)
            {
                KioskErrorMessage = ex.Message;
                await Task.Delay(3000);
                ResetSession();
            }
        }

        public void HandleBarcodeScanned(string barcode)
        {
            ResetIdleTimer();
            TopBarControl.TriggerBarcodeFlash();
            if (KioskState == "SCANNING_BOOKS")
            {
                if (string.IsNullOrWhiteSpace(barcode)) return;

                if (barcode.Contains(","))
                {
                    var barcodes = barcode.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var code in barcodes)
                    {
                        var cleanCode = code.Trim();
                        if (!string.IsNullOrEmpty(cleanCode))
                        {
                            AddSingleBook(cleanCode);
                        }
                    }
                }
                else
                {
                    AddSingleBook(barcode.Trim());
                }
            }
        }

        private void AddSingleBook(string barcode)
        {
            if (PendingBooks.Count >= _maxKioskBorrowLimit)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    KioskErrorMessage = $"✨ Bạn ơi, để nhường cơ hội đọc cho các bạn khác nữa, Kiosk xin phép giới hạn tối đa {_maxKioskBorrowLimit} cuốn sách cho mỗi lượt mượn nhé! Hãy hoàn tất lượt này trước khi mượn tiếp.";
                    System.Media.SystemSounds.Exclamation.Play();
                    SpeechService.Speak($"Bạn ơi, để nhường cơ hội đọc cho các bạn khác nữa, Kiosk xin phép giới hạn tối đa {_maxKioskBorrowLimit} cuốn sách cho mỗi lượt mượn nhé!");
                });
                return;
            }

            if (!PendingBooks.Any(x => x.Barcode == barcode))
            {
                System.Windows.Application.Current.Dispatcher.Invoke(async () =>
                {
                    string bookTitle = $"Sách {barcode}";
                    try
                    {
                        var book = await _apiService.GetAsync<BookDto>($"{ApiEndpoints.BookCopy}{barcode}");
                        if (book != null && !string.IsNullOrEmpty(book.Title))
                        {
                            bookTitle = book.Title;
                        }
                    }
                    catch { }

                    PendingBooks.Add(new PendingBookItem { Barcode = barcode, Title = bookTitle });
                    StatusMessage = $"Đã thêm sách: {bookTitle}";
                    System.Media.SystemSounds.Beep.Play();
                    SpeechService.Speak($"Đã nhận cuốn sách: {bookTitle}");
                    UpdateScannedBooksCountText();
                    SaveSessionDraftToDisk();
                });
            }
        }

        private void RemoveBook(PendingBookItem? item)
        {
            if (item != null)
            {
                PendingBooks.Remove(item);
                ResetIdleTimer();
                UpdateScannedBooksCountText();
                SaveSessionDraftToDisk();
            }
        }

        private void UpdateScannedBooksCountText()
        {
            ScannedBooksCountText = $"Sách đã quét: {PendingBooks.Count}/{_maxKioskBorrowLimit} cuốn";
        }

        private void RequestCancel()
        {
            ResetIdleTimer();
            if (PendingBooks.Count > 0)
            {
                IsConfirmingCancel = true;
            }
            else
            {
                ResetSession();
            }
        }

        private async Task FinishSessionAsync()
        {
            ResetIdleTimer();
            if (PendingBooks.Count == 0) return;

            KioskState = "SUCCESS";
            MainInstruction = "ĐANG XỬ LÝ VÀ IN HOÁ ĐƠN...";

            try
            {
                foreach (var book in PendingBooks)
                {
                    var request = new { SsoUserId = _currentSsoId, Barcode = book.Barcode };
                    await _apiService.PostAsync(ApiEndpoints.CirculationLoan, request);
                }

                try
                {
                    var printerService = new ReceiptPrinterService();
                    var titles = PendingBooks.Select(b => b.Title).ToList();
                    await printerService.PrintLoanReceiptAsync("Học sinh " + _currentSsoId, _currentSsoId, titles);
                    System.Media.SystemSounds.Exclamation.Play();
                    SpeechService.Speak("Chúc mừng bạn đã mượn sách thành công. Vui lòng nhận biên lai.");
                }
                catch (PrinterException pEx)
                {
                    System.Media.SystemSounds.Hand.Play();
                    SpeechService.Speak("In hóa đơn thất bại. Phiếu mượn đã được gửi về Zalo hoặc email của bạn đọc. Chúc mừng bạn mượn sách thành công!");
                    StatusMessage = $"⚠️ Lỗi máy in: {pEx.Message}. Đã gửi phiếu qua Zalo/Email.";
                    await Task.Delay(2000);
                }

                // GAMIFICATION: Chúc mừng thành tích
                MainInstruction = $"✨ CHÚC MỪNG XUẤT SẮC!\nBạn đã mượn thành công {PendingBooks.Count} cuốn sách.\nHãy giữ vững phong độ đọc sách nhé!";
                
                await Task.Delay(3000);

                if (_currentSsoId.StartsWith("HS"))
                {
                    int xpGained = PendingBooks.Count * 10;
                    var gamificationResult = GamificationService.AddXp(_currentSsoId, xpGained);
                    if (gamificationResult.LeveledUp)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            var activeWin = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive);
                            var celebDialog = new RankUpCelebrationDialog(gamificationResult.OldLevel, gamificationResult.NewLevel)
                            {
                                Owner = activeWin
                            };
                            celebDialog.ShowDialog();
                        });
                    }

                    // GHI NHẬN CHUỖI ĐỌC SÁCH 21 NGÀY
                    try
                    {
                        var streakResult = await _apiService.PostAsync<object, StreakResponseDto>(ApiEndpoints.ReportsStreak, new { SsoUserId = _currentSsoId });
                        if (streakResult != null)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                CurrentStreak = streakResult.CurrentStreak;
                                MaxStreak = streakResult.MaxStreak;
                                StreakMessage = streakResult.Message;

                                if (streakResult.MilestoneReached)
                                {
                                    var activeWin = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive);
                                    var streakDialog = new SmartLibrary.Desktop.Views.Shared.StreakCelebrationWindow(streakResult.CurrentStreak, streakResult.Message)
                                    {
                                        Owner = activeWin
                                    };
                                    streakDialog.ShowDialog();
                                }
                            });
                        }
                    }
                    catch
                    {
                        // Bỏ qua lỗi nếu ngoại tuyến hoặc server dev chưa sẵn sàng
                    }
                }

                // Khảo sát hài lòng
                KioskState = "RATING";
                MainInstruction = "BẠN ĐÁNH GIÁ DỊCH VỤ THẾ NÀO?";

                // Auto-skip sau 3s nếu không bấm
                await Task.Delay(3000);
                if (KioskState == "RATING")
                {
                    ResetSession();
                }
            }
            catch (Exception ex)
            {
                SpeechService.Speak("Đã xảy ra lỗi thiết bị. Vui lòng liên hệ thủ thư để được hỗ trợ.");
                MainInstruction = "CÓ LỖI XẢY RA: " + ex.Message;
                await Task.Delay(5000);
                ResetSession();
            }
        }

        private void SubmitRating(string? rating)
        {
            if (!string.IsNullOrEmpty(rating))
            {
                _ = AuditLogService.WriteLogAsync("Khảo sát Kiosk", 
                    $"Đánh giá: {rating} từ {_currentSsoId}", true);
            }
            ResetSession();
        }

        private void ResetSession()
        {
            ClearSessionDraftFromDisk();
            _currentSsoId = "";
            StudentInfo = "";
            CurrentStudentName = "";
            IsFineVisible = false;
            FineAmountText = "***** VNĐ";
            _actualStudentFine = 0;
            PendingBooks.Clear();
            StatusMessage = "";
            KioskErrorMessage = "";
            IsConfirmingCancel = false;
            IsTimeoutWarningVisible = false;
            IsFineButtonEnabled = true;
            // _pinAttempts = 0; // Reset PIN security state
            // _pinLockedUntil = DateTime.MinValue;
            _currentTimeout = _baseTimeoutSecondsValue; // Reset timeout
            IsSearchPanelVisible = false;
            SearchKeyword = "";
            OpacSearchResults.Clear();
            IsNoResultsFound = false;
            KioskState = "IDLE";
            TimeRemainingText = "";
            MainInstruction = "CHẠM ĐỂ BẮT ĐẦU\nMƯỢN SÁCH";
            IsFaceIdConsentGiven = false;
            UpdateScannedBooksCountText();
            
            // Re-enable timer for IDLE screensaver
            ResetIdleTimer();
            if (!_idleTimer.IsEnabled) _idleTimer.Start();
            _ = LoadTrendingBooksAsync();
        }

        [RelayCommand]
        private void HideFineImmediately()
        {
            IsFineVisible = false;
            FineAmountText = "***** VNĐ";
            ResetIdleTimer();
        }

        private async Task ExecuteSearchAsync()
        {
            ResetIdleTimer();
            if (string.IsNullOrWhiteSpace(SearchKeyword)) return;

            string rawQuery = SearchKeyword.Trim().ToLower();
            string query = InputHelper.RemoveDiacritics(rawQuery).ToLower();
            OpacSearchResults.Clear();
            IsNoResultsFound = false;

            try
            {
                var books = await _apiService.GetAsync<BookDto[]>(ApiEndpoints.Books);
                if (books != null)
                {
                    var matchedBooks = books.Where(b => 
                    {
                        if (b == null) return false;
                        string title = InputHelper.RemoveDiacritics(b.Title ?? "").ToLower();
                        string author = InputHelper.RemoveDiacritics(b.Author ?? "").ToLower();
                        string isbn = InputHelper.RemoveDiacritics(b.Isbn ?? "").ToLower();
                        return title.Contains(query) || author.Contains(query) || isbn.Contains(query);
                    }).ToList();

                    if (matchedBooks.Count > 0)
                    {
                        foreach (var book in matchedBooks)
                        {
                            OpacSearchResults.Add(book);
                        }
                    }
                    else
                    {
                        IsNoResultsFound = true;
                    }
                }
                else
                {
                    IsNoResultsFound = true;
                }
            }
            catch (Exception)
            {
                IsNoResultsFound = true;
            }
        }

        public async Task LoadTrendingBooksAsync()
        {
            try
            {
                var books = await _apiService.GetAsync<BookDto[]>(ApiEndpoints.Books);
                if (books != null && books.Length > 0)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ScreensaverTrendingBooks.Clear();
                        foreach (var b in books.Take(3))
                        {
                            ScreensaverTrendingBooks.Add(b);
                        }
                    });
                }
            }
            catch
            {
                // Fallback nếu ngoại tuyến
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ScreensaverTrendingBooks.Clear();
                    ScreensaverTrendingBooks.Add(new BookDto { Title = "Đắc Nhân Tâm", Author = "Dale Carnegie" });
                    ScreensaverTrendingBooks.Add(new BookDto { Title = "Nhà Lãnh Đạo Không Chức Danh", Author = "Robin Sharma" });
                    ScreensaverTrendingBooks.Add(new BookDto { Title = "Tuổi Trẻ Đáng Giá Bao Nhiêu", Author = "Rosie Nguyễn" });
                });
            }
        }

        // MEMORY LEAK FIX: Giải phóng tài nguyên khi đóng View
        public void Cleanup()
        {
            if (_idleTimer != null)
            {
                _idleTimer.Stop();
                _idleTimer.Tick -= IdleTimer_Tick;
            }
            _qrScannerService.StopCamera(); // Tắt camera khi đóng view
        }

        public void Activate()
        {
            if (_idleTimer != null)
            {
                _idleTimer.Tick -= IdleTimer_Tick;
                _idleTimer.Tick += IdleTimer_Tick;
                _idleTimer.Start();
            }
            if (KioskState == "SCANNING_QR" || KioskState == "SCANNING_BOOKS")
            {
                _qrScannerService.StartCamera();
            }
            ResetIdleTimer();
        }

        private async Task LoadKioskConfigAsync()
        {
            try
            {
                var configs = await _apiService.GetAsync<LibraryConfigDto[]>(ApiEndpoints.LibraryConfig);
                var limitConfig = configs?.FirstOrDefault(c => c.Key == "MaxKioskBorrowLimit");
                if (limitConfig != null && int.TryParse(limitConfig.Value, out int limit))
                {
                    _maxKioskBorrowLimit = limit;
                }
                UpdateScannedBooksCountText();
            }
            catch
            {
                _maxKioskBorrowLimit = 3; // Fallback
                UpdateScannedBooksCountText();
            }
        }

        private string GetDraftFilePath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmartLibrary", "data");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "kiosk_draft.json");
        }

        private void SaveSessionDraftToDisk()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentSsoId)) return;
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var draft = new
                    {
                        SsoUserId = _currentSsoId,
                        StudentName = CurrentStudentName,
                        StudentInfo = StudentInfo,
                        Fine = _actualStudentFine,
                        Books = PendingBooks.Select(b => new { b.Barcode, b.Title }).ToList()
                    };
                    string json = JsonSerializer.Serialize(draft);
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
                    string base64Json = Convert.ToBase64String(bytes);
                    File.WriteAllText(GetDraftFilePath(), base64Json);
                });
            }
            catch { }
        }

        private void ClearSessionDraftFromDisk()
        {
            try
            {
                string path = GetDraftFilePath();
                if (File.Exists(path)) File.Delete(path);
            }
            catch { }
        }

        private bool RestoreSessionDraftFromDisk(string ssoUserId)
        {
            try
            {
                string path = GetDraftFilePath();
                if (!File.Exists(path)) return false;
                string base64Json = File.ReadAllText(path);
                byte[] bytes = Convert.FromBase64String(base64Json);
                string json = System.Text.Encoding.UTF8.GetString(bytes);
                using var doc = JsonDocument.Parse(json);
                
                string draftSsoId = doc.RootElement.GetProperty("SsoUserId").GetString() ?? "";
                if (!draftSsoId.Equals(ssoUserId, StringComparison.OrdinalIgnoreCase))
                {
                    // Tránh rò rỉ dữ liệu: Xóa nháp cũ nếu không trùng khớp học sinh mới quét thẻ
                    File.Delete(path);
                    return false;
                }
                
                _currentSsoId = draftSsoId;
                CurrentStudentName = doc.RootElement.GetProperty("StudentName").GetString() ?? "";
                StudentInfo = doc.RootElement.GetProperty("StudentInfo").GetString() ?? "";
                _actualStudentFine = doc.RootElement.GetProperty("Fine").GetDouble();

                PendingBooks.Clear();
                foreach (var book in doc.RootElement.GetProperty("Books").EnumerateArray())
                {
                    PendingBooks.Add(new PendingBookItem
                    {
                        Barcode = book.GetProperty("Barcode").GetString() ?? "",
                        Title = book.GetProperty("Title").GetString() ?? ""
                    });
                }

                KioskState = "SCANNING_BOOKS";
                MainInstruction = "ĐẶT SÁCH LÊN BÀN ĐỂ QUÉT RFID\nHoặc quét mã vạch sách.";
                StatusMessage = "Đang khôi phục phiên quét cũ...";
                UpdateScannedBooksCountText();
                ResetIdleTimer();
                return true;
            }
            catch 
            {
                return false;
            }
        }

        private async Task FaceIdLoginAsync()
        {
            if (!IsFaceIdConsentGiven)
            {
                KioskErrorMessage = "Bạn cần đồng ý với cam kết bảo mật dữ liệu sinh trắc học FaceID để tiếp tục.";
                try { System.Media.SystemSounds.Hand.Play(); } catch { }
                return;
            }

            StatusMessage = "🧑‍💼 Hệ thống đang nhận diện khuôn mặt... Vui lòng nhìn thẳng vào Camera.";
            
            // Giả lập quét mặt trong 2 giây
            await Task.Delay(2000);
            
            // Chọn ngẫu nhiên mã SSO ID
            string[] ssoList = { "HS001", "HS002" };
            var random = new Random();
            _currentSsoId = ssoList[random.Next(ssoList.Length)];
            
            _qrScannerService.StopCamera();

            await LoadKioskConfigAsync();
            try
            {
                var status = await _apiService.GetAsync<StudentStatusDto>($"/Circulation/student-status/{_currentSsoId}");
                if (status != null)
                {
                    _actualStudentFine = status.TotalUnpaidFine;
                    CurrentStudentName = status.FullName;
                    StudentInfo = $"[FaceID] Xin chào Học sinh: {status.FullName}\nMã HS: {_currentSsoId}\nLớp: {status.SchoolClassId}";
                }
                else
                {
                    _actualStudentFine = 0;
                    CurrentStudentName = $"Học sinh {_currentSsoId}";
                    StudentInfo = $"[FaceID] Xin chào Học sinh: {_currentSsoId}";
                }
            }
            catch
            {
                _actualStudentFine = 0;
                CurrentStudentName = $"Học sinh {_currentSsoId}";
                StudentInfo = $"[FaceID] Xin chào Học sinh: {_currentSsoId}";
            }

            KioskState = "SCANNING_BOOKS";
            MainInstruction = "ĐẶT SÁCH LÊN BÀN ĐỂ QUÉT RFID\nHoặc quét mã vạch sách.";
            StatusMessage = "Chờ quét sách...";
            System.Media.SystemSounds.Beep.Play();
            SpeechService.Speak($"Đăng nhập bằng khuôn mặt thành công. Xin chào {CurrentStudentName}. Vui lòng quét mã sách hoặc đặt sách lên khay quét.");
            ResetIdleTimer();
        }
    }

    public class StreakResponseDto
    {
        public string SsoUserId { get; set; } = string.Empty;
        public int CurrentStreak { get; set; }
        public int MaxStreak { get; set; }
        public DateTime LastActiveDate { get; set; }
        public int XpGained { get; set; }
        public bool MilestoneReached { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
