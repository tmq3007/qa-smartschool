using System;
using System.Windows;
using System.Windows.Controls;

namespace QASmartTouch.Forms
{
    public partial class Form2_6_SubMenuSelectionRecognition : Window
    {
        private Form2_MainDashboard _mainDashboard;

        public Form2_6_SubMenuSelectionRecognition(Form2_MainDashboard mainDashboard)
        {
            InitializeComponent();
            if (btnClose != null)
            {
                btnClose.PreviewTouchDown += (s, e) => { this.Close(); e.Handled = true; };
                btnClose.PreviewStylusDown += (s, e) => { this.Close(); e.Handled = true; };
            }
            _mainDashboard = mainDashboard;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Selection Tools
        private void btnSelectRect_Click(object sender, RoutedEventArgs e)
        {
            _mainDashboard.ActivateRectangleSelectionMode();
            this.Close();
        }

        private void btnSelectLasso_Click(object sender, RoutedEventArgs e)
        {
            _mainDashboard.ActivateLassoSelectionMode();
            this.Close();
        }

        private void btnSelectMagic_Click(object sender, RoutedEventArgs e)
        {
            _mainDashboard.ActivateMagicWandSelectionMode();
            this.Close();
        }

        // Recognition Features
        private async void btnOCR_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            await _mainDashboard.ExecuteSmartActionAsync(SmartActionType.OCR, SelectedText);
        }

        private async void btnTranslate_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            await _mainDashboard.ExecuteSmartActionAsync(SmartActionType.Translate, SelectedText);
        }

        private async void btnTextToSpeech_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            await _mainDashboard.ExecuteSmartActionAsync(SmartActionType.Read, SelectedText);
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            await _mainDashboard.ExecuteSmartActionAsync(SmartActionType.Search, SelectedText);
        }

        public string SelectedText { get; set; } = string.Empty;

        private async void btnAIChat_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            await _mainDashboard.ExecuteSmartActionAsync(SmartActionType.AIChat, SelectedText);
        }
    }
}
