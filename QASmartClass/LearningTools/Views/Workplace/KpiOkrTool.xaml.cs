using System;
using System.Collections.Generic;
using QASmartClass.LearningTools.Helpers;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Workplace
{
    public class KpiOkrItemStateData
    {
        public string Name { get; set; } = "";
        public string Target { get; set; } = "";
        public string Actual { get; set; } = "";
    }

    public class KpiOkrStateData
    {
        public string Objective { get; set; } = "";
        public List<KpiOkrItemStateData> Items { get; set; } = new List<KpiOkrItemStateData>();
    }

    public partial class KpiOkrTool : UserControl
    {
        private bool _isInitialized = false;
        private bool _isLoadingState = false;
        private double _totalPct = 0;
        private bool _hasPlayedSuccessSound = false;

        public KpiOkrTool()
        {
            InitializeComponent();
            var toolInfo = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetToolInfo("kpi_okr");
            if (txtToolMeaning != null) txtToolMeaning.Text = toolInfo.Meaning;
            if (txtToolApplication != null) txtToolApplication.Text = toolInfo.RealWorldApplication;
            
            Loaded += (_, _) =>
            {
                TouchTextPad.Attach(txtObjective, mode: "text");
                LoadPracticalApps();
                if (!_isInitialized)
                {
                    LoadSavedState();
                    _isInitialized = true;
                }
            };

            txtObjective.LostFocus += (_, _) => { Update_Click(null, null); SaveCurrentState(); };

            if (trackTotal != null)
            {
                trackTotal.SizeChanged += (s, e) => UpdateTotalProgressBar();
                trackTotal.Loaded += (s, e) => UpdateTotalProgressBar();
            }
        }

        private void LoadPracticalApps()
        {
            try
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                string suffix = isVN ? "VN" : "EN";

                var items = new List<PracticalAppItem>
                {
                    new PracticalAppItem
                    {
                        Icon = "🎒",
                        Title = isVN ? "Mục tiêu Học tập Cá nhân" : "Personal Study OKR",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Resources/Images/okr_study_personal_{suffix}.png",
                        Description = isVN
                            ? "Giúp học sinh tự lên kế hoạch học tập cá nhân, theo dõi tiến độ ôn thi học kỳ hoặc các chứng chỉ ngoại ngữ thông qua các chỉ số đo lường cụ thể."
                            : "Helps students plan personal studies, track exam preparation, or language certifications through specific key results."
                    },
                    new PracticalAppItem
                    {
                        Icon = "👥",
                        Title = isVN ? "Dự án Tập thể Lớp học" : "Classroom Project OKR",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Resources/Images/okr_class_project_{suffix}.png",
                        Description = isVN
                            ? "Quản lý tiến độ các dự án nghiên cứu khoa học kỹ thuật, làm bài tập nhóm hoặc các hoạt động ngoại khóa tập thể của lớp."
                            : "Manages progress of science projects, group assignments, or extracurricular activities for the classroom."
                    },
                    new PracticalAppItem
                    {
                        Icon = "👩‍🏫",
                        Title = isVN ? "Quản lý Giảng dạy & Nhà trường" : "School & Teaching Management",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Resources/Images/okr_school_mgmt_{suffix}.png",
                        Description = isVN
                            ? "Giáo viên và ban giám hiệu thiết lập mục tiêu giảng dạy, theo dõi tỷ lệ hoàn thành giáo án, chất lượng đào tạo và cải tiến học đường."
                            : "Teachers and school administrators set teaching goals, track lesson plan completion rates, education quality, and school improvements."
                    }
                };

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for KpiOkrTool: {Err}", ex.Message);
            }
        }

        private TextBox GetTextBoxByColumn(Grid row, int column)
        {
            foreach (System.Windows.UIElement child in row.Children)
            {
                if (child is TextBox tb && Grid.GetColumn(child) == column)
                {
                    return tb;
                }
            }
            return null;
        }

        private void SaveCurrentState()
        {
            if (_isLoadingState) return;
            try
            {
                var state = new KpiOkrStateData
                {
                    Objective = txtObjective.Text.Trim()
                };

                foreach (Grid row in dataPanel.Children)
                {
                    var txtName = GetTextBoxByColumn(row, 0);
                    var txtTgt = GetTextBoxByColumn(row, 1);
                    var txtAct = GetTextBoxByColumn(row, 2);

                    if (txtName == null || txtTgt == null || txtAct == null) continue;

                    state.Items.Add(new KpiOkrItemStateData
                    {
                        Name = txtName.Text.Trim(),
                        Target = txtTgt.Text.Trim(),
                        Actual = txtAct.Text.Trim()
                    });
                }

                string json = System.Text.Json.JsonSerializer.Serialize(state);
                System.Threading.Tasks.Task.Run(() => DbManager.SaveWorkplaceState("kpi_okr", json));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error saving KPI/OKR state: " + ex.Message);
            }
        }

        private void LoadSavedState()
        {
            _isLoadingState = true;
            try
            {
                string json = DbManager.LoadWorkplaceState("kpi_okr");
                if (string.IsNullOrWhiteSpace(json))
                {
                    for (int i = 0; i < 3; i++) AddDataRow();
                    Update_Click(null, null);
                    return;
                }

                var state = System.Text.Json.JsonSerializer.Deserialize<KpiOkrStateData>(json);
                if (state == null)
                {
                    for (int i = 0; i < 3; i++) AddDataRow();
                    Update_Click(null, null);
                    return;
                }

                txtObjective.Text = state.Objective;
                dataPanel.Children.Clear();
                if (state.Items != null)
                {
                    foreach (var item in state.Items)
                    {
                        AddDataRow(item.Name, item.Target, item.Actual);
                    }
                }

                if (dataPanel.Children.Count == 0)
                {
                    for (int i = 0; i < 3; i++) AddDataRow();
                }

                Update_Click(null, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading KPI/OKR state: " + ex.Message);
                for (int i = 0; i < 3; i++) AddDataRow();
                Update_Click(null, null);
            }
            finally
            {
                _isLoadingState = false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  DATA ENTRY
        // ═══════════════════════════════════════════════════════════

        private void AddRow_Click(object sender, RoutedEventArgs e) => AddDataRow();

        private void AddDataRow(string name = "", string target = "", string actual = "")
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });

            var txtName = new TextBox
            {
                Text = name, FontSize = 14, FontFamily = DS.FontPrimary,
                Padding = new Thickness(6, 6, 6, 6), Margin = new Thickness(0, 0, 4, 0)
            };
            txtName.SetResourceReference(TextBox.BackgroundProperty, "SurfaceBackground");
            txtName.SetResourceReference(TextBox.ForegroundProperty, "TextPrimary");
            txtName.SetResourceReference(TextBox.BorderBrushProperty, "BorderBrush");
            TouchTextPad.Attach(txtName, mode: "text");
            txtName.LostFocus += (_, _) => { Update_Click(null, null); SaveCurrentState(); };
            Grid.SetColumn(txtName, 0);
            row.Children.Add(txtName);

            var txtTarget = new TextBox
            {
                Text = target, FontSize = 14, FontFamily = DS.FontPrimary,
                Padding = new Thickness(6, 6, 6, 6), Margin = new Thickness(0, 0, 4, 0),
                TextAlignment = TextAlignment.Center
            };
            txtTarget.SetResourceReference(TextBox.BackgroundProperty, "SurfaceBackground");
            txtTarget.SetResourceReference(TextBox.ForegroundProperty, "TextPrimary");
            txtTarget.SetResourceReference(TextBox.BorderBrushProperty, "BorderBrush");
            TouchTextPad.Attach(txtTarget, mode: "number");
            txtTarget.LostFocus += (_, _) => { Update_Click(null, null); SaveCurrentState(); };
            Grid.SetColumn(txtTarget, 1);
            row.Children.Add(txtTarget);

            var txtActual = new TextBox
            {
                Text = actual, FontSize = 14, FontFamily = DS.FontPrimary,
                Padding = new Thickness(6, 6, 6, 6), Margin = new Thickness(0, 0, 4, 0),
                TextAlignment = TextAlignment.Center
            };
            txtActual.SetResourceReference(TextBox.BackgroundProperty, "SurfaceBackground");
            txtActual.SetResourceReference(TextBox.ForegroundProperty, "TextPrimary");
            txtActual.SetResourceReference(TextBox.BorderBrushProperty, "BorderBrush");
            TouchTextPad.Attach(txtActual, mode: "number");
            txtActual.LostFocus += (_, _) => { Update_Click(null, null); SaveCurrentState(); };
            Grid.SetColumn(txtActual, 2);
            row.Children.Add(txtActual);

            var btnDel = new Button
            {
                Content = "✕", FontSize = 13, FontWeight = FontWeights.Bold,
                Width = 28, Height = 28, Cursor = Cursors.Hand,
                BorderBrush = Brushes.Transparent,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            btnDel.SetResourceReference(Button.BackgroundProperty, "SidebarHeaderBackground");
            btnDel.SetResourceReference(Button.ForegroundProperty, "TextSecondary");
            
            var style = new Style(typeof(Border));
            style.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(14)));
            btnDel.Resources.Add(typeof(Border), style);

            btnDel.MouseEnter += (_, _) => { btnDel.Foreground = Brushes.White; btnDel.Background = Brushes.Red; };
            btnDel.MouseLeave += (_, _) => { btnDel.SetResourceReference(Button.ForegroundProperty, "TextSecondary"); btnDel.SetResourceReference(Button.BackgroundProperty, "SidebarHeaderBackground"); };
            btnDel.Click += (_, _) => 
            { 
                dataPanel.Children.Remove(row); 
                Update_Click(null, null); 
                SaveCurrentState();
            };
            Grid.SetColumn(btnDel, 3);
            row.Children.Add(btnDel);

            dataPanel.Children.Add(row);
        }

        // ═══════════════════════════════════════════════════════════
        //  UPDATE DASHBOARD
        // ═══════════════════════════════════════════════════════════

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            cardsPanel.Children.Clear();
            string obj = txtObjective.Text.Trim();
            lblObjDisplay.Text = string.IsNullOrEmpty(obj) ? "[Chưa nhập Mục tiêu]" : obj;

            double sumPct = 0;
            int validCount = 0;

            foreach (Grid row in dataPanel.Children)
            {
                var txtName = GetTextBoxByColumn(row, 0);
                var txtTgt = GetTextBoxByColumn(row, 1);
                var txtAct = GetTextBoxByColumn(row, 2);

                if (txtName == null || txtTgt == null || txtAct == null) continue;

                string name = txtName.Text.Trim();
                if (string.IsNullOrEmpty(name)) continue;

                bool tgtValid = ParsingHelper.TryParseDouble(txtTgt.Text, out double tgt);
                bool actValid = ParsingHelper.TryParseDouble(txtAct.Text, out double act);

                if (tgtValid) txtTgt.SetResourceReference(TextBox.BorderBrushProperty, "BorderBrush");
                else txtTgt.BorderBrush = Brushes.Red;

                if (actValid) txtAct.SetResourceReference(TextBox.BorderBrushProperty, "BorderBrush");
                else txtAct.BorderBrush = Brushes.Red;

                if (tgtValid && actValid)
                {
                    double pct = 0;
                    if (tgt == 0)
                    {
                        pct = (act <= 0) ? 100 : 0;
                    }
                    else
                    {
                        pct = (act / tgt) * 100;
                    }
                    
                    if (pct < 0) pct = 0;
                    
                    sumPct += pct > 100 ? 100 : pct;
                    validCount++;
                    
                    AddDashboardCard(name, act, tgt, pct);
                }
                else
                {
                    AddDashboardCard(name, 0, 0, 0, true);
                }
            }

            if (validCount > 0)
            {
                _totalPct = sumPct / validCount;
                txtTotalPct.Text = $"{_totalPct:F1}%";
                UpdateTotalProgressBar();

                // Logic ăn mừng thành tích khi tiến độ đạt 100%
                if (global::System.Math.Abs(_totalPct - 100.0) < 0.01)
                {
                    lblObjDisplay.Text = (string.IsNullOrEmpty(obj) ? "[Chưa nhập Mục tiêu]" : obj) + " 🏆";
                    if (!_hasPlayedSuccessSound)
                    {
                        SoundHelper.Play(true);
                        _hasPlayedSuccessSound = true;
                    }
                }
                else
                {
                    _hasPlayedSuccessSound = false;
                }
            }
            else
            {
                _totalPct = 0;
                txtTotalPct.Text = "0%";
                UpdateTotalProgressBar();
                _hasPlayedSuccessSound = false;
            }
        }

        private void UpdateTotalProgressBar()
        {
            if (trackTotal != null && barTotal != null)
            {
                barTotal.Width = trackTotal.ActualWidth * (_totalPct / 100.0);
                
                Color barColor;
                if (_totalPct >= 80) barColor = Color.FromRgb(46, 125, 50); // Xanh lá
                else if (_totalPct >= 50) barColor = Color.FromRgb(239, 108, 0); // Cam
                else barColor = Color.FromRgb(198, 40, 40); // Đỏ
                
                barTotal.Background = new SolidColorBrush(barColor);
            }
        }

        private void AddDashboardCard(string name, double actual, double target, double pct, bool isInvalid = false)
        {
            var card = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 12),
                BorderThickness = new Thickness(1)
            };
            card.SetResourceReference(Border.BackgroundProperty, "SurfaceBackground");
            card.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");

            var sp = new StackPanel();

            var dp = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            
            var titleTxt = new TextBlock
            {
                Text = name, FontSize = 14, FontWeight = FontWeights.Bold
            };
            titleTxt.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
            dp.Children.Add(titleTxt);

            if (!isInvalid)
            {
                var lblPct = new TextBlock
                {
                    Text = $"{pct:F1}%", FontSize = 15, FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                dp.Children.Add(lblPct);
                DockPanel.SetDock(lblPct, Dock.Right);

                Color barColor;
                if (pct >= 80) barColor = Color.FromRgb(46, 125, 50);
                else if (pct >= 50) barColor = Color.FromRgb(239, 108, 0);
                else barColor = Color.FromRgb(198, 40, 40);

                lblPct.Foreground = new SolidColorBrush(barColor);

                var track = new Border { Height = 12, CornerRadius = new CornerRadius(6), Margin = new Thickness(0, 0, 0, 8) };
                track.SetResourceReference(Border.BackgroundProperty, "SidebarBackground");
                double wPct = pct > 100 ? 100 : pct;
                var fill = new Border { HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(6), Background = new SolidColorBrush(barColor) };
                
                track.Loaded += (s, e) => fill.Width = track.ActualWidth * (wPct / 100.0);
                track.SizeChanged += (s, e) => fill.Width = track.ActualWidth * (wPct / 100.0);
                
                track.Child = fill;
                
                sp.Children.Add(dp);
                sp.Children.Add(track);

                var detailTxt = new TextBlock
                {
                    Text = $"Thực tế: {actual} / Mục tiêu: {target}", FontSize = 13, HorizontalAlignment = HorizontalAlignment.Right
                };
                detailTxt.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");
                sp.Children.Add(detailTxt);
            }
            else
            {
                var lblErr = new TextBlock { Text = "Chưa có số liệu", FontSize = 13, Foreground = Brushes.Red, HorizontalAlignment = HorizontalAlignment.Right };
                dp.Children.Add(lblErr);
                DockPanel.SetDock(lblErr, Dock.Right);
                sp.Children.Add(dp);
            }

            card.Child = sp;
            cardsPanel.Children.Add(card);
        }

        // ═══════════════════════════════════════════════════════════
        //  TEMPLATES & ACTIONS
        // ═══════════════════════════════════════════════════════════

        private void Template_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var templates = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetTemplates("kpi_okr");
            var groups = new Dictionary<string, MenuItem>();

            foreach (var t in templates)
            {
                if (!groups.TryGetValue(t.Category, out var parentItem))
                {
                    parentItem = new MenuItem { Header = t.Category, FontSize = 12, FontWeight = FontWeights.Bold };
                    groups[t.Category] = parentItem;
                    menu.Items.Add(parentItem);
                }

                var item = new MenuItem { Header = t.Name, FontSize = 12, FontWeight = FontWeights.Normal };
                var data = (QASmartClass.LearningTools.Helpers.KpiOkrData)t.Data;
                item.Click += (_, _) =>
                {
                    if (sideMenu != null && sideMenu.SelectedIndex != 1)
                    {
                        sideMenu.SelectedIndex = 0;
                    }
                    _hasPlayedSuccessSound = false; // Reset cờ âm thanh
                    txtObjective.Text = data.Obj;
                    dataPanel.Children.Clear();
                    foreach (var d in data.Data) AddDataRow(d.Name, d.Tgt, d.Act);
                    Update_Click(null, null);
                    SaveCurrentState();
                };
                parentItem.Items.Add(item);
            }

            menu.PlacementTarget = (Button)sender;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 1)
            {
                sideMenu.SelectedIndex = 1;
                // Chờ layout cập nhật xong mới thực hiện chụp hình xuất ảnh
                Dispatcher.InvokeAsync(() => RunExport(), System.Windows.Threading.DispatcherPriority.Background);
                return;
            }
            RunExport();
        }

        private void RunExport()
        {
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "PNG Image|*.png", FileName = $"Dashboard_KPI_OKR_{DateTime.Now:yyyyMMdd_HHmmss}.png" };
                if (dlg.ShowDialog() == true)
                {
                    var target = dashboardBorder;
                    Size originalSize = target.RenderSize;
                    
                    // Tạm ẩn thanh cuộn dọc để tránh xuất hiện trong ảnh PNG kết xuất
                    var originalScrollVisibility = scrollDashboard.VerticalScrollBarVisibility;
                    scrollDashboard.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                    
                    try
                    {
                        // Đo đạc và xếp đặt với chiều dọc vô hạn để mở rộng toàn bộ ScrollViewer chứa các card
                        target.Measure(new Size(target.ActualWidth, double.PositiveInfinity));
                        target.Arrange(new Rect(new Point(0, 0), target.DesiredSize));
                        target.UpdateLayout();
                        
                        var bounds = new Rect(target.RenderSize);
                        var rtb = new RenderTargetBitmap((int)bounds.Width, (int)bounds.Height, 96, 96, PixelFormats.Pbgra32);
                        var dv = new DrawingVisual();
                        using (var dc = dv.RenderOpen()) 
                        { 
                            // Vẽ màu nền trắng đục đè lên để tránh nền trong suốt trong PNG
                            dc.DrawRectangle(Brushes.White, null, new Rect(new Point(), bounds.Size));
                            dc.DrawRectangle(new VisualBrush(target), null, new Rect(new Point(), bounds.Size)); 
                        }
                        rtb.Render(dv);
                        
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(rtb));
                        using var stream = File.Create(dlg.FileName);
                        encoder.Save(stream);
                        MessageBox.Show($"Đã xuất ảnh Dashboard thành công:\n{dlg.FileName}", "Lưu thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    finally
                    {
                        // Luôn khôi phục kích thước gốc của layout và trạng thái thanh cuộn tránh làm hỏng hiển thị UI chính
                        target.Measure(originalSize);
                        target.Arrange(new Rect(new Point(0, 0), originalSize));
                        scrollDashboard.VerticalScrollBarVisibility = originalScrollVisibility;
                        target.UpdateLayout();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"Không thể xuất ảnh: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Xóa toàn bộ dữ liệu đo lường hiện tại và bắt đầu lại?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _hasPlayedSuccessSound = false; // Reset cờ âm thanh
                txtObjective.Text = "";
                dataPanel.Children.Clear();
                cardsPanel.Children.Clear();
                for (int i = 0; i < 3; i++) AddDataRow();
                Update_Click(null, null);
                SaveCurrentState();
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewWorkspace == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewWorkspace.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewWorkspace.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}