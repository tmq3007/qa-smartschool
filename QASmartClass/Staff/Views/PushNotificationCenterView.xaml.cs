using System;
using System.Windows.Controls;

namespace QASmartClass.Staff.Views
{
    public partial class PushNotificationCenterView : UserControl
    {
        public PushNotificationCenterView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (DataContext is ViewModels.PushNotificationCenterViewModel vm)
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

