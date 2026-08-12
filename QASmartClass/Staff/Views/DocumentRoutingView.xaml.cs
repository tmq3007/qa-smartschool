using System;
using System.Windows.Controls;

namespace QASmartClass.Staff.Views
{
    public partial class DocumentRoutingView : UserControl
    {
        public DocumentRoutingView()
        {
            InitializeComponent();
            Loaded += async (s, e) => 
            { 
                SearchBox.Focus(); 
                if (DataContext is ViewModels.DocumentRoutingViewModel vm)
                {
                    await vm.InitializeAsync();
                }
            };
        }
    }
}

