using System;
using Xunit;
using QASmartClass.LearningTools.Views.Math;

namespace QASmartClass.Tests
{
    public class IntegralToolTests
    {
        [Fact]
        public void Gcd_CorrectlyCalculatesGCD()
        {
            Assert.Equal(2, IntegralTool.Gcd(6, 4));
            Assert.Equal(1, IntegralTool.Gcd(3, 2));
            Assert.Equal(5, IntegralTool.Gcd(10, 5));
            Assert.Equal(3, IntegralTool.Gcd(-6, 9));
        }

        [Fact]
        public void FormatCoefficient_SimplifiesFractions()
        {
            // Case 1: Result is an integer
            var r1 = IntegralTool.FormatCoefficient(4, 2);
            Assert.Equal(2, r1.value);
            Assert.Equal("2", r1.text);

            var r2 = IntegralTool.FormatCoefficient(-3, 3);
            Assert.Equal(-1, r2.value);
            Assert.Equal("-", r2.text);

            var r3 = IntegralTool.FormatCoefficient(2, 2);
            Assert.Equal(1, r3.value);
            Assert.Equal("", r3.text);

            // Case 2: Fractions that can be simplified
            var r4 = IntegralTool.FormatCoefficient(2, 4);
            Assert.Equal(0.5, r4.value);
            Assert.Equal("(1/2)", r4.text);

            var r5 = IntegralTool.FormatCoefficient(-6, 4);
            Assert.Equal(-1.5, r5.value);
            Assert.Equal("-(3/2)", r5.text);

            // Case 3: Zero coeff
            var r6 = IntegralTool.FormatCoefficient(0, 5);
            Assert.Equal(0, r6.value);
            Assert.Equal("", r6.text);
        }

        [Fact]
        public void FormatLnTerm_CorrectlyFormats()
        {
            Assert.Equal("ln|x|", IntegralTool.FormatLnTerm(1));
            Assert.Equal("-ln|x|", IntegralTool.FormatLnTerm(-1));
            Assert.Equal("3·ln|x|", IntegralTool.FormatLnTerm(3));
            Assert.Equal("-2.5·ln|x|", IntegralTool.FormatLnTerm(-2.5));
            Assert.Equal("0", IntegralTool.FormatLnTerm(0));
        }

        [Fact]
        public void FormatAntideriv_FormatsPolynomialCorrectly()
        {
            // Case: f(x) = x^2 - 2x + 1 -> A=0, B=1, C=-2, D=1
            // Antiderivative: F(x) = (1/3)x^3 - x^2 + x
            string F1 = IntegralTool.FormatAntideriv(0, 1, -2, 1);
            Assert.Equal("(1/3)x^3 - x^2 + x", F1);

            // Case: f(x) = 4x^3 - 6x^2 + 2x - 3 -> A=4, B=-6, C=2, D=-3
            // Antiderivative: F(x) = x^4 - 2x^3 + x^2 - 3x
            string F2 = IntegralTool.FormatAntideriv(4, -6, 2, -3);
            Assert.Equal("x^4 - 2x^3 + x^2 - 3x", F2);

            // Case: f(x) = 0
            string F3 = IntegralTool.FormatAntideriv(0, 0, 0, 0);
            Assert.Equal("0", F3);
        }

        [Fact]
        public void CalculateSimpsonArea_CalculatesCorrectly()
        {
            // Case: f(x) = x^2 - 2x on [0, 3].
            // It crosses x-axis at x=2.
            // Area = ∫₀² |x²-2x| dx + ∫₂³ |x²-2x| dx = 4/3 + 4/3 = 8/3
            double area = IntegralTool.CalculateSimpsonArea(0, 1, -2, 0, 0, 3);
            Assert.Equal(8.0 / 3.0, area, 4); // delta check with 4 decimal places

            // Case: f(x) = 5 on [1, 4] -> area = 5 * (4 - 1) = 15
            double areaConst = IntegralTool.CalculateSimpsonArea(0, 0, 0, 5, 1, 4);
            Assert.Equal(15.0, areaConst, 4);
        }
    }
}
