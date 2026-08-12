using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.Dashboard.Views
{
    public partial class TimetablePage : Page
    {
        private readonly AppDbContext _db;
        private List<TimetableEntry> _allEntries = new();

        public TimetablePage(AppDbContext db)
        {
            InitializeComponent();
            _db = db;

            if (!Resources.Contains("BooleanToVisibilityConverter"))
            {
                Resources.Add("BooleanToVisibilityConverter", new BooleanToVisibilityConverter());
            }

            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _allEntries = _db.TimetableEntries.ToList();
                CbViewMode.SelectedIndex = 0; // Trigger selection change
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi tải dữ liệu TimetablePage");
            }
        }

        private void CbViewMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            string mode = (CbViewMode.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Class";
            CbTargetSelection.Items.Clear();

            try
            {
                if (mode == "Class")
                {
                    var rosters = _db.ClassRosters.ToList();
                    foreach (var roster in rosters)
                    {
                        CbTargetSelection.Items.Add(new ComboBoxItem
                        {
                            Content = $"{roster.ClassName} ({roster.Subject})",
                            Tag = roster.Id
                        });
                    }
                }
                else // Teacher
                {
                    var teachers = _allEntries.Select(x => x.TeacherName).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
                    foreach (var teacher in teachers)
                    {
                        CbTargetSelection.Items.Add(new ComboBoxItem
                        {
                            Content = teacher,
                            Tag = teacher
                        });
                    }
                }

                if (CbTargetSelection.Items.Count > 0)
                {
                    CbTargetSelection.SelectedIndex = 0;
                }
                else
                {
                    LoadGridData(mode, null);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi thay đổi ViewMode TKB");
            }
        }

        private void CbTargetSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            string mode = (CbViewMode.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Class";
            object? tag = (CbTargetSelection.SelectedItem as ComboBoxItem)?.Tag;
            LoadGridData(mode, tag);
        }

        private void LoadGridData(string mode, object? targetTag)
        {
            var cellViewModels = new List<TimetableCellViewModel>();
            List<TimetableEntry> currentEntries = new();

            if (targetTag != null)
            {
                if (mode == "Class" && targetTag is int rosterId)
                {
                    currentEntries = _allEntries.Where(x => x.RosterId == rosterId).ToList();
                }
                else if (mode == "Teacher" && targetTag is string teacherName)
                {
                    currentEntries = _allEntries.Where(x => x.TeacherName == teacherName).ToList();
                }
            }

            int currentDayOfWeek = (int)DateTime.Today.DayOfWeek;
            // Map Sunday=0, Monday=1 to our logic: Monday=2, Sunday=8
            if (currentDayOfWeek == 0) currentDayOfWeek = 8; else currentDayOfWeek++;

            for (int period = 1; period <= 5; period++)
            {
                // Ti?t c?t (Period header)
                cellViewModels.Add(new TimetableCellViewModel
                {
                    IsPeriodHeader = true,
                    PeriodLabel = $"Tiết {period}",
                    BackgroundBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"))
                });

                // 6 c?t dữ liệu (Th? 2 -> Th? 7)
                for (int day = 2; day <= 7; day++)
                {
                    var entry = currentEntries.FirstOrDefault(x => x.DayOfWeek == day && x.Period == period);
                    bool isCurrent = (day == currentDayOfWeek); // simplified current period logic
                    
                    var vm = new TimetableCellViewModel
                    {
                        IsDataCell = true,
                        IsCurrent = isCurrent
                    };

                    if (entry != null)
                    {
                        vm.Subject = entry.Subject;
                        if (mode == "Class")
                        {
                            vm.SubInfo = string.IsNullOrWhiteSpace(entry.TeacherName) ? "" : entry.TeacherName;
                        }
                        else
                        {
                            var roster = _db.ClassRosters.FirstOrDefault(r => r.Id == entry.RosterId);
                            vm.SubInfo = roster != null ? roster.ClassName : "";
                        }
                        
                        if (!string.IsNullOrWhiteSpace(entry.Room))
                        {
                            vm.SubInfo += $" - {entry.Room}";
                        }
                    }
                    else
                    {
                        vm.Subject = "(trống)";
                        vm.SubInfo = "";
                    }

                    vm.UpdateBrush();
                    cellViewModels.Add(vm);
                }
            }

            IcTimetableGrid.ItemsSource = cellViewModels;
        }
    }

    public class TimetableCellViewModel
    {
        public bool IsPeriodHeader { get; set; }
        public bool IsDataCell { get; set; }
        public string PeriodLabel { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string SubInfo { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }
        public Brush BackgroundBrush { get; set; } = new SolidColorBrush(Colors.White);

        public void UpdateBrush()
        {
            if (IsPeriodHeader) return;

            if (Subject == "(trống)")
            {
                BackgroundBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                return;
            }

            if (IsCurrent)
            {
                BackgroundBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBEAFE")); // Blue for current/today
            }
            else
            {
                BackgroundBrush = new SolidColorBrush(Colors.White);
            }
        }
    }
}

