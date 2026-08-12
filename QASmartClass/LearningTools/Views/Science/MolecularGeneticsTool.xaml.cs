using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Science
{
    public partial class MolecularGeneticsTool : BaseToolControl
    {
        private bool _isPresetClicking = false;

        public MolecularGeneticsTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextSimulator != null) menuTextSimulator.Text = isVN ? "Trình mô phỏng" : "Simulator";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                BuildPresets();
                Calc();
                LoadPracticalApps();
                TouchTextPad.Attach(txtWild, mode: "text");
                TouchTextPad.Attach(txtMutant, mode: "text");
            };
        }

        private void Input_Changed(object sender, EventArgs e)
        {
            if (IsLoaded) Calc();
        }

        private void Calc()
        {
            if (resultPanel == null || visualGridPanel == null || labelsGrid == null) return;

            resultPanel.Children.Clear();
            visualGridPanel.Children.Clear();
            labelsGrid.Children.Clear();
            labelsGrid.RowDefinitions.Clear();
            labelsGrid.ColumnDefinitions.Clear();

            // Reset border colors
            if (borderWild != null)
            {
                borderWild.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBDEFB"));
                borderWild.BorderThickness = new Thickness(1);
            }
            if (borderMutant != null)
            {
                borderMutant.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCDD2"));
                borderMutant.BorderThickness = new Thickness(1);
            }

            string wild = txtWild?.Text?.Trim() ?? "";
            string mutant = txtMutant?.Text?.Trim() ?? "";

            // Validate DNA input
            if (!MolecularSolver.IsValidDNA(wild, out string errWild))
            {
                if (borderWild != null)
                {
                    borderWild.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D32F2F"));
                    borderWild.BorderThickness = new Thickness(1.5);
                }
                UI.ResultRow($"⚠️ Mạch gốc: {errWild}", "#C62828", resultPanel);
                return;
            }
            if (!MolecularSolver.IsValidDNA(mutant, out string errMutant))
            {
                if (borderMutant != null)
                {
                    borderMutant.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D32F2F"));
                    borderMutant.BorderThickness = new Thickness(1.5);
                }
                UI.ResultRow($"⚠️ Mạch đột biến: {errMutant}", "#C62828", resultPanel);
                return;
            }

            string normWild = MolecularSolver.NormalizeDNA(wild);
            string normMutant = MolecularSolver.NormalizeDNA(mutant);

            // Transcribe and Translate
            string rnaWild = MolecularSolver.Transcribe(normWild);
            string rnaMutant = MolecularSolver.Transcribe(normMutant);

            var aaWild = MolecularSolver.Translate(rnaWild);
            var aaMutant = MolecularSolver.Translate(rnaMutant);

            // Mutation Analysis
            var analysis = MolecularSolver.AnalyzeMutation(normWild, normMutant);

            // Align Wild and Mutant for visual alignment (single indel handling)
            string wildAlign = normWild;
            string mutantAlign = normMutant;
            int mutationIndex = -1;

            if (normWild.Length != normMutant.Length)
            {
                int firstDiff = 0;
                int minLen = global::System.Math.Min(normWild.Length, normMutant.Length);
                while (firstDiff < minLen && normWild[firstDiff] == normMutant[firstDiff])
                {
                    firstDiff++;
                }
                mutationIndex = firstDiff;

                if (normWild.Length < normMutant.Length)
                {
                    wildAlign = normWild.Substring(0, firstDiff) + new string('-', normMutant.Length - normWild.Length) + normWild.Substring(firstDiff);
                }
                else
                {
                    mutantAlign = normMutant.Substring(0, firstDiff) + new string('-', normWild.Length - normMutant.Length) + normMutant.Substring(firstDiff);
                }
            }
            else
            {
                // Find first difference for substitution
                for (int i = 0; i < normWild.Length; i++)
                {
                    if (normWild[i] != normMutant[i])
                    {
                        mutationIndex = i;
                        break;
                    }
                }
            }

            // Create Labels Grid (left side)
            labelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            for (int r = 0; r < 8; r++)
            {
                labelsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(r >= 6 ? 45 : 35) });
            }

            AddLabelCell(labelsGrid, 0, "Mạch gốc (3'→5')", "#0D47A1");
            AddLabelCell(labelsGrid, 1, "Mạch gốc đột biến (3'→5')", "#B71C1C");
            AddLabelCell(labelsGrid, 2, "Mạch bổ sung gốc (5'→3')", "#37474F");
            AddLabelCell(labelsGrid, 3, "Mạch bổ sung đột biến (5'→3')", "#37474F");
            AddLabelCell(labelsGrid, 4, "mARN gốc (5'→3')", "#E65100");
            AddLabelCell(labelsGrid, 5, "mARN đột biến (5'→3')", "#E65100");
            AddLabelCell(labelsGrid, 6, "Protein gốc (Đầu N → Đầu C)", "#1B5E20");
            AddLabelCell(labelsGrid, 7, "Protein đột biến (Đầu N → Đầu C)", "#1B5E20");

            // Create Visual Grid (right side, scrollable)
            Grid grid = new Grid();
            for (int i = 0; i < wildAlign.Length; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });
            }

            for (int r = 0; r < 8; r++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(r >= 6 ? 45 : 35) });
            }

            // Populating Nucleotide Rows
            string compWild = MolecularSolver.GetComplementaryDNA(normWild);
            string compMutant = MolecularSolver.GetComplementaryDNA(normMutant);

            // Re-align complementary and RNA sequences based on template alignment
            string compWildAlign = AlignSequence(compWild, wildAlign);
            string compMutantAlign = AlignSequence(compMutant, mutantAlign);
            string rnaWildAlign = AlignSequence(rnaWild, wildAlign);
            string rnaMutantAlign = AlignSequence(rnaMutant, mutantAlign);

            for (int col = 0; col < wildAlign.Length; col++)
            {
                int colIdx = col; // Bắt đầu từ 0
                bool isMutatedCol = (col == mutationIndex || (normWild.Length != normMutant.Length && col >= mutationIndex));

                // Wild DNA Template
                char wBase = wildAlign[col];
                AddNucleotideCell(grid, 0, colIdx, wBase.ToString(), "#0D47A1", wBase == '-' ? "#ECEFF1" : "#E3F2FD", isMutatedCol && wBase != '-');

                // Mutant DNA Template
                char mBase = mutantAlign[col];
                AddNucleotideCell(grid, 1, colIdx, mBase.ToString(), "#B71C1C", mBase == '-' ? "#ECEFF1" : "#FFEBEE", isMutatedCol && mBase != '-');

                // Wild DNA Complementary
                char wcBase = compWildAlign[col];
                AddNucleotideCell(grid, 2, colIdx, wcBase.ToString(), "#37474F", wcBase == '-' ? "#ECEFF1" : "#F5F5F5", isMutatedCol && wcBase != '-');

                // Mutant DNA Complementary
                char mcBase = compMutantAlign[col];
                AddNucleotideCell(grid, 3, colIdx, mcBase.ToString(), "#37474F", mcBase == '-' ? "#ECEFF1" : "#F5F5F5", isMutatedCol && mcBase != '-');

                // Wild mRNA
                char wrBase = rnaWildAlign[col];
                AddNucleotideCell(grid, 4, colIdx, wrBase.ToString(), "#E65100", wrBase == '-' ? "#ECEFF1" : "#FFF3E0", isMutatedCol && wrBase != '-');

                // Mutant mRNA
                char mrBase = rnaMutantAlign[col];
                if (chkTeacherMode?.IsChecked == true)
                {
                    AddNucleotideCell(grid, 5, colIdx, "?", "#757575", "#ECEFF1", false);
                }
                else
                {
                    AddNucleotideCell(grid, 5, colIdx, mrBase.ToString(), "#E65100", mrBase == '-' ? "#ECEFF1" : "#FFF3E0", isMutatedCol && mrBase != '-');
                }
            }

            // Map non-gap columns for Amino Acid alignment
            var wildNonGapCols = new List<int>();
            for (int i = 0; i < wildAlign.Length; i++)
            {
                if (wildAlign[i] != '-') wildNonGapCols.Add(i);
            }

            var mutantNonGapCols = new List<int>();
            for (int i = 0; i < mutantAlign.Length; i++)
            {
                if (mutantAlign[i] != '-') mutantNonGapCols.Add(i);
            }

            // Populate Wild Amino Acids
            for (int k = 0; k < aaWild.Count; k++)
            {
                if (k * 3 + 2 >= wildNonGapCols.Count) break;
                int start = wildNonGapCols[k * 3];
                int end = wildNonGapCols[k * 3 + 2];
                int span = end - start + 1;

                string name = aaWild[k];
                bool isChanged = k >= aaMutant.Count || aaWild[k] != aaMutant[k];

                AddCell(grid, 6, start, name, "#1B5E20", isChanged ? "#FFF9C4" : "#E8F5E9", span);
            }

            // Populate Mutant Amino Acids
            for (int k = 0; k < aaMutant.Count; k++)
            {
                if (k * 3 + 2 >= mutantNonGapCols.Count) break;
                int start = mutantNonGapCols[k * 3];
                int end = mutantNonGapCols[k * 3 + 2];
                int span = end - start + 1;

                string name = aaMutant[k];
                bool isChanged = k >= aaWild.Count || aaWild[k] != aaMutant[k];

                if (chkTeacherMode?.IsChecked == true)
                {
                    AddCell(grid, 7, start, "?", "#757575", "#ECEFF1", span);
                }
                else
                {
                    AddCell(grid, 7, start, name, isChanged ? "#C62828" : "#1B5E20", isChanged ? "#FFEBEE" : "#E8F5E9", span);
                }
            }

            visualGridPanel.Children.Add(grid);

            // Display Results Analysis
            if (chkTeacherMode?.IsChecked == true)
            {
                UI.ResultRow("📌 Phân loại đột biến: [Ẩn ở chế độ giáo viên]", "#1B5E20", resultPanel);
                UI.ResultRow("🔎 Chi tiết cơ chế & hệ quả: [Đã ẩn ở chế độ giáo viên. Hãy tự suy luận chuỗi mARN và chuỗi polypeptide đột biến!]", "#1565C0", resultPanel);
                UI.ResultRow($"📏 Chiều dài chuỗi Axit amin: Gốc = {aaWild.Count} aa | Đột biến = ?", "#7B1FA2", resultPanel);
            }
            else
            {
                UI.ResultRow($"📌 Phân loại đột biến: {analysis.Description}", "#1B5E20", resultPanel);
                UI.ResultRow(analysis.DetailExplanation, "#1565C0", resultPanel);
                UI.ResultRow($"📏 Chiều dài chuỗi Axit amin: Gốc = {aaWild.Count} aa | Đột biến = {aaMutant.Count} aa", "#7B1FA2", resultPanel);
            }

            if (_isPresetClicking && mutationIndex >= 0 && visualGridScrollViewer != null)
            {
                int mutCol = mutationIndex;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    double offset = (mutCol * 35) - (visualGridScrollViewer.ViewportWidth / 2.0);
                    if (offset < 0) offset = 0;
                    visualGridScrollViewer.ScrollToHorizontalOffset(offset);
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private string AlignSequence(string actual, string alignmentTemplate)
        {
            var sb = new System.Text.StringBuilder();
            int actualIdx = 0;
            for (int i = 0; i < alignmentTemplate.Length; i++)
            {
                if (alignmentTemplate[i] == '-')
                {
                    sb.Append('-');
                }
                else
                {
                    if (actualIdx < actual.Length)
                    {
                        sb.Append(actual[actualIdx++]);
                    }
                    else
                    {
                        sb.Append('-');
                    }
                }
            }
            return sb.ToString();
        }

        private static void SetFormattedText(TextBlock tb, string text)
        {
            tb.Inlines.Clear();
            int i = 0;
            while (i < text.Length)
            {
                if (i <= text.Length - 2 && text[i] == '3' && text[i + 1] == '\'')
                {
                    tb.Inlines.Add(new System.Windows.Documents.Run("3'"));
                    i += 2;
                }
                else if (i <= text.Length - 2 && text[i] == '5' && text[i + 1] == '\'')
                {
                    tb.Inlines.Add(new System.Windows.Documents.Run("5'"));
                    i += 2;
                }
                else
                {
                    tb.Inlines.Add(new System.Windows.Documents.Run(text[i].ToString()));
                    i++;
                }
            }
        }

        private void AddLabelCell(Grid grid, int row, string text, string fgHex)
        {
            var fg = (Color)ColorConverter.ConvertFromString(fgHex);
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(0.5),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(1),
                Padding = new Thickness(8, 2, 8, 2)
            };
            var tb = new TextBlock
            {
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(fg),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            SetFormattedText(tb, text);
            border.Child = tb;
            Grid.SetRow(border, row);
            Grid.SetColumn(border, 0);
            grid.Children.Add(border);
        }

        private void AddNucleotideCell(Grid grid, int row, int col, string text, string fgHex, string bgHex, bool isMutated)
        {
            var fg = (Color)ColorConverter.ConvertFromString(isMutated ? "#D32F2F" : fgHex);
            var bg = (Color)ColorConverter.ConvertFromString(isMutated ? "#FFCDD2" : bgHex);
            var border = new Border
            {
                Background = new SolidColorBrush(bg),
                BorderBrush = new SolidColorBrush(isMutated ? Color.FromRgb(211, 47, 47) : Color.FromArgb(60, fg.R, fg.G, fg.B)),
                BorderThickness = new Thickness(isMutated ? 1.5 : 0.5),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(1)
            };

            if (!string.IsNullOrEmpty(text))
            {
                string normChar = text.Trim().ToUpperInvariant();
                if (normChar.Length == 1)
                {
                    string ntName = normChar[0] switch
                    {
                        'A' => "Ađênin (Adenine)",
                        'T' => "Timin (Thymine)",
                        'G' => "Guanin (Guanine)",
                        'X' => "Xitôzin (Cytosine)",
                        'C' => "Xitôzin (Cytosine)",
                        'U' => "Uraxin (Uracil)",
                        '-' => "Khuyết Nuclêôtit (Khe hở dịch khung)",
                        _ => ""
                    };
                    if (!string.IsNullOrEmpty(ntName))
                    {
                        border.ToolTip = ntName;
                    }
                }
            }

            border.Child = new TextBlock
            {
                Text = text,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(fg),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            grid.Children.Add(border);
        }

        private void AddCell(Grid grid, int row, int col, string text, string fgHex, string bgHex, int colSpan = 1)
        {
            var fg = (Color)ColorConverter.ConvertFromString(fgHex);
            var bg = (Color)ColorConverter.ConvertFromString(bgHex);
            var border = new Border
            {
                Background = new SolidColorBrush(bg),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, fg.R, fg.G, fg.B)),
                BorderThickness = new Thickness(0.5),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(1)
            };
            
            if (row >= 6 && !string.IsNullOrEmpty(text))
            {
                string fullName = MolecularSolver.GetAminoAcidFullName(text);
                if (!string.IsNullOrEmpty(fullName))
                {
                    border.ToolTip = fullName;
                }
            }

            border.Child = new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(fg),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };
            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            if (colSpan > 1)
            {
                Grid.SetColumnSpan(border, colSpan);
            }
            grid.Children.Add(border);
        }

        private void BuildPresets()
        {
            if (presetPanel == null) return;
            var presets = new (string Name, string Wild, string Mutant)[]
            {
                ("Đột biến đồng nghĩa (Silent)", "TACGGXTTAXGAATT", "TACGGTTTAXGAATT"),
                ("Đột biến sai nghĩa (Missense)", "TACGGXTTAXGAATT", "TACGGXATAXGAATT"),
                ("Đột biến vô nghĩa (Nonsense)", "TACGGXTTAXGAATT", "TACGGXATTXGAATT"),
                ("Đột biến mất mã dừng (Nonstop)", "TACGGXTTAXGAATT", "TACGGXTTAXGAATG"),
                ("Mất 1 Nu (Dịch khung)", "TACGGXTTAXGAATT", "TACGXTTAXGAATT"),
                ("Thêm 1 Nu (Dịch khung)", "TACGGXTTAXGAATT", "TACGGXTTAAXGAATT"),
                ("Mất 3 Nu (Mất bộ ba)", "TACGGXTTAXGAATT", "TACGGXGAATT")
            };

            foreach (var p in presets)
            {
                var c = (Color)ColorConverter.ConvertFromString("#1565C0");
                UI.PresetButton(p.Name, c, () =>
                {
                    _isPresetClicking = true;
                    try
                    {
                        txtWild.Text = p.Wild;
                        txtMutant.Text = p.Mutant;
                    }
                    finally
                    {
                        _isPresetClicking = false;
                    }
                }, presetPanel);
            }
        }

        private void LoadPracticalApps()
        {
            try
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                string suffix = isVN ? "VN" : "EN";

                var list = new List<PracticalAppItem>
                {
                    new PracticalAppItem
                    {
                        Icon = "🧬",
                        Title = isVN ? "Y học Pháp y & Thử ADN" : "Forensics & DNA Testing",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_molecular_genetics_1_{suffix}.png",
                        Description = isVN 
                            ? "Xét nghiệm DNA so sánh các chỉ thị đa hình và đột biến điểm để xác định quan hệ huyết thống hoặc lập bản đồ tội phạm phục vụ công tác điều tra phá án." 
                            : "DNA testing compares polymorphic markers and point mutations to determine familial relationships or compile offender profiles for crime investigation."
                    },
                    new PracticalAppItem
                    {
                        Icon = "✂️",
                        Title = isVN ? "Liệu Pháp Gen CRISPR" : "CRISPR Gene Therapy",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_molecular_genetics_2_{suffix}.png",
                        Description = isVN 
                            ? "Công nghệ chỉnh sửa gen CRISPR-Cas9 cho phép cắt bỏ chính xác đoạn gen đột biến gây bệnh di truyền trong tế bào và chèn đoạn DNA khỏe mạnh để chữa dứt điểm bệnh." 
                            : "CRISPR-Cas9 gene editing technology enables the precise excision of mutated genes causing hereditary diseases in cells and insertion of healthy DNA to cure them."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌾",
                        Title = isVN ? "Cây Trồng Biến Đổi Gen" : "Genetically Modified Crops (GMO)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_molecular_genetics_3_{suffix}.png",
                        Description = isVN 
                            ? "Đưa các gen chống sâu bệnh hại, gen chịu mặn hay tăng hàm lượng chất dinh dưỡng vào bộ gen thực vật để tạo ra các loại giống năng suất cao trong nông nghiệp." 
                            : "Introducing pest-resistant, salt-tolerant, or nutrient-enhancing genes into plant genomes to produce high-yield crop varieties in agriculture."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏥",
                        Title = isVN ? "Tầm Soát Bệnh Di Truyền" : "Genetic Disease Screening",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_molecular_genetics_4_{suffix}.png",
                        Description = isVN 
                            ? "Giải trình tự gen thế hệ mới giúp phát hiện sớm các đột biến điểm gây thiếu máu hồng cầu hình liềm hay bệnh tan máu bẩm sinh ngay từ giai đoạn bào thai." 
                            : "Next-generation sequencing helps detect early point mutations causing sickle cell anemia or thalassemia right from the fetal stage."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💊",
                        Title = isVN ? "Sản Xuất Insulin Tái Tổ Hợp" : "Recombinant Insulin Production",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_molecular_genetics_5_{suffix}.png",
                        Description = isVN 
                            ? "Chuyển gen tổng hợp insulin người vào hệ gen vi khuẩn E. coli, nhân nuôi công nghiệp để phiên mã và dịch mã tạo ra hoocmôn insulin sinh học quy mô lớn trị tiểu đường." 
                            : "Transfers the human insulin synthesis gene into the E. coli genome, growing it industrially to transcribe and translate bio-insulin on a large scale for diabetes treatment."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌳",
                        Title = isVN ? "Tiến Hóa Chủng Loại Học" : "Phylogenetic Evolution",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_molecular_genetics_6_{suffix}.png",
                        Description = isVN 
                            ? "So sánh trình tự gen và đột biến tích lũy giữa các loài sinh vật để dựng nên cây phát sinh tiến hóa, tìm lại cội nguồn chung của sinh giới." 
                            : "Compares gene sequences and accumulated mutations between biological species to construct evolutionary trees, tracing the common origins of life."
                    }
                ,
                    new PracticalAppItem
                    {
                        Icon = "🧪",
                        Title = isVN ? "Vắc-xin thế hệ mới mRNA" : "Next-Gen mRNA Vaccines",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_molecular_genetics_7_{suffix}.png",
                        Description = isVN 
                            ? "Thiết kế chuỗi mRNA nhân tạo mang thông tin mã hóa protein gai của virus để kích thích hệ miễn dịch sinh kháng thể." 
                            : "Design synthetic mRNA strands encoding viral spike proteins to trigger immune antibody production in humans."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧬",
                        Title = isVN ? "Bản đồ gen người" : "Human Genome Project",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_molecular_genetics_8_{suffix}.png",
                        Description = isVN 
                            ? "Giải trình tự và lập bản đồ toàn bộ hơn 3 tỷ cặp base DNA của người để tìm ra nguồn gốc gen gây ra các bệnh nan y." 
                            : "Sequence and map all 3 billion DNA base pairs in the human genome to identify genetic markers for chronic diseases."
                    }};

                practicalAppViewer.SetItemsSource(list);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading practical images: " + ex.Message);
            }
        }

        private static string GetNucleotideHtmlStyle(char c, bool isMutated)
        {
            if (isMutated)
            {
                return "background-color: #FFCDD2; color: #D32F2F; border: 1.5px solid #D32F2F; font-weight: bold;";
            }
            return c switch
            {
                'A' => "background-color: #E3F2FD; color: #0D47A1; font-weight: bold;",
                'T' => "background-color: #FFEBEE; color: #B71C1C; font-weight: bold;",
                'G' => "background-color: #E8F5E9; color: #1B5E20; font-weight: bold;",
                'X' => "background-color: #F5F5F5; color: #37474F; font-weight: bold;",
                'C' => "background-color: #F5F5F5; color: #37474F; font-weight: bold;",
                'U' => "background-color: #FFF3E0; color: #E65100; font-weight: bold;",
                '-' => "background-color: #ECEFF1; color: #757575;",
                _ => "background-color: #FFFFFF; color: #212121;"
            };
        }

        private static string GetNucleotideTooltip(char c)
        {
            return c switch
            {
                'A' => "Ađênin (Adenine)",
                'T' => "Timin (Thymine)",
                'G' => "Guanin (Guanine)",
                'X' => "Xitôzin (Cytosine)",
                'C' => "Xitôzin (Cytosine)",
                'U' => "Uraxin (Uracil)",
                '-' => "Khuyết Nuclêôtit",
                _ => ""
            };
        }

        private void BtnExportHtml_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                string wild = txtWild?.Text?.Trim() ?? "";
                string mutant = txtMutant?.Text?.Trim() ?? "";

                if (!MolecularSolver.IsValidDNA(wild, out string errWild) || !MolecularSolver.IsValidDNA(mutant, out string errMutant))
                {
                    MessageBox.Show("Vui lòng nhập trình tự DNA hợp lệ trước khi xuất báo cáo.", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string normWild = MolecularSolver.NormalizeDNA(wild);
                string normMutant = MolecularSolver.NormalizeDNA(mutant);

                string rnaWild = MolecularSolver.Transcribe(normWild);
                string rnaMutant = MolecularSolver.Transcribe(normMutant);

                var aaWild = MolecularSolver.Translate(rnaWild);
                var aaMutant = MolecularSolver.Translate(rnaMutant);

                var analysis = MolecularSolver.AnalyzeMutation(normWild, normMutant);

                string wildAlign = normWild;
                string mutantAlign = normMutant;
                int mutationIndex = -1;

                if (normWild.Length != normMutant.Length)
                {
                    int firstDiff = 0;
                    int minLen = global::System.Math.Min(normWild.Length, normMutant.Length);
                    while (firstDiff < minLen && normWild[firstDiff] == normMutant[firstDiff])
                    {
                        firstDiff++;
                    }
                    mutationIndex = firstDiff;

                    if (normWild.Length < normMutant.Length)
                    {
                        wildAlign = normWild.Substring(0, firstDiff) + new string('-', normMutant.Length - normWild.Length) + normWild.Substring(firstDiff);
                    }
                    else
                    {
                        mutantAlign = normMutant.Substring(0, firstDiff) + new string('-', normWild.Length - normMutant.Length) + normMutant.Substring(firstDiff);
                    }
                }
                else
                {
                    for (int i = 0; i < normWild.Length; i++)
                    {
                        if (normWild[i] != normMutant[i])
                        {
                            mutationIndex = i;
                            break;
                        }
                    }
                }

                string compWild = MolecularSolver.GetComplementaryDNA(normWild);
                string compMutant = MolecularSolver.GetComplementaryDNA(normMutant);

                string compWildAlign = AlignSequence(compWild, wildAlign);
                string compMutantAlign = AlignSequence(compMutant, mutantAlign);
                string rnaWildAlign = AlignSequence(rnaWild, wildAlign);
                string rnaMutantAlign = AlignSequence(rnaMutant, mutantAlign);

                var wildNonGapCols = new List<int>();
                for (int i = 0; i < wildAlign.Length; i++)
                {
                    if (wildAlign[i] != '-') wildNonGapCols.Add(i);
                }

                var mutantNonGapCols = new List<int>();
                for (int i = 0; i < mutantAlign.Length; i++)
                {
                    if (mutantAlign[i] != '-') mutantNonGapCols.Add(i);
                }

                var tb = new System.Text.StringBuilder();

                string[] labels = {
                    "Mạch gốc (3' &rarr; 5')",
                    "Mạch gốc đột biến (3' &rarr; 5')",
                    "Mạch bổ sung gốc (5' &rarr; 3')",
                    "Mạch bổ sung đột biến (5' &rarr; 3')",
                    "mARN gốc (5' &rarr; 3')",
                    "mARN đột biến (5' &rarr; 3')",
                    "Protein gốc (Đầu N &rarr; Đầu C)",
                    "Protein đột biến (Đầu N &rarr; Đầu C)"
                };

                Action<int, string, string> appendNucleotideRow = (rowIdx, label, seq) =>
                {
                    tb.Append("<tr>");
                    tb.Append($"<td class='row-label'>{label}</td>");
                    for (int col = 0; col < seq.Length; col++)
                    {
                        char c = seq[col];
                        bool isMutatedCol = (col == mutationIndex || (normWild.Length != normMutant.Length && col >= mutationIndex));
                        bool isMutated = isMutatedCol && c != '-';
                        
                        string displayChar = c.ToString();
                        string style = GetNucleotideHtmlStyle(c, isMutated);
                        if (chkTeacherMode?.IsChecked == true && rowIdx == 5)
                        {
                            displayChar = "?";
                            style = "background-color: #ECEFF1; color: #757575; font-weight: bold;";
                        }
                        
                        tb.Append($"<td class='nu-cell' style='{style}' title='{GetNucleotideTooltip(c)}'>{displayChar}</td>");
                    }
                    tb.Append("</tr>");
                };

                appendNucleotideRow(0, labels[0], wildAlign);
                appendNucleotideRow(1, labels[1], mutantAlign);
                appendNucleotideRow(2, labels[2], compWildAlign);
                appendNucleotideRow(3, labels[3], compMutantAlign);
                appendNucleotideRow(4, labels[4], rnaWildAlign);
                appendNucleotideRow(5, labels[5], rnaMutantAlign);

                // Wild Protein
                tb.Append("<tr>");
                tb.Append($"<td class='row-label'>{labels[6]}</td>");
                int currentCol = 0;
                for (int k = 0; k < aaWild.Count; k++)
                {
                    if (k * 3 + 2 >= wildNonGapCols.Count) break;
                    int start = wildNonGapCols[k * 3];
                    int end = wildNonGapCols[k * 3 + 2];
                    int span = end - start + 1;

                    while (currentCol < start)
                    {
                        tb.Append("<td class='empty-cell'>-</td>");
                        currentCol++;
                    }

                    string name = aaWild[k];
                    bool isChanged = k >= aaMutant.Count || aaWild[k] != aaMutant[k];
                    string style = isChanged ? "background-color: #FFF9C4; color: #7F5F00; font-weight: bold; border: 1px solid #FBC02D;" : "background-color: #E8F5E9; color: #1B5E20; font-weight: bold;";
                    tb.Append($"<td colspan='{span}' class='aa-cell' style='{style}' title='{MolecularSolver.GetAminoAcidFullName(name)}'>{name}</td>");
                    currentCol += span;
                }
                while (currentCol < wildAlign.Length)
                {
                    tb.Append("<td class='empty-cell'>-</td>");
                    currentCol++;
                }
                tb.Append("</tr>");

                // Mutant Protein
                tb.Append("<tr>");
                tb.Append($"<td class='row-label'>{labels[7]}</td>");
                int currentColMut = 0;
                for (int k = 0; k < aaMutant.Count; k++)
                {
                    if (k * 3 + 2 >= mutantNonGapCols.Count) break;
                    int start = mutantNonGapCols[k * 3];
                    int end = mutantNonGapCols[k * 3 + 2];
                    int span = end - start + 1;

                    while (currentColMut < start)
                    {
                        tb.Append("<td class='empty-cell'>-</td>");
                        currentColMut++;
                    }

                    string name = aaMutant[k];
                    bool isChanged = k >= aaWild.Count || aaWild[k] != aaMutant[k];
                    string style = isChanged ? "background-color: #FFEBEE; color: #C62828; font-weight: bold; border: 1px solid #EF5350;" : "background-color: #E8F5E9; color: #1B5E20; font-weight: bold;";
                    
                    string displayName = name;
                    if (chkTeacherMode?.IsChecked == true)
                    {
                        displayName = "?";
                        style = "background-color: #ECEFF1; color: #757575; font-weight: bold;";
                    }

                    tb.Append($"<td colspan='{span}' class='aa-cell' style='{style}' title='{MolecularSolver.GetAminoAcidFullName(name)}'>{displayName}</td>");
                    currentColMut += span;
                }
                while (currentColMut < mutantAlign.Length)
                {
                    tb.Append("<td class='empty-cell'>-</td>");
                    currentColMut++;
                }
                tb.Append("</tr>");

                var rb = new System.Text.StringBuilder();
                if (chkTeacherMode?.IsChecked == true)
                {
                    rb.Append("<div class='result-item'><span class='result-label'>📌 Phân loại đột biến:</span> [Ẩn ở chế độ giáo viên]</div>");
                    rb.Append("<div class='result-item'><span class='result-label'>🔎 Chi tiết cơ chế & hệ quả:</span> [Đã ẩn ở chế độ giáo viên. Hãy tự suy luận chuỗi mARN và chuỗi polypeptide đột biến!]</div>");
                    rb.Append($"<div class='result-item'><span class='result-label'>📏 Chiều dài chuỗi Axit amin:</span> Gốc = {aaWild.Count} aa | Đột biến = ?</div>");
                }
                else
                {
                    rb.Append($"<div class='result-item'><span class='result-label'>📌 Phân loại đột biến:</span> {analysis.Description}</div>");
                    rb.Append($"<div class='result-item'><span class='result-label'>🔎 Chi tiết cơ chế & hệ quả:</span> {analysis.DetailExplanation}</div>");
                    rb.Append($"<div class='result-item'><span class='result-label'>📏 Chiều dài chuỗi Axit amin:</span> Gốc = {aaWild.Count} aa | Đột biến = {aaMutant.Count} aa</div>");
                }
                rb.Append($"<div class='result-item'><span class='result-label'>🔗 Liên kết Hydro:</span> Gốc = {analysis.OriginalBonds} liên kết | Đột biến = {analysis.MutatedBonds} liên kết | Chênh lệch = {(analysis.BondsDelta >= 0 ? "+" : "")}{analysis.BondsDelta}</div>");

                string html = GetMolecularReportTemplate()
                    .Replace("##TABLE_CONTENT##", tb.ToString())
                    .Replace("##RESULTS_CONTENT##", rb.ToString())
                    .Replace("##DATE##", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MolecularGenetics_Report.html");
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

        private string GetMolecularReportTemplate()
        {
            return @"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <title>Báo Cáo Thực Hành Sinh Học Phân Tử</title>
    <meta name=""description"" content=""Báo cáo thực hành di truyền phân tử và đột biến gen"">
    <link href=""https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;600;700&display=swap"" rel=""stylesheet"">
    <style>
        :root {
            --primary: #1B5E20;
            --primary-light: #E8F5E9;
            --primary-dark: #0d3c13;
            --accent: #E65100;
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
            --accent: #FF9800;
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
            max-width: 1200px;
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
            font-size: 14px;
        }

        th, td {
            padding: 10px;
            border: 1px solid var(--border);
        }

        .row-label {
            font-weight: 700;
            background-color: var(--primary-light);
            color: var(--primary-dark);
            min-width: 220px;
            position: sticky;
            left: 0;
            z-index: 1;
        }

        .nu-cell {
            text-align: center;
            font-family: monospace;
            font-size: 16px;
            min-width: 32px;
            height: 32px;
        }

        .aa-cell {
            font-weight: 700;
            font-size: 12px;
            text-align: center;
        }
        
        .empty-cell {
            background-color: var(--bg);
            color: #a0aec0;
            text-align: center;
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
            <h1>🔬 BÁO CÁO THỰC HÀNH SINH HỌC PHÂN TỬ</h1>
            <p class=""subtitle"">Chủ đề: Khảo sát quá trình Phiên mã, Dịch mã &amp; Đột biến gen điểm</p>
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
            <h2 class=""card-title"">🧬 Bản đồ so sánh trình tự Nucleotide &amp; Axit Amin</h2>
            <div class=""table-responsive"">
                <table>
                    ##TABLE_CONTENT##
                </table>
            </div>
        </div>

        <div class=""card"">
            <h2 class=""card-title"">🔎 Kết quả Phân tích Đột biến Gen</h2>
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
