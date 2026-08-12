using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;
using System;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.Leadership.Views
{
    public partial class Evaluation360Page : Page
    {
        private readonly AppDbContext _db;
        private readonly EvaluationService _evalService;
        private readonly string _currentUser;

        public Evaluation360Page(AppDbContext db)
        {
            InitializeComponent();
            _db = db;
            _evalService = new EvaluationService(_db);
            _currentUser = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "GV_CurrentLoggedUser";
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string targetId = (CbTarget.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
                string role = (CbRole.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Peer";
                
                int rating = 5;
                if (RbRate1.IsChecked == true) rating = 1;
                else if (RbRate2.IsChecked == true) rating = 2;
                else if (RbRate3.IsChecked == true) rating = 3;
                else if (RbRate4.IsChecked == true) rating = 4;

                string comments = TxtComments.Text.Trim();

                if (string.IsNullOrEmpty(targetId))
                {
                    MessageBox.Show("Vui lòng chọn đối tượng đánh giá.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(comments) && rating <= 3)
                {
                    MessageBox.Show("Vui lòng nhập nhận xét chi tiết khi đánh giá dưới 4 sao để giúp đồng nghiệp cải thiện.", "Yêu cầu", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var record = new EvaluationRecord
                {
                    EvaluatorId = _currentUser,
                    TargetId = targetId,
                    RoleRelation = role,
                    Rating = rating,
                    Comments = comments
                };

                bool success = _evalService.SaveEvaluation(record);

                if (success)
                {
                    MessageBox.Show("Cảm ơn bạn đã gửi đánh giá. Dữ liệu đã được ghi nhận.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    TxtComments.Text = "";
                    RbRate5.IsChecked = true;
                }
                else
                {
                    MessageBox.Show("Có lỗi xảy ra trong quá trình lưu dữ liệu.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi submit form Evaluation360.");
                MessageBox.Show("Đã xảy ra lỗi không xác định.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

