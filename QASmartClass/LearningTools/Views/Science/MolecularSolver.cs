using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QASmartClass.LearningTools.Views.Science
{
    public enum MutationType
    {
        None,
        Silent,       // Đồng nghĩa
        Missense,     // Sai nghĩa
        Nonsense,     // Vô nghĩa
        Frameshift,   // Dịch khung
        InFrameIndel, // Thêm hoặc mất bộ ba không dịch khung
        NonStop       // Mất mã kết thúc
    }

    public class MutationAnalysisResult
    {
        public MutationType Type { get; set; } = MutationType.None;
        public string Description { get; set; } = "";
        public int OriginalBonds { get; set; }
        public int MutatedBonds { get; set; }
        public int BondsDelta { get; set; }
        public List<int> ChangedAminoAcidIndices { get; set; } = new();
        public string DetailExplanation { get; set; } = "";
    }

    public static class MolecularSolver
    {
        // Bảng mã di truyền chuẩn SGK Việt Nam (sử dụng X thay cho C)
        private static readonly Dictionary<string, string> CodonTable = new(StringComparer.OrdinalIgnoreCase)
        {
            { "UUU", "Phe" }, { "UUX", "Phe" },
            { "UUA", "Leu" }, { "UUG", "Leu" },
            { "UXU", "Ser" }, { "UXX", "Ser" }, { "UXA", "Ser" }, { "UXG", "Ser" },
            { "UAU", "Tyr" }, { "UAX", "Tyr" },
            { "UAA", "Stop" }, { "UAG", "Stop" }, { "UGA", "Stop" }, // Mã kết thúc
            { "UGU", "Cys" }, { "UGX", "Cys" },
            { "UGG", "Trp" },
            { "XUU", "Leu" }, { "XUX", "Leu" }, { "XUA", "Leu" }, { "XUG", "Leu" },
            { "XXU", "Pro" }, { "XXX", "Pro" }, { "XXA", "Pro" }, { "XXG", "Pro" },
            { "XAU", "His" }, { "XAX", "His" },
            { "XAA", "Gln" }, { "XAG", "Gln" },
            { "XGU", "Arg" }, { "XGX", "Arg" }, { "XGA", "Arg" }, { "XGG", "Arg" },
            { "AUU", "Ile" }, { "AUX", "Ile" }, { "AUA", "Ile" },
            { "AUG", "Met" }, // Mã mở đầu
            { "AXU", "Thr" }, { "AXX", "Thr" }, { "AXA", "Thr" }, { "AXG", "Thr" },
            { "AAU", "Asn" }, { "AAX", "Asn" },
            { "AAA", "Lys" }, { "AAG", "Lys" },
            { "AGU", "Ser" }, { "AGX", "Ser" },
            { "AGA", "Arg" }, { "AGG", "Arg" },
            { "GUU", "Val" }, { "GUX", "Val" }, { "GUA", "Val" }, { "GUG", "Val" },
            { "GXU", "Ala" }, { "GXX", "Ala" }, { "GXA", "Ala" }, { "GXG", "Ala" },
            { "GAU", "Asp" }, { "GAX", "Asp" },
            { "GAA", "Glu" }, { "GAG", "Glu" },
            { "GGU", "Gly" }, { "GGX", "Gly" }, { "GGA", "Gly" }, { "GGG", "Gly" }
        };

        public static string NormalizeDNA(string dna)
        {
            if (string.IsNullOrWhiteSpace(dna)) return "";
            string normalized = dna.Trim().ToUpperInvariant().Replace("C", "X");
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                if (c == 'A' || c == 'T' || c == 'G' || c == 'X')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string GetComplementaryDNA(string templateDna)
        {
            string normalized = NormalizeDNA(templateDna);
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                switch (c)
                {
                    case 'T': sb.Append('A'); break;
                    case 'A': sb.Append('T'); break;
                    case 'X': sb.Append('G'); break;
                    case 'G': sb.Append('X'); break;
                }
            }
            return sb.ToString();
        }

        public static string Transcribe(string templateDna)
        {
            string normalized = NormalizeDNA(templateDna);
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                switch (c)
                {
                    case 'T': sb.Append('A'); break;
                    case 'A': sb.Append('U'); break;
                    case 'X': sb.Append('G'); break;
                    case 'G': sb.Append('X'); break;
                }
            }
            return sb.ToString();
        }

        public static List<string> Translate(string mrna, bool requireStartCodon = false)
        {
            var aminoAcids = new List<string>();
            if (string.IsNullOrEmpty(mrna)) return aminoAcids;

            int startIdx = 0;
            if (requireStartCodon)
            {
                startIdx = -1;
                for (int i = 0; i <= mrna.Length - 3; i++)
                {
                    if (mrna.Substring(i, 3).Equals("AUG", StringComparison.OrdinalIgnoreCase))
                    {
                        startIdx = i;
                        break;
                    }
                }
                if (startIdx == -1) return aminoAcids;
            }

            for (int i = startIdx; i <= mrna.Length - 3; i += 3)
            {
                string codon = mrna.Substring(i, 3).ToUpperInvariant();
                if (CodonTable.TryGetValue(codon, out string aa))
                {
                    if (aa == "Stop") break;
                    aminoAcids.Add(aa);
                }
                else
                {
                    aminoAcids.Add("?");
                }
            }
            return aminoAcids;
        }

        public static int CalculateHydrogenBonds(string dna)
        {
            string normalized = NormalizeDNA(dna);
            int bonds = 0;
            foreach (char c in normalized)
            {
                if (c == 'A' || c == 'T') bonds += 2;
                else if (c == 'G' || c == 'X') bonds += 3;
            }
            return bonds;
        }

        public static bool IsValidDNA(string dna, out string errorMessage)
        {
            errorMessage = "";
            if (string.IsNullOrWhiteSpace(dna))
            {
                errorMessage = "Chuỗi DNA không được để trống.";
                return false;
            }

            string clean = dna.Replace(" ", "");
            if (clean.Length < 3)
            {
                errorMessage = "Độ dài chuỗi DNA quá ngắn (tối thiểu 3 nucleotit).";
                return false;
            }
            if (clean.Length > 120)
            {
                errorMessage = "Độ dài chuỗi vượt quá giới hạn (tối đa 120 nucleotit).";
                return false;
            }

            foreach (char c in clean.ToUpperInvariant())
            {
                if (c != 'A' && c != 'T' && c != 'G' && c != 'X' && c != 'C')
                {
                    errorMessage = $"Ký tự '{c}' không hợp lệ. Chỉ chấp nhận các nucleotit: A, T, G, X (hoặc C).";
                    return false;
                }
            }

            return true;
        }

        public static string GetAminoAcidFullName(string code)
        {
            if (string.IsNullOrEmpty(code)) return "";
            return code.ToUpperInvariant() switch
            {
                "MET" => "Mêtiônin (Mã mở đầu)",
                "PHE" => "Phênylalanin",
                "LEU" => "Lêuxin",
                "SER" => "Sêrin",
                "TYR" => "Tirôzin",
                "CYS" => "Xistêin",
                "TRP" => "Triptôphan",
                "PRO" => "Prôlin",
                "HIS" => "Histidin",
                "GLN" => "Glutamin",
                "ASN" => "Asparagin",
                "LYS" => "Lizin",
                "ASP" => "Axit Aspartic",
                "GLU" => "Axit Glutamic",
                "VAL" => "Valin",
                "ILE" => "Izôlêuxin",
                "THR" => "Thrêônin",
                "ALA" => "Alanin",
                "GLY" => "Glixin",
                "ARG" => "Arginin",
                "STOP" => "Mã kết thúc",
                _ => code
            };
        }

        private static string GetPairName(char baseChar)
        {
            return baseChar switch
            {
                'A' => "A-T",
                'T' => "T-A",
                'G' => "G-X",
                'X' => "X-G",
                _ => baseChar.ToString()
            };
        }

        public static MutationAnalysisResult AnalyzeMutation(string wild, string mutant)
        {
            var result = new MutationAnalysisResult();
            string normWild = NormalizeDNA(wild);
            string normMutant = NormalizeDNA(mutant);

            result.OriginalBonds = CalculateHydrogenBonds(normWild);
            result.MutatedBonds = CalculateHydrogenBonds(normMutant);
            result.BondsDelta = result.MutatedBonds - result.OriginalBonds;

            if (normWild == normMutant)
            {
                result.Type = MutationType.None;
                result.Description = "Không có đột biến.";
                result.DetailExplanation = "Mạch DNA gốc và mạch đột biến trùng khớp 100%.";
                return result;
            }

            // Dịch mã sang chuỗi axit amin
            var aaWild = Translate(Transcribe(normWild));
            var aaMutant = Translate(Transcribe(normMutant));

            // So sánh độ dài để xác định đột biến dịch khung hoặc thêm/mất bộ ba
            if (normWild.Length != normMutant.Length)
            {
                int diffCount = System.Math.Abs(normMutant.Length - normWild.Length);
                string typeStr = normMutant.Length > normWild.Length ? "thêm" : "mất";
                string deltaStr = result.BondsDelta >= 0 ? $"+{result.BondsDelta}" : $"{result.BondsDelta}";

                if (diffCount % 3 != 0)
                {
                    result.Type = MutationType.Frameshift;
                    result.Description = "Đột biến dịch khung (Frameshift mutation).";
                    result.DetailExplanation = $"Đột biến dịch khung do **{typeStr} {diffCount} nucleotit** làm lệch khung đọc của các bộ ba sau đó.\nTổng số liên kết hyđrô thay đổi: **{result.OriginalBonds} → {result.MutatedBonds}** ({deltaStr} liên kết).";
                }
                else
                {
                    result.Type = MutationType.InFrameIndel;
                    int codonDiff = diffCount / 3;
                    result.Description = $"Đột biến {typeStr} bộ ba (Không dịch khung).";
                    result.DetailExplanation = $"Đột biến {typeStr} {codonDiff} bộ ba nucleotit (**{diffCount} nucleotit**), làm {typeStr} tương ứng **{codonDiff} axit amin** trong chuỗi pôlipeptit mà không làm dịch khung đọc.\nTổng số liên kết hyđrô thay đổi: **{result.OriginalBonds} → {result.MutatedBonds}** ({deltaStr} liên kết).";
                }
                return result;
            }

            // Đột biến thay thế cặp nucleotit (độ dài bằng nhau)
            var diffIndices = new List<int>();
            for (int i = 0; i < normWild.Length; i++)
            {
                if (normWild[i] != normMutant[i]) diffIndices.Add(i);
            }

            // Xác định các chỉ số axit amin bị thay đổi
            int maxAaLen = System.Math.Max(aaWild.Count, aaMutant.Count);
            for (int i = 0; i < maxAaLen; i++)
            {
                if (i >= aaWild.Count || i >= aaMutant.Count || aaWild[i] != aaMutant[i])
                {
                    result.ChangedAminoAcidIndices.Add(i);
                }
            }

            string deltaSign = result.BondsDelta >= 0 ? "+" : "";
            string bondDetail = $"Tổng số liên kết hyđrô thay đổi: **{result.OriginalBonds} → {result.MutatedBonds}** ({deltaSign}{result.BondsDelta} liên kết)";

            if (result.ChangedAminoAcidIndices.Count == 0)
            {
                result.Type = MutationType.Silent;
                result.Description = "Đột biến đồng nghĩa (Silent mutation).";
                result.DetailExplanation = $"Thay thế cặp nucleotit xảy ra ở vị trí thoái hóa của mã di truyền, **không làm thay đổi** axit amin.\n{bondDetail}";
            }
            else
            {
                if (aaMutant.Count < aaWild.Count)
                {
                    result.Type = MutationType.Nonsense;
                    result.Description = "Đột biến vô nghĩa (Nonsense mutation).";
                    result.DetailExplanation = $"Thay thế cặp nucleotit làm biến đổi bộ ba mã hóa thành **bộ ba kết thúc sớm (Stop)**, làm ngắn chuỗi prôtêin.\n{bondDetail}";
                }
                else if (aaMutant.Count > aaWild.Count)
                {
                    result.Type = MutationType.NonStop;
                    result.Description = "Đột biến mất mã kết thúc (Nonstop mutation).";
                    result.DetailExplanation = $"Thay thế cặp nucleotit làm biến đổi bộ ba kết thúc thành **bộ ba mã hóa axit amin**, làm kéo dài chuỗi prôtêin.\n{bondDetail}";
                }
                else
                {
                    result.Type = MutationType.Missense;
                    result.Description = "Đột biến sai nghĩa (Missense mutation).";
                    var changes = new List<string>();
                    foreach (int idx in result.ChangedAminoAcidIndices)
                    {
                        string from = idx < aaWild.Count ? aaWild[idx] : "Không có";
                        string to = idx < aaMutant.Count ? aaMutant[idx] : "Không có";
                        changes.Add($"axit amin thứ **{idx + 1}** từ **{from}** thành **{to}**");
                    }
                    result.DetailExplanation = $"Thay thế cặp nucleotit dẫn đến thay đổi: {string.Join(", ", changes)}.\n{bondDetail}";
                }
            }

            return result;
        }
    }
}
