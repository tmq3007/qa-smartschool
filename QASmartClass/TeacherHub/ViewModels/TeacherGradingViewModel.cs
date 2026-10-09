using System;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.TeacherHub.ViewModels
{
    public partial class TeacherGradingViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ClassRoster> _classes = new();

        private ClassRoster? _selectedClass;
        public ClassRoster? SelectedClass
        {
            get => _selectedClass;
            set
            {
                if (SetProperty(ref _selectedClass, value))
                {
                    _ = LoadGradesAsync();
                }
            }
        }

        [ObservableProperty]
        private string _selectedSemester = "HK1";

        public ObservableCollection<string> SemesterList { get; } = new() { "HK1", "HK2", "Cả năm" };

        private List<ClassRoster> _allClasses = new();

        partial void OnSelectedSemesterChanged(string value)
        {
            ApplyClassFilter();
        }

        private void ApplyClassFilter()
        {
            var prevClass = SelectedClass;
            Classes.Clear();
            var filtered = _allClasses.AsEnumerable();
            if (SelectedSemester != "Cả năm")
            {
                filtered = filtered.Where(r => r.Semester == SelectedSemester);
            }
            foreach (var r in filtered)
            {
                Classes.Add(r);
            }

            if (prevClass != null)
            {
                var match = Classes.FirstOrDefault(c => c.ClassName == prevClass.ClassName && c.Subject == prevClass.Subject);
                if (match != null)
                {
                    SelectedClass = match;
                    return;
                }
            }
            SelectedClass = Classes.FirstOrDefault();
        }

        [ObservableProperty]
        private DataTable? _gradingTable;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        private Dictionary<string, int> _colNameToGradeTypeId = new();

        public TeacherGradingViewModel()
        {
            _ = LoadClassesAsync();
        }

        private async Task LoadClassesAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var rosters = await db.ClassRosters.Where(r => r.IsActive).ToListAsync();

                // Tự động tương thích ngược: Nếu database cũ chỉ có HK2, nhân bản sang HK1 để tránh hiển thị trống
                if (rosters.Any() && !rosters.Any(r => r.Semester == "HK1"))
                {
                    var hk2Rosters = rosters.Where(r => r.Semester == "HK2").ToList();
                    foreach (var r in hk2Rosters)
                    {
                        var hk1Roster = new ClassRoster
                        {
                            ClassName = r.ClassName,
                            GradeLevel = r.GradeLevel,
                            SchoolYear = r.SchoolYear,
                            Semester = "HK1",
                            TeacherName = r.TeacherName,
                            Subject = r.Subject,
                            StudentCount = r.StudentCount,
                            IsActive = r.IsActive,
                            Notes = r.Notes,
                            CreatedAt = DateTime.Now,
                            LastUsedAt = DateTime.Now
                        };
                        db.ClassRosters.Add(hk1Roster);
                        await db.SaveChangesAsync(); // Lưu để lấy Id tự sinh

                        // Nhân bản ánh xạ học sinh
                        var links = await db.ClassRosterStudents.Where(rs => rs.RosterId == r.Id).ToListAsync();
                        foreach (var l in links)
                        {
                            db.ClassRosterStudents.Add(new ClassRosterStudent
                            {
                                RosterId = hk1Roster.Id,
                                StudentId = l.StudentId,
                                SeatNumber = l.SeatNumber,
                                Notes = l.Notes
                            });
                        }
                    }
                    await db.SaveChangesAsync();
                    rosters = await db.ClassRosters.Where(r => r.IsActive).ToListAsync();
                }

                // Phân quyền hiển thị: Giáo viên chỉ xem lớp mình phụ trách (Admin/BGH xem toàn trường)
                var currentTeacher = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "";
                bool isManagement = QASmartClass.Staff.Services.StaffSession.CanAccessOverview();

                if (!isManagement && !string.IsNullOrWhiteSpace(currentTeacher))
                {
                    var trimmed = currentTeacher.Trim();
                    var myRosters = rosters.Where(r => 
                        !string.IsNullOrWhiteSpace(r.TeacherName) &&
                        (string.Equals(r.TeacherName.Trim(), trimmed, StringComparison.OrdinalIgnoreCase) ||
                         r.TeacherName.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                         trimmed.Contains(r.TeacherName.Trim(), StringComparison.OrdinalIgnoreCase))
                    ).ToList();

                    if (myRosters.Any())
                    {
                        rosters = myRosters;
                    }
                }

                _allClasses = rosters;
                ApplyClassFilter();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải danh sách lớp: {ex.Message}";
            }
        }

        private async Task LoadGradesAsync()
        {
            if (SelectedClass == null)
            {
                GradingTable = null;
                return;
            }

            StatusMessage = "Đang tải dữ liệu bảng điểm...";
            try
            {
                using var db = new AppDbContext();
                
                var rosterStudents = await db.ClassRosterStudents
                    .Where(rs => rs.RosterId == SelectedClass.Id)
                    .OrderBy(rs => rs.SeatNumber)
                    .ToListAsync();
                    
                var studentIds = rosterStudents.Select(rs => rs.StudentId).ToList();
                var students = await db.Students.Where(s => studentIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id);

                var gradeTypes = await db.GradeTypeMasters.Where(g => g.IsActive).OrderBy(g => g.SortOrder).ToListAsync();

                var existingGrades = await db.StudentGrades
                    .Where(g => g.RosterId == SelectedClass.Id)
                    .ToListAsync();

                var table = new DataTable();
                table.Columns.Add("StudentId", typeof(int));
                table.Columns.Add("STT", typeof(int));
                table.Columns.Add("Mã HS", typeof(string));
                table.Columns.Add("Họ và tên", typeof(string));

                _colNameToGradeTypeId.Clear();

                foreach (var gt in gradeTypes)
                {
                    string colName = $"{gt.ShortName} (HS{gt.Weight})";
                    int suffix = 1;
                    while (table.Columns.Contains(colName))
                    {
                        colName = $"{gt.ShortName} (HS{gt.Weight}) {suffix++}";
                    }
                    
                    table.Columns.Add(colName, typeof(string));
                    _colNameToGradeTypeId[colName] = gt.Id;
                }

                foreach (var rs in rosterStudents)
                {
                    if (!students.TryGetValue(rs.StudentId, out var student)) continue;

                    var row = table.NewRow();
                    row["StudentId"] = student.Id;
                    row["STT"] = rs.SeatNumber;
                    row["Mã HS"] = student.StudentCode;
                    row["Họ và tên"] = student.FullName;

                    foreach (var kvp in _colNameToGradeTypeId)
                    {
                        var gtId = kvp.Value;
                        var grade = existingGrades.FirstOrDefault(g => g.StudentId == student.Id && g.GradeTypeId == gtId && g.Attempt == 1);
                        row[kvp.Key] = grade != null ? grade.Score.ToString("0.##") : "";
                    }

                    table.Rows.Add(row);
                }

                table.AcceptChanges();
                GradingTable = table;
                StatusMessage = $"Đã tải bảng điểm cho {SelectedClass.DisplayName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải bảng điểm: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task SaveGradesAsync()
        {
            if (GradingTable == null || SelectedClass == null) return;

            StatusMessage = "Đang lưu bảng điểm...";
            try
            {
                using var db = new AppDbContext();
                int rosterId = SelectedClass.Id;
                
                var existingGrades = await db.StudentGrades
                    .Where(g => g.RosterId == rosterId && g.Attempt == 1)
                    .ToListAsync();

                foreach (DataRow row in GradingTable.Rows)
                {
                    int studentId = (int)row["StudentId"];
                    
                    foreach (var kvp in _colNameToGradeTypeId)
                    {
                        string colName = kvp.Key;
                        int gtId = kvp.Value;
                        string valStr = row[colName]?.ToString() ?? string.Empty;
                        
                        var existing = existingGrades.FirstOrDefault(g => 
                            g.StudentId == studentId && 
                            g.GradeTypeId == gtId);

                        string cleanValStr = (valStr ?? "").Trim().Replace(',', '.');
                        if (double.TryParse(cleanValStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double score))
                        {
                            if (score < 0 || score > 10)
                            {
                                System.Windows.MessageBox.Show($"Điểm không hợp lệ: {valStr}. Điểm phải từ 0 đến 10.", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                                StatusMessage = "❌ Lỗi: Điểm không hợp lệ.";
                                return;
                            }
                            
                            if (existing != null)
                            {
                                existing.Score = score;
                                existing.UpdatedAt = DateTime.Now;
                            }
                            else
                            {
                                db.StudentGrades.Add(new StudentGrade
                                {
                                    StudentId = studentId,
                                    RosterId = rosterId,
                                    GradeTypeId = gtId,
                                    Score = score,
                                    Attempt = 1,
                                    EnteredBy = "Giáo viên",
                                    UpdatedAt = DateTime.Now
                                });
                            }
                        }
                        else
                        {
                            if (existing != null && string.IsNullOrWhiteSpace(valStr))
                            {
                                db.StudentGrades.Remove(existing);
                            }
                            else if (!string.IsNullOrWhiteSpace(valStr))
                            {
                                System.Windows.MessageBox.Show($"Điểm không hợp lệ (có chứa ký tự chữ): {valStr}.", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                                StatusMessage = "❌ Lỗi: Điểm không hợp lệ.";
                                return;
                            }
                        }
                    }
                }

                await db.SaveChangesAsync();
                StatusMessage = "✅ Lưu bảng điểm thành công!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Lỗi lưu bảng điểm: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ExportGrades()
        {
            if (GradingTable == null || SelectedClass == null) return;
            
            string defaultName = $"BangDiem_{SelectedClass.DisplayName}_{DateTime.Now:yyyyMMdd}";
            string filePath = QASmartClass.Services.ExcelDataService.GetSaveFilePath(defaultName);
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                bool ok = QASmartClass.Services.ExcelDataService.ExportDataTableToExcel(filePath, GradingTable, "Bảng Điểm");
                if (ok)
                {
                    StatusMessage = $"✅ Đã xuất bảng điểm thành công ra file Excel!";
                }
                else
                {
                    StatusMessage = $"❌ Lỗi xuất bảng điểm.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Lỗi: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ImportGrades()
        {
            if (GradingTable == null || SelectedClass == null) return;

            string filePath = QASmartClass.Services.ExcelDataService.GetOpenFilePath("Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv");
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                int successCount = 0;
                bool isCsv = System.IO.Path.GetExtension(filePath).Equals(".csv", StringComparison.OrdinalIgnoreCase);

                if (isCsv)
                {
                    char delimiter = ',';
                    if (System.IO.File.Exists(filePath))
                    {
                        string firstLine = System.IO.File.ReadLines(filePath).FirstOrDefault();
                        if (firstLine != null)
                        {
                            int commaCount = firstLine.Count(c => c == ',');
                            int semicolonCount = firstLine.Count(c => c == ';');
                            if (semicolonCount > commaCount)
                            {
                                delimiter = ';';
                            }
                        }
                    }

                    var parsedRows = QASmartClass.Helpers.CsvHelper.ParseFile(filePath, hasHeader: false, delimiter: delimiter);
                    if (parsedRows.Count < 2) throw new Exception("File không có dữ liệu (Chỉ có Header).");

                    var headers = parsedRows[0];
                    var colMapping = new Dictionary<string, int>(); // ColumnName -> ColumnIndex (0-based)

                    for (int c = 0; c < headers.Length; c++)
                    {
                        string header = headers[c];
                        if (string.IsNullOrEmpty(header)) continue;

                        string cleanedHeader = CleanHeader(header);

                        // Match grade columns
                        foreach (var key in _colNameToGradeTypeId.Keys)
                        {
                            if (key.Equals(header, StringComparison.OrdinalIgnoreCase) ||
                                CleanHeader(key) == cleanedHeader)
                            {
                                colMapping[key] = c;
                                break;
                            }
                        }

                        // Match Mã HS
                        if (cleanedHeader == "mahs" || 
                            cleanedHeader == "mahocsinh" || 
                            cleanedHeader == "studentcode" ||
                            header.Equals("Mã HS", StringComparison.OrdinalIgnoreCase) ||
                            header.Equals("Ma HS", StringComparison.OrdinalIgnoreCase))
                        {
                            colMapping["Mã HS"] = c;
                        }
                    }

                    if (!colMapping.ContainsKey("Mã HS")) throw new Exception("File không có cột 'Mã HS'.");

                    for (int r = 1; r < parsedRows.Count; r++)
                    {
                        var row = parsedRows[r];
                        if (colMapping["Mã HS"] >= row.Length) continue;

                        string studentCode = row[colMapping["Mã HS"]]?.Trim().Trim('"').Trim();
                        if (string.IsNullOrEmpty(studentCode)) continue;

                        // Find row in GradingTable
                        DataRow targetRow = null;
                        foreach (DataRow dr in GradingTable.Rows)
                        {
                            if (dr["Mã HS"]?.ToString() == studentCode)
                            {
                                targetRow = dr;
                                break;
                            }
                        }

                        if (targetRow != null)
                        {
                            foreach (var kvp in colMapping)
                            {
                                if (kvp.Key == "Mã HS") continue;
                                if (kvp.Value >= row.Length) continue;

                                string val = row[kvp.Value]?.Trim().Trim('"').Trim().Replace(',', '.');
                                if (!string.IsNullOrEmpty(val))
                                {
                                    targetRow[kvp.Key] = val;
                                    successCount++;
                                }
                            }
                        }
                    }

                    GradingTable.AcceptChanges();
                    StatusMessage = $"✅ Đã import {successCount} ô điểm từ CSV. Vui lòng nhấn 'Lưu Điểm' để ghi vào hệ thống.";
                }
                else
                {
                    using var package = new OfficeOpenXml.ExcelPackage(new System.IO.FileInfo(filePath));
                    var ws = package.Workbook.Worksheets.FirstOrDefault();
                    if (ws == null) throw new Exception("Không tìm thấy Worksheet nào.");

                    int rowCount = ws.Dimension?.Rows ?? 0;
                    int colCount = ws.Dimension?.Columns ?? 0;
                    if (rowCount < 2) throw new Exception("File không có dữ liệu (Chỉ có Header).");

                    // Match columns by name
                    var colMapping = new Dictionary<string, int>(); // ColumnName -> ExcelColumnIndex (1-based)
                    for (int c = 1; c <= colCount; c++)
                    {
                        string header = ws.Cells[1, c].Text?.Trim();
                        if (string.IsNullOrEmpty(header)) continue;

                        string cleanedHeader = CleanHeader(header);

                        // Match grade columns
                        foreach (var key in _colNameToGradeTypeId.Keys)
                        {
                            if (key.Equals(header, StringComparison.OrdinalIgnoreCase) ||
                                CleanHeader(key) == cleanedHeader)
                            {
                                colMapping[key] = c;
                                break;
                            }
                        }

                        // Match Mã HS
                        if (cleanedHeader == "mahs" || 
                            cleanedHeader == "mahocsinh" || 
                            cleanedHeader == "studentcode" ||
                            header.Equals("Mã HS", StringComparison.OrdinalIgnoreCase) ||
                            header.Equals("Ma HS", StringComparison.OrdinalIgnoreCase))
                        {
                            colMapping["Mã HS"] = c;
                        }
                    }

                    if (!colMapping.ContainsKey("Mã HS")) throw new Exception("File không có cột 'Mã HS'.");

                    for (int r = 2; r <= rowCount; r++)
                    {
                        string studentCode = ws.Cells[r, colMapping["Mã HS"]].Text?.Trim();
                        if (string.IsNullOrEmpty(studentCode)) continue;

                        // Find row in GradingTable
                        DataRow targetRow = null;
                        foreach (DataRow dr in GradingTable.Rows)
                        {
                            if (dr["Mã HS"]?.ToString() == studentCode)
                            {
                                targetRow = dr;
                                break;
                            }
                        }

                        if (targetRow != null)
                        {
                            foreach (var kvp in colMapping)
                            {
                                if (kvp.Key == "Mã HS") continue;
                                string val = ws.Cells[r, kvp.Value].Text?.Trim().Replace(',', '.');
                                if (!string.IsNullOrEmpty(val))
                                {
                                    targetRow[kvp.Key] = val;
                                    successCount++;
                                }
                            }
                        }
                    }
                    
                    GradingTable.AcceptChanges();
                    StatusMessage = $"✅ Đã import {successCount} ô điểm từ Excel. Vui lòng nhấn 'Lưu Điểm' để ghi vào hệ thống.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Lỗi Import: {ex.Message}";
            }
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            
            var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            var stringBuilder = new System.Text.StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        private static string CleanHeader(string header)
        {
            if (string.IsNullOrEmpty(header)) return string.Empty;
            
            // Trim quotes and whitespace
            header = header.Trim('"').Trim();
            
            // Remove BOM if present
            if (header.StartsWith("\uFEFF", StringComparison.Ordinal))
            {
                header = header.Substring(1);
            }
            
            // Replace \uFFFD (replacement char) with 'a' (handling ANSI -> UTF8 corruptions for 'ã')
            header = header.Replace("\uFFFD", "a");
            
            // Strip accents
            header = RemoveDiacritics(header);
            
            // Convert to lowercase and keep alphanumeric only
            var sb = new System.Text.StringBuilder();
            foreach (char c in header)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
            }
            return sb.ToString();
        }
    }
}

