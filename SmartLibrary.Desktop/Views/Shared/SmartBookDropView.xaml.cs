using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Shared
{
    public partial class SmartBookDropView : UserControl
    {
        public SmartBookDropView()
        {
            InitializeComponent();
        }

        private void BookItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement element && element.DataContext is LoanItemDto loan)
            {
                DragDrop.DoDragDrop(element, loan, DragDropEffects.Move);
            }
        }

        private void DropZone_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(LoanItemDto)))
            {
                e.Effects = DragDropEffects.Move;
                DropZone.Background = (Brush)this.TryFindResource("SelectedBackgroundBrush") ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3A8A")); // Highlight blue
                DropZone.BorderBrush = (Brush)this.TryFindResource("AccentBrush") ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")); // Accent green
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void DropZone_DragLeave(object sender, DragEventArgs e)
        {
            DropZone.Background = (Brush)this.TryFindResource("CardBrush") ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")); // Reset
            DropZone.BorderBrush = (Brush)this.TryFindResource("BorderBrush") ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")); // Reset
        }

        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            DropZone.Background = (Brush)this.TryFindResource("CardBrush") ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")); // Reset
            DropZone.BorderBrush = (Brush)this.TryFindResource("BorderBrush") ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")); // Reset

            if (e.Data.GetDataPresent(typeof(LoanItemDto)) && e.Data.GetData(typeof(LoanItemDto)) is LoanItemDto loan)
            {
                if (this.DataContext is SmartBookDropViewModel vm)
                {
                    vm.DropBookCommand.Execute(loan);
                }
            }
        }
    }
}
