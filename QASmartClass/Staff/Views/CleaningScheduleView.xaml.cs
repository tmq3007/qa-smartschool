using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.Data;
using QASmartClass.Services;
using System.Threading.Tasks;

namespace QASmartClass.Staff.Views
{
    public partial class CleaningScheduleView : UserControl
    {
        private double _savedOffset = 0;

        public CleaningScheduleView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (DataContext is ViewModels.CleaningScheduleViewModel vm)
                {
                    vm.PropertyChanged += Vm_PropertyChanged;
                    await vm.InitializeAsync();
                }
            };
            Unloaded += (s, e) => 
            {
                if (DataContext is ViewModels.CleaningScheduleViewModel vm)
                {
                    vm.PropertyChanged -= Vm_PropertyChanged;
                }
                if (DataContext is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Tasks")
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(LvTasks);
                if (scrollViewer != null)
                {
                    _savedOffset = scrollViewer.VerticalOffset;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var sv = FindVisualChild<ScrollViewer>(LvTasks);
                        if (sv != null)
                        {
                            sv.ScrollToVerticalOffset(_savedOffset);
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }

        private static T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is T t)
                    return t;
                T? childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
    }
}
