using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class SmartBookDropViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _statusMessage = "";

        [ObservableProperty]
        private string _zaloNotificationText = "";

        [ObservableProperty]
        private bool _showZaloNotification;

        public ObservableCollection<LoanItemDto> BorrowedBooks { get; } = new();

        public ICommand LoadBorrowedBooksCommand { get; }
        public ICommand DropBookCommand { get; }

        public SmartBookDropViewModel(ApiService apiService)
        {
            _apiService = apiService;
            LoadBorrowedBooksCommand = new AsyncRelayCommand(LoadBorrowedBooksAsync);
            DropBookCommand = new AsyncRelayCommand<LoanItemDto>(DropBookAsync);

            _ = LoadBorrowedBooksAsync();
        }

        public async Task LoadBorrowedBooksAsync()
        {
            IsBusy = true;
            StatusMessage = "Đang tải danh sách sách bạn đang mượn...";

            var ssoId = AuthService.CurrentUserSsoId ?? "HS001";
            try
            {
                // Gọi API lấy lịch sử / trạng thái mượn sách
                var history = await _apiService.GetAsync<StudentHistoryResponseDto>($"/Circulation/student-history/{ssoId}");
                if (history != null && history.Loans != null)
                {
                    BorrowedBooks.Clear();
                    foreach (var loan in history.Loans)
                    {
                        if (loan.ReturnedDate == null || loan.Status == "Active" || loan.Status == "Overdue")
                        {
                            BorrowedBooks.Add(new LoanItemDto
                            {
                                Barcode = loan.Barcode,
                                BookTitle = loan.BookTitle,
                                BorrowedDate = loan.BorrowedDate,
                                DueDate = loan.DueDate
                            });
                        }
                    }
                }
                else
                {
                    LoadMockData();
                }
            }
            catch (Exception)
            {
                LoadMockData();
            }

            IsBusy = false;
            StatusMessage = BorrowedBooks.Count > 0 ? $"Bạn đang mượn {BorrowedBooks.Count} cuốn sách." : "Bạn không mượn cuốn sách nào.";
        }

        private void LoadMockData()
        {
            BorrowedBooks.Clear();
            BorrowedBooks.Add(new LoanItemDto
            {
                Barcode = "9781607967552-1",
                BookTitle = "Đắc Nhân Tâm",
                BorrowedDate = DateTime.Now.AddDays(-10),
                DueDate = DateTime.Now.AddDays(4)
            });
            BorrowedBooks.Add(new LoanItemDto
            {
                Barcode = "9786042131971-1",
                BookTitle = "Tắt Đèn",
                BorrowedDate = DateTime.Now.AddDays(-15),
                DueDate = DateTime.Now.AddDays(-1) // Overdue
            });
            BorrowedBooks.Add(new LoanItemDto
            {
                Barcode = "9786042131988-2",
                BookTitle = "Số Đỏ",
                BorrowedDate = DateTime.Now.AddDays(-5),
                DueDate = DateTime.Now.AddDays(9)
            });
        }

        private async Task DropBookAsync(LoanItemDto? loan)
        {
            if (loan == null) return;

            IsBusy = true;
            StatusMessage = $"Đang quét mã UHF RFID của sách '{loan.BookTitle}'...";

            try
            {
                // Gọi API trả sách
                await _apiService.PostAsync("/Circulation/return", new { BookBarcode = loan.Barcode });
                
                // Trả thành công trên database
                TriggerSuccessfulReturn(loan);
            }
            catch (Exception)
            {
                // Giả lập trả thành công ngoại tuyến
                TriggerSuccessfulReturn(loan);
            }

            IsBusy = false;
        }

        private void TriggerSuccessfulReturn(LoanItemDto loan)
        {
            // Xóa sách khỏi danh sách mượn
            BorrowedBooks.Remove(loan);

            // Âm thanh đóng nắp thùng / tiếng bip báo thành công
            try
            {
                System.Media.SystemSounds.Exclamation.Play();
            }
            catch { }

            // Hiển thị thông báo Zalo
            ZaloNotificationText = $"[Zalo] SmartLibrary 4.0: Cảm ơn bạn đã trả cuốn sách '{loan.BookTitle}' thành công tại Hộp trả sách thông minh ngoài sảnh vào lúc {DateTime.Now:HH:mm:ss dd/MM/yyyy}.";
            ShowZaloNotification = true;

            // Tự động tắt thông báo sau 8 giây
            Task.Run(async () =>
            {
                await Task.Delay(8000);
                ShowZaloNotification = false;
            });
        }
    }

    public class LoanItemDto
    {
        public string Barcode { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public DateTime BorrowedDate { get; set; }
        public DateTime DueDate { get; set; }

        public string FormattedBorrowedDate => BorrowedDate.ToString("dd/MM/yyyy");
        public string FormattedDueDate => DueDate.ToString("dd/MM/yyyy");
        public bool IsOverdue => DueDate < DateTime.Now;
        public string DueColor => IsOverdue ? "#EF4444" : "#10B981";
        public string DueText => IsOverdue ? "ĐÃ QUÁ HẠN ⚠️" : "Còn hạn";
    }

    public class StudentHistoryResponseDto
    {
        public string StudentName { get; set; } = string.Empty;
        public int TotalBorrowed { get; set; }
        public int TotalReturned { get; set; }
        public int TotalOverdue { get; set; }
        public List<StudentHistoryLoanDto> Loans { get; set; } = new();
    }

    public class StudentHistoryLoanDto
    {
        public string BookTitle { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public DateTime BorrowedDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnedDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
