using System.Windows.Controls;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Student;

public partial class ReadingGroupView : UserControl
{
    public ReadingGroupView(ReadingGroupViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
