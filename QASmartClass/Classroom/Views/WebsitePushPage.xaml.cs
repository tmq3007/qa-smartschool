using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class WebsitePushPage : Page
    {
        private List<(string Name, string Url, string Icon)> _bookmarks = new()
        {
            ("Google", "https://google.com", "🔍"),
            ("YouTube", "https://youtube.com", "▶️"),
            ("Kahoot!", "https://kahoot.it", "🎮"),
            ("Google Forms", "https://forms.google.com", "📋"),
            ("Wikipedia", "https://vi.wikipedia.org", "📚"),
            ("Khan Academy", "https://khanacademy.org", "🎓"),
            ("Quizlet", "https://quizlet.com", "📝"),
            ("GeoGebra", "https://geogebra.org/calculator", "📐"),
            ("Graph", "https://www.desmos.com/calculator", "📈"),
            ("Canva", "https://canva.com", "🎨"),
        };

        private List<(DateTime Time, string Url)> _history = new();

        public WebsitePushPage()
        {
            InitializeComponent();
            Loaded += (_, _) => { LoadBookmarks(); LoadHistory(); InitializeToggles(); };
        }

        private void LoadBookmarks()
        {
            bookmarkPanel.Children.Clear();
            foreach (var (name, url, icon) in _bookmarks)
            {
                var btn = new Button
                {
                    Content = $"{icon} {name}",
                    FontSize = 12, FontWeight = FontWeights.Bold,
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(0, 0, 6, 6),
                    Background = new SolidColorBrush(Color.FromRgb(232, 245, 255)),
                    Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                    BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = url, ToolTip = url
                };
                btn.Template = CreateBookmarkTemplate();
                btn.Click += (_, _) => { txtUrl.Text = url; };
                bookmarkPanel.Children.Add(btn);
            }
        }

        private ControlTemplate CreateBookmarkTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var bd = new FrameworkElementFactory(typeof(Border), "bd");
            bd.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(232, 245, 255)));
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(14));
            bd.SetValue(Border.PaddingProperty, new Thickness(10, 6, 10, 6));
            bd.SetValue(Border.CursorProperty, System.Windows.Input.Cursors.Hand);
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            bd.AppendChild(cp);
            template.VisualTree = bd;

            var trigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(187, 222, 251)), "bd"));
            template.Triggers.Add(trigger);

            return template;
        }

        private void InitializeToggles()
        {
            try
            {
                var control = QASmartClass.Services.ClassControlService.Instance;
                toggleWebAccess.IsChecked = !control.IsWebBlocked;
                toggleWhitelistMode.IsChecked = control.IsWebWhitelistActive;
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to initialize toggles: {Err}", ex.Message);
            }
        }

        private void ToggleWebAccess_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                bool isAllowed = toggleWebAccess.IsChecked == true;
                
                // Update state
                var control = QASmartClass.Services.ClassControlService.Instance;
                control.IsWebBlocked = !isAllowed;
                if (!isAllowed)
                {
                    control.IsWebWhitelistActive = false;
                    toggleWhitelistMode.IsChecked = false;
                }

                string cmd = isAllowed ? "CMD|BLOCK_WEB_OFF" : "CMD|BLOCK_WEB_ON";
                
                // Broadcast policy
                if (app.NetworkService?.IsBroadcasting == true)
                    _ = app.NetworkService.SendCommandAsync(cmd);
                else
                    app.RaiseLocalCommand(cmd);

                // Save event via local context (thread-safe)
                using (var db = new QASmartClass.Data.AppDbContext())
                {
                    db.EventLogs.Add(new Data.EventLog
                    {
                        EventType = "BLOCK_WEB_STATUS",
                        Actor = "GV",
                        Details = isAllowed ? "ALLOWED" : "BLOCKED",
                        Timestamp = DateTime.Now
                    });
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Log.Error("ToggleWebAccess error: {Err}", ex.Message);
            }
        }

        private void ToggleWhitelistMode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                bool isWhitelist = toggleWhitelistMode.IsChecked == true;
                
                // Update state
                var control = QASmartClass.Services.ClassControlService.Instance;
                control.IsWebWhitelistActive = isWhitelist;
                if (isWhitelist)
                {
                    control.IsWebBlocked = false;
                    toggleWebAccess.IsChecked = true;
                }

                string cmd = isWhitelist ? "CMD|WEB_WHITELIST_ON" : "CMD|WEB_WHITELIST_OFF";
                
                // Broadcast policy
                if (app.NetworkService?.IsBroadcasting == true)
                    _ = app.NetworkService.SendCommandAsync(cmd);
                else
                    app.RaiseLocalCommand(cmd);

                // If whitelist mode was turned on and we have existing whitelist URLs, broadcast them
                if (isWhitelist && !string.IsNullOrEmpty(control.WebWhitelistUrls))
                {
                    string whitelistCmd = $"CMD|WHITELIST_ADD|{control.WebWhitelistUrls}";
                    if (app.NetworkService?.IsBroadcasting == true)
                        _ = app.NetworkService.SendCommandAsync(whitelistCmd);
                    else
                        app.RaiseLocalCommand(whitelistCmd);
                }

                // Save event via local context (thread-safe)
                using (var db = new QASmartClass.Data.AppDbContext())
                {
                    db.EventLogs.Add(new Data.EventLog
                    {
                        EventType = "WEB_WHITELIST_STATUS",
                        Actor = "GV",
                        Details = isWhitelist ? "ACTIVE" : "INACTIVE",
                        Timestamp = DateTime.Now
                    });
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Log.Error("ToggleWhitelist error: {Err}", ex.Message);
            }
        }

        private void SendUrl_Click(object sender, RoutedEventArgs e)
        {
            string url = txtUrl.Text.Trim();
            if (string.IsNullOrEmpty(url) || url == "https://")
            {
                MessageBox.Show("Vui lòng nhập URL!", "Thiếu URL", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Ensure http prefix
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                url = "https://" + url;

            // Validate URL format before sending
            if (!Uri.TryCreate(url, UriKind.Absolute, out var tempUri) || 
                (tempUri.Scheme != Uri.UriSchemeHttp && tempUri.Scheme != Uri.UriSchemeHttps))
            {
                MessageBox.Show("Định dạng URL không hợp lệ! Vui lòng nhập địa chỉ website bắt đầu bằng http:// hoặc https://", "URL không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var control = QASmartClass.Services.ClassControlService.Instance;
                
                // Update whitelist in service
                var currentWhitelist = control.WebWhitelistUrls;
                if (string.IsNullOrEmpty(currentWhitelist))
                {
                    control.WebWhitelistUrls = url;
                }
                else if (!currentWhitelist.Split(',').Contains(url))
                {
                    control.WebWhitelistUrls = currentWhitelist + "," + url;
                }

                // Broadcast whitelist update command first
                string whitelistCmd = $"CMD|WHITELIST_ADD|{url}";
                if (app.NetworkService?.IsBroadcasting == true)
                    _ = app.NetworkService.SendCommandAsync(whitelistCmd);
                else
                    app.RaiseLocalCommand(whitelistCmd);

                // Broadcast opening URL
                string cmd = $"CMD|OPEN_URL|{url}";

                // Send via network if available, otherwise local bus
                // ⚠️ MUTUAL EXCLUSION: chỉ gửi 1 kênh để tránh HS mở trùng tab
                if (app.NetworkService?.IsBroadcasting == true)
                    _ = app.NetworkService.SendCommandAsync(cmd);
                else
                    app.RaiseLocalCommand(cmd);

                // Add to history
                _history.Insert(0, (DateTime.Now, url));
                RenderHistory();

                // Save event via local context (thread-safe)
                using (var db = new QASmartClass.Data.AppDbContext())
                {
                    db.EventLogs.Add(new Data.EventLog
                    {
                        EventType = "OPEN_URL",
                        Actor = "GV",
                        Details = url,
                        Timestamp = DateTime.Now
                    });
                    db.SaveChanges();
                }

                MessageBox.Show($"✅ Đã gửi URL đến tất cả HS!\n\n🌐 {url}",
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                Log.Information("URL pushed to students: {Url}", url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadHistory()
        {
            try
            {
                using var db = new QASmartClass.Data.AppDbContext();
                var logs = db.EventLogs
                    .Where(e => e.EventType == "OPEN_URL")
                    .OrderByDescending(e => e.Timestamp)
                    .Take(20).ToList();

                _history.Clear();
                foreach (var log in logs)
                    _history.Add((log.Timestamp, log.Details));

                RenderHistory();
            }
            catch { }
        }

        private void RenderHistory()
        {
            historyList.Children.Clear();

            if (_history.Count == 0)
            {
                historyList.Children.Add(new TextBlock
                {
                    Text = "Chưa có URL nào được gửi",
                    FontSize = 13, Foreground = Brushes.Gray, FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 8, 0, 0)
                });
                return;
            }

            foreach (var (time, url) in _history)
            {
                var border = new Border
                {
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 4),
                    CornerRadius = new CornerRadius(6),
                    Background = Brushes.White,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                border.MouseEnter += (_, _) => border.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                border.MouseLeave += (_, _) => border.Background = Brushes.White;

                var dp = new DockPanel();

                // Resend button
                var btnResend = new Button
                {
                    Content = "🔄", FontSize = 12,
                    Padding = new Thickness(6, 2, 6, 2),
                    Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand, ToolTip = "Gửi lại"
                };
                string resendUrl = url;
                btnResend.Click += (_, _) => { txtUrl.Text = resendUrl; SendUrl_Click(null!, null!); };
                DockPanel.SetDock(btnResend, Dock.Right);
                dp.Children.Add(btnResend);

                // Time
                var txtTime = new TextBlock
                {
                    Text = time.ToString("HH:mm dd/MM"),
                    FontSize = 12, Foreground = Brushes.Gray, Width = 95,
                    VerticalAlignment = VerticalAlignment.Center
                };
                DockPanel.SetDock(txtTime, Dock.Left);
                dp.Children.Add(txtTime);

                // URL
                dp.Children.Add(new TextBlock
                {
                    Text = $"🌐 {url}",
                    FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });

                border.Child = dp;
                // Click to set URL
                border.MouseLeftButtonDown += (_, _) => txtUrl.Text = url;
                historyList.Children.Add(border);
            }
        }
    }
}
