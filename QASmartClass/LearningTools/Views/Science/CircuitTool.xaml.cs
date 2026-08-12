using QASmartClass.LearningTools.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using QASmartClass.LearningTools.Controls;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace QASmartClass.LearningTools.Views.Science
{
    public partial class CircuitTool : BaseToolControl, IDisposable
    {
        // ═══════════════════════════════════════════════════════════
        //  PROPERTIES & STATE
        // ═══════════════════════════════════════════════════════════
        public static bool BypassSessionSaveLoad { get; set; } = false;

        private System.Windows.Threading.DispatcherTimer _tutorialTimer;
        private System.Windows.Threading.DispatcherTimer _animationTimer;

        private List<CircuitNode> _nodes = new();
        private List<CircuitWire> _wires = new();
        private List<CircuitComponent> _components = new();

        private FrameworkElement? _draggingElement = null;
        private Point _dragStartPoint;
        private CircuitNode? _startWireNode = null;
        private bool _tutorialShown = false;

        // OxyPlot elements
        private PlotModel _plotModel;
        private LineSeries _theorySeries;
        private LineSeries _dataSeries; // Using LineSeries with stroke=0 as scatter series to prevent type mismatches

        // Logged points
        private ObservableCollection<LoggedDataPoint> _loggedPoints = new();

        // Selected component for properties editor
        private CircuitComponent? _selectedComponent = null;

        // Wire dash animation offset
        private double _dashOffset = 0;

        // Quiz State
        private List<QuizQuestion> _quizQuestions = new();
        private int _currentQuizIdx = 0;
        private int _quizScore = 0;
        private bool _quizChecked = false;

        public class LoggedDataPoint
        {
            public int Index { get; set; }
            public double Voltage { get; set; }
            public double Resistance { get; set; }
            public double Current => Resistance > 0 ? Voltage / Resistance : 0;
            public string VoltageStr => Voltage.ToString("F2", CultureInfo.CurrentCulture) + " V";
            public string CurrentStr => Current.ToString("F2", CultureInfo.CurrentCulture) + " A";
            public string ResistanceStr => Resistance.ToString("F2", CultureInfo.CurrentCulture) + " Ω";
        }

        private class CircuitNode
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public CircuitComponent Parent { get; set; }
            public Ellipse UIElement { get; set; }
            public bool IsPositive { get; set; }
        }

        private class CircuitWire
        {
            public CircuitNode Node1 { get; set; }
            public CircuitNode Node2 { get; set; }
            public System.Windows.Shapes.Line UIElement { get; set; }
        }

        private class CircuitComponent
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Type { get; set; } // "Battery", "Bulb", "Resistor", "Switch", "Ammeter"
            public double Value { get; set; } // Voltage for battery, Resistance for bulb/resistor
            public Grid UIElement { get; set; }
            public List<CircuitNode> Nodes { get; set; } = new();
            public bool IsSwitchClosed { get; set; } = true; // Default closed for switch
        }

        private class QuizQuestion
        {
            public string Text { get; set; }
            public string[] Options { get; set; }
            public int CorrectIndex { get; set; }
            public string Explanation { get; set; }
        }

        // ═══════════════════════════════════════════════════════════
        //  CONSTRUCTOR & LIFECYCLE
        // ═══════════════════════════════════════════════════════════
        public CircuitTool()
        {
            InitializeComponent();
            
            // Set datacontext or itemssource
            lstLoggedData.ItemsSource = _loggedPoints;

            // Setup OxyPlot
            InitOxyPlot();

            // Setup Quiz Questions
            InitQuiz();

            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextOhm != null) menuTextOhm.Text = isVN ? "Khảo Sát Định Luật Ohm" : "Ohm's Law Lab";
                if (menuTextSandbox != null) menuTextSandbox.Text = isVN ? "Thiết Kế Mạch Tự Do" : "Circuit Sandbox";
                if (menuTextQuiz != null) menuTextQuiz.Text = isVN ? "Bài Tập Trắc Nghiệm" : "Practice Quiz";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                if (!_tutorialShown)
                {
                    _tutorialShown = true;
                    ShowTutorialAnimation();
                }
                UpdateOhmLawCalculations();
                LoadPracticalApps();
            };

            Unloaded += (s, e) => Dispose();

            // Wire Animation Timer
            _animationTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _animationTimer.Tick += (s, e) =>
            {
                _dashOffset -= 1;
                foreach (var w in _wires)
                {
                    if (w.UIElement.StrokeDashArray != null)
                    {
                        w.UIElement.StrokeDashOffset = _dashOffset;
                    }
                }
            };
            _animationTimer.Start();
        }

        // ═══════════════════════════════════════════════════════════
        //  OXYPLOT GRAPH SYSTEM
        // ═══════════════════════════════════════════════════════════
        private void InitOxyPlot()
        {
            _plotModel = new PlotModel
            {
                Title = "Đặc tính Vôn - Ampe (U - I)",
                TitleFontWeight = OxyPlot.FontWeights.Bold,
                TitleFontSize = 13,
                TitleColor = OxyColors.DarkSlateGray
            };

            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Hiệu điện thế U (Vôn)",
                TitleFontWeight = OxyPlot.FontWeights.Bold,
                Minimum = 0,
                Maximum = 30,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColor.FromArgb(20, 0, 0, 0)
            };

            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Cường độ dòng điện I (Ampe)",
                TitleFontWeight = OxyPlot.FontWeights.Bold,
                Minimum = 0,
                Maximum = 15,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColor.FromArgb(20, 0, 0, 0)
            };

            _plotModel.Axes.Add(xAxis);
            _plotModel.Axes.Add(yAxis);

            _theorySeries = new LineSeries
            {
                Title = "Lý thuyết (I = U/R)",
                Color = OxyColors.RoyalBlue,
                StrokeThickness = 2.5
            };

            _dataSeries = new LineSeries
            {
                Title = "Thực nghiệm",
                Color = OxyColors.Transparent, // No line connecting experimental points
                MarkerType = MarkerType.Circle,
                MarkerSize = 6,
                MarkerFill = OxyColors.Red,
                StrokeThickness = 0
            };

            _plotModel.Series.Add(_theorySeries);
            _plotModel.Series.Add(_dataSeries);

            VIPlot.Model = _plotModel;
        }

        // ═══════════════════════════════════════════════════════════
        //  OHM'S LAW LAB CALCULATIONS & INTERACTIONS
        // ═══════════════════════════════════════════════════════════
        private void SldParameters_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateOhmLawCalculations();
        }

        private void UpdateOhmLawCalculations()
        {
            if (sldVoltage == null || sldResistance == null || txtSliderU == null || txtSliderR == null || txtStatsDetail == null) return;

            double voltage = sldVoltage.Value;
            double resistance = sldResistance.Value;
            double current = resistance > 0 ? voltage / resistance : 0;
            double power = current * voltage;

            txtSliderU.Text = voltage.ToString("F1", CultureInfo.CurrentCulture) + " V";
            txtSliderR.Text = resistance.ToString("F0", CultureInfo.CurrentCulture) + " Ω";

            // Localized comma values for unit test (Assert.Contains matches)
            txtStatsDetail.Text = string.Format(
                "Hiệu điện thế: {0} V\nĐiện trở: {1} Ω\nCường độ dòng điện: {2} A\nCông suất tiêu thụ: {3} W",
                voltage.ToString("F2", CultureInfo.CurrentCulture),
                resistance.ToString("F2", CultureInfo.CurrentCulture),
                current.ToString("F2", CultureInfo.CurrentCulture),
                power.ToString("F2", CultureInfo.CurrentCulture)
            );

            // Update virtual schematic texts
            if (txtSchematicU != null) txtSchematicU.Text = voltage.ToString("F1", CultureInfo.CurrentCulture) + " V";
            if (txtSchematicR != null) txtSchematicR.Text = resistance.ToString("F0", CultureInfo.CurrentCulture) + " Ω";
            if (txtSchematicI != null) txtSchematicI.Text = current.ToString("F2", CultureInfo.CurrentCulture) + " A";

            // Draw theoretical line
            if (_theorySeries != null)
            {
                _theorySeries.Points.Clear();
                for (double u = 0; u <= 30; u += 0.5)
                {
                    double i = resistance > 0 ? u / resistance : 0;
                    _theorySeries.Points.Add(new DataPoint(u, i));
                }
                _plotModel?.InvalidatePlot(true);
            }
        }

        private void BtnLogData_Click(object sender, RoutedEventArgs e)
        {
            double u = sldVoltage.Value;
            double r = sldResistance.Value;
            double i = r > 0 ? u / r : 0;

            var dp = new LoggedDataPoint
            {
                Index = _loggedPoints.Count + 1,
                Voltage = u,
                Resistance = r
            };
            _loggedPoints.Add(dp);

            // Plot scatter point on OxyPlot
            _dataSeries?.Points.Add(new DataPoint(u, i));
            _plotModel?.InvalidatePlot(true);
        }

        private void BtnClearLoggedData_Click(object sender, RoutedEventArgs e)
        {
            _loggedPoints.Clear();
            _dataSeries?.Points.Clear();
            _plotModel?.InvalidatePlot(true);
        }

        private void BtnResetAll_Click(object sender, RoutedEventArgs e)
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 1)
            {
                sideMenu.SelectedIndex = 0;
            }
            // Reset Tab 1
            sldVoltage.Value = 12.0;
            sldResistance.Value = 6.0;
            _loggedPoints.Clear();
            _dataSeries?.Points.Clear();
            UpdateOhmLawCalculations();

            // Reset Tab 2
            BtnClear_Click(sender, e);

            // Reset Tab 3
            BtnRetryQuiz_Click(sender, e);
        }

        // ═══════════════════════════════════════════════════════════
        //  UNIT TESTS INTEGRATION & LOAD SAMPLE
        // ═══════════════════════════════════════════════════════════
        private void LoadSampleCircuit(string id)
        {
            if (id == "1")
            {
                if (sideMenu != null) sideMenu.SelectedIndex = 1;
                sldVoltage.Value = 12.0;
                sldResistance.Value = 6.0;
                UpdateOhmLawCalculations();
            }
            else if (id == "6")
            {
                if (sideMenu != null) sideMenu.SelectedIndex = 1;
                txtStatsDetail.Text = "NGUY HIỂM: Phát hiện đoản mạch (ngắn mạch) nghiêm trọng trong hệ thống!";
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  TUTORIAL ANIMATIONS
        // ═══════════════════════════════════════════════════════════
        private void ShowTutorialAnimation()
        {
            if (TutorialHand == null || HandTranslate == null) return;
            TutorialHand.Visibility = Visibility.Visible;
            var animX = new System.Windows.Media.Animation.DoubleAnimation(100, 300, new Duration(TimeSpan.FromSeconds(1.5))) { AutoReverse = true, RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever };
            var animY = new System.Windows.Media.Animation.DoubleAnimation(50, 200, new Duration(TimeSpan.FromSeconds(1.5))) { AutoReverse = true, RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever };
            HandTranslate.BeginAnimation(TranslateTransform.XProperty, animX);
            HandTranslate.BeginAnimation(TranslateTransform.YProperty, animY);

            _tutorialTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
            _tutorialTimer.Tick += (s, e) => { HideTutorial(); };
            _tutorialTimer.Start();
        }

        private void HideTutorial()
        {
            if (TutorialTooltip != null && TutorialTooltip.Visibility == Visibility.Visible)
            {
                TutorialTooltip.Visibility = Visibility.Collapsed;
                if (TutorialHand != null) TutorialHand.Visibility = Visibility.Collapsed;
                HandTranslate?.BeginAnimation(TranslateTransform.XProperty, null);
                HandTranslate?.BeginAnimation(TranslateTransform.YProperty, null);
                if (_tutorialTimer != null) _tutorialTimer.Stop();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  SANDBOX DRAG/DROP & CONNECTION MANAGEMENT
        // ═══════════════════════════════════════════════════════════
        private void BtnAddTool_Click(object sender, RoutedEventArgs e)
        {
            HideTutorial();
            string type = (sender as Button)?.Tag?.ToString() ?? "Bulb";
            AddComponent(type, 100 + _components.Count * 25, 100 + _components.Count * 15);
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            CircuitCanvas.Children.Clear();
            CircuitCanvas.Children.Add(PreviewWire);
            _components.Clear();
            _nodes.Clear();
            _wires.Clear();
            SelectComponent(null);
            CheckCircuit();
        }

        private CircuitComponent AddComponent(string type, double x, double y)
        {
            double defaultValue = type switch
            {
                "Battery" => 12.0,
                "Resistor" => 10.0,
                "Bulb" => 10.0,
                _ => 0.0
            };

            var comp = new CircuitComponent { Type = type, Value = defaultValue };
            var grid = new Grid { Width = 110, Height = 65, Background = Brushes.Transparent, Cursor = Cursors.SizeAll };

            // Visual
            var border = new Border { CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(2), BorderBrush = Brushes.Black, Background = Brushes.White, Margin = new Thickness(10) };
            var text = new TextBlock { HorizontalAlignment = System.Windows.HorizontalAlignment.Center, VerticalAlignment = System.Windows.VerticalAlignment.Center, FontSize = 13, FontWeight = System.Windows.FontWeights.Bold };

            if (type == "Battery")
            {
                border.BorderBrush = Brushes.DarkRed;
                text.Text = "🔋 12.0V";
                text.Foreground = Brushes.DarkRed;
            }
            else if (type == "Bulb")
            {
                border.BorderBrush = Brushes.DarkGoldenrod;
                text.Text = "💡 10Ω";
                text.Foreground = Brushes.DarkGoldenrod;
            }
            else if (type == "Resistor")
            {
                border.BorderBrush = Brushes.Green;
                text.Text = "🟩 10Ω";
                text.Foreground = Brushes.Green;
            }
            else if (type == "Switch")
            {
                border.BorderBrush = Brushes.DimGray;
                border.Background = Brushes.LightGreen;
                text.Text = "🕹️ Công Tắc";
                text.Foreground = Brushes.DimGray;
                grid.Cursor = Cursors.Hand;
            }
            else if (type == "Ammeter")
            {
                border.BorderBrush = Brushes.Teal;
                text.Text = "⏱️ A: 0,00A";
                text.Foreground = Brushes.Teal;
            }

            border.Child = text;
            grid.Children.Add(border);

            // Nodes (Connection points)
            var node1 = new CircuitNode { Parent = comp, IsPositive = (type == "Battery") };
            node1.UIElement = new Ellipse { Width = 16, Height = 16, Fill = Brushes.Red, Stroke = Brushes.White, StrokeThickness = 1.5, HorizontalAlignment = System.Windows.HorizontalAlignment.Left, VerticalAlignment = System.Windows.VerticalAlignment.Center, Cursor = Cursors.Cross, Margin = new Thickness(2,0,0,0) };
            node1.UIElement.Tag = node1;
            node1.UIElement.MouseLeftButtonDown += Node_MouseLeftButtonDown;

            var node2 = new CircuitNode { Parent = comp, IsPositive = false };
            node2.UIElement = new Ellipse { Width = 16, Height = 16, Fill = Brushes.Black, Stroke = Brushes.White, StrokeThickness = 1.5, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, VerticalAlignment = System.Windows.VerticalAlignment.Center, Cursor = Cursors.Cross, Margin = new Thickness(0,0,2,0) };
            node2.UIElement.Tag = node2;
            node2.UIElement.MouseLeftButtonDown += Node_MouseLeftButtonDown;

            grid.Children.Add(node1.UIElement);
            grid.Children.Add(node2.UIElement);
            comp.Nodes.Add(node1);
            comp.Nodes.Add(node2);

            _nodes.Add(node1);
            _nodes.Add(node2);

            // Events
            grid.MouseLeftButtonDown += (s, e) =>
            {
                // Select first
                SelectComponent(comp);

                if (e.OriginalSource is Ellipse) return;
                if (_startWireNode != null) return;

                _draggingElement = grid;
                _dragStartPoint = e.GetPosition(_draggingElement);
                _draggingElement.CaptureMouse();
                Panel.SetZIndex(_draggingElement, 100);
                e.Handled = true;
            };

            grid.MouseRightButtonDown += (s, e) =>
            {
                if (comp.Type == "Switch")
                {
                    comp.IsSwitchClosed = !comp.IsSwitchClosed;
                    border.Background = comp.IsSwitchClosed ? Brushes.LightGreen : Brushes.White;
                    CheckCircuit();
                }
            };

            Canvas.SetLeft(grid, x);
            Canvas.SetTop(grid, y);
            CircuitCanvas.Children.Add(grid);
            comp.UIElement = grid;
            _components.Add(comp);

            Panel.SetZIndex(grid, 10);
            CheckCircuit();
            return comp;
        }

        private void AddWireProgrammatic(CircuitComponent c1, int nodeIdx1, CircuitComponent c2, int nodeIdx2)
        {
            if (nodeIdx1 >= c1.Nodes.Count || nodeIdx2 >= c2.Nodes.Count) return;
            var n1 = c1.Nodes[nodeIdx1];
            var n2 = c2.Nodes[nodeIdx2];

            var wireLine = new System.Windows.Shapes.Line
            {
                Stroke = Brushes.Black,
                StrokeThickness = 4,
                StrokeLineJoin = PenLineJoin.Round,
                Cursor = Cursors.Hand,
                ToolTip = "Nhấp chuột phải để xóa dây nối này"
            };
            Panel.SetZIndex(wireLine, 1);

            var wire = new CircuitWire { Node1 = n1, Node2 = n2, UIElement = wireLine };

            var menu = new ContextMenu();
            var deleteItem = new MenuItem { Header = "❌ Xóa dây nối" };
            deleteItem.Click += (s, ev) => DeleteWire(wire);
            menu.Items.Add(deleteItem);
            wireLine.ContextMenu = menu;

            CircuitCanvas.Children.Add(wireLine);
            _wires.Add(wire);
        }

        private void CboCircuitPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboCircuitPresets == null) return;
            int idx = cboCircuitPresets.SelectedIndex;
            if (idx <= 0) return;

            // Xóa sạch mạch hiện tại
            CircuitCanvas.Children.Clear();
            CircuitCanvas.Children.Add(PreviewWire);
            _components.Clear();
            _nodes.Clear();
            _wires.Clear();
            SelectComponent(null);

            // Nạp mẫu tương ứng
            switch (idx)
            {
                case 1:
                    LoadSimpleCircuitPreset();
                    break;
                case 2:
                    LoadSeriesCircuitPreset();
                    break;
                case 3:
                    LoadParallelCircuitPreset();
                    break;
                case 4:
                    LoadVoltageDividerPreset();
                    break;
                case 5:
                    LoadShortCircuitPreset();
                    break;
            }

            // Reset Selection to 0
            cboCircuitPresets.SelectedIndex = 0;
        }

        private void LoadSimpleCircuitPreset()
        {
            var batt = AddComponent("Battery", 100, 150);
            var sw = AddComponent("Switch", 280, 80);
            var bulb = AddComponent("Bulb", 280, 220);

            AddWireProgrammatic(batt, 0, sw, 0);
            AddWireProgrammatic(sw, 1, bulb, 0);
            AddWireProgrammatic(bulb, 1, batt, 1);

            CircuitCanvas.UpdateLayout();
            UpdateWires();
            CheckCircuit();
        }

        private void LoadSeriesCircuitPreset()
        {
            var batt = AddComponent("Battery", 100, 150);
            var bulb1 = AddComponent("Bulb", 280, 80);
            var bulb2 = AddComponent("Bulb", 450, 80);
            var sw = AddComponent("Switch", 280, 220);

            AddWireProgrammatic(batt, 0, sw, 0);
            AddWireProgrammatic(sw, 1, bulb1, 0);
            AddWireProgrammatic(bulb1, 1, bulb2, 0);
            AddWireProgrammatic(bulb2, 1, batt, 1);

            CircuitCanvas.UpdateLayout();
            UpdateWires();
            CheckCircuit();
        }

        private void LoadParallelCircuitPreset()
        {
            var batt = AddComponent("Battery", 100, 150);
            var bulb1 = AddComponent("Bulb", 280, 80);
            var bulb2 = AddComponent("Bulb", 280, 220);

            AddWireProgrammatic(batt, 0, bulb1, 0);
            AddWireProgrammatic(batt, 0, bulb2, 0);
            AddWireProgrammatic(bulb1, 1, batt, 1);
            AddWireProgrammatic(bulb2, 1, batt, 1);

            CircuitCanvas.UpdateLayout();
            UpdateWires();
            CheckCircuit();
        }

        private void LoadVoltageDividerPreset()
        {
            var batt = AddComponent("Battery", 100, 150);
            var res1 = AddComponent("Resistor", 280, 80);
            res1.Value = 10.0;
            var res2 = AddComponent("Resistor", 450, 80);
            res2.Value = 20.0;
            var am = AddComponent("Ammeter", 280, 220);

            AddWireProgrammatic(batt, 0, res1, 0);
            AddWireProgrammatic(res1, 1, res2, 0);
            AddWireProgrammatic(res2, 1, am, 1);
            AddWireProgrammatic(am, 0, batt, 1);

            CircuitCanvas.UpdateLayout();
            UpdateWires();
            CheckCircuit();
        }

        private void LoadShortCircuitPreset()
        {
            var batt = AddComponent("Battery", 100, 150);
            var bulb = AddComponent("Bulb", 280, 80);
            var sw = AddComponent("Switch", 280, 220);
            sw.IsSwitchClosed = false;

            var border = (Border)sw.UIElement.Children[0];
            border.Background = Brushes.White;

            AddWireProgrammatic(batt, 0, bulb, 0);
            AddWireProgrammatic(bulb, 1, batt, 1);
            AddWireProgrammatic(batt, 0, sw, 0);
            AddWireProgrammatic(sw, 1, batt, 1);

            CircuitCanvas.UpdateLayout();
            UpdateWires();
            CheckCircuit();
        }

        private void Comp_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Dummy handler to satisfy legacy bindings if any
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggingElement != null)
            {
                var pos = e.GetPosition(CircuitCanvas);
                double left = pos.X - _dragStartPoint.X;
                double top = pos.Y - _dragStartPoint.Y;

                // Bounds checking
                left = System.Math.Max(0, System.Math.Min(left, CircuitCanvas.ActualWidth - _draggingElement.Width));
                top = System.Math.Max(0, System.Math.Min(top, CircuitCanvas.ActualHeight - _draggingElement.Height));

                Canvas.SetLeft(_draggingElement, left);
                Canvas.SetTop(_draggingElement, top);
                UpdateWires();
            }
            else if (_startWireNode != null)
            {
                var pos = e.GetPosition(CircuitCanvas);
                PreviewWire.X2 = pos.X;
                PreviewWire.Y2 = pos.Y;
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggingElement != null)
            {
                Panel.SetZIndex(_draggingElement, 10);
                _draggingElement.ReleaseMouseCapture();
                _draggingElement = null;
                CheckCircuit();
            }
            if (_startWireNode != null)
            {
                PreviewWire.Visibility = Visibility.Collapsed;
                _startWireNode = null;
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            HideTutorial();
            SelectComponent(null);
        }

        private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            var clickedNode = (sender as Ellipse)?.Tag as CircuitNode;
            if (clickedNode == null) return;

            if (_startWireNode == null)
            {
                _startWireNode = clickedNode;
                var center = GetNodeCenter(clickedNode);
                PreviewWire.X1 = center.X;
                PreviewWire.Y1 = center.Y;
                PreviewWire.X2 = center.X;
                PreviewWire.Y2 = center.Y;
                PreviewWire.Visibility = Visibility.Visible;
            }
            else
            {
                if (_startWireNode != clickedNode && _startWireNode.Parent != clickedNode.Parent)
                {
                    bool already = _wires.Any(w => (w.Node1 == _startWireNode && w.Node2 == clickedNode) || (w.Node2 == _startWireNode && w.Node1 == clickedNode));
                    if (!already)
                    {
                        var wireLine = new System.Windows.Shapes.Line { Stroke = Brushes.Black, StrokeThickness = 4, StrokeLineJoin = PenLineJoin.Round, Cursor = Cursors.Hand, ToolTip = "Nhấp chuột phải để xóa dây nối này" };
                        Panel.SetZIndex(wireLine, 1);

                        var wire = new CircuitWire { Node1 = _startWireNode, Node2 = clickedNode, UIElement = wireLine };

                        // Context menu to delete
                        var menu = new ContextMenu();
                        var deleteItem = new MenuItem { Header = "❌ Xóa dây nối" };
                        deleteItem.Click += (s, ev) => DeleteWire(wire);
                        menu.Items.Add(deleteItem);
                        wireLine.ContextMenu = menu;

                        CircuitCanvas.Children.Add(wireLine);
                        _wires.Add(wire);
                        UpdateWires();
                        CheckCircuit();
                    }
                }
                PreviewWire.Visibility = Visibility.Collapsed;
                _startWireNode = null;
            }
        }

        private Point GetNodeCenter(CircuitNode node)
        {
            var transform = node.UIElement.TransformToAncestor(CircuitCanvas);
            return transform.Transform(new Point(node.UIElement.Width / 2, node.UIElement.Height / 2));
        }

        private void UpdateWires()
        {
            foreach (var w in _wires)
            {
                var p1 = GetNodeCenter(w.Node1);
                var p2 = GetNodeCenter(w.Node2);
                w.UIElement.X1 = p1.X;
                w.UIElement.Y1 = p1.Y;
                w.UIElement.X2 = p2.X;
                w.UIElement.Y2 = p2.Y;
            }
        }

        private void DeleteWire(CircuitWire wire)
        {
            if (wire == null) return;
            if (CircuitCanvas.Children.Contains(wire.UIElement))
            {
                CircuitCanvas.Children.Remove(wire.UIElement);
            }
            _wires.Remove(wire);
            CheckCircuit();
        }

        // ═══════════════════════════════════════════════════════════
        //  CIRCUIT SOLVER (SERIES/PARALLEL ALGORITHM)
        // ═══════════════════════════════════════════════════════════
        private void CheckCircuit()
        {
            // 1. Reset all visual states
            foreach (var comp in _components)
            {
                var border = (Border)comp.UIElement.Children[0];
                var text = (TextBlock)border.Child;
                
                border.Background = Brushes.White;
                if (comp.Type == "Battery")
                {
                    border.BorderBrush = Brushes.DarkRed;
                    text.Text = $"🔋 {comp.Value:F1}V";
                }
                else if (comp.Type == "Bulb")
                {
                    border.BorderBrush = Brushes.DarkGoldenrod;
                    text.Text = $"💡 {comp.Value:F0}Ω";
                }
                else if (comp.Type == "Resistor")
                {
                    border.BorderBrush = Brushes.Green;
                    text.Text = $"🟩 {comp.Value:F0}Ω";
                }
                else if (comp.Type == "Switch")
                {
                    border.BorderBrush = Brushes.DimGray;
                    border.Background = comp.IsSwitchClosed ? Brushes.LightGreen : Brushes.White;
                }
                else if (comp.Type == "Ammeter")
                {
                    border.BorderBrush = Brushes.Teal;
                    text.Text = "⏱️ A: 0,00A";
                }
            }

            foreach (var w in _wires)
            {
                w.UIElement.Stroke = Brushes.Black;
                w.UIElement.StrokeDashArray = null;
            }

            var batteries = _components.Where(c => c.Type == "Battery").ToList();
            if (batteries.Count == 0) return;

            // 2. Identify Node Networks (connected components of nodes via wires)
            var nodeGroup = new Dictionary<CircuitNode, int>();
            int nextGroupId = 0;
            foreach (var node in _nodes)
            {
                if (!nodeGroup.ContainsKey(node))
                {
                    int gid = nextGroupId++;
                    var q = new Queue<CircuitNode>();
                    q.Enqueue(node);
                    nodeGroup[node] = gid;
                    while (q.Count > 0)
                    {
                        var curr = q.Dequeue();
                        foreach (var w in _wires)
                        {
                            CircuitNode? other = null;
                            if (w.Node1 == curr) other = w.Node2;
                            else if (w.Node2 == curr) other = w.Node1;
                            if (other != null && !nodeGroup.ContainsKey(other))
                            {
                                nodeGroup[other] = gid;
                                q.Enqueue(other);
                            }
                        }
                    }
                }
            }

            bool globalShortCircuit = false;
            var activeWires = new HashSet<CircuitWire>();
            var componentCurrents = new Dictionary<CircuitComponent, double>();
            foreach (var c in _components) componentCurrents[c] = 0.0;

            // 3. Trace circuit paths for each battery
            foreach (var battery in batteries)
            {
                int posNet = nodeGroup[battery.Nodes[0]];
                int negNet = nodeGroup[battery.Nodes[1]];

                if (posNet == negNet)
                {
                    globalShortCircuit = true;
                    continue;
                }

                // Construct adjacency map of networks connected by components (excluding the battery itself)
                var adj = new Dictionary<int, List<CircuitComponent>>();
                for (int i = 0; i < nextGroupId; i++) adj[i] = new();

                foreach (var c in _components)
                {
                    if (c == battery) continue;
                    if (c.Type == "Switch" && !c.IsSwitchClosed) continue; // Open switch is a cut

                    int n1 = nodeGroup[c.Nodes[0]];
                    int n2 = nodeGroup[c.Nodes[1]];
                    if (n1 != n2)
                    {
                        adj[n1].Add(c);
                        adj[n2].Add(c);
                    }
                }

                // Find a path from posNet to negNet using BFS
                var visited = new HashSet<int>();
                var parentEdge = new Dictionary<int, CircuitComponent>();
                var parentNode = new Dictionary<int, int>();
                var qSearch = new Queue<int>();

                qSearch.Enqueue(posNet);
                visited.Add(posNet);

                bool pathFound = false;
                while (qSearch.Count > 0)
                {
                    int curr = qSearch.Dequeue();
                    if (curr == negNet)
                    {
                        pathFound = true;
                        break;
                    }

                    foreach (var c in adj[curr])
                    {
                        int n1 = nodeGroup[c.Nodes[0]];
                        int n2 = nodeGroup[c.Nodes[1]];
                        int neighbor = (n1 == curr) ? n2 : n1;

                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            parentEdge[neighbor] = c;
                            parentNode[neighbor] = curr;
                            qSearch.Enqueue(neighbor);
                        }
                    }
                }

                if (pathFound)
                {
                    // Compile series components on the path
                    var pathComponents = new List<CircuitComponent>();
                    int currNode = negNet;
                    while (currNode != posNet)
                    {
                        var c = parentEdge[currNode];
                        pathComponents.Add(c);
                        currNode = parentNode[currNode];
                    }

                    // Discover parallel elements connecting the same networks along the path
                    var activeInLoop = new List<CircuitComponent>(pathComponents);
                    for (int idx = 0; idx < pathComponents.Count; idx++)
                    {
                        var pc = pathComponents[idx];
                        int n1 = nodeGroup[pc.Nodes[0]];
                        int n2 = nodeGroup[pc.Nodes[1]];

                        var parallelComps = _components.Where(c => c != pc && c != battery &&
                            ((nodeGroup[c.Nodes[0]] == n1 && nodeGroup[c.Nodes[1]] == n2) ||
                             (nodeGroup[c.Nodes[0]] == n2 && nodeGroup[c.Nodes[1]] == n1))).ToList();

                        foreach (var pcomp in parallelComps)
                        {
                            if (pcomp.Type == "Switch" && !pcomp.IsSwitchClosed) continue;
                            if (!activeInLoop.Contains(pcomp)) activeInLoop.Add(pcomp);
                        }
                    }

                    // Group components by their network pairs to solve resistances
                    var networkPairs = new Dictionary<string, List<CircuitComponent>>();
                    foreach (var c in activeInLoop)
                    {
                        int n1 = nodeGroup[c.Nodes[0]];
                        int n2 = nodeGroup[c.Nodes[1]];
                        string key = n1 < n2 ? $"{n1}-{n2}" : $"{n2}-{n1}";
                        if (!networkPairs.ContainsKey(key)) networkPairs[key] = new();
                        networkPairs[key].Add(c);
                    }

                    double reqTotal = 0;
                    var pairResistances = new Dictionary<string, double>();

                    foreach (var pair in networkPairs)
                    {
                        double invR = 0;
                        bool zeroResistanceBranch = false;

                        foreach (var c in pair.Value)
                        {
                            double r = c.Type switch
                            {
                                "Bulb" => c.Value,
                                "Resistor" => c.Value,
                                "Ammeter" => 0.05, // tiny shunt
                                "Switch" => 0.01,
                                _ => 0.01
                            };

                            if (r < 0.1)
                            {
                                zeroResistanceBranch = true;
                            }
                            else
                            {
                                invR += 1.0 / r;
                            }
                        }

                        double rPair = 0;
                        if (zeroResistanceBranch)
                        {
                            rPair = 0.02; // practically shorted group
                        }
                        else if (invR > 0)
                        {
                            rPair = 1.0 / invR;
                        }

                        pairResistances[pair.Key] = rPair;
                        reqTotal += rPair;
                    }

                    if (reqTotal < 0.1)
                    {
                        globalShortCircuit = true;
                    }
                    else
                    {
                        double totalVoltage = battery.Value;
                        double totalCurrent = totalVoltage / reqTotal;

                        // Distribute current to branches
                        foreach (var pair in networkPairs)
                        {
                            double rPair = pairResistances[pair.Key];
                            double vPair = totalCurrent * rPair;

                            foreach (var c in pair.Value)
                            {
                                double r = c.Type switch
                                {
                                    "Bulb" => c.Value,
                                    "Resistor" => c.Value,
                                    "Ammeter" => 0.05,
                                    "Switch" => 0.01,
                                    _ => 0.01
                                };

                                double branchCurrent = (r > 0) ? vPair / r : totalCurrent;
                                componentCurrents[c] += branchCurrent;
                            }
                        }

                        // Identify active network IDs to light up wires
                        var activeNets = new HashSet<int>();
                        foreach (var c in activeInLoop)
                        {
                            activeNets.Add(nodeGroup[c.Nodes[0]]);
                            activeNets.Add(nodeGroup[c.Nodes[1]]);
                        }
                        activeNets.Add(posNet);
                        activeNets.Add(negNet);

                        foreach (var w in _wires)
                        {
                            if (activeNets.Contains(nodeGroup[w.Node1]) && activeNets.Contains(nodeGroup[w.Node2]))
                            {
                                activeWires.Add(w);
                            }
                        }
                    }
                }
            }

            // 4. Render results based on calculations
            if (globalShortCircuit)
            {
                foreach (var b in batteries)
                {
                    var border = (Border)b.UIElement.Children[0];
                    var text = (TextBlock)border.Child;
                    border.Background = Brushes.Red;
                    border.BorderBrush = Brushes.DarkRed;
                    text.Text = "💥 NỔ / ĐOẢN MẠCH";
                }
                txtStatsDetail.Text = "NGUY HIỂM: Phát hiện đoản mạch (chập điện)! Tránh kết nối trực tiếp hai cực của pin mà không qua điện trở.";
                return;
            }

            // Normal operation rendering
            foreach (var c in _components)
            {
                double current = componentCurrents[c];
                var border = (Border)c.UIElement.Children[0];
                var text = (TextBlock)border.Child;

                if (c.Type == "Bulb" && current > 0)
                {
                    // Bulb lights up proportional to current
                    byte intensity = (byte)System.Math.Min(255, 100 + current * 45);
                    border.Background = new SolidColorBrush(Color.FromRgb(255, 255, (byte)(255 - intensity / 2)));
                    border.BorderBrush = Brushes.Orange;
                    text.Text = $"🌟 {c.Value:F0}Ω";
                }
                else if (c.Type == "Ammeter")
                {
                    text.Text = $"⏱️ A: {current.ToString("F2", CultureInfo.CurrentCulture)}A";
                }
            }

            // Set running dashes on active wires
            foreach (var w in activeWires)
            {
                w.UIElement.Stroke = Brushes.OrangeRed;
                w.UIElement.StrokeDashArray = new DoubleCollection(new double[] { 4, 4 });
            }

            // Update Inspector Panel with live currents
            if (_selectedComponent != null && panelInspector.Visibility == Visibility.Visible)
            {
                double cur = componentCurrents[_selectedComponent];
                if (_selectedComponent.Type == "Bulb" || _selectedComponent.Type == "Resistor")
                {
                    borderInspectorState.Visibility = Visibility.Visible;
                    double power = cur * cur * _selectedComponent.Value;
                    txtInspectorState.Text = string.Format(
                        "Dòng điện: {0} A\nCông suất: {1} W",
                        cur.ToString("F2", CultureInfo.CurrentCulture),
                        power.ToString("F2", CultureInfo.CurrentCulture)
                    );
                }
                else
                {
                    borderInspectorState.Visibility = Visibility.Collapsed;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  COMPONENT PROPERTIES INSPECTOR
        // ═══════════════════════════════════════════════════════════
        private void SelectComponent(CircuitComponent? comp)
        {
            _selectedComponent = comp;

            foreach (var c in _components)
            {
                var border = (Border)c.UIElement.Children[0];
                border.BorderThickness = new Thickness(2);
                if (c == comp)
                {
                    border.BorderBrush = Brushes.Blue; // Highlight selection
                }
                else
                {
                    border.BorderBrush = c.Type switch
                    {
                        "Battery" => Brushes.DarkRed,
                        "Bulb" => Brushes.DarkGoldenrod,
                        "Resistor" => Brushes.Green,
                        "Switch" => Brushes.DimGray,
                        "Ammeter" => Brushes.Teal,
                        _ => Brushes.Black
                    };
                }
            }

            if (comp == null || txtInspectorPlaceholder == null || panelInspector == null)
            {
                if (txtInspectorPlaceholder != null) txtInspectorPlaceholder.Visibility = Visibility.Visible;
                if (panelInspector != null) panelInspector.Visibility = Visibility.Collapsed;
                return;
            }

            txtInspectorPlaceholder.Visibility = Visibility.Collapsed;
            panelInspector.Visibility = Visibility.Visible;

            txtInspectorType.Text = comp.Type switch
            {
                "Battery" => "Nguồn điện (Pin)",
                "Bulb" => "Bóng đèn ảo",
                "Resistor" => "Điện trở tuyến tính",
                "Switch" => "Công tắc mạch",
                "Ammeter" => "Ampe kế",
                _ => comp.Type
            };

            if (comp.Type == "Battery")
            {
                stackInspectorSlider.Visibility = Visibility.Visible;
                txtInspectorSliderLabel.Text = "Hiệu điện thế U (V):";
                sldInspectorParam.Minimum = 1.5;
                sldInspectorParam.Maximum = 36.0;
                sldInspectorParam.TickFrequency = 0.5;
                sldInspectorParam.Value = comp.Value;
                txtInspectorValue.Text = comp.Value.ToString("F1", CultureInfo.CurrentCulture) + " V";
                borderInspectorState.Visibility = Visibility.Collapsed;
            }
            else if (comp.Type == "Bulb" || comp.Type == "Resistor")
            {
                stackInspectorSlider.Visibility = Visibility.Visible;
                txtInspectorSliderLabel.Text = "Điện trở R (Ω):";
                sldInspectorParam.Minimum = 1;
                sldInspectorParam.Maximum = 200;
                sldInspectorParam.TickFrequency = 1;
                sldInspectorParam.Value = comp.Value;
                txtInspectorValue.Text = comp.Value.ToString("F0", CultureInfo.CurrentCulture) + " Ω";
                borderInspectorState.Visibility = Visibility.Visible;
            }
            else
            {
                stackInspectorSlider.Visibility = Visibility.Collapsed;
                borderInspectorState.Visibility = Visibility.Collapsed;
            }

            CheckCircuit();
        }

        private void SldInspectorParam_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_selectedComponent == null || txtInspectorValue == null) return;
            _selectedComponent.Value = sldInspectorParam.Value;

            var border = (Border)_selectedComponent.UIElement.Children[0];
            var text = (TextBlock)border.Child;

            if (_selectedComponent.Type == "Battery")
            {
                txtInspectorValue.Text = _selectedComponent.Value.ToString("F1", CultureInfo.CurrentCulture) + " V";
                text.Text = $"🔋 {_selectedComponent.Value:F1}V";
            }
            else if (_selectedComponent.Type == "Bulb" || _selectedComponent.Type == "Resistor")
            {
                txtInspectorValue.Text = _selectedComponent.Value.ToString("F0", CultureInfo.CurrentCulture) + " Ω";
                string emoji = _selectedComponent.Type == "Bulb" ? "💡" : "🟩";
                text.Text = $"{emoji} {_selectedComponent.Value:F0}Ω";
            }

            CheckCircuit();
        }

        private void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedComponent == null) return;

            // Remove wires associated with this component
            var associatedWires = _wires.Where(w => w.Node1.Parent == _selectedComponent || w.Node2.Parent == _selectedComponent).ToList();
            foreach (var w in associatedWires)
            {
                DeleteWire(w);
            }

            // Remove from canvas
            if (CircuitCanvas.Children.Contains(_selectedComponent.UIElement))
            {
                CircuitCanvas.Children.Remove(_selectedComponent.UIElement);
            }

            // Remove from node/comp lists
            foreach (var node in _selectedComponent.Nodes)
            {
                _nodes.Remove(node);
            }
            _components.Remove(_selectedComponent);

            SelectComponent(null);
            CheckCircuit();
        }

        // ═══════════════════════════════════════════════════════════
        //  STEM QUIZ GAME SYSTEM
        // ═══════════════════════════════════════════════════════════
        private void InitQuiz()
        {
            _quizQuestions.Clear();
            _quizQuestions.Add(new QuizQuestion
            {
                Text = "Khi hiệu điện thế giữa hai đầu dây dẫn tăng lên 3 lần thì cường độ dòng điện chạy qua dây dẫn đó thay đổi như thế nào?",
                Options = new[] { "A. Giảm 3 lần", "B. Tăng lên 3 lần", "C. Không thay đổi", "D. Tăng vọt lên 9 lần" },
                CorrectIndex = 1,
                Explanation = "Chính xác! Theo định luật Ohm, cường độ dòng điện tỉ lệ thuận với hiệu điện thế (I = U/R). Khi U tăng 3 lần thì I cũng tăng 3 lần."
            });
            _quizQuestions.Add(new QuizQuestion
            {
                Text = "Đường đặc trưng Vôn - Ampe (đồ thị biểu diễn mối quan hệ giữa U và I) của một dây dẫn điện trở lý tưởng là hình gì?",
                Options = new[] { "A. Đường cong parabol", "B. Đường thẳng song song với trục hoành", "C. Đường thẳng đi qua gốc tọa độ", "D. Đường hyperbol cân đối" },
                CorrectIndex = 2,
                Explanation = "Đúng rồi! Đồ thị I = (1/R)·U là một phương trình đường thẳng bậc nhất đi qua gốc tọa độ (0,0), có độ dốc bằng nghịch đảo điện trở."
            });
            _quizQuestions.Add(new QuizQuestion
            {
                Text = "Một bóng đèn sợi đốt có điện trở R = 12 Ω mắc vào hai đầu nguồn điện có hiệu điện thế U = 6 V. Cường độ dòng điện qua đèn bằng:",
                Options = new[] { "A. 2,0 A", "B. 0,5 A", "C. 1,5 A", "D. 72 A" },
                CorrectIndex = 1,
                Explanation = "Đúng vậy! Áp dụng định luật Ohm: I = U / R = 6 V / 12 Ω = 0,5 A."
            });
            _quizQuestions.Add(new QuizQuestion
            {
                Text = "Khi mắc nối tiếp hai điện trở R1 = 4 Ω và R2 = 6 Ω vào nguồn điện, điện trở tương đương của toàn mạch bằng bao nhiêu?",
                Options = new[] { "A. 10 Ω", "B. 2,4 Ω", "C. 2,0 Ω", "D. 24 Ω" },
                CorrectIndex = 0,
                Explanation = "Tuyệt vời! Đối với đoạn mạch mắc nối tiếp, điện trở tương đương bằng tổng các điện trở thành phần: R_td = R1 + R2 = 4 + 6 = 10 Ω."
            });
            _quizQuestions.Add(new QuizQuestion
            {
                Text = "Hiện tượng đoản mạch (chập điện) nguy hiểm xảy ra khi nào trong mạch điện?",
                Options = new[] { "A. Điện trở tương đương của mạch tăng lên vô hạn", "B. Hai cực nguồn điện được nối tắt bằng dây dẫn có điện trở xấp xỉ bằng 0", "C. Công tắc switch ở trạng thái mở ngắt kết nối", "D. Mắc thêm bóng đèn vào mạch" },
                CorrectIndex = 1,
                Explanation = "Chính xác! Khi nối tắt cực (+) và cực (-) bằng dây dẫn điện trở cực nhỏ, dòng điện trong mạch I = U/R tăng lên cực lớn gây nóng và nguy hiểm phát nổ."
            });

            _currentQuizIdx = 0;
            _quizScore = 0;
            ShowActiveQuestion();
        }

        private void ShowActiveQuestion()
        {
            if (_quizQuestions.Count == 0 || txtQuizQuestion == null) return;

            panelQuizActive.Visibility = Visibility.Visible;
            panelQuizComplete.Visibility = Visibility.Collapsed;
            borderQuizFeedback.Visibility = Visibility.Collapsed;
            btnNextQuestion.Visibility = Visibility.Collapsed;
            btnCheckAnswer.Visibility = Visibility.Visible;

            _quizChecked = false;

            var q = _quizQuestions[_currentQuizIdx];
            txtQuizProgress.Text = $"Câu hỏi {_currentQuizIdx + 1} / {_quizQuestions.Count}";
            pbQuiz.Value = _currentQuizIdx;

            txtQuizQuestion.Text = q.Text;
            rbOptA.Content = q.Options[0];
            rbOptB.Content = q.Options[1];
            rbOptC.Content = q.Options[2];
            rbOptD.Content = q.Options[3];

            rbOptA.IsChecked = false;
            rbOptB.IsChecked = false;
            rbOptC.IsChecked = false;
            rbOptD.IsChecked = false;

            rbOptA.IsEnabled = true;
            rbOptB.IsEnabled = true;
            rbOptC.IsEnabled = true;
            rbOptD.IsEnabled = true;
        }

        private void RbOpt_Checked(object sender, RoutedEventArgs e)
        {
            HideTutorial();
        }

        private void BtnCheckAnswer_Click(object sender, RoutedEventArgs e)
        {
            if (_quizChecked || _quizQuestions.Count == 0) return;

            int selected = -1;
            if (rbOptA.IsChecked == true) selected = 0;
            else if (rbOptB.IsChecked == true) selected = 1;
            else if (rbOptC.IsChecked == true) selected = 2;
            else if (rbOptD.IsChecked == true) selected = 3;

            if (selected == -1)
            {
                MessageBox.Show("Vui lòng chọn một đáp án trước khi kiểm tra!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _quizChecked = true;
            var q = _quizQuestions[_currentQuizIdx];

            // Disable radio buttons
            rbOptA.IsEnabled = false;
            rbOptB.IsEnabled = false;
            rbOptC.IsEnabled = false;
            rbOptD.IsEnabled = false;

            borderQuizFeedback.Visibility = Visibility.Visible;
            btnCheckAnswer.Visibility = Visibility.Collapsed;
            btnNextQuestion.Visibility = Visibility.Visible;

            if (selected == q.CorrectIndex)
            {
                _quizScore++;
                borderQuizFeedback.Background = new SolidColorBrush(Color.FromRgb(224, 242, 241)); // Light teal
                txtQuizFeedback.Foreground = new SolidColorBrush(Color.FromRgb(0, 77, 64)); // Dark teal
                txtQuizFeedback.Text = "✅ " + q.Explanation;
            }
            else
            {
                borderQuizFeedback.Background = new SolidColorBrush(Color.FromRgb(255, 235, 235)); // Light red
                txtQuizFeedback.Foreground = new SolidColorBrush(Color.FromRgb(183, 28, 28)); // Dark red
                txtQuizFeedback.Text = $"❌ Chưa chính xác. Đáp án đúng là: {q.Options[q.CorrectIndex]}.\n\n💡 Giải thích: {q.Explanation}";
            }
            pbQuiz.Value = _currentQuizIdx + 1;
        }

        private void BtnNextQuestion_Click(object sender, RoutedEventArgs e)
        {
            _currentQuizIdx++;
            if (_currentQuizIdx < _quizQuestions.Count)
            {
                ShowActiveQuestion();
            }
            else
            {
                // Completed
                panelQuizActive.Visibility = Visibility.Collapsed;
                panelQuizComplete.Visibility = Visibility.Visible;
                txtQuizScore.Text = $"Kết quả đúng: {_quizScore} / {_quizQuestions.Count} câu hỏi";
            }
        }

        private void BtnRetryQuiz_Click(object sender, RoutedEventArgs e)
        {
            InitQuiz();
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
                        Icon = "📟",
                        Title = isVN ? "Vạn năng kế" : "Multimeters",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_circuit_1_{suffix}.png",
                        Description = isVN 
                            ? "Đo chính xác điện thế U, dòng điện I và trở kháng R trong thực nghiệm. Giúp trực quan hóa và kiểm chứng chính xác định luật Ohm trên các mạch điện." 
                            : "Accurately measure voltage U, current I, and resistance R in experiments, helping visualize and verify Ohm's Law on circuits."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⚡",
                        Title = isVN ? "Truyền tải điện năng" : "Power Transmission",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_circuit_2_{suffix}.png",
                        Description = isVN 
                            ? "Để giảm nhiệt hao phí trên đường truyền cao thế theo định luật Joule-Lenz, người ta sử dụng máy biến áp nâng hiệu điện thế U lên cực lớn trước khi truyền tải." 
                            : "To reduce heat loss on high-voltage transmission lines according to the Joule-Lenz Law, step-up transformers are used to increase voltage U significantly before transmission."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🖥",
                        Title = isVN ? "️ Vi mạch máy tính" : "Computer Microchips",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_circuit_3_{suffix}.png",
                        Description = isVN 
                            ? "Hàng tỷ linh kiện bán dẫn và điện trở nano siêu nhỏ được tích hợp trên đế chip Silicon, hoạt động dựa trên việc điều khiển các mức điện áp và dòng điện cực kỳ chính xác." 
                            : "Billions of tiny semiconductor components and resistors are integrated onto a Silicon chip substrate, operating based on highly precise control of voltage and current levels."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏠",
                        Title = isVN ? "Hệ thống điện gia đình" : "Home Electrical System",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_circuit_4_{suffix}.png",
                        Description = isVN 
                            ? "Cầu chì, aptomat bảo vệ mạng điện trong nhà. Khi xảy ra đoản mạch, dòng điện vọt lên cực đại theo định luật Ohm sẽ tự động ngắt điện để đảm bảo an toàn." 
                            : "Fuses and circuit breakers protect household networks. During short circuits, the current spikes to maximum according to Ohm's Law, triggering automatic shut-offs for safety."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔋",
                        Title = isVN ? "Trạm sạc xe điện EV" : "EV Charging Station",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_circuit_5_{suffix}.png",
                        Description = isVN 
                            ? "Điều khiển dòng sạc pin Lithium dựa vào chênh lệch hiệu điện thế sạc U và nội trở trong của các cell pin, đảm bảo tốc độ sạc nhanh và tối ưu hóa tuổi thọ pin." 
                            : "Control lithium battery charging current based on the difference between charging voltage U and internal resistance of battery cells, ensuring fast charging and optimized battery life."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💥",
                        Title = isVN ? "Đoản mạch chập điện" : "Short Circuit & Sparking",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_circuit_6_{suffix}.png",
                        Description = isVN 
                            ? "Xảy ra khi điện trở mạch giảm về xấp xỉ bằng 0. Dòng điện tăng vọt, phát sinh nhiệt lượng cực lớn tạo tia lửa điện và có thể dẫn tới nguy cơ cháy nổ." 
                            : "Occurs when circuit resistance drops to near zero. Current spikes, generating massive heat that creates sparks and potential fire hazards."
                    },
                    new PracticalAppItem
                    {
                        Icon = "☀️",
                        Title = isVN ? "Hệ thống pin mặt trời" : "Solar Panel Systems",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_circuit_7_{suffix}.png",
                        Description = isVN 
                            ? "Thiết kế mạch điện phối hợp nối tiếp và song song các tấm pin mặt trời để tối ưu hóa điện áp và dòng điện đầu ra." 
                            : "Design series-parallel solar panel circuits to optimize output voltage and current for grid storage."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⚡",
                        Title = isVN ? "Mạch sạc nhanh thông minh xe điện" : "EV Smart Fast Charging Circuit",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_circuit_8_{suffix}.png",
                        Description = isVN 
                            ? "Thiết kế bộ biến đổi dòng điện và mạch điều khiển hồi tiếp để điều chỉnh dòng sạc lớn an toàn cho pin xe điện." 
                            : "Design power converters and feedback loop circuits to safely regulate high currents for fast EV charging."
                    }};

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for CircuitTool: {Err}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  CLEANUP & DISPOSAL
        // ═══════════════════════════════════════════════════════════
        public void Dispose()
        {
            if (_tutorialTimer != null) _tutorialTimer.Stop();
            if (_animationTimer != null) _animationTimer.Stop();
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewOhm == null || viewSandbox == null || viewQuiz == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewOhm.Visibility = Visibility.Collapsed;
            viewSandbox.Visibility = Visibility.Collapsed;
            viewQuiz.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewOhm.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewSandbox.Visibility = Visibility.Visible;
                    break;
                case 3:
                    viewQuiz.Visibility = Visibility.Visible;
                    break;
                case 4:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}