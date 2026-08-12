using System;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;

namespace QASmartClass.Staff.ViewModels
{
    public partial class CanteenPosViewModel : ObservableObject, IDisposable
    {
        public event EventHandler? PaymentCompleted;
        public event Action? FocusRequested;

        private System.Threading.CancellationTokenSource? _cts;

        [ObservableProperty]
        private string _searchCode = string.Empty;

        [ObservableProperty]
        private Student? _currentStudent;

        [ObservableProperty]
        private decimal _currentBalance = 0; 

        [ObservableProperty]
        private decimal _deductionAmount;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _allergyWarning = string.Empty;

        [ObservableProperty]
        private bool _isAllergyOverlayOpen;

        [ObservableProperty]
        private bool _isAllergyConfirmedByStaff;

        [ObservableProperty]
        private string _studentAvatarImageSource = string.Empty;

        [ObservableProperty]
        private bool _hasNoAvatar = true;

        [ObservableProperty]
        private ObservableCollection<CanteenTransactionDisplay> _recentTransactions = new();

        [ObservableProperty]
        private string _orderDetails = "Suất ăn Canteen";

        [ObservableProperty]
        private bool _autoPrintReceipt = true;

        [ObservableProperty]
        private bool _isQuickPayMode;

        [ObservableProperty]
        private decimal _quickPayAmount = 30000;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private bool _isReaderConnected = true;

        [ObservableProperty]
        private bool _isHardwareConnected = true;

        [ObservableProperty]
        private EventLog? _lastLog;

        [ObservableProperty]
        private bool _isLowBalanceWarningVisible;

        [ObservableProperty]
        private string _lowBalanceWarningMessage = string.Empty;

        public CanteenPosViewModel()
        {
        }

        public async Task InitializeAsync()
        {
            IsLoading = true;
            try
            {
                await LoadRecentAsync();

                // Start background connection monitoring loop
                _cts = new System.Threading.CancellationTokenSource();
                _ = CheckReaderConnectionAsync(_cts.Token);
                _ = StartConnectionMonitorAsync(_cts.Token);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CheckReaderConnectionAsync(System.Threading.CancellationToken token)
        {
            if (token.IsCancellationRequested) return;

            // Support Keyboard emulation mode / None config to bypass COM port checks
            try
            {
                var config = AppConfig.Load();
                if (string.Equals(config.CanteenComPort?.Trim(), "KEYBOARD", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(config.CanteenComPort?.Trim(), "NONE", StringComparison.OrdinalIgnoreCase))
                {
                    IsReaderConnected = true;
                    IsHardwareConnected = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Failed to check CanteenComPort bypass config: {Msg}", ex.Message);
            }

            // Check actual COM ports with handshake in a background thread
            bool hasComPorts = await Task.Run(() =>
            {
                try
                {
                    if (token.IsCancellationRequested) return false;
                    var config = AppConfig.Load();
                    string[] ports;
                    if (!string.IsNullOrWhiteSpace(config.CanteenComPort))
                    {
                        ports = new[] { config.CanteenComPort.Trim() };
                    }
                    else
                    {
                        ports = System.IO.Ports.SerialPort.GetPortNames();
                    }

                    foreach (var port in ports)
                    {
                        if (token.IsCancellationRequested) return false;
                        try
                        {
                            using (var sp = new System.IO.Ports.SerialPort(port, 9600))
                            {
                                sp.ReadTimeout = 100;
                                sp.WriteTimeout = 100;
                                sp.Open();
                                if (token.IsCancellationRequested) return false;
                                sp.Write(new byte[] { 0x02 }, 0, 1);
                                int b = sp.ReadByte();
                                if (b != -1)
                                {
                                    return true;
                                }
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Port is busy, meaning the RFID reader is connected and opened by the main thread
                            return true;
                        }
                        catch
                        {
                            // Ignore other exceptions for incompatible/unresponsive ports
                        }
                    }
                }
                catch
                {
                    if (token.IsCancellationRequested) return false;
                    // Fallback to registry if serial port enumeration fails
                    try
                    {
                        using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM"))
                        {
                            if (key != null)
                            {
                                return key.GetValueNames().Length > 0;
                            }
                        }
                    }
                    catch { }
                }
                return false;
            }, token);

            if (token.IsCancellationRequested) return;

            IsReaderConnected = hasComPorts;
            IsHardwareConnected = hasComPorts;
        }

        private async Task StartConnectionMonitorAsync(System.Threading.CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(3000, token);
                    await CheckReaderConnectionAsync(token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning("RFID reader connection monitor error: {Msg}", ex.Message);
                }
            }
        }

        public void Dispose()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        private void PlaySecurityAlert()
        {
            AppServices.UIService.PlaySound("SecurityAlert");
            System.Media.SystemSounds.Exclamation.Play();
            // Phát 3 tiếng beep dồn dập tần số cao chạy ngầm để không khóa luồng UI
            Task.Run(() =>
            {
                for (int i = 0; i < 3; i++)
                {
                    Console.Beep(1600, 180);
                    System.Threading.Thread.Sleep(80);
                }
            });
        }


        [RelayCommand]
        private async Task SearchStudentAsync()
        {
            IsLowBalanceWarningVisible = false;
            IsAllergyOverlayOpen = false; // Reset overlay state on new search
            IsAllergyConfirmedByStaff = false; // Reset staff confirmation on new search
            try
            {
                if (string.IsNullOrWhiteSpace(SearchCode))
                {
                    CurrentStudent = null;
                    CurrentBalance = 0;
                    AllergyWarning = string.Empty;
                    StatusMessage = "Vui lòng nhập mã học sinh.";
                    return;
                }

                try
                {
                    using var db = new AppDbContext();
                    CurrentStudent = await db.Students.FirstOrDefaultAsync(s => s.StudentCode.ToUpper() == SearchCode.Trim().ToUpper());
                    
                    if (CurrentStudent != null)
                    {
                        StatusMessage = "Đã tìm thấy học sinh.";
                        CurrentBalance = CurrentStudent.WalletBalance;

                        if (!string.IsNullOrEmpty(CurrentStudent.AvatarPath) && System.IO.File.Exists(CurrentStudent.AvatarPath))
                        {
                            StudentAvatarImageSource = CurrentStudent.AvatarPath;
                            HasNoAvatar = false;
                        }
                        else
                        {
                            StudentAvatarImageSource = string.Empty;
                            HasNoAvatar = true;
                        }
                        IsAllergyConfirmedByStaff = false;

                        if (CurrentStudent.IsAtRisk)
                        {
                            StatusMessage = $"⚠️ VÍ CỦA HỌC SINH ĐÃ BỊ KHÓA DO NGHI VẤN CAN THIỆP SỐ DƯ! (Lý do: {CurrentStudent.RiskReason})";
                            PlaySecurityAlert();
                        }
                        else if (CurrentStudent.WalletBalance < CurrentStudent.LowBalanceThreshold)
                        {
                            IsLowBalanceWarningVisible = true;
                            LowBalanceWarningMessage = $"⚠️ CẢNH BÁO SỐ DƯ VÍ THẤP: Số dư hiện tại chỉ còn {CurrentStudent.WalletBalance:N0} VNĐ!";
                            AppServices.UIService.PlaySound("Warning");
                        }

                        // Query food allergy - prioritize StudentCode, fallback to StudentName for backward compatibility
                        var allergy = await db.FoodAllergies.FirstOrDefaultAsync(a => 
                            (a.StudentCode.ToLower() == CurrentStudent.StudentCode.ToLower()) || 
                            (string.IsNullOrEmpty(a.StudentCode) && a.StudentName.ToLower() == CurrentStudent.FullName.ToLower()));
                        if (allergy != null)
                        {
                            AllergyWarning = $"⚠ CẢNH BÁO DỊ ỨNG: Dị ứng {allergy.Allergen} ({allergy.Severity}). Xử lý: {allergy.ActionPlan}";
                            IsAllergyOverlayOpen = true;
                            AppServices.UIService.PlaySound("Warning");
                        }
                        else
                        {
                            AllergyWarning = string.Empty;
                        }

                        if (IsQuickPayMode)
                        {
                            if (QuickPayAmount <= 0)
                            {
                                StatusMessage = "Số tiền suất mặc định không hợp lệ.";
                                AppServices.UIService.PlaySound("Error");
                                System.Media.SystemSounds.Hand.Play();
                                return;
                            }

                            if (!string.IsNullOrEmpty(AllergyWarning))
                            {
                                StatusMessage = "Giao dịch bán nhanh tạm dừng do có cảnh báo dị ứng. Vui lòng kiểm tra và thanh toán thủ công.";
                            }
                            else
                            {
                                DeductionAmount = QuickPayAmount;
                                await ProcessPaymentAsync();
                            }
                        }
                    }
                    else
                    {
                        StatusMessage = "Không tìm thấy mã học sinh này.";
                        CurrentStudent = null;
                        CurrentBalance = 0;
                        AllergyWarning = string.Empty;
                        StudentAvatarImageSource = string.Empty;
                        HasNoAvatar = true;
                        IsAllergyConfirmedByStaff = false;
                        AppServices.UIService.PlaySound("Error");
                        System.Media.SystemSounds.Hand.Play();
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Lỗi: {ex.Message}";
                    AllergyWarning = string.Empty;
                    AppServices.UIService.PlaySound("Error");
                    System.Media.SystemSounds.Hand.Play();
                }
            }
            finally
            {
                PaymentCompleted?.Invoke(this, EventArgs.Empty);
                FocusRequested?.Invoke();
            }
        }

        [RelayCommand]
        private async Task ProcessPaymentAsync()
        {
            if (IsProcessing) return;
            IsProcessing = true;
            try
            {
                if (IsAllergyOverlayOpen)
                {
                    StatusMessage = "Vui lòng xác nhận đã kiểm tra dị ứng trước khi thanh toán.";
                    AppServices.UIService.PlaySound("Error");
                    System.Media.SystemSounds.Hand.Play();
                    return;
                }

                if (CurrentStudent == null)
                {
                    StatusMessage = "Vui lòng chọn học sinh trước.";
                    AppServices.UIService.PlaySound("Error");
                    System.Media.SystemSounds.Hand.Play();
                    return;
                }

                if (DeductionAmount <= 0)
                {
                    StatusMessage = "Số tiền không hợp lệ.";
                    AppServices.UIService.PlaySound("Error");
                    System.Media.SystemSounds.Hand.Play();
                    return;
                }

                if (DeductionAmount > 100000)
                {
                    StatusMessage = "Vượt quá hạn mức 100.000đ/giao dịch.";
                    AppServices.UIService.PlaySound("Error");
                    System.Media.SystemSounds.Hand.Play();
                    return;
                }

                // KIỂM TRA SỐ DƯ TRƯỚC KHI HIỂN THỊ XÁC NHẬN
                try
                {
                    using (var dbCheck = new AppDbContext())
                    {
                        var studentDb = await dbCheck.Students.FirstOrDefaultAsync(s => s.Id == CurrentStudent.Id);
                        if (studentDb == null)
                        {
                            StatusMessage = "Lỗi: Không tìm thấy học sinh trong cơ sở dữ liệu.";
                            AppServices.UIService.PlaySound("Error");
                            System.Media.SystemSounds.Hand.Play();
                            return;
                        }

                        if (studentDb.IsAtRisk)
                        {
                            StatusMessage = $"⚠️ VÍ CỦA HỌC SINH ĐÃ BỊ KHÓA DO NGHI VẤN CAN THIỆP SỐ DƯ! (Lý do: {studentDb.RiskReason})";
                            PlaySecurityAlert();
                            return;
                        }

                        if (DeductionAmount > studentDb.WalletBalance)
                        {
                            StatusMessage = "Số dư không đủ!";
                            AppServices.UIService.PlaySound("Error");
                            System.Media.SystemSounds.Hand.Play();
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Lỗi kiểm tra số dư: {ex.Message}";
                    AppServices.UIService.PlaySound("Error");
                    System.Media.SystemSounds.Hand.Play();
                    return;
                }

                bool isConfirm = true;
                if (!IsQuickPayMode)
                {
                    string confirmMsg = $"Xác nhận trừ tiền ví của học sinh {CurrentStudent.FullName} với số tiền {DeductionAmount:N0} đ?";
                    isConfirm = await AppServices.UIService.ShowConfirmAsync(confirmMsg, "Xác nhận thanh toán");

                    if (isConfirm && DeductionAmount > 50000)
                    {
                        isConfirm = await AppServices.UIService.ShowConfirmAsync(
                             "CẢNH BÁO: Số tiền thanh toán lớn (> 50.000đ). Bạn có chắc chắn muốn tiếp tục?",
                             "Cảnh báo hạn mức ví", true);
                    }
                }

                if (!isConfirm) return;

                try
                {
                    using var db = new AppDbContext();
                    using var transaction = await db.Database.BeginTransactionAsync();
                    try
                    {
                        var studentDb = await db.Students.FirstOrDefaultAsync(s => s.Id == CurrentStudent.Id);
                        if (studentDb == null)
                        {
                            StatusMessage = "Lỗi: Không tìm thấy học sinh trong cơ sở dữ liệu.";
                            AppServices.UIService.PlaySound("Error");
                            System.Media.SystemSounds.Hand.Play();
                            return;
                        }

                        if (studentDb.Status != "Active")
                        {
                            StatusMessage = "Vui lòng kiểm tra lại thông tin thẻ tại Văn phòng hỗ trợ";
                            AppServices.UIService.PlaySound("Error");
                            System.Media.SystemSounds.Hand.Play();
                            return;
                        }

                        if (studentDb.IsAtRisk)
                        {
                            StatusMessage = $"⚠️ VÍ CỦA HỌC SINH ĐÃ BỊ KHÓA DO NGHI VẤN CAN THIỆP SỐ DƯ! (Lý do: {studentDb.RiskReason})";
                            PlaySecurityAlert();
                            return;
                        }

                        if (DeductionAmount > studentDb.WalletBalance)
                        {
                            StatusMessage = "Số dư không đủ!";
                            AppServices.UIService.PlaySound("Error");
                            System.Media.SystemSounds.Hand.Play();
                            return;
                        }

                        // Trừ tiền trong Database
                        studentDb.WalletBalance -= DeductionAmount;

                        string itemsStr = string.IsNullOrWhiteSpace(OrderDetails) ? "Suất ăn Canteen" : OrderDetails.Trim();
                        var detailsObj = new { 
                            Type = "Canteen_Deduction", 
                            Amount = DeductionAmount, 
                            BalanceAfter = studentDb.WalletBalance,
                            Cashier = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "Canteen_POS",
                            Items = itemsStr
                        };
                        var log = new EventLog
                        {
                            EventType = "WalletTransaction",
                            Actor = studentDb.StudentCode,
                            Timestamp = DateTime.Now,
                            Details = System.Text.Json.JsonSerializer.Serialize(detailsObj)
                        };

                        db.EventLogs.Add(log);
                        await db.SaveChangesAsync();
                        await transaction.CommitAsync();

                        LastLog = log; // Save last log

                        decimal originalDeduction = DeductionAmount;
                        var studentId = studentDb.Id;
                        var studentFullName = studentDb.FullName;
                        var walletBalance = studentDb.WalletBalance;
                        var lowBalanceThreshold = studentDb.LowBalanceThreshold;

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using var dbNotify = new AppDbContext();
                                var mobileApi = new QASmartClass.Services.MobileApiService(dbNotify);
                                await mobileApi.SendPushNotificationAsync(
                                    studentId, 
                                    "Parent", 
                                    "Biến động số dư ví Canteen", 
                                    $"Ví Canteen của học sinh {studentFullName} vừa được thanh toán {originalDeduction:N0} đ. Chi tiết: {itemsStr}. Số dư còn lại: {walletBalance:N0} đ", 
                                    "General");

                                if (walletBalance < lowBalanceThreshold)
                                {
                                    await mobileApi.SendPushNotificationAsync(
                                        studentId,
                                        "Parent",
                                        "Cảnh báo số dư ví thấp",
                                        $"Số dư ví Canteen của học sinh {studentFullName} hiện tại còn dưới ngưỡng an toàn: {walletBalance:N0} đ. Quý phụ huynh vui lòng nạp thêm tiền để đảm bảo suất ăn cho con.",
                                        "General");
                                }
                            }
                            catch (Exception ex)
                            {
                                Serilog.Log.Error(ex, "[CanteenPOS] Send notification to parent failed");
                            }
                        });

                        if (studentDb.WalletBalance < studentDb.LowBalanceThreshold)
                        {
                            IsLowBalanceWarningVisible = true;
                            LowBalanceWarningMessage = $"⚠️ CẢNH BÁO SỐ DƯ VÍ THẤP: Số dư hiện tại chỉ còn {studentDb.WalletBalance:N0} VNĐ!";
                            AppServices.UIService.PlaySound("Warning");
                        }

                        // Cập nhật lại UI sau khi DB đã lưu thành công
                        CurrentBalance = studentDb.WalletBalance;
                        CurrentStudent = studentDb;
                        RecentTransactions.Insert(0, MapToDisplay(log));
                        StatusMessage = $"Đã thanh toán thành công {DeductionAmount:N0} đ";
                        DeductionAmount = 0;

                        AppServices.UIService.PlaySound("Success");
                        System.Media.SystemSounds.Asterisk.Play();

                        // Trigger virtual printing
                        if (AutoPrintReceipt)
                        {
                            PrintReceipt(log);
                        }
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        AppServices.UIService.PlaySound("Error");
                        System.Media.SystemSounds.Hand.Play();
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Lỗi thanh toán: {ex.Message}";
                    AppServices.UIService.PlaySound("Error");
                    System.Media.SystemSounds.Hand.Play();
                }
            }
            finally
            {
                IsProcessing = false;
                PaymentCompleted?.Invoke(this, EventArgs.Empty);
                FocusRequested?.Invoke();
            }
        }
        [RelayCommand]
        private async Task ConfirmAllergyAsync()
        {
            if (!IsAllergyConfirmedByStaff) return;
            if (CurrentStudent == null) return;

            try
            {
                using var db = new AppDbContext();
                string cashier = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "Canteen_POS";
                var detailsObj = new
                {
                    Action = "Allergy_Warning_Bypassed",
                    StudentName = CurrentStudent.FullName,
                    AllergyInfo = AllergyWarning,
                    Cashier = cashier
                };

                var log = new EventLog
                {
                    EventType = "AllergyWarningBypassed",
                    Actor = CurrentStudent.StudentCode,
                    Timestamp = DateTime.Now,
                    Details = System.Text.Json.JsonSerializer.Serialize(detailsObj)
                };

                db.EventLogs.Add(log);
                await db.SaveChangesAsync();

                IsAllergyOverlayOpen = false;
                StatusMessage = "Cảnh báo dị ứng đã được xác nhận. Mở khóa thanh toán.";
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[CanteenPOS] Failed to log allergy bypass event");
                StatusMessage = "Lỗi kết nối cơ sở dữ liệu. Không thể bỏ qua cảnh báo dị ứng.";
                AppServices.UIService.PlaySound("Error");
            }
        }

        [RelayCommand]
        private void ReprintReceipt()
        {
            if (LastLog != null)
            {
                PrintReceipt(LastLog);
            }
            else
            {
                StatusMessage = "Không tìm thấy giao dịch gần đây để in lại.";
            }
        }

        [RelayCommand]
        public void PrintReceipt(EventLog log)
        {
            if (log == null) return;
            
            try
            {
                string items = "Suất ăn Canteen";
                decimal amount = 0;
                decimal balAfter = 0;
                string cashier = "Canteen_POS";
                string studentName = "Không rõ";
                string studentClass = "Không rõ";

                using (var db = new AppDbContext())
                {
                    var targetStudent = db.Students.FirstOrDefault(s => s.StudentCode == log.Actor);
                    if (targetStudent != null)
                    {
                        studentName = targetStudent.FullName;
                        studentClass = targetStudent.ClassName;
                    }
                }

                using (var doc = System.Text.Json.JsonDocument.Parse(log.Details))
                {
                    var root = doc.RootElement;
                    amount = (decimal)root.GetProperty("Amount").GetDouble();
                    balAfter = (decimal)root.GetProperty("BalanceAfter").GetDouble();
                    if (root.TryGetProperty("Items", out var it)) items = it.GetString() ?? "Suất ăn Canteen";
                    if (root.TryGetProperty("Cashier", out var cs)) cashier = cs.GetString() ?? "Canteen_POS";
                }

                var config = AppConfig.Load();
                string schoolNameUpper = (config.SchoolName ?? "TRƯỜNG QA SMARTSCHOOL").ToUpper();
                int padWidth = 32;
                string centeredSchool = schoolNameUpper.Length < padWidth 
                    ? schoolNameUpper.PadLeft((padWidth + schoolNameUpper.Length) / 2) 
                    : schoolNameUpper;

                var receiptText = new System.Text.StringBuilder()
                    .AppendLine("================================")
                    .AppendLine(centeredSchool)
                    .AppendLine("        BIÊN LAI CANTEEN")
                    .AppendLine("================================")
                    .AppendLine($"Thời gian: {log.Timestamp:dd/MM/yyyy HH:mm:ss}")
                    .AppendLine($"Thu ngân: {cashier}")
                    .AppendLine($"Học sinh: {studentName}")
                    .AppendLine($"Lớp: {studentClass}")
                    .AppendLine("--------------------------------")
                    .AppendLine($"Chi tiết: {items}")
                    .AppendLine($"Số tiền trừ: -{amount:N0} VNĐ")
                    .AppendLine("--------------------------------")
                    .AppendLine($"SỐ DƯ MỚI: {balAfter:N0} VNĐ")
                    .AppendLine("================================")
                    .AppendLine("   Chúc các em ngon miệng!")
                    .AppendLine("         & Học tốt!")
                    .AppendLine("================================")
                    .ToString();

                AppServices.PrintService.PrintReceipt(receiptText);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[CanteenPOS] Print receipt failed");
            }
        }

        [RelayCommand]
        private void AddAmount(string amountStr)
        {
            if (decimal.TryParse(amountStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal val))
            {
                DeductionAmount += val;
            }
        }

        [RelayCommand]
        private void ClearAmount()
        {
            DeductionAmount = 0;
        }

        private CanteenTransactionDisplay MapToDisplay(EventLog log)
        {
            string display = "Thanh toán Canteen";
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(log.Details);
                var root = doc.RootElement;
                double amount = root.GetProperty("Amount").GetDouble();
                double balAfter = root.GetProperty("BalanceAfter").GetDouble();
                string items = root.TryGetProperty("Items", out var it) ? it.GetString() ?? "" : "";
                string itemsSuffix = string.IsNullOrEmpty(items) ? "" : $" ({items})";
                display = $"Trừ tiền Canteen: -{amount:N0} đ{itemsSuffix} (Số dư: {balAfter:N0} đ)";
            }
            catch
            {
                display = log.Details;
            }
            return new CanteenTransactionDisplay
            {
                Timestamp = log.Timestamp,
                StudentCode = log.Actor,
                DisplayDetails = display
            };
        }

        private async Task LoadRecentAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var today = DateTime.Today;
                var logs = await db.EventLogs
                    .Where(l => l.EventType == "WalletTransaction" && l.Timestamp >= today)
                    .OrderByDescending(l => l.Timestamp)
                    .Take(10)
                    .ToListAsync();
                var list = logs.Select(MapToDisplay).ToList();
                RecentTransactions = new ObservableCollection<CanteenTransactionDisplay>(list);
            }
            catch { }
        }

        partial void OnDeductionAmountChanged(decimal value)
        {
            if (value < 0)
            {
                _deductionAmount = 0;
                StatusMessage = "Số tiền thanh toán không được âm.";
            }
            else if (value > 100000)
            {
                _deductionAmount = 100000;
                StatusMessage = "Vượt quá hạn mức 100.000đ/giao dịch (hạn mức tối đa).";
            }
            OnPropertyChanged(nameof(DeductionAmount));
            OnPropertyChanged(nameof(DeductionAmountString));
        }

        public string DeductionAmountString
        {
            get => DeductionAmount == 0 ? string.Empty : DeductionAmount.ToString("0");
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    DeductionAmount = 0;
                }
                else if (decimal.TryParse(value, out decimal result))
                {
                    DeductionAmount = result;
                }
                OnPropertyChanged(nameof(DeductionAmountString));
            }
        }

        partial void OnQuickPayAmountChanged(decimal value)
        {
            if (value < 0)
            {
                QuickPayAmount = 0;
                StatusMessage = "Số tiền thanh toán nhanh không được âm.";
            }
            else if (value > 100000)
            {
                QuickPayAmount = 100000;
                StatusMessage = "Hạn mức thanh toán nhanh tối đa là 100.000đ.";
            }
        }
    }

    public class CanteenTransactionDisplay
    {
        public DateTime Timestamp { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string DisplayDetails { get; set; } = string.Empty;
    }
}
