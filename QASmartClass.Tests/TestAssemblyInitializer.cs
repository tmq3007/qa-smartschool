using System.Runtime.CompilerServices;
using System.IO.Packaging;

namespace QASmartClass.Tests
{
    public static class TestAssemblyInitializer
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            // Register the pack URI scheme globally for all WPF unit tests in this assembly
            _ = PackUriHelper.UriSchemePack;
        }
    }
}
