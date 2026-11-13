using System;
using System.Windows;
using System.Windows.Controls;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_6_SubMenuSelectionRecognition : Window
    {
        private Form2_MainDashboard _mainDashboard;

        public Form2_6_SubMenuSelectionRecognition(Form2_MainDashboard mainDashboard)
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToSubMenu(this, btnClose);
            if (btnClose != null)
            {
                System.Windows.Input.Stylus.SetIsPressAndHoldEnabled(btnClose, false);

                btnClose.PreviewTouchDown += (s, e) =>
                {
                    e.TouchDevice.Capture(btnClose);
                    e.Handled = true;
                };

                btnClose.PreviewTouchUp += (s, e) =>
                {
                    if (e.TouchDevice.Captured == btnClose)
                    {
                        btnClose.ReleaseTouchCapture(e.TouchDevice);
                    }
                    e.Handled = true;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try 
                        {
                            this.Owner?.Activate();
                            this.Close();
                        } 
                        catch { }
                    }), System.Windows.Threading.DispatcherPriority.Normal);
                };

                btnClose.PreviewStylusDown += (s, e) =>
                {
                    e.StylusDevice.Capture(btnClose);
                    e.Handled = true;
                };

                btnClose.PreviewStylusUp += (s, e) =>
                {
                    if (e.StylusDevice.Captured == btnClose)
                    {
                        btnClose.ReleaseStylusCapture();
                    }
                    e.Handled = true;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try 
                        {
                            this.Owner?.Activate();
                            this.Close();
                        } 
                        catch { }
                    }), System.Windows.Threading.DispatcherPriority.Normal);
                };
            }
            _mainDashboard = mainDashboard;
            if (btnSelectRect != null) WireTouchActivation(btnSelectRect, btnSelectRect_Click);
            if (btnSelectLasso != null) WireTouchActivation(btnSelectLasso, btnSelectLasso_Click);
            if (btnSelectMagic != null) WireTouchActivation(btnSelectMagic, btnSelectMagic_Click);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            try { this.Owner?.Activate(); } catch { }
            this.Close();
        }

        // Selection Tools
        private void btnSelectRect_Click(object sender, RoutedEventArgs e)
        {
            _mainDashboard.ActivateRectangleSelectionMode();
            try { _mainDashboard?.Activate(); _mainDashboard?.Focus(); } catch { }
            this.Close();
        }

        private void btnSelectLasso_Click(object sender, RoutedEventArgs e)
        {
            _mainDashboard.ActivateLassoSelectionMode();
            try { _mainDashboard?.Activate(); _mainDashboard?.Focus(); } catch { }
            this.Close();
        }

        private void btnSelectMagic_Click(object sender, RoutedEventArgs e)
        {
            _mainDashboard.ActivateMagicWandSelectionMode();
            try { _mainDashboard?.Activate(); _mainDashboard?.Focus(); } catch { }
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

        private void WireTouchActivation(Button button, RoutedEventHandler clickHandler)
        {
            if (button == null) return;
            button.Focusable = false;
            System.Windows.Input.Stylus.SetIsPressAndHoldEnabled(button, false);

            button.PreviewTouchDown += (s, e) =>
            {
                e.TouchDevice.Capture(button);
                e.Handled = true;
            };

            button.PreviewTouchUp += (s, e) =>
            {
                if (e.TouchDevice.Captured == button)
                {
                    button.ReleaseTouchCapture(e.TouchDevice);
                    try
                    {
                        var pos = e.GetTouchPoint(button).Position;
                        if (pos.X >= 0 && pos.X <= button.ActualWidth &&
                            pos.Y >= 0 && pos.Y <= button.ActualHeight)
                        {
                            clickHandler(button, new RoutedEventArgs(Button.ClickEvent, button));
                        }
                    }
                    catch
                    {
                        clickHandler(button, new RoutedEventArgs(Button.ClickEvent, button));
                    }
                }
                e.Handled = true;
            };

            button.PreviewStylusDown += (s, e) =>
            {
                e.StylusDevice.Capture(button);
                e.Handled = true;
            };

            button.PreviewStylusUp += (s, e) =>
            {
                if (e.StylusDevice.Captured == button)
                {
                    button.ReleaseStylusCapture();
                    try
                    {
                        var pos = e.GetPosition(button);
                        if (pos.X >= 0 && pos.X <= button.ActualWidth &&
                            pos.Y >= 0 && pos.Y <= button.ActualHeight)
                        {
                            clickHandler(button, new RoutedEventArgs(Button.ClickEvent, button));
                        }
                    }
                    catch
                    {
                        clickHandler(button, new RoutedEventArgs(Button.ClickEvent, button));
                    }
                }
                e.Handled = true;
            };
        }
    }
}
