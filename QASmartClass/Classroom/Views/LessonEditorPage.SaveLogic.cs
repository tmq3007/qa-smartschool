using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class LessonEditorPage
    {
        private void AutoSave_Tick(object? sender, EventArgs e)
        {
            if (HasUnsavedChanges && _currentLessonId > 0 && !string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                PerformSave(isAutoSave: true);
            }
        }

        private void LoadLesson(int id)
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var lesson = app.Database.Lessons.Find(id);
                if (lesson == null) return;

                txtTitle.Text = lesson.Title;
                txtDescription.Text = lesson.Description;
                // Match subject combobox
                for (int i = 0; i < cmbSubject.Items.Count; i++)
                    if ((cmbSubject.Items[i] as ComboBoxItem)?.Content?.ToString() == lesson.Subject)
                    { cmbSubject.SelectedIndex = i; break; }
                for (int i = 0; i < cmbGrade.Items.Count; i++)
                    if ((cmbGrade.Items[i] as ComboBoxItem)?.Content?.ToString() == lesson.Grade)
                    { cmbGrade.SelectedIndex = i; break; }

                // ═══ Load lesson content blocks from DB ═══
                var contents = app.Database.LessonContents
                    .Where(c => c.LessonId == id)
                    .OrderBy(c => c.SortOrder)
                    .ToList();

                if (contents.Any())
                {
                    emptyPlaceholder.Visibility = Visibility.Collapsed;
                    foreach (var block in contents)
                    {
                        RenderContentBlock(block.ContentType, block.Data);
                    }
                }

                Log.Information("Lesson loaded for edit: {Title} (ID={Id}), {BlockCount} blocks", lesson.Title, id, contents.Count);
                HasUnsavedChanges = false; // Reset sau khi load
            }
            catch (Exception ex)
            {
                Log.Warning("LoadLesson error: {Error}", ex.Message);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            PerformSave(isAutoSave: false);
        }

        private void PerformSave(bool isAutoSave = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtTitle.Text))
                {
                    if (!isAutoSave)
                        MessageBox.Show("Vui lòng nhập tiêu đề bài giảng!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var app = (QASmartTouch.App)Application.Current;
                Lesson lesson;

                if (_currentLessonId > 0)
                {
                    lesson = app.Database.Lessons.Find(_currentLessonId) ?? new Lesson();
                }
                else
                {
                    lesson = new Lesson { CreatedAt = DateTime.Now };
                    app.Database.Lessons.Add(lesson);
                }

                lesson.Title = txtTitle.Text.Trim();
                lesson.Subject = (cmbSubject.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Toán";
                lesson.Grade = (cmbGrade.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Lớp 10";
                lesson.Description = txtDescription.Text.Trim();
                lesson.UpdatedAt = DateTime.Now;

                app.Database.SaveChanges();
                _currentLessonId = lesson.Id;

                // ── v4.1: PERSIST CONTENT BLOCKS TO DB ──
                try
                {
                    // Remove old content blocks
                    var oldBlocks = app.Database.LessonContents
                        .Where(c => c.LessonId == lesson.Id).ToList();
                    app.Database.LessonContents.RemoveRange(oldBlocks);

                    // Save current blocks from editor
                    int sortOrder = 0;
                    foreach (var child in blocksPanel.Children)
                    {
                        if (child is Border blockBorder && blockBorder != emptyPlaceholder)
                        {
                            var contentType = "Text";
                            var data = "";
                            var tag = blockBorder.Tag?.ToString() ?? "";

                            if (tag.Contains("|"))
                            {
                                var parts = tag.Split('|', 2);
                                contentType = parts[0];
                                data = parts.Length > 1 ? parts[1] : "";
                            }
                            else
                            {
                                // Extract text from RichTextBox
                                contentType = "Text";
                                var rtb = FindChild<RichTextBox>(blockBorder);
                                if (rtb != null)
                                    data = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd).Text.Trim();
                            }

                            if (!string.IsNullOrWhiteSpace(data) || !string.IsNullOrWhiteSpace(contentType))
                            {
                                app.Database.LessonContents.Add(new LessonContent
                                {
                                    LessonId = lesson.Id,
                                    ContentType = contentType,
                                    Data = data,
                                    SortOrder = sortOrder++
                                });
                            }
                        }
                    }
                    app.Database.SaveChanges();
                }
                catch (Exception ex2)
                {
                    Log.Warning("Save content blocks warning: {Err}", ex2.Message);
                }

                // ── v4: Snapshot version history ──
                try
                {
                    var hist = new LessonHistory
                    {
                        LessonId       = lesson.Id,
                        TitleSnapshot  = lesson.Title,
                        StatusSnapshot = lesson.Status,
                        ChangedBy      = "GV",
                        ChangeNote     = $"Lưu lần {DateTime.Now:HH:mm dd/MM/yyyy}",
                        SavedAt        = DateTime.Now
                    };
                    app.Database.LessonHistories.Add(hist);
                    app.Database.SaveChanges();
                }
                catch { /* history is non-critical */ }

                UpdateStats();
                HasUnsavedChanges = false; // Reset sau khi lưu thành công
                if (!isAutoSave)
                {
                    MessageBox.Show($"💾 Đã lưu: '{lesson.Title}' ({_blockCount} blocks)!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                Log.Information(isAutoSave ? "Lesson auto-saved: {Title} (ID={Id})" : "Lesson saved: {Title} (ID={Id}), {Blocks} blocks", lesson.Title, lesson.Id, _blockCount);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Save lesson error");
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
