using System.Windows.Controls;

namespace QASmartClass.Staff.Views
{
    public partial class CanteenPosView : UserControl
    {
        public CanteenPosView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                SearchTextBox.Focus();
                if (DataContext is ViewModels.CanteenPosViewModel vm)
                {
                    await vm.InitializeAsync();
                    vm.PaymentCompleted += (sender, args) =>
                    {
                        SearchTextBox.Focus();
                        SearchTextBox.SelectAll();
                    };
                    vm.FocusRequested += () =>
                    {
                        SearchTextBox.Focus();
                        SearchTextBox.SelectAll();
                    };
                }
            };
            Unloaded += (s, e) =>
            {
                if (DataContext is System.IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };
        }

        private void NumberOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Chỉ cho phép nhập các chữ số
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^[0-9]+$");
        }
    }
}

