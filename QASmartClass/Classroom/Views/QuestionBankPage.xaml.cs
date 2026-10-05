using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.Data;
using QASmartClass.Classroom.Helpers;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class QuestionBankPage : Page
    {
        private List<Question> _allQuestions = new();
        private int _selectedQuizId = 0;

        public QuestionBankPage()
        {
            InitializeComponent();
            Loaded += (_, _) => LoadData();
        }

        private void LoadData()
        {
            try
            {
                // → ClassroomAppContext
                var db = ClassroomAppContext.Db;

                // Load quizzes into combo
                var quizzes = db.Quizzes.ToList();
                cmbQuiz.ItemsSource = quizzes;
                cmbQuiz.DisplayMemberPath = "Title";
                cmbQuiz.SelectedValuePath = "Id";

                if (quizzes.Any())
                {
                    cmbQuiz.SelectedIndex = 0;
                    _selectedQuizId = quizzes[0].Id;
                    LoadQuestions(_selectedQuizId);
                }
                else
                {
                    cmbQuiz.SelectedIndex = -1;
                    _selectedQuizId = 0;
                    _allQuestions = new List<Question>();
                    questionGrid.ItemsSource = null;
                    txtQuestionCount.Text = "0 câu hỏi";
                }

                UpdateStats();
                Log.Information("QuestionBankPage: {Q} quizzes loaded", quizzes.Count);
            }
            catch (Exception ex)
            {
                Log.Warning("QuestionBankPage load error: {Err}", ex.Message);
            }
        }

        private void LoadQuestions(int quizId)
        {
            try
            {
                // → ClassroomAppContext
                _allQuestions = ClassroomAppContext.Db.Questions
                    .Where(q => q.QuizId == quizId)
                    .OrderBy(q => q.SortOrder)
                    .ToList();
                questionGrid.ItemsSource = _allQuestions;
                txtQuestionCount.Text = $"{_allQuestions.Count} câu hỏi";
            }
            catch (Exception ex)
            {
                Log.Warning("LoadQuestions error: {Err}", ex.Message);
            }
        }

        private void UpdateStats()
        {
            try
            {
                // → ClassroomAppContext
                var db = ClassroomAppContext.Db;
                txtTotalQuizzes.Text = db.Quizzes.Count().ToString();
                txtTotalQuestions.Text = db.Questions.Count().ToString();
            }
            catch { }
        }

        private void QuizChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbQuiz == null || questionGrid == null) return;
            if (cmbQuiz.SelectedValue is int id)
            {
                _selectedQuizId = id;
                LoadQuestions(id);
            }
        }

        private void GoBackToQuiz_Click(object sender, RoutedEventArgs e)
        {
            // Điều hướng quay lại QuizPage (F6)
            var shell = Window.GetWindow(this) as ClassroomShell;
            if (shell != null)
            {
                Log.Information("QuestionBank sub-flow: Navigating back to Quiz (F6)");
                shell.NavigateTo("F6", addToStack: false); // Đã có trên stack
            }
        }

        private void AddQuestion_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new QuestionEditorDialog(_selectedQuizId, _allQuestions.Count);
            dlg.Owner = Window.GetWindow(this);
            if (dlg.ShowDialog() == true && dlg.Result != null)
            {
                try
                {
                    // → ClassroomAppContext
                    ClassroomAppContext.Db.Questions.Add(dlg.Result);
                    ClassroomAppContext.Db.SaveChanges();
                    LoadQuestions(_selectedQuizId);
                    UpdateStats();
                    ClassroomDialog.Info("✅ Đã thêm câu hỏi!", "Thành công");
                    Log.Information("Question added to quiz {Id}", _selectedQuizId);
                }
                catch (Exception ex)
                {
                    ClassroomDialog.Error($"Lỗi: {ex.Message}", "Lỗi");
                }
            }
        }

        private void AddQuiz_Click(object sender, RoutedEventArgs e)
        {
            var win = new Window
            {
                Title = "➕ Tạo Quiz mới", Width = 480, Height = 380,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize, Background = Brushes.White,
                Owner = Window.GetWindow(this)
            };
            var sp = new StackPanel { Margin = new Thickness(20) };

            // Title
            sp.Children.Add(MakeLabel("📝 Tên Quiz:"));
            var tbTitle = new TextBox { FontSize = 13, Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(0, 0, 0, 10), Text = "Quiz Toán 10A" };
            sp.Children.Add(tbTitle);

            // Type + Time row
            var row1 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            row1.Children.Add(MakeLabel("Loại quiz:"));
            var cmbType = new ComboBox { Width = 140, FontSize = 12, Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(8, 0, 16, 0), SelectedIndex = 0 };
            cmbType.Items.Add(new ComboBoxItem { Content = "🏆 Thi đua", Tag = "Competition" });
            cmbType.Items.Add(new ComboBoxItem { Content = "📋 Kiểm tra", Tag = "Test" });
            cmbType.Items.Add(new ComboBoxItem { Content = "📊 Khảo sát", Tag = "Survey" });
            cmbType.Items.Add(new ComboBoxItem { Content = "🎮 Trò chơi", Tag = "Game" });
            row1.Children.Add(cmbType);

            row1.Children.Add(MakeLabel("⏱ Thời gian:"));
            var tbTime = new TextBox { Width = 60, FontSize = 12, Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(8, 0, 4, 0), Text = "300", TextAlignment = TextAlignment.Center };
            row1.Children.Add(tbTime);
            row1.Children.Add(new TextBlock { Text = "giây", FontSize = 11, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center });
            sp.Children.Add(row1);

            // Subject + Grade row
            var row2 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            row2.Children.Add(MakeLabel("Môn học:"));
            var cmbSubj = new ComboBox { Width = 120, FontSize = 12, Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(8, 0, 16, 0), SelectedIndex = 0 };
            foreach (var s in new[] { "Toán", "Vật lý", "Hóa học", "Sinh học", "Ngữ văn", "Tiếng Anh", "Lịch sử", "Địa lý", "GDCD", "Tin học" })
                cmbSubj.Items.Add(s);
            row2.Children.Add(cmbSubj);

            row2.Children.Add(MakeLabel("Lớp:"));
            var cmbGrade = new ComboBox { Width = 90, FontSize = 12, Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(8, 0, 0, 0), SelectedIndex = 0 };
            foreach (var g in new[] { "Lớp 10", "Lớp 11", "Lớp 12", "Lớp 6", "Lớp 7", "Lớp 8", "Lớp 9" })
                cmbGrade.Items.Add(g);
            row2.Children.Add(cmbGrade);
            sp.Children.Add(row2);

            // Description
            sp.Children.Add(MakeLabel("📋 Mô tả (tùy chọn):"));
            var tbDesc = new TextBox { FontSize = 12, Padding = new Thickness(8, 6, 8, 6), AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 50, Margin = new Thickness(0, 0, 0, 10) };
            sp.Children.Add(tbDesc);

            // Buttons
            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
            var btnCancel = new Button
            {
                Content = "Hủy", Width = 90, Height = 36, FontSize = 13,
                Margin = new Thickness(0, 0, 8, 0), Cursor = System.Windows.Input.Cursors.Hand
            };
            btnCancel.Click += (s, ev) => { win.DialogResult = false; win.Close(); };

            var btnOk = new Button
            {
                Content = "✅ Tạo Quiz", Width = 120, Height = 36, FontSize = 13,
                Background = new SolidColorBrush(Color.FromRgb(0, 105, 92)),
                Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand
            };
            btnOk.Click += (s, ev) => { win.DialogResult = true; win.Close(); };
            btnRow.Children.Add(btnCancel); btnRow.Children.Add(btnOk);
            sp.Children.Add(btnRow);
            win.Content = sp;

            if (win.ShowDialog() == true && !string.IsNullOrWhiteSpace(tbTitle.Text))
            {
                var title = tbTitle.Text.Trim();
                try
                {
                    // → ClassroomAppContext
                    var quizType = (cmbType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Competition";
                    int.TryParse(tbTime.Text, out int timeLimit);
                    if (timeLimit <= 0) timeLimit = 300;
                    var quiz = new Quiz { Title = title, QuizType = quizType, TimeLimitSeconds = timeLimit, CreatedAt = DateTime.Now };
                    ClassroomAppContext.Db.Quizzes.Add(quiz);
                    ClassroomAppContext.Db.SaveChanges();
                    LoadData();
                    ClassroomDialog.Info($"✅ Đã tạo Quiz: {title}", "Thành công");
                }
                catch (Exception ex)
                {
                    ClassroomDialog.Error($"Lỗi: {ex.Message}", "Lỗi");
                }
            }
        }

        private void DeleteQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (questionGrid.SelectedItem is Question q)
            {
                var r = MessageBox.Show($"Xóa câu hỏi: \"{q.Content}\"?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    // → ClassroomAppContext
                    ClassroomAppContext.Db.Questions.Remove(q);
                    ClassroomAppContext.Db.SaveChanges();
                    LoadQuestions(_selectedQuizId);
                    UpdateStats();
                }
            }
        }

        private void DeleteQuiz_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedQuizId == 0)
            {
                ClassroomDialog.Warn("Vui lòng chọn một Quiz cần xóa!", "Thông báo");
                return;
            }

            try
            {
                // → ClassroomAppContext
                var db = ClassroomAppContext.Db;
                var quiz = db.Quizzes.FirstOrDefault(q => q.Id == _selectedQuizId);

                if (quiz == null)
                {
                    ClassroomDialog.Error("Không tìm thấy thông tin Quiz cần xóa.", "Lỗi");
                    return;
                }

                var confirm = MessageBox.Show($"Bạn có chắc chắn muốn xóa Quiz '{quiz.Title}' cùng với toàn bộ các câu hỏi và kết quả thi liên quan?",
                    "Xác nhận xóa Quiz", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    // 1. Xóa các câu hỏi của Quiz này
                    var relatedQuestions = db.Questions.Where(q => q.QuizId == _selectedQuizId).ToList();
                    db.Questions.RemoveRange(relatedQuestions);

                    // 2. Xóa các kết quả thi của Quiz này
                    var relatedResults = db.QuizResults.Where(r => r.QuizId == _selectedQuizId).ToList();
                    db.QuizResults.RemoveRange(relatedResults);

                    // 3. Xóa chính Quiz
                    db.Quizzes.Remove(quiz);
                    db.SaveChanges();

                    Log.Information("Quiz '{Title}' (ID: {Id}) and {QCount} questions were deleted by teacher.", quiz.Title, quiz.Id, relatedQuestions.Count);
                    ClassroomDialog.Info($"✅ Đã xóa thành công Quiz: {quiz.Title}", "Thành công");

                    // 4. Reset selection và nạp lại danh sách Quiz
                    _selectedQuizId = 0;
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                Log.Warning("DeleteQuiz error: {Err}", ex.Message);
                ClassroomDialog.Error($"Lỗi khi xóa Quiz: {ex.Message}", "Lỗi");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  IMPORT / EXPORT HANDLERS
        // ═══════════════════════════════════════════════════════════

        /// <summary>📥 Import câu hỏi từ Ngân hàng (QuestionBankItem) vào Quiz đang chọn</summary>
        private void ImportFromBank_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedQuizId == 0) { MessageBox.Show("Vui lòng chọn hoặc tạo Quiz trước!", "Thông báo"); return; }

            var wnd = new Window
            {
                Title = "📥 Import câu hỏi từ Ngân hàng",
                Width = 700, Height = 520,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this), Background = Brushes.White
            };
            var stack = new StackPanel { Margin = new Thickness(16) };

            // Category selector
            stack.Children.Add(new TextBlock { Text = "📂 Chọn danh mục Ngân hàng:", FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
            var cmbCat = new ComboBox { FontSize = 12, Padding = new Thickness(8, 5, 8, 5), Margin = new Thickness(0, 0, 0, 12) };

            using (var bankDb = new AppDbContext())
            {
                bankDb.Database.EnsureCreated();
                foreach (var cat in bankDb.QuestionBankCategories.ToList())
                {
                    int cnt = bankDb.QuestionBankItems.Count(q => q.CategoryId == cat.Id);
                    cmbCat.Items.Add(new ComboBoxItem { Content = $"📁 {cat.Name} ({cat.Subject} {cat.Grade}) — {cnt} câu", Tag = cat.Id });
                }
            }
            if (cmbCat.Items.Count == 0) { MessageBox.Show("Ngân hàng chưa có danh mục nào!\nHãy lưu câu hỏi vào NH trước.", "Thông báo"); return; }
            cmbCat.SelectedIndex = 0;
            stack.Children.Add(cmbCat);

            // Questions with checkboxes
            var questionsScroll = new ScrollViewer { MaxHeight = 300, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var questionsStack = new StackPanel();
            var checkboxes = new List<(CheckBox cb, QuestionBankItem item)>();

            void LoadBankItems()
            {
                questionsStack.Children.Clear();
                checkboxes.Clear();
                if (cmbCat.SelectedItem is ComboBoxItem ci && ci.Tag is int catId)
                {
                    using var db2 = new AppDbContext();
                    foreach (var q in db2.QuestionBankItems.Where(x => x.CategoryId == catId && x.ApprovalStatus == "Approved").ToList())
                    {
                        var row = new Border
                        {
                            Background = Brushes.White, CornerRadius = new CornerRadius(5),
                            BorderBrush = new SolidColorBrush(Color.FromRgb(228, 228, 228)),
                            BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 3), Padding = new Thickness(8, 5, 8, 5)
                        };
                        var dp = new DockPanel();
                        var cb = new CheckBox { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
                        DockPanel.SetDock(cb, Dock.Left); dp.Children.Add(cb);

                        var typeBadge = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(224, 242, 241)),
                            CornerRadius = new CornerRadius(3), Padding = new Thickness(4, 1, 4, 1), Margin = new Thickness(0, 0, 6, 0)
                        };
                        typeBadge.Child = new TextBlock { Text = q.QuestionType, FontSize = 9, FontWeight = FontWeights.Bold };
                        DockPanel.SetDock(typeBadge, Dock.Left); dp.Children.Add(typeBadge);

                        dp.Children.Add(new TextBlock
                        {
                            Text = q.Content.Length > 80 ? q.Content.Substring(0, 80) + "..." : q.Content,
                            FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center
                        });
                        row.Child = dp;
                        questionsStack.Children.Add(row);
                        checkboxes.Add((cb, q));
                    }
                    if (!checkboxes.Any())
                        questionsStack.Children.Add(new TextBlock { Text = "Danh mục này chưa có câu hỏi", FontSize = 12, Foreground = Brushes.Gray });
                }
            }
            LoadBankItems();
            cmbCat.SelectionChanged += (s, e2) => LoadBankItems();
            questionsScroll.Content = questionsStack;
            stack.Children.Add(questionsScroll);

            // Bottom buttons
            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
            var chkAll = new CheckBox { Content = " Chọn tất cả", FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0) };
            chkAll.Checked += (s, e2) => { foreach (var (cb, _) in checkboxes) cb.IsChecked = true; };
            chkAll.Unchecked += (s, e2) => { foreach (var (cb, _) in checkboxes) cb.IsChecked = false; };
            btnRow.Children.Add(chkAll);

            var btnImport = new Button
            {
                Content = "📥 Import vào Quiz đang chọn", FontSize = 13, Padding = new Thickness(16, 8, 16, 8),
                Background = new SolidColorBrush(Color.FromRgb(0, 105, 92)), Foreground = Brushes.White,
                BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand
            };
            btnImport.Click += (s, e2) =>
            {
                int count = 0;
                // → ClassroomAppContext
                int sortBase = _allQuestions.Any() ? _allQuestions.Max(x => x.SortOrder) + 1 : 0;
                foreach (var (cb, item) in checkboxes)
                {
                    if (cb.IsChecked == true)
                    {
                        var qtMap = new Dictionary<string, string>
                        {
                            { "MCQ", "MultipleChoice" }, { "TF", "TrueFalse" }, { "FIB", "FillBlank" },
                            { "MATCH", "Matching" }, { "SHORT", "ShortAnswer" }, { "ORDER", "Ordering" }
                        };
                        ClassroomAppContext.Db.Questions.Add(new Question
                        {
                            QuizId = _selectedQuizId,
                            SortOrder = sortBase + count,
                            Content = item.Content,
                            QuestionType = qtMap.GetValueOrDefault(item.QuestionType, item.QuestionType),
                            OptionsJson = item.OptionsJson,
                            CorrectAnswer = item.CorrectAnswer,
                            Difficulty = item.Difficulty,
                            Points = item.Points
                        });
                        count++;
                    }
                }
                if (count > 0)
                {
                    ClassroomAppContext.Db.SaveChanges();
                    LoadQuestions(_selectedQuizId);
                    UpdateStats();
                    ClassroomDialog.Info($"✅ Đã import {count} câu hỏi vào Quiz!", "Thành công");
                    wnd.Close();
                }
                else
                    ClassroomDialog.Warn("Vui lòng chọn ít nhất 1 câu hỏi!", "Thông báo");
            };
            btnRow.Children.Add(btnImport);
            stack.Children.Add(btnRow);

            wnd.Content = stack;
            wnd.ShowDialog();
        }

        /// <summary>📤 Export Quiz đang chọn + tất cả câu hỏi ra file JSON</summary>
        private void ExportQuiz_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedQuizId == 0) { MessageBox.Show("Vui lòng chọn Quiz trước!", "Thông báo"); return; }

            // → ClassroomAppContext
            var quiz = ClassroomAppContext.Db.Quizzes.Find(_selectedQuizId);
            if (quiz == null) return;

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Xuất Quiz ra file JSON",
                Filter = "JSON|*.json",
                FileName = $"Quiz_{quiz.Title.Replace(' ', '_')}_{DateTime.Now:yyyyMMdd}.json"
            };
            if (dlg.ShowDialog() == true)
            {
                var questions = ClassroomAppContext.Db.Questions.Where(q => q.QuizId == _selectedQuizId).OrderBy(q => q.SortOrder).ToList();
                var export = new
                {
                    Quiz = new { quiz.Title, quiz.QuizType, quiz.TimeLimitSeconds },
                    Questions = questions.Select(q => new
                    {
                        q.Content, q.QuestionType, q.OptionsJson, q.CorrectAnswer,
                        q.Difficulty, q.Points, q.SortOrder
                    }),
                    QuestionCount = questions.Count,
                    ExportedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    Version = "1.0"
                };
                var json = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(dlg.FileName, json);
                MessageBox.Show($"✅ Đã xuất Quiz \"{quiz.Title}\" ({questions.Count} câu hỏi)!", "Export thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                Log.Information("Quiz exported: {Title}, {Count} questions", quiz.Title, questions.Count);
            }
        }

        /// <summary>📂 Import Quiz + câu hỏi từ file (JSON/DOCX/CSV)</summary>
        private void ImportFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Nhập Quiz từ file",
                Filter = "Hỗ trợ (JSON, DOCX, CSV)|*.json;*.docx;*.csv|JSON|*.json|Word Document|*.docx|CSV|*.csv"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                // → ClassroomAppContext
                int importedCount = 0;

                if (dlg.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    var json = System.IO.File.ReadAllText(dlg.FileName);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    // Check if file has Quiz info → create new quiz
                    int targetQuizId = _selectedQuizId;
                    if (root.TryGetProperty("Quiz", out var quizEl))
                    {
                        var title = quizEl.TryGetProperty("Title", out var t) ? t.GetString() ?? "Import Quiz" : "Import Quiz";
                        var quizType = quizEl.TryGetProperty("QuizType", out var qt) ? qt.GetString() ?? "Competition" : "Competition";
                        int timeLimit = quizEl.TryGetProperty("TimeLimitSeconds", out var tl) ? tl.GetInt32() : 300;

                        var result = MessageBox.Show(
                            $"File chứa Quiz: \"{title}\"\n\nYes = Tạo Quiz mới \"{title}\"\nNo = Import vào Quiz đang chọn",
                            "Import Quiz", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Cancel) return;
                        if (result == MessageBoxResult.Yes)
                        {
                            var newQuiz = new Quiz { Title = title, QuizType = quizType, TimeLimitSeconds = timeLimit, CreatedAt = DateTime.Now };
                            ClassroomAppContext.Db.Quizzes.Add(newQuiz);
                            ClassroomAppContext.Db.SaveChanges();
                            targetQuizId = newQuiz.Id;
                        }
                    }

                    if (targetQuizId == 0) { MessageBox.Show("Không có Quiz nào để import vào!", "Lỗi"); return; }

                    if (root.TryGetProperty("Questions", out var questionsEl))
                    {
                        int sortBase = ClassroomAppContext.Db.Questions.Where(q => q.QuizId == targetQuizId).Any()
                            ? ClassroomAppContext.Db.Questions.Where(q => q.QuizId == targetQuizId).Max(q => q.SortOrder) + 1 : 0;

                        foreach (var qEl in questionsEl.EnumerateArray())
                        {
                            ClassroomAppContext.Db.Questions.Add(new Question
                            {
                                QuizId = targetQuizId,
                                SortOrder = sortBase + importedCount,
                                Content = qEl.TryGetProperty("Content", out var c) ? c.GetString() ?? "" : "",
                                QuestionType = qEl.TryGetProperty("QuestionType", out var qtype) ? qtype.GetString() ?? "MultipleChoice" : "MultipleChoice",
                                OptionsJson = qEl.TryGetProperty("OptionsJson", out var oj) ? oj.GetString() ?? "[]" : "[]",
                                CorrectAnswer = qEl.TryGetProperty("CorrectAnswer", out var ca) ? ca.GetString() ?? "" : "",
                                Difficulty = qEl.TryGetProperty("Difficulty", out var diff) ? diff.GetString() ?? "Easy" : "Easy",
                                Points = qEl.TryGetProperty("Points", out var pts) ? pts.GetInt32() : 10
                            });
                            importedCount++;
                        }
                    }
                    ClassroomAppContext.Db.SaveChanges();
                    LoadData();
                    ClassroomDialog.Info($"✅ Đã import {importedCount} câu hỏi từ JSON!", "Import thành công");
                }
                else if (dlg.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                {
                    if (_selectedQuizId == 0) { MessageBox.Show("Vui lòng chọn Quiz trước khi import Word!", "Lỗi"); return; }
                    var parsed = ParseWordFile(dlg.FileName);
                    ImportParsedQuestions(parsed, _selectedQuizId);
                }
                else if (dlg.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    if (_selectedQuizId == 0) { MessageBox.Show("Vui lòng chọn Quiz trước khi import CSV!", "Lỗi"); return; }
                    var parsed = ParseCsvFile(dlg.FileName);
                    ImportParsedQuestions(parsed, _selectedQuizId);
                }
            }
            catch (Exception ex)
            {
                ClassroomDialog.Error($"Lỗi import: {ex.Message}", "Lỗi");
            }
        }

        private void ImportParsedQuestions(List<Question> parsed, int targetQuizId)
        {
            if (parsed.Count == 0)
            {
                ClassroomDialog.Warn("Không tìm thấy câu hỏi hợp lệ trong file!", "Thông báo");
                return;
            }

            int sortBase = ClassroomAppContext.Db.Questions.Where(q => q.QuizId == targetQuizId).Any()
                ? ClassroomAppContext.Db.Questions.Where(q => q.QuizId == targetQuizId).Max(q => q.SortOrder) + 1 : 0;

            for (int i = 0; i < parsed.Count; i++)
            {
                parsed[i].QuizId = targetQuizId;
                parsed[i].SortOrder = sortBase + i;
                ClassroomAppContext.Db.Questions.Add(parsed[i]);
            }
            
            ClassroomAppContext.Db.SaveChanges();
            LoadData();
            ClassroomDialog.Info($"✅ Đã nạp thành công {parsed.Count} câu hỏi!", "Import thành công");
        }

        private List<Question> ParseWordFile(string filePath)
        {
            var list = new List<Question>();
            using (var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(filePath, false))
            {
                var paragraphs = doc.MainDocumentPart.Document.Body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>();
                
                Question currentQuestion = null;
                var options = new List<string>();

                foreach (var p in paragraphs)
                {
                    var line = p.InnerText.Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // Nhận dạng Câu hỏi: Bắt đầu bằng "Câu X:"
                    if (System.Text.RegularExpressions.Regex.IsMatch(line, @"^Câu\s*\d+\s*[:\.]", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    {
                        if (currentQuestion != null)
                        {
                            if (options.Count > 0) currentQuestion.OptionsJson = System.Text.Json.JsonSerializer.Serialize(options);
                            if (string.IsNullOrEmpty(currentQuestion.CorrectAnswer) && options.Count > 0) currentQuestion.CorrectAnswer = "A";
                            list.Add(currentQuestion);
                        }

                        currentQuestion = new Question
                        {
                            Content = System.Text.RegularExpressions.Regex.Replace(line, @"^Câu\s*\d+\s*[:\.]\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                            QuestionType = "MultipleChoice",
                            Difficulty = "Medium",
                            Points = 10
                        };
                        options = new List<string>();
                    }
                    else if (currentQuestion != null)
                    {
                        // Nhận dạng lựa chọn A., B., C., D.
                        if (System.Text.RegularExpressions.Regex.IsMatch(line, @"^[A-D]\s*[\.\)]", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        {
                            var optionText = System.Text.RegularExpressions.Regex.Replace(line, @"^[A-D]\s*[\.\)]\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            options.Add(optionText);
                        }
                        else if (line.StartsWith("Đáp án:", StringComparison.OrdinalIgnoreCase))
                        {
                            currentQuestion.CorrectAnswer = line.Substring("Đáp án:".Length).Trim();
                        }
                        else
                        {
                            // Nối vào câu hỏi nếu chưa có options
                            if (options.Count == 0)
                                currentQuestion.Content += "\n" + line;
                        }
                    }
                }

                if (currentQuestion != null)
                {
                    if (options.Count > 0) currentQuestion.OptionsJson = System.Text.Json.JsonSerializer.Serialize(options);
                    if (string.IsNullOrEmpty(currentQuestion.CorrectAnswer) && options.Count > 0) currentQuestion.CorrectAnswer = "A";
                    list.Add(currentQuestion);
                }
            }
            return list;
        }

        private List<Question> ParseCsvFile(string filePath)
        {
            var list = new List<Question>();
            var lines = System.IO.File.ReadAllLines(filePath);
            for (int i = 1; i < lines.Length; i++)
            {
                var row = lines[i];
                if (string.IsNullOrWhiteSpace(row)) continue;
                
                var cols = row.Split(',');
                if (cols.Length >= 6)
                {
                    var q = new Question
                    {
                        Content = cols[0].Trim(),
                        QuestionType = "MultipleChoice",
                        Difficulty = "Medium",
                        Points = 10,
                        OptionsJson = System.Text.Json.JsonSerializer.Serialize(new[] { cols[1].Trim(), cols[2].Trim(), cols[3].Trim(), cols[4].Trim() }),
                        CorrectAnswer = cols[5].Trim()
                    };
                    list.Add(q);
                }
            }
            return list;
        }

        private static TextBlock MakeLabel(string text) => new()
        {
            Text = text, FontSize = 11, Foreground = Brushes.Gray,
            Margin = new Thickness(0, 4, 0, 2), VerticalAlignment = VerticalAlignment.Center
        };
    }

    // ═══════════════════════════════════════════════════════════
    //  QUESTION EDITOR DIALOG — Nâng cấp đa loại câu hỏi
    // ═══════════════════════════════════════════════════════════

    /// <summary>Dialog thêm/sửa câu hỏi - hỗ trợ 6 loại: MCQ, TF, FIB, MATCH, SHORT, ORDER</summary>
    public class QuestionEditorDialog : Window
    {
        public Question? Result { get; private set; }
        private readonly int _quizId, _order;

        // Controls
        private readonly ComboBox _typeBox;
        private readonly RichTextBox _contentRtb;
        private readonly StackPanel _answerArea;
        private readonly TextBox _pointsBox, _timeBox, _explainBox;
        private readonly ComboBox _diffBox;
        private readonly CheckBox _chkSaveToBank;

        public QuestionEditorDialog(int quizId, int order)
        {
            _quizId = quizId; _order = order;
            Title = "📝 Thêm câu hỏi"; Width = 600; Height = 680;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Brushes.White;

            var mainScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var sp = new StackPanel { Margin = new Thickness(20, 16, 20, 16) };

            // ═══ ROW 1: Question type selector ═══
            var typeRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            typeRow.Children.Add(new TextBlock { Text = "Loại câu hỏi:", FontSize = 12, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
            _typeBox = new ComboBox { FontSize = 12, Padding = new Thickness(8, 5, 8, 5), MinWidth = 180, SelectedIndex = 0 };
            _typeBox.Items.Add(new ComboBoxItem { Content = "📋 Trắc nghiệm (A/B/C/D)", Tag = "MCQ" });
            _typeBox.Items.Add(new ComboBoxItem { Content = "✅ Đúng / Sai", Tag = "TF" });
            _typeBox.Items.Add(new ComboBoxItem { Content = "✏️ Điền khuyết", Tag = "FIB" });
            _typeBox.Items.Add(new ComboBoxItem { Content = "🔗 Nối phương án", Tag = "MATCH" });
            _typeBox.Items.Add(new ComboBoxItem { Content = "📝 Tự luận ngắn", Tag = "SHORT" });
            _typeBox.Items.Add(new ComboBoxItem { Content = "🔢 Sắp xếp thứ tự", Tag = "ORDER" });
            typeRow.Children.Add(_typeBox);
            sp.Children.Add(typeRow);

            // ═══ ROW 2: Question content with formatting toolbar ═══
            sp.Children.Add(new TextBlock { Text = "📋 Nội dung câu hỏi:", FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });

            // ── Formatting toolbar ──
            var fmtBar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(210, 210, 210)),
                BorderThickness = new Thickness(1, 1, 1, 0),
                Padding = new Thickness(4, 3, 4, 3)
            };
            var fmtRow = new StackPanel { Orientation = Orientation.Horizontal };

            // B / I / U
            var bBtn = MakeFmtButton("B", "In đậm", FontWeights.Bold, FontStyles.Normal);
            var iBtn = MakeFmtButton("I", "In nghiêng", FontWeights.Normal, FontStyles.Italic);
            var uBtn = MakeFmtButton("U", "Gạch chân", FontWeights.Normal, FontStyles.Normal);
            fmtRow.Children.Add(bBtn); fmtRow.Children.Add(iBtn); fmtRow.Children.Add(uBtn);

            // Separator
            fmtRow.Children.Add(new Border { Width = 1, Height = 16, Background = new SolidColorBrush(Color.FromRgb(200, 200, 200)), Margin = new Thickness(4, 2, 4, 2) });

            // Font size
            fmtRow.Children.Add(new TextBlock { Text = "Cỡ:", FontSize = 10, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 3, 0) });
            var cmbFSize = new ComboBox { FontSize = 10, Padding = new Thickness(2, 1, 2, 1), SelectedIndex = 2, MinWidth = 44 };
            foreach (var sz in new[] { "10", "11", "13", "14", "16", "18", "20", "24" })
                cmbFSize.Items.Add(new ComboBoxItem { Content = sz });
            fmtRow.Children.Add(cmbFSize);

            // Separator
            fmtRow.Children.Add(new Border { Width = 1, Height = 16, Background = new SolidColorBrush(Color.FromRgb(200, 200, 200)), Margin = new Thickness(4, 2, 4, 2) });

            // Color dots
            var colorDefs = new (byte r, byte g, byte b, string tip)[]
            {
                (33, 33, 33, "Đen"), (198, 40, 40, "Đỏ"), (25, 118, 210, "Xanh"), (46, 125, 50, "Lá")
            };
            foreach (var (r, g, b, tip) in colorDefs)
            {
                var clr = Color.FromRgb(r, g, b);
                var dot = new System.Windows.Shapes.Ellipse
                {
                    Width = 12, Height = 12, Fill = new SolidColorBrush(clr),
                    Stroke = new SolidColorBrush(Color.FromRgb(180, 180, 180)), StrokeThickness = 1
                };
                var colorBtn = new Button
                {
                    Content = dot, ToolTip = $"Chữ {tip}", Width = 22, Height = 22, Tag = clr,
                    Background = Brushes.White, BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand, Margin = new Thickness(0, 0, 1, 0), Padding = new Thickness(0)
                };
                fmtRow.Children.Add(colorBtn);
            }

            // Separator
            fmtRow.Children.Add(new Border { Width = 1, Height = 16, Background = new SolidColorBrush(Color.FromRgb(200, 200, 200)), Margin = new Thickness(4, 2, 4, 2) });

            // Highlight
            var hlBtn = new Button
            {
                Content = "🔆", FontSize = 11, ToolTip = "Tô highlight", Width = 24, Height = 22,
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 176)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1), Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 2, 0), Padding = new Thickness(0)
            };
            fmtRow.Children.Add(hlBtn);

            // Insert image
            var imgBtn = new Button
            {
                Content = "🖼️ Ảnh", FontSize = 10, ToolTip = "Chèn hình ảnh minh họa",
                Padding = new Thickness(6, 2, 6, 2),
                Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)),
                Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(2, 0, 0, 0)
            };
            fmtRow.Children.Add(imgBtn);

            fmtBar.Child = fmtRow;
            sp.Children.Add(fmtBar);

            // ── RichTextBox ──
            _contentRtb = new RichTextBox
            {
                MinHeight = 80, MaxHeight = 200, BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                Padding = new Thickness(8), FontFamily = new FontFamily("Segoe UI"), FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            _contentRtb.Document.Blocks.Clear();
            _contentRtb.Document.Blocks.Add(new System.Windows.Documents.Paragraph());
            sp.Children.Add(_contentRtb);

            // Wire B/I/U
            bBtn.Click += (s, ev) => { ToggleSelectionProperty(System.Windows.Documents.TextElement.FontWeightProperty, FontWeights.Bold, FontWeights.Normal); };
            iBtn.Click += (s, ev) => { ToggleSelectionProperty(System.Windows.Documents.TextElement.FontStyleProperty, FontStyles.Italic, FontStyles.Normal); };
            uBtn.Click += (s, ev) =>
            {
                var sel = _contentRtb.Selection;
                if (!sel.IsEmpty)
                {
                    var dec = sel.GetPropertyValue(System.Windows.Documents.Inline.TextDecorationsProperty);
                    sel.ApplyPropertyValue(System.Windows.Documents.Inline.TextDecorationsProperty,
                        dec == TextDecorations.Underline ? null : TextDecorations.Underline);
                }
                _contentRtb.Focus();
            };

            // Wire font size
            cmbFSize.SelectionChanged += (s, ev) =>
            {
                if (cmbFSize.SelectedItem is ComboBoxItem ci && double.TryParse(ci.Content?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sz))
                {
                    if (!_contentRtb.Selection.IsEmpty)
                        _contentRtb.Selection.ApplyPropertyValue(System.Windows.Documents.TextElement.FontSizeProperty, sz);
                }
            };

            // Wire color buttons
            foreach (var child in fmtRow.Children)
            {
                if (child is Button cb && cb.Tag is Color tagColor)
                {
                    var capturedColor = tagColor;
                    cb.Click += (s, ev) =>
                    {
                        if (!_contentRtb.Selection.IsEmpty)
                            _contentRtb.Selection.ApplyPropertyValue(System.Windows.Documents.TextElement.ForegroundProperty, new SolidColorBrush(capturedColor));
                        _contentRtb.Focus();
                    };
                }
            }

            // Wire highlight
            hlBtn.Click += (s, ev) =>
            {
                if (!_contentRtb.Selection.IsEmpty)
                {
                    var bg = _contentRtb.Selection.GetPropertyValue(System.Windows.Documents.TextElement.BackgroundProperty);
                    _contentRtb.Selection.ApplyPropertyValue(System.Windows.Documents.TextElement.BackgroundProperty,
                        bg is SolidColorBrush scb && scb.Color == Color.FromRgb(255, 255, 176) ? Brushes.Transparent : new SolidColorBrush(Color.FromRgb(255, 255, 176)));
                }
                _contentRtb.Focus();
            };

            // Wire image insert
            imgBtn.Click += (s, ev) =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Chọn hình ảnh minh họa", Filter = "Hình ảnh|*.png;*.jpg;*.jpeg;*.gif;*.bmp" };
                if (dlg.ShowDialog() == true)
                {
                    try
                    {
                        var bmp = new System.Windows.Media.Imaging.BitmapImage(new Uri(dlg.FileName));
                        var img = new System.Windows.Controls.Image { Source = bmp, MaxWidth = 380, MaxHeight = 220, Stretch = Stretch.Uniform, Margin = new Thickness(0, 4, 0, 4) };
                        new System.Windows.Documents.InlineUIContainer(img, _contentRtb.CaretPosition);
                    }
                    catch (Exception ex3) { Log.Warning("Insert question image: {Err}", ex3.Message); }
                }
            };

            // ═══ ROW 3: Dynamic answer section ═══
            _answerArea = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            sp.Children.Add(_answerArea);
            BuildAnswerSection("MCQ"); // default

            _typeBox.SelectionChanged += (s, e) =>
            {
                var tag = (_typeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "MCQ";
                BuildAnswerSection(tag);
            };

            // ═══ Separator ═══
            sp.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromRgb(230, 230, 230)), Margin = new Thickness(0, 4, 0, 8) });

            // ═══ ROW 4: Meta — Points, Difficulty, Time ═══
            var metaRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };

            metaRow.Children.Add(new TextBlock { Text = "Điểm:", FontSize = 11, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) });
            _pointsBox = new TextBox
            {
                Text = "10", Width = 50, FontSize = 12, Padding = new Thickness(6, 4, 6, 4),
                TextAlignment = TextAlignment.Center, BorderBrush = new SolidColorBrush(Color.FromRgb(210, 210, 210)), BorderThickness = new Thickness(1)
            };
            metaRow.Children.Add(_pointsBox);

            metaRow.Children.Add(new TextBlock { Text = "Độ khó:", FontSize = 11, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 4, 0) });
            _diffBox = new ComboBox { FontSize = 12, Padding = new Thickness(6, 3, 6, 3), SelectedIndex = 0 };
            _diffBox.Items.Add(new ComboBoxItem { Content = "🟢 Dễ", Tag = "Easy" });
            _diffBox.Items.Add(new ComboBoxItem { Content = "🟡 Trung bình", Tag = "Medium" });
            _diffBox.Items.Add(new ComboBoxItem { Content = "🔴 Khó", Tag = "Hard" });
            metaRow.Children.Add(_diffBox);

            metaRow.Children.Add(new TextBlock { Text = "⏱:", FontSize = 11, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 4, 0) });
            _timeBox = new TextBox
            {
                Text = "60", Width = 45, FontSize = 12, Padding = new Thickness(6, 4, 6, 4),
                TextAlignment = TextAlignment.Center, BorderBrush = new SolidColorBrush(Color.FromRgb(210, 210, 210)), BorderThickness = new Thickness(1)
            };
            metaRow.Children.Add(_timeBox);
            metaRow.Children.Add(new TextBlock { Text = "s", FontSize = 10, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 0, 0) });
            sp.Children.Add(metaRow);

            // ═══ ROW 5: Explanation ═══
            sp.Children.Add(new TextBlock { Text = "💡 Giải thích đáp án (tùy chọn):", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 0, 0, 4) });
            _explainBox = new TextBox
            {
                FontSize = 12, Padding = new Thickness(8, 6, 8, 6), AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap, MinHeight = 40, MaxHeight = 70,
                BorderBrush = new SolidColorBrush(Color.FromRgb(210, 210, 210)), BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 8)
            };
            sp.Children.Add(_explainBox);

            // ═══ ROW 6: Save to bank checkbox ═══
            _chkSaveToBank = new CheckBox
            {
                Content = " 💾 Đồng thời lưu vào Ngân hàng câu hỏi",
                FontSize = 11, Margin = new Thickness(0, 0, 0, 10), IsChecked = false
            };
            sp.Children.Add(_chkSaveToBank);

            // ═══ Buttons ═══
            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 4, 0, 0) };
            var btnCancel = new Button
            {
                Content = "Hủy", Width = 90, Height = 36, FontSize = 13,
                Margin = new Thickness(0, 0, 8, 0), Cursor = System.Windows.Input.Cursors.Hand
            };
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };

            var btnSave = new Button
            {
                Content = "✅ Lưu câu hỏi", Width = 130, Height = 36, FontSize = 13,
                Background = new SolidColorBrush(Color.FromRgb(0, 105, 92)),
                Foreground = Brushes.White, BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnSave.Click += Save_Click;
            btnRow.Children.Add(btnCancel); btnRow.Children.Add(btnSave);
            sp.Children.Add(btnRow);

            mainScroll.Content = sp;
            Content = mainScroll;
        }

        // ── Dynamic Answer Section Builder ──────────────────────

        private readonly List<TextBox> _optionBoxes = new();
        private ComboBox? _correctAnswerBox;
        private readonly List<(TextBox left, TextBox right)> _matchPairs = new();

        private void BuildAnswerSection(string type)
        {
            _answerArea.Children.Clear();
            _optionBoxes.Clear();
            _matchPairs.Clear();
            _correctAnswerBox = null;

            var labels = new[] { "A", "B", "C", "D" };
            var colors = new[]
            {
                Color.FromRgb(227, 242, 253), Color.FromRgb(232, 245, 233),
                Color.FromRgb(255, 243, 224), Color.FromRgb(243, 229, 245)
            };

            switch (type)
            {
                case "MCQ":
                    _answerArea.Children.Add(new TextBlock { Text = "📋 Đáp án trắc nghiệm:", FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
                    for (int i = 0; i < 4; i++)
                    {
                        var row = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
                        var badge = new Border
                        {
                            Background = new SolidColorBrush(colors[i]),
                            CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 3, 8, 3),
                            Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center
                        };
                        badge.Child = new TextBlock { Text = labels[i], FontWeight = FontWeights.Bold, FontSize = 12 };
                        DockPanel.SetDock(badge, Dock.Left); row.Children.Add(badge);

                        var tb = new TextBox
                        {
                            FontSize = 12, Padding = new Thickness(8, 5, 8, 5),
                            BorderBrush = new SolidColorBrush(Color.FromRgb(210, 210, 210)), BorderThickness = new Thickness(1)
                        };
                        _optionBoxes.Add(tb);
                        row.Children.Add(tb);
                        _answerArea.Children.Add(row);
                    }
                    // Correct answer selector
                    var mcqRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
                    mcqRow.Children.Add(new TextBlock { Text = "✅ Đáp án đúng:", FontSize = 11, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
                    _correctAnswerBox = new ComboBox { FontSize = 12, Padding = new Thickness(6, 3, 6, 3), SelectedIndex = 0, MinWidth = 60 };
                    foreach (var l in labels) _correctAnswerBox.Items.Add(l);
                    mcqRow.Children.Add(_correctAnswerBox);
                    _answerArea.Children.Add(mcqRow);
                    break;

                case "TF":
                    _answerArea.Children.Add(new TextBlock { Text = "✅ Chọn đáp án đúng:", FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
                    _correctAnswerBox = new ComboBox { FontSize = 13, Padding = new Thickness(8, 5, 8, 5), SelectedIndex = 0, MinWidth = 160 };
                    _correctAnswerBox.Items.Add(new ComboBoxItem { Content = "✅ Đúng (True)" });
                    _correctAnswerBox.Items.Add(new ComboBoxItem { Content = "❌ Sai (False)" });
                    _answerArea.Children.Add(_correctAnswerBox);
                    break;

                case "FIB":
                    _answerArea.Children.Add(new TextBlock { Text = "✏️ Đáp án điền khuyết:", FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
                    _answerArea.Children.Add(new TextBlock { Text = "Gợi ý: Dùng [___] trong câu hỏi để đánh dấu chỗ trống", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)), Margin = new Thickness(0, 0, 0, 6) });
                    for (int i = 1; i <= 3; i++)
                    {
                        var row = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
                        var lb = new TextBlock { Text = $"Chỗ trống {i}:", FontSize = 11, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Width = 80 };
                        DockPanel.SetDock(lb, Dock.Left); row.Children.Add(lb);
                        var tb = new TextBox { FontSize = 12, Padding = new Thickness(8, 5, 8, 5), BorderBrush = new SolidColorBrush(Color.FromRgb(210, 210, 210)), BorderThickness = new Thickness(1) };
                        _optionBoxes.Add(tb);
                        row.Children.Add(tb);
                        _answerArea.Children.Add(row);
                    }
                    _answerArea.Children.Add(new CheckBox { Content = " Không phân biệt hoa/thường", IsChecked = true, FontSize = 11, Margin = new Thickness(0, 4, 0, 0) });
                    break;

                case "MATCH":
                    _answerArea.Children.Add(new TextBlock { Text = "🔗 Nối phương án (trái → phải):", FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
                    for (int i = 1; i <= 4; i++)
                    {
                        var row = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36, GridUnitType.Pixel) });
                        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        var left = new TextBox { Text = $"Vế trái {i}...", FontSize = 12, Padding = new Thickness(8, 5, 8, 5), BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)), BorderThickness = new Thickness(1) };
                        Grid.SetColumn(left, 0); row.Children.Add(left);

                        var arrow = new TextBlock { Text = "→", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0, 105, 92)), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                        Grid.SetColumn(arrow, 1); row.Children.Add(arrow);

                        var right = new TextBox { Text = $"Vế phải {i}...", FontSize = 12, Padding = new Thickness(8, 5, 8, 5), BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)), BorderThickness = new Thickness(1) };
                        Grid.SetColumn(right, 2); row.Children.Add(right);

                        _matchPairs.Add((left, right));
                        _answerArea.Children.Add(row);
                    }
                    break;

                case "SHORT":
                    _answerArea.Children.Add(new TextBlock { Text = "📝 Đáp án mẫu (HS tự viết):", FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
                    var shortTb = new TextBox
                    {
                        FontSize = 12, Padding = new Thickness(10, 8, 10, 8), AcceptsReturn = true,
                        TextWrapping = TextWrapping.Wrap, MinHeight = 60,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)), BorderThickness = new Thickness(1),
                        Margin = new Thickness(0, 0, 0, 6)
                    };
                    _optionBoxes.Add(shortTb);
                    _answerArea.Children.Add(shortTb);

                    var minRow = new StackPanel { Orientation = Orientation.Horizontal };
                    minRow.Children.Add(new TextBlock { Text = "Số từ tối thiểu:", FontSize = 11, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
                    var minTb = new TextBox
                    {
                        Text = "20", Width = 60, FontSize = 12, Padding = new Thickness(6, 4, 6, 4),
                        TextAlignment = TextAlignment.Center, BorderBrush = new SolidColorBrush(Color.FromRgb(210, 210, 210)), BorderThickness = new Thickness(1)
                    };
                    _optionBoxes.Add(minTb);
                    minRow.Children.Add(minTb);
                    _answerArea.Children.Add(minRow);
                    break;

                case "ORDER":
                    _answerArea.Children.Add(new TextBlock { Text = "🔢 Sắp xếp đúng thứ tự (từ trên xuống):", FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
                    for (int i = 1; i <= 5; i++)
                    {
                        var row = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
                        var numBadge = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(227, 242, 253)),
                            CornerRadius = new CornerRadius(12), Width = 24, Height = 24,
                            Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center
                        };
                        numBadge.Child = new TextBlock { Text = i.ToString(), FontSize = 11, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                        DockPanel.SetDock(numBadge, Dock.Left); row.Children.Add(numBadge);

                        var tb = new TextBox
                        {
                            Text = $"Bước {i}...", FontSize = 12, Padding = new Thickness(8, 5, 8, 5),
                            BorderBrush = new SolidColorBrush(Color.FromRgb(210, 210, 210)), BorderThickness = new Thickness(1)
                        };
                        _optionBoxes.Add(tb);
                        row.Children.Add(tb);
                        _answerArea.Children.Add(row);
                    }
                    break;
            }
        }

        // ── Save handler ──────────────────────────────────────────

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var contentText = GetContentText();
            if (string.IsNullOrWhiteSpace(contentText))
            {
                ClassroomDialog.Warn("Vui lòng nhập nội dung câu hỏi!", "Thiếu thông tin");
                return;
            }

            var type = (_typeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "MCQ";
            string optionsJson = "[]";
            string correctAnswer = "";

            switch (type)
            {
                case "MCQ":
                    var opts = _optionBoxes.Select((tb, i) => $"{(char)('A' + i)}. {tb.Text.Trim()}").ToArray();
                    optionsJson = JsonSerializer.Serialize(opts);
                    correctAnswer = _correctAnswerBox?.SelectedItem?.ToString() ?? "A";
                    break;

                case "TF":
                    optionsJson = JsonSerializer.Serialize(new[] { "Đúng", "Sai" });
                    correctAnswer = _correctAnswerBox?.SelectedIndex == 0 ? "True" : "False";
                    break;

                case "FIB":
                    var blanks = _optionBoxes.Where(tb => !string.IsNullOrWhiteSpace(tb.Text)).Select(tb => tb.Text.Trim()).ToArray();
                    optionsJson = JsonSerializer.Serialize(blanks);
                    correctAnswer = string.Join(";", blanks);
                    break;

                case "MATCH":
                    var pairs = _matchPairs
                        .Where(p => !string.IsNullOrWhiteSpace(p.left.Text) && !p.left.Text.StartsWith("Vế"))
                        .Select(p => new { Left = p.left.Text.Trim(), Right = p.right.Text.Trim() }).ToArray();
                    optionsJson = JsonSerializer.Serialize(pairs);
                    correctAnswer = "MATCH:" + string.Join(",", pairs.Select(p => $"{p.Left}->{p.Right}"));
                    break;

                case "SHORT":
                    var sampleAnswer = _optionBoxes.FirstOrDefault()?.Text.Trim() ?? "";
                    optionsJson = JsonSerializer.Serialize(new[] { sampleAnswer });
                    correctAnswer = sampleAnswer;
                    break;

                case "ORDER":
                    var steps = _optionBoxes.Where(tb => !string.IsNullOrWhiteSpace(tb.Text) && !tb.Text.StartsWith("Bước")).Select(tb => tb.Text.Trim()).ToArray();
                    optionsJson = JsonSerializer.Serialize(steps);
                    correctAnswer = string.Join(" → ", steps);
                    break;
            }

            // Map QuestionType
            var qtMap = new Dictionary<string, string>
            {
                { "MCQ", "MultipleChoice" }, { "TF", "TrueFalse" }, { "FIB", "FillBlank" },
                { "MATCH", "Matching" }, { "SHORT", "ShortAnswer" }, { "ORDER", "Ordering" }
            };
            var questionType = qtMap.GetValueOrDefault(type, "MultipleChoice");

            int.TryParse(_pointsBox.Text, out int points); if (points <= 0) points = 10;
            var difficulty = (_diffBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Easy";

            Result = new Question
            {
                QuizId = _quizId, SortOrder = _order,
                Content = contentText,
                QuestionType = questionType,
                OptionsJson = optionsJson,
                CorrectAnswer = correctAnswer,
                Difficulty = difficulty,
                Points = points
            };

            // Also save to bank if checked
            if (_chkSaveToBank.IsChecked == true)
            {
                try
                {
                    int.TryParse(_timeBox.Text, out int timeLimit); if (timeLimit <= 0) timeLimit = 60;
                    using var db = new AppDbContext();
                    db.Database.EnsureCreated();

                    // Get or create default category
                    var cat = db.QuestionBankCategories.FirstOrDefault();
                    if (cat == null)
                    {
                        cat = new QuestionBankCategory { Name = "Mặc định", Subject = "Chung", Grade = "" };
                        db.QuestionBankCategories.Add(cat);
                        db.SaveChanges();
                    }

                    db.QuestionBankItems.Add(new QuestionBankItem
                    {
                        CategoryId = cat.Id, QuestionType = type,
                        Content = contentText,
                        OptionsJson = optionsJson, CorrectAnswer = correctAnswer,
                        Explanation = _explainBox.Text.Trim(),
                        Points = points, Difficulty = difficulty,
                        TimeLimitSeconds = timeLimit
                    });
                    db.SaveChanges();
                    Log.Information("Question also saved to bank");
                }
                catch (Exception ex)
                {
                    Log.Warning("Save to bank failed: {Err}", ex.Message);
                }
            }

            DialogResult = true; Close();
        }

        // ── Helpers ────────────────────────────────────────────

        private string GetContentText()
        {
            return new System.Windows.Documents.TextRange(
                _contentRtb.Document.ContentStart,
                _contentRtb.Document.ContentEnd).Text.Trim();
        }

        private void ToggleSelectionProperty(DependencyProperty prop, object onValue, object offValue)
        {
            var sel = _contentRtb.Selection;
            if (sel.IsEmpty) return;
            var cur = sel.GetPropertyValue(prop);
            sel.ApplyPropertyValue(prop, cur != null && cur.Equals(onValue) ? offValue : onValue);
            _contentRtb.Focus();
        }

        private static Button MakeFmtButton(string text, string tooltip, FontWeight weight, FontStyle style)
        {
            return new Button
            {
                Content = text, ToolTip = tooltip, Width = 26, Height = 22, FontSize = 11,
                FontWeight = weight, FontStyle = style,
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(215, 215, 215)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 2, 0), Padding = new Thickness(0)
            };
        }
    }
}
