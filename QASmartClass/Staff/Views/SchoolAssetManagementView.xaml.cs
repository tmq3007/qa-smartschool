using System;
using System.Windows.Controls;

namespace QASmartClass.Staff.Views
{
    public partial class SchoolAssetManagementView : UserControl
    {
        public SchoolAssetManagementView()
        {
            InitializeComponent();
            Loaded += async (s, e) => 
            { 
                SearchBox.Focus(); 
                if (DataContext is ViewModels.SchoolAssetManagementViewModel vm)
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

        private void LeftTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tc)
            {
                if (tc.SelectedIndex == 0 && NewAssetNameBox != null)
                {
                    NewAssetNameBox.Focus();
                }
                else if (tc.SelectedIndex == 1 && BookedByBox != null)
                {
                    BookedByBox.Focus();
                }
            }
        }
    }
}

