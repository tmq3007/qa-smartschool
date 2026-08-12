using QASmartClass.Data;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.StudentClient.Views
{
    public partial class DiaryPage : Page
    {
        private AppDbContext? _db;
        private int _currentStudentId = 0;

        public DiaryPage()
        {
            InitializeComponent();
            DpDate.SelectedDate = DateTime.Now.Date;
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _db = new AppDbContext();
            var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(_db);
            _currentStudentId = identityService.GetCurrentStudent().Id;
            if (_currentStudentId <= 0) { MessageBox.Show("Vui lòng đăng nhập!"); return; }
            LoadRecent();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _db?.Dispose();
            _db = null;
        }

        private void DpDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadDiary(DpDate.SelectedDate ?? DateTime.Now.Date);
        }

        private void LoadDiary(DateTime date)
        {
            if (_db == null) return;
            try
            {
                var diary = _db.LearningDiaries.FirstOrDefault(d => d.StudentId == _currentStudentId && d.Date == date);
                if (diary != null)
                {
                    TxtContent.Text = diary.Content;
                    TxtGoals.Text = diary.Goals;
                    TxtReflection.Text = diary.Reflection;
                    
                    if (diary.Mood == "Happy") RbHappy.IsChecked = true;
                    else if (diary.Mood == "Stressed") RbStressed.IsChecked = true;
                    else RbNeutral.IsChecked = true;
                }
                else
                {
                    TxtContent.Text = "";
                    TxtGoals.Text = "";
                    TxtReflection.Text = "";
                    RbHappy.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi load LearningDiary cho ngày {Date}", date);
            }
        }

        private void LoadRecent()
        {
            if (_db == null) return;
            try
            {
                var recent = _db.LearningDiaries
                                .Where(d => d.StudentId == _currentStudentId)
                                .OrderByDescending(d => d.Date)
                                .Take(5)
                                .ToList()
                                .Select(d => new { d.Date, DateDisplay = $"Ngày {d.Date:dd/MM/yyyy}", d.Mood, d.Content })
                                .ToList();
                LvRecentDiaries.ItemsSource = recent;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi load danh sách LearningDiaries gần đây");
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            try
            {
                DateTime date = DpDate.SelectedDate ?? DateTime.Now.Date;
                string mood = "Happy";
                if (RbNeutral.IsChecked == true) mood = "Neutral";
                else if (RbStressed.IsChecked == true) mood = "Stressed";

                var diary = _db.LearningDiaries.FirstOrDefault(d => d.StudentId == _currentStudentId && d.Date == date);
                
                if (diary != null)
                {
                    // Edit mode
                    diary.Content = TxtContent.Text.Trim();
                    diary.Goals = TxtGoals.Text.Trim();
                    diary.Reflection = TxtReflection.Text.Trim();
                    diary.Mood = mood;
                }
                else
                {
                    // Create mode
                    diary = new LearningDiary
                    {
                        StudentId = _currentStudentId,
                        Date = date,
                        Content = TxtContent.Text.Trim(),
                        Goals = TxtGoals.Text.Trim(),
                        Reflection = TxtReflection.Text.Trim(),
                        Mood = mood
                    };
                    _db.LearningDiaries.Add(diary);
                }

                _db.SaveChanges();

                // --- SEL Sync: Tự động quét từ khóa và đồng bộ cảnh báo tâm lý học sinh ---
                try
                {
                    // Kiểm tra sự đồng ý của học sinh (SEL Consent)
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string consentPath = Path.Combine(appData, "QASmartClass", $"sel_consent_{_currentStudentId}.dat");
                    bool hasConsent = false;
                    if (File.Exists(consentPath))
                    {
                        hasConsent = File.ReadAllText(consentPath).Trim() == "Approved";
                    }
                    else
                    {
                        var result = MessageBox.Show(
                            "Để hỗ trợ sức khỏe tinh thần học sinh, hệ thống tích hợp bộ lọc bảo vệ SEL tự động hỗ trợ phát hiện dấu hiệu căng thẳng cực độ từ nhật ký học tập để chuyển phòng tư vấn học đường. Bạn có đồng ý bật bộ lọc này không?",
                            "Chia sẻ cảm xúc & Bảo mật SEL",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                        hasConsent = result == MessageBoxResult.Yes;
                        Directory.CreateDirectory(Path.GetDirectoryName(consentPath)!);
                        File.WriteAllText(consentPath, hasConsent ? "Approved" : "Rejected");
                    }

                    if (hasConsent)
                    {
                        string contentLower = (TxtContent.Text + " " + TxtReflection.Text + " " + TxtGoals.Text).ToLower();
                        string[] sensitiveKeywords = { "đánh nhau", "tự kỷ", "bắt nạt", "áp lực" };
                        string[] criticalKeywords = { "tự tử", "tự sát", "muốn chết", "hủy hoại bản thân", "cắt tay", "uống thuốc ngủ", "bạo hành" };
                        
                        var detected = sensitiveKeywords.Where(k => contentLower.Contains(k)).ToList();
                        var detectedCritical = criticalKeywords.Where(k => contentLower.Contains(k)).ToList();

                        if (mood == "Stressed" || detected.Any() || detectedCritical.Any())
                        {
                            string rLevel = mood == "Stressed" ? "High" : "Medium";
                            if (detectedCritical.Any())
                            {
                                rLevel = "Critical";
                                detected.AddRange(detectedCritical);
                            }
                            else if (detected.Contains("tự kỷ"))
                            {
                                rLevel = "High";
                            }

                            var todayWarning = _db.StudentMentalHealthRecords.FirstOrDefault(r => 
                                r.StudentId == _currentStudentId && 
                                r.RecordedAt.Date == date.Date && 
                                r.Notes.StartsWith("[Tự động từ Nhật ký học tập]"));

                            if (todayWarning != null)
                            {
                                // Cập nhật lại bản ghi cũ của ngày hôm nay
                                todayWarning.MoodScore = mood == "Stressed" ? 2 : (mood == "Neutral" ? 5 : 8);
                                todayWarning.Notes = "[Tự động từ Nhật ký học tập] Phát hiện trạng thái cảm xúc bất thường hoặc từ khóa nhạy cảm. (Nội dung chi tiết nhật ký gốc được bảo mật).";
                                todayWarning.RiskLevel = rLevel;
                                todayWarning.DetectedKeywords = string.Join(", ", detected);
                                todayWarning.RecordedAt = DateTime.Now;
                            }
                            else
                            {
                                // Tạo mới nếu chưa có bản ghi nào trong ngày
                                var mentalRecord = new StudentMentalHealthRecord
                                {
                                    StudentId = _currentStudentId,
                                    MoodScore = mood == "Stressed" ? 2 : (mood == "Neutral" ? 5 : 8),
                                    Notes = "[Tự động từ Nhật ký học tập] Phát hiện trạng thái cảm xúc bất thường hoặc từ khóa nhạy cảm. (Nội dung chi tiết nhật ký gốc được bảo mật).",
                                    RiskLevel = rLevel,
                                    DetectedKeywords = string.Join(", ", detected),
                                    RecordedAt = DateTime.Now,
                                    InterventionLevel = 0,
                                    InterventionNotes = "",
                                    IsNotified = false
                                };
                                _db.StudentMentalHealthRecords.Add(mentalRecord);
                            }
                            _db.SaveChanges();
                            Log.Information("[SEL Sync] Synced mental health record for student {StudentId} from learning diary.", _currentStudentId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("LearningDiary: Failed to sync mental health record: {Err}", ex.Message);
                }

                MessageBox.Show("✅ Đã lưu nhật ký thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadRecent();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi lưu LearningDiary.");
                MessageBox.Show("Đã xảy ra lỗi khi lưu nhật ký.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
