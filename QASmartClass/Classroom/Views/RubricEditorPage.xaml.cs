using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.Services;

namespace QASmartClass.Classroom.Views
{
    public partial class RubricEditorPage : Page
    {
        private readonly AppDbContext _db;
        private readonly RubricService _rubricService;
        public ObservableCollection<CriteriaItem> CriteriaList { get; set; } = new();

        public RubricEditorPage()
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
            }

            icCriteria.ItemsSource = CriteriaList;
            
            // Thêm 1 tiêu chí mặc định
            AddCriteria_Click(null, null);
        }

        private void AddCriteria_Click(object? sender, RoutedEventArgs? e)
        {
            CriteriaList.Add(new CriteriaItem 
            { 
                Name = "Tiêu chí mới", 
                MaxPoints = 10,
                L1 = "Nội dung chưa đạt, còn nhiều thiếu sót.",
                L2 = "Nội dung đạt yêu cầu cơ bản.",
                L3 = "Nội dung tốt, trình bày rõ ràng.",
                L4 = "Xuất sắc, sáng tạo và hoàn thiện."
            });
        }

        private void RemoveCriteria_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CriteriaItem item)
            {
                CriteriaList.Remove(item);
            }
        }

        private void SaveRubric_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtTitle.Text))
                {
                    MessageBox.Show("Vui lòng nhập tên Rubric!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!CriteriaList.Any())
                {
                    MessageBox.Show("Cần ít nhất 1 tiêu chí!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string subject = cbSubject.Text;
                string grade = txtGrade.Text;
                string teacherName = "GV_Admin"; // TODO: Lấy từ session

                var criteriaData = CriteriaList.Select(c => 
                    (c.Name, c.MaxPoints, c.L1, c.L2, c.L3, c.L4)
                ).ToList();

                _rubricService.CreateRubric(txtTitle.Text, subject, grade, teacherName, criteriaData);

                MessageBox.Show("Lưu Rubric thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Reset form
                txtTitle.Text = "";
                CriteriaList.Clear();
                AddCriteria_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class CriteriaItem
    {
        public string Name { get; set; } = "";
        public double MaxPoints { get; set; }
        public string L1 { get; set; } = "";
        public string L2 { get; set; } = "";
        public string L3 { get; set; } = "";
        public string L4 { get; set; } = "";
    }
}
