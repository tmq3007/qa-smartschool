using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using QASmartClass.LearningTools.Views.Science;

namespace QASmartClass.Tests.Services
{
    public class MolecularGeneticsTests
    {
        [Fact]
        public void TestNormalizeDNA_ConvertsCtoXAndUpper()
        {
            string raw = "tac ggc tta cga att"; // corrected typo (tta instead of cta)
            string normalized = MolecularSolver.NormalizeDNA(raw);
            Assert.Equal("TAXGGXTTAXGAATT", normalized);
        }

        [Fact]
        public void TestGetComplementaryDNA()
        {
            string template = "TACGGXTTAXGAATT";
            string complementary = MolecularSolver.GetComplementaryDNA(template);
            Assert.Equal("ATGXXGAATGXTTAA", complementary);
        }

        [Fact]
        public void TestTranscribe()
        {
            string template = "TACGGXTTAXGAATT";
            string mrna = MolecularSolver.Transcribe(template);
            Assert.Equal("AUGXXGAAUGXUUAA", mrna);
        }

        [Fact]
        public void TestTranslate()
        {
            string mrna = "AUGXXGAAUXUUUAA";
            List<string> aa = MolecularSolver.Translate(mrna);
            
            // Expected translation (Stop codon is excluded):
            // AUG -> Met
            // XXG -> Pro
            // AAU -> Asn
            // XUU -> Leu
            Assert.Equal(4, aa.Count);
            Assert.Equal("Met", aa[0]);
            Assert.Equal("Pro", aa[1]);
            Assert.Equal("Asn", aa[2]);
            Assert.Equal("Leu", aa[3]);
        }

        [Fact]
        public void TestHydrogenBonds()
        {
            // A-T = 2 bonds, G-X = 3 bonds
            // template: "TACGGXTTAXGAATT"
            // A/T: 9 bases (bonds = 18)
            // G/X: 6 bases (bonds = 18)
            // Total bonds: 18 + 18 = 36.
            string template = "TACGGXTTAXGAATT";
            int bonds = MolecularSolver.CalculateHydrogenBonds(template);
            Assert.Equal(36, bonds);
        }

        [Fact]
        public void TestAnalyzeMutation_None()
        {
            string wild = "TACGGXTTAXGAATT";
            string mutant = "TACGGXTTAXGAATT";
            var result = MolecularSolver.AnalyzeMutation(wild, mutant);
            Assert.Equal(MutationType.None, result.Type);
            Assert.Equal(0, result.BondsDelta);
        }

        [Fact]
        public void TestAnalyzeMutation_Silent()
        {
            string wild = "TACGGXTTAXGAATT";
            string mutant = "TACGGTTTAXGAATT";
            var result = MolecularSolver.AnalyzeMutation(wild, mutant);
            
            Assert.Equal(MutationType.Silent, result.Type);
            Assert.Equal(-1, result.BondsDelta);
            Assert.Empty(result.ChangedAminoAcidIndices);
        }

        [Fact]
        public void TestAnalyzeMutation_Missense()
        {
            string wild = "TACGGXTTAXGAATT";
            string mutant = "TACGGXATAXGAATT";
            var result = MolecularSolver.AnalyzeMutation(wild, mutant);

            Assert.Equal(MutationType.Missense, result.Type);
            Assert.Equal(0, result.BondsDelta);
            Assert.Single(result.ChangedAminoAcidIndices);
            Assert.Equal(2, result.ChangedAminoAcidIndices[0]);
        }

        [Fact]
        public void TestAnalyzeMutation_Nonsense()
        {
            string wild = "TACGGXTTAXGAATT";
            string mutant = "TACGGXATTXGAATT";
            var result = MolecularSolver.AnalyzeMutation(wild, mutant);

            Assert.Equal(MutationType.Nonsense, result.Type);
            Assert.Equal(0, result.BondsDelta);
            Assert.Contains(2, result.ChangedAminoAcidIndices);
        }

        [Fact]
        public void TestAnalyzeMutation_Frameshift_Deletion()
        {
            string wild = "TACGGXTTAXGAATT";
            string mutant = "TACGXTTAXGAATT";
            var result = MolecularSolver.AnalyzeMutation(wild, mutant);

            Assert.Equal(MutationType.Frameshift, result.Type);
            Assert.Equal(-3, result.BondsDelta);
        }

        [Fact]
        public void TestAnalyzeMutation_Frameshift_Insertion()
        {
            string wild = "TACGGXTTAXGAATT";
            string mutant = "TACGGGXTTAXGAATT";
            var result = MolecularSolver.AnalyzeMutation(wild, mutant);

            Assert.Equal(MutationType.Frameshift, result.Type);
            Assert.Equal(3, result.BondsDelta);
        }

        [Fact]
        public void TestMolecular_DnaValidation()
        {
            // Valid DNA
            Assert.True(MolecularSolver.IsValidDNA("TACGGT", out string err1));
            Assert.Empty(err1);

            // Invalid characters
            Assert.False(MolecularSolver.IsValidDNA("TACZGT", out string err2));
            Assert.Contains("không hợp lệ", err2);

            // Too short
            Assert.False(MolecularSolver.IsValidDNA("TA", out string err3));
            Assert.Contains("quá ngắn", err3);

            // Too long (>120)
            Assert.False(MolecularSolver.IsValidDNA(new string('A', 121), out string err4));
            Assert.Contains("vượt quá giới hạn", err4);

            Assert.True(MolecularSolver.IsValidDNA("TACGG", out _));
        }

        [Fact]
        public void TestMolecular_HydrogenBondsExplanation()
        {
            // Substitution that changes hydrogen bonds: replacing T-A with G-X (increases bonds by 1)
            // Wild: "TAC" (T:2, A:2, X:3 -> 7)
            // Mutant: "GAC" (G:3, A:2, X:3 -> 8)
            var result = MolecularSolver.AnalyzeMutation("TAC", "GAC");
            Assert.Equal(MutationType.Missense, result.Type);
            Assert.Equal(1, result.BondsDelta);
            Assert.Contains("Thay thế cặp", result.DetailExplanation);
            Assert.Contains("Tổng số liên kết hyđrô thay đổi: 7 → 8 (+1 liên kết)", result.DetailExplanation);
        }

        [Fact]
        public void TestCysteineAbbreviation_Cys()
        {
            // Translating UGU should yield Cys
            List<string> aa = MolecularSolver.Translate("UGU");
            Assert.Single(aa);
            Assert.Equal("Cys", aa[0]);
            Assert.Equal("Xistêin", MolecularSolver.GetAminoAcidFullName("Cys"));
        }

        [Fact]
        public void TestAnalyzeMutation_NonFrameshiftIndel_CodonDeletion()
        {
            // Wild: "TACGGXTTAXGAATT" (15 nu)
            // Mutant: "TACGGXGAATT" (12 nu, deleted TTA)
            var result = MolecularSolver.AnalyzeMutation("TACGGXTTAXGAATT", "TACGGXGAATT");
            Assert.Equal(MutationType.InFrameIndel, result.Type); 
            Assert.Contains("mất bộ ba", result.Description.ToLower());
            Assert.Contains("mất 1 bộ ba nucleotit", result.DetailExplanation);
        }

        [Fact]
        public void TestAnalyzeMutation_NonstopMutation()
        {
            // Wild: "TACGGXTTAXGAATT" (translates to Met-Pro-Asn-Ala)
            // Mutant: "TACGGXTTAXGAATG" (mutated ATT -> ATG, transcribes to UAX -> Tyr)
            var result = MolecularSolver.AnalyzeMutation("TACGGXTTAXGAATT", "TACGGXTTAXGAATG");
            Assert.Equal(MutationType.NonStop, result.Type);
            Assert.Contains("mất mã kết thúc", result.Description.ToLower());
        }
    }
}
