using System.ComponentModel;

namespace QASmartTouch.PeriodicTable.Models
{
    public class MetalReaction : INotifyPropertyChanged
    {
        private bool _isHighlighted;

        public string Symbol { get; set; }
        public string Name { get; set; }
        public string ColdWater { get; set; }
        public string HotWater { get; set; }
        public string Acid { get; set; }
        public int Order { get; set; } // Thứ tự trong dãy hoạt động

        // Mức độ phản ứng (dùng để so sánh)
        public int ColdWaterLevel { get; set; }
        public int HotWaterLevel { get; set; }
        public int AcidLevel { get; set; }

        // Màu sắc hiển thị (Background)
        public string ColdWaterColor { get; set; }
        public string HotWaterColor { get; set; }
        public string AcidColor { get; set; }

        // Màu text (Foreground - cho contrast tốt)
        public string ColdWaterTextColor { get; set; }
        public string HotWaterTextColor { get; set; }
        public string AcidTextColor { get; set; }

        // Biểu tượng
        public string ColdWaterIcon { get; set; }
        public string HotWaterIcon { get; set; }
        public string AcidIcon { get; set; }

        // Chi tiết phản ứng (PTHH, hiện tượng)
        public string ColdWaterEquation { get; set; }
        public string HotWaterEquation { get; set; }
        public string AcidEquation { get; set; }

        public string ColdWaterPhenomenon { get; set; }
        public string HotWaterPhenomenon { get; set; }
        public string AcidPhenomenon { get; set; }

        public string Note { get; set; } // Ghi chú, mẹo ghi nhớ

        public bool IsHighlighted
        {
            get => _isHighlighted;
            set
            {
                _isHighlighted = value;
                OnPropertyChanged(nameof(IsHighlighted));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Helper method để get màu theo reaction text (Pastel Colors)
        public static string GetColorFromReaction(string reaction)
        {
            if (string.IsNullOrEmpty(reaction)) return "#F5F5F5";
            
            if (reaction.Contains("Dữ dội") || reaction.Contains("dữ dội"))
                return "#FFE5E5"; // Đỏ pastel nhẹ
            if (reaction.Contains("Mạnh") || reaction.Contains("mạnh"))
                return "#FFF4E6"; // Cam pastel nhẹ
            if (reaction.Contains("Vừa phải") || reaction.Contains("vừa phải"))
                return "#FFFBEA"; // Vàng pastel nhẹ
            if (reaction.Contains("Chậm") || reaction.Contains("chậm") || reaction.Contains("Rất chậm"))
                return "#F5F0EB"; // Be xám nhẹ
            if (reaction.Contains("Không") || reaction.Contains("không"))
                return "#F5F5F5"; // Xám nhạt
            if (reaction.Contains("—") || reaction == "Mốc so sánh")
                return "#E3F2FD"; // Xanh pastel nhẹ (Hiđrô)
            
            return "#F5F5F5";
        }

        // Helper method để get text color (màu đậm cho contrast tốt)
        public static string GetTextColorFromReaction(string reaction)
        {
            if (string.IsNullOrEmpty(reaction)) return "#616161";
            
            if (reaction.Contains("Dữ dội") || reaction.Contains("dữ dội"))
                return "#B71C1C"; // Đỏ đậm
            if (reaction.Contains("Mạnh") || reaction.Contains("mạnh"))
                return "#E65100"; // Cam đậm
            if (reaction.Contains("Vừa phải") || reaction.Contains("vừa phải"))
                return "#F57F17"; // Vàng đậm
            if (reaction.Contains("Chậm") || reaction.Contains("chậm") || reaction.Contains("Rất chậm"))
                return "#4E342E"; // Nâu đậm
            if (reaction.Contains("Không") || reaction.Contains("không"))
                return "#616161"; // Xám đậm
            if (reaction.Contains("—") || reaction == "Mốc so sánh")
                return "#0D47A1"; // Xanh đậm (Hiđrô)
            
            return "#616161";
        }

        // Helper method để get icon theo reaction text
        public static string GetIconFromReaction(string reaction)
        {
            if (string.IsNullOrEmpty(reaction)) return "🚫";
            
            if (reaction.Contains("Dữ dội") || reaction.Contains("dữ dội"))
                return "⚡";
            if (reaction.Contains("Mạnh") || reaction.Contains("mạnh"))
                return "🔥";
            if (reaction.Contains("Vừa phải") || reaction.Contains("vừa phải"))
                return "💥";
            if (reaction.Contains("Chậm") || reaction.Contains("chậm"))
                return "🌡️";
            if (reaction.Contains("Không") || reaction.Contains("không"))
                return "❌";
            if (reaction.Contains("—") || reaction == "Mốc so sánh")
                return "⚛️";
            
            return "❓";
        }

        // Helper method để get level số (dùng để so sánh)
        public static int GetLevelFromReaction(string reaction)
        {
            if (string.IsNullOrEmpty(reaction)) return 0;
            
            if (reaction.Contains("Dữ dội")) return 5;
            if (reaction.Contains("Mạnh")) return 4;
            if (reaction.Contains("Vừa phải")) return 3;
            if (reaction.Contains("Chậm")) return 2;
            if (reaction.Contains("Rất chậm")) return 1;
            if (reaction.Contains("Không")) return 0;
            if (reaction.Contains("—")) return -1; // Hiđrô (mốc)
            
            return 0;
        }
    }
}

