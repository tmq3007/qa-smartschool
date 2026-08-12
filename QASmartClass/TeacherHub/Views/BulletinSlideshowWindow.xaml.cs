using QASmartClass.Data;
using QASmartClass.Services;
using QRCoder;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace QASmartClass.TeacherHub.Views
{
    public partial class BulletinSlideshowWindow : Window
    {
        private readonly AppDbContext _db;
        private readonly BulletinService _bulletinService;
        private List<Bulletin> _slides = new List<Bulletin>();
        private int _currentIndex = -1;
        private DispatcherTimer _slideTimer;
        private DispatcherTimer _clockTimer;
        private readonly int _slideDurationSeconds = 10;

        public BulletinSlideshowWindow(AppDbContext db, BulletinService bulletinService)
        {
            InitializeComponent();
            _db = db;
            _bulletinService = bulletinService;
            
            Loaded += Window_Loaded;
            Unloaded += Window_Unloaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadBulletins();
            
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, args) => TxtClock.Text = DateTime.Now.ToString("HH:mm:ss");
            _clockTimer.Start();

            if (_slides.Count > 0)
            {
                _slideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_slideDurationSeconds) };
                _slideTimer.Tick += SlideTimer_Tick;
                _slideTimer.Start();
                
                ShowNextSlide();
            }
            else
            {
                Close();
            }
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            _clockTimer?.Stop();
            _slideTimer?.Stop();
            
            // BUG-08 FIX: Properly release resources instead of forcing GC
            ImgBulletin.Source = null;
            ImgQrCode.Source = null;
            RtbContent.Document.Blocks.Clear();
            _slides.Clear();
        }

        private void LoadBulletins()
        {
            try
            {
                _slides = _db.Bulletins
                             .Where(b => b.Status == "Published" || b.Status == "Scheduled")
                             .Where(b => !b.IsScheduled || (b.ScheduledAt.HasValue && b.ScheduledAt.Value <= DateTime.Now))
                             .Where(b => !b.ExpiresAt.HasValue || b.ExpiresAt.Value > DateTime.Now)
                             .OrderByDescending(b => b.Priority == "High" || b.Priority == "Urgent" ? 1 : 0)
                             .ThenByDescending(b => b.CreatedAt)
                             .Take(20) // Limit to top 20 to avoid memory issues
                             .ToList();
            }
            catch (Exception ex) { Log.Error(ex, "Lỗi load data cho Slideshow"); }
        }

        private void SlideTimer_Tick(object? sender, EventArgs e)
        {
            ShowNextSlide();
        }

        private void ShowNextSlide()
        {
            if (_slides.Count == 0) return;

            _currentIndex++;
            if (_currentIndex >= _slides.Count)
            {
                _currentIndex = 0;
                LoadBulletins(); // Refresh data each loop
            }

            var b = _slides[_currentIndex];

            // Setup UI for the current bulletin
            TxtTitle.Text = b.Title;
            TxtProgress.Text = $"{_currentIndex + 1} / {_slides.Count}";

            // Priority logic
            TxtPriority.Text = b.Priority.ToUpper() == "HIGH" ? "QUAN TRỌNG" : "THÔNG BÁO";
            BorderPriority.Background = b.Priority.ToUpper() == "HIGH" 
                                        ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)) 
                                        : new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));

            // Parse RichText
            try
            {
                if (b.Content.Contains("</FlowDocument>"))
                {
                    var stringReader = new StringReader(b.Content);
                    var xmlReader = System.Xml.XmlReader.Create(stringReader);
                    var document = (System.Windows.Documents.FlowDocument)System.Windows.Markup.XamlReader.Load(xmlReader);
                    ClearLocalFontSizes(document);
                    document.FontSize = 28; // Scale up for TV
                    RtbContent.Document = document;
                }
                else
                {
                    RtbContent.Document.Blocks.Clear();
                    RtbContent.Document.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(b.Content)));
                }
            }
            catch
            {
                RtbContent.Document.Blocks.Clear();
                RtbContent.Document.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(b.Content)));
            }

            // Image
            if (!string.IsNullOrEmpty(b.ImageUrl) && File.Exists(b.ImageUrl))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(b.ImageUrl, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                ImgBulletin.Source = bitmap;
                ImgBulletin.Visibility = Visibility.Visible;
                BorderImageContainer.Visibility = Visibility.Visible;
            }
            else
            {
                ImgBulletin.Source = null;
                ImgBulletin.Visibility = Visibility.Collapsed;
                BorderImageContainer.Visibility = Visibility.Collapsed;
            }

            // QR Code
            GenerateQrCode(b.Id);

            // Animate progress bar with dynamic duration
            int currentDuration = CalculateSlideDuration(b.Content);
            if (_slideTimer != null)
            {
                _slideTimer.Interval = TimeSpan.FromSeconds(currentDuration);
            }

            var widthAnimation = new DoubleAnimation
            {
                From = 0,
                To = this.ActualWidth,
                Duration = TimeSpan.FromSeconds(currentDuration)
            };
            ProgressBar.BeginAnimation(FrameworkElement.WidthProperty, widthAnimation);
        }

        private void ClearLocalFontSizes(System.Windows.Documents.FlowDocument doc)
        {
            if (doc == null) return;
            doc.ClearValue(System.Windows.Documents.TextElement.FontSizeProperty);
            foreach (var block in doc.Blocks)
            {
                ClearBlockFontSizes(block);
            }
        }

        private void ClearBlockFontSizes(System.Windows.Documents.Block block)
        {
            if (block == null) return;
            block.ClearValue(System.Windows.Documents.TextElement.FontSizeProperty);

            if (block is System.Windows.Documents.Paragraph p)
            {
                foreach (var inline in p.Inlines)
                {
                    ClearInlineFontSizes(inline);
                }
            }
            else if (block is System.Windows.Documents.List list)
            {
                foreach (var listItem in list.ListItems)
                {
                    listItem.ClearValue(System.Windows.Documents.TextElement.FontSizeProperty);
                    foreach (var subBlock in listItem.Blocks)
                    {
                        ClearBlockFontSizes(subBlock);
                    }
                }
            }
            else if (block is System.Windows.Documents.Table table)
            {
                foreach (var rowGroup in table.RowGroups)
                {
                    foreach (var row in rowGroup.Rows)
                    {
                        row.ClearValue(System.Windows.Documents.TextElement.FontSizeProperty);
                        foreach (var cell in row.Cells)
                        {
                            cell.ClearValue(System.Windows.Documents.TextElement.FontSizeProperty);
                            foreach (var cellBlock in cell.Blocks)
                            {
                                ClearBlockFontSizes(cellBlock);
                            }
                        }
                    }
                }
            }
        }

        private void ClearInlineFontSizes(System.Windows.Documents.Inline inline)
        {
            if (inline == null) return;
            inline.ClearValue(System.Windows.Documents.TextElement.FontSizeProperty);

            if (inline is System.Windows.Documents.Span span)
            {
                foreach (var subInline in span.Inlines)
                {
                    ClearInlineFontSizes(subInline);
                }
            }
        }

        private int CalculateSlideDuration(string content)
        {
            string plainText = StripHtmlOrXamlTags(content);
            if (string.IsNullOrEmpty(plainText)) return 10;
            int wordCount = plainText.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            int duration = (wordCount / 3) + 3;
            return Math.Clamp(duration, 10, 35);
        }

        private static string StripHtmlOrXamlTags(string xamlContent)
        {
            if (string.IsNullOrEmpty(xamlContent)) return string.Empty;
            string stripped = System.Text.RegularExpressions.Regex.Replace(xamlContent, "<[^>]+>", " ");
            stripped = stripped.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"");
            return stripped.Trim();
        }

        private void GenerateQrCode(int bulletinId)
        {
            try
            {
                string deepLinkUrl = $"qasmartclass://bulletin/{bulletinId}";
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(deepLinkUrl, QRCodeGenerator.ECCLevel.Q))
                using (QRCode qrCode = new QRCode(qrCodeData))
                using (Bitmap qrBitmap = qrCode.GetGraphic(20, System.Drawing.Color.Black, System.Drawing.Color.White, true))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        qrBitmap.Save(ms, ImageFormat.Png);
                        ms.Position = 0;
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = ms;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze(); 
                        ImgQrCode.Source = bitmapImage;
                    }
                }
            }
            catch { /* Ignore QR errors in slideshow */ }
        }

        private bool _isPaused = false;
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.Space)
            {
                _isPaused = !_isPaused;
                if (_isPaused)
                {
                    _slideTimer?.Stop();
                    ProgressBar.BeginAnimation(FrameworkElement.WidthProperty, null);
                    Log.Information("Slideshow tạm dừng");
                }
                else
                {
                    _slideTimer?.Start();
                    ShowNextSlide();
                    Log.Information("Slideshow chạy tiếp");
                }
            }
            else if (e.Key == Key.Left)
            {
                _slideTimer?.Stop();
                _currentIndex -= 2;
                if (_currentIndex < -1) _currentIndex = _slides.Count - 2;
                ShowNextSlide();
                if (!_isPaused) _slideTimer?.Start();
            }
            else if (e.Key == Key.Right)
            {
                _slideTimer?.Stop();
                ShowNextSlide();
                if (!_isPaused) _slideTimer?.Start();
            }
        }
    }
}

