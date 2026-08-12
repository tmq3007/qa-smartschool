using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.Classroom.ViewModels;

namespace QASmartClass.Classroom.Views
{
    public partial class HomeworkPage : Page
    {
        public HomeworkPage()
        {
            InitializeComponent();
            DataContext = new HomeworkViewModel();
            Loaded += (_, _) => ((HomeworkViewModel)DataContext).LoadHomework();
        }

        private void Filter_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border b && b.Tag is string tag)
            {
                filterAll.Background = Brushes.Transparent;
                filterActive.Background = Brushes.Transparent;
                filterExpired.Background = Brushes.Transparent;
                b.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                ((HomeworkViewModel)DataContext).FilterCommand.Execute(tag);
            }
        }

        private void HideGuide_Click(object sender, RoutedEventArgs e) => guidePanel.Visibility = Visibility.Collapsed;
        private void ShowGuide_Click(object sender, RoutedEventArgs e) => guidePanel.Visibility = guidePanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }
}
