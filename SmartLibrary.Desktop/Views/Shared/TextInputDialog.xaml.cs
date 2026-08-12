using System.Windows;
using System.Windows.Input;

namespace SmartLibrary.Desktop.Views.Shared
{
    public partial class TextInputDialog : Window
    {
        public string DialogTitle { get; }
        public string PromptText { get; }
        public string InputText { get; set; }

        public TextInputDialog(string title, string promptText, string defaultText = "")
        {
            DialogTitle = title;
            PromptText = promptText;
            InputText = defaultText;
            InitializeComponent();
            DataContext = this;
            
            Loaded += (s, e) =>
            {
                InputTextBox.Focus();
                InputTextBox.SelectAll();
            };
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
