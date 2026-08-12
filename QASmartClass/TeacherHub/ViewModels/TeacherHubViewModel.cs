﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using CommunityToolkit.Mvvm.Input;

namespace QASmartClass.TeacherHub.ViewModels
{
    public partial class TeacherHubViewModel : ObservableObject
    {
        [ObservableProperty]
        private object _currentViewModel;

        private TeacherDashboardViewModel _dashboardVM = new();
        private TeacherClassesViewModel _classesVM = new();
        private TeacherGradingViewModel _gradingVM = new();
        private TeacherAssignmentsViewModel _assignmentsVM = new();

        public TeacherHubViewModel()
        {
            CurrentViewModel = _dashboardVM;
        }

        [RelayCommand]
        private void NavigateToDashboard() => CurrentViewModel = _dashboardVM;

        [RelayCommand]
        private void NavigateToClasses() => CurrentViewModel = _classesVM;

        [RelayCommand]
        private void NavigateToGrading() => CurrentViewModel = _gradingVM;

        [RelayCommand]
        private void NavigateToAssignments() => CurrentViewModel = _assignmentsVM;
    }

    public partial class TeacherDashboardViewModel : ObservableObject 
    {
        [ObservableProperty]
        private ObservableCollection<Lesson> _todaysLessons = new();

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _hasLessons = false;

        public TeacherDashboardViewModel()
        {
            _ = LoadTodaysLessonsAsync();
        }

        [RelayCommand]
        private async Task LoadTodaysLessonsAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var today = DateTime.Today;
                var lessons = await db.Lessons
                    .Where(l => l.ScheduledFor.HasValue && l.ScheduledFor.Value.Date == today)
                    .OrderBy(l => l.Period)
                    .ToListAsync();

                TodaysLessons = new ObservableCollection<Lesson>(lessons);
                HasLessons = TodaysLessons.Count > 0;
                
                if (!HasLessons)
                {
                    StatusMessage = "Hôm nay không có tiết dạy nào.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải dữ liệu: {ex.Message}";
                HasLessons = false;
            }
        }
    }

    public partial class TeacherClassesViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<object> _classes = new();

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public TeacherClassesViewModel()
        {
            _ = LoadClassesAsync();
        }

        [RelayCommand]
        private async Task LoadClassesAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var rosters = await db.ClassRosters.ToListAsync();
                var items = rosters.Select(r => new
                {
                    r.Id,
                    r.ClassName,
                    r.SchoolYear,
                    r.Semester,
                    r.GradeLevel,
                    r.StudentCount,
                    DisplayInfo = $"Sĩ số: {r.StudentCount} HS | Năm học: {r.SchoolYear} | HK: {r.Semester}",
                    CreatedDisplay = $"T?o ngày: {r.CreatedAt:dd/MM/yyyy}"
                }).ToList();

                Classes = new ObservableCollection<object>(items);
                StatusMessage = $"Tổng cộng {items.Count} lớp học";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
            }
        }
    }
}

