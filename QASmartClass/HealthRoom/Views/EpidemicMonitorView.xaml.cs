using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.HealthRoom.Views
{
    public partial class EpidemicMonitorView : Page
    {
        private readonly AppDbContext _db;

        public static readonly DependencyProperty AlertVisibilityProperty =
            DependencyProperty.Register(nameof(AlertVisibility), typeof(Visibility), typeof(EpidemicMonitorView), new PropertyMetadata(Visibility.Collapsed));

        public Visibility AlertVisibility
        {
            get => (Visibility)GetValue(AlertVisibilityProperty);
            set => SetValue(AlertVisibilityProperty, value);
        }

        public EpidemicMonitorView()
        {
            InitializeComponent();
            _db = new AppDbContext();
            DataContext = this;
            Unloaded += (s, e) => { _db?.Dispose(); };
            Loaded += (_, __) => LoadData();
        }

        private void LoadStudents()
        {
            try
            {
                var activeStudents = _db.Students
                    .Where(s => s.Status == "Active")
                    .OrderBy(s => s.FullName)
                    .ToList();
                CboStudent.ItemsSource = activeStudents;
                CboStudent.DisplayMemberPath = "FullName";
                CboStudent.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load active students for epidemic monitoring");
            }
        }

        private void CboStudent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboStudent.SelectedItem is Student selectedStudent)
            {
                TxtClassName.Text = selectedStudent.ClassName ?? "";
            }
        }

        private void LoadData()
        {
            LoadStudents();
            try
            {
                if (!_db.EpidemicCases.Any())
                {
                    _db.EpidemicCases.AddRange(
                        new EpidemicCase { StudentId = 1, StudentName = "Nguyễn Văn A", ClassName = "10A", Disease = "Sốt xuất huyết", OnsetDate = DateTime.Today.AddDays(-2), Status = "Active", IsolatedAt = "Home" },
                        new EpidemicCase { StudentId = 2, StudentName = "Trần Thị B", ClassName = "10B", Disease = "Sốt xuất huyết", OnsetDate = DateTime.Today.AddDays(-3), Status = "Active", IsolatedAt = "Hospital" },
                        new EpidemicCase { StudentId = 3, StudentName = "Lê Văn C", ClassName = "11C", Disease = "Sốt xuất huyết", OnsetDate = DateTime.Today.AddDays(-1), Status = "Active", IsolatedAt = "Home" },
                        new EpidemicCase { StudentId = 4, StudentName = "Phạm Thị D", ClassName = "12A", Disease = "Cúm A", OnsetDate = DateTime.Today.AddDays(-5), Status = "Recovered", IsolatedAt = "Home" }
                    );
                    _db.SaveChanges();
                }

                var cases = _db.EpidemicCases.OrderByDescending(c => c.OnsetDate).ToList();
                DgCases.ItemsSource = cases;

                // Threshold Check for all infectious diseases (>= 3 active cases in last 7 days)
                var last7Days = DateTime.Today.AddDays(-7);
                var activeCasesIn7Days = cases
                    .Where(c => c.OnsetDate >= last7Days && c.Status == "Active")
                    .GroupBy(c => c.Disease)
                    .Select(g => new { Disease = g.Key, Count = g.Count() })
                    .Where(x => x.Count >= 3)
                    .ToList();

                if (activeCasesIn7Days.Any())
                {
                    var alertMessages = activeCasesIn7Days.Select(a =>
                    {
                        string recommendation = a.Disease switch
                        {
                            "Sốt xuất huyết" => "Đề nghị phun thuốc diệt muỗi toàn trường và phát quang bụi rậm!",
                            "Cúm A" => "Đề nghị khử khuẩn các phòng học liên quan và đeo khẩu trang!",
                            "Thủy đậu" => "Đề nghị cách ly nghiêm ngặt các ca bệnh và lau dọn đồ dùng học tập bằng Cloramin B!",
                            "Sởi" => "Đề nghị rà soát tiêm chủng và phun khử khuẩn lớp học!",
                            _ => "Đề nghị theo dõi sát sao tình hình sức khỏe và thực hiện các biện pháp cách ly y tế cần thiết!"
                        };
                        return $"Phát hiện {a.Count} ca {a.Disease} trong tuần qua. {recommendation}";
                    });

                    TxtAlertMessage.Text = string.Join("\n", alertMessages);
                    AlertVisibility = Visibility.Visible;
                }
                else
                {
                    AlertVisibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex) { Log.Warning("Epidemic Load error: {Err}", ex.Message); }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadData();

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            CboStudent.SelectedIndex = -1;
            TxtClassName.Text = string.Empty;
            CbDisease.SelectedIndex = 0;
            DpOnsetDate.SelectedDate = DateTime.Today;
            CbIsolatedAt.SelectedIndex = 0;
            CbStatus.SelectedIndex = 0;
            PopupAddCase.Visibility = Visibility.Visible;
        }

        private void BtnCancelAddCase_Click(object sender, RoutedEventArgs e)
        {
            PopupAddCase.Visibility = Visibility.Collapsed;
        }

        private void BtnSaveCase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CboStudent.SelectedItem is not Student student)
                {
                    MessageBox.Show("Vui lòng chọn học sinh!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string name = student.FullName;
                string className = TxtClassName.Text.Trim();
                string disease = (CbDisease.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Khác";
                DateTime onsetDate = DpOnsetDate.SelectedDate ?? DateTime.Today;
                string isolatedAt = (CbIsolatedAt.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Home";
                string status = (CbStatus.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Active";

                if (onsetDate > DateTime.Today)
                {
                    MessageBox.Show("Ngày phát bệnh không được ở tương lai!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newCase = new EpidemicCase
                {
                    StudentId = student.Id,
                    StudentName = name,
                    ClassName = className,
                    Disease = disease,
                    OnsetDate = onsetDate,
                    IsolatedAt = isolatedAt,
                    Status = status
                };

                _db.EpidemicCases.Add(newCase);
                _db.SaveChanges();

                PopupAddCase.Visibility = Visibility.Collapsed;
                LoadData();
                MessageBox.Show("✅ Đã ghi nhận ca bệnh thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu ca bệnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;
            try
            {
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt",
                    FileName = $"BaoCao_DichBenh_{DateTime.Now:yyyyMMdd}.txt"
                };

                if (sfd.ShowDialog() == true)
                {
                    var cases = _db.EpidemicCases.OrderByDescending(c => c.OnsetDate).ToList();
                    await System.Threading.Tasks.Task.Run(() =>
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("BÁO CÁO GIÁM SÁT DỊCH BỆNH HỌC ĐƯỜNG");
                        sb.AppendLine($"Ngày lập: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                        sb.AppendLine("=".PadRight(50, '='));
                        sb.AppendLine();
                        sb.AppendLine($"Tổng số ca ghi nhận: {cases.Count} ca");
                        sb.AppendLine($"Số ca đang điều trị: {cases.Count(c => c.Status == "Active")} ca");
                        sb.AppendLine($"Số ca đã khỏi bệnh: {cases.Count(c => c.Status == "Recovered")} ca");
                        sb.AppendLine();
                        sb.AppendLine("DANH SÁCH CHI TIẾT:");
                        sb.AppendLine("Học sinh\tLớp\tBệnh\tNgày phát\tNơi cách ly\tTrạng thái");
                        foreach (var c in cases)
                        {
                            string isolatedAtVi = c.IsolatedAt == "Hospital" ? "Bệnh viện" : "Tại nhà";
                            string statusVi = c.Status == "Recovered" ? "Đã khỏi bệnh" : "Đang điều trị";
                            sb.AppendLine($"{c.StudentName}\t{c.ClassName}\t{c.Disease}\t{c.OnsetDate:dd/MM/yyyy}\t{isolatedAtVi}\t{statusVi}");
                        }
                        System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                    });
                    MessageBox.Show("✅ Xuất báo cáo thành công!", "Xuất báo cáo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất báo cáo: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }
    }
}
