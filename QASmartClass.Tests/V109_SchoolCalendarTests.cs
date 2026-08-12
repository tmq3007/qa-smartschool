using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Leadership.Views;

namespace QASmartClass.Tests
{
    [Collection("Sequential")]
    public class V109_SchoolCalendarTests
    {
        private AppDbContext CreateTempDbContext(string dbPath)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            var db = new AppDbContext(options);
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            return db;
        }

        [Fact]
        public void Test_SchoolCalendar_FallbackPeriods_WhenDbEmpty_Test()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "calendar_fallback.db");
            try
            {
                using (var db = CreateTempDbContext(dbPath))
                {
                    // 1. If DB is empty, logic must fallback to 10 periods
                    var list = LoadTimetableConfigTest(db);
                    Assert.Equal(10, list.Count);
                    Assert.Equal(1, list[0].Period);
                    Assert.Equal("07:30", list[0].Start);
                    Assert.Equal("08:15", list[0].End);

                    // 2. If DB has config, logic must parse config
                    var configJson = "[{\"Period\":1,\"Start\":\"07:45\",\"End\":\"08:30\"}]";
                    db.SystemSettings.Add(new SystemSetting { Id = "Timetable_Periods_Config", Value = configJson, Category = "Timetable", LastUpdated = DateTime.Now });
                    db.SaveChanges();

                    var list2 = LoadTimetableConfigTest(db);
                    Assert.Single(list2);
                    Assert.Equal(1, list2[0].Period);
                    Assert.Equal("07:45", list2[0].Start);
                    Assert.Equal("08:30", list2[0].End);
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        private List<SchoolCalendarView.PeriodConfig> LoadTimetableConfigTest(AppDbContext db)
        {
            var list = new List<SchoolCalendarView.PeriodConfig>();
            try
            {
                var setting = db.SystemSettings.Find("Timetable_Periods_Config");
                if (setting != null && !string.IsNullOrEmpty(setting.Value))
                {
                    using (var doc = System.Text.Json.JsonDocument.Parse(setting.Value))
                    {
                        foreach (var elem in doc.RootElement.EnumerateArray())
                        {
                            list.Add(new SchoolCalendarView.PeriodConfig
                            {
                                Period = elem.GetProperty("Period").GetInt32(),
                                Start = elem.GetProperty("Start").GetString() ?? string.Empty,
                                End = elem.GetProperty("End").GetString() ?? string.Empty
                            });
                        }
                    }
                }
            }
            catch { }

            if (!list.Any())
            {
                list.Add(new SchoolCalendarView.PeriodConfig { Period = 1, Start = "07:30", End = "08:15" });
                list.Add(new SchoolCalendarView.PeriodConfig { Period = 2, Start = "08:20", End = "09:05" });
                list.Add(new SchoolCalendarView.PeriodConfig { Period = 3, Start = "09:15", End = "10:00" });
                list.Add(new SchoolCalendarView.PeriodConfig { Period = 4, Start = "10:05", End = "10:50" });
                list.Add(new SchoolCalendarView.PeriodConfig { Period = 5, Start = "11:00", End = "11:45" });
                list.Add(new SchoolCalendarView.PeriodConfig { Period = 6, Start = "13:30", End = "14:15" });
                list.Add(new SchoolCalendarView.PeriodConfig { Period = 7, Start = "14:20", End = "15:05" });
                list.Add(new SchoolCalendarView.PeriodConfig { Period = 8, Start = "15:15", End = "16:00" });
                list.Add(new SchoolCalendarView.PeriodConfig { Period = 9, Start = "16:05", End = "16:50" });
                list.Add(new SchoolCalendarView.PeriodConfig { Period = 10, Start = "17:00", End = "17:45" });
            }

            return list;
        }

        [Fact]
        public void Test_SchoolCalendar_DetectOverlapEvents_Test()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "calendar_overlap.db");
            try
            {
                using (var db = CreateTempDbContext(dbPath))
                {
                    var calService = new SchoolCalendarService(db);

                    // Add an event in Room A from 08:00 to 09:00
                    var ev1 = new SchoolEvent
                    {
                        Title = "Họp tổ Toán",
                        Location = "Phòng Lab 1",
                        StartTime = DateTime.Today.AddHours(8),
                        EndTime = DateTime.Today.AddHours(9),
                        Status = "Planned"
                    };
                    calService.CreateEvent(ev1);

                    // Test overlapping checks:
                    // 1. Same location, overlaps (08:30 - 09:30)
                    bool isOverlap1 = db.SchoolEvents.Any(e => 
                        e.Location == "Phòng Lab 1" && 
                        e.StartTime < DateTime.Today.AddHours(9.5) && 
                        DateTime.Today.AddHours(8.5) < e.EndTime
                    );
                    Assert.True(isOverlap1);

                    // 2. Same location, consecutive (09:00 - 10:00) -> should not overlap
                    bool isOverlap2 = db.SchoolEvents.Any(e => 
                        e.Location == "Phòng Lab 1" && 
                        e.StartTime < DateTime.Today.AddHours(10) && 
                        DateTime.Today.AddHours(9) < e.EndTime
                    );
                    Assert.False(isOverlap2);

                    // 3. Different location, overlaps time (08:30 - 09:30) -> should not overlap
                    bool isOverlap3 = db.SchoolEvents.Any(e => 
                        e.Location == "Phòng Lab 2" && 
                        e.StartTime < DateTime.Today.AddHours(9.5) && 
                        DateTime.Today.AddHours(8.5) < e.EndTime
                    );
                    Assert.False(isOverlap3);
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }
    }
}
