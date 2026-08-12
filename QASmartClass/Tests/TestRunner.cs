// Test runner — chạy từ command line
// dotnet run -- --test

using System;
using QASmartClass.Tests;

namespace QASmartClass.TestRunner
{
    public static class TestEntryPoint
    {
        public static void RunTests()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var result = IntegrationTest.RunAll();
            Console.WriteLine(result);
        }
    }
}
