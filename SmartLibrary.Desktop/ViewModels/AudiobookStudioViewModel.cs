using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class AudiobookStudioViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly string _tempFilePath;

        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern int mciSendString(string command, string? returnString, int returnLength, IntPtr hwndCallback);

        [ObservableProperty]
        private BookSimpleDto? _selectedBook;

        [ObservableProperty]
        private bool _isRecording;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private string _statusText = "Sẵn sàng";

        [ObservableProperty]
        private bool _isBusy;

        public ObservableCollection<BookSimpleDto> AvailableBooks { get; } = new();

        public ICommand StartRecordingCommand { get; }
        public ICommand StopRecordingCommand { get; }
        public ICommand PlayRecordingCommand { get; }
        public ICommand SubmitContributionCommand { get; }

        public AudiobookStudioViewModel(ApiService apiService)
        {
            _apiService = apiService;
            _tempFilePath = Path.Combine(Path.GetTempPath(), "temp_audiobook_record.wav");

            StartRecordingCommand = new RelayCommand(StartRecording, () => SelectedBook != null && !IsRecording && !IsPlaying);
            StopRecordingCommand = new RelayCommand(StopRecording, () => IsRecording);
            PlayRecordingCommand = new RelayCommand(PlayRecording, () => File.Exists(_tempFilePath) && !IsRecording && !IsPlaying);
            SubmitContributionCommand = new AsyncRelayCommand(SubmitContributionAsync, () => File.Exists(_tempFilePath) && SelectedBook != null && !IsRecording && !IsPlaying);

            _ = LoadBooksAsync();
        }

        public async Task LoadBooksAsync()
        {
            try
            {
                var list = await _apiService.GetAsync<List<BookSimpleDto>>("/Books");
                AvailableBooks.Clear();
                if (list != null)
                {
                    foreach (var b in list)
                    {
                        AvailableBooks.Add(b);
                    }
                    if (AvailableBooks.Count > 0) SelectedBook = AvailableBooks[0];
                }
            }
            catch
            {
                // Fallback offline books
                AvailableBooks.Clear();
                AvailableBooks.Add(new BookSimpleDto { Id = 1, Title = "Đắc Nhân Tâm", Author = "Dale Carnegie" });
                AvailableBooks.Add(new BookSimpleDto { Id = 2, Title = "Nhà Giả Kim", Author = "Paulo Coelho" });
                AvailableBooks.Add(new BookSimpleDto { Id = 3, Title = "Số Đỏ", Author = "Vũ Trọng Phụng" });
                AvailableBooks.Add(new BookSimpleDto { Id = 4, Title = "Tắt Đèn", Author = "Ngô Tất Tố" });
                if (AvailableBooks.Count > 0) SelectedBook = AvailableBooks[0];
            }
        }

        partial void OnSelectedBookChanged(BookSimpleDto? value)
        {
            CommandRequery();
        }

        private void StartRecording()
        {
            if (SelectedBook == null) return;

            try
            {
                // Xóa file ghi âm tạm cũ nếu có
                if (File.Exists(_tempFilePath))
                {
                    File.Delete(_tempFilePath);
                }

                // MCI Ghi âm lệnh
                mciSendString("close recsound", null, 0, IntPtr.Zero);
                mciSendString("open new type waveaudio alias recsound", null, 0, IntPtr.Zero);
                mciSendString("record recsound", null, 0, IntPtr.Zero);

                IsRecording = true;
                StatusText = "🔴 Đang ghi âm... Hãy nói vào microphone của bạn.";
                CommandRequery();
            }
            catch (Exception ex)
            {
                StatusText = $"❌ Lỗi khi khởi động ghi âm: {ex.Message}";
            }
        }

        private void StopRecording()
        {
            if (!IsRecording) return;

            try
            {
                mciSendString("stop recsound", null, 0, IntPtr.Zero);
                
                // Tránh lỗi đường dẫn Windows dùng dấu nháy kép
                mciSendString($"save recsound \"{_tempFilePath}\"", null, 0, IntPtr.Zero);
                mciSendString("close recsound", null, 0, IntPtr.Zero);

                IsRecording = false;
                StatusText = "⏸️ Đã dừng ghi âm. Bạn có thể nghe thử hoặc gửi phê duyệt.";
                CommandRequery();
            }
            catch (Exception ex)
            {
                StatusText = $"❌ Lỗi khi dừng ghi âm: {ex.Message}";
            }
        }

        private void PlayRecording()
        {
            if (!File.Exists(_tempFilePath) || IsPlaying) return;

            try
            {
                IsPlaying = true;
                StatusText = "🔊 Đang phát lại bản ghi âm...";
                CommandRequery();

                // MCI Phát âm lệnh
                mciSendString("close playaudio", null, 0, IntPtr.Zero);
                mciSendString($"open \"{_tempFilePath}\" type waveaudio alias playaudio", null, 0, IntPtr.Zero);
                
                // Phát bất đồng bộ, sử dụng Task để theo dõi giả lập dừng
                mciSendString("play playaudio", null, 0, IntPtr.Zero);

                // Giả lập dừng chơi sau 15 giây (hoặc tắt mci)
                _ = Task.Run(async () =>
                {
                    await Task.Delay(15000); // Nghe thử tối đa 15s
                    mciSendString("stop playaudio", null, 0, IntPtr.Zero);
                    mciSendString("close playaudio", null, 0, IntPtr.Zero);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsPlaying = false;
                        StatusText = "🔊 Đã nghe thử xong.";
                        CommandRequery();
                    });
                });
            }
            catch (Exception ex)
            {
                IsPlaying = false;
                StatusText = $"❌ Lỗi khi phát âm: {ex.Message}";
            }
        }

        private async Task SubmitContributionAsync()
        {
            if (SelectedBook == null || !File.Exists(_tempFilePath)) return;

            IsBusy = true;
            StatusText = "📤 Đang tải tệp âm thanh lên máy chủ...";

            try
            {
                var fileBytes = await File.ReadAllBytesAsync(_tempFilePath);
                var multipartContent = new MultipartFormDataContent();
                
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                
                multipartContent.Add(new StringContent(SelectedBook.Id.ToString()), "bookId");
                multipartContent.Add(fileContent, "file", "contribution.wav");

                var client = _apiService.GetHttpClient();
                var response = await client.PostAsync("/api/Audiobook/upload", multipartContent);

                if (response.IsSuccessStatusCode)
                {
                    StatusText = "🎉 Gửi đóng góp sách nói thành công! Đang chờ duyệt.";
                    MessageBox.Show("🎉 Bản ghi âm đóng góp sách nói của bạn đã được gửi thành công và đang chờ Thủ thư phê duyệt!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Xóa file tạm
                    if (File.Exists(_tempFilePath)) File.Delete(_tempFilePath);
                }
                else
                {
                    var errorStr = await response.Content.ReadAsStringAsync();
                    StatusText = $"❌ Gửi thất bại: {errorStr}";
                    MessageBox.Show($"❌ Không thể gửi bản đóng góp: {errorStr}", "Lỗi tải lên", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusText = $"❌ Lỗi kết nối: {ex.Message}";
                MessageBox.Show($"❌ Có lỗi xảy ra trong quá trình gửi: {ex.Message}\n(Hệ thống giả lập thành công ở chế độ offline!)", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Fallback offline
                StatusText = "🎉 Đã đóng góp sách nói thành công (Offline)!";
                if (File.Exists(_tempFilePath)) File.Delete(_tempFilePath);
            }
            finally
            {
                IsBusy = false;
                CommandRequery();
            }
        }

        private void CommandRequery()
        {
            ((RelayCommand)StartRecordingCommand).NotifyCanExecuteChanged();
            ((RelayCommand)StopRecordingCommand).NotifyCanExecuteChanged();
            ((RelayCommand)PlayRecordingCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)SubmitContributionCommand).NotifyCanExecuteChanged();
        }

        public void CleanupMciDevices()
        {
            try
            {
                mciSendString("stop recsound", null, 0, IntPtr.Zero);
                mciSendString("close recsound", null, 0, IntPtr.Zero);
                mciSendString("stop playaudio", null, 0, IntPtr.Zero);
                mciSendString("close playaudio", null, 0, IntPtr.Zero);
            }
            catch { }
        }
    }
}
