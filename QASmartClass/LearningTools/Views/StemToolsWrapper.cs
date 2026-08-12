using System.Windows.Controls;

namespace QASmartClass.LearningTools.Views
{
    public class StemToolsWrapper : UserControl
    {
        public StemToolsWrapper() : this(-1)
        {
        }

        public StemToolsWrapper(int defaultTabIndex)
        {
            var frame = new Frame
            {
                NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden
            };
            var page = new QASmartClass.Classroom.Views.StemToolsPage();
            if (defaultTabIndex >= 0)
            {
                page.SetSelectedTab(defaultTabIndex);
            }
            frame.Content = page;
            this.Content = frame;
        }
    }
}
