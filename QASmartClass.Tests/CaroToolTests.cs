using System;
using System.Threading;
using System.Windows.Threading;
using QASmartClass.LearningTools.Models;
using QASmartClass.LearningTools.Views.Thinking;
using Xunit;

namespace QASmartClass.Tests
{
    public class CaroToolTests
    {
        private static Dispatcher? _dispatcher;

        private static void EnsureStaThread()
        {
            if (_dispatcher != null) return;

            var staReady = new ManualResetEvent(false);
            var thread = new Thread(() =>
            {
                _dispatcher = Dispatcher.CurrentDispatcher;
                staReady.Set();
                Dispatcher.Run();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

            staReady.WaitOne();
        }

        [Fact]
        public void Test_CaroToolRegisteredInRegistry()
        {
            var tool = ToolRegistry.GetById("caro_game");
            Assert.NotNull(tool);
            Assert.Equal("Cờ Caro & Gomoku Học Đường", tool!.Name);
            Assert.Equal(ToolCategory.Thinking, tool.Category);
            Assert.True(tool.IsInteractive);
            Assert.True(tool.IsAvailable);
        }

        [Fact]
        public void Test_CaroToolInstantiation_STA()
        {
            EnsureStaThread();

            Exception? ex = null;
            _dispatcher!.Invoke(() =>
            {
                try
                {
                    var control = new CaroTool();
                    Assert.NotNull(control);
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });

            Assert.Null(ex);
        }

        [Fact]
        public void Test_CaroWinnerCheckEngine_5InARow()
        {
            EnsureStaThread();

            _dispatcher!.Invoke(() =>
            {
                var tool = new CaroTool();
                var board = new CaroPieceColor[15, 15];

                // Create a 5-in-a-row horizontal line for X on row 7, cols 3 to 7
                for (int c = 3; c <= 7; c++)
                {
                    board[7, c] = CaroPieceColor.X;
                }

                // Check winner under Gomoku rule
                bool isWinGomoku = tool.CheckWinner(7, 5, board, CaroRuleMode.GomokuFree, out var winningLine);
                Assert.True(isWinGomoku);
                Assert.Equal(5, winningLine.Count);

                // Check winner under Vietnamese rule (Unblocked)
                bool isWinVN = tool.CheckWinner(7, 5, board, CaroRuleMode.Vietnamese, out _);
                Assert.True(isWinVN);

                // Block both ends with O at col 2 and col 8
                board[7, 2] = CaroPieceColor.O;
                board[7, 8] = CaroPieceColor.O;

                // Vietnamese rule MUST block win when blocked at both ends!
                bool isWinVNBlocked = tool.CheckWinner(7, 5, board, CaroRuleMode.Vietnamese, out _);
                Assert.False(isWinVNBlocked);
            });
        }

        [Fact]
        public void Test_WinningLine_AlwaysExactly5Cells()
        {
            EnsureStaThread();

            _dispatcher!.Invoke(() =>
            {
                var tool = new CaroTool();
                var board = new CaroPieceColor[15, 15];

                // Create a 7-in-a-row line
                for (int c = 2; c <= 8; c++)
                {
                    board[5, c] = CaroPieceColor.X;
                }

                bool isWin = tool.CheckWinner(5, 5, board, CaroRuleMode.GomokuFree, out var winningLine);
                Assert.True(isWin);
                Assert.Equal(5, winningLine.Count); // MUST trim to exactly 5 cells!
            });
        }

        [Fact]
        public void Test_DrawDetection_FullBoard()
        {
            EnsureStaThread();

            _dispatcher!.Invoke(() =>
            {
                var tool = new CaroTool();
                // Empty board initially -> not full
                Assert.False(tool.IsBoardFull());
            });
        }
    }
}
