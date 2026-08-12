﻿using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.Dashboard.Views
{
    public partial class SchoolCalendarPage : Page
    {
        private readonly AppDbContext _db;
        private readonly SchoolCalendarService _calendarService;
        private DateTime _currentWeekDate;

        public SchoolCalendarPage(AppDbContext db)
        {
            InitializeComponent();
            _db = db;
            _calendarService = new SchoolCalendarService(_db);
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _currentWeekDate = DateTime.Today;
            LoadWeek(_currentWeekDate);
        }

        private void LoadWeek(DateTime date)
        {
            try
            {
                // Calculate week range for header
                int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
                DateTime monday = date.AddDays(-diff).Date;
                DateTime sunday = monday.AddDays(6).Date;
                TxtWeekHeader.Text = $"Tuần {monday:dd/MM} – {sunday:dd/MM/yyyy}";

                // Get filter
                string department = "All";
                if (CbDepartment.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
                {
                    department = selectedItem.Tag.ToString() ?? "All";
                }

                // Query events
                var events = _calendarService.GetEventsByWeek(date);

                if (department != "All")
                {
                    events = events.Where(e => e.Department == department || e.Department == "All").ToList();
                }

                if (events.Count == 0)
                {
                    LvEvents.Visibility = Visibility.Collapsed;
                    SpEmptyState.Visibility = Visibility.Visible;
                }
                else
                {
                    LvEvents.Visibility = Visibility.Visible;
                    SpEmptyState.Visibility = Visibility.Collapsed;
                    
                    var viewModels = events.Select(e => new SchoolEventViewModel(e)).ToList();
                    LvEvents.ItemsSource = viewModels;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi tải lịch tuần.");
                MessageBox.Show("Có lỗi xảy ra khi tải lịch sự kiện.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPrevWeek_Click(object sender, RoutedEventArgs e)
        {
            _currentWeekDate = _currentWeekDate.AddDays(-7);
            LoadWeek(_currentWeekDate);
        }

        private void BtnNextWeek_Click(object sender, RoutedEventArgs e)
        {
            _currentWeekDate = _currentWeekDate.AddDays(7);
            LoadWeek(_currentWeekDate);
        }

        private void CbDepartment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                LoadWeek(_currentWeekDate);
            }
        }

        private void BtnAddEvent_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chức năng thêm sự kiện sẽ được hiển thị trong Dialog.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: M? AddEventDialog
        }
    }

    public class SchoolEventViewModel
    {
        private readonly SchoolEvent _ev;
        public SchoolEventViewModel(SchoolEvent ev) { _ev = ev; }

        public DateTime StartTime => _ev.StartTime;
        public string Title => _ev.Title;
        public string Organizer => _ev.Organizer;
        public string Location => string.IsNullOrWhiteSpace(_ev.Location) ? "Chưa xác định" : _ev.Location;

        public string StatusText
        {
            get
            {
                return _ev.Status switch
                {
                    "Done" => "✅ Hoàn thành",
                    "InProgress" => "⏳ Đang diễn ra",
                    "Cancelled" => "❌ Đã hủy",
                    _ => "📅 Kế hoạch"
                };
            }
        }

        public Brush StatusColor
        {
            get
            {
                return _ev.Status switch
                {
                    "Done" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1FAE5")), // Light Green
                    "InProgress" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7")), // Light Yellow
                    "Cancelled" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2")), // Light Red
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")) // Light Gray
                };
            }
        }

        public Brush StatusTextColor
        {
            get
            {
                return _ev.Status switch
                {
                    "Done" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#065F46")), // Dark Green
                    "InProgress" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#92400E")), // Dark Yellow
                    "Cancelled" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991B1B")), // Dark Red
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")) // Dark Gray
                };
            }
        }
    }
}

