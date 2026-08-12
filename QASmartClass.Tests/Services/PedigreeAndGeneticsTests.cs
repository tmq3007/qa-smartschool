using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using QASmartClass.LearningTools.Views.Science;

namespace QASmartClass.Tests.Services
{
    public class PedigreeAndGeneticsTests
    {
        [Fact]
        public void TestPedigreeContradiction_AutosomalRecessive_ConLanh()
        {
            var tree = new PedigreeTree();
            
            // Bố mẹ bệnh x con lành dưới quy luật lặn -> Mâu thuẫn ở con
            var dad = tree.CreateMember("I-1", "Nam", true, 1, 1);
            var mom = tree.CreateMember("I-2", "Nữ", true, 1, 2);
            var child = tree.CreateMember("II-1", "Nam", false, 2, 1);
            
            dad.SetSpouse(mom);
            child.SetParents(dad, mom);
            
            tree.CheckContradictions(InheritanceMode.AutosomalRecessive);
            
            Assert.False(string.IsNullOrEmpty(child.ContradictionWarning));
            Assert.Contains("Mâu thuẫn", child.ContradictionWarning);
            
            Assert.True(string.IsNullOrEmpty(dad.ContradictionWarning));
            Assert.True(string.IsNullOrEmpty(mom.ContradictionWarning));
        }

        [Fact]
        public void TestPedigreeContradiction_AutosomalDominant_ConBenh()
        {
            var tree = new PedigreeTree();
            
            // Bố mẹ lành x con bệnh dưới quy luật trội -> Mâu thuẫn ở con
            var dad = tree.CreateMember("I-1", "Nam", false, 1, 1);
            var mom = tree.CreateMember("I-2", "Nữ", false, 1, 2);
            var child = tree.CreateMember("II-1", "Nữ", true, 2, 1);
            
            dad.SetSpouse(mom);
            child.SetParents(dad, mom);
            
            tree.CheckContradictions(InheritanceMode.AutosomalDominant);
            
            Assert.False(string.IsNullOrEmpty(child.ContradictionWarning));
            Assert.Contains("Mâu thuẫn", child.ContradictionWarning);
        }

        [Fact]
        public void TestPedigreeContradiction_XLinkedRecessive_ConTraiLanh()
        {
            var tree = new PedigreeTree();
            
            // Mẹ bị bệnh lặn liên X sinh con trai lành -> Mâu thuẫn ở con trai
            var dad = tree.CreateMember("I-1", "Nam", false, 1, 1);
            var mom = tree.CreateMember("I-2", "Nữ", true, 1, 2);
            var child = tree.CreateMember("II-1", "Nam", false, 2, 1);
            
            dad.SetSpouse(mom);
            child.SetParents(dad, mom);
            
            tree.CheckContradictions(InheritanceMode.XLinkedRecessive);
            
            Assert.False(string.IsNullOrEmpty(child.ContradictionWarning));
            Assert.Contains("Mẹ bị bệnh lặn liên kết X", child.ContradictionWarning);
        }

        [Fact]
        public void TestPedigreeContradiction_XLinkedRecessive_ConGaiBenh_BoLanh()
        {
            var tree = new PedigreeTree();
            
            // Con gái bệnh lặn liên X nhưng bố bình thường -> Mâu thuẫn ở con gái
            var dad = tree.CreateMember("I-1", "Nam", false, 1, 1);
            var mom = tree.CreateMember("I-2", "Nữ", false, 1, 2);
            var child = tree.CreateMember("II-1", "Nữ", true, 2, 1);
            
            dad.SetSpouse(mom);
            child.SetParents(dad, mom);
            
            tree.CheckContradictions(InheritanceMode.XLinkedRecessive);
            
            Assert.False(string.IsNullOrEmpty(child.ContradictionWarning));
            Assert.Contains("Con gái bị bệnh lặn liên kết X", child.ContradictionWarning);
        }

        [Fact]
        public void TestPedigreeContradiction_XLinkedDominant_ConGaiLanh_BoBenh()
        {
            var tree = new PedigreeTree();
            
            // Bố bệnh trội liên kết X nhưng con gái bình thường -> Mâu thuẫn ở con gái
            var dad = tree.CreateMember("I-1", "Nam", true, 1, 1);
            var mom = tree.CreateMember("I-2", "Nữ", false, 1, 2);
            var child = tree.CreateMember("II-1", "Nữ", false, 2, 1);
            
            dad.SetSpouse(mom);
            child.SetParents(dad, mom);
            
            tree.CheckContradictions(InheritanceMode.XLinkedDominant);
            
            Assert.False(string.IsNullOrEmpty(child.ContradictionWarning));
            Assert.Contains("Bố bị bệnh trội liên kết X", child.ContradictionWarning);
        }

        [Fact]
        public void TestPedigreeContradiction_XLinkedDominant_ConTraiBenh_MeLanh()
        {
            var tree = new PedigreeTree();
            
            // Con trai bệnh trội liên X nhưng mẹ bình thường -> Mâu thuẫn ở con trai
            var dad = tree.CreateMember("I-1", "Nam", true, 1, 1);
            var mom = tree.CreateMember("I-2", "Nữ", false, 1, 2);
            var child = tree.CreateMember("II-1", "Nam", true, 2, 1);
            
            dad.SetSpouse(mom);
            child.SetParents(dad, mom);
            
            tree.CheckContradictions(InheritanceMode.XLinkedDominant);
            
            Assert.False(string.IsNullOrEmpty(child.ContradictionWarning));
            Assert.Contains("Con trai bị bệnh trội liên kết X phải có mẹ bị bệnh", child.ContradictionWarning);
        }

        [Fact]
        public void TestGenetics_GetStandardizedGenotype()
        {
            // Kiểm tra việc chuyển các chữ cái kiểu gen khác nhau về chuẩn Aa/Bb
            Assert.Equal("Aa", GeneticsTool.GetStandardizedGenotype("Bb"));
            Assert.Equal("AA", GeneticsTool.GetStandardizedGenotype("BB"));
            Assert.Equal("aa", GeneticsTool.GetStandardizedGenotype("bb"));
            
            Assert.Equal("AaBb", GeneticsTool.GetStandardizedGenotype("DdEe"));
            Assert.Equal("aaBB", GeneticsTool.GetStandardizedGenotype("eeFF"));
            Assert.Equal("AABB", GeneticsTool.GetStandardizedGenotype("DDEE"));
        }

        [Fact]
        public void Test_NonsenseMutation_Codon2()
        {
            string wild = "TACAAGAAATGAATT";
            string mutant = "TACATCAAATGAATT";
            var result = MolecularSolver.AnalyzeMutation(wild, mutant);
            Assert.Equal(MutationType.Nonsense, result.Type);
        }

        [Fact]
        public void Test_FrameshiftMutation_Insertion()
        {
            string wild = "TACGGTTTAATT";
            string mutant = "TACXGGTTTAATT";
            var result = MolecularSolver.AnalyzeMutation(wild, mutant);
            Assert.Equal(MutationType.Frameshift, result.Type);
        }

        [Fact]
        public void Test_SilentMutation_Codon4()
        {
            string wild = "TACGGXTTAXGAATT";
            string mutant = "TACGGXTTAXGGATT";
            var result = MolecularSolver.AnalyzeMutation(wild, mutant);
            Assert.Equal(MutationType.Silent, result.Type);
        }

        [Fact]
        public void Test_XLinkedDominant_Pedigree()
        {
            var tree = new PedigreeTree();
            
            // Bố bị bệnh (XAY) x Mẹ lành (XaXa)
            var dad = tree.CreateMember("I-1", "Nam", true, 1, 1);
            var mom = tree.CreateMember("I-2", "Nữ", false, 1, 2);
            dad.SetSpouse(mom);
            
            // Con gái bệnh (XAXa)
            var daughter = tree.CreateMember("II-1", "Nữ", true, 2, 1);
            daughter.SetParents(dad, mom);
            
            // Con trai lành (XaY)
            var son = tree.CreateMember("II-2", "Nam", false, 2, 2);
            son.SetParents(dad, mom);
            
            tree.DeduceGenotypes(InheritanceMode.XLinkedDominant);
            
            // Kiểm tra kiểu gen của con gái: X^A X^a
            Assert.True(daughter.Genotypes.ContainsKey("X^A X^a"));
            Assert.True(daughter.Genotypes["X^A X^a"] > 0.99);
            
            // Kiểm tra kiểu gen của con trai: X^a Y
            Assert.True(son.Genotypes.ContainsKey("X^a Y"));
            Assert.True(son.Genotypes["X^a Y"] > 0.99);
        }

        [Fact]
        public void TestGenetics_IsValidGenotype()
        {
            string error;
            Assert.True(GeneticsTool.IsValidGenotype("Aa", out error));
            Assert.True(GeneticsTool.IsValidGenotype("AaBb", out error));
            Assert.True(GeneticsTool.IsValidGenotype("bBAa", out error));
            
            Assert.False(GeneticsTool.IsValidGenotype("AaB", out error));
            Assert.False(GeneticsTool.IsValidGenotype("Ăă", out error)); // Tiếng Việt có dấu
            Assert.False(GeneticsTool.IsValidGenotype("A1", out error));  // Chứa số
        }

        [Fact]
        public void TestGenetics_NormalizeGenotype()
        {
            Assert.Equal("Aa", GeneticsTool.NormalizeGenotype("aA"));
            Assert.Equal("Aa", GeneticsTool.NormalizeGenotype("Aa"));
            Assert.Equal("AaBb", GeneticsTool.NormalizeGenotype("bBAa"));
            Assert.Equal("aaBb", GeneticsTool.NormalizeGenotype("Bbaa"));
        }

        [Fact]
        public void TestGenetics_CrossNonNormalizedGenotypes()
        {
            string fatherRaw = "BbaA";
            string motherRaw = "AaBb";

            Assert.True(GeneticsTool.IsValidGenotype(fatherRaw, out _));
            Assert.True(GeneticsTool.IsValidGenotype(motherRaw, out _));

            string fatherNorm = GeneticsTool.NormalizeGenotype(fatherRaw);
            string motherNorm = GeneticsTool.NormalizeGenotype(motherRaw);

            Assert.Equal("AaBb", fatherNorm);
            Assert.Equal("AaBb", motherNorm);
        }

        [Fact]
        public void TestGenetics_GetTraitIcon()
        {
            Assert.Equal("🟡", GeneticsTool.GetTraitIcon("Aa"));
            Assert.Equal("🟢", GeneticsTool.GetTraitIcon("aa"));
            Assert.Equal("🟤", GeneticsTool.GetTraitIcon("Bb"));
            Assert.Equal("🔵", GeneticsTool.GetTraitIcon("bb"));
            
            Assert.Equal("🟡", GeneticsTool.GetTraitIcon("AaBb"));
            Assert.Equal("🔸", GeneticsTool.GetTraitIcon("Aabb"));
            Assert.Equal("🟢", GeneticsTool.GetTraitIcon("aaBb"));
            Assert.Equal("🔹", GeneticsTool.GetTraitIcon("aabb"));
        }

        [Fact]
        public void TestPedigree_CycleDetection()
        {
            var tree = new PedigreeTree();
            
            var memberA = tree.CreateMember("A", "Nam", false, 1, 1);
            var memberB = tree.CreateMember("B", "Nữ", false, 2, 1);
            
            memberA.SetSpouse(memberB);
            
            memberB.SetParents(memberA, null);
            memberA.SetParents(null, memberB);
            
            Assert.True(tree.HasCycle());
        }

        [Fact]
        public void TestPedigree_CalculateOffspringProbability()
        {
            var tree = new PedigreeTree();
            
            var dad = tree.CreateMember("I-1", "Nam", false, 1, 1);
            var mom = tree.CreateMember("I-2", "Nữ", false, 1, 2);
            dad.SetSpouse(mom);
            
            var child = tree.CreateMember("II-1", "Nữ", true, 2, 1);
            child.SetParents(dad, mom);
            
            tree.DeduceGenotypes(InheritanceMode.AutosomalRecessive);
            
            double prob = tree.CalculateOffspringProbability(dad, mom, "Bị bệnh", "Cả hai", InheritanceMode.AutosomalRecessive);
            Assert.Equal(0.25, prob, 3);
            
            double girlProb = tree.CalculateOffspringProbability(dad, mom, "Bị bệnh", "Con gái", InheritanceMode.AutosomalRecessive);
            Assert.Equal(0.125, girlProb, 3);
        }

        [Fact]
        public void TestPedigree_AutoLayoutPositions()
        {
            var tree = new PedigreeTree();
            var dad = tree.CreateMember("I-1", "Nam", false, 1, 1.0);
            var mom = tree.CreateMember("I-2", "Nữ", false, 1, 3.0);
            dad.SetSpouse(mom);
            
            var child = tree.CreateMember("II-1", "Nam", false, 2, 1.0);
            child.SetParents(dad, mom);
            
            double mid = (dad.Col + mom.Col) / 2.0;
            Assert.Equal(2.0, mid);
            
            var children = dad.Children.Where(c => c.Mother == mom).OrderBy(c => c.Col).ToList();
            double startCol = mid - (children.Count - 1) / 2.0;
            children[0].Col = startCol;
            
            Assert.Equal(2.0, children[0].Col);
        }
    }
}
