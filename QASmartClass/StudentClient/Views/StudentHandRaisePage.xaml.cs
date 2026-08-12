using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Threading.Tasks;
using Serilog;
using QASmartClass.Utilities;

namespace QASmartClass.StudentClient.Views
{
    public partial class StudentHandRaisePage : Page
    {
        private bool _isProcessing = false;
        private bool _isSendingQuestion = false;
        private System.ComponentModel.PropertyChangedEventHandler? _propertyChangedHandler;

        // Thêm trường hỗ trợ debounce và cooldown (Phase 8)
        private DateTime _lastClickTime = DateTime.MinValue;
        private System.Windows.Threading.DispatcherTimer? _dbDebounceTimer;
        private bool _pendingTargetState = false;
        private string _pendingReason = "";
        private bool _networkSendPending = false;

        public StudentHandRaisePage()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                var app = (QASmartTouch.App)Application.Current;
                btnRaiseHand.ApplyTemplate();
                UpdateHandRaiseUI(app.StudentNetwork.IsHandRaised);

                _propertyChangedHandler = (sender, args) =>
                {
                    if (args.PropertyName == "IsHandRaised")
                    {
                        Dispatcher.Invoke(() => UpdateHandRaiseUI(app.StudentNetwork.IsHandRaised));
                    }
                };
                app.StudentNetwork.PropertyChanged += _propertyChangedHandler;
            };

            Unloaded += (s, e) =>
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.StudentNetwork != null && _propertyChangedHandler != null)
                {
                    app.StudentNetwork.PropertyChanged -= _propertyChangedHandler;
                }
            };
        }

        private void UpdateHandRaiseUI(bool raised)
        {
            if (cbReason != null)
            {
                cbReason.IsEnabled = !raised;
            }

            var circle = btnRaiseHand.Template.FindName("circle", btnRaiseHand) as Border;
            var handIcon = btnRaiseHand.Template.FindName("handIcon", btnRaiseHand) as TextBlock;
            var handLabel = btnRaiseHand.Template.FindName("handLabel", btnRaiseHand) as TextBlock;

            if (raised)
            {
                txtStatus.Text = "🖐️ Đang giơ tay — GV sẽ thấy em!";
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0)); // #E65100
                txtStatus.FontWeight = FontWeights.Bold;

                if (circle != null)
                {
                    circle.BorderBrush = new SolidColorBrush(Color.FromRgb(230, 81, 0));
                    circle.Background = new LinearGradientBrush(
                        Color.FromRgb(255, 243, 224), // #FFF3E0
                        Color.FromRgb(255, 224, 178), // #FFE0B2
                        new Point(0, 0), new Point(1, 1)
                    );
                }
                if (handLabel != null)
                {
                    handLabel.Text = "Đang Giơ Tay";
                    handLabel.Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0));
                }
                if (handIcon != null)
                {
                    handIcon.Text = "🙋";
                }
            }
            else
            {
                txtStatus.Text = "Chưa giơ tay";
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)); // #64748B
                txtStatus.FontWeight = FontWeights.Normal;

                if (circle != null)
                {
                    circle.BorderBrush = new SolidColorBrush(Color.FromRgb(26, 115, 232)); // #1A73E8
                    circle.Background = new LinearGradientBrush(
                        Color.FromRgb(232, 240, 254), // #E8F0FE
                        Color.FromRgb(210, 227, 252), // #D2E3FC
                        new Point(0, 0), new Point(1, 1)
                    );
                }
                if (handLabel != null)
                {
                    handLabel.Text = "Click để Giơ tay";
                    handLabel.Foreground = new SolidColorBrush(Color.FromRgb(26, 115, 232));
                }
                if (handIcon != null)
                {
                    handIcon.Text = "✋";
                }
            }
        }

        private async Task SendNetworkHandRaiseAsync(bool targetState, string cleanReason)
        {
            var app = (QASmartTouch.App)Application.Current;
            var client = app.StudentNetwork;
            var net = app.NetworkService;

            if (client.IsConnected)
            {
                await client.SendHandRaiseWithReason(targetState, cleanReason);
            }
            else if (net != null && net.IsBroadcasting)
            {
                string classCode = client.ClassCode;
                if (string.IsNullOrEmpty(classCode)) classCode = "DEFAULT_CLASS";
                
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (client.ClockDrift != 0)
                {
                    timestamp += client.ClockDrift;
                }
                
                string payload = $"raised={targetState}|reason={cleanReason}|ts={timestamp}";
                string encryptedPayload = CryptoHelper.Encrypt(payload, classCode);
                await net.SendCommandAsync($"HAND_RAISE_ENC|payload={encryptedPayload}");
            }
        }

        private async void RaiseHand_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;
            _isProcessing = true;
            this.Cursor = System.Windows.Input.Cursors.Wait;

            // Kích hoạt hiệu ứng xoay hồi chiêu vòng tròn nét đứt (Gamer-style) - 0.8s
            if (cooldownRing != null && cooldownRotation != null)
            {
                cooldownRing.Visibility = Visibility.Visible;
                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = TimeSpan.FromSeconds(0.8),
                    RepeatBehavior = new System.Windows.Media.Animation.RepeatBehavior(1)
                };
                cooldownRotation.BeginAnimation(RotateTransform.AngleProperty, animation);
            }

            var app = (QASmartTouch.App)Application.Current;
            var client = app.StudentNetwork;
            var net = app.NetworkService;
            bool targetState = !client.IsHandRaised;

            try
            {
                bool hasNetwork = client.IsConnected || (net != null && net.IsBroadcasting);

                if (!hasNetwork)
                {
                    MessageBox.Show("Không có kết nối mạng. Vui lòng kiểm tra lại đường truyền!", "Lỗi kết nối", MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (cooldownRotation != null) cooldownRotation.BeginAnimation(RotateTransform.AngleProperty, null);
                    if (cooldownRing != null) cooldownRing.Visibility = Visibility.Collapsed;
                    this.Cursor = null;
                    _isProcessing = false;
                    return;
                }

                client.IsHandRaised = targetState;
                UpdateHandRaiseUI(targetState);

                string selectedReason = "Phát biểu";
                if (cbReason.SelectedItem is ComboBoxItem selectedItem)
                {
                    selectedReason = selectedItem.Content?.ToString() ?? "Phát biểu";
                }

                string cleanReason = StripLeadingEmoji(selectedReason);

                var now = DateTime.Now;
                bool isConsecutive = (now - _lastClickTime).TotalSeconds <= 2.0;
                _lastClickTime = now;

                _pendingTargetState = targetState;
                _pendingReason = cleanReason;

                if (!isConsecutive)
                {
                    // Lần đầu bấm: Gửi ngay lập tức qua mạng LAN (No Debounce)
                    await SendNetworkHandRaiseAsync(targetState, cleanReason);
                    _networkSendPending = false;
                }
                else
                {
                    // Từ lần thứ 2: kích hoạt hoãn gửi mạng để chặn spam
                    _networkSendPending = true;
                }

                // Luôn luôn hoãn ghi CSDL SQLite cục bộ để bảo vệ ổ đĩa
                if (_dbDebounceTimer != null)
                {
                    _dbDebounceTimer.Stop();
                }

                _dbDebounceTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1.0)
                };
                _dbDebounceTimer.Tick += async (timerSender, timerArgs) =>
                {
                    _dbDebounceTimer?.Stop();
                    _dbDebounceTimer = null;

                    // 1. Ghi SQLite
                    try
                    {
                        var dbApp = (QASmartTouch.App)Application.Current;
                        if (dbApp?.Database != null)
                        {
                            dbApp.Database.EventLogs.Add(new Data.EventLog
                            {
                                EventType = "HAND_RAISE",
                                Actor = "Student",
                                Details = $"Giơ tay: {_pendingTargetState} | Lý do: {_pendingReason}",
                                Timestamp = DateTime.Now
                            });
                            await dbApp.Database.SaveChangesAsync();
                            Log.Information("Debounced SQLite write: Hand raise logged successfully.");
                        }
                    }
                    catch (Exception dbEx)
                    {
                        Log.Error(dbEx, "Failed to perform debounced SQLite write");
                    }

                    // 2. Gửi mạng nếu có trạng thái chờ gửi
                    if (_networkSendPending)
                    {
                        try
                        {
                            await SendNetworkHandRaiseAsync(_pendingTargetState, _pendingReason);
                            _networkSendPending = false;
                            Log.Information("Debounced Network Send: Hand raise packet sent to teacher.");
                        }
                        catch (Exception netEx)
                        {
                            Log.Error(netEx, "Failed to perform debounced Network send");
                        }
                    }
                };
                _dbDebounceTimer.Start();

                Log.Information("Hand raise state changed: {State} (Reason: {Reason})", targetState, cleanReason);
                await Task.Delay(800); // 0.8s cooldown UI
            }
            catch (Exception ex)
            {
                Log.Warning("Hand raise click error: {Err}. Rolling back UI state.", ex.Message);
                try
                {
                    client.IsHandRaised = !targetState;
                    UpdateHandRaiseUI(!targetState);
                }
                catch { }
            }
            finally
            {
                if (cooldownRotation != null)
                {
                    cooldownRotation.BeginAnimation(RotateTransform.AngleProperty, null);
                }
                if (cooldownRing != null)
                {
                    cooldownRing.Visibility = Visibility.Collapsed;
                }
                this.Cursor = null;
                _isProcessing = false;
            }
        }

        private async void SendQuestion_Click(object sender, RoutedEventArgs e)
        {
            var question = txtQuestion.Text?.Trim();
            if (string.IsNullOrEmpty(question))
            {
                MessageBox.Show("Vui lòng nhập câu hỏi!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_isSendingQuestion) return;
            _isSendingQuestion = true;
            var sendButton = sender as Button;
            if (sendButton != null) sendButton.IsEnabled = false;
            var originalCursor = this.Cursor;
            this.Cursor = System.Windows.Input.Cursors.Wait;

            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var client = app.StudentNetwork;
                bool sent = false;

                if (client.IsConnected)
                {
                    await client.SendQuestion(question);
                    sent = true;
                }
                else
                {
                    var net = app.NetworkService;
                    if (net != null && net.IsBroadcasting)
                    {
                        // Fallback mode: Encrypt question with ClassCode (if available) before broadcasting
                        string classCode = client.ClassCode;
                        if (string.IsNullOrEmpty(classCode)) classCode = "DEFAULT_CLASS";
                        
                        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        if (client.ClockDrift != 0)
                        {
                            timestamp += client.ClockDrift;
                        }
                        
                        string payload = $"text={question}|ts={timestamp}";
                        string encryptedText = CryptoHelper.Encrypt(payload, classCode);
                        
                        await net.SendCommandAsync($"STUDENT_QUESTION_ENC|text={encryptedText}");
                        sent = true;
                    }
                }

                // Always log to DB asynchronously
                if (app?.Database != null)
                {
                    app.Database.EventLogs.Add(new Data.EventLog
                    {
                        EventType = "QUESTION",
                        Actor = "Student",
                        Details = $"Câu hỏi: {question}",
                        Timestamp = DateTime.Now
                    });
                    await app.Database.SaveChangesAsync();
                }

                if (sent)
                {
                    MessageBox.Show("Đã gửi câu hỏi cho GV!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    txtQuestion.Clear();
                }
                else
                {
                    // Save question to offline queue
                    await client.SaveOfflineMessageAsync("QUESTION", question);
                    MessageBox.Show("Mạng gián đoạn. Câu hỏi đã được đưa vào hàng đợi tự động gửi lại.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtQuestion.Clear();
                }

                Log.Information("Student question processed: {Q}, network={Sent}", question, sent);
            }
            catch (Exception ex)
            {
                Log.Warning("Send question error: {Err}", ex.Message);
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                this.Cursor = originalCursor;
                if (sendButton != null) sendButton.IsEnabled = true;
                _isSendingQuestion = false;
            }
        }

        private string StripLeadingEmoji(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            int start = 0;
            while (start < input.Length && !char.IsLetterOrDigit(input[start]))
            {
                start++;
            }
            return start < input.Length ? input.Substring(start) : input;
        }
    }
}