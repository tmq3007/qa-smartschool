using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Xunit;
using QASmartClass.LearningTools.Views.Thinking;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.Tests
{
    public class ChessToolTests
    {
        private static Thread? _staThread;
        private static System.Windows.Threading.Dispatcher? _dispatcher;
        private static readonly object _lock = new();

        private static void EnsureStaThread()
        {
            lock (_lock)
            {
                if (_staThread == null)
                {
                    var readyEvent = new ManualResetEvent(false);
                    _staThread = new Thread(() =>
                    {
                        _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
                        try
                        {
                            var appField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                            var createdField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                            if (appField != null) appField.SetValue(null, null);
                            if (createdField != null) createdField.SetValue(null, false);
                        }
                        catch { }

                        Application? app = null;
                        try
                        {
                            app = new Application();
                        }
                        catch
                        {
                            app = Application.Current;
                        }

                        if (app != null)
                        {
                            try
                            {
                                bool hasTokens = false;
                                foreach (var dict in app.Resources.MergedDictionaries)
                                {
                                    if (dict.Source != null && dict.Source.OriginalString.Contains("DesignTokens.xaml"))
                                    {
                                        hasTokens = true;
                                        break;
                                    }
                                }

                                if (!hasTokens)
                                {
                                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                                    {
                                        Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute)
                                    });
                                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                                    {
                                        Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/Styles.xaml", UriKind.Absolute)
                                    });
                                }
                            }
                            catch { }
                        }

                        _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                        readyEvent.Set();
                        System.Windows.Threading.Dispatcher.Run();
                    });

                    _staThread.SetApartmentState(ApartmentState.STA);
                    _staThread.IsBackground = true;
                    _staThread.Start();
                    readyEvent.WaitOne();
                }
            }
        }

        [Fact]
        public void Test_ChessToolRegisteredInRegistry()
        {
            var tool = ToolRegistry.GetById("chess_game");
            Assert.NotNull(tool);
            Assert.Equal("Cờ Vua Chiến Thuật", tool!.Name);
            Assert.Equal(ToolCategory.Thinking, tool.Category);
            Assert.True(tool.IsInteractive);
            Assert.True(tool.IsAvailable);
        }

        [Fact]
        public void Test_ChessPieceSymbolsAndValues()
        {
            var whiteKing = new ChessPiece(PieceType.King, PieceColor.White);
            var blackQueen = new ChessPiece(PieceType.Queen, PieceColor.Black);
            var whitePawn = new ChessPiece(PieceType.Pawn, PieceColor.White);
            var blackKnight = new ChessPiece(PieceType.Knight, PieceColor.Black);

            Assert.Equal("♔", whiteKing.Symbol);
            Assert.Equal("♛", blackQueen.Symbol);
            Assert.Equal("♙", whitePawn.Symbol);
            Assert.Equal("♞", blackKnight.Symbol);

            Assert.Equal(200, whiteKing.Value);
            Assert.Equal(9, blackQueen.Value);
            Assert.Equal(1, whitePawn.Value);
            Assert.Equal(3, blackKnight.Value);
        }

        [Fact]
        public void Test_ChessToolInstantiation_STA()
        {
            EnsureStaThread();

            Exception? ex = null;
            _dispatcher!.Invoke(() =>
            {
                try
                {
                    var control = new ChessTool();
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
        public void Test_DynamicMoveClassificationEngine()
        {
            EnsureStaThread();

            _dispatcher!.Invoke(() =>
            {
                var tool = new ChessTool();
                var pawn = new ChessPiece(PieceType.Pawn, PieceColor.White);
                var queen = new ChessPiece(PieceType.Queen, PieceColor.Black);

                // Capturing Queen (value 9) with Pawn (value 1) MUST classify as Brilliant (!!)
                var quality = tool.ClassifyMove(6, 4, 1, 3, pawn, queen);
                Assert.Equal(MoveQuality.Brilliant, quality);
            });
        }

        [Fact]
        public void Test_LegalMoveRestrictionEngine()
        {
            EnsureStaThread();

            _dispatcher!.Invoke(() =>
            {
                var tool = new ChessTool();
                
                // Test 1: White Pawn on starting square e2 (row 6, col 4)
                // Allowed pseudo/legal moves: e3 (row 5, col 4) and e4 (row 4, col 4) only (2 moves total)
                // Cannot move backward, sideways, or diagonally (since no black piece to capture)
                var pawnMoves = tool.GetLegalMovesForSquareForTest(6, 4);
                Assert.Equal(2, pawnMoves.Count);
                Assert.Contains((5, 4), pawnMoves);
                Assert.Contains((4, 4), pawnMoves);

                // Test 2: White King on e1 (row 7, col 4) surrounded by friendly pieces
                // Initial position: Bishop f1, Knight g1 block Kingside castling
                // Knight b1, Bishop c1, Queen d1 block Queenside castling
                // So King has 0 legal moves including castling
                var kingMoves = tool.GetLegalMovesForSquareForTest(7, 4);
                Assert.Empty(kingMoves);
            });
        }

        // ═══════════════════════════════════════════════════════════
        // P0-01: CASTLING TESTS (Luật FIDE Điều 3.8)
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void Test_P0_01_CastlingKingside_WhenPathClear()
        {
            EnsureStaThread();

            _dispatcher!.Invoke(() =>
            {
                var tool = new ChessTool();

                // Access private _board via reflection to set up a custom position
                var boardField = typeof(ChessTool).GetField("_board", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(boardField);

                var board = new ChessPiece?[8, 8];
                // White King on e1, White Rook on h1 (both unmoved)
                board[7, 4] = new ChessPiece(PieceType.King, PieceColor.White);
                board[7, 7] = new ChessPiece(PieceType.Rook, PieceColor.White);
                // Black King on e8 (required to avoid null king checks)
                board[0, 4] = new ChessPiece(PieceType.King, PieceColor.Black);
                // f1 and g1 are empty → Kingside castling should be available

                boardField!.SetValue(tool, board);

                var kingMoves = tool.GetLegalMovesForSquareForTest(7, 4);
                // King should be able to move to d1(7,3), d2(6,3), e2(6,4), f2(6,5), f1(7,5) AND castle to g1(7,6)
                Assert.Contains((7, 6), kingMoves); // Kingside castling target square
            });
        }

        [Fact]
        public void Test_P0_01_CastlingBlocked_WhenKingMoved()
        {
            EnsureStaThread();

            _dispatcher!.Invoke(() =>
            {
                var tool = new ChessTool();

                var boardField = typeof(ChessTool).GetField("_board", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(boardField);

                var board = new ChessPiece?[8, 8];
                // White King on e1 (HAS MOVED), White Rook on h1
                var king = new ChessPiece(PieceType.King, PieceColor.White) { HasMoved = true };
                board[7, 4] = king;
                board[7, 7] = new ChessPiece(PieceType.Rook, PieceColor.White);
                board[0, 4] = new ChessPiece(PieceType.King, PieceColor.Black);

                boardField!.SetValue(tool, board);

                var kingMoves = tool.GetLegalMovesForSquareForTest(7, 4);
                // King has moved → NO castling allowed
                Assert.DoesNotContain((7, 6), kingMoves);
                Assert.DoesNotContain((7, 2), kingMoves);
            });
        }

        [Fact]
        public void Test_P0_01_CastlingQueenside_WhenPathClear()
        {
            EnsureStaThread();

            _dispatcher!.Invoke(() =>
            {
                var tool = new ChessTool();

                var boardField = typeof(ChessTool).GetField("_board", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(boardField);

                var board = new ChessPiece?[8, 8];
                // White King on e1, White Rook on a1 (both unmoved)
                board[7, 4] = new ChessPiece(PieceType.King, PieceColor.White);
                board[7, 0] = new ChessPiece(PieceType.Rook, PieceColor.White);
                // b1, c1, d1 are empty → Queenside castling should be available
                board[0, 4] = new ChessPiece(PieceType.King, PieceColor.Black);

                boardField!.SetValue(tool, board);

                var kingMoves = tool.GetLegalMovesForSquareForTest(7, 4);
                Assert.Contains((7, 2), kingMoves); // Queenside castling target square
            });
        }

        // ═══════════════════════════════════════════════════════════
        // P0-05: OPENING NAME FIX TEST
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void Test_P0_05_NoFalseOpeningName()
        {
            // Verify that the old hardcoded "Italian Game" text no longer appears in the source code
            string sourceCode = System.IO.File.ReadAllText(
                @"d:\JOB\QA SmartClass -062026\QASmartClass\LearningTools\Views\Thinking\ChessTool.xaml.cs");
            
            Assert.DoesNotContain("Italian Game", sourceCode);
            Assert.DoesNotContain("Ruy Lopez", sourceCode);
            Assert.DoesNotContain("Main Game Phase", sourceCode);
            // Verify new correct labels exist
            Assert.Contains("Khai cuộc", sourceCode);
            Assert.Contains("Trung cuộc", sourceCode);
            Assert.Contains("Tàn cuộc", sourceCode);
        }

        // ═══════════════════════════════════════════════════════════
        // P0-04: STOCKFISH BRANDING REMOVAL TEST
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void Test_P0_04_NoStockfishBranding()
        {
            // Verify "Stockfish" does not appear in C# code
            string csCode = System.IO.File.ReadAllText(
                @"d:\JOB\QA SmartClass -062026\QASmartClass\LearningTools\Views\Thinking\ChessTool.xaml.cs");
            Assert.DoesNotContain("Stockfish", csCode);

            // Verify "Stockfish" does not appear in XAML
            string xamlCode = System.IO.File.ReadAllText(
                @"d:\JOB\QA SmartClass -062026\QASmartClass\LearningTools\Views\Thinking\ChessTool.xaml");
            Assert.DoesNotContain("Stockfish", xamlCode);
        }

        // ═══════════════════════════════════════════════════════════
        // P0-07: ROOK DESCRIPTION FIX TEST
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void Test_P0_07_RookDescriptionCorrect()
        {
            string xamlCode = System.IO.File.ReadAllText(
                @"d:\JOB\QA SmartClass -062026\QASmartClass\LearningTools\Views\Thinking\ChessTool.xaml");
            
            // Must NOT contain the old incorrect text "ngang &amp; ngang"
            Assert.DoesNotContain("ngang &amp; ngang", xamlCode);
            // Must contain the correct text "ngang &amp; dọc"
            Assert.Contains("ngang &amp; dọc", xamlCode);
        }

        // ═══════════════════════════════════════════════════════════
        // P1-01: INSUFFICIENT MATERIAL TEST (Luật FIDE Điều 5.2.2)
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void Test_P1_01_InsufficientMaterial_KvsK()
        {
            EnsureStaThread();

            _dispatcher!.Invoke(() =>
            {
                var tool = new ChessTool();
                var boardField = typeof(ChessTool).GetField("_board", BindingFlags.NonPublic | BindingFlags.Instance);
                var isInsufMethod = typeof(ChessTool).GetMethod("IsInsufficientMaterial", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(boardField);
                Assert.NotNull(isInsufMethod);

                var board = new ChessPiece?[8, 8];
                // Only King vs King
                board[7, 4] = new ChessPiece(PieceType.King, PieceColor.White);
                board[0, 4] = new ChessPiece(PieceType.King, PieceColor.Black);

                boardField!.SetValue(tool, board);
                bool result = (bool)isInsufMethod!.Invoke(tool, null)!;

                Assert.True(result, "King vs King must be classified as Insufficient Material (Draw)");
            });
        }

        // ═══════════════════════════════════════════════════════════
        // P1-02: FEN AND PGN FORMAT TESTS
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void Test_P1_02_FEN_Format_InitialPosition()
        {
            EnsureStaThread();

            _dispatcher!.Invoke(() =>
            {
                var tool = new ChessTool();
                var fenMethod = typeof(ChessTool).GetMethod("GenerateFEN", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(fenMethod);

                string fen = (string)fenMethod!.Invoke(tool, null)!;
                // Initial FEN must contain full castling rights KQkq
                Assert.Equal("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", fen);
            });
        }

        [Fact]
        public void Test_P1_02_PGN_Format_NoFakeFallback()
        {
            EnsureStaThread();

            _dispatcher!.Invoke(() =>
            {
                var tool = new ChessTool();
                var pgnMethod = typeof(ChessTool).GetMethod("GeneratePGN", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(pgnMethod);

                string pgn = (string)pgnMethod!.Invoke(tool, null)!;
                // Empty game PGN must NOT contain fake hardcoded move "1. e4 e5 2. Nf3 Nc6 3. Bc4"
                Assert.DoesNotContain("1. e4 e5 2. Nf3 Nc6 3. Bc4", pgn);
                Assert.Contains("*", pgn);
            });
        }

        // ═══════════════════════════════════════════════════════════
        // P1-08: NO NULL CAST PATTERN TEST
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void Test_P1_08_NoNullCastPatternInSource()
        {
            string sourceCode = System.IO.File.ReadAllText(
                @"d:\JOB\QA SmartClass -062026\QASmartClass\LearningTools\Views\Thinking\ChessTool.xaml.cs");

            Assert.DoesNotContain("(MouseButtonEventArgs)null!", sourceCode);
        }

        // ═══════════════════════════════════════════════════════════
        // P2-01: PIECE-SQUARE TABLES POSITIONAL AWARENESS TEST
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void Test_P2_01_PositionalAwareness_CenterPawnEvaluatedHigher()
        {
            EnsureStaThread();

            _dispatcher!.Invoke(() =>
            {
                var tool = new ChessTool();
                var evalMethod = typeof(ChessTool).GetMethod("EvaluateBoard", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(evalMethod);

                var boardCenterPawn = new ChessPiece?[8, 8];
                boardCenterPawn[4, 4] = new ChessPiece(PieceType.Pawn, PieceColor.White); // e4 pawn

                var boardEdgePawn = new ChessPiece?[8, 8];
                boardEdgePawn[6, 0] = new ChessPiece(PieceType.Pawn, PieceColor.White); // a2 pawn

                int centerEval = (int)evalMethod!.Invoke(tool, new object[] { boardCenterPawn })!;
                int edgeEval = (int)evalMethod!.Invoke(tool, new object[] { boardEdgePawn })!;

                // Center pawn e4 MUST have higher positional score than edge pawn a2
                Assert.True(centerEval > edgeEval, $"Center pawn score ({centerEval}) should be higher than edge pawn score ({edgeEval})");
            });
        }

        // ═══════════════════════════════════════════════════════════
        // P2-03: EDUCATIONAL PUZZLES LOAD TEST
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void Test_P2_03_PuzzleLoading_AllPuzzlesValid()
        {
            EnsureStaThread();

            _dispatcher!.Invoke(() =>
            {
                var tool = new ChessTool();
                var loadPuzzleMethod = typeof(ChessTool).GetMethod("LoadPuzzle", BindingFlags.NonPublic | BindingFlags.Instance);
                var boardField = typeof(ChessTool).GetField("_board", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(loadPuzzleMethod);
                Assert.NotNull(boardField);

                for (int i = 0; i <= 4; i++)
                {
                    loadPuzzleMethod!.Invoke(tool, new object[] { i });
                    var board = (ChessPiece?[,])boardField!.GetValue(tool)!;
                    Assert.NotNull(board);

                    // Check that King exists on board for both colors
                    bool whiteKingExists = false, blackKingExists = false;
                    for (int r = 0; r < 8; r++)
                    {
                        for (int c = 0; c < 8; c++)
                        {
                            if (board[r, c]?.Type == PieceType.King)
                            {
                                if (board[r, c]?.Color == PieceColor.White) whiteKingExists = true;
                                if (board[r, c]?.Color == PieceColor.Black) blackKingExists = true;
                            }
                        }
                    }
                    Assert.True(whiteKingExists, $"Puzzle {i} must have White King");
                    Assert.True(blackKingExists, $"Puzzle {i} must have Black King");
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // P2-08: AI AUTO-TRIGGER STATE MACHINE TEST
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void Test_P2_08_AiAutoTriggerInPvAiMode()
        {
            EnsureStaThread();

            _dispatcher!.Invoke(() =>
            {
                var tool = new ChessTool();
                var modeField = typeof(ChessTool).GetField("_currentMode", BindingFlags.NonPublic | BindingFlags.Instance);
                var checkTriggerMethod = typeof(ChessTool).GetMethod("CheckAndTriggerAiMove", BindingFlags.NonPublic | BindingFlags.Instance);
                var isThinkingField = typeof(ChessTool).GetField("_isAiThinking", BindingFlags.NonPublic | BindingFlags.Instance);

                Assert.NotNull(modeField);
                Assert.NotNull(checkTriggerMethod);
                Assert.NotNull(isThinkingField);

                // Set mode to PvAI (index 1)
                modeField!.SetValue(tool, 1);

                // Execute trigger check — since active turn is White initially, AI should not lock thinking
                checkTriggerMethod!.Invoke(tool, null);
                bool isThinkingWhite = (bool)isThinkingField!.GetValue(tool)!;
                Assert.False(isThinkingWhite, "AI should not trigger when it is White's turn");
            });
        }
    }
}

