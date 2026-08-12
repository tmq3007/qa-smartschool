﻿using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.Dashboard.Views
{
    public partial class TeamPerformancePage : Page
    {
        private readonly AppDbContext _db;

        public TeamPerformancePage(AppDbContext db)
        {
            InitializeComponent();
            _db = db;
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPerformanceData();
        }

        private void LoadPerformanceData()
        {
            try
            {
                var allTasks = _db.TaskItems.ToList();
                
                // Group by Department
                var depts = new[] { "Toán-Tin", "Văn-Sử-Địa", "Đoàn-Đội", "Văn phòng" };
                var deptVms = new List<DepartmentPerformanceViewModel>();

                foreach (var dept in depts)
                {
                    // For mock purpose, mapping "ToanTin" -> "Toán-Tin" if needed
                    // Assume data matches perfectly or use Contains
                    var deptTasks = allTasks.Where(t => t.Department.Contains(dept) || t.Department == dept.Replace("-", "")).ToList();
                    
                    int total = deptTasks.Count;
                    if (total == 0) continue;

                    int done = deptTasks.Count(t => t.Status == "Done");
                    int overdue = deptTasks.Count(t => t.Status != "Done" && t.Deadline.Date < DateTime.Today);

                    deptVms.Add(new DepartmentPerformanceViewModel
                    {
                        DepartmentName = dept,
                        TotalTasks = total,
                        DoneTasks = done,
                        OverdueTasks = overdue
                    });
                }

                LvDepartments.ItemsSource = deptVms.OrderByDescending(x => x.CompletionRate).ToList();

                // Top performers
                var userStats = allTasks.Where(t => t.Status == "Done")
                                        .GroupBy(t => t.AssignedTo)
                                        .Select(g => new { TeacherName = g.Key, DoneCount = g.Count() })
                                        .OrderByDescending(x => x.DoneCount)
                                        .Take(5)
                                        .ToList();

                var performerVms = new List<TopPerformerViewModel>();
                for (int i = 0; i < userStats.Count; i++)
                {
                    performerVms.Add(new TopPerformerViewModel
                    {
                        Rank = i + 1,
                        TeacherName = userStats[i].TeacherName,
                        CompletedTasks = userStats[i].DoneCount
                    });
                }

                LvTopPerformers.ItemsSource = performerVms;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi tải dữ liệu Team Performance.");
                MessageBox.Show("Có lỗi xảy ra khi tải biểu đồ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class DepartmentPerformanceViewModel
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int TotalTasks { get; set; }
        public int DoneTasks { get; set; }
        public int OverdueTasks { get; set; }

        public double CompletionRate => TotalTasks == 0 ? 0 : (double)DoneTasks * 100 / TotalTasks;

        public string StatsText => $"{DoneTasks}/{TotalTasks} hoàn thành ({OverdueTasks} quá hạn)";

        public Brush BarColor
        {
            get
            {
                if (CompletionRate >= 80) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")); // Green
                if (CompletionRate >= 50) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")); // Blue
                if (CompletionRate >= 30) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")); // Yellow
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")); // Red
            }
        }
    }

    public class TopPerformerViewModel
    {
        public int Rank { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public int CompletedTasks { get; set; }

        public string RankIcon
        {
            get
            {
                return Rank switch
                {
                    1 => "🥇",
                    2 => "🥈",
                    3 => "🥉",
                    _ => $"#{Rank}"
                };
            }
        }
    }
}

