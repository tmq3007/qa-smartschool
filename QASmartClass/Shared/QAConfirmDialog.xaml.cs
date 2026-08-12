using System.Windows;

namespace QASmartClass.Shared
{
    public partial class QAConfirmDialog : Window
    {
        public bool Result { get; private set; }

        public QAConfirmDialog(string title, string message, Window? owner = null)
        {
            InitializeComponent();
            txtTitle.Text = title;
            txtMessage.Text = message;
            if (owner != null)
            {
                this.Owner = owner;
            }
        }

        public static bool ShowDialog(string title, string message, Window? owner = null)
        {
            var dialog = new QAConfirmDialog(title, message, owner);
            dialog.ShowDialog();
            return dialog.Result;
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            DialogResult = false;
            Close();
        }
    }
}
