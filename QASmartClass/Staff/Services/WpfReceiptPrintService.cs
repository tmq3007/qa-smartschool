using System;
using System.Windows;

namespace QASmartClass.Staff.Services
{
    public class WpfReceiptPrintService : IReceiptPrintService
    {
        public void PrintReceipt(string receiptText)
        {
            try
            {
                if (Application.Current != null && Application.Current.MainWindow != null)
                {
                    if (Application.Current.Dispatcher.CheckAccess())
                    {
                        ShowPrintWindow(receiptText);
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() => ShowPrintWindow(receiptText));
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[WpfReceiptPrintService] Print receipt window failed to show");
            }
        }

        private void ShowPrintWindow(string receiptText)
        {
            var printWnd = new Views.CanteenReceiptWindow(receiptText);
            printWnd.Owner = Application.Current.MainWindow;
            printWnd.ShowDialog();
        }
    }
}
