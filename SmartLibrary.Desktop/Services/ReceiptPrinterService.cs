using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SmartLibrary.Desktop.Services
{
    public class PrinterException : Exception
    {
        public PrinterException(string message) : base(message) { }
    }

    public class ReceiptPrinterService
    {
        public async Task PrintLoanReceiptAsync(string studentName, string studentId, List<string> bookTitles, string transactionId = "", int printerWidth = 48)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string errorTriggerFile = Path.Combine(appDataPath, "SmartLibrary", "simulate_printer_error.txt");
            if (File.Exists(errorTriggerFile))
            {
                throw new PrinterException("Máy in Zebra hết tem nhãn hoặc bị kẹt giấy!");
            }

            try
            {
                // Khổ giấy động dựa trên printerWidth
                var sb = new StringBuilder();
                string separator = new string('-', printerWidth);
                sb.AppendLine(separator);
                sb.AppendLine(CenterText("TRƯỜNG TIỂU HỌC VÀ THCS QA SMART SCHOOL", printerWidth));
                sb.AppendLine(CenterText("SMART LIBRARY - THƯ VIỆN 4.0", printerWidth));
                sb.AppendLine(CenterText("PHIẾU MƯỢN SÁCH TỰ ĐỘNG", printerWidth));
                sb.AppendLine(separator);
                sb.AppendLine($"Ngày in : {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");
                sb.AppendLine($"Độc giả : {studentName}");
                sb.AppendLine($"Mã thẻ  : {studentId}");
                if (!string.IsNullOrEmpty(transactionId))
                {
                    sb.AppendLine($"Mã GD   : {transactionId}");
                }
                sb.AppendLine(separator);
                sb.AppendLine("Danh sách sách mượn:");
                
                int index = 1;
                foreach (var title in bookTitles)
                {
                    sb.AppendLine($"{index}. {title}");
                    index++;
                }

                sb.AppendLine(separator);
                sb.AppendLine($"Tổng số lượng: {bookTitles.Count} cuốn");
                sb.AppendLine("Hạn trả: 14 ngày kể từ ngày mượn.");
                sb.AppendLine(" ");
                sb.AppendLine(CenterText("Vui lòng giữ phiếu này để đối chiếu.", printerWidth));
                sb.AppendLine(CenterText("XIN CẢM ƠN QUÝ ĐỘC GIẢ", printerWidth));
                if (!string.IsNullOrEmpty(transactionId))
                {
                    sb.AppendLine(separator);
                    sb.AppendLine("MÃ QR ĐỐI SOÁT GIAO DỊCH:");
                    sb.AppendLine($"[QR_CODE_START]{transactionId}[QR_CODE_END]");
                }
                sb.AppendLine(separator);
                sb.AppendLine("\n\n\n\n"); // Lệnh cắt giấy mô phỏng

                // Ghi ra file Text trong thư mục receipts tương đối
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string receiptsDir = Path.Combine(appData, "SmartLibrary", "receipts");
                if (!Directory.Exists(receiptsDir))
                {
                    Directory.CreateDirectory(receiptsDir);
                }
                string fileName = $"Receipt_Loan_{studentId}_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt";
                string fullPath = Path.Combine(receiptsDir, fileName);

                await File.WriteAllTextAsync(fullPath, sb.ToString(), Encoding.UTF8);
                
                // Thực hiện gửi lệnh in tới driver máy in thật của Windows
                SendToPrinter(sb.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi máy in: {ex.Message}");
                await AuditLogService.WriteLogAsync("PrinterError", $"Lỗi in phiếu mượn tự động: {ex.Message}", false);
            }
        }

        public async Task PrintFineReceiptAsync(string studentName, string ssoId, double amountPaid, double remainingFine, int printerWidth = 48)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string errorTriggerFile = Path.Combine(appDataPath, "SmartLibrary", "simulate_printer_error.txt");
            if (File.Exists(errorTriggerFile))
            {
                throw new PrinterException("Máy in Zebra hết tem nhãn hoặc bị kẹt giấy!");
            }

            try
            {
                var sb = new StringBuilder();
                string separator = new string('-', printerWidth);
                sb.AppendLine(separator);
                sb.AppendLine(CenterText("TRƯỜNG TIỂU HỌC VÀ THCS QA SMART SCHOOL", printerWidth));
                sb.AppendLine(CenterText("SMART LIBRARY - THƯ VIỆN 4.0", printerWidth));
                sb.AppendLine(CenterText("BIÊN LAI THU TIỀN PHẠT", printerWidth));
                sb.AppendLine(separator);
                sb.AppendLine($"Ngày in : {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");
                sb.AppendLine($"Độc giả : {studentName}");
                sb.AppendLine($"Mã thẻ  : {ssoId}");
                sb.AppendLine(separator);
                sb.AppendLine($"Số tiền đã đóng   : {amountPaid:N0} VNĐ");
                sb.AppendLine($"Số tiền nợ còn lại : {remainingFine:N0} VNĐ");
                sb.AppendLine(separator);
                sb.AppendLine("Thủ thư xác nhận đã thu đủ tiền mặt.");
                sb.AppendLine("Cảm ơn bạn đã hợp tác giữ gìn thư viện!");
                sb.AppendLine(separator);
                sb.AppendLine("\n\n\n\n"); // Cut paper feed simulation

                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string receiptsDir = Path.Combine(appData, "SmartLibrary", "receipts");
                if (!Directory.Exists(receiptsDir))
                {
                    Directory.CreateDirectory(receiptsDir);
                }
                string fileName = $"Receipt_Fine_{ssoId}_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt";
                string fullPath = Path.Combine(receiptsDir, fileName);

                await File.WriteAllTextAsync(fullPath, sb.ToString(), Encoding.UTF8);

                // Thực hiện gửi lệnh in tới driver máy in thật của Windows
                SendToPrinter(sb.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi máy in biên lai phạt: {ex.Message}");
                await AuditLogService.WriteLogAsync("PrinterError", $"Lỗi in biên lai thu phạt: {ex.Message}", false);
            }
        }

        private string CenterText(string text, int width)
        {
            if (string.IsNullOrEmpty(text)) return new string(' ', width);
            if (text.Length >= width) return text.Substring(0, width);
            int leftSpaces = (width - text.Length) / 2;
            return new string(' ', leftSpaces) + text;
        }

        private void SendToPrinter(string text)
        {
            try
            {
                using (var pd = new System.Drawing.Printing.PrintDocument())
                {
                    pd.PrintPage += (sender, ev) =>
                    {
                        if (ev.Graphics == null) return;

                        using (var font = new System.Drawing.Font("Courier New", 9))
                        {
                            float yPos = 0;
                            int count = 0;
                            float leftMargin = ev.MarginBounds.Left;
                            float topMargin = ev.MarginBounds.Top;
                            string? line = null;

                            using (var reader = new System.IO.StringReader(text))
                            {
                                while ((line = reader.ReadLine()) != null)
                                {
                                    yPos = topMargin + (count * font.GetHeight(ev.Graphics));
                                    ev.Graphics.DrawString(line, font, System.Drawing.Brushes.Black, leftMargin, yPos, new System.Drawing.StringFormat());
                                    count++;
                                }
                            }
                        }
                    };
                    pd.Print();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send document to default printer driver: {ex.Message}");
            }
        }
    }
}
