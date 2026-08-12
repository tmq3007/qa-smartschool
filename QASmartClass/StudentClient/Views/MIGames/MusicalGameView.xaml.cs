using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace QASmartClass.StudentClient.Views.MIGames
{
    public partial class MusicalGameView : UserControl, IMiGameControl
    {
        public event Action<bool, int> OnGameOver;

        private class GameNote
        {
            public int Lane { get; set; } // 0, 1, 2, 3
            public double Y { get; set; }
            public Ellipse Visual { get; set; } = null!;
            public bool Hit { get; set; }
        }

        private readonly List<GameNote> _notes = new();
        private readonly Random _random = new();
        private DispatcherTimer? _gameTimer;
        private DateTime _lastTime;
        private double _spawnTimerSeconds = 0.0;
        
        private int _score = 0;
        private int _combo = 0;
        private int _totalXp = 0;
        private int _notesProcessed = 0;
        private const int MaxNotes = 20; // 20 notes total for a quick game
        private bool _isGameRunning = false;

        private readonly double[] _laneXCoords = { 35, 135, 235, 335 };

        public MusicalGameView()
        {
            InitializeComponent();
            this.Focusable = true;
            this.KeyDown += UserControl_KeyDown;
        }

        public void StartGame()
        {
            _notes.Clear();
            PlayCanvas.Children.Clear();
            _score = 0;
            _combo = 0;
            _totalXp = 0;
            _spawnTimerSeconds = 0.0;
            _notesProcessed = 0;
            _isGameRunning = true;
            _lastTime = DateTime.UtcNow;

            TxtScore.Text = "Score: 0";
            BorderCombo.Visibility = Visibility.Collapsed;
            TxtFeedback.Visibility = Visibility.Collapsed;
            BtnStart.IsEnabled = false;

            this.Focus();

            // Vòng lặp game 60 FPS
            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60fps
            _gameTimer.Tick += GameLoop_Tick;
            _gameTimer.Start();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            StartGame();
        }

        private void GameLoop_Tick(object? sender, EventArgs e)
        {
            if (!_isGameRunning) return;

            var now = DateTime.UtcNow;
            double deltaTime = (now - _lastTime).TotalSeconds;
            _lastTime = now;

            // Clamp delta time to avoid huge teleportation jumps during system lags
            if (deltaTime > 0.1) deltaTime = 0.1;

            // Spawn notes logic
            _spawnTimerSeconds += deltaTime;
            // Spawn every 0.833 seconds (equivalent to 50 frames at 60fps) or if no notes left
            if ((_spawnTimerSeconds >= 0.833 && _notesProcessed + _notes.Count < MaxNotes) || (_notes.Count == 0 && _notesProcessed < MaxNotes))
            {
                _spawnTimerSeconds = 0.0;
                SpawnNote();
            }

            // Move notes down
            double speed = 250.0; // speed of notes falling in pixels per second (~4.16 pixels per frame at 60fps)
            for (int i = _notes.Count - 1; i >= 0; i--)
            {
                var note = _notes[i];
                note.Y += speed * deltaTime;
                Canvas.SetTop(note.Visual, note.Y);

                // Check for Miss (fell past hit zone, y = 270)
                if (note.Y > 270 && !note.Hit)
                {
                    // Trigger miss
                    ShowFeedback("MISS!", Brushes.Red);
                    _combo = 0;
                    BorderCombo.Visibility = Visibility.Collapsed;
                    TriggerMissEffects();
                    
                    // Cleanup visual
                    PlayCanvas.Children.Remove(note.Visual);
                    _notes.RemoveAt(i);
                    _notesProcessed++;

                    CheckGameEnd();
                }
            }
        }

        private void TriggerMissEffects()
        {
            try
            {
                // Red flash animation on overlay Border
                var flashAnim = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0.35,
                    To = 0.0,
                    Duration = TimeSpan.FromSeconds(0.2)
                };
                RedFlashBorder.BeginAnimation(UIElement.OpacityProperty, flashAnim);

                // Screen shake animation using TranslateTransform
                var shakeAnim = new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames();
                shakeAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(-8, TimeSpan.FromSeconds(0.05)));
                shakeAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(8, TimeSpan.FromSeconds(0.1)));
                shakeAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(-4, TimeSpan.FromSeconds(0.15)));
                shakeAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(4, TimeSpan.FromSeconds(0.2)));
                shakeAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(0, TimeSpan.FromSeconds(0.25)));
                GameBorderTransform.BeginAnimation(TranslateTransform.XProperty, shakeAnim);
            }
            catch { }
        }

        private void SpawnNote()
        {
            int lane = _random.Next(0, 4);
            var ellipse = new Ellipse
            {
                Width = 30,
                Height = 30,
                Fill = GetLaneColor(lane),
                Stroke = Brushes.White,
                StrokeThickness = 1
            };

            var note = new GameNote
            {
                Lane = lane,
                Y = 0,
                Visual = ellipse,
                Hit = false
            };

            Canvas.SetLeft(ellipse, _laneXCoords[lane]);
            Canvas.SetTop(ellipse, 0);
            PlayCanvas.Children.Add(ellipse);
            _notes.Add(note);
        }

        private Brush GetLaneColor(int lane)
        {
            if (lane == 0) return new SolidColorBrush(Color.FromRgb(79, 70, 229));  // Indigo
            if (lane == 1) return new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Emerald
            if (lane == 2) return new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Amber
            return new SolidColorBrush(Color.FromRgb(236, 72, 153));                // Pink
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_isGameRunning) return;

            int targetLane = -1;
            if (e.Key == Key.A) targetLane = 0;
            else if (e.Key == Key.S) targetLane = 1;
            else if (e.Key == Key.D) targetLane = 2;
            else if (e.Key == Key.F) targetLane = 3;

            if (targetLane != -1)
            {
                CheckHit(targetLane);
                e.Handled = true;
            }
        }

        private void CheckHit(int lane)
        {
            // Find the closest note in the same lane
            var closestNote = _notes
                .Where(n => n.Lane == lane && !n.Hit)
                .OrderBy(n => Math.Abs(n.Y - 240))
                .FirstOrDefault();

            if (closestNote == null) return;

            double diff = Math.Abs(closestNote.Y - 240);

            if (diff <= 35) // Hit window
            {
                closestNote.Hit = true;
                PlayCanvas.Children.Remove(closestNote.Visual);
                _notes.Remove(closestNote);
                _notesProcessed++;

                // Sound feedback
                try { System.Media.SystemSounds.Hand.Play(); } catch { }

                if (diff <= 10)
                {
                    ShowFeedback("PERFECT!!!", new SolidColorBrush(Color.FromRgb(16, 185, 129)));
                    _combo++;
                    _score += 15 + _combo * 2;
                    _totalXp += 3 + (_combo > 3 ? 2 : 1);
                }
                else
                {
                    ShowFeedback("GOOD!", new SolidColorBrush(Color.FromRgb(59, 130, 246)));
                    _combo++;
                    _score += 10 + _combo;
                    _totalXp += 2;
                }

                TxtScore.Text = $"Score: {_score}";
                if (_combo > 1)
                {
                    TxtCombo.Text = $"Combo x{_combo}";
                    BorderCombo.Visibility = Visibility.Visible;
                }

                CheckGameEnd();
            }
        }

        private void ShowFeedback(string text, Brush color)
        {
            TxtFeedback.Text = text;
            TxtFeedback.Foreground = color;
            TxtFeedback.Visibility = Visibility.Visible;

            // Simple timer to hide feedback after 500ms
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Tick += (s, e) =>
            {
                TxtFeedback.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }

        private void CheckGameEnd()
        {
            if (_notesProcessed >= MaxNotes && _notes.Count == 0)
            {
                EndGame();
            }
        }

        private void EndGame()
        {
            _isGameRunning = false;
            _gameTimer?.Stop();
            BtnStart.IsEnabled = true;

            OnGameOver?.Invoke(true, _totalXp);
        }
    }
}
