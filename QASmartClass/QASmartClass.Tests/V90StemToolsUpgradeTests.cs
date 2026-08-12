using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using QASmartClass.LearningTools.Views.Science;
using Xunit;

namespace QASmartClass.Tests
{
    public class V90StemToolsUpgradeTests
    {
        private void RunOnStaThread(Action action)
        {
            Exception? ex = null;
            var t = new Thread(() =>
            {
                try
                {
                    action();
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
        public void TestConstantsTool_Initialization_And_UniqueSecId()
        {
            RunOnStaThread(() =>
            {
                // Ensure Application exists (needed for WPF controls using resources)
                if (System.Windows.Application.Current == null)
                {
                    try
                    {
                        var app = new System.Windows.Application();
                    }
                    catch { }
                }

                var tool = new ConstantsTool();
                Assert.NotNull(tool);

                // Use reflection to access the AllConstants field
                var allConstantsField = typeof(ConstantsTool).GetField("AllConstants", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(allConstantsField);

                var allConstantsValue = allConstantsField.GetValue(null);
                Assert.NotNull(allConstantsValue);

                // It's a List<(string Symbol, string Name, string Value, string CopyValue, string Unit, string Category, string Color, string Desc)>
                // We can cast it or iterate using reflection or dynamic
                var list = (System.Collections.IEnumerable)allConstantsValue;
                var secIds = new HashSet<string>();
                var elements = new List<(string Symbol, string Category, string SecId)>();

                var normalizeMethod = typeof(ConstantsTool).GetMethod("NormalizeString", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(normalizeMethod);

                foreach (var item in list)
                {
                    // Access fields via reflection
                    var type = item.GetType();
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    
                    var symbol = (string)type.GetField("Item1").GetValue(item);
                    var name = (string)type.GetField("Item2").GetValue(item);
                    var value = (string)type.GetField("Item3").GetValue(item);
                    var copyValue = (string)type.GetField("Item4").GetValue(item);
                    var unit = (string)type.GetField("Item5").GetValue(item);
                    var cat = (string)type.GetField("Item6").GetValue(item);

                    string normCat = (string)normalizeMethod.Invoke(null, new object[] { cat });
                    string normSymbol = (string)normalizeMethod.Invoke(null, new object[] { symbol });
                    string secId = $"{normCat}_{normSymbol}";

                    elements.Add((symbol, cat, secId));

                    // Verify copyValue is standard float / number
                    if (symbol == "c") Assert.Equal("299792458", copyValue);
                    if (symbol == "h") Assert.Equal("6.626e-34", copyValue);
                    if (symbol == "e" && cat == "Hóa học") Assert.Equal("1.602e-19", copyValue);
                    if (symbol == "e" && cat == "Toán học") Assert.Equal("2.71828182845905", copyValue);

                    secIds.Add(secId);
                }

                // Verify that there are no duplicate secIds
                Assert.Equal(elements.Count, secIds.Count);

                // Specific collision check: "e" in Hóa học and Toán học
                var hoaHocE = elements.FirstOrDefault(x => x.Symbol == "e" && x.Category == "Hóa học");
                var toanHocE = elements.FirstOrDefault(x => x.Symbol == "e" && x.Category == "Toán học");
                Assert.NotNull(hoaHocE.Symbol);
                Assert.NotNull(toanHocE.Symbol);
                Assert.NotEqual(hoaHocE.SecId, toanHocE.SecId);

                // Check that secId contains no accents/diacritics
                Assert.Contains("hoa_hoc_e", hoaHocE.SecId);
                Assert.Contains("toan_hoc_e", toanHocE.SecId);

                // Specific collision check: "G" and "g" in Vật lý
                var vatLyG = elements.FirstOrDefault(x => x.Symbol == "G" && x.Category == "Vật lý");
                var vatLyg = elements.FirstOrDefault(x => x.Symbol == "g" && x.Category == "Vật lý");
                Assert.NotNull(vatLyG.Symbol);
                Assert.NotNull(vatLyg.Symbol);
                Assert.NotEqual(vatLyG.SecId, vatLyg.SecId);
                Assert.Contains("vat_ly_g_cap", vatLyG.SecId);
                Assert.Contains("vat_ly_g", vatLyg.SecId);
            });
        }

        [Fact]
        public void TestNormalizeString_RemovesDiacriticsAndSpecialSymbols()
        {
            var normalizeMethod = typeof(ConstantsTool).GetMethod("NormalizeString", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(normalizeMethod);

            string? result1 = (string?)normalizeMethod.Invoke(null, new object[] { "Hóa học" });
            Assert.Equal("hoa_hoc", result1);

            string? result2 = (string?)normalizeMethod.Invoke(null, new object[] { "Thiên văn" });
            Assert.Equal("thien_van", result2);

            string? result3 = (string?)normalizeMethod.Invoke(null, new object[] { "μ₀" });
            Assert.Equal("mu_0", result3);

            string? result4 = (string?)normalizeMethod.Invoke(null, new object[] { "ε₀" });
            Assert.Equal("ep_0", result4);
        }
    }
}
