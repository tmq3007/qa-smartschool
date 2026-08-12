using QASmartClass.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.TeacherHub.Views
{
    public partial class QuestionBankView : UserControl
    {
        private AppDbContext? _db;
        private int _currentPage = 1;
        private const int PageSize = 50;

        public QuestionBankView()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _db = new AppDbContext();
            NormalizeExistingDbData();
            InitFilters();
            LoadQuestions();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _db?.Dispose();
            _db = null;
        }

        private void InitFilters()
        {
            if (_db == null) return;
            // Subject filter
            var subjects = _db.QuestionBankItems.Select(q => q.Subject).Distinct().ToList();
            subjects.Insert(0, "Tất cả");
            CboSubject.ItemsSource = subjects;
            CboSubject.SelectedIndex = 0;

            // Grade filter
            CboGrade.Items.Clear();
            CboGrade.Items.Add("Tất cả");
            CboGrade.Items.Add("Khối 10");
            CboGrade.Items.Add("Khối 11");
            CboGrade.Items.Add("Khối 12");
            CboGrade.SelectedIndex = 0;

            // Difficulty filter
            CboDifficulty.Items.Clear();
            CboDifficulty.Items.Add("Tất cả");
            CboDifficulty.Items.Add("Dễ");
            CboDifficulty.Items.Add("Trung bình");
            CboDifficulty.Items.Add("Khó");
            CboDifficulty.SelectedIndex = 0;

            // Approval status filter
            CboApprovalStatus.Items.Clear();
            CboApprovalStatus.Items.Add("Tất cả");
            CboApprovalStatus.Items.Add("Đã duyệt");
            CboApprovalStatus.Items.Add("Chờ duyệt");
            CboApprovalStatus.Items.Add("Từ chối");
            CboApprovalStatus.SelectedIndex = 0;
        }

        private void LoadQuestions()
        {
            _currentPage = 1;
            ApplyFilter();
        }

        private void Filter_Changed(object sender, object e)
        {
            _currentPage = 1;
            ApplyFilter();
        }

        private static string GetDifficultyRecommendation(string currentDiff, int total, int wrong)
        {
            if (total < 5) return string.Empty;
            double rate = (double)wrong / total;
            if (rate > 0.70 && currentDiff != "Hard") return "⚠️ Khuyên dùng: Khó";
            if (rate < 0.15 && currentDiff != "Easy") return "⚠️ Khuyên dùng: Dễ";
            return string.Empty;
        }

        private void ApplyFilter()
        {
            if (_db == null) return;
            var query = _db.QuestionBankItems.AsQueryable();

            // Subject filter
            string subject = CboSubject?.SelectedItem?.ToString() ?? "Tất cả";
            if (subject != "Tất cả")
                query = query.Where(q => q.Subject == subject);

            // Grade filter
            string grade = CboGrade?.SelectedItem?.ToString() ?? "Tất cả";
            if (grade != "Tất cả")
            {
                string dbGrade = grade switch
                {
                    "Khối 10" => "10",
                    "Khối 11" => "11",
                    "Khối 12" => "12",
                    _ => grade
                };
                query = query.Where(q => q.Grade == dbGrade);
            }

            // Difficulty filter
            string diff = CboDifficulty?.SelectedItem?.ToString() ?? "Tất cả";
            if (diff != "Tất cả")
            {
                string dbDiff = diff switch
                {
                    "Dễ" => "Easy",
                    "Trung bình" => "Medium",
                    "Khó" => "Hard",
                    _ => diff
                };
                query = query.Where(q => q.Difficulty == dbDiff);
            }

            // Approval Status filter
            string appStatus = CboApprovalStatus?.SelectedItem?.ToString() ?? "Tất cả";
            if (appStatus != "Tất cả")
            {
                string dbStatus = appStatus switch
                {
                    "Đã duyệt" => "Approved",
                    "Chờ duyệt" => "Pending",
                    "Từ chối" => "Rejected",
                    _ => appStatus
                };
                query = query.Where(q => q.ApprovalStatus == dbStatus);
            }

            // Text search
            string search = TxtSearch?.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(search))
                query = query.Where(q => q.Content.Contains(search));

            var sw = System.Diagnostics.Stopwatch.StartNew();
            int totalItems = query.Count();
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalItems / PageSize));
            if (_currentPage > totalPages) _currentPage = totalPages;
            if (_currentPage < 1) _currentPage = 1;

            var dbItems = query.OrderByDescending(q => q.CreatedAt)
                               .Skip((_currentPage - 1) * PageSize)
                               .Take(PageSize)
                               .ToList();
            sw.Stop();
            long elapsedMs = sw.ElapsedMilliseconds;
            if (elapsedMs > 200)
            {
                Log.Warning("SLOW QUERY WARNING: ApplyFilter database query took {ElapsedMs}ms! Filters: Subject={Subject}, Difficulty={Difficulty}, Status={Status}, Search={Search}", 
                    elapsedMs, subject, diff, appStatus, search);
            }
            else
            {
                Log.Information("ApplyFilter database query executed in {ElapsedMs}ms. Total records: {TotalItems}", elapsedMs, totalItems);
            }

            var pageItems = dbItems.Select(q =>
            {
                double rate = q.TotalAttempts > 0 ? (double)q.WrongCount / q.TotalAttempts : 0;
                bool hasRecommend = q.TotalAttempts >= 5 && ((rate > 0.70 && q.Difficulty != "Hard") || (rate < 0.15 && q.Difficulty != "Easy"));
                return new QuestionViewModel
                {
                    Id = q.Id,
                    Content = q.Content,
                    Subject = q.Subject,
                    Grade = "Khối " + (string.IsNullOrEmpty(q.Grade) ? "10" : q.Grade),
                    Difficulty = q.Difficulty == "Easy" ? "Dễ" : q.Difficulty == "Hard" ? "Khó" : "Trung bình",
                    QuestionType = q.QuestionType,
                    CorrectAnswer = q.CorrectAnswer,
                    PointsText = $"{q.Points} điểm",
                    WrongRateText = q.TotalAttempts > 0 ? $"Sai {(int)(rate * 100)}% ({q.WrongCount}/{q.TotalAttempts})" : "Chưa có dữ liệu",
                    DiffBg = GetDiffBg(q.Difficulty),
                    DiffFg = GetDiffFg(q.Difficulty),
                    ApprovalStatusText = q.ApprovalStatus == "Approved" ? "Đã duyệt" : (q.ApprovalStatus == "Rejected" ? "Từ chối" : "Chờ duyệt"),
                    ApprovalStatusBg = q.ApprovalStatus == "Approved" ? "#DCFCE7" : (q.ApprovalStatus == "Rejected" ? "#FEE2E2" : "#FEF9C3"),
                    ApprovalStatusFg = q.ApprovalStatus == "Approved" ? "#16A34A" : (q.ApprovalStatus == "Rejected" ? "#DC2626" : "#CA8A04"),
                    DifficultyRecommendation = GetDifficultyRecommendation(q.Difficulty, q.TotalAttempts, q.WrongCount),
                    HasRecommendationVisibility = hasRecommend ? Visibility.Visible : Visibility.Collapsed,
                    IsSelected = false
                };
            }).ToList();

            LvQuestions.ItemsSource = pageItems;
            TxtTotal.Text = $"{totalItems} câu hỏi";

            if (TxtPageInfo != null)
                TxtPageInfo.Text = $"Trang {_currentPage} / {totalPages} (Tổng số {totalItems} câu)";

            if (BtnPrevPage != null)
                BtnPrevPage.IsEnabled = _currentPage > 1;
            if (BtnNextPage != null)
                BtnNextPage.IsEnabled = _currentPage < totalPages;
        }

        private static Brush GetDiffBg(string d) => d switch
        {
            "Easy"   => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FDF4")),
            "Medium" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF9C3")),
            "Hard"   => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF2F2")),
            _        => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"))
        };

        private static Brush GetDiffFg(string d) => d switch
        {
            "Easy"   => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A")),
            "Medium" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CA8A04")),
            "Hard"   => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")),
            _        => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"))
        };

        // --- F2.3: M? c?a s? AI Copilot ------------------
        private void BtnAiCopilot_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            var win = new AiCopilotWindow(_db);
            win.Owner = Window.GetWindow(this);
            if (win.ShowDialog() == true)
            {
                // Refresh list
                LoadQuestions();
            }
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            try
            {
                string defaultName = $"QuestionBank_{DateTime.Now:yyyyMMdd}";
                string filePath = QASmartClass.Services.ExcelDataService.GetSaveFilePath(defaultName);
                if (string.IsNullOrEmpty(filePath)) return;

                var allItems = _db.QuestionBankItems.OrderByDescending(q => q.CreatedAt).ToList();
                var formattedData = allItems.Select(q => {
                    string optionsText = "";
                    try {
                        var opts = System.Text.Json.JsonSerializer.Deserialize<List<string>>(q.OptionsJson ?? "[]");
                        optionsText = string.Join(" | ", opts);
                    } catch { optionsText = q.OptionsJson; }
                    return new {
                        NoiDung = q.Content,
                        LoaiCauHoi = q.QuestionType,
                        LuaChon = optionsText,
                        DapAnDung = q.CorrectAnswer,
                        MonHoc = q.Subject,
                        MucDo = q.Difficulty
                    };
                }).ToList();

                bool ok = QASmartClass.Services.ExcelDataService.ExportToExcel(filePath, formattedData, "QuestionBank");
                if (ok) MessageBox.Show($"Đã xuất ngân hàng câu hỏi thành công!\nFile: {filePath}", "Xuất dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnImportExcel_Click(object sender, RoutedEventArgs e)
        {
            string filePath = QASmartClass.Services.ExcelDataService.GetOpenFilePath("Excel Files (*.xlsx)|*.xlsx");
            if (string.IsNullOrEmpty(filePath)) return;

            var result = new QASmartClass.Models.ImportResultDto();
            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    using var package = new OfficeOpenXml.ExcelPackage(new System.IO.FileInfo(filePath));
                    var ws = package.Workbook.Worksheets.FirstOrDefault();
                    if (ws == null) throw new Exception("Không tìm thấy Worksheet nào trong tệp Excel.");

                    int rowCount = ws.Dimension?.Rows ?? 0;
                    if (rowCount < 2) throw new Exception("Tệp Excel không chứa dữ liệu hoặc chỉ có Header.");

                    for (int row = 2; row <= rowCount; row++)
                    {
                        string content = ws.Cells[row, 1].Text?.Trim();
                        if (!string.IsNullOrEmpty(content) && content.Contains("[DÒNG MẪU", StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // Bỏ qua dòng mẫu
                        }

                        result.TotalProcessed++;
                        string qType = ws.Cells[row, 2].Text?.Trim();
                        string optionsText = ws.Cells[row, 3].Text?.Trim();
                        string correctAnswer = ws.Cells[row, 4].Text?.Trim();
                        string subject = ws.Cells[row, 5].Text?.Trim();
                        string difficulty = ws.Cells[row, 6].Text?.Trim();
                        string grade = ws.Cells[row, 7].Text?.Trim();

                        if (string.IsNullOrEmpty(content))
                        {
                            result.ErrorCount++;
                            result.ErrorDetails.Add($"Dòng {row} [Nội dung câu hỏi]: Nội dung câu hỏi không được để trống.");
                            continue;
                        }
                        if (string.IsNullOrEmpty(correctAnswer))
                        {
                            result.ErrorCount++;
                            result.ErrorDetails.Add($"Dòng {row} [Đáp án đúng]: Đáp án đúng không được để trống.");
                            continue;
                        }

                        // Validate question type
                        string upperType = (qType ?? "MCQ").ToUpper();
                        if (upperType != "MCQ" && upperType != "TF" && upperType != "FIB" && 
                            upperType != "MATCH" && upperType != "SHORT" && upperType != "ORDER")
                        {
                            result.ErrorCount++;
                            result.ErrorDetails.Add($"Dòng {row} [Loại câu hỏi]: Loại câu hỏi '{qType}' không hợp lệ (Hỗ trợ: MCQ, TF, FIB, MATCH, SHORT, ORDER).");
                            continue;
                        }

                        string optionsJson = "[]";
                        if (!string.IsNullOrEmpty(optionsText))
                        {
                            try
                            {
                                var opts = optionsText.Split('|').Select(s => s.Trim()).ToList();
                                optionsJson = System.Text.Json.JsonSerializer.Serialize(opts);
                            }
                            catch
                            {
                                result.ErrorCount++;
                                result.ErrorDetails.Add($"Dòng {row} [Các lựa chọn]: Định dạng tùy chọn '{optionsText}' không hợp lệ.");
                                continue;
                            }
                        }

                        int defaultCatId = 1;
                        var firstCat = _db.QuestionBankCategories.FirstOrDefault();
                        if (firstCat != null) defaultCatId = firstCat.Id;
                        else
                        {
                            firstCat = new QuestionBankCategory { Name = "Chung", Subject = "Chung", Grade = "Chung" };
                            _db.QuestionBankCategories.Add(firstCat);
                            _db.SaveChanges();
                            defaultCatId = firstCat.Id;
                        }

                        var item = new QuestionBankItem
                        {
                            CategoryId = defaultCatId,
                            Content = content,
                            QuestionType = upperType,
                            OptionsJson = optionsJson,
                            CorrectAnswer = correctAnswer,
                            Subject = NormalizeSubject(subject),
                            Difficulty = NormalizeDifficulty(difficulty),
                            Grade = NormalizeGrade(grade),
                            Points = 10,
                            CreatedAt = DateTime.Now,
                            ApprovalStatus = "Pending", // Tự động đưa vào hàng chờ duyệt
                            CreatedBy = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "GV001",
                            TotalAttempts = 0,
                            WrongCount = 0
                        };

                        _db.QuestionBankItems.Add(item);
                        result.SuccessCount++;
                    }

                    if (result.ErrorCount > 0)
                    {
                        transaction.Rollback();
                        _db.ChangeTracker.Clear();
                        string errorMsg = $"Import thất bại (Đã hủy toàn bộ thay đổi để đảm bảo dữ liệu toàn vẹn)!\n\nSố dòng lỗi: {result.ErrorCount}\n\nChi tiết lỗi:\n" + string.Join("\n", result.ErrorDetails.Take(10));
                        if (result.ErrorCount > 10) errorMsg += $"\n... và {result.ErrorCount - 10} lỗi khác.";
                        MessageBox.Show(errorMsg, "Lỗi Import (Rollback)", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    _db.SaveChanges();
                    transaction.Commit();
                    LoadQuestions();
                    InitFilters();

                    string msg = $"Import hoàn tất thành công!\n\nĐã thêm: {result.SuccessCount} câu hỏi\n\nCác câu hỏi mới đã được đặt trạng thái 'Chờ duyệt' (Pending).";
                    MessageBox.Show(msg, "Kết quả Import", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _db.ChangeTracker.Clear();
                    MessageBox.Show($"Lỗi hệ thống khi Import (Rollback): {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // --- Add question dialog -------------------------
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Window
            {
                Title = "Thêm câu hỏi mới",
                Width = 500, Height = 580,
                Owner = Window.GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                FontFamily = new FontFamily("Segoe UI"),
                ResizeMode = ResizeMode.NoResize
            };

            var sp = new StackPanel { Margin = new Thickness(20) };

            var txtContent = new TextBox { FontSize = 13, AcceptsReturn = true, Height = 60,
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) };
            sp.Children.Add(Label("Nội dung câu hỏi:"));
            sp.Children.Add(txtContent);

            var txtOptions = new TextBox { FontSize = 13, AcceptsReturn = true, Height = 50,
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) };
            sp.Children.Add(Label("Lựa chọn (ngăn cách bằng dấu | ví dụ: A|B|C|D):"));
            sp.Children.Add(txtOptions);

            var txtAnswer = new TextBox { FontSize = 13, Margin = new Thickness(0, 0, 0, 10) };
            sp.Children.Add(Label("Đáp án đúng:"));
            sp.Children.Add(txtAnswer);

            var cboSubj = new ComboBox { FontSize = 13, Margin = new Thickness(0, 0, 0, 10) };
            foreach (var s in new[] { "Toán", "Vật lý", "Hóa học", "Sinh học", "Ngữ văn", "Tiếng Anh", "Lịch sử", "Địa lý", "GDCD", "Tin học" })
                cboSubj.Items.Add(s);
            cboSubj.SelectedIndex = 0;
            sp.Children.Add(Label("Môn học:"));
            sp.Children.Add(cboSubj);

            var cboGrade = new ComboBox { FontSize = 13, Margin = new Thickness(0, 0, 0, 10) };
            cboGrade.Items.Add("10"); cboGrade.Items.Add("11"); cboGrade.Items.Add("12");
            cboGrade.SelectedIndex = 0;
            sp.Children.Add(Label("Khối lớp:"));
            sp.Children.Add(cboGrade);

            var cboDiff = new ComboBox { FontSize = 13, Margin = new Thickness(0, 0, 0, 10) };
            cboDiff.Items.Add("Dễ"); cboDiff.Items.Add("Trung bình"); cboDiff.Items.Add("Khó");
            cboDiff.SelectedIndex = 0;
            sp.Children.Add(Label("Mức độ:"));
            sp.Children.Add(cboDiff);

            var btnSave = new Button
            {
                Content = "Lưu câu hỏi", FontSize = 14, Padding = new Thickness(16, 8, 16, 8),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                Foreground = Brushes.White, BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 8, 0, 0)
            };
            btnSave.Click += (_, __) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(txtContent.Text))
                    {
                        MessageBox.Show("Vui lòng nhập nội dung câu hỏi.", "Thiếu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(txtAnswer.Text))
                    {
                        MessageBox.Show("Vui lòng nhập đáp án đúng.", "Thiếu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int defaultCatId = 1;
                    var firstCat = _db.QuestionBankCategories.FirstOrDefault();
                    if (firstCat != null) defaultCatId = firstCat.Id;
                    else
                    {
                        firstCat = new QuestionBankCategory { Name = "Chung", Subject = "Chung", Grade = "Chung" };
                        _db.QuestionBankCategories.Add(firstCat);
                        _db.SaveChanges();
                        defaultCatId = firstCat.Id;
                    }

                    var opts = txtOptions.Text.Split('|').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                    if (!opts.Any())
                    {
                        MessageBox.Show("Vui lòng nhập các lựa chọn cho câu hỏi MCQ (ngăn cách bằng dấu |).", "Thiếu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    bool found = opts.Any(opt => opt.Equals(txtAnswer.Text.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (!found)
                    {
                        MessageBox.Show("Đáp án đúng phải trùng khớp với một trong các phương án lựa chọn đã nhập (không phân biệt chữ hoa/thường, khoảng trắng).", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    string optionsJson = System.Text.Json.JsonSerializer.Serialize(opts);

                    var currentUser = QASmartClass.Staff.Services.StaffSession.CurrentUser;
                    string userCode = currentUser?.TeacherCode ?? "GV001";
                    string userRole = QASmartClass.Staff.Services.StaffSession.Role;
                    string initialApproval = (userRole == "Admin" || userRole == "HieuTruong" || userRole == "HieuPho") ? "Approved" : "Pending";

                    var item = new QuestionBankItem
                    {
                        CategoryId    = defaultCatId,
                        Content       = txtContent.Text.Trim(),
                        OptionsJson   = optionsJson,
                        CorrectAnswer = txtAnswer.Text.Trim(),
                        Subject       = cboSubj.SelectedItem?.ToString() ?? "Toán",
                        Grade         = cboGrade.SelectedItem?.ToString() ?? "10",
                        Difficulty    = cboDiff.SelectedItem?.ToString() switch { "Dễ" => "Easy", "Trung bình" => "Medium", "Khó" => "Hard", _ => "Easy" },
                        QuestionType  = "MCQ",
                        Points        = 10,
                        CreatedAt     = DateTime.Now,
                        TotalAttempts = 0,
                        WrongCount    = 0,
                        CreatedBy     = userCode,
                        ApprovalStatus = initialApproval
                    };

                    _db.QuestionBankItems.Add(item);
                    _db.SaveChanges();

                    MessageBox.Show("Đã lưu câu hỏi thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    dlg.Close();
                    LoadQuestions();
                    InitFilters(); // refresh subjects
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            sp.Children.Add(btnSave);

            dlg.Content = new ScrollViewer { Content = sp };
            dlg.ShowDialog();
        }

        // --- F1.1: Share question ------------------------
        private void BtnShare_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                try
                {
                    var item = _db.QuestionBankItems.Find(id);
                    if (item != null)
                    {
                        if (item.IsPublicToDepartment)
                        {
                            MessageBox.Show("Câu hỏi này đã được chia sẻ công khai cho Tổ bộ môn rồi.", "Đã chia sẻ", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        var result = MessageBox.Show($"Chia sẻ câu hỏi '{item.Content.Substring(0, Math.Min(20, item.Content.Length))}...' vào thư viện dùng chung của Tổ bộ môn?", 
                            "Xác nhận chia sẻ", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        
                        if (result == MessageBoxResult.Yes)
                        {
                            item.IsPublicToDepartment = true;
                            _db.SaveChanges();
                            
                            // Log Audit
                            string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "Teacher";
                            QASmartClass.Services.AuditHelper.Log(_db, "Share_Question", actor, $"Shared Question ID {id} to Department");
                            
                            MessageBox.Show("Đã chia sẻ thành công!", "Chia sẻ", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadQuestions();
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        // --- Delete question -----------------------------
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            if (sender is Button btn && btn.Tag is int id)
            {
                var result = MessageBox.Show("Xác nhận xóa câu hỏi này khỏi ngân hàng?", "Xác nhận xóa",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var item = _db.QuestionBankItems.Find(id);
                        if (item != null)
                        {
                            var currentUser = QASmartClass.Staff.Services.StaffSession.CurrentUser;
                            string userCode = currentUser?.TeacherCode ?? "GV001";
                            string userRole = QASmartClass.Staff.Services.StaffSession.Role;

                            bool isAdmin = userRole == "Admin" || userRole == "HieuTruong";
                            bool isOwner = string.IsNullOrEmpty(item.CreatedBy) || item.CreatedBy == userCode;

                            if (!isAdmin && !isOwner)
                            {
                                MessageBox.Show("Bạn không có quyền xóa câu hỏi này (chỉ người tạo hoặc quản trị viên mới có quyền).", 
                                    "Không đủ thẩm quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            _db.QuestionBankItems.Remove(item);
                            _db.SaveChanges();

                            // Log Audit
                            string actor = currentUser?.FullName ?? "Teacher";
                            QASmartClass.Services.AuditHelper.Log(_db, "Delete_Question", actor, $"Deleted Question ID {id}: {item.Content}");

                            LoadQuestions();
                        }
                    }
                    catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
            }
        }

        private static string NormalizeDifficulty(string input)
        {
            if (string.IsNullOrEmpty(input)) return "Medium";
            string normalized = input.Trim().ToLower();
            if (normalized == "dễ" || normalized == "de" || normalized == "easy") return "Easy";
            if (normalized == "khó" || normalized == "kho" || normalized == "hard") return "Hard";
            return "Medium";
        }

        private static string NormalizeSubject(string input)
        {
            if (string.IsNullOrEmpty(input)) return "Toán";
            string n = input.Trim();
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "toan", "Toán" }, { "toán", "Toán" },
                { "ly", "Vật lý" }, { "vật lý", "Vật lý" }, { "vat ly", "Vật lý" }, { "vật lí", "Vật lý" }, { "vat li", "Vật lý" },
                { "hoa", "Hóa học" }, { "hóa học", "Hóa học" }, { "hoa hoc", "Hóa học" },
                { "sinh", "Sinh học" }, { "sinh học", "Sinh học" }, { "sinh hoc", "Sinh học" },
                { "van", "Ngữ văn" }, { "ngữ văn", "Ngữ văn" }, { "ngu van", "Ngữ văn" },
                { "anh", "Tiếng Anh" }, { "tiếng anh", "Tiếng Anh" }, { "tieng anh", "Tiếng Anh" },
                { "su", "Lịch sử" }, { "lịch sử", "Lịch sử" }, { "lich su", "Lịch sử" },
                { "dia", "Địa lý" }, { "địa lý", "Địa lý" }, { "dia ly", "Địa lý" }, { "địa lí", "Địa lý" }, { "dia li", "Địa lý" },
                { "gdcd", "GDCD" },
                { "tin", "Tin học" }, { "tin học", "Tin học" }, { "tin hoc", "Tin học" }
            };
            return map.GetValueOrDefault(n, n);
        }

        private static string NormalizeGrade(string input)
        {
            if (string.IsNullOrEmpty(input)) return "10";
            string n = input.Trim();
            if (n.Contains("10")) return "10";
            if (n.Contains("11")) return "11";
            if (n.Contains("12")) return "12";
            return "10";
        }

        private void NormalizeExistingDbData()
        {
            if (_db == null) return;
            try
            {
                // Create index on Subject and Grade if not exists
                _db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS idx_questionbankitem_subject_grade ON QuestionBankItems (Subject, Grade);");

                var items = _db.QuestionBankItems.ToList();
                bool changed = false;
                foreach (var item in items)
                {
                    string normalizedSubject = NormalizeSubject(item.Subject);
                    string normalizedDifficulty = NormalizeDifficulty(item.Difficulty);
                    if (item.Subject != normalizedSubject)
                    {
                        item.Subject = normalizedSubject;
                        changed = true;
                    }
                    if (item.Difficulty != normalizedDifficulty)
                    {
                        item.Difficulty = normalizedDifficulty;
                        changed = true;
                    }
                }
                if (changed)
                {
                    _db.SaveChanges();
                    Log.Information("Normalized subjects and difficulties in database.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("NormalizeExistingDbData error: {Err}", ex.Message);
            }
        }

        private static TextBlock Label(string text) => new TextBlock
        {
            Text = text, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
            Margin = new Thickness(0, 0, 0, 4)
        };

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                ApplyFilter();
            }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage++;
            ApplyFilter();
        }

        private void BtnDownloadTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    FileName = "QuestionBank_Template.xlsx",
                    Title = "Tải file Excel biểu mẫu câu hỏi"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    using (var package = new OfficeOpenXml.ExcelPackage())
                    {
                        var ws = package.Workbook.Worksheets.Add("SampleQuestions");
                        ws.Cells[1, 1].Value = "Nội dung câu hỏi";
                        ws.Cells[1, 2].Value = "Loại câu hỏi (MCQ, TF, FIB, MATCH, SHORT, ORDER)";
                        ws.Cells[1, 3].Value = "Các lựa chọn (Ngăn cách bằng dấu |)";
                        ws.Cells[1, 4].Value = "Đáp án đúng";
                        ws.Cells[1, 5].Value = "Môn học";
                        ws.Cells[1, 6].Value = "Mức độ (Dễ, Trung bình, Khó)";
                        ws.Cells[1, 7].Value = "Khối lớp (10, 11, 12)";

                        // Set header style
                        using (var range = ws.Cells[1, 1, 1, 7])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        }

                        // Add mockup rows
                        ws.Cells[2, 1].Value = "[DÒNG MẪU - XÓA KHI NHẬP] Một tam giác có mấy cạnh?";
                        ws.Cells[2, 2].Value = "MCQ";
                        ws.Cells[2, 3].Value = "2 | 3 | 4 | 5";
                        ws.Cells[2, 4].Value = "3";
                        ws.Cells[2, 5].Value = "Toán";
                        ws.Cells[2, 6].Value = "Dễ";
                        ws.Cells[2, 7].Value = "10";

                        ws.Cells[3, 1].Value = "[DÒNG MẪU - XÓA KHI NHẬP] Cường độ dòng điện ký hiệu là gì?";
                        ws.Cells[3, 2].Value = "SHORT";
                        ws.Cells[3, 3].Value = "";
                        ws.Cells[3, 4].Value = "I";
                        ws.Cells[3, 5].Value = "Vật lý";
                        ws.Cells[3, 6].Value = "Trung bình";
                        ws.Cells[3, 7].Value = "11";

                        // Set style for mockup rows
                        using (var range = ws.Cells[2, 1, 3, 7])
                        {
                            range.Style.Font.Italic = true;
                            range.Style.Font.Color.SetColor(System.Drawing.Color.Gray);
                            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                        }

                        ws.Column(1).Width = 40;
                        ws.Column(2).Width = 20;
                        ws.Column(3).Width = 30;
                        ws.Column(4).Width = 20;
                        ws.Column(5).Width = 12;
                        ws.Column(6).Width = 12;
                        ws.Column(7).Width = 20;

                        package.SaveAs(new System.IO.FileInfo(filePath));
                    }
                    MessageBox.Show("Đã tải biểu mẫu Excel thành công!", "Tải biểu mẫu", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải biểu mẫu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBulkApprove_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            var viewModels = LvQuestions.ItemsSource as List<QuestionViewModel>;
            if (viewModels == null) return;

            var selectedIds = viewModels.Where(vm => vm.IsSelected).Select(vm => vm.Id).ToList();
            if (!selectedIds.Any())
            {
                MessageBox.Show("Vui lòng chọn ít nhất một câu hỏi để duyệt.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmResult = MessageBox.Show($"Bạn có chắc chắn muốn phê duyệt {selectedIds.Count} câu hỏi đã chọn?", "Xác nhận duyệt hàng loạt", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmResult != MessageBoxResult.Yes) return;

            try
            {
                var itemsToApprove = _db.QuestionBankItems.Where(q => selectedIds.Contains(q.Id)).ToList();
                string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "Teacher";
                foreach (var item in itemsToApprove)
                {
                    item.ApprovalStatus = "Approved";
                    item.ApprovedBy = actor;
                }
                _db.SaveChanges();

                // Log Audit
                string idsStr = string.Join(", ", selectedIds);
                QASmartClass.Services.AuditHelper.Log(_db, "Bulk_Approve_Questions", actor, $"Approved {itemsToApprove.Count} questions in bulk. Question IDs: [{idsStr}]");

                MessageBox.Show($"Đã duyệt thành công {itemsToApprove.Count} câu hỏi!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi duyệt hàng loạt: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string SanitizeInputText(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input.Replace("<", "&lt;").Replace(">", "&gt;").Trim();
        }

        private void BtnBulkReject_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            var viewModels = LvQuestions.ItemsSource as List<QuestionViewModel>;
            if (viewModels == null) return;

            var selectedIds = viewModels.Where(vm => vm.IsSelected).Select(vm => vm.Id).ToList();
            if (!selectedIds.Any())
            {
                MessageBox.Show("Vui lòng chọn ít nhất một câu hỏi để từ chối.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string reason = "";
            bool isConfirmed = false;
            
            var dlg = new Window
            {
                Title = "Lý do từ chối phê duyệt",
                Width = 400, Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                FontFamily = new FontFamily("Segoe UI"),
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent
            };

            var border = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                BorderThickness = new Thickness(1)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var titleBar = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")),
                CornerRadius = new CornerRadius(7, 7, 0, 0),
                Height = 35,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            titleBar.MouseLeftButtonDown += (s2, e2) => {
                if (e2.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                {
                    dlg.DragMove();
                }
            };

            var titleText = new TextBlock
            {
                Text = "Lý do từ chối phê duyệt",
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0)
            };
            titleBar.Child = titleText;
            Grid.SetRow(titleBar, 0);
            grid.Children.Add(titleBar);

            var sp = new StackPanel { Margin = new Thickness(16) };
            sp.Children.Add(new TextBlock { Text = $"Nhập lý do từ chối {selectedIds.Count} câu hỏi đã chọn:", FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0,0,0,8) });
            
            var txtReason = new TextBox { FontSize = 13, Padding = new Thickness(4), Height = 30, VerticalContentAlignment = VerticalAlignment.Center };
            sp.Children.Add(txtReason);

            var buttonsGrid = new Grid { Margin = new Thickness(0, 15, 0, 0) };
            buttonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            buttonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var btnConfirm = new Button { Content = "Xác nhận", Height = 30, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
            btnConfirm.Click += (s2, e2) => {
                reason = SanitizeInputText(txtReason.Text);
                isConfirmed = true;
                dlg.Close();
            };
            Grid.SetColumn(btnConfirm, 0);
            buttonsGrid.Children.Add(btnConfirm);

            var btnCancel = new Button { Content = "Hủy bỏ", Height = 30, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
            btnCancel.Click += (s2, e2) => {
                dlg.Close();
            };
            Grid.SetColumn(btnCancel, 2);
            buttonsGrid.Children.Add(btnCancel);

            sp.Children.Add(buttonsGrid);
            Grid.SetRow(sp, 1);
            grid.Children.Add(sp);

            border.Child = grid;
            dlg.Content = border;
            dlg.ShowDialog();

            if (!isConfirmed) return;

            if (string.IsNullOrEmpty(reason))
            {
                reason = "Không có lý do cụ thể";
            }

            try
            {
                var itemsToReject = _db.QuestionBankItems.Where(q => selectedIds.Contains(q.Id)).ToList();
                string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "Teacher";
                foreach (var item in itemsToReject)
                {
                    item.ApprovalStatus = "Rejected";
                    item.ReviewComments = reason;
                }
                _db.SaveChanges();

                string idsStr = string.Join(", ", selectedIds);
                QASmartClass.Services.AuditHelper.Log(_db, "Bulk_Reject_Questions", actor, $"Rejected {itemsToReject.Count} questions in bulk. Reason: '{reason}'. Question IDs: [{idsStr}]");

                MessageBox.Show($"Đã từ chối phê duyệt thành công {itemsToReject.Count} câu hỏi!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi từ chối hàng loạt: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportReport_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    FileName = "QuestionBank_SummaryReport.xlsx",
                    Title = "Xuất báo cáo tổng hợp ngân hàng câu hỏi"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    var questions = _db.QuestionBankItems.ToList();
                    
                    // Group by Subject
                    var reportData = questions.GroupBy(q => q.Subject)
                        .Select(g => {
                            int total = g.Count();
                            int approved = g.Count(q => q.ApprovalStatus == "Approved");
                            int pending = g.Count(q => q.ApprovalStatus == "Pending");
                            int rejected = g.Count(q => q.ApprovalStatus == "Rejected");
                            
                            double totalRate = 0;
                            int countWithAttempts = 0;
                            foreach (var q in g)
                            {
                                if (q.TotalAttempts > 0)
                                {
                                    totalRate += (double)q.WrongCount / q.TotalAttempts;
                                    countWithAttempts++;
                                }
                            }
                            double avgWrongRate = countWithAttempts > 0 ? (totalRate / countWithAttempts) * 100 : 0;
                            
                            return new {
                                MonHoc = g.Key,
                                TongSo = total,
                                DaDuyet = approved,
                                ChoDuyet = pending,
                                TuChoi = rejected,
                                TyLeDuyet = total > 0 ? ((double)approved / total) * 100 : 0,
                                AvgWrong = avgWrongRate
                            };
                        }).ToList();

                    using (var package = new OfficeOpenXml.ExcelPackage())
                    {
                        var ws = package.Workbook.Worksheets.Add("Báo cáo chuyên môn");
                        
                        ws.Cells[1, 1].Value = "BÁO CÁO TỔNG HỢP CHẤT LƯỢNG NGÂN HÀNG CÂU HỎI";
                        ws.Cells[1, 1].Style.Font.Size = 16;
                        ws.Cells[1, 1].Style.Font.Bold = true;
                        ws.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.Navy);
                        
                        ws.Cells[2, 1].Value = $"Ngày lập báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}";
                        ws.Cells[2, 1].Style.Font.Italic = true;
                        
                        ws.Cells[4, 1].Value = "Môn học";
                        ws.Cells[4, 2].Value = "Tổng số câu";
                        ws.Cells[4, 3].Value = "Đã duyệt";
                        ws.Cells[4, 4].Value = "Chờ duyệt";
                        ws.Cells[4, 5].Value = "Từ chối";
                        ws.Cells[4, 6].Value = "Tỷ lệ đã duyệt (%)";
                        ws.Cells[4, 7].Value = "Tỷ lệ làm sai TB (%)";

                        using (var range = ws.Cells[4, 1, 4, 7])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                        }

                        int row = 5;
                        foreach (var item in reportData)
                        {
                            ws.Cells[row, 1].Value = item.MonHoc;
                            ws.Cells[row, 2].Value = item.TongSo;
                            ws.Cells[row, 3].Value = item.DaDuyet;
                            ws.Cells[row, 4].Value = item.ChoDuyet;
                            ws.Cells[row, 5].Value = item.TuChoi;
                            ws.Cells[row, 6].Value = item.TyLeDuyet;
                            ws.Cells[row, 6].Style.Numberformat.Format = "0.0";
                            ws.Cells[row, 7].Value = item.AvgWrong;
                            ws.Cells[row, 7].Style.Numberformat.Format = "0.0";
                            row++;
                        }

                        using (var range = ws.Cells[4, 1, row - 1, 7])
                        {
                            range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        }

                        ws.Column(1).Width = 15;
                        ws.Column(2).Width = 15;
                        ws.Column(3).Width = 15;
                        ws.Column(4).Width = 15;
                        ws.Column(5).Width = 15;
                        ws.Column(6).Width = 20;
                        ws.Column(7).Width = 22;

                        package.SaveAs(new System.IO.FileInfo(filePath));
                    }
                    MessageBox.Show("Đã xuất báo cáo tổng hợp thành công!", "Báo cáo tổng hợp", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất báo cáo: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChkSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            var viewModels = LvQuestions.ItemsSource as List<QuestionViewModel>;
            if (viewModels == null) return;
            foreach (var vm in viewModels)
            {
                vm.IsSelected = true;
            }
            LvQuestions.Items.Refresh();
        }

        private void ChkSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            var viewModels = LvQuestions.ItemsSource as List<QuestionViewModel>;
            if (viewModels == null) return;
            foreach (var vm in viewModels)
            {
                vm.IsSelected = false;
            }
            LvQuestions.Items.Refresh();
        }

        private void BtnApplyDifficulty_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            if (sender is Button btn && btn.Tag is int id)
            {
                try
                {
                    var item = _db.QuestionBankItems.Find(id);
                    if (item != null)
                    {
                        if (item.TotalAttempts < 5) return;
                        double rate = (double)item.WrongCount / item.TotalAttempts;
                        string targetDiff = "";
                        string targetDiffVi = "";
                        if (rate > 0.70 && item.Difficulty != "Hard")
                        {
                            targetDiff = "Hard";
                            targetDiffVi = "Khó";
                        }
                        else if (rate < 0.15 && item.Difficulty != "Easy")
                        {
                            targetDiff = "Easy";
                            targetDiffVi = "Dễ";
                        }

                        if (!string.IsNullOrEmpty(targetDiff))
                        {
                            var confirmResult = MessageBox.Show($"Bạn có muốn cập nhật độ khó của câu hỏi này thành '{targetDiffVi}' theo khuyến nghị?", "Xác nhận áp dụng độ khó", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (confirmResult == MessageBoxResult.Yes)
                            {
                                item.Difficulty = targetDiff;
                                _db.SaveChanges();
                                
                                string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "Teacher";
                                QASmartClass.Services.AuditHelper.Log(_db, "Apply_Difficulty_Recommendation", actor, $"Updated Question ID {id} difficulty to {targetDiff}");
                                
                                MessageBox.Show("Đã cập nhật độ khó khuyến nghị thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                                ApplyFilter();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi áp dụng độ khó: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class QuestionViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public string PointsText { get; set; } = string.Empty;
        public string WrongRateText { get; set; } = string.Empty;
        public Brush DiffBg { get; set; } = Brushes.Transparent;
        public Brush DiffFg { get; set; } = Brushes.Black;
        public string ApprovalStatusText { get; set; } = string.Empty;
        public string ApprovalStatusBg { get; set; } = string.Empty;
        public string ApprovalStatusFg { get; set; } = string.Empty;
        public string DifficultyRecommendation { get; set; } = string.Empty;
        public Visibility HasRecommendationVisibility { get; set; } = Visibility.Collapsed;
        public bool IsSelected { get; set; }
    }
}

