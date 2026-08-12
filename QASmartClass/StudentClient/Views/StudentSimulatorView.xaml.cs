using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;

namespace QASmartClass.StudentClient.Views
{
    public partial class StudentSimulatorView : Page
    {
        private AppDbContext _db = null!;
        private int _studentId;
        private string _studentClassName = "";
        private string _studentCode = "";
        private bool _isSimulatorMode = false;
        private DataTable _dataTable = new();
        private readonly List<string> _gradeCols = new() { "TX1", "TX2", "TX3", "GK", "CK" };

        public StudentSimulatorView()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            InitPage();
        }

        private void InitPage()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                _db = app.Database;

                var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(_db);
                var (currentId, studentCode, className) = identityService.GetCurrentStudent();
                _studentId = currentId;
                _studentCode = studentCode;
                _studentClassName = className;

                LoadGrades();
            }
            catch (Exception ex)
            {
                Log.Warning("StudentSimulatorView init error: {Err}", ex.Message);
            }
        }

        private void LoadGrades()
        {
            if (_db == null || _studentId <= 0) return;

            try
            {
                string semester = (cboSemester.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "HK2";

                // Get all class rosters for this student
                var rosterLinks = _db.ClassRosterStudents
                    .Where(rs => rs.StudentId == _studentId)
                    .ToList();
                var rosterIds = rosterLinks.Select(rs => rs.RosterId).ToList();

                var rosters = _db.ClassRosters
                    .Where(r => rosterIds.Contains(r.Id) && r.Semester == semester && r.IsActive)
                    .ToList();

                if (!rosters.Any())
                {
                    // Fallback to show all active student classes if no match for selected semester
                    rosters = _db.ClassRosters
                        .Where(r => rosterIds.Contains(r.Id) && r.IsActive)
                        .ToList();
                }

                if (!rosters.Any())
                {
                    txtNoData.Visibility = Visibility.Visible;
                    dgSimulator.Visibility = Visibility.Collapsed;
                    ClearSummary();
                    return;
                }

                txtNoData.Visibility = Visibility.Collapsed;
                dgSimulator.Visibility = Visibility.Visible;

                var currentRosterIds = rosters.Select(r => r.Id).ToList();
                var grades = _db.StudentGrades
                    .Where(g => g.StudentId == _studentId && currentRosterIds.Contains(g.RosterId))
                    .ToList();

                // Setup DataTable
                _dataTable = new DataTable();
                _dataTable.Columns.Add("RosterId", typeof(int));
                _dataTable.Columns.Add("Subject", typeof(string));
                _dataTable.Columns.Add("TX1", typeof(string));
                _dataTable.Columns.Add("TX2", typeof(string));
                _dataTable.Columns.Add("TX3", typeof(string));
                _dataTable.Columns.Add("GK", typeof(string));
                _dataTable.Columns.Add("CK", typeof(string));
                _dataTable.Columns.Add("TB", typeof(string));
                _dataTable.Columns.Add("XepLoai", typeof(string));

                foreach (var roster in rosters)
                {
                    var row = _dataTable.NewRow();
                    row["RosterId"] = roster.Id;
                    row["Subject"] = roster.Subject;

                    var rosterGrades = grades.Where(g => g.RosterId == roster.Id).ToList();

                    // Map TX
                    var txGrades = rosterGrades.Where(g => g.GradeTypeId == 1).OrderBy(g => g.Attempt).ToList();
                    row["TX1"] = GetGradeString(txGrades.ElementAtOrDefault(0));
                    row["TX2"] = GetGradeString(txGrades.ElementAtOrDefault(1));
                    row["TX3"] = GetGradeString(txGrades.ElementAtOrDefault(2));

                    // Map GK
                    var gkGrade = rosterGrades.FirstOrDefault(g => g.GradeTypeId == 2);
                    row["GK"] = GetGradeString(gkGrade);

                    // Map CK
                    var ckGrade = rosterGrades.FirstOrDefault(g => g.GradeTypeId == 3);
                    row["CK"] = GetGradeString(ckGrade);

                    // Calc average
                    row["TB"] = CalcAverageFromRow(row);
                    row["XepLoai"] = CalcClassificationFromRow(row);

                    _dataTable.Rows.Add(row);
                }

                BuildDataGridColumns();
                dgSimulator.ItemsSource = _dataTable.DefaultView;

                UpdateSummary();
            }
            catch (Exception ex)
            {
                Log.Warning("StudentSimulatorView LoadGrades error: {Err}", ex.Message);
            }
        }

        private string GetGradeString(StudentGrade? g)
        {
            if (g == null) return "";
            if (g.Notes == "Vắng") return "V";
            if (g.Notes == "Miễn") return "M";
            return g.Score.ToString("F1");
        }

        private void BuildDataGridColumns()
        {
            dgSimulator.Columns.Clear();
            
            // Môn học (Read-only)
            dgSimulator.Columns.Add(new DataGridTextColumn
            {
                Header = "Môn Học",
                Binding = new System.Windows.Data.Binding("Subject"),
                Width = new DataGridLength(1.5, DataGridLengthUnitType.Star),
                IsReadOnly = true,
                FontWeight = FontWeights.SemiBold
            });

            // TX1, TX2, TX3, GK, CK
            foreach (var col in _gradeCols)
            {
                var binding = new System.Windows.Data.Binding(col)
                {
                    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.LostFocus
                };

                dgSimulator.Columns.Add(new DataGridTextColumn
                {
                    Header = col,
                    Binding = binding,
                    Width = 80,
                    IsReadOnly = !_isSimulatorMode
                });
            }

            // TB môn (Read-only)
            dgSimulator.Columns.Add(new DataGridTextColumn
            {
                Header = "TB Môn",
                Binding = new System.Windows.Data.Binding("TB"),
                Width = 100,
                IsReadOnly = true,
                FontWeight = FontWeights.Bold
            });

            // Xếp loại môn (Read-only)
            dgSimulator.Columns.Add(new DataGridTextColumn
            {
                Header = "Xếp Loại",
                Binding = new System.Windows.Data.Binding("XepLoai"),
                Width = 120,
                IsReadOnly = true,
                FontWeight = FontWeights.SemiBold
            });
        }

        private void Semester_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isSimulatorMode)
            {
                ExitSimulator();
            }
            LoadGrades();
        }

        private void StartSim_Click(object sender, RoutedEventArgs e)
        {
            if (_dataTable.Rows.Count == 0) return;

            _isSimulatorMode = true;

            // Update UI state
            btnStartSim.Visibility = Visibility.Collapsed;
            btnStopSim.Visibility = Visibility.Visible;
            cboSemester.IsEnabled = false;

            statusBanner.Background = new SolidColorBrush(Color.FromRgb(255, 235, 235));
            txtStatus.Text = "⚠️ ĐANG GIẢ LẬP ĐIỂM (Dữ liệu trên RAM, không lưu)";
            txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));

            // Change grid backgrounds
            dgSimulator.Background = new SolidColorBrush(Color.FromRgb(224, 242, 241));
            dgSimulator.RowBackground = new SolidColorBrush(Color.FromRgb(224, 242, 241));
            dgSimulator.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(204, 232, 231));

            // Enable grid edit columns
            BuildDataGridColumns();
            dgSimulator.ItemsSource = _dataTable.DefaultView;
        }

        private void StopSim_Click(object sender, RoutedEventArgs e)
        {
            ExitSimulator();
            LoadGrades();
        }

        private void ExitSimulator()
        {
            _isSimulatorMode = false;

            btnStartSim.Visibility = Visibility.Visible;
            btnStopSim.Visibility = Visibility.Collapsed;
            cboSemester.IsEnabled = true;

            statusBanner.Background = new SolidColorBrush(Color.FromRgb(236, 239, 241));
            txtStatus.Text = "Chế độ xem bảng điểm thực tế";
            txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(84, 110, 122));

            dgSimulator.Background = new SolidColorBrush(Colors.White);
            dgSimulator.RowBackground = new SolidColorBrush(Colors.White);
            dgSimulator.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(250, 250, 250));
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;

            var col = e.Column as DataGridTextColumn;
            if (col == null) return;

            string colPath = (col.Binding as System.Windows.Data.Binding)?.Path?.Path ?? "";
            if (!_gradeCols.Contains(colPath)) return;

            var rowView = e.Row.Item as DataRowView;
            if (rowView == null) return;

            var textBox = e.EditingElement as TextBox;
            if (textBox == null) return;

            string input = textBox.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(input))
            {
                rowView[colPath] = "";
            }
            else if (input == "V" || input == "M")
            {
                rowView[colPath] = input;
            }
            else
            {
                input = input.Replace(',', '.');
                if (double.TryParse(input, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double score))
                {
                    score = Math.Max(0, Math.Min(10, Math.Round(score, 1)));
                    textBox.Text = score.ToString("F1");
                    rowView[colPath] = score.ToString("F1");
                }
                else
                {
                    // Invalid, revert
                    e.Cancel = true;
                    textBox.Text = rowView[colPath]?.ToString() ?? "";
                    return;
                }
            }

            // Recalc average and classification for this row
            Dispatcher.BeginInvoke(new Action(() =>
            {
                rowView["TB"] = CalcAverageFromRow(rowView.Row);
                rowView["XepLoai"] = CalcClassificationFromRow(rowView.Row);
                UpdateSummary();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private string CalcAverageFromRow(DataRow row)
        {
            double totalWeighted = 0;
            int totalWeight = 0;
            bool hasAny = false;

            bool hasGK = false;
            bool hasCK = false;

            // Weights: TX=1, GK=2, CK=3
            foreach (var col in _gradeCols)
            {
                string val = row[col]?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(val)) continue;

                string norm = val.ToUpper();
                if (norm == "M")
                {
                    if (col == "GK") hasGK = true;
                    if (col == "CK") hasCK = true;
                    continue;
                }

                double score = 0.0;
                if (norm == "V")
                {
                    score = 0.0;
                    if (col == "GK") hasGK = true;
                    if (col == "CK") hasCK = true;
                }
                else if (double.TryParse(val.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sc))
                {
                    score = sc;
                    if (col == "GK") hasGK = true;
                    if (col == "CK") hasCK = true;
                }
                else
                {
                    continue;
                }

                int w = (col == "GK") ? 2 : (col == "CK") ? 3 : 1;
                totalWeighted += score * w;
                totalWeight += w;
                hasAny = true;
            }

            // Circular 22 rules require at least GK and CK for average calculation
            if (!hasGK && !hasCK) return "CDD (Thiếu GK, CK)";
            if (!hasGK) return "CDD (Thiếu GK)";
            if (!hasCK) return "CDD (Thiếu CK)";

            return hasAny && totalWeight > 0
                ? Math.Round(totalWeighted / totalWeight, 1).ToString("F1")
                : "";
        }

        private string CalcClassificationFromRow(DataRow row)
        {
            string avgStr = row["TB"]?.ToString() ?? "";
            if (double.TryParse(avgStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double score))
            {
                return TT22GradingService.ClassifyAcademic(score);
            }
            return "";
        }

        private void UpdateSummary()
        {
            if (_dataTable.Rows.Count == 0)
            {
                ClearSummary();
                return;
            }

            var subjectAvgs = new List<double>();
            bool isCDD = false;

            foreach (DataRow row in _dataTable.Rows)
            {
                string avgStr = row["TB"]?.ToString() ?? "";
                if (avgStr.StartsWith("CDD") || string.IsNullOrEmpty(avgStr))
                {
                    isCDD = true;
                }
                else if (double.TryParse(avgStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double avg))
                {
                    subjectAvgs.Add(avg);
                }
            }

            if (isCDD || !subjectAvgs.Any())
            {
                txtEstClassification.Text = "Chưa đủ điểm";
                txtEstCountTot.Text = "0";
                txtEstCountKha.Text = "0";
                return;
            }

            int n = subjectAvgs.Count;
            int n8 = subjectAvgs.Count(a => a >= 8.0);
            int n65 = subjectAvgs.Count(a => a >= 6.5);
            int n50 = subjectAvgs.Count(a => a >= 5.0);
            int n35 = subjectAvgs.Count(a => a >= 3.5);

            txtEstCountTot.Text = n8.ToString();
            txtEstCountKha.Text = n65.ToString();

            // Circular 22 Overall Classification logic:
            if (n65 == n && (n8 >= 6 || (n < 6 && n8 == n)))
            {
                txtEstClassification.Text = "Tốt";
            }
            else if (n50 == n && (n65 >= 6 || (n < 6 && n65 == n)))
            {
                txtEstClassification.Text = "Khá";
            }
            else if (n35 == n && n50 >= n - 1)
            {
                txtEstClassification.Text = "Đạt";
            }
            else
            {
                txtEstClassification.Text = "Chưa đạt";
            }
        }

        private void ClearSummary()
        {
            txtEstClassification.Text = "—";
            txtEstCountTot.Text = "0";
            txtEstCountKha.Text = "0";
        }
    }
}
