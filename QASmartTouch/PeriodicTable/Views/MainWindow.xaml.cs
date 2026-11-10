using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QASmartTouch.PeriodicTable.Models;
using QASmartTouch.PeriodicTable.ViewModels;
using QASmartTouch.Shared;

namespace QASmartTouch.PeriodicTable.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Topmost = true;
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Chỉ xử lý khi click vào Canvas trống, không phải vào các element con
            if (e.Source == sender)
            {
                var viewModel = DataContext as MainViewModel;
                viewModel?.ClearSelectionCommand.Execute(null);
            }
        }

        private void ElementButton_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button button && button.DataContext is Element element)
            {
                // Mở cửa sổ chi tiết nguyên tố
                OpenElementDetail(element.AtomicNumber);
            }
        }

        private void DetailButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel?.SelectedElement != null)
            {
                OpenElementDetail(viewModel.SelectedElement.AtomicNumber);
            }
        }

        private void OpenElementDetail(int atomicNumber)
        {
            var detailViewModel = new ElementDetailViewModel();
            detailViewModel.LoadElementByAtomicNumber(atomicNumber);

            if (detailViewModel.CurrentElement != null)
            {
                var detailWindow = new ElementDetailWindow
                {
                    DataContext = detailViewModel
                };
                WindowHelper.ShowChildDialog(detailWindow, this);
            }
        }
    }
}
