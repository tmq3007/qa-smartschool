using QASmartClass.Data;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows.Controls;

namespace QASmartClass.TeacherHub.Views
{
    public partial class QuestionDetailWindow : Window
    {
        public string ActionTaken { get; private set; } = "None";
        public string Comment { get; private set; } = "";

        public QuestionDetailWindow(QuestionBankItem item)
        {
            InitializeComponent();
            PopulateData(item);
            
            // Ẩn các nút Duyệt/Từ chối nếu người dùng hiện tại không có quyền quản lý
            if (!QASmartClass.Services.UserSessionService.Instance.IsManager)
            {
                PnlActionButtons.Visibility = Visibility.Collapsed;
            }
        }

        private void PopulateData(QuestionBankItem item)
        {
            TxtMeta.Text = $"Mã câu hỏi: ID {item.Id} | Tạo bởi: {item.CreatedBy} | Ngày tạo: {item.CreatedAt:dd/MM/yyyy}";
            
            // Trạng thái duyệt
            TxtStatus.Text = item.ApprovalStatus switch
            {
                "Approved" => "ĐÃ PHÊ DUYỆT",
                "Pending" => "CHỜ PHÊ DUYỆT",
                "Rejected" => "YÊU CẦU CHỈNH SỬA",
                _ => item.ApprovalStatus.ToUpper()
            };
            BdrStatus.Background = new SolidColorBrush(item.ApprovalStatus switch
            {
                "Approved" => Color.FromRgb(220, 252, 231), // #DCFCE7
                "Pending" => Color.FromRgb(254, 249, 195),  // #FEF9C3
                _ => Color.FromRgb(254, 226, 226)           // #FEE2E2
            });
            TxtStatus.Foreground = new SolidColorBrush(item.ApprovalStatus switch
            {
                "Approved" => Color.FromRgb(22, 163, 74),  // #16A34A
                "Pending" => Color.FromRgb(202, 138, 4),   // #CA8A04
                _ => Color.FromRgb(220, 38, 38)            // #DC2626
            });

            TxtSubject.Text = item.Subject;
            TxtGrade.Text = item.Grade;
            TxtType.Text = item.QuestionType;
            TxtDifficulty.Text = item.Difficulty switch
            {
                "Easy" => "Dễ",
                "Medium" => "Trung bình",
                "Hard" => "Khó",
                _ => item.Difficulty
            };
            TxtPoints.Text = $"{item.Points} điểm";
            TxtTimeLimit.Text = $"{item.TimeLimitSeconds} giây";

            // Đề bài
            TxtContent.Text = item.Content;

            // Đáp án lựa chọn (OptionsJson)
            try
            {
                var options = JsonSerializer.Deserialize<List<string>>(item.OptionsJson ?? "[]");
                if (options != null && options.Count > 0)
                {
                    LstOptions.ItemsSource = options;
                    BdrOptions.Visibility = Visibility.Visible;
                    LblOptions.Visibility = Visibility.Visible;
                }
                else
                {
                    BdrOptions.Visibility = Visibility.Collapsed;
                    LblOptions.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                BdrOptions.Visibility = Visibility.Collapsed;
                LblOptions.Visibility = Visibility.Collapsed;
            }

            // Đáp án đúng & Giải thích
            TxtCorrectAnswer.Text = item.CorrectAnswer;
            TxtExplanation.Text = string.IsNullOrWhiteSpace(item.Explanation) ? "(Không có lời giải thích)" : item.Explanation;

            // Nhận xét cũ (nếu có)
            if (!string.IsNullOrEmpty(item.ReviewComments))
            {
                PnlReviewComments.Visibility = Visibility.Visible;
                TxtReviewComments.Text = item.ReviewComments;
            }
            else
            {
                PnlReviewComments.Visibility = Visibility.Collapsed;
            }

            // Tải hình ảnh minh họa (v4.2)
            ParseAndLoadImage(item.Content, item.Explanation);
        }

        private string? _detectedImagePath;

        private void ParseAndLoadImage(string content, string explanation)
        {
            _detectedImagePath = null;
            
            // Tìm các mẫu ![caption](path) hoặc [IMG:path]
            var match = Regex.Match(content, @"!\[([^\]]*)\]\(([^\)]+)\)");
            string caption = "";
            string path = "";
            
            if (match.Success)
            {
                caption = match.Groups[1].Value;
                path = match.Groups[2].Value;
            }
            else
            {
                match = Regex.Match(explanation, @"!\[([^\]]*)\]\(([^\)]+)\)");
                if (match.Success)
                {
                    caption = match.Groups[1].Value;
                    path = match.Groups[2].Value;
                }
                else
                {
                    match = Regex.Match(content, @"\[IMG:\s*([^\]]+)\]", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        path = match.Groups[1].Value;
                        caption = "Hình ảnh minh họa";
                    }
                    else
                    {
                        match = Regex.Match(explanation, @"\[IMG:\s*([^\]]+)\]", RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            path = match.Groups[1].Value;
                            caption = "Hình ảnh minh họa";
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    _detectedImagePath = path;
                    
                    // Nạp ảnh
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    
                    if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        bitmap.UriSource = new Uri(path);
                    }
                    else
                    {
                        // Thư mục học liệu hoặc thư mục chạy hiện tại
                        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(baseDir, path);
                        if (!File.Exists(fullPath))
                        {
                            string assetsPath = Path.Combine(baseDir, "Assets", path);
                            if (File.Exists(assetsPath)) fullPath = assetsPath;
                        }
                        
                        bitmap.UriSource = new Uri(fullPath);
                    }
                    
                    bitmap.EndInit();
                    ImgQuestion.Source = bitmap;
                    TxtImageCaption.Text = string.IsNullOrEmpty(caption) ? "Hình ảnh minh họa" : caption;
                    
                    LblQuestionImage.Visibility = Visibility.Visible;
                    BdrQuestionImage.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning("[QuestionDetail] Load image failed: {Err}", ex.Message);
                    LblQuestionImage.Visibility = Visibility.Collapsed;
                    BdrQuestionImage.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                LblQuestionImage.Visibility = Visibility.Collapsed;
                BdrQuestionImage.Visibility = Visibility.Collapsed;
            }
        }

        private void ImgQuestion_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && ImgQuestion.Source != null)
            {
                // Mở cửa sổ xem ảnh lớn
                var viewWindow = new Window
                {
                    Title = "Xem hình ảnh minh họa đầy đủ",
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)), // Dark Slate
                };
                
                var scrollViewer = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                };
                
                var img = new Image
                {
                    Source = ImgQuestion.Source,
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(20),
                };
                
                scrollViewer.Content = img;
                viewWindow.Content = scrollViewer;
                viewWindow.ShowDialog();
            }
        }

        private void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            ActionTaken = "Approve";
            DialogResult = true;
            Close();
        }

        private void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            var rejectDlg = new RejectCommentWindow
            {
                Owner = parentWindow
            };
            if (rejectDlg.ShowDialog() == true)
            {
                ActionTaken = "Reject";
                Comment = rejectDlg.Comment;
                DialogResult = true;
                Close();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            ActionTaken = "None";
            DialogResult = false;
            Close();
        }
    }
}
