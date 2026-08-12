using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;
using SmartLibrary.Desktop.Views.Shared;
using SmartLibrary.Desktop.Views.Admin;
using SmartLibrary.Desktop.Views.Librarian;
using SmartLibrary.Desktop.Views.Student;
using SmartLibrary.Desktop.Views.Teacher;
using System.Windows.Controls;
using SmartLibrary.Desktop.Helpers;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly AuthService _authService;
        private readonly ApiService _apiService;

        [ObservableProperty]
        private string _userName = "";

        [ObservableProperty]
        private string _userRole = "";

        [ObservableProperty]
        private string _userInitials = "";

        [ObservableProperty]
        private object? _currentView;

        [ObservableProperty]
        private string _searchKeyword = "";

        [ObservableProperty]
        private bool _isSearchPopupOpen = false;

        [ObservableProperty]
        private bool _isNoResultsFound = false;

        // Debounce cho Global Search
        private DispatcherTimer? _globalSearchDebounceTimer;
        private const int GlobalSearchDebounceMs = 400;

        private List<BookDto>? _booksCache = null;
        private DateTime _lastCacheTime = DateTime.MinValue;

        public ObservableCollection<GlobalSearchResult> GlobalSearchResults { get; } = new();

        // Gamification Properties
        [ObservableProperty]
        private int _currentXP = 350;

        [ObservableProperty]
        private int _maxXP = 500;

        [ObservableProperty]
        private string _currentLevel = "Cấp 2: Độc giả Bạc";

        [ObservableProperty] private string _equippedAvatar = "👦";
        [ObservableProperty] private System.Windows.Media.Brush _equippedBorderBrush = System.Windows.Media.Brushes.Transparent;
        [ObservableProperty] private System.Windows.Thickness _equippedBorderThickness = new System.Windows.Thickness(0);
        [ObservableProperty] private System.Windows.Media.Effects.Effect? _equippedBorderEffect = null;

        public ObservableCollection<MenuItemViewModel> MenuItems { get; } = new();

        public ICommand MenuClickCommand { get; }
        public ICommand GlobalSearchCommand { get; }
        public ICommand CloseSearchPopupCommand { get; }
        public ICommand BorrowFromSearchCommand { get; }
        public ICommand RetryCommand { get; }
        public ICommand LogoutCommand { get; }
        public event Action? OnLogoutRequested;

        public MainViewModel(AuthService authService, ApiService apiService)
        {
            _authService = authService;
            _apiService = apiService;
            MenuClickCommand = new RelayCommand<string>(OnMenuClick);
            GlobalSearchCommand = new AsyncRelayCommand(ExecuteGlobalSearchAsync);
            CloseSearchPopupCommand = new RelayCommand(() => IsSearchPopupOpen = false);
            BorrowFromSearchCommand = new RelayCommand<string>(ExecuteBorrowFromSearch);
            RetryCommand = new RelayCommand(ExecuteRetry);
            LogoutCommand = new RelayCommand(() => OnLogoutRequested?.Invoke());

            // Khởi tạo Debounce Timer cho Global Search
            _globalSearchDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(GlobalSearchDebounceMs)
            };
            _globalSearchDebounceTimer.Tick += async (s, e) =>
            {
                _globalSearchDebounceTimer.Stop();
                await ExecuteGlobalSearchAsync();
            };
        }

        /// <summary>
        /// Debounce trigger: Gọi tự động khi SearchKeyword thay đổi (source-generated partial method)
        /// </summary>
        partial void OnSearchKeywordChanged(string value)
        {
            // Reset debounce timer mỗi khi user gõ
            _globalSearchDebounceTimer?.Stop();

            if (string.IsNullOrWhiteSpace(value))
            {
                // Nếu xóa sạch text → đóng popup ngay
                IsSearchPopupOpen = false;
                GlobalSearchResults.Clear();
                IsNoResultsFound = false;
                return;
            }

            _globalSearchDebounceTimer?.Start();
        }

        partial void OnUserNameChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var words = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length >= 2)
                {
                    UserInitials = (words[0][0].ToString() + words[words.Length - 1][0].ToString()).ToUpper();
                }
                else if (words.Length == 1)
                {
                    UserInitials = words[0].Substring(0, Math.Min(2, words[0].Length)).ToUpper();
                }
                else
                {
                    UserInitials = "TT";
                }
            }
            else
            {
                UserInitials = "TT";
            }
        }

        private async Task ExecuteGlobalSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword)) return;

            string query = SearchKeyword.Trim().ToLower();
            GlobalSearchResults.Clear();
            IsNoResultsFound = false;

            try
            {
                List<BookDto> books;
                if (_booksCache != null && (DateTime.Now - _lastCacheTime).TotalMinutes < 1)
                {
                    books = _booksCache;
                }
                else
                {
                    var serverBooks = await _apiService.GetAsync<BookDto[]>(ApiEndpoints.Books);
                    books = serverBooks?.ToList() ?? new List<BookDto>();
                    if (books.Count > 0)
                    {
                        _booksCache = books;
                        _lastCacheTime = DateTime.Now;
                    }
                }

                if (books != null && books.Count > 0)
                {
                    string matchQuery = query;
                    if (query.Contains("-"))
                    {
                        matchQuery = query.Split('-')[0];
                    }

                    var matchedBooks = books.Where(b => 
                        b.Title.ToLower().Contains(matchQuery) || 
                        b.Author.ToLower().Contains(matchQuery) || 
                        (b.Isbn != null && b.Isbn.ToLower().Contains(matchQuery))
                    ).Take(5).ToList();

                    if (matchedBooks.Count > 0)
                    {
                        var random = new Random();
                        string[] shelves = { "Kệ A1, Tầng 1", "Kệ A2, Tầng 2", "Kệ A3, Tầng 1", "Kệ B1, Tầng 3", "Kệ B2, Tầng 2" };
                        foreach (var book in matchedBooks)
                        {
                            BookLookupService.RecordLookup(book.Id);
                            GlobalSearchResults.Add(new GlobalSearchResult
                            {
                                Title = book.Title,
                                Author = book.Author,
                                Barcode = book.Isbn ?? $"S{book.Id:000}",
                                AvailableCopies = book.AvailableCopies,
                                TotalCopies = book.TotalCopies,
                                ShelfLocation = shelves[book.Id % shelves.Length]
                            });
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
                // Fallback giả lập nếu API mất mạng hoặc đang test offline
                if (query.Contains("đắc") || query.Contains("nhân") || query.Contains("tâm"))
                {
                    GlobalSearchResults.Add(new GlobalSearchResult { Title = "Đắc Nhân Tâm", Author = "Dale Carnegie", Barcode = "ISBN-9781607967552", AvailableCopies = 2, TotalCopies = 5, ShelfLocation = "Kệ A3, Tầng 2" });
                }
                else if (query.Contains("conan"))
                {
                    GlobalSearchResults.Add(new GlobalSearchResult { Title = "Thám tử lừng danh Conan - Tập 95", Author = "Gosho Aoyama", Barcode = "ISBN-9786042131971", AvailableCopies = 1, TotalCopies = 3, ShelfLocation = "Kệ B1, Tầng 1" });
                    GlobalSearchResults.Add(new GlobalSearchResult { Title = "Thám tử lừng danh Conan - Tập 96", Author = "Gosho Aoyama", Barcode = "ISBN-9786042131988", AvailableCopies = 0, TotalCopies = 2, ShelfLocation = "Kệ B1, Tầng 1" });
                }
                else
                {
                    GlobalSearchResults.Add(new GlobalSearchResult 
                    { 
                        Title = $"Sách giả lập: {SearchKeyword}", 
                        Author = "Nhiều tác giả", 
                        Barcode = "S001", 
                        AvailableCopies = 3, 
                        TotalCopies = 5, 
                        ShelfLocation = "Kệ A2, Tầng 1" 
                    });
                }
            }

            IsSearchPopupOpen = true;
        }

        private void ExecuteBorrowFromSearch(string? barcode)
        {
            if (string.IsNullOrEmpty(barcode)) return;

            // Kiểm tra số lượng bản sao khả dụng
            var searchResult = GlobalSearchResults.FirstOrDefault(r => r.Barcode == barcode);
            if (searchResult != null && searchResult.AvailableCopies <= 0)
            {
                System.Windows.MessageBox.Show("Cuốn sách này đã hết bản sao khả dụng trong kho! Không thể mượn.", "Cảnh báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (CurrentView is UserControl userControl && userControl.DataContext is CirculationViewModel circulationVm)
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await circulationVm.HandleBarcodeScannedAsync(barcode);
                    IsSearchPopupOpen = false;
                    SearchKeyword = "";
                });
            }
            else
            {
                System.Windows.MessageBox.Show("Vui lòng chuyển sang màn hình Mượn/Trả sách để thực hiện mượn cuốn sách này nhanh!", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        private void ExecuteRetry()
        {
            _booksCache = null;
            _lastCacheTime = DateTime.MinValue;
            ApiService.IsOfflineMode = false;
            var win = System.Windows.Application.Current.MainWindow as MainWindow;
            if (win != null)
            {
                win.OfflineOverlay.Visibility = System.Windows.Visibility.Collapsed;
            }
            Initialize(); // Re-load data
        }

        private readonly Dictionary<string, UserControl> _viewCache = new();
        private RfidReaderService? _sharedRfidService;
        private QrScannerService? _sharedQrService;

        private void OnMenuClick(string? menuText)
        {
            if (string.IsNullOrEmpty(menuText)) return;

            // Dọn dẹp thiết bị ghi âm MCI nếu chuyển đi từ màn hình Audiobook Studio
            if (CurrentView is UserControl currentUc && currentUc.DataContext is AudiobookStudioViewModel studioVmBefore)
            {
                studioVmBefore.CleanupMciDevices();
            }

            // Dọn dẹp thiết bị di chuyển mô phỏng nếu chuyển đi từ màn hình Chỉ đường
            if (CurrentView is UserControl currentUc2 && currentUc2.DataContext is LibraryNavigatorViewModel navVmBefore)
            {
                navVmBefore.StopSimulation();
            }

            // Giải phóng sự kiện RFID của BookManagement khi chuyển tab
            if (CurrentView is UserControl currentUc3 && currentUc3.DataContext is BookManagementViewModel bookVmBefore)
            {
                bookVmBefore.Cleanup();
            }

            // Giải phóng sự kiện RFID của Kiểm kho khi chuyển tab
            if (CurrentView is UserControl currentUc4 && currentUc4.DataContext is InventoryCheckViewModel checkVmBefore)
            {
                checkVmBefore.Cleanup();
            }

            // Dọn dẹp camera và timer của Kiosk khi chuyển tab
            if (CurrentView is UserControl currentUc5 && currentUc5.DataContext is KioskModeViewModel kioskVmBefore)
            {
                kioskVmBefore.Cleanup();
            }

            // Hủy lắng nghe sự kiện Nhật ký Hệ thống để tránh leak bộ nhớ tĩnh
            if (CurrentView is UserControl currentUc6 && currentUc6.DataContext is SystemLogViewModel logVmBefore)
            {
                logVmBefore.Cleanup();
            }

            // Dừng phát âm thanh nghe thử sách nói nếu thủ thư chuyển tab đi
            if (CurrentView is UserControl currentUc7 && currentUc7.DataContext is AudiobookApprovalViewModel audioVmBefore)
            {
                audioVmBefore.StopAudio();
            }

            foreach (var item in MenuItems)
            {
                if (item.IsTitle)
                {
                    foreach (var subItem in item.SubItems)
                    {
                        subItem.IsActive = (subItem.Text == menuText);
                    }
                }
                else
                {
                    item.IsActive = (item.Text == menuText);
                }
            }

            if (_sharedRfidService == null) _sharedRfidService = new RfidReaderService();
            if (_sharedQrService == null) _sharedQrService = new QrScannerService();

            string cacheKey = menuText;
            if (menuText == "📈 Tiến độ đọc của lớp")
            {
                cacheKey = "🏫 Sách tự học lớp học";
            }

            if (!_viewCache.TryGetValue(cacheKey, out var view))
            {
                if (menuText == "📊 Dashboard")
                {
                    view = new LibrarianDashboard { DataContext = new DashboardViewModel(_apiService) };
                }
                else if (menuText == "📖 Mượn trả")
                {
                    view = new CirculationView { DataContext = new CirculationViewModel(_apiService) };
                }
                else if (menuText == "📚 Danh mục sách" || menuText == "📥 Nhập kho")
                {
                    view = new BookManagementView { DataContext = new BookManagementViewModel(_apiService, _sharedRfidService) };
                }
                else if (menuText == "🔍 Kiểm kho")
                {
                    view = new InventoryCheckView { DataContext = new InventoryCheckViewModel(_apiService, _sharedRfidService) };
                }
                else if (menuText == "⭐ Phê duyệt đánh giá")
                {
                    view = new Views.Librarian.BookReviewApprovalView { DataContext = new BookReviewApprovalViewModel(_apiService) };
                }
                else if (menuText == "🔔 SMS/Zalo Nhắc nợ")
                {
                    view = new Views.Admin.NotificationHubView { DataContext = new NotificationHubViewModel(_apiService) };
                }
                else if (menuText == "🖥️ Kiosk (Tự phục vụ)")
                {
                    view = new KioskModeView { DataContext = new KioskModeViewModel(_apiService, _sharedQrService) };
                }
                else if (menuText == "⏱️ Dự trữ")
                {
                    view = new BookReservationView { DataContext = new BookReservationViewModel(_apiService) };
                }
                else if (menuText == "💰 Phí hao mòn tài liệu")
                {
                    view = new FinesManagementView { DataContext = new FinesManagementViewModel(_apiService) };
                }
                else if (menuText == "📈 Báo cáo sử dụng")
                {
                    view = new UsageReportsView { DataContext = new UsageReportsViewModel(_apiService) };
                }
                else if (menuText == "⚙️ Danh mục dùng chung")
                {
                    view = new MasterDataView { DataContext = new MasterDataViewModel(_apiService) };
                }
                else if (menuText == "📖 Sổ đăng ký cá biệt")
                {
                    var regVm = new AccessionRegisterViewModel(_apiService);
                    view = new AccessionRegisterView { DataContext = regVm };
                }
                else if (menuText == "📜 Nhật ký hệ thống")
                {
                    view = new SystemLogView { DataContext = new SystemLogViewModel() };
                }
                else if (cacheKey == "🏫 Sách tự học lớp học")
                {
                    view = new Views.Teacher.TeacherDashboardView { DataContext = new TeacherDashboardViewModel(_apiService) };
                }
                else if (menuText == "🏆 Nhiệm vụ và huy hiệu")
                {
                    view = new Views.Student.LeaderboardView { DataContext = new LeaderboardViewModel(_apiService) };
                }
                else if (menuText == "🎁 Quyên góp sách")
                {
                    view = new Views.Student.BookDonationView { DataContext = new BookDonationViewModel(_apiService) };
                }
                else if (menuText == "📦 Nhận sách Locker 24/7")
                {
                    view = new Views.Shared.LockerReservationView { DataContext = new LockerReservationViewModel() };
                }
                else if (menuText == "🔍 Tra cứu và tìm kiếm sách")
                {
                    view = new Views.Student.StudentBookBrowseView { DataContext = new StudentBookBrowseViewModel(_apiService) };
                }
                else if (menuText == "📥 Đề xuất giáo trình")
                {
                    view = new Views.Teacher.ProposalView { DataContext = new ProposalViewModel(_apiService) };
                }
                else if (menuText == "📉 Báo cáo Thất thoát")
                {
                    view = new LossReportView { DataContext = new LossReportViewModel(_apiService) };
                }
                else if (menuText == "👥 Phân quyền")
                {
                    view = new UserPermissionsView { DataContext = new UserPermissionsViewModel(_apiService) };
                }
                else if (menuText == "🛍️ Cửa hàng đổi quà")
                {
                    view = new XpShopView { DataContext = new XpShopViewModel(_apiService) };
                }
                else if (menuText == "📥 Trả sách Drop-box")
                {
                    view = new SmartBookDropView { DataContext = new SmartBookDropViewModel(_apiService) };
                }
                else if (menuText == "📋 Nhiệm vụ giảng dạy")
                {
                    view = new TeacherMissionView { DataContext = new TeacherMissionViewModel(_apiService) };
                }
                else if (menuText == "📉 Tính hao mòn sách")
                {
                    view = new AssetDepreciationView { DataContext = new AssetDepreciationViewModel(_apiService) };
                }
                else if (menuText == "🎁 Duyệt đổi quà XP")
                {
                    view = new LibrarianXpShopView { DataContext = new LibrarianXpShopViewModel(_apiService) };
                }
                else if (menuText == "📍 Chỉ đường tìm sách")
                {
                    view = new LibraryNavigatorView { DataContext = new LibraryNavigatorViewModel(_apiService) };
                }
                else if (menuText == "🌱 Báo cáo carbon xanh")
                {
                    view = new GreenMetricsView { DataContext = new GreenMetricsViewModel(_apiService) };
                }
                else if (menuText == "📊 Phân tích học tập lớp")
                {
                    view = new TeacherAnalyticsView { DataContext = new TeacherAnalyticsViewModel(_apiService) };
                }
                else if (menuText == "🗺️ Sơ đồ nhiệt kệ sách")
                {
                    view = new ShelfHeatmapView { DataContext = new ShelfHeatmapViewModel(_apiService) };
                }
                else if (menuText == "🎙️ Đóng góp sách nói")
                {
                    view = new AudiobookStudioView { DataContext = new AudiobookStudioViewModel(_apiService) };
                }
                else if (menuText == "🎙️ Phê duyệt sách nói")
                {
                    view = new Views.Librarian.AudiobookApprovalView { DataContext = new AudiobookApprovalViewModel(_apiService) };
                }
                else if (menuText == "📺 Trình chiếu Signage")
                {
                    view = new ReadingSignageView { DataContext = new ReadingSignageViewModel(_apiService) };
                }
                else if (menuText == "📞 Hỗ trợ độc giả")
                {
                    view = new ReaderSupportView { DataContext = new ReaderSupportViewModel() };
                }
                else if (menuText == "👥 Nhóm đọc sách" || menuText == "ReadingGroup")
                {
                    view = new ReadingGroupView(new ReadingGroupViewModel(_apiService));
                }
                else if (menuText == "🌱 Thư viện xanh (IoT)" || menuText == "GreenEnergy")
                {
                    view = new GreenEnergyView(new GreenEnergyViewModel());
                }
                else
                {
                    // Placeholder cho các màn hình chưa làm
                    var textBlock = new System.Windows.Controls.TextBlock 
                    { 
                        Text = $"Màn hình {menuText} đang được phát triển...", 
                        FontSize = 24, 
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center, 
                        VerticalAlignment = System.Windows.VerticalAlignment.Center 
                    };
                    view = new UserControl { Content = textBlock };
                }
                _viewCache[cacheKey] = view;
            }
            
            UpdateAdminBadges();
            CurrentView = view;

            if (view.DataContext is IActiveAwareViewModel activeVm)
            {
                activeVm.Activate();
            }

            // Tự động làm mới dữ liệu khi chuyển tab để tránh kẹt dữ liệu cũ trong cache view
            if (view.DataContext is TeacherDashboardViewModel teacherVm)
            {
                teacherVm.SelectedTabIndex = (menuText == "📈 Tiến độ đọc của lớp") ? 1 : 0;
                _ = teacherVm.LoadDataAsync();
            }
            else if (view.DataContext is LeaderboardViewModel lbVm)
            {
                _ = lbVm.LoadDataFromServerAsync();
            }
            else if (view.DataContext is BookDonationViewModel donationVm)
            {
                _ = donationVm.LoadDataAsync();
            }
            else if (view.DataContext is StudentBookBrowseViewModel browseVm)
            {
                _ = browseVm.LoadDataFromServerAsync();
            }
            else if (view.DataContext is ProposalViewModel proposalVm)
            {
                _ = proposalVm.LoadMyProposalsFromServerAsync();
            }
            else if (view.DataContext is XpShopViewModel shopVm)
            {
                _ = shopVm.LoadDataAsync();
            }
            else if (view.DataContext is TeacherMissionViewModel missionVm)
            {
                _ = missionVm.LoadMissionsAsync();
            }
            else if (view.DataContext is SmartBookDropViewModel dropVm)
            {
                _ = dropVm.LoadBorrowedBooksAsync();
            }
            else if (view.DataContext is TeacherAnalyticsViewModel analyticsVm)
            {
                _ = analyticsVm.LoadAnalyticsAsync();
            }
            else if (view.DataContext is AudiobookStudioViewModel studioVm)
            {
                _ = studioVm.LoadBooksAsync();
            }
            else if (view.DataContext is LibraryNavigatorViewModel navigatorVm)
            {
                _ = navigatorVm.LoadBooksForSelectionAsync();
            }
            else if (view.DataContext is ReadingGroupViewModel groupVm)
            {
                _ = groupVm.LoadGroupsAsync();
            }
        }

        public void Initialize()
        {
            foreach (var view in _viewCache.Values)
            {
                if (view.DataContext is KioskModeViewModel kioskVm)
                {
                    kioskVm.Cleanup();
                }
                else if (view.DataContext is SystemLogViewModel systemLogVm)
                {
                    systemLogVm.Cleanup();
                }
                else if (view.DataContext is BookManagementViewModel bookVm)
                {
                    bookVm.Cleanup();
                }
                else if (view.DataContext is InventoryCheckViewModel checkVm)
                {
                    checkVm.Cleanup();
                }
            }
            _viewCache.Clear(); // Giải phóng cache để nạp lại view mới khi Online/Offline đổi
            UserName = AuthService.CurrentUserName ?? "Võ Minh Em";
            UserRole = AuthService.CurrentRole;

            if (UserRole == "Student")
            {
                var ssoId = AuthService.CurrentUserSsoId ?? "";
                var progress = GamificationService.GetProgress(ssoId);
                CurrentXP = progress.XP;
                CurrentLevel = progress.Level;
                MaxXP = GamificationService.GetMaxXpForLevel(progress.XP);
                UpdateEquippedAvatarInfo();

                // Đồng bộ từ máy chủ (chạy ngầm để không lock UI thread)
                _ = GamificationService.LoadProgressFromServerAsync(ssoId, (updatedProgress) =>
                {
                    CurrentXP = updatedProgress.XP;
                    CurrentLevel = updatedProgress.Level;
                    MaxXP = GamificationService.GetMaxXpForLevel(updatedProgress.XP);
                    UpdateEquippedAvatarInfo();
                });
            }

            LoadMenu();
            UpdateAdminBadges();
            
            // Fix: Tự động mở màn hình đầu tiên dựa vào vai trò
            if (UserRole == "Librarian" || UserRole == "Admin")
            {
                OnMenuClick("📊 Dashboard");
            }
            else if (UserRole == "Student")
            {
                OnMenuClick("🔍 Tra cứu và tìm kiếm sách");
            }
            else if (UserRole == "Teacher")
            {
                OnMenuClick("🏫 Sách tự học lớp học");
            }
        }

        public void UpdateEquippedAvatarInfo()
        {
            var ssoId = AuthService.CurrentUserSsoId ?? "";
            if (string.IsNullOrEmpty(ssoId)) return;
            var progress = GamificationService.GetProgress(ssoId);
            EquippedAvatar = progress.EquippedAvatar ?? "👦";
            
            string border = progress.EquippedBorder ?? "None";
            switch (border)
            {
                case "Neon":
                    EquippedBorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00F5FF"));
                    EquippedBorderThickness = new System.Windows.Thickness(2.5);
                    EquippedBorderEffect = new System.Windows.Media.Effects.DropShadowEffect 
                    { 
                        Color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00F5FF"), 
                        BlurRadius = 10, 
                        ShadowDepth = 0, 
                        Opacity = 0.85 
                    };
                    break;
                case "Gold":
                    EquippedBorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFD700"));
                    EquippedBorderThickness = new System.Windows.Thickness(2.5);
                    EquippedBorderEffect = new System.Windows.Media.Effects.DropShadowEffect 
                    { 
                        Color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFD700"), 
                        BlurRadius = 8, 
                        ShadowDepth = 0, 
                        Opacity = 0.8 
                    };
                    break;
                case "Mythic":
                    EquippedBorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#A855F7"));
                    EquippedBorderThickness = new System.Windows.Thickness(3);
                    EquippedBorderEffect = new System.Windows.Media.Effects.DropShadowEffect 
                    { 
                        Color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#A855F7"), 
                        BlurRadius = 12, 
                        ShadowDepth = 0, 
                        Opacity = 0.9 
                    };
                    break;
                default:
                    EquippedBorderBrush = System.Windows.Media.Brushes.Transparent;
                    EquippedBorderThickness = new System.Windows.Thickness(0);
                    EquippedBorderEffect = null;
                    break;
            }
        }

        public void LogoutCleanup()
        {
            Initialize(); // Dọn dẹp cache view và reset trạng thái nhàn rỗi
        }

        public void UpdateAdminBadges()
        {
            System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                int pendingCount = 0;
                try
                {
                    var proposals = await _apiService.GetAsync<ProposalItem[]>(ApiEndpoints.Proposals);
                    if (proposals != null)
                    {
                        pendingCount = proposals.Count(p => p.Status == "Chờ duyệt");
                    }
                }
                catch
                {
                    try
                    {
                        var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmartLibrary", "data", "proposals.json");
                        if (System.IO.File.Exists(filePath))
                        {
                            var json = System.IO.File.ReadAllText(filePath);
                            var proposals = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<ProposalItemMock>>(json);
                            if (proposals != null)
                            {
                                pendingCount = proposals.Count(p => p.Status == "Chờ duyệt");
                            }
                        }
                    }
                    catch { }
                }

                var systemAdminMenu = MenuItems.FirstOrDefault(m => m.Text == "III. QUẢN TRỊ HỆ THỐNG");
                if (systemAdminMenu != null)
                {
                    var masterDataMenu = systemAdminMenu.SubItems.FirstOrDefault(s => s.Text == "⚙️ Danh mục dùng chung");
                    if (masterDataMenu != null)
                    {
                        masterDataMenu.BadgeText = pendingCount > 0 ? pendingCount.ToString() : string.Empty;
                    }
                }
            });
        }

        private class ProposalItemMock
        {
            public string Status { get; set; } = "";
        }

        private void LoadMenu()
        {
            MenuItems.Clear();

            if (UserRole == "Librarian" || UserRole == "Admin")
            {
                MenuItems.Add(new MenuItemViewModel { Text = "📊 Dashboard", IsActive = true, IsTitle = false });

                var circulation = new MenuItemViewModel { Text = "I. NGHIỆP VỤ THƯ VIỆN", IsTitle = true, Icon = "🔄" };
                circulation.SubItems.Add(new MenuItemViewModel { Text = "📖 Mượn trả" });
                circulation.SubItems.Add(new MenuItemViewModel { Text = "⏱️ Dự trữ" });
                circulation.SubItems.Add(new MenuItemViewModel { Text = "💰 Phí hao mòn tài liệu" });
                circulation.SubItems.Add(new MenuItemViewModel { Text = "🖥️ Kiosk (Tự phục vụ)" });
                circulation.SubItems.Add(new MenuItemViewModel { Text = "🎁 Duyệt đổi quà XP" });
                MenuItems.Add(circulation);

                var bookMgmt = new MenuItemViewModel { Text = "II. KIỂM KÊ VÀ QUẢN LÝ", IsTitle = true, Icon = "📦" };
                bookMgmt.SubItems.Add(new MenuItemViewModel { Text = "📚 Danh mục sách" });
                bookMgmt.SubItems.Add(new MenuItemViewModel { Text = "📥 Nhập kho" });
                bookMgmt.SubItems.Add(new MenuItemViewModel { Text = "🔍 Kiểm kho" });
                bookMgmt.SubItems.Add(new MenuItemViewModel { Text = "🗺️ Sơ đồ nhiệt kệ sách" });
                bookMgmt.SubItems.Add(new MenuItemViewModel { Text = "📖 Sổ đăng ký cá biệt" });
                bookMgmt.SubItems.Add(new MenuItemViewModel { Text = "📉 Tính hao mòn sách" });
                bookMgmt.SubItems.Add(new MenuItemViewModel { Text = "📈 Báo cáo sử dụng" });
                bookMgmt.SubItems.Add(new MenuItemViewModel { Text = "🌱 Báo cáo carbon xanh" });
                bookMgmt.SubItems.Add(new MenuItemViewModel { Text = "🌱 Thư viện xanh (IoT)" });
                bookMgmt.SubItems.Add(new MenuItemViewModel { Text = "⭐ Phê duyệt đánh giá" });
                bookMgmt.SubItems.Add(new MenuItemViewModel { Text = "🎙️ Phê duyệt sách nói" });
                bookMgmt.SubItems.Add(new MenuItemViewModel { Text = "📺 Trình chiếu Signage" });
                MenuItems.Add(bookMgmt);

                var systemAdmin = new MenuItemViewModel { Text = "III. QUẢN TRỊ HỆ THỐNG", IsTitle = true, Icon = "⚙️" };
                systemAdmin.SubItems.Add(new MenuItemViewModel { Text = "⚙️ Danh mục dùng chung" });
                systemAdmin.SubItems.Add(new MenuItemViewModel { Text = "📜 Nhật ký hệ thống" });
                
                if (UserRole == "Admin")
                {
                    systemAdmin.SubItems.Add(new MenuItemViewModel { Text = "📉 Báo cáo Thất thoát" });
                    systemAdmin.SubItems.Add(new MenuItemViewModel { Text = "👥 Phân quyền" });
                    systemAdmin.SubItems.Add(new MenuItemViewModel { Text = "🔔 SMS/Zalo Nhắc nợ" });
                }
                MenuItems.Add(systemAdmin);
            }
            else if (UserRole == "Student")
            {
                var studentSection = new MenuItemViewModel { Text = "I. HỌC TẬP VÀ GIẢI TRÍ", IsTitle = true, Icon = "🎓" };
                studentSection.SubItems.Add(new MenuItemViewModel { Text = "🔍 Tra cứu và tìm kiếm sách", IsActive = true });
                studentSection.SubItems.Add(new MenuItemViewModel { Text = "👥 Nhóm đọc sách" });
                studentSection.SubItems.Add(new MenuItemViewModel { Text = "📍 Chỉ đường tìm sách" });
                studentSection.SubItems.Add(new MenuItemViewModel { Text = "🏆 Nhiệm vụ và huy hiệu" });
                studentSection.SubItems.Add(new MenuItemViewModel { Text = "🎙️ Đóng góp sách nói" });
                studentSection.SubItems.Add(new MenuItemViewModel { Text = "🎁 Quyên góp sách" });
                studentSection.SubItems.Add(new MenuItemViewModel { Text = "📦 Nhận sách Locker 24/7" });
                studentSection.SubItems.Add(new MenuItemViewModel { Text = "🛍️ Cửa hàng đổi quà" });
                studentSection.SubItems.Add(new MenuItemViewModel { Text = "📥 Trả sách Drop-box" });
                studentSection.SubItems.Add(new MenuItemViewModel { Text = "📞 Hỗ trợ độc giả" });
                MenuItems.Add(studentSection);
            }
            else if (UserRole == "Teacher")
            {
                var teacherSection = new MenuItemViewModel { Text = "I. HỖ TRỢ GIẢNG DẠY", IsTitle = true, Icon = "🏫" };
                teacherSection.SubItems.Add(new MenuItemViewModel { Text = "🏫 Sách tự học lớp học", IsActive = true });
                teacherSection.SubItems.Add(new MenuItemViewModel { Text = "📋 Nhiệm vụ giảng dạy" });
                teacherSection.SubItems.Add(new MenuItemViewModel { Text = "📊 Phân tích học tập lớp" });
                teacherSection.SubItems.Add(new MenuItemViewModel { Text = "📥 Đề xuất giáo trình" });
                teacherSection.SubItems.Add(new MenuItemViewModel { Text = "📈 Tiến độ đọc của lớp" });
                MenuItems.Add(teacherSection);
            }
        }
    }

    public class GlobalSearchResult
    {
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string ShelfLocation { get; set; } = "";
        public string Barcode { get; set; } = "";
        public int AvailableCopies { get; set; }
        public int TotalCopies { get; set; }

        public string AvailabilityText => AvailableCopies > 0 ? $"Còn {AvailableCopies}/{TotalCopies} cuốn" : "Hết sách";
        public string AvailabilityColor => AvailableCopies > 0 ? "#10B981" : "#EF4444";
    }
}
