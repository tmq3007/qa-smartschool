using System.Windows.Controls;
using System.Windows.Input;

namespace SmartLibrary.Desktop.Views.Teacher
{
    public partial class TeacherAnalyticsView : UserControl
    {
        public TeacherAnalyticsView()
        {
            InitializeComponent();
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                TxtStudentSearch.Focus();
                TxtStudentSearch.SelectAll();
                e.Handled = true;
            }
        }
    }
}
