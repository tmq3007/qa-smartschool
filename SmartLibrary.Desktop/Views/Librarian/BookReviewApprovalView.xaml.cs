using System.Windows.Controls;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Librarian
{
    public partial class BookReviewApprovalView : UserControl
    {
        public BookReviewApprovalView()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Focus();
            SearchBox.Focus();
            if (DataContext is BookReviewApprovalViewModel vm)
            {
                if (vm.PendingReviews.Count == 0)
                {
                    await vm.LoadPendingReviewsAsync();
                }
            }
        }

        private void UserControl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control && e.Key == System.Windows.Input.Key.F)
            {
                SearchBox.Focus();
                SearchBox.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.F5)
            {
                if (DataContext is BookReviewApprovalViewModel vm && vm.LoadPendingReviewsCommand.CanExecute(null))
                {
                    vm.LoadPendingReviewsCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}
