using Xunit;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using System.Collections.Generic;
using QASmartClass.LearningTools.Views.Science;

namespace QASmartClass.Tests
{
    public class MolecularGeneticsToolTests
    {
        private void RunOnStaThread(Action action)
        {
            void InitializeApplicationFull()
            {
                var urls = new[] {
                    "pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/Styles.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/StaffTheme.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/InterOutfitFonts.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/SvgIcons.xaml",
                    "pack://application:,,,/QASmartClass;component/Localization/Strings_vi.xaml",
                    "pack://application:,,,/QASmartClass;component/LearningTools/Themes/LearningToolsStyles.xaml"
                };

                try
                {
                    var appField = typeof(System.Windows.Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    var createdField = typeof(System.Windows.Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (appField != null) appField.SetValue(null, null);
                    if (createdField != null) createdField.SetValue(null, false);

                    var app = new QASmartTouch.App();
                    foreach (var url in urls)
                    {
                        app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                        {
                            Source = new Uri(url, UriKind.Absolute)
                        });
                    }
                }
                catch { }
            }
            Exception ex = null;
            var t = new Thread(() =>
            {
                try
                {
                    if (Application.Current == null)
                    {
                        try
                        {
                            new Application();
                        }
                        catch { }
                    }
                    if (Application.Current != null)
                    {
                        lock (Application.Current.Resources)
                        {
                            if (!Application.Current.Resources.Contains("Gray100"))
                            {
                                Application.Current.Resources.Add("Gray100", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray));
                            }
                        }
                    }
                    InitializeApplicationFull(); action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (ex != null)
            {
                throw ex;
            }
        }

        [Fact]
        public void Test_IsValidDNA()
        {
            // Valid cases
            Assert.True(MolecularSolver.IsValidDNA("TACGGXTTAXGAATT", out _));
            Assert.True(MolecularSolver.IsValidDNA("tacggcttacgaatt", out _)); // accepts c and C
            Assert.True(MolecularSolver.IsValidDNA("TAC GGX TTA XGA ATT", out _)); // ignores spaces in validation

            // Invalid cases
            Assert.False(MolecularSolver.IsValidDNA("", out string err1));
            Assert.Contains("không được để trống", err1);

            Assert.False(MolecularSolver.IsValidDNA("TA", out string err2));
            Assert.Contains("quá ngắn", err2);

            Assert.False(MolecularSolver.IsValidDNA(new string('A', 121), out string err3));
            Assert.Contains("vượt quá giới hạn", err3);

            Assert.False(MolecularSolver.IsValidDNA("TACGGXTTAXGAATTY", out string err4));
            Assert.Contains("không hợp lệ", err4); // Y is invalid

            Assert.True(MolecularSolver.IsValidDNA("TACGGXTTAXGAAT", out _));
        }

        [Fact]
        public void Test_NormalizeDNA_Complementary_Transcribe_Translate()
        {
            string original = "tac ggx tta xga att";
            string normalized = MolecularSolver.NormalizeDNA(original);
            Assert.Equal("TAXGGXTTAXGAATT", normalized);

            string comp = MolecularSolver.GetComplementaryDNA(normalized);
            Assert.Equal("ATGXXGAATGXTTAA", comp);

            string mrna = MolecularSolver.Transcribe(normalized);
            Assert.Equal("AUGXXGAAUGXUUAA", mrna);

            var aa = MolecularSolver.Translate(mrna);
            // AUG -> Met, XXG -> Pro, AAU -> Asn, GXU -> Ala, UAA -> Stop
            Assert.Equal(4, aa.Count);
            Assert.Equal("Met", aa[0]);
            Assert.Equal("Pro", aa[1]);
            Assert.Equal("Asn", aa[2]);
            Assert.Equal("Ala", aa[3]);
        }

        [Fact]
        public void Test_MutationAnalysis_None()
        {
            var res = MolecularSolver.AnalyzeMutation("TACGGXTTAXGAATT", "TACGGXTTAXGAATT");
            Assert.Equal(MutationType.None, res.Type);
            Assert.Equal("Không có đột biến.", res.Description);
        }

        [Fact]
        public void Test_MutationAnalysis_Silent()
        {
            // Silent mutation: TAC (Tyr) -> TAT (Tyr) (RNA: UAU -> Tyr)
            // Original: TACGGXTTAXGAATT -> RNA: AUGUXAAUGUXUAAA (Met, Ser, Met, Ser, Lys)
            // Mutant:   TACGGXTTAXGAATG -> RNA: AUGUXAAUGUXUAAU (Met, Ser, Met, Ser, Asn) -> Wait, AAA -> AAG is Lys/Lys
            // Let's check: AAA is Lys, AAG is Lys. 
            // DNA: TAC GGX TTA XGA ATT (RNA: AUG UXA AUG UXU AAA) -> AAA is Lys.
            // If mutated to: TAC GGX TTA XGA ATX (RNA: AUG UXA AUG UXU AAG) -> AAG is Lys.
            var res = MolecularSolver.AnalyzeMutation("TACGGXTTAXGAATT", "TACGGXTTAXGAATX");
            Assert.Equal(MutationType.Silent, res.Type);
            Assert.Contains("Đột biến đồng nghĩa", res.Description);
        }

        [Fact]
        public void Test_MutationAnalysis_Missense()
        {
            // Missense: TAC (Met template complementary, transcribes to AUG -> Met)
            // Let's change second codon: GGX (transcribes to XXG -> Pro)
            // Mutant: TAX (transcribes to AUG -> Met)
            var res = MolecularSolver.AnalyzeMutation("TACGGXTTAXGAATT", "TACTAXTTAXGAATT");
            Assert.Equal(MutationType.Missense, res.Type);
            Assert.Contains("Đột biến sai nghĩa", res.Description);
        }

        [Fact]
        public void Test_MutationAnalysis_Nonsense()
        {
            // Nonsense: codon changes to Stop.
            // Original: TACGGXTTAXGAATT -> RNA: AUG UXA AUG UXU AAA
            // Stop codons: UAA, UAG, UGA
            // Let's change second codon RNA to UAA (DNA: ATT)
            var res = MolecularSolver.AnalyzeMutation("TACGGXTTAXGAATT", "TACATXTTAXGAATT");
            Assert.Equal(MutationType.Nonsense, res.Type);
            Assert.Contains("Đột biến vô nghĩa", res.Description);
        }

        [Fact]
        public void Test_MutationAnalysis_NonStop()
        {
            // Nonstop: Stop codon mutated to coding.
            // Original: TACGGXTTAXGAATT (RNA: AUG UXA AUG UXU AAA, wait, there is no Stop codon here?
            // Ah! Translate translates until Stop or end. If there is a Stop in the middle, then mutating it is a Nonstop.
            // Let's create a template with Stop in middle:
            // DNA: TAC ATT TTA XGA ATT (RNA: AUG UAA AUG UXU AAA) -> UAA is Stop. Translates to [Met].
            // Mutant: TAC ATX TTA XGA ATT (RNA: AUG UAG, wait, UAG is also Stop. Let's mutate to ATG -> RNA: UAX -> Tyr).
            // Mutant RNA: AUG UAX AUG UXU AAA -> Translates to [Met, Tyr, Met, Ser, Lys].
            var res = MolecularSolver.AnalyzeMutation("TACATTTTAXGAATT", "TACATGTTAXGAATT");
            Assert.Equal(MutationType.NonStop, res.Type);
            Assert.Contains("Đột biến mất mã kết thúc", res.Description);
        }

        [Fact]
        public void Test_MutationAnalysis_Frameshift()
        {
            // Frameshift: insertion/deletion not multiple of 3.
            var res = MolecularSolver.AnalyzeMutation("TACGGXTTAXGAATT", "TACGXTTAXGAATT");
            Assert.Equal(MutationType.Frameshift, res.Type);
            Assert.Contains("Đột biến dịch khung", res.Description);
        }

        [Fact]
        public void Test_MutationAnalysis_InFrameIndel()
        {
            // In-frame indel: insertion/deletion multiple of 3.
            var res = MolecularSolver.AnalyzeMutation("TACGGXTTAXGAATT", "TACGGXXGAATT");
            Assert.Equal(MutationType.InFrameIndel, res.Type);
            Assert.Contains("Đột biến mất bộ ba", res.Description);
        }

        [Fact]
        public void Test_Tool_UI_LoadAndCalculate()
        {
            RunOnStaThread(() =>
            {
                var tool = new MolecularGeneticsTool();
                Assert.NotNull(tool);

                // Raise Loaded event to trigger calculation
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                var txtWild = tool.FindName("txtWild") as TextBox;
                var txtMutant = tool.FindName("txtMutant") as TextBox;
                var labelsGrid = tool.FindName("labelsGrid") as Grid;
                var visualGridPanel = tool.FindName("visualGridPanel") as StackPanel;
                var resultPanel = tool.FindName("resultPanel") as StackPanel;

                Assert.NotNull(txtWild);
                Assert.NotNull(txtMutant);
                Assert.NotNull(labelsGrid);
                Assert.NotNull(visualGridPanel);
                Assert.NotNull(resultPanel);

                // Check preset options loaded
                var presetPanel = tool.FindName("presetPanel") as WrapPanel;
                Assert.NotNull(presetPanel);
                Assert.NotEmpty(presetPanel.Children);

                // Test Missense preset click
                txtWild.Text = "TACGGXTTAXGAATT";
                txtMutant.Text = "TACGGXATAXGAATT";

                // Trigger UI update
                var method = typeof(MolecularGeneticsTool).GetMethod("Calc", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // Verify result labels and grids
                Assert.Equal(8, labelsGrid.RowDefinitions.Count);
                Assert.Equal(8, labelsGrid.Children.Count);

                Assert.Single(visualGridPanel.Children);
                var innerGrid = visualGridPanel.Children[0] as Grid;
                Assert.NotNull(innerGrid);
                Assert.Equal(8, innerGrid.RowDefinitions.Count);

                // Verify new images loaded in Tab 3 (Applications)
                var practicalAppViewer = tool.FindName("practicalAppViewer") as QASmartClass.LearningTools.Controls.PracticalAppViewer;
                Assert.NotNull(practicalAppViewer);
            });
        }
    }
}
