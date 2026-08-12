using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;
using SmartLibrary.Desktop.Helpers;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class BookReviewApprovalViewModel : ObservableObject, IActiveAwareViewModel
    {
        private readonly ApiService _apiService;

        public ObservableCollection<PendingReviewDto> PendingReviews { get; } = new();
        public ObservableCollection<PendingReviewDto> FilteredReviews { get; } = new();

        [ObservableProperty]
        private string _searchKeyword = "";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private bool _isListEmpty = true;

        partial void OnSearchKeywordChanged(string value)
        {
            FilterReviews();
        }

        private void FilterReviews()
        {
            FilteredReviews.Clear();
            var query = SearchKeyword?.Trim().ToLower() ?? "";
            if (string.IsNullOrEmpty(query))
            {
                foreach (var r in PendingReviews)
                {
                    FilteredReviews.Add(r);
                }
            }
            else
            {
                string cleanQuery = InputHelper.RemoveDiacritics(query);
                var matches = PendingReviews.Where(r =>
                    (r.BorrowerName != null && InputHelper.RemoveDiacritics(r.BorrowerName).ToLower().Contains(cleanQuery)) ||
                    (r.BorrowerSsoId != null && InputHelper.RemoveDiacritics(r.BorrowerSsoId).ToLower().Contains(cleanQuery)) ||
                    (r.BookTitle != null && InputHelper.RemoveDiacritics(r.BookTitle).ToLower().Contains(cleanQuery))
                );
                foreach (var r in matches)
                {
                    FilteredReviews.Add(r);
                }
            }
            IsListEmpty = FilteredReviews.Count == 0;
        }

        public ICommand LoadPendingReviewsCommand { get; }
        public ICommand ApproveReviewCommand { get; }
        public ICommand ApproveFeaturedReviewCommand { get; }
        public ICommand RejectReviewCommand { get; }

        public BookReviewApprovalViewModel(ApiService apiService)
        {
            _apiService = apiService;
            LoadPendingReviewsCommand = new AsyncRelayCommand(LoadPendingReviewsAsync);
            ApproveReviewCommand = new AsyncRelayCommand<PendingReviewDto>(ApproveReviewAsync);
            ApproveFeaturedReviewCommand = new AsyncRelayCommand<PendingReviewDto>(ApproveFeaturedReviewAsync);
            RejectReviewCommand = new AsyncRelayCommand<PendingReviewDto>(RejectReviewAsync);
        }

        public void Activate()
        {
            _ = LoadPendingReviewsAsync();
        }

        public async Task LoadPendingReviewsAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                var reviews = await _apiService.GetAsync<PendingReviewDto[]>(ApiEndpoints.PendingReviews);
                PendingReviews.Clear();
                if (reviews != null)
                {
                    foreach (var r in reviews)
                    {
                        PendingReviews.Add(r);
                    }
                }
            }
            catch
            {
                LoadMockPendingReviews();
            }
            finally
            {
                FilterReviews();
                IsLoading = false;
            }
        }

        private void LoadMockPendingReviews()
        {
            PendingReviews.Clear();
            PendingReviews.Add(new PendingReviewDto
            {
                Id = 101,
                RatingStar = 5,
                Content = "Cuốn sách Đắc Nhân Tâm này thực sự thay đổi tư duy của mình rất nhiều! Khuyên các bạn học sinh nên đọc.",
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                BorrowerName = "Nguyễn Văn An",
                BorrowerSsoId = "HS001",
                BookTitle = "Đắc Nhân Tâm"
            });
            PendingReviews.Add(new PendingReviewDto
            {
                Id = 102,
                RatingStar = 4,
                Content = "Truyện Conan đọc giải trí rất tốt, hình ảnh đẹp, tuy nhiên thỉnh thoảng hơi rách gáy.",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                BorrowerName = "Võ Minh Em",
                BorrowerSsoId = "HS002",
                BookTitle = "Thám tử lừng danh Conan - Tập 95"
            });
        }

        private async Task ApproveReviewAsync(PendingReviewDto? review)
        {
            if (review == null) return;
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                var response = await _apiService.PostAsync<object, ApproveResponseDto>(ApiEndpoints.GetApproveReviewUrl(review.Id), new object());
                if (response != null && !string.IsNullOrEmpty(response.SsoUserId))
                {
                    GamificationService.AddXp(response.SsoUserId, 50);
                }
                
                await AuditLogService.WriteLogAsync("Phê duyệt đánh giá sách", $"Đã phê duyệt đánh giá của học sinh {review.BorrowerName} ({review.BorrowerSsoId}) cho sách '{review.BookTitle}'. Cộng +50 XP.", true);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show("✨ Phê duyệt đánh giá sách thành công và đã cộng +50 XP cho học sinh!", "Phê Duyệt", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                });
                PendingReviews.Remove(review);
            }
            catch (Exception ex)
            {
                GamificationService.AddXp(review.BorrowerSsoId, 50);
                await AuditLogService.WriteLogAsync("Phê duyệt đánh giá sách ngoại tuyến", $"[OFFLINE] Đã phê duyệt đánh giá của học sinh {review.BorrowerName} ({review.BorrowerSsoId}) cho sách '{review.BookTitle}'. Lỗi API: {ex.Message}", true);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show("✨ [Giả lập Offline] Phê duyệt thành công và cộng +50 XP!", "Phê Duyệt", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                });
                PendingReviews.Remove(review);
            }
            finally
            {
                FilterReviews();
                IsLoading = false;
            }
        }

        private async Task ApproveFeaturedReviewAsync(PendingReviewDto? review)
        {
            if (review == null) return;
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                var response = await _apiService.PostAsync<object, ApproveResponseDto>(ApiEndpoints.GetApproveFeaturedReviewUrl(review.Id), new object());
                if (response != null && !string.IsNullOrEmpty(response.SsoUserId))
                {
                    GamificationService.AddXp(response.SsoUserId, 100);
                }
                else
                {
                    GamificationService.AddXp(review.BorrowerSsoId, 100);
                }
                
                await AuditLogService.WriteLogAsync("Phê duyệt tiêu điểm văn học", $"Đã phê duyệt tiêu điểm văn học cho đánh giá của học sinh {review.BorrowerName} ({review.BorrowerSsoId}) cho sách '{review.BookTitle}'. Cộng +100 XP.", true);
                
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    System.Windows.MessageBox.Show("🎖️ Phê duyệt tiêu điểm văn học thành công và đã cộng +100 XP cho học sinh!", "Phê Duyệt Tiêu Điểm", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                });
                PendingReviews.Remove(review);
            }
            catch (Exception ex)
            {
                GamificationService.AddXp(review.BorrowerSsoId, 100);
                await AuditLogService.WriteLogAsync("Phê duyệt tiêu điểm văn học ngoại tuyến", $"[OFFLINE] Đã phê duyệt tiêu điểm văn học cho đánh giá của học sinh {review.BorrowerName} ({review.BorrowerSsoId}) cho sách '{review.BookTitle}'. Lỗi API: {ex.Message}", true);
                
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    System.Windows.MessageBox.Show("🎖️ [Giả lập Offline] Phê duyệt Tiêu điểm thành công và cộng +100 XP!", "Phê Duyệt Tiêu Điểm", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                });
                PendingReviews.Remove(review);
            }
            finally
            {
                FilterReviews();
                IsLoading = false;
            }
        }

        private async Task RejectReviewAsync(PendingReviewDto? review)
        {
            if (review == null) return;
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                await _apiService.DeleteAsync(ApiEndpoints.GetDeleteReviewUrl(review.Id));
                await AuditLogService.WriteLogAsync("Từ chối đánh giá sách", $"Đã từ chối và gỡ bỏ đánh giá không phù hợp của học sinh {review.BorrowerName} ({review.BorrowerSsoId}) cho sách '{review.BookTitle}'", true);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show("❌ Đã từ chối và gỡ bỏ đánh giá không phù hợp thành công.", "Từ chối Đánh giá", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                });
                PendingReviews.Remove(review);
            }
            catch (Exception ex)
            {
                await AuditLogService.WriteLogAsync("Từ chối đánh giá sách ngoại tuyến", $"[OFFLINE] Đã từ chối đánh giá của học sinh {review.BorrowerName} ({review.BorrowerSsoId}) cho sách '{review.BookTitle}'. Lỗi API: {ex.Message}", true);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show("❌ [Giả lập Offline] Đã từ chối và gỡ bỏ đánh giá không phù hợp.", "Từ chối Đánh giá", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                });
                PendingReviews.Remove(review);
            }
            finally
            {
                FilterReviews();
                IsLoading = false;
            }
        }
    }

    public class PendingReviewDto
    {
        public int Id { get; set; }
        public int RatingStar { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string BorrowerName { get; set; } = string.Empty;
        public string BorrowerSsoId { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        
        public string RatingStarsText => new string('★', RatingStar) + new string('☆', 5 - RatingStar);
        public string FormattedDate => CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }

    public class ApproveResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public int ReviewId { get; set; }
        public string SsoUserId { get; set; } = string.Empty;
    }
}
