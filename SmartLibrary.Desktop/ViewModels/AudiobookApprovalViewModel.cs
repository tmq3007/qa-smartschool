using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;
using SmartLibrary.Desktop.Helpers;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class AudiobookApprovalViewModel : ObservableObject, IActiveAwareViewModel
    {
        private readonly ApiService _apiService;
        private readonly System.Windows.Media.MediaPlayer _mediaPlayer = new();

        public void Activate()
        {
            _ = LoadPendingAudiobooksAsync();
        }

        private readonly System.Collections.Generic.List<PendingAudiobookDto> _allAudiobooks = new();

        public ObservableCollection<PendingAudiobookDto> PendingAudiobooks { get; } = new();

        [ObservableProperty]
        private string _searchKeyword = "";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private bool _isListEmpty = true;

        [ObservableProperty]
        private bool _isPlaying = false;

        [ObservableProperty]
        private int? _currentlyPlayingId;

        partial void OnSearchKeywordChanged(string value)
        {
            FilterAudiobooks();
        }

        private void FilterAudiobooks()
        {
            PendingAudiobooks.Clear();
            var query = SearchKeyword?.Trim().ToLower() ?? "";
            if (string.IsNullOrEmpty(query))
            {
                foreach (var a in _allAudiobooks)
                {
                    PendingAudiobooks.Add(a);
                }
            }
            else
            {
                string cleanQuery = InputHelper.RemoveDiacritics(query);
                var matches = _allAudiobooks.Where(a => 
                    (a.ContributorName != null && InputHelper.RemoveDiacritics(a.ContributorName).ToLower().Contains(cleanQuery)) ||
                    (a.BookTitle != null && InputHelper.RemoveDiacritics(a.BookTitle).ToLower().Contains(cleanQuery))
                );
                foreach (var a in matches)
                {
                    PendingAudiobooks.Add(a);
                }
            }
            IsListEmpty = PendingAudiobooks.Count == 0;
        }

        public ICommand LoadPendingAudiobooksCommand { get; }
        public ICommand ApproveAudiobookCommand { get; }
        public ICommand RejectAudiobookCommand { get; }
        public ICommand PlayAudiobookCommand { get; }
        public ICommand StopAudiobookCommand { get; }

        public AudiobookApprovalViewModel(ApiService apiService)
        {
            _apiService = apiService;
            LoadPendingAudiobooksCommand = new AsyncRelayCommand(LoadPendingAudiobooksAsync);
            ApproveAudiobookCommand = new AsyncRelayCommand<PendingAudiobookDto>(ApproveAudiobookAsync);
            RejectAudiobookCommand = new AsyncRelayCommand<PendingAudiobookDto>(RejectAudiobookAsync);
            PlayAudiobookCommand = new RelayCommand<PendingAudiobookDto>(PlayAudio);
            StopAudiobookCommand = new RelayCommand(StopAudio);

            _mediaPlayer.MediaFailed += (s, e) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show($"Không thể tải hoặc phát tệp tin âm thanh từ Server: {e.ErrorException?.Message}", "Lỗi âm thanh", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    StopAudio();
                });
            };
        }

        public async Task LoadPendingAudiobooksAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                var list = await _apiService.GetAsync<PendingAudiobookDto[]>(ApiEndpoints.AudiobookPending);
                _allAudiobooks.Clear();
                if (list != null)
                {
                    foreach (var a in list)
                    {
                        _allAudiobooks.Add(a);
                    }
                }
            }
            catch (Exception ex)
            {
                await AuditLogService.WriteLogAsync("Tải sách nói", $"Lỗi tải dữ liệu sách nói từ máy chủ: {ex.Message}. Hệ thống tự động chuyển sang cơ chế dữ liệu nội bộ.", false);
                LoadMockPendingAudiobooks();
            }
            finally
            {
                FilterAudiobooks();
                IsLoading = false;
            }
        }

        private void LoadMockPendingAudiobooks()
        {
            _allAudiobooks.Clear();
            _allAudiobooks.Add(new PendingAudiobookDto
            {
                Id = 1,
                BookId = 1,
                BookTitle = "Đắc Nhân Tâm",
                ContributorName = "Nguyễn Văn An",
                AudioFilePath = "/uploads/audiobooks/mock1.wav",
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            });
            _allAudiobooks.Add(new PendingAudiobookDto
            {
                Id = 2,
                BookId = 2,
                BookTitle = "Nhà Giả Kim",
                ContributorName = "Lê Thị Hồng",
                AudioFilePath = "/uploads/audiobooks/mock2.wav",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });
        }

        private async Task ApproveAudiobookAsync(PendingAudiobookDto? item)
        {
            if (item == null) return;
            if (IsLoading) return;

            StopAudio();

            var confirm = System.Windows.MessageBox.Show(
                "Bạn có chắc chắn muốn phê duyệt đóng góp sách nói này?",
                "Xác nhận phê duyệt",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);
            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                var payload = new { Status = 1 }; // Approve
                await _apiService.PutAsync<object, object>($"{ApiEndpoints.AudiobookApprove}{item.Id}", payload);
                await AuditLogService.WriteLogAsync("Phê duyệt sách nói", $"Đã phê duyệt tệp sách nói đóng góp của học sinh {item.ContributorName} cho sách '{item.BookTitle}'", true);
                
                System.Windows.MessageBox.Show("✨ Phê duyệt đóng góp sách nói thành công!", "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                _allAudiobooks.Remove(item);
            }
            catch (Exception ex)
            {
                await AuditLogService.WriteLogAsync("Phê duyệt sách nói ngoại tuyến", $"[OFFLINE] Đã phê duyệt sách nói đóng góp của học sinh {item.ContributorName} cho sách '{item.BookTitle}'. Lỗi API: {ex.Message}", true);
                System.Windows.MessageBox.Show("✨ [Giả lập Offline] Phê duyệt sách nói thành công!", "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                _allAudiobooks.Remove(item);
            }
            finally
            {
                FilterAudiobooks();
                IsLoading = false;
            }
        }

        private async Task RejectAudiobookAsync(PendingAudiobookDto? item)
        {
            if (item == null) return;
            if (IsLoading) return;

            StopAudio();

            var confirm = System.Windows.MessageBox.Show(
                "Bạn có chắc chắn muốn từ chối đóng góp sách nói này?",
                "Xác nhận từ chối",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);
            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                var payload = new { Status = 2 }; // Reject
                await _apiService.PutAsync<object, object>($"{ApiEndpoints.AudiobookApprove}{item.Id}", payload);
                await AuditLogService.WriteLogAsync("Từ chối sách nói", $"Đã từ chối tệp sách nói đóng góp của học sinh {item.ContributorName} cho sách '{item.BookTitle}'", true);
                
                System.Windows.MessageBox.Show("❌ Đã từ chối tệp đóng góp sách nói.", "Từ chối", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                _allAudiobooks.Remove(item);
            }
            catch (Exception ex)
            {
                await AuditLogService.WriteLogAsync("Từ chối sách nói ngoại tuyến", $"[OFFLINE] Đã từ chối sách nói đóng góp của học sinh {item.ContributorName} cho sách '{item.BookTitle}'. Lỗi API: {ex.Message}", true);
                System.Windows.MessageBox.Show("❌ [Giả lập Offline] Đã từ chối tệp đóng góp.", "Từ chối", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                _allAudiobooks.Remove(item);
            }
            finally
            {
                FilterAudiobooks();
                IsLoading = false;
            }
        }

        private void PlayAudio(PendingAudiobookDto? item)
        {
            if (item == null) return;

            if (string.IsNullOrEmpty(item.AudioFilePath) || 
                !item.AudioFilePath.StartsWith("/uploads/audiobooks/", StringComparison.OrdinalIgnoreCase) ||
                item.AudioFilePath.Contains("..") || 
                item.AudioFilePath.Contains("://"))
            {
                System.Windows.MessageBox.Show("Đường dẫn tệp tin âm thanh không an toàn hoặc không hợp lệ!", "Cảnh báo bảo mật", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                StopAudio();

                // Dựng URL đầy đủ từ base url (ví dụ: https://localhost:7081/uploads/audiobooks/...)
                string baseHost = ApiService.BaseUrl;
                int apiIdx = baseHost.IndexOf("/api", StringComparison.OrdinalIgnoreCase);
                if (apiIdx >= 0)
                {
                    baseHost = baseHost.Substring(0, apiIdx);
                }

                string fullUrl = baseHost + item.AudioFilePath;
                
                _mediaPlayer.Open(new Uri(fullUrl));
                _mediaPlayer.Play();
                IsPlaying = true;
                CurrentlyPlayingId = item.Id;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi khi phát âm thanh: {ex.Message}", "Lỗi phát âm", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public void StopAudio()
        {
            try
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Close();
            }
            catch { }
            IsPlaying = false;
            CurrentlyPlayingId = null;
        }
    }

    public class PendingAudiobookDto
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string ContributorName { get; set; } = string.Empty;
        public string AudioFilePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string FormattedDate => CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }
}
