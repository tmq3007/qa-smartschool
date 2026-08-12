using System;
using System.Windows;
using QASmartClass.Data;

namespace QASmartClass.Counseling.Views
{
    public partial class CounselingSessionDialog : Window
    {
        public CounselingSession SessionResult { get; private set; }

        public CounselingSessionDialog()
        {
            InitializeComponent();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string content = TxtContent.Text.Trim();
            string solution = TxtSolution.Text.Trim();
            DateTime? followUpDate = DpFollowUp.SelectedDate;

            if (string.IsNullOrWhiteSpace(content))
            {
                MessageBox.Show("Vui lòng nhập nội dung trao đổi!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (followUpDate.HasValue && followUpDate.Value.Date < DateTime.Today)
            {
                MessageBox.Show("Ngày hẹn tiếp theo không được nhỏ hơn ngày hiện tại!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SessionResult = new CounselingSession
            {
                SessionDate = DateTime.Now,
                Content = content,
                Solution = solution,
                FollowUpDate = followUpDate
            };

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
