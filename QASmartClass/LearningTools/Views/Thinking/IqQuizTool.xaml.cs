using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Ink;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Helpers;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Thinking
{
    public partial class IqQuizTool : BaseToolControl, IWhiteboardCaptureProvider
    {
        public System.Threading.Tasks.Task<System.Windows.Media.Imaging.BitmapSource?> GetWhiteboardBitmapAsync()
        {
            var rtb = TeachingActionHelper.RenderVisualUnclipped(spIqQuestionArea, 2.0);
            return System.Threading.Tasks.Task.FromResult<System.Windows.Media.Imaging.BitmapSource?>(rtb);
        }
        private readonly Random _rng = new();
        private int _idx, _correct, _total;
        private bool _active;
        private List<IqQ> _questions = new();
        private int _consecutiveCorrect;
        private int _consecutiveWrong;
        private readonly HashSet<string> _historyQuestions = new();
        private DateTime _startTime;
        private bool _isEndless;
        private int _lives;
        private readonly List<int> _levelHistory = new();
        private MediaPlayer? _bgmPlayer;
        private DispatcherTimer? _answerDelayTimer;
        private readonly Stack<StrokeCollection> _scratchUndoStack = new();
        private readonly Stack<StrokeCollection> _scratchRedoStack = new();
        private bool _isScratchUndoRedoing;
        private DateTime _lastSubmitTime = DateTime.MinValue;

        public IqQuizTool()
        {
            InitializeComponent();
            PreviewKeyDown += Tool_PreviewKeyDown;
            Unloaded += Tool_Unloaded;
            DbManager.Initialize();
            scratchCanvas.Strokes.StrokesChanged += ScratchCanvas_StrokesChanged;
            _scratchUndoStack.Push(scratchCanvas.Strokes.Clone());

            // Handle tab selection change in Hub to manage BGM volume
            this.DataContextChanged += (s, args) =>
            {
                // When active view changes, if this is not the active control, lower BGM or stop it
            };

            Loaded += (_, _) =>
            {
                QASmartClass.Shared.LanguageManager.LanguageChanged -= OnLanguageChanged;
                QASmartClass.Shared.LanguageManager.LanguageChanged += OnLanguageChanged;

                var settings = GameSettingsManager.Load();
                
                LocalizeStaticUI();
                cboIqLevel.SelectedIndex = settings.IqLevel;

                if (cvsIqChart != null)
                {
                    cvsIqChart.SizeChanged += (s, args) =>
                    {
                        if (!_active && spIqEndGame != null && spIqEndGame.Visibility == Visibility.Visible)
                        {
                            DrawAdaptiveChart();
                        }
                    };
                }

                TouchTextPad.Attach(txtIqAnswer, mode: "text");

                var scratchInkAttr = new DrawingAttributes
                {
                    Color = Color.FromRgb(33, 150, 243),
                    Width = 3,
                    Height = 3,
                    FitToCurve = true,
                    StylusTip = StylusTip.Ellipse
                };
                scratchCanvas.DefaultDrawingAttributes = scratchInkAttr;

                InitBgm(settings.IsSoundEnabled);
            };
        }

        private void OnLanguageChanged(string langCode)
        {
            LocalizeStaticUI();
            if (!_active)
            {
                if (txtIqQuestion != null)
                {
                    txtIqQuestion.Text = GetLocText("Nhấn 'Bắt đầu' để chơi", "Press 'Start' to play");
                }
            }
        }

        private void LocalizeStaticUI()
        {
            if (lblIqMode != null) lblIqMode.Text = GetLocText("Chế độ:", "Mode:");
            if (lblIqLevel != null) lblIqLevel.Text = GetLocText("Độ khó:", "Difficulty:");
            if (txtIqTitle != null) txtIqTitle.Text = GetLocText("🧠 Luyện IQ & Logic", "🧠 IQ & Logic Practice");
            if (txtIqSubtitle != null) txtIqSubtitle.Text = GetLocText("Dãy số • Suy luận tương tự • Logic • Phân loại", "Number Series • Analogy • Logic • Classification");
            if (menuTextGuide != null) menuTextGuide.Text = GetLocText("📖 Hướng dẫn & Quy trình", "📖 Guide & Process");
            if (menuTextPractice != null) menuTextPractice.Text = GetLocText("🧠 Luyện tập IQ", "🧠 IQ Practice");
            if (menuTextPractical != null) menuTextPractical.Text = GetLocText("🌍 Ứng dụng thực tế", "🌍 Real-world Applications");
            
            if (lblIqLives != null) lblIqLives.Text = GetLocText("Mạng: ", "Lives: ");
            if (btnToggleScratchPad != null) btnToggleScratchPad.Content = GetLocText("📝 Bảng nháp", "📝 Scratchpad");
            if (lblScratchTitle != null) lblScratchTitle.Text = GetLocText("📝 Bảng nháp", "📝 Scratchpad");
            
            // Scratchpad controls
            if (radScratchPen != null) radScratchPen.Content = GetLocText("🖊️ Bút", "🖊️ Pen");
            if (radScratchEraser != null) radScratchEraser.Content = GetLocText("🧽 Tẩy", "🧽 Eraser");
            if (btnSubmitScratch != null) btnSubmitScratch.Content = GetLocText("📤 Nộp nháp", "📤 Submit Sketch");
            if (btnClearScratch != null) btnClearScratch.Content = GetLocText("🗑️ Xóa nháp", "🗑️ Clear Sketch");
            
            if (btnColorBlack != null) btnColorBlack.ToolTip = GetLocText("Màu Đen", "Black");
            if (btnColorBlue != null) btnColorBlue.ToolTip = GetLocText("Màu Xanh Dương", "Blue");
            if (btnColorRed != null) btnColorRed.ToolTip = GetLocText("Màu Đỏ", "Red");
            if (btnColorGreen != null) btnColorGreen.ToolTip = GetLocText("Màu Xanh Lá", "Green");
            
            if (txtIqLeaderboardHeader != null) txtIqLeaderboardHeader.Text = GetLocText("🏆 Kỷ Lục Cá Nhân (Top 5)", "🏆 Personal Records (Top 5)");
            if (btnIqExportPdf != null) btnIqExportPdf.Content = GetLocText("📄 Xuất báo cáo PDF", "📄 Export PDF Report");
            if (txtIqSyncStatus != null) txtIqSyncStatus.Text = GetLocText("☁️ Trực tuyến", "☁️ Online");

            LocalizeGridViewHeaders();
            PopulateComboBoxItems();
            LoadPracticalApps();

            if (spIqEndGame != null && spIqEndGame.Visibility == Visibility.Visible)
            {
                LoadTopScores();
                DrawAdaptiveChart();
            }
        }

        private void LocalizeGridViewHeaders()
        {
            if (lsvIqLeaderboard == null) return;
            if (lsvIqLeaderboard.View is GridView gridView)
            {
                if (gridView.Columns.Count >= 4)
                {
                    gridView.Columns[0].Header = GetLocText("Độ khó", "Difficulty");
                    gridView.Columns[1].Header = GetLocText("Điểm", "Score");
                    gridView.Columns[2].Header = GetLocText("Thời gian", "Duration");
                    gridView.Columns[3].Header = GetLocText("Ngày chơi", "Date Played");
                }
            }
        }

        private void PopulateComboBoxItems()
        {
            if (cboIqLevel == null || cboIqMode == null) return;

            // Save selected indexes
            int levelIdx = cboIqLevel.SelectedIndex;
            if (levelIdx < 0)
            {
                var settings = GameSettingsManager.Load();
                levelIdx = settings.IqLevel;
            }
            int modeIdx = cboIqMode.SelectedIndex;
            if (modeIdx < 0) modeIdx = 0;

            // Temporarily unhook selection changed events to avoid triggering updates while modifying items
            cboIqLevel.SelectionChanged -= Level_SelectionChanged;
            cboIqMode.SelectionChanged -= Mode_SelectionChanged;

            cboIqLevel.Items.Clear();
            var levels = QASmartClass.Shared.LanguageManager.CurrentLanguage == "en"
                ? new[] { "🟢 Easy", "🟡 Medium", "🔴 Hard", "⚫ Expert" }
                : new[] { "🟢 Dễ", "🟡 Trung bình", "🔴 Khó", "⚫ Chuyên gia" };
            foreach (var l in levels) cboIqLevel.Items.Add(new ComboBoxItem { Content = l });
            cboIqLevel.SelectedIndex = levelIdx;

            cboIqMode.Items.Clear();
            var modes = QASmartClass.Shared.LanguageManager.CurrentLanguage == "en"
                ? new[] { "15 questions", "Endless" }
                : new[] { "15 câu", "Vô hạn" };
            foreach (var m in modes) cboIqMode.Items.Add(new ComboBoxItem { Content = m });
            cboIqMode.SelectedIndex = modeIdx;

            cboIqLevel.SelectionChanged += Level_SelectionChanged;
            cboIqMode.SelectionChanged += Mode_SelectionChanged;

            UpdateCurrentLevelLabel();
            UpdateStartButtonText();
        }

        private void Level_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboIqLevel.SelectedIndex < 0) return;
            var currentSettings = GameSettingsManager.Load();
            currentSettings.IqLevel = cboIqLevel.SelectedIndex;
            GameSettingsManager.Save(currentSettings);
            UpdateCurrentLevelLabel();
        }

        private void Mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_active)
            {
                UpdateStartButtonText();
                if (spIqEndGame != null && spIqEndGame.Visibility == Visibility.Visible)
                {
                    LoadTopScores();
                }
            }
        }

        private void UpdateStartButtonText()
        {
            if (btnIqStart == null) return;
            if (_active)
            {
                btnIqStart.Content = GetLocText("🔄 Chơi lại", "🔄 Restart");
            }
            else
            {
                if (cboIqMode != null && cboIqMode.SelectedIndex == 1)
                {
                    btnIqStart.Content = GetLocText("▶ Bắt đầu (Vô hạn)", "▶ Start (Endless)");
                }
                else
                {
                    btnIqStart.Content = GetLocText("▶ Bắt đầu (15 câu)", "▶ Start (15 Qs)");
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            CleanUpResources();
        }

        private void Tool_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanUpResources();
        }

        private void CleanUpResources()
        {
            try
            {
                QASmartClass.Shared.LanguageManager.LanguageChanged -= OnLanguageChanged;
            }
            catch { }

            try
            {
                if (_bgmPlayer != null)
                {
                    _bgmPlayer.Stop();
                    _bgmPlayer.Close();
                    _bgmPlayer = null;
                }
            }
            catch { }

            try
            {
                if (_answerDelayTimer != null)
                {
                    _answerDelayTimer.Stop();
                    _answerDelayTimer = null;
                }
            }
            catch { }
        }

        private void InitBgm(bool isSoundEnabled)
        {
            try
            {
                if (btnIqSound != null)
                {
                    btnIqSound.Content = isSoundEnabled ? "🔊" : "🔇";
                }
                
                if (_bgmPlayer == null)
                {
                    _bgmPlayer = new MediaPlayer();
                    string bgmPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio", "bgm.mp3");
                    if (System.IO.File.Exists(bgmPath))
                    {
                        _bgmPlayer.Open(new Uri(bgmPath));
                    }
                    else
                    {
                        string fallback = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media", "chimes.wav");
                        if (System.IO.File.Exists(fallback))
                        {
                            _bgmPlayer.Open(new Uri(fallback));
                        }
                    }
                    _bgmPlayer.MediaEnded += (s, e) =>
                    {
                        try
                        {
                            if (_bgmPlayer != null)
                            {
                                _bgmPlayer.Position = TimeSpan.Zero;
                                _bgmPlayer.Play();
                            }
                        }
                        catch { }
                    };
                }

                if (isSoundEnabled)
                {
                    if (_active)
                    {
                        _bgmPlayer.Volume = 0.5;
                        _bgmPlayer.Play();
                    }
                }
                else
                {
                    _bgmPlayer.Pause();
                }
            }
            catch
            {
                // Safety first
            }
        }

        private void IqSound_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = GameSettingsManager.Load();
                settings.IsSoundEnabled = !settings.IsSoundEnabled;
                GameSettingsManager.Save(settings);
                
                if (btnIqSound != null)
                {
                    btnIqSound.Content = settings.IsSoundEnabled ? "🔊" : "🔇";
                }

                if (_bgmPlayer != null)
                {
                    if (settings.IsSoundEnabled)
                    {
                        if (_active)
                        {
                            _bgmPlayer.Volume = 0.5;
                            _bgmPlayer.Play();
                        }
                    }
                    else
                    {
                        _bgmPlayer.Pause();
                    }
                }
            }
            catch
            {
                // Safety first
            }
        }

        private void UpdateCurrentLevelLabel()
        {
            if (txtIqCurrentLevel == null) return;
            int idx = cboIqLevel?.SelectedIndex ?? 0;
            switch (idx)
            {
                case 0:
                    txtIqCurrentLevel.Text = GetLocText("🟢 Cấp độ: Dễ", "🟢 Level: Easy");
                    txtIqCurrentLevel.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    break;
                case 1:
                    txtIqCurrentLevel.Text = GetLocText("🟡 Cấp độ: Trung bình", "🟡 Level: Medium");
                    txtIqCurrentLevel.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                    break;
                case 2:
                    txtIqCurrentLevel.Text = GetLocText("🔴 Cấp độ: Khó", "🔴 Level: Hard");
                    txtIqCurrentLevel.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    break;
                default:
                    txtIqCurrentLevel.Text = GetLocText("⚫ Cấp độ: Chuyên gia 🔥", "⚫ Level: Expert 🔥");
                    txtIqCurrentLevel.Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33));
                    break;
            }
        }

        private void ResetAll_Click(object sender, MouseButtonEventArgs e)
        {
            if (_answerDelayTimer != null)
            {
                _answerDelayTimer.Stop();
                _answerDelayTimer = null;
            }
            if (cboIqMode != null) cboIqMode.IsEnabled = true;
            if (cboIqLevel != null) cboIqLevel.IsEnabled = true;

            _active = false; _idx = 0; _correct = 0; _total = 0;
            _consecutiveCorrect = 0;
            _consecutiveWrong = 0;
            _historyQuestions.Clear();
            _questions.Clear();
            _levelHistory.Clear();
            if (txtIqSequence != null) txtIqSequence.Text = "";
            if (txtIqQuestion != null) txtIqQuestion.Text = GetLocText("Nhấn 'Bắt đầu' để chơi", "Press 'Start' to play");
            if (txtIqAnswer != null) { txtIqAnswer.Text = ""; txtIqAnswer.Visibility = Visibility.Collapsed; }
            if (grdIqNumpad != null) grdIqNumpad.Visibility = Visibility.Collapsed;
            if (mcIqPanel != null) mcIqPanel.Visibility = Visibility.Collapsed;
            if (txtIqFeedback != null) txtIqFeedback.Text = "";
            if (txtIqScore != null) txtIqScore.Text = GetLocText("Điểm: 0/0", "Score: 0/0");
            if (txtIqProgress != null) txtIqProgress.Text = "";
            if (spIqLives != null) spIqLives.Visibility = Visibility.Collapsed;
            if (spIqEndGame != null) spIqEndGame.Visibility = Visibility.Collapsed;
            if (cvsIqChart != null) cvsIqChart.Children.Clear();
            if (lsvIqLeaderboard != null) lsvIqLeaderboard.ItemsSource = null;
            UpdateStartButtonText();
            UpdateCurrentLevelLabel();
        }

        private void IqStart_Click(object sender, RoutedEventArgs e)
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 1)
            {
                sideMenu.SelectedIndex = 0;
            }
            if (_answerDelayTimer != null)
            {
                _answerDelayTimer.Stop();
                _answerDelayTimer = null;
            }
            if (cboIqMode != null) cboIqMode.IsEnabled = false;
            if (cboIqLevel != null) cboIqLevel.IsEnabled = false;

            _correct = 0; _total = 0; _idx = 0; _active = true;
            _consecutiveCorrect = 0;
            _consecutiveWrong = 0;
            _historyQuestions.Clear();
            _questions.Clear();
            _levelHistory.Clear();
            _startTime = DateTime.Now;
            UpdateCurrentLevelLabel();
            
            _isEndless = cboIqMode?.SelectedIndex == 1;
            if (_isEndless)
            {
                _lives = 3;
                if (spIqLives != null) spIqLives.Visibility = Visibility.Visible;
                UpdateLivesDisplay();
            }
            else
            {
                if (spIqLives != null) spIqLives.Visibility = Visibility.Collapsed;
            }

            if (spIqEndGame != null) spIqEndGame.Visibility = Visibility.Collapsed;
            if (btnIqExportPdf != null) btnIqExportPdf.Visibility = Visibility.Collapsed;
            if (cvsIqChart != null) cvsIqChart.Children.Clear();
            if (lsvIqLeaderboard != null) lsvIqLeaderboard.ItemsSource = null;
            
            int level = cboIqLevel?.SelectedIndex ?? 0;
            _questions.Add(GenerateSingleQuestion(level));

            btnIqStart.Content = "🔄 Chơi lại";
            
            try
            {
                var settings = GameSettingsManager.Load();
                if (settings.IsSoundEnabled && _bgmPlayer != null)
                {
                    _bgmPlayer.Volume = 0.5;
                    _bgmPlayer.Position = TimeSpan.Zero;
                    _bgmPlayer.Play();
                }
            }
            catch { }

            ShowIqQ();
        }

        private void UpdateLivesDisplay()
        {
            if (txtIqLives == null) return;
            _lives = System.Math.Max(0, System.Math.Min(3, _lives));
            txtIqLives.Text = string.Concat(Enumerable.Repeat("❤️", _lives)) + string.Concat(Enumerable.Repeat("🖤", 3 - _lives));
        }

        private void ShowIqQ()
        {
            bool isGameOver = _isEndless ? (_lives <= 0) : (_idx >= 15);
            if (isGameOver)
            {
                _active = false;
                if (cboIqMode != null) cboIqMode.IsEnabled = true;
                if (cboIqLevel != null) cboIqLevel.IsEnabled = true;
                
                try
                {
                    if (_bgmPlayer != null)
                    {
                        _bgmPlayer.Pause();
                    }
                }
                catch { }

                if (_isEndless)
                {
                    txtIqSequence.Text = GetLocText($"🎉 Hoàn thành! Đúng: {_correct} câu", $"🎉 Finished! Correct: {_correct} Qs");
                    txtIqQuestion.Text = GetLocText("Kết thúc!", "Game Over!");
                    txtIqFeedback.Text = _correct >= 30 ? GetLocText("🔥 Kỷ lục vô song!", "🔥 Ultimate Record!") :
                                         _correct >= 20 ? GetLocText("⭐ Xuất sắc!", "⭐ Excellent!") :
                                         _correct >= 10 ? GetLocText("🏆 Giỏi!", "🏆 Great!") :
                                                          GetLocText("💪 Hãy cố gắng hơn!", "💪 Keep Trying!");
                }
                else
                {
                    txtIqSequence.Text = GetLocText($"🎉 {_correct}/15!", $"🎉 {_correct}/15!");
                    txtIqQuestion.Text = GetLocText("Hoàn thành!", "Completed!");
                    txtIqFeedback.Text = _correct >= 13 ? GetLocText("⭐ Xuất sắc!", "⭐ Excellent!") :
                                         _correct >= 10 ? GetLocText("🏆 Giỏi!", "🏆 Great!") :
                                         _correct >= 7 ? GetLocText("👍 Khá!", "👍 Good!") :
                                                         GetLocText("💪 Cần luyện thêm!", "💪 Needs Practice!");
                }
                
                txtIqAnswer.Visibility = Visibility.Collapsed;
                if (grdIqNumpad != null) grdIqNumpad.Visibility = Visibility.Collapsed;
                mcIqPanel.Visibility = Visibility.Collapsed;
                txtIqFeedback.Foreground = new SolidColorBrush(Color.FromRgb(173, 20, 87));
                
                SaveGameProgressToDb();
                DrawAdaptiveChart();
                LoadTopScores();
                if (spIqEndGame != null)
                {
                    spIqEndGame.Visibility = Visibility.Visible;
                }
                if (btnIqExportPdf != null)
                {
                    btnIqExportPdf.Visibility = Visibility.Visible;
                }
                return;
            }
            
            if (_idx >= _questions.Count)
            {
                int level = cboIqLevel?.SelectedIndex ?? 0;
                _questions.Add(GenerateSingleQuestion(level));
            }
            
            var q = _questions[_idx];
            txtIqQuestion.Text = q.Question;
            txtIqSequence.Text = q.Display;
            txtIqFeedback.Text = "";
            txtIqProgress.Text = _isEndless ? GetLocText($"Câu {_idx + 1}", $"Question {_idx + 1}") : GetLocText($"Câu {_idx + 1}/15", $"Question {_idx + 1}/15");

            // Record level history for this question index
            int currentLevel = cboIqLevel?.SelectedIndex ?? 0;
            while (_levelHistory.Count <= _idx)
            {
                _levelHistory.Add(currentLevel);
            }

            if (q.Options != null && q.Options.Length > 0)
            {
                txtIqAnswer.Visibility = Visibility.Collapsed;
                if (grdIqNumpad != null) grdIqNumpad.Visibility = Visibility.Collapsed;
                mcIqPanel.Visibility = Visibility.Visible;
                mcIqPanel.IsEnabled = true;
                mcIqPanel.Children.Clear();
                foreach (var opt in q.Options)
                {
                    var btn = new Button
                    {
                        Content = opt, FontSize = 18, FontWeight = FontWeights.SemiBold,
                        Padding = new Thickness(24, 12, 24, 12),
                        Margin = new Thickness(0, 4, 0, 4), MinWidth = 340,
                        Background = new SolidColorBrush(Color.FromRgb(252, 228, 236)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(244, 143, 177)),
                        BorderThickness = new Thickness(1.5), Cursor = Cursors.Hand,
                        HorizontalContentAlignment = HorizontalAlignment.Left
                    };
                    var cap = opt;
                    btn.Click += (_, _) => CheckMcAnswer(cap, q.Answer);
                    mcIqPanel.Children.Add(btn);
                }
            }
            else
            {
                txtIqAnswer.Visibility = Visibility.Visible;
                txtIqAnswer.IsEnabled = true;
                if (grdIqNumpad != null)
                {
                    grdIqNumpad.Visibility = Visibility.Visible;
                    grdIqNumpad.IsEnabled = true;
                }
                mcIqPanel.Visibility = Visibility.Collapsed;
                txtIqAnswer.Text = "";
                txtIqAnswer.Focus();
            }
        }

        private void CheckMcAnswer(string sel, string correct)
        {
            if (!_active) return;
            _active = false;
            mcIqPanel.IsEnabled = false;
            _total++;
            bool isCorrect = (sel == correct);
            SoundHelper.Play(isCorrect);
            if (isCorrect)
            {
                _correct++;
                _consecutiveCorrect++;
                _consecutiveWrong = 0;
                txtIqFeedback.Text = GetLocText("✅ Chính xác!", "✅ Correct!");
                txtIqFeedback.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));

                if (_isEndless && _correct > 0 && _correct % 10 == 0)
                {
                    StartConfetti();
                }

                if (_consecutiveCorrect >= 3)
                {
                    int currentLevel = cboIqLevel?.SelectedIndex ?? 0;
                    if (currentLevel < 3)
                    {
                        cboIqLevel.SelectedIndex = currentLevel + 1;
                        txtIqFeedback.Text += " " + GetLocText("📈 Tăng độ khó!", "📈 Increase difficulty!");
                        UpdateCurrentLevelLabel();
                        ShowToastNotification("📈", GetLocText("Tăng độ khó!", "Increase difficulty!"), true);
                    }
                    _consecutiveCorrect = 0;
                }
            }
            else
            {
                _consecutiveWrong++;
                _consecutiveCorrect = 0;
                txtIqFeedback.Text = GetLocText($"❌ Đáp án: {correct}", $"❌ Answer: {correct}");
                txtIqFeedback.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                if (_isEndless)
                {
                    _lives--;
                    UpdateLivesDisplay();
                    ShakeHearts();
                }
                if (_consecutiveWrong >= 2)
                {
                    int currentLevel = cboIqLevel?.SelectedIndex ?? 0;
                    if (currentLevel > 0)
                    {
                        cboIqLevel.SelectedIndex = currentLevel - 1;
                        txtIqFeedback.Text += " " + GetLocText("📉 Giảm độ khó!", "📉 Decrease difficulty!");
                        UpdateCurrentLevelLabel();
                        ShowToastNotification("📉", GetLocText("Giảm độ khó!", "Decrease difficulty!"), false);
                    }
                    _consecutiveWrong = 0;
                }
            }
            txtIqScore.Text = GetLocText($"Điểm: {_correct}/{_total}", $"Score: {_correct}/{_total}"); _idx++;
            if (_answerDelayTimer != null)
            {
                _answerDelayTimer.Stop();
            }
            _answerDelayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
            _answerDelayTimer.Tick += (_, _) =>
            {
                if (_answerDelayTimer != null)
                {
                    _answerDelayTimer.Stop();
                    _answerDelayTimer = null;
                }
                _active = true;
                ShowIqQ();
            };
            _answerDelayTimer.Start();
        }

        private void IqAnswer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || !_active) return;
            ProcessTextAnswer(txtIqAnswer.Text);
        }

        private void ProcessTextAnswer(string answerText)
        {
            if (!_active) return;
            _active = false;
            txtIqAnswer.IsEnabled = false;
            if (grdIqNumpad != null) grdIqNumpad.IsEnabled = false;

            var q = _questions[_idx];
            string ans = answerText.Trim();
            _total++;
            bool isCorrect = ans.Equals(q.Answer, StringComparison.OrdinalIgnoreCase);
            SoundHelper.Play(isCorrect);
            if (isCorrect)
            {
                _correct++;
                _consecutiveCorrect++;
                _consecutiveWrong = 0;
                txtIqFeedback.Text = GetLocText("✅ Chính xác!", "✅ Correct!");
                txtIqFeedback.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));

                if (_isEndless && _correct > 0 && _correct % 10 == 0)
                {
                    StartConfetti();
                }

                if (_consecutiveCorrect >= 3)
                {
                    int currentLevel = cboIqLevel?.SelectedIndex ?? 0;
                    if (currentLevel < 3)
                    {
                        cboIqLevel.SelectedIndex = currentLevel + 1;
                        txtIqFeedback.Text += " " + GetLocText("📈 Tăng độ khó!", "📈 Increase difficulty!");
                        UpdateCurrentLevelLabel();
                        ShowToastNotification("📈", GetLocText("Tăng độ khó!", "Increase difficulty!"), true);
                    }
                    _consecutiveCorrect = 0;
                }
            }
            else
            {
                _consecutiveWrong++;
                _consecutiveCorrect = 0;
                txtIqFeedback.Text = GetLocText($"❌ Đáp án: {q.Answer}", $"❌ Answer: {q.Answer}");
                txtIqFeedback.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                if (_isEndless)
                {
                    _lives--;
                    UpdateLivesDisplay();
                    ShakeHearts();
                }
                if (_consecutiveWrong >= 2)
                {
                    int currentLevel = cboIqLevel?.SelectedIndex ?? 0;
                    if (currentLevel > 0)
                    {
                        cboIqLevel.SelectedIndex = currentLevel - 1;
                        txtIqFeedback.Text += " " + GetLocText("📉 Giảm độ khó!", "📉 Decrease difficulty!");
                        UpdateCurrentLevelLabel();
                        ShowToastNotification("📉", GetLocText("Giảm độ khó!", "Decrease difficulty!"), false);
                    }
                    _consecutiveWrong = 0;
                }
            }
            txtIqScore.Text = GetLocText($"Điểm: {_correct}/{_total}", $"Score: {_correct}/{_total}");
            _idx++;

            if (_answerDelayTimer != null)
            {
                _answerDelayTimer.Stop();
            }
            _answerDelayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
            _answerDelayTimer.Tick += (_, _) =>
            {
                if (_answerDelayTimer != null)
                {
                    _answerDelayTimer.Stop();
                    _answerDelayTimer = null;
                }
                _active = true;
                ShowIqQ();
            };
            _answerDelayTimer.Start();
        }

        private void Numpad_Click(object sender, RoutedEventArgs e)
        {
            if (!_active) return;
            if (sender is Button btn && btn.Tag is string val)
            {
                if (val == "Backspace")
                {
                    if (txtIqAnswer.Text.Length > 0)
                    {
                        txtIqAnswer.Text = txtIqAnswer.Text.Substring(0, txtIqAnswer.Text.Length - 1);
                    }
                }
                else if (val == "Enter")
                {
                    ProcessTextAnswer(txtIqAnswer.Text);
                }
                else
                {
                    if (txtIqAnswer.Text.Length < txtIqAnswer.MaxLength)
                    {
                        txtIqAnswer.Text += val;
                    }
                }
                txtIqAnswer.Focus();
                txtIqAnswer.CaretIndex = txtIqAnswer.Text.Length;
            }
        }

        private string GetLocText(string viText, string enText)
        {
            return QASmartClass.Shared.LanguageManager.CurrentLanguage == "en" ? enText : viText;
        }

        private void ShowToastNotification(string icon, string msg, bool isUpgrade)
        {
            if (brdIqToast == null || txtIqToastIcon == null || txtIqToastMsg == null || ttIqToast == null) return;
            
            txtIqToastIcon.Text = icon;
            txtIqToastMsg.Text = msg;
            
            if (isUpgrade)
            {
                brdIqToast.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                brdIqToast.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                txtIqToastMsg.Foreground = new SolidColorBrush(Color.FromRgb(27, 94, 32));
            }
            else
            {
                brdIqToast.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                brdIqToast.BorderBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                txtIqToastMsg.Foreground = new SolidColorBrush(Color.FromRgb(183, 28, 28));
            }

            brdIqToast.BeginAnimation(UIElement.OpacityProperty, null);
            ttIqToast.BeginAnimation(TranslateTransform.YProperty, null);

            var opacityAnim = new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames();
            opacityAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(0, TimeSpan.FromSeconds(0)));
            opacityAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(1, TimeSpan.FromSeconds(0.2)));
            opacityAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(1, TimeSpan.FromSeconds(1.2)));
            opacityAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(0, TimeSpan.FromSeconds(1.5)));

            var yAnim = new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames();
            yAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(30, TimeSpan.FromSeconds(0)));
            yAnim.KeyFrames.Add(new System.Windows.Media.Animation.EasingDoubleKeyFrame(0, TimeSpan.FromSeconds(0.2), new System.Windows.Media.Animation.PowerEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }));
            yAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(0, TimeSpan.FromSeconds(1.2)));
            yAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(-10, TimeSpan.FromSeconds(1.5)));

            brdIqToast.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
            ttIqToast.BeginAnimation(TranslateTransform.YProperty, yAnim);
        }

        private void ShakeHearts()
        {
            if (ttIqLives == null) return;
            ttIqLives.BeginAnimation(TranslateTransform.XProperty, null);

            var shakeAnim = new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames();
            double amplitude = 12;
            double duration = 0.05;
            
            shakeAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(0, TimeSpan.FromSeconds(0)));
            shakeAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(-amplitude, TimeSpan.FromSeconds(duration)));
            shakeAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(amplitude, TimeSpan.FromSeconds(duration * 2)));
            shakeAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(-amplitude * 0.7, TimeSpan.FromSeconds(duration * 3)));
            shakeAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(amplitude * 0.7, TimeSpan.FromSeconds(duration * 4)));
            shakeAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(-amplitude * 0.4, TimeSpan.FromSeconds(duration * 5)));
            shakeAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(amplitude * 0.4, TimeSpan.FromSeconds(duration * 6)));
            shakeAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(0, TimeSpan.FromSeconds(duration * 7)));

            ttIqLives.BeginAnimation(TranslateTransform.XProperty, shakeAnim);
        }

        private void StartConfetti()
        {
            if (cvsIqConfetti == null) return;
            cvsIqConfetti.Children.Clear();

            double w = cvsIqConfetti.ActualWidth > 0 ? cvsIqConfetti.ActualWidth : 900;
            double h = cvsIqConfetti.ActualHeight > 0 ? cvsIqConfetti.ActualHeight : 600;

            var rand = new Random();
            var particles = new List<Tuple<System.Windows.Shapes.Rectangle, double, double, double>>(); // shape, x, y, speedY

            for (int i = 0; i < 35; i++)
            {
                var rect = new System.Windows.Shapes.Rectangle
                {
                    Width = rand.Next(8, 14),
                    Height = rand.Next(6, 11),
                    Fill = new SolidColorBrush(Color.FromRgb((byte)rand.Next(100, 256), (byte)rand.Next(100, 256), (byte)rand.Next(100, 256))),
                    RenderTransform = new RotateTransform(rand.Next(0, 360))
                };

                double x = rand.NextDouble() * w;
                double y = -rand.Next(10, 100);
                double speedY = rand.Next(4, 9);

                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                cvsIqConfetti.Children.Add(rect);

                particles.Add(new Tuple<System.Windows.Shapes.Rectangle, double, double, double>(rect, x, y, speedY));
            }

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(25) };
            int ticks = 0;
            int maxTicks = 80; // 2.0 seconds

            timer.Tick += (s, e) =>
            {
                ticks++;
                if (ticks > maxTicks)
                {
                    timer.Stop();
                    cvsIqConfetti.Children.Clear();
                    return;
                }

                for (int i = 0; i < particles.Count; i++)
                {
                    var p = particles[i];
                    double newY = p.Item3 + p.Item4;
                    Canvas.SetTop(p.Item1, newY);
                    particles[i] = new Tuple<System.Windows.Shapes.Rectangle, double, double, double>(p.Item1, p.Item2, newY, p.Item4);
                }
            };
            timer.Start();
        }

        private class LeaderboardBindItem
        {
            public string Difficulty { get; set; } = "";
            public string ScoreDisplay { get; set; } = "";
            public string DurationDisplay { get; set; } = "";
            public string DateDisplay { get; set; } = "";
        }

        private void LoadTopScores()
        {
            try
            {
                int isEndlessVal = 0;
                if (_active)
                {
                    isEndlessVal = _isEndless ? 1 : 0;
                }
                else
                {
                    isEndlessVal = (cboIqMode != null && cboIqMode.SelectedIndex == 1) ? 1 : 0;
                }
                var list = DbManager.GetTopProgress("Luyện IQ & Logic", 5, isEndlessVal);
                if (list == null || list.Count == 0)
                {
                    if (lsvIqLeaderboard != null) lsvIqLeaderboard.Visibility = Visibility.Collapsed;
                    if (txtIqLeaderboardEmpty != null) txtIqLeaderboardEmpty.Visibility = Visibility.Visible;
                }
                else
                {
                    if (txtIqLeaderboardEmpty != null) txtIqLeaderboardEmpty.Visibility = Visibility.Collapsed;
                    if (lsvIqLeaderboard != null)
                    {
                        lsvIqLeaderboard.Visibility = Visibility.Visible;
                        string dateFormat = GetLocText("dd/MM/yyyy HH:mm", "MM/dd/yyyy hh:mm tt");
                        var bindList = list.Select(item => new LeaderboardBindItem
                        {
                            Difficulty = GetLocalizedDifficulty(item.Difficulty),
                            ScoreDisplay = $"{item.Score}/{item.Total}",
                            DurationDisplay = $"{item.DurationSeconds:F1}s",
                            DateDisplay = item.CreatedAt.ToString(dateFormat)
                        }).ToList();
                        lsvIqLeaderboard.ItemsSource = bindList;
                    }
                }
            }
            catch
            {
                // Safety first
            }
        }

        private void DrawAdaptiveChart()
        {
            if (cvsIqChart == null) return;
            cvsIqChart.Children.Clear();
            if (_levelHistory == null || _levelHistory.Count == 0) return;

            const int maxDisplayPoints = 30;
            int startIndex = 0;
            int countToDraw = _levelHistory.Count;
            string titleSuffix = "";

            if (_levelHistory.Count > maxDisplayPoints)
            {
                startIndex = _levelHistory.Count - maxDisplayPoints;
                countToDraw = maxDisplayPoints;
                titleSuffix = " (30 câu gần nhất)";
            }

            double w = cvsIqChart.ActualWidth > 0 ? cvsIqChart.ActualWidth : 460;
            double h = cvsIqChart.ActualHeight > 0 ? cvsIqChart.ActualHeight : 200;

            double paddingLeft = 60;
            double paddingRight = 20;
            double paddingTop = 35;
            double paddingBottom = 30;

            double plotW = w - paddingLeft - paddingRight;
            double plotH = h - paddingTop - paddingBottom;

            if (plotW <= 0 || plotH <= 0) return;

            // 1. Title
            var tbTitle = new TextBlock
            {
                Text = "Đồ thị tiến trình độ khó thích ứng" + titleSuffix,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(173, 20, 87))
            };
            Canvas.SetLeft(tbTitle, paddingLeft);
            Canvas.SetTop(tbTitle, 8);
            cvsIqChart.Children.Add(tbTitle);

            // 2. Y-Axis Grid Lines & Labels
            string[] levelLabels = { "Dễ", "Trung bình", "Khó", "Chuyên gia" };
            for (int L = 0; L <= 3; L++)
            {
                double yCoord = paddingTop + plotH - (L / 3.0) * plotH;

                var gridLine = new System.Windows.Shapes.Line
                {
                    X1 = paddingLeft,
                    Y1 = yCoord,
                    X2 = w - paddingRight,
                    Y2 = yCoord,
                    Stroke = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection(new double[] { 4, 4 })
                };
                cvsIqChart.Children.Add(gridLine);

                var tbLabel = new TextBlock
                {
                    Text = levelLabels[L],
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                    Width = paddingLeft - 8,
                    TextAlignment = TextAlignment.Right
                };
                Canvas.SetLeft(tbLabel, 2);
                Canvas.SetTop(tbLabel, yCoord - 7);
                cvsIqChart.Children.Add(tbLabel);
            }

            // 3. X-Axis line
            var xAxis = new System.Windows.Shapes.Line
            {
                X1 = paddingLeft,
                Y1 = paddingTop + plotH,
                X2 = w - paddingRight,
                Y2 = paddingTop + plotH,
                Stroke = new SolidColorBrush(Color.FromRgb(189, 189, 189)),
                StrokeThickness = 1.5
            };
            cvsIqChart.Children.Add(xAxis);

            // 4. X-Axis Labels (Ticks)
            int step = System.Math.Max(1, countToDraw / 10);
            for (int i = startIndex; i < _levelHistory.Count; i += step)
            {
                double xCoord;
                if (countToDraw > 1)
                    xCoord = paddingLeft + ((double)(i - startIndex) / (countToDraw - 1)) * plotW;
                else
                    xCoord = paddingLeft + plotW / 2.0;

                var tbXLabel = new TextBlock
                {
                    Text = $"C{i + 1}",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117))
                };
                Canvas.SetLeft(tbXLabel, xCoord - 10);
                Canvas.SetTop(tbXLabel, paddingTop + plotH + 6);
                cvsIqChart.Children.Add(tbXLabel);
            }

            // 5. Drawing Polyline for data connections
            if (countToDraw > 1)
            {
                var points = new PointCollection();
                for (int i = startIndex; i < _levelHistory.Count; i++)
                {
                    double xCoord = paddingLeft + ((double)(i - startIndex) / (countToDraw - 1)) * plotW;
                    double yCoord = paddingTop + plotH - (_levelHistory[i] / 3.0) * plotH;
                    points.Add(new Point(xCoord, yCoord));
                }

                var polyline = new System.Windows.Shapes.Polyline
                {
                    Points = points,
                    Stroke = new SolidColorBrush(Color.FromRgb(233, 30, 99)),
                    StrokeThickness = 2.5,
                    StrokeLineJoin = PenLineJoin.Round
                };
                cvsIqChart.Children.Add(polyline);
            }

            // 6. Dots on data points
            for (int i = startIndex; i < _levelHistory.Count; i++)
            {
                double xCoord;
                if (countToDraw > 1)
                    xCoord = paddingLeft + ((double)(i - startIndex) / (countToDraw - 1)) * plotW;
                else
                    xCoord = paddingLeft + plotW / 2.0;

                double yCoord = paddingTop + plotH - (_levelHistory[i] / 3.0) * plotH;

                var dot = new System.Windows.Shapes.Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(Color.FromRgb(173, 20, 87)),
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(dot, xCoord - 3);
                Canvas.SetTop(dot, yCoord - 3);
                cvsIqChart.Children.Add(dot);
            }
        }

        // ═══ QUESTION MODEL ═══
        private record IqQ(string Question, string Display, string Answer, string[]? Options = null);

        // ═══ GENERATE ═══
        private List<IqQ> GenerateQuestions(int count, int level)
        {
            var all = new List<IqQ>();
            int seqCount = level <= 1 ? 8 : 5;
            for (int i = 0; i < seqCount; i++) all.Add(GenSequence(level));
            foreach (var q in Analogies.OrderBy(_ => _rng.Next()).Take(3)) all.Add(q);
            foreach (var q in LogicQuestions.OrderBy(_ => _rng.Next()).Take(2)) all.Add(q);
            foreach (var q in OddOneOut.OrderBy(_ => _rng.Next()).Take(2)) all.Add(q);
            foreach (var q in PatternQuestions.OrderBy(_ => _rng.Next()).Take(3)) all.Add(q);
            return all.OrderBy(_ => _rng.Next()).Take(count).ToList();
        }

        private IqQ GenerateSingleQuestion(int level)
        {
            for (int attempt = 0; attempt < 50; attempt++)
            {
                IqQ q;
                int type = _rng.Next(5);
                if (type == 0) q = GenSequence(level);
                else if (type == 1) q = Analogies[_rng.Next(Analogies.Length)];
                else if (type == 2) q = LogicQuestions[_rng.Next(LogicQuestions.Length)];
                else if (type == 3) q = OddOneOut[_rng.Next(OddOneOut.Length)];
                else q = PatternQuestions[_rng.Next(PatternQuestions.Length)];

                string key = q.Display + "|" + q.Question;
                if (!_historyQuestions.Contains(key))
                {
                    _historyQuestions.Add(key);
                    return q;
                }
            }
            return GenSequence(level);
        }

        private void SaveGameProgressToDb()
        {
            try
            {
                string levelName = (cboIqLevel?.SelectedIndex ?? 0) switch
                {
                    0 => "Dễ",
                    1 => "Trung bình",
                    2 => "Khó",
                    _ => "Chuyên gia"
                };

                double seconds = (DateTime.Now - _startTime).TotalSeconds;

                var progress = new UserProgress
                {
                    GameName = "Luyện IQ & Logic",
                    Score = _correct,
                    Total = _isEndless ? _total : 15,
                    DurationSeconds = System.Math.Round(seconds, 1),
                    Difficulty = levelName,
                    CreatedAt = DateTime.Now,
                    IsEndless = _isEndless ? 1 : 0
                };
                DbManager.SaveProgress(progress);

                // Kích hoạt đồng bộ Cloud bất đồng bộ trên luồng nền
                Task.Run(async () =>
                {
                    try
                    {
                        if (txtIqSyncStatus != null)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                txtIqSyncStatus.Text = "☁️ Đang đồng bộ...";
                                txtIqSyncStatus.Foreground = new SolidColorBrush(Color.FromRgb(2, 136, 209));
                                txtIqSyncStatus.ToolTip = "Đang đồng bộ dữ liệu học tập lên máy chủ lớp học...";
                            });
                        }

                        bool success = await CloudSyncService.SyncProgressAsync(progress);

                        if (txtIqSyncStatus != null)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                if (success)
                                {
                                    txtIqSyncStatus.Text = "☁️ Đã đồng bộ";
                                    txtIqSyncStatus.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                                    txtIqSyncStatus.ToolTip = "Dữ liệu học tập được tự động đồng bộ trực tuyến lên máy chủ lớp học.";
                                }
                                else
                                {
                                    txtIqSyncStatus.Text = "⚠️ Lưu ngoại tuyến";
                                    txtIqSyncStatus.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                                    txtIqSyncStatus.ToolTip = "Mất kết nối mạng. Dữ liệu đã được lưu ngoại tuyến an toàn trong SQLite cục bộ và tự động đồng bộ lại khi khôi phục kết nối.";
                                }
                            });
                        }
                    }
                    catch
                    {
                        // Tránh ném lỗi ra ngoài luồng nền
                    }
                });
            }
            catch
            {
                // Safety first
            }
        }

        private IqQ GenSequence(int level)
        {
            var generators = new Func<IqQ>[] { () => GenArith(level), () => GenMul(level), () => GenSquare(level), () => GenFib(level) };
            return generators[_rng.Next(generators.Length)]();
        }

        private IqQ GenArith(int level)
        {
            int max = level switch { 0 => 20, 1 => 50, 2 => 100, _ => 200 };
            int a = _rng.Next(1, max); int d = _rng.Next(2, 12 + level * 5);
            var seq = Enumerable.Range(0, 5).Select(i => a + d * i).ToArray();
            int answer = a + d * 5;
            return new IqQ(GetLocText("Tìm số tiếp theo:", "Find the next number:"), string.Join(", ", seq) + ", ?", answer.ToString());
        }

        private IqQ GenMul(int level)
        {
            int maxA = level switch { 0 => 3, 1 => 5, 2 => 8, _ => 12 };
            int maxR = level switch { 0 => 3, 1 => 4, 2 => 5, _ => 6 };
            int r = _rng.Next(2, maxR);
            int limitA = 5000 / (int)System.Math.Pow(r, 5);
            int a = _rng.Next(1, System.Math.Min(maxA, limitA + 1));
            var seq = Enumerable.Range(0, 5).Select(i => a * (int)System.Math.Pow(r, i)).ToArray();
            return new IqQ(GetLocText("Tìm số tiếp theo (nhân):", "Find the next number (multiplication):"), string.Join(", ", seq) + ", ?", (a * (int)System.Math.Pow(r, 5)).ToString());
        }

        private IqQ GenSquare(int level)
        {
            int startMax = level switch { 0 => 6, 1 => 12, 2 => 18, _ => 21 };
            int s = _rng.Next(1, startMax);
            var seq = Enumerable.Range(s, 5).Select(i => i * i).ToArray();
            return new IqQ(GetLocText("Tìm số tiếp theo (bình phương):", "Find the next number (square):"), string.Join(", ", seq) + ", ?", ((s + 5) * (s + 5)).ToString());
        }

        private IqQ GenFib(int level)
        {
            int max = level switch { 0 => 4, 1 => 8, 2 => 12, _ => 16 };
            int a = _rng.Next(1, max), b = _rng.Next(1, max);
            var seq = new List<int> { a, b };
            for (int i = 2; i < 6; i++) seq.Add(seq[i - 1] + seq[i - 2]);
            return new IqQ(GetLocText("Tìm số tiếp theo:", "Find the next number:"), string.Join(", ", seq.Take(5)) + ", ?", seq[5].ToString());
        }

        // ═══ STATIC DATA ═══
        // ═══ STATIC DATA ═══
        private static readonly IqQ[] AnalogiesVi =
        {
            new("Tương tự: Nóng → Lạnh, thì Cao → ?", "Nóng : Lạnh = Cao : ?", "Thấp", new[] { "Thấp", "Dài", "Rộng", "Nặng" }),
            new("Tương tự: Mèo → Meo, thì Chó → ?", "Mèo : Meo = Chó : ?", "Gâu gâu", new[] { "Gâu gâu", "Cục cục", "Bê bê", "Quạc quạc" }),
            new("Tương tự: Mắt → Nhìn, thì Tai → ?", "Mắt : Nhìn = Tai : ?", "Nghe", new[] { "Nói", "Nghe", "Chạm", "Ngửi" }),
            new("Tương tự: Sách → Đọc, thì Nhạc → ?", "Sách : Đọc = Nhạc : ?", "Nghe", new[] { "Viết", "Xem", "Nghe", "Hát" }),
            new("Tương tự: Bác sĩ → Bệnh viện, thì Giáo viên → ?", "Bác sĩ : Bệnh viện = GV : ?", "Trường học", new[] { "Nhà máy", "Trường học", "Chợ", "Sân bay" }),
            new("Tương tự: Nước → Biển, thì Cát → ?", "Nước : Biển = Cát : ?", "Sa mạc", new[] { "Rừng", "Đồng cỏ", "Sa mạc", "Sông" }),
            new("Tương tự: Ngày → Đêm, thì Sáng → ?", "Ngày : Đêm = Sáng : ?", "Tối", new[] { "Chiều", "Tối", "Trưa", "Rạng" }),
            new("Tương tự: Cha → Con, thì Thầy → ?", "Cha : Con = Thầy : ?", "Trò", new[] { "Bạn", "Trò", "Đồng nghiệp", "Cháu" }),
        };

        private static readonly IqQ[] AnalogiesEn =
        {
            new("Analogy: Hot → Cold, then Tall → ?", "Hot : Cold = Tall : ?", "Short", new[] { "Short", "Long", "Wide", "Heavy" }),
            new("Analogy: Cat → Meow, then Dog → ?", "Cat : Meow = Dog : ?", "Woof", new[] { "Woof", "Cluck", "Bleat", "Quack" }),
            new("Analogy: Eye → See, then Ear → ?", "Eye : See = Ear : ?", "Hear", new[] { "Speak", "Hear", "Touch", "Smell" }),
            new("Analogy: Book → Read, then Music → ?", "Book : Read = Music : ?", "Listen", new[] { "Write", "Watch", "Listen", "Sing" }),
            new("Analogy: Doctor → Hospital, then Teacher → ?", "Doctor : Hospital = Teacher : ?", "School", new[] { "Factory", "School", "Market", "Airport" }),
            new("Analogy: Water → Ocean, then Sand → ?", "Water : Ocean = Sand : ?", "Desert", new[] { "Forest", "Grassland", "Desert", "River" }),
            new("Analogy: Day → Night, then Morning → ?", "Day : Night = Morning : ?", "Evening", new[] { "Afternoon", "Evening", "Noon", "Dawn" }),
            new("Analogy: Parent → Child, then Teacher → ?", "Parent : Child = Teacher : ?", "Student", new[] { "Friend", "Student", "Colleague", "Niece/Nephew" }),
        };

        private static IqQ[] Analogies => QASmartClass.Shared.LanguageManager.CurrentLanguage == "en" ? AnalogiesEn : AnalogiesVi;

        private static readonly IqQ[] LogicQuestionsVi =
        {
            new("Nếu tất cả hoa hồng đều đẹp, và bông hoa này là hoa hồng, thì:", "🌹 Suy luận logic", "Bông hoa này đẹp", new[] { "Bông hoa này đẹp", "Bông hoa này xấu", "Không biết", "Có thể đẹp" }),
            new("A cao hơn B, B cao hơn C. Ai thấp nhất?", "A > B > C", "C", new[] { "A", "B", "C", "Không biết" }),
            new("Hôm nay là thứ 3, 5 ngày nữa là thứ mấy?", "Thứ 3 + 5 = ?", "Chủ nhật", new[] { "Thứ 7", "Chủ nhật", "Thứ 2", "Thứ 6" }),
            new("Một con ốc sên leo lên cột 10m, mỗi ngày leo 3m nhưng ban đêm tụt 2m. Hỏi bao nhiêu ngày leo đến đỉnh?", "🐌 Logic", "8", new[] { "8", "10", "7", "9" }),
            new("Nếu 2 = 6, 3 = 12, 4 = 20, thì 5 = ?", "Tìm quy luật: n × (n+1)", "30", new[] { "25", "30", "35", "24" }),
            new("Kim đồng hồ chỉ 3:15. Góc giữa kim giờ và kim phút là bao nhiêu?", "⏰ Góc đồng hồ", "7.5 độ", new[] { "0 độ", "7.5 độ", "90 độ", "15 độ" }),
        };

        private static readonly IqQ[] LogicQuestionsEn =
        {
            new("If all roses are beautiful, and this flower is a rose, then:", "🌹 Logical reasoning", "This flower is beautiful", new[] { "This flower is beautiful", "This flower is ugly", "Unknown", "Could be beautiful" }),
            new("A is taller than B, B is taller than C. Who is the shortest?", "A > B > C", "C", new[] { "A", "B", "C", "Unknown" }),
            new("Today is Tuesday, what day is 5 days from now?", "Tuesday + 5 = ?", "Sunday", new[] { "Saturday", "Sunday", "Monday", "Friday" }),
            new("A snail climbs a 10m pole. It climbs 3m daily but slips 2m nightly. How many days to reach the top?", "🐌 Logic", "8", new[] { "8", "10", "7", "9" }),
            new("If 2 = 6, 3 = 12, 4 = 20, then 5 = ?", "Pattern: n × (n+1)", "30", new[] { "25", "30", "35", "24" }),
            new("The clock shows 3:15. What is the angle between the hour and minute hands?", "⏰ Clock angle", "7.5 degrees", new[] { "0 degrees", "7.5 degrees", "90 degrees", "15 degrees" }),
        };

        private static IqQ[] LogicQuestions => QASmartClass.Shared.LanguageManager.CurrentLanguage == "en" ? LogicQuestionsEn : LogicQuestionsVi;

        private static readonly IqQ[] OddOneOutVi =
        {
            new("Từ nào KHÔNG cùng nhóm?", "🍎 Táo, 🍌 Chuối, 🥕 Cà rốt, 🍇 Nho", "Cà rốt", new[] { "Táo", "Chuối", "Cà rốt", "Nho" }),
            new("Số nào KHÔNG cùng nhóm?", "2, 3, 5, 9, 7", "9", new[] { "2", "3", "5", "9", "7" }),
            new("Từ nào KHÔNG cùng nhóm?", "Chó, Mèo, Cá, Chim, Bàn", "Bàn", new[] { "Chó", "Mèo", "Cá", "Chim", "Bàn" }),
            new("Hình nào KHÔNG cùng nhóm?", "⬛ Vuông, ⬜ Chữ nhật, 🔺 Tam giác, 🔵 Tròn", "Tròn", new[] { "Vuông", "Chữ nhật", "Tam giác", "Tròn" }),
            new("Từ nào KHÔNG cùng nhóm?", "Sông, Hồ, Biển, Núi", "Núi", new[] { "Sông", "Hồ", "Biển", "Núi" }),
        };

        private static readonly IqQ[] OddOneOutEn =
        {
            new("Which word does NOT belong to the group?", "🍎 Apple, 🍌 Banana, 🥕 Carrot, 🍇 Grape", "Carrot", new[] { "Apple", "Banana", "Carrot", "Grape" }),
            new("Which number does NOT belong to the group?", "2, 3, 5, 9, 7", "9", new[] { "2", "3", "5", "9", "7" }),
            new("Which word does NOT belong to the group?", "Dog, Cat, Fish, Bird, Table", "Table", new[] { "Dog", "Cat", "Fish", "Bird", "Table" }),
            new("Which shape does NOT belong to the group?", "⬛ Square, ⬜ Rectangle, 🔺 Triangle, 🔵 Circle", "Circle", new[] { "Square", "Rectangle", "Triangle", "Circle" }),
            new("Which word does NOT belong to the group?", "River, Lake, Sea, Mountain", "Mountain", new[] { "River", "Lake", "Sea", "Mountain" }),
        };

        private static IqQ[] OddOneOut => QASmartClass.Shared.LanguageManager.CurrentLanguage == "en" ? OddOneOutEn : OddOneOutVi;

        private static readonly IqQ[] PatternQuestionsVi =
        {
            new("Hoàn thành mẫu: AB, CD, EF, ?", "AB → CD → EF → ?", "GH", new[] { "FG", "GH", "HI", "EF" }),
            new("Hoàn thành mẫu: 1A, 2B, 3C, ?", "1A → 2B → 3C → ?", "4D", new[] { "4D", "3D", "4C", "5E" }),
            new("Hoàn thành: ☀️🌙☀️🌙☀️ → ?", "☀️🌙☀️🌙☀️?", "🌙", new[] { "☀️", "🌙", "⭐", "🌈" }),
            new("Hoàn thành: ▲▼▲▲▼▼▲▲▲ → ?", "Tìm quy luật lặp", "▼▼▼", new[] { "▲▲▲", "▼▼▼", "▲▼▲", "▼▲▼" }),
            new("Hoàn thành: AZ, BY, CX, ?", "A→B→C, Z→Y→X", "DW", new[] { "DW", "DV", "EW", "CW" }),
        };

        private static readonly IqQ[] PatternQuestionsEn =
        {
            new("Complete the pattern: AB, CD, EF, ?", "AB → CD → EF → ?", "GH", new[] { "FG", "GH", "HI", "EF" }),
            new("Complete the pattern: 1A, 2B, 3C, ?", "1A → 2B → 3C → ?", "4D", new[] { "4D", "3D", "4C", "5E" }),
            new("Complete the pattern: ☀️🌙☀️🌙☀️ → ?", "☀️🌙☀️🌙☀️?", "🌙", new[] { "☀️", "🌙", "⭐", "🌈" }),
            new("Complete the pattern: ▲▼▲▲▼▼▲▲▲ → ?", "Find the pattern rule", "▼▼▼", new[] { "▲▲▲", "▼▼▼", "▲▼▲", "▼▲▼" }),
            new("Complete the pattern: AZ, BY, CX, ?", "A→B→C, Z→Y→X", "DW", new[] { "DW", "DV", "EW", "CW" }),
        };

        private static IqQ[] PatternQuestions => QASmartClass.Shared.LanguageManager.CurrentLanguage == "en" ? PatternQuestionsEn : PatternQuestionsVi;

        private void BtnIqExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string tempDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QASmartClass", "Temp");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                string pngPath = Path.Combine(tempDir, "chart_temp.png");
                string htmlPath = Path.Combine(tempDir, "report_temp.html");

                // Capture the chart visually
                SaveVisualToPng(brdIqChart, pngPath);

                // Build HTML body
                var topProgress = DbManager.GetTopProgress("Luyện IQ & Logic", 5, _isEndless ? 1 : 0);
                StringBuilder sbRecords = new StringBuilder();
                
                string emptyRecordsText = GetLocText("Chưa có kỷ lục nào được ghi nhận. Hãy bắt đầu chơi để thiết lập kỷ lục đầu tiên!", "No records recorded yet. Start playing to set your first record!");

                string dateFormat = GetLocText("dd/MM/yyyy HH:mm", "MM/dd/yyyy hh:mm tt");
                string dateTimeSecFormat = GetLocText("dd/MM/yyyy HH:mm:ss", "MM/dd/yyyy hh:mm:ss tt");

                if (topProgress != null && topProgress.Count > 0)
                {
                    foreach (var r in topProgress)
                    {
                        string localizedDiff = GetLocalizedDifficulty(r.Difficulty);
                        sbRecords.Append("<tr>");
                        sbRecords.Append($"<td>{localizedDiff}</td>");
                        sbRecords.Append($"<td>{r.Score}/{r.Total}</td>");
                        sbRecords.Append($"<td>{r.DurationSeconds:F1}s</td>");
                        sbRecords.Append($"<td>{r.CreatedAt.ToString(dateFormat)}</td>");
                        sbRecords.Append("</tr>");
                    }
                }
                else
                {
                    sbRecords.Append($"<tr><td colspan='4' style='text-align:center; color:#9E9E9E;'>{emptyRecordsText}</td></tr>");
                }

                double seconds = (DateTime.Now - _startTime).TotalSeconds;
                string levelName = cboIqLevel.SelectedIndex switch
                {
                    0 => GetLocText("Dễ", "Easy"),
                    1 => GetLocText("Trung bình", "Medium"),
                    2 => GetLocText("Khó", "Hard"),
                    _ => GetLocText("Chuyên gia", "Expert")
                };

                double correctRate = _total > 0 ? (double)_correct / _total : 0;
                string advice = correctRate >= 0.85 ? GetLocText("Tư duy logic xuất sắc, cần tiếp tục phát huy!", "Excellent logical thinking, keep up the great work!") :
                                 correctRate >= 0.70 ? GetLocText("Khả năng phân tích tốt, hãy luyện tập thêm để hoàn hảo hơn!", "Good analytical skills, practice more to reach perfection!") :
                                 correctRate >= 0.50 ? GetLocText("Khá tốt, hãy rèn luyện thêm các câu hỏi chuỗi số để tăng phản xạ!", "Quite good, practice more number series questions to improve your reflexes!") :
                                                       GetLocText("Cần rèn luyện thêm các dạng toán quy luật và tư duy logic.", "Needs more practice on pattern recognition and logical reasoning.");

                string modeText = _isEndless ? GetLocText("Vô hạn", "Endless") : GetLocText("15 câu", "15 Questions");

                // Read image source as absolute file URI so Edge can load it
                string imageUri = new Uri(pngPath).AbsoluteUri;

                string reportTitle = GetLocText("BÁO CÁO KẾT QUẢ HỌC TẬP", "STUDENT PROGRESS REPORT");
                string systemTitle = GetLocText("Hệ thống Học cụ Tư duy QA SmartClass - Luyện IQ & Logic", "QA SmartClass Thinking Tools - IQ & Logic Practice");
                string sec1Title = GetLocText("I. Thông tin lượt chơi mới nhất", "I. Latest Session Information");
                string lblDate = GetLocText("Ngày giờ chơi:", "Date/Time:");
                string lblMode = GetLocText("Chế độ chơi:", "Game Mode:");
                string lblCorrectCount = GetLocText("Số câu đúng:", "Correct Answers:");
                string lblDuration = GetLocText("Thời gian làm bài:", "Duration:");
                string lblEndLevel = GetLocText("Cấp độ kết thúc:", "Ending Level:");
                string lblAccuracy = GetLocText("Tỷ lệ chính xác:", "Accuracy Rate:");
                string secondsText = GetLocText("giây", "seconds");
                
                string sec2Title = GetLocText("II. Đồ thị tiến trình thích ứng", "II. Adaptive Difficulty Progress Chart");
                string sec3Title = GetLocText("III. Kỷ lục cá nhân (Top 5 cao nhất)", "III. Personal Records (Top 5 Highest)");
                
                string colDiff = GetLocText("Độ khó", "Difficulty");
                string colScore = GetLocText("Điểm số", "Score");
                string colDuration = GetLocText("Thời gian", "Duration");
                string colDate = GetLocText("Ngày hoàn thành", "Completion Date");
                
                string adviceTitleText = GetLocText("💡 Nhận xét từ Hệ thống Sư phạm:", "💡 Pedagogical System Feedback:");
                string footerText = GetLocText("Báo cáo được trích xuất tự động từ phần mềm QA SmartClass. Bản quyền © 2026.", "Report generated automatically by QA SmartClass software. Copyright © 2026.");

                string html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <style>
        body {{
            font-family: 'Segoe UI', Arial, sans-serif;
            margin: 40px;
            color: #212121;
            background-color: #ffffff;
        }}
        .header {{
            text-align: center;
            border-bottom: 3px solid #AD1457;
            padding-bottom: 20px;
            margin-bottom: 30px;
        }}
        .header h1 {{
            color: #AD1457;
            margin: 0;
            font-size: 28px;
            font-weight: bold;
        }}
        .header p {{
            color: #EC407A;
            margin: 5px 0 0 0;
            font-size: 16px;
        }}
        .section-title {{
            color: #AD1457;
            border-bottom: 1.5px solid #E0E0E0;
            padding-bottom: 5px;
            margin-top: 30px;
            font-size: 18px;
            font-weight: bold;
        }}
        .meta-table {{
            width: 100%;
            margin-bottom: 20px;
            border-collapse: collapse;
        }}
        .meta-table td {{
            padding: 8px;
            font-size: 14px;
        }}
        .meta-table td.label {{
            font-weight: bold;
            color: #757575;
            width: 30%;
        }}
        .meta-table td.value {{
            color: #212121;
        }}
        .chart-container {{
            text-align: center;
            margin: 20px 0;
            padding: 10px;
            background-color: #F9F9F9;
            border: 1px solid #E0E0E0;
            border-radius: 8px;
        }}
        .chart-img {{
            max-width: 100%;
            height: auto;
        }}
        table.data-table {{
            width: 100%;
            border-collapse: collapse;
            margin-top: 10px;
        }}
        table.data-table th, table.data-table td {{
            border: 1px solid #E0E0E0;
            padding: 10px;
            text-align: left;
            font-size: 14px;
        }}
        table.data-table th {{
            background-color: #FCE4EC;
            color: #AD1457;
            font-weight: bold;
        }}
        table.data-table tr:nth-child(even) {{
            background-color: #FAFAFA;
        }}
        .advice-box {{
            background-color: #E3F2FD;
            border-left: 5px solid #2196F3;
            padding: 15px;
            margin-top: 20px;
            border-radius: 4px;
        }}
        .advice-title {{
            font-weight: bold;
            color: #0D47A1;
            margin-bottom: 5px;
        }}
        .footer {{
            text-align: center;
            margin-top: 50px;
            font-size: 12px;
            color: #9E9E9E;
            border-top: 1px solid #E0E0E0;
            padding-top: 20px;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>{reportTitle}</h1>
        <p>{systemTitle}</p>
    </div>

    <div class='section-title'>{sec1Title}</div>
    <table class='meta-table'>
        <tr>
            <td class='label'>{lblDate}</td>
            <td class='value'>{DateTime.Now.ToString(dateTimeSecFormat)}</td>
            <td class='label'>{lblMode}</td>
            <td class='value'>{modeText}</td>
        </tr>
        <tr>
            <td class='label'>{lblCorrectCount}</td>
            <td class='value'>{_correct}/{(_isEndless ? _total : 15)}</td>
            <td class='label'>{lblDuration}</td>
            <td class='value'>{seconds:F1} {secondsText}</td>
        </tr>
        <tr>
            <td class='label'>{lblEndLevel}</td>
            <td class='value'>{levelName}</td>
            <td class='label'>{lblAccuracy}</td>
            <td class='value'>{(correctRate * 100):F1}%</td>
        </tr>
    </table>

    <div class='section-title'>{sec2Title}</div>
    <div class='chart-container'>
        <img class='chart-img' src='{imageUri}'/>
    </div>

    <div class='section-title'>{sec3Title}</div>
    <table class='data-table'>
        <thead>
            <tr>
                <th>{colDiff}</th>
                <th>{colScore}</th>
                <th>{colDuration}</th>
                <th>{colDate}</th>
            </tr>
        </thead>
        <tbody>
            {sbRecords}
        </tbody>
    </table>

    <div class='advice-box'>
        <div class='advice-title'>{adviceTitleText}</div>
        <div>{advice}</div>
    </div>

    <div class='footer'>
        {footerText}
    </div>
</body>
</html>
";
                File.WriteAllText(htmlPath, html, Encoding.UTF8);

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = GetLocText("Tài liệu PDF (*.pdf)|*.pdf", "PDF Document (*.pdf)|*.pdf"),
                    FileName = GetLocText($"BaoCao_IQ_Logic_{DateTime.Now:yyyyMMdd_HHmmss}.pdf", $"IQ_Logic_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf")
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string pdfPath = saveDialog.FileName;
                    string escapedPdfPath = pdfPath.Replace("\"", "\\\"");
                    string escapedHtmlPath = htmlPath.Replace("\"", "\\\"");

                    Task.Run(() =>
                    {
                        try
                        {
                            var startInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = GetEdgePath(),
                                Arguments = $"--headless --disable-gpu --print-to-pdf=\"{escapedPdfPath}\" \"{escapedHtmlPath}\"",
                                CreateNoWindow = true,
                                UseShellExecute = false
                            };
                            using (var process = System.Diagnostics.Process.Start(startInfo))
                            {
                                if (process != null)
                                {
                                    process.WaitForExit(10000);
                                }
                            }

                            if (File.Exists(pdfPath) && new FileInfo(pdfPath).Length > 0)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    MessageBox.Show(
                                        GetLocText("Xuất báo cáo PDF thành công!", "PDF report exported successfully!"),
                                        GetLocText("Thành công", "Success"),
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                });
                            }
                            else
                            {
                                throw new Exception("Edge headless did not generate PDF file.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show(
                                    GetLocText($"Lỗi xuất PDF: {ex.Message}\nHãy đảm bảo Microsoft Edge đã được cài đặt và hoạt động tốt.", $"PDF export error: {ex.Message}\nPlease ensure Microsoft Edge is installed and working correctly."),
                                    GetLocText("Lỗi", "Error"),
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                        }
                        finally
                        {
                            try { if (File.Exists(pngPath)) File.Delete(pngPath); } catch {}
                            try { if (File.Exists(htmlPath)) File.Delete(htmlPath); } catch {}
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    GetLocText($"Lỗi chuẩn bị dữ liệu xuất báo cáo: {ex.Message}", $"Error preparing report data: {ex.Message}"),
                    GetLocText("Lỗi", "Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetLocalizedDifficulty(string diff)
        {
            if (string.IsNullOrEmpty(diff)) return "";
            if (diff.Contains("Dễ") || diff.Equals("Easy", StringComparison.OrdinalIgnoreCase))
                return GetLocText("Dễ", "Easy");
            if (diff.Contains("Trung bình") || diff.Equals("Medium", StringComparison.OrdinalIgnoreCase))
                return GetLocText("Trung bình", "Medium");
            if (diff.Contains("Khó") || diff.Equals("Hard", StringComparison.OrdinalIgnoreCase))
                return GetLocText("Khó", "Hard");
            if (diff.Contains("Chuyên gia") || diff.Equals("Expert", StringComparison.OrdinalIgnoreCase))
                return GetLocText("Chuyên gia", "Expert");
            return diff;
        }

        private void SaveVisualToPng(UIElement element, string filePath)
        {
            double width = element.RenderSize.Width;
            double height = element.RenderSize.Height;
            if (width <= 0) width = 480;
            if (height <= 0) height = 220;

            RenderTargetBitmap bmp = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
            
            element.Measure(new Size(width, height));
            element.Arrange(new Rect(new Point(0, 0), new Size(width, height)));
            bmp.Render(element);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }

        private string GetEdgePath()
        {
            string[] paths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft\Edge\Application\msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Microsoft\Edge\Application\msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Edge\Application\msedge.exe")
            };

            foreach (var path in paths)
            {
                if (File.Exists(path)) return path;
            }

            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe"))
                {
                    if (key != null)
                    {
                        object? val = key.GetValue("");
                        if (val != null && File.Exists(val.ToString()))
                        {
                            return val.ToString()!;
                        }
                    }
                }
            }
            catch { }

            return "msedge";
        }

        public void BtnToggleScratchPad_Click(object sender, RoutedEventArgs e)
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

        public void ScratchMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rad && rad.Tag is string mode)
            {
                if (mode == "Pen")
                {
                    scratchCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    if (spScratchColors != null) spScratchColors.Visibility = Visibility.Visible;
                }
                else if (mode == "Eraser")
                {
                    scratchCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                    if (spScratchColors != null) spScratchColors.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void ScratchColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hexColor)
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(hexColor);
                    scratchCanvas.DefaultDrawingAttributes.Color = color;
                    scratchCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    if (radScratchPen != null) radScratchPen.IsChecked = true;
                    if (radScratchEraser != null) radScratchEraser.IsChecked = false;
                    if (spScratchColors != null) spScratchColors.Visibility = Visibility.Visible;
                }
                catch { }
            }
        }

        private void ScratchCanvas_StrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            if (_isScratchUndoRedoing) return;
            _scratchUndoStack.Push(scratchCanvas.Strokes.Clone());
            _scratchRedoStack.Clear();
        }

        public void BtnClearScratch_Click(object sender, RoutedEventArgs e)
        {
            if (scratchCanvas.Strokes.Count > 0)
            {
                string msg = GetLocText("Bạn có chắc chắn muốn xóa toàn bộ nét vẽ nháp?", "Are you sure you want to clear the entire scratchpad?");
                string title = GetLocText("Xóa nháp", "Clear Sketch");
                if (QASmartClass.LearningTools.Views.Multi.NotebookTool.MessageBoxShowHandler(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    SaveScratchUndoState();
                    scratchCanvas.Strokes.StrokesChanged -= ScratchCanvas_StrokesChanged;
                    scratchCanvas.Strokes.Clear();
                    scratchCanvas.Strokes.StrokesChanged += ScratchCanvas_StrokesChanged;
                }
            }
        }

        private async void BtnSubmitScratch_Click(object sender, RoutedEventArgs e)
        {
            if ((DateTime.Now - _lastSubmitTime).TotalSeconds < 3)
            {
                MessageBox.Show(
                    GetLocText("Vui lòng đợi 3 giây giữa mỗi lần nộp nháp để tránh nghẽn mạng!", "Please wait 3 seconds between sketch submissions to avoid network congestion!"),
                    GetLocText("Thông báo", "Notification"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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

                var app = Application.Current as QASmartTouch.App;
                if (app?.StudentNetwork != null && app.StudentNetwork.IsConnected)
                {
                    string studentCode = app.StudentNetwork.StudentCode;
                    string packet = $"SUBMIT_SCRATCHPAD|{studentCode}|{base64}";
                    await app.StudentNetwork.SendAsync(packet);
                    _lastSubmitTime = DateTime.Now;
                    MessageBox.Show(
                        GetLocText("Đã gửi nháp thành công đến Giáo viên!", "Sketch submitted successfully to Teacher!"),
                        GetLocText("Thông báo", "Notification"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        GetLocText("Không kết nối được với máy Giáo viên. Vui lòng kiểm tra lại kết nối mạng!", "Failed to connect to Teacher device. Please check your network connection!"),
                        GetLocText("Lỗi kết nối", "Connection Error"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    GetLocText($"Lỗi khi gửi nháp: {ex.Message}", $"Error sending sketch: {ex.Message}"),
                    GetLocText("Lỗi", "Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnSubmitScratch.Content = GetLocText("📤 Nộp nháp", "📤 Submit Sketch");
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
                        Icon = "💼",
                        Title = isVN ? "Tuyển dụng & Đánh giá Nhân sự" : "Recruitment & Talent Assessment",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_iq_quiz_1_{suffix}.png",
                        Description = isVN
                            ? "Các nhà tuyển dụng sử dụng bài trắc nghiệm IQ và Logic để đánh giá khả năng giải quyết vấn đề, tư duy phân tích nhanh của các ứng viên."
                            : "Recruiters use IQ and logic tests to evaluate candidates' rapid problem-solving abilities and analytical thinking."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🤖",
                        Title = isVN ? "Nhận diện Mẫu trong AI & Data Science" : "Pattern Recognition in AI & Data Science",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_iq_quiz_2_{suffix}.png",
                        Description = isVN
                            ? "Các thuật toán học máy (Machine Learning) mô phỏng lại cách con người nhận diện mẫu số, quy luật hình học để phân tích dữ liệu lớn."
                            : "Machine learning algorithms replicate how humans recognize number patterns and geometric rules to analyze big data."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧠",
                        Title = isVN ? "Rèn luyện Não bộ & Sức khỏe Nhận thức" : "Brain Training & Cognitive Health",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_iq_quiz_3_{suffix}.png",
                        Description = isVN
                            ? "Thường xuyên giải các bài đố logic giúp củng cố kết nối thần kinh, nâng cao trí nhớ ngắn hạn và phòng ngừa lão hóa nhận thức."
                            : "Regularly solving logic puzzles strengthens neural connections, improves short-term memory, and helps prevent cognitive aging."
                    }
                };

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for IqQuizTool: {Err}", ex.Message);
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