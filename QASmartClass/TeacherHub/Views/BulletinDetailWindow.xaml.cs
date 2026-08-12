using QASmartClass.Data;
using QASmartClass.Services;
using QRCoder;
using Serilog;
using QASmartClass.Staff.Services;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QASmartClass.TeacherHub.Views
{
    public partial class BulletinDetailWindow : Window
    {
        private readonly Bulletin _bulletin;
        private readonly BulletinService _bulletinService;

        public BulletinDetailWindow(Bulletin bulletin, BulletinService bulletinService)
        {
            InitializeComponent();
            _bulletin = bulletin;
            _bulletinService = bulletinService;
            
            Loaded += BulletinDetailWindow_Loaded;
        }

        private void BulletinDetailWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Format the object to match binding in UI
                var displayItem = new
                {
                    _bulletin.Title,
                    _bulletin.Content,
                    _bulletin.CreatedBy,
                    CreatedAtStr = _bulletin.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    Audience = _bulletin.Audience switch { "All" => "Tất cả", "GV" => "Giáo viên", "HS" => "Học sinh", "DV" => "Đoàn viên", "PH" => "Phụ huynh", "NV" => "Nhân viên", _ => "Học sinh" },
                    Priority = _bulletin.Priority.ToUpper(),
                    PriorityColor = _bulletin.Priority == "High" ? "#EF4444" : _bulletin.Priority == "Low" ? "#94A3B8" : "#3B82F6",
                    _bulletin.ViewCount,
                    _bulletin.LikeCount
                };
                
                this.DataContext = displayItem;
                
                // Load RichText Content
                try
                {
                    if (_bulletin.Content.Contains("</FlowDocument>"))
                    {
                        var stringReader = new StringReader(_bulletin.Content);
                        var xmlReader = System.Xml.XmlReader.Create(stringReader);
                        var document = (System.Windows.Documents.FlowDocument)System.Windows.Markup.XamlReader.Load(xmlReader);
                        RtbContent.Document = document;
                    }
                    else
                    {
                        // Fallback plain text
                        RtbContent.Document.Blocks.Clear();
                        RtbContent.Document.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(_bulletin.Content)));
                    }
                }
                catch
                {
                    RtbContent.Document.Blocks.Clear();
                    RtbContent.Document.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(_bulletin.Content)));
                }

                // Load Image
                if (!string.IsNullOrEmpty(_bulletin.ImageUrl) && File.Exists(_bulletin.ImageUrl))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_bulletin.ImageUrl, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    ImgBulletin.Source = bitmap;
                    ImgBulletin.Visibility = Visibility.Visible;
                }
                
                GenerateAndShowQrCode();
                LoadComments();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi load BulletinDetailWindow");
            }
        }

        private void GenerateAndShowQrCode()
        {
            try
            {
                // URL Deep link (Task 1.4)
                string deepLinkUrl = $"qasmartclass://bulletin/{_bulletin.Id}";

                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(deepLinkUrl, QRCodeGenerator.ECCLevel.Q))
                using (QRCode qrCode = new QRCode(qrCodeData))
                using (Bitmap qrBitmap = qrCode.GetGraphic(20, System.Drawing.Color.Black, System.Drawing.Color.White, true))
                {
                    // Convert System.Drawing.Bitmap to WPF BitmapImage
                    using (MemoryStream ms = new MemoryStream())
                    {
                        qrBitmap.Save(ms, ImageFormat.Png);
                        ms.Position = 0;
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = ms;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze(); // Make cross-thread safe just in case
                        
                        ImgQrCode.Source = bitmapImage;
                    }
                }
                Log.Information("Generated QR Code for Bulletin #{Id}", _bulletin.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi tạo QR code cho Bulletin #{Id}", _bulletin.Id);
            }
        }

        // --- PHASE 2: Engagement ---
        private bool _isLiked = false;

        private void LoadComments()
        {
            try
            {
                var comments = _bulletinService.GetComments(_bulletin.Id);
                IcComments.ItemsSource = comments;
            }
            catch (Exception ex) { Log.Error(ex, "Lỗi load comments"); }
        }

        private void BtnToggleLike_Click(object sender, RoutedEventArgs e)
        {
            _isLiked = !_isLiked;
            _bulletinService.ToggleLike(_bulletin.Id, _isLiked);
            
            // BUG-06 FIX: Reload from DB instead of manually incrementing (avoids double-counting)
            var refreshed = _bulletinService.GetBulletinById(_bulletin.Id);
            if (refreshed != null)
            {
                _bulletin.LikeCount = refreshed.LikeCount;
                _bulletin.ViewCount = refreshed.ViewCount;
            }
            
            BtnToggleLike.Content = _isLiked ? "❤️ Đã thích bản tin" : "🤍 Thích bản tin này";
            BtnToggleLike.Background = _isLiked ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 202, 202)) 
                                                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 242, 242));
            
            // Refresh DataContext
            this.DataContext = new
            {
                _bulletin.Title,
                _bulletin.Content,
                _bulletin.CreatedBy,
                CreatedAtStr = _bulletin.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                Audience = _bulletin.Audience switch { "All" => "Tất cả", "GV" => "Giáo viên", "HS" => "Học sinh", "DV" => "Đoàn viên", "PH" => "Phụ huynh", "NV" => "Nhân viên", _ => "Học sinh" },
                Priority = _bulletin.Priority.ToUpper(),
                PriorityColor = _bulletin.Priority == "High" ? "#EF4444" : _bulletin.Priority == "Low" ? "#94A3B8" : "#3B82F6",
                _bulletin.ViewCount,
                _bulletin.LikeCount
            };
        }

        private void BtnSendComment_Click(object sender, RoutedEventArgs e)
        {
            string content = TxtComment.Text.Trim();
            if (string.IsNullOrEmpty(content)) return;

            // L?y user hi?n tại (gi? l?p cho Staff)
            string currentUser = StaffSession.CurrentUser?.FullName ?? "Giáo viên / Nhân viên"; // L?y t? StaffSession

            _bulletinService.AddComment(_bulletin.Id, content, currentUser);
            TxtComment.Text = string.Empty;
            
            LoadComments();
        }

        private void BtnExportImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PNG Image (*.png)|*.png",
                    Title = "Lưu Bảng Tin Thành Ảnh",
                    FileName = $"BanTin_{_bulletin.Id}_{DateTime.Now:yyyyMMdd}.png"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var visual = PrintableArea;
                    if (visual != null)
                    {
                        double width = visual.ActualWidth;
                        double height = visual.ActualHeight;
                        if (width <= 0) width = 600;
                        if (height <= 0) height = 800;

                        var renderTarget = new RenderTargetBitmap(
                            (int)width, (int)height,
                            96, 96, PixelFormats.Pbgra32);

                        // Use a DrawingVisual to draw a white background first
                        var drawingVisual = new DrawingVisual();
                        using (var drawingContext = drawingVisual.RenderOpen())
                        {
                            drawingContext.DrawRectangle(System.Windows.Media.Brushes.White, null, new Rect(0, 0, width, height));
                            drawingContext.DrawRectangle(new VisualBrush(visual), null, new Rect(0, 0, width, height));
                        }

                        renderTarget.Render(drawingVisual);

                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                        using (var stream = File.Create(saveDialog.FileName))
                        {
                            encoder.Save(stream);
                        }

                        MessageBox.Show("Đã lưu ảnh thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi xuất ảnh bản tin");
                MessageBox.Show("Có lỗi xảy ra khi lưu ảnh.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

