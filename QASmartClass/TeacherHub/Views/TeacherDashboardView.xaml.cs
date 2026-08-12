﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;

namespace QASmartClass.TeacherHub.Views
{
    public partial class TeacherDashboardView : UserControl
    {
        private AppDbContext? _db;

        public TeacherDashboardView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _db = new AppDbContext();
                Unloaded += (s, e) => { _db?.Dispose(); _db = null!; };
                LoadDashboard();
            }
            catch (Exception ex)
            {
                Log.Warning("[TeacherDashboard] Load error: {Err}", ex.Message);
            }
        }

        private void LoadDashboard()
        {
            if (_db == null) return;

            // --- 1. i?m TB l?p -----------------------------------
            var teacherName = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "";
            var myRosterIds = _db.ClassRosters
                .Where(r => r.TeacherName == teacherName && r.IsActive)
                .Select(r => r.Id)
                .ToList();
            var myStudentIds = _db.ClassRosterStudents
                .Where(rs => myRosterIds.Contains(rs.RosterId))
                .Select(rs => rs.StudentId)
                .Distinct()
                .ToList();

            var allGrades = _db.StudentGrades
                .Where(g => myStudentIds.Contains(g.StudentId) && myRosterIds.Contains(g.RosterId))
                .ToList();
            double classAvg = allGrades.Any()
                ? Math.Round(allGrades.Average(g => g.Score), 1)
                : 0;

            // --- 2. Phn b? di?m ----------------------------------
            int excellent = allGrades.Count(g => g.Score >= 8.5);   // Giỏi
            int good      = allGrades.Count(g => g.Score >= 7.0 && g.Score < 8.5); // Kh
            int average   = allGrades.Count(g => g.Score >= 5.0 && g.Score < 7.0); // TB
            int weak      = allGrades.Count(g => g.Score < 5.0);    // Yếu
            int total     = allGrades.Count;

            // --- 3. Chuyn c?n hm nay ----------------------------
            var today = DateTime.Today;
            var todayAtt = _db.AttendanceRecords
                .Where(a => a.Date.Date == today && myStudentIds.Contains(a.StudentId) && myRosterIds.Contains(a.RosterId))
                .ToList();
            int presentCount = todayAtt.Count(a =>
                a.Status == "Present" || a.Status == "Có mặt");
            double attendanceRate = todayAtt.Any()
                ? Math.Round((double)presentCount / todayAtt.Count * 100, 1)
                : 0;

            // --- 4. HS c?n quan tm -------------------------------
            var atRiskService = new EarlyWarningService(_db);
            var atRiskList = atRiskService.GetAtRiskStudents(today.Month, today.Year);

            // --- 5. C?p nh?t UI -----------------------------------
            Dispatcher.Invoke(() => UpdateUI(
                classAvg, excellent, good, average, weak, total,
                attendanceRate, todayAtt.Count, atRiskList));
        }

        private void UpdateUI(double avg, int exc, int gd, int avr, int wk, int total,
            double attRate, int totalAtt, List<AtRiskStudentDTO> atRisk)
        {
            // KPI: i?m TB
            if (FindName("TxtClassAvg") is TextBlock txtAvg)
                txtAvg.Text = avg.ToString("F1");

            // KPI: Chuyn c?n
            if (FindName("TxtAttendance") is TextBlock txtAtt)
                txtAtt.Text = $"{attRate}% ({totalAtt} HS)";

            // KPI: HS c?n quan tm
            if (FindName("TxtAtRisk") is TextBlock txtRisk)
                txtRisk.Text = atRisk.Count.ToString();

            // Phn b? di?m
            if (total > 0)
            {
                SetBar("BarExcellent", "TxtExcellent", exc, total);
                SetBar("BarGood",      "TxtGood",      gd,  total);
                SetBar("BarAverage",   "TxtAverage",   avr, total);
                SetBar("BarWeak",      "TxtWeak",       wk, total);
            }

            // Danh sch HS c?n quan tm
            if (FindName("ListAtRisk") is ListBox lst)
            {
                lst.ItemsSource = atRisk.Select(r =>
                    $"{r.StudentName}  {r.Reason}").ToList();
            }

            Log.Information("[TeacherDashboard] Loaded: Avg={Avg}, Attendance={Att}%, AtRisk={Risk}",
                avg, attRate, atRisk.Count);
        }

        private void SetBar(string barName, string txtName, int count, int total)
        {
            double pct = Math.Round((double)count / total * 100, 1);
            if (FindName(barName) is System.Windows.Shapes.Rectangle bar)
                bar.Width = pct * 2; // scale: 100% = 200px
            if (FindName(txtName) is TextBlock txt)
                txt.Text = $"{count} HS ({pct}%)";
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
            => LoadDashboard();
    }
}


