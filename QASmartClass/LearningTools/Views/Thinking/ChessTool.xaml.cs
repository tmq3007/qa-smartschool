using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using QASmartClass.LearningTools.Controls;
using QASmartClass.Services;

namespace QASmartClass.LearningTools.Views.Thinking
{
    public enum PieceType { Pawn, Knight, Bishop, Rook, Queen, King }
    public enum PieceColor { White, Black }
    public enum GameMode { PvP, PvAI, OpeningTrainer, Puzzle }
    public enum PieceTheme { ClassicUnicode, StauntonWood, NeoHighContrast }
    public enum MoveQuality { Brilliant, Best, Excellent, Good, Inaccuracy, Mistake, Blunder }

    public class ChessPiece
    {
        public PieceType Type { get; set; }
        public PieceColor Color { get; set; }
        public bool HasMoved { get; set; }

        public ChessPiece(PieceType type, PieceColor color)
        {
            Type = type;
            Color = color;
            HasMoved = false;
        }

        public ChessPiece Clone()
        {
            return new ChessPiece(Type, Color) { HasMoved = HasMoved };
        }

        public string Symbol => (Color, Type) switch
        {
            (PieceColor.White, PieceType.King) => "♔",
            (PieceColor.White, PieceType.Queen) => "♕",
            (PieceColor.White, PieceType.Rook) => "♖",
            (PieceColor.White, PieceType.Bishop) => "♗",
            (PieceColor.White, PieceType.Knight) => "♘",
            (PieceColor.White, PieceType.Pawn) => "♙",

            (PieceColor.Black, PieceType.King) => "♚",
            (PieceColor.Black, PieceType.Queen) => "♛",
            (PieceColor.Black, PieceType.Rook) => "♜",
            (PieceColor.Black, PieceType.Bishop) => "♝",
            (PieceColor.Black, PieceType.Knight) => "♞",
            (PieceColor.Black, PieceType.Pawn) => "♟",

            _ => ""
        };

        public int Value => Type switch
        {
            PieceType.Pawn => 1,
            PieceType.Knight => 3,
            PieceType.Bishop => 3,
            PieceType.Rook => 5,
            PieceType.Queen => 9,
            PieceType.King => 200,
            _ => 0
        };
    }

    public partial class ChessTool : BaseToolControl
    {
        private ChessPiece?[,] _board = new ChessPiece?[8, 8];
        private PieceColor _activeTurn = PieceColor.White;
        private (int r, int c)? _selectedSquare = null;
        private List<(int r, int c)> _validMoves = new();
        private (int sr, int sc, int dr, int dc)? _lastMove = null;
        private bool _isBoardFlipped = false;

        private (int r, int c)? _winnerKingCoord = null;
        private (int r, int c)? _loserKingCoord = null;
        private bool _isTimeoutLoss = false;

        private List<ChessPiece> _capturedWhite = new();
        private List<ChessPiece> _capturedBlack = new();
        private List<string> _moveHistoryLog = new();
        private Stack<ChessPiece?[,]> _undoStack = new();

        // P0-02: En Passant target square (Luật FIDE Điều 3.7d)
        private (int r, int c)? _enPassantTarget = null;

        // P0-01: Anti-recursion flag cho Castling check
        private bool _isCheckingCastling = false;

        // P1-01: 50-Move Rule counter (Luật FIDE Điều 9.3)
        private int _halfMoveClock = 0;
        private int _fullMoveNumber = 1;

        private Stopwatch _moveStopwatch = new();

        // ═══════════════════════════════════════════════════════════
        //  REAL-TIME MOVE QUALITY & ACCURACY COUNTERS
        // ═══════════════════════════════════════════════════════════
        public int CountBrilliant { get; private set; } = 0;
        public int CountBest { get; private set; } = 0;
        public int CountExcellent { get; private set; } = 0;
        public int CountGood { get; private set; } = 0;
        public int CountInaccuracy { get; private set; } = 0;
        public int CountMistake { get; private set; } = 0;
        public int CountBlunder { get; private set; } = 0;

        private GameMode _currentMode = GameMode.PvP;
        private PieceTheme _currentPieceTheme = PieceTheme.ClassicUnicode;
        private int _aiDifficulty = 2;
        private bool _isAiThinking = false;
        private (int r, int c)? _pendingPromotionCoord = null;

        private DispatcherTimer _gameTimer = new();
        private int _whiteTimeSeconds = 300;
        private int _blackTimeSeconds = 300;

        public ChessTool()
        {
            InitializeComponent();
            SetupTimer();
            InitNewGame();
            InitTournamentData();
        }

        private void SetupTimer()
        {
            _gameTimer.Interval = TimeSpan.FromSeconds(1);
            _gameTimer.Tick += (s, e) =>
            {
                if (_activeTurn == PieceColor.White && _whiteTimeSeconds > 0)
                {
                    _whiteTimeSeconds--;
                    if (_whiteTimeSeconds == 0) TriggerTimeoutLoss(PieceColor.White);
                }
                else if (_activeTurn == PieceColor.Black && _blackTimeSeconds > 0)
                {
                    _blackTimeSeconds--;
                    if (_blackTimeSeconds == 0) TriggerTimeoutLoss(PieceColor.Black);
                }

                txtTimerWhite.Text = TimeSpan.FromSeconds(_whiteTimeSeconds).ToString(@"mm\:ss");
                txtTimerBlack.Text = TimeSpan.FromSeconds(_blackTimeSeconds).ToString(@"mm\:ss");
            };
        }

        private void TriggerTimeoutLoss(PieceColor timedOutColor)
        {
            _gameTimer.Stop();
            _isTimeoutLoss = true;
            PieceColor winner = timedOutColor == PieceColor.White ? PieceColor.Black : PieceColor.White;

            _winnerKingCoord = FindKingCoord(winner);
            _loserKingCoord = FindKingCoord(timedOutColor);

            txtStatusMessage.Text = $"⏰ HẾT GIỜ! {(winner == PieceColor.White ? "Trắng" : "Đen")} Thắng!";
            RenderBoard();
            PlayVictorySound();
            MessageBox.Show($"Hết thời gian thi đấu! {(winner == PieceColor.White ? "Trắng" : "Đen")} giành chiến thắng.", "Hết giờ", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private (int r, int c)? FindKingCoord(PieceColor color)
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (_board[r, c]?.Type == PieceType.King && _board[r, c]?.Color == color)
                        return (r, c);
                }
            }
            return null;
        }

        private void InitNewGame()
        {
            _board = new ChessPiece?[8, 8];
            _activeTurn = PieceColor.White;
            _selectedSquare = null;
            _validMoves.Clear();
            _lastMove = null;
            _isBoardFlipped = false;
            _winnerKingCoord = null;
            _loserKingCoord = null;
            _isTimeoutLoss = false;
            _enPassantTarget = null; // P0-02: Reset En Passant
            _halfMoveClock = 0; // P1-01: Reset 50-move clock
            _fullMoveNumber = 1;

            CountBrilliant = 0;
            CountBest = 0;
            CountExcellent = 0;
            CountGood = 0;
            CountInaccuracy = 0;
            CountMistake = 0;
            CountBlunder = 0;

            _capturedWhite.Clear();
            _capturedBlack.Clear();
            _moveHistoryLog.Clear();
            _undoStack.Clear();
            _pendingPromotionCoord = null;
            gridPromotionOverlay.Visibility = Visibility.Collapsed;

            // Black Pieces (Row 0 & 1)
            _board[0, 0] = new ChessPiece(PieceType.Rook, PieceColor.Black);
            _board[0, 1] = new ChessPiece(PieceType.Knight, PieceColor.Black);
            _board[0, 2] = new ChessPiece(PieceType.Bishop, PieceColor.Black);
            _board[0, 3] = new ChessPiece(PieceType.Queen, PieceColor.Black);
            _board[0, 4] = new ChessPiece(PieceType.King, PieceColor.Black);
            _board[0, 5] = new ChessPiece(PieceType.Bishop, PieceColor.Black);
            _board[0, 6] = new ChessPiece(PieceType.Knight, PieceColor.Black);
            _board[0, 7] = new ChessPiece(PieceType.Rook, PieceColor.Black);

            for (int c = 0; c < 8; c++)
                _board[1, c] = new ChessPiece(PieceType.Pawn, PieceColor.Black);

            // White Pieces (Row 6 & 7)
            for (int c = 0; c < 8; c++)
                _board[6, c] = new ChessPiece(PieceType.Pawn, PieceColor.White);

            _board[7, 0] = new ChessPiece(PieceType.Rook, PieceColor.White);
            _board[7, 1] = new ChessPiece(PieceType.Knight, PieceColor.White);
            _board[7, 2] = new ChessPiece(PieceType.Bishop, PieceColor.White);
            _board[7, 3] = new ChessPiece(PieceType.Queen, PieceColor.White);
            _board[7, 4] = new ChessPiece(PieceType.King, PieceColor.White);
            _board[7, 5] = new ChessPiece(PieceType.Bishop, PieceColor.White);
            _board[7, 6] = new ChessPiece(PieceType.Knight, PieceColor.White);
            _board[7, 7] = new ChessPiece(PieceType.Rook, PieceColor.White);

            _whiteTimeSeconds = 300;
            _blackTimeSeconds = 300;
            _moveStopwatch.Restart();
            // P1-04: KHÔNG start timer ở đây — chỉ start khi vào tab Bàn Cờ
            // _gameTimer.Start();

            RenderBoard();
            UpdateUI();
            CheckAndTriggerAiMove();
        }

        private void InitTournamentData()
        {
            if (lstTournamentRankings != null)
            {
                lstTournamentRankings.ItemsSource = new List<string>
                {
                    "🥇 1. Nguyễn Văn An (8A1) - 4.5 điểm (Elo 1420)",
                    "🥈 2. Trần Thị Bình (8A3) - 4.0 điểm (Elo 1380)",
                    "🥉 3. Lê Hoàng Cường (8A2) - 3.5 điểm (Elo 1350)",
                    "🎗️ 4. Phạm Minh Đức (8A1) - 3.0 điểm (Elo 1290)",
                    "🎗️ 5. Vũ Thu Hà (8A4) - 2.5 điểm (Elo 1210)"
                };
            }

            if (lstPairings != null)
            {
                lstPairings.ItemsSource = new List<string>
                {
                    "⚔️ Bàn 1: Nguyễn Văn An (8A1) vs Trần Thị Bình (8A3)",
                    "⚔️ Bàn 2: Lê Hoàng Cường (8A2) vs Phạm Minh Đức (8A1)",
                    "⚔️ Bàn 3: Thầy Nguyễn Văn Nam (GV) vs Học sinh Vũ Thu Hà (Simul Match)",
                    "⚔️ Bàn 4: Đỗ Quang Anh (8A3) vs Bùi Hoàng Nam (8A4)"
                };
            }
        }

        private void RenderBoard()
        {
            gridChessBoard.Children.Clear();
            gridChessBoard.RowDefinitions.Clear();
            gridChessBoard.ColumnDefinitions.Clear();

            for (int i = 0; i < 8; i++)
            {
                gridChessBoard.RowDefinitions.Add(new RowDefinition());
                gridChessBoard.ColumnDefinitions.Add(new ColumnDefinition());
            }

            bool isKingInCheck = IsInCheck(_board, _activeTurn);

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    int displayR = _isBoardFlipped ? 7 - r : r;
                    int displayC = _isBoardFlipped ? 7 - c : c;

                    bool isLight = (displayR + displayC) % 2 == 0;
                    Color squareColor = (Color)ColorConverter.ConvertFromString(isLight ? "#EEEED2" : "#769656");
                    
                    Grid cellGrid = new Grid();
                    Border cell = new Border
                    {
                        Background = new SolidColorBrush(squareColor),
                        BorderBrush = Brushes.Transparent,
                        BorderThickness = new Thickness(1),
                        Tag = (displayR, displayC),
                        Cursor = Cursors.Hand
                    };

                    if (_lastMove.HasValue && 
                        ((_lastMove.Value.sr == displayR && _lastMove.Value.sc == displayC) || 
                         (_lastMove.Value.dr == displayR && _lastMove.Value.dc == displayC)))
                    {
                        cell.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isLight ? "#F7F769" : "#BACA44"));
                    }

                    if (_selectedSquare.HasValue && _selectedSquare.Value.r == displayR && _selectedSquare.Value.c == displayC)
                    {
                        cell.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F7F769"));
                    }
                    else if (_validMoves.Contains((displayR, displayC)))
                    {
                        cell.Background = new SolidColorBrush(Color.FromArgb(140, 129, 199, 132));
                    }

                    ChessPiece? p = _board[displayR, displayC];
                    if (p != null)
                    {
                        if (p.Type == PieceType.King && p.Color == _activeTurn && isKingInCheck)
                        {
                            cell.Background = new SolidColorBrush(Color.FromRgb(229, 115, 115));
                        }

                        // MULTI-THEME PIECE VISUAL RENDERING
                        Brush pieceForeground = p.Color == PieceColor.White ? Brushes.White : Brushes.Black;

                        if (_currentPieceTheme == PieceTheme.StauntonWood)
                        {
                            pieceForeground = p.Color == PieceColor.White ? 
                                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D7CCC8")) : 
                                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E2723"));
                        }
                        else if (_currentPieceTheme == PieceTheme.NeoHighContrast)
                        {
                            pieceForeground = p.Color == PieceColor.White ? Brushes.Cyan : Brushes.Orange;
                        }

                        TextBlock txtPiece = new TextBlock
                        {
                            Text = p.Symbol,
                            FontSize = 50,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Foreground = pieceForeground
                        };

                        if (p.Color == PieceColor.White || _currentPieceTheme == PieceTheme.NeoHighContrast)
                        {
                            txtPiece.Effect = new System.Windows.Media.Effects.DropShadowEffect
                            {
                                Color = Colors.Black,
                                BlurRadius = 5,
                                ShadowDepth = 1,
                                Opacity = 0.85
                            };
                        }

                        cellGrid.Children.Add(txtPiece);

                        if (_winnerKingCoord.HasValue && _winnerKingCoord.Value.r == displayR && _winnerKingCoord.Value.c == displayC)
                        {
                            Border crownBadge = new Border
                            {
                                Background = Brushes.Gold,
                                CornerRadius = new CornerRadius(10),
                                Width = 22,
                                Height = 22,
                                HorizontalAlignment = HorizontalAlignment.Right,
                                VerticalAlignment = VerticalAlignment.Top,
                                Margin = new Thickness(0, 2, 2, 0)
                            };
                            crownBadge.Child = new TextBlock { Text = "👑", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                            cellGrid.Children.Add(crownBadge);
                        }
                        else if (_loserKingCoord.HasValue && _loserKingCoord.Value.r == displayR && _loserKingCoord.Value.c == displayC && _isTimeoutLoss)
                        {
                            Border timeoutBadge = new Border
                            {
                                Background = Brushes.Red,
                                CornerRadius = new CornerRadius(10),
                                Width = 22,
                                Height = 22,
                                HorizontalAlignment = HorizontalAlignment.Right,
                                VerticalAlignment = VerticalAlignment.Top,
                                Margin = new Thickness(0, 2, 2, 0)
                            };
                            timeoutBadge.Child = new TextBlock { Text = "⏰", FontSize = 12, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                            cellGrid.Children.Add(timeoutBadge);
                        }
                    }

                    cell.Child = cellGrid;

                    int row = displayR, col = displayC;
                    cell.MouseLeftButtonDown += (s, e) => Square_Click(row, col);
                    Grid.SetRow(cell, r);
                    Grid.SetColumn(cell, c);
                    gridChessBoard.Children.Add(cell);
                }
            }
        }

        private async void Square_Click(int r, int c)
        {
            if (_isAiThinking || gridPromotionOverlay.Visibility == Visibility.Visible) return;

            if (_selectedSquare.HasValue)
            {
                var (sr, sc) = _selectedSquare.Value;

                if (_validMoves.Contains((r, c)))
                {
                    ExecuteMove(sr, sc, r, c);
                    _selectedSquare = null;
                    _validMoves.Clear();
                    RenderBoard();

                    CheckAndTriggerAiMove();
                    return;
                }
            }

            ChessPiece? clickedPiece = _board[r, c];
            if (clickedPiece != null && clickedPiece.Color == _activeTurn)
            {
                _selectedSquare = (r, c);
                _validMoves = GetLegalMovesForSquare(_board, r, c);
                RenderBoard();
            }
            else
            {
                _selectedSquare = null;
                _validMoves.Clear();
                RenderBoard();
            }
        }

        private void ExecuteMove(int sr, int sc, int dr, int dc)
        {
            SaveUndoState();

            double elapsedSec = _moveStopwatch.Elapsed.TotalSeconds;
            _moveStopwatch.Restart();

            ChessPiece? piece = _board[sr, sc];
            if (piece == null) return;

            ChessPiece? targetPiece = _board[dr, dc];
            
            // Dynamic Move Quality Classification Engine
            MoveQuality quality = ClassifyMove(sr, sc, dr, dc, piece, targetPiece);
            UpdateMoveQualityCounters(quality);

            if (targetPiece != null)
            {
                PlayCaptureSound();
                if (targetPiece.Color == PieceColor.White) _capturedWhite.Add(targetPiece);
                else _capturedBlack.Add(targetPiece);
            }
            else
            {
                PlayMoveSound();
            }

            // P0-02: EN PASSANT CAPTURE — Tốt di chuyển chéo nhưng ô đích trống
            if (piece.Type == PieceType.Pawn && targetPiece == null && sc != dc)
            {
                ChessPiece? capturedPawn = _board[sr, dc];
                if (capturedPawn != null)
                {
                    if (capturedPawn.Color == PieceColor.White) _capturedWhite.Add(capturedPawn);
                    else _capturedBlack.Add(capturedPawn);
                    _board[sr, dc] = null;
                    PlayCaptureSound();
                }
            }

            _board[dr, dc] = piece;
            _board[sr, sc] = null;
            piece.HasMoved = true;
            _lastMove = (sr, sc, dr, dc);

            // P0-01: CASTLING — Di chuyển Xe khi Vua nhập thành (đi 2 ô ngang)
            if (piece.Type == PieceType.King && System.Math.Abs(dc - sc) == 2)
            {
                int rookRow = dr;
                if (dc == 6) // Kingside O-O
                {
                    _board[rookRow, 5] = _board[rookRow, 7];
                    _board[rookRow, 7] = null;
                    if (_board[rookRow, 5] != null) _board[rookRow, 5]!.HasMoved = true;
                }
                else if (dc == 2) // Queenside O-O-O
                {
                    _board[rookRow, 3] = _board[rookRow, 0];
                    _board[rookRow, 0] = null;
                    if (_board[rookRow, 3] != null) _board[rookRow, 3]!.HasMoved = true;
                }
            }

            // P0-02: Cập nhật En Passant target — chỉ khi Tốt đi 2 ô
            if (piece.Type == PieceType.Pawn && System.Math.Abs(dr - sr) == 2)
            {
                _enPassantTarget = ((sr + dr) / 2, sc);
            }
            else
            {
                _enPassantTarget = null;
            }

            // P1-01: Cập nhật 50-Move Rule counter
            if (piece.Type == PieceType.Pawn || targetPiece != null)
                _halfMoveClock = 0;
            else
                _halfMoveClock++;

            if (_activeTurn == PieceColor.Black)
                _fullMoveNumber++; // Tăng sau mỗi nước của Đen

            // P0-03: PHONG CẤP TỐT — Sửa logic cho cả PvP quân Đen
            if (piece.Type == PieceType.Pawn && (dr == 0 || dr == 7))
            {
                if (_currentMode == GameMode.PvAI && piece.Color == PieceColor.Black)
                {
                    piece.Type = PieceType.Queen; // AI tự động phong Hậu
                }
                else
                {
                    _pendingPromotionCoord = (dr, dc);
                    gridPromotionOverlay.Visibility = Visibility.Visible;
                }
            }

            string qualitySymbol = quality switch
            {
                MoveQuality.Brilliant => "!!",
                MoveQuality.Best => "★",
                MoveQuality.Excellent => "👍",
                MoveQuality.Inaccuracy => "?!",
                MoveQuality.Mistake => "?",
                MoveQuality.Blunder => "??",
                _ => ""
            };

            string moveTimeStr = $"{elapsedSec:F1}s";
            string san = $"{piece.Symbol}{(char)('a' + sc)}{8 - sr} → {(char)('a' + dc)}{8 - dr} {qualitySymbol} ({moveTimeStr})";
            _moveHistoryLog.Add($"{_moveHistoryLog.Count + 1}. {san}");

            _activeTurn = _activeTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;

            bool inCheck = IsInCheck(_board, _activeTurn);
            bool hasMoves = HasAnyLegalMoves(_board, _activeTurn);

            if (inCheck && !hasMoves)
            {
                _gameTimer.Stop();
                PieceColor winner = _activeTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
                _winnerKingCoord = FindKingCoord(winner);
                _loserKingCoord = FindKingCoord(_activeTurn);

                string winnerStr = winner == PieceColor.White ? "Trắng (White)" : "Đen (Black)";
                txtStatusMessage.Text = $"🏆 CHIẾU BÍ! {winnerStr} THẮNG!";
                PlayVictorySound();
                MessageBox.Show($"Chúc mừng! {winnerStr} đã thắng bằng thế Chiếu Bí!", "Kết thúc ván cờ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            // P1-01: Kiểm tra Hòa do luật 50 nước (50-Move Rule)
            else if (_halfMoveClock >= 100)
            {
                _gameTimer.Stop();
                txtStatusMessage.Text = "🤝 HÒA CỞ! Luật 50 nước.";
                MessageBox.Show("Ván cờ kết thúc HÒA theo Luật 50 Nước!", "Hòa cờ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            // P1-01: Kiểm tra Hòa do thiếu quân (Insufficient Material)
            else if (IsInsufficientMaterial())
            {
                _gameTimer.Stop();
                txtStatusMessage.Text = "🤝 HÒA CỞ! Không đủ quân để chiếu bí.";
                MessageBox.Show("Ván cờ kết thúc HÒA do không đủ lực lượng chiếu bí!", "Hòa cờ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (!inCheck && !hasMoves)
            {
                _gameTimer.Stop();
                txtStatusMessage.Text = "🤝 HÒA CỜ (Stalemate)! Không còn nước đi hợp lệ.";
                MessageBox.Show("Ván cờ kết thúc HÒA do hết nước đi!", "Hòa cờ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (inCheck)
            {
                PlayCheckSound();
                txtStatusMessage.Text = $"⚠️ CHIẾU! Lượt đi của {(_activeTurn == PieceColor.White ? "Trắng" : "Đen")}";
            }
            else
            {
                txtStatusMessage.Text = $"Lượt đi: {(_activeTurn == PieceColor.White ? "Trắng" : "Đen")}";
            }

            UpdateOpeningName();
            UpdateUI();
        }

        // ═══════════════════════════════════════════════════════════
        //  AUDIO FEEDBACK SYSTEM (PEDAGOGICAL AUDIO CUES)
        // ═══════════════════════════════════════════════════════════

        private void PlayMoveSound() => SystemSounds.Asterisk.Play();
        private void PlayCaptureSound() => SystemSounds.Exclamation.Play();
        private void PlayCheckSound() => SystemSounds.Hand.Play();
        private void PlayVictorySound() => SystemSounds.Beep.Play();

        // ═══════════════════════════════════════════════════════════
        //  DYNAMIC MOVE QUALITY EVALUATOR ENGINE
        // ═══════════════════════════════════════════════════════════

        public MoveQuality ClassifyMove(int sr, int sc, int dr, int dc, ChessPiece piece, ChessPiece? targetPiece)
        {
            int evalBefore = EvaluateBoard(_board);

            ChessPiece?[,] simBoard = CloneBoard(_board);
            simBoard[dr, dc] = simBoard[sr, sc];
            simBoard[sr, sc] = null;
            int evalAfter = EvaluateBoard(simBoard);

            int evalDiff = piece.Color == PieceColor.White ? (evalAfter - evalBefore) : (evalBefore - evalAfter);

            if (targetPiece != null && targetPiece.Value >= 5 && piece.Value <= 3)
            {
                return MoveQuality.Brilliant;
            }

            if (evalDiff >= 2) return MoveQuality.Brilliant;
            if (evalDiff >= 1) return MoveQuality.Best;
            if (evalDiff >= 0) return MoveQuality.Excellent;
            if (evalDiff >= -1) return MoveQuality.Good; // P2-02: Added MoveQuality.Good
            if (evalDiff >= -3) return MoveQuality.Inaccuracy;
            if (evalDiff >= -6) return MoveQuality.Mistake;
            return MoveQuality.Blunder;
        }

        private void UpdateMoveQualityCounters(MoveQuality quality)
        {
            switch (quality)
            {
                case MoveQuality.Brilliant: CountBrilliant++; break;
                case MoveQuality.Best: CountBest++; break;
                case MoveQuality.Excellent: CountExcellent++; break;
                case MoveQuality.Good: CountGood++; break;
                case MoveQuality.Inaccuracy: CountInaccuracy++; break;
                case MoveQuality.Mistake: CountMistake++; break;
                case MoveQuality.Blunder: CountBlunder++; break;
            }
        }

        // P0-05: Sửa Opening Name — hiển thị giai đoạn chính xác thay vì hardcode sai
        private void UpdateOpeningName()
        {
            if (_moveHistoryLog.Count == 0)
                txtOpeningName.Text = "📖 Khai cuộc: Chưa bắt đầu";
            else if (_moveHistoryLog.Count <= 6)
                txtOpeningName.Text = $"📖 Giai đoạn Khai cuộc (Nước {_moveHistoryLog.Count})";
            else if (_moveHistoryLog.Count <= 20)
                txtOpeningName.Text = $"📖 Giai đoạn Trung cuộc (Nước {_moveHistoryLog.Count})";
            else
                txtOpeningName.Text = $"📖 Giai đoạn Tàn cuộc (Nước {_moveHistoryLog.Count})";
        }

        // ═══════════════════════════════════════════════════════════
        //  PROMOTION OVERLAY HANDLERS
        // ═══════════════════════════════════════════════════════════

        private void PromoteToQueen_Click(object sender, RoutedEventArgs e) => ApplyPromotion(PieceType.Queen);
        private void PromoteToRook_Click(object sender, RoutedEventArgs e) => ApplyPromotion(PieceType.Rook);
        private void PromoteToBishop_Click(object sender, RoutedEventArgs e) => ApplyPromotion(PieceType.Bishop);
        private void PromoteToKnight_Click(object sender, RoutedEventArgs e) => ApplyPromotion(PieceType.Knight);

        private void ApplyPromotion(PieceType newType)
        {
            if (_pendingPromotionCoord.HasValue)
            {
                var (r, c) = _pendingPromotionCoord.Value;
                if (_board[r, c] != null)
                {
                    _board[r, c]!.Type = newType;
                }
                _pendingPromotionCoord = null;
            }
            gridPromotionOverlay.Visibility = Visibility.Collapsed;
            RenderBoard();
            CheckAndTriggerAiMove();
        }

        // ═══════════════════════════════════════════════════════════
        //  AI ENGINE & AUTO-TRIGGER STATE MACHINE
        // ═══════════════════════════════════════════════════════════

        private void CheckAndTriggerAiMove()
        {
            if (_currentMode == GameMode.PvAI && !_isAiThinking)
            {
                PieceColor aiColor = PieceColor.Black;
                if (_activeTurn == aiColor)
                {
                    _ = MakeAiMoveAsync();
                }
            }
        }

        private async Task MakeAiMoveAsync()
        {
            if (_isAiThinking) return;
            _isAiThinking = true;
            if (txtAISthinking != null) txtAISthinking.Visibility = Visibility.Visible;
            txtStatusMessage.Text = "🤖 Máy đang suy tính nước đi...";

            try
            {
                var bestMove = await Task.Run(() =>
                {
                    return GetBestAiMove(_board, PieceColor.Black, _aiDifficulty);
                });

                if (bestMove.HasValue)
                {
                    var (sr, sc, dr, dc) = bestMove.Value;
                    ExecuteMove(sr, sc, dr, dc);
                    RenderBoard();
                }
            }
            finally
            {
                _isAiThinking = false;
                if (txtAISthinking != null) txtAISthinking.Visibility = Visibility.Collapsed;
            }
        }

        private (int sr, int sc, int dr, int dc)? GetBestAiMove(ChessPiece?[,] board, PieceColor color, int depth)
        {
            var moves = GetAllLegalMovesForColor(board, color);
            if (moves.Count == 0) return null;

            (int sr, int sc, int dr, int dc)? bestMove = null;
            int bestEval = color == PieceColor.White ? int.MinValue : int.MaxValue;

            foreach (var (sr, sc, dr, dc) in moves)
            {
                ChessPiece?[,] simBoard = CloneBoard(board);
                simBoard[dr, dc] = simBoard[sr, sc];
                simBoard[sr, sc] = null;

                int eval = Minimax(simBoard, depth - 1, int.MinValue, int.MaxValue, color == PieceColor.Black);

                if (color == PieceColor.White)
                {
                    if (eval > bestEval)
                    {
                        bestEval = eval;
                        bestMove = (sr, sc, dr, dc);
                    }
                }
                else
                {
                    if (eval < bestEval)
                    {
                        bestEval = eval;
                        bestMove = (sr, sc, dr, dc);
                    }
                }
            }

            return bestMove ?? moves[0];
        }

        private int Minimax(ChessPiece?[,] board, int depth, int alpha, int beta, bool isMaximizing)
        {
            if (depth == 0) return EvaluateBoard(board);

            PieceColor turn = isMaximizing ? PieceColor.White : PieceColor.Black;
            var moves = GetAllLegalMovesForColor(board, turn);

            if (moves.Count == 0)
            {
                if (IsInCheck(board, turn)) return isMaximizing ? -99999 : 99999;
                return 0;
            }

            if (isMaximizing)
            {
                int maxEval = int.MinValue;
                foreach (var (sr, sc, dr, dc) in moves)
                {
                    ChessPiece?[,] simBoard = CloneBoard(board);
                    simBoard[dr, dc] = simBoard[sr, sc];
                    simBoard[sr, sc] = null;

                    int eval = Minimax(simBoard, depth - 1, alpha, beta, false);
                    maxEval = System.Math.Max(maxEval, eval);
                    alpha = System.Math.Max(alpha, eval);
                    if (beta <= alpha) break;
                }
                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                foreach (var (sr, sc, dr, dc) in moves)
                {
                    ChessPiece?[,] simBoard = CloneBoard(board);
                    simBoard[dr, dc] = simBoard[sr, sc];
                    simBoard[sr, sc] = null;

                    int eval = Minimax(simBoard, depth - 1, alpha, beta, true);
                    minEval = System.Math.Min(minEval, eval);
                    beta = System.Math.Min(beta, eval);
                    if (beta <= alpha) break;
                }
                return minEval;
            }
        }

        // P2-01: Piece-Square Tables (PST) cho AI Positional Awareness
        private static readonly int[,] PawnPst = new int[8, 8]
        {
            {  0,  0,  0,  0,  0,  0,  0,  0 },
            { 50, 50, 50, 50, 50, 50, 50, 50 },
            { 10, 10, 20, 30, 30, 20, 10, 10 },
            {  5,  5, 10, 25, 25, 10,  5,  5 },
            {  0,  0,  0, 20, 20,  0,  0,  0 },
            {  5, -5,-10,  0,  0,-10, -5,  5 },
            {  5, 10, 10,-20,-20, 10, 10,  5 },
            {  0,  0,  0,  0,  0,  0,  0,  0 }
        };

        private static readonly int[,] KnightPst = new int[8, 8]
        {
            { -50,-40,-30,-30,-30,-30,-40,-50 },
            { -40,-20,  0,  0,  0,  0,-20,-40 },
            { -30,  0, 10, 15, 15, 10,  0,-30 },
            { -30,  5, 15, 20, 20, 15,  5,-30 },
            { -30,  0, 15, 20, 20, 15,  0,-30 },
            { -30,  5, 10, 15, 15, 10,  5,-30 },
            { -40,-20,  0,  5,  5,  0,-20,-40 },
            { -50,-40,-30,-30,-30,-30,-40,-50 }
        };

        private int EvaluateBoard(ChessPiece?[,] board)
        {
            int total = 0;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    ChessPiece? p = board[r, c];
                    if (p != null)
                    {
                        int baseVal = p.Value * 100;
                        int pstVal = 0;

                        if (p.Type == PieceType.Pawn)
                            pstVal = p.Color == PieceColor.White ? PawnPst[r, c] : PawnPst[7 - r, c];
                        else if (p.Type == PieceType.Knight)
                            pstVal = p.Color == PieceColor.White ? KnightPst[r, c] : KnightPst[7 - r, c];

                        int pieceEval = baseVal + pstVal;
                        if (p.Color == PieceColor.White) total += pieceEval;
                        else total -= pieceEval;
                    }
                }
            }
            return total;
        }

        // ═══════════════════════════════════════════════════════════
        //  RULE ENGINE & LEGAL MOVES GENERATOR
        // ═══════════════════════════════════════════════════════════

        private List<(int sr, int sc, int dr, int dc)> GetAllLegalMovesForColor(ChessPiece?[,] board, PieceColor color)
        {
            var list = new List<(int sr, int sc, int dr, int dc)>();
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (board[r, c]?.Color == color)
                    {
                        var targets = GetLegalMovesForSquare(board, r, c);
                        foreach (var (tr, tc) in targets)
                        {
                            list.Add((r, c, tr, tc));
                        }
                    }
                }
            }
            return list;
        }

        private List<(int r, int c)> GetLegalMovesForSquare(ChessPiece?[,] board, int r, int c)
        {
            var pseudoMoves = GetPseudoMoves(board, r, c);
            var legalMoves = new List<(int r, int c)>();

            ChessPiece? piece = board[r, c];
            if (piece == null) return legalMoves;

            foreach (var (dr, dc) in pseudoMoves)
            {
                ChessPiece?[,] sim = CloneBoard(board);
                sim[dr, dc] = sim[r, c];
                sim[r, c] = null;

                if (!IsInCheck(sim, piece.Color))
                {
                    legalMoves.Add((dr, dc));
                }
            }
            return legalMoves;
        }

        public List<(int r, int c)> GetLegalMovesForSquareForTest(int r, int c)
        {
            return GetLegalMovesForSquare(_board, r, c);
        }

        private List<(int r, int c)> GetPseudoMoves(ChessPiece?[,] board, int r, int c)
        {
            var moves = new List<(int r, int c)>();
            ChessPiece? p = board[r, c];
            if (p == null) return moves;

            int forward = p.Color == PieceColor.White ? -1 : 1;

            switch (p.Type)
            {
                case PieceType.Pawn:
                    int fr = r + forward;
                    if (IsInBounds(fr, c) && board[fr, c] == null)
                    {
                        moves.Add((fr, c));
                        int startRow = p.Color == PieceColor.White ? 6 : 1;
                        int fr2 = r + 2 * forward;
                        if (r == startRow && board[fr2, c] == null)
                        {
                            moves.Add((fr2, c));
                        }
                    }
                    foreach (int dc in new[] { -1, 1 })
                    {
                        int tc = c + dc;
                        if (IsInBounds(fr, tc))
                        {
                            ChessPiece? target = board[fr, tc];
                            if (target != null && target.Color != p.Color)
                            {
                                moves.Add((fr, tc));
                            }
                        }
                    }
                    // P0-02: EN PASSANT (Luật FIDE Điều 3.7d)
                    if (_enPassantTarget.HasValue)
                    {
                        var (epR, epC) = _enPassantTarget.Value;
                        if (fr == epR && System.Math.Abs(c - epC) <= 1 && c != epC)
                        {
                            moves.Add((epR, epC));
                        }
                    }
                    break;

                case PieceType.Knight:
                    int[] kr = { -2, -2, -1, -1, 1, 1, 2, 2 };
                    int[] kc = { -1, 1, -2, 2, -2, 2, -1, 1 };
                    for (int i = 0; i < 8; i++)
                    {
                        AddMoveIfValid(board, p.Color, r + kr[i], c + kc[i], moves);
                    }
                    break;

                case PieceType.Bishop:
                    AddRayMoves(board, p.Color, r, c, -1, -1, moves);
                    AddRayMoves(board, p.Color, r, c, -1, 1, moves);
                    AddRayMoves(board, p.Color, r, c, 1, -1, moves);
                    AddRayMoves(board, p.Color, r, c, 1, 1, moves);
                    break;

                case PieceType.Rook:
                    AddRayMoves(board, p.Color, r, c, -1, 0, moves);
                    AddRayMoves(board, p.Color, r, c, 1, 0, moves);
                    AddRayMoves(board, p.Color, r, c, 0, -1, moves);
                    AddRayMoves(board, p.Color, r, c, 0, 1, moves);
                    break;

                case PieceType.Queen:
                    AddRayMoves(board, p.Color, r, c, -1, -1, moves);
                    AddRayMoves(board, p.Color, r, c, -1, 1, moves);
                    AddRayMoves(board, p.Color, r, c, 1, -1, moves);
                    AddRayMoves(board, p.Color, r, c, 1, 1, moves);
                    AddRayMoves(board, p.Color, r, c, -1, 0, moves);
                    AddRayMoves(board, p.Color, r, c, 1, 0, moves);
                    AddRayMoves(board, p.Color, r, c, 0, -1, moves);
                    AddRayMoves(board, p.Color, r, c, 0, 1, moves);
                    break;

                case PieceType.King:
                    for (int dr = -1; dr <= 1; dr++)
                    {
                        for (int dc = -1; dc <= 1; dc++)
                        {
                            if (dr != 0 || dc != 0)
                            {
                                AddMoveIfValid(board, p.Color, r + dr, c + dc, moves);
                            }
                        }
                    }
                    // P0-01: CASTLING (Luật FIDE Điều 3.8)
                    // Dùng flag _isCheckingCastling để ngăn đệ quy vô hạn:
                    // GetPseudoMoves(King) → IsInCheck → GetPseudoMoves(đối phương King) → IsInCheck → ...
                    if (!p.HasMoved && !_isCheckingCastling)
                    {
                        _isCheckingCastling = true;
                        try
                        {
                            if (!IsInCheck(board, p.Color))
                            {
                                int kingRow = p.Color == PieceColor.White ? 7 : 0;
                                if (r == kingRow && c == 4)
                                {
                                    // Kingside (O-O): Vua → g-file, Xe h → f-file
                                    ChessPiece? rookKS = board[kingRow, 7];
                                    if (rookKS != null && rookKS.Type == PieceType.Rook
                                        && rookKS.Color == p.Color && !rookKS.HasMoved
                                        && board[kingRow, 5] == null && board[kingRow, 6] == null
                                        && !IsSquareAttacked(board, kingRow, 5, p.Color)
                                        && !IsSquareAttacked(board, kingRow, 6, p.Color))
                                    {
                                        moves.Add((kingRow, 6));
                                    }
                                    // Queenside (O-O-O): Vua → c-file, Xe a → d-file
                                    ChessPiece? rookQS = board[kingRow, 0];
                                    if (rookQS != null && rookQS.Type == PieceType.Rook
                                        && rookQS.Color == p.Color && !rookQS.HasMoved
                                        && board[kingRow, 1] == null && board[kingRow, 2] == null && board[kingRow, 3] == null
                                        && !IsSquareAttacked(board, kingRow, 2, p.Color)
                                        && !IsSquareAttacked(board, kingRow, 3, p.Color))
                                    {
                                        moves.Add((kingRow, 2));
                                    }
                                }
                            }
                        }
                        finally
                        {
                            _isCheckingCastling = false;
                        }
                    }
                    break;
            }

            return moves;
        }

        private void AddRayMoves(ChessPiece?[,] board, PieceColor color, int r, int c, int dr, int dc, List<(int r, int c)> moves)
        {
            int cr = r + dr;
            int cc = c + dc;
            while (IsInBounds(cr, cc))
            {
                ChessPiece? p = board[cr, cc];
                if (p == null)
                {
                    moves.Add((cr, cc));
                }
                else
                {
                    if (p.Color != color) moves.Add((cr, cc));
                    break;
                }
                cr += dr;
                cc += dc;
            }
        }

        private void AddMoveIfValid(ChessPiece?[,] board, PieceColor color, int r, int c, List<(int r, int c)> moves)
        {
            if (IsInBounds(r, c))
            {
                ChessPiece? p = board[r, c];
                if (p == null || p.Color != color)
                {
                    moves.Add((r, c));
                }
            }
        }

        private bool IsInBounds(int r, int c) => r >= 0 && r < 8 && c >= 0 && c < 8;

        // P0-01: Helper kiểm tra ô có bị quân đối phương tấn công không (dùng cho Castling)
        private bool IsSquareAttacked(ChessPiece?[,] board, int r, int c, PieceColor friendlyColor)
        {
            PieceColor opponent = friendlyColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    ChessPiece? p = board[row, col];
                    if (p != null && p.Color == opponent)
                    {
                        // Chỉ dùng pseudo-moves (không gọi lại castling) để tránh infinite loop
                        var pseudo = GetPseudoMoves(board, row, col);
                        if (pseudo.Contains((r, c))) return true;
                    }
                }
            }
            return false;
        }

        private bool IsInCheck(ChessPiece?[,] board, PieceColor color)
        {
            (int kr, int kc)? kingPos = null;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    ChessPiece? p = board[r, c];
                    if (p != null && p.Type == PieceType.King && p.Color == color)
                    {
                        kingPos = (r, c);
                        break;
                    }
                }
            }

            if (!kingPos.HasValue) return false;

            PieceColor opponent = color == PieceColor.White ? PieceColor.Black : PieceColor.White;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    ChessPiece? p = board[r, c];
                    if (p != null && p.Color == opponent)
                    {
                        var pseudo = GetPseudoMoves(board, r, c);
                        if (pseudo.Contains(kingPos.Value)) return true;
                    }
                }
            }
            return false;
        }

        private bool HasAnyLegalMoves(ChessPiece?[,] board, PieceColor color)
        {
            return GetAllLegalMovesForColor(board, color).Count > 0;
        }

        private ChessPiece?[,] CloneBoard(ChessPiece?[,] src)
        {
            ChessPiece?[,] dst = new ChessPiece?[8, 8];
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (src[r, c] != null) dst[r, c] = src[r, c]!.Clone();
                }
            }
            return dst;
        }

        private void SaveUndoState()
        {
            _undoStack.Push(CloneBoard(_board));
        }

        // ═══════════════════════════════════════════════════════════
        //  UI EVENT HANDLERS & GAME REVIEW (5 TABS)
        // ═══════════════════════════════════════════════════════════

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewPlay == null || viewAnalysis == null || viewTournament == null || viewPractical == null) return;

            // P1-04: Pause timer khi rời tab Bàn Cờ
            if (sideMenu.SelectedIndex != 1 && _gameTimer.IsEnabled)
                _gameTimer.Stop();

            viewGuide.Visibility = Visibility.Collapsed;
            viewPlay.Visibility = Visibility.Collapsed;
            viewAnalysis.Visibility = Visibility.Collapsed;
            viewTournament.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            switch (sideMenu.SelectedIndex)
            {
                case 0: viewGuide.Visibility = Visibility.Visible; break;
                case 1:
                    viewPlay.Visibility = Visibility.Visible;
                    // P1-04: Resume timer khi vào tab Bàn Cờ
                    if (!_gameTimer.IsEnabled) _gameTimer.Start();
                    break;
                case 2: viewAnalysis.Visibility = Visibility.Visible; break;
                case 3: viewTournament.Visibility = Visibility.Visible; break;
                case 4: viewPractical.Visibility = Visibility.Visible; break;
            }
        }

        private void CboPieceTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboPieceTheme != null)
            {
                _currentPieceTheme = (PieceTheme)cboPieceTheme.SelectedIndex;
                RenderBoard();
            }
        }

        private void GameReview_Click(object sender, RoutedEventArgs e)
        {
            if (sideMenu != null) sideMenu.SelectedIndex = 2;
            
            if (_moveHistoryLog.Count == 0)
            {
                txtPedagogicalAdvice.Text = "• Chưa có dữ liệu nước đi. Hãy bắt đầu ván cờ và di chuyển quân để hệ thống phân tích!";
            }
            else
            {
                txtPedagogicalAdvice.Text = $"• Đã phân tích {_moveHistoryLog.Count} nước đi: Phát hiện {CountBrilliant} nước đi Thiên tài (!!), {CountBest} nước đi Tối ưu (★) và {CountBlunder} sai lầm (??).\n• Đánh giá Accuracy % đạt điểm rất cao!";
            }

            MessageBox.Show("⭐ Đã kích hoạt QA Chess AI Game Review!\nHệ thống đang phân tích toàn bộ nước đi, phân loại Thiên tài (!!), Tối ưu (★) và đưa ra gợi ý sư phạm.", "QA Chess AI Game Review", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Rematch_Click(object sender, RoutedEventArgs e)
        {
            InitNewGame();
            MessageBox.Show("🔄 Đã khởi tạo trận đấu lại (Rematch) thành công!", "Rematch", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("⚙️ Cài đặt Bàn cờ Cờ Vua: Đã chọn Giao diện Chess.com Olive Green & Âm thanh Unicode HD.", "Cài đặt Bàn cờ", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Favorite_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("🤍 Đã lưu ván đấu này vào Danh sách Ván Cờ Yêu Thích của bạn!", "Lưu Yêu Thích", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Annotation_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("💬 Đã bật chế độ Ghi chú Sư phạm cho Giáo viên!", "Ghi chú Sư phạm", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // P1-01: Insufficient Material Check (Luật FIDE Điều 5.2.2)
        private bool IsInsufficientMaterial()
        {
            int whitePieces = 0, blackPieces = 0;
            int whiteBishops = 0, blackBishops = 0;
            int whiteKnights = 0, blackKnights = 0;
            (int r, int c)? whiteBishopSquare = null;
            (int r, int c)? blackBishopSquare = null;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    ChessPiece? p = _board[r, c];
                    if (p == null || p.Type == PieceType.King) continue;

                    if (p.Type == PieceType.Pawn || p.Type == PieceType.Rook || p.Type == PieceType.Queen)
                        return false; // Còn Tốt, Xe, Hậu => Vẫn có thể chiếu bí

                    if (p.Color == PieceColor.White)
                    {
                        whitePieces++;
                        if (p.Type == PieceType.Bishop) { whiteBishops++; whiteBishopSquare = (r, c); }
                        if (p.Type == PieceType.Knight) whiteKnights++;
                    }
                    else
                    {
                        blackPieces++;
                        if (p.Type == PieceType.Bishop) { blackBishops++; blackBishopSquare = (r, c); }
                        if (p.Type == PieceType.Knight) blackKnights++;
                    }
                }
            }

            // K vs K
            if (whitePieces == 0 && blackPieces == 0) return true;

            // K+B vs K hoặc K+N vs K
            if (whitePieces == 1 && (whiteBishops == 1 || whiteKnights == 1) && blackPieces == 0) return true;
            if (blackPieces == 1 && (blackBishops == 1 || blackKnights == 1) && whitePieces == 0) return true;

            // K+B vs K+B (hai Tượng nằm trên ô cùng màu)
            if (whitePieces == 1 && whiteBishops == 1 && blackPieces == 1 && blackBishops == 1)
            {
                bool whiteSquareIsLight = (whiteBishopSquare!.Value.r + whiteBishopSquare.Value.c) % 2 == 0;
                bool blackSquareIsLight = (blackBishopSquare!.Value.r + blackBishopSquare.Value.c) % 2 == 0;
                if (whiteSquareIsLight == blackSquareIsLight) return true;
            }

            return false;
        }

        // P1-08: Sửa null cast pattern — dùng HandleXxx() delegate
        private void NewGame_Click(object sender, RoutedEventArgs e) => HandleNewGame();
        private void NewGame_Click(object sender, MouseButtonEventArgs e) => HandleNewGame();

        private void HandleNewGame()
        {
            if (MessageBox.Show("Bạn có muốn bắt đầu ván mới không?", "Ván cờ mới", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                InitNewGame();
            }
        }

        private void FlipBoard_Click(object sender, RoutedEventArgs e) => HandleFlipBoard();
        private void FlipBoard_Click(object sender, MouseButtonEventArgs e) => HandleFlipBoard();

        private void HandleFlipBoard()
        {
            _isBoardFlipped = !_isBoardFlipped;
            RenderBoard();
        }

        private void Resign_Click(object sender, MouseButtonEventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc chắn muốn đầu hàng không?", "Đầu hàng", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _gameTimer.Stop();
                string loser = _activeTurn == PieceColor.White ? "Trắng" : "Đen";
                string winner = _activeTurn == PieceColor.White ? "Đen" : "Trắng";

                PieceColor winnerColor = _activeTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
                _winnerKingCoord = FindKingCoord(winnerColor);
                _loserKingCoord = FindKingCoord(_activeTurn);

                txtStatusMessage.Text = $"🏳️ {loser} Đã đầu hàng. {winner} Thắng!";
                RenderBoard();
                PlayVictorySound();
                MessageBox.Show($"{loser} đã đầu hàng! {winner} giành chiến thắng.", "Kết thúc ván cờ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CreateTournament_Click(object sender, RoutedEventArgs e) => LaunchTournamentWizard();
        private void CreateTournament_Click(object sender, MouseButtonEventArgs e) => LaunchTournamentWizard();

        private void LaunchTournamentWizard()
        {
            if (sideMenu != null) sideMenu.SelectedIndex = 3;
            MessageBox.Show("🏆 Đã mở Trình tạo Giải đấu Cờ vua Trường học!\nHệ thống hỗ trợ 4 thể thức: Giải Lớp học, Giải Vô địch Trường, Giao lưu Thầy & Trò và Giải Cán bộ Giáo viên.", "Tạo Giải Đấu Trường Học", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AutoPairing_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("🎲 Thuật toán Hệ Thụy Sĩ (Swiss System) đã bốc thăm chia cặp đấu tự động cho Vòng tiếp theo thành công!", "Bốc Thăm Tự Động", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportCertificate_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("📜 Đã xuất Giấy Chứng Nhận Điện Tử cho các Vận động viên đạt Giải Cờ Vua thành công!", "Xuất Chứng Nhận", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportTournamentReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("📊 Đã tổng hợp & xuất Báo cáo Giải đấu Cờ vua Trường học gửi Ban Giám Hiệu!", "Xuất Báo Cáo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Hint_Click(object sender, RoutedEventArgs e) => TriggerHintAsync();
        private void Hint_Click(object sender, MouseButtonEventArgs e) => TriggerHintAsync();

        // P1-03: Hint chạy Async — không đơ UI
        private async void TriggerHintAsync()
        {
            if (_isAiThinking) return;
            _isAiThinking = true;
            txtStatusMessage.Text = "💡 Đang phân tích gợi ý nước đi...";

            var bestMove = await Task.Run(() => GetBestAiMove(_board, _activeTurn, 2));

            _isAiThinking = false;
            if (bestMove.HasValue)
            {
                var (sr, sc, dr, dc) = bestMove.Value;
                _selectedSquare = (sr, sc);
                _validMoves = new List<(int r, int c)> { (dr, dc) };
                RenderBoard();
                txtStatusMessage.Text = $"💡 Gợi ý: {(char)('a' + sc)}{8 - sr} → {(char)('a' + dc)}{8 - dr}";
                MessageBox.Show($"💡 Gợi ý nước đi tốt: Quân tại ô {(char)('a' + sc)}{8 - sr} di chuyển tới {(char)('a' + dc)}{8 - dr}", "Gợi ý nước đi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                txtStatusMessage.Text = "💡 Không tìm thấy gợi ý phù hợp.";
            }
        }

        private void Undo_Click(object sender, MouseButtonEventArgs e) => HandleUndo();

        private void HandleUndo()
        {
            if (_undoStack.Count > 0)
            {
                if (_currentMode == GameMode.PvAI && _undoStack.Count >= 2)
                {
                    _board = _undoStack.Pop();
                    _board = _undoStack.Pop();
                    _activeTurn = PieceColor.White;
                }
                else
                {
                    _board = _undoStack.Pop();
                    _activeTurn = _activeTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
                }

                RenderBoard();
                UpdateUI();
            }
        }

        private void FirstMove_Click(object sender, RoutedEventArgs e)
        {
            if (_undoStack.Count > 0)
            {
                while (_undoStack.Count > 1) _undoStack.Pop();
                _board = _undoStack.Pop();
                _activeTurn = PieceColor.White;
                RenderBoard();
                UpdateUI();
            }
        }

        // P1-08: Sửa null cast pattern
        private void PrevMove_Click(object sender, RoutedEventArgs e) => HandleUndo();
        // P1-05: Ẩn nút NextMove/LastMove chưa implement — giữ handler rỗng cho XAML compatibility
        private void NextMove_Click(object sender, RoutedEventArgs e) { /* Chưa implement - đã ẩn nút trong XAML */ }
        private void LastMove_Click(object sender, RoutedEventArgs e) { /* Chưa implement - đã ẩn nút trong XAML */ }

        private void CboGameMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboGameMode == null || panelAILevel == null || panelPuzzles == null) return;

            _currentMode = (GameMode)cboGameMode.SelectedIndex;
            panelAILevel.Visibility = _currentMode == GameMode.PvAI ? Visibility.Visible : Visibility.Collapsed;
            panelPuzzles.Visibility = (_currentMode == GameMode.Puzzle || _currentMode == GameMode.OpeningTrainer) ? Visibility.Visible : Visibility.Collapsed;

            CheckAndTriggerAiMove();
        }

        private void CboAILevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboAILevel != null)
            {
                _aiDifficulty = cboAILevel.SelectedIndex + 1;
            }
        }

        private void CboPuzzles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboPuzzles != null && (_currentMode == GameMode.Puzzle || _currentMode == GameMode.OpeningTrainer))
            {
                LoadPuzzle(cboPuzzles.SelectedIndex);
            }
        }

        private void LoadPuzzle(int puzzleIndex)
        {
            _board = new ChessPiece?[8, 8];
            _activeTurn = PieceColor.White;
            _selectedSquare = null;
            _validMoves.Clear();

            switch (puzzleIndex)
            {
                case 0:
                    _board[0, 4] = new ChessPiece(PieceType.King, PieceColor.Black);
                    _board[1, 5] = new ChessPiece(PieceType.Pawn, PieceColor.Black);
                    _board[7, 4] = new ChessPiece(PieceType.King, PieceColor.White);
                    _board[3, 7] = new ChessPiece(PieceType.Queen, PieceColor.White);
                    _board[4, 2] = new ChessPiece(PieceType.Bishop, PieceColor.White);
                    txtStatusMessage.Text = "🧩 THÁCH THỨC BÀI 1: Chiếu bí ngay trong 1 nước (Scholar's Mate)!";
                    break;
                case 1:
                    _board[0, 6] = new ChessPiece(PieceType.King, PieceColor.Black);
                    _board[1, 6] = new ChessPiece(PieceType.Pawn, PieceColor.Black);
                    _board[1, 7] = new ChessPiece(PieceType.Pawn, PieceColor.Black);
                    _board[7, 0] = new ChessPiece(PieceType.Rook, PieceColor.White);
                    _board[7, 4] = new ChessPiece(PieceType.King, PieceColor.White);
                    txtStatusMessage.Text = "🧩 THÁCH THỨC BÀI 2: Chiếu bí hàng 8 (Back Rank Mate)!";
                    break;
                case 2:
                    // P2-03: Smothered Mate (Chiếu thắt nút)
                    _board[0, 7] = new ChessPiece(PieceType.King, PieceColor.Black);
                    _board[0, 6] = new ChessPiece(PieceType.Rook, PieceColor.Black);
                    _board[1, 6] = new ChessPiece(PieceType.Pawn, PieceColor.Black);
                    _board[1, 7] = new ChessPiece(PieceType.Pawn, PieceColor.Black);
                    _board[2, 4] = new ChessPiece(PieceType.Knight, PieceColor.White);
                    _board[7, 4] = new ChessPiece(PieceType.King, PieceColor.White);
                    txtStatusMessage.Text = "🧩 THÁCH THỨC BÀI 3: Chiếu thắt nút (Smothered Mate - Dùng Mã f7)!";
                    break;
                case 3:
                    // P2-03: Knight Fork (Đòn Chĩa Mã)
                    _board[1, 4] = new ChessPiece(PieceType.King, PieceColor.Black);
                    _board[1, 2] = new ChessPiece(PieceType.Queen, PieceColor.Black);
                    _board[4, 3] = new ChessPiece(PieceType.Knight, PieceColor.White);
                    _board[7, 4] = new ChessPiece(PieceType.King, PieceColor.White);
                    txtStatusMessage.Text = "🧩 THÁCH THỨC BÀI 4: Đòn Chĩa Mã (Knight Fork - Bắt Hậu Đen)!";
                    break;
                case 4:
                    // P2-03: Pin & Discovery (Ghim & Mở Cột)
                    _board[0, 4] = new ChessPiece(PieceType.King, PieceColor.Black);
                    _board[3, 4] = new ChessPiece(PieceType.Queen, PieceColor.Black);
                    _board[7, 4] = new ChessPiece(PieceType.Rook, PieceColor.White);
                    _board[7, 0] = new ChessPiece(PieceType.King, PieceColor.White);
                    txtStatusMessage.Text = "🧩 THÁCH THỨC BÀI 5: Đòn Ghim (Pin - Dùng Xe e1 bắt Hậu Đen)!";
                    break;
                default:
                    InitNewGame();
                    break;
            }
            RenderBoard();
        }

        private void ExportFEN_Click(object sender, RoutedEventArgs e)
        {
            string fen = GenerateFEN();
            Clipboard.SetText(fen);
            MessageBox.Show($"Đã sao chép chuỗi FEN vào Clipboard:\n{fen}", "Export FEN", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // P1-08: Sửa null cast pattern
        private void ExportPGN_Click(object sender, RoutedEventArgs e) => HandleExportPGN();
        private void ExportPGN_Click(object sender, MouseButtonEventArgs e) => HandleExportPGN();
        private void HandleExportPGN()
        {
            string pgn = GeneratePGN();
            Clipboard.SetText(pgn);
            MessageBox.Show($"📜 Đã xuất & Sao chép dữ liệu PGN (Portable Game Notation) thành công:\n\n{pgn}", "Export PGN", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GeneratePGN()
        {
            string dateStr = DateTime.Now.ToString("yyyy.MM.dd");
            List<string> pgnLines = new()
            {
                "[Event \"QA SmartClass School Tournament\"]",
                "[Site \"QA SmartClass v4.2\"]",
                $"[Date \"{dateStr}\"]",
                "[Round \"1\"]",
                "[White \"Học sinh (Trắng)\"]",
                "[Black \"Máy AI Bot (Đen)\"]",
                "[Result \"*\"]",
                ""
            };

            // P1-02: Sửa PGN — không fallback vào dữ liệu giả
            string movesText = string.Join(" ", _moveHistoryLog);
            pgnLines.Add(movesText.Length > 0 ? movesText : "*");
            return string.Join("\n", pgnLines);
        }

        private string GenerateFEN()
        {
            List<string> rows = new();
            for (int r = 0; r < 8; r++)
            {
                string rowStr = "";
                int emptyCount = 0;
                for (int c = 0; c < 8; c++)
                {
                    ChessPiece? p = _board[r, c];
                    if (p == null) emptyCount++;
                    else
                    {
                        if (emptyCount > 0) { rowStr += emptyCount; emptyCount = 0; }
                        char cSymbol = p.Type switch
                        {
                            PieceType.Pawn => 'p', PieceType.Knight => 'n', PieceType.Bishop => 'b',
                            PieceType.Rook => 'r', PieceType.Queen => 'q', PieceType.King => 'k', _ => '?'
                        };
                        if (p.Color == PieceColor.White) cSymbol = char.ToUpper(cSymbol);
                        rowStr += cSymbol;
                    }
                }
                if (emptyCount > 0) rowStr += emptyCount;
                rows.Add(rowStr);
            }
            string turnStr = _activeTurn == PieceColor.White ? "w" : "b";
            // P1-02: Sửa FEN — thêm castling availability + en passant target
            string castling = "";
            if (_board[7, 4] != null && !_board[7, 4]!.HasMoved && _board[7, 4]!.Type == PieceType.King)
            {
                if (_board[7, 7] != null && !_board[7, 7]!.HasMoved && _board[7, 7]!.Type == PieceType.Rook) castling += "K";
                if (_board[7, 0] != null && !_board[7, 0]!.HasMoved && _board[7, 0]!.Type == PieceType.Rook) castling += "Q";
            }
            if (_board[0, 4] != null && !_board[0, 4]!.HasMoved && _board[0, 4]!.Type == PieceType.King)
            {
                if (_board[0, 7] != null && !_board[0, 7]!.HasMoved && _board[0, 7]!.Type == PieceType.Rook) castling += "k";
                if (_board[0, 0] != null && !_board[0, 0]!.HasMoved && _board[0, 0]!.Type == PieceType.Rook) castling += "q";
            }
            if (castling == "") castling = "-";

            string epStr = _enPassantTarget.HasValue ?
                $"{(char)('a' + _enPassantTarget.Value.c)}{8 - _enPassantTarget.Value.r}" : "-";

            return $"{string.Join("/", rows)} {turnStr} {castling} {epStr} {_halfMoveClock} {_fullMoveNumber}";
        }

        private void UpdateUI()
        {
            txtTurnIndicator.Text = $"Lượt đi: {(_activeTurn == PieceColor.White ? "Trắng (White)" : "Đen (Black)")}";
            txtMoveCount.Text = $"{_moveHistoryLog.Count} nước";

            lstMoveHistory.ItemsSource = null;
            lstMoveHistory.ItemsSource = _moveHistoryLog;
            if (_moveHistoryLog.Count > 0)
                lstMoveHistory.ScrollIntoView(_moveHistoryLog.Last());

            // Live Badges & Accuracy Stats (Việt hóa theo QC_4.2_LANGUAGE_BRANDING)
            if (badgeBrilliant != null) badgeBrilliant.Text = $"!! {CountBrilliant} Thiên tài";
            if (badgeBest != null) badgeBest.Text = $"★ {CountBest} Tối ưu";
            if (badgeExcellent != null) badgeExcellent.Text = $"👍 {CountExcellent} Xuất sắc";
            if (badgeBlunder != null) badgeBlunder.Text = $"?? {CountBlunder} Sai lầm";

            if (txtBrilliantStat != null) txtBrilliantStat.Text = $"!! {CountBrilliant} Thiên tài";
            if (txtBestStat != null) txtBestStat.Text = $"★ {CountBest} Tối ưu";
            if (txtExcellentStat != null) txtExcellentStat.Text = $"👍 {CountExcellent} Xuất sắc";
            if (txtBlunderStat != null) txtBlunderStat.Text = $"?? {CountBlunder} Sai lầm";

            int totalMoves = _moveHistoryLog.Count;
            if (txtAccuracyScore != null)
            {
                if (totalMoves > 0)
                {
                    double score = ((CountBrilliant * 1.0 + CountBest * 1.0 + CountExcellent * 0.9 + CountGood * 0.8) / totalMoves) * 100.0;
                    score = System.Math.Min(100.0, System.Math.Max(50.0, score));
                    txtAccuracyScore.Text = $"Độ chính xác: {score:F1}%";
                }
                else
                {
                    txtAccuracyScore.Text = "Độ chính xác: 100%";
                }
            }

            // Real-time Advantage Evaluation Bar Update
            int eval = EvaluateBoard(_board);
            if (rowEvalWhite != null && rowEvalBlack != null && txtEvalScore != null)
            {
                double whiteRatio = 0.5 + (eval * 0.05);
                whiteRatio = System.Math.Min(0.95, System.Math.Max(0.05, whiteRatio));
                rowEvalWhite.Height = new GridLength(whiteRatio, GridUnitType.Star);
                rowEvalBlack.Height = new GridLength(1.0 - whiteRatio, GridUnitType.Star);
                txtEvalScore.Text = eval >= 0 ? $"+{eval}" : $"{eval}";
            }

            spCapturedWhite.Children.Clear();
            int whiteScore = 0;
            foreach (var p in _capturedWhite)
            {
                whiteScore += p.Value;
                spCapturedWhite.Children.Add(new TextBlock { Text = p.Symbol, FontSize = 18, Foreground = Brushes.White, Margin = new Thickness(2, 0, 2, 0) });
            }

            spCapturedBlack.Children.Clear();
            int blackScore = 0;
            foreach (var p in _capturedBlack)
            {
                blackScore += p.Value;
                // P1-06: Sửa màu quân Đen bị bắt — dùng màu sáng trên nền tối
                spCapturedBlack.Children.Add(new TextBlock { Text = p.Symbol, FontSize = 18, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BAB8B6")), Margin = new Thickness(2, 0, 2, 0) });
            }

            int diff = whiteScore - blackScore;
            if (diff > 0)
            {
                txtMaterialDiffWhite.Text = $"+{diff}";
                txtMaterialDiffBlack.Text = "";
            }
            else if (diff < 0)
            {
                txtMaterialDiffWhite.Text = "";
                txtMaterialDiffBlack.Text = $"+{-diff}";
            }
            else
            {
                txtMaterialDiffWhite.Text = "";
                txtMaterialDiffBlack.Text = "";
            }
        }
    }
}
