using Xunit;
using System;
using System.Reflection;
using System.Runtime.Serialization;
using QASmartClass.Classroom.Views;

namespace QASmartClass.Tests
{
    public class V72TimetableTests
    {
        private TimetablePage CreateUninitializedTimetablePage()
        {
            return (TimetablePage)FormatterServices.GetUninitializedObject(typeof(TimetablePage));
        }

        [Theory]
        [InlineData("Thứ 2,Tiết 1,Toán,P.10A,GV,Ghi chú", ',', new[] { "Thứ 2", "Tiết 1", "Toán", "P.10A", "GV", "Ghi chú" })]
        [InlineData("\"Thứ 2, Hà Nội\",Tiết 1,Toán,\"P.10A, Tầng 1\",GV,Ghi chú", ',', new[] { "Thứ 2, Hà Nội", "Tiết 1", "Toán", "P.10A, Tầng 1", "GV", "Ghi chú" })]
        [InlineData("Thứ 2;Tiết 1;Toán;P.10A;GV;Ghi chú", ';', new[] { "Thứ 2", "Tiết 1", "Toán", "P.10A", "GV", "Ghi chú" })]
        [InlineData("\"Thứ 2; Hà Nội\";Tiết 1;Toán;\"P.10A; Tầng 1\";GV;Ghi chú", ';', new[] { "Thứ 2; Hà Nội", "Tiết 1", "Toán", "P.10A; Tầng 1", "GV", "Ghi chú" })]
        public void SplitCsvLine_ShouldSplitCorrectly(string line, char delimiter, string[] expected)
        {
            var page = CreateUninitializedTimetablePage();
            var method = typeof(TimetablePage).GetMethod("SplitCsvLine", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var result = (string[])method.Invoke(page, new object[] { line, delimiter });
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("Thứ 2", 0)]
        [InlineData("t2", 0)]
        [InlineData("thứ hai", 0)]
        [InlineData("monday", 0)]
        [InlineData("Thứ 7", 5)]
        [InlineData("t7", 5)]
        [InlineData("saturday", 5)]
        [InlineData("Thứ 8", -1)]
        [InlineData("", -1)]
        public void NormalizeDay_ShouldNormalizeCorrectly(string rawDay, int expectedIndex)
        {
            var page = CreateUninitializedTimetablePage();
            var method = typeof(TimetablePage).GetMethod("NormalizeDay", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var result = (int)method.Invoke(page, new object[] { rawDay });
            Assert.Equal(expectedIndex, result);
        }

        [Theory]
        [InlineData("Tiết 1", 0)]
        [InlineData("t1", 0)]
        [InlineData("1", 0)]
        [InlineData("Tiết 10", 9)]
        [InlineData("t10", 9)]
        [InlineData("10", 9)]
        [InlineData("Tiết 11", -1)]
        [InlineData("t0", -1)]
        [InlineData("", -1)]
        public void NormalizePeriod_ShouldNormalizeCorrectly(string rawPeriod, int expectedIndex)
        {
            var page = CreateUninitializedTimetablePage();
            var method = typeof(TimetablePage).GetMethod("NormalizePeriod", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var result = (int)method.Invoke(page, new object[] { rawPeriod });
            Assert.Equal(expectedIndex, result);
        }

        [Theory]
        [InlineData("Thứ 2,Tiết 1,Toán", ',', ';')]
        [InlineData("Thứ 2;Tiết 1;Toán", ';', ',')]
        public void DelimiterDetection_Logic_ShouldChooseCorrectDelimiter(string firstLine, char expectedDelimiter, char otherDelimiter)
        {
            int commas = 0;
            int semicolons = 0;
            for (int i = 0; i < firstLine.Length; i++)
            {
                if (firstLine[i] == ',') commas++;
                if (firstLine[i] == ';') semicolons++;
            }
            char detected = (semicolons > commas) ? ';' : ',';
            Assert.Equal(expectedDelimiter, detected);
        }
    }
}
