using System;
using System.Globalization;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.Data;
using QASmartClass.Classroom.Helpers;
using Serilog;
using QASmartClass.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace QASmartClass.Classroom.Views
{
    public partial class ReportPage : Page
    {
        private AppDbContext _db = null!;
        private List<GradeTypeMaster> _gradeTypes = new();
        private int _selectedRosterId;
        private bool _suppressFilterEvent;
        private bool _isSimulatorMode = false;
        private List<Student> _currentStudents = new();
        private List<double> _currentAvgs = new();

        public ReportPage()
        {
            InitializeComponent();
            Loaded += (_, _) => InitPage();
        }

        // ═══════════════════════════════════════════════════
        //  INIT & FILTER
        // ═══════════════════════════════════════════════════

        private void InitPage()
        {
            try
            {
                // → ClassroomAppContext
                _db = ClassroomAppContext.Db;

                // Load grade type master config
                _gradeTypes = _db.GradeTypeMasters
                    .Where(g => g.IsActive)
                    .OrderBy(g => g.SortOrder)
                    .ToList();

                LoadGradeTypeInfo();
                PopulateGradeLevelFilter();
                Log.Information("ReportPage initialized with {Count} grade types", _gradeTypes.Count);
            }
            catch (Exception ex)
            {
                Log.Warning("ReportPage init error: {Err}", ex.Message);
            }
        }

        private void PopulateGradeLevelFilter()
        {
            _suppressFilterEvent = true;
            var allRosters = _db.ClassRosters.Where(r => r.IsActive).ToList();
            Log.Information("PopulateGradeLevelFilter: found {Count} active rosters", allRosters.Count);

            var levels = allRosters
                .Select(r => r.GradeLevel)
                .Where(lv => !string.IsNullOrWhiteSpace(lv))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            cboGradeLevel.Items.Clear();
            cboGradeLevel.Items.Add(new ComboBoxItem { Content = "Tất cả", Tag = "", IsSelected = true });
            foreach (var lv in levels)
            {
                int count = allRosters.Count(r => r.GradeLevel == lv);
                cboGradeLevel.Items.Add(new ComboBoxItem
                {
                    Content = $"Khối {lv} ({count} lớp)",
                    Tag = lv
                });
            }
            cboGradeLevel.SelectedIndex = 0;
            _suppressFilterEvent = false;

            PopulateClassFilter();
        }

        private void PopulateClassFilter()
        {
            _suppressFilterEvent = true;
            var selectedLevel = (cboGradeLevel.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            var semester = (cboSemester.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "HK2";

            var query = _db.ClassRosters.Where(r => r.IsActive);
            if (!string.IsNullOrEmpty(selectedLevel))
                query = query.Where(r => r.GradeLevel == selectedLevel);

            // Try with semester filter first
            var rosters = query.Where(r => r.Semester == semester).OrderBy(r => r.ClassName).ToList();

            // Fallback: if no rosters match semester, show all (data may not have Semester set)
            if (!rosters.Any())
                rosters = query.OrderBy(r => r.ClassName).ToList();

            cboClass.Items.Clear();
            cboClass.Items.Add(new ComboBoxItem { Content = "— Chọn lớp —", Tag = "", IsSelected = true });
            foreach (var r in rosters)
            {
                // Rich display: "10A1 - Toán (GV Nguyễn Văn Hùng, 40 HS)"
                string display = r.DisplayName;
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(r.TeacherName))
                    parts.Add($"GV {r.TeacherName}");
                if (r.StudentCount > 0)
                    parts.Add($"{r.StudentCount} HS");
                if (parts.Any())
                    display += $" ({string.Join(", ", parts)})";

                cboClass.Items.Add(new ComboBoxItem { Content = display, Tag = r.Id.ToString() });
            }

            cboClass.SelectedIndex = 0;
            _suppressFilterEvent = false;

            txtFilterStatus.Text = $"{rosters.Count} lớp";
            Log.Information("PopulateClassFilter: {Level}/{Semester} → {Count} rosters", selectedLevel, semester, rosters.Count);
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressFilterEvent || _db == null) return;

            if (sender == cboGradeLevel || sender == cboSemester)
            {
                PopulateClassFilter();
                ClearGradeTable();
                return;
            }

            // cboClass changed
            var tag = (cboClass.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            if (int.TryParse(tag, out int rosterId) && rosterId > 0)
            {
                _selectedRosterId = rosterId;
                LoadGradeTable(rosterId);
            }
            else
            {
                ClearGradeTable();
            }
        }

        // ═══════════════════════════════════════════════════
        //  GRADE TYPE INFO PANEL
        // ═══════════════════════════════════════════════════

        private void LoadGradeTypeInfo()
        {
            gradeTypeInfoPanel.Children.Clear();

            if (!_gradeTypes.Any())
            {
                txtFormula.Text = "(Chưa cấu hình loại điểm — vào Settings để thêm)";
                return;
            }

            // Formula
            var parts = _gradeTypes.SelectMany(t =>
                Enumerable.Range(1, t.MaxAttempts).Select(a =>
                    t.MaxAttempts > 1 ? $"{t.ShortName}{a}×{t.Weight}" : $"{t.ShortName}×{t.Weight}")
            );
            int totalW = _gradeTypes.Sum(t => t.Weight * t.MaxAttempts);
            txtFormula.Text = $"TB = ({string.Join(" + ", parts)}) / {totalW}";

            // Info badges
            foreach (var gt in _gradeTypes)
            {
                var badge = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(241, 243, 245)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 0, 4)
                };
                badge.Child = new TextBlock
                {
                    Text = $"{gt.DisplayName} ({gt.ShortName}) — HS {gt.Weight}, {gt.MaxAttempts} lần",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(73, 80, 87))
                };
                gradeTypeInfoPanel.Children.Add(badge);
            }
        }

        // ═══════════════════════════════════════════════════
        //  GRADE TABLE — DYNAMIC COLUMNS + DATA
        // ═══════════════════════════════════════════════════

        private DataTable _dataTable = new();
        private List<(int GradeTypeId, int Attempt, string ColName)> _gradeColumns = new();

        private void ClearGradeTable()
        {
            _isSimulatorMode = false;
            _selectedRosterId = 0;
            _currentStudents.Clear();
            _currentAvgs.Clear();
            gradeDataGrid.ItemsSource = null;
            gradeDataGrid.Columns.Clear();
            _dataTable.Clear();
            txtNoData.Visibility = Visibility.Visible;
            btnSaveAll.Visibility = Visibility.Collapsed;
            btnTemplate.Visibility = Visibility.Collapsed;
            btnImport.Visibility = Visibility.Collapsed;
            btnSimulator.Visibility = Visibility.Collapsed;
            gaussCanvas.Visibility = Visibility.Collapsed;
            txtStats.Visibility = Visibility.Collapsed;

            txtAvgScore.Text = "—";
            txtAvgNote.Text = "Chọn lớp để xem";
            txtPassRate.Text = "—";
            txtPassDetail.Text = "";
            txtTotalStudents.Text = "0";
            txtGradeEntryCount.Text = "0";

            distributionPanel.Children.Clear();
            distributionPanel.Children.Add(new TextBlock
            {
                Text = "Chọn lớp để xem phân bố",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(134, 142, 150)),
                FontStyle = FontStyles.Italic
            });
        }

        private void LoadGradeTable(int rosterId)
        {
            try
            {
                var roster = _db.ClassRosters.FirstOrDefault(r => r.Id == rosterId);
                if (roster == null) return;

                // Get students in this roster
                var rosterStudentIds = _db.ClassRosterStudents
                    .Where(rs => rs.RosterId == rosterId)
                    .OrderBy(rs => rs.SeatNumber)
                    .Select(rs => rs.StudentId)
                    .ToList();

                var students = _db.Students
                    .Where(s => rosterStudentIds.Contains(s.Id))
                    .ToList()
                    .OrderBy(s => rosterStudentIds.IndexOf(s.Id))
                    .ToList();

                if (!students.Any())
                {
                    txtNoData.Text = "📌 Lớp này chưa có học sinh. Vào Quản lý lớp để thêm HS.";
                    txtNoData.Visibility = Visibility.Visible;
                    gradeDataGrid.Visibility = Visibility.Collapsed;
                    return;
                }

                // Get existing grades for this roster
                var allGrades = _db.StudentGrades
                    .Where(g => g.RosterId == rosterId)
                    .ToList();

                // Build DataTable
                _dataTable = new DataTable();
                _dataTable.Columns.Add("STT", typeof(int));
                _dataTable.Columns.Add("StudentId", typeof(int));
                _dataTable.Columns.Add("HoTen", typeof(string));

                _gradeColumns.Clear();
                foreach (var gt in _gradeTypes)
                {
                    for (int a = 1; a <= gt.MaxAttempts; a++)
                    {
                        string colName = gt.MaxAttempts > 1 ? $"{gt.ShortName}{a}" : gt.ShortName;
                        _dataTable.Columns.Add(colName, typeof(string));
                        _gradeColumns.Add((gt.Id, a, colName));
                    }
                }
                _dataTable.Columns.Add("TB", typeof(string));

                // Fill rows
                int idx = 1;
                foreach (var s in students)
                {
                    var row = _dataTable.NewRow();
                    row["STT"] = idx++;
                    row["StudentId"] = s.Id;
                    row["HoTen"] = s.FullName;

                    foreach (var gc in _gradeColumns)
                    {
                        var grade = allGrades.FirstOrDefault(g =>
                            g.StudentId == s.Id && g.GradeTypeId == gc.GradeTypeId && g.Attempt == gc.Attempt);
                        if (grade != null)
                        {
                            if (grade.Notes == "Vắng") row[gc.ColName] = "V";
                            else if (grade.Notes == "Miễn") row[gc.ColName] = "M";
                            else row[gc.ColName] = grade.Score.ToString("F1");
                        }
                        else
                        {
                            row[gc.ColName] = "";
                        }
                    }

                    row["TB"] = CalcAverage(s.Id, allGrades);
                    _dataTable.Rows.Add(row);
                }

                // Build DataGrid columns
                gradeDataGrid.Columns.Clear();
                gradeDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "#", Binding = new System.Windows.Data.Binding("STT"),
                    Width = 40, IsReadOnly = true
                });
                gradeDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Họ và tên", Binding = new System.Windows.Data.Binding("HoTen"),
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true
                });

                foreach (var gc in _gradeColumns)
                {
                    var gt = _gradeTypes.First(t => t.Id == gc.GradeTypeId);
                    string header = gc.ColName;
                    if (gt.Weight > 1) header += $"\n(HS{gt.Weight})";

                    var binding = new System.Windows.Data.Binding(gc.ColName)
                    {
                        UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.LostFocus,
                        ValidatesOnExceptions = true,
                        NotifyOnValidationError = true
                    };
                    binding.ValidationRules.Add(new GradeValidationRule());

                    gradeDataGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = header,
                        Binding = binding,
                        Width = 70,
                        IsReadOnly = false
                    });
                }

                gradeDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "TB môn", Binding = new System.Windows.Data.Binding("TB"),
                    Width = 80, IsReadOnly = true,
                    FontWeight = FontWeights.Bold
                });

                gradeDataGrid.ItemsSource = _dataTable.DefaultView;
                gradeDataGrid.Visibility = Visibility.Visible;
                txtNoData.Visibility = Visibility.Collapsed;

                if (_isSimulatorMode)
                {
                    btnSaveAll.Visibility = Visibility.Collapsed;
                    btnTemplate.Visibility = Visibility.Collapsed;
                    btnImport.Visibility = Visibility.Collapsed;
                    btnSimulator.Visibility = Visibility.Visible;
                    btnSimulator.Content = "🛑 Dừng giả lập";
                }
                else
                {
                    btnSaveAll.Visibility = Visibility.Visible;
                    btnTemplate.Visibility = Visibility.Visible;
                    btnImport.Visibility = Visibility.Visible;
                    btnSimulator.Visibility = Visibility.Visible;
                    btnSimulator.Content = "🔍  Giả lập điểm";
                }

                // Update subtitle
                txtSubtitle.Text = $"{roster.DisplayName} • {roster.Semester} • {roster.SchoolYear}";

                // Compute averages and entry count from DataTable
                List<double> avgs = new List<double>();
                int totalGradeRecords = 0;
                foreach (DataRow row in _dataTable.Rows)
                {
                    var avgStr = row["TB"]?.ToString() ?? "";
                    if (double.TryParse(avgStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v))
                        avgs.Add(v);

                    foreach (var colG in _gradeColumns)
                    {
                        if (!string.IsNullOrWhiteSpace(row[colG.ColName]?.ToString()))
                            totalGradeRecords++;
                    }
                }

                if (_isSimulatorMode)
                {
                    txtTableStatus.Text = $"⚠️ ĐANG Ở CHẾ ĐỘ GIẢ LẬP ĐIỂM (KHÔNG THỂ LƯU) • {students.Count} HS • {totalGradeRecords} điểm";
                }
                else
                {
                    txtTableStatus.Text = $"{students.Count} HS • {totalGradeRecords} điểm đã nhập";
                }

                // Update summary cards and distribution
                UpdateSummaryCards(students, avgs, roster, totalGradeRecords);
                UpdateDistribution(students, avgs);

                // Trigger Fade-in transition
                var fadeStoryboard = this.Resources["FadeInGridAndCanvas"] as System.Windows.Media.Animation.Storyboard;
                fadeStoryboard?.Begin(this);

                Log.Information("Loaded grade table: Roster={Id} ({Name}), {Students} students, {Grades} grades",
                    rosterId, roster.DisplayName, students.Count, allGrades.Count);
            }
            catch (Exception ex)
            {
                Log.Warning("LoadGradeTable error: {Err}", ex.Message);
            }
        }

        private string CalcAverage(int studentId, List<StudentGrade> allGrades)
        {
            double totalWeighted = 0;
            int totalWeight = 0;
            bool hasAny = false;

            bool hasGK = false;
            bool hasCK = false;
            bool gkDefined = _gradeTypes.Any(t => t.Id == 2 || t.ShortName.Equals("GK", StringComparison.OrdinalIgnoreCase));
            bool ckDefined = _gradeTypes.Any(t => t.Id == 3 || t.ShortName.Equals("CK", StringComparison.OrdinalIgnoreCase));

            foreach (var gt in _gradeTypes)
            {
                for (int a = 1; a <= gt.MaxAttempts; a++)
                {
                    var g = allGrades.FirstOrDefault(x =>
                        x.StudentId == studentId && x.GradeTypeId == gt.Id && x.Attempt == a);
                    if (g != null)
                    {
                        if (g.Notes == "Miễn")
                        {
                            if (gt.Id == 2 || gt.ShortName.Equals("GK", StringComparison.OrdinalIgnoreCase)) hasGK = true;
                            if (gt.Id == 3 || gt.ShortName.Equals("CK", StringComparison.OrdinalIgnoreCase)) hasCK = true;
                            continue;
                        }
                        totalWeighted += g.Score * gt.Weight;
                        totalWeight += gt.Weight;
                        hasAny = true;
                        if (gt.Id == 2 || gt.ShortName.Equals("GK", StringComparison.OrdinalIgnoreCase)) hasGK = true;
                        if (gt.Id == 3 || gt.ShortName.Equals("CK", StringComparison.OrdinalIgnoreCase)) hasCK = true;
                    }
                }
            }

            if (gkDefined && !hasGK && ckDefined && !hasCK) return "CDD (Thiếu GK, CK)";
            if (gkDefined && !hasGK) return "CDD (Thiếu GK)";
            if (ckDefined && !hasCK) return "CDD (Thiếu CK)";

            return hasAny && totalWeight > 0
                ? Math.Round(totalWeighted / totalWeight, 1).ToString("F1")
                : "";
        }

        // ═══════════════════════════════════════════════════
        //  SUMMARY CARDS & DISTRIBUTION
        // ═══════════════════════════════════════════════════

        private void UpdateSummaryCards(List<Student> students, List<double> avgs, ClassRoster roster, int totalGradeRecords)
        {
            txtTotalStudents.Text = students.Count.ToString();
            txtStudentNote.Text = roster.DisplayName;
            txtGradeEntryCount.Text = totalGradeRecords.ToString();
            txtGradeEntryNote.Text = "bản ghi điểm";

            if (avgs.Any())
            {
                double classAvg = Math.Round(avgs.Average(), 1);
                txtAvgScore.Text = classAvg.ToString("F1");
                txtAvgNote.Text = $"Từ {avgs.Count}/{students.Count} HS có điểm";

                int pass = avgs.Count(a => a >= 5.0);
                txtPassRate.Text = $"{pass * 100.0 / avgs.Count:F0}%";
                txtPassDetail.Text = $"{pass}/{avgs.Count} HS đạt ≥ 5.0";
            }
            else
            {
                txtAvgScore.Text = "—";
                txtAvgNote.Text = "Chưa có điểm";
                txtPassRate.Text = "—";
                txtPassDetail.Text = "";
            }
        }

        private void UpdateDistribution(List<Student> students, List<double> avgs)
        {
            if (students != _currentStudents) _currentStudents = students ?? new();
            if (avgs != _currentAvgs) _currentAvgs = avgs ?? new();

            distributionPanel.Children.Clear();
            gaussCanvas.Children.Clear();

            if (!avgs.Any())
            {
                distributionPanel.Children.Add(new TextBlock
                {
                    Text = "Chưa có dữ liệu điểm", FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(134, 142, 150)), FontStyle = FontStyles.Italic
                });
                gaussCanvas.Visibility = Visibility.Collapsed;
                txtStats.Visibility = Visibility.Collapsed;
                return;
            }

            var bands = new[]
            {
                ("Tốt (8.0-10)", avgs.Count(a => a >= 8.0), "#2E7D32", "#81C784"),
                ("Khá (6.5-7.9)", avgs.Count(a => a >= 6.5 && a < 8.0), "#1565C0", "#42A5F5"),
                ("Đạt (5.0-6.4)", avgs.Count(a => a >= 5.0 && a < 6.5), "#E65100", "#FFB74D"),
                ("Chưa đạt (<5.0)", avgs.Count(a => a < 5.0), "#C62828", "#EF9A9A"),
            };

            int maxCount = Math.Max(bands.Max(b => b.Item2), 1);

            foreach (var (label, count, c1, c2) in bands)
            {
                var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });

                var lbl = new TextBlock { Text = label, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)), VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(lbl, 0);

                var barContainer = new Grid();
                double activeShare = Math.Max(0.001, count);
                double emptyShare = Math.Max(0.001, maxCount - count);
                barContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(activeShare, GridUnitType.Star) });
                barContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(emptyShare, GridUnitType.Star) });

                var bar = new Border
                {
                    Height = 20, CornerRadius = new CornerRadius(4),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Background = new LinearGradientBrush(
                        (Color)ColorConverter.ConvertFromString(c1),
                        (Color)ColorConverter.ConvertFromString(c2), 0)
                };
                Grid.SetColumn(bar, 0);
                barContainer.Children.Add(bar);

                Grid.SetColumn(barContainer, 1);

                var cnt = new TextBlock
                {
                    Text = $"{count} HS", FontSize = 12, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(c1)),
                    VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0)
                };
                Grid.SetColumn(cnt, 2);

                grid.Children.Add(lbl);
                grid.Children.Add(barContainer);
                grid.Children.Add(cnt);
                distributionPanel.Children.Add(grid);
            }

            // ═══════════════════════════════════════════════════
            //  DRAW GAUSS BELL CURVE
            // ═══════════════════════════════════════════════════
            try
            {
                double mean = avgs.Average();
                double variance = avgs.Average(v => Math.Pow(v - mean, 2));
                double stdDev = Math.Sqrt(variance);

                txtStats.Text = $"Độ lệch chuẩn (σ): {stdDev:F2}  |  Điểm trung bình (μ): {mean:F2}";
                txtStats.Visibility = Visibility.Visible;
                gaussCanvas.Visibility = Visibility.Visible;

                double width = gaussCanvas.ActualWidth > 0 ? gaussCanvas.ActualWidth : 330;
                double height = gaussCanvas.ActualHeight > 0 ? gaussCanvas.ActualHeight : 180;

                // If Standard Deviation is 0 (all grades are identical)
                if (stdDev < 0.01)
                {
                    double xPos = mean * (width / 10.0);
                    var line = new System.Windows.Shapes.Line
                    {
                        X1 = xPos, Y1 = 5,
                        X2 = xPos, Y2 = height - 5,
                        Stroke = new SolidColorBrush(Color.FromRgb(229, 57, 53)),
                        StrokeThickness = 2.5,
                        StrokeDashArray = new DoubleCollection { 4, 3 }
                    };
                    gaussCanvas.Children.Add(line);
                    return;
                }

                // Probability Density Function: f(x) = (1 / (stdDev * sqrt(2*pi))) * e^(-0.5 * ((x-mean)/stdDev)^2)
                double factor = 1.0 / (stdDev * Math.Sqrt(2 * Math.PI));
                double maxPdf = factor; // peak is at x = mean

                var points = new PointCollection();
                for (double x = 0; x <= 10; x += 0.1)
                {
                    double exponent = -0.5 * Math.Pow((x - mean) / stdDev, 2);
                    double pdfValue = factor * Math.Exp(exponent);

                    double px = x * (width / 10.0);
                    // Scale peak to 85% of height
                    double py = height - (pdfValue / maxPdf) * (height * 0.85);
                    points.Add(new Point(px, py));
                }

                // Create Curve Path
                var curveGeometry = new PathGeometry();
                var figure = new PathFigure { StartPoint = points[0], IsClosed = false };
                var segments = new PathSegmentCollection();
                for (int i = 1; i < points.Count; i++)
                {
                    segments.Add(new LineSegment(points[i], true));
                }
                figure.Segments = segments;
                curveGeometry.Figures.Add(figure);

                var curvePath = new System.Windows.Shapes.Path
                {
                    Stroke = new SolidColorBrush(Color.FromRgb(0, 150, 136)), // Teal
                    StrokeThickness = 2.5,
                    Data = curveGeometry
                };

                // Create Shaded Area under curve
                var areaGeometry = new PathGeometry();
                var areaFigure = new PathFigure { StartPoint = new Point(0, height), IsClosed = true };
                var areaSegments = new PathSegmentCollection();
                foreach (var pt in points)
                {
                    areaSegments.Add(new LineSegment(pt, true));
                }
                areaSegments.Add(new LineSegment(new Point(width, height), true));
                areaFigure.Segments = areaSegments;
                areaGeometry.Figures.Add(areaFigure);

                var fillBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1)
                };
                fillBrush.GradientStops.Add(new GradientStop(Color.FromArgb(50, 0, 150, 136), 0));
                fillBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 150, 136), 1));

                var areaPath = new System.Windows.Shapes.Path
                {
                    Fill = fillBrush,
                    Data = areaGeometry
                };

                // Add to Canvas
                gaussCanvas.Children.Add(areaPath);
                gaussCanvas.Children.Add(curvePath);
            }
            catch (Exception ex)
            {
                Log.Warning("UpdateDistribution Gauss drawing error: {Err}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════
        //  CELL EDIT → SAVE GRADE
        // ═══════════════════════════════════════════════════

        private void GradeCell_EditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (_selectedRosterId <= 0) return;

            try
            {
                var col = e.Column as DataGridTextColumn;
                if (col == null) return;

                string colName = (col.Binding as System.Windows.Data.Binding)?.Path?.Path ?? "";
                var gc = _gradeColumns.FirstOrDefault(c => c.ColName == colName);
                if (gc.ColName == null) return; // not a grade column

                var rowView = e.Row.Item as DataRowView;
                if (rowView == null) return;
                int studentId = (int)rowView["StudentId"];

                var textBox = e.EditingElement as TextBox;
                if (textBox == null) return;

                // If cell has validation error, abort DB save, and cancel edit to restore original value
                if (Validation.GetHasError(textBox))
                {
                    e.Cancel = true;
                    textBox.Text = rowView[gc.ColName]?.ToString() ?? "";
                    return;
                }

                string input = textBox.Text.Trim();
                string norm = input.ToUpper();

                if (string.IsNullOrEmpty(input))
                {
                    if (!_isSimulatorMode)
                    {
                        // Delete grade
                        var existing = _db.StudentGrades.FirstOrDefault(g =>
                            g.StudentId == studentId && g.RosterId == _selectedRosterId &&
                            g.GradeTypeId == gc.GradeTypeId && g.Attempt == gc.Attempt);
                        if (existing != null)
                        {
                            _db.StudentGrades.Remove(existing);
                            _db.SaveChanges();
                        }
                    }
                    else
                    {
                        rowView[gc.ColName] = "";
                    }
                }
                else if (norm == "V" || norm == "M")
                {
                    double score = 0.0;
                    string notes = norm == "V" ? "Vắng" : "Miễn";
                    textBox.Text = norm;

                    if (!_isSimulatorMode)
                    {
                        var existing = _db.StudentGrades.FirstOrDefault(g =>
                            g.StudentId == studentId && g.RosterId == _selectedRosterId &&
                            g.GradeTypeId == gc.GradeTypeId && g.Attempt == gc.Attempt);

                        if (existing != null)
                        {
                            existing.Score = score;
                            existing.Notes = notes;
                            existing.UpdatedAt = DateTime.Now;
                        }
                        else
                        {
                            _db.StudentGrades.Add(new StudentGrade
                            {
                                StudentId = studentId,
                                RosterId = _selectedRosterId,
                                GradeTypeId = gc.GradeTypeId,
                                Attempt = gc.Attempt,
                                Score = score,
                                Notes = notes,
                                EnteredBy = "GV",
                                UpdatedAt = DateTime.Now
                            });
                        }
                        _db.SaveChanges();
                    }
                    else
                    {
                        rowView[gc.ColName] = norm;
                    }
                }
                else
                {
                    input = input.Replace(',', '.');
                    if (double.TryParse(input, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double score))
                    {
                        score = Math.Max(0, Math.Min(10, Math.Round(score, 1)));
                        textBox.Text = score.ToString("F1");

                        if (!_isSimulatorMode)
                        {
                            var existing = _db.StudentGrades.FirstOrDefault(g =>
                                g.StudentId == studentId && g.RosterId == _selectedRosterId &&
                                g.GradeTypeId == gc.GradeTypeId && g.Attempt == gc.Attempt);

                            if (existing != null)
                            {
                                existing.Score = score;
                                existing.Notes = ""; // Reset notes
                                existing.UpdatedAt = DateTime.Now;
                            }
                            else
                            {
                                _db.StudentGrades.Add(new StudentGrade
                                {
                                    StudentId = studentId,
                                    RosterId = _selectedRosterId,
                                    GradeTypeId = gc.GradeTypeId,
                                    Attempt = gc.Attempt,
                                    Score = score,
                                    Notes = "",
                                    EnteredBy = "GV",
                                    UpdatedAt = DateTime.Now
                                });
                            }
                            _db.SaveChanges();
                        }
                        else
                        {
                            rowView[gc.ColName] = score.ToString("F1");
                        }
                    }
                    else
                    {
                        e.Cancel = true;
                        textBox.Text = rowView[gc.ColName]?.ToString() ?? "";
                        return;
                    }
                }

                // Recalc average for this student
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    List<double> avgs = new List<double>();
                    int totalGradeRecords = 0;

                    // Update row average
                    rowView["TB"] = CalcAverageFromRow(rowView.Row);

                    // Compute all averages and count entries from DataTable
                    foreach (DataRow row in _dataTable.Rows)
                    {
                        var avgStr = row["TB"]?.ToString() ?? "";
                        if (double.TryParse(avgStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v))
                            avgs.Add(v);

                        foreach (var colG in _gradeColumns)
                        {
                            if (!string.IsNullOrWhiteSpace(row[colG.ColName]?.ToString()))
                                totalGradeRecords++;
                        }
                    }

                    var rosterStudentIds = _db.ClassRosterStudents
                        .Where(rs => rs.RosterId == _selectedRosterId)
                        .Select(rs => rs.StudentId).ToList();
                    var students = _db.Students.Where(s => rosterStudentIds.Contains(s.Id)).ToList();
                    var roster = _db.ClassRosters.FirstOrDefault(r => r.Id == _selectedRosterId);
                    if (roster != null)
                    {
                        UpdateSummaryCards(students, avgs, roster, totalGradeRecords);
                        UpdateDistribution(students, avgs);

                        if (_isSimulatorMode)
                        {
                            txtTableStatus.Text = $"⚠️ ĐANG Ở CHẾ ĐỘ GIẢ LẬP ĐIỂM (KHÔNG THỂ LƯU) • {students.Count} HS • {totalGradeRecords} điểm";
                        }
                        else
                        {
                            txtTableStatus.Text = $"{students.Count} HS • {totalGradeRecords} điểm đã nhập";
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                Log.Warning("GradeCell edit error: {Err}", ex.Message);
            }
        }

        private void SaveAllGrades_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRosterId <= 0) return;
            if (_isSimulatorMode)
            {
                ClassroomDialog.Warn("Không thể lưu khi đang ở chế độ giả lập điểm!", "Cảnh báo");
                return;
            }

            try
            {
                // Commit any pending edit
                gradeDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

                // Re-save all from DataTable
                int saved = 0;
                foreach (DataRow row in _dataTable.Rows)
                {
                    int studentId = (int)row["StudentId"];
                    foreach (var gc in _gradeColumns)
                    {
                        string val = row[gc.ColName]?.ToString()?.Trim() ?? "";
                        if (!double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double score)) continue;
                        score = Math.Max(0, Math.Min(10, Math.Round(score, 1)));

                        var existing = _db.StudentGrades.FirstOrDefault(g =>
                            g.StudentId == studentId && g.RosterId == _selectedRosterId &&
                            g.GradeTypeId == gc.GradeTypeId && g.Attempt == gc.Attempt);

                        if (existing != null)
                        {
                            existing.Score = score;
                            existing.UpdatedAt = DateTime.Now;
                        }
                        else
                        {
                            _db.StudentGrades.Add(new StudentGrade
                            {
                                StudentId = studentId,
                                RosterId = _selectedRosterId,
                                GradeTypeId = gc.GradeTypeId,
                                Attempt = gc.Attempt,
                                Score = score,
                                EnteredBy = "GV",
                                UpdatedAt = DateTime.Now
                            });
                        }
                        saved++;
                    }
                }

                _db.SaveChanges();
                LoadGradeTable(_selectedRosterId); // Refresh

                ClassroomDialog.Info($"✅ Đã lưu {saved} ô điểm!", "Lưu thành công");
                Log.Information("SaveAllGrades: {Count} cells for roster {Id}", saved, _selectedRosterId);
            }
            catch (Exception ex)
            {
                ClassroomDialog.Error($"Lỗi: {ex.Message}", "Lỗi");
            }
        }

        private void Page_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control && e.Key == System.Windows.Input.Key.G)
            {
                e.Handled = true;
                Simulator_Click(this, new RoutedEventArgs());
            }
        }

        private void Simulator_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRosterId <= 0) return;

            _isSimulatorMode = !_isSimulatorMode;

            if (_isSimulatorMode)
            {
                // Enter Simulator Mode
                btnSimulator.Content = "🛑 Dừng giả lập";
                btnSimulator.Background = new SolidColorBrush(Color.FromRgb(255, 235, 235));
                btnSimulator.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                btnSimulator.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 205, 210));

                btnSaveAll.Visibility = Visibility.Collapsed;
                btnTemplate.Visibility = Visibility.Collapsed;
                btnImport.Visibility = Visibility.Collapsed;

                // Color background and rows to Teal
                gradeDataGrid.Background = new SolidColorBrush(Color.FromRgb(224, 242, 241));
                gradeDataGrid.RowBackground = new SolidColorBrush(Color.FromRgb(224, 242, 241));
                gradeDataGrid.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(204, 232, 231));

                var rosterStudentIds = _db.ClassRosterStudents
                    .Where(rs => rs.RosterId == _selectedRosterId)
                    .Select(rs => rs.StudentId).ToList();
                txtTableStatus.Text = $"⚠️ ĐANG Ở CHẾ ĐỘ GIẢ LẬP ĐIỂM (KHÔNG THỂ LƯU) • {rosterStudentIds.Count} HS";
            }
            else
            {
                // Exit Simulator Mode
                btnSimulator.Content = "🔍  Giả lập điểm";
                btnSimulator.Background = new SolidColorBrush(Color.FromRgb(224, 242, 241));
                btnSimulator.Foreground = new SolidColorBrush(Color.FromRgb(0, 77, 64));
                btnSimulator.BorderBrush = new SolidColorBrush(Color.FromRgb(178, 223, 219));

                btnSaveAll.Visibility = Visibility.Visible;
                btnTemplate.Visibility = Visibility.Visible;
                btnImport.Visibility = Visibility.Visible;

                // Restore backgrounds
                gradeDataGrid.Background = new SolidColorBrush(System.Windows.Media.Colors.White);
                gradeDataGrid.RowBackground = new SolidColorBrush(System.Windows.Media.Colors.White);
                gradeDataGrid.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(250, 250, 250));

                // Reload original values
                LoadGradeTable(_selectedRosterId);
            }
        }

        private string CalcAverageFromRow(DataRow row)
        {
            double totalWeighted = 0;
            int totalWeight = 0;
            bool hasAny = false;

            bool hasGK = false;
            bool hasCK = false;
            bool gkDefined = _gradeTypes.Any(t => t.Id == 2 || t.ShortName.Equals("GK", StringComparison.OrdinalIgnoreCase));
            bool ckDefined = _gradeTypes.Any(t => t.Id == 3 || t.ShortName.Equals("CK", StringComparison.OrdinalIgnoreCase));

            foreach (var gc in _gradeColumns)
            {
                var gt = _gradeTypes.First(t => t.Id == gc.GradeTypeId);
                string val = row[gc.ColName]?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(val)) continue;

                string norm = val.ToUpper();
                if (norm == "M")
                {
                    if (gt.Id == 2 || gt.ShortName.Equals("GK", StringComparison.OrdinalIgnoreCase)) hasGK = true;
                    if (gt.Id == 3 || gt.ShortName.Equals("CK", StringComparison.OrdinalIgnoreCase)) hasCK = true;
                    continue;
                }

                double score = 0.0;
                if (norm == "V")
                {
                    score = 0.0;
                    if (gt.Id == 2 || gt.ShortName.Equals("GK", StringComparison.OrdinalIgnoreCase)) hasGK = true;
                    if (gt.Id == 3 || gt.ShortName.Equals("CK", StringComparison.OrdinalIgnoreCase)) hasCK = true;
                }
                else if (double.TryParse(val.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sc))
                {
                    score = sc;
                    if (gt.Id == 2 || gt.ShortName.Equals("GK", StringComparison.OrdinalIgnoreCase)) hasGK = true;
                    if (gt.Id == 3 || gt.ShortName.Equals("CK", StringComparison.OrdinalIgnoreCase)) hasCK = true;
                }
                else
                {
                    continue;
                }

                totalWeighted += score * gt.Weight;
                totalWeight += gt.Weight;
                hasAny = true;
            }

            if (gkDefined && !hasGK && ckDefined && !hasCK) return "CDD (Thiếu GK, CK)";
            if (gkDefined && !hasGK) return "CDD (Thiếu GK)";
            if (ckDefined && !hasCK) return "CDD (Thiếu CK)";

            return hasAny && totalWeight > 0
                ? Math.Round(totalWeighted / totalWeight, 1).ToString("F1")
                : "";
        }

        // ═══════════════════════════════════════════════════
        //  EXPORT
        // ═══════════════════════════════════════════════════

        private void ExportPdf_Click(object sender, RoutedEventArgs e) => ExportGradebook("pdf");
        private void ExportExcel_Click(object sender, RoutedEventArgs e) => ExportGradebook("csv");

        private void ExportGradebook(string format)
        {
            try
            {
                if (_selectedRosterId <= 0 || _dataTable.Rows.Count == 0)
                {
                    ClassroomDialog.Warn("Vui lòng chọn lớp và nhập điểm trước khi xuất.", "Chưa có dữ liệu");
                    return;
                }

                var roster = _db.ClassRosters.FirstOrDefault(r => r.Id == _selectedRosterId);
                string rosterName = roster?.DisplayName ?? "Lớp";

                if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
                {
                    ExportGradebookToPdf(rosterName);
                }
                else
                {
                    ExportGradebookToCsv(rosterName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Export gradebook error");
                ClassroomDialog.Error($"Lỗi xuất: {ex.Message}", "Lỗi");
            }
        }

        private void ExportGradebookToPdf(string rosterName)
        {
            string folder = QASmartClass.Services.PdfTemplateHelper.GetOutputDirectory();
            string path = QASmartClass.Services.PdfTemplateHelper.GetOutputPath($"BangDiem_{rosterName}");

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4.Height, QuestPDF.Helpers.PageSizes.A4.Width);
                    page.Margin(1.5f, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(9));

                    page.Header().Element(c => QASmartClass.Services.PdfTemplateHelper.ComposeHeader(c, 
                        $"BẢNG ĐIỂM CHI TIẾT - LỚP {rosterName.ToUpper()}", 
                        $"Học kỳ: {cboSemester.Text} • Năm học: {txtSubtitle.Text}"));

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30); // STT
                                columns.RelativeColumn(3);  // Họ và tên
                                foreach (var gc in _gradeColumns)
                                {
                                    columns.RelativeColumn(); // Cột điểm
                                }
                                columns.RelativeColumn();   // TB môn
                                columns.RelativeColumn();   // Xếp loại
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(QASmartClass.Services.PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("STT");
                                header.Cell().Element(QASmartClass.Services.PdfTemplateHelper.TableHeaderStyle).Text("Họ và tên");
                                foreach (var gc in _gradeColumns)
                                {
                                    header.Cell().Element(QASmartClass.Services.PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text(gc.ColName);
                                }
                                header.Cell().Element(QASmartClass.Services.PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("TB");
                                header.Cell().Element(QASmartClass.Services.PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Xếp loại");
                            });

                            int idx = 1;
                            foreach (DataRow row in _dataTable.Rows)
                            {
                                bool alt = idx % 2 == 0;
                                table.Cell().Element(c => QASmartClass.Services.PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(row["STT"].ToString());
                                table.Cell().Element(c => QASmartClass.Services.PdfTemplateHelper.TableCellStyle(c, alt)).Text(row["HoTen"].ToString());
                                
                                foreach (var gc in _gradeColumns)
                                {
                                    string val = row[gc.ColName]?.ToString() ?? "";
                                    table.Cell().Element(c => QASmartClass.Services.PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(val);
                                }

                                string avg = row["TB"]?.ToString() ?? "";
                                table.Cell().Element(c => QASmartClass.Services.PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(avg);

                                string loai = "";
                                if (double.TryParse(avg, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v))
                                    loai = TT22GradingService.ClassifyAcademic(v);
                                
                                table.Cell().Element(c => QASmartClass.Services.PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(loai);
                                idx++;
                            }
                        });

                        col.Item().Element(c => QASmartClass.Services.PdfTemplateHelper.ComposeSignatureBlock(c, "Giáo viên chủ nhiệm", "Hiệu trưởng"));
                    });

                    page.Footer().Element(QASmartClass.Services.PdfTemplateHelper.ComposeFooter);
                });
            }).GeneratePdf(path);

            MessageBox.Show(
                $"✅ Đã xuất bảng điểm PDF!\n\nLớp: {rosterName}\nFile: {System.IO.Path.GetFileName(path)}\nThư mục: {folder}",
                "Xuất Bảng Điểm", MessageBoxButton.OK, MessageBoxImage.Information);

            System.Diagnostics.Process.Start("explorer.exe", folder);
        }

        private void ExportGradebookToCsv(string rosterName)
        {
            string folder = QASmartClass.Services.AppPaths.DocumentsDir;
            System.IO.Directory.CreateDirectory(folder);

            string path = System.IO.Path.Combine(folder,
                $"BangDiem_{rosterName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            var sb = new System.Text.StringBuilder();

            // Header row
            var headers = new List<string> { "STT", "Họ và tên" };
            foreach (var gc in _gradeColumns) headers.Add(gc.ColName);
            headers.Add("TB môn");
            headers.Add("Xếp loại");
            sb.AppendLine(string.Join(",", headers));

            // Data rows
            foreach (DataRow row in _dataTable.Rows)
            {
                var cells = new List<string>
                {
                    row["STT"].ToString() ?? "",
                    $"\"{row["HoTen"]}\""
                };
                foreach (var gc in _gradeColumns)
                    cells.Add(row[gc.ColName]?.ToString() ?? "");

                string avg = row["TB"]?.ToString() ?? "";
                cells.Add(avg);

                string loai = "";
                if (double.TryParse(avg, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v))
                    loai = TT22GradingService.ClassifyAcademic(v);
                cells.Add(loai);

                sb.AppendLine(string.Join(",", cells));
            }

            System.IO.File.WriteAllText(path, sb.ToString(), new System.Text.UTF8Encoding(true));

            MessageBox.Show(
                $"✅ Đã xuất bảng điểm Excel CSV!\n\nLớp: {rosterName}\nFile: {System.IO.Path.GetFileName(path)}\nThư mục: {folder}",
                "Xuất Bảng Điểm", MessageBoxButton.OK, MessageBoxImage.Information);

            System.Diagnostics.Process.Start("explorer.exe", folder);
        }

        // ═══════════════════════════════════════════════════════
        //  TEMPLATE DOWNLOAD + CSV IMPORT
        // ═══════════════════════════════════════════════════════

        private void DownloadTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRosterId <= 0) return;
            try
            {
                var roster = _db.ClassRosters.FirstOrDefault(r => r.Id == _selectedRosterId);
                string rosterName = roster?.DisplayName ?? "Lop";

                var rosterStudentIds = _db.ClassRosterStudents
                    .Where(rs => rs.RosterId == _selectedRosterId)
                    .OrderBy(rs => rs.SeatNumber)
                    .Select(rs => rs.StudentId).ToList();

                var students = _db.Students
                    .Where(s => rosterStudentIds.Contains(s.Id))
                    .ToList()
                    .OrderBy(s => rosterStudentIds.IndexOf(s.Id))
                    .ToList();

                string folder = QASmartClass.Services.AppPaths.DocumentsDir;
                System.IO.Directory.CreateDirectory(folder);

                string path = System.IO.Path.Combine(folder, $"FormMau_{rosterName}_{DateTime.Now:yyyyMMdd}.xlsx");

                OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization("QA SmartClass");
                using (var package = new OfficeOpenXml.ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("MauNhapDiem");

                    // Header row
                    var headers = new List<string> { "STT", "Họ và tên", "Mã HS" };
                    foreach (var gc in _gradeColumns) headers.Add(gc.ColName);

                    for (int i = 0; i < headers.Count; i++)
                    {
                        ws.Cells[1, i + 1].Value = headers[i];
                        ws.Cells[1, i + 1].Style.Font.Bold = true;
                        ws.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                        ws.Cells[1, i + 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                    }

                    // Student rows
                    int idx = 1;
                    for (int row = 0; row < students.Count; row++)
                    {
                        var student = students[row];
                        ws.Cells[row + 2, 1].Value = idx++;
                        ws.Cells[row + 2, 2].Value = student.FullName;
                        ws.Cells[row + 2, 3].Value = student.StudentCode;

                        for (int col = 0; col < headers.Count; col++)
                        {
                            ws.Cells[row + 2, col + 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                        }
                    }

                    // Instructions at the bottom
                    int instructionRow = students.Count + 4;
                    ws.Cells[instructionRow, 1].Value = "Hướng dẫn:";
                    ws.Cells[instructionRow, 1].Style.Font.Bold = true;
                    ws.Cells[instructionRow + 1, 1].Value = "- Điền điểm vào các cột trống (0.0 - 10.0), hoặc V (Vắng), M (Miễn).";
                    ws.Cells[instructionRow + 2, 1].Value = "- Không sửa cột STT, Họ và tên và Mã HS.";
                    ws.Cells[instructionRow + 3, 1].Value = "- Lưu file định dạng Excel hoặc xuất sang CSV để import vào hệ thống.";

                    // Auto-fit columns
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();

                    var fileInfo = new System.IO.FileInfo(path);
                    package.SaveAs(fileInfo);
                }

                MessageBox.Show(
                    $"✅ Đã tạo form mẫu Excel!\n\nLớp: {rosterName}\nFile: {System.IO.Path.GetFileName(path)}\nThư mục: {folder}\n\n"
                    + $"• {students.Count} học sinh\n• {_gradeColumns.Count} cột điểm\n• Mở bằng Excel, điền điểm rồi import lại",
                    "Tải Form Mẫu Excel", MessageBoxButton.OK, MessageBoxImage.Information);

                System.Diagnostics.Process.Start("explorer.exe", folder);
                Log.Information("Template exported: {Path}", path);
            }
            catch (Exception ex)
            {
                ClassroomDialog.Error($"Lỗi: {ex.Message}", "Lỗi");
            }
        }

        private void ImportGrades_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRosterId <= 0) return;
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "CSV Files|*.csv",
                    Title = "Chọn file CSV điểm"
                };
                if (dlg.ShowDialog() != true) return;

                var lines = System.IO.File.ReadAllLines(dlg.FileName, System.Text.Encoding.UTF8)
                    .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#"))
                    .ToList();

                if (lines.Count < 2)
                {
                    ClassroomDialog.Warn("File không có dữ liệu.", "Lỗi");
                    return;
                }

                char delimiter = DetectDelimiter(lines[0]);
                var header = ParseCsvLine(lines[0], delimiter);
                int codeIdx = header.IndexOf("Mã HS");
                if (codeIdx < 0)
                {
                    ClassroomDialog.Error("File thiếu cột 'Mã HS'. Vui lòng dùng form mẫu.", "Lỗi");
                    return;
                }

                // Map grade columns
                var colMap = new List<(int ColIdx, int GradeTypeId, int Attempt)>();
                foreach (var gc in _gradeColumns)
                {
                    int ci = header.IndexOf(gc.ColName);
                    if (ci >= 0) colMap.Add((ci, gc.GradeTypeId, gc.Attempt));
                }

                int imported = 0;
                for (int i = 1; i < lines.Count; i++)
                {
                    var cells = ParseCsvLine(lines[i], delimiter);
                    if (cells.Count <= codeIdx) continue;

                    string studentCode = cells[codeIdx].Trim();
                    var student = _db.Students.FirstOrDefault(s => s.StudentCode == studentCode);
                    if (student == null) continue;

                    foreach (var (colIdx, gtId, attempt) in colMap)
                    {
                        if (colIdx >= cells.Count) continue;
                        string val = cells[colIdx].Trim();
                        if (string.IsNullOrEmpty(val)) continue;

                        val = val.Replace(',', '.');

                        if (!double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double score)) continue;
                        score = Math.Max(0, Math.Min(10, Math.Round(score, 1)));

                        var existing = _db.StudentGrades.FirstOrDefault(g =>
                            g.StudentId == student.Id && g.RosterId == _selectedRosterId &&
                            g.GradeTypeId == gtId && g.Attempt == attempt);

                        if (existing != null)
                        {
                            existing.Score = score;
                            existing.UpdatedAt = DateTime.Now;
                        }
                        else
                        {
                            _db.StudentGrades.Add(new StudentGrade
                            {
                                StudentId = student.Id,
                                RosterId = _selectedRosterId,
                                GradeTypeId = gtId,
                                Attempt = attempt,
                                Score = score,
                                EnteredBy = "CSV",
                                UpdatedAt = DateTime.Now
                            });
                        }
                        imported++;
                    }
                }

                _db.SaveChanges();
                LoadGradeTable(_selectedRosterId); // Refresh grid

                ClassroomDialog.Info($"✅ Import thành công!\n\n• {imported} ô điểm đã nhập\n• Bảng điểm đã cập nhật", "Import Điểm");
                Log.Information("Imported {Count} grades from CSV: {File}", imported, dlg.FileName);
            }
            catch (Exception ex)
            {
                ClassroomDialog.Error($"Lỗi import: {ex.Message}", "Lỗi");
            }
        }

        private static char DetectDelimiter(string line)
        {
            int commaCount = line.Count(c => c == ',');
            int semiCount = line.Count(c => c == ';');
            return semiCount > commaCount ? ';' : ',';
        }

        private static List<string> ParseCsvLine(string line, char delimiter)
        {
            var result = new List<string>();
            bool inQuote = false;
            var current = new System.Text.StringBuilder();
            foreach (char c in line)
            {
                if (c == '"') { inQuote = !inQuote; continue; }
                if (c == delimiter && !inQuote) { result.Add(current.ToString()); current.Clear(); continue; }
                current.Append(c);
            }
            result.Add(current.ToString());
            return result;
        }

        private void GaussCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_currentStudents != null && _currentStudents.Any() && _currentAvgs != null && _currentAvgs.Any())
            {
                UpdateDistribution(_currentStudents, _currentAvgs);
            }
        }
    }

    public class GradeValidationRule : System.Windows.Controls.ValidationRule
    {
        public override System.Windows.Controls.ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value == null) return System.Windows.Controls.ValidationResult.ValidResult;
            string input = value.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(input)) return System.Windows.Controls.ValidationResult.ValidResult;

            string norm = input.Trim().ToUpper();
            if (norm == "V" || norm == "M")
            {
                return System.Windows.Controls.ValidationResult.ValidResult;
            }

            input = input.Replace(',', '.');

            if (!double.TryParse(input, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double score))
            {
                return new System.Windows.Controls.ValidationResult(false, "Vui lòng nhập số từ 0.0 đến 10.0, hoặc V (Vắng), M (Miễn)");
            }

            if (score < 0 || score > 10)
            {
                return new System.Windows.Controls.ValidationResult(false, "Điểm số phải nằm trong khoảng từ 0.0 đến 10.0");
            }

            return System.Windows.Controls.ValidationResult.ValidResult;
        }
    }
}
