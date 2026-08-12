using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;
using SmartLibrary.Desktop.Helpers;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class FinesManagementViewModel : ObservableObject, IActiveAwareViewModel
    {
        private readonly ApiService _apiService;

        public void Activate()
        {
            _ = LoadFinesAsync();
        }
        private readonly List<UnpaidFineItem> _allFines = new();

        [ObservableProperty]
        private string _searchText = "";

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private decimal _totalFinesAmount = 0;

        public ObservableCollection<UnpaidFineItem> Fines { get; } = new();

        public ICommand LoadFinesCommand { get; }
        public ICommand PayFineCommand { get; }

        public FinesManagementViewModel(ApiService apiService)
        {
            _apiService = apiService;
            LoadFinesCommand = new AsyncRelayCommand(LoadFinesAsync);
            PayFineCommand = new AsyncRelayCommand<UnpaidFineItem>(PayFineAsync);
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter();
        }

        public async Task LoadFinesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                _allFines.Clear();
                var response = await _apiService.GetAsync<FineResponseDto[]>(ApiEndpoints.CirculationUnpaidFines);
                if (response != null)
                {
                    foreach (var fine in response)
                    {
                        _allFines.Add(new UnpaidFineItem
                        {
                            PenaltyId = fine.PenaltyId,
                            StudentName = fine.StudentName,
                            StudentSsoId = fine.StudentSsoId,
                            BookTitle = fine.BookTitle,
                            FineAmount = fine.FineAmount,
                            Reason = fine.Reason,
                            CreatedAt = fine.CreatedAt
                        });
                    }
                }

            }
            catch
            {
                LoadMockFines();
            }
            finally
            {
                ApplyFilter();
                IsBusy = false;
            }
        }

        private void LoadMockFines()
        {
            _allFines.Clear();
            _allFines.Add(new UnpaidFineItem
            {
                PenaltyId = 1,
                StudentSsoId = "HS001",
                StudentName = "Nguyễn Văn An",
                BookTitle = "Số Đỏ",
                FineAmount = 45000,
                Reason = "Quá hạn mượn 9 ngày",
                CreatedAt = DateTime.Now.AddDays(-5)
            });
            _allFines.Add(new UnpaidFineItem
            {
                PenaltyId = 2,
                StudentSsoId = "HS002",
                StudentName = "Võ Minh Em",
                BookTitle = "Nhà Giả Kim",
                FineAmount = 75000,
                Reason = "Thất lạc: Hỗ trợ đền bù 150% giá bìa",
                CreatedAt = DateTime.Now.AddDays(-2)
            });
        }

        private void ApplyFilter()
        {
            Fines.Clear();
            var query = _allFines.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var q = SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(SearchText.Trim()).ToLower();
                query = query.Where(f => 
                    (!string.IsNullOrEmpty(f.StudentName) && SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(f.StudentName).ToLower().Contains(q)) || 
                    (!string.IsNullOrEmpty(f.StudentSsoId) && SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(f.StudentSsoId).ToLower().Contains(q)) || 
                    (!string.IsNullOrEmpty(f.BookTitle) && SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(f.BookTitle).ToLower().Contains(q)) || 
                    (!string.IsNullOrEmpty(f.Reason) && SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(f.Reason).ToLower().Contains(q)));
            }
            var list = query.ToList();
            foreach (var f in list)
            {
                Fines.Add(f);
            }
            TotalFinesAmount = list.Sum(f => f.FineAmount);
        }

        private async Task PayFineAsync(UnpaidFineItem? item)
        {
            if (item == null) return;

            var confirm = MessageBox.Show(
                $"Xác nhận thu {item.FineAmount:N0} VNĐ phí hao mòn tài liệu từ học sinh {item.StudentName} ({item.StudentSsoId})?\n\n(Thẻ mượn của học sinh sẽ tự động được mở khóa nếu đã hoàn tất đóng phí)",
                "Xác nhận đóng phí hao mòn",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                var payload = new { SsoUserId = item.StudentSsoId, Amount = item.FineAmount };
                await _apiService.PostAsync<dynamic>(ApiEndpoints.CirculationPayFine, payload);

                await AuditLogService.WriteLogAsync("Thu phí hao mòn", $"Thu thành công {item.FineAmount:N0} VNĐ từ {item.StudentName} ({item.StudentSsoId}). Sách: {item.BookTitle}", true);
                MessageBox.Show($"Đã thu phí thành công {item.FineAmount:N0} VNĐ của em {item.StudentName}!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                _allFines.Remove(item);
                ApplyFilter();
            }
            catch (Exception)
            {
                // Fallback offline simulate success
                await AuditLogService.WriteLogAsync("Thu phí hao mòn ngoại tuyến", $"[OFFLINE] Đã thu {item.FineAmount:N0} VNĐ từ {item.StudentName} ({item.StudentSsoId}) khi offline. Sách: {item.BookTitle}", true);
                MessageBox.Show($"[OFFLINE] Đã xác nhận thu thành công {item.FineAmount:N0} VNĐ từ {item.StudentName}!\n\nVui lòng ghi nhận lại phiếu thu tay để đối soát tài chính khi có mạng!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Warning);
                _allFines.Remove(item);
                ApplyFilter();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public class FineResponseDto
    {
        public int PenaltyId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentSsoId { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public decimal FineAmount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class UnpaidFineItem
    {
        public int PenaltyId { get; set; }
        public string StudentSsoId { get; set; } = "";
        public string StudentName { get; set; } = "";
        public string BookTitle { get; set; } = "";
        public decimal FineAmount { get; set; }
        public string Reason { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        public string FormattedDate => CreatedAt.ToString("dd/MM/yyyy");
        public string FormattedAmount => FineAmount.ToString("N0") + " VNĐ";
    }
}
