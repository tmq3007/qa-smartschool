using System.Windows;
#if !SMARTTOUCH_STANDALONE
using QASmartClass.Classroom.Views;
#endif

namespace QASmartTouch.Forms
{
    public partial class AskAIWindow : Window
    {
#if !SMARTTOUCH_STANDALONE
        private AIAssistantPage? _page;
#endif

        public AskAIWindow(string initialText)
        {
            InitializeComponent();
            
#if !SMARTTOUCH_STANDALONE
            _page = new AIAssistantPage();
            MainFrame.Navigate(_page);
            
            Loaded += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(initialText))
                {
                    _page.PrepopulateWithText(initialText);
                }
            };
#endif
        }
    }
}
