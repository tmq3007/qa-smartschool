using QASmartClass.LearningTools.Models;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.ViewModels;
using System.Collections.Generic;

namespace QASmartClass.LearningTools.Views.Workplace
{
    public partial class PdcaTool : UserControl
    {
        private readonly PdcaViewModel _viewModel;
        private bool _isVN = true;

        public PdcaTool()
        {
            InitializeComponent();
            _viewModel = new PdcaViewModel();
            this.DataContext = _viewModel;

            // Check language
            _isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";

            var toolInfo = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetToolInfo("pdca");
            if (txtToolMeaning != null) txtToolMeaning.Text = toolInfo.Meaning;
            if (txtToolApplication != null) txtToolApplication.Text = toolInfo.RealWorldApplication;

            Loaded += (_, _) =>
            {
                TouchTextPad.Attach(txtSubject, mode: "text");
                TouchTextPad.Attach(txtPlan, mode: "text");
                TouchTextPad.Attach(txtDo, mode: "text");
                TouchTextPad.Attach(txtCheck, mode: "text");
                TouchTextPad.Attach(txtAct, mode: "text");

                // Đăng ký tự động lưu khi mất focus
                txtSubject.LostFocus += (s, e) => _viewModel.SaveState();
                txtPlan.LostFocus += (s, e) => _viewModel.SaveState();
                txtDo.LostFocus += (s, e) => _viewModel.SaveState();
                txtCheck.LostFocus += (s, e) => _viewModel.SaveState();
                txtAct.LostFocus += (s, e) => _viewModel.SaveState();

                // Load ảnh và dịch giao diện
                LoadPracticalApps();
                LocalizeUI();
            };

            Unloaded += (s, e) =>
            {
                _viewModel.SaveState();
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  CYCLE COUNTER
        // ═══════════════════════════════════════════════════════════

        private void PrevCycle_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ChangeCycle(-1);
        }

        private void NextCycle_Click(object sender, RoutedEventArgs e)
        {
            int nextCycleNum = _viewModel.CurrentCycle + 1;
            bool targetExists = _viewModel.Cycles.Any(c => c.CycleNumber == nextCycleNum);

            if (targetExists)
            {
                _viewModel.ChangeCycle(1);
            }
            else
            {
                var result = MessageBox.Show(
                    $"Bắt đầu chu kỳ {nextCycleNum}? Bạn có muốn làm mới bảng không?\n" +
                    " - Bấm [Yes] để bắt đầu với các ô trống.\n" +
                    " - Bấm [No] để kế thừa nội dung chu kỳ cũ sang làm bàn đạp.", 
                    "Chu kỳ mới", 
                    MessageBoxButton.YesNoCancel, 
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _viewModel.ChangeCycle(1, shouldClearOnNew: true);
                }
                else if (result == MessageBoxResult.No)
                {
                    _viewModel.ChangeCycle(1, shouldClearOnNew: false);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  TEMPLATES
        // ═══════════════════════════════════════════════════════════

        private void Template_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var templates = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetTemplates("pdca");
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
                var data = (QASmartClass.LearningTools.Helpers.PdcaData)t.Data;
                item.Click += (_, _) =>
                {
                    if (sideMenu != null && sideMenu.SelectedIndex != 1)
                    {
                        sideMenu.SelectedIndex = 0;
                    }
                    bool hasData = !string.IsNullOrWhiteSpace(_viewModel.PlanText) ||
                                   !string.IsNullOrWhiteSpace(_viewModel.DoText) ||
                                   !string.IsNullOrWhiteSpace(_viewModel.CheckText) ||
                                   !string.IsNullOrWhiteSpace(_viewModel.ActText);

                    if (hasData)
                    {
                        var result = MessageBox.Show(
                            "Bạn có muốn ghi đè mẫu này lên nội dung hiện tại không? Hành động này sẽ làm mất nội dung cũ.",
                            "Xác nhận ghi đè",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.No)
                        {
                            return; // Hủy ghi đè
                        }
                    }

                    _viewModel.ApplyTemplate(data.Plan, data.Do, data.Check, data.Act);
                };
                parentItem.Items.Add(item);
            }

            menu.PlacementTarget = (Button)sender;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        // ═══════════════════════════════════════════════════════════
        //  ACTIONS
        // ═══════════════════════════════════════════════════════════

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
                var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "PNG Image|*.png", FileName = $"PDCA_{DateTime.Now:yyyyMMdd_HHmmss}.png" };
                if (dlg.ShowDialog() == true)
                {
                    var target = exportArea;
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
                    MessageBox.Show($"Đã lưu: {dlg.FileName}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Xóa toàn bộ nội dung và thiết lập lại từ Chu kỳ 1?", "Xác nhận xóa sạch", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _viewModel.ClearAll();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  LOCALIZATION & IMAGES
        // ═══════════════════════════════════════════════════════════

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
                        Icon = "🔬",
                        Title = isVN ? "Dự án Nghiên cứu Khoa học trong phòng thí nghiệm" : "Quality Improvement Cycle",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_pdca_1_{suffix}.png",
                        Description = isVN 
                            ? "Cải tiến quy trình phát triển sản phẩm công nghệ học đường.&#x0a;• Plan: Thiết kế mạch cảm biến đo độ ẩm đất tự động tưới nước trong 2 tuần.&#x0a;• Do: Lắp ráp phần cứng và nạp chương trình điều khiển thử nghiệm.&#x0a;• Check: Cảm biến đo sai lệch 15%, lượng nước tưới quá nhiều gây ngập úng.&#x0a;• Act: Điều chỉnh hiệu chuẩn (calibration) phần mềm và thay vòi phun sương." 
                            : "Plan process upgrades (Plan), run pilot tests (Do), audit results (Check), and standardize best methods (Act)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📚",
                        Title = isVN ? "Lập Kế hoạch Ôn thi Học kỳ môn Vật lý" : "Improving Study Habits",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_pdca_2_{suffix}.png",
                        Description = isVN 
                            ? "Nâng cao kết quả học tập thông qua ôn luyện có hệ thống.&#x0a;• Plan: Ôn tập 5 chương lý thuyết, giải 10 đề thi thử trong 2 tuần để đạt điểm 9.&#x0a;• Do: Tuần 1 ôn lý thuyết và làm 3 đề. Tuần 2 do thiếu thời gian nên chỉ giải tiếp 5 đề.&#x0a;• Check: Thi thử đạt 8 điểm. Nhận thấy mất điểm nhiều phần quang học do chưa ôn kỹ.&#x0a;• Act: Dành riêng 3 ngày cuối tập trung giải đề phần quang học và nhờ bạn hỗ trợ." 
                            : "Plan a new weekly study schedule (Plan), follow it for 7 days (Do), check test grades (Check), and adjust the schedule (Act)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "♻",
                        Title = isVN ? "Phân loại và Tái chế Rác thải tại Lớp học" : "Customer Service Boost",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_pdca_3_{suffix}.png",
                        Description = isVN 
                            ? "Bảo vệ môi trường lớp học và xây dựng thói quen xanh.&#x0a;• Plan: Giảm 30% rác thải nhựa sau 1 tháng bằng cách đặt thùng phân loại rác.&#x0a;• Do: Bố trí thùng rác hữu cơ/tái chế, treo bảng thông tin tuyên truyền cuối lớp.&#x0a;• Check: Sau tuần đầu, 20% học sinh vẫn vứt nhầm rác do nhãn dán chưa rõ ràng.&#x0a;• Act: Vẽ thêm hình ảnh minh họa trực quan trực tiếp lên nắp thùng và di dời thùng rác." 
                            : "Design new staff scripts (Plan), train a pilot group (Do), survey customer satisfaction (Check), and roll out to all staff (Act)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📖",
                        Title = isVN ? "Quản lý Sách Thư viện Trường học" : "Software Release Cycles",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_pdca_4_{suffix}.png",
                        Description = isVN 
                            ? "Tối ưu hóa quy trình mượn trả và sắp xếp sách khoa học.&#x0a;• Plan: Rút ngắn thời gian tìm sách từ 5 phút xuống còn 1 phút bằng mã màu kệ sách.&#x0a;• Do: Dán mã màu tương ứng cho 4 thể loại sách chính và tập huấn cho thủ thư.&#x0a;• Check: Học sinh tìm sách nhanh hơn, nhưng 15% sách vẫn bị trả nhầm kệ.&#x0a;• Act: Làm bảng chỉ dẫn mã màu lớn ở đầu mỗi dãy kệ sách và dán nhãn màu gáy sách." 
                            : "Define new features (Plan), code and debug (Do), run QA tests (Check), and deploy to production servers (Act)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "👥",
                        Title = isVN ? "Cải thiện Hiệu quả Thảo luận Nhóm" : "Energy Saving in Factory",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_pdca_5_{suffix}.png",
                        Description = isVN 
                            ? "Gia tăng sự tương tác và đảm bảo mọi thành viên đều đóng góp.&#x0a;• Plan: Đạt 100% thành viên phát biểu ý kiến trong buổi thảo luận nhóm 45 phút.&#x0a;• Do: Tiến hành họp nhóm, trưởng nhóm nêu chủ đề để các bạn tự do phát biểu.&#x0a;• Check: Chỉ có 2 bạn tích cực tranh luận, 3 thành viên khác im lặng dùng điện thoại.&#x0a;• Act: Phân công nhiệm vụ cụ thể trước buổi họp và quy định lượt phát biểu luân phiên." 
                            : "Set goals to cut power (Plan), install LED lights (Do), compare monthly bills (Check), and update facility policy (Act)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⚽",
                        Title = isVN ? "Huấn luyện Đội bóng đá của Lớp" : "Safety Hazard Prevention",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_pdca_6_{suffix}.png",
                        Description = isVN 
                            ? "Cải thiện thể lực và tinh thần phối hợp đồng đội chuẩn bị giải đấu.&#x0a;• Plan: Tập luyện 2 buổi/tuần, cải thiện tỷ lệ sút trúng đích lên 80% sau 3 tuần.&#x0a;• Do: Tổ chức tập chiến thuật phối hợp nhóm và sút phạt đền trực diện.&#x0a;• Check: Đá giao hữu thua lớp bạn 1-3. Đội hình rời rạc và thể lực sút giảm ở hiệp 2.&#x0a;• Act: Bổ sung bài tập chạy bền cuối buổi, đổi giờ tập thuận lợi để đi đông đủ." 
                            : "Assess risk areas (Plan), install safety signs (Do), count accident rate (Check), and train teams on protocol (Act)."
                    }
            };

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for PdcaTool: {Err}", ex.Message);
            }
        }

        private void LocalizeUI()
        {
            if (!_isVN)
            {
                // Headers & Tabs
                if (menuTextGuide != null) menuTextGuide.Text = "Guide & Process";
                if (menuTextWorkspace != null) menuTextWorkspace.Text = "PDCA Workspace";
                if (menuTextPractical != null) menuTextPractical.Text = "Practical Applications";
                if (expGuide != null) expGuide.Header = "📖 User Guide & Notes";

                // Header Titles & Buttons
                lblHeaderTitle.Text = "🔄 PDCA Cycle";
                lblHeaderSubtitle.Text = "Plan - Do - Check - Act (Deming Wheel) for continuous improvement";
                lblCycleNum.Text = "Cycle:";
                
                btnTemplate.Content = "🎯 Templates ▼";
                btnTemplate.ToolTip = "Select a sample analysis template";
                btnExport.Content = "📷 Export PNG";
                btnExport.ToolTip = "Export analysis as PNG image";
                btnClear.Content = "🗑️ Clear";
                btnClear.ToolTip = "Clear entire analysis and reset to Cycle 1";

                // Guide Content
                lblOriginTitle.Text = "💡 ORIGIN & MEANING";
                lblAppTitle.Text = "🌍 REAL-WORLD APPLICATION";
                lblIntro.Text = "PDCA Cycle helps you continuously self-assess and improve your learning outcomes or habits through a closed 4-step loop.\n\n" +
                              "How to perform 1 loop:\n" +
                              "• PLAN: Set a clear goal and detail how to execute it (e.g., target 8 points in Math next week by solving 5 exercises/day).\n" +
                              "• DO: Take action exactly as proposed in the Plan step (Record what you actually did).\n" +
                              "• CHECK: At the end of the week, review if you followed the plan. What was the actual test score?\n" +
                              "• ACT: Learn from experience. If not achieved, adjust the learning method (e.g., change study time). If achieved, maintain the habit. Then click '>' above to proceed to the 'Next Cycle'.";
                lblOpTitle.Text = "🎯 VISUAL WORKFLOW WORKSTEP (4 STEPS FOR TEACHERS & STUDENTS)";
                lblOpContent.Text = "Step 1: Identify your goal. Enter the improvement goal in the 'Improvement Goal' box (e.g., 'Focus on learning English', 'Reduce junk food').\n" +
                                   "Step 2: Plan & Execute. Write details in the 'PLAN' and 'DO' boxes. You can click 'Templates' to view guide samples.\n" +
                                   "Step 3: Evaluate & Modify. After testing, record results in 'CHECK' and next improvement actions in 'ACT'.\n" +
                                   "Step 4: Go to next cycle. Click '>' button on the right. Select [No] to copy old contents as a baseline, or [Yes] to clear and start a new plan. Click 'Export PNG' to save your work and submit to teachers.";
                
                lblExTitle.Text = "💡 VISUAL CONTINUOUS IMPROVEMENT EXAMPLE (2 CYCLES)";
                lblExCol1.Text = "Cycle 1 (Initial Trial)";
                lblExCol2.Text = "Cycle 2 (Post-Reflection Improvement)";
                lblExRow1.Text = "📝 PLAN & DO";
                lblExRow1Col1.Text = "• Plan: Set alarm clock for 5:30 AM\n• Do: Went to bed late (12 PM), turned off alarm and slept again";
                lblExRow1Col2.Text = "• Plan: Go to bed by 10:45 PM, place alarm clock 3 meters away from bed\n• Do: Woke up when alarm rang to turn it off, drank warm water to wake up";
                lblExRow2.Text = "🔍 CHECK & ACT";
                lblExRow2Col1.Text = "• Check: Only woke up 1/7 days, felt tired\n• Act: Learn from experience, sleep earlier and place alarm further";
                lblExRow2Col2.Text = "• Check: Woke up early 6/7 days, felt alert and studied well\n• Act: Maintain this good habit and raise goal to wake up at 5:15 AM";

                // Form Inputs
                lblSubject.Text = "📋 Improvement Goal:";
                txtSubject.ToolTip = "Enter a specific goal you want to improve (e.g., Increase Math exam score)";

                lblPlanTitle.Text = "📝 PLAN (Plan)";
                lblPlanSub.Text = "Identify problems, causes, and set specific goals";
                txtPlan.ToolTip = "Set specific goals (What do you want to achieve? How to achieve it?)";

                lblDoTitle.Text = "⚡ DO (Do)";
                lblDoSub.Text = "Implement actual plans and keep a log";
                txtDo.ToolTip = "Record actual actions you did according to plan";

                lblCheckTitle.Text = "🔍 CHECK (Check)";
                lblCheckSub.Text = "Measure actual results against goals";
                txtCheck.ToolTip = "Evaluate achieved results against goals (Achieved? Any issues?)";

                lblActTitle.Text = "🎯 ACT (Act)";
                lblActSub.Text = "Learn from experience, adjust plan, or standardize";
                txtAct.ToolTip = "Learn from experience (Maintain habit or change method if not achieved)";

                // Tab 2: Practical Applications
                
                

                
                

                
                

                
                

                
                

                
                

                
                
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