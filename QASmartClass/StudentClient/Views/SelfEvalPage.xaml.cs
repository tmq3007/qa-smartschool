using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QASmartClass.StudentClient.Views
{
    public partial class SelfEvalPage : Page
    {
        private AppDbContext? _db;
        private int _currentStudentId = 0;
        private string _currentSemester = "";

        private bool _isInitializing = false;

        public SelfEvalPage()
        {
            InitializeComponent();
            
            SldTuChu.ValueChanged += (s, ev) => DrawRadarChart();
            SldGiaoTiep.ValueChanged += (s, ev) => DrawRadarChart();
            SldSangTao.ValueChanged += (s, ev) => DrawRadarChart();
            SldNhanAi.ValueChanged += (s, ev) => DrawRadarChart();
            SldChamChi.ValueChanged += (s, ev) => DrawRadarChart();
            SldTrachNhiem.ValueChanged += (s, ev) => DrawRadarChart();

            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _db?.Dispose();
            _db = null;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _db = new AppDbContext();
            var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(_db);
            _currentStudentId = identityService.GetCurrentStudent().Id;

            // Nạp học kỳ vào ComboBox trước
            _isInitializing = true;
            CbSemester.Items.Clear();
            CbSemester.Items.Add("HK1_2025-2026");
            CbSemester.Items.Add("HK2_2025-2026");
            CbSemester.SelectedIndex = 0; // Mặc định chọn HK1
            _currentSemester = CbSemester.SelectedItem.ToString()!;
            _isInitializing = false;

            if (_currentStudentId <= 0) 
            { 
                MessageBox.Show("Vui lòng đăng nhập hệ thống để tự đánh giá năng lực phẩm chất!", "Yêu cầu đăng nhập", MessageBoxButton.OK, MessageBoxImage.Warning);
                // Khóa tương tác UI
                SldTuChu.IsEnabled = false;
                SldGiaoTiep.IsEnabled = false;
                SldSangTao.IsEnabled = false;
                SldNhanAi.IsEnabled = false;
                SldChamChi.IsEnabled = false;
                SldTrachNhiem.IsEnabled = false;
                BtnSave.IsEnabled = false;
                return; 
            }

            LoadEvaluation();
            DrawRadarChart();
        }

        private void CbSemester_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbSemester.SelectedItem != null && _currentStudentId > 0 && !_isInitializing)
            {
                _currentSemester = CbSemester.SelectedItem.ToString()!;
                LoadEvaluation();
                DrawRadarChart();
            }
        }

        private void DrawRadarChart()
        {
            if (RadarCanvas == null || _isInitializing) return;
            RadarCanvas.Children.Clear();

            double cx = 140, cy = 140, maxR = 120;
            int axes = 6;
            double[] values = {
                SldTuChu.Value, SldGiaoTiep.Value, SldSangTao.Value,
                SldNhanAi.Value, SldChamChi.Value, SldTrachNhiem.Value
            };

            // Draw grid circles (levels 1-4)
            for (int level = 1; level <= 4; level++)
            {
                double r = maxR * level / 4.0;
                var ellipse = new Ellipse
                {
                    Width = r * 2, Height = r * 2,
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
                    StrokeThickness = 1, Fill = Brushes.Transparent
                };
                Canvas.SetLeft(ellipse, cx - r);
                Canvas.SetTop(ellipse, cy - r);
                RadarCanvas.Children.Add(ellipse);
            }

            // Draw axis lines
            for (int i = 0; i < axes; i++)
            {
                double angle = Math.PI * 2 * i / axes - Math.PI / 2;
                var line = new Line
                {
                    X1 = cx, Y1 = cy,
                    X2 = cx + maxR * Math.Cos(angle),
                    Y2 = cy + maxR * Math.Sin(angle),
                    Stroke = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                    StrokeThickness = 1
                };
                RadarCanvas.Children.Add(line);
            }

            // Draw student polygon (blue)
            DrawPolygon(cx, cy, maxR, axes, values, Color.FromArgb(80, 59, 130, 246), Color.FromArgb(180, 59, 130, 246));
        }

        private void DrawPolygon(double cx, double cy, double maxR, int axes, double[] values, Color fill, Color stroke)
        {
            var points = new PointCollection();
            for (int i = 0; i < axes; i++)
            {
                double angle = Math.PI * 2 * i / axes - Math.PI / 2;
                double r = maxR * values[i] / 4.0;
                points.Add(new Point(cx + r * Math.Cos(angle), cy + r * Math.Sin(angle)));
            }
            var polygon = new Polygon
            {
                Points = points,
                Fill = new SolidColorBrush(fill),
                Stroke = new SolidColorBrush(stroke),
                StrokeThickness = 2
            };
            RadarCanvas.Children.Add(polygon);
        }

        private void LoadEvaluation()
        {
            try
            {
                var eval = _db.SelfEvaluations.FirstOrDefault(s => s.StudentId == _currentStudentId && s.Semester == _currentSemester);
                if (eval != null)
                {
                    _isInitializing = true;
                    var comp = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, int>>(eval.Competency);
                    if (comp != null)
                    {
                        if (comp.ContainsKey("TuChuTuHoc")) SldTuChu.Value = comp["TuChuTuHoc"];
                        if (comp.ContainsKey("GiaoTiep")) SldGiaoTiep.Value = comp["GiaoTiep"];
                        if (comp.ContainsKey("SangTao")) SldSangTao.Value = comp["SangTao"];
                    }

                    var character = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, int>>(eval.Character);
                    if (character != null)
                    {
                        if (character.ContainsKey("NhanAi")) SldNhanAi.Value = character["NhanAi"];
                        if (character.ContainsKey("ChamChi")) SldChamChi.Value = character["ChamChi"];
                        if (character.ContainsKey("TrachNhiem")) SldTrachNhiem.Value = character["TrachNhiem"];
                    }
                    _isInitializing = false;
                }
                else
                {
                    _isInitializing = true;
                    // Reset all sliders to default value (2)
                    SldTuChu.Value = 2;
                    SldGiaoTiep.Value = 2;
                    SldSangTao.Value = 2;
                    SldNhanAi.Value = 2;
                    SldChamChi.Value = 2;
                    SldTrachNhiem.Value = 2;
                    _isInitializing = false;
                    DrawRadarChart();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi load SelfEvaluation");
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStudentId <= 0)
            {
                MessageBox.Show("Vui lòng đăng nhập!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var comp = new System.Collections.Generic.Dictionary<string, int>
                {
                    { "TuChuTuHoc", (int)SldTuChu.Value },
                    { "GiaoTiep", (int)SldGiaoTiep.Value },
                    { "SangTao", (int)SldSangTao.Value }
                };

                var character = new System.Collections.Generic.Dictionary<string, int>
                {
                    { "NhanAi", (int)SldNhanAi.Value },
                    { "ChamChi", (int)SldChamChi.Value },
                    { "TrachNhiem", (int)SldTrachNhiem.Value }
                };

                var eval = _db.SelfEvaluations.FirstOrDefault(s => s.StudentId == _currentStudentId && s.Semester == _currentSemester);
                if (eval == null)
                {
                    eval = new SelfEvaluation
                    {
                        StudentId = _currentStudentId,
                        Semester = _currentSemester,
                        Competency = JsonSerializer.Serialize(comp),
                        Character = JsonSerializer.Serialize(character)
                    };
                    _db.SelfEvaluations.Add(eval);
                }
                else
                {
                    eval.Competency = JsonSerializer.Serialize(comp);
                    eval.Character = JsonSerializer.Serialize(character);
                }

                _db.SaveChanges();
                MessageBox.Show("Đã ghi nhận kết quả Tự đánh giá thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi lưu SelfEvaluation.");
                MessageBox.Show("Có lỗi xảy ra khi lưu đánh giá.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

