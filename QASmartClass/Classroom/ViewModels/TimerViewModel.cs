using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using QASmartClass.Classroom.Services;
using QASmartTouch.Services;

namespace QASmartClass.Classroom.ViewModels
{
    public partial class TimerViewModel : ObservableObject
    {
        // ─── COUNTDOWN ──────────────────────────────────────────────
        private readonly DispatcherTimer _countdownTimer = new();
        private int _secondsLeft = 300;

        [ObservableProperty]
        private string _countdownDisplay = "05:00";

        [ObservableProperty]
        private string _startTimerButtonText = "▶️ Bắt đầu";

        [ObservableProperty]
        private bool _isCountdownRunning;

        [ObservableProperty]
        private bool _isTimeUp;

        [ObservableProperty]
        private bool _isSessionActive;

        // ─── STOPWATCH ──────────────────────────────────────────────
        private readonly DispatcherTimer _stopwatchTimer = new();
        private DateTime _stopwatchStart;
        private TimeSpan _stopwatchElapsed = TimeSpan.Zero;
        private TimeSpan _stopwatchPaused = TimeSpan.Zero;

        [ObservableProperty]
        private string _stopwatchDisplay = "00:00.000";

        [ObservableProperty]
        private string _startStopwatchButtonText = "▶️ Bắt đầu";

        [ObservableProperty]
        private bool _isStopwatchRunning;

        [ObservableProperty]
        private ObservableCollection<string> _laps = new();

        public TimerViewModel()
        {
            // Countdown timer setup
            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += CountdownTimer_Tick;

            // Stopwatch timer setup
            _stopwatchTimer.Interval = TimeSpan.FromMilliseconds(37);
            _stopwatchTimer.Tick += StopwatchTimer_Tick;

            // Session active sync
            var app = Application.Current as QASmartTouch.App;
            if (app?.ClassroomSession != null)
            {
                IsSessionActive = app.ClassroomSession.IsSessionActive;
                app.ClassroomSession.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ClassroomSessionService.IsSessionActive))
                    {
                        IsSessionActive = app.ClassroomSession.IsSessionActive;
                    }
                };
            }

            // Load last countdown settings
            _secondsLeft = AppSettings.LastCountdownSeconds > 0 ? AppSettings.LastCountdownSeconds : 300;
            UpdateCountdownDisplay();
        }

        // ─── COUNTDOWN COMMANDS ─────────────────────────────────────

        [RelayCommand]
        private void StartTimer()
        {
            IsCountdownRunning = !IsCountdownRunning;
            if (IsCountdownRunning)
            {
                IsTimeUp = false;
                if (_secondsLeft <= 0) _secondsLeft = AppSettings.LastCountdownSeconds > 0 ? AppSettings.LastCountdownSeconds : 300;
                
                AppSettings.LastCountdownSeconds = _secondsLeft;
                AppSettings.Save();

                _countdownTimer.Start();
                StartTimerButtonText = "⏸️ Tạm dừng";
                Log.Information("Countdown started: {Sec}s", _secondsLeft);
                BroadcastTimerCommand($"CMD|TIMER_START|{_secondsLeft}");
            }
            else
            {
                _countdownTimer.Stop();
                UpdateCountdownDisplay();
                StartTimerButtonText = "▶️ Tiếp tục";
                Log.Information("Countdown paused at {Sec}s", _secondsLeft);
                BroadcastTimerCommand("CMD|TIMER_STOP");
            }
        }

        [RelayCommand]
        private void ResetTimer()
        {
            _countdownTimer.Stop();
            IsCountdownRunning = false;
            IsTimeUp = false;
            _secondsLeft = AppSettings.LastCountdownSeconds > 0 ? AppSettings.LastCountdownSeconds : 300;
            _blinkToggle = false;
            UpdateCountdownDisplay();
            StartTimerButtonText = "▶️ Bắt đầu";
            Log.Information("Countdown reset");
            BroadcastTimerCommand("CMD|TIMER_STOP");
        }

        [RelayCommand]
        private void SetTimer(string secondsStr)
        {
            if (int.TryParse(secondsStr, out int s))
            {
                IsTimeUp = false;
                _secondsLeft = s;
                UpdateCountdownDisplay();
                Log.Information("Timer preset: {Seconds}s", s);

                AppSettings.LastCountdownSeconds = _secondsLeft;
                AppSettings.Save();

                // Sync with students if the timer changes
                if (!IsCountdownRunning)
                {
                    BroadcastTimerCommand($"CMD|TIMER_START|{_secondsLeft}");
                }
            }
        }

        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            if (_secondsLeft > 0)
            {
                _secondsLeft--;
                _blinkToggle = !_blinkToggle;
                UpdateCountdownDisplay();
                
                if (_secondsLeft == 0)
                {
                    _countdownTimer.Stop();
                    IsCountdownRunning = false;
                    IsTimeUp = true;
                    StartTimerButtonText = "▶️ Bắt đầu";
                    
                    try
                    {
                        System.Media.SystemSounds.Exclamation.Play();
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Failed to play timer finish chime: {Err}", ex.Message);
                    }
                    
                    Log.Information("Countdown finished");
                    BroadcastTimerCommand("CMD|TIMER_STOP");
                }
            }
        }

        private bool _blinkToggle;

        private void UpdateCountdownDisplay()
        {
            string separator = (IsCountdownRunning && !_blinkToggle) ? " " : ":";
            CountdownDisplay = $"{_secondsLeft / 60:D2}{separator}{_secondsLeft % 60:D2}";
        }

        // ─── STOPWATCH COMMANDS ─────────────────────────────────────

        [RelayCommand]
        private void StartStopwatch()
        {
            IsStopwatchRunning = !IsStopwatchRunning;
            if (IsStopwatchRunning)
            {
                _stopwatchStart = DateTime.Now;
                _stopwatchTimer.Start();
                StartStopwatchButtonText = "⏸️ Tạm dừng";
                Log.Information("Stopwatch started");
            }
            else
            {
                _stopwatchTimer.Stop();
                _stopwatchPaused = _stopwatchElapsed;
                StartStopwatchButtonText = "▶️ Tiếp tục";
                Log.Information("Stopwatch paused at {Elapsed}", _stopwatchElapsed);
            }
        }

        [RelayCommand]
        private void LapStopwatch()
        {
            if (!IsStopwatchRunning && _stopwatchElapsed == TimeSpan.Zero) return;

            var ts = _stopwatchElapsed;
            var lapText = ts.TotalHours >= 1
                ? $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}"
                : $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";

            Laps.Insert(0, $"Lần {Laps.Count + 1}: {lapText}");
            Log.Information("Lap {N}: {Time}", Laps.Count, lapText);
        }

        [RelayCommand]
        private void StopStopwatch()
        {
            _stopwatchTimer.Stop();
            IsStopwatchRunning = false;
            _stopwatchPaused = TimeSpan.Zero;
            _stopwatchElapsed = TimeSpan.Zero;
            Laps.Clear();
            UpdateStopwatchDisplay();
            StartStopwatchButtonText = "▶️ Bắt đầu";
            Log.Information("Stopwatch reset");
        }

        private void StopwatchTimer_Tick(object? sender, EventArgs e)
        {
            _stopwatchElapsed = _stopwatchPaused + (DateTime.Now - _stopwatchStart);
            UpdateStopwatchDisplay();
        }

        private void UpdateStopwatchDisplay()
        {
            var ts = _stopwatchElapsed;
            StopwatchDisplay = ts.TotalHours >= 1
                ? $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}"
                : $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
        }

        private void BroadcastTimerCommand(string cmd)
        {
            try
            {
                var app = Application.Current as QASmartTouch.App;
                if (app != null)
                {
                    var net = app.NetworkService;
                    if (net?.IsBroadcasting == true)
                    {
                        _ = net.SendCommandAsync(cmd);
                    }
                    else
                    {
                        app.RaiseLocalCommand(cmd);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to broadcast timer command: {Err}", ex.Message);
            }
        }
    }
}
