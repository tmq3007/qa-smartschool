using QASmartClass.LearningTools.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.ViewModels;

namespace QASmartClass.LearningTools.Views.Workplace
{
    /// <summary>
    /// Interaction logic for EisenhowerTool.xaml
    /// </summary>
    public partial class EisenhowerTool : UserControl
    {
        private readonly EisenhowerViewModel _viewModel;
        private Point _dragStartPoint;
        private readonly List<ConfettiParticle> _particles = new();
        private System.Windows.Threading.DispatcherTimer? _confettiTimer;
        private readonly Random _rng = new();

        public EisenhowerTool()
        {
            InitializeComponent();
            _viewModel = new EisenhowerViewModel();
            DataContext = _viewModel;
            LoadPracticalApps();

            // Load tool metadata
            var toolInfo = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetToolInfo("eisenhower");
            if (txtToolMeaning != null) txtToolMeaning.Text = toolInfo.Meaning;
            if (txtToolApplication != null) txtToolApplication.Text = toolInfo.RealWorldApplication;

            // Hook confetti trigger event
            _viewModel.OnConfettiTriggered += ViewModel_OnConfettiTriggered;

            Loaded += (_, _) =>
            {
                TouchTextPad.Attach(txtQuickAdd, mode: "text");
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  QUICK ADD HANDLERS
        // ═══════════════════════════════════════════════════════════

        private void QuickAdd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) QuickAdd_Click(sender, e);
        }

        private void QuickAdd_Click(object sender, RoutedEventArgs e)
        {
            string text = txtQuickAdd.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            // Show popup menu to choose quadrant
            var menu = new ContextMenu();
            var options = new (string Label, int Quadrant)[]
            {
                ("🔥 Ô 1: Làm ngay (Khẩn + Quan trọng)", 1),
                ("📅 Ô 2: Lên lịch (Quan trọng + Không khẩn)", 2),
                ("🤝 Ô 3: Ủy thác (Khẩn + Không quan trọng)", 3),
                ("🗑️ Ô 4: Loại bỏ (Không khẩn + Không quan trọng)", 4)
            };

            foreach (var (label, quadrant) in options)
            {
                var item = new MenuItem { Header = label, FontSize = 13, FontWeight = FontWeights.Medium };
                var targetQuadrant = quadrant;
                item.Click += (_, _) =>
                {
                    _viewModel.QuickAddCommand.Execute(targetQuadrant);
                };
                menu.Items.Add(item);
            }

            menu.PlacementTarget = btnAdd;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        // GotFocus attachment for dynamic textboxes inside ItemsControl
        private void TaskTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox txt)
            {
                TouchTextPad.Attach(txt, mode: "text");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  DRAG & DROP EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════

        private void Task_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Record drag start point
            _dragStartPoint = e.GetPosition(null);
        }

        private void Task_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var diff = e.GetPosition(null) - _dragStartPoint;
                if (global::System.Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    global::System.Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    // Ensure we are dragging the Grid or item template
                    if (sender is FrameworkElement element && element.DataContext is TaskItemViewModel task)
                    {
                        try
                        {
                            DragDrop.DoDragDrop(element, task, DragDropEffects.Move);
                        }
                        catch
                        {
                            // Avoid crash during drag drop exception
                        }
                    }
                }
            }
        }

        private void Quadrant_Drop(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement target && target.Tag != null)
            {
                if (int.TryParse(target.Tag.ToString(), out int targetQuadrant))
                {
                    var task = e.Data.GetData(typeof(TaskItemViewModel)) as TaskItemViewModel;
                    if (task != null && task.Quadrant != targetQuadrant)
                    {
                        task.Quadrant = targetQuadrant;
                    }
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  CONFETTI EFFECT (Pháo hoa giấy Canvas)
        // ═══════════════════════════════════════════════════════════

        private void ViewModel_OnConfettiTriggered(object? sender, EventArgs e)
        {
            // Run UI code on dispatcher thread
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_confettiTimer == null)
                {
                    _confettiTimer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
                    };
                    _confettiTimer.Tick += ConfettiTimer_Tick;
                }

                double width = confettiCanvas.ActualWidth;
                if (width <= 0) width = 900;

                // Color palette for confetti particles
                var colors = new Brush[]
                {
                    Brushes.Red, Brushes.Green, Brushes.Orange, Brushes.Teal,
                    Brushes.Blue, Brushes.Purple, Brushes.DeepPink, Brushes.Gold
                };

                for (int i = 0; i < 70; i++)
                {
                    var rect = new System.Windows.Shapes.Rectangle
                    {
                        Width = _rng.Next(8, 14),
                        Height = _rng.Next(8, 14),
                        Fill = colors[_rng.Next(colors.Length)],
                        RenderTransformOrigin = new Point(0.5, 0.5)
                    };

                    var transform = new TransformGroup();
                    transform.Children.Add(new RotateTransform());
                    transform.Children.Add(new TranslateTransform());
                    rect.RenderTransform = transform;

                    confettiCanvas.Children.Add(rect);

                    _particles.Add(new ConfettiParticle
                    {
                        Element = rect,
                        X = _rng.NextDouble() * width,
                        Y = _rng.NextDouble() * -80 - 20, // Spawn just above canvas
                        SpeedX = _rng.NextDouble() * 5 - 2.5,
                        SpeedY = _rng.NextDouble() * 6 + 4,
                        RotationSpeed = _rng.NextDouble() * 12 - 6,
                        Angle = _rng.NextDouble() * 360
                    });
                }

                _confettiTimer.Start();
            }));
        }

        private void ConfettiTimer_Tick(object? sender, EventArgs e)
        {
            double height = confettiCanvas.ActualHeight;
            if (height <= 0) height = 600;

            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.X += p.SpeedX;
                p.Y += p.SpeedY;
                p.Angle += p.RotationSpeed;

                if (p.Y > height)
                {
                    confettiCanvas.Children.Remove(p.Element);
                    _particles.RemoveAt(i);
                }
                else
                {
                    var tg = (TransformGroup)p.Element.RenderTransform;
                    var rt = (RotateTransform)tg.Children[0];
                    var tt = (TranslateTransform)tg.Children[1];

                    rt.Angle = p.Angle;
                    tt.X = p.X;
                    tt.Y = p.Y;
                }
            }

            if (_particles.Count == 0)
            {
                _confettiTimer?.Stop();
            }
        }

        private class ConfettiParticle
        {
            public FrameworkElement Element { get; set; } = null!;
            public double X { get; set; }
            public double Y { get; set; }
            public double SpeedX { get; set; }
            public double SpeedY { get; set; }
            public double Angle { get; set; }
            public double RotationSpeed { get; set; }
        }

        // ═══════════════════════════════════════════════════════════
        //  TEMPLATES
        // ═══════════════════════════════════════════════════════════

        private void Template_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var templates = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetTemplates("eisenhower");
            var groups = new Dictionary<string, MenuItem>();

            foreach (var t in templates)
            {
                if (!groups.TryGetValue(t.Category, out var parentItem))
                {
                    parentItem = new MenuItem { Header = t.Category, FontSize = 13, FontWeight = FontWeights.Bold };
                    groups[t.Category] = parentItem;
                    menu.Items.Add(parentItem);
                }

                var item = new MenuItem { Header = t.Name, FontSize = 13, FontWeight = FontWeights.Normal };
                var data = (QASmartClass.LearningTools.Helpers.EisenhowerData)t.Data;
                item.Click += (_, _) =>
                {
                    _viewModel.ApplyTemplateData(data.UI, data.NUI, data.UNI, data.NUNI);
                };
                parentItem.Items.Add(item);
            }

            menu.PlacementTarget = (Button)sender;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        // ═══════════════════════════════════════════════════════════
        //  EXPORT PNG
        // ═══════════════════════════════════════════════════════════

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PNG Image|*.png",
                    FileName = $"Eisenhower_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                };

                if (dlg.ShowDialog() == true)
                {
                    // Target matrixGrid (the clean 4-quadrant layout) instead of mainPanel
                    var target = matrixGrid;
                    var bounds = new Rect(target.RenderSize);
                    double dpi = 192; // High-resolution export

                    var rtb = new RenderTargetBitmap(
                        (int)(bounds.Width * dpi / 96), (int)(bounds.Height * dpi / 96),
                        dpi, dpi, PixelFormats.Pbgra32);

                    var dv = new DrawingVisual();
                    using (var dc = dv.RenderOpen())
                    {
                        // Fill white background for the exported image
                        dc.DrawRectangle(Brushes.White, null, new Rect(new Point(), bounds.Size));
                        dc.DrawRectangle(new VisualBrush(target), null, new Rect(new Point(), bounds.Size));
                    }

                    rtb.Render(dv);

                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));

                    using (var stream = File.Create(dlg.FileName))
                    {
                        encoder.Save(stream);
                    }

                    MessageBox.Show($"Đã lưu ảnh ma trận: {dlg.FileName}", "Xuất ảnh thành công",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        Icon = "🔥",
                        Title = isVN ? "Ôn Thi Học Kỳ" : "Exam Prep Management",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_eisenhower_1_{suffix}.png",
                        Description = isVN 
                            ? "Lập kế hoạch và ôn tập ngay các môn học sắp thi vào ngày mai hoặc tuần tới. Đây là nhiệm vụ khẩn cấp và cực kỳ quan trọng cần thực hiện trước tiên." 
                            : "Prioritize studying for tomorrow's exam as Urgent and Important (Quadrant 1), ensuring it gets done first."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📅",
                        Title = isVN ? "Luyện Ngoại Ngữ" : "Long-term Goal Planning",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_eisenhower_2_{suffix}.png",
                        Description = isVN 
                            ? "Học từ vựng tiếng Anh, rèn luyện kỹ năng mềm mỗi ngày. Dù không khẩn cấp ngay tức thì nhưng lại vô cùng quan trọng cho sự phát triển dài hạn của học sinh." 
                            : "Schedule regular exercise or career development courses as Important but Not Urgent (Quadrant 2) to prevent procrastination."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🤝",
                        Title = isVN ? "Phân Công Làm Nhóm" : "Handling Interruptions",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_eisenhower_3_{suffix}.png",
                        Description = isVN 
                            ? "Phân chia công việc làm slide thuyết trình nhóm cho các bạn khác có kỹ năng chuyên biệt hơn. Nhiệm vụ cần làm gấp nhưng bản thân có thể ủy thác cho người khác." 
                            : "Identify sudden phone calls or general emails as Urgent but Not Important (Quadrant 3) and delegate or limit them."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧹",
                        Title = isVN ? "Giúp Đỡ Việc Nhà" : "Social Media Limits",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_eisenhower_4_{suffix}.png",
                        Description = isVN 
                            ? "Quét dọn nhà cửa, rửa bát giúp cha mẹ sau giờ học. Đây là các việc khẩn cấp phát sinh nhưng không quá ảnh hưởng tới mục tiêu học tập lớn của bạn." 
                            : "Recognize scrolling social media or playing video games as Not Urgent and Not Important (Quadrant 4) and delete them."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📚",
                        Title = isVN ? "Đọc Sách Kỹ Năng" : "Stress Load Reduction",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_eisenhower_5_{suffix}.png",
                        Description = isVN 
                            ? "Đọc sách văn học, sách khoa học hoặc rèn luyện thể dục. Hoạt động giúp tinh thần thoải mái, tích lũy tri thức dài hạn (Quan trọng nhưng không khẩn cấp)." 
                            : "Prevent burnout by shifting tasks from Quadrant 1 (crisis mode) into Quadrant 2 (planned preparation) early on."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📵",
                        Title = isVN ? "Hạn Chế Lướt Mạng" : "Team Task Delegation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_eisenhower_6_{suffix}.png",
                        Description = isVN 
                            ? "Cắt giảm thời gian chơi game quá giờ, lướt Facebook/TikTok hoặc xem phim thâu đêm. Đây là việc không quan trọng, không khẩn cấp cần được loại bỏ." 
                            : "Identify low-value urgent tasks in Quadrant 3 to delegate to assistants, freeing up time for strategic planning."
                    }
            };

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for EisenhowerTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewPractice == null || viewStats == null || viewPractical == null)
                return;

            // Ẩn tất cả
            viewGuide.Visibility = Visibility.Collapsed;
            viewPractice.Visibility = Visibility.Collapsed;
            viewStats.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            // Hiện đúng tab tương ứng
            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewPractice.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewStats.Visibility = Visibility.Visible;
                    break;
                case 3:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
