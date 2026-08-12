using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using QASmartClass.LearningTools.Views.Math;
using QASmartClass.LearningTools.Helpers;

namespace QASmartClass.Tests
{
    public class MatrixToolTests
    {
        private static string InvokeFormatNumber(double v)
        {
            var method = typeof(MatrixTool).GetMethod("FormatNumber", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new InvalidOperationException("FormatNumber method not found");
            return (string)method.Invoke(null, new object[] { v });
        }

        private static double InvokeDeterminant(double[,] m, int n, List<string>? steps = null)
        {
            var method = typeof(MatrixTool).GetMethod("Determinant", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new InvalidOperationException("Determinant method not found");
            return (double)method.Invoke(null, new object[] { m, n, steps });
        }

        private static double[,] InvokeInverse(double[,] m, int n, double det)
        {
            var method = typeof(MatrixTool).GetMethod("Inverse", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new InvalidOperationException("Inverse method not found");
            return (double[,])method.Invoke(null, new object[] { m, n, det });
        }

        [Fact]
        public void FormatNumber_ValidatesFormattings()
        {
            Assert.Equal("5", InvokeFormatNumber(5.0));
            Assert.Equal("-2", InvokeFormatNumber(-2.0));
            Assert.Equal("1/3 (0.3333)", InvokeFormatNumber(1.0 / 3.0));
            Assert.Equal("-5/2 (-2.5)", InvokeFormatNumber(-2.5));
            Assert.Equal("0", InvokeFormatNumber(0.0));
        }

        [Fact]
        public void Determinant_2x2_ValidatesCorrectness()
        {
            var m = new double[,] { { 1, 2 }, { 3, 4 } };
            var steps = new List<string>();
            double det = InvokeDeterminant(m, 2, steps);
            Assert.Equal(-2, det);
            Assert.NotEmpty(steps);
            Assert.Contains("Det = (1 × 4) - (2 × 3) = -2", steps[0]);
        }

        [Fact]
        public void Determinant_3x3_WithNegatives_ValidatesCorrectnessAndSteps()
        {
            var m = new double[,] {
                { -2, 3, -1 },
                { 4, 0, 5 },
                { 2, 1, -3 }
            };
            var steps = new List<string>();
            double det = InvokeDeterminant(m, 3, steps);

            Assert.Equal(72, det);
            Assert.NotEmpty(steps);

            string stepText = steps[0];
            Assert.Contains("Det = -2×M₀₀ - 3×M₀₁ - 1×M₀₂", stepText);
            Assert.Contains("Minor M₀₀", stepText);
            Assert.Contains("Minor M₀₁", stepText);
            Assert.Contains("Minor M₀₂", stepText);
            Assert.Contains("-2×(-5) - 3×(-22) - 1×(4)", stepText);
            Assert.Contains("72", stepText);
        }

        [Fact]
        public void Inverse_3x3_ValidatesCorrectness()
        {
            var m = new double[,] {
                { 1, 2, 0 },
                { 0, 3, 0 },
                { 0, 0, 1 }
            };
            double det = InvokeDeterminant(m, 3);
            Assert.Equal(3, det);

            var inv = InvokeInverse(m, 3, det);
            Assert.Equal(1, inv[0, 0], 5);
            Assert.Equal(-2.0 / 3.0, inv[0, 1], 5);
            Assert.Equal(0, inv[0, 2], 5);
            Assert.Equal(0, inv[1, 0], 5);
            Assert.Equal(1.0 / 3.0, inv[1, 1], 5);
            Assert.Equal(0, inv[1, 2], 5);
            Assert.Equal(0, inv[2, 0], 5);
            Assert.Equal(0, inv[2, 1], 5);
            Assert.Equal(1, inv[2, 2], 5);
        }

        [Fact]
        public void TryParseDouble_WithFractions_ValidatesCorrectness()
        {
            Assert.True(ParsingHelper.TryParseDouble("1/2", out double v1));
            Assert.Equal(0.5, v1);

            Assert.True(ParsingHelper.TryParseDouble("-3/4", out double v2));
            Assert.Equal(-0.75, v2);

            Assert.False(ParsingHelper.TryParseDouble("5/0", out double _));
            Assert.False(ParsingHelper.TryParseDouble("1/2/3", out double _));
            Assert.False(ParsingHelper.TryParseDouble("2/", out double _));

            Assert.True(ParsingHelper.TryParseDouble("2.5/0.5", out double v6));
            Assert.Equal(5.0, v6);

            Assert.True(ParsingHelper.TryParseDouble("1,5/0.5", out double v7));
            Assert.Equal(3.0, v7);

            // New advanced mathematical parsing assertions
            Assert.True(ParsingHelper.TryParseDouble("pi/3", out double vPiDiv));
            Assert.Equal(Math.PI / 3.0, vPiDiv, 5);

            Assert.True(ParsingHelper.TryParseDouble("-pi/6", out double vPiNeg));
            Assert.Equal(-Math.PI / 6.0, vPiNeg, 5);

            Assert.True(ParsingHelper.TryParseDouble("sqrt(3)/2", out double vSqrtDiv));
            Assert.Equal(Math.Sqrt(3) / 2.0, vSqrtDiv, 5);

            Assert.True(ParsingHelper.TryParseDouble("-sqrt(2)/2", out double vSqrtNeg));
            Assert.Equal(-Math.Sqrt(2) / 2.0, vSqrtNeg, 5);

            Assert.True(ParsingHelper.TryParseDouble("2pi", out double v2Pi));
            Assert.Equal(2.0 * Math.PI, v2Pi, 5);

            Assert.True(ParsingHelper.TryParseDouble("pi*2", out double vPi2));
            Assert.Equal(Math.PI * 2.0, vPi2, 5);
        }

        [Fact]
        public void Determinant_5x5_ValidatesCorrectness()
        {
            var m = new double[,] {
                { 1, 0, 0, 0, 0 },
                { 0, 2, 0, 0, 0 },
                { 0, 0, 3, 0, 0 },
                { 0, 0, 0, 4, 0 },
                { 0, 0, 0, 0, 5 }
            };
            double det = InvokeDeterminant(m, 5);
            Assert.Equal(120, det);

            var m2 = new double[,] {
                { 2, 0, 0, 0, 0 },
                { 0, 3, 0, 0, 0 },
                { 0, 0, -1, 0, 0 },
                { 0, 0, 0, 4, 0 },
                { 0, 0, 0, 0, 0.5 }
            };
            double det2 = InvokeDeterminant(m2, 5);
            Assert.Equal(-12, det2);
        }
    }
}
