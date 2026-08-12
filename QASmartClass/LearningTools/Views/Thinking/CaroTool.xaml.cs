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
    public enum CaroPieceColor { None, X, O }
    public enum CaroRuleMode { Vietnamese, GomokuFree }

    public partial class CaroTool : BaseToolControl
    {
        private CaroPieceColor[,] _board = new CaroPieceColor[15, 15];
        private CaroPieceColor _activeTurn = CaroPieceColor.X;
        private (int r, int c)? _lastMove = null;
        private List<(int r, int c)> _winningFive = new();
        private List<string> _moveHistoryLog = new();
        private Stack<CaroPieceColor[,]> _undoStack = new();

        private GameMode _currentMode = GameMode.PvP;
        private CaroRuleMode _currentRule = CaroRuleMode.Vietnamese;
        private bool _isAiThinking = false;
        private bool _isGameOver = false;

        private DispatcherTimer _gameTimer = new();
        private int _timeXSeconds = 300;
        private int _timeOSeconds = 300;

        public CaroTool()
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
                if (_isGameOver) return;

                if (_activeTurn == CaroPieceColor.X && _timeXSeconds > 0)
                {
                    _timeXSeconds--;
                    if (_timeXSeconds == 0) TriggerTimeoutLoss(CaroPieceColor.X);
                }
                else if (_activeTurn == CaroPieceColor.O && _timeOSeconds > 0)
                {
                    _timeOSeconds--;
                    if (_timeOSeconds == 0) TriggerTimeoutLoss(CaroPieceColor.O);
                }

                txtTimerX.Text = TimeSpan.FromSeconds(_timeXSeconds).ToString(@"mm\:ss");
                txtTimerO.Text = TimeSpan.FromSeconds(_timeOSeconds).ToString(@"mm\:ss");
            };
        }

        private void TriggerTimeoutLoss(CaroPieceColor timedOutPlayer)
        {
            _gameTimer.Stop();
            _isGameOver = true;
            CaroPieceColor winner = timedOutPlayer == CaroPieceColor.X ? CaroPieceColor.O : CaroPieceColor.X;

            txtStatusMessage.Text = $"⏰ HẾT GIỜ! Quân {(winner == CaroPieceColor.X ? "X (Xanh)" : "O (Đỏ)")} Thắng!";
            MessageBox.Show($"Hết thời gian thi đấu! Quân {(winner == CaroPieceColor.X ? "X (Xanh)" : "O (Đỏ)")} giành chiến thắng.", "Hết giờ", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void InitNewGame()
        {
            _board = new CaroPieceColor[15, 15];
            _activeTurn = CaroPieceColor.X;
            _lastMove = null;
            _winningFive.Clear();
            _moveHistoryLog.Clear();
            _undoStack.Clear();
            _isGameOver = false;

            _timeXSeconds = 300;
            _timeOSeconds = 300;

            // Timer will start when user navigates to Play Tab or makes first move
            if (sideMenu != null && sideMenu.SelectedIndex == 1)
            {
                _gameTimer.Start();
            }
            else
            {
                _gameTimer.Stop();
            }

            RenderBoard();
            UpdateUI();
        }

        private void InitTournamentData()
        {
            if (lstTournamentRankings != null)
            {
                lstTournamentRankings.ItemsSource = new List<string>
                {
                    "🥇 1. Nguyễn Hoàng Long (8A1) - 5/5 trận thắng (Elo 1550)",
                    "🥈 2. Trần Khánh Linh (8A2) - 4/5 trận thắng (Elo 1490)",
                    "🥉 3. Phạm Quốc Bảo (8A4) - 3.5/5 trận thắng (Elo 1420)",
                    "🎗️ 4. Vũ Minh Nhật (8A3) - 3/5 trận thắng (Elo 1380)"
                };
            }
        }

        private void RenderBoard()
        {
            gridCaroBoard.Children.Clear();
            gridCaroBoard.RowDefinitions.Clear();
            gridCaroBoard.ColumnDefinitions.Clear();

            for (int i = 0; i < 15; i++)
            {
                gridCaroBoard.RowDefinitions.Add(new RowDefinition());
                gridCaroBoard.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int r = 0; r < 15; r++)
            {
                for (int c = 0; c < 15; c++)
                {
                    Border cell = new Border
                    {
                        Background = Brushes.Transparent,
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                        BorderThickness = new Thickness(0.5),
                        Tag = (r, c),
                        Cursor = Cursors.Hand
                    };

                    if (_lastMove.HasValue && _lastMove.Value.r == r && _lastMove.Value.c == c)
                    {
                        cell.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF08A"));
                    }
                    else if (_winningFive.Contains((r, c)))
                    {
                        cell.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));
                    }

                    CaroPieceColor piece = _board[r, c];
                    if (piece != CaroPieceColor.None)
                    {
                        TextBlock txtPiece = new TextBlock
                        {
                            Text = piece == CaroPieceColor.X ? "❌" : "⭕",
                            FontSize = 18,
                            FontWeight = FontWeights.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Foreground = piece == CaroPieceColor.X ? 
                                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB")) : 
                                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"))
                        };
                        cell.Child = txtPiece;
                    }

                    int row = r, col = c;
                    cell.MouseLeftButtonDown += (s, e) => Square_Click(row, col);
                    Grid.SetRow(cell, r);
                    Grid.SetColumn(cell, c);
                    gridCaroBoard.Children.Add(cell);
                }
            }
        }

        private async void Square_Click(int r, int c)
        {
            if (_isGameOver || _isAiThinking || _board[r, c] != CaroPieceColor.None) return;

            if (!_gameTimer.IsEnabled)
            {
                _gameTimer.Start();
            }

            ExecuteMove(r, c);
            RenderBoard();

            if (!_isGameOver && _currentMode == GameMode.PvAI && _activeTurn == CaroPieceColor.O)
            {
                await MakeAiMoveAsync();
            }
        }

        private void ExecuteMove(int r, int c)
        {
            SaveUndoState();

            _board[r, c] = _activeTurn;
            _lastMove = (r, c);
            PlayMoveSound();

            string pieceSymbol = _activeTurn == CaroPieceColor.X ? "❌ (X)" : "⭕ (O)";
            string moveNotation = $"{pieceSymbol} tại ô (Hàng {r + 1}, Cột {c + 1})";
            _moveHistoryLog.Add($"{_moveHistoryLog.Count + 1}. {moveNotation}");

            if (CheckWinner(r, c, _board, _currentRule, out List<(int r, int c)> winningFive))
            {
                _gameTimer.Stop();
                _isGameOver = true;
                _winningFive = winningFive;

                txtStatusMessage.Text = $"🏆 CHIẾN THẮNG! Quân {pieceSymbol} đã tạo chuỗi 5 quân thắng cuộc!";
                PlayVictorySound();
                MessageBox.Show($"Chúc mừng! Quân {pieceSymbol} đã chiến thắng!", "Kết thúc ván cờ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (IsBoardFull())
            {
                _gameTimer.Stop();
                _isGameOver = true;
                txtStatusMessage.Text = "🤝 HÒA CỜ! Tất cả các ô cờ đã được điền đầy.";
                MessageBox.Show("Ván cờ Hòa! Tất cả các ô cờ đã được sử dụng.", "Hòa cờ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                _activeTurn = _activeTurn == CaroPieceColor.X ? CaroPieceColor.O : CaroPieceColor.X;
                txtStatusMessage.Text = $"Lượt đi: {(_activeTurn == CaroPieceColor.X ? "X (Xanh)" : "O (Đỏ)")}";
            }

            UpdateUI();
        }

        public bool IsBoardFull()
        {
            for (int r = 0; r < 15; r++)
            {
                for (int c = 0; c < 15; c++)
                {
                    if (_board[r, c] == CaroPieceColor.None) return false;
                }
            }
            return true;
        }

        private void PlayMoveSound() => SystemSounds.Asterisk.Play();
        private void PlayVictorySound() => SystemSounds.Beep.Play();

        // ═══════════════════════════════════════════════════════════
        //  CARO WINNER CHECK ENGINE (4 DIRECTIONS)
        // ═══════════════════════════════════════════════════════════

        public bool CheckWinner(int r, int c, CaroPieceColor[,] board, CaroRuleMode rule, out List<(int r, int c)> winningLine)
        {
            winningLine = new List<(int r, int c)>();
            CaroPieceColor player = board[r, c];
            if (player == CaroPieceColor.None) return false;

            int[,] dirs = { { 0, 1 }, { 1, 0 }, { 1, 1 }, { 1, -1 } }; // Horizontal, Vertical, Main Diag, Anti Diag

            for (int d = 0; d < 4; d++)
            {
                int dr = dirs[d, 0];
                int dc = dirs[d, 1];

                var line = new List<(int r, int c)> { (r, c) };

                // Forward direction
                int step = 1;
                while (IsInBounds(r + step * dr, c + step * dc) && board[r + step * dr, c + step * dc] == player)
                {
                    line.Add((r + step * dr, c + step * dc));
                    step++;
                }
                int endR1 = r + step * dr;
                int endC1 = c + step * dc;

                // Backward direction
                step = 1;
                while (IsInBounds(r - step * dr, c - step * dc) && board[r - step * dr, c - step * dc] == player)
                {
                    line.Add((r - step * dr, c - step * dc));
                    step++;
                }
                int endR2 = r - step * dr;
                int endC2 = c - step * dc;

                if (line.Count >= 5)
                {
                    if (rule == CaroRuleMode.Vietnamese)
                    {
                        // Check blocked both ends constraint
                        bool blocked1 = IsInBounds(endR1, endC1) && board[endR1, endC1] != CaroPieceColor.None && board[endR1, endC1] != player;
                        bool blocked2 = IsInBounds(endR2, endC2) && board[endR2, endC2] != CaroPieceColor.None && board[endR2, endC2] != player;

                        if (blocked1 && blocked2) continue; // Blocked both ends! Not a win in Vietnamese rule!
                    }

                    winningLine = line.Take(5).ToList();
                    return true;
                }
            }

            return false;
        }

        public bool CheckWinnerForTest(int r, int c) => CheckWinner(r, c, _board, _currentRule, out _);

        private bool IsInBounds(int r, int c) => r >= 0 && r < 15 && c >= 0 && c < 15;

        // ═══════════════════════════════════════════════════════════
        //  AI MINIMAX ENGINE WITH HEURISTIC CANDIDATE FILTER
        // ═══════════════════════════════════════════════════════════

        private async Task MakeAiMoveAsync()
        {
            _isAiThinking = true;
            txtStatusMessage.Text = "🤖 Máy AI đang tính toán nước đi...";

            var boardSnapshot = CloneBoard(_board);
            var bestMove = await Task.Run(() =>
            {
                return GetBestAiMove(boardSnapshot, CaroPieceColor.O);
            });

            _isAiThinking = false;

            if (bestMove.HasValue)
            {
                ExecuteMove(bestMove.Value.r, bestMove.Value.c);
                RenderBoard();
            }
        }

        private (int r, int c)? GetBestAiMove(CaroPieceColor[,] board, CaroPieceColor aiPlayer)
        {
            var candidates = GetCandidates(board);
            if (candidates.Count == 0) return (7, 7);

            (int r, int c)? bestMove = null;
            int maxEval = int.MinValue;

            foreach (var (r, c) in candidates)
            {
                board[r, c] = aiPlayer;
                if (CheckWinner(r, c, board, _currentRule, out _))
                {
                    board[r, c] = CaroPieceColor.None;
                    return (r, c); // Immediate winning move!
                }

                // Check opponent winning threat
                CaroPieceColor opponent = aiPlayer == CaroPieceColor.X ? CaroPieceColor.O : CaroPieceColor.X;
                board[r, c] = opponent;
                if (CheckWinner(r, c, board, _currentRule, out _))
                {
                    board[r, c] = CaroPieceColor.None;
                    return (r, c); // Block opponent immediate win!
                }

                board[r, c] = aiPlayer;
                int eval = EvaluateBoard(board, aiPlayer);
                board[r, c] = CaroPieceColor.None;

                if (eval > maxEval)
                {
                    maxEval = eval;
                    bestMove = (r, c);
                }
            }

            return bestMove ?? candidates[0];
        }

        private List<(int r, int c)> GetCandidates(CaroPieceColor[,] board)
        {
            var list = new List<(int r, int c)>();
            bool hasPieces = false;

            for (int r = 0; r < 15; r++)
            {
                for (int c = 0; c < 15; c++)
                {
                    if (board[r, c] != CaroPieceColor.None)
                    {
                        hasPieces = true;
                        for (int dr = -2; dr <= 2; dr++)
                        {
                            for (int dc = -2; dc <= 2; dc++)
                            {
                                int nr = r + dr;
                                int nc = c + dc;
                                if (IsInBounds(nr, nc) && board[nr, nc] == CaroPieceColor.None && !list.Contains((nr, nc)))
                                {
                                    list.Add((nr, nc));
                                }
                            }
                        }
                    }
                }
            }

            if (!hasPieces) list.Add((7, 7));
            return list;
        }

        private int EvaluateBoard(CaroPieceColor[,] board, CaroPieceColor player)
        {
            int score = 0;
            CaroPieceColor opponent = player == CaroPieceColor.X ? CaroPieceColor.O : CaroPieceColor.X;

            for (int r = 0; r < 15; r++)
            {
                for (int c = 0; c < 15; c++)
                {
                    if (board[r, c] == player)
                    {
                        score += EvaluateCellPattern(r, c, board, player);
                    }
                    else if (board[r, c] == opponent)
                    {
                        score -= (int)(EvaluateCellPattern(r, c, board, opponent) * 1.2);
                    }
                }
            }
            return score;
        }

        private int EvaluateCellPattern(int r, int c, CaroPieceColor[,] board, CaroPieceColor player)
        {
            int cellScore = 0;
            int[,] dirs = { { 0, 1 }, { 1, 0 }, { 1, 1 }, { 1, -1 } };

            for (int d = 0; d < 4; d++)
            {
                int dr = dirs[d, 0];
                int dc = dirs[d, 1];
                int count = 1;

                int step = 1;
                while (IsInBounds(r + step * dr, c + step * dc) && board[r + step * dr, c + step * dc] == player)
                {
                    count++;
                    step++;
                }
                int stepBack = 1;
                while (IsInBounds(r - stepBack * dr, c - stepBack * dc) && board[r - stepBack * dr, c - stepBack * dc] == player)
                {
                    count++;
                    stepBack++;
                }

                if (count >= 5) cellScore += 10000;
                else if (count == 4) cellScore += 1000;
                else if (count == 3) cellScore += 200;
                else if (count == 2) cellScore += 30;
            }

            return cellScore;
        }

        private CaroPieceColor[,] CloneBoard(CaroPieceColor[,] src)
        {
            CaroPieceColor[,] dst = new CaroPieceColor[15, 15];
            Array.Copy(src, dst, src.Length);
            return dst;
        }

        private void SaveUndoState()
        {
            _undoStack.Push(CloneBoard(_board));
        }

        // ═══════════════════════════════════════════════════════════
        //  UI HANDLERS
        // ═══════════════════════════════════════════════════════════

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewPlay == null || viewAnalysis == null || viewTournament == null || viewPractical == null) return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewPlay.Visibility = Visibility.Collapsed;
            viewAnalysis.Visibility = Visibility.Collapsed;
            viewTournament.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            // Toggle header Undo & Resign buttons visibility based on Tab
            if (btnHeaderUndo != null) btnHeaderUndo.Visibility = sideMenu.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
            if (btnHeaderResign != null) btnHeaderResign.Visibility = sideMenu.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    _gameTimer.Stop();
                    break;
                case 1:
                    viewPlay.Visibility = Visibility.Visible;
                    if (!_isGameOver) _gameTimer.Start();
                    break;
                case 2:
                    viewAnalysis.Visibility = Visibility.Visible;
                    _gameTimer.Stop();
                    break;
                case 3:
                    viewTournament.Visibility = Visibility.Visible;
                    _gameTimer.Stop();
                    break;
                case 4:
                    viewPractical.Visibility = Visibility.Visible;
                    _gameTimer.Stop();
                    break;
            }
        }

        private void NewGame_Click(object sender, RoutedEventArgs e) => NewGame_Click(sender, (MouseButtonEventArgs)null!);
        private void NewGame_Click(object sender, MouseButtonEventArgs e)
        {
            if (MessageBox.Show("Bạn có muốn bắt đầu ván Caro mới không?", "Ván mới", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                InitNewGame();
            }
        }

        private void Undo_Click(object sender, RoutedEventArgs e) => Undo_Click(sender, (MouseButtonEventArgs)null!);
        private void Undo_Click(object sender, MouseButtonEventArgs e)
        {
            if (_undoStack.Count > 0)
            {
                if (_currentMode == GameMode.PvAI && _undoStack.Count >= 2)
                {
                    _board = _undoStack.Pop();
                    _board = _undoStack.Pop();
                    _activeTurn = CaroPieceColor.X;
                }
                else
                {
                    _board = _undoStack.Pop();
                    _activeTurn = _activeTurn == CaroPieceColor.X ? CaroPieceColor.O : CaroPieceColor.X;
                }

                _isGameOver = false;
                _winningFive.Clear();
                RenderBoard();
                UpdateUI();
            }
        }

        private void Resign_Click(object sender, RoutedEventArgs e) => Resign_Click(sender, (MouseButtonEventArgs)null!);
        private void Resign_Click(object sender, MouseButtonEventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc chắn muốn đầu hàng không?", "Đầu hàng", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _gameTimer.Stop();
                _isGameOver = true;
                string loser = _activeTurn == CaroPieceColor.X ? "X (Xanh)" : "O (Đỏ)";
                string winner = _activeTurn == CaroPieceColor.X ? "O (Đỏ)" : "X (Xanh)";

                txtStatusMessage.Text = $"🏳️ Quân {loser} Đã đầu hàng. Quân {winner} Thắng!";
                MessageBox.Show($"Quân {loser} đã đầu hàng! Quân {winner} giành chiến thắng.", "Kết thúc ván cờ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void Hint_Click(object sender, RoutedEventArgs e) => Hint_Click(sender, (MouseButtonEventArgs)null!);
        private async void Hint_Click(object sender, MouseButtonEventArgs e)
        {
            if (_isGameOver) return;
            txtStatusMessage.Text = "💡 Đang tính toán gợi ý...";
            var boardSnapshot = CloneBoard(_board);
            var bestMove = await Task.Run(() => GetBestAiMove(boardSnapshot, _activeTurn));
            
            if (bestMove.HasValue)
            {
                var (r, c) = bestMove.Value;
                txtStatusMessage.Text = $"💡 Gợi ý: Gần nhất tại Ô (Hàng {r + 1}, Cột {c + 1})";
                MessageBox.Show($"💡 Gợi ý nước đi tốt nhất: Ô tại (Hàng {r + 1}, Cột {c + 1})", "Gợi ý nước đi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CreateTournament_Click(object sender, RoutedEventArgs e) => CreateTournament_Click(sender, (MouseButtonEventArgs)null!);
        private void CreateTournament_Click(object sender, MouseButtonEventArgs e)
        {
            if (sideMenu != null) sideMenu.SelectedIndex = 3;
            MessageBox.Show("🏆 Đã kích hoạt Trình tạo Giải đấu Cờ Caro Trường học!", "Giải Đấu Caro", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CboGameMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboGameMode != null)
            {
                _currentMode = (GameMode)cboGameMode.SelectedIndex;
            }
        }

        private void CboCaroRule_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboCaroRule != null)
            {
                _currentRule = (CaroRuleMode)cboCaroRule.SelectedIndex;
            }
        }

        private void UpdateUI()
        {
            txtTurnIndicator.Text = $"Lượt đi: {(_activeTurn == CaroPieceColor.X ? "X (Xanh)" : "O (Đỏ)")}";
            txtMoveCount.Text = $"{_moveHistoryLog.Count} nước";

            // Update active player card borders and rating text
            if (borderPlayerX != null && borderPlayerO != null && txtPlayerXRating != null && txtPlayerORating != null)
            {
                if (_activeTurn == CaroPieceColor.X)
                {
                    borderPlayerX.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                    borderPlayerX.BorderThickness = new Thickness(2);
                    txtPlayerXRating.Text = "▶️ Lượt đi";
                    txtPlayerXRating.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));

                    borderPlayerO.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
                    borderPlayerO.BorderThickness = new Thickness(1);
                    txtPlayerORating.Text = "Chờ...";
                    txtPlayerORating.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                }
                else
                {
                    borderPlayerO.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                    borderPlayerO.BorderThickness = new Thickness(2);
                    txtPlayerORating.Text = "▶️ Lượt đi";
                    txtPlayerORating.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));

                    borderPlayerX.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
                    borderPlayerX.BorderThickness = new Thickness(1);
                    txtPlayerXRating.Text = "Chờ...";
                    txtPlayerXRating.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                }
            }

            // Update Tab 3 AI Evaluator advice dynamically
            if (txtAnalysisAdvice != null)
            {
                if (_moveHistoryLog.Count == 0)
                {
                    txtAnalysisAdvice.Text = "• Chưa có dữ liệu nước đi. Hãy di chuyển quân trên bàn cờ để AI phân tích!";
                }
                else
                {
                    txtAnalysisAdvice.Text = $"• Trận đấu đã diễn ra {_moveHistoryLog.Count} nước đi.\n" +
                                             $"• Đánh giá AI Engine: Lượt đi tiếp theo nghiêng về bên {(_activeTurn == CaroPieceColor.X ? "Quân X (Xanh)" : "Quân O (Đỏ)")}.\n" +
                                             $"• Lời khuyên chiến thuật: Tập trung kiểm soát ô trung tâm và tạo các thế cờ 3 mở hai đầu.";
                }
            }

            lstMoveHistory.ItemsSource = null;
            lstMoveHistory.ItemsSource = _moveHistoryLog;
            if (_moveHistoryLog.Count > 0)
                lstMoveHistory.ScrollIntoView(_moveHistoryLog.Last());
        }
    }
}
