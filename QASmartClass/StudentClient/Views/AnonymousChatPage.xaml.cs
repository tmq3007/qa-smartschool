using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.StudentClient.Services;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.StudentClient.Views
{
    public partial class AnonymousChatPage : Page
    {
        private readonly AppDbContext _db;
        private readonly int _studentId;
        private readonly AnonymousService _anonymousService;

        public AnonymousChatPage(AppDbContext db, int studentId)
        {
            InitializeComponent();
            _db = db;
            _studentId = studentId;
            _anonymousService = new AnonymousService(_db);
            Loaded += (_, __) => LoadHistory();
        }

        private string GetOrCreateAnonymousToken()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                // Băm ID học sinh để tạo tên file bảo mật
                string hashedId;
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    var hashBytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(_studentId.ToString()));
                    hashedId = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
                string tokenPath = Path.Combine(appData, "QASmartClass", $"anon_{hashedId}.dat");
                
                if (File.Exists(tokenPath))
                {
                    return SecureProfileHelper.ReadProfileText(tokenPath).Trim();
                }
                
                string newToken = Guid.NewGuid().ToString("N");
                Directory.CreateDirectory(Path.GetDirectoryName(tokenPath)!);
                SecureProfileHelper.WriteProfileText(tokenPath, newToken);
                return newToken;
            }
            catch (Exception ex)
            {
                Log.Warning("Không thể lưu token ẩn danh cục bộ: {Err}", ex.Message);
                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    byte[] idBytes = System.Text.Encoding.UTF8.GetBytes("SmartClass_Anon_Salt_" + _studentId);
                    byte[] hashBytes = sha.ComputeHash(idBytes);
                    return "HS_ANON_" + Convert.ToBase64String(hashBytes).Substring(0, 16).Replace("/", "_").Replace("+", "-");
                }
            }
        }

        private void LoadHistory()
        {
            try
            {
                string token = GetOrCreateAnonymousToken();
                var history = _anonymousService.GetMyQuestions(token);
                LvHistory.ItemsSource = history.Select(h => new
                {
                    h.Category,
                    h.CreatedAt,
                    h.Question,
                    h.Answer,
                    HasAnswer = string.IsNullOrEmpty(h.Answer) ? Visibility.Collapsed : Visibility.Visible
                }).ToList();
            }
            catch (Exception ex) { Log.Warning("AnonymousChat Load error: {Err}", ex.Message); }
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtQuestion.Text))
            {
                MessageBox.Show("Vui lòng nhập nội dung câu hỏi.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string token = GetOrCreateAnonymousToken();
            string category = (CbCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Khác";
            bool success = _anonymousService.SendQuestion(token, category, TxtQuestion.Text.Trim());

            if (success)
            {
                MessageBox.Show("Câu hỏi của bạn đã được gửi ẩn danh thành công. Chuyên viên sẽ trả lời sớm nhất.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtQuestion.Text = "";
                LoadHistory();
            }
            else
            {
                MessageBox.Show("Lỗi khi gửi câu hỏi.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

