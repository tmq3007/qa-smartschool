using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Staff.Services;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.IO;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QASmartClass.TeacherHub.Views
{
    public partial class BulletinBoardPage : Page
    {
        private AppDbContext? _db;
        private BulletinService? _bulletinService;
        private string _currentUser = "Khách";
        private DispatcherTimer? _pollTimer;
        private bool _bypassWarning = false;

        public BulletinBoardPage()
        {
            InitializeComponent();
            InitializeTimeCombos();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private void InitializeTimeCombos()
        {
            CbScheduledHour.Items.Clear();
            CbExpiresHour.Items.Clear();
            for (int i = 0; i < 24; i++)
            {
                string val = i.ToString("D2");
                CbScheduledHour.Items.Add(val);
                CbExpiresHour.Items.Add(val);
            }

            CbScheduledMinute.Items.Clear();
            CbExpiresMinute.Items.Clear();
            for (int i = 0; i < 60; i++)
            {
                string val = i.ToString("D2");
                CbScheduledMinute.Items.Add(val);
                CbExpiresMinute.Items.Add(val);
            }

            CbScheduledHour.SelectedValue = "08";
            CbScheduledMinute.SelectedValue = "00";
            CbExpiresHour.SelectedValue = "23";
            CbExpiresMinute.SelectedValue = "59";
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _db = new AppDbContext();
            _bulletinService = new BulletinService(_db);
            _currentUser = StaffSession.CurrentUser?.TeacherCode ?? "GV_MockUser";

            // Permission Check
            if (StaffSession.CanCreateBulletin())
            {
                BorderCreateBulletin.Visibility = Visibility.Visible;
                Grid.SetColumnSpan(BorderBulletinList, 1);
            }
            else
            {
                BorderCreateBulletin.Visibility = Visibility.Collapsed;
                Grid.SetColumnSpan(BorderBulletinList, 2);
            }

            SeedSampleBulletins();
            LoadBulletins();
            StartBackgroundPolling();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _pollTimer?.Stop();
            _pollTimer = null;
            _db?.Dispose();
            _db = null;
            _bulletinService = null;
        }

        // --- P2-06: Background polling moi 5 phut -------
        private void StartBackgroundPolling()
        {
            if (_bulletinService == null) return;
            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
            _pollTimer.Tick += (_, __) =>
            {
                if (_bulletinService == null) return;
                try
                {
                    int published = _bulletinService.PublishDue();
                    if (published > 0)
                    {
                        LoadBulletins();
                        Log.Information("[Bulletin] Auto-published {Count} bulletins", published);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Lỗi xảy ra trong quá trình chạy polling ngầm xuất bản tin");
                }
            };
            _pollTimer.Start();
            // Run once immediately
            try
            {
                _bulletinService.PublishDue();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi chạy PublishDue lúc khởi động Page");
            }
        }

        // --- P2-06: Toggle scheduling panel --------------
        private void ChkSchedule_Changed(object sender, RoutedEventArgs e)
        {
            PanelSchedule.Visibility = ChkSchedule.IsChecked == true
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadBulletins();
        }

        private void CbCategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadBulletins();
        }

        private void LoadBulletins()
        {
            if (_db == null || _bulletinService == null) return;
            try
            {
                string audienceFilter = (CbFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
                string categoryFilter = CbCategoryFilter != null 
                    ? ((CbCategoryFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All") 
                    : "All";

                // Hien thi tat ca (ke ca Scheduled) de admin quan ly
                var bulletins = _bulletinService.GetAllBulletins();
                if (audienceFilter != "All")
                    bulletins = bulletins.Where(b => b.Audience == audienceFilter || b.Audience == "All").ToList();

                if (categoryFilter != "All")
                    bulletins = bulletins.Where(b => b.Category == categoryFilter).ToList();

                var isAdmin = StaffSession.CanManageBulletin();

                var displayList = bulletins.Select(b => new
                {
                    b.Id,
                    b.Title,
                    ContentPreview = StripXamlTags(b.Content),
                    b.CreatedBy,
                    b.CreatedAt,
                    CreatedAtStr = b.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    Audience = b.Audience switch { "All" => "Tất cả", "GV" => "Giáo viên", "HS" => "Học sinh", "DV" => "Đoàn viên", "PH" => "Phụ huynh", "NV" => "Nhân viên", _ => "Học sinh" },
                    Priority = b.Priority.ToUpper(),
                    PriorityColor = b.Status == "Scheduled" ? "#F59E0B"
                                  : b.Status == "Expired" ? "#94A3B8"
                                  : b.Priority == "High" ? "#EF4444"
                                  : b.Priority == "Urgent" ? "#DC2626"
                                  : b.Priority == "Low" ? "#94A3B8" : "#3B82F6",
                    StatusBadge = b.Status == "Scheduled" ? $" [? {b.ScheduledAt:dd/MM HH:mm}]"
                                : b.Status == "Expired" ? " [Het han]" : "",
                    b.ViewCount,
                    b.LikeCount,
                    AdminVisibility = isAdmin ? Visibility.Visible : Visibility.Collapsed
                }).ToList();

                LvBulletins.ItemsSource = displayList;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Loi khi load BulletinBoardPage");
            }
        }

        private void BtnViewDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int bulletinId)
            {
                if (_db == null || _bulletinService == null) return;
                // Tang ViewCount
                _bulletinService.IncrementViewCount(bulletinId);
                
                // L?y chi ti?t b?n tin
                var bulletin = _db.Bulletins.FirstOrDefault(b => b.Id == bulletinId);
                if (bulletin != null)
                {
                    // M? c?a s? chi ti?t (Task 1.4)
                    var detailWindow = new BulletinDetailWindow(bulletin, _bulletinService);
                    detailWindow.Owner = Window.GetWindow(this);
                    detailWindow.ShowDialog();
                    
                    // Refresh UI sau khi dng
                    LoadBulletins(); 
                }
            }
        }

        private string GetRichTextContent()
        {
            var range = new TextRange(RtbContent.Document.ContentStart, RtbContent.Document.ContentEnd);
            using (var ms = new MemoryStream())
            {
                range.Save(ms, DataFormats.Xaml);
                ms.Position = 0;
                using (var sr = new StreamReader(ms))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        private void ClearRichTextContent()
        {
            RtbContent.Document.Blocks.Clear();
            RtbContent.Document.Blocks.Add(new Paragraph());
        }

        private void BtnPost_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null || _bulletinService == null) return;
            try
            {
                string title = TxtTitle.Text.Trim();
                string content = GetRichTextContent();
                string audience = (CbAudience.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
                string priority = (CbPriority.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Normal";
                string imageUrl = TxtImagePath.Text.Trim();
                string category = (CbCategory.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "School";

                var textRange = new TextRange(RtbContent.Document.ContentStart, RtbContent.Document.ContentEnd);
                if (string.IsNullOrEmpty(title) || string.IsNullOrWhiteSpace(textRange.Text))
                {
                    MessageBox.Show("Vui lòng nhập đầy đủ tiêu đề và nội dung.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // P2-06: Get scheduling dates
                DateTime? scheduledAt = null;
                DateTime? expiresAt = null;
                if (ChkSchedule.IsChecked == true)
                {
                    if (!DpScheduledAt.SelectedDate.HasValue)
                    {
                        MessageBox.Show("Vui lòng chọn ngày xuất bản khi bật chế độ Lên lịch.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var sDate = DpScheduledAt.SelectedDate.Value;
                    int sh = int.TryParse(CbScheduledHour.SelectedValue?.ToString(), out int pSH) ? pSH : 8;
                    int sm = int.TryParse(CbScheduledMinute.SelectedValue?.ToString(), out int pSM) ? pSM : 0;
                    scheduledAt = new DateTime(sDate.Year, sDate.Month, sDate.Day, sh, sm, 0);
                }

                if (DpExpiresAt.SelectedDate.HasValue)
                {
                    var eDate = DpExpiresAt.SelectedDate.Value;
                    int eh = int.TryParse(CbExpiresHour.SelectedValue?.ToString(), out int pEH) ? pEH : 23;
                    int em = int.TryParse(CbExpiresMinute.SelectedValue?.ToString(), out int pEM) ? pEM : 59;
                    expiresAt = new DateTime(eDate.Year, eDate.Month, eDate.Day, eh, em, 59);

                    if (scheduledAt.HasValue && expiresAt <= scheduledAt)
                    {
                        MessageBox.Show("Ngày hết hạn phải sau ngày xuất bản.", "Lỗi thời gian", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    else if (!scheduledAt.HasValue && expiresAt <= DateTime.Now)
                    {
                        MessageBox.Show("Ngày hết hạn phải sau thời điểm hiện tại.", "Lỗi thời gian", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                BulletinResult result;
                if (_editingBulletinId.HasValue)
                {
                    result = _bulletinService.UpdateBulletin(_editingBulletinId.Value, title, content, audience, priority, imageUrl,
                        scheduledAt, expiresAt, "", "", category, _bypassWarning);
                }
                else
                {
                    result = _bulletinService.CreateBulletin(title, content, audience, priority, _currentUser,
                        scheduledAt, expiresAt, imageUrl, "", "", category, _bypassWarning);
                }

                if (result.Status == "RequireConfirmation")
                {
                    var confirmResult = MessageBox.Show(result.Message, "Cảnh báo an toàn sư phạm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (confirmResult == MessageBoxResult.Yes)
                    {
                        _bypassWarning = true;
                        BtnPost_Click(sender, e);
                        _bypassWarning = false;
                    }
                    return;
                }

                if (result.Success)
                {
                    if (result.Status == "AutoCorrected")
                    {
                        MessageBox.Show(result.Message, "Thông báo bảo mật hệ thống", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    string msg = _editingBulletinId.HasValue
                        ? "Cập nhật bảng tin thành công!"
                        : (scheduledAt.HasValue
                            ? $"Lên lịch xuất bản lúc {scheduledAt:dd/MM/yyyy}!"
                            : "Đăng bảng tin thành công!");

                    MessageBox.Show(msg, "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    TxtTitle.Text = "";
                    ClearRichTextContent();
                    TxtImagePath.Text = "";
                    GridPreview.Visibility = Visibility.Collapsed;
                    ImgPreview.Source = null;
                    ChkSchedule.IsChecked = false;

                    _editingBulletinId = null;
                    BtnPost.Content = "Đăng Lên Bảng Tin";
                    BtnCancelEdit.Visibility = Visibility.Collapsed;

                    LoadBulletins();
                }
                else
                {
                    MessageBox.Show("Có lỗi xảy ra khi lưu bảng tin.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi đăng tin");
                MessageBox.Show("Lỗi hệ thống.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- PHASE 3: Content Creation Handlers ---
        private void BtnFormat_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string format)
            {
                if (format == "Bold")
                {
                    var currentWeight = RtbContent.Selection.GetPropertyValue(TextElement.FontWeightProperty);
                    bool isBold = currentWeight != DependencyProperty.UnsetValue && currentWeight.Equals(FontWeights.Bold);
                    RtbContent.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, isBold ? FontWeights.Normal : FontWeights.Bold);
                }
                else if (format == "Italic")
                {
                    var currentStyle = RtbContent.Selection.GetPropertyValue(TextElement.FontStyleProperty);
                    bool isItalic = currentStyle != DependencyProperty.UnsetValue && currentStyle.Equals(FontStyles.Italic);
                    RtbContent.Selection.ApplyPropertyValue(TextElement.FontStyleProperty, isItalic ? FontStyles.Normal : FontStyles.Italic);
                }
                else if (format == "Underline")
                {
                    var currentDecorations = RtbContent.Selection.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection;
                    bool isUnderline = currentDecorations != DependencyProperty.UnsetValue && currentDecorations != null && currentDecorations.Count > 0;
                    RtbContent.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty, isUnderline ? null : TextDecorations.Underline);
                }
            }
        }

        private void BtnColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string colorStr)
            {
                SolidColorBrush brush = (SolidColorBrush)new BrushConverter().ConvertFrom("#1E293B");
                if (colorStr == "Red") brush = (SolidColorBrush)new BrushConverter().ConvertFrom("#E11D48");
                else if (colorStr == "Blue") brush = (SolidColorBrush)new BrushConverter().ConvertFrom("#2563EB");
                
                RtbContent.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
            }
        }

        private void BtnUploadImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png",
                    Title = "Chọn ảnh minh họa"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    // Validate extension
                    string ext = Path.GetExtension(openFileDialog.FileName).ToLower();
                    if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                    {
                        MessageBox.Show("Chỉ cho phép tải lên các tệp ảnh định dạng JPG, JPEG, PNG.", "Định dạng không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Validate file size (Max 5MB)
                    var fileInfo = new FileInfo(openFileDialog.FileName);
                    if (fileInfo.Length > 5 * 1024 * 1024)
                    {
                        MessageBox.Show("Dung lượng ảnh vượt quá giới hạn cho phép (Tối đa 5MB).", "Kích thước quá lớn", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Copy file to AppData (simulated)
                    string fileName = Path.GetFileName(openFileDialog.FileName);
                    string destPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads", "Images");
                    if (!Directory.Exists(destPath)) Directory.CreateDirectory(destPath);
                    
                    string newFilePath = Path.Combine(destPath, $"{Guid.NewGuid()}_{fileName}");
                    File.Copy(openFileDialog.FileName, newFilePath, true);

                    TxtImagePath.Text = newFilePath;
                    
                    // Show preview
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(newFilePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    
                    ImgPreview.Source = bitmap;
                    GridPreview.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi upload ảnh");
                MessageBox.Show("Lỗi upload ảnh.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnChooseTemplate_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new BulletinTemplateView();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true && dialog.SelectedTemplate != null)
            {
                TxtTitle.Text = dialog.SelectedTemplate.Name;
                string content = dialog.SelectedTemplate.HtmlContent;
                
                try
                {
                    if (content.Contains("</FlowDocument>"))
                    {
                        var stringReader = new System.IO.StringReader(content);
                        var xmlReader = System.Xml.XmlReader.Create(stringReader);
                        var document = (FlowDocument)System.Windows.Markup.XamlReader.Load(xmlReader);
                        RtbContent.Document = document;
                    }
                    else
                    {
                        string plainText = content.Replace("<h1>", "").Replace("</h1>", "\n").Replace("<p>", "").Replace("</p>", "\n").Trim();
                        RtbContent.Document.Blocks.Clear();
                        RtbContent.Document.Blocks.Add(new Paragraph(new Run(plainText)));
                    }
                }
                catch
                {
                    RtbContent.Document.Blocks.Clear();
                    RtbContent.Document.Blocks.Add(new Paragraph(new Run(content)));
                }
            }
        }

        private void BtnAiSuggest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var aiService = new AiAssistantService(_db);
                // Gi? l?p eventType
                string suggestion = aiService.SuggestBulletinContent(DateTime.Today, "Holiday");
                
                if (!string.IsNullOrEmpty(suggestion))
                {
                    TxtTitle.Text = "💡 [AI Gợi ý] Thông báo nội bộ";
                    RtbContent.Document.Blocks.Clear();
                    RtbContent.Document.Blocks.Add(new Paragraph(new Run(suggestion)));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi gửi AI suggest");
                MessageBox.Show("Tính năng gợi ý AI đang gặp sự cố.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnSlideshow_Click(object sender, RoutedEventArgs e)
        {
            var slideshow = new BulletinSlideshowWindow(_db, _bulletinService);
            slideshow.Show();
        }

        private void SeedSampleBulletins()
        {
            if (_db == null) return;
            try
            {
                if (!_db.Bulletins.Any(b => b.Title == "Nghỉ lễ Quốc Khánh 2/9"))
                {
                    var newBulletins = new List<Bulletin>
                    {
                        new Bulletin 
                        {
                            Title = "Nghỉ lễ Quốc Khánh 2/9", Audience = "All", Priority = "High",
                            Status = "Published", CreatedBy = "BGH Nhà Trường", CreatedAt = DateTime.Now.AddHours(-1),
                            ViewCount = 154, LikeCount = 42,
                            Content = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"22\" FontWeight=\"Bold\" Foreground=\"#D97706\" TextAlignment=\"Center\">THÔNG BÁO NGHỈ LỄ QUỐC KHÁNH 2/9</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Kính gửi toàn thể Cán bộ, Giáo viên và Học sinh,</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Nhà trường trân trọng thông báo lịch nghỉ lễ Quốc Khánh 2/9 như sau:</Paragraph><List MarkerStyle=\"Disc\"><ListItem><Paragraph FontSize=\"16\" Foreground=\"#334155\">Thời gian nghỉ: Từ ngày 01/09 đến hết ngày 04/09.</Paragraph></ListItem><ListItem><Paragraph FontSize=\"16\" Foreground=\"#334155\">Thời gian đi học lại: Thứ ba, ngày 05/09.</Paragraph></ListItem></List><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#EF4444\">Lưu ý:</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">- Học sinh ôn tập bài cũ tại nhà---<LineBreak/>- Giáo viên kiểm tra vệ sinh lớp học trước khi nghỉ.</Paragraph><Paragraph FontSize=\"16\" FontStyle=\"Italic\" Foreground=\"#64748B\">Kính chúc mọi người kỳ nghỉ lễ vui vẻ và an toàn!</Paragraph></FlowDocument>"
                        },
                        new Bulletin 
                        {
                            Title = "Kế hoạch Thi Giữa Kỳ", Audience = "HS", Priority = "Urgent",
                            Status = "Published", CreatedBy = "Phòng Khảo Thí", CreatedAt = DateTime.Now.AddDays(-2),
                            ViewCount = 420, LikeCount = 85,
                            Content = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"22\" FontWeight=\"Bold\" Foreground=\"#2563EB\" TextAlignment=\"Center\">LỊCH THI GIỮA HỌC KỲ I</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Ban Giám Hiệu thông báo kế hoạch tổ chức kiểm tra giữa học kỳ I năm học 2026-2027:</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">1. Khối 10 và 11</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">- Thời gian: Sáng từ 07:30 đến 11:30 (Thứ 4 đến Thứ 6).<LineBreak/>- Môn thi: Toán, Văn, Anh, Lý, Hóa.</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">2. Khối 12</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">- Thời gian: Chiều từ 13:30 đến 17:00 (Thứ 2 đến Thứ 4).<LineBreak/>- Đề thi theo định dạng tốt nghiệp THPT Quốc gia.</Paragraph><Paragraph FontSize=\"16\" FontStyle=\"Italic\" Foreground=\"#DC2626\">* Yêu cầu học sinh mang thẻ học sinh khi vào phòng thi.</Paragraph></FlowDocument>"
                        },
                        new Bulletin 
                        {
                            Title = "Phát động Phong trào Xanh", Audience = "All", Priority = "Low",
                            Status = "Published", CreatedBy = "Đoàn Thanh niên", CreatedAt = DateTime.Now.AddDays(-5),
                            ViewCount = 89, LikeCount = 120,
                            Content = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"24\" FontWeight=\"Black\" Foreground=\"#16A34A\" TextAlignment=\"Center\">🌿 CHIẾN DỊCH TRƯỜNG HỌC XANH 🌿</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Đoàn Thanh niên trường phát động tháng hành động vì môi trường học đường.</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">Nội dung hoạt động:</Paragraph><List MarkerStyle=\"Box\"><ListItem><Paragraph FontSize=\"16\" Foreground=\"#334155\">Thu gom pin cũ đổi cây sen đá (Tầng 1 - Sảnh A).</Paragraph></ListItem><ListItem><Paragraph FontSize=\"16\" Foreground=\"#334155\">Mỗi lớp chăm sóc 2 chậu cây xanh ban công.</Paragraph></ListItem><ListItem><Paragraph FontSize=\"16\" Foreground=\"#334155\">Tổng vệ sinh lớp học chiều thứ 6 hàng tuần.</Paragraph></ListItem></List><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#EA580C\">Phần thưởng:</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Lớp có mảng xanh đẹp nhất sẽ được cộng 50 điểm thi đua và 1 voucher liên hoan trị giá 1.000.000đ.</Paragraph></FlowDocument>"
                        },
                        new Bulletin 
                        {
                            Title = "Thông báo Tuyển sinh CLB", Audience = "HS", Priority = "Normal",
                            Status = "Published", CreatedBy = "Phòng Công tác HS", CreatedAt = DateTime.Now.AddDays(-6),
                            ViewCount = 312, LikeCount = 210,
                            Content = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"22\" FontWeight=\"Bold\" Foreground=\"#9333EA\" TextAlignment=\"Center\">🔥 TUYỂN THÀNH VIÊN CÁC CÂU LẠC BỘ 🔥</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Xin chào các bạn học sinh khối 10!</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Tuần lễ định hướng và đăng ký CLB chính thức bắt đầu. Hãy nhanh tay chọn cho mình một câu lạc bộ để phát triển đam mê:</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">Các CLB đang tuyển:</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">- CLB Âm nhạc (Giai điệu thanh xuân)<LineBreak/>- CLB Bóng rổ (Dunk Kings)<LineBreak/>- CLB Lập trình &amp; Robotics<LineBreak/>- CLB Truyền thông (Media Team)</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#2563EB\" TextDecorations=\"Underline\">Hạn chót đăng ký: 23:59 Chủ nhật tuần này.</Paragraph><Paragraph FontSize=\"16\" FontStyle=\"Italic\" Foreground=\"#64748B\">Link đăng ký chi tiết đã được gửi qua email học sinh.</Paragraph></FlowDocument>"
                        },
                        new Bulletin 
                        {
                            Title = "Khen thưởng Học sinh Giỏi", Audience = "All", Priority = "High",
                            Status = "Published", CreatedBy = "BGH Nhà Trường", CreatedAt = DateTime.Now.AddDays(-7),
                            ViewCount = 502, LikeCount = 315,
                            Content = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"22\" FontWeight=\"Bold\" Foreground=\"#EAB308\" TextAlignment=\"Center\">DANH SÁCH VINH DANH HỌC SINH GIỎI</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Nhà trường nhiệt liệt chúc mừng các em học sinh đã xuất sắc đạt giải trong kỳ thi Học sinh giỏi cấp Tỉnh vừa qua:</Paragraph><List MarkerStyle=\"Decimal\"><ListItem><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">Nguyễn Trần A - 12A1 (Giải Nhất Toán)</Paragraph></ListItem><ListItem><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">Lê Thị B - 11A2 (Giải Nhì Văn)</Paragraph></ListItem><ListItem><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">Trần Văn C - 12A1 (Giải Ba Lý)</Paragraph></ListItem></List><Paragraph FontSize=\"16\" Foreground=\"#334155\">Lễ trao thưởng sẽ diễn ra vào Lễ chào cờ sáng thứ 2 tuần sau.</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Thầy cô và các bạn hãy đến chung vui cùng các em nhé!</Paragraph></FlowDocument>"
                        },
                        new Bulletin 
                        {
                            Title = "Nội quy Canteen Trường", Audience = "All", Priority = "Normal",
                            Status = "Published", CreatedBy = "Phòng Hành chính", CreatedAt = DateTime.Now.AddDays(-10),
                            ViewCount = 120, LikeCount = 15,
                            Content = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"22\" FontWeight=\"Bold\" Foreground=\"#0284C7\" TextAlignment=\"Center\">🍴 NỘI QUY CANTEEN TRƯỜNG 🍴</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Để đảm bảo vệ sinh và trật tự, yêu cầu học sinh và giáo viên tuân thủ các quy định sau khi ăn tại Canteen:</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">1. Xếp hàng lấy thức ăn theo thứ tự, không chen lấn.<LineBreak/>2. Ăn uống xong phải dọn dẹp khay và phân loại rác (Nhựa - Thức ăn thừa).<LineBreak/>3. Không mang đồ ăn, nước uống ra khỏi khu vực Canteen.<LineBreak/>4. Thanh toán bằng thẻ học sinh (Smart Card) hoặc quét mã QR.</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#DC2626\">Nhà trường sẽ trừ điểm rèn luyện nếu vi phạm.</Paragraph></FlowDocument>"
                        },
                        new Bulletin 
                        {
                            Title = "Thông báo Thu Học phí", Audience = "HS", Priority = "High",
                            Status = "Published", CreatedBy = "Phòng Tài vụ", CreatedAt = DateTime.Now.AddDays(-12),
                            ViewCount = 890, LikeCount = 12,
                            Content = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"22\" FontWeight=\"Bold\" Foreground=\"#BE123C\" TextAlignment=\"Center\">📢 THÔNG BÁO THU HỌC PHÍ KỲ 2 📢</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Kính gửi Quý Phụ huynh,</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Nhà trường xin thông báo thời gian đóng học phí và các khoản thu học kỳ 2 năm học 2026-2027.</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">- Thời hạn: Từ ngày 01/02 đến 15/02.</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">- Hình thức thanh toán:</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">1. Chuyển khoản qua tài khoản ngân hàng của trường (Cú pháp: MaHS_HoTen_HocPhiK2).<LineBreak/>2. Thanh toán trực tiếp tại Phòng Tài vụ (Tầng 1).</Paragraph><Paragraph FontSize=\"16\" FontStyle=\"Italic\" Foreground=\"#64748B\">Mọi thắc mắc xin liên hệ Hotline: 1900 1234.</Paragraph></FlowDocument>"
                        },
                        new Bulletin 
                        {
                            Title = "Hướng dẫn Kích hoạt App", Audience = "All", Priority = "Normal",
                            Status = "Published", CreatedBy = "Phòng IT", CreatedAt = DateTime.Now.AddDays(-15),
                            ViewCount = 450, LikeCount = 95,
                            Content = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"22\" FontWeight=\"Bold\" Foreground=\"#047857\" TextAlignment=\"Center\">📲 HƯỚNG DẪN CÀI ĐẶT SMARTCLASS APP</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Bắt đầu từ tuần này, mọi thông báo sẽ được đẩy về ứng dụng điện thoại.</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">Các bước cài đặt:</Paragraph><List MarkerStyle=\"Decimal\"><ListItem><Paragraph FontSize=\"16\" Foreground=\"#334155\">Lên App Store hoặc Google Play tải \"QA SmartClass\".</Paragraph></ListItem><ListItem><Paragraph FontSize=\"16\" Foreground=\"#334155\">Mở app, đăng nhập bằng tài khoản (Mã HS/GV và Mật khẩu mặc định).</Paragraph></ListItem><ListItem><Paragraph FontSize=\"16\" Foreground=\"#334155\">Vào phần Cài đặt -> Đổi mật khẩu.</Paragraph></ListItem></List><Paragraph FontSize=\"16\" Foreground=\"#2563EB\" TextDecorations=\"Underline\">Lưu ý: Bật thông báo (Notifications) để không bị lỡ tin tức quan trọng.</Paragraph></FlowDocument>"
                        },
                        new Bulletin 
                        {
                            Title = "Triệu tập Họp Hội đồng", Audience = "GV", Priority = "Urgent",
                            Status = "Published", CreatedBy = "BGH Nhà Trường", CreatedAt = DateTime.Now.AddDays(-18),
                            ViewCount = 65, LikeCount = 20,
                            Content = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"22\" FontWeight=\"Bold\" Foreground=\"#B91C1C\" TextAlignment=\"Center\">LỊCH HỌP HỘI ĐỒNG SƯ PHẠM</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Kính mời toàn thể Cán bộ, Giáo viên và Nhân viên tham dự cuộc họp Hội đồng định kỳ tháng này.</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">Thông tin chi tiết:</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">- Thời gian: 14h00, Thứ 6 ngày 25/09.<LineBreak/>- Địa điểm: Phòng Hội đồng (Tầng 2 - Tòa A).<LineBreak/>- Thành phần: BGH, Tổ trưởng chuyên môn và toàn thể giáo viên.</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">Nội dung chính:</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">- Đánh giá hoạt động tháng 9.<LineBreak/>- Triển khai chuyên đề Dạy học STEM.</Paragraph><Paragraph FontSize=\"16\" FontStyle=\"Italic\" Foreground=\"#DC2626\">Yêu cầu 100% tham gia đầy đủ, đúng giờ.</Paragraph></FlowDocument>"
                        },
                        new Bulletin 
                        {
                            Title = "Thông báo Nghỉ Đột Xuất (Bão)", Audience = "All", Priority = "Urgent",
                            Status = "Published", CreatedBy = "Sở GD&ĐT", CreatedAt = DateTime.Now.AddDays(-30),
                            ViewCount = 1250, LikeCount = 600,
                            Content = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"26\" FontWeight=\"Black\" Foreground=\"#DC2626\" TextAlignment=\"Center\">THÔNG BÁO KHẨN CẤP: NGHỈ HỌC TRÁNH BÃO</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">Kính gửi Quý Phụ huynh và Học sinh,</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Theo công điện khẩn của Sở GD&ĐT về tình hình siêu bão đang đổ bộ, Nhà trường thông báo:</Paragraph><Paragraph FontSize=\"18\" FontWeight=\"Bold\" Foreground=\"#B91C1C\" TextAlignment=\"Center\">CHO TẤT CẢ HỌC SINH NGHỈ HỌC TỪ CHIỀU HÔM NAY (CHO ĐẾN KHI CÓ THÔNG BÁO MỚI)</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Đề nghị Phụ huynh sắp xếp đón con em trước 15:00 hôm nay. Giáo viên chủ nhiệm đảm bảo không học sinh nào ở lại trường sau 15:30.</Paragraph><Paragraph FontSize=\"16\" FontStyle=\"Italic\" Foreground=\"#64748B\">Lịch học bù sẽ được thông báo sau. Rất mong mọi người giữ an toàn tuyệt đối!</Paragraph></FlowDocument>"
                        }
                    };
                    _db.Bulletins.AddRange(newBulletins);
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi SeedSampleBulletins");
            }
        }

        // --- BUG-01 FIX: Strip XAML/HTML tags to show plain text preview ---
        private static string StripXamlTags(string xamlContent)
        {
            if (string.IsNullOrEmpty(xamlContent)) return string.Empty;
            // Remove all XML/XAML tags
            string stripped = System.Text.RegularExpressions.Regex.Replace(xamlContent, "<[^>]+>", " ");
            // Decode common XML entities
            stripped = stripped.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"");
            // Collapse whitespace
            stripped = System.Text.RegularExpressions.Regex.Replace(stripped, @"\s+", " ").Trim();
            // Truncate to 150 chars
            return stripped.Length > 150 ? stripped.Substring(0, 150) + "..." : stripped;
        }

        private int? _editingBulletinId = null;

        // --- BUG-07 FIX: Edit bulletin handler ---
        private void BtnEditBulletin_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            if (sender is Button btn && btn.Tag is int bulletinId)
            {
                var bulletin = _db.Bulletins.FirstOrDefault(b => b.Id == bulletinId);
                if (bulletin == null) return;

                // Load content into the editor form
                TxtTitle.Text = bulletin.Title;

                try
                {
                    if (bulletin.Content.Contains("</FlowDocument>"))
                    {
                        var sr = new System.IO.StringReader(bulletin.Content);
                        var xr = System.Xml.XmlReader.Create(sr);
                        RtbContent.Document = (FlowDocument)System.Windows.Markup.XamlReader.Load(xr);
                    }
                    else
                    {
                        RtbContent.Document.Blocks.Clear();
                        RtbContent.Document.Blocks.Add(new Paragraph(new Run(bulletin.Content)));
                    }
                }
                catch
                {
                    RtbContent.Document.Blocks.Clear();
                    RtbContent.Document.Blocks.Add(new Paragraph(new Run(bulletin.Content)));
                }

                // Set combo selections
                foreach (ComboBoxItem item in CbAudience.Items)
                    if (item.Tag?.ToString() == bulletin.Audience) { CbAudience.SelectedItem = item; break; }
                foreach (ComboBoxItem item in CbPriority.Items)
                    if (item.Tag?.ToString() == bulletin.Priority) { CbPriority.SelectedItem = item; break; }
                foreach (ComboBoxItem item in CbCategory.Items)
                    if (item.Tag?.ToString() == bulletin.Category) { CbCategory.SelectedItem = item; break; }

                if (bulletin.ScheduledAt.HasValue)
                {
                    ChkSchedule.IsChecked = true;
                    DpScheduledAt.SelectedDate = bulletin.ScheduledAt.Value.Date;
                    CbScheduledHour.SelectedValue = bulletin.ScheduledAt.Value.Hour.ToString("D2");
                    CbScheduledMinute.SelectedValue = bulletin.ScheduledAt.Value.Minute.ToString("D2");
                }
                else
                {
                    ChkSchedule.IsChecked = false;
                    DpScheduledAt.SelectedDate = null;
                    CbScheduledHour.SelectedValue = "08";
                    CbScheduledMinute.SelectedValue = "00";
                }

                if (bulletin.ExpiresAt.HasValue)
                {
                    DpExpiresAt.SelectedDate = bulletin.ExpiresAt.Value.Date;
                    CbExpiresHour.SelectedValue = bulletin.ExpiresAt.Value.Hour.ToString("D2");
                    CbExpiresMinute.SelectedValue = bulletin.ExpiresAt.Value.Minute.ToString("D2");
                }
                else
                {
                    DpExpiresAt.SelectedDate = null;
                    CbExpiresHour.SelectedValue = "23";
                    CbExpiresMinute.SelectedValue = "59";
                }

                _editingBulletinId = bulletinId;
                BtnPost.Content = "Cập nhật Bản Tin";
                BtnCancelEdit.Visibility = Visibility.Visible;

                TxtImagePath.Text = bulletin.ImageUrl;
                if (!string.IsNullOrEmpty(bulletin.ImageUrl) && File.Exists(bulletin.ImageUrl))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(bulletin.ImageUrl, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        ImgPreview.Source = bitmap;
                        GridPreview.Visibility = Visibility.Visible;
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Lỗi nạp ảnh xem trước khi sửa");
                        GridPreview.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    GridPreview.Visibility = Visibility.Collapsed;
                    ImgPreview.Source = null;
                }

                MessageBox.Show("Nội dung bảng tin đã được đưa vào form chỉnh sửa.\nHãy sửa và bấm 'Cập nhật Bản Tin' để lưu.",
                    "Chỉnh sửa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // --- BUG-07 FIX: Delete bulletin handler ---
        private void BtnDeleteBulletin_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null || _bulletinService == null) return;
            if (sender is Button btn && btn.Tag is int bulletinId)
            {
                var result = MessageBox.Show("Bạn có chắc muốn xóa bảng tin này?\nThao tác này không thể hoàn tác.",
                    "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _bulletinService.DeleteBulletin(bulletinId);
                        LoadBulletins();
                        MessageBox.Show("Đã xóa bảng tin thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Lỗi xóa bảng tin {Id}", bulletinId);
                        MessageBox.Show("Có lỗi khi xóa bảng tin.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnCancelEdit_Click(object sender, RoutedEventArgs e)
        {
            var textRange = new TextRange(RtbContent.Document.ContentStart, RtbContent.Document.ContentEnd);
            if (!string.IsNullOrEmpty(TxtTitle.Text.Trim()) || !string.IsNullOrWhiteSpace(textRange.Text.Replace("\r", "").Replace("\n", "").Trim()))
            {
                var confirm = MessageBox.Show("Nội dung nháp soạn thảo sẽ bị mất. Bạn có chắc chắn muốn hủy bỏ?", 
                    "Xác nhận hủy", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm == MessageBoxResult.No) return;
            }

            _editingBulletinId = null;
            BtnPost.Content = "Đăng Lên Bảng Tin";
            BtnCancelEdit.Visibility = Visibility.Collapsed;

            TxtTitle.Text = "";
            ClearRichTextContent();
            TxtImagePath.Text = "";
            GridPreview.Visibility = Visibility.Collapsed;
            ImgPreview.Source = null;
            ChkSchedule.IsChecked = false;
        }

        private void BtnRemoveImage_Click(object sender, RoutedEventArgs e)
        {
            TxtImagePath.Text = "";
            GridPreview.Visibility = Visibility.Collapsed;
            ImgPreview.Source = null;
        }

        private void BtnHideHelp_Click(object sender, RoutedEventArgs e)
        {
            BorderHelpWizard.Visibility = Visibility.Collapsed;
            BtnShowHelp.Visibility = Visibility.Visible;
        }

        private void BtnShowHelp_Click(object sender, RoutedEventArgs e)
        {
            BorderHelpWizard.Visibility = Visibility.Visible;
            BtnShowHelp.Visibility = Visibility.Collapsed;
        }
    }
}

