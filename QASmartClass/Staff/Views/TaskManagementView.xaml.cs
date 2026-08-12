using System;
using System.Windows.Controls;

namespace QASmartClass.Staff.Views
{
    public partial class TaskManagementView : UserControl
    {
        public TaskManagementView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (DataContext is ViewModels.TaskManagementViewModel vm)
                {
                    await vm.InitializeAsync();
                }
            };
            Unloaded += (s, e) => 
            {
                if (DataContext is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };
        }
    }
}

