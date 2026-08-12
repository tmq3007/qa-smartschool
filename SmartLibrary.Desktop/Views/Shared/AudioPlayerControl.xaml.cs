using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SmartLibrary.Desktop.Views.Shared;

public partial class AudioPlayerControl : UserControl
{
    private readonly MediaPlayer _mediaPlayer = new();
    private readonly DispatcherTimer _timer = new();
    private bool _isUserDragging = false;
    private bool _isPlaying = false;
    private bool _isMuted = false;
    private double _preMuteVolume = 0.5;

    public AudioPlayerControl()
    {
        InitializeComponent();

        _timer.Interval = TimeSpan.FromMilliseconds(250);
        _timer.Tick += Timer_Tick;

        _mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
        _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

        // Set default volume
        _mediaPlayer.Volume = SliderVolume.Value;
    }

    public void Play(string url, string title)
    {
        try
        {
            TxtTitle.Text = title;
            TxtStatus.Text = "Đang tải...";
            
            _mediaPlayer.Open(new Uri(url));
            _mediaPlayer.Play();
            _isPlaying = true;
            BtnPlay.Content = "⏸";
            _timer.Start();
        }
        catch (Exception ex)
        {
            TxtStatus.Text = "Lỗi phát âm thanh";
            MessageBox.Show($"Không thể phát audiobook: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MediaPlayer_MediaOpened(object? sender, EventArgs e)
    {
        TxtStatus.Text = "Đang phát";
        if (_mediaPlayer.NaturalDuration.HasTimeSpan)
        {
            SliderProgress.Maximum = _mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
            UpdateTimerText();
        }
    }

    private void MediaPlayer_MediaEnded(object? sender, EventArgs e)
    {
        _isPlaying = false;
        BtnPlay.Content = "▶";
        _timer.Stop();
        SliderProgress.Value = 0;
        _mediaPlayer.Position = TimeSpan.Zero;
        UpdateTimerText();
        TxtStatus.Text = "Đã phát xong";
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (!_isUserDragging && _mediaPlayer.NaturalDuration.HasTimeSpan)
        {
            SliderProgress.Value = _mediaPlayer.Position.TotalSeconds;
            UpdateTimerText();
        }
    }

    private void UpdateTimerText()
    {
        if (_mediaPlayer.NaturalDuration.HasTimeSpan)
        {
            var cur = _mediaPlayer.Position;
            var total = _mediaPlayer.NaturalDuration.TimeSpan;
            TxtTime.Text = $"{FormatTime(cur)} / {FormatTime(total)}";
        }
        else
        {
            TxtTime.Text = "00:00 / 00:00";
        }
    }

    private string FormatTime(TimeSpan span)
    {
        return $"{(int)span.TotalMinutes:00}:{span.Seconds:00}";
    }

    private void BtnPlay_Click(object sender, RoutedEventArgs e)
    {
        if (_isPlaying)
        {
            _mediaPlayer.Pause();
            _isPlaying = false;
            BtnPlay.Content = "▶";
            TxtStatus.Text = "Đang tạm dừng";
        }
        else
        {
            _mediaPlayer.Play();
            _isPlaying = true;
            BtnPlay.Content = "⏸";
            TxtStatus.Text = "Đang phát";
            _timer.Start();
        }
    }

    private void BtnMute_Click(object sender, RoutedEventArgs e)
    {
        if (_isMuted)
        {
            _mediaPlayer.Volume = _preMuteVolume;
            SliderVolume.Value = _preMuteVolume;
            BtnMute.Content = "🔊";
            _isMuted = false;
        }
        else
        {
            _preMuteVolume = SliderVolume.Value;
            _mediaPlayer.Volume = 0;
            SliderVolume.Value = 0;
            BtnMute.Content = "🔇";
            _isMuted = true;
        }
    }

    private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _mediaPlayer.Volume = e.NewValue;
        if (e.NewValue > 0)
        {
            BtnMute.Content = "🔊";
            _isMuted = false;
        }
        else
        {
            BtnMute.Content = "🔇";
            _isMuted = true;
        }
    }

    private void SliderProgress_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
    {
        _isUserDragging = true;
    }

    private void SliderProgress_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
    {
        _isUserDragging = false;
        _mediaPlayer.Position = TimeSpan.FromSeconds(SliderProgress.Value);
        UpdateTimerText();
    }
}
