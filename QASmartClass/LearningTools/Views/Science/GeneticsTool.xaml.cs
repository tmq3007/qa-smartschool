using QASmartClass.LearningTools.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Science
{
    public partial class GeneticsTool : BaseToolControl
    {
        public GeneticsTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextSimulator != null) menuTextSimulator.Text = isVN ? "Mô phỏng & Lai Mendel" : "Simulator & Mendel Crossing";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                BuildPresets();
                Calc();
                TouchTextPad.Attach(txtFather, mode: "text");
                TouchTextPad.Attach(txtMother, mode: "text");
                LoadPracticalApps();
            };
        }

        private void Calc_Changed(object sender, EventArgs e) { if (IsLoaded) Calc(); }

        private void Calc()
        {
            resultPanel.Children.Clear();
            punnettGrid.Children.Clear();
            punnettGrid.RowDefinitions.Clear();
            punnettGrid.ColumnDefinitions.Clear();

            string father = txtFather?.Text?.Trim() ?? "";
            string mother = txtMother?.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(father) || string.IsNullOrEmpty(mother)) return;

            string fatherClean = System.Text.RegularExpressions.Regex.Replace(father, @"\s+", "");
            string motherClean = System.Text.RegularExpressions.Regex.Replace(mother, @"\s+", "");

            if (fatherClean.Length == 1 || fatherClean.Length == 3)
            {
                UI.ResultRow("💬 Bố: Đang nhập kiểu gen...", "#757575", resultPanel);
                return;
            }
            if (motherClean.Length == 1 || motherClean.Length == 3)
            {
                UI.ResultRow("💬 Mẹ: Đang nhập kiểu gen...", "#757575", resultPanel);
                return;
            }

            // Validate genotypes
            if (!IsValidGenotype(father, out string errF))
            {
                UI.ResultRow($"⚠️ Bố: {errF}", "#C62828", resultPanel);
                return;
            }
            if (!IsValidGenotype(mother, out string errM))
            {
                UI.ResultRow($"⚠️ Mẹ: {errM}", "#C62828", resultPanel);
                return;
            }

            father = NormalizeGenotype(father);
            mother = NormalizeGenotype(mother);

            if (father.Length != mother.Length) { UI.ResultRow("Độ dài kiểu gen phải bằng nhau", "#C62828", resultPanel); return; }

            // Get gametes
            var fGametes = GetGametes(father);
            var mGametes = GetGametes(mother);
            if (fGametes.Count == 0 || mGametes.Count == 0) return;

            int rows = mGametes.Count;
            int cols = fGametes.Count;

            // Build Punnett grid (increased cell sizes and star-width for classroom presentation flexibility)
            for (int i = 0; i <= rows; i++)
                punnettGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(85) });
            for (int j = 0; j <= cols; j++)
                punnettGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Corner
            AddCell(0, 0, "♂\\♀", "#424242", "#F5F5F5");

            // Father gametes (top)
            for (int j = 0; j < cols; j++)
                AddCell(0, j + 1, fGametes[j], "#1565C0", "#E3F2FD");

            // Mother gametes (left)
            for (int i = 0; i < rows; i++)
                AddCell(i + 1, 0, mGametes[i], "#AD1457", "#FCE4EC");

            // Offspring
            var offspring = new List<string>();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    string genotype = CombineGametes(fGametes[j], mGametes[i]);
                    string normalizedGeno = NormalizeGenotype(genotype);
                    offspring.Add(normalizedGeno);
                    AddCell(i + 1, j + 1, normalizedGeno, "#1B5E20", "#E8F5E9");
                }
            }

            // Genotype ratios
            var genoGroups = offspring.GroupBy(g => NormalizeGenotype(g))
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key}: {g.Count()}/{offspring.Count}")
                .ToList();
            UI.ResultRow($"🧬 Tỉ lệ kiểu gen: {string.Join(" | ", genoGroups)}", "#1B5E20", resultPanel);

            // Phenotype ratios
            var phenoGroups = offspring.GroupBy(g => GetPhenotype(NormalizeGenotype(g)))
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key}: {g.Count()}/{offspring.Count}")
                .ToList();
            UI.ResultRow($"🎨 Tỉ lệ kiểu hình: {string.Join(" | ", phenoGroups)}", "#7B1FA2", resultPanel);

            // Summary
            if (father.Length == 2) // 1 gene
            {
                int dominant = offspring.Count(g => IsDominant(NormalizeGenotype(g)));
                int recessive = offspring.Count - dominant;
                char gLetter = char.ToUpper(father[0]);
                string traitDom = gLetter == 'B' ? "Trội (Thân cao)" : "Trội";
                string traitRec = gLetter == 'B' ? "Lặn (Thân thấp)" : "Lặn";
                UI.ResultRow($"Tổng quát = {dominant} {traitDom} : {recessive} {traitRec}", "#1565C0", resultPanel);

                // Mendel ratios
                if (genoGroups.Count == 3)
                    UI.ResultRow($"📌 Nhận xét = Quy luật phân li (Tỉ lệ kiểu gen 1:2:1, kiểu hình 3:1)", "#E65100", resultPanel);
                else if (genoGroups.Count == 2 && dominant == recessive)
                    UI.ResultRow($"📌 Nhận xét = Lai phân tích (Tỉ lệ 1:1)", "#E65100", resultPanel);
            }
            else if (father.Length == 4) // 2 genes
            {
                // Count dominant/recessive for gene 1 (first pair, index 0,1)
                int dom1 = offspring.Count(g => char.IsUpper(NormalizeGenotype(g)[0]));
                int rec1 = offspring.Count - dom1;

                // Count dominant/recessive for gene 2 (second pair, index 2,3)
                int dom2 = offspring.Count(g => char.IsUpper(NormalizeGenotype(g)[2]));
                int rec2 = offspring.Count - dom2;

                UI.ResultRow($"Tỉ lệ riêng cặp tính trạng 1 = {dom1} Trội : {rec1} Lặn", "#1565C0", resultPanel);
                UI.ResultRow($"Tỉ lệ riêng cặp tính trạng 2 = {dom2} Trội : {rec2} Lặn", "#1565C0", resultPanel);

                // Mendel ratios
                if (genoGroups.Count == 9) // AaBb x AaBb has 9 genotypes
                    UI.ResultRow($"📌 Nhận xét = Quy luật phân li độc lập (Tỉ lệ kiểu hình 9:3:3:1)", "#E65100", resultPanel);
                else if (genoGroups.Count == 4 && dom1 == rec1 && dom2 == rec2) // AaBb x aabb has 4 genotypes, 1:1:1:1
                    UI.ResultRow($"📌 Nhận xét = Lai phân tích hai cặp tính trạng (Tỉ lệ 1:1:1:1)", "#E65100", resultPanel);
            }
        }

        private static List<string> GetGametes(string genotype)
        {
            if (genotype.Length == 2) // 1 gene: Aa → A, a
            {
                return new List<string> { genotype[0].ToString(), genotype[1].ToString() }.Distinct().ToList();
            }
            if (genotype.Length == 4) // 2 genes: AaBb → AB, Ab, aB, ab
            {
                var alleles1 = new[] { genotype[0], genotype[1] };
                var alleles2 = new[] { genotype[2], genotype[3] };
                var result = new List<string>();
                foreach (var a1 in alleles1)
                    foreach (var a2 in alleles2)
                        result.Add($"{a1}{a2}");
                return result.Distinct().ToList();
            }
            return new List<string>();
        }

        private static string CombineGametes(string g1, string g2)
        {
            if (g1.Length == 1 && g2.Length == 1)
                return $"{g1}{g2}";
            if (g1.Length == 2 && g2.Length == 2)
                return $"{g1[0]}{g2[0]}{g1[1]}{g2[1]}";
            return $"{g1}{g2}";
        }



        private static string GetPhenotype(string genotype)
        {
            if (genotype.Length == 2)
                return char.IsUpper(genotype[0]) ? "Trội" : "Lặn";
            if (genotype.Length == 4)
            {
                string p1 = char.IsUpper(genotype[0]) ? "Trội₁" : "Lặn₁";
                string p2 = char.IsUpper(genotype[2]) ? "Trội₂" : "Lặn₂";
                return $"{p1}-{p2}";
            }
            return "?";
        }

        private static bool IsDominant(string genotype)
        {
            if (genotype.Length >= 2) return char.IsUpper(genotype[0]);
            return false;
        }

        private void AddCell(int row, int col, string text, string fgHex, string bgHex)
        {
            var fg = (Color)ColorConverter.ConvertFromString(fgHex);
            var bg = (Color)ColorConverter.ConvertFromString(bgHex);
            var border = new Border
            {
                Background = new SolidColorBrush(bg),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, fg.R, fg.G, fg.B)),
                BorderThickness = new Thickness(0.5),
                CornerRadius = new CornerRadius(4), Margin = new Thickness(1)
            };

            // Set ToolTip
            if (row == 0 && col == 0)
            {
                border.ToolTip = "Bảng Punnett: Dòng trên là Giao tử của Bố (♂), Cột trái là Giao tử của Mẹ (♀).";
            }
            else if (row == 0 && col > 0)
            {
                border.ToolTip = $"Giao tử của Bố (♂): {text}";
            }
            else if (row > 0 && col == 0)
            {
                border.ToolTip = $"Giao tử của Mẹ (♀): {text}";
            }
            else if (row > 0 && col > 0)
            {
                string pheno = GetPhenotype(text);
                string icon = GetTraitIcon(text);
                string desc = "";
                if (text.Length == 2)
                {
                    char g = char.ToUpper(text[0]);
                    if (g == 'B')
                    {
                        desc = pheno == "Trội" ? "Trội hoàn toàn (ví dụ: Thân cao)" : "Lặn hoàn toàn (ví dụ: Thân thấp)";
                    }
                    else
                    {
                        desc = pheno == "Trội" ? "Trội hoàn toàn (ví dụ: Hạt vàng)" : "Lặn hoàn toàn (ví dụ: Hạt xanh)";
                    }
                }
                else
                {
                    desc = pheno switch
                    {
                        "Trội₁-Trội₂" => "Trội 1 - Trội 2 (ví dụ: Hạt vàng, trơn)",
                        "Trội₁-Lặn₂" => "Trội 1 - Lặn 2 (ví dụ: Hạt vàng, nhăn)",
                        "Lặn₁-Trội₂" => "Lặn 1 - Trội 2 (ví dụ: Hạt xanh, trơn)",
                        "Lặn₁-Lặn₂" => "Lặn 1 - Lặn 2 (ví dụ: Hạt xanh, nhăn)",
                        _ => pheno
                    };
                }
                border.ToolTip = $"Tổ hợp F1:\n• Kiểu gen: {text}\n• Kiểu hình: {desc} {icon}";
            }

            border.Child = new TextBlock
            {
                Text = text, FontSize = 22, FontWeight = FontWeights.Bold, FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(fg),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            punnettGrid.Children.Add(border);
        }

        // ═══ PRESETS ═══
        private void BuildPresets()
        {
            if (presetPanel == null) return;
            var presets = new (string L, string F, string M)[]
            {
                ("Aa × Aa", "Aa", "Aa"),
                ("AA × aa", "AA", "aa"),
                ("Aa × aa (phân tích)", "Aa", "aa"),
                ("AaBb × AaBb", "AaBb", "AaBb"),
                ("AaBb × aabb (phân tích)", "AaBb", "aabb"),
                ("AABb × AaBb", "AABb", "AaBb"),
                ("Aabb × aaBb", "Aabb", "aaBb"),
            };
            foreach (var (l, f, m) in presets)
            {
                var c = (Color)ColorConverter.ConvertFromString("#1B5E20");
                var btn = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(25, c.R, c.G, c.B)),
                    BorderBrush = new SolidColorBrush(c), BorderThickness = new Thickness(0, 0, 0, 2),
                    CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 8, 8), Cursor = Cursors.Hand
                };
                btn.Child = new TextBlock { Text = l, FontSize = 15, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(c) };
                string cf = f, cm = m;
                btn.MouseLeftButtonDown += (_, _) => { txtFather.Text = cf; txtMother.Text = cm; };
                presetPanel.Children.Add(btn);
            }
        }

        // ═══ HELPERS ═══
        /* AddResult / AddR replaced by UI.ResultRow */
        
        public static string GetStandardizedGenotype(string genotype)
        {
            if (string.IsNullOrEmpty(genotype)) return "";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < genotype.Length; i++)
            {
                char c = genotype[i];
                int pairIdx = i / 2;
                char stdChar = (pairIdx == 0) ? 'A' : 'B';
                if (char.IsLower(c))
                {
                    sb.Append(char.ToLower(stdChar));
                }
                else
                {
                    sb.Append(char.ToUpper(stdChar));
                }
            }
            return sb.ToString();
        }

        public static bool IsValidGenotype(string genotype, out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(genotype))
            {
                error = "Kiểu gen không được trống.";
                return false;
            }
            genotype = System.Text.RegularExpressions.Regex.Replace(genotype, @"\s+", "");
            if (genotype.Length != 2 && genotype.Length != 4)
            {
                error = "Kiểu gen phải có độ dài 2 hoặc 4 ký tự.";
                return false;
            }
            foreach (char c in genotype)
            {
                if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')))
                {
                    error = "Kiểu gen chỉ được chứa các chữ cái Latin không dấu.";
                    return false;
                }
            }
            if (char.ToUpper(genotype[0]) != char.ToUpper(genotype[1]))
            {
                error = "Cặp alen thứ nhất phải cùng một loại gen (ví dụ: Aa, AA, aa).";
                return false;
            }
            if (genotype.Length == 4)
            {
                if (char.ToUpper(genotype[2]) != char.ToUpper(genotype[3]))
                {
                    error = "Cặp alen thứ hai phải cùng một loại gen (ví dụ: Bb, BB, bb).";
                    return false;
                }
                if (char.ToUpper(genotype[0]) == char.ToUpper(genotype[2]))
                {
                    error = "Hai cặp alen phải thuộc hai gen khác nhau.";
                    return false;
                }
            }
            return true;
        }

        public static string NormalizeGenotype(string genotype)
        {
            if (string.IsNullOrEmpty(genotype)) return "";
            genotype = System.Text.RegularExpressions.Regex.Replace(genotype, @"\s+", "");
            
            var pairs = new List<string>();
            for (int i = 0; i < genotype.Length; i += 2)
            {
                char c1 = genotype[i];
                char c2 = genotype[i + 1];
                if (char.IsLower(c1) && char.IsUpper(c2))
                {
                    pairs.Add($"{c2}{c1}");
                }
                else
                {
                    pairs.Add($"{c1}{c2}");
                }
            }
            
            var sorted = pairs.OrderBy(p => char.ToUpper(p[0])).ToList();
            return string.Join("", sorted);
        }

        public static string GetTraitIcon(string genotype)
        {
            if (string.IsNullOrEmpty(genotype)) return "";
            genotype = NormalizeGenotype(genotype);
            if (genotype.Length == 2)
            {
                char g = char.ToUpper(genotype[0]);
                bool isDominant = char.IsUpper(genotype[0]) || char.IsUpper(genotype[1]);
                if (g == 'A')
                {
                    return isDominant ? "🟡" : "🟢";
                }
                else if (g == 'B')
                {
                    return isDominant ? "🟤" : "🔵";
                }
                else
                {
                    return isDominant ? "🟡" : "🟢"; // Default fallback for other letters
                }
            }
            else if (genotype.Length == 4)
            {
                bool hasDomA = char.IsUpper(genotype[0]);
                bool hasDomB = char.IsUpper(genotype[2]);
                if (hasDomA && hasDomB) return "🟡";
                if (hasDomA && !hasDomB) return "🔸";
                if (!hasDomA && hasDomB) return "🟢";
                if (!hasDomA && !hasDomB) return "🔹";
            }
            return "❓";
        }

        private void BtnExportHtml_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                string father = txtFather?.Text?.Trim() ?? "";
                string mother = txtMother?.Text?.Trim() ?? "";

                if (string.IsNullOrEmpty(father) || string.IsNullOrEmpty(mother))
                {
                    MessageBox.Show("Vui lòng nhập kiểu gen bố mẹ hợp lệ trước khi xuất báo cáo.", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string errF = "";
                string errM = "";
                bool isFatherValid = IsValidGenotype(father, out errF);
                bool isMotherValid = IsValidGenotype(mother, out errM);
                if (!isFatherValid || !isMotherValid)
                {
                    MessageBox.Show($"Kiểu gen không hợp lệ.\nBố: {errF}\nMẹ: {errM}", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                father = NormalizeGenotype(father);
                mother = NormalizeGenotype(mother);

                if (father.Length != mother.Length)
                {
                    MessageBox.Show("Độ dài kiểu gen bố và mẹ phải bằng nhau.", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var fGametes = GetGametes(father);
                var mGametes = GetGametes(mother);
                if (fGametes.Count == 0 || mGametes.Count == 0) return;

                int rows = mGametes.Count;
                int cols = fGametes.Count;

                var tb = new System.Text.StringBuilder();

                // Header row
                tb.Append("<tr>");
                tb.Append("<td class='corner-cell'>♂\\♀</td>");
                for (int j = 0; j < cols; j++)
                {
                    tb.Append($"<td class='father-cell'>{fGametes[j]}</td>");
                }
                tb.Append("</tr>");

                // Offspring rows
                var offspring = new List<string>();
                for (int i = 0; i < rows; i++)
                {
                    tb.Append("<tr>");
                    tb.Append($"<td class='mother-cell'>{mGametes[i]}</td>");
                    for (int j = 0; j < cols; j++)
                      {
                        string genotype = CombineGametes(fGametes[j], mGametes[i]);
                        string normalizedGeno = NormalizeGenotype(genotype);
                        offspring.Add(normalizedGeno);

                        string pheno = GetPhenotype(normalizedGeno);
                        string icon = GetTraitIcon(normalizedGeno);
                        string desc = "";
                        if (normalizedGeno.Length == 2)
                        {
                            char g = char.ToUpper(normalizedGeno[0]);
                            if (g == 'B')
                            {
                                desc = pheno == "Trội" ? "Trội hoàn toàn (Thân cao)" : "Lặn hoàn toàn (Thân thấp)";
                            }
                            else
                            {
                                desc = pheno == "Trội" ? "Trội hoàn toàn (Hạt vàng)" : "Lặn hoàn toàn (Hạt xanh)";
                            }
                        }
                        else
                        {
                            desc = pheno switch
                            {
                                "Trội₁-Trội₂" => "Trội 1 - Trội 2 (Hạt vàng, trơn)",
                                "Trội₁-Lặn₂" => "Trội 1 - Lặn 2 (Hạt vàng, nhăn)",
                                "Lặn₁-Trội₂" => "Lặn 1 - Trội 2 (Hạt xanh, trơn)",
                                "Lặn₁-Lặn₂" => "Lặn 1 - Lặn 2 (Hạt xanh, nhăn)",
                                _ => pheno
                            };
                        }

                        tb.Append($"<td class='offspring-cell' title='Kiểu gen: {normalizedGeno}&#10;Kiểu hình: {desc} {icon}'>{normalizedGeno} {icon}</td>");
                    }
                    tb.Append("</tr>");
                }

                // Results Content
                var rb = new System.Text.StringBuilder();
                rb.Append($"<div class='result-item'><span class='result-label'>♂ Bố:</span> {father} | <span class='result-label'>♀ Mẹ:</span> {mother}</div>");

                var genoGroups = offspring.GroupBy(g => NormalizeGenotype(g))
                    .OrderByDescending(g => g.Count())
                    .Select(g => $"{g.Key}: {g.Count()}/{offspring.Count}")
                    .ToList();
                rb.Append($"<div class='result-item'><span class='result-label'>🧬 Tỉ lệ kiểu gen F1:</span> {string.Join(" | ", genoGroups)}</div>");

                var phenoGroups = offspring.GroupBy(g => GetPhenotype(NormalizeGenotype(g)))
                    .OrderByDescending(g => g.Count())
                    .Select(g => $"{g.Key}: {g.Count()}/{offspring.Count}")
                    .ToList();
                rb.Append($"<div class='result-item'><span class='result-label'>🎨 Tỉ lệ kiểu hình F1:</span> {string.Join(" | ", phenoGroups)}</div>");

                if (father.Length == 2) // 1 gene
                {
                    int dominant = offspring.Count(g => IsDominant(NormalizeGenotype(g)));
                    int recessive = offspring.Count - dominant;
                    char gLetter = char.ToUpper(father[0]);
                    string traitDom = gLetter == 'B' ? "Trội (Thân cao)" : "Trội";
                    string traitRec = gLetter == 'B' ? "Lặn (Thân thấp)" : "Lặn";
                    rb.Append($"<div class='result-item'><span class='result-label'>📊 Phân li trội/lặn:</span> {dominant} {traitDom} : {recessive} {traitRec}</div>");

                    if (genoGroups.Count == 3)
                        rb.Append($"<div class='result-item'><span class='result-label'>📌 Nhận xét di truyền:</span> Quy luật phân li (Tỉ lệ kiểu gen 1:2:1, kiểu hình 3:1)</div>");
                    else if (genoGroups.Count == 2 && dominant == recessive)
                        rb.Append($"<div class='result-item'><span class='result-label'>📌 Nhận xét di truyền:</span> Lai phân tích (Tỉ lệ 1:1)</div>");
                }
                else if (father.Length == 4) // 2 genes
                {
                    int dom1 = offspring.Count(g => char.IsUpper(NormalizeGenotype(g)[0]));
                    int rec1 = offspring.Count - dom1;
                    int dom2 = offspring.Count(g => char.IsUpper(NormalizeGenotype(g)[2]));
                    int rec2 = offspring.Count - dom2;

                    rb.Append($"<div class='result-item'><span class='result-label'>📊 Tỉ lệ riêng cặp tính trạng 1:</span> {dom1} Trội : {rec1} Lặn</div>");
                    rb.Append($"<div class='result-item'><span class='result-label'>📊 Tỉ lệ riêng cặp tính trạng 2:</span> {dom2} Trội : {rec2} Lặn</div>");

                    if (genoGroups.Count == 9)
                        rb.Append($"<div class='result-item'><span class='result-label'>📌 Nhận xét di truyền:</span> Quy luật phân li độc lập (Tỉ lệ kiểu hình 9:3:3:1)</div>");
                    else if (genoGroups.Count == 4 && dom1 == rec1 && dom2 == rec2)
                        rb.Append($"<div class='result-item'><span class='result-label'>📌 Nhận xét di truyền:</span> Lai phân tích hai cặp tính trạng (Tỉ lệ 1:1:1:1)</div>");
                }

                string html = GetGeneticsReportTemplate()
                    .Replace("##TABLE_CONTENT##", tb.ToString())
                    .Replace("##RESULTS_CONTENT##", rb.ToString())
                    .Replace("##DATE##", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MendelGenetics_Report.html");
                System.IO.File.WriteAllText(tempPath, html, System.Text.Encoding.UTF8);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất báo cáo: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetGeneticsReportTemplate()
        {
            return @"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <title>Báo Cáo Thực Hành Lai Mendel</title>
    <meta name=""description"" content=""Báo cáo thực hành di truyền Mendel và bảng Punnett"">
    <link href=""https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;600;700&display=swap"" rel=""stylesheet"">
    <style>
        :root {
            --primary: #2E7D32;
            --primary-light: #E8F5E9;
            --primary-dark: #1B5E20;
            --accent: #AD1457;
            --bg: #f4f6f9;
            --card-bg: #ffffff;
            --text: #2c3e50;
            --border: #e2e8f0;
            --shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.05), 0 4px 6px -2px rgba(0, 0, 0, 0.02);
        }
        
        [data-theme=""dark""] {
            --primary: #81C784;
            --primary-light: #1B5E20;
            --primary-dark: #a5d6a7;
            --accent: #F48FB1;
            --bg: #121212;
            --card-bg: #1e1e1e;
            --text: #e2e8f0;
            --border: #2d3748;
            --shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.3), 0 4px 6px -2px rgba(0, 0, 0, 0.2);
        }

        body {
            font-family: 'Outfit', 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: var(--bg);
            color: var(--text);
            margin: 0;
            padding: 40px 20px;
            transition: all 0.3s ease;
        }

        .container {
            max-width: 1000px;
            margin: 0 auto;
        }

        header {
            background: linear-gradient(135deg, var(--primary), var(--primary-dark));
            color: white;
            padding: 30px;
            border-radius: 16px;
            box-shadow: var(--shadow);
            margin-bottom: 30px;
            position: relative;
            overflow: hidden;
        }

        header::before {
            content: '';
            position: absolute;
            top: -50%;
            left: -50%;
            width: 200%;
            height: 200%;
            background: radial-gradient(circle, rgba(255,255,255,0.05) 0%, transparent 80%);
            pointer-events: none;
        }

        h1 {
            margin: 0 0 10px 0;
            font-size: 28px;
            font-weight: 700;
        }

        .subtitle {
            margin: 0;
            font-size: 16px;
            opacity: 0.9;
        }

        .info-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
            gap: 20px;
            margin-bottom: 30px;
        }

        .info-card {
            background-color: var(--card-bg);
            padding: 20px;
            border-radius: 12px;
            box-shadow: var(--shadow);
            border: 1px solid var(--border);
        }

        .info-label {
            font-size: 12px;
            font-weight: 600;
            color: #718096;
            text-transform: uppercase;
            margin-bottom: 5px;
        }

        .info-value {
            font-size: 16px;
            font-weight: 700;
            color: var(--text);
        }
        
        .info-value input {
            border: none;
            border-bottom: 1px dashed var(--primary);
            font-family: inherit;
            font-size: inherit;
            font-weight: inherit;
            color: inherit;
            background: transparent;
            width: 100%;
            outline: none;
        }

        .card {
            background-color: var(--card-bg);
            padding: 30px;
            border-radius: 16px;
            box-shadow: var(--shadow);
            border: 1px solid var(--border);
            margin-bottom: 30px;
        }

        .card-title {
            font-size: 20px;
            font-weight: 700;
            color: var(--primary);
            margin-top: 0;
            margin-bottom: 20px;
            display: flex;
            align-items: center;
            gap: 10px;
        }

        .table-responsive {
            overflow-x: auto;
            margin: 20px 0;
            border-radius: 8px;
            border: 1px solid var(--border);
        }

        table {
            width: 100%;
            border-collapse: collapse;
            font-size: 16px;
        }

        th, td {
            padding: 15px;
            border: 1px solid var(--border);
            text-align: center;
        }

        .corner-cell {
            font-weight: 700;
            background-color: var(--bg);
            color: var(--text);
        }

        .father-cell {
            background-color: #E3F2FD;
            color: #1565C0;
            font-weight: 700;
        }

        .mother-cell {
            background-color: #FCE4EC;
            color: #AD1457;
            font-weight: 700;
        }

        .offspring-cell {
            background-color: #E8F5E9;
            color: #1B5E20;
            font-weight: 700;
            cursor: help;
        }

        .results-box {
            background-color: var(--primary-light);
            border-left: 5px solid var(--primary);
            padding: 20px;
            border-radius: 8px;
            margin-top: 20px;
        }

        .result-item {
            margin-bottom: 12px;
        }
        
        .result-item:last-child {
            margin-bottom: 0;
        }

        .result-label {
            font-weight: 700;
            color: var(--primary-dark);
        }

        .floating-buttons {
            position: fixed;
            bottom: 30px;
            right: 30px;
            display: flex;
            gap: 12px;
            z-index: 10;
        }

        .btn {
            background-color: var(--primary);
            color: white;
            border: none;
            padding: 12px 20px;
            border-radius: 30px;
            font-weight: 600;
            cursor: pointer;
            box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
            display: flex;
            align-items: center;
            gap: 8px;
            transition: all 0.2s ease;
        }

        .btn:hover {
            transform: translateY(-2px);
            background-color: var(--primary-dark);
        }

        .btn-secondary {
            background-color: #4a5568;
        }

        .btn-secondary:hover {
            background-color: #2d3748;
        }

        @media print {
            body {
                background-color: white;
                color: black;
                padding: 0;
            }
            .card, header, .info-card {
                box-shadow: none;
                border: 1px solid #cbd5e0;
            }
            .floating-buttons {
                display: none;
            }
            .info-value input {
                border-bottom: none;
            }
        }
    </style>
</head>
<body>
    <div class=""container"">
        <header>
            <h1>🔬 BÁO CÁO THỰC HÀNH DI TRUYỀN MENDEL</h1>
            <p class=""subtitle"">Chủ đề: Khảo sát Phép lai Mendel &amp; Sơ đồ lưới Punnett</p>
        </header>

        <section class=""info-grid"">
            <div class=""info-card"">
                <div class=""info-label"">Họ và tên học sinh</div>
                <div class=""info-value""><input type=""text"" placeholder=""Nhập họ và tên..."" id=""studentName""></div>
            </div>
            <div class=""info-card"">
                <div class=""info-label"">Lớp</div>
                <div class=""info-value""><input type=""text"" placeholder=""Nhập lớp..."" id=""studentClass""></div>
            </div>
            <div class=""info-card"">
                <div class=""info-label"">Ngày thực hành</div>
                <div class=""info-value"" id=""reportDate"">##DATE##</div>
            </div>
        </section>

        <div class=""card"">
            <h2 class=""card-title"">📊 Sơ đồ tổ hợp giao tử - Bảng Punnett</h2>
            <div class=""table-responsive"">
                <table>
                    ##TABLE_CONTENT##
                </table>
            </div>
        </div>

        <div class=""card"">
            <h2 class=""card-title"">🔎 Thống kê kết quả phân li F1</h2>
            <div class=""results-box"">
                ##RESULTS_CONTENT##
            </div>
        </div>
    </div>

    <div class=""floating-buttons"">
        <button class=""btn btn-secondary"" onclick=""toggleTheme()"">🌓 Đổi Chế Độ</button>
        <button class=""btn"" onclick=""window.print()"">🖨️ In Báo Cáo</button>
    </div>

    <script>
        function toggleTheme() {
            const body = document.body;
            const currentTheme = body.getAttribute('data-theme');
            if (currentTheme === 'dark') {
                body.removeAttribute('data-theme');
            } else {
                body.setAttribute('data-theme', 'dark');
            }
        }
        
        // Auto set current date if not set
        document.getElementById('reportDate').innerText = new Date().toLocaleString('vi-VN');
    </script>
</body>
</html>";
        }

        private void LoadPracticalApps()
        {
            try
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                string suffix = isVN ? "VN" : "EN";

                var items = new List<PracticalAppItem>
                {
                    new PracticalAppItem
                    {
                        Icon = "🌾",
                        Title = isVN ? "Chọn giống cây trồng" : "Agricultural Crop Breeding",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_genetics_1_{suffix}.png",
                        Description = isVN 
                            ? "Ứng dụng bảng Punnett để dự đoán các đặc tính trội mong muốn (như năng suất, kháng bệnh) và loại bỏ các đặc tính lặn có hại trong lai tạo thực vật." 
                            : "Cross-breed crop varieties to combine desirable genes, such as drought resistance and high grain yield."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🐄",
                        Title = isVN ? "Lai tạo giống vật nuôi" : "Livestock Breeding",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_genetics_2_{suffix}.png",
                        Description = isVN 
                            ? "Tính toán tỉ lệ kiểu hình màu lông, chất lượng thịt và khả năng chống chịu thời tiết để chọn lọc các cá thể tốt nhất phục vụ sản xuất nông nghiệp." 
                            : "Calculate phenotypic ratios of coat color, meat quality, and climate tolerance to select the best livestock individuals for agricultural production."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏥",
                        Title = isVN ? "Y học & Tư vấn di truyền" : "Medical & Genetic Counseling",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_genetics_3_{suffix}.png",
                        Description = isVN 
                            ? "Dự đoán xác suất mắc các bệnh di truyền của con cái (như bệnh mù màu, máu khó đông) khi bố mẹ mang các gen dị hợp mang mầm bệnh." 
                            : "Predict the risk probabilities of offspring inheriting genetic disorders (such as color blindness, hemophilia) from heterozygous carrier parents."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🐕",
                        Title = isVN ? "Di truyền học thú cưng" : "Pet Genetics",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_genetics_4_{suffix}.png",
                        Description = isVN 
                            ? "Giải thích sự phân li và tổ hợp của các gen quy định màu lông (ví dụ: lông vàng, đen, nâu ở chó Labrador) và các đặc điểm ngoại hình khác." 
                            : "Explain the segregation and recombination of genes determining coat color (e.g., yellow, black, brown in Labrador retrievers) and other physical traits."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🐼",
                        Title = isVN ? "Bảo tồn đa dạng sinh học" : "Biodiversity Conservation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_genetics_5_{suffix}.png",
                        Description = isVN 
                            ? "Phân tích đa dạng di truyền trong các quần thể nhỏ đang gặp nguy hiểm (như gấu trúc, hổ trắng) nhằm tránh hiện tượng giao phối cận huyết." 
                            : "Analyze genetic diversity in endangered small populations (such as giant pandas, white tigers) to prevent inbreeding depression."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌸",
                        Title = isVN ? "Lai tạo hoa cảnh nghệ thuật" : "Ornamental Flower Breeding",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_genetics_6_{suffix}.png",
                        Description = isVN 
                            ? "Tổ hợp các gen quy định màu sắc và hình dáng cánh hoa (như hoa lan, hoa hồng) để tạo ra các giống hoa lai độc đáo có giá trị thẩm mỹ cao." 
                            : "Combine genes determining flower colors and petal shapes (e.g., orchids, roses) to create unique hybrid flower varieties of high aesthetic value."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌾",
                        Title = isVN ? "Lai tạo lúa kháng mặn" : "Salt-Tolerant Rice Breeding",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_genetics_7_{suffix}.png",
                        Description = isVN 
                            ? "Tổ hợp các gen trội kháng mặn thông qua sơ đồ di truyền Mendel để chọn lọc giống lúa chịu mặn cho vùng ngập mặn." 
                            : "Combine salt-tolerant dominant genes using Mendelian genetics to breed rice varieties for coastal saline soils."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏥",
                        Title = isVN ? "Tầm soát dị tật bẩm sinh" : "Congenital Defect Screening",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_genetics_8_{suffix}.png",
                        Description = isVN 
                            ? "Phân tích xác suất tổ hợp gen lặn từ cha mẹ để dự báo và phòng ngừa các hội chứng bệnh di truyền ở thai nhi." 
                            : "Analyze parental recessive gene combination probabilities to predict and prevent genetic syndromes in fetuses."
                    }};

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for GeneticsTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewSimulator == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewSimulator.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewSimulator.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
