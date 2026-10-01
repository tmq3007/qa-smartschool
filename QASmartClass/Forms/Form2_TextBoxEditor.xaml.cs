using System;
using System.Windows;
using QASmartTouch.Controls;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    /// <summary>
    /// Text Box Editor Dialog
    /// Allows users to create rich text boxes with preset templates
    /// </summary>
    public partial class Form2_TextBoxEditor : Window
    {
        public bool IsConfirmed { get; private set; } = false;
        public RichTextBoxControl? TextBoxControl => textBoxEditor;

        public Form2_TextBoxEditor()
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
            // QC_4.2_TOUCH_PIPELINE: TextBox Window — WPF tự cô lập, KHÔNG cần ApplyTouchIsolation
            
            // Set default custom template
            textBoxEditor.ApplyTemplate(TextBoxTemplate.Note);

            Loaded += (s, e) =>
            {
                textBoxEditor.Focus();
                TouchKeyboardHelper.ShowTouchKeyboard();
            };

            Closed += (s, e) =>
            {
                TouchKeyboardHelper.HideTouchKeyboard();
            };
        }

        #region Template Selection

        private void NoteTemplate_Click(object sender, RoutedEventArgs e)
        {
            textBoxEditor.ApplyTemplate(TextBoxTemplate.Note);
            System.Diagnostics.Debug.WriteLine("📌 Note template applied");
        }

        private void WarningTemplate_Click(object sender, RoutedEventArgs e)
        {
            textBoxEditor.ApplyTemplate(TextBoxTemplate.Warning);
            System.Diagnostics.Debug.WriteLine("⚠️ Warning template applied");
        }

        private void TipTemplate_Click(object sender, RoutedEventArgs e)
        {
            textBoxEditor.ApplyTemplate(TextBoxTemplate.Tip);
            System.Diagnostics.Debug.WriteLine("💡 Tip template applied");
        }

        private void ConclusionTemplate_Click(object sender, RoutedEventArgs e)
        {
            textBoxEditor.ApplyTemplate(TextBoxTemplate.Conclusion);
            System.Diagnostics.Debug.WriteLine("✅ Conclusion template applied");
        }

        private void DefinitionTemplate_Click(object sender, RoutedEventArgs e)
        {
            textBoxEditor.ApplyTemplate(TextBoxTemplate.Definition);
            System.Diagnostics.Debug.WriteLine("📖 Definition template applied");
        }

        private void CustomTemplate_Click(object sender, RoutedEventArgs e)
        {
            // Reset to blank custom template
            textBoxEditor.Text = "";
            System.Diagnostics.Debug.WriteLine("✏️ Custom template selected");
        }

        #endregion

        #region Dialog Actions

        private void Insert_Click(object sender, RoutedEventArgs e)
        {
            // Validate that there's content
            if (string.IsNullOrWhiteSpace(textBoxEditor.Text))
            {
                MessageBox.Show(
                    "Vui lòng nhập nội dung văn bản trước khi chèn.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            IsConfirmed = true;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            DialogResult = false;
            Close();
        }

        #endregion
    }
}
