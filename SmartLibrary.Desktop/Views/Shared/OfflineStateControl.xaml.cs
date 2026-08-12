using System.Windows;
using System.Windows.Controls;

namespace SmartLibrary.Desktop.Views.Shared
{
    public partial class OfflineStateControl : UserControl
    {
        public OfflineStateControl()
        {
            InitializeComponent();
        }

        private void BtnOffline_Click(object sender, RoutedEventArgs e)
        {
            var win = Window.GetWindow(this) as MainWindow;
            win?.GoOffline();
        }
    }
}
