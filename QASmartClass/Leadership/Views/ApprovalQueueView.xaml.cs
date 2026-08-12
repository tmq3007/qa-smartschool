using System.Windows;
using System.Windows.Controls;
using QASmartClass.Leadership.ViewModels;

namespace QASmartClass.Leadership.Views
{
    public partial class ApprovalQueueView : UserControl
    {
        public ApprovalQueueView()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;
            var vm = new ApprovalQueueViewModel();
            DataContext = vm;
            Loaded += (s, e) =>
            {
                if (vm.LoadPendingLessonsCommand.CanExecute(null))
                {
                    vm.LoadPendingLessonsCommand.Execute(null);
                }
            };
        }
    }
}
