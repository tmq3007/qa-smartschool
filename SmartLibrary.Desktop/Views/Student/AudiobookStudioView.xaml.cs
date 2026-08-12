using System.Windows.Controls;

namespace SmartLibrary.Desktop.Views.Student
{
    public partial class AudiobookStudioView : UserControl
    {
        public AudiobookStudioView()
        {
            InitializeComponent();
            Unloaded += (s, e) =>
            {
                if (DataContext is ViewModels.AudiobookStudioViewModel vm)
                {
                    vm.CleanupMciDevices();
                }
            };
        }
    }
}
