using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.Leadership.Views
{
    public partial class ProfessionalTopicView : Page
    {
        private readonly AppDbContext _db;

        public ProfessionalTopicView()
        {
            InitializeComponent();
            _db = new AppDbContext();
            Unloaded += (s, e) => { _db?.Dispose(); };
            Loaded += (_, __) => {
                if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;
                LoadData();
            };
        }

        private void LoadData()
        {
            try
            {
                var topics = _db.ProfessionalTopics
                    .OrderByDescending(t => t.PresentationDate)
                    .ToList();

                // Stats
                TxtTotalTopics.Text = topics.Count.ToString();
                TxtCompleted.Text = topics.Count(t => t.Status == "Completed").ToString();
                TxtPlanned.Text = topics.Count(t => t.Status == "Planned").ToString();
                var avgAll = topics.Where(t => t.EvaluatorCount > 0).Select(t => t.AverageRating);
                TxtAvgRating.Text = avgAll.Any() ? $"{avgAll.Average():F1} ⭐" : "0.0 ⭐";

                // List
                LvTopics.ItemsSource = topics.Select(t => new
                {
                    t.Id,
                    t.TopicTitle,
                    PresenterInfo = $"Người trình bày: {t.Presenter}  |  Ngày trình bày: {t.PresentationDate:dd/MM/yyyy}",
                    Description = string.IsNullOrWhiteSpace(t.Description) ? "" : t.Description,
                    StarsText = RenderStars(t.AverageRating),
                    RatingInfo = t.EvaluatorCount > 0 ? $"{t.AverageRating:F1}/5 ({t.EvaluatorCount} lượt)" : "Chưa có đánh giá",
                    StatusText = t.Status == "Completed" ? "Đã trình bày" : "Đang lên kế hoạch",
                    StatusBg = t.Status == "Completed" ? "#DCFCE7" : "#FEF9C3",
                    StatusFg = t.Status == "Completed" ? "#16A34A" : "#CA8A04",
                    DotColor = t.Status == "Completed" ? "#16A34A" : "#F59E0B"
                }).ToList();
            }
            catch (Exception ex) { Log.Error("[ProfTopic] LoadData err: {Err}", ex.Message); }
        }

        private static string RenderStars(double rating)
        {
            int full = (int)Math.Floor(rating);
            var s = new string('⭐', Math.Min(full, 5));
            while (s.Length < 5) s += "☆";
            return s;
        }

        private void BtnAddTopic_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Window
            {
                Title = "Tạo chuyên đề mới",
                Width = 460, Height = 520,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                FontFamily = new FontFamily("Segoe UI"),
                ResizeMode = ResizeMode.NoResize
            };
            var sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var sp = new StackPanel { Margin = new Thickness(20) };

            sp.Children.Add(Header("THÔNG TIN CHUYÊN ĐỀ"));

            sp.Children.Add(Lbl("Tên chuyên đề:"));
            var txtTitle = Txt(""); sp.Children.Add(txtTitle);

            sp.Children.Add(Lbl("Người trình bày:"));
            var txtPresenter = Txt(""); sp.Children.Add(txtPresenter);

            sp.Children.Add(Lbl("Tổ chuyên môn:"));
            var cbDept = new ComboBox { FontSize = 13, Margin = new Thickness(0, 0, 0, 10) };
            try
            {
                cbDept.DisplayMemberPath = "DepartmentName";
                cbDept.SelectedValuePath = "Id";
                cbDept.ItemsSource = _db.Departments.OrderBy(d => d.DepartmentName).ToList();
                if (cbDept.Items.Count > 0) cbDept.SelectedIndex = 0;
            }
            catch { }
            sp.Children.Add(cbDept);

            sp.Children.Add(Lbl("Ngày trình bày:"));
            var dp = new DatePicker { FontSize = 13, Margin = new Thickness(0, 0, 0, 10), SelectedDate = DateTime.Today };
            sp.Children.Add(dp);

            sp.Children.Add(Lbl("Trạng thái:"));
            var cbStatus = new ComboBox { FontSize = 13, Margin = new Thickness(0, 0, 0, 10) };
            cbStatus.Items.Add("Planned");
            cbStatus.Items.Add("Completed");
            cbStatus.SelectedIndex = 0;
            sp.Children.Add(cbStatus);

            sp.Children.Add(Lbl("Mô tả:"));
            var txtDesc = new TextBox
            {
                FontSize = 13, Height = 60, TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true, Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(8, 6, 8, 6)
            };
            sp.Children.Add(txtDesc);

            var btn = new Button
            {
                Content = "Lưu chuyên đề",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C3AED")),
                Foreground = Brushes.White, Padding = new Thickness(12, 8, 12, 8),
                FontSize = 14, FontWeight = FontWeights.SemiBold, BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 6, 0, 0)
            };
            btn.Click += (_, __) =>
            {
                try
                {
                    _db.ProfessionalTopics.Add(new ProfessionalTopic
                    {
                        TopicTitle = txtTitle.Text.Trim(),
                        Presenter = txtPresenter.Text.Trim(),
                        DepartmentId = cbDept.SelectedValue is int did ? did : 0,
                        PresentationDate = dp.SelectedDate ?? DateTime.Today,
                        Status = cbStatus.SelectedItem?.ToString() ?? "Planned",
                        Description = txtDesc.Text.Trim()
                    });
                    _db.SaveChanges();
                    dlg.Close();
                    LoadData();
                }
                catch (Exception ex)
                {
                    Log.Error("[ProfTopic] Add err: {Err}", ex.Message);
                    MessageBox.Show(ex.Message);
                }
            };
            sp.Children.Add(btn);
            sv.Content = sp;
            dlg.Content = sv;
            dlg.ShowDialog();
        }

        private void BtnRate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button btnRate || btnRate.Tag is not int topicId) return;

                var topic = _db.ProfessionalTopics.Find(topicId);
                if (topic == null) { MessageBox.Show("Không tìm thấy chuyên đề."); return; }

                var dlg = new Window
                {
                    Title = $"Đánh giá: {topic.TopicTitle}",
                    Width = 380, Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    FontFamily = new FontFamily("Segoe UI"),
                    ResizeMode = ResizeMode.NoResize
                };
                var sp = new StackPanel { Margin = new Thickness(20) };

                sp.Children.Add(Header("ĐÁNH GIÁ CHUYÊN ĐỀ"));

                sp.Children.Add(new TextBlock
                {
                    Text = topic.TopicTitle, FontSize = 14, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
                    TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10)
                });

                sp.Children.Add(Lbl($"Điểm hiện tại: {topic.AverageRating:F1}/5 ({topic.EvaluatorCount} lượt)"));

                // Star buttons
                var starPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 14) };
                int selectedStar = 0;
                var starButtons = new Button[5];
                for (int i = 0; i < 5; i++)
                {
                    int starVal = i + 1;
                    var starBtn = new Button
                    {
                        Content = "☆", FontSize = 28, Width = 44, Height = 44,
                        Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB")),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };
                    starButtons[i] = starBtn;
                    starBtn.Click += (_, __) =>
                    {
                        selectedStar = starVal;
                        for (int j = 0; j < 5; j++)
                        {
                            starButtons[j].Content = j < starVal ? "⭐" : "☆";
                            starButtons[j].Foreground = j < starVal
                                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"))
                                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB"));
                        }
                    };
                    starPanel.Children.Add(starBtn);
                }
                sp.Children.Add(starPanel);

                var btnSave = new Button
                {
                    Content = "Gửi đánh giá",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                    Foreground = Brushes.White, Padding = new Thickness(12, 8, 12, 8),
                    FontSize = 14, FontWeight = FontWeights.SemiBold, BorderThickness = new Thickness(0)
                };
                btnSave.Click += (_, __) =>
                {
                    try
                    {
                        if (selectedStar == 0) { MessageBox.Show("Vui lòng chọn số sao."); return; }

                        // Recalculate average
                        double totalOld = topic.AverageRating * topic.EvaluatorCount;
                        topic.EvaluatorCount += 1;
                        topic.AverageRating = (totalOld + selectedStar) / topic.EvaluatorCount;
                        _db.SaveChanges();
                        dlg.Close();
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("[ProfTopic] Rate err: {Err}", ex.Message);
                        MessageBox.Show(ex.Message);
                    }
                };
                sp.Children.Add(btnSave);
                dlg.Content = sp;
                dlg.ShowDialog();
            }
            catch (Exception ex) { Log.Error("[ProfTopic] BtnRate err: {Err}", ex.Message); }
        }

        // --- Helpers ---
        private static TextBlock Lbl(string t) => new TextBlock
        {
            Text = t, Foreground = Brushes.Gray, FontSize = 12,
            Margin = new Thickness(0, 0, 0, 4)
        };

        private static TextBox Txt(string def) => new TextBox
        {
            Text = def, FontSize = 13,
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(8, 6, 8, 6)
        };

        private static TextBlock Header(string t) => new TextBlock
        {
            Text = t, FontSize = 14, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
            Margin = new Thickness(0, 6, 0, 10)
        };
    }
}
