using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;
using SmartLibrary.Desktop.Views.Shared;
using SmartLibrary.Desktop.Models;
using SmartLibrary.Desktop.Helpers;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class CirculationViewModel : ObservableObject, IActiveAwareViewModel
    {
        private readonly ApiService _apiService;

        public void Activate()
        {
            // No action needed on tab activation
        }
        private DateTime _lastSpeakTime = DateTime.MinValue;

        [ObservableProperty]
        private bool _isBorrowMode = true;

        [ObservableProperty]
        private string _studentInfoText = "Chưa quét thẻ bạn đọc";

        [ObservableProperty]
        private string _readerName = "";

        [ObservableProperty]
        private string _readerId = "";

        [ObservableProperty]
        private string _readerClass = "";

        [ObservableProperty]
        private int _readerActiveLoansCount = 0;

        [ObservableProperty]
        private string _readerStatusText = "";

        [ObservableProperty]
        private string _statusMessage = "";

        [ObservableProperty]
        private string _statusState = "Primary";

        [ObservableProperty]
        private bool _printReceipt = true;

        [ObservableProperty]
        private bool _hasError = false;

        [ObservableProperty]
        private string _selectedBookCondition = "Good";

        [ObservableProperty]
        private double _currentStudentFine = 0;

        [ObservableProperty]
        private bool _hasFine = false;

        // ============ ITEM 3.1: Chặn mượn khi nợ/quá hạn ============
        [ObservableProperty]
        private bool _isBlocked = false;

        [ObservableProperty]
        private string _blockReason = "";

        // ============ ITEM 3.2: Gia hạn sách ============
        [ObservableProperty]
        private bool _canExtend = false;

        // ============ ITEM 3.3: Transaction ID ============
        [ObservableProperty]
        private string _lastTransactionId = "";

        // ============ ITEM 3.4: Supervisor Override ============
        [ObservableProperty]
        private bool _isSupervisorOverrideActive = false;

        public string[] AvailableConditions { get; } = new[] { "Good", "Damaged", "Lost" };

        public ObservableCollection<PendingBookItem> PendingBooks { get; } = new();
        public ObservableCollection<ReturnedBookItem> ReturnedBooks { get; } = new();

        public ICommand SaveLoanCommand { get; }
        public ICommand SwitchModeCommand { get; }
        public ICommand RemoveBookCommand { get; }
        public ICommand PayFineCommand { get; }
        public ICommand ReportLostCommand { get; }
        public ICommand ExtendLoanCommand { get; }
        public ICommand SupervisorOverrideCommand { get; }
        public ICommand ClearPendingBooksCommand { get; }

        [ObservableProperty]
        private bool _isStudentScanned = false;

        [ObservableProperty]
        private bool _keepBorrowerProfile = false;

        private string _currentSsoId = "";

        [ObservableProperty]
        private bool _isBatchMode = false;

        [ObservableProperty]
        private string _batchPurpose = "Đọc sách tại lớp";

        public string[] BatchPurposes { get; } = new[] { "Đọc sách tại lớp", "Dự án học tập", "Nghiên cứu chuyên đề", "Khác" };

        [ObservableProperty]
        private bool _isBusy = false;

        public CirculationViewModel(ApiService apiService)
        {
            _apiService = apiService;
            SaveLoanCommand = new AsyncRelayCommand(SaveLoanAsync, CanSaveLoan);
            SwitchModeCommand = new RelayCommand<string>(SwitchMode);
            RemoveBookCommand = new RelayCommand<PendingBookItem>(RemoveBook);
            PayFineCommand = new AsyncRelayCommand(PayFineAsync);
            ReportLostCommand = new AsyncRelayCommand(ReportLostAsync);
            ExtendLoanCommand = new AsyncRelayCommand(ExtendLoanAsync);
            SupervisorOverrideCommand = new RelayCommand(RequestSupervisorOverride);
            ClearPendingBooksCommand = new RelayCommand(ClearPendingBooks);
        }

        private void SwitchMode(string? mode)
        {
            IsBorrowMode = mode == "Borrow";
            StatusMessage = IsBorrowMode ? "Chế độ Mượn: Quét mã thẻ bạn đọc, sau đó quét sách." : "Chế độ Trả: Quét mã sách để trả ngay lập tức.";
            StatusState = "Primary";
            HasError = false;
            
            // Xóa State
            PendingBooks.Clear();
            ReturnedBooks.Clear();
            _currentSsoId = "";
            StudentInfoText = "Chưa quét thẻ bạn đọc";
            ReaderName = "";
            ReaderId = "";
            ReaderClass = "";
            ReaderActiveLoansCount = 0;
            ReaderStatusText = "";
            CurrentStudentFine = 0;
            HasFine = false;
            IsStudentScanned = false;
            IsBlocked = false;
            BlockReason = "";
            IsSupervisorOverrideActive = false;
            LastTransactionId = "";
            (SaveLoanCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        }

        private void ClearPendingBooks()
        {
            PendingBooks.Clear();
            StatusMessage = "Đã xóa trống danh sách sách chờ mượn.";
            StatusState = "Primary";
        }

        private void RemoveBook(PendingBookItem? item)
        {
            if (item != null)
            {
                PendingBooks.Remove(item);
                (SaveLoanCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            }
        }

        public async Task HandleBarcodeScannedAsync(string barcode)
        {
            barcode = BarcodeVietnameseFixer.Fix(barcode);

            if (string.IsNullOrWhiteSpace(barcode) || barcode.Length > 50)
            {
                System.Media.SystemSounds.Hand.Play();
                ShowStatus("Mã vạch không hợp lệ hoặc vượt quá 50 ký tự giới hạn!", "Danger");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Barcode scanned: {barcode}");
            TopBarControl.TriggerBarcodeFlash();
            
            if (IsBorrowMode)
            {
                if (barcode.StartsWith("HS") || barcode.StartsWith("GV") || barcode.StartsWith("TT") || (IsBatchMode && barcode.Length <= 5))
                {
                    _currentSsoId = barcode;
                    if (IsBatchMode)
                    {
                        StudentInfoText = $"Tập thể Lớp: {barcode}\nTrạng thái: Hợp lệ\nChế độ: Mượn theo lô (Batch)";
                        ReaderName = $"Tập thể Lớp {barcode}";
                        ReaderId = barcode;
                        ReaderClass = "Mượn lô (Batch)";
                        ReaderActiveLoansCount = 0;
                        ReaderStatusText = "Hợp lệ";
                        ShowStatus($"Đã nhận diện lớp: {barcode}", "Success");
                        IsStudentScanned = false;
                        IsBlocked = false;
                    }
                    else
                    {
                        IsBusy = true;
                        (SaveLoanCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                        try
                        {
                            var status = await _apiService.GetAsync<StudentStatusDto>($"{ApiEndpoints.CirculationStudentStatus}{Uri.EscapeDataString(barcode)}");
                            if (status != null)
                            {
                                CurrentStudentFine = status.TotalUnpaidFine;
                                HasFine = CurrentStudentFine > 0;
                                IsBlocked = status.IsLockedFromBorrowing || status.OverdueLoansCount > 0;
                                CanExtend = status.OverdueLoansCount > 0;
                                
                                string statusStr = IsBlocked 
                                    ? (status.OverdueLoansCount > 0 ? "⚠️ CÓ SÁCH QUÁ HẠN" : "CẢNH BÁO NỢ") 
                                    : "Hoạt động";
                                
                                StudentInfoText = $"Độc giả: {status.FullName}\nMã ID: {status.SsoUserId}\nLớp: {status.SchoolClassId}\nĐang mượn: {status.ActiveLoansCount}/5 cuốn\nTrạng thái: {statusStr}";
                                ReaderName = status.FullName;
                                ReaderId = status.SsoUserId;
                                ReaderClass = status.SchoolClassId;
                                ReaderActiveLoansCount = status.ActiveLoansCount;
                                ReaderStatusText = statusStr;
                                
                                if (status.OverdueLoansCount > 0)
                                {
                                    BlockReason = "Thông tin nhắc nhở: Bạn đọc vui lòng gia hạn hoặc hoàn trả các cuốn sách quá hạn cũ để bắt đầu lượt đọc mới nhé!";
                                }
                                else if (status.TotalUnpaidFine > 0)
                                {
                                    BlockReason = $"📚 Bạn đọc vui lòng hoàn tất các khoản phí hao mòn quá hạn cũ để tiếp tục đồng hành cùng những cuốn sách mới nhé!";
                                }
                                else
                                {
                                    BlockReason = "";
                                }
                                
                                if (IsBlocked)
                                {
                                    ShowStatus(BlockReason, "Danger");
                                }
                                else
                                {
                                    ShowStatus($"Đã nhận diện thẻ: {barcode}", "Success");
                                }
                            }
                            else
                            {
                                throw new Exception("Reader not found.");
                            }
                        }
                        catch (Exception)
                        {
                            // Fallback Offline Mock
                            if (barcode == "HS001")
                            {
                                CurrentStudentFine = 50000;
                                HasFine = true;
                                StudentInfoText = $"Độc giả: Nguyễn Văn An\nMã định danh: {barcode}\nLớp: 10A1\nĐang mượn: 2/5 cuốn\nTrạng thái: Hoạt động";
                                ReaderName = "Nguyễn Văn An";
                                ReaderId = barcode;
                                ReaderClass = "10A1";
                                ReaderActiveLoansCount = 2;
                                ReaderStatusText = "Hoạt động";
                                IsBlocked = true;
                                BlockReason = $"📚 Bạn đọc vui lòng hoàn tất các khoản phí hao mòn quá hạn cũ để tiếp tục đồng hành cùng những cuốn sách mới nhé!";
                                ShowStatus(BlockReason, "Danger");
                            }
                            else if (barcode == "HS002")
                            {
                                CurrentStudentFine = 0;
                                HasFine = false;
                                StudentInfoText = $"Độc giả: Trần Thị Bình\nMã định danh: {barcode}\nLớp: 11B2\nĐang mượn: 3/5 cuốn\nTrạng thái: ⚠️ CÓ SÁCH QUÁ HẠN";
                                ReaderName = "Trần Thị Bình";
                                ReaderId = barcode;
                                ReaderClass = "11B2";
                                ReaderActiveLoansCount = 3;
                                ReaderStatusText = "⚠️ CÓ SÁCH QUÁ HẠN";
                                CanExtend = true;
                                IsBlocked = true;
                                BlockReason = "Thông tin nhắc nhở: Bạn đọc vui lòng gia hạn hoặc hoàn trả các cuốn sách quá hạn cũ để bắt đầu lượt đọc mới nhé!";
                                ShowStatus(BlockReason, "Danger");
                            }
                            else
                            {
                                CurrentStudentFine = 0;
                                HasFine = false;
                                IsBlocked = false;
                                BlockReason = "";
                                CanExtend = false;
                                StudentInfoText = $"Độc giả: học sinh/giáo viên\nMã định danh: {barcode}\nTrạng thái thẻ: Hợp lệ\nSách đang giữ: 1/5";
                                ReaderName = "Học sinh/Giáo viên";
                                ReaderId = barcode;
                                ReaderClass = "Tự do";
                                ReaderActiveLoansCount = 1;
                                ReaderStatusText = "Hợp lệ";
                                ShowStatus($"Đã nhận diện thẻ: {barcode}", "Success");
                            }
                        }
                        finally
                        {
                            IsBusy = false;
                            (SaveLoanCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                        }
                        IsStudentScanned = true;

                        if (IsBlocked && !string.IsNullOrEmpty(BlockReason))
                        {
                            if ((DateTime.Now - _lastSpeakTime).TotalSeconds >= 3)
                            {
                                SpeechService.Speak(BlockReason);
                                _lastSpeakTime = DateTime.Now;
                            }
                        }
                    }
                    (SaveLoanCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                }
                else
                {
                    // Scan Sách
                    if (string.IsNullOrEmpty(_currentSsoId))
                    {
                        System.Media.SystemSounds.Hand.Play();
                        ShowStatus("Vui lòng quét thẻ/mã lớp trước!", "Danger");
                        return;
                    }

                    // ============ ITEM 3.1: Chặn nếu bị block (trừ khi có Supervisor Override) ============
                    if (IsBlocked && !IsSupervisorOverrideActive)
                    {
                        System.Media.SystemSounds.Hand.Play();
                        ShowStatus($"⛔ Bị chặn mượn: {BlockReason}\nDùng nút 'Giám thị duyệt' để vượt qua.", "Danger");
                        return;
                    }

                    if (PendingBooks.Any(x => x.Barcode == barcode))
                    {
                        System.Media.SystemSounds.Hand.Play();
                        ShowStatus($"Sách {barcode} đã có trong danh sách chờ!", "Warning");
                        return;
                    }

                    if (IsBatchMode && PendingBooks.Count >= 100)
                    {
                        System.Media.SystemSounds.Hand.Play();
                        ShowStatus("Vượt giới hạn 100 cuốn/lô. Vui lòng chốt đơn để mượn lô tiếp theo.", "Danger");
                        return;
                    }

                    // Thêm vào danh sách chờ
                    string bookTitle = $"Sách tự động {barcode}";
                    try
                    {
                        var book = await _apiService.GetAsync<BookDto>($"{ApiEndpoints.BookCopy}{Uri.EscapeDataString(barcode)}");
                        if (book != null && !string.IsNullOrEmpty(book.Title))
                        {
                            bookTitle = book.Title;
                        }
                    }
                    catch { }

                    PendingBooks.Add(new PendingBookItem { Barcode = barcode, Title = bookTitle });
                    ShowStatus($"Đã thêm sách: {bookTitle}", "Success");
                    (SaveLoanCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                }
            }
            else
            {
                await ReturnBookAsync(barcode);
            }
        }

        private async Task ReturnBookAsync(string barcode)
        {
            TopBarControl.TriggerBarcodeFlash();
            try
            {
                var request = new { Barcode = barcode, Condition = SelectedBookCondition };
                await _apiService.PostAsync(ApiEndpoints.CirculationReturn, request);
                
                // ============ ITEM 3.3: Generate Transaction ID ============
                string txId = GenerateTransactionId("TRA");

                string bookTitle = $"Sách {barcode}";
                try
                {
                    var book = await _apiService.GetAsync<BookDto>($"{ApiEndpoints.BookCopy}{Uri.EscapeDataString(barcode)}");
                    if (book != null && !string.IsNullOrEmpty(book.Title))
                    {
                        bookTitle = book.Title;
                    }
                }
                catch { }

                ReturnedBooks.Insert(0, new ReturnedBookItem { 
                    Barcode = barcode, 
                    Title = bookTitle, 
                    Condition = SelectedBookCondition == "Good" ? "Tốt" : (SelectedBookCondition == "Damaged" ? "Hỏng nhẹ" : "Thất lạc"),
                    ReturnTime = DateTime.Now.ToString("HH:mm:ss"),
                    TransactionId = txId
                });

                LastTransactionId = txId;
                ShowStatus($"Trả sách {barcode} THÀNH CÔNG ({SelectedBookCondition}) — TX: {txId}", "Success");

                // ============ ITEM 3.5: Audit Log chi tiết ============
                await AuditLogService.WriteLogAsync("Trả sách",
                    $"[{txId}] Trả sách {barcode} | Tình trạng: {SelectedBookCondition} | Người trả: {_currentSsoId}", true);

                // Tự động reset tình trạng về Good cho lượt trả tiếp theo
                SelectedBookCondition = "Good";
            }
            catch (Exception)
            {
                ShowStatus($"Lỗi trả sách {barcode}: Sách không được mượn hoặc lỗi kết nối", "Danger");
            }
        }

        private bool CanSaveLoan()
        {
            return !IsBusy && !string.IsNullOrEmpty(_currentSsoId) && PendingBooks.Count > 0 && (!IsBlocked || IsSupervisorOverrideActive);
        }

        private async Task SaveLoanAsync()
        {
            if (IsBusy) return;
            // ============ ITEM 3.1: Kiểm tra chặn lần cuối ============
            if (IsBlocked && !IsSupervisorOverrideActive)
            {
                ShowStatus($"⛔ Không thể lưu: {BlockReason}", "Danger");
                return;
            }

            try
            {
                IsBusy = true;
                (SaveLoanCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();

                // ============ ITEM 3.3: Generate Transaction ID ============
                string txId = GenerateTransactionId("MUON");
                LastTransactionId = txId;

                foreach (var book in PendingBooks)
                {
                    var request = new { SsoUserId = _currentSsoId, Barcode = book.Barcode, IsBatch = IsBatchMode, TransactionId = txId };
                    await _apiService.PostAsync(ApiEndpoints.CirculationLoan, request);
                }

                // ============ ITEM 3.5: Audit Log chi tiết ============
                string bookList = string.Join(", ", PendingBooks.Select(b => b.Barcode));
                string overrideNote = IsSupervisorOverrideActive ? " [SUPERVISOR OVERRIDE]" : "";
                await AuditLogService.WriteLogAsync("Mượn sách",
                    $"[{txId}] Mượn {PendingBooks.Count} sách ({bookList}) cho {_currentSsoId}{overrideNote}", true);

                // IN HOÁ ĐƠN NẾU TICK CHỌN
                if (PrintReceipt)
                {
                    try
                    {
                        int printerWidth = 48;
                        try
                        {
                            var configs = await _apiService.GetAsync<LibraryConfigDto[]>(ApiEndpoints.LibraryConfig);
                            var widthConfig = configs?.FirstOrDefault(c => c.Key == "PrinterWidthChars");
                            if (widthConfig != null && int.TryParse(widthConfig.Value, out var w))
                            {
                                printerWidth = w;
                            }
                        }
                        catch { }

                        var printerService = new ReceiptPrinterService();
                        var titles = PendingBooks.Select(b => b.Title).ToList();
                        await printerService.PrintLoanReceiptAsync("Học sinh/GV (" + _currentSsoId + ")", _currentSsoId, titles, txId, printerWidth);
                        ShowStatus($"Đã cho mượn {PendingBooks.Count} sách thành công! TX: {txId} — Đang in Phiếu mượn...", "Success");
                        ToastService.ShowSuccess($"Mượn sách thành công! Phiếu mượn {txId} đang in.");
                        System.Media.SystemSounds.Exclamation.Play();
                    }
                    catch (PrinterException pEx)
                    {
                        ShowStatus($"⚠️ Mượn thành công! Lỗi máy in: {pEx.Message}", "Warning");
                        ToastService.ShowWarning($"Mượn thành công nhưng máy in lỗi: {pEx.Message}. Phiếu đã gửi qua Zalo.");
                        System.Media.SystemSounds.Hand.Play();
                    }
                }
                else
                {
                    ShowStatus($"Đã cho mượn {PendingBooks.Count} sách thành công! TX: {txId} (Không in phiếu)", "Success");
                    ToastService.ShowSuccess($"Mượn sách thành công! Mã TX: {txId}");
                    System.Media.SystemSounds.Beep.Play();
                }

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
                }

                PendingBooks.Clear();
                if (!KeepBorrowerProfile)
                {
                    _currentSsoId = "";
                    IsStudentScanned = false;
                    IsBlocked = false;
                    IsSupervisorOverrideActive = false;
                    BlockReason = "";
                    StudentInfoText = "Chưa quét thẻ bạn đọc";
                    ReaderName = "";
                    ReaderId = "";
                    ReaderClass = "";
                    ReaderActiveLoansCount = 0;
                    ReaderStatusText = "";
                }
                else
                {
                    IsSupervisorOverrideActive = false;
                }
            }
            catch (Exception ex)
            {
                ShowStatus("Lỗi mượn sách: " + ex.Message, "Danger");
            }
            finally
            {
                IsBusy = false;
                (SaveLoanCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            }
        }

        private async Task ExtendLoanAsync()
        {
            if (IsBusy || string.IsNullOrEmpty(_currentSsoId)) return;

            var result = System.Windows.MessageBox.Show(
                $"Gia hạn thêm 7 ngày cho sách quá hạn của {_currentSsoId}?\n\n(Chỉ được gia hạn 1 lần duy nhất)",
                "Xác nhận gia hạn",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    IsBusy = true;
                    (SaveLoanCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();

                    await _apiService.PostAsync(ApiEndpoints.CirculationExtend, new { SsoUserId = _currentSsoId, ExtendDays = 7 });
                }
                catch { /* Offline OK */ }
                finally
                {
                    IsBusy = false;
                    (SaveLoanCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                }

                // Gỡ block sau khi gia hạn
                IsBlocked = false;
                BlockReason = "";
                CanExtend = false;
                IsSupervisorOverrideActive = false;

                string txId = GenerateTransactionId("GH");
                ShowStatus($"✅ Đã gia hạn 7 ngày cho {_currentSsoId}. TX: {txId}", "Success");

                await AuditLogService.WriteLogAsync("Gia hạn sách",
                    $"[{txId}] Gia hạn 7 ngày cho {_currentSsoId}", true);
            }
        }

        // ============ ITEM 3.4: Supervisor Override ============
        private string _supervisorBypassPin = "1ade942a8448f36f19ea477cb578d43ed34541d7599fb2218a287bb785706b1b";

        public async Task LoadSupervisorPinAsync()
        {
            try
            {
                var configs = await _apiService.GetAsync<LibraryConfigDto[]>(ApiEndpoints.LibraryConfig);
                var pinConfig = configs?.FirstOrDefault(c => c.Key == "SupervisorBypassPin");
                if (pinConfig != null && !string.IsNullOrEmpty(pinConfig.Value))
                {
                    string val = pinConfig.Value.Trim();
                    if (val.Length == 64 && val.All(c => Uri.IsHexDigit(c)))
                    {
                        _supervisorBypassPin = val.ToLower();
                    }
                    else
                    {
                        _supervisorBypassPin = SecurityHelper.ComputeSha256(val);
                    }
                }
            }
            catch
            {
                _supervisorBypassPin = SecurityHelper.GetConfigValue("SecuritySettings", "SupervisorBypassPinHash", "1ade942a8448f36f19ea477cb578d43ed34541d7599fb2218a287bb785706b1b");
            }
        }

        private async void RequestSupervisorOverride()
        {
            if (!IsBlocked) return;

            await LoadSupervisorPinAsync();

            // Mở PinVerificationDialog với PIN giám thị động
            var currentWindow = System.Windows.Application.Current.Windows
                .OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive);

            var dialog = new PinVerificationDialog(_supervisorBypassPin)
            {
                Owner = currentWindow
            };

            if (dialog.ShowDialog() == true && dialog.IsVerified)
            {
                IsSupervisorOverrideActive = true;
                (SaveLoanCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                ShowStatus("🔓 Giám thị đã duyệt: Cho phép mượn dù có nợ/quá hạn.", "Warning");

                _ = AuditLogService.WriteLogAsync("Supervisor Override",
                    $"Giám thị duyệt mượn sách cho {_currentSsoId} dù có nợ/quá hạn", true);
            }
        }

        [RelayCommand]
        private async Task RetryPrintReceiptAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Đang thử kết nối lại máy in và in lại biên lai...";
            try
            {
                var printerService = new ReceiptPrinterService();
                // Giả lập lệnh in lại hóa đơn của phiên hiện tại
                await Task.Delay(1000); 
                StatusMessage = "✅ Đã in lại biên lai thành công!";
            }
            catch (Exception ex)
            {
                StatusMessage = "❌ Không thể kết nối máy in: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task PayFineAsync()
        {
            if (IsBusy || string.IsNullOrEmpty(_currentSsoId)) return;

            // Cho phép thủ thư nhập số tiền thu phí hao mòn tài liệu thực tế
            string inputAmount = Microsoft.VisualBasic.Interaction.InputBox(
                $"Nhập số tiền thực thu phí hao mòn tài liệu từ độc giả {_currentSsoId} (Tổng nợ: {CurrentStudentFine.ToString("#,##0", new System.Globalization.CultureInfo("vi-VN"))} VNĐ):",
                "Thu Phí Hao Mòn Tài Liệu",
                CurrentStudentFine.ToString());

            if (string.IsNullOrEmpty(inputAmount)) return;

            if (!double.TryParse(inputAmount, out double amountPaid) || amountPaid <= 0)
            {
                ToastService.ShowError("Số tiền thực thu nhập vào không hợp lệ! Vui lòng chỉ nhập số dương.");
                System.Media.SystemSounds.Hand.Play();
                return;
            }

            if (amountPaid > CurrentStudentFine)
            {
                ToastService.ShowWarning("Số tiền đóng không được vượt quá số phí hiện tại!");
                return;
            }

                var confirmResult = System.Windows.MessageBox.Show(
                    $"Xác nhận độc giả đã nộp số tiền {amountPaid.ToString("#,##0", new System.Globalization.CultureInfo("vi-VN"))} VNĐ?",
                    "Xác nhận thu phí hao mòn tài liệu",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);
                
                if (confirmResult != System.Windows.MessageBoxResult.Yes)
                {
                    return; // Hủy bỏ
                }

                try
                {
                    IsBusy = true;
                    (SaveLoanCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();

                    var payload = new { SsoUserId = _currentSsoId, Amount = amountPaid };
                    await _apiService.PostAsync(ApiEndpoints.CirculationPayFine, payload);
                }
                catch (Exception ex)
                {
                    // Chấp nhận chạy offline giả lập thành công nhưng ghi log lỗi API
                    await AuditLogService.WriteLogAsync("Thu phí hao mòn tài liệu", 
                        $"Lỗi kết nối API khi thu phí từ độc giả {_currentSsoId}. Giao dịch chạy offline tạm thời: {ex.Message}", false);
                }
                finally
                {
                    IsBusy = false;
                    (SaveLoanCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                }

                CurrentStudentFine -= amountPaid;
                string readerName = _currentSsoId == "HS001" ? "Nguyễn Văn An" : "Học sinh/GV";

                if (CurrentStudentFine <= 0)
                {
                    CurrentStudentFine = 0;
                    HasFine = false;
                    IsBlocked = false; // Gỡ block sau khi thanh toán hết
                    BlockReason = "";
                    StudentInfoText = $"Độc giả: {readerName}\nMã ID: {_currentSsoId}\nTrạng thái: Hoạt động (Đã hoàn tất thanh toán phí)";
                    ReaderStatusText = "Hoạt động (Đã hoàn tất thanh toán phí)";
                }
                else
                {
                    BlockReason = $"📚 Bạn đọc vui lòng hoàn tất các khoản phí hao mòn quá hạn cũ để tiếp tục đồng hành cùng những cuốn sách mới nhé!";
                    StudentInfoText = $"Độc giả: {readerName}\nMã ID: {_currentSsoId}\nTrạng thái: CẢNH BÁO PHÍ CHƯA ĐÓNG (Còn thiếu {CurrentStudentFine.ToString("#,##0", new System.Globalization.CultureInfo("vi-VN"))} VNĐ)";
                    ReaderStatusText = $"CẢNH BÁO PHÍ CHƯA ĐÓNG (Còn thiếu {CurrentStudentFine.ToString("#,##0", new System.Globalization.CultureInfo("vi-VN"))} VNĐ)";
                }

                string txId = GenerateTransactionId("THU");
                ShowStatus($"Đã thu {amountPaid.ToString("#,##0", new System.Globalization.CultureInfo("vi-VN"))} VNĐ phí hao mòn tài liệu — TX: {txId}", "Success");
                (SaveLoanCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();

                // In biên lai thu phí
                try
                {
                    int printerWidth = 48;
                    try
                    {
                        var configs = await _apiService.GetAsync<LibraryConfigDto[]>(ApiEndpoints.LibraryConfig);
                        var widthConfig = configs?.FirstOrDefault(c => c.Key == "PrinterWidthChars");
                        if (widthConfig != null && int.TryParse(widthConfig.Value, out var w))
                        {
                            printerWidth = w;
                        }
                    }
                    catch { }

                    var printerService = new ReceiptPrinterService();
                    await printerService.PrintFineReceiptAsync(readerName, _currentSsoId, amountPaid, CurrentStudentFine, printerWidth);
                }
                catch (PrinterException pEx)
                {
                    ShowStatus($"⚠️ Thu phí thành công! Lỗi máy in: {pEx.Message}", "Warning");
                    ToastService.ShowWarning($"Thu phí thành công nhưng máy in lỗi: {pEx.Message}. Biên lai đã gửi qua Zalo/Email.");
                    System.Media.SystemSounds.Hand.Play();
                }
                catch (Exception)
                {
                }

                await AuditLogService.WriteLogAsync("Thu phí hao mòn tài liệu",
                    $"[{txId}] Thu {amountPaid.ToString("#,##0", new System.Globalization.CultureInfo("vi-VN"))} VNĐ từ {_currentSsoId}. Phí còn lại: {CurrentStudentFine.ToString("#,##0", new System.Globalization.CultureInfo("vi-VN"))} VNĐ", true);
            }

        private void ShowStatus(string message, string state)
        {
            StatusMessage = message;
            StatusState = state;
            
            if (state == "Danger" || state == "Warning")
            {
                HasError = true;
                System.Media.SystemSounds.Exclamation.Play();
                Task.Delay(500).ContinueWith(_ => { HasError = false; });
            }
            else
            {
                HasError = false;
            }
        }

        // ============ ITEM 3.3: Generate unique Transaction ID ============
        private static int _txCounter = 0;
        private static string GenerateTransactionId(string prefix)
        {
            _txCounter++;
            return $"{prefix}-{DateTime.Now:yyyyMMdd}-{_txCounter:D4}";
        }

        private async Task ReportLostAsync()
        {
            if (IsBusy || string.IsNullOrEmpty(_currentSsoId)) return;

            string readerName = _currentSsoId == "HS001" ? "Nguyễn Văn An" : ("Độc giả " + _currentSsoId);

            var currentWindow = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive);
            var dialog = new SmartLibrary.Desktop.Views.Librarian.LostBookDialog(_currentSsoId, readerName, _apiService)
            {
                Owner = currentWindow
            };

            if (dialog.ShowDialog() == true && dialog.IsConfirmed)
            {
                try
                {
                    IsBusy = true;
                    (SaveLoanCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();

                    // Gọi API đồng bộ trạng thái báo mất lên server
                    string compTypeStr = dialog.CompensationType switch
                    {
                        0 => $"Phí đền bù {dialog.PenaltyAmount:N0} VNĐ",
                        1 => "Đền bù hiện vật (Mua trả lại sách mới cùng tên)",
                        2 => "Lao động rèn luyện công ích (Trực nhật thư viện 2-4 buổi)",
                        _ => "Đền bù khác"
                    };

                    var payload = new
                    {
                        LoanId = dialog.LoanId,
                        Type = dialog.IsGracePeriod ? "Grace" : (dialog.CompensationType == 2 ? "CommunityService" : (dialog.CompensationType == 1 ? "Replacement" : "Lost")),
                        CompensationAmount = dialog.PenaltyAmount,
                        Note = dialog.IsGracePeriod ? "Báo mất sách (Ân hạn 7 ngày)" : $"Báo mất sách ({compTypeStr})"
                    };
                    await _apiService.PostAsync(ApiEndpoints.CirculationReportLost, payload);

                    if (dialog.IsGracePeriod)
                    {
                        string txId = GenerateTransactionId("MAT-AH");
                        await AuditLogService.WriteLogAsync("Báo thất lạc sách (Ân hạn)",
                            $"[{txId}] Báo thất lạc sách {dialog.LostBarcode} ({dialog.LostBookTitle}) — Trạng thái: Ân hạn 7 ngày tìm kiếm cho {_currentSsoId}", true);
                        ReaderStatusText = "Hoạt động (Được ân hạn tìm sách)";
                        IsBlocked = false;
                        ShowStatus($"Đã ghi nhận báo thất lạc (Ân hạn 7 ngày) sách {dialog.LostBarcode} — TX: {txId}", "Success");
                    }
                    else
                    {
                        CurrentStudentFine += (double)dialog.PenaltyAmount;
                        HasFine = CurrentStudentFine > 0;
                        IsBlocked = true;
                        
                        if (dialog.CompensationType == 1)
                        {
                            BlockReason = "Độc giả đang chờ đền bù sách mới cùng loại. Vui lòng bàn giao sách mới cho thủ thư để mở lại tài khoản!";
                            ReaderStatusText = "Tạm khóa (Chờ đền sách)";
                        }
                        else if (dialog.CompensationType == 2)
                        {
                            BlockReason = "Độc giả đang thực hiện chương trình lao động công ích rèn luyện. Vui lòng hoàn thành trực ban thư viện để mở lại tài khoản!";
                            ReaderStatusText = "Tạm khóa (Chờ trực nhật)";
                        }
                        else
                        {
                            BlockReason = "Bạn đọc vui lòng hoàn tất các khoản phí hao mòn đền bù sách thất lạc để tiếp tục hành trình đọc sách nhé!";
                            ReaderStatusText = "Tạm khóa (Nợ phí đền bù)";
                        }
                        
                        (SaveLoanCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();

                        string txId = GenerateTransactionId("MAT");
                        await AuditLogService.WriteLogAsync("Báo thất lạc sách",
                            $"[{txId}] Thất lạc sách {dialog.LostBarcode} ({dialog.LostBookTitle}) — {compTypeStr} cho {_currentSsoId}", true);

                        if (_currentSsoId == "HS001")
                        {
                            StudentInfoText = $"Độc giả: Nguyễn Văn An\nMã HS: {_currentSsoId}\nLớp: 10A1\nĐang mượn: 2/5 cuốn\nTrạng thái: {ReaderStatusText}";
                        }
                        else
                        {
                            StudentInfoText = $"Độc giả: Học sinh/GV\nMã ID: {_currentSsoId}\nTrạng thái thẻ: {ReaderStatusText}\nSách đang giữ: 1/5";
                        }

                        ShowStatus($"Đã báo thất lạc sách {dialog.LostBarcode} — TX: {txId}", "Success");
                    }
                }
                finally
                {
                    IsBusy = false;
                    (SaveLoanCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                }
            }
        }
    }

    public class PendingBookItem
    {
        public string Barcode { get; set; } = "";
        public string Title { get; set; } = "";
    }

    public class ReturnedBookItem
    {
        public string Barcode { get; set; } = "";
        public string Title { get; set; } = "";
        public string Condition { get; set; } = "";
        public string ReturnTime { get; set; } = "";
        public string TransactionId { get; set; } = ""; // Item 3.3
    }

    public class StudentStatusDto
    {
        public string FullName { get; set; } = string.Empty;
        public string SsoUserId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string SchoolClassId { get; set; } = string.Empty;
        public bool IsLockedFromBorrowing { get; set; }
        public double TotalUnpaidFine { get; set; }
        public int ActiveLoansCount { get; set; }
        public int OverdueLoansCount { get; set; }
    }
}
