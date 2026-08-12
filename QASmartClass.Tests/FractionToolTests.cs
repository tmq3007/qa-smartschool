using System;
using System.Reflection;
using Xunit;
using QASmartClass.LearningTools.Views.Math;

namespace QASmartClass.Tests
{
    public class FractionToolTests
    {
        private static void InvokeNormalizeFraction(ref int num, ref int den)
        {
            var method = typeof(FractionTool).GetMethod("NormalizeFraction", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new InvalidOperationException("NormalizeFraction method not found");
            
            object[] args = new object[] { num, den };
            method.Invoke(null, args);
            num = (int)args[0];
            den = (int)args[1];
        }

        private static int InvokeGcd(int a, int b)
        {
            var method = typeof(FractionTool).GetMethod("Gcd", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new InvalidOperationException("Gcd method not found");
            return (int)method.Invoke(null, new object[] { a, b });
        }

        private static int InvokeLcm(int a, int b)
        {
            var method = typeof(FractionTool).GetMethod("Lcm", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new InvalidOperationException("Lcm method not found");
            return (int)method.Invoke(null, new object[] { a, b });
        }

        private static string InvokeFracStr(int n, int d)
        {
            var method = typeof(FractionTool).GetMethod("FracStr", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new InvalidOperationException("FracStr method not found");
            return (string)method.Invoke(null, new object[] { n, d });
        }

        [Theory]
        [InlineData(3, -4, -3, 4)]
        [InlineData(-3, -4, 3, 4)]
        [InlineData(3, 4, 3, 4)]
        [InlineData(-3, 4, -3, 4)]
        [InlineData(0, -5, 0, 5)]
        public void NormalizeFraction_CorrectlyPushesNegativeToNumerator(int num, int den, int expectedNum, int expectedDen)
        {
            int n = num;
            int d = den;
            InvokeNormalizeFraction(ref n, ref d);
            Assert.Equal(expectedNum, n);
            Assert.Equal(expectedDen, d);
        }

        [Theory]
        [InlineData(12, 18, 6)]
        [InlineData(101, 103, 1)]
        [InlineData(0, 5, 5)]
        [InlineData(5, 0, 5)]
        [InlineData(-12, 18, 6)]
        [InlineData(12, -18, 6)]
        public void Gcd_CorrectlyCalculatesGreatestCommonDivisor(int a, int b, int expected)
        {
            int result = InvokeGcd(a, b);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(12, 18, 36)]
        [InlineData(5, 7, 35)]
        [InlineData(0, 5, 1)]
        [InlineData(5, 0, 1)]
        [InlineData(-12, 18, 36)]
        public void Lcm_CorrectlyCalculatesLeastCommonMultiple(int a, int b, int expected)
        {
            int result = InvokeLcm(a, b);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(3, 4, "3/4")]
        [InlineData(5, 1, "5")]
        [InlineData(-3, 1, "-3")]
        [InlineData(0, 1, "0")]
        [InlineData(0, 3, "0/3")]
        public void FracStr_FormatsFractionStringCorrectly(int n, int d, string expected)
        {
            string result = InvokeFracStr(n, d);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Test_GenerateQuestion_ReturnsValidQuestion()
        {
            Exception threadEx = null;
            var t = new System.Threading.Thread(() =>
            {
                try
                {
                    lock (typeof(System.Windows.Application))
                    {
                        try
                        {
                            var appField = typeof(System.Windows.Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                            var createdField = typeof(System.Windows.Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                            if (appField != null) appField.SetValue(null, null);
                            if (createdField != null) createdField.SetValue(null, false);
                        }
                        catch {}

                        _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
                        var app = new System.Windows.Application();
                        app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                        {
                            Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute)
                        });
                    }

                    var tool = new FractionTool();
                    var method = typeof(FractionTool).GetMethod("GenerateQuestion", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(method);

                    // Test topic 0 (Operations), difficulty 0 (Easy)
                    var q1 = (FractionTool.FractionQuestion)method.Invoke(tool, new object[] { 0, 0 });
                    Assert.NotNull(q1);
                    Assert.False(string.IsNullOrEmpty(q1.QuestionText));
                    Assert.False(q1.IsComparison);
                    Assert.True(q1.ExpectedDen > 0);

                    // Test topic 2 (Compare), difficulty 1 (Medium)
                    var q2 = (FractionTool.FractionQuestion)method.Invoke(tool, new object[] { 2, 1 });
                    Assert.NotNull(q2);
                    Assert.True(q2.IsComparison);
                    Assert.Contains(q2.ExpectedCompareSymbol, new[] { "<", "=", ">" });

                    // Test topic 3 (Mixed), difficulty 2 (Hard)
                    var q3 = (FractionTool.FractionQuestion)method.Invoke(tool, new object[] { 3, 2 });
                    Assert.NotNull(q3);
                    Assert.False(q3.IsComparison);
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });
            t.SetApartmentState(System.Threading.ApartmentState.STA);
            t.Start();
            t.Join();

            if (threadEx != null) throw threadEx;
        }
    }
}

