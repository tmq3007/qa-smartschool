using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.ParentPortal.Views
{
    public partial class ParentTimetablePage : Page
    {
        private readonly AppDbContext _db;
        private readonly Student _student;

        public ParentTimetablePage(AppDbContext db, Student student)
        {
            InitializeComponent();
            _db = db;
            _student = student;
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTimetable();
        }

        private void LoadTimetable()
        {
            try
            {
                // Dọn sạch các ô cũ để tránh rò rỉ và vẽ đè
                var toRemove = TimetableGrid.Children.Cast<UIElement>()
                    .Where(c => Grid.GetRow(c) > 0 && Grid.GetColumn(c) > 0)
                    .ToList();
                foreach (var el in toRemove)
                {
                    TimetableGrid.Children.Remove(el);
                }

                var entries = (from t in _db.TimetableEntries
                               join r in _db.ClassRosters on t.RosterId equals r.Id
                               where r.ClassName == _student.ClassName && r.IsActive
                               select t).ToList();

                if (entries.Count == 0)
                {
                    // Hiển thị thông báo khi TKB chưa được thiết lập, tránh hiển thị dữ liệu mẫu gây nhầm lẫn
                    var alertBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(254, 242, 242)), // Soft red
                        BorderBrush = new SolidColorBrush(Color.FromRgb(252, 165, 165)), // Border red
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(20),
                        Margin = new Thickness(10),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    var alertText = new TextBlock
                    {
                        Text = "⚠️ Thời khóa biểu lớp hiện chưa được cập nhật.\nVui lòng liên hệ Giáo viên chủ nhiệm để biết thêm chi tiết.",
                        FontSize = 14,
                        FontWeight = FontWeights.Medium,
                        Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28)), // Dark red
                        TextAlignment = TextAlignment.Center,
                        LineHeight = 22
                    };
                    alertBorder.Child = alertText;

                    Grid.SetRow(alertBorder, 1);
                    Grid.SetRowSpan(alertBorder, 5);
                    Grid.SetColumn(alertBorder, 1);
                    Grid.SetColumnSpan(alertBorder, 6);
                    TimetableGrid.Children.Add(alertBorder);
                    return;
                }

                // Color map per subject
                var colors = new[] { "#DBEAFE", "#D1FAE5", "#FEF3C7", "#FCE7F3", "#E0E7FF", "#CFFAFE", "#FEE2E2" };
                var subjectIndex = 0;
                var colorMap = new System.Collections.Generic.Dictionary<string, string>();

                foreach (var entry in entries)
                {
                    int col = entry.DayOfWeek - 1; // 2=Mon -> Col 1 ... 7=Sat -> Col 6
                    int row = entry.Period;        // 1-5 -> Row 1-5

                    if (col < 1 || col > 6 || row < 1 || row > 5) continue;

                    if (!colorMap.ContainsKey(entry.Subject))
                    {
                        colorMap[entry.Subject] = colors[subjectIndex % colors.Length];
                        subjectIndex++;
                    }

                    var bg = colorMap[entry.Subject];
                    var border = new Border
                    {
                        Background = (SolidColorBrush)new BrushConverter().ConvertFrom(bg),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(5),
                        Margin = new Thickness(2)
                    };
                    var text = new TextBlock
                    {
                        Text = entry.Subject,
                        FontSize = 13,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.Black,
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    border.Child = text;

                    Grid.SetRow(border, row);
                    Grid.SetColumn(border, col);
                    TimetableGrid.Children.Add(border);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi tải thời khóa biểu cho phụ huynh");
            }
        }
    }
}

