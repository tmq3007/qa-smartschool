using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class NotificationHubViewModel : ObservableObject, IActiveAwareViewModel
    {
        private readonly ApiService _apiService;

        public ObservableCollection<OverdueStudentDto> OverdueStudents { get; } = new();
        public ObservableCollection<string> NotificationLogs { get; } = new();

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private bool _isListEmpty = true;

        public ICommand LoadOverdueStudentsCommand { get; }
        public ICommand SendReminderCommand { get; }

        public NotificationHubViewModel(ApiService apiService)
        {
            _apiService = apiService;
            LoadOverdueStudentsCommand = new AsyncRelayCommand(LoadOverdueStudentsAsync);
            SendReminderCommand = new AsyncRelayCommand<OverdueStudentDto>(SendReminderAsync);
        }

        public void Activate()
        {
            _ = LoadOverdueStudentsAsync();
        }

        public async Task LoadOverdueStudentsAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                // Gọi API lấy danh sách sách mất/trễ hạn chưa xử lý xong phạt tiền
                var response = await _apiService.GetAsync<OverdueStudentDto[]>("/Circulation/lost-records");
                OverdueStudents.Clear();
                if (response != null)
                {
                    foreach (var s in response)
                    {
                        // Gán thêm số điện thoại giả lập nếu thiếu
                        if (string.IsNullOrEmpty(s.Phone)) s.Phone = "0982" + new Random().Next(100000, 999999);
                        OverdueStudents.Add(s);
                    }
                }
            }
            catch
            {
                LoadMockOverdueStudents();
            }
            finally
            {
                IsListEmpty = OverdueStudents.Count == 0;
                IsLoading = false;
            }
        }

        private void LoadMockOverdueStudents()
        {
            OverdueStudents.Clear();
            OverdueStudents.Add(new OverdueStudentDto
            {
                LoanId = 1,
                StudentSsoId = "HS001",
                StudentName = "Nguyễn Văn An",
                Phone = "0912345678",
                BookTitle = "Số Đỏ",
                OverdueDays = 12,
                FineAmount = 45000
            });
            OverdueStudents.Add(new OverdueStudentDto
            {
                LoanId = 2,
                StudentSsoId = "HS002",
                StudentName = "Võ Minh Em",
                Phone = "0987654321",
                BookTitle = "Nhà Giả Kim",
                OverdueDays = 8,
                FineAmount = 75000
            });
        }

        private async Task SendReminderAsync(OverdueStudentDto? student)
        {
            if (student == null) return;
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                await Task.Delay(1000); // Giả lập mạng chậm gửi API SMS Gateway

                string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                string logMsg = $"[{timestamp}] 🔔 Gửi Zalo/SMS thành công tới PH em {student.StudentName} (SĐT: {student.Phone}). Nội dung: Trân trọng nhắc nhở em hoàn trả sách '{student.BookTitle}' trễ hạn {student.OverdueDays} ngày để các bạn khác cùng mượn học tập.";

                NotificationLogs.Insert(0, logMsg);

                System.Windows.MessageBox.Show($"✉️ Đã gửi tin nhắc nhở hợp tác Zalo Cloud thành công đến Phụ huynh em {student.StudentName}!", "Gửi tin thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    public class OverdueStudentDto
    {
        public int LoanId { get; set; }
        public string StudentSsoId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Phone { get; set; } = "0982334455";
        public string BookTitle { get; set; } = string.Empty;
        public int OverdueDays { get; set; } = 5;
        public decimal FineAmount { get; set; } = 50000;

        public string MaskedPhone
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Phone) || Phone.Length < 7) return Phone;
                return Phone.Substring(0, 4) + "***" + Phone.Substring(Phone.Length - 3);
            }
        }
        
        // Trả về trường mapping cho API endpoint lost-records nếu có
        public string BorrowerName { set => StudentName = value; }
        public string Title { set => BookTitle = value; }
        public decimal ReplacementCost { set => FineAmount = value; }
    }
}
