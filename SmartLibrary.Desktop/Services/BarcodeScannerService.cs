using System;
using System.Text;
using System.Windows.Input;

namespace SmartLibrary.Desktop.Services
{
    public class BarcodeScannerService
    {
        private readonly StringBuilder _barcodeBuilder = new StringBuilder();
        private DateTime _lastKeystroke = DateTime.Now;
        private const int MaxMillisecondsBetweenKeystrokes = 25;

        // Event phát ra khi quét thành công
        public event EventHandler<string>? BarcodeScanned;

        public void HookScanner(System.Windows.Window window)
        {
            if (window != null)
            {
                window.PreviewKeyDown += Window_PreviewKeyDown;
            }
        }

        public void UnhookScanner(System.Windows.Window window)
        {
            if (window != null)
            {
                window.PreviewKeyDown -= Window_PreviewKeyDown;
            }
        }

        private void AppendToBarcode(char c)
        {
            if (_barcodeBuilder.Length < 50)
            {
                _barcodeBuilder.Append(c);
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Bỏ qua nếu có phím chức năng/bổ trợ Ctrl, Alt, Windows hoạt động
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows)) != ModifierKeys.None)
            {
                return;
            }

            // Bỏ qua nếu tiêu điểm đang nằm ở một TextBox gõ phím thường, cho phép các TextBox nhập mã vạch/quét chuyên dụng
            if (Keyboard.FocusedElement is System.Windows.Controls.TextBox textBox)
            {
                string name = textBox.Name ?? "";
                bool isBarcodeField = name.Contains("Barcode", StringComparison.OrdinalIgnoreCase) || 
                                       name.Contains("Rfid", StringComparison.OrdinalIgnoreCase) || 
                                       name.Contains("Isbn", StringComparison.OrdinalIgnoreCase) ||
                                       name.Contains("Scan", StringComparison.OrdinalIgnoreCase);

                if (!isBarcodeField)
                {
                    var elapsedScan = (DateTime.Now - _lastKeystroke).TotalMilliseconds;
                    _lastKeystroke = DateTime.Now;
                    
                    if (elapsedScan > 0 && elapsedScan < MaxMillisecondsBetweenKeystrokes)
                    {
                        // Quét tốc độ cao trên TextBox thường -> Đây là súng quét!
                        if (!string.IsNullOrEmpty(textBox.Text))
                        {
                            // 1. Phục hồi ký tự đầu tiên bị gõ nhầm vào TextBox
                            char lastChar = textBox.Text[textBox.Text.Length - 1];
                            textBox.Text = textBox.Text.Substring(0, textBox.Text.Length - 1);
                            textBox.CaretIndex = textBox.Text.Length;
                            
                            _barcodeBuilder.Clear();
                            AppendToBarcode(lastChar);
                        }
                        
                        // 2. Clear focus TextBox để chuyển hướng nhập liệu toàn cục
                        Keyboard.ClearFocus();
                        
                        // 3. Gom ký tự phím hiện tại
                        var scanChar = GetCharFromKey(e.Key);
                        if (scanChar != null)
                        {
                            AppendToBarcode(scanChar.Value);
                        }
                        
                        e.Handled = true;
                        return;
                    }
                    else
                    {
                        _lastKeystroke = DateTime.Now;
                        _barcodeBuilder.Clear();
                        return; // Người dùng gõ tay bình thường
                    }
                }
            }

            // Bỏ qua các phím điều khiển
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift || 
                e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl || 
                e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
            {
                return;
            }

            var elapsed = (DateTime.Now - _lastKeystroke).TotalMilliseconds;
            _lastKeystroke = DateTime.Now;

            // Nếu khoảng cách giữa 2 phím quá lâu, reset chuỗi
            if (elapsed > MaxMillisecondsBetweenKeystrokes)
            {
                _barcodeBuilder.Clear();
            }

            // Nếu là phím Enter, kiểm tra chuỗi có hợp lệ không
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (_barcodeBuilder.Length > 0)
                {
                    var barcode = _barcodeBuilder.ToString();
                    _barcodeBuilder.Clear();
                    
                    // Phát sự kiện
                    BarcodeScanned?.Invoke(this, barcode);
                    
                    // Ngăn sự kiện Enter lan ra các control khác nếu cần thiết (tùy chọn)
                    e.Handled = true; 
                }
                return;
            }

            // Chuyển phím thành ký tự chữ/số
            var keyChar = GetCharFromKey(e.Key);
            if (keyChar != null)
            {
                AppendToBarcode(keyChar.Value);
            }
        }

        private char? GetCharFromKey(Key key)
        {
            bool isShift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            if (key >= Key.A && key <= Key.Z)
            {
                bool isCapsLock = Keyboard.IsKeyToggled(Key.CapsLock);
                bool isUpper = isShift ^ isCapsLock;
                char baseChar = key.ToString()[0];
                return isUpper ? baseChar : char.ToLower(baseChar);
            }
            
            if (key >= Key.D0 && key <= Key.D9)
                return (char)('0' + (key - Key.D0));
                
            if (key >= Key.NumPad0 && key <= Key.NumPad9)
                return (char)('0' + (key - Key.NumPad0));

            // Bổ sung ký tự dấu trừ (-) và gạch chéo (/)
            if (key == Key.OemMinus || key == Key.Subtract)
            {
                if (key == Key.OemMinus)
                {
                    return isShift ? '_' : '-';
                }
                return '-';
            }
            if (key == Key.OemQuestion || key == Key.Divide)
                return '/';

            // Bổ sung phím cách và dấu chấm
            if (key == Key.Space)
                return ' ';
            if (key == Key.OemPeriod || key == Key.Decimal)
                return '.';

            return null;
        }
    }
}
