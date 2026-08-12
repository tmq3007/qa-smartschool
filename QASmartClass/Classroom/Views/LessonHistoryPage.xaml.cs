using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class LessonHistoryPage : Page
    {
        private int _selectedLessonId = 0;
        private int _selectedHistoryId = 0;
        private int _initialLessonId = 0;
        private List<LessonComboItem> _lessons = new();

        public LessonHistoryPage(int lessonId = 0)
        {
            InitializeComponent();
            _initialLessonId = lessonId;
            Loaded += (s, e) => LoadLessons();
        }

        // ═══════════════════════════════════════════════════════
        //  LOAD LESSONS
        // ═══════════════════════════════════════════════════════

        private void LoadLessons()
        {
            try
            {
                var db = ((QASmartTouch.App)Application.Current).Database;
                _lessons = db.Lessons
                    .OrderByDescending(l => l.UpdatedAt)
                    .Select(l => new LessonComboItem { Id = l.Id, Title = l.Title, Status = l.Status })
                    .ToList();

                cmbLesson.ItemsSource       = _lessons;
                cmbLesson.DisplayMemberPath = "Label";

                // Pre-select lesson if passed from Editor/Shell
                int preSelectIdx = _initialLessonId > 0
                    ? _lessons.FindIndex(l => l.Id == _initialLessonId)
                    : 0;
                cmbLesson.SelectedIndex = Math.Max(0, preSelectIdx);

                if (_lessons.Any())
                {
                    var selected = _lessons[Math.Max(0, preSelectIdx)];
                    _selectedLessonId = selected.Id;
                    LoadHistory(_selectedLessonId);
                }

                txtInfo.Text = $"📚 {_lessons.Count} bài giảng có lịch sử phiên bản";
            }
            catch (Exception ex)
            {
                Log.Warning("LessonHistoryPage LoadLessons error: {Err}", ex.Message);
            }
        }

        private void LessonSelected(object sender, SelectionChangedEventArgs e)
        {
            if (cmbLesson.SelectedItem is LessonComboItem item)
            {
                _selectedLessonId = item.Id;
                LoadHistory(item.Id);
            }
        }

        // ═══════════════════════════════════════════════════════
        //  LOAD HISTORY
        // ═══════════════════════════════════════════════════════

        private void LoadHistory(int lessonId)
        {
            try
            {
                var db = ((QASmartTouch.App)Application.Current).Database;
                var histories = db.LessonHistories
                    .Where(h => h.LessonId == lessonId)
                    .OrderByDescending(h => h.SavedAt)
                    .Select(h => new HistoryDisplayItem
                    {
                        Id            = h.Id,
                        LessonId      = h.LessonId,
                        TitleSnapshot = h.TitleSnapshot,
                        Status        = h.StatusSnapshot,
                        ChangeNote    = h.ChangeNote,
                        ChangedBy     = h.ChangedBy,
                        SavedAt       = h.SavedAt,
                        StatusBg      = GetStatusBg(h.StatusSnapshot),
                        StatusFg      = GetStatusFg(h.StatusSnapshot)
                    })
                    .ToList();

                historyList.ItemsSource = histories;
                UpdateTimeline(histories);

                if (!histories.Any())
                {
                    // Show placeholder if no history yet
                    historyList.ItemsSource = new[]
                    {
                        new HistoryDisplayItem
                        {
                            TitleSnapshot = "Chưa có phiên bản nào",
                            Status        = "—",
                            ChangeNote    = "Hãy lưu bài giảng để tạo phiên bản đầu tiên",
                            SavedAt       = DateTime.Now,
                            StatusBg      = "#F5F5F5", StatusFg = "#9E9E9E"
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Warning("LoadHistory error: {Err}", ex.Message);
            }
        }

        private void UpdateTimeline(List<HistoryDisplayItem> histories)
        {
            var dots = histories.Take(20).Select(h => new TimelineDot
            {
                Label = h.SavedAt.ToString("dd/MM"),
                Dot   = h.Status == "Approved" ? "#4CAF50" :
                        h.Status == "Taught"   ? "#1976D2" :
                        h.Status == "Draft"    ? "#FFC107" : "#9E9E9E",
                Tip   = $"{h.TitleSnapshot} — {h.Status} ({h.SavedAt:HH:mm})"
            }).ToList();

            timelineDots.ItemsSource = dots;
        }

        // ═══════════════════════════════════════════════════════
        //  SELECT HISTORY VERSION
        // ═══════════════════════════════════════════════════════

        private void HistoryItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is HistoryDisplayItem item)
            {
                _selectedHistoryId = item.Id;
                txtDetailTitle.Text  = item.TitleSnapshot;
                txtDetailDate.Text   = item.SavedAt.ToString("HH:mm dd/MM/yyyy");
                txtDetailStatus.Text = item.Status;
                txtDetailEditor.Text = item.ChangedBy;
                txtDetailNote.Text   = string.IsNullOrWhiteSpace(item.ChangeNote) ? "—" : item.ChangeNote;
            }
        }

        // ═══════════════════════════════════════════════════════
        //  ACTIONS
        // ═══════════════════════════════════════════════════════

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedHistoryId <= 0)
            {
                MessageBox.Show("Vui lòng chọn một phiên bản để khôi phục!", "Chọn phiên bản",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var r = MessageBox.Show(
                $"Khôi phục phiên bản: \"{txtDetailTitle.Text}\"?\n\nBài giảng hiện tại sẽ được thay thế.",
                "Khôi phục", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (r != MessageBoxResult.Yes) return;

            try
            {
                var db = ((QASmartTouch.App)Application.Current).Database;
                var hist = db.LessonHistories.Find(_selectedHistoryId);
                if (hist == null) return;

                var lesson = db.Lessons.Find(hist.LessonId);
                if (lesson == null) return;

                // Save current as a backup history entry first
                db.LessonHistories.Add(new LessonHistory
                {
                    LessonId       = lesson.Id,
                    TitleSnapshot  = lesson.Title,
                    StatusSnapshot = lesson.Status,
                    ChangedBy      = "GV (auto-backup before restore)",
                    ChangeNote     = $"Backup trước khôi phục lúc {DateTime.Now:HH:mm}",
                    SavedAt        = DateTime.Now
                });

                // Restore title & status
                lesson.Title     = hist.TitleSnapshot;
                lesson.Status    = hist.StatusSnapshot;
                lesson.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                MessageBox.Show(
                    $"✅ Đã khôi phục phiên bản!\n\nBài giảng: \"{lesson.Title}\"\nTrạng thái: {lesson.Status}",
                    "Khôi phục thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadHistory(_selectedLessonId);
                Log.Information("Lesson {Id} restored to history {HId}", lesson.Id, _selectedHistoryId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Restore error");
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteVersion_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedHistoryId <= 0) return;
            var r = MessageBox.Show("Xoá phiên bản này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (r != MessageBoxResult.Yes) return;
            try
            {
                var db = ((QASmartTouch.App)Application.Current).Database;
                var hist = db.LessonHistories.Find(_selectedHistoryId);
                if (hist != null) { db.LessonHistories.Remove(hist); db.SaveChanges(); }
                _selectedHistoryId = 0;
                LoadHistory(_selectedLessonId);
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadHistory(_selectedLessonId);

        // ═══════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════
        private static string GetStatusBg(string s) => s switch
        {
            "Approved"        => "#E8F5E9", "Taught" => "#E3F2FD",
            "PendingApproval" => "#FFF3E0", _ => "#F5F5F5"
        };
        private static string GetStatusFg(string s) => s switch
        {
            "Approved"        => "#2E7D32", "Taught" => "#1565C0",
            "PendingApproval" => "#E65100", _ => "#666666"
        };
    }

    // ─── Local View Models ─────────────────────────────────────
    public class LessonComboItem
    {
        public int    Id     { get; set; }
        public string Title  { get; set; } = "";
        public string Status { get; set; } = "";
        public string Label  => $"[{Status}] {Title}";
    }

    public class HistoryDisplayItem
    {
        public int      Id            { get; set; }
        public int      LessonId      { get; set; }
        public string   TitleSnapshot { get; set; } = "";
        public string   Status        { get; set; } = "";
        public string   ChangeNote    { get; set; } = "";
        public string   ChangedBy     { get; set; } = "";
        public DateTime SavedAt       { get; set; }
        public string   StatusBg      { get; set; } = "#F5F5F5";
        public string   StatusFg      { get; set; } = "#666";
    }

    public class TimelineDot
    {
        public string Dot   { get; set; } = "#9E9E9E";
        public string Label { get; set; } = "";
        public string Tip   { get; set; } = "";
    }
}
