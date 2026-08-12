using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using NAudio.Wave;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using QASmartClass.LearningTools.Models;

using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Multi
{
    using Math = System.Math;

    public partial class NoiseMonitorTool : BaseToolControl, IDisposable
    {
        private bool IsUnitTest => 
            AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.StartsWith("xunit", StringComparison.OrdinalIgnoreCase));

        private WaveInEvent? _waveIn;
        private Window? _parentWindow;
        private bool _isRecording = false;
        private bool _manuallyPaused = false;
        private double _thresholdDb = 75;
        private readonly List<WaveOutEvent> _activeWaveOuts = new();
        
        // Smoothing
        private readonly List<double> _dbHistory = new();
        private const int HistorySize = 5; // smooth over 5 samples

        // Cooldown for "Shhh" sound
        private DateTime _lastShushTime = DateTime.MinValue;

        // Calibration
        private bool _isCalibrating = false;
        private double _calibrationSum = 0;
        private int _calibrationCount = 0;
        private DispatcherTimer? _calibrationTimer;
        private double _calibrationSeconds = 0;

        // Gamification & State
        private string _currentNoiseState = "Green";
        private int _silenceStreak = 0;
        private int _greenSecondsCounter = 0;
        private DispatcherTimer? _streakTimer;
        private bool _hasCelebrated = false;

        public NoiseMonitorTool()
        {
            InitializeComponent();

            Loaded += (_, _) =>
            {
                bool settingsLoaded = LoadToolSettings();
                SetupWindowHotkeys();
                StartStreakTimer();
                LoadPracticalApps();

                if (IsUnitTest) return;

                if (SafeGetMicrophoneDeviceCount() == 0)
                {
                    pnlNoMicrophone.Visibility = Visibility.Visible;
                    pnlCalibration.Visibility = Visibility.Collapsed;
                    txtStatus.Text = "Không tìm thấy Microphone! Hãy cắm micro.";
                    txtStatus.Foreground = Brushes.Red;
                    btnStartStop.Content = "🔄 Thử lại (Quét mic)";
                    btnStartStop.Foreground = Brushes.White;
                    btnStartStop.Background = new SolidColorBrush(Color.FromRgb(30, 136, 229));
                    _isRecording = false;
                }
                else
                {
                    pnlNoMicrophone.Visibility = Visibility.Collapsed;
                    if (!settingsLoaded)
                    {
                        StartCalibration();
                    }
                    else
                    {
                        pnlCalibration.Visibility = Visibility.Collapsed;
                        if (!_isRecording && !_manuallyPaused) StartMicrophone();
                    }
                }

                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Study Guide & Notes";
                if (menuTextRadar != null) menuTextRadar.Text = isVN ? "Đo tiếng ồn" : "Noise Monitor";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                if (sideMenu != null)
                {
                    sideMenu.SelectedIndex = 0; // Default to measuring tab
                }
            };


            Unloaded += (s, e) =>
            {
                CleanupWindowHotkeys();
                Dispose();
            };

            IsVisibleChanged += (s, e) =>
            {
                if (IsUnitTest) return;

                if (!(bool)e.NewValue)
                {
                    StopMicrophone();
                }
                else
                {
                    if (IsLoaded && !_isRecording && !_manuallyPaused && SafeGetMicrophoneDeviceCount() > 0)
                    {
                        StartMicrophone();
                    }
                }
            };
        }

        private void SetupWindowHotkeys()
        {
            _parentWindow = Window.GetWindow(this);
            if (_parentWindow != null)
            {
                _parentWindow.PreviewKeyDown += Window_PreviewKeyDown;
            }
        }

        private void CleanupWindowHotkeys()
        {
            if (_parentWindow != null)
            {
                _parentWindow.PreviewKeyDown -= Window_PreviewKeyDown;
                _parentWindow = null;
            }
        }

        ~NoiseMonitorTool()
        {
            try
            {
                StopMicrophone();
            }
            catch { }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Visibility != Visibility.Visible) return;

            // Space: Toggle Start/Stop Radar
            if (e.Key == Key.Space)
            {
                e.Handled = true;
                if (btnStartStop != null && btnStartStop.IsEnabled)
                {
                    BtnStartStop_Click(this, new RoutedEventArgs());
                }
            }
            // Ctrl + R: Recalibrate
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.R)
            {
                e.Handled = true;
                if (btnCalibrate != null && btnCalibrate.IsEnabled)
                {
                    BtnCalibrate_Click(this, new RoutedEventArgs());
                }
            }
            // Ctrl + S: Toggle Auto-Shush
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.S)
            {
                e.Handled = true;
                if (btnAutoShush != null && btnAutoShush.IsEnabled)
                {
                    btnAutoShush.IsChecked = !btnAutoShush.IsChecked;
                }
            }
        }

        private void StartStreakTimer()
        {
            _streakTimer?.Stop();
            _streakTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _streakTimer.Tick += StreakTimer_Tick;
            _streakTimer.Start();
        }

        private void StreakTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isRecording || _isCalibrating) return;

            if (_currentNoiseState == "Green")
            {
                _greenSecondsCounter++;
                if (_greenSecondsCounter >= 10)
                {
                    _greenSecondsCounter = 0;
                    _silenceStreak++;
                    UpdateStreakUI();
                }
            }
            else if (_currentNoiseState == "Yellow")
            {
                // Freeze counter, do not increase, do not reset.
            }
            else if (_currentNoiseState == "Red")
            {
                // Too loud! Reset streak and counter immediately.
                if (_silenceStreak > 0 || _greenSecondsCounter > 0)
                {
                    _silenceStreak = 0;
                    _greenSecondsCounter = 0;
                    UpdateStreakUI();
                }
            }
        }

        private void UpdateStreakUI()
        {
            if (txtStreak != null)
            {
                txtStreak.Text = $"Chuỗi trật tự: {_silenceStreak} 🔥";
            }
            if (txtStars != null)
            {
                int starCount = _silenceStreak / 5;
                if (starCount > 5) starCount = 5;
                txtStars.Text = new string('⭐', starCount);
            }

            if (_silenceStreak >= 25)
            {
                if (!_hasCelebrated)
                {
                    _hasCelebrated = true;
                    TriggerCelebrationEffect();
                }
            }
            else
            {
                _hasCelebrated = false;
            }
        }

        private void StartCalibration()
        {
            if (_isCalibrating) return;

            _calibrationTimer?.Stop();

            if (SafeGetMicrophoneDeviceCount() == 0)
            {
                pnlCalibration.Visibility = Visibility.Collapsed;
                pnlNoMicrophone.Visibility = Visibility.Visible;
                txtStatus.Text = "Không tìm thấy Microphone! Hãy cắm micro.";
                txtStatus.Foreground = Brushes.Red;
                btnStartStop.Content = "🔄 Thử lại (Quét mic)";
                btnStartStop.Foreground = Brushes.White;
                btnStartStop.Background = new SolidColorBrush(Color.FromRgb(30, 136, 229));
                _isRecording = false;
                return;
            }

            pnlNoMicrophone.Visibility = Visibility.Collapsed;
            pnlCalibration.Visibility = Visibility.Visible;
            _isCalibrating = true;
            _calibrationSum = 0;
            _calibrationCount = 0;
            _calibrationSeconds = 0;
            progCalibration.Value = 0;

            if (btnCalibrate != null) btnCalibrate.IsEnabled = false;
            if (btnStartStop != null) btnStartStop.IsEnabled = false;
            
            if (!_isRecording) StartMicrophone();

            _calibrationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _calibrationTimer.Tick += (s, e) =>
            {
                _calibrationSeconds += 0.1;
                progCalibration.Value = _calibrationSeconds;
                
                if (_calibrationSeconds >= 3.0)
                {
                    _calibrationTimer.Stop();
                    FinishCalibration();
                }
            };
            _calibrationTimer.Start();
        }

        private void FinishCalibration()
        {
            _isCalibrating = false;
            pnlCalibration.Visibility = Visibility.Collapsed;

            if (btnCalibrate != null) btnCalibrate.IsEnabled = true;
            if (btnStartStop != null) btnStartStop.IsEnabled = true;

            if (_calibrationCount > 0)
            {
                double baseNoise = _calibrationSum / _calibrationCount;
                double newThreshold = baseNoise + 20;
                if (newThreshold < 50) newThreshold = 50;
                if (newThreshold > 90) newThreshold = 90;
                sliderThreshold.Value = newThreshold;
                int actualThreshold = (int)sliderThreshold.Value;

                txtStatus.Text = $"Đã định chuẩn! Độ ồn nền: {(int)baseNoise} dB. Ngưỡng báo động đặt ở: {actualThreshold} dB";
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(30, 136, 229));

                var resetTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                resetTimer.Tick += (s, ev) =>
                {
                    resetTimer.Stop();
                    if (_isRecording)
                    {
                        txtStatus.Text = _currentNoiseState == "Green" ? "Lớp học đang rất trật tự" : 
                                         _currentNoiseState == "Yellow" ? "Lớp học bắt đầu ồn..." : "QUÁ ỒN! HÃY TRẬT TỰ!";
                        UpdateVisuals(_dbHistory.Count > 0 ? _dbHistory.Average() : 40);
                    }
                };
                resetTimer.Start();
            }
        }

        private void StartMicrophone()
        {
            if (IsUnitTest) return;

            try
            {
                if (SafeGetMicrophoneDeviceCount() == 0)
                {
                    pnlNoMicrophone.Visibility = Visibility.Visible;
                    txtStatus.Text = "Không tìm thấy Microphone! Hãy cắm micro.";
                    txtStatus.Foreground = Brushes.Red;
                    btnStartStop.Content = "🔄 Thử lại (Quét mic)";
                    btnStartStop.Foreground = Brushes.White;
                    btnStartStop.Background = new SolidColorBrush(Color.FromRgb(30, 136, 229));
                    _isRecording = false;
                    return;
                }

                pnlNoMicrophone.Visibility = Visibility.Collapsed;
                _waveIn = new WaveInEvent();
                _waveIn.WaveFormat = new WaveFormat(8000, 16, 1);
                _waveIn.DataAvailable += WaveIn_DataAvailable;
                _waveIn.RecordingStopped += WaveIn_RecordingStopped;
                _waveIn.StartRecording();
                _isRecording = true;

                btnStartStop.Content = "⏸ Tạm Dừng Radar";
                btnStartStop.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47)); // Red
                btnStartStop.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
            }
            catch (UnauthorizedAccessException)
            {
                txtStatus.Text = "Lỗi quyền Microphone! Hãy bật quyền trong Cài đặt Windows.";
                txtStatus.Foreground = Brushes.Red;
                _isRecording = false;
                MessageBox.Show("Lỗi quyền truy cập Microphone!\n\nVui lòng cho phép ứng dụng truy cập Microphone trong cài đặt Windows (Cài đặt > Quyền riêng tư > Micrô) và thử lại.", "Lỗi quyền riêng tư", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2147024891)
                {
                    txtStatus.Text = "Lỗi quyền Microphone! Hãy bật quyền trong Cài đặt Windows.";
                    txtStatus.Foreground = Brushes.Red;
                    _isRecording = false;
                    MessageBox.Show("Lỗi quyền truy cập Microphone!\n\nVui lòng cho phép ứng dụng truy cập Microphone trong cài đặt Windows (Cài đặt > Quyền riêng tư > Micrô) và thử lại.", "Lỗi quyền riêng tư", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    txtStatus.Text = "Lỗi Microphone: " + ex.Message;
                    txtStatus.Foreground = Brushes.Red;
                    _isRecording = false;
                }
            }
        }

        private void WaveIn_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                Dispatcher.InvokeAsync(() =>
                {
                    StopMicrophone();
                    txtStatus.Text = "Mất kết nối Microphone!";
                    txtStatus.Foreground = Brushes.Red;
                    btnStartStop.Content = "🔄 Thử lại (Quét mic)";
                    btnStartStop.Foreground = Brushes.White;
                    btnStartStop.Background = new SolidColorBrush(Color.FromRgb(30, 136, 229));
                });
            }
        }

        private void StopMicrophone()
        {
            if (_waveIn != null)
            {
                _waveIn.DataAvailable -= WaveIn_DataAvailable;
                _waveIn.RecordingStopped -= WaveIn_RecordingStopped;
                try
                {
                    _waveIn.StopRecording();
                }
                catch { /* Ignore if already stopped or failed */ }
                try
                {
                    _waveIn.Dispose();
                }
                catch { /* Ignore */ }
                _waveIn = null;
            }
            _isRecording = false;
            
            btnStartStop.Content = "▶ Bật Radar";
            btnStartStop.Foreground = new SolidColorBrush(Color.FromRgb(56, 142, 60)); // Green
            btnStartStop.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
            
            txtStatus.Text = "Radar đang tạm dừng";
            txtStatus.Foreground = Brushes.Gray;
            txtDecibel.Text = "-- dB";
            
            // Reset UI to idle
            UpdateVisuals(40);
        }

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            try
            {
                double sumSquares = 0;
                int sampleCount = e.BytesRecorded / 2;
                if (sampleCount <= 0) return;

                int safeLimit = e.BytesRecorded - (e.BytesRecorded % 2);
                for (int i = 0; i < safeLimit; i += 2)
                {
                    short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i + 0]);
                    double sample32 = sample / 32768.0;
                    sumSquares += sample32 * sample32;
                }

                double rms = System.Math.Sqrt(sumSquares / sampleCount);
                double decibels = 35;
                if (rms > 0)
                {
                    decibels = 100 + (20 * System.Math.Log10(rms));
                }

                if (decibels < 35) decibels = 35; // noise floor

                _dbHistory.Add(decibels);
                if (_dbHistory.Count > HistorySize)
                    _dbHistory.RemoveAt(0);

                double smoothDb = _dbHistory.Average();

                if (_isCalibrating)
                {
                    _calibrationSum += smoothDb;
                    _calibrationCount++;
                    Dispatcher.InvokeAsync(() => { txtCalibrationLevel.Text = $"Đang đo: {(int)smoothDb} dB"; }, DispatcherPriority.Render);
                    return;
                }

                Dispatcher.InvokeAsync(() =>
                {
                    if (!_isRecording) return;
                    
                    txtDecibel.Text = $"{(int)smoothDb} dB";
                    UpdateVisuals(smoothDb);
                    CheckThresholdAndWarn(smoothDb);
                }, DispatcherPriority.Render);
            }
            catch { /* Prevent crashes on sudden device removal during callback */ }
        }

        private void UpdateVisuals(double db)
        {
            double scale = (db - 40) / 60.0;
            if (scale < 0) scale = 0;
            if (scale > 1) scale = 1;

            double warningMargin = 8.0;
            if (db < _thresholdDb - warningMargin)
            {
                // Green - Quiet
                _currentNoiseState = "Green";
                FaceBg.Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                txtFace.Text = "😀";
                if (txtStatus.Foreground.ToString() != "#FF1E88E5")
                {
                    txtStatus.Text = "Lớp học đang rất trật tự";
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                }
                
                scaleWave1.ScaleX = scaleWave1.ScaleY = 0.9 + (scale * 0.25);
                scaleWave2.ScaleX = scaleWave2.ScaleY = 0.73 + (scale * 0.27);
                scaleWave3.ScaleX = scaleWave3.ScaleY = 0.65 + (scale * 0.25);

                // Reset shadow color to green when in safe state
                if (FaceBg.Effect is DropShadowEffect dse)
                    dse.Color = Color.FromRgb(76, 175, 80);
            }
            else if (db < _thresholdDb)
            {
                // Yellow - Warning
                _currentNoiseState = "Yellow";
                FaceBg.Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                txtFace.Text = "😐";
                if (txtStatus.Foreground.ToString() != "#FF1E88E5")
                {
                    txtStatus.Text = "Lớp học bắt đầu ồn...";
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                }

                scaleWave1.ScaleX = scaleWave1.ScaleY = 1.0 + (scale * 0.4);
                scaleWave2.ScaleX = scaleWave2.ScaleY = 0.86 + (scale * 0.33);
                scaleWave3.ScaleX = scaleWave3.ScaleY = 0.8 + (scale * 0.3);
                
                if (FaceBg.Effect is DropShadowEffect dse)
                    dse.Color = Color.FromRgb(255, 152, 0);
            }
            else
            {
                // Red - Too loud
                _currentNoiseState = "Red";
                FaceBg.Fill = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                txtFace.Text = "😡";
                if (txtStatus.Foreground.ToString() != "#FF1E88E5")
                {
                    txtStatus.Text = "QUÁ ỒN! HÃY TRẬT TỰ!";
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                }

                scaleWave1.ScaleX = scaleWave1.ScaleY = 1.25 + (scale * 0.25);
                scaleWave2.ScaleX = scaleWave2.ScaleY = 1.06 + (scale * 0.2);
                scaleWave3.ScaleX = scaleWave3.ScaleY = 1.0 + (scale * 0.1);

                if (FaceBg.Effect is DropShadowEffect dse)
                    dse.Color = Color.FromRgb(244, 67, 54);
            }

            // Apply Dynamic ripple fade-out opacity
            Wave1.Opacity = 0.6 * (1.0 - scale * 0.4);
            Wave2.Opacity = 0.4 * (1.0 - scale * 0.6);
            Wave3.Opacity = 0.2 * (1.0 - scale * 0.8);

            // Sync wave colors with face
            var brush = FaceBg.Fill;
            Wave1.Stroke = brush;
            Wave2.Stroke = brush;
            Wave3.Stroke = brush;
        }

        private void CheckThresholdAndWarn(double db)
        {
            if (db >= _thresholdDb)
            {
                if ((DateTime.Now - _lastShushTime).TotalSeconds > 5)
                {
                    _lastShushTime = DateTime.Now;
                    if (btnAutoShush.IsChecked == true)
                    {
                        PlayShushSound();
                    }
                    LogAlertToDb(db);
                }
            }
        }

        private void PlayShushSound()
        {
            try
            {
                var waveOut = new WaveOutEvent();
                var shushProvider = new ShushSampleProvider();
                waveOut.Init(shushProvider);
                
                lock (_activeWaveOuts)
                {
                    _activeWaveOuts.Add(waveOut);
                }
                
                waveOut.Play();
                waveOut.PlaybackStopped += (s, e) =>
                {
                    lock (_activeWaveOuts)
                    {
                        _activeWaveOuts.Remove(waveOut);
                    }
                    waveOut.Dispose();
                };
            }
            catch (Exception)
            {
                System.Media.SystemSounds.Asterisk.Play();
            }
        }

        private void SliderThreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtThreshold != null)
            {
                _thresholdDb = e.NewValue;
                txtThreshold.Text = $"{(int)_thresholdDb} dB";
                if (IsLoaded)
                {
                    SaveToolSettings();
                }
            }
        }

        private void BtnStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (_isRecording)
            {
                _manuallyPaused = true;
                StopMicrophone();
            }
            else
            {
                _manuallyPaused = false;
                StartMicrophone();
            }
        }

        private void BtnCalibrate_Click(object sender, RoutedEventArgs e)
        {
            StartCalibration();
        }

        private void BtnAutoShush_Checked(object sender, RoutedEventArgs e)
        {
            if (btnAutoShush == null) return;
            btnAutoShush.Content = "🔊 Tự động Suỵt: BẬT";
            btnAutoShush.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)); // #E8F5E9
            btnAutoShush.Foreground = new SolidColorBrush(Color.FromRgb(56, 142, 60));   // #388E3C
            if (IsLoaded) SaveToolSettings();
        }

        private void BtnAutoShush_Unchecked(object sender, RoutedEventArgs e)
        {
            if (btnAutoShush == null) return;
            btnAutoShush.Content = "🔇 Tự động Suỵt: TẮT";
            btnAutoShush.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238)); // #FFEBEE
            btnAutoShush.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47));   // #D32F2F
            if (IsLoaded) SaveToolSettings();
        }

        private void SaveToolSettings()
        {
            try
            {
                var settings = new NoiseToolSettings
                {
                    ThresholdDb = _thresholdDb,
                    AutoShush = btnAutoShush.IsChecked ?? true
                };

                string settingsDir = QASmartClass.Services.AppPaths.SettingsDir;
                Directory.CreateDirectory(settingsDir);
                string path = Path.Combine(settingsDir, "noise_monitor_settings.json");

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("SaveToolSettings failed: {Error}", ex.Message);
            }
        }

        private bool LoadToolSettings()
        {
            try
            {
                string settingsDir = QASmartClass.Services.AppPaths.SettingsDir;
                string path = Path.Combine(settingsDir, "noise_monitor_settings.json");

                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var settings = JsonSerializer.Deserialize<NoiseToolSettings>(json);
                    if (settings != null)
                    {
                        _thresholdDb = settings.ThresholdDb;
                        if (sliderThreshold != null)
                        {
                            sliderThreshold.Value = _thresholdDb;
                        }
                        if (btnAutoShush != null)
                        {
                            btnAutoShush.IsChecked = settings.AutoShush;
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("LoadToolSettings failed: {Error}", ex.Message);
            }
            return false;
        }

        private void LogCelebrationToDb()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    using (var db = new QASmartClass.Data.AppDbContext())
                    {
                        var log = new QASmartClass.Data.EventLog
                        {
                            EventType = "NOISE_RADAR_CELEBRATION",
                            Actor = "NoiseRadar",
                            Details = $"Lớp học duy trì trật tự xuất sắc đạt chuỗi {_silenceStreak} 🔥",
                            Timestamp = DateTime.Now
                        };
                        db.EventLogs.Add(log);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning("LogCelebrationToDb failed: {Error}", ex.Message);
                }
            });
        }

        private void LogAlertToDb(double dbLevel)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    using (var db = new QASmartClass.Data.AppDbContext())
                    {
                        var log = new QASmartClass.Data.EventLog
                        {
                            EventType = "NOISE_RADAR_ALERT",
                            Actor = "NoiseRadar",
                            Details = $"Tiếng ồn vượt ngưỡng: {(int)dbLevel} dB (Ngưỡng: {(int)_thresholdDb} dB)",
                            Timestamp = DateTime.Now
                        };
                        db.EventLogs.Add(log);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning("LogAlertToDb failed: {Error}", ex.Message);
                }
            });
        }

        private void TriggerCelebrationEffect()
        {
            if (pnlCelebration == null || scaleCelebration == null) return;

            PlayCelebrationSound();
            LogCelebrationToDb();

            pnlCelebration.Visibility = Visibility.Visible;

            var opacityAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(500)
            };

            var scaleAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new System.Windows.Media.Animation.BackEase
                {
                    Amplitude = 0.3,
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
                }
            };

            pnlCelebration.BeginAnimation(OpacityProperty, opacityAnim);
            scaleCelebration.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            scaleCelebration.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            timer.Tick += (s, ev) =>
            {
                timer.Stop();
                var fadeOut = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(500)
                };
                fadeOut.Completed += (sender, args) =>
                {
                    pnlCelebration.Visibility = Visibility.Collapsed;
                };
                pnlCelebration.BeginAnimation(OpacityProperty, fadeOut);
            };
            timer.Start();
        }

        private int SafeGetMicrophoneDeviceCount()
        {
            try
            {
                return WaveInEvent.DeviceCount;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error checking microphone device count via NAudio");
                return 0;
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewPractice == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewPractice.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            if (sideMenu.SelectedIndex == 2) // Tab Ứng dụng thực tế
            {
                StopMicrophone();
                
                viewPractical.Visibility = Visibility.Visible;
            }
            else
            {
                

                if (sideMenu.SelectedIndex == 0)
                {
                    StopMicrophone();
                    viewGuide.Visibility = Visibility.Visible;
                }
                else if (sideMenu.SelectedIndex == 1)
                {
                    if (IsLoaded && !_isRecording && !_manuallyPaused && SafeGetMicrophoneDeviceCount() > 0)
                    {
                        StartMicrophone();
                    }
                    viewPractice.Visibility = Visibility.Visible;
                }
            }
        }

        private void BtnRetryMic_Click(object sender, RoutedEventArgs e)
        {
            if (SafeGetMicrophoneDeviceCount() > 0)
            {
                pnlNoMicrophone.Visibility = Visibility.Collapsed;
                StartCalibration();
            }
            else
            {
                txtStatus.Text = "Không tìm thấy Microphone! Hãy cắm micro.";
                txtStatus.Foreground = Brushes.Red;
            }
        }

        private void PlayCelebrationSound()
        {
            if (btnAutoShush == null) return;
            try
            {
                var waveOut = new WaveOutEvent();
                var chimeProvider = new CelebrationChimeSampleProvider();
                waveOut.Init(chimeProvider);
                
                lock (_activeWaveOuts)
                {
                    _activeWaveOuts.Add(waveOut);
                }
                
                waveOut.Play();
                waveOut.PlaybackStopped += (s, e) => 
                {
                    lock (_activeWaveOuts)
                    {
                        _activeWaveOuts.Remove(waveOut);
                    }
                    waveOut.Dispose();
                };
            }
            catch { /* Ignore audio output errors */ }
        }

        public void Dispose()
        {
            _streakTimer?.Stop();
            _calibrationTimer?.Stop();
            StopMicrophone();
            
            lock (_activeWaveOuts)
            {
                foreach (var waveOut in _activeWaveOuts)
                {
                    try
                    {
                        waveOut.Stop();
                        waveOut.Dispose();
                    }
                    catch { }
                }
                _activeWaveOuts.Clear();
            }
        }

        private void LoadPracticalApps()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            string suffix = isVN ? "VN" : "EN";

            var items = new System.Collections.Generic.List<PracticalAppItem>
            {
                new PracticalAppItem
                {
                    Icon = "🚦",
                    Title = isVN ? "Tự Quản Học Đường & Rèn Luyện Ý Thức" : "Self-Regulated Learning Environment",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_noise_monitor_1_{suffix}.png",
                    Description = isVN 
                        ? "Học sinh tự theo dõi đồng hồ đo tiếng ồn để tự giác điều chỉnh âm lượng thảo luận nhóm, rèn luyện kỹ năng làm việc tập thể văn minh mà không cần giáo viên nhắc nhở." 
                        : "Students monitor the noise level meter to self-regulate their group discussion volume, practicing civilized teamwork skills without constant teacher reminders."
                },
                new PracticalAppItem
                {
                    Icon = "🤫",
                    Title = isVN ? "Quản Lý Thư Viện & Phòng Thi" : "Library & Exam Hall Quiet Zones",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_noise_monitor_2_{suffix}.png",
                    Description = isVN 
                        ? "Hệ thống tự động phát hiện và cảnh báo tức thì khi mức âm thanh vượt quá 50 dB, đảm bảo không gian yên tĩnh tuyệt đối cho việc đọc sách, tự học và tập trung làm bài thi." 
                        : "The system automatically detects and alerts instantly when sound level exceeds 50 dB, ensuring absolute silence for reading, self-study, and exams."
                },
                new PracticalAppItem
                {
                    Icon = "🔊",
                    Title = isVN ? "Thử Nghiệm Khoa Học & Âm Học" : "Acoustic Calibration & Science Lab",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_noise_monitor_3_{suffix}.png",
                    Description = isVN 
                        ? "Sử dụng cảm biến âm thanh để làm quen với đơn vị đo decibel (dB), tìm hiểu mối liên hệ giữa các nguồn âm trong phòng và học cách định chuẩn (calibrate) thiết bị đo đạc." 
                        : "Use the sound sensor to get familiar with decibel (dB) units, study the relationship between sound sources, and learn how to calibrate measuring equipment."
                }
            };

            try
            {
                if (practicalAppViewer != null) practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for NoiseMonitorTool: {Err}", ex.Message);
            }
        }
    }

    public class ShushSampleProvider : ISampleProvider
    {
        private readonly Random _random = new Random();
        private readonly int _sampleRate;
        private readonly double _durationSeconds;
        private int _currentSample = 0;
        private readonly int _totalSamples;
        private float _lastNoise = 0f;
        private float _lastLowPass = 0f;

        public WaveFormat WaveFormat { get; }

        public ShushSampleProvider(int sampleRate = 44100, double durationSeconds = 1.2)
        {
            _sampleRate = sampleRate;
            _durationSeconds = durationSeconds;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
            _totalSamples = (int)(sampleRate * durationSeconds);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesToRead = System.Math.Min(count, _totalSamples - _currentSample);
            for (int i = 0; i < samplesToRead; i++)
            {
                float noise = (float)(_random.NextDouble() * 2.0 - 1.0);
                
                float highPass = noise - _lastNoise;
                _lastNoise = noise;
                
                _lastLowPass = 0.7f * _lastLowPass + 0.3f * highPass;
                float filtered = _lastLowPass;

                double t = (double)(_currentSample + i) / _sampleRate;
                float envelope = 0;
                if (t < 0.15)
                    envelope = (float)(t / 0.15);
                else if (t < 0.8)
                    envelope = 1.0f;
                else
                    envelope = (float)(1.0 - (t - 0.8) / (_durationSeconds - 0.8));

                if (envelope < 0) envelope = 0;

                buffer[offset + i] = filtered * envelope * 0.12f;
            }
            _currentSample += samplesToRead;
            return samplesToRead;
        }
    }

    public class NoiseToolSettings
    {
        public double ThresholdDb { get; set; } = 75;
        public bool AutoShush { get; set; } = true;
    }

    public class CelebrationChimeSampleProvider : ISampleProvider
    {
        private readonly int _sampleRate;
        private readonly double _durationSeconds;
        private int _currentSample = 0;
        private readonly int _totalSamples;

        public WaveFormat WaveFormat { get; }

        public CelebrationChimeSampleProvider(int sampleRate = 44100, double durationSeconds = 0.6)
        {
            _sampleRate = sampleRate;
            _durationSeconds = durationSeconds;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
            _totalSamples = (int)(sampleRate * durationSeconds);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesToRead = System.Math.Min(count, _totalSamples - _currentSample);
            for (int i = 0; i < samplesToRead; i++)
            {
                double t = (double)(_currentSample + i) / _sampleRate;
                
                // 3 notes ascending: C5 (523.25 Hz), E5 (659.25 Hz), G5 (783.99 Hz)
                double freq = 523.25;
                double noteStart = 0.0;
                double noteDuration = 0.2;

                if (t < 0.2)
                {
                    freq = 523.25;
                    noteStart = 0.0;
                }
                else if (t < 0.4)
                {
                    freq = 659.25;
                    noteStart = 0.2;
                }
                else
                {
                    freq = 783.99;
                    noteStart = 0.4;
                }

                double noteT = t - noteStart;
                
                // Sine wave
                double val = System.Math.Sin(2.0 * System.Math.PI * freq * noteT);
                
                // Envelope for each note: quick attack (20ms), gradual decay/release
                double envelope = 0;
                if (noteT < 0.02)
                {
                    envelope = noteT / 0.02;
                }
                else if (noteT < noteDuration)
                {
                    envelope = 1.0 - (noteT - 0.02) / (noteDuration - 0.02);
                }

                if (envelope < 0) envelope = 0;

                buffer[offset + i] = (float)(val * envelope * 0.08); // soft volume
            }
            _currentSample += samplesToRead;
            return samplesToRead;
        }
    }
}