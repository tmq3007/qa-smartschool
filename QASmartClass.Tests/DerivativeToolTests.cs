using System;
using System.Reflection;
using Xunit;
using QASmartClass.LearningTools.Views.Math;

namespace QASmartClass.Tests
{
    public class DerivativeToolTests
    {
        private static string InvokeFormatTerm(double coeff, double power, string variable)
        {
            var method = typeof(DerivativeTool).GetMethod("FormatTerm", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new InvalidOperationException("FormatTerm method not found");
            return (string)method.Invoke(null, new object[] { coeff, power, variable });
        }

        private static string InvokeFormatLineEquation(double m, double c)
        {
            var method = typeof(DerivativeTool).GetMethod("FormatLineEquation", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new InvalidOperationException("FormatLineEquation method not found");
            return (string)method.Invoke(null, new object[] { m, c });
        }

        private static string InvokeFormatPoly(double a, double b, double c, double d)
        {
            var method = typeof(DerivativeTool).GetMethod("FormatPoly", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new InvalidOperationException("FormatPoly method not found");
            return (string)method.Invoke(null, new object[] { a, b, c, d });
        }

        private static string InvokeFormatTermLaTeX(double coeff, double power, string variable)
        {
            var method = typeof(DerivativeTool).GetMethod("FormatTermLaTeX", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new InvalidOperationException("FormatTermLaTeX method not found");
            return (string)method.Invoke(null, new object[] { coeff, power, variable });
        }

        private static string InvokeFormatPolyLaTeX(double a, double b, double c, double d)
        {
            var method = typeof(DerivativeTool).GetMethod("FormatPolyLaTeX", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new InvalidOperationException("FormatPolyLaTeX method not found");
            return (string)method.Invoke(null, new object[] { a, b, c, d });
        }

        [Theory]
        [InlineData(3, 2, "x", "3x²")]
        [InlineData(1, 3, "x", "x³")]
        [InlineData(-1, 2, "x", "-x²")]
        [InlineData(0, 5, "x", "0")]
        [InlineData(2.5, 0, "x", "2.5")]
        [InlineData(-4.2, 1, "x", "-4.2x")]
        public void FormatTerm_ReturnsExpectedString(double coeff, double power, string variable, string expected)
        {
            string result = InvokeFormatTerm(coeff, power, variable);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(2, 3, "y = 2x + 3")]
        [InlineData(1, -5, "y = x - 5")]
        [InlineData(-1, 0, "y = -x")]
        [InlineData(0, 4.5, "y = 4.5")]
        [InlineData(0.5, -0.25, "y = 0.5x - 0.25")]
        [InlineData(0, 0, "y = 0")]
        public void FormatLineEquation_ReturnsExpectedString(double m, double c, string expected)
        {
            string result = InvokeFormatLineEquation(m, c);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(1, -3, 2, 0, "x³ - 3x² + 2x")]
        [InlineData(0, 1, -4, 3, "x² - 4x + 3")]
        [InlineData(0, 0, 5, -2, "5x - 2")]
        [InlineData(-2, 0, 0, 10, "-2x³ + 10")]
        public void FormatPoly_ReturnsExpectedString(double a, double b, double c, double d, string expected)
        {
            string result = InvokeFormatPoly(a, b, c, d);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(3, 2, "x", "3x^2")]
        [InlineData(1, 3, "x", "x^3")]
        [InlineData(-1, 2, "x", "-x^2")]
        [InlineData(0, 5, "x", "0")]
        [InlineData(2.5, 0, "x", "2.5")]
        [InlineData(-4.2, 1, "x", "-4.2x")]
        [InlineData(3, -2, "x", "3x^{-2}")]
        public void FormatTermLaTeX_ReturnsExpectedString(double coeff, double power, string variable, string expected)
        {
            string result = InvokeFormatTermLaTeX(coeff, power, variable);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(1, -3, 2, 0, "x^3 - 3x^2 + 2x")]
        [InlineData(0, 1, -4, 3, "x^2 - 4x + 3")]
        [InlineData(0, 0, 5, -2, "5x - 2")]
        [InlineData(-2, 0, 0, 10, "-2x^3 + 10")]
        public void FormatPolyLaTeX_ReturnsExpectedString(double a, double b, double c, double d, string expected)
        {
            string result = InvokeFormatPolyLaTeX(a, b, c, d);
            Assert.Equal(expected, result);
        }
    }
}
