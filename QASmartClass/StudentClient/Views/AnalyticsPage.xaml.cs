using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace QASmartClass.StudentClient.Views
{
    public partial class AnalyticsPage : Page
    {
        private readonly AppDbContext _db;
        private readonly LearningAnalyticsService _analyticsService;
        private bool _isUpdatingFilters = false;

        public AnalyticsPage()
        {
            InitializeComponent();
            
            // Lấy DbContext từ App
            if (Application.Current is QASmartTouch.App app && app.Database != null)
            {
                _db = app.Database;
                _analyticsService = new LearningAnalyticsService(_db);
                Loaded += AnalyticsPage_Loaded;
            }
            else
            {
                txtTrend.Text = "Không thể kết nối cơ sở dữ liệu.";
            }
        }

        private void AnalyticsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingFilters) return;
            UpdateFilteredChart();
        }

        private void UpdateFilteredChart()
        {
            try
            {
                if (_db == null || _analyticsService == null) return;

                var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(_db);
                var (studentId, studentCode, studentName) = identityService.GetCurrentStudent();

                var timeline = _analyticsService.GetScoreTimeline(studentId);

                // Apply Semester Filter
                if (cbSemesterFilter.SelectedItem is ComboBoxItem selectedSemItem)
                {
                    var semTag = selectedSemItem.Tag?.ToString();
                    if (semTag == "HK1")
                    {
                        timeline = timeline.Where(t => t.Date.Month >= 8 || t.Date.Month == 1).ToList();
                    }
                    else if (semTag == "HK2")
                    {
                        timeline = timeline.Where(t => t.Date.Month >= 2 && t.Date.Month <= 7).ToList();
                    }
                }

                // Apply Subject Filter
                string selectedSub = null;
                if (cbSubjectFilter.SelectedItem is string s)
                {
                    selectedSub = s;
                }
                else if (cbSubjectFilter.SelectedItem is ComboBoxItem cbi)
                {
                    selectedSub = cbi.Content?.ToString();
                }

                if (!string.IsNullOrEmpty(selectedSub) && selectedSub != "Tất cả môn học")
                {
                    timeline = timeline.Where(t => t.Subject == selectedSub).ToList();
                }

                // Take last 10 to fit in the chart
                var chartTimelineData = timeline.TakeLast(10).ToList();

                if (chartTimelineData.Any())
                {
                    var chartData = chartTimelineData.Select(t => {
                        string colorStart = "#3B82F6"; // Blue (Default / Middle)
                        string colorMid = "#60A5FA";
                        string colorEnd = "#93C5FD";

                        if (t.Percentage >= 80)
                        {
                            colorStart = "#10B981"; // Emerald Green
                            colorMid = "#34D399";
                            colorEnd = "#6EE7B7";
                        }
                        else if (t.Percentage < 50)
                        {
                            colorStart = "#EF4444"; // Red
                            colorMid = "#F87171";
                            colorEnd = "#FCA5A5";
                        }

                        return new
                        {
                            t.Subject,
                            t.Topic,
                            ScoreText = $"{t.Score}/{t.MaxScore}",
                            ChartHeight = Math.Max(20, t.Percentage * 2),
                            ColorStart = colorStart,
                            ColorMid = colorMid,
                            ColorEnd = colorEnd
                        };
                    }).ToList();

                    chartTimeline.ItemsSource = chartData;
                    gridChartArea.Visibility = Visibility.Visible;
                    borderNoChartData.Visibility = Visibility.Collapsed;
                }
                else
                {
                    chartTimeline.ItemsSource = null;
                    gridChartArea.Visibility = Visibility.Collapsed;
                    borderNoChartData.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Log.Error("UpdateFilteredChart error: {Err}", ex.Message);
            }
        }

        private void LoadData()
        {
            try
            {
                // Resolve student identity via StudentIdentityService
                var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(_db);
                var (studentId, studentCode, studentName) = identityService.GetCurrentStudent();

                // 1. Trend
                txtTrend.Text = _analyticsService.GetTrend(studentId);

                // Populate cbSubjectFilter with dynamic subjects
                _isUpdatingFilters = true;
                var allScores = _analyticsService.GetScoreTimeline(studentId);
                var subjects = allScores.Select(s => s.Subject).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToList();

                string selectedSubject = null;
                if (cbSubjectFilter.SelectedItem is string strItem)
                {
                    selectedSubject = strItem;
                }
                else if (cbSubjectFilter.SelectedItem is ComboBoxItem cboItem)
                {
                    selectedSubject = cboItem.Content?.ToString();
                }

                cbSubjectFilter.Items.Clear();
                cbSubjectFilter.Items.Add("Tất cả môn học");
                foreach (var sub in subjects)
                {
                    cbSubjectFilter.Items.Add(sub);
                }

                if (!string.IsNullOrEmpty(selectedSubject) && cbSubjectFilter.Items.Cast<object>().Any(item => (item is string s && s == selectedSubject)))
                {
                    cbSubjectFilter.SelectedItem = selectedSubject;
                }
                else
                {
                    cbSubjectFilter.SelectedIndex = 0;
                }
                _isUpdatingFilters = false;

                // 2. Timeline Chart with Dynamic Coloring
                UpdateFilteredChart();

                // 3. Achievements
                var achievements = _analyticsService.GetAchievements(studentId);
                if (achievements.Any())
                {
                    listAchievements.ItemsSource = achievements;
                    txtNoAchievements.Visibility = Visibility.Collapsed;
                }
                else
                {
                    txtNoAchievements.Visibility = Visibility.Visible;
                }

                // 4. Weak Areas & Positive Reinforcement Card
                var weakAreas = _analyticsService.GetWeakAreas(studentId);
                if (weakAreas.Any())
                {
                    var weakData = weakAreas.Select(w => new
                    {
                        w.Subject,
                        w.Topic,
                        AvgText = $"Trung bình: {w.AveragePercentage:F1}% ({w.AttemptCount} lần làm)"
                    }).ToList();
                    listWeakAreas.ItemsSource = weakData;
                    
                    cardWeakAreas.Visibility = Visibility.Visible;
                    cardNoWeakAreas.Visibility = Visibility.Collapsed;
                }
                else
                {
                    cardWeakAreas.Visibility = Visibility.Collapsed;
                    cardNoWeakAreas.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Log.Error("AnalyticsPage load error: {Err}", ex.Message);
            }
        }

        private void btnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(_db);
                var (studentId, studentCode, studentName) = identityService.GetCurrentStudent();

                var student = _db.Students.FirstOrDefault(s => s.Id == studentId);
                if (student == null)
                {
                    MessageBox.Show("Không tìm thấy thông tin học sinh trong cơ sở dữ liệu.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Tài liệu PDF (*.pdf)|*.pdf",
                    FileName = $"Bao_cao_hoc_tap_{studentCode}.pdf",
                    Title = "Chọn nơi lưu Báo cáo tiến trình học tập"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string filePath = saveDialog.FileName;

                    // Lấy dữ liệu phân tích học tập
                    var trend = _analyticsService.GetTrend(studentId);
                    var timeline = _analyticsService.GetScoreTimeline(studentId).ToList();
                    var achievements = _analyticsService.GetAchievements(studentId).ToList();
                    var weakAreas = _analyticsService.GetWeakAreas(studentId).ToList();

                    // Tính điểm trung bình toàn cục (GPA %)
                    var averages = _analyticsService.GetSubjectAverages(studentId);
                    double globalGpa = averages.Any() ? averages.Average(a => a.AverageScore) : 0;
                    int totalAssessments = averages.Any() ? averages.Sum(a => a.TotalAssessments) : 0;

                    // Nhận định sư phạm cá nhân hóa dựa trên học lực
                    string pedagogicalAdvice = "";
                    if (averages.Count == 0)
                    {
                        pedagogicalAdvice = "📊 Nhận xét sư phạm: Chưa có đủ dữ liệu đánh giá tiến trình học tập. Khuyên dùng: Hãy tích cực tham gia làm các bài kiểm tra/quiz trên hệ thống để nhận được phân tích và lời khuyên định hướng học tập chi tiết.";
                    }
                    else if (globalGpa >= 80)
                    {
                        pedagogicalAdvice = "🌟 Nhận xét sư phạm: Học sinh có năng lực tiếp thu rất tốt, kiến thức vững vàng. Khuyên dùng: Tiếp tục phát huy và thử sức với các bài tập thực hành nâng cao, nghiên cứu chuyên sâu các chuyên đề STEM để phát triển tư duy sáng tạo tối đa.";
                    }
                    else if (globalGpa >= 50)
                    {
                        pedagogicalAdvice = "📚 Nhận xét sư phạm: Học sinh nắm được kiến thức cơ bản nhưng cần rèn luyện thêm tính cẩn thận và thực hành nhiều hơn. Khuyên dùng: Tập trung ôn luyện lại các chủ đề yếu được liệt kê ở Mục 3, làm thêm các bài tập tự luyện để cải thiện điểm số.";
                    }
                    else
                    {
                        if (weakAreas.Count == 0)
                        {
                            pedagogicalAdvice = "⚠️ Nhận xét sư phạm: Kết quả học tập chung chưa đạt kỳ vọng. Hệ thống chưa ghi nhận đầy đủ lịch sử làm bài chi tiết của từng chủ đề yếu. Khuyên dùng: Liên hệ ngay với giáo viên bộ môn để cập nhật thêm dữ liệu kiểm tra và lập kế hoạch phụ đạo trực tiếp.";
                        }
                        else
                        {
                            pedagogicalAdvice = "⚠️ Nhận xét sư phạm: Kết quả học tập chưa đạt kỳ vọng. Học sinh đang gặp khó khăn trong việc tiếp thu một số chuyên đề cốt lõi. Khuyên dùng: Liên hệ ngay với giáo viên bộ môn để được hỗ trợ phụ đạo trực tiếp, lập kế hoạch ôn tập lại các kiến thức căn bản liệt kê ở Mục 3.";
                        }
                    }

                    // Sinh mã QR Code đối sánh an toàn chống giả mạo
                    byte[] qrCodeBytes = null;
                    try
                    {
                        string dataToHash = $"{studentCode}|{studentName}|{globalGpa:F2}|{totalAssessments}";
                        string hmac = QASmartClass.Utilities.CryptoHelper.ComputeHMAC(dataToHash, QASmartClass.Utilities.SecurityKeyProvider.GetHmacKey());
                        string qrText = $"{dataToHash}|{hmac}";

                        using (var qrGenerator = new QRCoder.QRCodeGenerator())
                        using (var qrCodeData = qrGenerator.CreateQrCode(qrText, QRCoder.QRCodeGenerator.ECCLevel.M))
                        using (var qrCode = new QRCoder.QRCode(qrCodeData))
                        using (System.Drawing.Bitmap qrBitmap = qrCode.GetGraphic(12, System.Drawing.Color.Black, System.Drawing.Color.White, true))
                        {
                            using (var ms = new System.IO.MemoryStream())
                            {
                                qrBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                qrCodeBytes = ms.ToArray();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to generate QR Code for student report");
                    }

                    // Cấu hình bản quyền QuestPDF Community
                    QuestPDF.Settings.License = LicenseType.Community;

                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(1.5f, Unit.Centimetre);
                            page.PageColor(Colors.White);
                            page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                            // Header báo cáo
                            page.Header().Row(row =>
                            {
                                row.RelativeItem().Column(column =>
                                {
                                    column.Item().Text("SỞ GIÁO DỤC VÀ ĐÀO TẠO").FontSize(10).SemiBold().FontColor(Colors.Grey.Medium);
                                    column.Item().Text(string.IsNullOrEmpty(student.SchoolName) ? "TRƯỜNG THPT QA SMARTCLASS" : student.SchoolName.ToUpper()).FontSize(11).Bold().FontColor(Colors.Blue.Darken3);
                                    column.Item().PaddingTop(10).Text("BÁO CÁO TIẾN TRÌNH HỌC TẬP CÁ NHÂN").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                                });
                                row.ConstantItem(80).AlignRight().Text("QA SMART").FontSize(14).Bold().FontColor(Colors.Blue.Medium);
                            });

                            // Nội dung chính
                            page.Content().PaddingVertical(0.5f, Unit.Centimetre).Column(col =>
                            {
                                // Bảng thông tin học sinh
                                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(10).Column(subCol =>
                                {
                                    subCol.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text(t => { t.Span("Họ và tên: ").Bold(); t.Span(student.FullName); });
                                        r.RelativeItem().Text(t => { t.Span("Mã học sinh: ").Bold(); t.Span(student.StudentCode); });
                                    });
                                    subCol.Item().PaddingTop(4).Row(r =>
                                    {
                                        r.RelativeItem().Text(t => { t.Span("Lớp: ").Bold(); t.Span(string.IsNullOrEmpty(student.ClassName) ? "Chưa rõ" : student.ClassName); });
                                        r.RelativeItem().Text(t => { t.Span("Ngày xuất báo cáo: ").Bold(); t.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")); });
                                    });
                                });

                                col.Item().PaddingTop(6).Text(x =>
                                {
                                    x.Span("Xu hướng chung: ").Bold().FontSize(12).FontColor(Colors.Grey.Darken3);
                                    x.Span(trend).Italic().FontSize(12).FontColor(Colors.Grey.Darken3);
                                });
                                col.Item().PaddingTop(6).Text(pedagogicalAdvice).FontSize(11).FontColor(Colors.Grey.Darken4);

                                // 2. Lịch sử điểm số (Bảng số liệu chi tiết thay thế biểu đồ)
                                col.Item().PaddingTop(20).Text("2. Kết quả các bài đánh giá gần đây").FontSize(13).Bold().FontColor(Colors.Blue.Darken3);
                                col.Item().PaddingTop(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                                
                                if (timeline.Any())
                                {
                                    col.Item().PaddingTop(8).Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(40);
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();
                                            columns.ConstantColumn(80);
                                            columns.ConstantColumn(80);
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Element(HeaderStyle).Text("STT");
                                            header.Cell().Element(HeaderStyle).Text("Môn học");
                                            header.Cell().Element(HeaderStyle).Text("Chủ đề / Bài thi");
                                            header.Cell().Element(HeaderStyle).Text("Điểm số");
                                            header.Cell().Element(HeaderStyle).Text("Tỉ lệ đạt");
                                        });

                                        int idx = 1;
                                        foreach (var t in timeline)
                                        {
                                            table.Cell().Element(CellStyle).Text(idx.ToString());
                                            table.Cell().Element(CellStyle).Text(t.Subject);
                                            table.Cell().Element(CellStyle).Text(t.Topic);
                                            table.Cell().Element(CellStyle).Text($"{t.Score}/{t.MaxScore}").Bold();
                                            table.Cell().Element(CellStyle).Text($"{t.Percentage:F1}%");
                                            idx++;
                                        }
                                    });
                                }
                                else
                                {
                                    col.Item().PaddingTop(8).Text("Chưa có lịch sử làm bài kiểm tra nào.").Italic().FontColor(Colors.Grey.Medium);
                                }

                                // 3. Điểm yếu cần cải thiện
                                col.Item().PaddingTop(20).Text("3. Các chủ đề cần cải thiện (Điểm dưới 50%)").FontSize(13).Bold().FontColor(Colors.Red.Medium);
                                col.Item().PaddingTop(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

                                if (weakAreas.Any())
                                {
                                    col.Item().PaddingTop(8).Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(40);
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();
                                            columns.ConstantColumn(120);
                                            columns.ConstantColumn(100);
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Element(HeaderStyleRed).Text("STT");
                                            header.Cell().Element(HeaderStyleRed).Text("Môn học");
                                            header.Cell().Element(HeaderStyleRed).Text("Chủ đề / Kiến thức");
                                            header.Cell().Element(HeaderStyleRed).Text("Điểm TB (%)");
                                            header.Cell().Element(HeaderStyleRed).Text("Số bài làm");
                                        });

                                        int idx = 1;
                                        foreach (var w in weakAreas)
                                        {
                                            table.Cell().Element(CellStyle).Text(idx.ToString());
                                            table.Cell().Element(CellStyle).Text(w.Subject);
                                            table.Cell().Element(CellStyle).Text(w.Topic);
                                            table.Cell().Element(CellStyle).Text($"{w.AveragePercentage:F1}%").Bold().FontColor(Colors.Red.Medium);
                                            table.Cell().Element(CellStyle).Text($"{w.AttemptCount} lần");
                                            idx++;
                                        }
                                    });
                                    col.Item().PaddingTop(8).Text("💡 Khuyên dùng: Học sinh cần ôn tập lại lý thuyết và làm thêm các bài tập tự luyện của những chủ đề yếu trên.").FontSize(10).Italic().FontColor(Colors.Grey.Medium);
                                }
                                else
                                {
                                    col.Item().PaddingTop(8).Text("🎉 Tuyệt vời! Bạn không có chủ đề nào bị đánh giá là yếu (trung bình dưới 50%). Hãy tiếp tục phát huy!").Bold().FontColor(Colors.Green.Darken2);
                                }

                                // 4. Thành tựu
                                col.Item().PaddingTop(20).Text("4. Thành tựu đã đạt được").FontSize(13).Bold().FontColor(Colors.Green.Darken3);
                                col.Item().PaddingTop(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

                                if (achievements.Any())
                                {
                                    col.Item().PaddingTop(8).Column(achCol =>
                                    {
                                        foreach (var a in achievements)
                                        {
                                            achCol.Item().PaddingVertical(4).Row(r =>
                                            {
                                                r.ConstantItem(25).Text(a.Icon).FontSize(14);
                                                r.RelativeItem().Column(ac =>
                                                {
                                                    ac.Item().Text(a.AchievementName).Bold().FontColor(Colors.Grey.Darken4);
                                                    ac.Item().Text(a.Description).FontSize(10).FontColor(Colors.Grey.Medium);
                                                });
                                            });
                                        }
                                    });
                                }
                                else
                                {
                                    col.Item().PaddingTop(8).Text("Chưa nhận được thành tựu nào. Hãy làm nhiều bài tập để nhận huy chương danh dự!").Italic().FontColor(Colors.Grey.Medium);
                                }

                                // 5. Khung xác nhận & Mã QR đối chiếu
                                col.Item().PaddingTop(25).BorderTop(1).BorderColor(Colors.Grey.Lighten2);
                                col.Item().PaddingTop(15).Row(signRow =>
                                {
                                    signRow.RelativeItem().AlignLeft().Column(c =>
                                    {
                                        c.Item().Text("GIÁO VIÊN CHỦ NHIỆM").Bold().FontSize(10);
                                        c.Item().Text("(Ký và ghi rõ họ tên)").Italic().FontSize(8).FontColor(Colors.Grey.Medium);
                                        c.Item().PaddingTop(50).Text("....................................................").FontSize(10).FontColor(Colors.Grey.Medium);
                                    });

                                    signRow.RelativeItem().AlignCenter().Column(c =>
                                    {
                                        c.Item().Text("HỌC SINH").Bold().FontSize(10).AlignCenter();
                                        c.Item().Text("(Ký và ghi rõ họ tên)").Italic().FontSize(8).FontColor(Colors.Grey.Medium).AlignCenter();
                                        c.Item().PaddingTop(50).Text("....................................................").FontSize(10).FontColor(Colors.Grey.Medium).AlignCenter();
                                    });

                                    if (qrCodeBytes != null)
                                    {
                                        signRow.ConstantItem(80).AlignRight().Column(c =>
                                        {
                                            c.Item().Text("QR XÁC THỰC").Bold().FontSize(9).AlignRight();
                                            c.Item().PaddingTop(5).AlignRight().Image(qrCodeBytes).FitWidth();
                                        });
                                    }
                                });
                            });

                            // Footer báo cáo
                            page.Footer().Row(row =>
                            {
                                row.RelativeItem().AlignLeft().Text(x =>
                                {
                                    x.Span("Trang ").FontSize(9);
                                    x.CurrentPageNumber().FontSize(9);
                                    x.Span(" / ").FontSize(9);
                                    x.TotalPages().FontSize(9);
                                    x.Span("  |  Báo cáo tự động từ Hệ thống QA SmartClass").FontSize(9);
                                });
                            });
                        });
                    })
                    .GeneratePdf(filePath);

                    MessageBox.Show("Xuất báo cáo PDF thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    Log.Information("[Export] Analytics report PDF exported successfully to: {Path}", filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất báo cáo PDF: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error(ex, "Failed to export learning progress PDF report");
            }
        }

        private static IContainer HeaderStyle(IContainer container)
        {
            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
        }

        private static IContainer HeaderStyleRed(IContainer container)
        {
            return container.DefaultTextStyle(x => x.SemiBold().FontColor(Colors.Red.Medium)).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Red.Medium);
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
        }
    }
}
