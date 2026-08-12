using QASmartClass.LearningTools.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Workplace
{
    public partial class FishboneTool : UserControl
    {
        private bool _isVN = true;

        public FishboneTool()
        {
            InitializeComponent();
            _isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";

            var toolInfo = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetToolInfo("fishbone");
            if (txtToolMeaning != null) txtToolMeaning.Text = toolInfo.Meaning;
            if (txtToolApplication != null) txtToolApplication.Text = toolInfo.RealWorldApplication;
            Loaded += (_, _) =>
            {
                TouchTextPad.Attach(txtProblem, mode: "text");
                TouchTextPad.Attach(txtCat1, mode: "text");
                TouchTextPad.Attach(txtCat2, mode: "text");
                TouchTextPad.Attach(txtCat3, mode: "text");
                TouchTextPad.Attach(txtCat4, mode: "text");
                TouchTextPad.Attach(txtCat5, mode: "text");
                TouchTextPad.Attach(txtCat6, mode: "text");

                LoadPracticalApps();
            };
        }

        private void AddCat1_Click(object sender, RoutedEventArgs e) => AddCause(panelCat1, "#1565C0");
        private void AddCat2_Click(object sender, RoutedEventArgs e) => AddCause(panelCat2, "#2E7D32");
        private void AddCat3_Click(object sender, RoutedEventArgs e) => AddCause(panelCat3, "#E65100");
        private void AddCat4_Click(object sender, RoutedEventArgs e) => AddCause(panelCat4, "#6A1B9A");
        private void AddCat5_Click(object sender, RoutedEventArgs e) => AddCause(panelCat5, "#AD1457");
        private void AddCat6_Click(object sender, RoutedEventArgs e) => AddCause(panelCat6, "#455A64");

        private void AddCause(StackPanel panel, string fgHex, string text = "")
        {
            var fg = (Color)ColorConverter.ConvertFromString(fgHex);
            var row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Cải tiến: Thêm hoạt ảnh mượt mà khi dòng nguyên nhân xuất hiện
            row.Opacity = 0;
            var anim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.2));
            row.BeginAnimation(UIElement.OpacityProperty, anim);

            var bullet = new TextBlock
            {
                Text = "•", FontSize = 16, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(fg), Margin = new Thickness(2, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(bullet, 0);
            row.Children.Add(bullet);

            var tb = new TextBox
            {
                Text = text, FontSize = 14, FontFamily = DS.FontPrimary,
                Padding = new Thickness(6, 4, 6, 4),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, fg.R, fg.G, fg.B)),
                BorderThickness = new Thickness(1), Background = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                MaxLength = 120 // Hạn chế nhập quá dài gây lỗi vỡ khung hình
            };
            TouchTextPad.Attach(tb, mode: "text");
            Grid.SetColumn(tb, 1);
            row.Children.Add(tb);

            // Cải tiến: Bọc nút xóa trong Border để mở rộng diện tích điểm chạm tương tác (Hit-test)
            var delWrapper = new Border
            {
                Width = 28, Height = 28, Background = Brushes.Transparent,
                Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 2, 0)
            };
            var del = new TextBlock
            {
                Text = "✕", FontSize = 14, Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            delWrapper.Child = del;
            delWrapper.MouseEnter += (_, _) => del.Foreground = Brushes.Red;
            delWrapper.MouseLeave += (_, _) => del.Foreground = Brushes.Gray;
            delWrapper.MouseLeftButtonDown += (_, _) => panel.Children.Remove(row);
            Grid.SetColumn(delWrapper, 2);
            row.Children.Add(delWrapper);

            panel.Children.Add(row);
            if (string.IsNullOrEmpty(text))
                tb.Loaded += (_, _) => tb.Focus();
        }

        private void Template_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var templates = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetTemplates("fishbone");
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
                var data = (QASmartClass.LearningTools.Helpers.FishboneData)t.Data;
                item.Click += (_, _) =>
                {
                    ClearAll();
                    // Đặt tạm tiêu đề của cả 6 nhóm về trống để chuẩn bị nhận dữ liệu từ template
                    txtCat1.Text = "";
                    txtCat2.Text = "";
                    txtCat3.Text = "";
                    txtCat4.Text = "";
                    txtCat5.Text = "";
                    txtCat6.Text = "";

                    txtProblem.Text = data.MainProblem;
                    var panels = new[] { panelCat1, panelCat2, panelCat3, panelCat4, panelCat5, panelCat6 };
                    var titles = new[] { txtCat1, txtCat2, txtCat3, txtCat4, txtCat5, txtCat6 };
                    var colors = new[] { "#1565C0", "#2E7D32", "#E65100", "#6A1B9A", "#AD1457", "#455A64" };
                    int i = 0;
                    foreach(var kvp in data.Groups) {
                        if(i < 6) {
                            titles[i].Text = kvp.Key;
                            foreach(var cause in kvp.Value) AddCause(panels[i], colors[i], cause);
                            i++;
                        }
                    }
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
                sideMenu.SelectedIndex = 0;
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
                var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "PNG Image|*.png", FileName = $"Fishbone_{DateTime.Now:yyyyMMdd_HHmmss}.png" };
                if (dlg.ShowDialog() == true)
                {
                    var target = mainPanel;
                    var bounds = new Rect(target.RenderSize);
                    
                    // Phản biện an toàn: Tránh lỗi ArgumentException khi chiều rộng hoặc chiều cao bằng 0
                    if (bounds.Width <= 0 || bounds.Height <= 0)
                    {
                        MessageBox.Show("Không thể xuất ảnh do vùng hiển thị chưa sẵn sàng.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var rtb = new RenderTargetBitmap((int)bounds.Width, (int)bounds.Height, 96, 96, PixelFormats.Pbgra32);
                    var dv = new DrawingVisual();
                    using (var dc = dv.RenderOpen()) 
                    {
                        // Cải tiến: Vẽ nền trắng đục đè trước để chống trong suốt ở góc ảnh
                        dc.DrawRectangle(Brushes.White, null, new Rect(new Point(), bounds.Size));
                        dc.DrawRectangle(new VisualBrush(target), null, new Rect(new Point(), bounds.Size)); 
                    }
                    rtb.Render(dv);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    using var stream = File.Create(dlg.FileName);
                    encoder.Save(stream);
                    MessageBox.Show($"Đã lưu: {dlg.FileName}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Xóa toàn bộ?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                ClearAll();
        }

        private void ClearAll()
        {
            txtProblem.Text = "";
            // Khôi phục nhãn tiêu đề mặc định chuẩn giáo dục và thống nhất với hướng dẫn sử dụng
            txtCat1.Text = _isVN ? "Công cụ / Máy móc (Machine)" : "Machine (Tool)";
            txtCat2.Text = _isVN ? "Phương pháp (Method)" : "Method";
            txtCat3.Text = _isVN ? "Tài liệu / Học liệu (Material)" : "Material (Resource)";
            txtCat4.Text = _isVN ? "Con người (Man)" : "Man (People)";
            txtCat5.Text = _isVN ? "Đánh giá / Kiểm tra (Measurement)" : "Measurement (Evaluation)";
            txtCat6.Text = _isVN ? "Môi trường (Environment)" : "Environment";

            panelCat1.Children.Clear();
            panelCat2.Children.Clear();
            panelCat3.Children.Clear();
            panelCat4.Children.Clear();
            panelCat5.Children.Clear();
            panelCat6.Children.Clear();
        }

        private void LoadPracticalApps()
        {
            try
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                string suffix = isVN ? "VN" : "EN";

                if (!isVN)
                {
                    if (menuTextGuide != null) menuTextGuide.Text = "Guide & Workflow";
                    if (menuTextWorkspace != null) menuTextWorkspace.Text = "Analysis Workspace";
                    if (menuTextPractical != null) menuTextPractical.Text = "Practical Applications";
                    if (expGuide != null) expGuide.Header = "📖 User Guide & Pedagogical Notes";

                    if (lblOriginTitle != null) lblOriginTitle.Text = "💡 ORIGIN & MEANING";
                    if (lblAppTitle != null) lblAppTitle.Text = "🌍 REAL-WORLD APPLICATION";

                    if (lblGuideExampleTitle != null) lblGuideExampleTitle.Text = "📝 VISUAL ANALYSIS EXAMPLE (LEARNING)";
                    if (lblGuideExampleProblem != null) lblGuideExampleProblem.Text = "Fish head problem: Chemistry semester exam result dropped";
                    if (gManLabel != null) gManLabel.Text = "• Man:";
                    if (gManVal != null) gManVal.Text = "Student lacked focus during theory class, hesitated to do homework.";
                    if (gMethodLabel != null) gMethodLabel.Text = "• Method:";
                    if (gMethodVal != null) gMethodVal.Text = "Rote learning formulas, did not connect theory with actual experiments.";
                    if (gMachineLabel != null) gMachineLabel.Text = "• Machine:";
                    if (gMachineVal != null) gMachineVal.Text = "Calculator keys got stuck; lacked virtual lab software for visual learning.";
                    if (gMaterialLabel != null) gMaterialLabel.Text = "• Material:";
                    if (gMaterialVal != null) gMaterialVal.Text = "Exercise book lacked detailed answers; did not make summary cheat sheets.";
                    if (gMeasurementLabel != null) gMeasurementLabel.Text = "• Measurement:";
                    if (gMeasurementVal != null) gMeasurementVal.Text = "Multiple-choice exam required fast reflexes, time allocation was poor.";
                    if (gEnvironmentLabel != null) gEnvironmentLabel.Text = "• Environment:";
                    if (gEnvironmentVal != null) gEnvironmentVal.Text = "Home study area was noisy near the street, desk lamp light was dim.";

                    if (lblWorkflowTitle != null) lblWorkflowTitle.Text = "🛠️ VISUAL WORKFLOW WORKSTEP";
                    if (step1Header != null) step1Header.Text = "Step 1";
                    if (step1Title != null) step1Title.Text = "📋 Problem";
                    if (step1Desc != null) step1Desc.Text = "Identify and enter the core problem to be solved into the right 'Fish head' box.";

                    if (step2Header != null) step2Header.Text = "Step 2";
                    if (step2Title != null) step2Title.Text = "🏷️ 6M Categories";
                    if (step2Desc != null) step2Desc.Text = "Double-click the 6 bone labels to customize group titles to fit your context.";

                    if (step3Header != null) step3Header.Text = "Step 3";
                    if (step3Title != null) step3Title.Text = "✍️ Add Causes";
                    if (step3Desc != null) step3Desc.Text = "Click the '+ Add' button to record specific causes within each group.";

                    if (step4Header != null) step4Header.Text = "Step 4";
                    if (step4Title != null) step4Title.Text = "📷 Export & Review";
                    if (step4Desc != null) step4Desc.Text = "Analyze to identify the root cause and export PNG for study documentation.";

                    if (txtCat1 != null) txtCat1.Text = "Machine (Tool)";
                    if (txtCat2 != null) txtCat2.Text = "Method";
                    if (txtCat3 != null) txtCat3.Text = "Material (Resource)";
                    if (txtCat4 != null) txtCat4.Text = "Man (People)";
                    if (txtCat5 != null) txtCat5.Text = "Measurement (Evaluation)";
                    if (txtCat6 != null) txtCat6.Text = "Environment";
                }

                var items = new List<PracticalAppItem>
                {
new PracticalAppItem
                    {
                        Icon = "✍",
                        Title = isVN ? "Phương pháp học tập không hiệu quả" : "Manufacturing Defect Analysis",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fishbone_1_{suffix}.png",
                        Description = isVN 
                            ? "Học sinh gặp khó khăn trong việc hấp thu kiến thức trên lớp.&#x0a;• Con người: Thiếu tập trung, lười tự học.&#x0a;• Phương pháp: Học vẹt, không làm sơ đồ tư duy.&#x0a;• Công cụ: Thiếu máy tính hỗ trợ tra cứu bài giảng.&#x0a;• Học liệu: Sách bài tập thiếu phần giải thích rõ ràng.&#x0a;• Đánh giá: Chưa có thói quen tự kiểm tra năng lực.&#x0a;• Môi trường: Góc tự học ở nhà bị ồn và thiếu ánh sáng." 
                            : "Trace causes of defective products back to 6Ms (Man, Machine, Material, Method, Measurement, Mother Nature)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏫",
                        Title = isVN ? "Lớp học thường bị mất trật tự" : "Software Bug Debugging",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fishbone_2_{suffix}.png",
                        Description = isVN 
                            ? "Lớp học thường xuyên ồn ào ảnh hưởng chất lượng bài học.&#x0a;• Con người: Học sinh nghịch ngợm, ban cán sự lớp nể nang.&#x0a;• Phương pháp: Tiết giảng chưa đủ cuốn hút, hoạt động nhóm lộn xộn.&#x0a;• Công cụ: Micro giáo viên bị rè, hỏng loa lớp.&#x0a;• Học liệu: Nội quy lớp quá dài, thiếu bảng thi đua trực quan.&#x0a;• Đánh giá: Thiếu quy trình chấm điểm nề nếp hàng tuần giữa các tổ.&#x0a;• Môi trường: Phòng học quá nóng bức do thiếu quạt." 
                            : "Analyze root causes of application crashes by categorizing factors into code errors, server load, or database issues."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📉",
                        Title = isVN ? "Kết quả thi học kỳ bị sa sút" : "Customer Service Improvement",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fishbone_3_{suffix}.png",
                        Description = isVN 
                            ? "Học sinh bị điểm kém trong kỳ thi học kỳ.&#x0a;• Con người: Ôn tập muộn, tâm lý lo lắng, mất bình tĩnh khi làm bài.&#x0a;• Phương pháp: Trì hoãn ôn thi, học tủ, phân bổ thời gian bài thi sai.&#x0a;• Công cụ: Máy tính cầm tay hết pin đột xuất, bút bị tắc mực.&#x0a;• Học liệu: Đề cương quá rộng, không bám sát cấu trúc đề.&#x0a;• Đánh giá: Barem điểm chấm thi quá khắt khe, không chữa lỗi sai.&#x0a;• Môi trường: Phòng thi nóng bức hoặc bị ồn ào do tiếng ồn ngoài." 
                            : "Investigate high customer complaint rates by brainstorming root causes in staff training, software, or delivery policies."
                    },
                    new PracticalAppItem
                    {
                        Icon = "♻",
                        Title = isVN ? "Rác thải nhựa tăng cao ở trường" : "Project Delay Diagnostics",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fishbone_4_{suffix}.png",
                        Description = isVN 
                            ? "Lượng rác thải nhựa phát sinh hàng ngày tăng nhanh chóng.&#x0a;• Con người: Chưa có ý thức phân loại rác, thói quen ăn quà vặt.&#x0a;• Phương pháp: Chưa phát động phong trào gom rác tái chế có thưởng.&#x0a;• Công cụ: Thiếu thùng rác phân loại, thiếu chổi dọn vệ sinh.&#x0a;• Học liệu: Thiếu tranh ảnh, áp phích giáo dục bảo vệ môi trường.&#x0a;• Đánh giá: Không đưa tiêu chí xanh sạch vào thi đua xếp hạng lớp.&#x0a;• Môi trường: Căng-tin trường dùng túi nilon và hộp xốp nhiều." 
                            : "Diagnose reasons for missed milestones by grouping causes into budget shortages, poor communication, or scope creep."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⏱",
                        Title = isVN ? "Trễ hạn nộp bài tập về nhà" : "Academic Performance Analysis",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fishbone_5_{suffix}.png",
                        Description = isVN 
                            ? "Nộp bài tập muộn so với yêu cầu của giáo viên bộ môn.&#x0a;• Con người: Thói quen trì hoãn, mải chơi game quên giờ giấc.&#x0a;• Phương pháp: Không ghi danh sách việc cần làm, không xếp thứ tự ưu tiên.&#x0a;• Công cụ: Không cài đặt nhắc hẹn trên điện thoại hay đồng hồ.&#x0a;• Học liệu: Bài tập của các môn giao dồn dập cùng thời điểm.&#x0a;• Đánh giá: Giáo viên nương tay, chưa có hình thức kỷ luật nộp muộn.&#x0a;• Môi trường: Góc học tập quá gần Tivi, giường ngủ nên dễ mất tập trung." 
                            : "Help students analyze poor test grades by looking at study habits, materials, sleep quality, and exam time management."
                    },
                    new PracticalAppItem
                    {
                        Icon = "👥",
                        Title = isVN ? "Dự án học tập nhóm bị thất bại" : "Medical Error Prevention",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fishbone_6_{suffix}.png",
                        Description = isVN 
                            ? "Bài báo cáo nhóm bị điểm kém hoặc trễ hạn nộp.&#x0a;• Con người: Các thành viên đùn đẩy, trưởng nhóm thiếu kỹ năng điều hành.&#x0a;• Phương pháp: Không chia việc rõ ràng, không đặt mốc kiểm soát tiến độ.&#x0a;• Công cụ: Không sử dụng Google Docs hay Trello làm việc chung trực tuyến.&#x0a;• Học liệu: Tài liệu hướng dẫn làm dự án của giáo viên bị mơ hồ.&#x0a;• Đánh giá: Cách đánh giá chia đều điểm cho cả nhóm, không công bằng.&#x0a;• Môi trường: Nhà xa nhau, khó sắp xếp lịch họp trực tiếp ngoài giờ." 
                            : "Examine hospital medication errors by checking nurse shift handovers, labeling, storage, and prescription workflows."
                    }
            };

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for FishboneTool: {Err}", ex.Message);
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