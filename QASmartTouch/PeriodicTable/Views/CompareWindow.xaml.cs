using System.Windows;
using System.Windows.Controls;
using QASmartTouch.PeriodicTable.ViewModels;

namespace QASmartTouch.PeriodicTable.Views
{
    public partial class CompareWindow : Window
    {
        public CompareWindow()
        {
            InitializeComponent();
        }

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && DataContext is CompareViewModel viewModel)
            {
                // Lấy category từ Tag của RadioButton
                string category = radioButton.Tag?.ToString();
                if (!string.IsNullOrEmpty(category))
                {
                    viewModel.SelectedTab = category;
                }
            }
        }
    }
}

