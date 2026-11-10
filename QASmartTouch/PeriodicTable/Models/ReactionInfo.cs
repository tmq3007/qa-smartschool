namespace QASmartTouch.PeriodicTable.Models
{
    public class ReactionInfo
    {
        public string Cation { get; set; }
        public string Anion { get; set; }
        public string Product { get; set; }
        public string Equation { get; set; }
        public string Solubility { get; set; } // "Tan", "Không tan", "Ít tan"
        public string Explanation { get; set; }
        public string PrecipitateColor { get; set; }

        public ReactionInfo(string cation, string anion, string product, string solubility, string precipitateColor)
        {
            Cation = cation;
            Anion = anion;
            Product = product;
            Solubility = solubility;
            PrecipitateColor = precipitateColor;
            GenerateEquation();
            GenerateExplanation();
        }

        private void GenerateEquation()
        {
            // Simplified equation generation
            if (Solubility == "Không tan" || Solubility == "Ít tan")
            {
                Equation = $"{Cation}²⁺ + {Anion}²⁻ → {Product}↓";
            }
            else
            {
                Equation = $"{Cation}²⁺ + {Anion}²⁻ → {Product} (tan)";
            }
        }

        private void GenerateExplanation()
        {
            if (Solubility == "Không tan")
            {
                Explanation = $"{Product} không tan trong nước, tạo kết tủa màu {PrecipitateColor.ToLower()}.";
            }
            else if (Solubility == "Ít tan")
            {
                Explanation = $"{Product} ít tan trong nước, tạo kết tủa nhẹ màu {PrecipitateColor.ToLower()}.";
            }
            else
            {
                Explanation = $"{Product} tan hoàn toàn trong nước, không có kết tủa.";
            }
        }
    }
}

