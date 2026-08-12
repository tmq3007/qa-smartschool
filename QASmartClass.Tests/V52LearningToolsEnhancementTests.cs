using Xunit;
using System;
using System.Linq;
using System.IO;
using QASmartClass.Services;
using QASmartClass.Data;
using QASmartClass.LearningTools.Models;
using QASmartClass.LearningTools.Helpers;

namespace QASmartClass.Tests
{
    public class V52LearningToolsEnhancementTests
    {
        [Fact]
        public void Test_AllTools_WhiteboardConfiguration()
        {
            // 1. Verify that ToolRegistry has all 72 tools registered
            var allTools = ToolRegistry.AllTools;
            Assert.NotNull(allTools);
            Assert.NotEmpty(allTools);
            Assert.True(allTools.Count >= 72, $"Expected at least 72 tools, found {allTools.Count}");

            // 2. Verify iq_quiz has HideWhiteboard set to false
            var iqQuiz = allTools.FirstOrDefault(t => t.Id == "iq_quiz");
            Assert.NotNull(iqQuiz);
            Assert.False(iqQuiz.HideWhiteboard);
            Assert.False(TeachingActionHelper.IsGameTool("iq_quiz"));

            // 3. Verify mental_math has HideWhiteboard set to false (whiteboard enabled)
            var mentalMath = allTools.FirstOrDefault(t => t.Id == "mental_math");
            Assert.NotNull(mentalMath);
            Assert.False(mentalMath.HideWhiteboard);
            Assert.False(TeachingActionHelper.IsGameTool("mental_math"));

            // 4. Verify sudoku has HideWhiteboard set to false (whiteboard enabled)
            var sudoku = allTools.FirstOrDefault(t => t.Id == "sudoku");
            Assert.NotNull(sudoku);
            Assert.False(sudoku.HideWhiteboard);
            Assert.False(TeachingActionHelper.IsGameTool("sudoku"));

            // 5. Verify memory_game has HideWhiteboard set to false (whiteboard enabled)
            var memoryGame = allTools.FirstOrDefault(t => t.Id == "memory_game");
            Assert.NotNull(memoryGame);
            Assert.False(memoryGame.HideWhiteboard);
            Assert.False(TeachingActionHelper.IsGameTool("memory_game"));

            // 6. Verify that no tools have HideWhiteboard = true
            var hiddenWhiteboardTools = allTools.Where(t => t.HideWhiteboard).Select(t => t.Id).ToList();
            Assert.Empty(hiddenWhiteboardTools);
        }

        [Fact]
        public void Test_Database_SelfHealing()
        {
            var tempDb = Path.Combine(Path.GetTempPath(), $"smartclass_test_heal_{Guid.NewGuid():N}.db");
            var tempVer = Path.Combine(Path.GetTempPath(), $"smartclass_test_heal_{Guid.NewGuid():N}.txt");

            var originalDbFile = AppPaths.DatabaseFile;
            var originalVersionFile = AppPaths.DbVersionFile;

            AppPaths.DatabaseFile = tempDb;
            AppPaths.DbVersionFile = tempVer;

            var hexKey = Convert.ToHexString(QASmartClass.Data.DbEncryptionKeyManager.GetOrInitializeKey());

            try
            {
                // 1. Simulate a corrupt database file containing only a Notebooks table
                Directory.CreateDirectory(Path.GetDirectoryName(tempDb)!);
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={tempDb};Password={hexKey}"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "CREATE TABLE Notebooks (Id TEXT PRIMARY KEY)";
                        cmd.ExecuteNonQuery();
                    }
                }
                File.WriteAllText(tempVer, "5.46.0");

                // Ensure the corrupt database exists with only Notebooks table
                Assert.True(File.Exists(tempDb));
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={tempDb};Password={hexKey}"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Students'";
                        var count = cmd.ExecuteScalar();
                        Assert.Null(count);
                    }
                }

                // 2. Run DbMigrator.Migrate - it should detect missing Students table, delete corrupt DB, and recreate
                using (var db = new AppDbContext())
                {
                    DbMigrator.Migrate(db, "5.46.0");
                }

                // 3. Verify database successfully healed and recreated all tables (including Students)
                Assert.True(File.Exists(tempDb));
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={tempDb};Password={hexKey}"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Students'";
                        var count = Convert.ToInt32(cmd.ExecuteScalar());
                        Assert.True(count > 0, "Students table should have been recreated by self-healing");
                    }
                }
            }
            finally
            {
                // Clean up temp files
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                try { if (File.Exists(tempDb)) File.Delete(tempDb); } catch { }
                try { if (File.Exists(tempVer)) File.Delete(tempVer); } catch { }
                
                AppPaths.DatabaseFile = originalDbFile;
                AppPaths.DbVersionFile = originalVersionFile;
            }
        }

        [Fact]
        public void Test_StatisticsTool_ParseData_And_QuartilesSGK()
        {
            // 1. Verify ParseData multi-tier logic
            var methodParse = typeof(QASmartClass.LearningTools.Views.Math.StatisticsTool)
                .GetMethod("ParseData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(methodParse);

            // Case A: Comma-separated list with dot decimals
            var resA = (double[])methodParse.Invoke(null, new object[] { "4.5, 5.5, 6, 7.8" });
            Assert.Equal(new double[] { 4.5, 5.5, 6, 7.8 }, resA);

            // Case B: Semicolon-separated list with comma decimals
            var resB = (double[])methodParse.Invoke(null, new object[] { "4,5; 5,5; 6; 7,8" });
            Assert.Equal(new double[] { 4.5, 5.5, 6, 7.8 }, resB);

            // Case C: Whitespace-separated list with comma decimals
            var resC = (double[])methodParse.Invoke(null, new object[] { "4,5 5,5 6 7,8" });
            Assert.Equal(new double[] { 4.5, 5.5, 6, 7.8 }, resC);

            // Case D: Comma-separated list with no spaces
            var resD = (double[])methodParse.Invoke(null, new object[] { "4.5,5.5,6" });
            Assert.Equal(new double[] { 4.5, 5.5, 6 }, resD);

            // Case E: Semicolon-separated list with mixed decimals
            var resE = (double[])methodParse.Invoke(null, new object[] { "4.5; 5,5; 6" });
            Assert.Equal(new double[] { 4.5, 5.5, 6 }, resE);

            // 2. Verify GetQuartilesSGK textbook algorithm
            var methodQuartiles = typeof(QASmartClass.LearningTools.Views.Math.StatisticsTool)
                .GetMethod("GetQuartilesSGK", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(methodQuartiles);

            // Case 1: n = 5 (odd) -> [1, 2, 3, 4, 5]
            var qOdd5 = ((double q1, double q3))methodQuartiles.Invoke(null, new object[] { new double[] { 1, 2, 3, 4, 5 } });
            Assert.Equal(1.5, qOdd5.q1); // Median of lower [1, 2]
            Assert.Equal(4.5, qOdd5.q3); // Median of upper [4, 5]

            // Case 2: n = 7 (odd) -> [1, 2, 3, 4, 5, 6, 7]
            var qOdd7 = ((double q1, double q3))methodQuartiles.Invoke(null, new object[] { new double[] { 1, 2, 3, 4, 5, 6, 7 } });
            Assert.Equal(2.0, qOdd7.q1); // Median of lower [1, 2, 3]
            Assert.Equal(6.0, qOdd7.q3); // Median of upper [5, 6, 7]

            // Case 3: n = 6 (even) -> [1, 2, 3, 4, 5, 6]
            var qEven6 = ((double q1, double q3))methodQuartiles.Invoke(null, new object[] { new double[] { 1, 2, 3, 4, 5, 6 } });
            Assert.Equal(2.0, qEven6.q1); // Median of lower [1, 2, 3]
            Assert.Equal(5.0, qEven6.q3); // Median of upper [4, 5, 6]

            // Case 4: n = 8 (even) -> [3, 4, 6, 7, 8, 9, 10, 12]
            var qEven8 = ((double q1, double q3))methodQuartiles.Invoke(null, new object[] { new double[] { 3, 4, 6, 7, 8, 9, 10, 12 } });
            Assert.Equal(5.0, qEven8.q1); // Median of lower [3, 4, 6, 7]
            Assert.Equal(9.5, qEven8.q3); // Median of upper [8, 9, 10, 12]
        }
    }
}
