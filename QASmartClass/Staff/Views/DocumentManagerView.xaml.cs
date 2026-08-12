using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.Services;
using System.Threading.Tasks;

namespace QASmartClass.Staff.Views
{
    public partial class DocumentManagerView : UserControl
    {
        public DocumentManagerView()
        {
            InitializeComponent();
            Loaded += async (s, e) => 
            { 
                SearchBox.Focus(); 
                if (DataContext is ViewModels.DocumentManagerViewModel vm)
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

