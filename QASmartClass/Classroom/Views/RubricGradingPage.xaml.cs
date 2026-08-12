using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.Services;

namespace QASmartClass.Classroom.Views
{
    public partial class RubricGradingPage : Page
    {
        private readonly AppDbContext _db;
        private readonly RubricService _rubricService;
        public ObservableCollection<GradingItemViewModel> GradingItems { get; set; } = new();

        public RubricGradingPage()
        {
            InitializeComponent();

            if (Application.Current is QASmartTouch.App app && app.Database != null)
            {
                _db = app.Database;
                _rubricService = new RubricService(_db);
            }
            else
            {
                MessageBox.Show("Chưa kết nối CSDL!");
                return;
            }

            icGrading.ItemsSource = GradingItems;
            LoadInitialData();
        }

        private void LoadInitialData()
        {
            // Load Rubrics
            var rubrics = _rubricService.GetRubricsBySubject();
            cbRubrics.ItemsSource = rubrics;
            if (rubrics.Any()) cbRubrics.SelectedIndex = 0;

            // Load Students
            var students = _db.Students.OrderBy(s => s.FullName).ToList();
            listStudents.ItemsSource = students;
        }

        private void CbRubrics_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateGradingForm();
        }

        private void ListStudents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateGradingForm();
        }

        private void UpdateGradingForm()
        {
            if (cbRubrics.SelectedItem is Rubric selectedRubric && listStudents.SelectedItem is Student selectedStudent)
            {
                scrollContent.Visibility = Visibility.Visible;
                txtStudentName.Text = selectedStudent.FullName;
                txtRubricName.Text = $"Rubric: {selectedRubric.Title}";

                GradingItems.Clear();
                
                // Lấy kết quả đã chấm (nếu có)
                var result = _rubricService.GetStudentResult(selectedRubric.Id, selectedStudent.Id);

                foreach (var criteria in selectedRubric.Criteria.OrderBy(c => c.SortOrder))
                {
                    var existingGrade = result?.CriteriaResults.FirstOrDefault(c => c.CriteriaName == criteria.CriteriaName);
                    
                    var item = new GradingItemViewModel(criteria);
                    item.PropertyChanged += (s, ev) => 
                    {
                        if (ev.PropertyName == nameof(GradingItemViewModel.GivenPoints))
                            UpdateTotalPoints();
                    };

                    if (existingGrade != null)
                    {
                        item.GivenPoints = existingGrade.Points;
                        item.Feedback = existingGrade.Feedback;
                        item.SetLevel(existingGrade.Level);
                    }

                    GradingItems.Add(item);
                }

                UpdateTotalPoints();
            }
            else
            {
                scrollContent.Visibility = Visibility.Collapsed;
            }
        }

        private void Level_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.DataContext is GradingItemViewModel item && rb.Tag is string levelStr)
            {
                if (int.TryParse(levelStr, out int level))
                {
                    // Tự động gán điểm theo level (ví dụ L4 = max, L3 = 75%, L2 = 50%, L1 = 25%)
                    double pct = level switch { 4 => 1.0, 3 => 0.75, 2 => 0.5, 1 => 0.25, _ => 0 };
                    item.GivenPoints = Math.Round(item.Criteria.MaxPoints * pct, 1);
                }
            }
        }

        private void Points_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTotalPoints();
        }

        private void UpdateTotalPoints()
        {
            if (cbRubrics.SelectedItem is Rubric rubric)
            {
                double total = GradingItems.Sum(g => g.GivenPoints);
                double max = rubric.Criteria.Sum(c => c.MaxPoints);
                txtTotalPoints.Text = $"{total}/{max}";
            }
        }

        private void SaveGrades_Click(object sender, RoutedEventArgs e)
        {
            if (cbRubrics.SelectedItem is Rubric rubric && listStudents.SelectedItem is Student student)
            {
                try
                {
                    var grades = GradingItems.Select(g => (
                        g.Criteria.Id,
                        g.GetLevel(),
                        g.GivenPoints,
                        g.Feedback
                    )).ToList();

                    _rubricService.GradeStudent(rubric.Id, student.Id, "GV_Admin", grades);
                    MessageBox.Show("Lưu điểm thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi lưu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class GradingItemViewModel : INotifyPropertyChanged
    {
        public RubricCriteria Criteria { get; }
        public string MaxPointsLabel => $"/ {Criteria.MaxPoints}";

        private double _givenPoints;
        public double GivenPoints
        {
            get => _givenPoints;
            set { _givenPoints = value; OnPropertyChanged(nameof(GivenPoints)); }
        }

        private string _feedback = "";
        public string Feedback
        {
            get => _feedback;
            set { _feedback = value; OnPropertyChanged(nameof(Feedback)); }
        }

        private bool _isL1;
        public bool IsL1 { get => _isL1; set { _isL1 = value; OnPropertyChanged(nameof(IsL1)); } }
        private bool _isL2;
        public bool IsL2 { get => _isL2; set { _isL2 = value; OnPropertyChanged(nameof(IsL2)); } }
        private bool _isL3;
        public bool IsL3 { get => _isL3; set { _isL3 = value; OnPropertyChanged(nameof(IsL3)); } }
        private bool _isL4;
        public bool IsL4 { get => _isL4; set { _isL4 = value; OnPropertyChanged(nameof(IsL4)); } }

        public GradingItemViewModel(RubricCriteria criteria)
        {
            Criteria = criteria;
        }

        public void SetLevel(int level)
        {
            IsL1 = level == 1;
            IsL2 = level == 2;
            IsL3 = level == 3;
            IsL4 = level == 4;
        }

        public int GetLevel()
        {
            if (IsL4) return 4;
            if (IsL3) return 3;
            if (IsL2) return 2;
            if (IsL1) return 1;
            return 0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
