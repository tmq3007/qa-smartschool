using System;
using System.Reflection;
using Xunit;
using QASmartClass.LearningTools.Views.Math;

namespace QASmartClass.Tests
{
    public class PrimeNumberToolTests
    {
        private static bool InvokeIsPrime(long n)
        {
            var method = typeof(PrimeNumberTool).GetMethod("IsPrime", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new InvalidOperationException("IsPrime method not found");
            return (bool)method.Invoke(null, new object[] { n });
        }

        private static long InvokeGcd(long a, long b)
        {
            var method = typeof(PrimeNumberTool).GetMethod("Gcd", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new InvalidOperationException("Gcd method not found");
            return (long)method.Invoke(null, new object[] { a, b });
        }

        [Theory]
        [InlineData(-10, false)]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, true)]
        [InlineData(3, true)]
        [InlineData(4, false)]
        [InlineData(97, true)]
        [InlineData(100, false)]
        [InlineData(999999999989, true)] // large prime
        public void IsPrime_ValidatesCorrectness(long input, bool expected)
        {
            bool result = InvokeIsPrime(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(12, 18, 6)]
        [InlineData(101, 103, 1)] // primes
        [InlineData(3233, 61, 61)]
        [InlineData(100, 10, 10)]
        public void Gcd_ValidatesCorrectness(long a, long b, long expected)
        {
            long result = InvokeGcd(a, b);
            Assert.Equal(expected, result);
        }
    }
}
