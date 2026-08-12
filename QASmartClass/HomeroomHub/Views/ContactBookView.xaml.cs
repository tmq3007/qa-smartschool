using System.Windows.Controls;
using QASmartClass.HomeroomHub.ViewModels;

namespace QASmartClass.HomeroomHub.Views
{
    public partial class ContactBookView : UserControl
    {
        public ContactBookView()
        {
            InitializeComponent();
            DataContext = new ContactBookViewModel();
        }
    }
}

