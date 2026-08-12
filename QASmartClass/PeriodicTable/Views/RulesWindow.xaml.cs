using System.Windows;

namespace QASmartTouch.PeriodicTable.Views
{
    /// <summary>
    /// Cửa sổ hiển thị các quy tắc tan cơ bản
    /// </summary>
    public partial class RulesWindow : Window
    {
        public RulesWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

