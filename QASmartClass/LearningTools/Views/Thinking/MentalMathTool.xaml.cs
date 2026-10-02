using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using QASmartClass.LearningTools.Models;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Ink;
using System.IO;
using System.Windows.Media.Imaging;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Helpers;

namespace QASmartClass.LearningTools.Views.Thinking
{
    public partial class MentalMathTool : BaseToolControl, IWhiteboardCaptureProvider
    {
        public System.Threading.Tasks.Task<System.Windows.Media.Imaging.BitmapSource?> GetWhiteboardBitmapAsync()
        {
            var rtb = TeachingActionHelper.RenderVisualUnclipped(spQuestionArea, 2.0);
            return System.Threading.Tasks.Task.FromResult<System.Windows.Media.Imaging.BitmapSource?>(rtb);
        }

        private readonly Random _rng = new();
        private int _score, _total, _streak, _maxStreak;
        private double _correctAnswer;
        private bool _active;
        private DateTime _startTime;
        private DispatcherTimer? _timer;
        private DispatcherTimer? _feedbackTimer;
        private SelectionChangedEventHandler? _selectionChangedHandler;
        private bool _soundEnabled = true;
        private readonly Stack<StrokeCollection> _scratchUndoStack = new();
        private readonly Stack<StrokeCollection> _scratchRedoStack = new();
        private bool _isScratchUndoRedoing;
        private DateTime _lastSubmitTime = DateTime.MinValue;
        private string _currentHint = "";

        private const int TotalQuestions = 30;

        public MentalMathTool()
        {
            InitializeComponent();
            PreviewKeyDown += Tool_PreviewKeyDown;
            DbManager.Initialize();
            scratchCanvas.Strokes.StrokesChanged += ScratchCanvas_StrokesChanged;
            _scratchUndoStack.Push(scratchCanvas.Strokes.Clone());
            // Attach TouchNumPad
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtAnswer, step: 1, allowDecimal: true, placement: System.Windows.Controls.Primitives.PlacementMode.Right);

            // Đăng ký bộ lọc sự kiện dán dữ liệu vào TextBox đáp án
            DataObject.AddPastingHandler(txtAnswer, OnPasteAnswer);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _timer.Tick += (s, e) =>
            {
                var elapsed = DateTime.Now - _startTime;
                if (txtTimer != null) txtTimer.Text = $"⏱️ {elapsed:mm\\:ss}";
            };

            Loaded += (_, _) =>
            {
                QASmartClass.Shared.LanguageManager.LanguageChanged -= OnLanguageChanged;
                QASmartClass.Shared.LanguageManager.LanguageChanged += OnLanguageChanged;

                // Hủy đăng ký cũ nếu có để tránh rò rỉ bộ nhớ
                if (_selectionChangedHandler != null)
                {
                    cboLevel.SelectionChanged -= _selectionChangedHandler;
                }

                var settings = GameSettingsManager.Load();
                cboLevel.SelectedIndex = settings.MentalMathLevel;
                _soundEnabled = settings.IsSoundEnabled;
                btnSoundToggle.Content = _soundEnabled ? "🔊" : "🔇";

                var scratchInkAttr = new DrawingAttributes
                {
                    Color = Color.FromRgb(46, 125, 50), // Nice green ink to match theme
                    Width = 3,
                    Height = 3,
                    FitToCurve = true,
                    StylusTip = StylusTip.Ellipse
                };
                scratchCanvas.DefaultDrawingAttributes = scratchInkAttr;

                LocalizeStaticUI();

                // Đồng bộ hiển thị tên cấp độ ban đầu
                UpdateScoreDisplay();
                LoadLeaderboard();

                // Đăng ký sự kiện thay đổi cấp độ sau khi đã nạp xong giá trị cấu hình ban đầu
                _selectionChangedHandler = (s, args) =>
                {
                    var currentSettings = GameSettingsManager.Load();
                    currentSettings.MentalMathLevel = cboLevel.SelectedIndex;
                    GameSettingsManager.Save(currentSettings);
                    UpdateScoreDisplay();
                };
                cboLevel.SelectionChanged += _selectionChangedHandler;
                LoadPracticalApps();
            };

            Unloaded += (_, _) =>
            {
                QASmartClass.Shared.LanguageManager.LanguageChanged -= OnLanguageChanged;
                _timer?.Stop();
                _feedbackTimer?.Stop();
                DataObject.RemovePastingHandler(txtAnswer, OnPasteAnswer);
                if (_selectionChangedHandler != null)
                {
                    cboLevel.SelectionChanged -= _selectionChangedHandler;
                }
            };
        }

        // ═══ RESET ═══
        private void ResetAll_Click(object sender, RoutedEventArgs e)
        {
            _active = false;
            _timer?.Stop();
            _feedbackTimer?.Stop();
            _score = 0; _total = 0; _streak = 0; _maxStreak = 0;
            if (txtMathExpr != null) SetMathExprText("");
            if (txtQuestion != null) txtQuestion.Text = GetLocText("Nhấn 'Bắt đầu' để chơi", "Press 'Start' to play");
            if (txtMedal != null) { txtMedal.Text = ""; txtMedal.Visibility = Visibility.Collapsed; }
            if (txtAnswer != null) { txtAnswer.Text = ""; txtAnswer.Visibility = Visibility.Collapsed; txtAnswer.IsEnabled = true; }
            if (txtFeedback != null) txtFeedback.Text = "";
            if (txtScore != null) txtScore.Text = GetLocText("Điểm: 0", "Score: 0");
            if (txtStreak != null) txtStreak.Text = "🔥 0";
            if (txtTimer != null) txtTimer.Text = "⏱️ 00:00";
            if (brdInstructions != null) brdInstructions.Visibility = Visibility.Visible;
            if (brdLeaderboard != null) brdLeaderboard.Visibility = Visibility.Visible;
            LoadLeaderboard();
            if (btnStart != null) btnStart.Content = GetLocText("▶ Bắt đầu (30 câu)", "▶ Start (30 Qs)");
            if (cboLevel != null) { cboLevel.IsEnabled = true; }
            UpdateScoreDisplay();
        }

        // ═══ SOUND TOGGLE ═══
        private void SoundToggle_Click(object sender, RoutedEventArgs e)
        {
            _soundEnabled = !_soundEnabled;
            if (btnSoundToggle != null)
            {
                btnSoundToggle.Content = _soundEnabled ? "🔊" : "🔇";
            }
            var settings = GameSettingsManager.Load();
            settings.IsSoundEnabled = _soundEnabled;
            GameSettingsManager.Save(settings);
        }

        // ═══════════════════════════════════════════════════════════
        //  GAME CONTROL
        // ═══════════════════════════════════════════════════════════

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 1)
            {
                sideMenu.SelectedIndex = 0;
            }
            _score = 0;
            _total = 0;
            _streak = 0;
            _maxStreak = 0;
            _active = true;
            _startTime = DateTime.Now;

            _feedbackTimer?.Stop(); // Triệt tiêu timer chuyển câu cũ (tránh lỗi Race Condition)

            if (txtMedal != null) { txtMedal.Text = ""; txtMedal.Visibility = Visibility.Collapsed; }

            txtAnswer.Visibility = Visibility.Visible;
            txtAnswer.IsEnabled = true;
            btnStart.Content = GetLocText("🔄 Chơi lại", "🔄 Restart");
            if (cboLevel != null) cboLevel.IsEnabled = false;
            if (brdInstructions != null) brdInstructions.Visibility = Visibility.Collapsed;
            if (brdLeaderboard != null) brdLeaderboard.Visibility = Visibility.Collapsed;

            _timer?.Stop();
            _startTime = DateTime.Now;
            _timer?.Start();

            GenerateQuestion();
        }

        private void GenerateQuestion()
        {
            if (_total >= TotalQuestions)
            {
                FinishGame();
                return;
            }

            int level = cboLevel?.SelectedIndex ?? 0;
            int a, b;
            string op;
            int patternType = -1;

            switch (level)
            {
                case 0: // Dễ (Cộng trừ trong PHẠM VI 20)
                    op = _rng.Next(2) == 0 ? "+" : "-";
                    if (op == "+")
                    {
                        a = _rng.Next(1, 20);
                        b = _rng.Next(1, 21 - a); // Tổng <= 20
                    }
                    else
                    {
                        a = _rng.Next(1, 21);
                        b = _rng.Next(1, a + 1); // Hiệu >= 0
                    }
                    break;

                case 1: // Vừa (Cộng trừ phạm vi 100, nhân chia bảng cửu chương chuẩn)
                {
                    int opType = _rng.Next(4);
                    switch (opType)
                    {
                        case 0:
                            a = _rng.Next(10, 90);
                            b = _rng.Next(10, 101 - a); // Tổng <= 100
                            op = "+";
                            break;
                        case 1:
                            a = _rng.Next(20, 100);
                            b = _rng.Next(10, a);
                            op = "-";
                            break;
                        case 2:
                            a = _rng.Next(2, 10);
                            b = _rng.Next(2, 10); // Nhân cửu chương 2-9
                            op = "×";
                            break;
                        default:
                            b = _rng.Next(2, 10);
                            a = b * _rng.Next(2, 10); // Chia cửu chương 2-9
                            op = "÷";
                            break;
                    }
                    break;
                }

                case 2: // Khó (Cộng trừ phạm vi 1000, nhân chia nhẩm với số 1 chữ số)
                {
                    int opType = _rng.Next(4);
                    switch (opType)
                    {
                        case 0:
                            a = _rng.Next(100, 900);
                            b = _rng.Next(100, 1001 - a); // Tổng <= 1000
                            op = "+";
                            break;
                        case 1:
                            a = _rng.Next(200, 1000);
                            b = _rng.Next(100, a);
                            op = "-";
                            break;
                        case 2:
                            a = _rng.Next(10, 100);
                            b = _rng.Next(2, 10); // Nhân nhẩm số có 2 chữ số với số có 1 chữ số
                            op = "×";
                            break;
                        default:
                            b = _rng.Next(2, 10);
                            a = b * _rng.Next(10, 100); // Chia nhẩm có số chia 1 chữ số
                            op = "÷";
                            break;
                    }
                    break;
                }

                default: // Chuyên gia (Các mẹo tính nhẩm nhanh đặc trưng)
                {
                    patternType = _rng.Next(5);
                    switch (patternType)
                    {
                        case 0: // Mẹo nhân 11: (số 2 chữ số) × 11
                            a = _rng.Next(10, 100);
                            b = 11;
                            op = "×";
                            break;
                        case 1: // Mẹo nhân 5: (số chẵn 2-3 chữ số) × 5
                            a = _rng.Next(10, 150) * 2;
                            b = 5;
                            op = "×";
                            break;
                        case 2: // Mẹo bình phương số tận cùng là 5 (15, 25, ... 95)
                            a = _rng.Next(1, 10) * 10 + 5;
                            b = 2;
                            op = "²";
                            break;
                        case 3: // Bình phương số học thuộc lòng từ 11 đến 30
                            a = _rng.Next(11, 31);
                            b = 2;
                            op = "²";
                            break;
                        default: // Cộng nhanh làm tròn (Số tròn chục/trăm lớn)
                            a = _rng.Next(100, 1000);
                            b = _rng.Next(10, 100) * 10;
                            op = "+";
                            break;
                    }
                    break;
                }
            }

            _correctAnswer = op switch
            {
                "+" => a + b,
                "-" => a - b,
                "×" => a * b,
                "÷" => (double)a / b,
                "²" => a * a,
                _ => 0
            };

            string display = op == "²" ? $"{a}² = ?" : $"{a} {op} {b} = ?";
            SetMathExprText(display);

            // Cập nhật gợi ý thời gian thực cho cấp Chuyên gia
            if (level == 3)
            {
                _currentHint = patternType switch
                {
                    0 => GetLocText(
                        $"» Mẹo nhân 11: Cộng hai chữ số {a} ({a / 10} + {a % 10} = {a / 10 + a % 10}) rồi viết kết quả vào giữa.",
                        $"» Multiply by 11: Add the digits of {a} ({a / 10} + {a % 10} = {a / 10 + a % 10}) and write in between."
                    ),
                    1 => GetLocText(
                        $"» Mẹo nhân 5: Chia đôi số chẵn {a} ({a} ÷ 2 = {a / 2}) rồi nhân 10 (thêm số 0 vào sau).",
                        $"» Multiply by 5: Halve the even number {a} ({a} ÷ 2 = {a / 2}) then multiply by 10."
                    ),
                    2 => GetLocText(
                        $"» Mẹo bình phương đuôi 5: Lấy số đầu {a / 10} nhân số liền sau ({a / 10 + 1}) được {a / 10 * (a / 10 + 1)}, rồi viết thêm 25 vào sau.",
                        $"» Square numbers ending in 5: Multiply first digit {a / 10} by its consecutive {a / 10 + 1} to get {a / 10 * (a / 10 + 1)}, then append 25."
                    ),
                    3 => GetLocText(
                        $"» Bình phương thuộc lòng: {a}² là số chính phương cần nhớ phản xạ nhanh.",
                        $"» Recall square: {a}² is a perfect square. Quick recall."
                    ),
                    _ => GetLocText(
                        $"» Cộng nhanh làm tròn: Cộng phần trăm/chục trước hoặc làm tròn số.",
                        $"» Quick addition: Round numbers first to add quickly."
                    )
                };
                if (btnHint != null)
                {
                    btnHint.Visibility = Visibility.Visible;
                    if (txtHintContent != null) txtHintContent.Text = _currentHint;
                }
            }
            else
            {
                _currentHint = "";
                if (btnHint != null) btnHint.Visibility = Visibility.Collapsed;
            }

            txtQuestion.Text = GetLocText($"Câu {_total + 1}/{TotalQuestions}", $"Question {_total + 1}/{TotalQuestions}");
            txtAnswer.Text = "";
            txtFeedback.Text = "";
            txtAnswer.Focus();
        }

        private void Answer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || !_active) return;

            string rawInput = txtAnswer.Text.Trim();
            if (string.IsNullOrEmpty(rawInput) || rawInput == "-" || rawInput == "." || rawInput == ",")
            {
                txtFeedback.Text = GetLocText("⚠️ Vui lòng nhập số!", "⚠️ Please enter a number!");
                txtFeedback.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                return;
            }

            if (double.TryParse(rawInput.Replace(',', '.'), 
                System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out double answer))
            {
                _active = false;
                txtAnswer.IsEnabled = false;
                QASmartClass.LearningTools.Controls.TouchNumPad.HidePopup();
                _total++;
                bool isCorrect = System.Math.Abs(answer - _correctAnswer) < 0.0001;
                
                if (_soundEnabled)
                {
                    SoundHelper.Play(isCorrect);
                }

                if (isCorrect)
                {
                    _score++;
                    _streak++;
                    if (_streak > _maxStreak) _maxStreak = _streak;
                    txtFeedback.Text = GetLocText("✅ Đúng!", "✅ Correct!");
                    txtFeedback.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                }
                else
                {
                    _streak = 0;
                    txtFeedback.Text = GetLocText($"❌ Sai! Đáp án: {_correctAnswer}", $"❌ Incorrect! Answer: {_correctAnswer}");
                    txtFeedback.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                }

                UpdateScoreDisplay();

                // Auto next
                _feedbackTimer?.Stop();
                _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
                _feedbackTimer.Tick += (_, _) =>
                {
                    _feedbackTimer.Stop();
                    _active = true;
                    txtAnswer.IsEnabled = true;
                    GenerateQuestion();
                };
                _feedbackTimer.Start();
            }
            else
            {
                txtFeedback.Text = GetLocText("⚠️ Lỗi định dạng số!", "⚠️ Invalid number format!");
                txtFeedback.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
            }
        }

        private void FinishGame()
        {
            _active = false;
            _timer?.Stop();
            _feedbackTimer?.Stop();
            var elapsed = DateTime.Now - _startTime;

            txtAnswer.Visibility = Visibility.Collapsed;
            SetMathExprText($"🎉 {_score}/{TotalQuestions}");
            txtQuestion.Text = GetLocText("Hoàn thành!", "Completed!");
            if (cboLevel != null) cboLevel.IsEnabled = true;
            if (brdInstructions != null) brdInstructions.Visibility = Visibility.Collapsed;
            if (brdLeaderboard != null) brdLeaderboard.Visibility = Visibility.Visible;

            double avgTime = elapsed.TotalSeconds / TotalQuestions;
            string grade = _score >= 28 ? GetLocText("⭐ Xuất sắc", "⭐ Excellent") :
                           _score >= 24 ? GetLocText("🏆 Giỏi", "🏆 Great") :
                           _score >= 20 ? GetLocText("👍 Khá", "👍 Good") :
                           _score >= 15 ? GetLocText("📚 Trung bình", "📚 Average") : GetLocText("💪 Cần luyện thêm", "💪 Needs practice");

            if (txtMedal != null)
            {
                txtMedal.Visibility = Visibility.Visible;
                if (_score >= 28)
                {
                    txtMedal.Text = "🏆";
                }
                else if (_score >= 24)
                {
                    txtMedal.Text = "🥇";
                }
                else if (_score >= 20)
                {
                    txtMedal.Text = "🥈";
                }
                else if (_score >= 15)
                {
                    txtMedal.Text = "📚";
                }
                else
                {
                    txtMedal.Text = "💪";
                }
            }

            txtFeedback.Text = GetLocText(
                $"{grade} • Thời gian: {elapsed:mm\\:ss} • TB: {avgTime:F1}s/câu • Streak max: {_maxStreak}🔥",
                $"{grade} • Time: {elapsed:mm\\:ss} • Avg: {avgTime:F1}s/q • Max streak: {_maxStreak}🔥"
            );
            txtFeedback.Foreground = new SolidColorBrush(Color.FromRgb(40, 53, 147));

            btnStart.Content = GetLocText("▶ Chơi lại", "▶ Restart");

            try
            {
                string levelName = (cboLevel?.SelectedIndex ?? 0) switch
                {
                    0 => "Dễ",
                    1 => "Vừa",
                    2 => "Khó",
                    _ => "Chuyên gia"
                };
                var progress = new UserProgress
                {
                    GameName = "Tính nhẩm nhanh",
                    Score = _score,
                    Total = TotalQuestions,
                    DurationSeconds = elapsed.TotalSeconds,
                    Difficulty = levelName,
                    CreatedAt = DateTime.Now
                };
                DbManager.SaveProgress(progress);
                _ = CloudSyncService.SyncProgressAsync(progress); // Gọi không đồng bộ lên CloudSyncService
                LoadLeaderboard();
            }
            catch { }
        }

        private void UpdateScoreDisplay()
        {
            if (txtScore == null || txtStreak == null || txtLevel == null) return;

            txtScore.Text = GetLocText($"Điểm: {_score}/{_total}", $"Score: {_score}/{_total}");

            // Thiết lập màu sắc và hậu tố văn bản dựa trên chuỗi thắng liên tục (Streak)
            if (_streak >= 10)
            {
                txtStreak.Text = GetLocText($"🔥 {_streak} (Siêu cấp! 🔥🔥🔥)", $"🔥 {_streak} (Super! 🔥🔥🔥)");
                txtStreak.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Màu đỏ rực (#F44336)
            }
            else if (_streak >= 5)
            {
                txtStreak.Text = GetLocText($"🔥 {_streak} (Nóng bỏng! 🔥)", $"🔥 {_streak} (Hot! 🔥)");
                txtStreak.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Màu cam sáng (#FF9800)
            }
            else
            {
                txtStreak.Text = $"🔥 {_streak}";
                var brandAccent = Application.Current.TryFindResource("BrandAccent") as Brush;
                if (brandAccent != null)
                {
                    txtStreak.Foreground = brandAccent;
                }
                else
                {
                    txtStreak.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)); // Fallback màu xanh lá
                }
            }

            string levelNameLoc = (cboLevel?.SelectedIndex ?? 0) switch
            {
                0 => GetLocText("Dễ", "Easy"),
                1 => GetLocText("Vừa", "Medium"),
                2 => GetLocText("Khó", "Hard"),
                _ => GetLocText("Chuyên gia", "Expert")
            };
            txtLevel.Text = GetLocText($"Level: {levelNameLoc}", $"Level: {levelNameLoc}");
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var textBox = (TextBox)sender;
            var proposedText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
                                          .Insert(textBox.SelectionStart, e.Text);

            // Cho phép các trạng thái trung gian của số thực (có thể âm)
            // Ví dụ: "-", ".", "-.", "0.5", "-0.5", "12.34"
            var isAllowed = System.Text.RegularExpressions.Regex.IsMatch(proposedText, @"^-?[0-9]*[.,]?[0-9]*$");
            e.Handled = !isAllowed;
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Chặn phím khoảng trắng (Spacebar)
            if (e.Key == Key.Space)
            {
                e.Handled = true;
                return;
            }

            // Hỗ trợ phím chấm thập phân từ bàn phím số (Numpad Decimal key) và bàn phím thường (OemPeriod hoặc OemComma)
            if (e.Key == Key.Decimal || e.Key == Key.OemPeriod || e.Key == Key.OemComma)
            {
                var textBox = (TextBox)sender;
                string decSep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                
                int start = textBox.SelectionStart;
                int length = textBox.SelectionLength;
                
                // Xem văn bản sau khi đã xóa phần đang được bôi đen (nếu có)
                string textWithoutSelection = textBox.Text.Remove(start, length);
                
                // Chỉ cho phép chứa duy nhất một ký tự phân tách thập phân
                if (!textWithoutSelection.Contains(".") && !textWithoutSelection.Contains(","))
                {
                    // Thực hiện thay thế đoạn text đang chọn bằng ký tự phân tách thập phân
                    textBox.Text = textWithoutSelection.Insert(start, decSep);
                    textBox.SelectionStart = start + decSep.Length;
                }
                
                e.Handled = true; // Đánh dấu đã xử lý để tránh chèn ký tự mặc định bị lỗi
            }
        }

        private void OnPasteAnswer(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                string text = (string)e.DataObject.GetData(DataFormats.Text);
                // Đảm bảo dữ liệu dán vào là số thực hợp lệ
                if (string.IsNullOrEmpty(text) || !double.TryParse(text.Trim().Replace(',', '.'), 
                    System.Globalization.NumberStyles.Any, 
                    System.Globalization.CultureInfo.InvariantCulture, out _))
                {
                    e.CancelCommand(); // Hủy lệnh dán
                }
            }
            else
            {
                e.CancelCommand(); // Hủy nếu dữ liệu không phải text
            }
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var list = DbManager.GetAllProgress("Tính nhẩm nhanh");
                if (list == null || list.Count == 0)
                {
                    MessageBox.Show(GetLocText("Chưa có dữ liệu lịch sử luyện tập nào để xuất!", "No practice history data to export!"), GetLocText("Thông báo", "Notification"), MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = GetLocText("Tệp CSV (*.csv)|*.csv", "CSV Files (*.csv)|*.csv"),
                    FileName = GetLocText("LichSu_TinhNhamNhanh.csv", "MentalMath_History.csv"),
                    Title = GetLocText("Chọn nơi lưu file lịch sử luyện tập", "Select destination for practice history file")
                };

                if (sfd.ShowDialog() == true)
                {
                    // Sử dụng UTF-8 BOM (\uFEFF) giúp Excel tự động nhận diện chữ tiếng Việt có dấu khi mở file
                    using (var writer = new System.IO.StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8))
                    {
                        writer.Write('\uFEFF'); // Byte Order Mark
                        
                        // Ghi tiêu đề cột
                        if (QASmartClass.Shared.LanguageManager.CurrentLanguage == "en")
                        {
                            writer.WriteLine("No,Time,Correct Answers,Total Questions,Accuracy (%),Completion Time (seconds),Difficulty");
                        }
                        else
                        {
                            writer.WriteLine("STT,Thời gian,Số câu đúng,Tổng số câu,Tỷ lệ chính xác (%),Thời gian hoàn thành (giây),Cấp độ");
                        }
                        
                        int index = 1;
                        foreach (var p in list)
                        {
                            double rate = p.Total > 0 ? (double)p.Score / p.Total * 100 : 0;
                            string rateStr = rate.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                            string durationStr = p.DurationSeconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                            
                            string diffLoc = p.Difficulty;
                            if (QASmartClass.Shared.LanguageManager.CurrentLanguage == "en")
                            {
                                if (p.Difficulty == "Dễ") diffLoc = "Easy";
                                else if (p.Difficulty == "Vừa") diffLoc = "Medium";
                                else if (p.Difficulty == "Khó") diffLoc = "Hard";
                                else if (p.Difficulty == "Chuyên gia") diffLoc = "Expert";
                            }
                            
                            writer.WriteLine($"{index},{p.CreatedAt:yyyy-MM-dd HH:mm:ss},{p.Score},{p.Total},{rateStr},{durationStr},{diffLoc}");
                            index++;
                        }
                    }

                    MessageBox.Show(GetLocText("Xuất lịch sử luyện tập thành công!", "Exported practice history successfully!"), GetLocText("Thành công", "Success"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(GetLocText($"Lỗi trong quá trình xuất tệp: {ex.Message}", $"Error exporting file: {ex.Message}"), GetLocText("Lỗi", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadLeaderboard()
        {
            try
            {
                var list = DbManager.GetTopProgress("Tính nhẩm nhanh", 5);
                
                if (list == null || list.Count == 0)
                {
                    if (txtEmptyLeaderboard != null) txtEmptyLeaderboard.Visibility = Visibility.Visible;
                    if (gridLeaderboard != null) gridLeaderboard.Visibility = Visibility.Collapsed;
                    return;
                }

                if (txtEmptyLeaderboard != null) txtEmptyLeaderboard.Visibility = Visibility.Collapsed;
                if (gridLeaderboard != null) gridLeaderboard.Visibility = Visibility.Visible;

                // Xóa dữ liệu cũ
                for (int r = 1; r <= 5; r++)
                {
                    var lblScore = (TextBlock)FindName($"lblScore{r}");
                    var lblTime = (TextBlock)FindName($"lblTime{r}");
                    var lblLevel = (TextBlock)FindName($"lblLevel{r}");
                    var lblDate = (TextBlock)FindName($"lblDate{r}");
                    var lblRank = (TextBlock)FindName($"lblRank{r}");

                    if (lblScore != null) lblScore.Text = "--";
                    if (lblTime != null) lblTime.Text = "--";
                    if (lblLevel != null) lblLevel.Text = "--";
                    if (lblDate != null) lblDate.Text = "--";
                    if (lblRank != null) lblRank.Visibility = Visibility.Visible;
                }

                // Điền dữ liệu
                for (int i = 0; i < list.Count; i++)
                {
                    int rankIdx = i + 1;
                    var p = list[i];

                    var lblScore = (TextBlock)FindName($"lblScore{rankIdx}");
                    var lblTime = (TextBlock)FindName($"lblTime{rankIdx}");
                    var lblLevel = (TextBlock)FindName($"lblLevel{rankIdx}");
                    var lblDate = (TextBlock)FindName($"lblDate{rankIdx}");
                    var lblRank = (TextBlock)FindName($"lblRank{rankIdx}");

                    if (lblRank != null) lblRank.Visibility = Visibility.Visible;
                    if (lblScore != null) lblScore.Text = $"{p.Score}/{p.Total}";
                    if (lblTime != null) lblTime.Text = $"{p.DurationSeconds:F1}s";
                    
                    if (lblLevel != null) 
                    {
                        string diffLoc = p.Difficulty;
                        if (QASmartClass.Shared.LanguageManager.CurrentLanguage == "en")
                        {
                            if (p.Difficulty == "Dễ") diffLoc = "Easy";
                            else if (p.Difficulty == "Vừa") diffLoc = "Medium";
                            else if (p.Difficulty == "Khó") diffLoc = "Hard";
                            else if (p.Difficulty == "Chuyên gia") diffLoc = "Expert";
                        }
                        lblLevel.Text = diffLoc;
                    }
                    
                    if (lblDate != null) lblDate.Text = p.CreatedAt.ToString("dd/MM HH:mm");
                }
            }
            catch
            {
                // Tránh đổ bể nếu SQLite lỗi
            }
        }

        private void BtnToggleScratchPad_Click(object sender, RoutedEventArgs e)
        {
            if (brdScratchPad.Visibility == Visibility.Visible)
            {
                brdScratchPad.Visibility = Visibility.Collapsed;
            }
            else
            {
                brdScratchPad.Visibility = Visibility.Visible;
            }
        }

        private void ScratchMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rad && rad.Tag is string mode)
            {
                if (mode == "Pen")
                {
                    scratchCanvas.EditingMode = InkCanvasEditingMode.Ink;
                }
                else if (mode == "Eraser")
                {
                    scratchCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                }
            }
        }

        private void ScratchCanvas_StrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            if (_isScratchUndoRedoing) return;
            _scratchUndoStack.Push(scratchCanvas.Strokes.Clone());
            _scratchRedoStack.Clear();
        }

        private void SaveScratchUndoState()
        {
            if (_isScratchUndoRedoing) return;
            _scratchUndoStack.Push(scratchCanvas.Strokes.Clone());
            _scratchRedoStack.Clear();
        }

        private void PerformScratchUndo()
        {
            if (_scratchUndoStack.Count > 1)
            {
                _isScratchUndoRedoing = true;
                var current = _scratchUndoStack.Pop();
                _scratchRedoStack.Push(current);
                var prev = _scratchUndoStack.Peek();
                scratchCanvas.Strokes = prev.Clone();
                _isScratchUndoRedoing = false;
            }
        }

        private void PerformScratchRedo()
        {
            if (_scratchRedoStack.Count > 0)
            {
                _isScratchUndoRedoing = true;
                var next = _scratchRedoStack.Pop();
                _scratchUndoStack.Push(next);
                scratchCanvas.Strokes = next.Clone();
                _isScratchUndoRedoing = false;
            }
        }

        private void BtnClearScratch_Click(object sender, RoutedEventArgs e)
        {
            if (scratchCanvas.Strokes.Count > 0)
            {
                if (QASmartClass.LearningTools.Views.Multi.NotebookTool.MessageBoxShowHandler(
                    GetLocText("Bạn có chắc chắn muốn xóa toàn bộ nét vẽ nháp?", "Are you sure you want to clear the entire scratchpad?"),
                    GetLocText("Xóa nháp", "Clear Scratchpad"),
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    SaveScratchUndoState();
                    scratchCanvas.Strokes.Clear();
                }
            }
        }

        private async void BtnSubmitScratch_Click(object sender, RoutedEventArgs e)
        {
            if (scratchCanvas == null || scratchCanvas.Strokes.Count == 0)
            {
                MessageBox.Show(GetLocText("Bảng nháp hiện tại đang trống. Vui lòng vẽ nháp trước khi nộp!", "The scratchpad is empty. Please write/draw before submitting!"), GetLocText("Thông báo", "Notification"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if ((DateTime.Now - _lastSubmitTime).TotalSeconds < 3)
            {
                MessageBox.Show(GetLocText("Vui lòng đợi 3 giây giữa mỗi lần nộp nháp để tránh nghẽn mạng!", "Please wait 3 seconds between scratchpad submissions to prevent network congestion!"), GetLocText("Thông báo", "Notification"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                btnSubmitScratch.Content = GetLocText("⏳ Đang nộp...", "⏳ Submitting...");
                btnSubmitScratch.IsEnabled = false;

                double width = scratchCanvas.ActualWidth;
                double height = scratchCanvas.ActualHeight;
                if (width <= 0 || height <= 0)
                {
                    width = 350;
                    height = 500;
                }

                RenderTargetBitmap rtb = new RenderTargetBitmap(
                    (int)width,
                    (int)height,
                    96, 96, PixelFormats.Pbgra32);
                
                DrawingVisual dv = new DrawingVisual();
                using (DrawingContext dc = dv.RenderOpen())
                {
                    dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(250, 248, 245)), null, new Rect(0, 0, width, height));
                }
                rtb.Render(dv);
                rtb.Render(scratchCanvas);

                var encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = 75;
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                byte[] imgBytes;
                using (var ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    imgBytes = ms.ToArray();
                }

                string base64 = Convert.ToBase64String(imgBytes);

                MessageBox.Show(GetLocText("Chức năng nộp bài đã bị vô hiệu hóa.", "Submit feature disabled."), GetLocText("Cảnh báo", "Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(GetLocText($"Lỗi khi gửi nháp: {ex.Message}", $"Error submitting scratchpad: {ex.Message}"), GetLocText("Lỗi", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnSubmitScratch.Content = GetLocText("📤 Nộp nháp", "📤 Submit");
                btnSubmitScratch.IsEnabled = true;
            }
        }

        private void Tool_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2 || (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
            {
                e.Handled = true;
                BtnToggleScratchPad_Click(null, null);
            }
            else if (brdScratchPad.Visibility == Visibility.Visible)
            {
                if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    e.Handled = true;
                    PerformScratchUndo();
                }
                else if (e.Key == Key.Y && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    e.Handled = true;
                    PerformScratchRedo();
                }
            }
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
                        Icon = "🛒",
                        Title = isVN ? "Mua sắm & Tính Tiền thối" : "Shopping & Quick Bill Splitting",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_mental_math_1_{suffix}.png",
                        Description = isVN
                            ? "Tính nhanh tổng giá tiền của các món đồ, nhẩm số tiền thừa thối lại hoặc chia đều hóa đơn cho nhóm bạn một cách chính xác."
                            : "Quickly calculate the total price of items, estimate change to receive, or divide a bill evenly among a group of friends."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📊",
                        Title = isVN ? "Ước lượng Doanh thu & Tỷ lệ %" : "Business Estimation & Percentages",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_mental_math_2_{suffix}.png",
                        Description = isVN
                            ? "Tính toán nhanh tỷ lệ phần trăm tăng trưởng, doanh số dự kiến hoặc tỷ lệ chiết khấu giảm giá trong các cuộc họp kinh doanh."
                            : "Rapidly calculate growth percentages, projected sales, or discount rates during business meetings."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📐",
                        Title = isVN ? "Đo đạc & Tính toán Công trình" : "Engineering Estimation & Project Planning",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_mental_math_3_{suffix}.png",
                        Description = isVN
                            ? "Giúp kỹ sư và nhà quản lý ước lượng nhanh số lượng vật tư xây dựng, nhân công cần thiết hoặc phân bổ thời gian tiến độ."
                            : "Helps engineers and project managers estimate material quantities, required labor, or allocate scheduling on site."
                    }
                };

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for MentalMathTool: {Err}", ex.Message);
            }
        }

        private string GetLocText(string viText, string enText)
        {
            return QASmartClass.Shared.LanguageManager.CurrentLanguage == "en" ? enText : viText;
        }

        private void OnLanguageChanged(string langCode)
        {
            LocalizeStaticUI();
            if (!_active)
            {
                if (txtQuestion != null) txtQuestion.Text = GetLocText("Nhấn 'Bắt đầu' để chơi", "Press 'Start' to play");
            }
        }

        private void LocalizeStaticUI()
        {
            if (txtTitle != null) txtTitle.Text = GetLocText("⚡ Tính Nhẩm Nhanh", "⚡ Mental Math");
            if (txtSubtitle != null) txtSubtitle.Text = GetLocText("Mọi cấp độ • Luyện tính nhẩm theo level • Thi tốc độ", "All levels • Practice by level • Speed test");
            if (menuTextGuide != null) menuTextGuide.Text = GetLocText("📖 Hướng dẫn & Ghi chú", "📖 Guide & Notes");
            if (menuTextPractice != null) menuTextPractice.Text = GetLocText("⚡ Luyện tính nhẩm", "⚡ Practice");
            if (menuTextPractical != null) menuTextPractical.Text = GetLocText("🌍 Ứng dụng thực tế", "🌍 Real-world Applications");
            
            if (btnExport != null) btnExport.Content = GetLocText("📥 Xuất CSV", "📥 Export CSV");
            if (btnReset != null) btnReset.Content = GetLocText("🔄 Đặt lại", "🔄 Reset");
            if (btnToggleScratchPad != null) btnToggleScratchPad.Content = GetLocText("📝 Bảng nháp", "📝 Scratchpad");
            if (lblLevel != null) lblLevel.Text = GetLocText("Cấp độ:", "Difficulty:");

            // ComboBox Items translation
            if (cboLevel != null)
            {
                int levelIdx = cboLevel.SelectedIndex;
                cboLevelItemEasy.Content = GetLocText("Dễ (1-20)", "Easy (1-20)");
                cboLevelItemMedium.Content = GetLocText("Vừa (1-100)", "Medium (1-100)");
                cboLevelItemHard.Content = GetLocText("Khó (Nhẩm)", "Hard (Mental)");
                cboLevelItemExpert.Content = GetLocText("Chuyên gia", "Expert");
                cboLevel.SelectedIndex = levelIdx;
            }

            // Leaderboard Headers
            if (lblLeaderboardHeader != null) lblLeaderboardHeader.Text = GetLocText("🏆 BẢNG VÀNG THÀNH TÍCH (TOP 5)", "🏆 PERSONAL RECORDS (TOP 5)");
            if (txtEmptyLeaderboard != null) txtEmptyLeaderboard.Text = GetLocText("Chưa có thành tích nào được ghi nhận. Hãy chơi để chinh phục bảng xếp hạng!", "No records recorded yet. Play to conquer the leaderboard!");
            if (lblLeaderboardRank != null) lblLeaderboardRank.Text = GetLocText("Hạng", "Rank");
            if (lblLeaderboardScore != null) lblLeaderboardScore.Text = GetLocText("Điểm số", "Score");
            if (lblLeaderboardTime != null) lblLeaderboardTime.Text = GetLocText("Thời gian", "Duration");
            if (lblLeaderboardLevel != null) lblLeaderboardLevel.Text = GetLocText("Cấp độ", "Difficulty");
            if (lblLeaderboardDate != null) lblLeaderboardDate.Text = GetLocText("Ngày đạt", "Date Achieved");

            // Scratchpad Controls
            if (lblScratchTitle != null) lblScratchTitle.Text = GetLocText("📝 Bảng nháp", "📝 Scratchpad");
            if (radScratchPen != null) radScratchPen.Content = GetLocText("🖊️ Bút", "🖊️ Pen");
            if (radScratchEraser != null) radScratchEraser.Content = GetLocText("🧽 Tẩy", "🧽 Eraser");
            if (btnSubmitScratch != null && btnSubmitScratch.Content is string submitStr && (submitStr == "📤 Nộp nháp" || submitStr == "📤 Submit")) btnSubmitScratch.Content = GetLocText("📤 Nộp nháp", "📤 Submit");
            if (btnClearScratch != null) btnClearScratch.Content = GetLocText("🗑️ Xóa nháp", "🗑️ Clear");

            // Instructions translation
            if (lblInstrTitle != null) lblInstrTitle.Text = GetLocText("💡 HƯỚNG DẪN & MẸO TÍNH NHẨM NHANH", "💡 INSTRUCTIONS & MENTAL MATH TIPS");
            if (lblInstrEasy != null) lblInstrEasy.Text = GetLocText("• Cấp Dễ: Cộng trừ các số trong phạm vi 20.", "• Easy: Addition & Subtraction within 20.");
            if (lblInstrMedium != null) lblInstrMedium.Text = GetLocText("• Cấp Vừa: Cộng trừ phạm vi 100, nhân chia bảng cửu chương.", "• Medium: Addition & Subtraction within 100, standard multiplication & division tables.");
            if (lblInstrHard != null) lblInstrHard.Text = GetLocText("• Cấp Khó: Cộng trừ phạm vi 1000, nhân chia nhẩm với số có 1 chữ số.", "• Hard: Addition & Subtraction within 1000, mental single-digit multiplication & division.");
            if (lblInstrExpert != null) lblInstrExpert.Text = GetLocText("• Cấp Chuyên gia: Áp dụng các mẹo toán học đặc biệt để nhẩm:", "• Expert: Apply special mathematical shortcuts to calculate:");
            if (lblInstrExpert1 != null) lblInstrExpert1.Text = GetLocText("  » Nhân với 11: Cộng hai chữ số rồi viết ở giữa (VD: 35 × 11 = 3[8]5 = 385).", "  » Multiply by 11: Add the digits and write in between (e.g. 35 × 11 = 3[8]5 = 385).");
            if (lblInstrExpert2 != null) lblInstrExpert2.Text = GetLocText("  » Nhân với 5: Chia đôi số chẵn rồi nhân 10 (VD: 84 × 5 = 42 × 10 = 420).", "  » Multiply by 5: Halve the even number then multiply by 10 (e.g. 84 × 5 = 42 × 10 = 420).");
            if (lblInstrExpert3 != null) lblInstrExpert3.Text = GetLocText("  » Bình phương đuôi 5: Lấy số đầu nhân số liền sau rồi ghép 25 (VD: 35² = [3×4]25 = 1225).", "  » Square numbers ending in 5: Multiply first digit by its consecutive digit, then append 25 (e.g. 35² = [3×4]25 = 1225).");
            if (lblInstrExpert4 != null) lblInstrExpert4.Text = GetLocText("  » Bình phương từ 11 đến 30: Phản xạ nhớ nhanh các số chính phương.", "  » Square numbers from 11 to 30: Quick recall of perfect squares.");

            if (!_active)
            {
                if (btnStart != null) btnStart.Content = GetLocText("▶ Bắt đầu (30 câu)", "▶ Start (30 Qs)");
            }
            else
            {
                if (btnStart != null) btnStart.Content = GetLocText("🔄 Chơi lại", "🔄 Restart");
            }
            
            LoadPracticalApps();
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

        private void SetMathExprText(string text)
        {
            if (txtMathExpr == null) return;
            txtMathExpr.Inlines.Clear();
            if (text.Contains("²"))
            {
                int idx = text.IndexOf('²');
                string baseNum = text.Substring(0, idx);
                txtMathExpr.Inlines.Add(new Run(baseNum));
                
                var superRun = new Run("²")
                {
                    FontSize = 44,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0)), // Cam đậm (#E65100)
                    BaselineAlignment = BaselineAlignment.Superscript
                };
                txtMathExpr.Inlines.Add(superRun);
                
                string rest = text.Substring(idx + 1);
                txtMathExpr.Inlines.Add(new Run(rest));
            }
            else
            {
                txtMathExpr.Inlines.Add(new Run(text));
            }
        }

        private void BtnHint_Click(object sender, RoutedEventArgs e)
        {
            if (ttHint != null)
            {
                ttHint.IsOpen = !ttHint.IsOpen;
            }
        }
    }
}