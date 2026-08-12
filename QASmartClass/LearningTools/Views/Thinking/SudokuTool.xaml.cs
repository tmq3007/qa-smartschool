using QASmartClass.LearningTools.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Helpers;

namespace QASmartClass.LearningTools.Views.Thinking
{
    public partial class SudokuTool : BaseToolControl
    {
        private int _size = 9;
        private int[,] _solution = new int[0, 0];
        private int[,] _puzzle = new int[0, 0];
        private TextBox[,] _cells = new TextBox[0, 0];
        private TextBox? _selectedCell;
        private readonly Random _rng = new();
        private DateTime _startTime = DateTime.Now;
        private DateTime _lastCheckTime = DateTime.MinValue;

        private DispatcherTimer? _confettiTimer;
        private readonly List<ConfettiParticle> _particles = new();
        private readonly Color[] _confettiColors = new[]
        {
            Color.FromRgb(255, 193, 7),   // Vàng
            Color.FromRgb(233, 30, 99),   // Hồng
            Color.FromRgb(33, 150, 243),  // Xanh dương
            Color.FromRgb(76, 175, 80),   // Xanh lá
            Color.FromRgb(156, 39, 176),  // Tím
            Color.FromRgb(255, 87, 34)    // Cam
        };

        private class ConfettiParticle
        {
            public Rectangle Shape { get; set; } = null!;
            public double X { get; set; }
            public double Y { get; set; }
            public double SpeedY { get; set; }
            public double SpeedX { get; set; }
            public double Angle { get; set; }
            public double RotationSpeed { get; set; }
        }

        public SudokuTool()
        {
            InitializeComponent();
            DbManager.Initialize();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextPlay != null) menuTextPlay.Text = isVN ? "Trò chơi Sudoku" : "Sudoku Game";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                var settings = GameSettingsManager.Load();
                int targetSize = settings.SudokuSize;
                bool sizeChanged = false;
                if (targetSize == 4 && rb4?.IsChecked == false) { rb4.IsChecked = true; sizeChanged = true; }
                else if (targetSize == 6 && rb6?.IsChecked == false) { rb6.IsChecked = true; sizeChanged = true; }
                else if (targetSize == 9 && rb9?.IsChecked == false) { rb9.IsChecked = true; sizeChanged = true; }

                if (!sizeChanged)
                {
                    _size = targetSize;
                    NewGame();
                    BuildNumPad();
                }
                LoadPracticalApps();
            };
        }

        private void Size_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            if (rb4?.IsChecked == true) _size = 4;
            else if (rb6?.IsChecked == true) _size = 6;
            else _size = 9;

            var settings = GameSettingsManager.Load();
            settings.SudokuSize = _size;
            GameSettingsManager.Save(settings);

            NewGame();
            BuildNumPad();
        }

        private void NewGame_Click(object sender, MouseButtonEventArgs e) => NewGame();
        private void Hint_Click(object sender, MouseButtonEventArgs e) => GiveHint();
        private void Check_Click(object sender, MouseButtonEventArgs e) => CheckSolution();

        private void NewGame()
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 1)
            {
                sideMenu.SelectedIndex = 0;
            }
            _startTime = DateTime.Now;
            StopConfetti();
            _solution = GenerateSolution(_size);
            _puzzle = (int[,])_solution.Clone();
            RemoveCells(_puzzle, _size);
            BuildGrid();
            _selectedCell = null;
            txtStatus.Text = "🎮 Đang chơi...";
            txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(136, 14, 79));
            UpdateProgress();
        }

        // ═══════════════════════════════════════════════════════════
        //  BUILD GRID — larger cells, bigger fonts
        // ═══════════════════════════════════════════════════════════

        private void BuildGrid()
        {
            sudokuGrid.Children.Clear();
            sudokuGrid.RowDefinitions.Clear();
            sudokuGrid.ColumnDefinitions.Clear();

            _cells = new TextBox[_size, _size];

            // Larger cell sizes for better visibility
            int cellSize = _size switch
            {
                4 => 90,
                6 => 75,
                _ => 62
            };

            int fontSize = _size switch
            {
                4 => 32,
                6 => 26,
                _ => 24
            };

            for (int i = 0; i < _size; i++)
            {
                sudokuGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(cellSize) });
                sudokuGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cellSize) });
            }

            int boxH = _size == 4 ? 2 : _size == 6 ? 2 : 3;
            int boxW = _size == 4 ? 2 : _size == 6 ? 3 : 3;

            // Update tips dynamically
            if (tipRow != null) tipRow.Text = $"• Mỗi hàng: điền số từ 1 đến {_size} (không lặp)";
            if (tipCol != null) tipCol.Text = $"• Mỗi cột: điền số từ 1 đến {_size} (không lặp)";
            if (tipBox != null) tipBox.Text = $"• Mỗi ô vuông nhỏ {boxW}×{boxH}: điền số từ 1 đến {_size} (không lặp)";

            for (int r = 0; r < _size; r++)
            {
                for (int c = 0; c < _size; c++)
                {
                    bool isGiven = _puzzle[r, c] != 0;

                    var tb = new TextBox
                    {
                        Text = isGiven ? _puzzle[r, c].ToString() : "",
                        FontSize = fontSize,
                        FontFamily = new FontFamily("Segoe UI"),
                        FontWeight = isGiven ? FontWeights.Bold : FontWeights.SemiBold,
                        Foreground = isGiven
                            ? new SolidColorBrush(Color.FromRgb(33, 33, 33))
                            : new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                        Background = isGiven
                            ? new SolidColorBrush(Color.FromRgb(248, 248, 248))
                            : Brushes.White,
                        IsReadOnly = isGiven,
                        TextAlignment = TextAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        MaxLength = _size > 9 ? 2 : 1,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                        BorderThickness = new Thickness(
                            c % boxW == 0 ? 3 : 0.8,
                            r % boxH == 0 ? 3 : 0.8,
                            c == _size - 1 ? 3 : 0.8,
                            r == _size - 1 ? 3 : 0.8),
                        Padding = new Thickness(0),
                        Cursor = isGiven ? Cursors.Arrow : Cursors.Hand,
                    };

                    // Track selected cell for numpad
                    if (!isGiven)
                    {
                        tb.GotFocus += (s, _) =>
                        {
                            var cell = s as TextBox;
                            if (cell != null)
                            {
                                ClearCellHighlights();
                                _selectedCell = cell;
                                cell.Tag = cell.Background; // Save current background (e.g. Red, Green, or White)
                                cell.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                                cell.SelectAll(); // Select all text to allow overwrite
                            }
                        };

                        tb.LostFocus += (s, _) =>
                        {
                            var cell = s as TextBox;
                            if (cell != null)
                            {
                                // Revert to saved background if the text hasn't changed (or if it's still in Tag)
                                if (cell.Tag is Brush savedBrush)
                                {
                                    cell.Background = savedBrush;
                                }
                                else
                                {
                                    cell.Background = Brushes.White;
                                }
                            }
                        };

                        tb.PreviewTextInput += (sender, args) =>
                        {
                            if (!int.TryParse(args.Text, out int num) || num < 1 || num > _size)
                            {
                                args.Handled = true;
                            }
                        };
                    }

                    int capturedR = r;
                    int capturedC = c;
                    tb.PreviewKeyDown += (sender, args) =>
                    {
                        if (args.Key == Key.Space)
                        {
                            args.Handled = true;
                            return;
                        }

                        int targetR = capturedR;
                        int targetC = capturedC;
                        bool isArrowKey = false;

                        switch (args.Key)
                        {
                            case Key.Up:
                                targetR = (capturedR - 1 + _size) % _size;
                                isArrowKey = true;
                                break;
                            case Key.Down:
                                targetR = (capturedR + 1) % _size;
                                isArrowKey = true;
                                break;
                            case Key.Left:
                                targetC = (capturedC - 1 + _size) % _size;
                                isArrowKey = true;
                                break;
                            case Key.Right:
                                targetC = (capturedC + 1) % _size;
                                isArrowKey = true;
                                break;
                        }

                        if (isArrowKey)
                        {
                            _cells[targetR, targetC].Focus();
                            args.Handled = true;
                        }
                    };

                    tb.TextChanged += (sender, _) =>
                    {
                        var cell = sender as TextBox;
                        if (cell != null && !cell.IsReadOnly)
                        {
                            // If text changed, the check state is invalid. Reset Tag and background.
                            cell.Tag = Brushes.White;
                            if (!cell.IsFocused)
                            {
                                cell.Background = Brushes.White;
                            }
                        }
                        UpdateProgress();
                    };
                    Grid.SetRow(tb, r);
                    Grid.SetColumn(tb, c);
                    sudokuGrid.Children.Add(tb);
                    _cells[r, c] = tb;
                }
            }
        }

        private void ClearCellHighlights()
        {
            for (int r = 0; r < _size; r++)
                for (int c = 0; c < _size; c++)
                {
                    if (!_cells[r, c].IsReadOnly)
                        _cells[r, c].Background = Brushes.White;
                    else
                        _cells[r, c].Background = new SolidColorBrush(Color.FromRgb(248, 248, 248));
                }
        }

        // ═══════════════════════════════════════════════════════════
        //  NUMBER PAD — quick input buttons
        // ═══════════════════════════════════════════════════════════

        private void BuildNumPad()
        {
            if (numPad == null) return;
            numPad.Children.Clear();

            for (int n = 1; n <= _size; n++)
            {
                int captured = n;
                var btn = new Border
                {
                    Width = 44, Height = 44,
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush(Color.FromRgb(248, 187, 208)),
                    Margin = new Thickness(3),
                    Cursor = Cursors.Hand,
                };

                btn.MouseEnter += (s, _) =>
                    ((Border)s).Background = new SolidColorBrush(Color.FromRgb(136, 14, 79));
                btn.MouseLeave += (s, _) =>
                    ((Border)s).Background = new SolidColorBrush(Color.FromRgb(248, 187, 208));

                var tb = new TextBlock
                {
                    Text = n.ToString(),
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 14, 79)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Segoe UI"),
                };

                btn.MouseEnter += (_, _) => tb.Foreground = Brushes.White;
                btn.MouseLeave += (_, _) => tb.Foreground = new SolidColorBrush(Color.FromRgb(136, 14, 79));

                btn.Child = tb;
                btn.MouseLeftButtonDown += (s, e) =>
                {
                    if (_selectedCell != null && !_selectedCell.IsReadOnly)
                    {
                        _selectedCell.Text = captured.ToString();
                        _selectedCell.Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192));
                    }
                };
                numPad.Children.Add(btn);
            }

            // Clear button
            var clearBtn = new Border
            {
                Width = 44, Height = 44,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromRgb(239, 154, 154)),
                Margin = new Thickness(3),
                Cursor = Cursors.Hand,
            };
            var clearTb = new TextBlock
            {
                Text = "✕",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            clearBtn.Child = clearTb;
            clearBtn.MouseLeftButtonDown += (s, e) =>
            {
                if (_selectedCell != null && !_selectedCell.IsReadOnly)
                    _selectedCell.Text = "";
            };
            numPad.Children.Add(clearBtn);
        }

        // ═══════════════════════════════════════════════════════════
        //  HINT & CHECK
        // ═══════════════════════════════════════════════════════════

        private void GiveHint()
        {
            int targetR = -1, targetC = -1;
            if (_selectedCell != null && !_selectedCell.IsReadOnly)
            {
                for (int r = 0; r < _size; r++)
                    for (int c = 0; c < _size; c++)
                        if (_cells[r, c] == _selectedCell)
                        {
                            targetR = r;
                            targetC = c;
                            break;
                        }
            }

            // Nếu ô đang chọn trống hoặc điền sai, điền gợi ý vào ô đó
            if (targetR != -1 && targetC != -1 && 
                (string.IsNullOrWhiteSpace(_cells[targetR, targetC].Text) || 
                 _cells[targetR, targetC].Text.Trim() != _solution[targetR, targetC].ToString()))
            {
                FillHintCell(targetR, targetC);
                return;
            }

            // Tìm ô trống hoặc ô sai đầu tiên từ trên xuống dưới
            for (int r = 0; r < _size; r++)
                for (int c = 0; c < _size; c++)
                    if (!_cells[r, c].IsReadOnly && 
                        (string.IsNullOrWhiteSpace(_cells[r, c].Text) || 
                         _cells[r, c].Text.Trim() != _solution[r, c].ToString()))
                    {
                        FillHintCell(r, c);
                        return;
                    }
        }

        private void FillHintCell(int r, int c)
        {
            _cells[r, c].Text = _solution[r, c].ToString();
            _cells[r, c].Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            _cells[r, c].FontWeight = FontWeights.Bold;
            _cells[r, c].IsReadOnly = true;
            _cells[r, c].Background = new SolidColorBrush(Color.FromArgb(40, 46, 125, 50)); // Nền xanh lá nhạt
            UpdateProgress();
        }

        private void CheckSolution()
        {
            // Anti-brute-force Cooldown
            if ((DateTime.Now - _lastCheckTime).TotalSeconds < 1.5)
            {
                txtStatus.Text = "⚠️ Vui lòng đợi 1.5 giây giữa các lần kiểm tra!";
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0)); // Orange color
                return;
            }
            _lastCheckTime = DateTime.Now;

            int wrongCells = 0;
            int emptyCells = 0;
            int n = _size;
            int boxH = n == 4 ? 2 : n == 6 ? 2 : 3;
            int boxW = n == 4 ? 2 : n == 6 ? 3 : 3;

            // Kiểm tra từng ô để tô màu đỏ ô sai
            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    // Skip checking on readonly given clues (they are always correct)
                    if (_cells[r, c].IsReadOnly && _puzzle[r, c] != 0)
                    {
                        continue;
                    }

                    string text = _cells[r, c].Text.Trim();
                    if (string.IsNullOrEmpty(text))
                    {
                        emptyCells++;
                        // If cell background is currently red or green, reset to white
                        if (_cells[r, c].Background is SolidColorBrush brush && 
                            (brush.Color == Color.FromArgb(60, 198, 40, 40) || brush.Color == Color.FromArgb(40, 46, 125, 50)))
                        {
                            _cells[r, c].Background = Brushes.White;
                        }
                        continue;
                    }

                    if (!int.TryParse(text, out int val) || val < 1 || val > n)
                    {
                        _cells[r, c].Background = new SolidColorBrush(Color.FromArgb(60, 198, 40, 40)); // Red
                        wrongCells++;
                        continue;
                    }

                    // Kiểm tra trùng lắp
                    bool hasDuplicate = false;
                    for (int col = 0; col < n; col++)
                    {
                        if (col != c && _cells[r, col].Text.Trim() == text) { hasDuplicate = true; break; }
                    }
                    if (!hasDuplicate)
                    {
                        for (int row = 0; row < n; row++)
                        {
                            if (row != r && _cells[row, c].Text.Trim() == text) { hasDuplicate = true; break; }
                        }
                    }
                    if (!hasDuplicate)
                    {
                        int sr = r / boxH * boxH;
                        int sc = c / boxW * boxW;
                        for (int row = sr; row < sr + boxH; row++)
                        {
                            for (int col = sc; col < sc + boxW; col++)
                            {
                                if ((row != r || col != c) && _cells[row, col].Text.Trim() == text) { hasDuplicate = true; break; }
                            }
                            if (hasDuplicate) break;
                        }
                    }

                    // Báo lỗi nếu trùng hoặc không đúng với solution đề bài (để định hướng học sinh)
                    if (hasDuplicate || val != _solution[r, c])
                    {
                        _cells[r, c].Background = new SolidColorBrush(Color.FromArgb(60, 198, 40, 40)); // Red
                        wrongCells++;
                    }
                    else
                    {
                        // Ô đúng sẽ giữ màu trắng (không tô xanh lá để tránh brute-force trong quá trình chơi)
                        _cells[r, c].Background = Brushes.White;
                    }
                }
            }

            int duration = (int)(DateTime.Now - _startTime).TotalSeconds;
            if (duration < 1) duration = 1;

            if (wrongCells == 0 && emptyCells == 0)
            {
                // Hoàn thành xuất sắc toàn bộ bảng -> Tô xanh toàn bộ
                for (int r = 0; r < n; r++)
                    for (int c = 0; c < n; c++)
                    {
                        _cells[r, c].Background = _puzzle[r, c] != 0
                            ? new SolidColorBrush(Color.FromRgb(248, 248, 248))
                            : new SolidColorBrush(Color.FromArgb(40, 46, 125, 50)); // Green
                    }

                txtStatus.Text = $"🎉 Chính xác! Bạn đã giải xong Sudoku trong {duration} giây!";
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)); // Green
                SoundHelper.Play(true);
                StartConfetti();

                try
                {
                    string sizeName = _size switch
                    {
                        4 => "4x4 (Dễ)",
                        6 => "6x6 (Vừa)",
                        _ => "9x9 (Khó)"
                    };
                    DbManager.SaveProgress(new UserProgress
                    {
                        GameName = "Sudoku",
                        Score = 1,
                        Total = 1,
                        DurationSeconds = duration,
                        Difficulty = sizeName,
                        CreatedAt = DateTime.Now
                    });
                }
                catch { }
            }
            else if (wrongCells == 0 && emptyCells > 0)
            {
                // Các ô đã điền đều đúng, nhưng chưa xong
                txtStatus.Text = $"💡 Các ô đã điền đều đúng! Còn {emptyCells} ô trống cần điền.";
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)); // Blue
                
                // Phát âm thanh nhẹ nhàng báo hiệu tốt
                try { System.Media.SystemSounds.Asterisk.Play(); } catch { }
            }
            else
            {
                // Có lỗi sai
                txtStatus.Text = $"❌ Có {wrongCells} ô chưa chính xác và còn {emptyCells} ô trống.";
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)); // Red
            }
        }

        private void UpdateProgress()
        {
            int filled = 0, total = _size * _size;
            for (int r = 0; r < _size; r++)
                for (int c = 0; c < _size; c++)
                    if (!string.IsNullOrWhiteSpace(_cells[r, c].Text)) filled++;
            txtProgress.Text = $"📊 {filled}/{total} ô đã điền ({100 * filled / total}%)";
        }

        // ═══════════════════════════════════════════════════════════
        //  PUZZLE GENERATION
        // ═══════════════════════════════════════════════════════════

        private int[,] GenerateSolution(int n)
        {
            var grid = new int[n, n];
            FillGrid(grid, n, 0, 0);
            return grid;
        }

        private bool FillGrid(int[,] grid, int n, int row, int col)
        {
            if (row == n) return true;
            int nextR = col == n - 1 ? row + 1 : row;
            int nextC = col == n - 1 ? 0 : col + 1;

            var nums = Enumerable.Range(1, n).OrderBy(_ => _rng.Next()).ToArray();
            foreach (int num in nums)
            {
                if (IsValid(grid, n, row, col, num))
                {
                    grid[row, col] = num;
                    if (FillGrid(grid, n, nextR, nextC)) return true;
                    grid[row, col] = 0;
                }
            }
            return false;
        }

        private static bool IsValid(int[,] grid, int n, int row, int col, int num)
        {
            for (int i = 0; i < n; i++)
            {
                if (grid[row, i] == num) return false;
                if (grid[i, col] == num) return false;
            }
            int boxH = n == 4 ? 2 : n == 6 ? 2 : 3;
            int boxW = n == 4 ? 2 : n == 6 ? 3 : 3;
            int sr = row / boxH * boxH, sc = col / boxW * boxW;
            for (int r = sr; r < sr + boxH; r++)
                for (int c = sc; c < sc + boxW; c++)
                    if (grid[r, c] == num) return false;
            return true;
        }

        private void RemoveCells(int[,] grid, int n)
        {
            int[,] temp = (int[,])grid.Clone();
            int targetRemove = n switch { 4 => 6, 6 => 14, _ => 45 };
            int removed = 0;

            var cellPositions = new List<(int R, int C)>();
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    cellPositions.Add((r, c));

            // Fisher-Yates shuffle
            for (int i = cellPositions.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                var swap = cellPositions[i];
                cellPositions[i] = cellPositions[j];
                cellPositions[j] = swap;
            }

            foreach (var pos in cellPositions)
            {
                if (removed >= targetRemove) break;

                int r = pos.R;
                int c = pos.C;
                int backup = temp[r, c];

                temp[r, c] = 0;

                if (CountSolutions(temp, n, 2) == 1)
                {
                    grid[r, c] = 0;
                    removed++;
                }
                else
                {
                    temp[r, c] = backup;
                }
            }
        }

        private int CountSolutions(int[,] grid, int n, int limit = 2)
        {
            int count = 0;
            SolveAndCount(grid, n, 0, 0, ref count, limit);
            return count;
        }

        private void SolveAndCount(int[,] grid, int n, int row, int col, ref int count, int limit)
        {
            if (count >= limit) return;
            if (row == n)
            {
                count++;
                return;
            }

            int nextR = (col == n - 1) ? row + 1 : row;
            int nextC = (col == n - 1) ? 0 : col + 1;

            if (grid[row, col] != 0)
            {
                SolveAndCount(grid, n, nextR, nextC, ref count, limit);
            }
            else
            {
                for (int num = 1; num <= n; num++)
                {
                    if (IsValid(grid, n, row, col, num))
                    {
                        grid[row, col] = num;
                        SolveAndCount(grid, n, nextR, nextC, ref count, limit);
                        grid[row, col] = 0;
                        if (count >= limit) return;
                    }
                }
            }
        }

        private void StartConfetti()
        {
            if (confettiCanvas == null) return;

            // Clear any old particles
            StopConfetti();

            // Create particles
            double canvasWidth = confettiCanvas.ActualWidth;
            if (canvasWidth <= 0) canvasWidth = 600; // Fallback
            double canvasHeight = confettiCanvas.ActualHeight;
            if (canvasHeight <= 0) canvasHeight = 600; // Fallback

            for (int i = 0; i < 50; i++)
            {
                var color = _confettiColors[_rng.Next(_confettiColors.Length)];
                var rect = new Rectangle
                {
                    Width = _rng.Next(8, 15),
                    Height = _rng.Next(6, 12),
                    Fill = new SolidColorBrush(color),
                    RenderTransformOrigin = new Point(0.5, 0.5)
                };

                // Add a rotate transform to allow rotation
                rect.RenderTransform = new RotateTransform(0);

                var particle = new ConfettiParticle
                {
                    Shape = rect,
                    X = _rng.NextDouble() * canvasWidth,
                    Y = -_rng.Next(10, 100), // Start above the canvas
                    SpeedY = _rng.NextDouble() * 3 + 2, // Fall speed
                    SpeedX = (_rng.NextDouble() * 2 - 1) * 1.5, // Drift left/right
                    Angle = _rng.Next(0, 360),
                    RotationSpeed = (_rng.NextDouble() * 2 - 1) * 5
                };

                _particles.Add(particle);
                confettiCanvas.Children.Add(rect);
                Canvas.SetLeft(rect, particle.X);
                Canvas.SetTop(rect, particle.Y);
            }

            // Start timer
            _confettiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
            int tickCount = 0;
            _confettiTimer.Tick += (s, e) =>
            {
                tickCount++;
                // Update particles
                double width = confettiCanvas.ActualWidth;
                double height = confettiCanvas.ActualHeight;
                if (width <= 0) width = 600;
                if (height <= 0) height = 600;

                bool hasVisibleParticles = false;

                for (int i = _particles.Count - 1; i >= 0; i--)
                {
                    var p = _particles[i];
                    p.Y += p.SpeedY;
                    p.X += p.SpeedX;
                    p.Angle += p.RotationSpeed;

                    // Apply rotation
                    if (p.Shape.RenderTransform is RotateTransform rt)
                    {
                        rt.Angle = p.Angle;
                    }

                    // Check if particle went off-screen
                    if (p.Y > height || p.X < -20 || p.X > width + 20)
                    {
                        confettiCanvas.Children.Remove(p.Shape);
                        _particles.RemoveAt(i);
                    }
                    else
                    {
                        Canvas.SetLeft(p.Shape, p.X);
                        Canvas.SetTop(p.Shape, p.Y);
                        hasVisibleParticles = true;
                    }
                }

                // Auto stop after 3 seconds (150 ticks of 20ms) or when all particles are gone
                if (tickCount > 150 || !hasVisibleParticles)
                {
                    StopConfetti();
                }
            };
            _confettiTimer.Start();
        }

        private void StopConfetti()
        {
            _confettiTimer?.Stop();
            _confettiTimer = null;
            if (confettiCanvas != null)
            {
                confettiCanvas.Children.Clear();
            }
            _particles.Clear();
        }

        private void LoadPracticalApps()
        {
            try
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                string suffix = isVN ? "VN" : "EN";

                var items = new List<PracticalAppItem>
                {
new PracticalAppItem
                    {
                        Icon = "🔐",
                        Title = isVN ? "Mật mã học & Bảo mật thông tin" : "Logical Reasoning Skills",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_sudoku_1_{suffix}.png",
                        Description = isVN 
                            ? "Các cấu trúc ma trận Latin và Sudoku được sử dụng để thiết kế các thuật toán mã hóa mật mã khóa công khai nâng cao, tạo chuỗi số giả ngẫu nhiên và phân phối khóa dữ liệu bảo mật." 
                            : "Train deductive reasoning and analytical thinking by using elimination processes to fill in Sudoku numbers."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📅",
                        Title = isVN ? "Lập lịch & Phân bổ tài nguyên" : "Pattern Recognition Training",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_sudoku_2_{suffix}.png",
                        Description = isVN 
                            ? "Giải bài toán sắp xếp thời khóa biểu cho giáo viên, phân chia phòng thi học kỳ và xếp lịch bay của phi công hàng không, loại bỏ các xung đột về tài nguyên và thời gian chéo." 
                            : "Enhance visual pattern matching by scanning rows, columns, and 3x3 grids to spot missing numbers."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧬",
                        Title = isVN ? "Giải trình tự DNA & Tin sinh học" : "Patience & Perseverance",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_sudoku_3_{suffix}.png",
                        Description = isVN 
                            ? "Tối ưu thiết kế các mẫu thử nghiệm sinh học, gộp mẫu xét nghiệm DNA diện rộng dựa trên các ràng buộc ô Sudoku để giảm số lần xét nghiệm thực tế mà vẫn định danh gen chính xác." 
                            : "Develop persistence, focus, and problem-solving patience by completing difficult 9x9 Sudoku puzzles."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📡",
                        Title = isVN ? "Mã sửa sai & Phân phát sóng" : "Cognitive Decline Prevention",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_sudoku_4_{suffix}.png",
                        Description = isVN 
                            ? "Thiết kế mã sửa sai (Error-Correcting Codes) trong truyền dẫn kỹ thuật số và quy hoạch vị trí, kênh tần số trong truyền thông không dây để tối ưu băng thông và tránh nhiễu chéo." 
                            : "Stimulate mental agility and delay memory decline in seniors by solving number placement puzzles regularly."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌾",
                        Title = isVN ? "Thiết kế thực nghiệm nông nghiệp" : "Algorithm Design Practice",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_sudoku_5_{suffix}.png",
                        Description = isVN 
                            ? "Phân chia sơ đồ các ô đất trồng thử nghiệm nông nghiệp. Bố trí hạt giống theo ma trận Latin giúp loại bỏ các yếu tố nhiễu do độ màu mỡ của đất và hướng chiếu mặt trời không đều." 
                            : "Practice writing backtracking search and constraint satisfaction code to build automated Sudoku solver programs."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🤖",
                        Title = isVN ? "Trí tuệ nhân tạo & Giải thuật CSP" : "Relaxing Brain Break",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_sudoku_6_{suffix}.png",
                        Description = isVN 
                            ? "Rèn luyện và đánh giá hiệu năng giải thuật Thỏa mãn ràng buộc (CSP) của AI. Sudoku được coi là mô hình thử nghiệm lý tưởng cho các giải thuật duyệt đồ thị và suy luận tự động." 
                            : "Provide an engaging yet calming intellectual hobby that diverts focus from daily worries and reduces stress."
                    }
            };

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for SudokuTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewWorkspace == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewWorkspace.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewWorkspace.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}