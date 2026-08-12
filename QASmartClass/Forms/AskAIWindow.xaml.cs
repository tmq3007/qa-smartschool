using System.Windows;
using QASmartClass.Classroom.Views;

namespace QASmartTouch.Forms
{
    public partial class AskAIWindow : Window
    {
        private AIAssistantPage _page;

        public AskAIWindow(string initialText)
        {
            InitializeComponent();
            
            _page = new AIAssistantPage();
            MainFrame.Navigate(_page);
            
            Loaded += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(initialText))
                {
                    _page.PrepopulateWithText(initialText);
                }
            };
        }
    }
}
