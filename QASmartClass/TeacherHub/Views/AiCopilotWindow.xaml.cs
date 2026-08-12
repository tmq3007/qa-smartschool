using QASmartClass.Data;
using QASmartClass.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.TeacherHub.Views
{
    public partial class AiCopilotWindow : Window
    {
        private AppDbContext _db;

        public AiCopilotWindow(AppDbContext db)
        {
            InitializeComponent();
            _db = db;
            Closed += (s, e) => { _db = null; };
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string text = TxtSourceText.Text.Trim();
                if (string.IsNullOrEmpty(text))
                {
                    MessageBox.Show("Vui lòng dán nội dung văn bản gốc vào ô trống.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string subject = (CboSubject.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Khác";

                // Cập nhật trạng thái giao diện sang Loading
                PrgLoading.Visibility = Visibility.Visible;
                BtnGenerate.IsEnabled = false;
                BtnCancel.IsEnabled = false;
                BtnGenerate.Content = "Đang tạo...";

                // Chạy ngầm việc sinh câu hỏi trên luồng nền (Đảm bảo an toàn luồng)
                var generatedQuestions = await System.Threading.Tasks.Task.Run(() =>
                    AiCopilotService.GenerateQuestionsFromText(text, subject)
                );

                if (generatedQuestions != null && generatedQuestions.Count > 0)
                {
                    if (_db != null)
                    {
                        _db.QuestionBankItems.AddRange(generatedQuestions);
                        await _db.SaveChangesAsync();

                        AuditHelper.Log(_db, "AI_Copilot", QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "Teacher", $"Generated {generatedQuestions.Count} questions from text snippet");
                    }

                    MessageBox.Show($"🤖 AI đã tự động tạo và lưu thành công {generatedQuestions.Count} câu hỏi trắc nghiệm vào Ngân hàng câu hỏi!", 
                        "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Không thể tạo câu hỏi từ văn bản này.", "Lỗi AI", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi trong quá trình AI xử lý: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Phục hồi trạng thái giao diện
                PrgLoading.Visibility = Visibility.Collapsed;
                BtnGenerate.IsEnabled = true;
                BtnCancel.IsEnabled = true;
                BtnGenerate.Content = "Tạo Câu Hỏi (AI)";
            }
        }
    }
}

