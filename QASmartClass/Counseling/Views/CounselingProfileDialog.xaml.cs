using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;

namespace QASmartClass.Counseling.Views
{
    public partial class CounselingProfileDialog : Window
    {
        public CounselingProfile ProfileResult { get; private set; }

        public CounselingProfileDialog()
        {
            InitializeComponent();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtStudentName.Text.Trim();
            string className = TxtClassName.Text.Trim();
            string notes = TxtNotes.Text.Trim();
            string problemType = (CboProblemType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Học tập";

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Vui lòng nhập tên học sinh!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(className))
            {
                MessageBox.Show("Vui lòng nhập tên lớp!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ProfileResult = new CounselingProfile
            {
                StudentName = name,
                ClassName = className,
                ProblemType = problemType,
                Notes = notes,
                CounselorId = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "Counselor"
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
