using System;
using System.IO;
using Xunit;
using QASmartClass.Services;

namespace QASmartClass.Tests.Services
{
    public class AppPathsTests
    {
        [Fact]
        public void RootDir_Not_Null_Or_Empty()
        {
            Assert.False(string.IsNullOrEmpty(AppPaths.RootDir));
        }

        [Fact]
        public void DatabaseFile_Ends_With_Db()
        {
            Assert.EndsWith(".db", AppPaths.DatabaseFile);
        }

        [Fact]
        public void DatabaseFile_Is_Inside_RootDir()
        {
            Assert.StartsWith(AppPaths.RootDir, AppPaths.DatabaseFile);
        }

        [Fact]
        public void BackupsDir_Not_Null_Or_Empty()
        {
            Assert.False(string.IsNullOrEmpty(AppPaths.BackupsDir));
        }

        [Fact]
        public void LogsDir_Is_Inside_RootDir()
        {
            Assert.StartsWith(AppPaths.RootDir, AppPaths.LogsDir);
        }
    }
}
