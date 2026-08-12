using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using System.IO;
using System.Text.RegularExpressions;

namespace QASmartClass.Services
{
    public class SensitiveDataMaskingFormatter : ITextFormatter
    {
        private readonly MessageTemplateTextFormatter _innerFormatter;

        public SensitiveDataMaskingFormatter(string outputTemplate)
        {
            _innerFormatter = new MessageTemplateTextFormatter(outputTemplate, null);
        }

        public void Format(LogEvent logEvent, TextWriter output)
        {
            using (var sw = new StringWriter())
            {
                _innerFormatter.Format(logEvent, sw);
                string formatted = sw.ToString();

                // 1. Masking CCCD (12 consecutive digits, e.g. 123456789012 -> XXXXXXXX9012)
                formatted = Regex.Replace(formatted, @"\d{8}(\d{4})", "XXXXXXXX$1");
                formatted = Regex.Replace(formatted, @"(?i)(""?)(CCCD|ParentPhone|CitizenId|CitizenID)(""?)(\s*[:=]?\s*)(""?)(\d{8})(\d{4})(""?)", "$1$2$3$4$5XXXXXXXX$7$8");

                // 2. Masking RFID hex string (8 hex chars, e.g. 7E4A8F9C -> 7E4AXXXX)
                formatted = Regex.Replace(formatted, @"([0-9A-Fa-f]{4})[0-9A-Fa-f]{4}", "$1XXXX");
                formatted = Regex.Replace(formatted, @"(?i)(""?)(RFID|CardNumber|RFIDHex)(""?)(\s*[:=]?\s*)(""?)([0-9A-Fa-f]{4})([0-9A-Fa-f]{4})(""?)", "$1$2$3$4$5$6XXXX$8");

                // 3. Masking Wallet balance or POS balances (e.g. số dư ví: 50,000 -> số dư ví: XX,XXX)
                formatted = Regex.Replace(formatted, @"(?i)(""?)(số dư ví|WalletBalance|số dư|Hạn mức ví|hạn mức|số dư tài khoản|Balance|Amount|SoDu)(""?)(\s*[:=]?\s*)(""?)([\d,.]+)(""?)", m => {
                    string q1 = m.Groups[1].Value;
                    string key = m.Groups[2].Value;
                    string q2 = m.Groups[3].Value;
                    string sep = m.Groups[4].Value;
                    string q3 = m.Groups[5].Value;
                    string val = m.Groups[6].Value;
                    string q4 = m.Groups[7].Value;
                    
                    string maskedVal = Regex.Replace(val, @"\d", "X");
                    return $"{q1}{key}{q2}{sep}{q3}{maskedVal}{q4}";
                });

                output.Write(formatted);
            }
        }
    }
}
